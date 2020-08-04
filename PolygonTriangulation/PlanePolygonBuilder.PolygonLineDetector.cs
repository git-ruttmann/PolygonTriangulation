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
    /// Test interface for the polygon line detector (join edges to polygon lines)
    /// </summary>
    public interface IPolygonLineDetector
    {
        /// <summary>
        /// Gets the closed polygons
        /// </summary>
        IEnumerable<IReadOnlyCollection<int>> ClosedPolygons { get; }

        /// <summary>
        /// Gets the unclosed polygons
        /// </summary>
        IEnumerable<IReadOnlyCollection<int>> UnclosedPolygons { get; }

        /// <summary>
        /// Add multiple edges
        /// </summary>
        /// <param name="edges">pairs of vertex ids</param>
        void JoinEdgesToPolygones(IEnumerable<int> edges);

        /// <summary>
        /// Try to close unclosed polygons by connecting close vertices.
        /// </summary>
        /// <param name="vertices">the vertices</param>
        /// <param name="maxDistance">the maximum distance between vertices</param>
        void TryClusteringUnclosedEnds(Vertex[] vertices, float maxDistance);
    }

    /// <summary>
    /// subclass container
    /// </summary>
    public partial class PlanePolygonBuilder
    {
        /// <summary>
        /// Detect closed polygon lines
        /// </summary>
        private class PolygonLineDetector : IPolygonLineDetector
        {
            /// <summary>
            /// Mapping from start/end of polygon line to the line instance.
            /// </summary>
            private readonly Dictionary<int, PolygonLine> openPolygones;

            /// <summary>
            /// Closed polygon lines
            /// </summary>
            private readonly List<PolygonLine> closedPolygones;

            /// <summary>
            /// Unclosed polygon lines
            /// </summary>
            private readonly List<PolygonLine> unclosedPolygones;

            /// <summary>
            /// The fusion vertices
            /// </summary>
            private readonly ICollection<int> fusionVertices;

            /// <summary>
            /// Segments with fusion point that need delay
            /// </summary>
            private readonly List<(int, int)> fusionDelayedSegments;

            /// <summary>
            /// Initializes a new instance of the <see cref="PolygonLineDetector"/> class.
            /// </summary>
            /// <param name="fusionVertices">Vertices that are used by more than two edges</param>
            public PolygonLineDetector(ICollection<int> fusionVertices)
            {
                this.openPolygones = new Dictionary<int, PolygonLine>();
                this.closedPolygones = new List<PolygonLine>();
                this.unclosedPolygones = new List<PolygonLine>();
                this.fusionVertices = fusionVertices;
                this.fusionDelayedSegments = fusionVertices.Any() ? new List<(int, int)>() : null;
            }

            /// <summary>
            /// Gets the closed polygones.
            /// </summary>
            public IReadOnlyList<PolygonLine> Lines => this.closedPolygones;

            /// <inheritdoc/>
            public IEnumerable<IReadOnlyCollection<int>> ClosedPolygons => this.closedPolygones.Select(x => x.ToIndexes());

            /// <inheritdoc/>
            public IEnumerable<IReadOnlyCollection<int>> UnclosedPolygons => this.unclosedPolygones.Select(x => x.ToIndexes());

            /// <summary>
            /// find continous combination of edges
            /// </summary>
            /// <param name="edges">triangle data, the relevant edges are from vertex0 to vertex1. vertex2 is ignored</param>
            public void JoinEdgesToPolygones(IEnumerable<int> edges)
            {
                var iterator = edges.GetEnumerator();
                while (iterator.MoveNext())
                {
                    var start = iterator.Current;
                    iterator.MoveNext();
                    var end = iterator.Current;

                    if (start == end)
                    {
                        continue;
                    }

                    this.AddEdge(start, end);
                }

                if (this.fusionDelayedSegments?.Count > 0)
                {
                    foreach (var (start, end) in this.fusionDelayedSegments)
                    {
                        this.AddEdge(start, end);
                    }
                }

                this.unclosedPolygones.AddRange(this.openPolygones
                    .Where(x => x.Key == x.Value.StartKey)
                    .Select(x => x.Value));
            }

            /// <inheritdoc/>
            public void TryClusteringUnclosedEnds(Vertex[] vertices, float maxDistance)
            {
                bool vertexFound;
                do
                {
                    vertexFound = false;
                    foreach (var vertexId in this.openPolygones.Keys)
                    {
                        var closestPeer = this.openPolygones.Keys
                            .Where(x => x != vertexId)
                            .OrderBy(x => Distance(vertices, vertexId, x))
                            .First();
                        if (Distance(vertices, vertexId, closestPeer) < maxDistance)
                        {
                            this.JoinClusteredVertex(vertexId, closestPeer);
                            vertexFound = true;
                            break;
                        }
                    }
                }
                while (vertexFound);

                this.unclosedPolygones.Clear();
                this.unclosedPolygones.AddRange(this.openPolygones
                    .Where(x => x.Key == x.Value.StartKey)
                    .Select(x => x.Value));
            }

            /// <summary>
            /// Calculate the distance between two points
            /// </summary>
            /// <param name="vertices">the vertices</param>
            /// <param name="vertexId">the first point</param>
            /// <param name="peer">the second point</param>
            /// <returns>the sum of the x and y distance</returns>
            private static float Distance(Vertex[] vertices, int vertexId, int peer)
            {
#if UNITY_EDITOR || UNITY_STANDALONE
                return Math.Abs(vertices[vertexId].x - vertices[peer].x) + Math.Abs(vertices[vertexId].y - vertices[peer].y);
#else
                return Math.Abs(vertices[vertexId].X - vertices[peer].X) + Math.Abs(vertices[vertexId].Y - vertices[peer].Y);
#endif
            }

            /// <summary>
            /// Join two vertices as they are close together.
            /// </summary>
            /// <param name="vertexId">the vertex id to drop</param>
            /// <param name="closestPeer">the closest existing peer, that will act as replacement</param>
            private void JoinClusteredVertex(int vertexId, int closestPeer)
            {
                var vertexSegment = this.openPolygones[vertexId];
                this.openPolygones.Remove(vertexId);
                var peerIsLineStart = vertexSegment.RemoveVertex(vertexId);

                var peerSegment = this.openPolygones[closestPeer];
                this.openPolygones.Remove(closestPeer);

                var vertexReplacement = peerIsLineStart ? vertexSegment.StartKey : vertexSegment.EndKey;
                var joinedKey = peerSegment.Join(vertexSegment, vertexReplacement, closestPeer);
                if (joinedKey < 0)
                {
                    this.closedPolygones.Add(vertexSegment);
                }
                else
                {
                    this.openPolygones.Remove(peerIsLineStart ? vertexSegment.EndKey : vertexSegment.StartKey);
                    this.openPolygones.Add(joinedKey, peerSegment);
                }
            }

            /// <summary>
            /// Add a new edge to the polygon line. Either join two polygon lines, creates a new or adds the edge to the neighboring line
            /// </summary>
            /// <param name="start">the vertex id of the edge start</param>
            /// <param name="end">the vertex id of the edge end</param>
            private void AddEdge(int start, int end)
            {
                var startFits = this.openPolygones.TryGetValue(start, out var firstSegment);
                if (startFits)
                {
                    this.openPolygones.Remove(start);
                }

                var endFits = this.openPolygones.TryGetValue(end, out var lastSegment);
                if (endFits)
                {
                    this.openPolygones.Remove(end);
                }

                if (!startFits && !endFits)
                {
                    var segment = new PolygonLine(start, end);
                    this.openPolygones.Add(start, segment);
                    this.openPolygones.Add(end, segment);
                }
                else if (startFits && endFits)
                {
                    var remainingKeyOfOther = firstSegment.Join(lastSegment, start, end);
                    if (remainingKeyOfOther < 0)
                    {
                        this.closedPolygones.Add(firstSegment);
                    }
                    else
                    {
                        this.openPolygones[remainingKeyOfOther] = firstSegment;
                    }
                }
                else if (startFits)
                {
                    if ((start == firstSegment.EndKey) || !this.fusionVertices.Contains(start))
                    {
                        firstSegment.AddMatchingStart(start, end);
                        this.openPolygones[end] = firstSegment;
                    }
                    else
                    {
                        this.fusionDelayedSegments.Add((start, end));
                        this.openPolygones[start] = firstSegment;
                    }
                }
                else
                {
                    if ((end == lastSegment.StartKey) || !this.fusionVertices.Contains(end))
                    {
                        lastSegment.AddMatchingEnd(start, end);
                        this.openPolygones[start] = lastSegment;
                    }
                    else
                    {
                        this.fusionDelayedSegments.Add((start, end));
                        this.openPolygones[end] = lastSegment;
                    }
                }
            }
        }
    }
}