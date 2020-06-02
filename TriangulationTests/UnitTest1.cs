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

            var sorted = new SortedActiveEdgeList<int>(vertices);
            var (b0, b0Upper) = sorted.Begin(0, 5, 6);
            Assert.AreEqual("0<5 0>6", string.Join(" ", sorted.Edges));

            var (b1, b1Upper) = sorted.Begin(1, 3, 4);
            Assert.AreEqual("0<5 0>6 1<3 1>4", string.Join(" ", sorted.Edges));

            var (b2, b2Upper) = sorted.Begin(2, 7, 12);
            Assert.AreEqual("0<5 0>6 1<3 2<7 2>12 1>4", string.Join(" ", sorted.Edges));

            var t3 = sorted.Transition(b1, 6);
            Assert.AreEqual("0<5 0>6 3<6 2<7 2>12 1>4", string.Join(" ", sorted.Edges));

            var t4 = sorted.Transition(b1Upper, 12);
            Assert.AreEqual("0<5 0>6 3<6 2<7 2>12 4>12", string.Join(" ", sorted.Edges));

            var t5 = sorted.Transition(b0, 9);
            Assert.AreEqual("5<9 0>6 3<6 2<7 2>12 4>12", string.Join(" ", sorted.Edges));

            sorted.Finish(b0Upper);
            Assert.AreEqual("5<9 2<7 2>12 4>12", string.Join(" ", sorted.Edges));

            var t7 = sorted.Transition(b2, 11);
            Assert.AreEqual("5<9 7<11 2>12 4>12", string.Join(" ", sorted.Edges));

            var (b8, b8Upper) = sorted.Begin(8, 10, 11);
            Assert.AreEqual("5<9 8<10 8>11 7<11 2>12 4>12", string.Join(" ", sorted.Edges));

            var t9 = sorted.Transition(t5, 10);
            Assert.AreEqual("9<10 8<10 8>11 7<11 2>12 4>12", string.Join(" ", sorted.Edges));

            sorted.Finish(t9);
            Assert.AreEqual("8>11 7<11 2>12 4>12", string.Join(" ", sorted.Edges));

            sorted.Finish(b8Upper);
            Assert.AreEqual("2>12 4>12", string.Join(" ", sorted.Edges));

            sorted.Finish(b2Upper);
            Assert.AreEqual(string.Empty, string.Join(" ", sorted.Edges));
        }

        /// <summary>
        /// Triangluate a manually built polygon
        /// </summary>
        [TestMethod]
        public void TriangulateForm1()
        {
            var sortedVertices = new[]
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

            var polygon = Polygon.Build(sortedVertices)
                .AddVertices(5, 0, 6, 3, 1, 4, 12, 2, 7, 11, 8, 10, 9)
                .Close();

            Assert.AreEqual("5 0 6 3 1 4 12 2 7 11 8 10 9", string.Join(" ", polygon.Indices));

            var triangluator = new PolygonTriangulator(polygon);
            var splits = string.Join(" ", triangluator.GetSplits().OrderBy(x => x.Item1).ThenBy(x => x.Item2).Select(x => $"{x.Item1}-{x.Item2}"));
            Assert.AreEqual("1-2 2-3 2-4 5-6 6-7 7-8 8-9", splits);

            var triangles = triangluator.BuildTriangles();
            Assert.IsTrue(VerifyTriangle(triangles, 0, 6, 5));
            Assert.IsTrue(VerifyTriangle(triangles, 1, 2, 3));
            Assert.IsTrue(VerifyTriangle(triangles, 1, 4, 2));
            Assert.IsTrue(VerifyTriangle(triangles, 2, 7, 3));
            Assert.IsTrue(VerifyTriangle(triangles, 2, 4, 12));
            Assert.IsTrue(VerifyTriangle(triangles, 3, 7, 6));
            Assert.IsTrue(VerifyTriangle(triangles, 5, 6, 8));
            Assert.IsTrue(VerifyTriangle(triangles, 5, 8, 9));
            Assert.IsTrue(VerifyTriangle(triangles, 6, 7, 8));
            Assert.IsTrue(VerifyTriangle(triangles, 7, 11, 8));
            Assert.IsTrue(VerifyTriangle(triangles, 8, 10, 9));
            Assert.AreEqual(11 * 3, triangles.Length);
        }

        /// <summary>
        /// Triangluate a polygon with all trapezoid combinations (0,1,2 neighbors)
        /// </summary>
        [TestMethod]
        public void TriangulateForm2()
        {
            var sortedVertices = new[]
            {
                new Vertex(1, 16),
                new Vertex(2, 11),
                new Vertex(3, 15),
                new Vertex(4, 13),
                new Vertex(5, 7),
                new Vertex(5, 17), // 5
                new Vertex(6, 2),
                new Vertex(7, 4),
                new Vertex(7, 9),
                new Vertex(8, 14),
                new Vertex(9, 5), // 10
                new Vertex(11, 9),
                new Vertex(13, 14),
                new Vertex(14, 1),
                new Vertex(14, 12),
                new Vertex(15, 6), // 15
                new Vertex(16, 3),
                new Vertex(16, 19),
                new Vertex(17, 8),
                new Vertex(18, 2),
                new Vertex(19, 20), // 20
            };

            var polygon = Polygon.Build(sortedVertices)
                .AddVertices(0, 5, 2, 9, 17, 20, 12, 14, 11, 18, 15, 19, 13, 16, 6, 7, 10, 4, 8, 1, 3)
                .Close();

            var triangluator = new PolygonTriangulator(polygon);
            var splits = string.Join(" ", triangluator.GetSplits().OrderBy(x => x.Item1).ThenBy(x => x.Item2).Select(x => $"{x.Item1}-{x.Item2}"));
            Assert.AreEqual("0-2 2-3 3-8 8-9 9-10 10-11 11-12 11-15 12-17 15-16 16-19", splits);
            var triangles = triangluator.BuildTriangles();
            Assert.AreEqual((sortedVertices.Length - 2) * 3, triangles.Length);
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

        /// <summary>
        /// Verify a Triangle is part of the result list. The order is not important
        /// </summary>
        /// <param name="triangles">All triangles</param>
        /// <param name="p1">vertex index 1</param>
        /// <param name="p2">vertex index 2</param>
        /// <param name="p3">vertex index 3</param>
        /// <returns>true if the triangle is found</returns>
        private static bool VerifyTriangle(IReadOnlyList<int> triangles, int p1, int p2, int p3)
        {
            for (int i = 0; i < triangles.Count; i++)
            {
                if (triangles[i] == p1)
                {
                    switch (i % 3)
                    {
                        case 0:
                            if (triangles[i + 1] == p2 && triangles[i + 2] == p3)
                            {
                                return true;
                            }

                            break;

                        case 1:
                            if (triangles[i + 1] == p2 && triangles[i - 1] == p3)
                            {
                                return true;
                            }

                            break;

                        case 2:
                            if (triangles[i - 2] == p2 && triangles[i - 1] == p3)
                            {
                                return true;
                            }

                            break;
                    }
                }
            }

            return false;
        }
    }
}
