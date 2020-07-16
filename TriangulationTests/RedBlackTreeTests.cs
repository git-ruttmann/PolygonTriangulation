namespace TriangulationTests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Security.AccessControl;
    using System.Text;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using PolygonTriangulation;

    [TestClass]
    public class RedBlackTreeTests
    {
        /// <summary>
        /// Insert multiple items so all red/black situations are covered
        /// </summary>
        [TestMethod]
        public void MultiInsertOnRight()
        {
            var tree = new RedBlackTree<int>();
            var items = new[] { 18, 7, 15, 16, 30, 25, 40, 60, 2, 1, 70 };

            foreach (var item in items)
            {
                tree.AddNode(item);
            }

            Assert.AreEqual("1 2 7 15 16 18 25 30 40 60 70", string.Join(" ", tree.Items));
        }

        /// <summary>
        /// Delete from a fully black, fully balanced tree.
        /// </summary>
        [TestMethod]
        public void DeleteFromAllBlack()
        {
            var tree = CreateBalancedFiveLevel();
            tree.MarkBlack(0, 1, 2, 3, 4);

            var itemsToRemove = tree.Items.OrderBy(x => x).ToArray();
            foreach (var item in itemsToRemove)
            {
                tree.Remove(item);
                Assert.IsTrue(tree.Validate());
            }
        }

        /// <summary>
        /// Delete from a fully black, fully balanced tree.
        /// </summary>
        [TestMethod]
        public void DeleteFromAllBlackReverse()
        {
            var tree = CreateBalancedFiveLevel();
            tree.MarkBlack(0, 1, 2, 3, 4);

            var itemsToRemove = tree.Items.OrderBy(x => x).Reverse().ToArray();
            foreach (var item in itemsToRemove)
            {
                tree.Remove(item);
                Assert.IsTrue(tree.Validate());
            }
        }

        /// <summary>
        /// Delete from a fully black, fully balanced tree.
        /// </summary>
        [TestMethod]
        public void VerifyPrevNext()
        {
            var tree = CreateBalancedFiveLevel();
            var items = tree.Items.ToArray();

            for (int i = 0; i < items.Length; i++)
            {
                Assert.IsTrue(tree.TryLocateNode(items[i], out var node));
                if (i == 0)
                {
                    Assert.IsNull(node.Prev);
                }
                else
                {
                    Assert.AreEqual(items[i - 1], node.Prev.Data);
                }

                if (i == items.Length - 1)
                {
                    Assert.IsNull(node.Next);
                }
                else
                {
                    Assert.AreEqual(
                        items[i + 1], 
                        node.Next.Data, 
                        $"Next of {items[i]} is {node.Next.Data} instead of {items[i + 1]}");
                }
            }
        }

        /// <summary>
        /// Delete a double black with outer child of sibling being red
        /// </summary>
        [TestMethod]
        public void DeleteWithOuterRightRed()
        {
            var tree = CreateBalancedFiveLevel();
            tree.MarkBlack(0, 1, 2, 3);

            Assert.IsTrue(VerifyTree(tree,
                "20B",
                "10B 30B",
                "4B 15B 24B 35B",
                "2B 7B 13B 18B 22B 27B 33B 38B",
                "1R 3R 6R 8R 11R 14R 16R 19R 21R 23R 26R 28R 31R 34R 36R 39R"));

            tree.Remove(1);
            tree.Remove(3);

            Assert.IsTrue(VerifyTree(tree,
                "20B",
                "10B 30B",
                "4B 15B 24B 35B",
                "2B 7B 13B 18B 22B 27B 33B 38B",
                "- - 6R 8R 11R 14R 16R 19R 21R 23R 26R 28R 31R 34R 36R 39R"));

            tree.Remove(2);

            Assert.IsTrue(VerifyTree(tree,
                "20B",
                "10B 30B",
                "7B 15B 24B 35B",
                "4B 8B 13B 18B 22B 27B 33B 38B",
                "- 6R - - 11R 14R 16R 19R 21R 23R 26R 28R 31R 34R 36R 39R"),
                "Right child of right sibling is red, should be resolved by left rotation");
        }

        /// <summary>
        /// Delete a double black with inner child of sibling being red
        /// </summary>
        [TestMethod]
        public void DeleteWithInnerRightRed()
        {
            var tree = CreateBalancedFiveLevel();
            tree.MarkBlack(0, 1, 2, 3);

            tree.Remove(1);
            tree.Remove(3);
            tree.Remove(8);

            Assert.IsTrue(VerifyTree(tree,
                "20B",
                "10B 30B",
                "4B 15B 24B 35B",
                "2B 7B 13B 18B 22B 27B 33B 38B",
                "- - 6R - 11R 14R 16R 19R 21R 23R 26R 28R 31R 34R 36R 39R"));

            tree.Remove(2);

            Assert.IsTrue(tree.Validate());
            Assert.IsTrue(VerifyTree(tree,
                "20B",
                "10B 30B",
                "6B 15B 24B 35B",
                "4B 7B 13B 18B 22B 27B 33B 38B",
                "- - - - 11R 14R 16R 19R 21R 23R 26R 28R 31R 34R 36R 39R"),
                "Left child of right sibling is red, should be resolved by lower right and then left rotation");
        }

        /// <summary>
        /// Delete a double black while the sibling of the delete node is red.
        /// </summary>
        [TestMethod]
        public void DeleteWithRedSiblingAndNoChilds()
        {
            var tree = CreateBalancedFiveLevel();
            tree.MarkBlack(0, 1, 2, 4);

            Assert.IsTrue(VerifyTree(tree,
                "20B",
                "10B 30B",
                "4B 15B 24B 35B",
                "2R 7R 13R 18R 22R 27R 33R 38R",
                "1B 3B 6B 8B 11B 14B 16B 19B 21B 23B 26B 28B 31B 34B 36B 39B"));

            tree.Remove(1);
            Assert.IsTrue(VerifyTree(tree,
                "20B",
                "10B 30B",
                "4B 15B 24B 35B",
                "2B 7R 13R 18R 22R 27R 33R 38R",
                "- 3R 6B 8B 11B 14B 16B 19B 21B 23B 26B 28B 31B 34B 36B 39B"),
                "Delete of black node at red parent must turn parent red and flip sibling");

            tree.Remove(3);
            Assert.IsTrue(VerifyTree(tree,
                "20B",
                "10B 30B",
                "4B 15B 24B 35B",
                "2B 7R 13R 18R 22R 27R 33R 38R",
                "- - 6B 8B 11B 14B 16B 19B 21B 23B 26B 28B 31B 34B 36B 39B"),
                "Delete of red node without sibling must keep everything else the same");

            tree.Remove(2);
            Assert.IsTrue(VerifyTree(tree,
                "20B",
                "10B 30B",
                "7B 15B 24B 35B",
                "4B 8B 13R 18R 22R 27R 33R 38R",
                "- 6R - - 11B 14B 16B 19B 21B 23B 26B 28B 31B 34B 36B 39B"),
                "Delete of black node with red sibling must rotate and update the inner child of the sibling");
        }

        /// <summary>
        /// Delete a double black while the sibling of the delete node is red.
        /// </summary>
        [TestMethod]
        public void DeleteWithRedSiblingWithChilds()
        {
            var tree = CreateBalancedFiveLevel();
            tree.MarkBlack(0, 1, 3, 4);

            Assert.IsTrue(VerifyTree(tree,
                "20B",
                "10B 30B",
                "4R 15R 24R 35R",
                "2B 7B 13B 18B 22B 27B 33B 38B",
                "1B 3B 6B 8B 11B 14B 16B 19B 21B 23B 26B 28B 31B 34B 36B 39B"));
            Assert.AreEqual("1 2 3 4 6 7 8 10 11 13 14 15 16 18 19 20 21 22 23 24 26 27 28 30 31 33 34 35 36 38 39", string.Join(" ", tree.Items));

            tree.Remove(2);
            Assert.IsTrue(VerifyTree(tree,
                "20B",
                "10B 30B",
                "4B 15R 24R 35R",
                "3B 7R 13B 18B 22B 27B 33B 38B",
                "1R - 6B 8B 11B 14B 16B 19B 21B 23B 26B 28B 31B 34B 36B 39B"),
                "Delete of black node at red parent must turn parent red and flip sibling and child");

            tree.Remove(7);
            Assert.IsTrue(VerifyTree(tree,
                "20B",
                "10B 30B",
                "4B 15R 24R 35R",
                "3B 8B 13B 18B 22B 27B 33B 38B",
                "1R - 6R - 11B 14B 16B 19B 21B 23B 26B 28B 31B 34B 36B 39B"),
                "Delete of black node must rotate and flip child to red");

            tree.Remove(1);
            tree.Remove(6);
            Assert.IsTrue(VerifyTree(tree,
                "20B",
                "10B 30B",
                "4B 15R 24R 35R",
                "3B 8B 13B 18B 22B 27B 33B 38B",
                "- - - - 11B 14B 16B 19B 21B 23B 26B 28B 31B 34B 36B 39B"),
                "Delete of red nodes must not change anything");

            Assert.AreEqual("3 4 8 10 11 13 14 15 16 18 19 20 21 22 23 24 26 27 28 30 31 33 34 35 36 38 39", string.Join(" ", tree.Items));

            tree.Remove(4);
            Assert.IsTrue(VerifyTree(tree,
                "20B",
                "15B 30B",
                "10B 18B 24R 35R",
                "8B 13R 16B 19B 22B 27B 33B 38B",
                "3R - 11B 14B - - - - 21B 23B 26B 28B 31B 34B 36B 39B"),
                "Delete of black node with red sibling must rotate the parent (10) and keep all siblings");
            Assert.AreEqual("3 8 10 11 13 14 15 16 18 19 20 21 22 23 24 26 27 28 30 31 33 34 35 36 38 39", string.Join(" ", tree.Items));
        }

        /// <summary>
        /// Replace a known node.
        /// </summary>
        [TestMethod]
        public void TestReplace()
        {
            var tree = CreateBalancedFourLevel();
            tree.MarkBlack(0, 1, 3);

            tree.TryLocateNode(15, out var node);
            tree.ReplaceNode(node, 16);

            Assert.IsTrue(VerifyTree(tree,
                "20B",
                "10B 30B",
                "4R 16R 24R 35R",
                "2B 7B 13B 18B 22B 27B 33B 38B"));
            Assert.AreEqual("2 4 7 10 13 16 18 20 22 24 27 30 33 35 38", string.Join(" ", tree.Items));
        }

        /// <summary>
        /// Insert two nodes below a black node. => must rotate and the children are red
        /// </summary>
        [TestMethod]
        public void TestInsertPairAtBlackWithoutSibling()
        {
            var tree = CreateBalancedFourLevel();
            tree.MarkBlack(0, 1, 3);
            tree.TryLocateNode(10, out var node);
            tree.ReplaceNode(node, 12);
            tree.TryLocateNode(2, out node);
            tree.ReplaceNode(node, 3);

            Assert.IsTrue(VerifyTree(tree,
                "20B",
                "12B 30B",
                "4R 15R 24R 35R",
                "3B 7B 13B 18B 22B 27B 33B 38B"));

            tree.AddPair(8, 9);
            Assert.IsTrue(VerifyTree(tree,
                "               20B",
                "       12B              30B",
                "    4R      15R     24R     35R",
                " 3B   8B  13B 18B 22B 27B 33B 38B",
                "- - 7R 9R - - - - - - - - - - - -"));

            tree.AddPair(1, 2);
            Assert.IsTrue(VerifyTree(tree,
                "                     20B",
                "            12B               30B",
                "       4R          15R     24R     35R",
                "   2B      8B    13B 18B 22B 27B 33B 38B",
                " 1R  3R  7R  9R - - - - - - - - - - - -"));
        }

        /// <summary>
        /// Insert two nodes below a black node. => must rotate and the children are red
        /// </summary>
        [TestMethod]
        public void SwapNodeDuringDelete()
        {
            var tree = CreateBalancedFourLevel();
            tree.MarkBlack(0, 1, 2);
            Assert.IsTrue(VerifyTree(tree,
                "             20B",
                "     10B             30B",
                "  4B     15B     24B     35B",
                "2R  7R 13R 18R 22R 27R 33R 38R"));

            tree.Remove(20);
            Assert.IsTrue(tree.Validate());

            Assert.IsTrue(VerifyTree(tree,
                "             22B",
                "     10B             30B",
                "  4B     15B     24B     35B",
                "2R  7R 13R 18R  -  27R 33R 38R"),
                "Deleted node must be swaped with the left most of the right");
        }

        /// <summary>
        /// Remove a black node by replacing it with a black leaf and resolve the double black state
        /// </summary>
        [TestMethod]
        public void ResolveDoubleBlack()
        {
            var tree = CreateBalancedFiveLevel();
            tree.MarkBlack(0, 1, 2, 4);

            tree.Remove(31);
            tree.Remove(34);
            tree.Remove(21);
            tree.Remove(23);

            Assert.IsTrue(VerifyTree(tree,
                "                             20B",
                "             10B                             30B",
                "      4B             15B             24B             35B",
                "  2R      7R     13R     18R     22B     27R     33B     38R",
                "1B  3B  6B  8B 11B 14B 16B 19B  -   -  26B 28B  -   -  36B 39B"));

            tree.Remove(30);
            Assert.IsTrue(tree.Validate());

            Assert.IsTrue(VerifyTree(tree,
                "                             20B",
                "            10B                             33B",
                "     4B            15B             24B               38B",
                "  2R      7R     13R     18R     22B     27R     35B     39B",
                "1B  3B  6B  8B 11B 14B 16B 19B  -   -  26B 28B  -  36R  -   - "),
                "Deleted node 33 is replaced with black node 33. Double Black resolve rotates around 35");
        }

        /// <summary>
        /// Resolve a double black state below the root. The root will not inherit the double black state.
        /// </summary>
        [TestMethod]
        public void ResolveDoubleBlackUpToRoot()
        {
            var tree = new RedBlackTree<int>();
            tree.AddNode(20);
            tree.AddNode(10);
            tree.AddNode(30);
            tree.MarkBlack(0, 1);

            Assert.IsTrue(VerifyTree(tree,
                "         20B",
                "   10B         30B"));

            tree.Remove(30);
            Assert.IsTrue(VerifyTree(tree,
                "         20B",
                "   10R          - "));
        }

        /// <summary>
        /// Delete a black leaf in a fully black tree => all other peers must swap from black to red
        /// </summary>
        [TestMethod]
        public void DeleteBlackLeafInBlackTree()
        {
            var tree = CreateBalancedFourLevel();
            tree.MarkBlack(0, 1, 2, 3);

            Assert.IsTrue(VerifyTree(tree,
                "             20B",
                "     10B             30B",
                "  4B     15B     24B     35B",
                "2B  7B 13B 18B 22B 27B 33B 38B"));

            tree.Remove(27);
            Assert.IsTrue(tree.Validate());

            Assert.IsTrue(VerifyTree(tree,
                "             20B",
                "     10R             30B",
                "  4B     15B     24B     35R",
                "2B  7B 13B 18B 22R  -  33B 38B"));
        }

        /// <summary>
        /// Delete a black node with one single red leaf. Delete consecutively on the same tree side to enforce rebalance.
        /// </summary>
        [TestMethod]
        public void DeleteNodesOnOneTreeSideToEnforceRebalance()
        {
            var tree = CreateBalancedFourLevel();
            tree.MarkBlack(0, 1, 2);
            tree.Remove(22);

            Assert.IsTrue(tree.Validate());
            Assert.IsTrue(VerifyTree(tree,
                "             20B",
                "     10B             30B",
                "  4B     15B     24B     35B",
                "2R  7R 13R 18R  -  27R 33R 38R"));

            tree.Remove(24);
            Assert.IsTrue(tree.Validate());
            Assert.IsTrue(VerifyTree(tree,
                "             20B",
                "     10B             30B",
                "  4B     15B     27B     35B",
                "2R  7R 13R 18R  -   -  33R 38R"),
                "Must replace with only child");

            tree.Remove(27);
            Assert.IsTrue(tree.Validate());
            Assert.IsTrue(VerifyTree(tree,
                "             20B",
                "     10B             35B",
                "  4B     15B     30B     38B",
                "2R  7R 13R 18R  -  33R  -   -"),
                "Must rotate parent of deleted black leaf when there is no child to compensate");

            tree.Remove(33);
            Assert.IsTrue(tree.Validate());
            Assert.IsTrue(VerifyTree(tree,
                "             20B",
                "     10B             35B",
                "  4B     15B     30B     38B",
                "2R  7R 13R 18R  -   -   -   -"),
                "Simple remove of red leaf");

            tree.Remove(30);
            Assert.IsTrue(tree.Validate());
            Assert.IsTrue(VerifyTree(tree,
                "             20B",
                "     10R             35B",
                "  4B     15B      -      38R",
                "2R  7R 13R 18R  -   -   -   -"),
                "Delete black leaf must rebalance the sibling 10");

            tree.Remove(38);
            Assert.IsTrue(tree.Validate());
            Assert.IsTrue(VerifyTree(tree,
                "             20B",
                "     10R             35B",
                "  4B     15B      -       -",
                "2R  7R 13R 18R  -   -   -   -"),
                "Simple remove of red leaf");

            tree.Remove(35);
            Assert.IsTrue(tree.Validate());
            Assert.IsTrue(VerifyTree(tree,
                "             10B",
                "     4B            15R",
                "  2R    7R     13B     20B",
                " -  -  -  -   -   -  18R  -"),
                "First rotate around parent(20) because sibling(10) is red. Then rotate around new parent (15) because new outer sibling (13) is red.");

            tree.Remove(20);
            Assert.IsTrue(VerifyTree(tree,
                "             10B",
                "     4B            15R",
                "  2R    7R     13B     18B"),
                "Remove parent of single leaf. Just replace it.");

            tree.Remove(15);
            Assert.IsTrue(VerifyTree(tree,
                "             10B",
                "     4B            18B",
                "  2R    7R     13R     -"),
                "Remove red node with two black leafes. Replace 15 by 18 and remove it.");
        }

        /// <summary>
        /// Delete a black leaf in a fully black tree => all other peers must swap from black to red
        /// </summary>
        [TestMethod]
        public void DeleteBlackNode()
        {
            var tree = CreateBalancedFourLevel();
            tree.MarkBlack(0, 2);
            tree.Remove(22);

            Assert.IsTrue(tree.Validate());
            Assert.IsTrue(VerifyTree(tree,
                "             20B",
                "     10R             30R",
                "  4B     15B     24B     35B",
                "2R  7R 13R 18R  -  27R 33R 38R"));
            tree.Remove(24);
            Assert.IsTrue(VerifyTree(tree,
                "             20B",
                "     10R             30R",
                "  4B     15B     27B     35B",
                "2R  7R 13R 18R  -   -  33R 38R"));

            tree = CreateBalancedFourLevel();
            tree.MarkBlack(0, 2);
            tree.Remove(24);
            Assert.IsTrue(VerifyTree(tree,
                "             20B",
                "     10R             30R",
                "  4B     15B     27B     35B",
                "2R  7R 13R 18R 22R  -  33R 38R"));
        }

        /// <summary>
        /// Insert two nodes. The first insert is rotated so it's not a leaf.
        /// The second must look up the insert position and there is a non-trivial rebalance of the parent.
        /// </summary>
        [TestMethod]
        public void TestInsertPairSecondNotAtLeaf()
        {
            var tree = CreateBalancedFourLevel();
            tree.MarkBlack(0, 1, 2);
            tree.TryLocateNode(20, out var node);
            tree.ReplaceNode(node, 21);
            tree.ReplaceNode(new PseudoNode(18), 20);

            Assert.IsTrue(VerifyTree(tree,
                "             21B",
                "     10B             30B",
                "  4B     15B     24B     35B",
                "2R  7R 13R 20R 22R 27R 33R 38R"));

            tree.AddNode(17);

            Assert.IsTrue(VerifyTree(tree,
                "             21B",
                "     10B               30B",
                "  4B      15R       24B     35B",
                "2R   7R 13B   20B 22R 27R 33R 38R",
                "- - - - - - 17R - - - - - - - - -"));

            tree.AddPair(18, 19);
            Assert.IsTrue(VerifyTree(tree,
                "             21B",
                "     15B                  30B",
                "  10R        18R       24B     35B",
                "  4B   13B 17B   20B 22R 27R 33R 38R",
                "2R  7R - - - - 19R - - - - - - - - -"));
        }

        /// <summary>
        /// Test access by non internal nodes
        /// </summary>
        [TestMethod]
        public void TestPseudoNodeAccess()
        {
            var tree = CreateBalancedFourLevel();
            tree.MarkBlack(0, 1, 2);

            Assert.IsTrue(VerifyTree(tree,
                "20B",
                "10B 30B",
                "4B 15B 24B 35B",
                "2R 7R 13R 18R 22R 27R 33R 38R"));

            tree.ReplaceNode(new PseudoNode(18), 19);
            Assert.IsTrue(VerifyTree(tree,
                "20B",
                "10B 30B",
                "4B 15B 24B 35B",
                "2R 7R 13R 19R 22R 27R 33R 38R"));

            tree.RemoveNode(new PseudoNode(22));
            Assert.IsTrue(VerifyTree(tree,
                "20B",
                "10B 30B",
                "4B 15B 24B 35B",
                "2R 7R 13R 19R - 27R 33R 38R"));
        }

        /// <summary>
        /// Insert two nodes below a red node. => must rotate and adjust the red state
        /// </summary>
        [TestMethod]
        public void TestInsertPairAtRedWithoutSiblingButUncle()
        {
            var tree = CreateBalancedFourLevel();
            tree.MarkBlack(0, 1, 2);
            tree.TryLocateNode(10, out var node);
            tree.ReplaceNode(node, 12);

            Assert.IsTrue(VerifyTree(tree,
                "20B",
                "12B 30B",
                "4B 15B 24B 35B",
                "2R 7R 13R 18R 22R 27R 33R 38R"));

            Assert.IsTrue(tree.Validate());
            tree.AddPair(8, 9);
            Assert.IsTrue(tree.Validate());
            Assert.IsTrue(VerifyTree(tree,
                "                     20B",
                "            12B               30B",
                "       4R          15B     24B     35B",
                " 2B    8B    13R 18R 22R 27R 33R 38R",
                "- -  7R  9R  - - - - - - - - - - - -"));
        }

        /// <summary>
        /// Insert two nodes below a red node. => must rotate and adjust the red state
        /// </summary>
        [TestMethod]
        public void TestInsertPairAtRedWithoutSiblingAndNoUncleLeft()
        {
            var tree = CreateBalancedFourLevel();
            tree.MarkBlack(0, 1, 2);
            tree.TryLocateNode(10, out var node);
            tree.ReplaceNode(node, 12);
            tree.Remove(13);
            tree.Remove(2);

            Assert.IsTrue(VerifyTree(tree,
                "            20B",
                "     12B            30B",
                "   4B    15B     24B     35B",
                " -  7R  -  18R 22R 27R 33R 38R"));

            Assert.IsTrue(tree.Validate());
            tree.AddPair(8, 9);
            Assert.IsTrue(tree.Validate());
            Assert.IsTrue(VerifyTree(tree,
                "                20B",
                "         12B                30B",
                "    7R          15B     24B     35B",
                " 4B    8B      -  18R 22R 27R 33R 38R",
                "- -   -  9R   - - - - - - - - - - - -"));
        }

        /// <summary>
        /// Insert two nodes below a red node. => must rotate and adjust the red state
        /// </summary>
        [TestMethod]
        public void TestInsertPairAtRedWithoutSiblingAndNoUncleRight()
        {
            var tree = CreateBalancedFourLevel();
            tree.MarkBlack(0, 1, 2);
            tree.TryLocateNode(2, out var node);
            tree.ReplaceNode(node, 3);
            tree.Remove(13);
            tree.Remove(7);

            Assert.IsTrue(VerifyTree(tree,
                "            20B",
                "     10B            30B",
                "   4B    15B     24B     35B",
                " 3R  -  -  18R 22R 27R 33R 38R"));

            Assert.IsTrue(tree.Validate());
            tree.AddPair(1, 2);
            Assert.IsTrue(tree.Validate());
            Assert.IsTrue(VerifyTree(tree,
                "                20B",
                "         10B                30B",
                "    3R          15B     24B     35B",
                " 1B     4B     -  18R 22R 27R 33R 38R",
                "- 2R   -  -   - - - - - - - - - - - -"));
        }

        /// <summary>
        /// Insert two nodes below a red node. => must rotate and adjust the red state. The parent.parent is also red and must flip to black.
        /// </summary>
        [TestMethod]
        public void TestInsertPairAtRedWithoutSiblingAndNoUncleRightAndRedAbove()
        {
            var tree = CreateBalancedFourLevel();
            tree.MarkBlack(0, 2);
            tree.TryLocateNode(2, out var node);
            tree.ReplaceNode(node, 3);
            tree.Remove(13);
            tree.Remove(7);

            Assert.IsTrue(VerifyTree(tree,
                "            20B",
                "     10R            30R",
                "   4B    15B     24B     35B",
                " 3R  -  -  18R 22R 27R 33R 38R"));

            Assert.IsTrue(tree.Validate());
            tree.AddPair(1, 2);
            Assert.IsTrue(tree.Validate());
            Assert.IsTrue(VerifyTree(tree,
                "                20B",
                "         10B                30B",
                "    3R          15B     24B     35B",
                " 1B     4B     -  18R 22R 27R 33R 38R",
                "- 2R   -  -   - - - - - - - - - - - -"));
        }

        /// <summary>
        /// Insert two nodes below a red node. => must rotate and adjust the red state. The parent.parent is also red and must flip to black.
        /// </summary>
        [TestMethod]
        public void Fun()
        {
            var tree = new RedBlackTree<int>();
            tree.AddNode(10);
            tree.AddNode(5);
            tree.AddNode(15);
            tree.AddNode(77);

            Assert.IsTrue(tree.Validate());
            Assert.IsTrue(VerifyTree(tree,
                "            10B",
                "      5B            15B",
                "   -      -       -      77R",
                " -   -  -   -   -   -   -   - "));

            tree.AddPair(20, 25);
            Assert.IsTrue(tree.Validate());
            Assert.IsTrue(VerifyTree(tree,
                "            10B",
                "      5B            20R",
                "   -      -      15B     77B",
                " -   -  -   -   -   -  25R  - "));

            tree.AddPair(27, 29);
            Assert.IsTrue(tree.Validate());
            Assert.IsTrue(VerifyTree(tree,
                "            20B",
                "     10R            27R",
                "   5B    15B     25B     77B",
                " -   -  -   -   -   -  29R  - "));
        }

        /// <summary>
        /// Insert two nodes as sibling to a red node. Must adjust red flag of parent and propagate red flag of parent.
        /// </summary>
        [TestMethod]
        public void TestInsertPairWithRedSibling()
        {
            var tree = CreateBalancedFourLevel();
            tree.MarkBlack(0, 2);
            tree.Remove(13);

            Assert.IsTrue(VerifyTree(tree,
                "20B",
                "10R 30R",
                "   4B       15B      24B     35B",
                " 2R  7R   -    18R 22R 27R 33R 38R"));

            tree.AddPair(11, 12);
            Assert.IsTrue(VerifyTree(tree,
                "20B",
                "10B 30B",
                "   4B       15R      24B     35B",
                " 2R  7R  11B   18B 22R 27R 33R 38R",
                "- - - - - 12R  - - - - - - - - - -"));
        }

        /// <summary>
        /// Test the ICollection{T} level of a red black tree.
        /// </summary>
        [TestMethod]
        public void TestCollection()
        {
            var tree = new RedBlackTree<int>();
            var treeCollection = tree as ICollection<int>;
            treeCollection.Add(7);
            treeCollection.Add(12);
            treeCollection.Add(9);
            treeCollection.Add(3);
            treeCollection.Add(10);

            Assert.IsFalse(treeCollection.IsReadOnly);

            Assert.AreEqual(5, treeCollection.Count);
            Assert.IsTrue(treeCollection.Contains(7));
            Assert.IsFalse(treeCollection.Contains(1));

            Assert.IsTrue(treeCollection.Remove(7), "Must remove it");
            Assert.IsFalse(treeCollection.Remove(7), "Must not remove it twice");
            Assert.AreEqual(4, treeCollection.Count, "Count must be lower");
            treeCollection.Add(7);
            Assert.AreEqual(5, treeCollection.Count, "Must add element 7 it again");

            CollectionAssert.AreEqual(new[] { 3, 7, 9, 10, 12 }, treeCollection.ToArray());
            Assert.AreEqual("3 7 9 10 12", string.Join(" ", tree), "Access to typed enumeration must list items correctly");

            var treeEnumerator = ((IEnumerable)tree).GetEnumerator();
            treeEnumerator.MoveNext();
            Assert.AreEqual(3, treeEnumerator.Current, "Access to default enumeration must list items correctly");

            var arrayTarget = new int[7];
            arrayTarget[0] = 1;
            treeCollection.CopyTo(arrayTarget, 1);
            CollectionAssert.AreEqual(new[] { 1, 3, 7, 9, 10, 12, 0 }, arrayTarget, "CopyTo must access the correct range");

            treeCollection.Clear();
            Assert.AreEqual(0, treeCollection.Count, "Collection must be empty");
            Assert.AreEqual(0, tree.Dump().Count());

            try
            {
                tree.Remove(10);
                Assert.IsFalse(true, "May not reach here");
            }
            catch (KeyNotFoundException)
            {
                // must throw that exception.
            }
        }

        /// <summary>
        /// Insert two nodes below a black node. => must rotate and the children are red
        /// </summary>
        [TestMethod]
        public void TestValidator()
        {
            var tree = CreateBalancedFourLevel();
            tree.MarkBlack(1, 2, 3);
            Assert.IsFalse(tree.Validate(), "Red root is invalid");
            var topLine = tree.Dump().First();
            Assert.AreEqual("20R", topLine.Trim());

            try
            {
                tree = CreateBalancedFourLevel();
                tree.MarkBlack(0, 3);
                Assert.IsFalse(tree.Validate(), "Two consecutive red leves are invalid");
                Assert.Fail("Must not reach here as bad validation throws an error");
            }
            catch (InvalidOperationException)
            {
                // Must catch that exception.
            }

            try
            {
                tree = CreateBalancedFourLevel();
                tree.Remove(22);
                tree.MarkBlack(0, 2, 3);
                Assert.IsFalse(tree.Validate(), "Different black depth must be detected");
                Assert.Fail("Must not reach here as bad validation throws an error");
            }
            catch (InvalidOperationException)
            {
                // Must catch that exception.
            }
        }

        private static RedBlackTree<int> CreateBalancedFiveLevel()
        {
            var tree = new RedBlackTree<int>();
            var items = new[] 
            { 
                20,
                10, 30,
                4, 15, 24, 35,
                2, 7, 13, 18, 22, 27, 33, 38,
                1, 3, 6, 8, 11, 14, 16, 19, 21, 23, 26, 28, 31, 34, 36, 39
            };

            foreach (var item in items)
            {
                tree.AddNode(item);
            }

            Assert.IsTrue(VerifyTree(tree,
                "20B",
                "10B 30B",
                "4R 15R 24R 35R",
                "2B 7B 13B 18B 22B 27B 33B 38B",
                "1R 3R 6R 8R 11R 14R 16R 19R 21R 23R 26R 28R 31R 34R 36R 39R"));

            Assert.AreEqual("1 2 3 4 6 7 8 10 11 13 14 15 16 18 19 20 21 22 23 24 26 27 28 30 31 33 34 35 36 38 39", string.Join(" ", tree.Items));

            return tree;
        }

        private static RedBlackTree<int> CreateBalancedFourLevel()
        {
            var tree = new RedBlackTree<int>();
            var items = new[] 
            { 
                20,
                10, 30,
                4, 15, 24, 35,
                2, 7, 13, 18, 22, 27, 33, 38,
            };

            foreach (var item in items)
            {
                tree.AddNode(item);
            }

            Assert.IsTrue(VerifyTree(tree,
                "20B",
                "10R 30R",
                "4B 15B 24B 35B",
                "2R 7R 13R 18R 22R 27R 33R 38R"));
            Assert.AreEqual("2 4 7 10 13 15 18 20 22 24 27 30 33 35 38", string.Join(" ", tree.Items));

            return tree;
        }

        private static bool VerifyTree(RedBlackTree<int> tree, params string[] lines)
        {
            foreach (var item in tree.Dump(lines.Length).Zip(lines, Tuple.Create))
            {
                var actual = item.Item1.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                var request = item.Item2.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (request.Length != actual.Length)
                {
                    return false;
                }

                if (!request.Zip(actual, (r, a) => r == a).All(x => x))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// A custom IOrderedNode implementation to trigger the "not internal type" code of the red black tree
        /// </summary>
        /// <remarks>
        /// The Prev and Next implementation is utter nonsense.
        /// Sonar should do better on unit test mocks.
        /// </remarks>
        private class PseudoNode : IOrderedNode<int>
        {
            public PseudoNode(int data)
            {
                this.Data = data;
            }

            /// <inheritdoc/>
            public int Data { get; }

            /// <inheritdoc/>
            public IOrderedNode<int> Next => new PseudoNode(this.Data + 1);

            /// <inheritdoc/>
            public IOrderedNode<int> Prev => new PseudoNode(this.Data - 1);
        }
    }
}
