﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PolygonTriangulation
{
    /// <summary>
    /// An active edge sorted between all other edges. IsNone is set in the lowest and highest edge
    /// </summary>
    /// <typeparam name="TData">the data type</typeparam>
    public interface IActiveEdge<TData>
    {
        /// <summary>
        /// Gets the start of the edge
        /// </summary>
        int Start { get; }

        /// <summary>
        /// Gets the end of the edge
        /// </summary>
        int End { get; }

        /// <summary>
        /// Gets the left vertex of the edge
        /// </summary>
        int Left { get; }

        /// <summary>
        /// Gets the right vertex of the edge
        /// </summary>
        int Right { get; }

        /// <summary>
        /// not a real edge
        /// </summary>
        bool IsNone { get; }

        /// <summary>
        /// Gets the edge below this edge
        /// </summary>
        IActiveEdge<TData> Below { get; }

        /// <summary>
        /// Gets the edge above this edge
        /// </summary>
        IActiveEdge<TData> Above { get; }

        /// <summary>
        /// Gets or sets the associated data. Never modified by the <see cref="SortedActiveEdgeList{TData}"/>.
        /// </summary>
        TData Data { get; set; }

        /// <summary>
        /// Gets debug information
        /// </summary>
        string Debug { get; }
    }

    /// <summary>
    /// A list with active non-overlapping edges sorted by the y coordinate.
    /// </summary>
    /// <typeparam name="TData">The type of data to store per edge</typeparam>
    public class SortedActiveEdgeList<TData>
    {
        const float epsilon = 1.0E-5f;
        private readonly IReadOnlyList<Vector2> vertices;
        private readonly IActiveEdge<TData> upperNone;
        private readonly IActiveEdge<TData> lowerNone;
        private Dictionary<int, IActiveEdge<TData>> vertexToEdge;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="vertices">the coordinates of the vertices</param>
        public SortedActiveEdgeList(IReadOnlyList<Vector2> vertices)
        {
            this.vertices = vertices;
            this.vertexToEdge = new Dictionary<int, IActiveEdge<TData>>();

            var upper = new Edge(-1, -1, false);
            var lower = new Edge(-1, -1, false);
            upper.Below = lower;
            upper.Above = upper;
            lower.Above = upper;
            lower.Below = lower;
            this.upperNone = upper;
            this.lowerNone = lower;
        }

        /// <summary>
        /// Gets the active edge with the right point == vertexId. If there are two edges, it return the lower one.
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
        /// <returns>the lower edge</returns>
        /// <remarks>
        /// There is never a Begin where the lowerTarget has the same X coordinate as start.
        /// </remarks>
        public IActiveEdge<TData> Begin(int start, int prev, int next)
        {
            var (lower, upper) = this.CreateSortedStartingEdges(start, prev, next);
            lower.Above = upper;
            upper.Below = lower;

            // superslow.....
            var startVertex = this.vertices[start];
            var below = this.lowerNone;
            for (var candidate = this.upperNone.Below; !candidate.IsNone; candidate = candidate.Below)
            {
                if (this.CalculateYatX(candidate, startVertex.X) < startVertex.Y)
                {
                    below = candidate;
                    break;
                }
            }

            var above = below.Above;
            lower.Below = below;
            upper.Above = above;

            ((Edge)upper.Above).Below = upper;
            ((Edge)lower.Below).Above = lower;

            vertexToEdge[upper.Right] = upper;
            vertexToEdge[lower.Right] = lower;

            return lower;
        }

        /// <summary>
        /// Create a pair of edges with a common start point. The returned edges are sorted by the end point
        /// </summary>
        /// <param name="start">start vertex</param>
        /// <param name="prev">previous vertex, always > start</param>
        /// <param name="next">next vertex, always > start</param>
        /// <returns></returns>
        private (Edge lower, Edge upper) CreateSortedStartingEdges(int start, int prev, int next)
        {
            var prevEdge = new Edge(start, prev, true);
            var nextEdge = new Edge(start, next, false);

            var startVertex = this.vertices[start];
            var prevVertex = this.vertices[prev];
            var nextVertex = this.vertices[next];

            if (startVertex.Y < nextVertex.Y)
            {
                if (prevVertex.Y <= startVertex.Y || (prevVertex.X >= nextVertex.X && prevVertex.Y < nextVertex.Y))
                {
                    return (prevEdge, nextEdge);
                }
            }
            else
            {
                if (prevVertex.Y >= startVertex.Y || (prevVertex.X >= nextVertex.X && prevVertex.Y < nextVertex.Y))
                {
                    return (nextEdge, prevEdge);
                }
            }

            if (CalculateYatX(prevEdge, this.vertices[next].X) > this.vertices[next].Y)
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
            var nextEdge = new Edge(edge.Right, newTarget, edge.Right != edge.End);

            nextEdge.Above = edge.Above;
            nextEdge.Below = edge.Below;
            ((Edge)nextEdge.Above).Below = nextEdge;
            ((Edge)nextEdge.Below).Above = nextEdge;

            nextEdge.Data = edge.Data;

            this.vertexToEdge.Remove(edge.Right);
            this.vertexToEdge[nextEdge.Right] = nextEdge;

            return nextEdge;
        }

        /// <summary>
        /// Two edges join in a final vertex
        /// </summary>
        /// <param name="lower">the lower edge</param>
        /// <param name="upper">the upper edge</param>
        public void Finish(IActiveEdge<TData> lower, IActiveEdge<TData> upper)
        {
            if (lower.Right != upper.Right)
            {
                throw new InvalidOperationException("Internal error: joined edges must have the same vertex on the right");
            }

            if (lower.Above != upper || upper.Below != lower)
            {
                throw new InvalidOperationException("Internal error: can't join non-adjacent edges");
            }

            if (lower.Below is Edge nextLower && upper.Above is Edge nextUpper)
            {
                nextUpper.Below = nextLower;
                nextLower.Above = nextUpper;
                this.vertexToEdge.Remove(lower.Right);
            }
            else
            {
                throw new InvalidOperationException("Can't operate without internal IActiveEdge implementation");
            }
        }

        private float CalculateYatX(IActiveEdge<TData> edge, float x)
        {
            var left = this.vertices[edge.Left];
            var right = this.vertices[edge.Right];
            var xSpan = right.X - left.X;

            // during a start operation, the start.Y will always be larger than left.Y and right.Y of a vertical edge, 
            // otherwise start would have been sorted between left and right. So it's no difference to return left.Y or right.Y
            if (xSpan < epsilon)
            {
                return left.Y;
            }

            return (x - left.X) / (xSpan) * (right.Y - left.Y) + left.Y;
        }

        [DebuggerDisplay("{Debug}")]
        private class Edge : IActiveEdge<TData>
        {
            public Edge(int left, int right, bool reverse)
            {
                this.Start = reverse ? right : left;
                this.End = reverse ? left : right;
                this.Left = left;
                this.Right = right;
                this.IsNone = left < 0 && right < 0;
            }

            /// <inheritdoc/>
            public int Start { get; private set; }

            /// <inheritdoc/>
            public int End { get; private set; }

            /// <inheritdoc/>
            public int Left { get; private set; }

            /// <inheritdoc/>
            public int Right { get; private set; }

            /// <inheritdoc/>
            public bool IsNone { get; private set; }

            public string Debug => Start == Left ? $"{Left}>{Right}" : $"{Left}<{Right}";

            /// <inheritdoc/>
            public IActiveEdge<TData> Below { get; set; }

            /// <inheritdoc/>
            public IActiveEdge<TData> Above { get; set; }

            /// <inheritdoc/>
            public TData Data { get; set; }
        }
    }
}
