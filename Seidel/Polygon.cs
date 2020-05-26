namespace Ruttmann.PolygonTriangulation.Seidel
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Numerics;

    [DebuggerDisplay("{Debug}")]
    public class Polygon
    {
        private readonly SharedData data;
        private readonly int start;

        private Polygon(int start, SharedData sharedData)
        {
            this.start = start;
            this.data = sharedData;
        }

        public bool Locked { get; set; }

        public bool IsTriangle => this.data.Chain[this.data.Chain[this.data.Chain[this.start].Next].Next].Next == this.start;

        public int NextOf(int vertexId) => throw new NotImplementedException();
        // this.chain[vertexId];

        public String Debug => String.Join(" ", this.Indices);

        public IEnumerable<int> Indices => new NextChainEnumerator(this.start, this.data.Chain);

        public IReadOnlyList<Vector2> Vertices => Array.AsReadOnly(this.data.Vertices);

        public int Id => this.data.Chain[this.start].PolygonId;

        /// <summary>
        /// Legacy: converts a segment chain to a polygon
        /// </summary>
        /// <param name="segments">the segments</param>
        /// <returns>a polygon</returns>
        public static Polygon FromSegments(IEnumerable<ISegment> segments)
        {
            var segmentsArray = segments.ToArray();
            var vertexList = new List<Vector2>();
            var chain = new VertexInfo[segmentsArray.Length];


            foreach (var segment in segmentsArray)
            {
                if (segment.Id >= vertexList.Count)
                {
                    var itemsToAdd = segment.Id - vertexList.Count + 1;
                    vertexList.AddRange(Enumerable.Repeat(default(Vector2), itemsToAdd));
                }

                vertexList[segment.Id] = segment.Start;
            }

            var vertexToChain = new int[vertexList.Count];
            var chainId = 0;
            foreach (var segment in segmentsArray)
            {
                chain[chainId].VertexId = segment.Id;
                vertexToChain[segment.Id] = chainId++;
            }

            foreach (var segment in segmentsArray)
            {
                chain[vertexToChain[segment.Id]].Next = vertexToChain[segment.Next.Id];
            }

            // Assign one polygon id per chain (detect holes by comparing the polygon id)
            var polygonId = 0;
            for (int start = 0; start < chain.Length; start++)
            {
                if (chain[start].SameVertexChain == 0)
                {
                    var i = start;
                    do
                    {
                        chain[i].PolygonId = polygonId;
                        chain[start].SameVertexChain = -1;
                        i = chain[i].Next;
                    }
                    while (i != start);

                    polygonId++;
                }
            }

            var sharedData = new SharedData(polygonId, vertexList, chain, vertexToChain);

            return new Polygon(0, sharedData);
        }

        public static (Polygon[], Polygon[]) Split(Polygon polygon, IEnumerable<Tuple<int, int>> splits)
        {
            var splitter = new PolygonSplitter(polygon, splits);
            splitter.Execute();
            return (splitter.Triangles.ToArray(), splitter.Result.ToArray());
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

            return new NextChainEnumerator(startId, this.data.Chain);
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
            /// The next point in the polygon (same id)
            /// </summary>
            public int Next;

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
                    this.Current = this.chain[this.position].VertexId;
                }
                else
                {
                    this.position = this.chain[this.position].Next;
                    this.Current = this.chain[this.position].VertexId;
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
        /// data shared between the splitted polygons - the vertices, the chain and the mapping
        /// </summary>
        private class SharedData
        {
            private int polygonId;

            public SharedData(int polygonId, IEnumerable<Vector2> vertices, VertexInfo[] chain, int[] vertexToChain)
            {
                this.polygonId = polygonId;
                this.Vertices = vertices as Vector2[] ?? vertices.ToArray();
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
            public VertexInfo[] Chain { get; private set; }

            /// <summary>
            /// Gets the vertex coordinates
            /// </summary>
            public Vector2[] Vertices { get; }

            /// <summary>
            /// Adds 2 elements to the chain
            /// </summary>
            /// <returns>the first index with new data</returns>
            public int ExtendChain()
            {
                var oldChain = this.Chain;
                this.Chain = new VertexInfo[this.Chain.Length + 2];
                Array.Copy(oldChain, this.Chain, oldChain.Length);
                return oldChain.Length;
            }

            /// <summary>
            /// Creates a new polygon id
            /// </summary>
            /// <returns>the new polygon id</returns>
            public int NewPolygonId()
            {
                return ++this.polygonId;
            }
        }

        private class PolygonSplitter
        {
            private readonly Polygon initialPolygon;
            private readonly Tuple<int, int>[] allSplits;

            public PolygonSplitter(Polygon polygon, IEnumerable<Tuple<int, int>> splits)
            {
                this.initialPolygon = polygon;
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

                this.Triangles = new List<Polygon>();
                this.polygonDict = new Dictionary<int, Polygon>();
            }

            public IEnumerable<Polygon> Result => this.polygonDict.Values;

            public List<Polygon> Triangles { get; private set; }

            private Dictionary<int, Polygon> polygonDict;

            public void Execute()
            {
                var data = this.initialPolygon.data;
                IEnumerable<Tuple<int, int>> splits = this.allSplits;
                if (this.initialPolygon.data.LastInitialPolygonId > 1)
                {
                    var remaining = new List<Tuple<int, int>>();
                    foreach (var split in this.allSplits)
                    {
                        var from = split.Item1;
                        var to = split.Item2;
                        if (data.Chain[from].PolygonId != data.Chain[to].PolygonId)
                        {
                            JoinHoleIntoPolygon(data, from, to);
                        }
                        else
                        {
                            remaining.Add(split);
                        }
                    }

                    splits = remaining;
                }

                this.polygonDict.Add(this.initialPolygon.Id, this.initialPolygon);
                foreach (var split in splits)
                {
                    SplitPolygon(split.Item1, split.Item2);
                }
            }

            /// <summary>
            /// Split the polygon chains
            /// </summary>
            /// <param name="from">start of the segment</param>
            /// <param name="to">end of the segment</param>
            /// <returns>the splitted polygon</returns>
            private Polygon SplitPolygon(int from, int to)
            {
                (from, to) = this.FindCommonChain(from, to);

                var data = this.initialPolygon.data;
                var fromCopy = data.ExtendChain();
                var toCopy = fromCopy + 1;
                var chain = data.Chain;

                chain[toCopy] = chain[to];
                chain[to].SameVertexChain = toCopy;
                chain[fromCopy] = chain[from];
                chain[from].SameVertexChain = fromCopy;

                var oldPolygon = this.polygonDict[chain[from].PolygonId];

                // already copied: chain[fromCopy].Next = chain[from].Next;
                chain[from].Next = toCopy;
                chain[to].Next = fromCopy;

                var newPolygonId = data.NewPolygonId();

                FillPolygonId(chain, fromCopy, newPolygonId);

                // it's inpredictable yet, whether the "from" or the "to" belongs to the old polygon. After filling, the polygon id changes
                var startId = oldPolygon.Id == chain[fromCopy].PolygonId ? toCopy : fromCopy;
                var newPolygon = new Polygon(startId, data);
                this.polygonDict[newPolygon.Id] = newPolygon;
                this.polygonDict[oldPolygon.Id] = oldPolygon;
                return newPolygon;
            }

            /// <summary>
            /// Find the chain that contains the vertices of from and to
            /// </summary>
            /// <param name="from">the from index in the chain</param>
            /// <param name="to">the to index in the chain</param>
            /// <returns>the from and to of one polygon that belongs to the very same polygon id</returns>
            private (int, int) FindCommonChain(int from, int to)
            {
                var chain = this.initialPolygon.data.Chain;
                for (/**/; from >= 0; from = chain[from].SameVertexChain)
                {
                    for (var currentTo = to; currentTo >= 0; currentTo = chain[currentTo].SameVertexChain)
                    {
                        if (chain[from].PolygonId == chain[currentTo].PolygonId)
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
            /// <param name="chain">the chain data</param>
            /// <param name="start">start at that polygon index</param>
            /// <param name="polygonId"></param>
            /// <returns>the chain index that points back to the start</returns>
            private static int FillPolygonId(VertexInfo[] chain, int start, int polygonId)
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
            /// Join the 
            /// </summary>
            /// <param name="data"></param>
            /// <param name="from"></param>
            /// <param name="to"></param>
            private static void JoinHoleIntoPolygon(SharedData data, int from, int to)
            {
                var fromCopy = data.ExtendChain();
                var toCopy = fromCopy + 1;
                var chain = data.Chain;

                var lastVertexInHole = FillPolygonId(chain, to, chain[from].PolygonId);

                chain[toCopy] = chain[to];
                chain[to].SameVertexChain = toCopy;
                chain[fromCopy] = chain[from];
                chain[from].SameVertexChain = fromCopy;

                // already copied: chain[fromCopy].Next = chain[from].Next;
                chain[toCopy].Next = fromCopy;
                chain[from].Next = to;
                chain[lastVertexInHole].Next = toCopy;
            }
        }
    }
}
