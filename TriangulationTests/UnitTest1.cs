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

            Assert.AreEqual("5 0 6 3 1 4 12 2 7 11 8 10 9", string.Join(" ", polygon.VertexList(0)));

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
        /// Join a hole into the polygon and then split at the same point.
        /// Tests the collision detection in <see cref="Polygon.Split"/>
        /// </summary>
        [TestMethod]
        public void SplitAtJoin2()
        {
            var sortedVertices = new[]
            {
                new Vertex(0.0f, 0.0f), // 0 0
                new Vertex(1.0f, 0.5f), // 6 1
                new Vertex(1.5f, 1.0f), // 7 2
                new Vertex(2.0f, 0.0f), // 4 3
                new Vertex(3.0f, 0.5f), // 5 4
                new Vertex(3.5f, 0.0f), // 3 5
                new Vertex(4.0f, 4.0f), // 1 6
                new Vertex(5.0f, 0.0f), // 2 7
            };

            var polygon1 = Polygon.Build(sortedVertices)
                .AddVertices(3, 0, 6, 7, 5)
                .ClosePartialPolygon()
                .AddVertices(4, 2, 1)
                .Close();

            var triangleCollector = PolygonTriangulator.CreateTriangleCollector();

            var triangluator = new PolygonTriangulator(polygon1);
            var splits = string.Join(" ", triangluator.GetSplits().OrderBy(x => x.Item1).ThenBy(x => x.Item2).Select(x => $"{x.Item1}-{x.Item2}"));
            Assert.AreEqual("0-1 1-3 3-4 4-5 5-6", splits);

            var specialSplits = new[] { (4, 5), (3, 4), (0, 1), (1, 3), (5, 6) }
                .Select(x => x.ToTuple()).ToArray();

            var splittedPolygon = Polygon.Split(polygon1, specialSplits, triangleCollector);

            var polygon2 = Polygon.Build(sortedVertices)
                .AddVertices(5, 3, 0, 6, 7)
                .ClosePartialPolygon()
                .AddVertices(4, 2, 1)
                .Close();
            splittedPolygon = Polygon.Split(polygon2, specialSplits, triangleCollector);

            var polygon3 = Polygon.Build(sortedVertices)
                .AddVertices(7, 5, 3, 0, 6)
                .ClosePartialPolygon()
                .AddVertices(4, 2, 1)
                .Close();
            splittedPolygon = Polygon.Split(polygon3, specialSplits, triangleCollector);
        }

        /// <summary>
        /// Triangluate a polygon with all trapezoid combinations (0,1,2 left neighbors * 0,1,2 right neighbors)
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

        /// <summary>
        /// Triangluate two separate polygons at once
        /// </summary>
        [TestMethod]
        public void TriangluateDualPolygones()
        {
            var sortedVertices = new[]
            {
                new Vertex(1.0f, 1.0f),
                new Vertex(1.0f, 1.3f),
                new Vertex(1.0f, 2.8f),
                new Vertex(1.0f, 3.0f),
                new Vertex(2.0f, 1.0f),
                new Vertex(2.0f, 3.0f),
                new Vertex(3.0f, 1.0f),
                new Vertex(3.0f, 1.3f),
                new Vertex(3.0f, 2.8f),
                new Vertex(3.0f, 3.0f),
            };

            var polygon = Polygon.Build(sortedVertices)
                .AddVertices(0, 1, 7, 6, 4)
                .ClosePartialPolygon()
                .AddVertices(2, 3, 5, 9, 8)
                .Close();

            var triangluator = new PolygonTriangulator(polygon);
            var splits = string.Join(" ", triangluator.GetSplits().OrderBy(x => x.Item1).ThenBy(x => x.Item2).Select(x => $"{x.Item1}-{x.Item2}"));
            Assert.AreEqual("1-4 5-8", splits);
            var triangles = triangluator.BuildTriangles();
            Assert.AreEqual((sortedVertices.Length - 2 - 2) * 3, triangles.Length);
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

            var planeMeshBuilder = new PlanePolygonBuilder(new Plane(new Vector3(0, 0, -1), 0));
            var last = clockwise.Last();
            foreach (var vertex in clockwise)
            {
                planeMeshBuilder.AddEdge(new Vector3(last.X, last.Y, 0), new Vector3(vertex.X, vertex.Y, 0));
                last = vertex;
            }

            last = counter.Last();
            foreach (var dot in counter)
            {
                planeMeshBuilder.AddEdge(new Vector3(last.X, last.Y, 0), new Vector3(dot.X, dot.Y, 0));
                last = dot;
            }

            var result = planeMeshBuilder.Build();
            for (int i = 0; i < result.Triangles.Length; i += 3)
            {
                Console.WriteLine($"{result.Triangles[i + 0]} {result.Triangles[i + 1]} {result.Triangles[i + 2]}");
            }

            Assert.AreEqual(3 * 17, result.Triangles.Length);
        }

        /// <summary>
        /// Build a polygon by adding multile edges
        /// </summary>
        [TestMethod]
        public void BuildPolygonFromEdges()
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

            var builder = PlanePolygonBuilder.CreatePolygonBuilder();
            var last = clockwise.Last();
            foreach (var vertex in clockwise)
            {
                builder.AddEdge(new Vector3(last.X, last.Y, 0), new Vector3(vertex.X, vertex.Y, 0));
                last = vertex;
            }

            var result = builder.BuildPolygon();
            Assert.AreEqual("5 0 6 3 1 4 12 2 7 11 8 10 9", string.Join(" ", result.Polygon.VertexList(0)));
        }

        /// <summary>
        /// Join edges with bad directions to multiple polygons
        /// </summary>
        [TestMethod]
        public void PolygonJoinSegmentsTest()
        {
            var edges = new List<int>
            {
                // first - close 
                00 + 1, 00 + 2,
                00 + 3, 00 + 4,
                00 + 2, 00 + 3, // join to 1 2 3 4 with [2 3]
                00 + 4, 00 + 1, // close with [41]
                // same
                10 + 1, 10 + 2,
                10 + 3, 10 + 4,
                10 + 2, 10 + 3, // join to 1 2 3 4
                10 + 1, 10 + 4, // close with [14]
                // second
                20 + 1, 20 + 2,
                20 + 3, 20 + 4,
                20 + 1, 20 + 3, // join to 2 1 3 4 with [1 3]
                20 + 2, 20 + 4, // close
                // same
                30 + 1, 30 + 2,
                30 + 3, 30 + 4,
                30 + 3, 30 + 1, // join to 2 1 3 4 with [3 1]
                30 + 2, 30 + 4, // close
                // third
                40 + 1, 40 + 2,
                40 + 3, 40 + 4,
                40 + 1, 40 + 4, // join to 3 4 1 2 with [1 4]
                40 + 2, 40 + 3, // close
                // same
                50 + 1, 50 + 2,
                50 + 3, 50 + 4,
                50 + 1, 50 + 4, // join to 3 4 1 2 with [4 1]
                50 + 2, 50 + 3, // close
                // fourth
                60 + 1, 60 + 2,
                60 + 3, 60 + 4,
                60 + 2, 60 + 4, // join to 1 2 4 3 with [2 4]
                60 + 1, 60 + 3, // close
                // same
                70 + 1, 70 + 2,
                70 + 3, 70 + 4,
                70 + 2, 70 + 4, // join to 1 2 4 3 with [4 2]
                70 + 1, 70 + 3, // close
            };

            var builder = PlanePolygonBuilder.CreatePolygonLineDetector();
            builder.JoinEdgesToPolygones(edges);
            var result = builder.ClosedPolygons.ToArray();

            Assert.AreEqual(0, builder.UnclosedPolygons.Count());
            Assert.AreEqual(8, result.Length);
            Assert.IsTrue(ComparePolygon(new[] { 1, 2, 3, 4 }, result[0].Select(x => x % 10)), $"Unexpected { String.Join(" ", result[0]) }");
            Assert.IsTrue(ComparePolygon(new[] { 1, 2, 3, 4 }, result[1].Select(x => x % 10)), $"Unexpected { String.Join(" ", result[1]) }");
            Assert.IsTrue(ComparePolygon(new[] { 1, 2, 4, 3 }, result[2].Select(x => x % 10)), $"Unexpected { String.Join(" ", result[2]) }");
            Assert.IsTrue(ComparePolygon(new[] { 1, 3, 4, 2 }, result[3].Select(x => x % 10)), $"Unexpected { String.Join(" ", result[3]) }");
            Assert.IsTrue(ComparePolygon(new[] { 1, 2, 3, 4 }, result[4].Select(x => x % 10)), $"Unexpected { String.Join(" ", result[4]) }");
            Assert.IsTrue(ComparePolygon(new[] { 1, 2, 3, 4 }, result[5].Select(x => x % 10)), $"Unexpected { String.Join(" ", result[5]) }");
            Assert.IsTrue(ComparePolygon(new[] { 1, 2, 4, 3 }, result[6].Select(x => x % 10)), $"Unexpected { String.Join(" ", result[6]) }");
            Assert.IsTrue(ComparePolygon(new[] { 1, 2, 4, 3 }, result[7].Select(x => x % 10)), $"Unexpected { String.Join(" ", result[7]) }");
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

            var planeMeshBuilder = new PlanePolygonBuilder(new Plane(new Vector3(0, 0, -1), 0));
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

        /// <summary>
        /// a dummy split collector
        /// </summary>
        private class SplitCollector : IPolygonSplitSink
        {
            public List<Tuple<int, int>> Splits { get; private set; }

            public void SplitPolygon(int leftVertex, int rightVertex)
            {
                this.Splits.Add(Tuple.Create(leftVertex, rightVertex));
            }
        }

        /// <summary>
        /// Compare a polygon, start index is irrelevant. Reverse is not tolerated.
        /// </summary>
        /// <param name="expected">the expected array</param>
        /// <param name="actual">the effective array</param>
        /// <returns>true if sequence matches</returns>
        private static bool ComparePolygon(IEnumerable<int> expected, IEnumerable<int> actual)
        {
            var expectedArray = expected.ToArray();
            var actualArray = actual.ToArray();
            if (expectedArray.Length != actualArray.Length)
            {
                return false;
            }

            var firstValue = actualArray[0];
            var offset = expectedArray.TakeWhile(x => x != firstValue).Count();
            for (int i = 0; i < actualArray.Length; i++)
            {
                if (expectedArray[(i + offset) % expectedArray.Length] != actualArray[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
