namespace Ruttmann.PolygonTriangulation.Seidel
{
    /// <summary>
    /// Build a polygon
    /// </summary>
    public interface IPolygonBuilder
    {
        /// <summary>
        /// Add a single vertex id
        /// </summary>
        /// <param name="vertexId">the index in the vertices array of the builder</param>
        /// <returns>the same builder instance for call chains</returns>
        IPolygonBuilder Add(int vertexId);

        /// <summary>
        /// Add multiple vertex ids
        /// </summary>
        /// <param name="vertices">the indices in the vertices array of the builder</param>
        /// <returns>the same builder instance for call chains</returns>
        IPolygonBuilder AddVertices(params int[] vertices);

        /// <summary>
        /// Start a hole in the polygon. The previous vertex collection is closed an a new one is started
        /// </summary>
        /// <param name="vertexId">the vertex id of the first point in the hole</param>
        /// <returns>the same builder instance for call chains</returns>
        IPolygonBuilder StartHole(int vertexId);

        /// <summary>
        /// Start a hole and add multiple vertex ids to it
        /// </summary>
        /// <param name="vertices">the indices in the vertices array of the builder</param>
        /// <returns>the same builder instance for call chains</returns>
        IPolygonBuilder AddHole(params int[] vertices);

        /// <summary>
        /// Close the polygon. Do not use the builder after closing it.
        /// </summary>
        /// <returns>a polygon</returns>
        Polygon Close();

        /// <summary>
        /// Create one polygon that includes all vertices in the builder.
        /// </summary>
        /// <returns>a polygon</returns>
        Polygon Auto();
    }
}
