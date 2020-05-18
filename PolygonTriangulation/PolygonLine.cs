using System;
using System.Collections.Generic;
using System.Numerics;

namespace PolygonTriangulation
{
    /// <summary>
    /// A polygon, defined by connected edges.
    /// </summary>
    public class PolygonLine
    {
        private readonly IReadOnlyList<Vector3> vertices;

        private readonly List<int> vertexIds;

        public PolygonLine(int start, int end)
        {
            this.StartKey = start;
            this.EndKey = end;
            this.vertexIds = new List<int> { start, end };
        }

        public int StartKey { get; private set; }

        public int EndKey { get; private set; }

        public bool Dirty { get; private set; }
        
        public bool Closed { get; private set; }

        internal int[] ToIndexes()
        {
            return this.vertexIds.ToArray();
        }

        internal String Debug()
        {
            return string.Concat(this.Closed ? "*" : string.Empty, this.Dirty ? "#" : string.Empty, " ", string.Join(" ", this.vertexIds));
        }

        /// <summary>
        /// Join two polygones by an edge.
        /// </summary>
        /// <param name="other">the other polygone</param>
        /// <param name="edgeStart">the start of the edge that joines</param>
        /// <param name="edgeStart">the end of the edge that joines</param>
        /// <returns>-1: the polygone is closed. Otherwise the start/end key that was changed</returns>
        internal int Join(PolygonLine other, int edgeStart, int edgeEnd)
        {
            if (ReferenceEquals(this, other))
            {
                this.Closed = true;
                return -1;
            }

            // edge direction fit's the polygon direction
            if (edgeStart == this.EndKey && edgeEnd == other.StartKey)
            {
                this.vertexIds.AddRange(other.vertexIds);
                return this.EndKey = other.EndKey;
            }
            else if (edgeEnd == this.StartKey && edgeStart == other.EndKey)
            {
                this.vertexIds.InsertRange(0, other.vertexIds);
                return this.StartKey = other.StartKey;
            }

            // every other case has no continous edge direction
            this.Dirty = true;

            // join start and end, but the edge direction is reversed
            if (edgeEnd == this.EndKey && edgeStart == other.StartKey)
            {
                this.vertexIds.AddRange(other.vertexIds);
                return this.EndKey = other.EndKey;
            }
            else if (edgeStart == this.StartKey && edgeEnd == other.EndKey)
            {
                this.vertexIds.InsertRange(0, other.vertexIds);
                return this.StartKey = other.StartKey;
            }

            // new edge connects at both start or both end points, reverse the other segment and join
            var reversedOther = new List<int>(other.vertexIds);
            reversedOther.Reverse();
            if (edgeStart == this.StartKey && edgeEnd == other.StartKey)
            {
                this.vertexIds.InsertRange(0, reversedOther);
                return this.StartKey = other.EndKey;
            }
            else if (edgeStart == this.EndKey && edgeEnd == other.EndKey)
            {
                this.vertexIds.AddRange(reversedOther);
                return this.EndKey = other.StartKey;
            }
            else if (edgeEnd == this.StartKey && edgeStart == other.StartKey)
            {
                this.vertexIds.InsertRange(0, reversedOther);
                return this.StartKey = other.EndKey;
            }
            else if (edgeEnd == this.EndKey && edgeStart == other.EndKey)
            {
                this.vertexIds.AddRange(reversedOther);
                return this.EndKey = other.StartKey;
            }
            else
            {
                throw new InvalidOperationException($"Can't join s:{edgeStart} e:{edgeEnd}, ts: {this.StartKey} te: {this.EndKey}, os: {other.StartKey} oe: {other.EndKey}");
            }
        }

        /// <summary>
        /// The start value of the added edge matches either the end or start of this polygon
        /// </summary>
        /// <param name="edgeStart">the start of the added edge</param>
        /// <param name="value">the other value of the added edgeegment</param>
        internal void AddMatchingStart(int edgeStart, int value)
        {
            if (edgeStart == this.EndKey)
            {
                this.EndKey = value;
                this.vertexIds.Add(value);
            }
            else
            {
                this.Dirty = true;
                this.vertexIds.Insert(0, value);
                this.StartKey = value;
            }
        }

        /// <summary>
        /// The end value of the added edge matches either the end or start of this polygon
        /// </summary>
        /// <param name="value">the start value of the added edge</param>
        /// <param name="edgeEnd">the matching end</param>
        internal void AddMatchingEnd(int value, int edgeEnd)
        {
            if (edgeEnd == this.StartKey)
            {
                this.vertexIds.Insert(0, value);
                this.StartKey = value;
            }
            else
            {
                this.Dirty = true;
                this.EndKey = value;
                this.vertexIds.Add(value);
            }
        }
    }
}
