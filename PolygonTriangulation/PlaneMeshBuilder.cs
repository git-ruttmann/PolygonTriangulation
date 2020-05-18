using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PolygonTriangulation
{
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
        private Dictionary<int, IActiveEdge<Trapezoid>> leftToRight;
        private Dictionary<int, IActiveEdge<Trapezoid>> rightToLeft;

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

            Priority_Queue.IPriorityQueue<int, float> queue = new Priority_Queue.SimplePriorityQueue<int>();
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

            Console.WriteLine("closed: " + String.Join(" | ", this.closedPolygones.Select(x => x.Debug())));
            Console.WriteLine("unclosed: " + String.Join(" | ", this.unclosedPolygones.Select(x => x.Debug())));
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
            this.leftToRight = new Dictionary<int, IActiveEdge<Trapezoid>>();
            this.rightToLeft = new Dictionary<int, IActiveEdge<Trapezoid>>();

            for (int i = 0; i < edgeStart.Length; i++)
            {
                var start = edgeStart[i];
                var next = edgeNext[i];
                var prev = edgePrev[i];

                if (start < prev && start < next)
                {
                    var edge = this.activeEdges.Begin(start, prev, next);
                    if (edge.Start == edge.Left)
                    {
                        this.leftToRight[edge.Right] = edge;
                        this.rightToLeft[edge.Above.Right] = edge.Above;
                        this.StartInsideToOutside(edge);
                    }
                    else
                    {
                        this.rightToLeft[edge.Right] = edge;
                        this.leftToRight[edge.Above.Right] = edge.Above;
                        this.StartOutsideToInside(edge);
                    }
                }
                else if (start > prev && start > next)
                {
                    var rightToLeftEdge = this.rightToLeft[start];
                    var leftToRightEdge = this.leftToRight[start];
                    this.rightToLeft.Remove(start);
                    this.leftToRight.Remove(start);
                    if (rightToLeftEdge == leftToRightEdge.Below)
                    {
                        this.activeEdges.Finish(rightToLeftEdge, leftToRightEdge);
                        this.FinishInsideToOutside(rightToLeftEdge, leftToRightEdge);
                    }
                    else if (leftToRightEdge == rightToLeftEdge.Below)
                    {
                        this.activeEdges.Finish(leftToRightEdge, rightToLeftEdge);
                        this.FinishOutsideToInside(leftToRightEdge, rightToLeftEdge);
                    }
                    else
                    {
                        throw new InvalidOperationException($"data error: lower and upper edge not joining at point {start}");
                    }
                }
                else
                {
                    this.HandleTransition(start, next, prev);
                }
            }
        }

        private void FinishOutsideToInside(IActiveEdge<Trapezoid> upperEdge, IActiveEdge<Trapezoid> lowerEdge)
        {
            Console.WriteLine($"Finish outside to inside {upperEdge.Right}");
        }

        private void FinishInsideToOutside(IActiveEdge<Trapezoid> upperEdge, IActiveEdge<Trapezoid> lowerEdge)
        {
            Console.WriteLine($"Finish inside to outside {upperEdge.Right}");
        }

        /// <summary>
        /// A starting vertex from outside to inside (start new trapezoid)
        /// </summary>
        /// <param name="edge">the lower edge</param>
        private void StartOutsideToInside(IActiveEdge<Trapezoid> lowerEdge)
        {
            var trapezoid = new Trapezoid(lowerEdge.Left);
            lowerEdge.Data = lowerEdge.Above.Data = trapezoid;
        }

        /// <summary>
        /// A starting vertex from inside to outside (split outer trapezoid)
        /// </summary>
        /// <param name="lowerEdge"></param>
        private void StartInsideToOutside(IActiveEdge<Trapezoid> lowerEdge)
        {
            var upperEdge = lowerEdge.Above;

            if (upperEdge.Above.Data == lowerEdge.Below.Data)
            {
                var upperTrapezoid = upperEdge.Above.Data.SplitUpper(upperEdge.Left);
                upperEdge.Data = upperEdge.Above.Data = upperTrapezoid;

                var lowerTrapezoid = lowerEdge.Below.Data.SplitLower(lowerEdge.Left);
                lowerEdge.Data = lowerEdge.Below.Data = lowerTrapezoid;
            }
            else
            {
                var upperTrapezoid = upperEdge.Above.Data.AddLower(upperEdge.Left);
                upperEdge.Data = upperEdge.Above.Data = upperTrapezoid;

                var lowerTrapezoid = lowerEdge.Below.Data.AddUpper(lowerEdge.Left);
                lowerEdge.Data = lowerEdge.Below.Data = lowerTrapezoid;
            }
        }

        private void HandleTransition(int start, int next, int prev)
        {
            if (next < start)
            {
                var oldEdge = this.rightToLeft[start];
                var newEdge = this.activeEdges.Transition(oldEdge, prev);
                this.rightToLeft.Add(prev, newEdge);
                this.rightToLeft.Remove(start);

                newEdge.Data = oldEdge.Data.AddLower(start);
                if (oldEdge.Data == oldEdge.Above.Data)
                {
                    newEdge.Above.Data = newEdge.Data;
                }
            }
            else
            {
                var oldEdge = this.leftToRight[start];
                var newEdge = this.activeEdges.Transition(oldEdge, next);
                this.leftToRight.Add(next, newEdge);
                this.leftToRight.Remove(start);

                newEdge.Data = oldEdge.Data.AddUpper(start);
                if (oldEdge.Data == oldEdge.Below.Data)
                {
                    newEdge.Below.Data = newEdge.Data;
                }
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
