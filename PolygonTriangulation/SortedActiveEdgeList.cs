namespace PolygonTriangulation
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using Vertex = System.Numerics.Vector2;

    /// <summary>
    /// An active edge sorted between all other edges.
    /// </summary>
    /// <typeparam name="TData">the type of the stored data</typeparam>
    public interface IActiveEdge<TData>
    {
        /// <summary>
        /// Gets the direction of the edge
        /// </summary>
        bool IsRightToLeft { get; }

        /// <summary>
        /// Gets the data of the edge below
        /// </summary>
        TData BelowData { get; }

        /// <summary>
        /// Gets the data of the edge above
        /// </summary>
        TData AboveData { get; }

        /// <summary>
        /// Gets or sets the associated data. Never modified by the <see cref="SortedActiveEdgeList{TData}"/>.
        /// </summary>
        TData Data { get; set; }
    }

    /// <summary>
    /// A list with active non-overlapping edges sorted by the y coordinate.
    /// </summary>
    /// <typeparam name="TData">The type of data to store per edge</typeparam>
    public class SortedActiveEdgeList<TData>
    {
        const float epsilon = 1.0E-5f;
        private readonly EdgeComparer comparer;
        private readonly RedBlackTree<Edge> tree;
        private readonly Dictionary<int, Edge> vertexToEdge;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="vertices">the coordinates of the vertices</param>
        public SortedActiveEdgeList(IReadOnlyList<Vertex> vertices)
        {
            this.vertexToEdge = new Dictionary<int, Edge>();
            this.comparer = new EdgeComparer(vertices);
            this.tree = new RedBlackTree<Edge>(this.comparer);
        }

        /// <summary>
        /// Gets all edges starting from the lowest
        /// </summary>
        public IEnumerable<IActiveEdge<TData>> Edges => this.tree.Items;

        /// <summary>
        /// Gets the active edge with right point == vertexId. If there are two edges, return the lower one.
        /// </summary>
        /// <param name="vertexId">the vertex id</param>
        /// <returns>the edge</returns>
        public IActiveEdge<TData> EdgeForVertex(int vertexId)
        {
            var edge = this.vertexToEdge[vertexId];

            var prevEdge = edge.TreeNode.Prev?.Data;
            if (prevEdge?.Right == edge.Right)
            {
                return prevEdge;
            }

            return edge;
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
        public (IActiveEdge<TData> lower, IActiveEdge<TData> upper) Begin(int start, int prev, int next)
        {
            var (lower, upper) = this.CreateAndSortPairOfEdges(start, prev, next);
            (lower.TreeNode, upper.TreeNode) = this.tree.AddPair(lower, upper);

            this.vertexToEdge[upper.Right] = upper;
            this.vertexToEdge[lower.Right] = lower;

            return (lower, upper);
        }

        /// <summary>
        /// Create a pair of edges with a common start point. The returned edges are sorted by the end point
        /// </summary>
        /// <param name="start">start vertex</param>
        /// <param name="prev">previous vertex, always > start</param>
        /// <param name="next">next vertex, always > start</param>
        /// <returns>the lower edge</returns>
        private (Edge lower, Edge upper) CreateAndSortPairOfEdges(int start, int prev, int next)
        {
            var prevEdge = new Edge(start, prev, true);
            var nextEdge = new Edge(start, next, false);

            if (this.comparer.IsVertexAbove(prevEdge.Right, nextEdge))
            {
                return (nextEdge, prevEdge);
            }
            else
            {
                return (prevEdge, nextEdge);
            }
        }

        /// <summary>
        /// transition from one edge to the next
        /// </summary>
        /// <param name="previousStart"></param>
        /// <param name="start"></param>
        /// <param name="target"></param>
        /// <returns>the new edge</returns>
        public IActiveEdge<TData> Transition(IActiveEdge<TData> edge, int newTarget)
        {
            this.tree.Validate();
            var currentEdge = (Edge)edge;
            var nextEdge = new Edge(currentEdge.Right, newTarget, currentEdge.IsRightToLeft)
            {
                Data = currentEdge.Data,
            };

            nextEdge.TreeNode = this.tree.ReplaceNode(currentEdge.TreeNode, nextEdge);

            this.vertexToEdge.Remove(currentEdge.Right);
            this.vertexToEdge[nextEdge.Right] = nextEdge;

            this.tree.Validate();
            return nextEdge;
        }

        /// <summary>
        /// Two edges join in a final vertex
        /// </summary>
        /// <param name="lowerEdge">the lower edge</param>
        public void Finish(IActiveEdge<TData> lower)
        {
            var lowerEdge = (Edge)lower;

            this.tree.Validate();
            this.tree.RemoveNode(lowerEdge.TreeNode.Next);
            this.tree.Validate();
            this.tree.RemoveNode(lowerEdge.TreeNode);
            this.tree.Validate();

            this.vertexToEdge.Remove(lowerEdge.Right);
        }

        /// <summary>
        /// Compares two edges
        /// </summary>
        private class EdgeComparer : IComparer<Edge>
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
            public int Compare(Edge value, Edge storage)
            {
                var vertexOfValue = value.Left == storage.Left ? value.Right : value.Left;
                return this.IsVertexAbove(vertexOfValue, storage) ? 1 : -1;
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
            public bool IsVertexAbove(int vertexId, Edge edge)
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

                // during a start operation, the start.Y will always be larger than left.Y and right.Y of a vertical edge, 
                // otherwise start would have been sorted between left and right. So it's no difference to test against left.Y or right.Y
                if (xSpan < epsilon)
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
        private class Edge : IActiveEdge<TData>
        {
            public Edge(int left, int right, bool isRightToLeft)
            {
                this.IsRightToLeft = isRightToLeft;
                this.Left = left;
                this.Right = right;
                this.IsNone = left < 0 && right < 0;
            }

            /// <inheritdoc/>
            public bool IsRightToLeft { get; }

            /// <inheritdoc/>
            public int Left { get; }

            /// <inheritdoc/>
            public int Right { get; }

            /// <inheritdoc/>
            public bool IsNone { get; }

            /// <summary>
            /// The storage position in the tree
            /// </summary>
            public IOrderedNode<Edge> TreeNode { get; set; }

            /// <inheritdoc/>
            public TData Data { get; set; }

            /// <inheritdoc/>
            public TData BelowData => this.TreeNode.Prev.Data.Data;

            /// <inheritdoc/>
            public TData AboveData => this.TreeNode.Next.Data.Data;

            public override string ToString()
            {
                return $"{this.Left}{(this.IsRightToLeft ? "<" : ">")}{this.Right}";
            }
        }
    }
}
