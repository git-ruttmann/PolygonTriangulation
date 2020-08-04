namespace PolygonTriangulation
{
    /// <summary>
    /// subclass container for polygon
    /// </summary>
    public partial class Polygon
    {
        /// <summary>
        /// - each chain element belongs to exactly one polygon.
        /// - multiple polygons are stored in the chain. (avoids copy during split)
        /// - if a vertex belongs to multple polygons, it has multiple chain elements with the same VertexId
        ///   the start of that chain is in the <see cref="vertexToChain"/>, the collision list is in SameVertexChain
        ///   the combination of PolygonId/VertexId is distinct.
        ///   during polygon triangulation, the maximum collision count is 3
        /// - a polygon has a specific chain element as start index
        /// - a polygon with holes has multiple chain start elements. They are joined via <see cref="PolygonSplitter.JoinHoleIntoPolygon(int, int)"/>
        /// </summary>
        private struct VertexChain
        {
            /// <summary>
            /// the index in <see cref="vertexCoordinates"/>
            /// </summary>
            public int VertexId;

            /// <summary>
            /// The id of the polygon. Holes are a separate polygon.
            /// </summary>
            public int SubPolygonId;

            /// <summary>
            /// The next info with the same vertex id.
            /// </summary>
            public int SameVertexChain;

            /// <summary>
            /// Gets the previous vertex id (not chain index)
            /// </summary>
            public int Prev { get; private set; }

            /// <summary>
            /// Gets the next chain index in the polygon (same polygon id)
            /// </summary>
            public int Next { get; private set; }

            /// <summary>
            /// Chain two items
            /// </summary>
            /// <param name="current">the id of the current item</param>
            /// <param name="nextChain">the id of the next item</param>
            /// <param name="nextItem">the data of the next item</param>
            public void SetNext(int current, int nextChain, ref VertexChain nextItem)
            {
                this.Next = nextChain;
                nextItem.Prev = current;
            }
        }
    }
}
