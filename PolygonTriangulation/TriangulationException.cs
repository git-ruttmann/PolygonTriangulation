namespace PolygonTriangulation
{
    using System;
    using System.Linq;
    using System.Runtime.Serialization;

    /// <summary>
    /// polygon data for exceptions during polygon triangulation
    /// </summary>
    [Serializable]
    public class TriangulationException : InvalidOperationException
    {
        public TriangulationException(Polygon polygon, string edgeCreateCode, Exception innerException)
            : this(polygon, edgeCreateCode, innerException.Message, innerException)
        {

        }

        public TriangulationException(Polygon polygon, string edgeCreateCode, string message, Exception innerException)
            : base(message, innerException)
        {
            this.PolygonCreateCode = BuildPolygonCode(polygon);
            this.EdgeCreateCode = edgeCreateCode;
        }

        protected TriangulationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.EdgeCreateCode = info.GetString(nameof(EdgeCreateCode));
            this.PolygonCreateCode = info.GetString(nameof(PolygonCreateCode));
        }

        /// <summary>
        /// Gets the code to feed the edges to a polygon builder.
        /// </summary>
        public string EdgeCreateCode { get; }

        /// <summary>
        /// Gets the code to create the polygon in a unittest
        /// </summary>
        public string PolygonCreateCode { get; }

        /// <inheritdoc/>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(EdgeCreateCode), this.EdgeCreateCode);
            info.AddValue(nameof(PolygonCreateCode), this.PolygonCreateCode);
        }

        /// <summary>
        /// Create a polygon string that can be used to create the polygon
        /// </summary>
        /// <returns>polygon as code</returns>
        internal static string BuildPolygonCode(Polygon polygon)
        {
            if (polygon == null)
            {
                return string.Empty;
            }

            var sb = new System.Text.StringBuilder();
            var culture = System.Globalization.CultureInfo.InvariantCulture;

            sb.AppendLine("var vertices = new[]");
            sb.AppendLine("{");
            var vertexStrings = polygon.Vertices.Select(
#if UNITY_EDITOR || UNITY_STANDALONE
                x => string.Format(culture, "    new Vertex({0:0.0000000}f, {1:0.0000000}f),", x.x, x.y));
#else
                x => string.Format(culture, "    new Vertex({0:0.0000000}f, {1:0.0000000}f),", x.X, x.Y));
#endif
            sb.AppendLine(string.Join(Environment.NewLine, vertexStrings));
            sb.AppendLine("};");
            sb.AppendLine("");

            sb.AppendLine("var polygon = Polygon.Build(vertices)");
            foreach (var subPolygonId in polygon.SubPolygonIds)
            {
                sb.AppendLine($"    .AddVertices({string.Join(", ", polygon.SubPolygonVertices(subPolygonId))})");
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
