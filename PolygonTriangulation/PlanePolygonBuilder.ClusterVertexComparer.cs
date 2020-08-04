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
    /// subclass container
    /// </summary>
    public partial class PlanePolygonBuilder
    {
        /// <summary>
        /// Compare two vertices, very close vertices are considered equal.
        /// </summary>
        private class ClusterVertexComparer : IComparer<Vertex>
        {
            /// <inheritdoc/>
            public int Compare(Vertex x, Vertex y)
            {
#if UNITY_EDITOR || UNITY_STANDALONE
                var xdist = Math.Abs(x.x - y.x);
                if (xdist < epsilon)
                {
                    var ydist = Math.Abs(x.y - y.y);
                    if (ydist < epsilon)
                    {
                        return 0;
                    }

                    var xCompare = x.x.CompareTo(y.x);
                    if (xCompare != 0)
                    {
                        return xCompare;
                    }

                    if (x.y < y.y)
                    {
                        return -1;
                    }
                    else
                    {
                        return 1;
                    }
                }
                else if (x.x < y.x)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
#else
                var xdist = Math.Abs(x.X - y.X);
                if (xdist < Epsilon)
                {
                    var ydist = Math.Abs(x.Y - y.Y);
                    if (ydist < Epsilon)
                    {
                        return 0;
                    }

                    var xCompare = x.X.CompareTo(y.X);
                    if (xCompare != 0)
                    {
                        return xCompare;
                    }

                    if (x.Y < y.Y)
                    {
                        return -1;
                    }
                    else
                    {
                        return 1;
                    }
                }
                else if (x.X < y.X)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
#endif
            }
        }
    }
}
