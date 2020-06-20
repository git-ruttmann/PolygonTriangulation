namespace PolygonTriangulation
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;

    using Vertex = System.Numerics.Vector2;

    /// <summary>
    /// The action necessary for the vertex transition.
    /// Ordering is important, because for the same vertex id, we need to process closing before transition before opening
    /// </summary>
    public enum VertexAction
    {
        /// <summary>
        /// Prev and next are left of the vertex. => This is a closing cusp.
        /// </summary>
        ClosingCusp,

        /// <summary>
        /// Transition from one vertex to the net. No cusp.
        /// </summary>
        Transition,

        /// <summary>
        /// Prev and next are right of the vertex. => This is an opening cusp.
        /// </summary>
        OpeningCusp,
    }

    /// <summary>
    /// Information about an element in the vertex chain of a polygon.
    /// </summary>
    public interface IPolygonVertexInfo
    {
        /// <summary>
        /// Gets the action necessary to process the triple
        /// </summary>
        VertexAction Action { get; }

        /// <summary>
        /// Gets the id of the current vertex
        /// </summary>
        int Id { get; }

        /// <summary>
        /// Gets the id of the next vertex
        /// </summary>
        int Next { get; }

        /// <summary>
        /// Gets the id of the previous vertex
        /// </summary>
        int Prev { get; }

        /// <summary>
        /// Gets a unique identifier for overlaying vertexes
        /// </summary>
        int Unique { get; }

        /// <summary>
        /// Gets the <see cref="Unique"/> for the next vertex
        /// </summary>
        int NextUnique { get; }

        /// <summary>
        /// Gets the <see cref="Unique"/> for the prev vertex
        /// </summary>
        int PrevUnique { get; }
    }


    /// <summary>
    /// Build a polygon
    /// </summary>
    public interface IPolygonBuilder
    {
        /// <summary>
        /// Add a single vertex id
        /// </summary>
        /// <param name="vertexId">the index in the vertices array of the builder</param>
        /// <returns>the same builder instance for call chains</returns>
        IPolygonBuilder Add(int vertexId);

        /// <summary>
        /// Add multiple vertex ids
        /// </summary>
        /// <param name="vertices">the indices in the vertices array of the builder</param>
        /// <returns>the same builder instance for call chains</returns>
        IPolygonBuilder AddVertices(IEnumerable<int> vertices);

        /// <summary>
        /// Close the current polygon. Next vertices are considered a new polygon line i.e a hole or a non-intersecting polygon
        /// </summary>
        /// <returns>the same builder instance for call chains</returns>
        IPolygonBuilder ClosePartialPolygon();

        /// <summary>
        /// Close the polygon. Do not use the builder after closing it.
        /// </summary>
        /// <param name="fusionVertices">vertices that are used in more than one subpolygon</param>
        /// <returns>a polygon</returns>
        Polygon Close(params int[] fusionVertices);

        /// <summary>
        /// Create one polygon that includes all vertices in the builder.
        /// </summary>
        /// <returns>a polygon</returns>
        Polygon Auto();
    }

    /// <summary>
    /// Extension methods for polygon
    /// </summary>
    public static class PolygonExtensions
    {
        public static IPolygonBuilder AddVertices(this IPolygonBuilder builder, params int[] vertices)
        {
            return builder.AddVertices((IEnumerable<int>)vertices);
        }
    }

    /// <summary>
    /// Represent a collection of closed polygons.
    /// </summary>
    [DebuggerDisplay("{Debug}")]
    public class Polygon
    {

        /// <summary>
        /// Translate the vertex id to the index in the chain. For collisions look at <see cref="VertexChain.SameVertexChain"/>
        /// </summary>
        private readonly int[] vertexToChain;

        /// <summary>
        /// The current chain of vertices in the polygon. May contain multiple chains.
        /// </summary>
        private readonly VertexChain[] chain;

        /// <summary>
        /// Gets the vertex coordinates
        /// </summary>
        public readonly Vertex[] vertexCoordinates;

        /// <summary>
        /// the start index in <see cref="chain"/> per sub polygon
        /// </summary>
        private readonly List<int> polygonStartIndices;

        /// <summary>
        /// Initializes a new <see cref="Polygon"/>
        /// </summary>
        /// <param name="vertexCoordinates">the vertex coordinates</param>
        /// <param name="chain">the next vertex chain</param>
        /// <param name="vertexToChain">the translation from vertex id to chain index.</param>
        /// <param name="polygonStartIndices">the start of the subpolygones</param>
        private Polygon(Vertex[] vertexCoordinates, VertexChain[] chain, int[] vertexToChain, IEnumerable<int> polygonStartIndices)
        {
            this.chain = chain;
            this.vertexToChain = vertexToChain;
            this.vertexCoordinates = vertexCoordinates;
            this.polygonStartIndices = polygonStartIndices.ToList();
        }

        public string Debug => string.Join(" || ", this.SubPolygonIds.Select(x => string.Join(" ", this.SubPolygonVertices(x))));

        /// <summary>
        /// Gets the vertex ids of a subpolygon
        /// </summary>
        /// <param name="subPolygonId">the sub polygon id</param>
        /// <returns>vertex Ids</returns>
        public IEnumerable<int> SubPolygonVertices(int subPolygonId)
        {
            return new NextChainEnumerator(this.polygonStartIndices[subPolygonId], this.chain)
                .Select(x => this.chain[x].VertexId);
        }

        /// <summary>
        /// Gets the vertex coordinates
        /// </summary>
        public IReadOnlyList<Vertex> Vertices => Array.AsReadOnly(this.vertexCoordinates);

        /// <summary>
        /// Gets the ids of all available sub polygons. Use <see cref="SubPolygonVertices"/> to enemerate the other polygons
        /// </summary>
        public IEnumerable<int> SubPolygonIds => Enumerable.Range(0, this.polygonStartIndices.Count).Where(x => this.polygonStartIndices[x] >= 0);

        /// <summary>
        /// Get id/prev/next info per vertex sorted by vertex id.
        /// </summary>
        public IEnumerable<IPolygonVertexInfo> OrderedVertexes
        {
            get
            {
                var workOrder = Enumerable.Range(0, this.chain.Length).OrderBy(x => this.chain[x].VertexId);
                return workOrder.Select(x => new VertexInfo(x, this.chain));
            }
        }

        /// <summary>
        /// Create a polygon builder
        /// </summary>
        /// <param name="vertices">the vertices of the polygon</param>
        /// <returns>A builder to define vertex order and sub-polygons</returns>
        public static IPolygonBuilder Build(Vertex[] vertices)
        {
            return new PolygonBuilder(vertices);
        }

        /// <summary>
        /// create a polygon from multiple polygon lines
        /// </summary>
        /// <param name="vertexCoordinates">the vertex coordinates</param>
        /// <param name="lines">The polygon lines. Each vertex Id must be unique inside a polygon line.</param>
        /// <param name="fusionVertices">vertices that are used in more than one subpolygon</param>
        /// <returns>a polygon</returns>
        public static Polygon FromPolygonLines(Vertex[] vertexCoordinates, IReadOnlyCollection<int>[] lines, IReadOnlyList<int> fusionVertices)
        {
            var chain = new VertexChain[lines.Sum(x => x.Count)];
            var id = 0;
            var first = id;
            var vertexToChain = Enumerable.Repeat(-1, vertexCoordinates.Length).ToArray();
            var subPolygones = new List<int>();

            foreach (var line in lines)
            {
                var polygonId = subPolygones.Count;
                subPolygones.Add(id);

                foreach (var vertexId in line)
                {
                    chain[id].VertexId = vertexId;
                    chain[id].PolygonId = polygonId;

                    chain[id].SameVertexChain = vertexToChain[vertexId];
                    vertexToChain[vertexId] = id;

                    SetNext(chain, id, id == chain.Length - 1 ? first : id + 1);
                    id++;
                }

                SetNext(chain, id - 1, first);
                first = id;
            }

            var polygon = new Polygon(vertexCoordinates, chain, vertexToChain, subPolygones);
            polygon.FusionVerticesIntoChain(fusionVertices);
            return polygon;
        }

        /// <summary>
        /// Calculates an angle that grows counter clockwise from 0 to 4
        /// </summary>
        /// <param name="dx">delta in x direction</param>
        /// <param name="dy">delta in y direction</param>
        /// <returns>a float representing the angle</returns>
        private static float DiamondAngle(float dx, float dy)
        {
            if (dy >= 0)
            {
                return (dx >= 0 ? dy / (dx + dy) : 1 - dx / (-dx + dy));
            }
            else
            {
                return (dx < 0 ? 2 - dy / (-dx - dy) : 3 + dx / (dx - dy));
            }
        }

        /// <summary>
        /// Iterate all fusion points and join the chain
        /// </summary>
        private void FusionVerticesIntoChain(IEnumerable<int> fusionVertices)
        {
            if (fusionVertices == null)
            {
                return;
            }

            foreach (var fusionVertexId in fusionVertices)
            {
                var jobList = CreateVertexFusionJobs(fusionVertexId);

                var first = true;
                var newSubPolygons = new List<int>();
                foreach (var (prev, next, samePolygon) in jobList)
                {
                    if (this.chain[prev].Next == next)
                    {
                        continue;
                    }

                    if (first)
                    {
                        first = false;
                    }
                    else if (!samePolygon)
                    {
                        this.polygonStartIndices[chain[prev].PolygonId] = -1;
                        PolygonSplitter.FillPolygonId(this.chain, prev, chain[next].PolygonId);
                    }
                    else
                    {
                        newSubPolygons.Add(prev);
                    }

                    SetNext(this.chain, prev, next);
                }

                // During the split, the chain may contain loops, so FillPolygonId might hang. Update the polygon id in the end.
                foreach (var polygonStart in newSubPolygons)
                {
                    PolygonSplitter.FillPolygonId(this.chain, polygonStart, this.polygonStartIndices.Count);
                    this.polygonStartIndices.Add(polygonStart);
                }
            }
        }

        /// <summary>
        /// Creates a list of jobs to reorder all edges of a vertex.
        /// </summary>
        /// <param name="fusionVertexId">the central vertex id</param>
        /// <returns>tuples of prev/next and polygon split</returns>
        /// <remarks>
        /// After all jobs are completed, the polygon will leave the vertex with the next counter-clock-wise edge, that has reached the vertex.
        /// Hence there are no implizit crossings.
        /// As the eecution of the job manipulates the chain, it's required to collect all data before.
        /// </remarks>
        private (int prev, int next, bool samePolygon)[] CreateVertexFusionJobs(int fusionVertexId)
        {
            var vertex = this.vertexCoordinates[fusionVertexId];
            var vertexInstances = new List<(int chain, bool outgoing)>(8);
            for (int chain = vertexToChain[fusionVertexId]; chain >= 0; chain = this.chain[chain].SameVertexChain)
            {
                vertexInstances.Add((chain, true));
                vertexInstances.Add((chain, false));
            }

            var sortedByAngle = vertexInstances
                .OrderBy(x =>
                {
                    var peerId = x.outgoing ? this.chain[x.chain].Next : this.chain[x.chain].Prev;
                    ref var peer = ref this.vertexCoordinates[this.chain[peerId].VertexId];
                    return 4.0f - DiamondAngle(peer.X - vertex.X, peer.Y - vertex.Y);
                })
                .ToArray();

            var jobList = new (int prev, int next, bool samePolygon)[sortedByAngle.Length / 2];
            var start = sortedByAngle[0].outgoing ? 0 : 1;
            for (int i = 0; i < jobList.Length; i++)
            {
                var outgoing = i * 2 + start;
                var incoming = outgoing + 1 == sortedByAngle.Length ? 0 : outgoing + 1;
                var prev = this.chain[sortedByAngle[incoming].chain].Prev;
                var startOfEdge = sortedByAngle[outgoing].chain;
                jobList[i] = (prev, startOfEdge, this.chain[prev].PolygonId == this.chain[startOfEdge].PolygonId);
            }

            return jobList;
        }

        /// <summary>
        /// Creat a polygon with vertex id's and next chain. Can contain holes.
        /// </summary>
        /// <param name="vertexCoordinates">the coordinates</param>
        /// <param name="vertexIds">the vertex ids</param>
        /// <param name="nextIndices">the next index in vertexIds. Must be same length as vertexIds</param>
        /// <param name="fusionVertices">Vertices that are used in more than one subpolygon. Can be null.</param>
        /// <returns>a polygon</returns>
        public static Polygon FromVertexList(Vertex[] vertexCoordinates, IEnumerable<int> vertexIds, IEnumerable<int> nextIndices, IEnumerable<int> polygonIds, IReadOnlyList<int> fusionVertices)
        {
            var vertexIdCollection = vertexIds as IReadOnlyCollection<int> ?? vertexIds.ToArray();
            var polygonIdCollection = polygonIds as IList<int> ?? polygonIds.ToArray();
            var vertexToChain = Enumerable.Repeat(-1, vertexCoordinates.Length).ToArray();
            var chain = new VertexChain[vertexIdCollection.Count];
            var polygonStartIndex = new List<int>();

            var i = 0;
            foreach (var (vertexId, nextId) in vertexIdCollection.Zip(nextIndices, Tuple.Create))
            {
                chain[i].VertexId = vertexId;
                chain[i].SameVertexChain = vertexToChain[vertexId];
                chain[i].PolygonId = polygonIdCollection[i];
                SetNext(chain, i, nextId);

                if (polygonIdCollection[i] >= polygonStartIndex.Count)
                {
                    polygonStartIndex.Add(i);
                }

                vertexToChain[vertexId] = i;
                i++;
            }

            var polygon = new Polygon(vertexCoordinates, chain, vertexToChain, polygonStartIndex);
            polygon.FusionVerticesIntoChain(fusionVertices);
            return polygon;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetNext(VertexChain[] chain, int current, int next)
        {
            chain[current].SetNext(current, next, ref chain[next]);
        }

        /// <summary>
        /// Split the polygon along tuples of two vertex indices.
        /// </summary>
        /// <param name="polygon">the polygon to split. It's modified.</param>
        /// <param name="splits">the splits as tuples of vertex ids</param>
        /// <param name="triangleCollector">the collector for simple triangles</param>
        /// <returns>(simple triangles, other polygones)</returns>
        public static Polygon Split(Polygon polygon, IEnumerable<Tuple<int, int>> splits, ITriangleCollector triangleCollector)
        {
            var splitter = new PolygonSplitter(polygon, splits, triangleCollector);
            return splitter.Execute();
        }

        /// <summary>
        /// Gets an enumerator that starts at startVertex and loops the whole polygon once.
        /// </summary>
        /// <param name="startVertex">The first vertex id</param>
        /// <param name="subPolygonId">the id of the subpolygon to traverse</param>
        /// <returns>An Enumerable/Enumerator</returns>
        public IEnumerable<int> IndicesStartingAt(int startVertex, int subPolygonId)
        {
            var startId = this.vertexToChain[startVertex];
            while (this.chain[startId].PolygonId != subPolygonId)
            {
                startId = this.chain[startId].SameVertexChain;
                if (startId < 0)
                {
                    throw new InvalidOperationException($"Vertex {startVertex} is not part of polygon {subPolygonId}");
                }
            }

            return new NextChainEnumerator(startId, this.chain).Select(x => this.chain[x].VertexId);
        }

        /// <summary>
        /// - each chain element belongs to exactly one polygon.
        /// - multiple polygons are stored in the chain. (avoids copy during split)
        /// - if a vertex belongs to multple polygons, it has multiple chain elements with the same VertexId
        ///   the start of that chain is in the <see cref="SharedData.VertexToChain"/>, the collision list is in SameVertexChain
        ///   the combination of PolygonId/VertexId is distinct.
        ///   during polygon triangulation, the maximum collision count is 3
        /// - a polygon has a specific chain element as start index
        /// - a polygon with holes has multiple chain start elements. They are joined via <see cref="PolygonSplitter.JoinHoleIntoPolygon(int, int)"/>
        /// </summary>
        private struct VertexChain
        {
            /// <summary>
            /// the index in <see cref="SharedData.VertexCoordinates"/>
            /// </summary>
            public int VertexId;

            /// <summary>
            /// The id of the polygon. Holes are a separate polygon.
            /// </summary>
            public int PolygonId;

            /// <summary>
            /// The previous vertex id (not chain index)
            /// </summary>
            public int Prev { get; private set; }

            /// <summary>
            /// The next chain index in the polygon (same polygon id)
            /// </summary>
            public int Next { get; private set; }

            /// <summary>
            /// Chain two items
            /// </summary>
            /// <param name="currentChain">the id of the current item</param>
            /// <param name="nextChain">the id of the next item</param>
            /// <param name="nextItem">the data of the next item</param>
            public void SetNext(int current, int nextChain, ref VertexChain nextItem)
            {
                this.Next = nextChain;
                nextItem.Prev = current;
            }

            /// <summary>
            /// The next info with the same vertex id.
            /// </summary>
            public int SameVertexChain;
        }

        /// <summary>
        /// Internal enumerator
        /// </summary>
        private class NextChainEnumerator : IEnumerable<int>, IEnumerator<int>
        {
            private readonly int start;
            private readonly IList<VertexChain> chain;
            private bool reset;
#if DEBUG
            private int maxIteratorCount;
#endif

            public NextChainEnumerator(int start, IList<VertexChain> chain)
            {
                this.start = start;
                this.chain = chain;
                this.reset = true;
#if DEBUG
                this.maxIteratorCount = chain.Count();
#endif
        }

        /// <summary>
        /// The current chain index
        /// </summary>
        public int Current { get; private set; }

            object IEnumerator.Current => this.Current;

            public void Dispose()
            {
            }

            public IEnumerator<int> GetEnumerator()
            {
                return this;
            }

            public bool MoveNext()
            {
                if (this.reset)
                {
                    this.reset = false;
                    this.Current = this.start;
                }
                else
                {
                    this.Current = this.chain[this.Current].Next;
                    if (this.Current == this.start)
                    {
                        return false;
                    }
#if DEBUG
                    if (--this.maxIteratorCount < 0)
                    {
                        throw new InvalidOperationException("Chain is damaged");
                    }
#endif
                }

                return true;
            }

            public void Reset()
            {
                this.reset = true;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        /// <summary>
        /// Information about an element in the vertex chain.
        /// </summary>
        [DebuggerDisplay("{Prev}>{Id}>{Next}")]
        private class VertexInfo : IPolygonVertexInfo
        {
            private readonly int element;
            private readonly VertexChain[] chain;

            public VertexInfo(int element, VertexChain[] chain)
            {
                this.element = element;
                this.chain = chain;

                var id = this.Id;
                var prev = this.Prev;
                var next = this.Next;
                if (prev < id && next < id)
                {
                    this.Action = VertexAction.ClosingCusp;
                }
                else if (prev > id && next > id)
                {
                    this.Action = VertexAction.OpeningCusp;
                }
                else
                {
                    this.Action = VertexAction.Transition;
                }
            }

            /// <inheritdoc/>
            public VertexAction Action { get; }

            /// <inheritdoc/>
            public int Id => this.chain[this.element].VertexId;

            /// <inheritdoc/>
            public int Next => this.chain[this.chain[this.element].Next].VertexId;

            /// <inheritdoc/>
            public int Prev => this.chain[this.chain[this.element].Prev].VertexId;

            /// <inheritdoc/>
            public int Unique => this.element;

            /// <inheritdoc/>
            public int NextUnique => this.chain[this.element].Next;

            /// <inheritdoc/>
            public int PrevUnique => this.chain[this.element].Prev;
        }

        /// <summary>
        /// Splits the polygon
        /// </summary>
        private class PolygonSplitter
        {
            /// <summary>
            /// The original polygon.
            /// </summary>
            private readonly Polygon originalPolygon;

            /// <summary>
            /// The start per polygon id map
            /// </summary>

            private readonly List<int> polygonStartIndices;

            /// <summary>
            /// The splits to process, the tuple contains indexs in the chain - NOT vertex ids
            /// </summary>
            private readonly Tuple<int, int>[] allSplits;

            /// <summary>
            /// The chain, prealloceted length for additional chain entries (2 per split)
            /// </summary>
            private readonly VertexChain[] chain;

            /// <summary>
            /// the collector for simple triangles
            /// </summary>
            private readonly ITriangleCollector triangleCollector;

            /// <summary>
            /// The next free index in chain
            /// </summary>
            private int chainFreeIndex;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="polygon">the original polygon</param>
            /// <param name="splits">tuples of vertex ids, where to split</param>
            public PolygonSplitter(Polygon polygon, IEnumerable<Tuple<int, int>> splits, ITriangleCollector triangleCollector)
            {
                this.allSplits = splits
                    .Select(x =>
                    {
                        var chain1 = polygon.vertexToChain[x.Item1];
                        var chain2 = polygon.vertexToChain[x.Item2];
                        return chain1 < chain2 ? Tuple.Create(chain1, chain2) : Tuple.Create(chain2, chain1);
                    })
                    .OrderBy(x => x.Item1)
                    .ThenBy(x => x.Item2)
                    .ToArray();

                this.originalPolygon = polygon;
                this.polygonStartIndices = new List<int>(polygon.polygonStartIndices);

                this.chainFreeIndex = polygon.chain.Length;
                this.chain = new VertexChain[this.chainFreeIndex + this.allSplits.Length * 2];
                Array.Copy(polygon.chain, this.chain, this.chainFreeIndex);

                this.triangleCollector = triangleCollector;
            }

            /// <summary>
            /// Change the polygon id for the complete chain
            /// </summary>
            /// <param name="start">start at that polygon index</param>
            /// <param name="polygonId"></param>
            /// <returns>the chain index that points back to the start</returns>
            public static int FillPolygonId(VertexChain[] chain, int start, int polygonId)
            {
                var i = start;
                while (true)
                {
                    chain[i].PolygonId = polygonId;

                    var result = i;
                    i = chain[i].Next;

                    if (i == start)
                    {
                        return result;
                    }
                }
            }

            /// <summary>
            /// Process the splits
            /// </summary>
            /// <returns>a polygon with multiple montone subpolygons</returns>
            public Polygon Execute()
            {
                var splits = JoinHolesIntoPolygon();

                foreach (var split in splits)
                {
                    SplitPolygon(split.Item1, split.Item2);
                }

                return new Polygon(this.originalPolygon.vertexCoordinates, this.chain, this.originalPolygon.vertexToChain, this.polygonStartIndices);
            }

            /// <summary>
            /// Process all splits that join holes
            /// </summary>
            /// <returns>The remaining splits</returns>
            private IEnumerable<Tuple<int, int>> JoinHolesIntoPolygon()
            {
                if (this.polygonStartIndices.Count == 1)
                {
                    return this.allSplits;
                }

                var remaining = new List<Tuple<int, int>>();
                foreach (var split in this.allSplits)
                {
                    var from = split.Item1;
                    var to = split.Item2;
                    if (this.chain[from].PolygonId != this.chain[to].PolygonId)
                    {
                        this.JoinHoleIntoPolygon(from, to);
                    }
                    else if (from != to)
                    {
                        remaining.Add(split);
                    }
                }

                return remaining;
            }

            /// <summary>
            /// Split the polygon chains
            /// </summary>
            /// <param name="from">start of the segment</param>
            /// <param name="to">end of the segment</param>
            /// <param name="triangleCollector">collector for simple triangles</param>
            /// <returns>the splitted polygon</returns>
            private void SplitPolygon(int from, int to)
            {
                (from, to) = this.FindCommonChain(from, to);
                var polygonId = this.chain[from].PolygonId;

                if (this.IsTriangle(from, to))
                {
                    if (!this.IsTriangle(to, from))
                    {
                        // skip from.Next in the current polygon chain.
                        this.polygonStartIndices[polygonId] = from;
                        this.chain[this.chain[from].Next].PolygonId = -1;
                        SetNext(this.chain, from, to);
                    }
                    else
                    {
                        // The polygon was split into two triangles. Remove it from the result.
                        this.polygonStartIndices[polygonId] = -1;
                    }
                }
                else if (this.IsTriangle(to, from))
                {
                    // skip to.Next in the current polygon chain.
                    this.polygonStartIndices[polygonId] = from;
                    this.chain[this.chain[to].Next].PolygonId = -1;
                    SetNext(this.chain, to, from);
                }
                else
                {
                    SplitChainIntoTwoPolygons(from, to);
                }
            }

            /// <summary>
            /// Split the chain into two polygons
            /// </summary>
            /// <param name="from">the start of the common edge</param>
            /// <param name="to">the end of the common edge</param>
            private void SplitChainIntoTwoPolygons(int from, int to)
            {
                var fromCopy = this.chainFreeIndex++;
                var toCopy = this.chainFreeIndex++;

                chain[toCopy] = chain[to];
                chain[to].SameVertexChain = toCopy;
                chain[fromCopy] = chain[from];
                chain[from].SameVertexChain = fromCopy;

                var oldPolygonId = chain[from].PolygonId;

                // already copied: chain[fromCopy].Next = chain[from].Next;
                SetNext(chain, from, toCopy);
                SetNext(chain, to, fromCopy);

                var newPolygonId = this.polygonStartIndices.Count;
                this.polygonStartIndices.Add(fromCopy);

                FillPolygonId(chain, fromCopy, newPolygonId);

                this.polygonStartIndices[newPolygonId] = fromCopy;
                this.polygonStartIndices[oldPolygonId] = toCopy;
            }

            /// <summary>
            /// Check if the chain from..to forms a triangle. Adds the triangle to the result collector.
            /// </summary>
            /// <param name="from">the start index in the chain</param>
            /// <param name="to">the target index in the chain</param>
            /// <returns>true if it's a triangle</returns>
            private bool IsTriangle(int from, int to)
            {
                ref var p0 = ref this.chain[from];
                ref var p1 = ref this.chain[p0.Next];

                if (p1.Next == to)
                {
                    this.triangleCollector.AddTriangle(p0.VertexId, p1.VertexId, this.chain[p1.Next].VertexId);
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Find the chain that contains the vertices of from and to
            /// </summary>
            /// <param name="from">the from index in the chain</param>
            /// <param name="to">the to index in the chain</param>
            /// <returns>the from and to of one polygon that belongs to the very same polygon id</returns>
            private (int, int) FindCommonChain(int from, int to)
            {
                for (/**/; from >= 0; from = this.chain[from].SameVertexChain)
                {
                    for (var currentTo = to; currentTo >= 0; currentTo = this.chain[currentTo].SameVertexChain)
                    {
                        if (this.chain[from].PolygonId == this.chain[currentTo].PolygonId)
                        {
                            from = this.ShortestPathToChainEnd(from, currentTo);
                            currentTo = this.ShortestPathToChainEnd(currentTo, from);

                            return (from, currentTo);
                        }
                    }
                }

                throw new InvalidOperationException("No vertex chain found");
            }

            /// <summary>
            /// Get the entry in the polygon chain with the same vertex id and the shortest path to chainEnd
            /// </summary>
            /// <param name="chainIndex">the chain entry with the other end of the split</param>
            /// <param name="chainEnd">the chain index of the other end of the split</param>
            /// <returns>the chain index with the shortest path</returns>
            /// <remarks>
            /// This is for the situation after a polygon join:
            /// - The same vertex is in the polygon 2 times.
            /// - If we use the wrong vertex, there we introduce invalid splits
            /// - We're searching for the shorter chain to the target vertex.
            /// - For all non-joining splits, there is no same vertex in two polygons, so the overhead is minimal
            /// </remarks>
            private int ShortestPathToChainEnd(int chainIndex, int chainEnd)
            {
                var polygonId = this.chain[chainIndex].PolygonId;
                for (var next = this.chain[chainIndex].SameVertexChain; next >= 0; next = this.chain[next].SameVertexChain)
                {
                    if (this.chain[next].PolygonId == polygonId)
                    {
                        if (this.IsVertexDistanceShorter(chainIndex, next, chainEnd))
                        {
                            chainIndex = next;
                        }
                    }
                }

                return chainIndex;
            }

            /// <summary>
            /// Tests if the distance to chainEnd is shorter from chainStart or from otherChainStart
            /// </summary>
            /// <param name="chainStart">the start of the chain</param>
            /// <param name="otherChainStart">the start of the other chain</param>
            /// <param name="chainEnd">the chain id of the other end of the split</param>
            /// <returns>true if otherChainId has a shorter chain to targetVertex</returns>
            private bool IsVertexDistanceShorter(int chainStart, int otherChainStart, int chainEnd)
            {
                var chainId = this.chain[chainStart].Next;
                var otherChainId = this.chain[otherChainStart].Next;
                var endId = this.chain[chainEnd].Next;

                while (true)
                {
                    if (chainId == chainEnd || endId == chainStart)
                    {
                        return false;
                    }

                    if (otherChainId == chainEnd || endId == otherChainStart)
                    {
                        return true;
                    }

                    chainId = this.chain[chainId].Next;
                    otherChainId = this.chain[otherChainId].Next;
                    endId = this.chain[endId].Next;
                }
            }

            /// <summary>
            /// Join two polygons. Effectively joins a hole into the outer polygon.
            /// </summary>
            /// <param name="data">the shared data</param>
            /// <param name="from">the start vertex</param>
            /// <param name="to">the end vertex</param>
            private void JoinHoleIntoPolygon(int from, int to)
            {
                var fromCopy = this.chainFreeIndex++;
                var toCopy = this.chainFreeIndex++;

                var deletedPolygonId = chain[to].PolygonId;
                this.polygonStartIndices[deletedPolygonId] = -1;
                var lastVertexInHole = FillPolygonId(chain, to, chain[from].PolygonId);

                chain[toCopy] = chain[to];
                chain[to].SameVertexChain = toCopy;
                chain[fromCopy] = chain[from];
                chain[from].SameVertexChain = fromCopy;

                SetNext(chain, fromCopy, chain[from].Next);
                SetNext(chain, toCopy, fromCopy);
                SetNext(chain, from, to);
                SetNext(chain, lastVertexInHole, toCopy);
            }
        }

        /// <summary>
        /// Build a new polygon
        /// </summary>
        private class PolygonBuilder : IPolygonBuilder
        {
            private readonly Vertex[] vertices;
            private readonly List<int> vertexIds;
            private readonly List<int> nextIndices;
            private readonly List<int> polygonIds;
            private int first;
            private int polygonId;

            public PolygonBuilder(Vertex[] vertices)
            {
                this.first = 0;
                this.vertices = vertices;
                this.vertexIds = new List<int>();
                this.nextIndices = new List<int>();
                this.polygonIds = new List<int>();
                this.polygonId = 0;
            }

            public Polygon Auto()
            {
                return Polygon.FromVertexList(
                    this.vertices,
                    Enumerable.Range(0, this.vertices.Length),
                    Enumerable.Range(1, this.vertices.Length - 1).Concat(Enumerable.Range(0, 1)),
                    Enumerable.Repeat(0, this.vertices.Length),
                    null);
            }

            public IPolygonBuilder Add(int vertex)
            {
                this.nextIndices.Add(this.nextIndices.Count + 1);
                this.vertexIds.Add(vertex);
                this.polygonIds.Add(this.polygonId);
                return this;
            }

            public IPolygonBuilder AddVertices(IEnumerable<int> vertices)
            {
                foreach (var vertex in vertices)
                {
                    this.nextIndices.Add(this.nextIndices.Count + 1);
                    this.vertexIds.Add(vertex);
                    this.polygonIds.Add(this.polygonId);
                }

                return this;
            }

            public IPolygonBuilder ClosePartialPolygon()
            {
                if (this.vertexIds.Count > this.first)
                {
                    this.nextIndices[this.nextIndices.Count - 1] = first;
                    this.polygonId++;
                    this.first = this.vertexIds.Count;
                }

                return this;
            }

            public Polygon Close(params int[] fusionVertices)
            {
                this.ClosePartialPolygon();
                return Polygon.FromVertexList(this.vertices, this.vertexIds, this.nextIndices, this.polygonIds, fusionVertices);
            }
        }
    }
}
