namespace PolygonTriangulation
{
    using Vector3 = System.Numerics.Vector3;
    using Quaternion = System.Numerics.Quaternion;
    using Plane = System.Numerics.Plane;

    /// <summary>
    /// Resulting plane mesh with coordinates and triangles
    /// </summary>
    public interface IPlaneMeshResult
    {
        /// <summary>
        /// The 3D vertices
        /// </summary>
        Vector3[] Vertices { get; }

        /// <summary>
        /// The triangles with vertex offset
        /// </summary>
        int[] Triangles { get; }
    }

    /// <summary>
    /// Build a list of triangles from polygon edges
    /// </summary>
    public class PlaneMeshBuilder
    {
        private readonly Plane plane;
        private readonly EdgesToPolygonBuilder edgesToPolygon;

        public PlaneMeshBuilder(Plane plane)
        {
            this.plane = plane;
            var rotation = Quaternion.Identity;
            // .FromToRotation(this.plane.Normal, new Vector3(0, 0, -1));
            this.edgesToPolygon = new EdgesToPolygonBuilder(rotation);
        }

        public void AddEdge(Vector3 p0, Vector3 p1)
        {
            this.edgesToPolygon.AddEdge(p0, p1);
        }

        /// <summary>
        /// Build the plane triangles
        /// </summary>
        public IPlaneMeshResult Build()
        {
            var polygonResult = this.edgesToPolygon.BuildPolygon();
            var triangulator = new PolygonTriangulator(polygonResult.Polygon);
            var triangles = triangulator.BuildTriangles();

            return new PlaneMeshResult(polygonResult.Vertices, triangles);
        }

        /// <summary>
        /// Result for the plane mesh
        /// </summary>
        private class PlaneMeshResult : IPlaneMeshResult
        {
            public PlaneMeshResult(Vector3[] vertices, int[] triangles)
            {
                this.Vertices = vertices;
                this.Triangles = triangles;
            }

            /// <inheritdoc/>
            public Vector3[] Vertices { get; }

            /// <inheritdoc/>
            public int[] Triangles { get; }
        }
    }
}
