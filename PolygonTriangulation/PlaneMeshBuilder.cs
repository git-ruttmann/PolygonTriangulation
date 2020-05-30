namespace PolygonTriangulation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;

    /// <summary>
    /// Build a list of triangles from polygon edges
    /// </summary>
    public class PlaneMeshBuilder
    {
        private readonly Plane plane;
        private readonly List<int> pseudoTriangles;
        private readonly List<int> edges;
        private readonly List<Vector2> vertices2D;
        private readonly List<PolygonLine> closedPolygones;
        private readonly List<PolygonLine> unclosedPolygones;
        private readonly Quaternion rotation;
        private List<Vector3> vertices3D;
        private SortedActiveEdgeList<Trapezoid> activeEdges;

        public PlaneMeshBuilder(Plane plane)
        {
            this.plane = plane;
            this.pseudoTriangles = new List<int>();
            this.edges = new List<int>();
            this.vertices3D = new List<Vector3>();
            this.vertices2D = new List<Vector2>();
            this.closedPolygones = new List<PolygonLine>();
            this.unclosedPolygones = new List<PolygonLine>();
            this.rotation = Quaternion.Identity;
                // .FromToRotation(this.plane.Normal, new Vector3(0, 0, -1));
        }

        public PlaneMeshBuilder(IEnumerable<int> edges)
        {
            this.edges = edges.ToList();
            this.closedPolygones = new List<PolygonLine>();
            this.unclosedPolygones = new List<PolygonLine>();
            this.JoinEdgesToPolygones();
        }

        /// <summary>
        /// Gets the pseudo triangles during the buildup phase.
        /// </summary>
        public IEnumerable<int> DebugTriangles => this.pseudoTriangles;

        /// <summary>
        /// Get's a point on the plane that fits the last added triangle. Debug only for old unittests.
        /// </summary>
        public Vector3 LastPlanePoint { get; private set; }

        /// <summary>
        /// Gets the list of vertices
        /// </summary>
        public Vector3[] Vertices => this.vertices3D.ToArray();

        /// <summary>
        /// Gets the closed polygones
        /// </summary>
        public int[][] ClosedPolygoneIndizes => this.closedPolygones.Select(x => x.ToIndexes()).ToArray();

        /// <summary>
        /// Gets the unclosed polygones
        /// </summary>
        public int[][] UnclosedPolygoneIndizes => this.unclosedPolygones.Select(x => x.ToIndexes()).ToArray();

        public void AddEdge(Vector3 p0, Vector3 p1)
        {
            var planeTriangleOffset = this.vertices2D.Count;
            this.vertices3D.Add(p0);
            var p0Rotated = Vector3.Transform(p0, this.rotation);
            this.vertices2D.Add(new Vector2(p0Rotated.X, p0Rotated.Y));

            this.vertices3D.Add(p1);
            var p1Rotated = Vector3.Transform(p1, this.rotation);
            this.vertices2D.Add(new Vector2(p1Rotated.X, p1Rotated.Y));

            var planePoint = p0 + Vector3.Cross(this.plane.Normal, p1 - p0);
            this.LastPlanePoint = planePoint;
            // Console.WriteLine($"Add edge: {p0} {p1} {planePoint}");

            this.edges.Add(planeTriangleOffset + 0);
            this.edges.Add(planeTriangleOffset + 1);

            this.pseudoTriangles.Add(planeTriangleOffset + 0);
            this.pseudoTriangles.Add(planeTriangleOffset + 1);
            this.pseudoTriangles.Add(-1);
        }

        /// <summary>
        /// Build the plane triangles
        /// </summary>
        public void Build()
        {
            this.ClusterVertices();
            this.JoinEdgesToPolygones();
            this.BuildOrientationArray();
            this.FindPlanePolygones();
        }

        /// <summary>
        /// cluster the vertices
        /// </summary>
        private void ClusterVertices()
        {
            var translation = this.vertices2D.ClusterSort();

            var old = this.vertices3D;
            this.vertices3D = Enumerable.Repeat(Vector3.Zero, this.vertices2D.Count).ToList();
            for (int i = 0; i < translation.Length; i++)
            {
                this.vertices3D[translation[i]] = old[i];
            }

            TranslateList(translation, this.pseudoTriangles);
            TranslateList(translation, this.edges);
        }

        /// <summary>
        /// find continous combination of edges
        /// </summary>
        /// <param name="triangles">triangle data, the relevant edges are from vertex0 to vertex1. vertex2 is ignored</param>
        /// <returns>the list of closed polygones and the unclosed polygones </returns>
        private void JoinEdgesToPolygones()
        {
            var workList = new List<PolygonLine>();
            var openPolygones = new Dictionary<int, PolygonLine>();

            for (int i = 0; i < this.edges.Count; i += 2)
            {
                var start = this.edges[i];
                var end = this.edges[i + 1];

                if (start == end)
                {
                    continue;
                }

                bool startFits;
                if (startFits = openPolygones.TryGetValue(start, out var firstSegment))
                {
                    openPolygones.Remove(start);
                }

                bool endFits;
                if (endFits = openPolygones.TryGetValue(end, out var lastSegment))
                {
                    openPolygones.Remove(end);
                }

                // Console.WriteLine($"[{start}{(startFits ? "+" : "-")} {end}{(endFits ? "+" : "-")}] " + $"found:[{ firstSegment?.Debug() }|{ lastSegment?.Debug() }]" + " keys: " + String.Join(" . ", openPolygones.Select(x => $"[{x.Key}]: " + String.Join(" ", x.Value.Debug()))));
                if (!startFits && !endFits)
                {
                    var segment = new PolygonLine(start, end);
                    openPolygones.Add(start, segment);
                    openPolygones.Add(end, segment);
                }
                else if (startFits && endFits)
                {
                    var remainingKeyOfOther = firstSegment.Join(lastSegment, start, end);
                    if (remainingKeyOfOther < 0)
                    {
                        this.closedPolygones.Add(firstSegment);
                    }
                    else
                    {
                        openPolygones[remainingKeyOfOther] = firstSegment;
                    }
                }
                else if (startFits)
                {
                    firstSegment.AddMatchingStart(start, end);
                    openPolygones[end] = firstSegment;
                }
                else
                {
                    lastSegment.AddMatchingEnd(start, end);
                    openPolygones[start] = lastSegment;
                }

                // Console.WriteLine($"After[{start}{(startFits ? "+" : "-")} {end}{(endFits ? "+" : "-")}] " + " keys: " + String.Join(" . ", openPolygones.Select(x => $"[{x.Key}]: " + x.Value.Debug())));
            }

            this.unclosedPolygones.AddRange(openPolygones
                .Where(x => x.Key == x.Value.StartKey)
                .Select(x => x.Value));

            Console.WriteLine("closed: " + String.Join(" | ", this.closedPolygones.Select(x => x.Debug)));
            Console.WriteLine("unclosed: " + String.Join(" | ", this.unclosedPolygones.Select(x => x.Debug)));
        }

        private static void TranslateList(int[] translation, List<int> data)
        {
            for (int i = 0; i < data.Count; i++)
            {
                if (data[i] >= 0)
                {
                    data[i] = translation[data[i]];
                }
            }
        }

        private void FindPlanePolygones()
        {
            var vertices = this.vertices2D.ToArray();
            var edgeStart = new int[vertices.Length];
            var edgeNext = new int[vertices.Length];
            var edgePrev = new int[vertices.Length];

            for (int i = 0; i < this.ClosedPolygoneIndizes.Length; i++)
            {
                var polygon = this.ClosedPolygoneIndizes[i];
                for (int j = 0; j < this.ClosedPolygoneIndizes[i].Length; j++)
                {
                    var prev = j == 0 ? polygon.Length - 1 : j - 1;
                    var next = j == polygon.Length - 1 ? 0 : j + 1;
                    edgeStart[polygon[j]] = polygon[j];
                    edgeNext[polygon[j]] = polygon[next];
                    edgePrev[polygon[j]] = polygon[prev];

                    Console.WriteLine($"{edgePrev[polygon[j]]} {edgeStart[polygon[j]]} {edgeNext[polygon[j]]}");
                }

                Console.WriteLine($"Polygon points {i}: " + String.Join(" ", this.ClosedPolygoneIndizes[i].Select(x => $"{x}: {this.vertices2D[x]}")));
            }

            this.activeEdges = new SortedActiveEdgeList<Trapezoid>(vertices);

            for (int i = 0; i < edgeStart.Length; i++)
            {
                HandleVertex(edgeStart[i], edgeNext[i], edgePrev[i]);
            }
        }

        /// <summary>
        /// Handle a vertex in context of its previous and next vertex
        /// </summary>
        /// <param name="vertexId">the id of the vertex</param>
        /// <param name="next">the next vertex in the polygon</param>
        /// <param name="prev">the previous vertex in the polygon</param>
        private void HandleVertex(int vertexId, int next, int prev)
        {
            if (vertexId < prev && vertexId < next)
            {
                var edge = this.activeEdges.Begin(vertexId, prev, next);
                if (edge.End == edge.Left)
                {
                    Trapezoid.EnterInsideBySplit(vertexId, edge);
                }
                else
                {
                    var trapezoid = edge.Below.Data;
                    trapezoid.LeaveInsideBySplit(vertexId, edge);
                    this.DetectSplit(trapezoid);
                }
            }
            else if (vertexId > prev && vertexId > next)
            {
                var edge = this.activeEdges.EdgeForVertex(vertexId);
                activeEdges.Finish(edge, edge.Above);

                var lowerTrapezoid = edge.Data;
                var upperTrapezoid = edge.Above.Data;
                if (edge.Left == prev)
                {
                    Trapezoid.EnterInsideByJoin(lowerTrapezoid, upperTrapezoid, vertexId);
                    this.DetectSplit(lowerTrapezoid);
                    this.DetectSplit(upperTrapezoid);
                }
                else
                {
                    lowerTrapezoid.LeaveInsideByJoin(vertexId);
                    this.DetectSplit(lowerTrapezoid);
                }
            }
            else
            {
                var oldEdge = this.activeEdges.EdgeForVertex(vertexId);
                var trapezoid = oldEdge.Data;
                if (next > vertexId)
                {
                    var newEdge = this.activeEdges.Transition(oldEdge, next);
                    trapezoid.TransitionOnUpperEdge(vertexId, newEdge);
                }
                else
                {
                    var newEdge = this.activeEdges.Transition(oldEdge, prev);
                    trapezoid.TransitionOnLowerEdge(vertexId, newEdge);
                }

                this.DetectSplit(trapezoid);
            }
        }

        private void DetectSplit(Trapezoid trapezoid)
        {
            if (trapezoid.IsSplit)
            {
                Console.WriteLine($"Split from {trapezoid.LeftVertex} {trapezoid.RightVertex}");
            }
        }

        private void BuildOrientationArray()
        {
            Console.WriteLine("3D " + String.Join(" ", this.vertices3D));
            Console.WriteLine("2D " + String.Join(" ", this.vertices2D));

            foreach (var polygon in this.closedPolygones)
            {
                var dots = polygon.ToIndexes().Select(x => this.vertices2D[x]).ToArray();
                var dotsBackShifted = Enumerable.Repeat(dots.Last(), 1).Concat(dots);
                var vectors = dotsBackShifted.Zip(dots, (previous, current) => current - previous);
                var orientation = vectors.Skip(1).Concat(vectors.Take(1)).Zip(vectors, (x, y) => x.X * (x.Y + y.Y) - (x.X + y.X) * x.Y).ToArray();

                Console.WriteLine("Indexes " + String.Join(" ", polygon.ToIndexes()));
                Console.WriteLine("Vertizes " + String.Join(" ", dots));
                Console.WriteLine("2D Vectors " + String.Join(" ", vectors));
                Console.WriteLine("Orientation " + String.Join(" ", orientation) + " sum " + orientation.Sum());
            }
        }
    }
}
