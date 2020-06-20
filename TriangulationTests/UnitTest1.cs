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
        /// <summary>
        /// Add and Remove items from a sorted edges list
        /// </summary>
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

            var sorted = new Trapezoidation(vertices, new SplitCollector());
            var (b0, b0Upper) = sorted.TestBegin(0, 5, 6);
            Assert.AreEqual("0<5 0>6", string.Join(" ", sorted.Edges));

            var (b1, b1Upper) = sorted.TestBegin(1, 3, 4);
            Assert.AreEqual("0<5 0>6 1<3 1>4", string.Join(" ", sorted.Edges));

            var (b2, b2Upper) = sorted.TestBegin(2, 7, 12);
            Assert.AreEqual("0<5 0>6 1<3 2<7 2>12 1>4", string.Join(" ", sorted.Edges));

            var t3 = sorted.TestTransition(b1, 6);
            Assert.AreEqual("0<5 0>6 3<6 2<7 2>12 1>4", string.Join(" ", sorted.Edges));

            var t4 = sorted.TestTransition(b1Upper, 12);
            Assert.AreEqual("0<5 0>6 3<6 2<7 2>12 4>12", string.Join(" ", sorted.Edges));

            var t5 = sorted.TestTransition(b0, 9);
            Assert.AreEqual("5<9 0>6 3<6 2<7 2>12 4>12", string.Join(" ", sorted.Edges));

            sorted.TestJoin(b0Upper);
            Assert.AreEqual("5<9 2<7 2>12 4>12", string.Join(" ", sorted.Edges));

            var t7 = sorted.TestTransition(b2, 11);
            Assert.AreEqual("5<9 7<11 2>12 4>12", string.Join(" ", sorted.Edges));

            var (b8, b8Upper) = sorted.TestBegin(8, 10, 11);
            Assert.AreEqual("5<9 8<10 8>11 7<11 2>12 4>12", string.Join(" ", sorted.Edges));

            var t9 = sorted.TestTransition(t5, 10);
            Assert.AreEqual("9<10 8<10 8>11 7<11 2>12 4>12", string.Join(" ", sorted.Edges));

            sorted.TestJoin(t9);
            Assert.AreEqual("8>11 7<11 2>12 4>12", string.Join(" ", sorted.Edges));

            sorted.TestJoin(b8Upper);
            Assert.AreEqual("2>12 4>12", string.Join(" ", sorted.Edges));

            sorted.TestJoin(b2Upper);
            Assert.AreEqual(string.Empty, string.Join(" ", sorted.Edges));
        }

        /// <summary>
        /// a dummy split collector
        /// </summary>
        private class SplitCollector : IPolygonSplitSink
        {
            public void SplitPolygon(int leftVertex, int rightVertex)
            {
            }
        }
    }
}
