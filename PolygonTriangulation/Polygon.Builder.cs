namespace PolygonTriangulation
{
    using System.Collections.Generic;
    using System.Linq;

#if UNITY_EDITOR || UNITY_STANDALONE
    using Vertex = UnityEngine.Vector2;
#else
    using Vertex = System.Numerics.Vector2;
#endif

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
        /// <param name="fusionVertices">vertices that are used in more than one subpolygon</param>
        /// <returns>a polygon</returns>
        Polygon Close(params int[] fusionVertices);

        /// <summary>
        /// Create one polygon that includes all vertices in the builder.
        /// </summary>
        /// <returns>a polygon</returns>
        Polygon Auto();
    }

    /// <summary>
    /// subclass container for polygon
    /// </summary>
    public partial class Polygon
    {
        /// <summary>
        /// Build a new polygon
        /// </summary>
        private class PolygonBuilder : IPolygonBuilder
        {
            private readonly Vertex[] vertices;
            private readonly List<int> vertexIds;
            private readonly List<int> nextIndices;
            private readonly List<int> polygonIds;
            private int first;
            private int polygonId;

            public PolygonBuilder(Vertex[] vertices)
            {
                this.first = 0;
                this.vertices = vertices;
                this.vertexIds = new List<int>();
                this.nextIndices = new List<int>();
                this.polygonIds = new List<int>();
                this.polygonId = 0;
            }

            public Polygon Auto()
            {
                return Polygon.FromVertexList(
                    this.vertices,
                    Enumerable.Range(0, this.vertices.Length),
                    Enumerable.Range(1, this.vertices.Length - 1).Concat(Enumerable.Range(0, 1)),
                    Enumerable.Repeat(0, this.vertices.Length),
                    null);
            }

            public IPolygonBuilder Add(int vertexId)
            {
                this.nextIndices.Add(this.nextIndices.Count + 1);
                this.vertexIds.Add(vertexId);
                this.polygonIds.Add(this.polygonId);
                return this;
            }

            public IPolygonBuilder AddVertices(IEnumerable<int> vertices)
            {
                foreach (var vertex in vertices)
                {
                    this.nextIndices.Add(this.nextIndices.Count + 1);
                    this.vertexIds.Add(vertex);
                    this.polygonIds.Add(this.polygonId);
                }

                return this;
            }

            public IPolygonBuilder ClosePartialPolygon()
            {
                if (this.vertexIds.Count > this.first)
                {
                    this.nextIndices[this.nextIndices.Count - 1] = this.first;
                    this.polygonId++;
                    this.first = this.vertexIds.Count;
                }

                return this;
            }

            public Polygon Close(params int[] fusionVertices)
            {
                this.ClosePartialPolygon();
                return Polygon.FromVertexList(this.vertices, this.vertexIds, this.nextIndices, this.polygonIds, fusionVertices);
            }
        }
    }
}
