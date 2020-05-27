namespace Ruttmann.PolygonTriangulation.Seidel
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;

    using Vertex = System.Numerics.Vector2;

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
        /// Enumerate all points of the current polygon as Segments
        /// </summary>
        public IEnumerable<ISegment> PolygonSegments
            => new NextChainEnumerator(this.start, this.data.Chain)
            .Select(x => new Segment(x, this.data));

        /// <summary>
        /// Enumerate all segments in the chain. Contains all holes and after splitting all splitted polygones.
        /// </summary>
        public IEnumerable<ISegment> AllSegments => this.data.Chain.Select((x, i) => new Segment(i, this.data));

        public static IPolygonBuilder Build(Vertex[] vertices)
        {
            return new PolygonBuilder(vertices);
        }

        /// <summary>
        /// Create a polygon with the enumerated list of vertex ids. The successor of the vertex is implicitly the next (and last->first).
        /// </summary>
        /// <param name="vertexCoordinates">the coordinates</param>
        /// <param name="vertexIds">the vertex ids</param>
        /// <returns>a polygon</returns>
        public static Polygon FromVertexList(Vertex[] vertexCoordinates, IEnumerable<int> vertexIds)
        {
            var vertexIdCollection = vertexIds as IReadOnlyCollection<int> ?? vertexIds.ToArray();
            var vertexToChain = new int[vertexCoordinates.Length];
            var chain = new VertexInfo[vertexIdCollection.Count];

            var i = 0;
            foreach (var vertexId in vertexIdCollection)
            {
                SetNext(chain, i, i == chain.Length - 1 ? 0 : i + 1); 
                chain[i].VertexId = vertexId;
                chain[i].SameVertexChain = -1;
                chain[i].PolygonId = 1;
                vertexToChain[vertexId] = i;
                i++;
            }

            var data = new SharedData(1, vertexCoordinates, chain, vertexToChain);
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
            var chain = new VertexInfo[vertexIdCollection.Count];

            var i = 0;
            var maxPolygonId = 0;
            foreach (var (vertexId, nextId) in vertexIdCollection.Zip(nextIndices, Tuple.Create))
            {
                SetNext(chain, i, nextId); 
                chain[i].VertexId = vertexId;
                chain[i].SameVertexChain = -1;
                chain[i].PolygonId = polygonIdCollection[i];
                maxPolygonId = Math.Max(maxPolygonId, polygonIdCollection[i]);
                vertexToChain[vertexId] = i;
                i++;
            }

            var data = new SharedData(maxPolygonId, vertexCoordinates, chain, vertexToChain);
            return new Polygon(0, data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetNext(VertexInfo[] chain, int current, int next)
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
                if (startId == 0)
                {
                    throw new InvalidOperationException($"Vertex {startVertex} is not part of polygon {this.Id}");
                }
            }

            return new NextChainEnumerator(startId, this.data.Chain).Select(x => this.data.Chain[x].VertexId);
        }

        public struct VertexInfo
        {

            /// <summary>
            /// the id in the vertex list and the content of the result
            /// </summary>
            public int VertexId;

            /// <summary>
            /// The id of the polygon. Holes are a separate polygon
            /// </summary>
            public int PolygonId;

            /// <summary>
            /// The previous point in the polygon (same id)
            /// </summary>
            public int Prev { get; private set; }

            /// <summary>
            /// The next point in the polygon (same id)
            /// </summary>
            public int Next { get; private set; }

            /// <summary>
            /// Chain two items
            /// </summary>
            /// <param name="currentId">the id of the current item</param>
            /// <param name="nextId">the id of the next item</param>
            /// <param name="nextItem">the data of the next item</param>
            public void SetNext(int currentId, int nextId, ref VertexInfo nextItem)
            {
                this.Next = nextId;
                nextItem.Prev = currentId;
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
            private readonly IList<VertexInfo> chain;
            private int position;
            private bool reset;

            public NextChainEnumerator(int start, IList<VertexInfo> chain)
            {
                this.start = start;
                this.Current = start;
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
                    this.position = this.start;
                    this.Current = this.position;
                }
                else
                {
                    this.position = this.chain[this.position].Next;
                    this.Current = this.position;
                    if (this.position == this.start)
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

        /// <summary>
        /// data shared between the splitted polygons - the vertex coordinates, the chain and the mapping
        /// </summary>
        private class SharedData
        {
            private int polygonId;

            public SharedData(int polygonId, IEnumerable<Vertex> vertexCoordinates, VertexInfo[] chain, int[] vertexToChain)
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
            /// Translate the vertex id to the index in the chain. For collisions look at <see cref="VertexInfo.SameVertexChain"/>
            /// </summary>
            public int[] VertexToChain { get; }

            /// <summary>
            /// The current chain of vertices in the polygon. May contain multiple chains.
            /// </summary>
            public VertexInfo[] Chain { get; }

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
    }
}
