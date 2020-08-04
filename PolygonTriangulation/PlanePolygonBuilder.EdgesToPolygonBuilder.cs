namespace PolygonTriangulation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

#if UNITY_EDITOR || UNITY_STANDALONE
    using Quaternion = UnityEngine.Quaternion;
    using Vector3 = UnityEngine.Vector3;
    using Vertex = UnityEngine.Vector2;
#else
    using Quaternion = System.Numerics.Quaternion;
    using Vector3 = System.Numerics.Vector3;
    using Vertex = System.Numerics.Vector2;
#endif

    /// <summary>
    /// Test interface for the polygon builder
    /// </summary>
    internal interface IEdgesToPolygonBuilder
    {
        /// <summary>
        /// Add an edge between the two points
        /// </summary>
        /// <param name="p0">start point</param>
        /// <param name="p1">end point</param>
        void AddEdge(Vector3 p0, Vector3 p1);

        /// <summary>
        /// build the resulting polygon
        /// </summary>
        /// <returns>A polygon and the 3D vertices</returns>
        IPlanePolygon BuildPolygon();
    }

    /// <summary>
    /// subclass container
    /// </summary>
    public partial class PlanePolygonBuilder
    {
        /// <summary>
        /// Build a polygon from 3D edges, lying on a common plane.
        /// </summary>
        private class EdgesToPolygonBuilder : IEdgesToPolygonBuilder
        {
            /// <summary>
            /// An empty hash set
            /// </summary>
            private static readonly ICollection<int> EmptyHashSet = new HashSet<int>();

            /// <summary>
            /// current edges in pairs
            /// </summary>
            private readonly List<int> edges;

            /// <summary>
            /// original vertices
            /// </summary>
            private readonly List<Vector3> vertices3D;

            /// <summary>
            /// rotated vertices
            /// </summary>
            private readonly List<Vertex> vertices2D;

            /// <summary>
            /// Initializes a new instance of the <see cref="EdgesToPolygonBuilder" /> class.
            /// </summary>
            /// <param name="rotation">the rotation to map a vertex to a 2D plane</param>
            public EdgesToPolygonBuilder(Quaternion rotation)
            {
                this.edges = new List<int>();
                this.vertices3D = new List<Vector3>();
                this.vertices2D = new List<Vertex>();

                this.Rotation = rotation;
            }

            /// <summary>
            /// Gets the 3D to 2D rotation
            /// </summary>
            public Quaternion Rotation { get; }

            /// <summary>
            /// Dump the edges during debugging
            /// </summary>
            public string Dump()
            {
#if DEBUG
                var sb = new StringBuilder();

                sb.AppendLine("var builder = PlanePolygonBuilder.CreatePolygonBuilder();");
                for (int i = 0; i < this.vertices2D.Count - 1; i += 2)
                {
#if UNITY_EDITOR || UNITY_STANDALONE
                    sb.AppendLine($"builder.AddEdge(new Vector3({this.vertices2D[i].x:0.00000000}f, {this.vertices2D[i].y:0.00000000}f, 0), new Vector3({this.vertices2D[i + 1].x:0.00000000}f, {this.vertices2D[i + 1].y:0.00000000}f, 0));");
#else
                    sb.AppendLine($"builder.AddEdge(new Vector3({this.vertices2D[i].X:0.00000000}f, {this.vertices2D[i].Y:0.00000000}f, 0), new Vector3({this.vertices2D[i + 1].X:0.00000000}f, {this.vertices2D[i + 1].Y:0.00000000}f, 0));");
#endif
                }

                sb.AppendLine("builder.BuildPolygon();");

                return sb.ToString();
#else
                return this.ToString();
#endif
            }

            /// <summary>
            /// Add an edge
            /// </summary>
            /// <param name="p0">start point</param>
            /// <param name="p1">end point</param>
            public void AddEdge(Vector3 p0, Vector3 p1)
            {
                var planeTriangleOffset = this.vertices2D.Count;
                this.vertices3D.Add(p0);
                this.vertices3D.Add(p1);
#if UNITY_EDITOR || UNITY_STANDALONE
                var p0Rotated = this.Rotation * p0;
                this.vertices2D.Add(new Vertex(p0Rotated.x, p0Rotated.y));

                var p1Rotated = this.Rotation * p1;
                this.vertices2D.Add(new Vertex(p1Rotated.x, p1Rotated.y));
#else
                var p0Rotated = Vector3.Transform(p0, this.Rotation);
                this.vertices2D.Add(new Vertex(p0Rotated.X, p0Rotated.Y));

                var p1Rotated = Vector3.Transform(p1, this.Rotation);
                this.vertices2D.Add(new Vertex(p1Rotated.X, p1Rotated.Y));
#endif

                this.edges.Add(planeTriangleOffset + 0);
                this.edges.Add(planeTriangleOffset + 1);
            }

            /// <summary>
            /// Compress the vertices and connect all edges to polygons
            /// </summary>
            /// <returns>the polygon and the 3D vertices</returns>
            public IPlanePolygon BuildPolygon()
            {
                var sorted2D = this.vertices2D.ToArray();
                var sortedIndizes = Enumerable.Range(0, sorted2D.Length).ToArray();
                var comparer = new ClusterVertexComparer();
                ICollection<int> fusionedVertices = null;
                Array.Sort(sorted2D, sortedIndizes, comparer);

                var translation = new int[sorted2D.Length];
                var writeIndex = 0;
                var sameVertexCount = 0;
                for (int i = 1; i < sorted2D.Length; i++)
                {
                    if (comparer.Compare(sorted2D[writeIndex], sorted2D[i]) != 0)
                    {
                        sorted2D[++writeIndex] = sorted2D[i];
                        sameVertexCount = 0;
                    }
                    else
                    {
                        if (++sameVertexCount == 2)
                        {
                            fusionedVertices = fusionedVertices ?? new HashSet<int>();
                            fusionedVertices.Add(writeIndex);
                        }
                    }

                    translation[sortedIndizes[i]] = writeIndex;
                }

                var count = writeIndex + 1;
                Array.Resize(ref sorted2D, count);

                var compressed3D = new Vector3[sorted2D.Length];
                for (int i = 0; i < translation.Length; i++)
                {
                    compressed3D[translation[i]] = this.vertices3D[i];
                }

                var lineDetector = new PolygonLineDetector(fusionedVertices ?? EmptyHashSet);
                lineDetector.JoinEdgesToPolygones(this.edges.Select(x => translation[x]));

                if (lineDetector.UnclosedPolygons.Any())
                {
                    lineDetector.TryClusteringUnclosedEnds(sorted2D, Epsilon * 100);
                }

                var polygon = Polygon.FromPolygonLines(sorted2D, lineDetector.Lines.Select(x => x.ToIndexes()).ToArray(), fusionedVertices);
                return new PlanePolygonData(compressed3D, polygon);
            }
        }
    }
}