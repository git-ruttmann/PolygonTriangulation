namespace PolygonTriangulation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// subclass container for triangulator
    /// </summary>
    public partial class PolygonTriangulator
    {
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
            /// Traverse the polygon and build all splits
            /// </summary>
            /// <param name="stepCount">number of steps during debugging. Use -1 for all</param>
            public void BuildSplits(int stepCount)
            {
                foreach (var group in this.polygon.OrderedVertices.GroupBy(x => x.Id))
                {
                    var actions = group.ToArray();
                    if (actions.Length > 1)
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
        }
    }
}