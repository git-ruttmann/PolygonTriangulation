namespace PolygonTriangulation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using IEdge = IActiveEdge<Trapezoid>;

    [DebuggerDisplay("{Debug}")]
    public class Trapezoid
    {
        /// <summary>
        /// corners with valid points (to detect diagonales)
        /// </summary>
        [Flags]
        private enum CornerValidity
        {
            None = 0,
            UpperLeft = 1,
            LowerLeft = 2,
            UpperRight = 4,
            LowerRight = 8,
            Diagonale1 = UpperLeft + LowerRight,
            Diagonale2 = LowerLeft + UpperRight,
        }

        /// <summary>
        /// Number of neighbors to the left and to the right
        /// </summary>
        [Flags]
        private enum Count
        {
            None = 0,
            OneLeft = 1,
            TwoLeft = 2,
            OneRight = 4,
            TwoRight = 8,
        }

        private CornerValidity cornerValidity;

        private Count neighborCount;

        /// <summary>
        /// Instantiate a new trapezoid without left neighbors
        /// </summary>
        /// <param name="vertexId">the left vertex</param>
        /// <param name="lowerEdge">the lower edge</param>
        private Trapezoid(int vertexId, IEdge lowerEdge)
        {
            this.LeftVertex = vertexId;
            this.cornerValidity = CornerValidity.None;

            this.UpperEdge = lowerEdge.Above;
            this.LowerEdge = lowerEdge;

            this.UpperEdge.Data = this;
            this.LowerEdge.Data = this;

            this.neighborCount = Count.None;
        }

        /// <summary>
        /// Instanstiate a new trapezoid with one left neighbor
        /// </summary>
        /// <param name="left">the left neighbor</param>
        /// <param name="nextEdge">the next edge</param>
        /// <param name="upper">flag if nextEdge is the upper or the lower edge</param>
        private Trapezoid(Trapezoid left, IEdge nextEdge, bool upper)
        {
            this.LeftVertex = left.RightVertex;

            this.UpperEdge = upper ? nextEdge : left.UpperEdge;
            this.LowerEdge = !upper ? nextEdge : left.LowerEdge;

            this.UpperEdge.Data = this;
            this.LowerEdge.Data = this;

            this.neighborCount = Count.OneLeft;
        }

        /// <summary>
        /// Instanstiate a new trapezoid with two left neighbors
        /// </summary>
        /// <param name="upper">the upper left neighbor</param>
        /// <param name="lower">the lower left neighbor</param>
        private Trapezoid(Trapezoid upper, Trapezoid lower)
        {
            this.LeftVertex = upper.RightVertex;
            this.cornerValidity = CornerValidity.None;

            this.UpperEdge = upper.UpperEdge;
            this.LowerEdge = lower.LowerEdge;

            this.UpperEdge.Data = this;
            this.LowerEdge.Data = this;

            this.neighborCount = Count.TwoLeft;
        }

        /// <summary>
        /// Gets the index of the left vertex. The corner validity defines if it was an upper or a lower or a triangle point
        /// </summary>
        public int LeftVertex { get; private set; }

        /// <summary>
        /// Gets the index of the right vertex.
        /// </summary>
        public int RightVertex { get; private set; }

        /// <summary>
        /// Gets the upper edge, limiting the trapezoid
        /// </summary>
        public IEdge UpperEdge { get; }

        /// <summary>
        /// Gets the lower edge, limiting the trapezoid
        /// </summary>
        public IEdge LowerEdge { get; }

        /// <summary>
        /// Gets a debug string
        /// </summary>
        public string Debug => $"{this.LeftVertex} {this.RightVertex} {this.cornerValidity} {this.neighborCount} Low: {this.LowerEdge.Debug} High: {this.UpperEdge.Debug}";

        /// <summary>
        /// A left cusp that enters the polygon space. Create a new Trapezoid.
        /// </summary>
        /// <param name="vertexId"></param>
        /// <param name="lowerEdge"></param>
        public static void EnterInsideBySplit(int vertexId, IEdge lowerEdge)
        {
            var _ = new Trapezoid(vertexId, lowerEdge);
        }

        /// <summary>
        /// A right cusp that enters the polygon space. Join the two Trapezoids in one.
        /// </summary>
        /// <param name="vertexId">the vertex id</param>
        public static void EnterInsideByJoin(Trapezoid lower, Trapezoid upper, int vertexId)
        {
            lower.RightVertex = upper.RightVertex = vertexId;

            upper.cornerValidity |= CornerValidity.LowerRight;
            lower.cornerValidity |= CornerValidity.UpperRight;

            upper.neighborCount |= Count.OneRight;
            lower.neighborCount |= Count.OneRight;

            var _ = new Trapezoid(upper, lower);
        }

        /// <summary>
        /// Transition from one edge to the next
        /// </summary>
        /// <param name="vertexId">the vertex id of the transition point</param>
        /// <param name="nextEdge">the next edge</param>
        public void Transition(int vertexId, IEdge nextEdge)
        {
            this.RightVertex = vertexId;
            this.neighborCount |= Count.OneRight;

            bool upper;
            if (vertexId == this.UpperEdge.Right)
            {
                upper = true;
                this.cornerValidity |= CornerValidity.UpperRight;
            }
            else if (vertexId == this.LowerEdge.Right)
            {
                upper = false;
                this.cornerValidity |= CornerValidity.LowerRight;
            }
            else
            {
                throw new InvalidOperationException("Transition must be on either segment");
            }

            var nextTrapezoid = new Trapezoid(this, nextEdge, upper);
            nextTrapezoid.cornerValidity = (CornerValidity)(((int)this.cornerValidity) >> 2);
        }

        /// <summary>
        /// A cusp that transitions from inside to outside. Splits the Trapezoid by one point.
        /// </summary>
        /// <param name="vertexId">the vertex id of the start point</param>
        /// <param name="upperEdge">the upper edge</param>
        /// <param name="lowerEdge">the lower edge</param>
        public void LeaveInsideBySplit(int vertexId, IEdge lowerEdge)
        {
            this.RightVertex = vertexId;
            this.neighborCount |= Count.TwoRight;

            var upperTrapezoid = new Trapezoid(this, lowerEdge.Above, false);
            upperTrapezoid.cornerValidity = CornerValidity.LowerLeft;

            var lowerTrapezoid = new Trapezoid(this, lowerEdge, true);
            lowerTrapezoid.cornerValidity = CornerValidity.UpperLeft;
        }

        /// <summary>
        /// Join two edges and right to the vertex is outside the polygon
        /// </summary>
        /// <param name="vertexId">the closing vertex id</param>
        public void LeaveInsideByJoin(int vertexId)
        {
            if (this.UpperEdge.Right != this.LowerEdge.Right)
            {
                throw new InvalidOperationException("Joining two non-equal segements");
            }

            this.RightVertex = vertexId;
        }

        /// <summary>
        /// Get the split of the trapezoid
        /// </summary>
        /// <returns>null if no split</returns>
        public Tuple<int, int> GetSplit()
        {
            switch (this.neighborCount)
            {
                // anything with two neighbors => a cusp => always split
                case Count.TwoLeft:
                case Count.TwoRight:
                case Count.OneLeft | Count.TwoRight:
                case Count.TwoLeft | Count.OneRight:
                case Count.TwoLeft | Count.TwoRight:
                    return Tuple.Create(this.LeftVertex, this.RightVertex);

                // one left and one right => cut diagonales only
                case Count.OneLeft | Count.OneRight:
                    if (this.cornerValidity == CornerValidity.Diagonale1)
                    {
                        return Tuple.Create(this.LeftVertex, this.RightVertex);
                    }
                    if (this.cornerValidity == CornerValidity.Diagonale2)
                    {
                        return Tuple.Create(this.LeftVertex, this.RightVertex);
                    }
                    else
                    {
                        return null;
                    }

                case Count.OneLeft:
                case Count.OneRight:
                    return null;

                default:
                    throw new InvalidOperationException("Bad combination of neighbor count");
            }
        }
    }
}
