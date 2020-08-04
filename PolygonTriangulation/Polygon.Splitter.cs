namespace PolygonTriangulation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// subclass container for polygon
    /// </summary>
    public partial class Polygon
    {
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
            /// Initializes a new instance of the <see cref="PolygonSplitter" /> class.
            /// </summary>
            /// <param name="polygon">the original polygon</param>
            /// <param name="splits">tuples of vertex ids, where to split</param>
            /// <param name="triangleCollector">The triangle collector.</param>
            public PolygonSplitter(Polygon polygon, IEnumerable<Tuple<int, int>> splits, ITriangleCollector triangleCollector)
            {
                this.allSplits = splits.ToArray();
                this.originalPolygon = polygon;
                this.polygonStartIndices = new List<int>(polygon.polygonStartIndices);

                this.chainFreeIndex = polygon.chain.Length;
                this.chain = new VertexChain[this.chainFreeIndex + (this.allSplits.Length * 2)];
                Array.Copy(polygon.chain, this.chain, this.chainFreeIndex);

                this.triangleCollector = triangleCollector;
            }

            /// <summary>
            /// Change the polygon id for the complete chain
            /// </summary>
            /// <param name="chain">The polygon chain.</param>
            /// <param name="start">start of that polygon in the chain</param>
            /// <param name="polygonId">The polygon id.</param>
            /// <returns>the chain index that points back to the start</returns>
            public static int FillPolygonId(VertexChain[] chain, int start, int polygonId)
            {
                var i = start;
                while (true)
                {
                    chain[i].SubPolygonId = polygonId;

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
                var splits = this.JoinHolesIntoPolygon();

                foreach (var split in splits)
                {
                    var (from, to) = this.FindCommonChain(split.Item1, split.Item2);
                    this.SplitPolygon(from, to);
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
                    var from = this.originalPolygon.vertexToChain[split.Item1];
                    var to = this.originalPolygon.vertexToChain[split.Item2];
                    if (this.IsDifferentPolygon(from, to))
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
            /// Verifies that the chain elements from and to have no vertex instance that are on the same polygon
            /// </summary>
            /// <param name="from">the chain id of the start vertex</param>
            /// <param name="to">the chain id of the target vertex</param>
            /// <returns>true if vertices at from and to are always on different polygons</returns>
            /// <remarks>
            /// As a side effect of vertex fustion, there might be the same vertex on different polygons
            /// </remarks>
            private bool IsDifferentPolygon(int from, int to)
            {
                for (/* from = from */; from >= 0; from = this.chain[from].SameVertexChain)
                {
                    for (var i = to; i >= 0; i = this.chain[i].SameVertexChain)
                    {
                        if (this.chain[from].SubPolygonId == this.chain[i].SubPolygonId)
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            /// <summary>
            /// Split the polygon chains
            /// </summary>
            /// <param name="from">start of the segment</param>
            /// <param name="to">end of the segment</param>
            private void SplitPolygon(int from, int to)
            {
                var polygonId = this.chain[from].SubPolygonId;

                if (this.IsTriangle(from, to))
                {
                    if (!this.IsTriangle(to, from))
                    {
                        // skip from.Next in the current polygon chain.
                        this.polygonStartIndices[polygonId] = from;
                        this.chain[this.chain[from].Next].SubPolygonId = -1;
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
                    this.chain[this.chain[to].Next].SubPolygonId = -1;
                    SetNext(this.chain, to, from);
                }
                else
                {
                    this.SplitChainIntoTwoPolygons(from, to);
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

                this.chain[toCopy] = this.chain[to];
                this.chain[to].SameVertexChain = toCopy;
                this.chain[fromCopy] = this.chain[from];
                this.chain[from].SameVertexChain = fromCopy;

                var oldPolygonId = this.chain[from].SubPolygonId;

                //// already copied: chain[fromCopy].Next = chain[from].Next;
                SetNext(this.chain, from, toCopy);
                SetNext(this.chain, to, fromCopy);

                var newPolygonId = this.polygonStartIndices.Count;
                this.polygonStartIndices.Add(fromCopy);

                FillPolygonId(this.chain, fromCopy, newPolygonId);

                this.polygonStartIndices[newPolygonId] = fromCopy;
                this.polygonStartIndices[oldPolygonId] = toCopy;
            }

            /// <summary>
            /// Check if the chain from..to forms a triangle. Adds the triangle to the result collector.
            /// </summary>
            /// <param name="start">the start index in the chain</param>
            /// <param name="target">the target index in the chain</param>
            /// <returns>true if it's a triangle</returns>
            private bool IsTriangle(int start, int target)
            {
                ref var p0 = ref this.chain[start];
                ref var p1 = ref this.chain[p0.Next];

                if (p1.Next == target)
                {
                    this.triangleCollector.AddTriangle(p0.VertexId, p1.VertexId, this.chain[p1.Next].VertexId);
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Find the subpolygon that contains both vertices
            /// </summary>
            /// <param name="fromVertex">the from index in the chain</param>
            /// <param name="toVertex">the to index in the chain</param>
            /// <returns>the from and to of one polygon that belongs to the very same polygon id</returns>
            private (int, int) FindCommonChain(int fromVertex, int toVertex)
            {
                var from = this.originalPolygon.vertexToChain[fromVertex];
                var firstTo = this.originalPolygon.vertexToChain[toVertex];
                for (/* from = from */; from >= 0; from = this.chain[from].SameVertexChain)
                {
                    for (var to = firstTo; to >= 0; to = this.chain[to].SameVertexChain)
                    {
                        if (this.chain[from].SubPolygonId == this.chain[to].SubPolygonId)
                        {
                            from = this.ChooseInstanceForSplit(from, to);
                            var finalTo = this.ChooseInstanceForSplit(to, from);
                            return (from, finalTo);
                        }
                    }
                }

                throw new InvalidOperationException("No vertex chain found");
            }

            /// <summary>
            /// Get the entry with the same vertex id that is on the same polygon.
            /// </summary>
            /// <param name="chainId">the start chain id of the point</param>
            /// <param name="peer">the peer chain id of the split</param>
            /// <returns>the best instance with the same vertex id</returns>
            /// <remarks>
            /// This is for the situation after a polygon join:
            /// - The same vertex is in the polygon 2 times.
            /// - Using the wrong vertex causes cross-over subpolygons.
            /// - The duplicate vertex (created by <see cref="JoinHoleIntoPolygon"/>) is always the next in the SameVertexChain
            /// </remarks>
            private int ChooseInstanceForSplit(int chainId, int peer)
            {
                var sameVertexChain = this.chain[chainId].SameVertexChain;
                if (sameVertexChain < 0 || this.chain[chainId].SubPolygonId != this.chain[sameVertexChain].SubPolygonId)
                {
                    return chainId;
                }

                ref var vertex = ref this.originalPolygon.vertexCoordinates[this.chain[chainId].VertexId];
                ref var peerVertex = ref this.originalPolygon.vertexCoordinates[this.chain[peer].VertexId];
                var peerAngle = DiamondAngle(ref vertex, ref peerVertex);

                ref var prevVertex = ref this.originalPolygon.vertexCoordinates[this.chain[this.chain[chainId].Prev].VertexId];
                ref var nextVertex = ref this.originalPolygon.vertexCoordinates[this.chain[this.chain[chainId].Next].VertexId];
                var prevAngle = DiamondAngle(ref vertex, ref prevVertex);
                var nextAngle = DiamondAngle(ref vertex, ref nextVertex);

                nextAngle += nextAngle < prevAngle ? 4 : 0;
                peerAngle += peerAngle < prevAngle ? 4 : 0;
                if (prevAngle < peerAngle && nextAngle > peerAngle)
                {
                    // prev, peer and next are sorted in counter-clock-wise order => peer is inside the polygon
                    return chainId;
                }
                else
                {
                    return sameVertexChain;
                }
            }

            /// <summary>
            /// Join two polygons. Effectively joins a hole into the outer polygon.
            /// </summary>
            /// <param name="from">the start vertex</param>
            /// <param name="to">the end vertex</param>
            private void JoinHoleIntoPolygon(int from, int to)
            {
                var fromCopy = this.chainFreeIndex++;
                var toCopy = this.chainFreeIndex++;

                var deletedPolygonId = this.chain[to].SubPolygonId;
                this.polygonStartIndices[deletedPolygonId] = -1;
                var lastVertexInHole = FillPolygonId(this.chain, to, this.chain[from].SubPolygonId);

                this.chain[toCopy] = this.chain[to];
                this.chain[to].SameVertexChain = toCopy;
                this.chain[fromCopy] = this.chain[from];
                this.chain[from].SameVertexChain = fromCopy;

                SetNext(this.chain, fromCopy, this.chain[from].Next);
                SetNext(this.chain, toCopy, fromCopy);
                SetNext(this.chain, from, to);
                SetNext(this.chain, lastVertexInHole, toCopy);
            }
        }
    }
}
