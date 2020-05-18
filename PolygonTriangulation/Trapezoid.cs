using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolygonTriangulation
{
    [DebuggerDisplay("{Debug}")]
    public class Trapezoid
    {
        private List<int> upperPoints;
        private List<int> lowerPoints;
        private bool? lastWasLower;

        public Trapezoid(int start)
        {
            this.Start = start;
            this.lastWasLower = null;
            this.upperPoints = new List<int>();
            this.lowerPoints = new List<int>();
        }

        /// <summary>
        /// Gets the start point - the leftmost point.
        /// </summary>
        public int Start { get; }

        /// <summary>
        /// Get's the previous closed trapezoid
        /// </summary>
        public Trapezoid Prev { get; private set; }

        internal string Debug => $"{this.Start} _:{String.Join(" ", this.lowerPoints)} {(this.lastWasLower == true ? "*" : "")}|{(this.lastWasLower == false ? "*" : "")} ¯:{String.Join(" ", this.upperPoints)}";

        /// <summary>
        /// Create the lower half of a split. the new vertex is added at the upper points
        /// </summary>
        /// <param name="vertexId">the start point of the split</param>
        /// <returns>the new lower half</returns>
        internal Trapezoid SplitLower(int vertexId)
        {
            var trapezoid = new Trapezoid(this.Start);
            trapezoid.Prev = this.Prev;

            if (this.lastWasLower == null)
            {
                trapezoid.upperPoints.Add(this.Start);
                trapezoid.upperPoints.Add(vertexId);
                trapezoid.lastWasLower = false;
                return trapezoid;
            }

            trapezoid.lastWasLower = this.lastWasLower;
            if (this.lastWasLower == true)
            {
                trapezoid.lowerPoints.AddRange(this.lowerPoints);
                return trapezoid.AddedNewPoint(false);
            }
            else
            {
                trapezoid.lastWasLower = false;
                return trapezoid;
            }
        }

        /// <summary>
        /// Create the upper half of a split. the new vertex is added at the lower points
        /// </summary>
        /// <param name="vertexId">the start point of the split</param>
        /// <returns>the new lower half</returns>
        internal Trapezoid SplitUpper(int vertexId)
        {
            var trapezoid = new Trapezoid(this.Start);
            trapezoid.Prev = this.Prev;
            trapezoid.lowerPoints.Add(this.Start);
            trapezoid.lowerPoints.Add(vertexId);

            if (this.lastWasLower == null)
            {
                trapezoid.lastWasLower = true;
                return trapezoid;
            }

            trapezoid.lastWasLower = this.lastWasLower;
            if (this.lastWasLower == false)
            {
                trapezoid.upperPoints.AddRange(this.upperPoints);
                return trapezoid.AddedNewPoint(true);
            }
            else
            {
                trapezoid.lastWasLower = true;
                return trapezoid;
            }
        }

        /// <summary>
        /// Add a point from a leftToRight line transition.
        /// </summary>
        /// <param name="vertexId">the point</param>
        /// <returns>the same or a newly created trapezoid</returns>
        internal Trapezoid AddUpper(int vertexId)
        {
            if (this.lastWasLower == null)
            {
                this.lowerPoints.Add(this.Start);
                this.upperPoints.Add(vertexId);
                this.lastWasLower = false;
                return this;
            }

            this.upperPoints.Add(vertexId);
            return this.AddedNewPoint(false);
        }

        /// <summary>
        /// Add a point from a rightToLeft line transition.
        /// </summary>
        /// <param name="vertexId">the point</param>
        /// <returns>the same or a newly created trapezoid</returns>
        internal Trapezoid AddLower(int vertexId)
        {
            if (this.lastWasLower == null)
            {
                this.upperPoints.Add(this.Start);
                this.lowerPoints.Add(vertexId);
                this.lastWasLower = true;
                return this;
            }

            this.lowerPoints.Add(vertexId);
            return this.AddedNewPoint(true);
        }

        private Trapezoid AddedNewPoint(bool addedLower)
        {
            if (addedLower == this.lastWasLower)
            {
                return this;
            }

            this.lastWasLower = addedLower;
            if (this.upperPoints.Count <= 1 && this.lowerPoints.Count <= 1)
            {
                return this;
            }

            Trapezoid newTrapezoid;
            if (addedLower)
            {
                newTrapezoid = new Trapezoid(this.upperPoints.Last());
            }
            else
            {
                newTrapezoid = new Trapezoid(this.lowerPoints.Last());
            }

            newTrapezoid.lowerPoints.Add(this.lowerPoints.Last());
            newTrapezoid.upperPoints.Add(this.upperPoints.Last());
            newTrapezoid.lastWasLower = addedLower;
            newTrapezoid.Prev = this;
            return newTrapezoid;
        }
    }
}
