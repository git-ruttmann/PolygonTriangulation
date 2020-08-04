namespace PolygonTriangulation
{
    using System.Collections.Generic;

    /// <summary>
    /// Receive triangles
    /// </summary>
    public interface ITriangleCollector
    {
        /// <summary>
        /// Add a triangle
        /// </summary>
        /// <param name="v0">id of vertex 0</param>
        /// <param name="v1">id of vertex 1</param>
        /// <param name="v2">id of vertex 2</param>
        void AddTriangle(int v0, int v1, int v2);
    }

    /// <summary>
    /// Receive triangles and provide the recieved triangles
    /// </summary>
    public interface IArrayTriangleCollector : ITriangleCollector
    {
        /// <summary>
        /// Gets the triangles
        /// </summary>
        int[] Triangles { get; }
    }

    /// <summary>
    /// subclass container for triangulator
    /// </summary>
    public partial class PolygonTriangulator
    {
        /// <summary>
        /// The triangle collector
        /// </summary>
        private class TriangleCollector : IArrayTriangleCollector
        {
            private readonly List<int> triangles;

            public TriangleCollector()
            {
                this.triangles = new List<int>();
            }

            public int[] Triangles => this.triangles.ToArray();

            public void AddTriangle(int v0, int v1, int v2)
            {
                this.triangles.Add(v0);
                this.triangles.Add(v1);
                this.triangles.Add(v2);
            }
        }
    }
}