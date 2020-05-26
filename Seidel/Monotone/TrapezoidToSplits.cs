namespace Ruttmann.PolygonTriangulation.Seidel
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Converts the trapezoid structure to splits
    /// </summary>
    public class TrapezoidToSplits
    {
        private enum Direction
        {
            Up,
            Down,
        }

        private ISet<Trapezoid> visitedTrapezoids;
        private List<Tuple<int, int>> segmentSplits;

        private TrapezoidToSplits()
        {
            this.segmentSplits = new List<Tuple<int, int>>();
            this.visitedTrapezoids = new HashSet<Trapezoid>();
        }

        /// <summary>
        /// Extract the splits from the trapezoid
        /// </summary>
        /// <param name="firstTriangle">the first triangle - a trapezoid without either a top or bottom</param>
        /// <returns>Tuples of segment id's where to split</returns>
        public static IEnumerable<Tuple<int, int>> ExtractSplits(Trapezoid firstTriangle)
        {
            var instance = new TrapezoidToSplits();
            if (firstTriangle.u[0] != null)
                instance.Traverse(firstTriangle, firstTriangle.u[0], Direction.Up);
            else if (firstTriangle.d[0] != null)
                instance.Traverse(firstTriangle, firstTriangle.d[0], Direction.Down);

            return instance.segmentSplits;
        }

        /// <summary>
        /// Recursively traverse the Trapezoid pointer structure
        /// </summary>
        /// <param name="trapezoid">the current trapezoid</param>
        /// <param name="from">the source trapezoid</param>
        /// <param name="slotId">the slot we're from</param>
        /// <param name="direction">the direction where the source lies</param>
        /// <remarks>
        /// rseg points upwards, lseg points downwards
        /// </remarks>
        private void Traverse(Trapezoid trapezoid, Trapezoid from, Direction direction)
        {
            Segment s0 = null, s1 = null;

            if (trapezoid == null)
            {
                return;
            }

            if (!this.visitedTrapezoids.Add(trapezoid))
            {
                return;
            }

            int uplinkCount = (trapezoid.u[0] == null ? 0 : 1) + (trapezoid.u[1] == null ? 0 : 1);
            int downlinkCount = (trapezoid.d[0] == null ? 0 : 1) + (trapezoid.d[1] == null ? 0 : 1);

            bool invertSplitDirection = false;

            switch (uplinkCount * 4 + downlinkCount)
            {
                // downward opening triangle
                case 0 * 4 + 2:
                    s0 = trapezoid.d[1].lseg;
                    s1 = trapezoid.lseg;
                    invertSplitDirection = from == trapezoid.d[1];
                    break;

                // upward opening triangle
                case 2 * 4 + 0:
                    s0 = trapezoid.rseg;
                    s1 = trapezoid.u[0].rseg;
                    invertSplitDirection = from == trapezoid.u[1];
                    break;

                // downward + upward cusps
                case 2 * 4 + 2:
                    s0 = trapezoid.d[1].lseg;
                    s1 = trapezoid.u[0].rseg;
                    if (((direction == Direction.Down) && (trapezoid.d[1] == from)) ||
                        ((direction == Direction.Up) && (trapezoid.u[1] == from)))
                    {
                        invertSplitDirection = true;
                    }
                    break;

                // only downward cusp
                case 2 * 4 + 1:
                    if (VertexComparer.Instance.Equal(trapezoid.lo, trapezoid.lseg.v1))
                    {
                        s0 = trapezoid.u[0].rseg;
                        s1 = trapezoid.lseg.Next;
                        invertSplitDirection = (direction == Direction.Up) && (trapezoid.u[0] == from);
                    }
                    else
                    {
                        s0 = trapezoid.rseg;
                        s1 = trapezoid.u[0].rseg;
                        invertSplitDirection = (direction == Direction.Up) && (trapezoid.u[1] == from);
                    }
                    break;

                // only upward cusp
                case 1 * 4 + 2:
                    if (VertexComparer.Instance.Equal(trapezoid.hi, trapezoid.lseg.v0))
                    {
                        s0 = trapezoid.d[1].lseg;
                        s1 = trapezoid.lseg;
                        invertSplitDirection = !((direction == Direction.Down) && (trapezoid.d[0] == from));
                    }
                    else
                    {
                        s0 = trapezoid.d[1].lseg;
                        s1 = trapezoid.rseg.Next;
                        invertSplitDirection = (direction == Direction.Down) && (trapezoid.d[1] == from);
                    }
                    break;

                // no cusp
                case 1 * 4 + 1:
                    if (VertexComparer.Instance.Equal(trapezoid.hi, trapezoid.lseg.v0) &&
                        VertexComparer.Instance.Equal(trapezoid.lo, trapezoid.rseg.v0))
                    {
                        s0 = trapezoid.rseg;
                        s1 = trapezoid.lseg;
                        invertSplitDirection = direction == Direction.Up;
                    }
                    else if (VertexComparer.Instance.Equal(trapezoid.hi, trapezoid.rseg.v1) &&
                        VertexComparer.Instance.Equal(trapezoid.lo, trapezoid.lseg.v1))
                    {
                        s0 = trapezoid.rseg.Next;
                        s1 = trapezoid.lseg.Next;
                        invertSplitDirection = direction == Direction.Up;
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

            if (s0 != null && s1 != null)
            {
                if (invertSplitDirection)
                {
                    this.segmentSplits.Add(Tuple.Create(s1.Id, s0.Id));
                }
                else
                {
                    this.segmentSplits.Add(Tuple.Create(s0.Id, s1.Id));
                }
            }

            Traverse(trapezoid.d[1], trapezoid, Direction.Up);
            Traverse(trapezoid.d[0], trapezoid, Direction.Up);
            Traverse(trapezoid.u[1], trapezoid, Direction.Down);
            Traverse(trapezoid.u[0], trapezoid, Direction.Down);
        }
    }
}