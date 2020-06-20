namespace PolygonTriangulation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using Vertex = System.Numerics.Vector2;

    // Marker interface for trapezoid edge tests
    internal interface ITestingTrapezoidEdge
    {
    }

    /// <summary>
    /// The receiver of split commands
    /// </summary>
    public interface IPolygonSplitSink
    {
        /// <summary>
        /// Split the polygon between left and right vertex
        /// </summary>
        /// <param name="leftVertex">the left vertex</param>
        /// <param name="rightVertex">the right vertex</param>
        void SplitPolygon(int leftVertex, int rightVertex);
    }

    /// <summary>
    /// Splits a polygon into trapezoids and reports necessary splits.
    /// </summary>
    public class Trapezoidation
    {
        const float epsilon = 1.0E-5f;
        /// <summary>
        /// A comparer for trapezoid edges vs. vertex
        /// </summary>
        private readonly EdgeComparer comparer;

        /// <summary>
        /// Store the active adges with prev/next support
        /// </summary>
        private readonly RedBlackTree<TrapezoidEdge> activeEdges;

        /// <summary>
        /// map the right of the vertex to the active polygon edge. Collisions for closing vertices ar handled by <see cref="EdgeForVertex(int)"/>
        /// </summary>
        private readonly Dictionary<int, TrapezoidEdge> vertexToEdge;

        /// <summary>
        /// the receiver for the detected splits
        /// </summary>
        private readonly IPolygonSplitSink splitSink;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="vertices">the coordinates of the vertices</param>
        public Trapezoidation(IReadOnlyList<Vertex> vertices, IPolygonSplitSink splitSink)
        {
            this.vertexToEdge = new Dictionary<int, TrapezoidEdge>();
            this.comparer = new EdgeComparer(vertices);
            this.activeEdges = new RedBlackTree<TrapezoidEdge>(this.comparer);
            this.splitSink = splitSink;
        }

        /// <summary>
        /// Gets all edges starting from the lowest
        /// </summary>
        internal IEnumerable<ITestingTrapezoidEdge> Edges => this.activeEdges.Items;

        /// <summary>
        /// Handle an opening cusp. i.e. starts two new edges.
        /// </summary>
        /// <param name="id">the id of the cusp vertex</param>
        /// <param name="prev">the id of previous polygon vertex</param>
        /// <param name="next">the id of the next polygon vertex</param>
        public void HandleOpeningCusp(IPolygonVertexInfo info)
        {
            var (lowerEdge, upperEdge) = this.StartNewTrapezoidEdges(
                info.Id,
                info.Prev,
                info.PrevUnique,
                info.Next,
                info.NextUnique);
            if (lowerEdge.IsRightToLeft)
            {
                Trapezoid.EnterInsideBySplit(info.Id, lowerEdge, upperEdge, this.splitSink);
            }
            else
            {
                var belowEdge = this.activeEdges.Prev(lowerEdge.TreeNode).Data;
                var trapezoid = belowEdge.Trapezoid;
                trapezoid.LeaveInsideBySplit(info.Id, lowerEdge, upperEdge, this.splitSink);
            }
        }

        /// <summary>
        /// Handle a closing cusp. i.e. joins two edges
        /// </summary>
        /// <param name="id">the id of the cusp vertex</param>
        /// <param name="prev">the id of previous polygon vertex</param>
        /// <param name="next">the id of the next polygon vertex</param>
        public void HandleClosingCusp(IPolygonVertexInfo info)
        {
            var lowerEdge = this.EdgeForVertex(info.Id, info.Unique);
            TrapezoidEdge upperEdge;

            var prevEdge = this.activeEdges.Prev(lowerEdge.TreeNode)?.Data;
            if (prevEdge?.Right == lowerEdge.Right)
            {
                upperEdge = lowerEdge;
                lowerEdge = prevEdge;
            }
            else
            {
                upperEdge = this.activeEdges.Next(lowerEdge.TreeNode)?.Data;
            }

            if (lowerEdge.Right != upperEdge?.Right)
            {
                throw new InvalidOperationException($"Invalid join of edges lower: {lowerEdge} and upper: {upperEdge}");
            }

            var lowerTrapezoid = lowerEdge.Trapezoid;
            if (lowerEdge.IsRightToLeft)
            {
                lowerTrapezoid.LeaveInsideByJoin(info.Id, this.splitSink);
            }
            else
            {
                var upperEdge2 = this.activeEdges.Next(lowerEdge.TreeNode).Data;
                var upperTrapezoid = upperEdge2.Trapezoid;
                Trapezoid.EnterInsideByJoin(lowerTrapezoid, upperTrapezoid, info.Id, this.splitSink);
            }

            this.JoinTrapezoidEdges(lowerEdge);
        }

        /// <summary>
        /// A transition from one vertex to the next, where prev>id has the same direction as id>next
        /// </summary>
        /// <param name="id">the id of the current vertex</param>
        /// <param name="prev">the id of previous polygon vertex</param>
        /// <param name="next">the id of the next polygon vertex</param>
        public void HandleTransition(IPolygonVertexInfo info)
        {
            var oldEdge = this.EdgeForVertex(info.Id, info.Unique);
            var trapezoid = oldEdge.Trapezoid;
            if (oldEdge.IsRightToLeft)
            {
                var newEdge = this.Transition(oldEdge, info.Prev, info.PrevUnique);
                trapezoid.TransitionOnLowerEdge(info.Id, newEdge, this.splitSink);
            }
            else
            {
                var newEdge = this.Transition(oldEdge, info.Next, info.NextUnique);
                trapezoid.TransitionOnUpperEdge(info.Id, newEdge, this.splitSink);
            }
        }

        /// <summary>
        /// Insert two edges starting in one point.
        /// </summary>
        /// <param name="start">the index of the starting vertex</param>
        /// <param name="prev">the end index of the lower edge</param>
        /// <param name="next">the end index of the upper edge </param>
        /// <returns>(lower edge, upper edge)</returns>
        /// <remarks>
        /// There is never a Begin() where the prev or next is left to start or prev is at same X and below start.
        /// </remarks>
        internal (ITestingTrapezoidEdge lower, ITestingTrapezoidEdge upper) TestBegin(int start, int prev, int next)
        {
            return this.StartNewTrapezoidEdges(start, prev, prev, next, next);
        }

        /// <summary>
        /// transition from one edge to the next
        /// </summary>
        /// <param name="previousStart"></param>
        /// <param name="start"></param>
        /// <param name="target"></param>
        /// <returns>the new edge</returns>
        internal ITestingTrapezoidEdge TestTransition(ITestingTrapezoidEdge edge, int newTarget)
        {
            return this.Transition((TrapezoidEdge)edge, newTarget, newTarget);
        }

        /// <summary>
        /// Two edges join in a final vertex
        /// </summary>
        /// <param name="lowerEdge">the lower edge</param>
        internal void TestJoin(ITestingTrapezoidEdge lower)
        {
            this.JoinTrapezoidEdges((TrapezoidEdge)lower);
        }

        /// <summary>
        /// Insert two edges starting in one point.
        /// </summary>
        /// <param name="start">the index of the starting vertex</param>
        /// <param name="prev">the end index of the lower edge</param>
        /// <param name="next">the end index of the upper edge </param>
        /// <returns>(lower edge, upper edge)</returns>
        /// <remarks>
        /// There is never a Begin() where the prev or next is left to start or prev is at same X and below start.
        /// </remarks>
        private (TrapezoidEdge lower, TrapezoidEdge upper) StartNewTrapezoidEdges(int start, int prev, int prevUnique, int next, int nextUnique)
        {
            var lower = new TrapezoidEdge(start, prev, prevUnique, true);
            var upper = new TrapezoidEdge(start, next, nextUnique, false);

            if (!this.comparer.EdgeOrderingWithCommonLeftIsCorrect(lower, upper))
            {
                (lower, upper) = (upper, lower);
            }

            (lower.TreeNode, upper.TreeNode) = this.activeEdges.AddPair(lower, upper);

            this.StoreEdge(lower);
            this.StoreEdge(upper);

            return (lower, upper);
        }

        /// <summary>
        /// transition from one edge to the next
        /// </summary>
        /// <param name="previousStart"></param>
        /// <param name="start"></param>
        /// <param name="target"></param>
        /// <returns>the new edge</returns>
        private TrapezoidEdge Transition(TrapezoidEdge edge, int newTarget, int targetUnique)
        {
            var nextEdge = new TrapezoidEdge(edge.Right, newTarget, targetUnique, edge.IsRightToLeft)
            {
                Trapezoid = edge.Trapezoid,
            };

            nextEdge.TreeNode = this.activeEdges.ReplaceNode(edge.TreeNode, nextEdge);

            this.vertexToEdge.Remove(edge.RightUnique);
            this.StoreEdge(nextEdge);

            return nextEdge;
        }

        /// <summary>
        /// Two edges join in a final vertex
        /// </summary>
        /// <param name="lowerEdge">the lower edge</param>
        private void JoinTrapezoidEdges(TrapezoidEdge lowerEdge)
        {
            var nextNode = this.activeEdges.Next(lowerEdge.TreeNode);
            this.activeEdges.RemoveNode(nextNode);
            this.activeEdges.RemoveNode(lowerEdge.TreeNode);

            this.vertexToEdge.Remove(lowerEdge.RightUnique);
        }

        /// <summary>
        /// Store the edge for direct lookup
        /// </summary>
        /// <param name="edge">the edge</param>
        private void StoreEdge(TrapezoidEdge edge)
        {
            this.vertexToEdge[edge.RightUnique] = edge;
        }

        /// <summary>
        /// Gets the active edge with right point == vertexId. If there are two edges, return the lower one.
        /// </summary>
        /// <param name="vertexUniqe">the vertex id</param>
        /// <returns>the edge</returns>
        private TrapezoidEdge EdgeForVertex(int vertexId, int vertexUniqe)
        {
            if (!this.vertexToEdge.TryGetValue(vertexUniqe, out var edge))
            {
                throw new InvalidOperationException($"Can't find edge for vertex {vertexId} at {vertexUniqe}");
            }

            return edge;
        }

        /// <summary>
        /// Compares two edges
        /// </summary>
        private class EdgeComparer : IComparer<TrapezoidEdge>
        {
            private readonly IReadOnlyList<Vertex> vertices;

            /// <summary>
            /// Initializes a new <see cref="EdgeComparer"/>
            /// </summary>
            /// <param name="vertices">the real vertices referenced by vertex ids</param>
            public EdgeComparer(IReadOnlyList<Vertex> vertices)
            {
                this.vertices = vertices;
            }

            /// <summary>
            /// Test if the left vertex of value is above storage.
            /// </summary>
            /// <param name="value">the current added value</param>
            /// <param name="storage">the edge that is already part of the tree</param>
            /// <returns>a comparison result</returns>
            public int Compare(TrapezoidEdge value, TrapezoidEdge storage)
            {
                var vertexOfValue = value.Left == storage.Left ? value.Right : value.Left;
                return this.IsVertexAbove(vertexOfValue, storage) ? 1 : -1;
            }

            /// <summary>
            /// Test if the ordering of the edges is correct, both edges have a common point on the left
            /// </summary>
            /// <param name="lower">the lower edge</param>
            /// <param name="upper">the upper edge</param>
            /// <returns>true if upper is above lower</returns>
            /// <remarks>
            /// take the wider edge (larger X span) to avoid a large slope.
            /// </remarks>
            public bool EdgeOrderingWithCommonLeftIsCorrect(TrapezoidEdge lower, TrapezoidEdge upper)
            {
                var left = this.vertices[upper.Left];
                var upperRight = this.vertices[upper.Right];
                var lowerRight = this.vertices[lower.Right];

                if ((upperRight.Y > left.Y) != (lowerRight.Y > left.Y))
                {
                    return upperRight.Y > lowerRight.Y;
                }

                if (upperRight.X > lowerRight.X)
                {
                    if (upperRight.Y > left.Y && upperRight.Y < lowerRight.Y)
                    {
                        return false;
                    }

                    return !this.IsVertexAboveSlow(ref lowerRight, ref left, ref upperRight);
                }
                else
                {
                    if (lowerRight.Y < left.Y && lowerRight.Y < upperRight.Y)
                    {
                        return true;
                    }

                    return this.IsVertexAboveSlow(ref upperRight, ref left, ref lowerRight);
                }
            }

            /// <summary>
            /// Test if the vertex is above the line that is formed by the edge
            /// </summary>
            /// <param name="vertexId"></param>
            /// <param name="edge"></param>
            /// <returns>true if the vertex is above the edge</returns>
            /// <remarks>
            /// This is called only during insert operations, therefore value.left > storage.left.
            /// Try to find the result without calculation first, then calculate the storage.Y at value.Left.X
            /// </remarks>
            private bool IsVertexAbove(int vertexId, TrapezoidEdge edge)
            {
                var vertex = vertices[vertexId];
                var left = vertices[edge.Left];
                var right = vertices[edge.Right];

                // this is very likely as the points are added in order left to right
                if (vertex.X >= left.X)
                {
                    if (vertex.Y > left.Y)
                    {
                        if (left.Y >= right.Y || (vertex.X < right.X && vertex.Y > right.Y))
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (left.Y < right.Y || (vertex.X < right.X && vertex.Y < right.Y))
                        {
                            return false;
                        }
                    }
                }

                return this.IsVertexAboveSlow(ref vertex, ref left, ref right);
            }

            /// <summary>
            /// Test if the vertex is above this edge by calculating the edge.Y at vertex.X
            /// </summary>
            /// <param name="vertexId">the id of the vertex</param>
            /// <param name="vertices">the vertex list</param>
            /// <returns>true if the verex is above</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool IsVertexAboveSlow(ref Vertex vertex, ref Vertex left, ref Vertex right)
            {
                var xSpan = right.X - left.X;

                if (xSpan < epsilon * epsilon)
                {
                    return vertex.Y > left.Y;
                }

                var yOfEdgeAtVertex = (vertex.X - left.X) / xSpan * (right.Y - left.Y) + left.Y;
                return yOfEdgeAtVertex < vertex.Y;
            }
        }

        /// <summary>
        /// Internal representation of an edge
        /// </summary>
        private class TrapezoidEdge : ITestingTrapezoidEdge
        {
            public TrapezoidEdge(int left, int right, int rightUnique, bool isRightToLeft)
            {
                this.IsRightToLeft = isRightToLeft;
                this.Left = left;
                this.Right = right;
                this.RightUnique = rightUnique;
            }

            /// <inheritdoc/>
            public bool IsRightToLeft { get; }

            /// <inheritdoc/>
            public int Left { get; }

            /// <inheritdoc/>
            public int Right { get; }

            /// <summary>
            /// The unique id of the right vertex
            /// </summary>
            public int RightUnique { get; }

            /// <summary>
            /// The storage position in the tree
            /// </summary>
            public IOrderedNode<TrapezoidEdge> TreeNode { get; set; }

            /// <summary>
            /// Gets or sets the current associated trapezoid.
            /// </summary>
            public Trapezoid Trapezoid { get; set; }

            /// <inheritdoc/>
            public override string ToString()
            {
                return $"{this.Left}{(this.IsRightToLeft ? "<" : ">")}{this.Right}";
            }
        }

        /// <summary>
        /// Create trapezoids by splitting, joining and traversing along a polygon.
        /// Each trapezoid has a left and a right base and an upper and lower edge.
        /// The left/right base lines are vertical, parallel and the X coordinate is defined by the associated vertex.
        /// Trapezoids are built from left to right.
        /// Each edge, the upper and the lower, has one current trapezoid.
        /// </summary>
        [DebuggerDisplay("{Debug}")]
        private class Trapezoid
        {
            /// <summary>
            /// The neighbor state of the left/right base line
            /// </summary>
            [Flags]
            private enum Base
            {
                /// <summary>
                /// No neighbor, i.e. a triangle.
                /// </summary>
                NoNeighbor = 1,

                /// <summary>
                /// One neighbor and the common vertex is on the upper corner.
                /// </summary>
                UpperCorner = 2,

                /// <summary>
                /// One neighbor and the common vertex is on the lower corner.
                /// </summary>
                LowerCorner = 4,

                /// <summary>
                /// The baseline has two neighbors, the associated vertex is somewhere in the middle of the base.
                /// </summary>
                TwoNeighbors = 8,
            }

            /// <summary>
            /// Gets the upper edge
            /// </summary>
            private readonly TrapezoidEdge upperEdge;

            /// <summary>
            /// Gets the lower edge
            /// </summary>
            private readonly TrapezoidEdge lowerEdge;

            /// <summary>
            /// The neighbor state of the left base and <see cref="leftVertex"/>.
            /// </summary>
            private readonly Base leftBase;

            /// <summary>
            /// Gets the index of the left vertex, defining the left base.
            /// </summary>
            private readonly int leftVertex;

            /// <summary>
            /// Initialize a new trapzoid
            /// </summary>
            /// <param name="leftVertex">the id of the vertex for the left "virtual trapezoid" edge</param>
            /// <param name="leftNeighborCount">number of left trapezoids</param>
            /// <param name="leftCornerValidity">the validity of the leftVertex. Can be Upper, Lower or None for Cusps</param>
            /// <param name="lowerEdge">the lower edge</param>
            /// <param name="upperEdge">the upper edge</param>
            private Trapezoid(int leftVertex, Base leftBase, TrapezoidEdge lowerEdge, TrapezoidEdge upperEdge)
            {
                this.leftBase = leftBase;
                this.leftVertex = leftVertex;

                this.lowerEdge = lowerEdge;
                this.upperEdge = upperEdge;
                this.lowerEdge.Trapezoid = this;
                this.upperEdge.Trapezoid = this;
            }

            /// <summary>
            /// Gets a debug string
            /// </summary>
            public string Debug => $"Left:{this.leftVertex} {this.leftBase} Low: {this.lowerEdge} High: {this.upperEdge}";

            /// <summary>
            /// A left pointing cusp that enters the polygon space.
            /// </summary>
            /// <param name="vertexId">the vertex id of the cusp</param>
            /// <param name="lowerEdge">the lower edge of the new split</param>
            /// <param name="upperEdge">the upper edge of the new split</param>
            /// <param name="splitSink">the polygon splitter</param>
            public static void EnterInsideBySplit(int vertexId, TrapezoidEdge lowerEdge, TrapezoidEdge upperEdge, IPolygonSplitSink splitSink)
            {
                new Trapezoid(vertexId, Base.NoNeighbor, lowerEdge, upperEdge);
            }

            /// <summary>
            /// A right pointing cusp that enters the polygon space. Join the upper left and the lower left trapezoids in one.
            /// </summary>
            /// <param name="vertexId">the vertex id that joins the two edges.</param>
            /// <param name="lower">the left lower trapezoid</param>
            /// <param name="upper">the left upper trapezoid</param>
            /// <param name="splitSink">the polygon splitter</param>
            public static void EnterInsideByJoin(Trapezoid lower, Trapezoid upper, int vertexId, IPolygonSplitSink splitSink)
            {
                upper.EvaluateRight(vertexId, Base.LowerCorner, splitSink);
                lower.EvaluateRight(vertexId, Base.UpperCorner, splitSink);

                new Trapezoid(vertexId, Base.TwoNeighbors, lower.lowerEdge, upper.upperEdge);
            }

            /// <summary>
            /// A cusp that transitions from inside to outside. Splits the Trapezoid by one point.
            /// </summary>
            /// <param name="vertexId">the vertex id of the start point</param>
            /// <param name="lowerEdge">the lower edge of the new split</param>
            /// <param name="upperEdge">the upper edge of the new split</param>
            /// <param name="splitSink">the polygon splitter</param>
            public void LeaveInsideBySplit(int vertexId, TrapezoidEdge lowerEdge, TrapezoidEdge upperEdge, IPolygonSplitSink splitSink)
            {
                this.EvaluateRight(vertexId, Base.TwoNeighbors, splitSink);

                new Trapezoid(vertexId, Base.LowerCorner, upperEdge, this.upperEdge);
                new Trapezoid(vertexId, Base.UpperCorner, this.lowerEdge, lowerEdge);
            }

            /// <summary>
            /// Join two edges. Right of the vertex is outside.
            /// </summary>
            /// <param name="vertexId">the closing vertex id</param>
            /// <param name="splitSink">the polygon splitter</param>
            public void LeaveInsideByJoin(int vertexId, IPolygonSplitSink splitSink)
            {
                this.EvaluateRight(vertexId, Base.NoNeighbor, splitSink);
            }

            /// <summary>
            /// The upper edge transitions at vertex to a new edge
            /// </summary>
            /// <param name="vertexId">the transition vertex</param>
            /// <param name="nextEdge">the new edge</param>
            /// <param name="splitSink">the polygon splitter</param>
            public void TransitionOnUpperEdge(int vertexId, TrapezoidEdge nextEdge, IPolygonSplitSink splitSink)
            {
                this.EvaluateRight(vertexId, Base.UpperCorner, splitSink);

                new Trapezoid(vertexId, Base.UpperCorner, this.lowerEdge, nextEdge);
            }

            /// <summary>
            /// The lower edge transitions at vertex to a new edge
            /// </summary>
            /// <param name="vertexId">the transition vertex</param>
            /// <param name="nextEdge">the new edge</param>
            /// <param name="splitSink">the polygon splitter</param>
            public void TransitionOnLowerEdge(int vertexId, TrapezoidEdge nextEdge, IPolygonSplitSink splitSink)
            {
                this.EvaluateRight(vertexId, Base.LowerCorner, splitSink);

                new Trapezoid(vertexId, Base.LowerCorner, nextEdge, this.upperEdge);
            }

            /// <summary>
            /// Detects whether the left and right vertex represent a diagonale of the trapezoid.
            /// </summary>
            /// <param name="combinedBase">the combined base line state</param>
            /// <returns>true if a diagonale is detected</returns>
            private static bool DetectDiagonale(Base combinedBase)
            {
                return combinedBase == (Base.LowerCorner | Base.UpperCorner);
            }

            /// <summary>
            /// Detects whether one side has two neighbors (i.e. a touching cusp).
            /// </summary>
            /// <param name="combinedBase">the combined base line state</param>
            /// <returns>true if any base line has two neighbors</returns>
            private static bool DetectDoubleNeighbor(Base combinedBase)
            {
                return (combinedBase & Base.TwoNeighbors) != 0;
            }

            /// <summary>
            /// Combine the right side info with the left side and evaluate if it matches a split situation.
            /// </summary>
            /// <param name="rightVertex">the vertex that defines the right side</param>
            /// <param name="neighborCount">the number of right neighbors</param>
            /// <param name="cornerValidity">the vertex position of the right vertex</param>
            private void EvaluateRight(int rightVertex, Base rightBase, IPolygonSplitSink splitter)
            {
                var combinedBase = this.leftBase | rightBase;
                if (DetectDoubleNeighbor(combinedBase) || DetectDiagonale(combinedBase))
                {
                    splitter.SplitPolygon(this.leftVertex, rightVertex);
                }
            }
        }
    }
}
