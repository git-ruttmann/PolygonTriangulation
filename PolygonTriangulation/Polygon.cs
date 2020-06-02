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

    [DebuggerDisplay("{Debug}")]
    public partial class Polygon
    {
        private readonly SharedData data;
        private int start;

        private Polygon(int start, SharedData sharedData)
        {
            this.start = start;
            this.data = sharedData;
        }

        public bool Locked { get; set; }

        public bool IsTriangle => this.data.Chain[this.data.Chain[this.data.Chain[this.start].Next].Next].Next == this.start;

        public String Debug => String.Join(" ", this.Indices);

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
    }
}
