namespace Ruttmann.PolygonTriangulation.Seidel
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Converts the trapezoid structure to splits
    /// </summary>
    public class TrapezoidToSplits
    {
        private List<Tuple<int, int>> segmentSplits;

        private TrapezoidToSplits()
        {
            this.segmentSplits = new List<Tuple<int, int>>();
        }

        /// <summary>
        /// Extract the splits from the trapezoid
        /// </summary>
        /// <param name="firstTriangle">the first triangle - a trapezoid without either a top or bottom</param>
        /// <returns>Tuples of segment id's where to split</returns>
        public static IEnumerable<Tuple<int, int>> ExtractSplits(Trapezoid firstTriangle)
        {
            var instance = new TrapezoidToSplits();
            instance.Traverse(firstTriangle);
            return instance.segmentSplits;
        }

        /// <summary>
        /// Recursively traverse the Trapezoid pointer structure
        /// </summary>
        /// <param name="trapezoid">the current trapezoid</param>
        /// <remarks>
        /// rseg points upwards, lseg points downwards
        /// </remarks>
        private void Traverse(Trapezoid trapezoid)
        {
            var visitedTrapezoids = new HashSet<Trapezoid>();
            var stack = new Stack<Trapezoid>();
            stack.Push(trapezoid);

            while (stack.Count > 0)
            {
                trapezoid = stack.Pop();
                int uplinkCount = (trapezoid.u[0] == null ? 0 : 1) + (trapezoid.u[1] == null ? 0 : 1);
                int downlinkCount = (trapezoid.d[0] == null ? 0 : 1) + (trapezoid.d[1] == null ? 0 : 1);

                switch (uplinkCount * 4 + downlinkCount)
                {
                    // downward opening triangle
                    case 0 * 4 + 2:
                        this.AddSegmentSplit(trapezoid.d[1].lseg.Id, trapezoid.lseg.Id);
                        break;

                    // upward opening triangle
                    case 2 * 4 + 0:
                        this.AddSegmentSplit(trapezoid.rseg.Id, trapezoid.u[0].rseg.Id);
                        break;

                    // downward + upward cusps
                    case 2 * 4 + 2:
                        this.AddSegmentSplit(trapezoid.d[1].lseg.Id, trapezoid.u[0].rseg.Id);
                        break;

                    // only downward cusp
                    case 2 * 4 + 1:
                        if (VertexComparer.Instance.Equal(trapezoid.lo, trapezoid.lseg.v1))
                        {
                            this.AddSegmentSplit(trapezoid.u[0].rseg.Id, trapezoid.lseg.NextId);
                        }
                        else
                        {
                            this.AddSegmentSplit(trapezoid.rseg.Id, trapezoid.u[0].rseg.Id);
                        }
                        break;

                    // only upward cusp
                    case 1 * 4 + 2:
                        if (VertexComparer.Instance.Equal(trapezoid.hi, trapezoid.lseg.v0))
                        {
                            this.AddSegmentSplit(trapezoid.d[1].lseg.Id, trapezoid.lseg.Id);
                        }
                        else
                        {
                            this.AddSegmentSplit(trapezoid.d[1].lseg.Id, trapezoid.rseg.NextId);
                        }
                        break;

                    // no cusp
                    case 1 * 4 + 1:
                        if (VertexComparer.Instance.Equal(trapezoid.hi, trapezoid.lseg.v0) &&
                            VertexComparer.Instance.Equal(trapezoid.lo, trapezoid.rseg.v0))
                        {
                            this.AddSegmentSplit(trapezoid.rseg.Id, trapezoid.lseg.Id);
                        }
                        else if (VertexComparer.Instance.Equal(trapezoid.hi, trapezoid.rseg.v1) &&
                            VertexComparer.Instance.Equal(trapezoid.lo, trapezoid.lseg.v1))
                        {
                            this.AddSegmentSplit(trapezoid.rseg.NextId, trapezoid.lseg.NextId);
                        }
                        else
                        {
                            // no split possible
                        }
                        break;

                    case 1 * 4 + 0:
                        break;

                    case 0 * 4 + 1:
                        break;

                    default:
                        throw new InvalidOperationException("Bad UL/DL count combination");
                }

                PushIfNew(visitedTrapezoids, stack, trapezoid.d[0]);
                PushIfNew(visitedTrapezoids, stack, trapezoid.d[1]);
                PushIfNew(visitedTrapezoids, stack, trapezoid.u[0]);
                PushIfNew(visitedTrapezoids, stack, trapezoid.u[1]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddSegmentSplit(int from, int to)
        {
            this.segmentSplits.Add(Tuple.Create(from, to));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void PushIfNew(ISet<Trapezoid> known, Stack<Trapezoid> stack, Trapezoid trapezoid)
        {
            if (trapezoid != null && known.Add(trapezoid))
            {
                stack.Push(trapezoid);
            }
        }
    }
}