namespace PolygonTriangulation
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Build triangles for a polygon
    /// </summary>
    public partial class PolygonTriangulator
    {
        private readonly Polygon polygon;

        /// <summary>
        /// Initializes a new instance of the <see cref="PolygonTriangulator"/> class.
        /// </summary>
        /// <param name="polygon">the polygon to triangulate</param>
        public PolygonTriangulator(Polygon polygon)
        {
            this.polygon = polygon;
        }

        /// <summary>
        /// Create a triangle collector
        /// </summary>
        /// <returns>a triangle collector</returns>
        public static IArrayTriangleCollector CreateTriangleCollector()
        {
            return new TriangleCollector();
        }

        /// <summary>
        /// Finally, build the triangles
        /// </summary>
        /// <returns>the triangles to represent the polygon</returns>
        public int[] BuildTriangles()
        {
            var collector = new TriangleCollector();
            this.BuildTriangles(collector);
            return collector.Triangles;
        }

        /// <summary>
        /// Finally, build the triangles
        /// </summary>
        /// <param name="collector">the triangle collector</param>
        public void BuildTriangles(ITriangleCollector collector)
        {
            var splits = ScanSplitByTrapezoidation.BuildSplits(this.polygon);
            var polygonWithMonotones = Polygon.Split(this.polygon, splits, collector);
            foreach (var subPolygonId in polygonWithMonotones.SubPolygonIds)
            {
                var triangluator = new MonotonePolygonTriangulator(polygonWithMonotones, subPolygonId);
                triangluator.Build(collector);
            }
        }

        /// <summary>
        /// Get the possible splits for the polygon
        /// </summary>
        /// <returns>the splits</returns>
        internal IEnumerable<Tuple<int, int>> GetSplits()
        {
            return ScanSplitByTrapezoidation.BuildSplits(this.polygon);
        }

        /// <summary>
        /// Iterates over the first n vertices and reports the active edges and the sort order after that step.
        /// </summary>
        /// <param name="depth">The execution depth.</param>
        /// <returns>sorted active edges</returns>
        internal IEnumerable<string> GetEdgesAfterPartialTrapezoidation(int depth)
        {
            return ScanSplitByTrapezoidation.GetEdgesAfterPartialTrapezoidation(this.polygon, depth);
        }
    }
}