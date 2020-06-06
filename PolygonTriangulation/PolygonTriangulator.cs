namespace PolygonTriangulation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;

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
            var monotones = Polygon.Split(this.polygon, splits, collector);
            foreach (var monotonPolygon in monotones)
            {
                var triangluator = new MonotonePolygonTriangulator(monotonPolygon);
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
        /// Traverse the polygon, build trapezoids and evaluate possible splits
        /// </summary>
        private class ScanSplitByTrapezoidation : IPolygonSplitter
        {
            private readonly SortedActiveEdgeList<Trapezoid> activeEdges;
            private readonly List<Tuple<int, int>> splits;
            private readonly Polygon polygon;

            private ScanSplitByTrapezoidation(Polygon polygon)
            {
                this.splits = new List<Tuple<int, int>>();
                this.polygon = polygon;

                this.activeEdges = new SortedActiveEdgeList<Trapezoid>(this.polygon.Vertices);
            }

            /// <summary>
            /// Build the splits for the polygon
            /// </summary>
            /// <param name="polygon">the polygon</param>
            /// <returns>the splits</returns>
            public static IEnumerable<Tuple<int, int>> BuildSplits(Polygon polygon)
            {
                var splitter = new ScanSplitByTrapezoidation(polygon);
                splitter.BuildSplits();
                return splitter.splits;
            }

            /// <summary>
            /// Traverse the polygon and build all splits
            /// </summary>
            public void BuildSplits()
            {
                foreach (var vertexInfo in this.polygon.OrderedVertexes)
                {
                    this.HandleVertex(vertexInfo);
                }
            }

            /// <inheritdoc/>
            public void SplitPolygon(int leftVertex, int rightVertex)
            {
                this.splits.Add(Tuple.Create(leftVertex, rightVertex));
            }

            /// <summary>
            /// Handle a vertex in context of its previous and next vertex
            /// </summary>
            /// <param name="vertexId">the id of the vertex</param>
            /// <param name="next">the next vertex in the polygon</param>
            /// <param name="prev">the previous vertex in the polygon</param>
            private void HandleVertex(IPolygonVertexInfo info)
            {
                if (info.Id < info.Prev && info.Id < info.Next)
                {
                    this.HandleOpeningCusp(info);
                }
                else if (info.Id > info.Prev && info.Id > info.Next)
                {
                    this.HandleClosingCusp(info);
                }
                else
                {
                    this.HandleTransition(info);
                }
            }

            private void HandleOpeningCusp(IPolygonVertexInfo info)
            {
                var (lowerEdge, upperEdge) = this.activeEdges.Begin(info.Id, info.Prev, info.Next);
                if (lowerEdge.IsRightToLeft)
                {
                    Trapezoid.EnterInsideBySplit(info.Id, lowerEdge, upperEdge, this);
                }
                else
                {
                    var trapezoid = lowerEdge.BelowData;
                    trapezoid.LeaveInsideBySplit(info.Id, lowerEdge, upperEdge, this);
                }
            }

            private void HandleClosingCusp(IPolygonVertexInfo info)
            {
                var lowerEdge = this.activeEdges.EdgeForVertex(info.Id);

                var lowerTrapezoid = lowerEdge.Data;
                if (lowerEdge.IsRightToLeft)
                {
                    lowerTrapezoid.LeaveInsideByJoin(info.Id, this);
                }
                else
                {
                    var upperTrapezoid = lowerEdge.AboveData;
                    Trapezoid.EnterInsideByJoin(lowerTrapezoid, upperTrapezoid, info.Id, this);
                }

                this.activeEdges.Finish(lowerEdge);
            }

            private void HandleTransition(IPolygonVertexInfo info)
            {
                var oldEdge = this.activeEdges.EdgeForVertex(info.Id);
                var trapezoid = oldEdge.Data;
                if (oldEdge.IsRightToLeft)
                {
                    var newEdge = this.activeEdges.Transition(oldEdge, info.Prev);
                    trapezoid.TransitionOnLowerEdge(info.Id, newEdge, this);
                }
                else
                {
                    var newEdge = this.activeEdges.Transition(oldEdge, info.Next);
                    trapezoid.TransitionOnUpperEdge(info.Id, newEdge, this);
                }
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
            private readonly IReadOnlyList<Vector2> vertices;
            private readonly Stack<int> vertexStack;
            private int third;
            private int second;
            private int current;
            private IEnumerator<int> iterator;

            public MonotonePolygonTriangulator(Polygon polygon)
            {
                this.polygon = polygon;
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
                    var vertices = this.polygon.Indices.ToArray();
                    collector.AddTriangle(vertices[0], vertices[1], vertices[2]);
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
                this.iterator = this.polygon.IndicesStartingAt(startPoint).GetEnumerator();
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
                var cross = (v2.X - v0.X) * (v1.Y - v0.Y) - ((v2.Y - v0.Y) * (v1.X - v0.X));
                return cross < 0;
            }

            /// <summary>
            /// Find the point in the polygon that starts at the monotone side
            /// </summary>
            /// <param name="polygon">the polygon</param>
            /// <returns>highest/lowest point in the polygon, depending if itss left hand or right hand. -1 if its a triangle.</returns>
            private int FindStartOfMonotonePolygon()
            {
                var iterator = this.polygon.Indices.GetEnumerator();
                iterator.MoveNext();
                var first = iterator.Current;
                var posmax = first;
                var posmin = first;

                var movedNext = iterator.MoveNext();
                var posmaxNext = iterator.Current;
                var count = 1;

                while (movedNext)
                {
                    var index = iterator.Current;
                    movedNext = iterator.MoveNext();

                    if (index > posmax)
                    {
                        posmax = index;
                        posmaxNext = movedNext ? iterator.Current : first;
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