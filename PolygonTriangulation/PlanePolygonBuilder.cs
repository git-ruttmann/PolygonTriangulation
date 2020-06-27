namespace PolygonTriangulation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;

    using Vertex = System.Numerics.Vector2;
    using Vector3 = System.Numerics.Vector3;
    using Quaternion = System.Numerics.Quaternion;
    using Plane = System.Numerics.Plane;

    /// <summary>
    /// Test interface for the polygon builder
    /// </summary>
    internal interface IEdgesToPolygonBuilder
    {
        /// <summary>
        /// Add an edge between the two points
        /// </summary>
        /// <param name="p0">start point</param>
        /// <param name="p1">end point</param>
        void AddEdge(Vector3 p0, Vector3 p1);

        /// <summary>
        /// build the resulting polygon
        /// </summary>
        /// <returns></returns>
        IPlanePolygon BuildPolygon();
    }

    /// <summary>
    /// Test interface for the polygon line detector (join edges to polygon lines)
    /// </summary>
    public interface IPolygonLineDetector
    {
        /// <summary>
        /// Get the closed polygons
        /// </summary>
        IEnumerable<IReadOnlyCollection<int>> ClosedPolygons { get; }

        /// <summary>
        /// Get the unclosed polygons
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
    /// The polygon result after combining all edges
    /// </summary>
    public interface IPlanePolygon
    {
        /// <summary>
        /// Gets the 3D vertices.
        /// </summary>
        Vector3[] Vertices { get; }

        /// <summary>
        /// Gets the polygon. It contains the 2D vertices.
        /// </summary>
        Polygon Polygon { get; }
    }

    /// <summary>
    /// Resulting plane mesh with coordinates and triangles
    /// </summary>
    public interface ITriangulatedPlanePolygon
    {
        /// <summary>
        /// The 3D vertices
        /// </summary>
        Vector3[] Vertices { get; }

        /// <summary>
        /// The triangles with vertex offset
        /// </summary>
        int[] Triangles { get; }
    }

    /// <summary>
    /// Collect the edges for a plane polygon
    /// </summary>
    public interface IPlanePolygonEdgeCollector
    {
        /// <summary>
        /// Add an edge
        /// </summary>
        /// <param name="p0">start point</param>
        /// <param name="p1">end point</param>
        void AddEdge(Vector3 p0, Vector3 p1);

        /// <summary>
        /// Dump the collected edges
        /// </summary>
        /// <returns>dump the collected edges for debug</returns>
        string Dump();
    }

    /// <summary>
    /// Build a list of triangles from polygon edges
    /// </summary>
    public class PlanePolygonBuilder : IPlanePolygonEdgeCollector
    {
        const float epsilon = 1.1E-5f;

        private readonly EdgesToPolygonBuilder edgesToPolygon;

        public PlanePolygonBuilder(Plane plane)
        {
            var rotation = Quaternion
                .Identity;
            // .FromToRotation(plane.normal, new Vector3(0, 0, -1));
            this.edgesToPolygon = new EdgesToPolygonBuilder(rotation);
        }

        /// <summary>
        /// Create a edges to polygon builder for unit testing
        /// </summary>
        /// <returns></returns>
        internal static IEdgesToPolygonBuilder CreatePolygonBuilder() => new EdgesToPolygonBuilder(Quaternion.Identity);

        /// <summary>
        /// Create a polygon from edges detector. The edge is defined by the vertex ids
        /// </summary>
        /// <returns>the detector</returns>
        internal static IPolygonLineDetector CreatePolygonLineDetector(params int[] fusionVertices) => new PolygonLineDetector(fusionVertices);

        /// <inheritdoc/>
        public void AddEdge(Vector3 p0, Vector3 p1)
        {
            this.edgesToPolygon.AddEdge(p0, p1);
        }

        /// <inheritdoc/>
        public string Dump()
        {
            return this.edgesToPolygon.Dump();
        }

        /// <summary>
        /// Build the plane triangles
        /// </summary>
        public ITriangulatedPlanePolygon Build()
        {
            IPlanePolygon polygonResult = null;
            try
            {
                polygonResult = this.edgesToPolygon.BuildPolygon();
                var triangulator = new PolygonTriangulator(polygonResult.Polygon);
                var triangles = triangulator.BuildTriangles();
                return new TriangulatedPlanePolygon(polygonResult.Vertices, triangles);
            }
            catch (Exception e)
            {
                throw new TriangulationException(polygonResult?.Polygon, this.edgesToPolygon.Dump(), e);
            }
        }

        /// <summary>
        /// Result for the plane mesh
        /// </summary>
        private class TriangulatedPlanePolygon : ITriangulatedPlanePolygon
        {
            public TriangulatedPlanePolygon(Vector3[] vertices, int[] triangles)
            {
                this.Vertices = vertices;
                this.Triangles = triangles;
            }

            /// <inheritdoc/>
            public Vector3[] Vertices { get; }

            /// <inheritdoc/>
            public int[] Triangles { get; }
        }

        /// <summary>
        /// Result storage for polygon data
        /// </summary>
        private class PlanePolygonData : IPlanePolygon
        {
            public PlanePolygonData(Vector3[] vertices3D, Polygon polygon)
            {
                this.Vertices = vertices3D;
                this.Polygon = polygon;
            }

            /// <inheritdoc/>
            public Vector3[] Vertices { get; }

            /// <inheritdoc/>
            public Polygon Polygon { get; }
        }


        /// <summary>
        /// Compare two vertices, very close vertices are considered equal.
        /// </summary>
        private class ClusterVertexComparer : IComparer<Vertex>
        {
            /// <inheritdoc/>
            public int Compare(Vertex x, Vertex y)
            {
                var xdist = Math.Abs(x.X - y.X);
                if (xdist < epsilon)
                {
                    var ydist = Math.Abs(x.Y - y.Y);
                    if (ydist < epsilon)
                    {
                        return 0;
                    }

                    var xCompare = x.X.CompareTo(y.X);
                    if (xCompare != 0)
                    {
                        return xCompare;
                    }

                    if (x.Y < y.Y)
                    {
                        return -1;
                    }
                    else
                    {
                        return 1;
                    }
                }
                else if (x.X < y.X)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            }
        }

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
            /// vertices that are part of more than 2 polygon edges
            /// </summary>
            private readonly IReadOnlyList<int> fusionVertices;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="fusionVertices">Vertices that are used by more than two edges</param>
            public PolygonLineDetector(IReadOnlyList<int> fusionVertices)
            {
                this.fusionVertices = fusionVertices;
                this.openPolygones = new Dictionary<int, PolygonLine>();
                this.closedPolygones = new List<PolygonLine>();
                this.unclosedPolygones = new List<PolygonLine>();
            }

            /// <summary>
            /// Get the closed polygones.
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
            /// <returns>the list of closed polygones and the unclosed polygones </returns>
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
                            JoinClusteredVertex(vertexId, closestPeer);
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
            /// Calculate the distance between two points
            /// </summary>
            /// <param name="vertices">the vertices</param>
            /// <param name="vertexId">the first point</param>
            /// <param name="peer">the second point</param>
            /// <returns>the sum of the x and y distance</returns>
            private static float Distance(Vertex[] vertices, int vertexId, int peer)
            {
                return Math.Abs(vertices[vertexId].X - vertices[peer].X) + Math.Abs(vertices[vertexId].Y - vertices[peer].Y);
            }

            /// <summary>
            /// Add a new edge to the polygon line. Either join two polygon lines, creates a new or adds the edge to the neighboring line
            /// </summary>
            /// <param name="start"></param>
            /// <param name="end"></param>
            private void AddEdge(int start, int end)
            {
                bool startFits;
                if (startFits = openPolygones.TryGetValue(start, out var firstSegment))
                {
                    openPolygones.Remove(start);
                }

                bool endFits;
                if (endFits = openPolygones.TryGetValue(end, out var lastSegment))
                {
                    openPolygones.Remove(end);
                }

                if (!startFits && !endFits)
                {
                    var segment = new PolygonLine(start, end);
                    openPolygones.Add(start, segment);
                    openPolygones.Add(end, segment);
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
                        openPolygones[remainingKeyOfOther] = firstSegment;
                    }
                }
                else if (startFits)
                {
                    firstSegment.AddMatchingStart(start, end);
                    openPolygones[end] = firstSegment;
                }
                else
                {
                    lastSegment.AddMatchingEnd(start, end);
                    openPolygones[start] = lastSegment;
                }
            }
        }

        /// <summary>
        /// A polygon, defined by connected edges.
        /// </summary>
        [DebuggerDisplay("{Debug}")]
        private class PolygonLine
        {
            private readonly List<int> vertexIds;

            /// <summary>
            /// Create a new polygon consisting of 2 vertices
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
            /// Gets a flag indicating whether the edge direction was inconsistend at any time
            /// </summary>
            public bool Dirty { get; private set; }

            /// <summary>
            /// Gets a flag indicating whether the polygon is closed
            /// </summary>
            public bool Closed { get; private set; }

            /// <summary>
            /// Gets the vertex ids in order
            /// </summary>
            /// <returns>the vertex ids</returns>
            public IReadOnlyCollection<int> ToIndexes()
            {
                return this.vertexIds;
            }

            public string Debug => $"{(this.Closed ? "*" : string.Empty)}, {(this.Dirty ? "#" : string.Empty)}, {string.Join(" ", this.vertexIds)}";

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
            /// <param name="edgeStart">the end of the edge that joines</param>
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
            /// Compare the edge points to two corresponding keys
            /// </summary>
            /// <param name="edgeStart">the edge start</param>
            /// <param name="edgeEnd">the edge end</param>
            /// <param name="key1">the key for edgeStart</param>
            /// <param name="key2">the key for edgeEnd</param>
            /// <returns>true if edgeStart matches key1 and EdgeEnd matches key2</returns>
            private static bool CompareEdgeToKeys(int edgeStart, int edgeEnd, int key1, int key2)
            {
                return (edgeStart == key1) && (edgeEnd == key2);
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

        /// <summary>
        /// Build a polygon from 3D edges, lying on a common plane.
        /// </summary>
        private class EdgesToPolygonBuilder : IEdgesToPolygonBuilder
        {
            /// <summary>
            /// current edges in pairs
            /// </summary>
            private readonly List<int> edges;

            /// <summary>
            /// original vertices
            /// </summary>
            private readonly List<Vector3> vertices3D;

            /// <summary>
            /// rotated vertices
            /// </summary>
            private readonly List<Vertex> vertices2D;

            /// <summary>
            /// 3D to 2D rotation
            /// </summary>
            private readonly Quaternion rotation;

            /// <summary>
            /// Internal constructor without rotation
            /// </summary>
            internal EdgesToPolygonBuilder()
                : this(Quaternion.Identity)
            {

            }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="rotation">the rotation to map a vertex on a 2D plane</param>
            public EdgesToPolygonBuilder(Quaternion rotation)
            {
                this.edges = new List<int>();
                this.vertices3D = new List<Vector3>();
                this.vertices2D = new List<Vertex>();

                this.rotation = rotation;
            }

            /// <summary>
            /// Get a clustering vertex comparer
            /// </summary>
            public static IComparer<Vertex> VertexComparer => new ClusterVertexComparer();

            /// <summary>
            /// Dump the edges during debugging
            /// </summary>
            public string Dump()
            {
#if DEBUG
                var sb = new StringBuilder();
                
                sb.AppendLine("var builder = PlanePolygonBuilder.CreatePolygonBuilder();");
                for (int i = 0; i < this.vertices2D.Count - 1; i += 2)
                {
                    sb.AppendLine($"builder.AddEdge(new Vector3({this.vertices2D[i].X:0.00000000}f, {this.vertices2D[i].Y:0.00000000}f, 0), new Vector3({this.vertices2D[i + 1].X:0.00000000}f, {this.vertices2D[i + 1].Y:0.00000000}f, 0));");
                }

                sb.AppendLine("builder.BuildPolygon();");

                return sb.ToString();
#else
                return string.Empty;
#endif
            }

            /// <summary>
            /// Add an edge
            /// </summary>
            /// <param name="p0">start point</param>
            /// <param name="p1">end point</param>
            public void AddEdge(Vector3 p0, Vector3 p1)
            {
                var planeTriangleOffset = this.vertices2D.Count;
                this.vertices3D.Add(p0);
                var p0Rotated = Vector3.Transform(p0, this.rotation);
                this.vertices2D.Add(new Vertex(p0Rotated.X, p0Rotated.Y));

                this.vertices3D.Add(p1);
                var p1Rotated = Vector3.Transform(p1, this.rotation);
                this.vertices2D.Add(new Vertex(p1Rotated.X, p1Rotated.Y));

                this.edges.Add(planeTriangleOffset + 0);
                this.edges.Add(planeTriangleOffset + 1);
            }

            /// <summary>
            /// Compress the vertices and connect all edges to polygons
            /// </summary>
            /// <returns>the polygon and the 3D vertices</returns>
            public IPlanePolygon BuildPolygon()
            {
                var sorted2D = this.vertices2D.ToArray();
                var sortedIndizes = Enumerable.Range(0, sorted2D.Length).ToArray();
                var comparer = new ClusterVertexComparer();
                List<int> fusionedVertices = null;
                Array.Sort(sorted2D, sortedIndizes, comparer);

                var translation = new int[sorted2D.Length];
                var writeIndex = 0;
                var sameVertexCount = 0;
                for (int i = 1; i < sorted2D.Length; i++)
                {
                    if (comparer.Compare(sorted2D[writeIndex], sorted2D[i]) != 0)
                    {
                        sorted2D[++writeIndex] = sorted2D[i];
                        sameVertexCount = 0;
                    }
                    else
                    {
                        if (++sameVertexCount == 2)
                        {
                            fusionedVertices = fusionedVertices ?? new List<int>();
                            fusionedVertices.Add(writeIndex);
                        }
                    }

                    translation[sortedIndizes[i]] = writeIndex;
                }

                var count = writeIndex + 1;
                Array.Resize(ref sorted2D, count);

                var compressed3D = new Vector3[sorted2D.Length];
                for (int i = 0; i < translation.Length; i++)
                {
                    compressed3D[translation[i]] = this.vertices3D[i];
                }

                var lineDetector = new PolygonLineDetector(fusionedVertices);
                lineDetector.JoinEdgesToPolygones(this.edges.Select(x => translation[x]));

                if (lineDetector.UnclosedPolygons.Count() > 0)
                {
                    lineDetector.TryClusteringUnclosedEnds(sorted2D, epsilon * 100);
                }

                var polygon = Polygon.FromPolygonLines(sorted2D, lineDetector.Lines.Select(x => x.ToIndexes()).ToArray(), fusionedVertices);
                return new PlanePolygonData(compressed3D, polygon);
            }
        }
    }
}
