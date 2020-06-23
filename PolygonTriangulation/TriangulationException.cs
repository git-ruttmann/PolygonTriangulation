namespace PolygonTriangulation
{
    using System;
    using System.Linq;

    /// <summary>
    /// polygon data for exceptions during polygon triangulation
    /// </summary>
    public class TriangulationException : InvalidOperationException
    {
        public TriangulationException(Polygon polygon, string edgeCreateCode, Exception innerException)
            : this(polygon, edgeCreateCode, innerException.Message, innerException)
        {

        }

        public TriangulationException(Polygon polygon, string edgeCreateCode, string message, Exception innerException)
            : base(message, innerException)
        {
            this.Polygon = polygon;
            if (polygon != null)
            {
                this.PolygonCreateCode = BuildPolygonCode(polygon);
            }

            this.EdgeCreateCode = edgeCreateCode;
        }

        /// <summary>
        /// Gets the polygon with the problem, may be null
        /// </summary>
        public Polygon Polygon { get; }

        /// <summary>
        /// Gets the code to feed the edges to a polygon builder.
        /// </summary>
        public string EdgeCreateCode { get; }

        /// <summary>
        /// Gets the code to create the polygon in a unittest
        /// </summary>
        public string PolygonCreateCode { get; }

        /// <summary>
        /// Create a polygon string that can be used to create the polygon
        /// </summary>
        /// <returns>polygon as code</returns>
        internal static string BuildPolygonCode(Polygon polygon)
        {
            var sb = new System.Text.StringBuilder();
            var culture = System.Globalization.CultureInfo.InvariantCulture;

            sb.AppendLine("var vertices = new[]");
            sb.AppendLine("{");
            var vertexStrings = polygon.Vertices.Select(
                x => string.Format(culture, "    new Vertex({0:0.0000000}f, {1:0.0000000}f),", x.X, x.Y));
            sb.AppendLine(string.Join(Environment.NewLine, vertexStrings));
            sb.AppendLine("};");
            sb.AppendLine("");

            sb.AppendLine("var polygon = Polygon.Build(vertices)");
            foreach (var subPolygonId in polygon.SubPolygonIds)
            {
                sb.AppendLine($"    .AddVertices({string.Join(", ", polygon.SubPolygonVertices(0))})");
                sb.AppendLine($"    .ClosePartialPolygon()");
            }

            var fusionVerticex = polygon.SubPolygonIds
                .SelectMany(x => polygon.SubPolygonVertices(x))
                .GroupBy(x => x)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key);
            sb.AppendLine($"    .Close({string.Join(", ", fusionVerticex)});");

            return sb.ToString();
        }
    }
}
