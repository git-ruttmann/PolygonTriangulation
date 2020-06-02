namespace PolygonTriangulation
{
    using System.Collections.Generic;

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
        IPolygonBuilder AddVertices(IEnumerable<int> vertices);

        /// <summary>
        /// Close the current polygon. Next vertices are considered a new polygon line i.e a hole or a non-intersecting polygon
        /// </summary>
        /// <returns>the same builder instance for call chains</returns>
        IPolygonBuilder ClosePartialPolygon();

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

    /// <summary>
    /// Extension methods for polygon
    /// </summary>
    public static class PolygonExtensions
    {
        public static IPolygonBuilder AddVertices(this IPolygonBuilder builder, params int[] vertices)
        {
            return builder.AddVertices((IEnumerable<int>)vertices);
        }
    }
}
