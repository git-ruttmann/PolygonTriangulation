﻿namespace PolygonTriangulation
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;

    using Vertex = System.Numerics.Vector2;

    /// <summary>
    /// Information about an element in the vertex chain of a polygon.
    /// </summary>
    public interface IPolygonVertexInfo
    {
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
        /// <returns>a polygon</returns>
        Polygon Close();

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
    /// Represent a closed polygon.
    /// </summary>
    [DebuggerDisplay("{Debug}")]
    public class Polygon
    {
        private readonly SharedData data;
        private int start;

        private Polygon(int start, SharedData sharedData)
        {
            this.start = start;
            this.data = sharedData;
        }

        public string Debug => string.Join(" ", this.Indices);

        /// <summary>
        /// Gets the vertex ids of the polygon. Doesn't return holes.
        /// </summary>
        public IEnumerable<int> Indices 
            => new NextChainEnumerator(this.start, this.data.Chain)
            .Select(x => this.data.Chain[x].VertexId);

        /// <summary>
        /// Gets the vertex coordinates
        /// </summary>
        public IReadOnlyList<Vertex> Vertices => Array.AsReadOnly(this.data.VertexCoordinates);

        /// <summary>
        /// Gets the polygon id
        /// </summary>
        public int Id => this.data.Chain[this.start].PolygonId;

        /// <summary>
        /// Get id/prev/next info per vertex sorted by vertex id.
        /// </summary>
        public IEnumerable<IPolygonVertexInfo> OrderedVertexes
        {
            get
            {
                var workOrder = Enumerable.Range(0, this.data.Chain.Length).OrderBy(x => this.data.Chain[x].VertexId);
                return workOrder.Select(x => new VertexInfo(x, this.data.Chain));
            }
        }

        public static IPolygonBuilder Build(Vertex[] vertices)
        {
            return new PolygonBuilder(vertices);
        }

        /// <summary>
        /// create a polygon from multiple polygon lines
        /// </summary>
        /// <param name="vertexCoordinates">the vertex coordinates</param>
        /// <param name="lines">the polygon lines.</param>
        /// <returns>a polygon</returns>
        public static Polygon FromPolygonLines(Vertex[] vertexCoordinates, IReadOnlyCollection<int>[] lines)
        {
            var chain = new VertexChain[lines.Sum(x => x.Count)];
            var id = 0;
            var first = id;
            var polygonId = 1;
            var vertexToChain = new int[vertexCoordinates.Length];

            foreach (var line in lines)
            {
                foreach (var vertexId in line)
                {
                    chain[id].VertexId = vertexId;
                    chain[id].PolygonId = polygonId;
                    chain[id].SameVertexChain = -1;
                    SetNext(chain, id, id == chain.Length - 1 ? first : id + 1);
                    vertexToChain[vertexId] = id++;
                }

                SetNext(chain, id - 1, first);
                first = id;
                polygonId++;
            }

            var data = new SharedData(polygonId - 1, vertexCoordinates, chain, vertexToChain);
            return new Polygon(0, data);
        }

        /// <summary>
        /// Creat a polygon with vertex id's and next chain. Can contain holes.
        /// </summary>
        /// <param name="vertexCoordinates">the coordinates</param>
        /// <param name="vertexIds">the vertex ids</param>
        /// <param name="nextIndices">the next index in vertexIds. Must be same length as vertexIds</param>
        /// <returns>a polygon</returns>
        public static Polygon FromVertexList(Vertex[] vertexCoordinates, IEnumerable<int> vertexIds, IEnumerable<int> nextIndices, IEnumerable<int> polygonIds)
        {
            var vertexIdCollection = vertexIds as IReadOnlyCollection<int> ?? vertexIds.ToArray();
            var polygonIdCollection = polygonIds as IList<int> ?? polygonIds.ToArray();
            var vertexToChain = new int[vertexCoordinates.Length];
            var chain = new VertexChain[vertexIdCollection.Count];

            var i = 0;
            var maxPolygonId = 0;
            foreach (var (vertexId, nextId) in vertexIdCollection.Zip(nextIndices, Tuple.Create))
            {
                chain[i].VertexId = vertexId;
                chain[i].SameVertexChain = -1;
                chain[i].PolygonId = polygonIdCollection[i];
                SetNext(chain, i, nextId); 
                maxPolygonId = Math.Max(maxPolygonId, polygonIdCollection[i]);
                vertexToChain[vertexId] = i;
                i++;
            }

            var data = new SharedData(maxPolygonId, vertexCoordinates, chain, vertexToChain);
            return new Polygon(0, data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetNext(VertexChain[] chain, int current, int next)
        {
            chain[current].SetNext(next, ref chain[next]);
        }

        /// <summary>
        /// Split the polygon along tuples of two vertex indices.
        /// </summary>
        /// <param name="polygon">the polygon to split. It's modified.</param>
        /// <param name="splits">the splits as tuples of vertex ids</param>
        /// <param name="triangleCollector">the collector for simple triangles</param>
        /// <returns>(simple triangles, other polygones)</returns>
        public static Polygon[] Split(Polygon polygon, IEnumerable<Tuple<int, int>> splits, ITriangleCollector triangleCollector)
        {
            var splitter = new PolygonSplitter(polygon, splits, triangleCollector);
            splitter.Execute();
            return splitter.Result.ToArray();
        }

        /// <summary>
        /// Gets an enumerator that starts at startVertex and loops the whole polygon once.
        /// </summary>
        /// <param name="start">The first vertex id</param>
        /// <returns>An Enumerable/Enumerator</returns>
        public IEnumerable<int> IndicesStartingAt(int startVertex)
        {
            var startId = this.data.VertexToChain[startVertex];
            while (this.data.Chain[startId].PolygonId != this.Id)
            {
                startId = this.data.Chain[startId].SameVertexChain;
                if (startId < 0)
                {
                    throw new InvalidOperationException($"Vertex {startVertex} is not part of polygon {this.Id}");
                }
            }

            return new NextChainEnumerator(startId, this.data.Chain).Select(x => this.data.Chain[x].VertexId);
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
        public struct VertexChain
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
            public int PrevVertexId { get; private set; }

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
            public void SetNext(int nextChain, ref VertexChain nextItem)
            {
                this.Next = nextChain;
                nextItem.PrevVertexId = this.VertexId;
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

            public NextChainEnumerator(int start, IList<VertexChain> chain)
            {
                this.start = start;
                this.chain = chain;
                this.reset = true;
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

        public static object Build(object vertices)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// data shared between the splitted polygons - the vertex coordinates, the chain and the mapping
        /// </summary>
        private class SharedData
        {
            private int polygonId;

            public SharedData(int polygonId, IEnumerable<Vertex> vertexCoordinates, VertexChain[] chain, int[] vertexToChain)
            {
                this.polygonId = polygonId;
                this.VertexCoordinates = vertexCoordinates as Vertex[] ?? vertexCoordinates.ToArray();
                this.Chain = chain;
                this.VertexToChain = vertexToChain;
                this.LastInitialPolygonId = polygonId;
            }

            /// <summary>
            /// Get't the number of initial polygon id's. With a single polygon line (no holes), it's 1
            /// </summary>
            public int LastInitialPolygonId { get; }

            /// <summary>
            /// Translate the vertex id to the index in the chain. For collisions look at <see cref="VertexChain.SameVertexChain"/>
            /// </summary>
            public int[] VertexToChain { get; }

            /// <summary>
            /// The current chain of vertices in the polygon. May contain multiple chains.
            /// </summary>
            public VertexChain[] Chain { get; }

            /// <summary>
            /// Gets the vertex coordinates
            /// </summary>
            public Vertex[] VertexCoordinates { get; }

            /// <summary>
            /// Creates a new polygon id
            /// </summary>
            /// <returns>the new polygon id</returns>
            public int NewPolygonId()
            {
                return ++this.polygonId;
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
            }

            public int Id => this.chain[this.element].VertexId;

            public int Next => this.chain[this.chain[this.element].Next].VertexId;

            public int Prev => this.chain[this.element].PrevVertexId;
        }

        /// <summary>
        /// Splits the polygon
        /// </summary>
        private class PolygonSplitter
        {
            /// <summary>
            /// The polygon id to polygon dictionary
            /// </summary>
            private readonly Dictionary<int, Polygon> polygonDict;

            /// <summary>
            /// The shared data for the created polygons
            /// </summary>
            private readonly SharedData data;

            /// <summary>
            /// The splits to process, the tuple contains indexs in the chain - NOT vertex ids
            /// </summary>
            private readonly Tuple<int, int>[] allSplits;

            /// <summary>
            /// The chain, prealloceted length for additional chain entries (2 per split)
            /// </summary>
            private readonly VertexChain[] chain;

            // the collector for simple triangles
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
                        var chain1 = polygon.data.VertexToChain[x.Item1];
                        var chain2 = polygon.data.VertexToChain[x.Item2];
                        return chain1 < chain2 ? Tuple.Create(chain1, chain2) : Tuple.Create(chain2, chain1);
                    })
                    .OrderBy(x => x.Item1)
                    .ThenBy(x => x.Item2)
                    .ToArray();

                this.polygonDict = new Dictionary<int, Polygon>();

                this.chainFreeIndex = polygon.data.Chain.Length;
                this.chain = new VertexChain[this.chainFreeIndex + this.allSplits.Length * 2];
                Array.Copy(polygon.data.Chain, this.chain, this.chainFreeIndex);
                this.data = new SharedData(polygon.data.LastInitialPolygonId, polygon.data.VertexCoordinates, this.chain, polygon.data.VertexToChain);

                this.polygonDict.Add(polygon.Id, new Polygon(polygon.start, this.data));
                this.triangleCollector = triangleCollector;
            }

            /// <summary>
            /// The resulting polygons with length > 3
            /// </summary>
            public IEnumerable<Polygon> Result => this.polygonDict.Values;

            /// <summary>
            /// Process the splits
            /// </summary>
            public void Execute()
            {
                var splits = JoinHolesIntoPolygon();

                foreach (var split in splits)
                {
                    SplitPolygon(split.Item1, split.Item2);
                }
            }

            /// <summary>
            /// Process all splits that join holes
            /// </summary>
            /// <returns>The remaining splits</returns>
            private IEnumerable<Tuple<int, int>> JoinHolesIntoPolygon()
            {
                if (this.data.LastInitialPolygonId == 1)
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
                    else
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

                if (this.IsTriangle(from, to))
                {
                    if (!this.IsTriangle(to, from))
                    {
                        // only join skip from.Next in the current polygon chain.
                        var polygon = this.polygonDict[chain[from].PolygonId];
                        polygon.start = from;
                        this.chain[this.chain[from].Next].PolygonId = -1;
                        SetNext(this.chain, from, to);
                    }
                    else
                    {
                        // The polygon was split into two triangles. Remove it from the result.
                        this.polygonDict.Remove(chain[from].PolygonId);
                    }
                }
                else if (this.IsTriangle(to, from))
                {
                    // only skip to.Next in the current polygon chain.
                    var polygon = this.polygonDict[chain[from].PolygonId];
                    polygon.start = from;
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

                var oldPolygon = this.polygonDict[chain[from].PolygonId];

                // already copied: chain[fromCopy].Next = chain[from].Next;
                SetNext(chain, from, toCopy);
                SetNext(chain, to, fromCopy);

                var newPolygonId = data.NewPolygonId();

                this.FillPolygonId(fromCopy, newPolygonId);

                // it's inpredictable yet, whether the "from" or the "to" belongs to the old polygon. After filling, the polygon id changes
                var startId = oldPolygon.Id == chain[fromCopy].PolygonId ? toCopy : fromCopy;
                var newPolygon = new Polygon(startId, data);
                this.polygonDict[newPolygon.Id] = newPolygon;
                this.polygonDict[oldPolygon.Id] = oldPolygon;
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
                            return (from, currentTo);
                        }
                    }
                }

                throw new InvalidOperationException("No vertex chain found");
            }

            /// <summary>
            /// Change the polygon id for the complete chain
            /// </summary>
            /// <param name="start">start at that polygon index</param>
            /// <param name="polygonId"></param>
            /// <returns>the chain index that points back to the start</returns>
            private int FillPolygonId(int start, int polygonId)
            {
                var i = start;
                while (true)
                {
                    this.chain[i].PolygonId = polygonId;

                    var result = i;
                    i = chain[i].Next;

                    if (i == start)
                    {
                        return result;
                    }
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

                var lastVertexInHole = this.FillPolygonId(to, chain[from].PolygonId);

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
                this.polygonId = 1;
            }

            public Polygon Auto()
            {
                return Polygon.FromVertexList(
                    this.vertices,
                    Enumerable.Range(0, this.vertices.Length),
                    Enumerable.Range(1, this.vertices.Length - 1).Concat(Enumerable.Range(0, 1)),
                    Enumerable.Repeat(0, this.vertices.Length));
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

            public Polygon Close()
            {
                this.ClosePartialPolygon();
                return Polygon.FromVertexList(this.vertices, this.vertexIds, this.nextIndices, this.polygonIds);
            }
        }
    }
}
