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
            visitedTrapezoids.Add(trapezoid);
            stack.Push(trapezoid);

            while (stack.Count > 0)
            {
                trapezoid = stack.Pop();

                ProcessTrapezoid(trapezoid);

                PushIfNew(visitedTrapezoids, stack, trapezoid.d[0]);
                PushIfNew(visitedTrapezoids, stack, trapezoid.d[1]);
                PushIfNew(visitedTrapezoids, stack, trapezoid.u[0]);
                PushIfNew(visitedTrapezoids, stack, trapezoid.u[1]);
            }
        }

        /// <summary>
        /// Detect if the trapezoid represents a split situation, depending on number of trapezoids in up/down direction
        /// </summary>
        /// <param name="trapezoid">the trapezoid to process</param>
        private void ProcessTrapezoid(Trapezoid trapezoid)
        {
            int uplinkCount = (trapezoid.u[0] == null ? 0 : 1) + (trapezoid.u[1] == null ? 0 : 1);
            int downlinkCount = (trapezoid.d[0] == null ? 0 : 1) + (trapezoid.d[1] == null ? 0 : 1);

            switch (uplinkCount * 4 + downlinkCount)
            {
                // cusp from below is reaching inside terminal triangle. cut our triangle cusp and the touching cusp
                case 0 * 4 + 2:
                    this.AddSegmentSplit(trapezoid.d[1].lseg.Id, trapezoid.lseg.Id);
                    break;

                // cusp from above is reaching inside terminal triangle. cut our triangle cusp and the touching cusp
                case 2 * 4 + 0:
                    this.AddSegmentSplit(trapezoid.rseg.Id, trapezoid.u[0].rseg.Id);
                    break;

                // downward + upward cusps, connect the two cusps
                case 2 * 4 + 2:
                    this.AddSegmentSplit(trapezoid.d[1].lseg.Id, trapezoid.u[0].rseg.Id);
                    break;

                // downward cusp is touching from above
                case 2 * 4 + 1:
                    if (VertexComparer.Instance.Equal(trapezoid.lo, trapezoid.lseg.v1))
                    {
                        this.AddSegmentSplit(trapezoid.u[0].rseg.Id, trapezoid.lseg.NextId);
                    }
#if DEBUG
                    else if (!VertexComparer.Instance.Equal(trapezoid.lo, trapezoid.rseg.v0))
                    {
                        throw new InvalidOperationException("Low point must be either on left or right segment as there is only one downlink");
                    }
#endif
                    else
                    {
                        this.AddSegmentSplit(trapezoid.rseg.Id, trapezoid.u[0].rseg.Id);
                    }
                    break;

                // upward cusp is touching from below
                case 1 * 4 + 2:
                    if (VertexComparer.Instance.Equal(trapezoid.hi, trapezoid.lseg.v0))
                    {
                        this.AddSegmentSplit(trapezoid.d[1].lseg.Id, trapezoid.lseg.Id);
                    }
#if DEBUG
                    else if (!VertexComparer.Instance.Equal(trapezoid.hi, trapezoid.rseg.v1))
                    {
                        throw new InvalidOperationException("High point must be either on left or right segment as there is only one uplink");
                    }
#endif
                    else
                    {
                        this.AddSegmentSplit(trapezoid.d[1].lseg.Id, trapezoid.rseg.NextId);
                    }
                    break;

                // one above and one below, check if the trapezoid has two vertexes in the diagonale
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
                        // upper and lower are on the same segment diagonale - no split possible
                    }
                    break;

                case 1 * 4 + 0:
                    break;

                case 0 * 4 + 1:
                    break;

                default:
                    throw new InvalidOperationException("Bad UL/DL count combination");
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