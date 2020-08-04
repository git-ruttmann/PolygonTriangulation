namespace PolygonTriangulation
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// subclass container for polygon
    /// </summary>
    public partial class Polygon
    {
        /// <summary>
        /// An enumerable that creates a <see cref="NextChainEnumerator"/>
        /// </summary>
        private class NextChainEnumerable : IEnumerable<int>
        {
            private readonly int start;
            private readonly IReadOnlyList<VertexChain> chain;

            /// <summary>
            /// Initializes a new instance of the <see cref="NextChainEnumerable" /> class.
            /// </summary>
            /// <param name="start">The start.</param>
            /// <param name="chain">The chain.</param>
            public NextChainEnumerable(int start, IReadOnlyList<VertexChain> chain)
            {
                this.start = start;
                this.chain = chain;
            }

            /// <inheritdoc/>
            public IEnumerator<int> GetEnumerator() => new NextChainEnumerator(this.start, this.chain);

            /// <inheritdoc/>
            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

            /// <summary>
            /// Internal enumerator
            /// </summary>
            private sealed class NextChainEnumerator : IEnumerator<int>
            {
                private readonly int start;
                private readonly IReadOnlyList<VertexChain> chain;
                private bool reset;
#if DEBUG
                private int maxIteratorCount;
#endif

                /// <summary>
                /// Initializes a new instance of the <see cref="NextChainEnumerator" /> class.
                /// </summary>
                /// <param name="start">The start.</param>
                /// <param name="chain">The chain.</param>
                public NextChainEnumerator(int start, IReadOnlyList<VertexChain> chain)
                {
                    this.start = start;
                    this.chain = chain;
                    this.reset = true;
#if DEBUG
                    this.maxIteratorCount = chain.Count;
#endif
                }

                /// <inheritdoc/>
                public int Current { get; private set; }

                /// <inheritdoc/>
                object IEnumerator.Current => this.Current;

                /// <inheritdoc/>
                public void Dispose()
                {
                    this.Current = -1;
                }

                /// <inheritdoc/>
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

                /// <inheritdoc/>
                public void Reset()
                {
                    this.reset = true;
                }
            }
        }
    }
}
