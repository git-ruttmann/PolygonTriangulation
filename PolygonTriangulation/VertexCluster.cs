using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace PolygonTriangulation
{
    public static class VertexCluster
    {
        const float epsilon = 1.1E-5f;
        // const float epsilon = Vector3.kEpsilon* 1.1f

        /// <summary>
        /// Gets the comparer function for 3D
        /// </summary>
        public static Comparer<Vector3> Comparer3D { get; } = CreateComparer3D(epsilon);

        /// <summary>
        /// Gets the comparer function for 2D
        /// </summary>
        public static Comparer<Vector2> Comparer2D { get; } = CreateComparer2D(epsilon);

        /// <summary>
        /// Sort and cluster a list of vertices. Duplicate vertices are removed from the list.
        /// </summary>
        /// <param name="vertices">the input and output list.</param>
        /// <param name="start">the start in the list</param>
        /// <returns>translation from old to the new index in vertices. If nothing changes, it's idendity.</returns>
        public static int[] ClusterSort(this List<Vector3> vertices, int start = 0)
        {
            return ClusterSort(vertices, CreateComparer3D(epsilon), start);
        }

        /// <summary>
        /// Sort and cluster a list of vertices. Duplicate vertices are removed from the list.
        /// </summary>
        /// <param name="vertices">the input and output list.</param>
        /// <param name="start">the start in the list</param>
        /// <returns>translation from old to the new index in vertices. If nothing changes, it's idendity.</returns>
        public static int[] ClusterSort(this List<Vector2> vertices, int start = 0)
        {
            return ClusterSort(vertices, CreateComparer2D(epsilon), start);
        }

        /// <summary>
        /// Sort and cluster the vertices. Translate triangles to the new index in vertices.
        /// </summary>
        /// <param name="vertices">the vertices</param>
        /// <param name="triangles">the triangles - indizes in the vertices array</param>
        /// <param name="start">start sorting of vertices behind that index.</param>
        public static void ClusterSortAndTranslate(this List<Vector3> vertices, int[] triangles, int start = 0)
        {
            var translate = vertices.ClusterSort(start);
            for (int i = 0; i < triangles.Length; i++)
            {
                triangles[i] = translate[triangles[i]];
            }
        }


        /// <summary>
        /// Sort and cluster a list of vertices. Duplicate vertices are removed from the list.
        /// </summary>
        /// <param name="vertices">the input and output list.</param>
        /// <param name="start">the start in the list</param>
        /// <returns>translation from old to the new index in vertices. If nothing changes, it's idendity.</returns>
        private static int[] ClusterSort<T>(List<T> vertices, IComparer<T> comparer, int start = 0)
        {
            // copy data to array for "multi-array-sorting"
            var fullLength = vertices.Count;
            var sortedVertices = start == 0 ? vertices.ToArray() : vertices.Skip(start).ToArray();
            var sortedIndizes = Enumerable.Range(0, sortedVertices.Length).ToArray();
            Array.Sort(sortedVertices, sortedIndizes, comparer);

            // clear the result
            vertices.RemoveRange(start, vertices.Count - start);
            vertices.Capacity = fullLength;

            // add "different" vertices to result and set the index of the value in the translation array
            var last = sortedVertices[0];
            var translate = new int[fullLength];
            for (int i = 0; i < sortedVertices.Length; i++)
            {
                if (i == 0 || comparer.Compare(last, sortedVertices[i]) != 0)
                {
                    last = sortedVertices[i];
                    vertices.Add(last);
                }

                translate[start + sortedIndizes[i]] = vertices.Count - 1;
            }

            // set the idendity translation for the first part.
            for (int i = 0; i < start; i++)
            {
                translate[i] = i;
            }

            return translate;
        }

        /// <summary>
        /// Create a comparer
        /// </summary>
        /// <param name="epsilon">the tolerance</param>
        /// <returns>a comparer for 2 vertices</returns>
        private static Comparer<Vector3> CreateComparer3D(double epsilon)
        {
            return Comparer<Vector3>.Create((a, b) =>
            {
                var xdist = Math.Abs(a.X - b.X);
                if (xdist < epsilon)
                {
                    var ydist = Math.Abs(a.Y - b.Y);
                    if (ydist < epsilon)
                    {
                        var zdist = Math.Abs(a.Z - b.Z);
                        if (zdist < epsilon)
                        {
                            return 0;
                        }
                        else if (a.Z < b.Z)
                        {
                            return -1;
                        }
                        else
                        {
                            return 1;
                        }
                    }
                    else if (a.Y < b.Y)
                    {
                        return -1;
                    }
                    else
                    {
                        return 1;
                    }
                }
                else if (a.X < b.X)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            });
        }

        /// <summary>
        /// Create a comparer
        /// </summary>
        /// <param name="epsilon">the tolerance</param>
        /// <returns>a comparer for 2 vertices</returns>
        private static Comparer<Vector2> CreateComparer2D(double epsilon)
        {
            return Comparer<Vector2>.Create((a, b) =>
            {
                var xdist = Math.Abs(a.X - b.X);
                if (xdist < epsilon)
                {
                    var ydist = Math.Abs(a.Y - b.Y);
                    if (ydist < epsilon)
                    {
                        return 0;
                    }
                    else if (a.Y < b.Y)
                    {
                        return -1;
                    }
                    else
                    {
                        return 1;
                    }
                }
                else if (a.X < b.X)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            });
        }
    }
}
