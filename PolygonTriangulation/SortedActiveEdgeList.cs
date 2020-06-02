namespace PolygonTriangulation
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using Vertex = System.Numerics.Vector2;

    /// <summary>
    /// An active edge sorted between all other edges. IsNone is set in the lowest and highest edge
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
        private readonly IReadOnlyList<Vertex> vertices;
        private readonly Edge upperNone;
        private readonly Edge lowerNone;
        private Dictionary<int, Edge> vertexToEdge;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="vertices">the coordinates of the vertices</param>
        public SortedActiveEdgeList(IReadOnlyList<Vertex> vertices)
        {
            this.vertices = vertices;
            this.vertexToEdge = new Dictionary<int, Edge>();

            this.upperNone = new Edge(-1, -1, false);
            this.lowerNone = new Edge(-1, -1, false);
            this.lowerNone.Above = this.upperNone;
        }

        /// <summary>
        /// Gets all edges starting from the lowest
        /// </summary>
        public IEnumerable<IActiveEdge<TData>> Edges
        {
            get
            {
                for (var edge = this.lowerNone.Above; !edge.IsNone; edge = edge.Above)
                {
                    yield return edge;
                }
            }
        }

        /// <summary>
        /// Gets the active edge with right point == vertexId. If there are two edges, return the lower one.
        /// </summary>
        /// <param name="vertexId">the vertex id</param>
        /// <returns>the edge</returns>
        public IActiveEdge<TData> EdgeForVertex(int vertexId)
        {
            var edge = this.vertexToEdge[vertexId];
            if (edge.Below.Right == vertexId)
            {
                return edge.Below;
            }

            return edge;
        }

        /// <summary>
        /// Insert two edges starting in one point.
        /// </summary>
        /// <param name="start">the index of the starting vertex</param>
        /// <param name="prev">the end index of the lower edge</param>
        /// <param name="next">the end index of the upper edge </param>
        /// <param name="reversed">false: the direction is lowerTarget->start->upperTarget</param>
        /// <returns>(lower edge, upper edge)</returns>
        /// <remarks>
        /// There is never a Begin() where the prev or next is left to start or prev is at same X and below start.
        /// </remarks>
        public (IActiveEdge<TData> lower, IActiveEdge<TData> upper) Begin(int start, int prev, int next)
        {
            var lower = this.CreateAndSortPairOfEdges(start, prev, next);

            var below = this.FindEdgeBelowVertex(start);
            below.InsertAbove(lower);

            var upper = lower.Above;
            this.vertexToEdge[upper.Right] = upper;
            this.vertexToEdge[lower.Right] = lower;

            return (lower, upper);
        }

        /// <summary>
        /// Find the edge that is below the vertex. Edge.Above is above the vertex.
        /// </summary>
        /// <param name="vertexId">the id of the vertex</param>
        /// <returns>the edge below the vertex.</returns>
        private Edge FindEdgeBelowVertex(int vertexId)
        {
            // superslow.....
            for (var candidate = this.upperNone.Below; !candidate.IsNone; candidate = candidate.Below)
            {
                if (candidate.IsVertexAbove(vertexId, this.vertices))
                {
                    return candidate;
                }
            }

            return this.lowerNone;
        }

        /// <summary>
        /// Create a pair of edges with a common start point. The returned edges are sorted by the end point
        /// </summary>
        /// <param name="start">start vertex</param>
        /// <param name="prev">previous vertex, always > start</param>
        /// <param name="next">next vertex, always > start</param>
        /// <returns>the lower edge</returns>
        private Edge CreateAndSortPairOfEdges(int start, int prev, int next)
        {
            var prevEdge = new Edge(start, prev, true);
            var nextEdge = new Edge(start, next, false);

            if (prevEdge.IsVertexAbove(next, this.vertices))
            {
                prevEdge.Above = nextEdge;
                return prevEdge;
            }
            else
            {
                nextEdge.Above = prevEdge;
                return nextEdge;
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
            var currentEdge = (Edge)edge;
            var nextEdge = currentEdge.CreateTransition(newTarget);

            this.vertexToEdge.Remove(currentEdge.Right);
            this.vertexToEdge[nextEdge.Right] = nextEdge;

            return nextEdge;
        }

        /// <summary>
        /// Two edges join in a final vertex
        /// </summary>
        /// <param name="lowerEdge">the lower edge</param>
        public void Finish(IActiveEdge<TData> lower)
        {
            var lowerEdge = (Edge)lower;
            lowerEdge.Below.Above = lowerEdge.Above.Above;

            this.vertexToEdge.Remove(lowerEdge.Right);
        }

        /// <summary>
        /// Internal representation of an edge
        /// </summary>
        private class Edge : IActiveEdge<TData>
        {
            private Edge above;

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
            /// Gets or sets the edge below this edge
            /// </summary>
            public Edge Below { get; private set; }

            /// <summary>
            /// Gets or sets the edge above this edge
            /// </summary>
            public Edge Above 
            { 
                get => this.above;
                set
                {
                    this.above = value;
                    value.Below = this;
                }
            }

            /// <inheritdoc/>
            public TData Data { get; set; }

            /// <inheritdoc/>
            public TData BelowData => this.Below.Data;

            /// <inheritdoc/>
            public TData AboveData => this.Above.Data;

            /// <summary>
            /// Test if the vertex is above this edge.
            /// </summary>
            /// <param name="vertexId">the id of the vertex</param>
            /// <param name="vertices">the vertex list</param>
            /// <returns>true if the verex is above</returns>
            public bool IsVertexAbove(int vertexId, IReadOnlyList<Vertex> vertices)
            {
                var vertex = vertices[vertexId];
                var left = vertices[this.Left];
                var right = vertices[this.Right];

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

                return this.IsVertexAboveSlow(vertexId, vertices);
            }

            /// <summary>
            /// Test if the vertex is above this edge by calculating the edge.Y at vertex.X
            /// </summary>
            /// <param name="vertexId">the id of the vertex</param>
            /// <param name="vertices">the vertex list</param>
            /// <returns>true if the verex is above</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool IsVertexAboveSlow(int vertexId, IReadOnlyList<Vertex> vertices)
            {
                var vertex = vertices[vertexId];

                var left = vertices[this.Left];
                var right = vertices[this.Right];
                var xSpan = right.X - left.X;

                // during a start operation, the start.Y will always be larger than left.Y and right.Y of a vertical edge, 
                // otherwise start would have been sorted between left and right. So it's no difference to test against left.Y or right.Y
                if (xSpan < epsilon)
                {
                    return vertex.Y > left.Y;
                }

                var yOfEdgeAtVertex = (vertex.X - left.X) / (xSpan) * (right.Y - left.Y) + left.Y;
                return yOfEdgeAtVertex < vertex.Y;
            }

            public override string ToString()
            {
                return $"{this.Left}{(this.IsRightToLeft ? "<" : ">")}{this.Right}";
            }

            public Edge CreateTransition(int newTarget)
            {
                var nextEdge = new Edge(this.Right, newTarget, this.IsRightToLeft)
                {
                    Data = this.Data,
                    Above = this.Above,
                };

                this.Below.Above = nextEdge;
                return nextEdge;
            }

            /// <summary>
            /// Insert the new above between this and the current above
            /// </summary>
            /// <param name="newAbove">the element that's newly above this edge</param>
            public void InsertAbove(Edge newAbove)
            {
                newAbove.Above.Above = this.Above;
                this.Above = newAbove;
            }
        }
    }
}
