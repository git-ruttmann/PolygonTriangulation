namespace PolygonTriangulation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// subclass container
    /// </summary>
    public partial class PlanePolygonBuilder
    {
        /// <summary>
        /// A polygon, defined by connected edges.
        /// </summary>
        [DebuggerDisplay("{Debug}")]
        private class PolygonLine
        {
            private readonly List<int> vertexIds;

            /// <summary>
            /// Initializes a new instance of the <see cref="PolygonLine" /> class. The line starts with two vertices.
            /// </summary>
            /// <param name="start">the first vertex</param>
            /// <param name="end">the second vertex</param>
            public PolygonLine(int start, int end)
            {
                this.StartKey = start;
                this.EndKey = end;
                this.vertexIds = new List<int> { start, end };
            }

            /// <summary>
            /// Gets the first vertex id in the polygon
            /// </summary>
            public int StartKey { get; private set; }

            /// <summary>
            /// Gets the last vertex id in the polygon
            /// </summary>
            public int EndKey { get; private set; }

            /// <summary>
            /// Gets a value indicating whether the edge direction was inconsistend at any time
            /// </summary>
            public bool Dirty { get; private set; }

            /// <summary>
            /// Gets a value indicating whether the polygon is closed
            /// </summary>
            public bool Closed { get; private set; }

            /// <summary>
            /// Gets a debug string
            /// </summary>
            public string Debug => $"{(this.Closed ? "*" : string.Empty)}, {(this.Dirty ? "#" : string.Empty)}, {string.Join(" ", this.vertexIds)}";

            /// <summary>
            /// Gets the vertex ids in order
            /// </summary>
            /// <returns>the vertex ids</returns>
            public IReadOnlyCollection<int> ToIndexes()
            {
                return this.vertexIds;
            }

            /// <summary>
            /// The start value of the added edge matches either the end or start of this polygon
            /// </summary>
            /// <param name="edgeStart">the start of the added edge</param>
            /// <param name="value">the other value of the added edgeegment</param>
            public void AddMatchingStart(int edgeStart, int value)
            {
                if (edgeStart == this.EndKey)
                {
                    this.AddSingleVertex(value, true);
                }
                else
                {
                    this.Dirty = true;
                    this.AddSingleVertex(value, false);
                }
            }

            /// <summary>
            /// The end value of the added edge matches either the end or start of this polygon
            /// </summary>
            /// <param name="value">the start value of the added edge</param>
            /// <param name="edgeEnd">the matching end</param>
            public void AddMatchingEnd(int value, int edgeEnd)
            {
                if (edgeEnd == this.StartKey)
                {
                    this.AddSingleVertex(value, false);
                }
                else
                {
                    this.Dirty = true;
                    this.AddSingleVertex(value, true);
                }
            }

            /// <summary>
            /// Join two polygones by an edge.
            /// </summary>
            /// <param name="other">the other polygone</param>
            /// <param name="edgeStart">the start of the edge that joines</param>
            /// <param name="edgeEnd">the end of the edge that joines</param>
            /// <returns>-1: the polygone is closed. Otherwise the start/end key that was changed</returns>
            public int Join(PolygonLine other, int edgeStart, int edgeEnd)
            {
                return this.JoinAndClose(other)
                    ?? this.JoinSameDirection(other, edgeStart, edgeEnd)
                    ?? this.JoinWithEdgeInInverseDirection(other, edgeStart, edgeEnd)
                    ?? this.JoinReversingOtherPolygon(other, edgeStart, edgeEnd)
                    ?? throw new InvalidOperationException($"Can't join s:{edgeStart} e:{edgeEnd}, ts: {this.StartKey} te: {this.EndKey}, os: {other.StartKey} oe: {other.EndKey}");
            }

            /// <summary>
            /// Remove a vertex that is either start or end
            /// </summary>
            /// <param name="vertexId">the id of the vertex</param>
            /// <returns>true if start was modified, false if end was modified</returns>
            public bool RemoveVertex(int vertexId)
            {
                if (vertexId == this.StartKey)
                {
                    this.vertexIds.RemoveAt(0);
                    this.StartKey = this.vertexIds[0];
                    return true;
                }
                else if (vertexId == this.EndKey)
                {
                    this.vertexIds.RemoveAt(this.vertexIds.Count - 1);
                    this.EndKey = this.vertexIds[this.vertexIds.Count - 1];
                    return false;
                }
                else
                {
                    throw new InvalidOperationException("Can't remove a vertex in the middle of the polygon line");
                }
            }

            /// <summary>
            /// Compare the edge points to two corresponding keys.
            /// </summary>
            /// <param name="edgeStart">the edge start</param>
            /// <param name="edgeEnd">the edge end</param>
            /// <param name="keyStart">the key that's compared to edgeStart</param>
            /// <param name="keyEnd">the key that's compared to edgeEnd</param>
            /// <returns>true if edgeStart matches key1 and EdgeEnd matches key2</returns>
            private static bool CompareEdgeToKeys(int edgeStart, int edgeEnd, int keyStart, int keyEnd)
            {
                return (edgeStart == keyStart) && (edgeEnd == keyEnd);
            }

            /// <summary>
            /// Compare the edge points to two corresponding keys or the the swapped keys
            /// </summary>
            /// <param name="edgeStart">the edge start</param>
            /// <param name="edgeEnd">the edge end</param>
            /// <param name="key1">the key for edgeStart</param>
            /// <param name="key2">the key for edgeEnd</param>
            /// <returns>true if edgeStart matches key1 and EdgeEnd matches key2</returns>
            private static bool CompareEdgeToKeysOrSwappedKeys(int edgeStart, int edgeEnd, int key1, int key2)
            {
                return CompareEdgeToKeys(edgeStart, edgeEnd, key1, key2) || CompareEdgeToKeys(edgeStart, edgeEnd, key2, key1);
            }

            /// <summary>
            /// Add a single value at the end or the start. Adjust the according key.
            /// </summary>
            /// <param name="value">the value to add</param>
            /// <param name="append">true for end, false for start</param>
            private void AddSingleVertex(int value, bool append)
            {
                if (append)
                {
                    this.vertexIds.Add(value);
                    this.EndKey = value;
                }
                else
                {
                    this.vertexIds.Insert(0, value);
                    this.StartKey = value;
                }
            }

            /// <summary>
            /// Append the other vertices after our data and adjust the end
            /// </summary>
            /// <param name="otherVertices">the other vertices</param>
            /// <param name="newEnd">our new effective end</param>
            /// <returns>the new end</returns>
            private int AppendRange(IEnumerable<int> otherVertices, int newEnd)
            {
                this.vertexIds.AddRange(otherVertices);
                this.EndKey = newEnd;
                return newEnd;
            }

            /// <summary>
            /// Insert the other vertices before our data and adjust the start
            /// </summary>
            /// <param name="otherVertices">the other vertices</param>
            /// <param name="newStart">our new effective start</param>
            /// <returns>the new start</returns>
            private int InsertRange(IEnumerable<int> otherVertices, int newStart)
            {
                this.vertexIds.InsertRange(0, otherVertices);
                this.StartKey = newStart;
                return newStart;
            }

            /// <summary>
            /// Join and close the polygon if this and other is the same instance
            /// </summary>
            /// <param name="other">the other polygon line</param>
            /// <returns>-1 if the polygon was joined, null else</returns>
            private int? JoinAndClose(PolygonLine other)
            {
                if (ReferenceEquals(this, other))
                {
                    this.Closed = true;
                    return -1;
                }

                return null;
            }

            /// <summary>
            /// The connecting edge fits one start and one end. Join with consistent direction.
            /// </summary>
            /// <param name="other">the other polygon line</param>
            /// <param name="edgeStart">the start of the joining edge</param>
            /// <param name="edgeEnd">the end of the joining edge</param>
            /// <returns>The start/end key that was changed or null if it doesn't fit</returns>
            private int? JoinSameDirection(PolygonLine other, int edgeStart, int edgeEnd)
            {
                if (CompareEdgeToKeys(edgeStart, edgeEnd, this.EndKey, other.StartKey))
                {
                    return this.AppendRange(other.vertexIds, other.EndKey);
                }

                if (CompareEdgeToKeys(edgeStart, edgeEnd, other.EndKey, this.StartKey))
                {
                    return this.InsertRange(other.vertexIds, other.StartKey);
                }

                return null;
            }

            /// <summary>
            /// this and other has the same direction, but the edge direction is reversed
            /// </summary>
            /// <param name="other">the other polygon line</param>
            /// <param name="edgeStart">the start of the joining edge</param>
            /// <param name="edgeEnd">the end of the joining edge</param>
            /// <returns>The start/end key that was changed or null if it doesn't fit</returns>
            private int? JoinWithEdgeInInverseDirection(PolygonLine other, int edgeStart, int edgeEnd)
            {
                if (CompareEdgeToKeys(edgeStart, edgeEnd, other.StartKey, this.EndKey))
                {
                    this.Dirty = true;
                    return this.AppendRange(other.vertexIds, other.EndKey);
                }

                if (CompareEdgeToKeys(edgeStart, edgeEnd, this.StartKey, other.EndKey))
                {
                    this.Dirty = true;
                    return this.InsertRange(other.vertexIds, other.StartKey);
                }

                return null;
            }

            /// <summary>
            /// new edge connects at both start or both end points, reverse the other segment and join
            /// </summary>
            /// <param name="other">the other polygon line</param>
            /// <param name="edgeStart">the start of the joining edge</param>
            /// <param name="edgeEnd">the end of the joining edge</param>
            /// <returns>The start/end key that was changed or null if it doesn't fit</returns>
            private int? JoinReversingOtherPolygon(PolygonLine other, int edgeStart, int edgeEnd)
            {
                var reversedOther = new List<int>(other.vertexIds);
                reversedOther.Reverse();

                if (CompareEdgeToKeysOrSwappedKeys(edgeStart, edgeEnd, this.StartKey, other.StartKey))
                {
                    this.Dirty = true;
                    return this.InsertRange(reversedOther, other.EndKey);
                }

                if (CompareEdgeToKeysOrSwappedKeys(edgeStart, edgeEnd, this.EndKey, other.EndKey))
                {
                    this.Dirty = true;
                    return this.AppendRange(reversedOther, other.StartKey);
                }

                return null;
            }
        }
    }
}
