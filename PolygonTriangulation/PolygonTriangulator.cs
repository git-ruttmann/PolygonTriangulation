namespace PolygonTriangulation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
#if UNITY_EDITOR || UNITY_STANDALONE
    using Vertex = UnityEngine.Vector2;
#else
    using Vertex = System.Numerics.Vector2;
#endif

    /// <summary>
    /// Receive triangles
    /// </summary>
    public interface ITriangleCollector
    {
        /// <summary>
        /// Add a triangle
        /// </summary>
        /// <param name="v0">id of vertex 0</param>
        /// <param name="v1">id of vertex 1</param>
        /// <param name="v2">id of vertex 2</param>
        void AddTriangle(int v0, int v1, int v2);
    }

    /// <summary>
    /// Receive triangles and provide the recieved triangles
    /// </summary>
    public interface IArrayTriangleCollector : ITriangleCollector
    {
        /// <summary>
        /// Gets the triangles
        /// </summary>
        int[] Triangles { get; }
    }

    /// <summary>
    /// Build triangles for a polygon
    /// </summary>
    public class PolygonTriangulator
    {
        private readonly Polygon polygon;

        /// <summary>
        /// Creates a new instances
        /// </summary>
        /// <param name="polygon">the polygon to triangulize</param>
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
        /// <returns>sorted active edges</returns>
        internal IEnumerable<string> GetEdgesAfterPartialTrapezoidation(int depth)
        {
            return ScanSplitByTrapezoidation.GetEdgesAfterPartialTrapezoidation(this.polygon, depth);
        }

        /// <summary>
        /// Traverse the polygon, build trapezoids and collect possible splits
        /// </summary>
        private class ScanSplitByTrapezoidation : IPolygonSplitSink
        {
            private readonly Trapezoidation activeEdges;
            private readonly List<Tuple<int, int>> splits;
            private readonly Polygon polygon;

            private ScanSplitByTrapezoidation(Polygon polygon)
            {
                this.splits = new List<Tuple<int, int>>();
                this.polygon = polygon;

                this.activeEdges = new Trapezoidation(this.polygon.Vertices, this);
            }

            /// <summary>
            /// Build the splits for the polygon
            /// </summary>
            /// <param name="polygon">the polygon</param>
            /// <returns>the splits</returns>
            public static IEnumerable<Tuple<int, int>> BuildSplits(Polygon polygon)
            {
                var splitter = new ScanSplitByTrapezoidation(polygon);
                splitter.BuildSplits(-1);
                return splitter.splits;
            }

            /// <summary>
            /// Run n steps and return the edges after that step
            /// </summary>
            /// <param name="polygon">the polygon</param>
            /// <param name="depth">the number of steps to run</param>
            /// <returns>The edges sorted from High to Low</returns>
            internal static IEnumerable<string> GetEdgesAfterPartialTrapezoidation(Polygon polygon, int depth)
            {
                var splitter = new ScanSplitByTrapezoidation(polygon);
                splitter.BuildSplits(depth);
                return splitter.activeEdges.Edges.Reverse().Select(x => x.ToString());
            }

            /// <summary>
            /// Traverse the polygon and build all splits
            /// </summary>
            /// <param name="stepCount">number of steps during debugging. Use -1 for all</param>
            public void BuildSplits(int stepCount)
            {
                foreach (var group in this.polygon.OrderedVertices.GroupBy(x => x.Id))
                {
                    var actions = group.ToArray();
                    if (actions.Count() > 1)
                    {
                        actions = actions.OrderBy(x => x.Action).ToArray();
                    }

                    foreach (var info in actions)
                    {
                        if (stepCount >= 0)
                        {
                            stepCount -= 1;
                            if (stepCount < 0)
                            {
                                return;
                            }
                        }

                        switch (info.Action)
                        {
                            case VertexAction.ClosingCusp:
                                this.activeEdges.HandleClosingCusp(info);
                                break;
                            case VertexAction.Transition:
                                this.activeEdges.HandleTransition(info);
                                break;
                            case VertexAction.OpeningCusp:
                                this.activeEdges.HandleOpeningCusp(info);
                                break;
                            default:
                                throw new InvalidOperationException($"Unkown action {info.Action}");
                        }
                    }
                }
            }

            /// <inheritdoc/>
            void IPolygonSplitSink.SplitPolygon(int leftVertex, int rightVertex)
            {
                this.splits.Add(Tuple.Create(leftVertex, rightVertex));
            }
        }

        /// <summary>
        /// The triangle collector
        /// </summary>
        private class TriangleCollector : IArrayTriangleCollector
        {
            private readonly List<int> triangles;

            public TriangleCollector()
            {
                this.triangles = new List<int>();
            }

            public int[] Triangles => this.triangles.ToArray();

            public void AddTriangle(int v0, int v1, int v2)
            {
                this.triangles.Add(v0);
                this.triangles.Add(v1);
                this.triangles.Add(v2);
            }
        }

        /// <summary>
        /// Class to triangluate a monotone polygon
        /// </summary>
        private class MonotonePolygonTriangulator
        {
            private readonly Polygon polygon;
            private readonly int subPolygonId;
            private readonly IReadOnlyList<Vertex> vertices;
            private readonly Stack<int> vertexStack;
            private int third;
            private int second;
            private int current;
            private IEnumerator<int> iterator;

            public MonotonePolygonTriangulator(Polygon polygon, int subPolygonId)
            {
                this.polygon = polygon;
                this.subPolygonId = subPolygonId;
                this.vertices = polygon.Vertices;
                this.vertexStack = new Stack<int>();
            }

            /// <summary>
            /// traverse the polygon and add triangles to the collector
            /// </summary>
            /// <param name="collector">collector for resulting triangles</param>
            public void Build(ITriangleCollector collector)
            {
                var start = this.FindStartOfMonotonePolygon();
                if (start >= 0)
                {
                    this.TriangulateMonotonePolygon(start, collector);
                }
                else
                {
                    var triangleVertices = this.polygon.SubPolygonVertices(this.subPolygonId).ToArray();
                    collector.AddTriangle(triangleVertices[0], triangleVertices[1], triangleVertices[2]);
                }
            }

            /// <summary>
            /// Create triangles for a monotone polygon
            /// </summary>
            /// <param name="polygon">the monotone polygon</param>
            /// <param name="startPoint">the first point (clockwise) of the long edge.</param>
            /// <param name="result">the collector for resulting triangles</param>
            private void TriangulateMonotonePolygon(int startPoint, ITriangleCollector result)
            {
                this.PullFirstTriangle(startPoint);
                while (true)
                {
                    if (this.IsConvexCorner())
                    {
                        result.AddTriangle(this.current, this.third, this.second);
                        if (!this.PopOrPullNextVertex())
                        {
                            return;
                        }
                    }
                    else
                    {
                        this.PushAndPullNextVertex();
                    }
                }
            }

            /// <summary>
            /// Gets the first three points from the triangle
            /// </summary>
            /// <param name="startPoint"></param>
            private void PullFirstTriangle(int startPoint)
            {
                this.iterator = this.polygon.IndicesStartingAt(startPoint, this.subPolygonId).GetEnumerator();
                this.iterator.MoveNext();
                this.third = this.iterator.Current;
                this.iterator.MoveNext();
                this.second = this.iterator.Current;
                this.iterator.MoveNext();
                this.current = this.iterator.Current;
            }

            /// <summary>
            /// Current triangle is not valid, push the third point, shift down and pull the next vertex from the polygon
            /// </summary>
            private void PushAndPullNextVertex()
            {
                this.vertexStack.Push(this.third);
                this.third = this.second;
                this.second = this.current;
                if (!this.iterator.MoveNext())
                {
                    throw new InvalidOperationException("Triangle is incomplete");
                }

                this.current = this.iterator.Current;
            }

            /// <summary>
            /// either pop the last vertex from the stack or get the next vertex from the polygon
            /// </summary>
            /// <returns>true if there is one more vertex</returns>
            private bool PopOrPullNextVertex()
            {
                if (this.vertexStack.Count > 0)
                {
                    this.second = this.third;
                    this.third = this.vertexStack.Pop();
                }
                else if (!this.iterator.MoveNext())
                {
                    return false;
                }
                else
                {
                    this.second = this.current;
                    this.current = this.iterator.Current;
                }

                return true;
            }

            /// <summary>
            /// Test if the current three vertices form a clockwise triangle.
            /// </summary>
            /// <returns>true if the triangle is valid</returns>
            private bool IsConvexCorner()
            {
                var v0 = this.vertices[this.current];
                var v1 = this.vertices[this.second];
                var v2 = this.vertices[this.third];
#if UNITY_EDITOR || UNITY_STANDALONE
                var cross = (v2.x - v0.x) * (v1.y - v0.y) - ((v2.y - v0.y) * (v1.x - v0.x));
#else
                var cross = (v2.X - v0.X) * (v1.Y - v0.Y) - ((v2.Y - v0.Y) * (v1.X - v0.X));
#endif
                return cross < 0;
            }

            /// <summary>
            /// Find the point in the polygon that starts at the monotone side
            /// </summary>
            /// <param name="polygon">the polygon</param>
            /// <returns>highest/lowest point in the polygon, depending if itss left hand or right hand. -1 if its a triangle.</returns>
            private int FindStartOfMonotonePolygon()
            {
                var startLookupIterator = this.polygon.SubPolygonVertices(this.subPolygonId).GetEnumerator();
                startLookupIterator.MoveNext();
                var first = startLookupIterator.Current;
                var posmax = first;
                var posmin = first;

                var movedNext = startLookupIterator.MoveNext();
                var posmaxNext = startLookupIterator.Current;
                var count = 1;

                while (movedNext)
                {
                    var index = startLookupIterator.Current;
                    movedNext = startLookupIterator.MoveNext();

                    if (index > posmax)
                    {
                        posmax = index;
                        posmaxNext = movedNext ? startLookupIterator.Current : first;
                    }

                    if (index < posmin)
                    {
                        posmin = index;
                    }

                    count++;
                }

                if (count == 3)
                {
                    return -1;
                }

                if (posmin == posmaxNext)
                {
                    // LHS is a single segment and it's next in the chain
                    return posmaxNext;
                }
                else
                {
                    return posmax;
                }
            }
        }
    }
}