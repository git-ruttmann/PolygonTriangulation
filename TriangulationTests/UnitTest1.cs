namespace TriangulationTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    
    using Vertex = System.Numerics.Vector2;
    using Plane = System.Numerics.Plane;
    using Vector3 = System.Numerics.Vector3;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using PolygonTriangulation;

    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void SortedEdges()
        {
            var vertices = new[]
            {
                new Vertex(1, 1), // 0
                new Vertex(1, 3),
                new Vertex(1.5f, 3), // 2
                new Vertex(2, 2),
                new Vertex(2, 4), // 4
                new Vertex(2.5f, 1),
                new Vertex(2.5f, 2), // 6
                new Vertex(2.5f, 3),
                new Vertex(3.5f, 2.5f), // 8
                new Vertex(3.5f, 1),
                new Vertex(4, 1.5f), // 10
                new Vertex(4, 3.5f),
                new Vertex(4, 4), // 12
            };

            var sorted = new SortedActiveEdgeList<int>(vertices);
            var (b0, b0Upper) = sorted.Begin(0, 5, 6);
            Assert.IsTrue(VerifySortedEdges(b0, 0,5, 0,6));

            var (b1, b1Upper) = sorted.Begin(1, 3, 4);
            Assert.IsTrue(VerifySortedEdges(b0, 0,5, 0,6, 1,3, 1,4));

            var (b2, b2Upper) = sorted.Begin(2, 7, 12);
            Assert.IsTrue(VerifySortedEdges(b0, 0,5, 0,6, 1,3, 2,7, 2,12, 1,4));

            var t3 = sorted.Transition(b1, 6);
            Assert.IsTrue(VerifySortedEdges(b0, 0,5, 0,6, 3,6, 2,7, 2,12, 1,4));

            var t4 = sorted.Transition(b1Upper, 12);
            Assert.IsTrue(VerifySortedEdges(b0, 0,5, 0,6, 3,6, 2,7, 2,12, 4,12));

            var t5 = sorted.Transition(b0, 9);
            Assert.IsTrue(VerifySortedEdges(t5, 5,9, 0,6, 3,6, 2,7, 2,12, 4,12));

            sorted.Finish(b0Upper);
            Assert.IsTrue(VerifySortedEdges(t5, 5,9, 2,7, 2,12, 4,12));

            var t7 = sorted.Transition(b2, 11);
            Assert.IsTrue(VerifySortedEdges(t5, 5,9, 7,11, 2,12, 4,12));

            var (b8, b8Upper) = sorted.Begin(8, 10, 11);
            Assert.IsTrue(VerifySortedEdges(t5, 5,9, 8,10, 8,11, 7,11, 2,12, 4,12));

            var t9 = sorted.Transition(t5, 10);
            Assert.IsTrue(VerifySortedEdges(t9, 9,10, 8,10, 8,11, 7,11, 2,12, 4,12));

            sorted.Finish(t9);
            Assert.IsTrue(VerifySortedEdges(t9, 8,11, 7,11, 2,12, 4,12));

            sorted.Finish(b8Upper);
            Assert.IsTrue(VerifySortedEdges(t9, 2,12, 4,12));

            sorted.Finish(b2Upper);
            Assert.IsTrue(VerifySortedEdges(t9));
        }

        private bool VerifySortedEdges(IActiveEdge<int> edge, params int[] edgePairs)
        {
            var edgeTesting = (IActiveEdgeTesting<int>)edge;

            while (!edgeTesting.IsNone)
            {
                edgeTesting = edgeTesting.Below;
            }

            edgeTesting = edgeTesting.Above;

            for (int i = 0; i < edgePairs.Length; i += 2)
            {
                if (((IActiveEdgeTesting<int>)edgeTesting).Left != edgePairs[i])
                {
                    return false;
                }

                if (((IActiveEdgeTesting<int>)edgeTesting).Right != edgePairs[i + 1])
                {
                    return false;
                }

                edgeTesting = edgeTesting.Above;
            }

            if (!edgeTesting.IsNone)
            {
                return false;
            }

            return true;
        }

        [TestMethod]
        public void PolygonizeForm1()
        {
            var clockwise = new[]
            {
                new Vertex(1, 1),
                new Vertex(2.5f, 2),
                new Vertex(2, 2),
                new Vertex(1, 3),
                new Vertex(2, 4),
                new Vertex(4, 4),
                new Vertex(1.5f, 3),
                new Vertex(2.5f, 3),
                new Vertex(4, 3.5f),
//                new Vertex(2.25f, 2.5f),
                new Vertex(3, 2.5f),
                new Vertex(4, 1.5f),
                new Vertex(3.5f, 1),
                new Vertex(2.5f, 1),
            };

            var counter = new[]
            {
                new Vertex(3, 3),
                new Vertex(2, 2.5f),
                new Vertex(3, 2),
                new Vertex(2.5f, 2.5f),
            };

            var planeMeshBuilder = new PlaneMeshBuilder(new Plane(new Vector3(0, 0, -1), 0));
            var last = clockwise.Last();
            foreach (var dot in clockwise)
            {
                planeMeshBuilder.AddEdge(new Vector3(last.X, last.Y, 0), new Vector3(dot.X, dot.Y, 0));
                last = dot;
            }

            /*
            last = counter.Last();
            foreach (var dot in counter)
            {
                planeMeshBuilder.AddEdge(new Vector3(last.X, last.Y, 0), new Vector3(dot.X, dot.Y, 0));
                last = dot;
            }
            */

            planeMeshBuilder.Build();
        }

        /// <summary>
        /// Three concav segments on left side
        /// </summary>
        [TestMethod]
        public void PolygonizeTripleOutsideToInsideFinish()
        {
            var clockwise = new[]
            {
                new Vertex(1, 0),
                new Vertex(0, 1),
                new Vertex(2, 2),
                new Vertex(1, 2),
                new Vertex(0, 3),
                new Vertex(2, 4),
                new Vertex(1, 4),
                new Vertex(0, 5),
                new Vertex(1, 6),
                new Vertex(4, 6),
                new Vertex(5, 5),
                new Vertex(4, 4.5f),
                new Vertex(3, 3.5f),
                new Vertex(4, 2.5f),
                new Vertex(3, 1.5f),
                new Vertex(4, 0.5f),
                new Vertex(5.5f, 1),
                new Vertex(4.5f, 0),
            };

            var planeMeshBuilder = new PlaneMeshBuilder(new Plane(new Vector3(0, 0, -1), 0));
            var last = clockwise.Last();
            foreach (var dot in clockwise)
            {
                planeMeshBuilder.AddEdge(new Vector3(last.X, last.Y, 0), new Vector3(dot.X, dot.Y, 0));
                last = dot;
            }

            planeMeshBuilder.Build();
        }
    }
}
