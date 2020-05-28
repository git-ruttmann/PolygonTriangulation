namespace Ruttmann.PolygonTriangulation.Seidel
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Vertex = System.Numerics.Vector2;

    /// <summary>
    /// Internal classes for polygon handling
    /// </summary>
    public partial class Polygon
    {
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
            private readonly VertexInfo[] chain;

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
                this.chain = new VertexInfo[this.chainFreeIndex + this.allSplits.Length * 2];
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
        /// A segment abstraction for the current polygon
        /// </summary>
        [DebuggerDisplay("{Id} {Start} {End}")]
        private class Segment : ISegment, IEqualityComparer<ISegment>
        {
            private int chainId;
            private SharedData data;

            public Segment(int chainId, SharedData data)
            {
                this.chainId = chainId;
                this.data = data;
            }

            public ISegment Prev => new Segment(this.data.Chain[this.chainId].Prev, this.data);

            public int PrevId => this.data.Chain[this.data.Chain[this.chainId].Prev].VertexId;

            public ISegment Next => new Segment(this.data.Chain[this.chainId].Next, this.data);

            public int NextId => this.data.Chain[this.data.Chain[this.chainId].Next].VertexId;

            public int Id => this.data.Chain[this.chainId].VertexId;

            public Vertex v0 => this.Start;

            public Vertex v1 => this.End;

            public Vertex Start => this.data.VertexCoordinates[this.data.Chain[chainId].VertexId];
            
            public Vertex End => this.data.VertexCoordinates[this.data.Chain[this.data.Chain[chainId].Next].VertexId];

            public bool Equals(ISegment x, ISegment y)
            {
                return x.Id.Equals(y.Id);
            }

            public int GetHashCode(ISegment obj) => obj.Id.GetHashCode();

            public IEnumerator<ISegment> GetEnumerator() => throw new NotImplementedException();

            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
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

            public IPolygonBuilder AddVertices(params int[] vertices)
            {
                foreach (var vertex in vertices)
                {
                    this.nextIndices.Add(this.nextIndices.Count + 1);
                    this.vertexIds.Add(vertex);
                    this.polygonIds.Add(this.polygonId);
                }

                return this;
            }

            public IPolygonBuilder AddHole(params int[] vertices)
            {
                this.StartHole(vertices[0]);
                foreach (var vertex in vertices.Skip(1))
                {
                    this.nextIndices.Add(this.nextIndices.Count + 1);
                    this.vertexIds.Add(vertex);
                    this.polygonIds.Add(this.polygonId);
                }

                return this;
            }

            public IPolygonBuilder StartHole(int vertex)
            {
                this.nextIndices[this.nextIndices.Count - 1] = first;
                this.nextIndices.Add(this.nextIndices.Count + 1);

                this.polygonId++;
                this.first = this.vertexIds.Count;
                this.vertexIds.Add(vertex);
                this.polygonIds.Add(this.polygonId);

                return this;
            }

            public Polygon Close()
            {
                this.nextIndices[this.nextIndices.Count - 1] = first;
                return Polygon.FromVertexList(this.vertices, this.vertexIds, this.nextIndices, this.polygonIds);
            }
        }
    }
}
