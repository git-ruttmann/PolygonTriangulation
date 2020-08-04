namespace PolygonTriangulation
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// subclass container for redblacktree
    /// </summary>
    public sealed partial class RedBlackTree<T>
    {
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

            /// <summary>
            /// Create one line for each level and space the items suitable for 2 digit numbers
            /// </summary>
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
                    var spacing = new string(' ', ((itemLenght + 1) * (spacingFactor - 1)) + 1);
                    var left = new string(' ', (spacing.Length - 1) / 2);
                    var rest = string.Join(spacing, g.Select(x => ((Equals(x.data, default(T)) && !char.IsLower(x.color[0])) ? "- " : $"{x.data}{x.color}").PadLeft(itemLenght)));
                    return left + rest;
                });
            }
        }
    }
}
