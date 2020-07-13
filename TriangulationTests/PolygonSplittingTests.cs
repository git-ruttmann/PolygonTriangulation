namespace TriangulationTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    
    using Vertex = System.Numerics.Vector2;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using PolygonTriangulation;

    /// <summary>
    /// Test the splitting of polygons via trapezoidation.
    /// </summary>
    [TestClass]
    public class PolygonSplitting
    {
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

            Assert.AreEqual("5 0 6 3 1 4 12 2 7 11 8 10 9", string.Join(" ", polygon.SubPolygonVertices(0)));

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
        /// Get edges after triangulating a partial polygon.
        /// </summary>
        [TestMethod]
        public void TriangulatePartial()
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

            Assert.AreEqual("5 0 6 3 1 4 12 2 7 11 8 10 9", string.Join(" ", polygon.SubPolygonVertices(0)));

            var triangluator = new PolygonTriangulator(polygon);
            var partialEdges = triangluator.GetEdgesAfterPartialTrapezoidation(4).ToArray();

            CollectionAssert.AreEqual(new[] { "1>4", "2<12", "2>7", "3<6", "0>6", "0<5" }, partialEdges);
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
        /// Join a hole into the polygon and then split at the same point.
        /// Tests the collision detection in PolygonSplitter.ChooseInstanceForSplit
        /// </summary>
        [TestMethod]
        public void SplitAtJoin3()
        {
            var vertices = new[]
            {
                new Vertex(0, 0),
                new Vertex(1, 2),
                new Vertex(2, 3),
                new Vertex(3, 0),
                new Vertex(4, 1),
                new Vertex(5, 2),
                new Vertex(6, 2),
                new Vertex(8, 4),
                new Vertex(9, 0),
            };

            var polygon = Polygon.Build(vertices)
                .AddVertices(0, 1, 2, 7, 8, 3)
                .ClosePartialPolygon()
                .AddVertices(4, 6, 5)
                .Close();

            var triangleCollector = PolygonTriangulator.CreateTriangleCollector();

            var triangluator = new PolygonTriangulator(polygon);
            var splits = string.Join(" ", triangluator.GetSplits().OrderBy(x => x.Item1).ThenBy(x => x.Item2).Select(x => $"{x.Item1}-{x.Item2}"));
            Assert.AreEqual("2-3 3-4 6-7", splits);

            var splittedPolygon = Polygon.Split(polygon, triangluator.GetSplits(), triangleCollector);
            Assert.IsTrue(PolygonFusionTests.SubPolygonExists(splittedPolygon, 0, 1, 2, 3));
            Assert.IsTrue(PolygonFusionTests.SubPolygonExists(splittedPolygon, 3, 2, 7, 6, 5, 4));
            Assert.IsTrue(PolygonFusionTests.SubPolygonExists(splittedPolygon, 8, 3, 4, 6, 7));
        }

        /// <summary>
        /// An opening cusp with one steep edge. Should use the other edge to detect the IsVertexAbove.
        /// </summary>
        [TestMethod]
        public void SplitWithOneSteepEdge()
        {
            var sortedVertices = new[]
            {
                new Vertex(-0.49946270f, 0.99504360f),
                new Vertex(-0.21584960f, 1.06319800f),
                new Vertex(-0.21538340f, 1.06331000f),
                new Vertex(-0.21537550f, 1.06278700f),
                new Vertex(-0.21536440f, 1.06295200f),
                new Vertex(0.00160142f, 0.34509140f),
                new Vertex(0.38788400f, -0.15597250f),
                new Vertex(0.75422390f, 0.97874460f),
                new Vertex(0.79434130f, 0.37428540f),
                new Vertex(0.84721410f, 1.31866100f),
                new Vertex(0.86589320f, 0.96906510f),
                new Vertex(1.59814200f, 1.49911500f),
                new Vertex(1.66974300f, 1.51632200f)
            };

            var polygon = Polygon.Build(sortedVertices)
                .AddVertices(7, 10, 9, 11, 12, 8, 6, 5, 0, 1, 2, 4, 3)
                .Close();

            var triangluator = new PolygonTriangulator(polygon);
            var splits = string.Join(" ", triangluator.GetSplits().OrderBy(x => x.Item1).ThenBy(x => x.Item2).Select(x => $"{x.Item1}-{x.Item2}"));
            var triangles = triangluator.BuildTriangles();
            Assert.AreEqual((sortedVertices.Length - 2) * 3, triangles.Length);
            Assert.AreEqual("2-3 3-5 6-7 7-8 8-10 10-11", splits);
        }

        /// <summary>
        /// Join a hole into the polygon and then split at the same point.
        /// Tests the collision detection in <see cref="Polygon.Split"/>
        /// </summary>
        [TestMethod]
        public void BadClosingOne()
        {
            var sortedVertices = new[]
            {
                new Vertex(-0.30465070f, 0.45236350f),
                new Vertex(0.16610780f, 0.56549070f),
                new Vertex(0.16682580f, 0.56601040f),
                new Vertex(0.16683170f, 0.56590190f),
                new Vertex(0.16684420f, 0.56566770f),
                new Vertex(0.16788760f, 0.56591840f),
                new Vertex(0.34376790f, 1.29828100f),
                new Vertex(0.64483490f, 1.69104800f),
                new Vertex(1.09183400f, 0.78795030f),
                new Vertex(1.30210200f, 0.83847910f)
            };

            var polygon = Polygon.Build(sortedVertices)
                .AddVertices(3, 4, 1, 0, 6, 7, 9, 8, 5, 2)
                .Close();

            var triangluator = new PolygonTriangulator(polygon);
            var splits = string.Join(" ", triangluator.GetSplits().OrderBy(x => x.Item1).ThenBy(x => x.Item2).Select(x => $"{x.Item1}-{x.Item2}"));
            var triangles = triangluator.BuildTriangles();
            Assert.AreEqual((sortedVertices.Length - 2) * 3, triangles.Length);
            Assert.AreEqual("1-2 5-6 7-8", splits);
        }
        
        /// <summary>
        /// Polygonize a simple triangle.
        /// </summary>
        [TestMethod]
        public void SimpleTriangle()
        {
            var sortedVertices = new[]
            {
                new Vertex(0, 0),
                new Vertex(1, 2),
                new Vertex(2, 1),
            };

            var polygon = Polygon.Build(sortedVertices)
                .Add(0)
                .Add(1)
                .Add(2)
                .Close();

            var triangluator = new PolygonTriangulator(polygon);
            var triangles = triangluator.BuildTriangles();
            Assert.AreEqual(0, triangluator.GetSplits().Count());
            Assert.IsTrue(VerifyTriangle(triangles, 0, 1, 2));
        }

        /// <summary>
        /// Use the same vertex id (4) in multiple sub-polygons
        /// </summary>
        [TestMethod]
        public void InnerPolygonTouchesOuterPolygon()
        {
            var sortedVertices = new[]
            {
                new Vertex(-1.009218f, 1.241688f),
                new Vertex(-0.8848248f, 1.403969f),
                new Vertex(-0.6169144f, 1.753481f),
                new Vertex(-0.2567905f, 0.265681f),
                new Vertex(-0.1457446f, 1.866717f),
                new Vertex(-0.09439461f, 0.9056299f),
                new Vertex(-0.07060831f, 0.4604469f),
                new Vertex(0.4499452f, 0.4153259f),
                new Vertex(0.5326312f, -0.7583123f),
                new Vertex(0.7791165f, 2.088959f),
                new Vertex(0.9355021f, 1.773004f),
                new Vertex(0.951596f, 1.471793f),
                new Vertex(0.9893153f, 2.139471f),
                new Vertex(1.01064f, 0.3667254f),
                new Vertex(1.363283f, 0.3253429f),
                new Vertex(1.874112f, 0.9917631f),
            };

            var polygon = Polygon.Build(sortedVertices)
                .AddVertices(12, 15, 14, 8, 3, 0, 1, 2, 4, 9)
                .ClosePartialPolygon()
                .AddVertices(7, 13, 11, 10, 4, 5, 6)
                .Close(4);

            var triangluator = new PolygonTriangulator(polygon);
            var splits = string.Join(" ", triangluator.GetSplits().OrderBy(x => x.Item1).ThenBy(x => x.Item2).Select(x => $"{x.Item1}-{x.Item2}"));
            Assert.AreEqual("2-3 3-4 7-8 8-13 9-10 11-12 12-13 13-14", splits);
            var triangles = triangluator.BuildTriangles();
            Assert.AreEqual((sortedVertices.Length - 2 + 1) * 3, triangles.Length);
        }

        /// <summary>
        /// Join three inner polygon at the same point. One on the left, one on the middle, on on the right.
        /// </summary>
        /// <remarks>
        /// this is still a problem
        /// </remarks>
        [TestMethod]
        public void PointFusionWithInnerPolygonLeftMiddleRight()
        {
            var sortedVertices = new[]
            {
                new Vertex(0, 0),
                new Vertex(1, 2),
                new Vertex(1, 3),  // 2
                new Vertex(2, 2),
                new Vertex(2.5f, 2),
                new Vertex(3, 3),  // 5
                new Vertex(3.5f, 2),
                new Vertex(4, 2),
                new Vertex(5, 2),
                new Vertex(5, 3),  // 9
                new Vertex(6, 1),
            };

            var polygon = Polygon.Build(sortedVertices)
                .AddVertices(0, 2, 5, 9, 10)
                .ClosePartialPolygon()
                .AddVertices(5, 1, 3)
                .ClosePartialPolygon()
                .AddVertices(5, 4, 6)
                .ClosePartialPolygon()
                .AddVertices(5, 7, 8)
                .Close(5);

            Assert.AreEqual(1, polygon.SubPolygonIds.Count());
            Assert.IsTrue(PolygonFusionTests.SubPolygonExists(polygon, 5, 1, 3, 5, 4, 6, 5, 7, 8, 5, 9, 10, 0, 2), "All three inner poygons must be fusioned in one polygon");
            var triangluator = new PolygonTriangulator(polygon);
            var splits = string.Join(" ", triangluator.GetSplits().OrderBy(x => x.Item1).ThenBy(x => x.Item2).Select(x => $"{x.Item1}-{x.Item2}"));
            Assert.AreEqual("0-1 1-2 3-4 6-7 8-9", splits);

            var result = triangluator.BuildTriangles();
            Assert.IsNotNull(result);
        }

        /// <summary>
        /// Join two polygons at a center vertex, one polygon is left, the other is on the right of the joining point.
        /// </summary>
        /// <remarks>
        /// Problem: The "Trapezoidation.vertexToEdge" won't find the edge on the second close as there is no collision list.
        /// </remarks>
        [TestMethod]
        public void PointFusionLeftToRight()
        {
            var sortedVertices = new[]
            {
                new Vertex(0, 0),
                new Vertex(0, 2),
                new Vertex(1, 1),
                new Vertex(2, 0),
                new Vertex(2, 2),
            };

            var polygon = Polygon.Build(sortedVertices)
                .AddVertices(2, 0, 1)
                .ClosePartialPolygon()
                .AddVertices(2, 4, 3)
                .Close(2);

            var triangluator = new PolygonTriangulator(polygon);
            var splits = string.Join(" ", triangluator.GetSplits().OrderBy(x => x.Item1).ThenBy(x => x.Item2).Select(x => $"{x.Item1}-{x.Item2}"));
            Assert.AreEqual(string.Empty, splits);
            Assert.AreEqual(2, polygon.SubPolygonIds.Count());
        }

        /// <summary>
        /// Join two polygons at a center vertex, one polygon is about the other.
        /// </summary>
        /// <remarks>
        /// Problem: The "Trapezoidation.vertexToEdge" won't find the edge on the second close as there is no collision list.
        /// </remarks>
        [TestMethod]
        public void PointFusionTopAndBottom()
        {
            var sortedVertices = new[]
            {
                new Vertex(0, 0),
                new Vertex(0, 2),
                new Vertex(1, 1),
                new Vertex(2, 0),
                new Vertex(2, 2),
            };

            var polygon = Polygon.Build(sortedVertices)
                .AddVertices(2, 1, 4)
                .ClosePartialPolygon()
                .AddVertices(2, 3, 0)
                .Close(2);

            var triangluator = new PolygonTriangulator(polygon);
            var splits = string.Join(" ", triangluator.GetSplits().OrderBy(x => x.Item1).ThenBy(x => x.Item2).Select(x => $"{x.Item1}-{x.Item2}"));
            Assert.AreEqual(string.Empty, splits);
            Assert.AreEqual(2, polygon.SubPolygonIds.Count());
        }

        /// <summary>
        /// Join three polygons at the same left and the same right vertex.
        /// </summary>
        /// <remarks>
        /// Problem: The "Trapezoidation.vertexToEdge" won't find the edge on the second close as there is no collision list.
        /// </remarks>
        [TestMethod]
        public void TripleFusionSinglePoint()
        {
            var sortedVertices = new[]
            {
                new Vertex(0, 0),
                new Vertex(0, 2),
                new Vertex(0, 3),
                new Vertex(1, 2),
                new Vertex(2, 3),
                new Vertex(2, 4),
                new Vertex(3, 1),
            };

            var polygon = Polygon.Build(sortedVertices)
                .AddVertices(3, 0, 1)
                .ClosePartialPolygon()
                .AddVertices(3, 2, 5)
                .ClosePartialPolygon()
                .AddVertices(3, 4, 6)
                .Close(3);

            var triangluator = new PolygonTriangulator(polygon);
            var splits = string.Join(" ", triangluator.GetSplits().OrderBy(x => x.Item1).ThenBy(x => x.Item2).Select(x => $"{x.Item1}-{x.Item2}"));
            Assert.AreEqual(string.Empty, splits);
        }

        /// <summary>
        /// Join three polygons at the same left and the same right vertex.
        /// </summary>
        /// <remarks>
        /// Problem: The "Trapezoidation.vertexToEdge" won't find the edge on the second close as there is no collision list.
        /// </remarks>
        [TestMethod]
        public void TripleFusionLeftAndRight()
        {
            var sortedVertices = new[]
            {
                new Vertex(0, 2),
                new Vertex(1, 0),
                new Vertex(1, 1),
                new Vertex(1, 2),
                new Vertex(1, 3),
                new Vertex(1, 4),
                new Vertex(1, 5),
                new Vertex(2, 3),
            };
            var polygon = Polygon.Build(sortedVertices)
                .AddVertices(0, 2, 7, 1)
                .ClosePartialPolygon()
                .AddVertices(0, 4, 7, 3)
                .ClosePartialPolygon()
                .AddVertices(0, 6, 7, 5)
                .Close(0);

            var triangluator = new PolygonTriangulator(polygon);
            var splits = string.Join(" ", triangluator.GetSplits().OrderBy(x => x.Item1).ThenBy(x => x.Item2).Select(x => $"{x.Item1}-{x.Item2}"));
            Assert.AreEqual("1-2 3-4 5-6", splits);
        }

        /// <summary>
        /// Triangluate a polygon with all trapezoid combinations (0,1,2 left neighbors * 0,1,2 right neighbors)
        /// </summary>
        [TestMethod]
        public void TriangulateAllTrapezoids()
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

                        default:
                            throw new InvalidOperationException("Can't happen, dear sonar");
                    }
                }
            }

            return false;
        }
    }
}
