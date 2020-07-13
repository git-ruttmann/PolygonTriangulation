namespace PolygonTriangulation
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    /// <summary>
    /// A storage node with prev/next access
    /// </summary>
    /// <typeparam name="T">type of the stored data</typeparam>
    public interface IOrderedNode<T>
    {
        /// <summary>
        /// Gets the data of the node
        /// </summary>
        T Data { get; }

        /// <summary>
        /// Gets the next node
        /// </summary>
        IOrderedNode<T> Next { get; }

        /// <summary>
        /// Gets the previous node
        /// </summary>
        IOrderedNode<T> Prev { get; }
    };

    /// <summary>
    /// A binary search tree with O(n) autobalancing
    /// </summary>
    /// <typeparam name="T">type of the stored data</typeparam>
    public class RedBlackTree<T> : ICollection<T>
    {
        /// <summary>
        /// The comparer to use during insert / find operations
        /// </summary>
        private readonly IComparer<T> comparer;

        /// <summary>
        /// The toplevel node
        /// </summary>
        private Node root;

        /// <summary>
        /// Initializes a <see cref="RedBlackTree{T}"/>
        /// </summary>
        public RedBlackTree()
            : this(Comparer<T>.Default)
        {
        }

        /// <summary>
        /// Initializes a <see cref="RedBlackTree{T}"/> with a custom comparer.
        /// </summary>
        /// <param name="comparer">The comparer</param>
        public RedBlackTree(IComparer<T> comparer)
        {
            this.comparer = comparer;
        }

        ///<inheritdoc>/>
        public IEnumerable<T> Items => EnumerateSubItems(this.root);

        ///<inheritdoc>/>
        public int Count { get; private set; }

        ///<inheritdoc>/>
        bool ICollection<T>.IsReadOnly => false;

        /// <inheritdoc/>
        void ICollection<T>.Add(T item) => this.AddNode(item);

        /// <inheritdoc/>
        void ICollection<T>.Clear()
        {
            this.root = null;
            this.Count = 0;
        }

        /// <inheritdoc/>
        bool ICollection<T>.Contains(T item)
        {
            return this.TryLocateNode(item, out var _);
        }

        /// <inheritdoc/>
        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            foreach (var item in this.Items)
            {
                array[arrayIndex++] = item;
            }
        }

        /// <inheritdoc/>
        bool ICollection<T>.Remove(T item)
        {
            if (this.TryLocateNode(item, out var node) && node is var nodeInstance)
            {
                this.RemoveNode(nodeInstance);
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => this.Items.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)this).GetEnumerator();

        /// <summary>
        /// Dump the tree, one line per level
        /// </summary>
        /// <param name="maxDepth">the maximum tree level to report</param>
        /// <returns>one line per level</returns>
        internal IEnumerable<string> Dump(int maxDepth = -1)
        {
            return new DumpEnumerator(this, maxDepth);
        }

        /// <summary>
        /// For unittesting: mark certain levels as black, run on fully balanced tree only
        /// </summary>
        /// <param name="blackLevels">the levels that should be black, all others are marked red</param>
        internal void MarkBlack(params int[] blackLevels)
        {
            var levelSet = new HashSet<int>(blackLevels);
            MarkLevelRecursive(this.root, 0, levelSet);
        }

        /// <summary>
        /// Iterate over all items
        /// </summary>
        /// <param name="node">the start node</param>
        /// <returns>the data of the current item and all subitems</returns>
        private static IEnumerable<T> EnumerateSubItems(Node node)
        {
            if (node == null)
            {
                yield break;
            }

            foreach (var item in EnumerateSubItems(node.Left))
            {
                yield return item;
            }

            yield return node.Data;

            foreach (var item in EnumerateSubItems(node.Right))
            {
                yield return item;
            }
        }

        /// <summary>
        /// Add a new node
        /// </summary>
        /// <param name="value">the data for the new node</param>
        /// <returns>the new node as <see cref="IOrderedNode{T}"/></returns>
        public IOrderedNode<T> AddNode(T value)
        {
            var node = this.InsertNode(value);
            if (this.Count > 2)
            {
                this.FixTreeAfterInsertion(node);
            }

            return node;
        }

        /// <summary>
        /// Adds two adjacent values
        /// </summary>
        /// <param name="lower">the lower value</param>
        /// <param name="higher">the value that is exactly above lower</param>
        /// <returns>the lower node</returns>
        public (IOrderedNode<T> lower, IOrderedNode<T> upper) AddPair(T lower, T higher)
        {
            var lowerNode = this.InsertNode(lower);
            if (this.Count > 2)
            {
                this.FixTreeAfterInsertion(lowerNode);
            }

            var higherNode = new Node(higher);
            this.Count++;

            var node = lowerNode.Right;
            if (node == null)
            {
                lowerNode.SetRightChild(higherNode);
            }
            else
            {
                for (node = lowerNode.Right; node.Left != null; node = node.Left)
                {
                    // just iterate
                }

                node.SetLeftChild(higherNode);
            }

            this.FixTreeAfterInsertion(higherNode);
            return (lowerNode, higherNode);
        }

        /// <summary>
        /// Try to find the node with the value
        /// </summary>
        /// <param name="value">the value to find</param>
        /// <param name="node">The resulting node. null if not found</param>
        /// <returns>true if the value was found</returns>
        public bool TryLocateNode(T value, out IOrderedNode<T> node)
        {
            int comparison;
            for (var current = this.root; current != null; current = comparison > 0 ? current.Right : current.Left)
            {
                comparison = this.comparer.Compare(value, current.Data);
                if (comparison == 0)
                {
                    node = current;
                    return true;
                }
            }

            node = null;
            return false;
        }

        /// <summary>
        /// Remove a known node
        /// </summary>
        /// <param name="node">the node</param>
        public void RemoveNode(IOrderedNode<T> node)
        {
            if (node is Node nodeToDelete)
            {
                this.RemoveNode(nodeToDelete);
            }
            else
            {
                this.Remove(node.Data);
            }
        }

        /// <summary>
        /// Replace a known node with a value that would sort at the same position.
        /// </summary>
        /// <param name="node">the node</param>
        /// <param name="value">the new content</param>
        /// <returns>the new node</returns>
        public IOrderedNode<T> ReplaceNode(IOrderedNode<T> node, T value)
        {
            if (!(node is Node old))
            {
                this.Remove(node.Data);
                return this.AddNode(value);
            }

            var newNode = new Node(value);
            this.ReplaceAtParent(old, newNode);
            newNode.SetLeftChild(old.Left);
            newNode.SetRightChild(old.Right);
            newNode.IsRed = old.IsRed;
            return newNode;
        }

        /// <summary>
        /// Unittesting only: mark the level as black
        /// </summary>
        /// <param name="node">the node</param>
        /// <param name="level">the current level for the node</param>
        /// <param name="blackLevelSet">the black levels</param>
        private static void MarkLevelRecursive(Node node, int level, ISet<int> blackLevelSet)
        {
            if (node == null)
            {
                return;
            }

            node.IsRed = (level > 0) && !blackLevelSet.Contains(level);

            MarkLevelRecursive(node.Left, level + 1, blackLevelSet);
            MarkLevelRecursive(node.Right, level + 1, blackLevelSet);
        }

        /// <summary>
        /// Unittest only: Validate the tree structure
        /// </summary>
        /// <returns>true if the tree is valid</returns>
        internal bool Validate() => Validator.ValidateRoot(this.root);

        /// <summary>
        /// Resolve Red-Red conflicts after insert.
        /// </summary>
        /// <param name="node">the inserted node</param>
        private void FixTreeAfterInsertion(Node node)
        {
            var parent = node.Parent;
            var grandParent = parent?.Parent;
            if (grandParent == null)
            {
                if (this.root.IsRed)
                {
                    this.root.IsRed = false;
                }

                return;
            }

            var uncle = parent.Sibling;
            if (uncle != null && parent.IsRed && uncle.IsRed)
            {
                // propagate red from parent and it's sibling to the grand parent (that can't be red, otherwise uncle would already violate red-red)
                uncle.IsRed = false;
                parent.IsRed = false;
                grandParent.IsRed = true;
                this.FixTreeAfterInsertion(grandParent);
            }
            else if (uncle == null || parent.IsRed && !uncle.IsRed)
            {
                // use the non-red or empty uncle position by rotating
                var nodeIsLeft = node.IsLeft;
                grandParent.IsRed = true;
                if (parent.IsLeft == nodeIsLeft)
                {
                    parent.IsRed = false;
                    this.Rotate(!nodeIsLeft, grandParent);
                }
                else //// if (parent.IsLeft != nodeIsLeft)
                {
                    node.IsRed = false;
                    this.Rotate(!nodeIsLeft, parent);
                    this.Rotate(nodeIsLeft, grandParent);
                }
            }
        }

        /// <summary>
        /// Create a new node and insert it into the tree
        /// </summary>
        /// <param name="value">the data for the new node</param>
        /// <returns>the new node</returns>
        private Node InsertNode(T value)
        {
            var newNode = new Node(value);
            this.Count++;
            var parentNode = this.root;

            if (parentNode == null)
            {
                newNode.SetRoot(ref this.root);
                return newNode;
            }

            while (true)
            {
                var direction = this.comparer.Compare(value, parentNode.Data);
                var nextItem = direction < 0 ? parentNode.Left : parentNode.Right;
                if (nextItem == null)
                {
                    parentNode.SetChild(direction < 0, newNode);
                    newNode.IsRed = true;
                    break;
                }

                parentNode = nextItem;
            }

            return newNode;
        }

        /// <summary>
        /// Remove a value. Throws KeyNotFoundException.
        /// </summary>
        /// <param name="value">the value</param>
        public void Remove(T value)
        {
            if (!this.TryLocateNode(value, out var node))
            {
                throw new KeyNotFoundException();
            }

            this.RemoveNode((Node)node);
        }

        /// <summary>
        /// Removes the node by deleting it and then rebalancing the tree
        /// </summary>
        /// <param name="deleteNode">the node to delete</param>
        private void RemoveNode(Node deleteNode)
        {
            if (this.Count == 1)
            {
                this.root = null;
                this.Count = 0;
                return;
            }

            var replacementNode = this.DeleteNode(deleteNode);
            this.Count--;

            if (replacementNode != null)
            {
                // the top level node never accepts the double black state => node != null
                for (var node = replacementNode; node.IsDoubleBlack == true; node = node.Parent)
                {
                    this.ResolveSiblingOfDoubleBlack(node);
                }
            }
        }

        /// <summary>
        /// A node is double black after deletion. Try to compensate with it's sibling
        /// </summary>
        /// <param name="doubleBlackNode">the double black node</param>
        /// <remarks>
        /// Sibling must exist, otherwise node wouldn't have double-black. 
        /// If the sibling is red, it must have 2 black children because we had a black node below us.
        /// 
        /// If there is any red in the sibling or it's children, rotate that to the common top and flip it to black.
        /// 
        /// If neither sibling nor it's children has any red, then make the sibling red 
        /// => the sibling tree has the same black count. Instead the parent is now DoubleBlack.
        /// </remarks>
        private void ResolveSiblingOfDoubleBlack(Node doubleBlackNode)
        {
            var isLeft = doubleBlackNode.IsLeft;
            var sibling = doubleBlackNode.Sibling;

            if (sibling.IsRed)
            {
                sibling.Parent.IsRed = true;
                sibling.IsRed = false;
                this.Rotate(isLeft, sibling.Parent);

                // After rotation, the node and it's NEW sibling will both be black. 
                sibling = doubleBlackNode.Sibling;
            }

            var outerSiblingChild = isLeft ? sibling.Right : sibling.Left;
            if (outerSiblingChild?.IsRed == true)
            {
                this.Rotate(isLeft, sibling.Parent);
                sibling.IsRed = doubleBlackNode.Parent.IsRed;
                outerSiblingChild.IsRed = false;
            }
            else
            {
                var innerSiblingChild = isLeft ? sibling.Left : sibling.Right;
                if (innerSiblingChild?.IsRed == true)
                {
                    this.Rotate(!isLeft, sibling);
                    this.Rotate(isLeft, innerSiblingChild.Parent);
                    innerSiblingChild.IsRed = doubleBlackNode.Parent.IsRed;
                }
                else
                {
                    sibling.FlipRed();
                    doubleBlackNode.Parent.InheritDoubleBlackStateFromChild();
                }
            }

            doubleBlackNode.ResolveDoubleBlackState();
        }

        /// <summary>
        /// Delete the node. After the call, nothing in the tree will point at node. The node.Children and node.Parent are meaningless.
        /// The IsRed state of the node might change in case it has two children.
        /// If there is anything below the node, the node is replaced and the replaced value is returned.
        /// If there is no replacement, the node was black and not the root, a pseude node is inserted and returned to mark the position.
        /// </summary>
        /// <param name="node">the node to prepare for deletion</param>
        /// <returns>The replacement of the node. Can be null or a special node.</returns>
        private Node DeleteNode(Node node)
        {
            if (node.Left == null || node.Right == null)
            {
                if (node.IsTop)
                {
                    var newRoot = node.Left ?? node.Right;
                    newRoot.SetRoot(ref this.root);
                    return null;
                }

                return node.Parent.ReplaceDeletedChild(node.IsLeft, node, node.Left ?? node.Right);
            }

            // swap the node with it's next. The next is node.Right or node.Right[.Left]+
            var swapNode = node.Right.Left;
            if (swapNode == null)
            {
                swapNode = node.Right;
                var remainingRight = swapNode.Right;

                this.ReplaceAtParent(node, swapNode);
                swapNode.SwapRedFlag(node);
                swapNode.SetLeftChild(node.Left);

                return swapNode.ReplaceDeletedChild(false, node, remainingRight);
            }
            else
            {
                for (/*swapNode = swapnode */; swapNode.Left != null; swapNode = swapNode.Left)
                {
                }

                // Swap has a parent and is left to that. It has no left but maybe a right.
                var swapParent = swapNode.Parent;
                var remainingRight = swapNode.Right;

                swapNode.SwapRedFlag(node);
                this.ReplaceAtParent(node, swapNode);
                swapNode.SetLeftChild(node.Left);
                swapNode.SetRightChild(node.Right);

                return swapParent.ReplaceDeletedChild(true, node, remainingRight);
            }
        }

        /// <summary>
        /// Replace at parent or replace the root
        /// </summary>
        /// <param name="node">the node to replace</param>
        /// <param name="replaceWith">the new child of the parent</param>
        private void ReplaceAtParent(Node node, Node replaceWith)
        {
            if (node == this.root)
            {
                replaceWith.SetRoot(ref this.root);
            }
            else
            {
                node.Parent.SetChild(node.IsLeft, replaceWith);
            }
        }

        /// <summary>
        /// Rotate in any direction
        /// </summary>
        /// <param name="leftRotate">rotate to left</param>
        /// <param name="node">the node to rotate</param>
        /// <returns>the new parent node</returns>
        private void Rotate(bool leftRotate, Node node)
        {
            var parent = node.Parent;
            var isAtLeft = node.IsLeft;
            Node newTop;

            if (leftRotate)
            {
                newTop = node.Right;
                node.SetRightChild(newTop.Left);
                newTop.SetLeftChild(node);
            }
            else
            {
                newTop = node.Left;
                node.SetLeftChild(newTop.Right);
                newTop.SetRightChild(node);
            }

            if (parent != null)
            {
                parent.SetChild(isAtLeft, newTop);
            }
            else
            {
                newTop.SetRoot(ref this.root);
            }
        }

        /// <summary>
        /// Tree node
        /// </summary>
        [DebuggerDisplay("{Data}{ColorText}")]
        private class Node : IOrderedNode<T>
        {
            private enum Color
            {
                Red,
                Black,
                DoubleBlack,
                DoubleBlackNull,
            };

            private Color color;

            public Node(T data)
            {
                this.Data = data;
                this.color = Color.Red;
            }

            /// <summary>
            /// Gets a flag whether is is the root node
            /// </summary>
            public bool IsTop => this.Parent == null;

            /// <summary>
            /// Gets a flag whether this.parent.left == this
            /// </summary>
            public bool IsLeft => this.Parent?.Left == this;

            /// <summary>
            /// Gets a flag whether this.parent.right == this
            /// </summary>
            public bool IsRight => this.Parent?.Right == this;

            /// <summary>
            /// Get's the other node with the same parent
            /// </summary>
            public Node Sibling => this.Parent == null ? null : this.IsLeft ? this.Parent.Right : this.Parent.Left;

            /// <inheritdoc/>
            public T Data { get; }

            /// <summary>
            /// Gets or sets the red flag. false sets color to black
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
            /// For debug - get color as text
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
                        case Color.DoubleBlack:
                            return "b";
                        case Color.DoubleBlackNull:
                            return "x";
                        default:
                            return "_";
                    }

                }
            }

            /// <summary>
            /// Gets a value indicating a double black state.
            /// </summary>
            public bool IsDoubleBlack => this.color == Color.DoubleBlack || this.color == Color.DoubleBlackNull;

            public Node Parent { get; private set; }

            public Node Left { get; private set; }

            public Node Right { get; private set; }

            /// <inheritdoc/>
            public IOrderedNode<T> Next
            {
                get
                {
                    Node node;
                    if (this.Right != null)
                    {
                        for (node = this.Right; node.Left != null; node = node.Left)
                        {
                        }
                    }
                    else
                    {
                        for (node = this; node.IsRight; node = node.Parent)
                        {
                        }

                        node = node.Parent;
                    }

                    return node;
                }
            }

            /// <inheritdoc/>
            public IOrderedNode<T> Prev
            {
                get
                {
                    Node node;
                    if (this.Left != null)
                    {
                        for (node = this.Left; node.Right != null; node = node.Right)
                        {
                        }
                    }
                    else
                    {
                        for (node = this; node.IsLeft; node = node.Parent)
                        {
                        }

                        node = node.Parent;
                    }

                    return node;
                }
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
                            color = Color.DoubleBlackNull
                        };
                    }
                    else if (replacement.IsRed)
                    {
                        replacement.IsRed = false;
                    }
                    else
                    {
                        replacement.color = Color.DoubleBlack;
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
                    this.color = Color.Black;
                }
                else
                {
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
                    this.color = this.color == Color.Red ? Color.Black : Color.DoubleBlack;
                }
            }
        }

        /// <summary>
        /// Validation support
        /// </summary>
        private static class Validator
        {
            /// <summary>
            /// Validate the root node and all sibliings
            /// </summary>
            /// <param name="root">the root node</param>
            /// <returns>true if the tree is valid</returns>
            public static bool ValidateRoot(Node root)
            {
                if (root == null)
                {
                    return true;
                }

                if (!root.IsTop || root.IsRed)
                {
                    return false;
                }

                var blackHeight = 0;
                for (var node = root; node != null; node = node.Left)
                {
                    blackHeight += node.IsRed ? 0 : 1;
                }

                var isValid = ValidateRecursive(root.Left, 1, blackHeight, root) && ValidateRecursive(root.Right, 1, blackHeight, root);
                if (!isValid)
                {
                    throw new InvalidOperationException("tree validation failed");
                }

                return isValid;
            }

            /// <summary>
            /// Recursive helper for <see cref="Validate"/>
            /// </summary>
            /// <param name="node">the current node</param>
            /// <param name="blackLevel">the black level of the current node</param>
            /// <param name="blackHeight">the black height of the tree</param>
            /// <param name="parent">the parent of the node</param>
            /// <returns>true if the tree below node</returns>
            private static bool ValidateRecursive(Node node, int blackLevel, int blackHeight, Node parent)
            {
                if (node == null)
                {
                    if (blackLevel != blackHeight)
                    {
                        return false;
                    }

                    return true;
                }

                if (node.IsRed && parent.IsRed)
                {
                    return false;
                }

                if (node.IsDoubleBlack)
                {
                    return false;
                }

                blackLevel += node.IsRed ? 0 : 1;
                if (node.Parent != parent)
                {
                    return false;
                }

                return ValidateRecursive(node.Left, blackLevel, blackHeight, node)
                    && ValidateRecursive(node.Right, blackLevel, blackHeight, node);
            }
        }

        /// <summary>
        /// Dump enumeration support. Prints one line per tree level with proper spacing.
        /// </summary>
        private class DumpEnumerator : IEnumerable<string>
        {
            private readonly RedBlackTree<T> tree;
            private readonly int configuredDept;

            public DumpEnumerator(RedBlackTree<T> tree, int maxDepth)
            {
                this.tree = tree;
                this.configuredDept = maxDepth;
            }

            /// <inheritdoc/>
            public IEnumerator<string> GetEnumerator()
            {
                foreach (var line in this.Dump())
                {
                    yield return line;
                }
            }

            /// <inheritdoc/>
            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

            /// <summary>
            /// Create one line for each level and space the items suitable for 2 digit numbers
            /// </summary>
            /// <param name="maxDepth">the maximum depth</param>
            /// <returns>array of strings, one line per level</returns>
            private IEnumerable<string> Dump()
            {
                var maxDepth = this.configuredDept;
                if (maxDepth < 0)
                {
                    if (((ICollection<T>)this.tree).Count == 0)
                    {
                        return Enumerable.Empty<string>();
                    }

                    maxDepth = EnumerateLevels(this.tree.root, 0, -1).Select(x => x.level).Max();
                }

                var groups = EnumerateLevels(this.tree.root, 0, maxDepth)
                    .GroupBy(x => x.level)
                    .OrderBy(x => x.Key)
                    .ToArray();

                var height = groups.Length;
                var total = 1 << height;
                var itemLenght = 4 + 1;

                return groups.Select(g =>
                {
                    var spacingFactor = total / (1 << g.Key) / 2;
                    var spacing = new string(' ', (itemLenght + 1) * (spacingFactor - 1) + 1);
                    var left = new string(' ', (spacing.Length - 1) / 2);
                    var rest = string.Join(spacing, g.Select(x => ((Equals(x.data, default(T)) && !char.IsLower(x.color[0])) ? "- " : $"{x.data}{x.color}").PadLeft(itemLenght)));
                    return left + rest;
                });
            }

            /// <summary>
            /// Enumerate values, their red/black state and their depth. Used only to dump the tree.
            /// </summary>
            /// <param name="node">the current node</param>
            /// <param name="level">the level of node</param>
            /// <param name="maxDepth">the maximum allowed level</param>
            /// <returns>tuple of value, red/black state, node depth</returns>
            private static IEnumerable<(T data, string color, int level)> EnumerateLevels(Node node, int level, int maxDepth)
            {
                Node left;
                Node right;
                if (node == null)
                {
                    if (maxDepth < 0 || level > maxDepth)
                    {
                        yield break;
                    }

                    left = null;
                    right = null;
                    yield return (default(T), "_", level);
                }
                else
                {
                    left = node.Left;
                    right = node.Right;
                    yield return (node.Data, node.ColorText, level);
                }

                foreach (var item in EnumerateLevels(left, level + 1, maxDepth))
                {
                    yield return item;
                }

                foreach (var item in EnumerateLevels(right, level + 1, maxDepth))
                {
                    yield return item;
                }
            }
        }
    }
}
