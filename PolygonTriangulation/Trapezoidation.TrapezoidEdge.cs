namespace PolygonTriangulation
{
    /// <summary>
    /// subclass container for trapzoidation
    /// </summary>
    public partial class Trapezoidation
    {
        /// <summary>
        /// Internal representation of an edge
        /// </summary>
        private class TrapezoidEdge : ITestingTrapezoidEdge
        {
            public TrapezoidEdge(int left, int right, int rightUnique, bool isRightToLeft)
            {
                this.IsRightToLeft = isRightToLeft;
                this.Left = left;
                this.Right = right;
                this.RightUnique = rightUnique;
            }

            /// <summary>
            /// Gets a value indicating whether this instance is right to left.
            /// </summary>
            public bool IsRightToLeft { get; }

            /// <summary>
            /// Gets the left vertex id.
            /// </summary>
            public int Left { get; }

            /// <summary>
            /// Gets the right vertex id.
            /// </summary>
            public int Right { get; }

            /// <summary>
            /// Gets the unique id of the right vertex
            /// </summary>
            public int RightUnique { get; }

            /// <summary>
            /// Gets or sets the node in the red black tree
            /// </summary>
            public IOrderedNode<TrapezoidEdge> TreeNode { get; set; }

            /// <summary>
            /// Gets or sets the current associated trapezoid.
            /// </summary>
            public Trapezoid Trapezoid { get; set; }

            /// <inheritdoc/>
            public override string ToString()
            {
                return $"{this.Left}{(this.IsRightToLeft ? "<" : ">")}{this.Right}";
            }
        }
    }
}
