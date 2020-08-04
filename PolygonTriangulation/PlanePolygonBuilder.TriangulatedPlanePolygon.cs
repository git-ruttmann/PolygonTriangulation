namespace PolygonTriangulation
{
    using System.Collections.Generic;

#if UNITY_EDITOR || UNITY_STANDALONE
    using Vector3 = UnityEngine.Vector3;
    using Vertex = UnityEngine.Vector2;
#else
    using Vector3 = System.Numerics.Vector3;
    using Vertex = System.Numerics.Vector2;
#endif

    /// <summary>
    /// Resulting plane mesh with coordinates and triangles
    /// </summary>
    public interface ITriangulatedPlanePolygon
    {
        /// <summary>
        /// Gets the 3D vertices
        /// </summary>
        Vector3[] Vertices { get; }

        /// <summary>
        /// Gets the 2D vertices of the plane points
        /// </summary>
        IReadOnlyList<Vertex> Vertices2D { get; }

        /// <summary>
        /// Gets the triangles with vertex offset
        /// </summary>
        int[] Triangles { get; }
    }

    /// <summary>
    /// subclass container
    /// </summary>
    public partial class PlanePolygonBuilder
    {
        /// <summary>
        /// Result for the plane mesh
        /// </summary>
        private class TriangulatedPlanePolygon : ITriangulatedPlanePolygon
        {
            public TriangulatedPlanePolygon(Vector3[] vertices, IReadOnlyList<Vertex> vertices2D, int[] triangles)
            {
                this.Vertices = vertices;
                this.Triangles = triangles;
                this.Vertices2D = vertices2D;
            }

            /// <inheritdoc/>
            public Vector3[] Vertices { get; }

            /// <inheritdoc/>
            public int[] Triangles { get; }

            /// <inheritdoc/>
            public IReadOnlyList<Vertex> Vertices2D { get; }
        }
    }
}
