namespace PolygonTriangulation
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// subclass container for redblacktree
    /// </summary>
    public sealed partial class RedBlackTree<T>
    {
        /// <summary>
        /// Tree node
        /// </summary>
        [DebuggerDisplay("{Data}{ColorText}")]
        private class Node : IOrderedNode<T>
        {
            private Color color;

            /// <summary>
            /// Initializes a new instance of the <see cref="Node"/> class.
            /// </summary>
            /// <param name="data">The data associated with the node.</param>
            public Node(T data)
            {
                this.Data = data;
                this.color = Color.Red;
            }

            /// <summary>
            /// The color of the node.
            /// </summary>
            private enum Color
            {
                /// <summary>
                /// Node is red - do not count for black height
                /// </summary>
                Red,

                /// <summary>
                /// Node is black - count for black height and rebalance on delete
                /// </summary>
                Black,

                /// <summary>
                /// Node is double black. Can be inherited from child only.
                /// </summary>
                DoubleBlackNode,

                /// <summary>
                /// A black leaf was deleted, rebalance the parent tree and remove this node afterwards.
                /// </summary>
                DoubleBlackNull,
            }

            /// <summary>
            /// Gets a value indicating whether is is the root node
            /// </summary>
            public bool IsTop => this.Parent == null;

            /// <summary>
            /// Gets a value indicating whether this.parent.left == this
            /// </summary>
            public bool IsLeft => this.Parent?.Left == this;

            /// <summary>
            /// Gets a value indicating whether this.parent.right == this
            /// </summary>
            public bool IsRight => this.Parent?.Right == this;

            /// <inheritdoc/>
            public T Data { get; }

            /// <summary>
            /// Gets or sets a value indicating whether the node is red (true) or black (false)
            /// </summary>
            public bool IsRed
            {
                get => this.color == Color.Red;
                set
                {
                    if (this.color == Color.DoubleBlackNull)
                    {
                        throw new InvalidOperationException("Can't change the DeletedBlack color");
                    }

                    this.color = value ? Color.Red : Color.Black;
                }
            }

            /// <summary>
            /// Gets a value indicating whether the state is double black.
            /// </summary>
            public bool IsDoubleBlack => this.color == Color.DoubleBlackNode || this.color == Color.DoubleBlackNull;

            /// <summary>
            /// Gets the parent node.
            /// </summary>
            public Node Parent { get; private set; }

            /// <summary>
            /// Gets the left node.
            /// </summary>
            public Node Left { get; private set; }

            /// <summary>
            /// Gets the right node.
            /// </summary>
            public Node Right { get; private set; }

            /// <inheritdoc/>
            public IOrderedNode<T> NextNode
            {
                get
                {
                    Node node;
                    if (this.Right != null)
                    {
                        for (node = this.Right; node.Left != null; node = node.Left)
                        {
                            // just iterate
                        }
                    }
                    else
                    {
                        for (node = this; node.IsRight; node = node.Parent)
                        {
                            // just iterate
                        }

                        node = node.Parent;
                    }

                    return node;
                }
            }

            /// <inheritdoc/>
            public IOrderedNode<T> PrevNode
            {
                get
                {
                    Node node;
                    if (this.Left != null)
                    {
                        for (node = this.Left; node.Right != null; node = node.Right)
                        {
                            // just iterate
                        }
                    }
                    else
                    {
                        for (node = this; node.IsLeft; node = node.Parent)
                        {
                            // just iterate
                        }

                        node = node.Parent;
                    }

                    return node;
                }
            }

            /// <summary>
            /// Gets the color as text - debug only.
            /// </summary>
            internal string ColorText
            {
                get
                {
                    switch (this.color)
                    {
                        case Color.Red:
                            return "R";
                        case Color.Black:
                            return "B";
                        case Color.DoubleBlackNode:
                            return "b";
                        case Color.DoubleBlackNull:
                            return "x";
                        default:
                            return "_";
                    }
                }
            }

            /// <summary>
            /// Get's the other node with the same parent
            /// </summary>
            /// <returns>the sibling of the item</returns>
            public Node GetSibling()
            {
                if (this.Parent == null)
                {
                    return null;
                }

                return this.IsLeft ? this.Parent.Right : this.Parent.Left;
            }

            /// <summary>
            /// Set this node as root node
            /// </summary>
            /// <param name="root">the root variable</param>
            public void SetRoot(ref Node root)
            {
                root = this;
                this.Parent = null;
                this.color = Color.Black;
            }

            /// <summary>
            /// Flip the red flag
            /// </summary>
            public void FlipRed()
            {
                this.IsRed = !this.IsRed;
            }

            /// <summary>
            /// Set the left child. Same as SetChild(true, child)
            /// </summary>
            /// <param name="child">the new child</param>
            public void SetLeftChild(Node child)
            {
                this.Left = child;
                if (child != null)
                {
                    child.Parent = this;
                }
            }

            /// <summary>
            /// Set the right child. Same as SetChild(false, child)
            /// </summary>
            /// <param name="child">the new child</param>
            public void SetRightChild(Node child)
            {
                this.Right = child;
                if (child != null)
                {
                    child.Parent = this;
                }
            }

            /// <summary>
            /// Set the child node either as left or right
            /// </summary>
            /// <param name="atLeft">choose left or right node</param>
            /// <param name="child">the new child</param>
            public void SetChild(bool atLeft, Node child)
            {
                if (atLeft)
                {
                    this.Left = child;
                }
                else
                {
                    this.Right = child;
                }

                if (child != null)
                {
                    child.Parent = this;
                }
            }

            /// <summary>
            /// Exchanges this color with the color of the peer
            /// </summary>
            /// <param name="peer">the other</param>
            public void SwapRedFlag(Node peer)
            {
                if (this.color != peer.color)
                {
                    peer.color = this.color;
                    this.FlipRed();
                }
            }

            /// <summary>
            /// Replace the deleted child with a new value.
            /// Create a special value for double black situations.
            /// Handles red/black/doubleblack state of replaced child.
            /// </summary>
            /// <param name="atLeft">the flag if child is left of it's parent.</param>
            /// <param name="child">the child to replace</param>
            /// <param name="replacement">the replacement node (a child of child)</param>
            /// <returns>The replaced valued. If child.Black + replacement==null, a special value</returns>
            public Node ReplaceDeletedChild(bool atLeft, Node child, Node replacement)
            {
                if (!child.IsRed)
                {
                    if (replacement == null)
                    {
                        replacement = new Node(default)
                        {
                            color = Color.DoubleBlackNull,
                        };
                    }
                    else if (replacement.IsRed)
                    {
                        replacement.IsRed = false;
                    }
                    else
                    {
                        throw new InvalidOperationException("replacement can't be black because replacement.parent.left == null");
                    }
                }
                else if (replacement?.IsRed == false)
                {
                    throw new InvalidOperationException("child and replacement are red, violated precondition");
                }

                this.SetChild(atLeft, replacement);
                return replacement;
            }

            /// <summary>
            /// Remove the double black state
            /// </summary>
            public void ResolveDoubleBlackState()
            {
                if (!this.IsDoubleBlack)
                {
                    throw new InvalidOperationException("Don't call double black operation if not double black");
                }

                if (this.Parent == null)
                {
                    throw new InvalidOperationException("The parent should never take the double black state");
                }

                if (this.Parent.IsRed)
                {
                    this.Parent.IsRed = false;
                }

                if (this.color == Color.DoubleBlackNull)
                {
                    this.Parent.SetChild(this.IsLeft, null);
                }
                else
                {
                    this.color = Color.Black;
                }
            }

            /// <summary>
            /// The child has a double black state and can't resolve it. Take it over.
            /// </summary>
            /// <remarks>
            /// The root node doesn't need to take double black.
            /// </remarks>
            public void InheritDoubleBlackStateFromChild()
            {
                if (this.Parent != null)
                {
                    this.color = this.color == Color.Red ? Color.Black : Color.DoubleBlackNode;
                }
            }
        }
    }
}
