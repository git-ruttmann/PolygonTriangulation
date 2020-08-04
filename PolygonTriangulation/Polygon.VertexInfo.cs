namespace PolygonTriangulation
{
    using System.Diagnostics;

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
        int NextVertexId { get; }

        /// <summary>
        /// Gets the id of the previous vertex
        /// </summary>
        int PrevVertexId { get; }

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
    /// subclass container for polygon
    /// </summary>
    public partial class Polygon
    {
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
                var prev = this.PrevVertexId;
                var next = this.NextVertexId;
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
            public int NextVertexId => this.chain[this.chain[this.element].Next].VertexId;

            /// <inheritdoc/>
            public int PrevVertexId => this.chain[this.chain[this.element].Prev].VertexId;

            /// <inheritdoc/>
            public int Unique => this.element;

            /// <inheritdoc/>
            public int NextUnique => this.chain[this.element].Next;

            /// <inheritdoc/>
            public int PrevUnique => this.chain[this.element].Prev;
        }
    }
}
