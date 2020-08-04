namespace PolygonTriangulation
{
    using System;
    using System.Collections.Generic;

#if UNITY_EDITOR || UNITY_STANDALONE
    using Vertex = UnityEngine.Vector2;
#else
    using Vertex = System.Numerics.Vector2;
#endif

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
    /// Marker interface for trapezoid edge tests
    /// </summary>
    internal interface ITestingTrapezoidEdge
    {
    }

    /// <summary>
    /// Splits a polygon into trapezoids and reports necessary splits.
    /// </summary>
    public partial class Trapezoidation
    {
        private const float Epsilon = 1.0E-5f;

        /// <summary>
        /// A comparer for trapezoid edges vs. vertex
        /// </summary>
        private readonly EdgeComparer comparer;

        /// <summary>
        /// Store the active adges with prev/next support
        /// </summary>
        private readonly RedBlackTree<TrapezoidEdge> activeEdges;

        /// <summary>
        /// map the right of the vertex to the active polygon edge. Collisions for closing vertices ar handled by <see cref="EdgeForVertex"/>
        /// </summary>
        private readonly Dictionary<int, TrapezoidEdge> vertexToEdge;

        /// <summary>
        /// the receiver for the detected splits
        /// </summary>
        private readonly IPolygonSplitSink splitSink;

        /// <summary>
        /// Initializes a new instance of the <see cref="Trapezoidation"/> class.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="splitSink">The sink for detected splits.</param>
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
        /// <param name="info">The vertex information.</param>
        public void HandleOpeningCusp(IPolygonVertexInfo info)
        {
            var (lowerEdge, upperEdge) = this.StartNewTrapezoidEdges(
                info.Id,
                info.PrevVertexId,
                info.PrevUnique,
                info.NextVertexId,
                info.NextUnique);
            if (lowerEdge.IsRightToLeft)
            {
                Trapezoid.EnterInsideBySplit(info.Id, lowerEdge, upperEdge);
            }
            else
            {
                var belowEdge = lowerEdge.TreeNode.PrevNode.Data;
                var trapezoid = belowEdge.Trapezoid;
                trapezoid.LeaveInsideBySplit(info.Id, lowerEdge, upperEdge, this.splitSink);
            }
        }

        /// <summary>
        /// Handle a closing cusp. i.e. joins two edges
        /// </summary>
        /// <param name="info">The vertex information.</param>
        public void HandleClosingCusp(IPolygonVertexInfo info)
        {
            var lowerEdge = this.EdgeForVertex(info.Id, info.Unique);
            TrapezoidEdge upperEdge;

            var prevEdge = lowerEdge.TreeNode.PrevNode?.Data;
            if (prevEdge?.Right == lowerEdge.Right && (prevEdge.Left == info.PrevVertexId || prevEdge.Left == info.NextVertexId))
            {
                upperEdge = lowerEdge;
                lowerEdge = prevEdge;
            }
            else
            {
                upperEdge = lowerEdge.TreeNode.NextNode?.Data;
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
                var upperEdge2 = lowerEdge.TreeNode.NextNode.Data;
                var upperTrapezoid = upperEdge2.Trapezoid;
                Trapezoid.EnterInsideByJoin(lowerTrapezoid, upperTrapezoid, info.Id, this.splitSink);
            }

            this.JoinTrapezoidEdges(lowerEdge);
        }

        /// <summary>
        /// A transition from one vertex to the next, where prev&gt;id has the same direction as id&gt;next
        /// </summary>
        /// <param name="info">The vertex information.</param>
        public void HandleTransition(IPolygonVertexInfo info)
        {
            var oldEdge = this.EdgeForVertex(info.Id, info.Unique);
            var trapezoid = oldEdge.Trapezoid;
            if (oldEdge.IsRightToLeft)
            {
                var newEdge = this.Transition(oldEdge, info.PrevVertexId, info.PrevUnique);
                trapezoid.TransitionOnLowerEdge(info.Id, newEdge, this.splitSink);
            }
            else
            {
                var newEdge = this.Transition(oldEdge, info.NextVertexId, info.NextUnique);
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
        /// <param name="edge">the previous edge</param>
        /// <param name="newTarget">the new target vertex id</param>
        /// <returns>the new edge</returns>
        internal ITestingTrapezoidEdge TestTransition(ITestingTrapezoidEdge edge, int newTarget)
        {
            if (edge is TrapezoidEdge trapezoidEdge)
            {
                return this.Transition(trapezoidEdge, newTarget, newTarget);
            }

            throw new InvalidOperationException("Invalid use of internal test function");
        }

        /// <summary>
        /// Two edges join in a final vertex
        /// </summary>
        /// <param name="lower">the lower edge</param>
        internal void TestJoin(ITestingTrapezoidEdge lower)
        {
            if (lower is TrapezoidEdge trapezoidEdge)
            {
                this.JoinTrapezoidEdges(trapezoidEdge);
                return;
            }

            throw new InvalidOperationException("Invalid use of internal test function");
        }

        /// <summary>
        /// Insert two edges starting in one point.
        /// </summary>
        /// <param name="start">the id of the common start vertex (left)</param>
        /// <param name="prev">the end vertex of the lower edge</param>
        /// <param name="prevUnique">the uniqe id of the prev vertex</param>
        /// <param name="next">the end vertex of the upper edge</param>
        /// <param name="nextUnique">the uniqe id of the next vertex</param>
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
        /// <param name="edge">the existing edge</param>
        /// <param name="newTarget">the new target vertex</param>
        /// <param name="targetUnique">the uniqe id of newTarget</param>
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
            var nextNode = lowerEdge.TreeNode.NextNode;
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
        /// <param name="vertexId">the vertex id</param>
        /// <param name="vertexUniqe">the uniqe id of the vertex</param>
        /// <returns>the edge</returns>
        private TrapezoidEdge EdgeForVertex(int vertexId, int vertexUniqe)
        {
            if (!this.vertexToEdge.TryGetValue(vertexUniqe, out var edge))
            {
                throw new InvalidOperationException($"Can't find edge for vertex {vertexId} at {vertexUniqe}");
            }

            return edge;
        }
    }
}
