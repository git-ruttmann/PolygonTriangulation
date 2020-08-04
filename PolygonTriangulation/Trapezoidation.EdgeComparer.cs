namespace PolygonTriangulation
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

#if UNITY_EDITOR || UNITY_STANDALONE
    using Vertex = UnityEngine.Vector2;
#else
    using Vertex = System.Numerics.Vector2;
#endif

    /// <summary>
    /// subclass container for trapzoidation
    /// </summary>
    public partial class Trapezoidation
    {
        /// <summary>
        /// Compares two edges
        /// </summary>
        private class EdgeComparer : IComparer<TrapezoidEdge>
        {
            private readonly IReadOnlyList<Vertex> vertices;

            /// <summary>
            /// Initializes a new instance of the <see cref="EdgeComparer"/> class.
            /// </summary>
            /// <param name="vertices">the real vertices referenced by vertex ids</param>
            public EdgeComparer(IReadOnlyList<Vertex> vertices)
            {
                this.vertices = vertices;
            }

            /// <summary>
            /// Test if the left vertex of x is above y.
            /// </summary>
            /// <param name="x">the current added value</param>
            /// <param name="y">the edge that is already part of the tree</param>
            /// <returns>a comparison result</returns>
            public int Compare(TrapezoidEdge x, TrapezoidEdge y)
            {
                var value = x;
                var storage = y;
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

#if UNITY_EDITOR || UNITY_STANDALONE
                var leftY = left.y;
                var upperRightX = upperRight.x;
                var upperRightY = upperRight.y;
                var lowerRightX = lowerRight.x;
                var lowerRightY = lowerRight.y;
#else
                var leftY = left.Y;
                var upperRightX = upperRight.X;
                var upperRightY = upperRight.Y;
                var lowerRightX = lowerRight.X;
                var lowerRightY = lowerRight.Y;
#endif

                if ((upperRightY > leftY) != (lowerRightY > leftY))
                {
                    return upperRightY > lowerRightY;
                }

                if (upperRightX > lowerRightX)
                {
                    if (upperRightY < leftY && upperRightY > lowerRightY)
                    {
                        return true;
                    }
                    else if (upperRightY > leftY && upperRightY < lowerRightY)
                    {
                        return false;
                    }
                    else
                    {
                        return !IsVertexAboveSlow(ref lowerRight, ref left, ref upperRight);
                    }
                }
                else
                {
                    if (lowerRightY > leftY && upperRightY > lowerRightY)
                    {
                        return true;
                    }
                    else if (lowerRightY < leftY && upperRightY < lowerRightY)
                    {
                        return false;
                    }
                    else
                    {
                        return IsVertexAboveSlow(ref upperRight, ref left, ref lowerRight);
                    }
                }
            }

            /// <summary>
            /// Test if the vertex is above this edge by calculating the edge.Y at vertex.X
            /// </summary>
            /// <param name="vertex">The vertex.</param>
            /// <param name="left">The left vertex of the edge.</param>
            /// <param name="right">The right vertex of the edge.</param>
            /// <returns>true if the verex is above</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool IsVertexAboveSlow(ref Vertex vertex, ref Vertex left, ref Vertex right)
            {
#if UNITY_EDITOR || UNITY_STANDALONE
                var xSpan = right.x - left.x;

                if (xSpan < epsilon * epsilon)
                {
                    return vertex.y > left.y;
                }

                var yOfEdgeAtVertex = (vertex.x - left.x) / xSpan * (right.y - left.y) + left.y;
                return yOfEdgeAtVertex < vertex.y;
#else
                var xSpan = right.X - left.X;

                if (xSpan < Epsilon * Epsilon)
                {
                    return vertex.Y > left.Y;
                }

                var yOfEdgeAtVertex = ((vertex.X - left.X) / xSpan * (right.Y - left.Y)) + left.Y;
                return yOfEdgeAtVertex < vertex.Y;
#endif
            }

            /// <summary>
            /// Test if the vertex is above the line that is formed by the edge
            /// </summary>
            /// <param name="vertexId">The vertex identifier.</param>
            /// <param name="edge">The edge.</param>
            /// <returns>true if the vertex is above the edge</returns>
            /// <remarks>
            /// This is called only during insert operations, therefore value.left is larger than storage.left.
            /// Try to find the result without calculation first, then calculate the storage.Y at value.Left.X
            /// </remarks>
            private bool IsVertexAbove(int vertexId, TrapezoidEdge edge)
            {
                var vertex = this.vertices[vertexId];
                var left = this.vertices[edge.Left];
                var right = this.vertices[edge.Right];

#if UNITY_EDITOR || UNITY_STANDALONE
                // this is very likely as the points are added in order left to right
                if (vertex.x >= left.x)
                {
                    if (vertex.y > left.y)
                    {
                        if (left.y >= right.y || (vertex.x < right.x && vertex.y > right.y))
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (left.y < right.y || (vertex.x < right.x && vertex.y < right.y))
                        {
                            return false;
                        }
                    }
                }
#else
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
#endif

                return IsVertexAboveSlow(ref vertex, ref left, ref right);
            }
        }
    }
}
