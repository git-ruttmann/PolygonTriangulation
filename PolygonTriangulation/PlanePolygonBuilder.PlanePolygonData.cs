namespace PolygonTriangulation
{
#if UNITY_EDITOR || UNITY_STANDALONE
    using Vector3 = UnityEngine.Vector3;
#else
    using Vector3 = System.Numerics.Vector3;
#endif

    /// <summary>
    /// The polygon result after combining all edges
    /// </summary>
    public interface IPlanePolygon
    {
        /// <summary>
        /// Gets the 3D vertices.
        /// </summary>
        Vector3[] Vertices { get; }

        /// <summary>
        /// Gets the polygon. It contains the 2D vertices.
        /// </summary>
        Polygon Polygon { get; }
    }

    /// <summary>
    /// subclass container
    /// </summary>
    public partial class PlanePolygonBuilder
    {
        /// <summary>
        /// Result storage for polygon data
        /// </summary>
        private class PlanePolygonData : IPlanePolygon
        {
            public PlanePolygonData(Vector3[] vertices3D, Polygon polygon)
            {
                this.Vertices = vertices3D;
                this.Polygon = polygon;
            }

            /// <inheritdoc/>
            public Vector3[] Vertices { get; }

            /// <inheritdoc/>
            public Polygon Polygon { get; }
        }
    }
}