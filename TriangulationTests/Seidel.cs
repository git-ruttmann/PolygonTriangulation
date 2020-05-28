namespace Ruttmann.PolygonTriangulation.Seidel.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Vertex = System.Numerics.Vector2;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Test the polygon triangulation in the Seidel library
    /// </summary>
    [TestClass]
    public class Seidel
    {
        /// <summary>
        /// Test the split of a simple quad.
        /// </summary>
        [TestMethod]
        public void SplitSimpleConcave()
        {
            var polygon = this.PolygonDoubleTriangleWithConcave();

            Assert.AreEqual("0 1 2 3", string.Join(" ", polygon.Indices));

            var triangleCollector = TriangleBuilder.CreateTriangleCollecor();
            var result = Polygon.Split(polygon, new[] { Tuple.Create(1, 3) }, triangleCollector);

            Assert.AreEqual(0, result.Length);
            Assert.AreEqual(6, triangleCollector.Triangles.Length);
        }

        /// <summary>
        /// Test the polygon split operation - join holes into a polygon and finally split it to two monotones
        /// </summary>
        [TestMethod]
        public void SplitSquareWithHoles()
        {
            var polygon = this.PolygonSquareWithThreeNonOverlappingHoles();
            var splits = new[]
            {
                Tuple.Create(9, 4),
                Tuple.Create(8, 12),
                Tuple.Create(6, 13),
                Tuple.Create(5, 2),
            };

            var triangleCollector = TriangleBuilder.CreateTriangleCollecor();
            var result = Polygon.Split(polygon, splits, triangleCollector);

            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(0, triangleCollector.Triangles.Length);
        }

        /// <summary>
        /// Test the <see cref="Polygon.IndicesStartingAt"/> after a polygon is split with multiple colliding points
        /// </summary>
        [TestMethod]
        public void IteratePolygonsAfterSplit()
        {
            var vertices = Enumerable.Repeat(new Vertex(0f, 0f), 40).ToArray();
            var polygon = Polygon.Build(vertices).Auto();

            var splits = new[]
            {
                Tuple.Create(5, 8),
                Tuple.Create(5, 12),
                Tuple.Create(5, 16),
                Tuple.Create(5, 32),
                Tuple.Create(20, 32),
                Tuple.Create(24, 32),
                Tuple.Create(32, 36),
                Tuple.Create(36, 0),
            };

            var triangleCollector = TriangleBuilder.CreateTriangleCollecor();
            var result = Polygon.Split(polygon, splits, triangleCollector);
            Assert.AreEqual(0, triangleCollector.Triangles.Length);

            var sorted = result
                .OrderBy(x => x.Indices.First())
                .ThenBy(x => x.Indices.Skip(1).First())
                .ToArray();

            Assert.AreEqual("0 1 2 3 4 5 32 36", string.Join(" ", sorted[0].Indices));
            Assert.AreEqual("0 36 37 38 39", string.Join(" ", sorted[1].Indices));
            Assert.AreEqual("5 6 7 8", string.Join(" ", sorted[2].Indices));
            Assert.AreEqual("5 8 9 10 11 12", string.Join(" ", sorted[3].Indices));
            Assert.AreEqual("5 12 13 14 15 16", string.Join(" ", sorted[4].Indices));
            Assert.AreEqual("5 16 17 18 19 20 32", string.Join(" ", sorted[5].Indices));
            Assert.AreEqual("20 21 22 23 24 32", string.Join(" ", sorted[6].Indices));
            Assert.AreEqual("24 25 26 27 28 29 30 31 32", string.Join(" ", sorted[7].Indices));
            Assert.AreEqual("32 33 34 35 36", string.Join(" ", sorted[8].Indices));

            Assert.AreEqual("5 6 7 8", string.Join(" ", sorted[2].IndicesStartingAt(5)), "Must find the correct start vertex in a collision chain");
            Assert.AreEqual("5 8 9 10 11 12", string.Join(" ", sorted[3].IndicesStartingAt(5)));
            Assert.AreEqual("5 12 13 14 15 16", string.Join(" ", sorted[4].IndicesStartingAt(5)));
            Assert.AreEqual("5 16 17 18 19 20 32", string.Join(" ", sorted[5].IndicesStartingAt(5)));

            Assert.AreEqual("6 7 8 5", string.Join(" ", sorted[2].IndicesStartingAt(6)), "Must find the correct vertex in the middle of the polygon");
            Assert.AreEqual("7 8 5 6", string.Join(" ", sorted[2].IndicesStartingAt(7)));
            Assert.AreEqual("8 5 6 7", string.Join(" ", sorted[2].IndicesStartingAt(8)));
            Assert.AreEqual("8 9 10 11 12 5", string.Join(" ", sorted[3].IndicesStartingAt(8)));
            Assert.AreEqual("12 13 14 15 16 5", string.Join(" ", sorted[4].IndicesStartingAt(12)));
            Assert.AreEqual("16 17 18 19 20 32 5", string.Join(" ", sorted[5].IndicesStartingAt(16)));

            Assert.AreEqual("32 5 16 17 18 19 20", string.Join(" ", sorted[5].IndicesStartingAt(32)), "Must find the correct vertex in an end-point collision chain");
            Assert.AreEqual("32 20 21 22 23 24", string.Join(" ", sorted[6].IndicesStartingAt(32)));

            Assert.AreEqual("0 1 2 3 4 5 32 36", string.Join(" ", sorted[0].IndicesStartingAt(0)), "Must accept 0 as vertex id in collision chain");
            Assert.AreEqual("0 36 37 38 39", string.Join(" ", sorted[1].IndicesStartingAt(0)), "Must accept 0 as vertex id in collision chain");

            try
            {
                var _ = string.Join(" ", sorted[1].IndicesStartingAt(9));
                Assert.IsTrue(false, "that polygon doesn't contain vertex 9");
            }
            catch (InvalidOperationException)
            {
            }
        }

        /// <summary>
        /// Test the segment/vertex compearer
        /// </summary>
        [TestMethod]
        public void PointIsLeftOfSegmentComparer()
        {
            var segment = this.PolygonTripleStart().PolygonSegments.ToArray()[9];
            Assert.IsTrue(VertexComparer.Instance.PointIsLeftOfSegment(new Vertex(1, 4), segment));
            Assert.IsFalse(VertexComparer.Instance.PointIsLeftOfSegment(new Vertex(3, 4), segment));
            Assert.IsTrue(VertexComparer.Instance.PointIsLeftOfSegment(new Vertex(1, 4.5f), segment));
            Assert.IsFalse(VertexComparer.Instance.PointIsLeftOfSegment(new Vertex(3, 4.5f), segment));
            Assert.IsTrue(VertexComparer.Instance.PointIsLeftOfSegment(new Vertex(1, 3.5f), segment));
            Assert.IsFalse(VertexComparer.Instance.PointIsLeftOfSegment(new Vertex(3, 3.5f), segment));
            Assert.IsTrue(VertexComparer.Instance.PointIsLeftOfSegment(new Vertex(1, 3.5f), segment));

            Assert.IsFalse(VertexComparer.Instance.PointIsLeftOfSegment(new Vertex(2.51f, 3.5f), segment), "Close after lower");
            Assert.IsTrue(VertexComparer.Instance.PointIsLeftOfSegment(new Vertex(2.49f, 3.5f), segment), "Close before lower");
            Assert.IsFalse(VertexComparer.Instance.PointIsLeftOfSegment(new Vertex(1.51f, 4.5f), segment), "Close after upper");
            Assert.IsTrue(VertexComparer.Instance.PointIsLeftOfSegment(new Vertex(1.49f, 4.5f), segment), "Close before lower");
            Assert.IsFalse(VertexComparer.Instance.PointIsLeftOfSegment(new Vertex(2.01f, 4.0f), segment), "Close after center");
            Assert.IsTrue(VertexComparer.Instance.PointIsLeftOfSegment(new Vertex(1.9f, 4.0f), segment), "Close before center");
        }

        /// <summary>
        /// Add segments of a quad with a concave corner.
        /// </summary>
        [TestMethod]
        public void AddSegmentsSimpleConcave()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            var polygon = this.PolygonDoubleTriangleWithConcave();
            var segments = polygon.PolygonSegments.ToArray();
            var trapezoidBuilder = new TrapezoidBuilder(segments[2]);
            trapezoidBuilder.AddSegment(segments[0]);
            trapezoidBuilder.AddSegment(segments[1]);
            trapezoidBuilder.AddSegment(segments[3]);

            trapezoidBuilder.Tree.DumpTree();

            var firstInsideTriangle = trapezoidBuilder.GetFirstInsideTriangle();
            Assert.AreEqual(2, firstInsideTriangle.Id);

            var splits = TrapezoidToSplits.ExtractSplits(firstInsideTriangle);
            var result = TriangleBuilder.SplitAndTriangluate(polygon, splits);

            Assert.IsTrue(VerifyTriangle(result, 0, 1, 3));
            Assert.IsTrue(VerifyTriangle(result, 1, 2, 3));
            Assert.AreEqual(2 * 3, result.Length);
        }

        /// <summary>
        /// Test triangulation with a monotone that has the last three elements on the stack
        /// </summary>
        [TestMethod]
        public void AddSegmentsPolygonTripleStartWithSideTriangles()
        {
            var polygon = this.PolygonTripleStartWithSideTriangles();
            var result = TriangleBuilder.TriangulatePolygon(polygon);

            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            for (int i = 0; i < result.Length; i += 3)
            {
                Console.WriteLine($"{result[i + 0]} {result[i + 1]} {result[i + 2]}");
            }
        }

        /// <summary>
        /// Test triangulation with all kinds of splits in <see cref="TrapezoidToSplits"/>
        /// </summary>
        [TestMethod]
        public void DownwardAndUpwardOpeningTriangle()
        {
            var vertices = new[]
            {
                new Vertex(0.0f, 2.0f),
                new Vertex(0.5f, 0.0f),
                new Vertex(1.5f, 1.5f),
                new Vertex(2.5f, 0.5f),
                new Vertex(3.0f, 2.0f),
                new Vertex(3.5f, 1.0f),
                new Vertex(4.0f, 2.5f),
                new Vertex(4.5f, 1.0f),
                new Vertex(5.0f, 0.5f),
                new Vertex(4.5f, 4.0f),
                new Vertex(5.5f, 3.0f),
                new Vertex(5.0f, 5.0f),
                new Vertex(4.0f, 3.5f),
                new Vertex(3.5f, 4.5f),
                new Vertex(3.0f, 3.0f),
                new Vertex(2.5f, 4.5f),
                new Vertex(2.0f, 3.7f),
                new Vertex(0.5f, 5.0f),
                new Vertex(1.0f, 4.0f),
                new Vertex(1.5f, 2.2f),
                new Vertex(1.0f, 1.0f),
            };

            var polygon = Polygon.Build(vertices).Auto();
            var result = TriangleBuilder.TriangulatePolygon(polygon);

            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            for (int i = 0; i < result.Length; i += 3)
            {
                Console.WriteLine($"{result[i + 0]} {result[i + 1]} {result[i + 2]}");
            }

            Assert.IsTrue(VerifyTriangle(result, 20, 0, 1));
            Assert.IsTrue(VerifyTriangle(result, 2, 3, 4));
            Assert.IsTrue(VerifyTriangle(result, 20, 1, 2));
            Assert.IsTrue(VerifyTriangle(result, 9, 10, 11));
            Assert.IsTrue(VerifyTriangle(result, 9, 11, 12));
            Assert.IsTrue(VerifyTriangle(result, 12, 13, 14));
            Assert.IsTrue(VerifyTriangle(result, 14, 15, 16));
            Assert.IsTrue(VerifyTriangle(result, 16, 17, 18));
            Assert.IsTrue(VerifyTriangle(result, 19, 2, 4));
            Assert.IsTrue(VerifyTriangle(result, 19, 20, 2));
            Assert.IsTrue(VerifyTriangle(result, 4, 6, 19));
            Assert.IsTrue(VerifyTriangle(result, 5, 6, 4));
            Assert.IsTrue(VerifyTriangle(result, 6, 12, 14));
            Assert.IsTrue(VerifyTriangle(result, 6, 9, 12));
            Assert.IsTrue(VerifyTriangle(result, 7, 9, 6));
            Assert.IsTrue(VerifyTriangle(result, 8, 9, 7));
            Assert.IsTrue(VerifyTriangle(result, 14, 19, 6));
            Assert.IsTrue(VerifyTriangle(result, 16, 19, 14));
            Assert.IsTrue(VerifyTriangle(result, 18, 19, 16));
        }

        /// <summary>
        /// Test triangulation with a monotone that has the last three elements on the stack
        /// </summary>
        [TestMethod]
        public void AddSegmentsSquareHolesLastTriangleOnStack()
        {
            var polygon = this.PolygonSquareHolesLastTriangleOnStack();
            var result = TriangleBuilder.TriangulatePolygon(polygon);

            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            for (int i = 0; i < result.Length; i += 3)
            {
                Console.WriteLine($"{result[i + 0]} {result[i + 1]} {result[i + 2]}");
            }

            Assert.IsTrue(VerifyTriangle(result, 10, 3, 4));
            Assert.IsTrue(VerifyTriangle(result, 8, 3, 10));
            Assert.IsTrue(VerifyTriangle(result, 12, 3, 8));
            Assert.IsTrue(VerifyTriangle(result, 13, 3, 12));
            Assert.IsTrue(VerifyTriangle(result, 6, 13, 11));
            Assert.IsTrue(VerifyTriangle(result, 6, 3, 13));
            Assert.IsTrue(VerifyTriangle(result, 7, 3, 6));
            Assert.IsTrue(VerifyTriangle(result, 2, 7, 5));
            Assert.IsTrue(VerifyTriangle(result, 2, 3, 7));
            Assert.IsTrue(VerifyTriangle(result, 5, 1, 2));
            Assert.IsTrue(VerifyTriangle(result, 6, 1, 5));
            Assert.IsTrue(VerifyTriangle(result, 11, 1, 6));
            Assert.IsTrue(VerifyTriangle(result, 8, 11, 12));
            Assert.IsTrue(VerifyTriangle(result, 8, 1, 11));
            Assert.IsTrue(VerifyTriangle(result, 4, 9, 10));
            Assert.IsTrue(VerifyTriangle(result, 4, 8, 9));
            Assert.IsTrue(VerifyTriangle(result, 4, 1, 8));
        }

        /// <summary>
        /// Test triangulation of a polygon with holes
        /// </summary>
        [TestMethod]
        public void AddSegmentsSquareWithThreeNonOverlappingHoles()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            var polygon = this.PolygonSquareWithThreeNonOverlappingHoles();
            var result = TriangleBuilder.TriangulatePolygon(polygon);

            for (int i = 0; i < result.Length; i += 3)
            {
                Console.WriteLine($"{result[i + 0]} {result[i + 1]} {result[i + 2]}");
            }

            Assert.IsTrue(VerifyTriangle(result, 3, 4, 9));
            Assert.IsTrue(VerifyTriangle(result, 3, 9, 10));
            Assert.IsTrue(VerifyTriangle(result, 10, 8, 12));
            Assert.IsTrue(VerifyTriangle(result, 3, 10, 12));
            Assert.IsTrue(VerifyTriangle(result, 3, 12, 13));
            Assert.IsTrue(VerifyTriangle(result, 13, 6, 7));
            Assert.IsTrue(VerifyTriangle(result, 7, 5, 2));
            Assert.IsTrue(VerifyTriangle(result, 13, 7, 2));
            Assert.IsTrue(VerifyTriangle(result, 3, 13, 2));
            Assert.IsTrue(VerifyTriangle(result, 1, 2, 5));
            Assert.IsTrue(VerifyTriangle(result, 6, 13, 11));
            Assert.IsTrue(VerifyTriangle(result, 11, 12, 8));
            Assert.IsTrue(VerifyTriangle(result, 6, 11, 8));
            Assert.IsTrue(VerifyTriangle(result, 5, 6, 8));
            Assert.IsTrue(VerifyTriangle(result, 1, 5, 8));
            Assert.IsTrue(VerifyTriangle(result, 8, 9, 4));
            Assert.IsTrue(VerifyTriangle(result, 1, 8, 4));

            // Assert.AreEqual(2 * 3, result.Length);
        }

        /// <summary>
        /// Test adding segments in an "random" order
        /// </summary>
        [TestMethod]
        public void AddSegmentsTripleStart()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            var polygon = this.PolygonTripleStart();
            var segments = polygon.PolygonSegments.ToArray();
            var trapezoidBuilder = new TrapezoidBuilder(segments[9]);
            trapezoidBuilder.AddSegment(segments[0]);
            trapezoidBuilder.AddSegment(segments[4]);
            trapezoidBuilder.AddSegment(segments[3]);
            trapezoidBuilder.AddSegment(segments[6]);
            trapezoidBuilder.AddSegment(segments[11]);
            trapezoidBuilder.AddSegment(segments[15]);
            trapezoidBuilder.AddSegment(segments[17]);
            trapezoidBuilder.AddSegment(segments[12]);
            trapezoidBuilder.AddSegment(segments[13]);
            trapezoidBuilder.AddSegment(segments[14]);
            trapezoidBuilder.AddSegment(segments[7]);
            trapezoidBuilder.AddSegment(segments[16]);
            trapezoidBuilder.AddSegment(segments[2]);
            trapezoidBuilder.AddSegment(segments[1]);
            trapezoidBuilder.AddSegment(segments[5]);
            trapezoidBuilder.AddSegment(segments[8]);
            trapezoidBuilder.AddSegment(segments[10]);

            // trapezoidBuilder.Tree.DumpTree();
            var firstInsideTriangle = trapezoidBuilder.GetFirstInsideTriangle();
            var splits = TrapezoidToSplits.ExtractSplits(firstInsideTriangle);
            var result = TriangleBuilder.SplitAndTriangluate(polygon, splits);

            for (int i = 0; i < result.Length; i += 3)
            {
                Console.WriteLine($"{result[i + 0]} {result[i + 1]} {result[i + 2]}");
            }

            Assert.IsTrue(VerifyTriangle(result, 12, 13, 11));
            Assert.IsTrue(VerifyTriangle(result, 13, 10, 11));
            Assert.IsTrue(VerifyTriangle(result, 14, 16, 17));
            Assert.IsTrue(VerifyTriangle(result, 17, 2, 8));
            Assert.IsTrue(VerifyTriangle(result, 17, 8, 10));
            Assert.IsTrue(VerifyTriangle(result, 14, 17, 10));
            Assert.IsTrue(VerifyTriangle(result, 14, 10, 13));
            Assert.IsTrue(VerifyTriangle(result, 8, 9, 10));
            Assert.IsTrue(VerifyTriangle(result, 5, 7, 8));
            Assert.IsTrue(VerifyTriangle(result, 5, 8, 2));
            Assert.IsTrue(VerifyTriangle(result, 5, 2, 4));
            Assert.IsTrue(VerifyTriangle(result, 17, 1, 2));
            Assert.IsTrue(VerifyTriangle(result, 14, 15, 16));
            Assert.IsTrue(VerifyTriangle(result, 17, 18, 1));
            Assert.IsTrue(VerifyTriangle(result, 5, 6, 7));
            Assert.IsTrue(VerifyTriangle(result, 2, 3, 4));
            Assert.AreEqual(16 * 3, result.Length);
        }

        /// <summary>
        /// Test location inside a TrapezoidBuilder tree, initialized with a single segment
        /// </summary>
        [TestMethod]
        public void LocatePointAfterInitializeTree()
        {
            var segment = this.PolygonTripleStart().PolygonSegments.ToArray()[9];
            var locationTree = new TrapezoidBuilder(segment).Tree;

            var trapezoid = locationTree.LocateEndpoint(new Vertex(1, 4), new Vertex(0, 4));
            Assert.IsNull(trapezoid.lseg);
            Assert.AreEqual(trapezoid.rseg, segment, "Must find the left trapezoid and the segment is right to it");

            trapezoid = locationTree.LocateEndpoint(new Vertex(3, 4), new Vertex(3, 4));
            Assert.AreEqual(trapezoid.lseg, segment, "Must find the left trapezoid and the segment is right to it");
            Assert.IsNull(trapezoid.rseg);

            trapezoid = locationTree.LocateEndpoint(new Vertex(2.5f, 3.5f), new Vertex(3, 4));
            Assert.AreEqual(trapezoid.lseg, segment, "Joined at point but more to the right");
            Assert.IsNull(trapezoid.rseg);

            trapezoid = locationTree.LocateEndpoint(new Vertex(2.5f, 3.5f), new Vertex(3, 1));
            Assert.IsNull(trapezoid.lseg);
            Assert.IsNull(trapezoid.rseg, "Joined at point but more to the left => must be treated as 'lower than the existing trapezoids'");
        }

        /// <summary>
        /// Test polygon construction and Segment idendity
        /// </summary>
        [TestMethod]
        public void ConstructSegments()
        {
            var polygon = this.PolygonTripleStart();
            var first = polygon.PolygonSegments.First();
            Assert.AreEqual(1, first.Id);

            var segmentCount = 0;
            foreach (var segment in polygon.PolygonSegments)
            {
                segmentCount++;
                Assert.AreEqual(segmentCount, segment.Id);
            }

            Assert.AreEqual(18, segmentCount);
            Assert.AreEqual(1, first.Id);
            Assert.AreEqual(18, polygon.PolygonSegments.Count());
        }

        /// <summary>
        /// Verify a Triangle is part of the result list. The order is not important
        /// </summary>
        /// <param name="triangles">All triangles</param>
        /// <param name="p1">vertex index 1</param>
        /// <param name="p2">vertex index 2</param>
        /// <param name="p3">vertex index 3</param>
        /// <returns>true if the triangle is found</returns>
        private static bool VerifyTriangle(IList<int> triangles, int p1, int p2, int p3)
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

        private Polygon PolygonTripleStart()
        {
            var vertices = new[]
            {
                new Vertex(-20f, -20f),
                new Vertex(4.0f, 1.0f),
                new Vertex(4.0f, 2.0f),
                new Vertex(5.0f, 0.0f),
                new Vertex(6.0f, 1.0f),
                new Vertex(6.0f, 5.0f),
                new Vertex(5.0f, 6.0f),
                new Vertex(5.5f, 4.5f),
                new Vertex(4.5f, 3.0f),
                new Vertex(3.5f, 4.5f),
                new Vertex(2.5f, 3.5f),
                new Vertex(1.5f, 4.5f),
                new Vertex(1.0f, 5.5f),
                new Vertex(0.0f, 4.5f),
                new Vertex(0.0f, 1.0f),
                new Vertex(1.0f, 0.0f),
                new Vertex(2.0f, 1.0f),
                new Vertex(2.0f, 2.0f),
                new Vertex(3.0f, 0.0f),
            };

            var builder = Polygon
                .Build(vertices)
                .AddVertices(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18);

            return builder.Close();
        }

        private Polygon PolygonTripleStartWithSideTriangles()
        {
            var vertices = new[]
            {
                new Vertex(-20f, -20f),
                new Vertex(4.0f, 1.0f),
                new Vertex(4.0f, 2.0f),
                new Vertex(5.0f, 0.0f),
                new Vertex(6.0f, 1.0f),
                new Vertex(6.0f, 2.5f),
                new Vertex(5.0f, 3.5f),
                new Vertex(6.0f, 4.5f),
                new Vertex(6.0f, 5.0f),
                new Vertex(5.0f, 6.0f),
                new Vertex(5.5f, 4.5f),
                new Vertex(4.5f, 3.0f),
                new Vertex(3.5f, 4.5f),
                new Vertex(2.5f, 3.5f),
                new Vertex(1.5f, 4.5f),
                new Vertex(1.0f, 5.5f),
                new Vertex(0.0f, 4.5f),
                new Vertex(0.0f, 3.5f),
                new Vertex(1.0f, 2.5f),
                new Vertex(0.0f, 1.5f),
                new Vertex(0.0f, 1.0f),
                new Vertex(1.0f, 0.0f),
                new Vertex(2.0f, 1.0f),
                new Vertex(2.0f, 2.0f),
                new Vertex(3.0f, 0.0f),
            };

            var builder = Polygon
                .Build(vertices)
                .AddVertices(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24);

            return builder.Close();
        }

        /// <summary>
        /// Return a simple polygon with two triangles and one concave vertex
        /// </summary>
        /// <returns></returns>
        private Polygon PolygonDoubleTriangleWithConcave()
        {
            var vertices = new[]
            {
                new Vertex(0.5f, 0.0f),
                new Vertex(1.5f, 2.0f),
                new Vertex(0.0f, 3.0f),
                new Vertex(1.0f, 1.5f),
            };

            return Polygon.Build(vertices).Auto();
        }

        /// <summary>
        /// Return a simple polygon with two triangles and one concave vertex
        /// </summary>
        /// <returns>a polygon</returns>
        private Polygon PolygonSquareWithThreeNonOverlappingHoles()
        {
            var vertices = new[]
            {
                new Vertex(-20f, -20f),
                new Vertex(0.0f, 0.0f),
                new Vertex(6.0f, 0.0f),
                new Vertex(6.0f, 6.0f),
                new Vertex(0.0f, 6.0f),
                new Vertex(0.5f, 1.0f),
                new Vertex(1.0f, 2.0f),
                new Vertex(2.0f, 1.5f),
                new Vertex(0.5f, 4.0f),
                new Vertex(1.0f, 5.0f),
                new Vertex(2.0f, 4.5f),
                new Vertex(3.0f, 3.0f),
                new Vertex(5.0f, 3.5f),
                new Vertex(5.0f, 2.5f), 
            };

            var builder = Polygon
                .Build(vertices)
                .AddVertices(1, 2, 3, 4)
                .AddHole(5, 6, 7)
                .AddHole(8, 9, 10)
                .AddHole(11, 12, 13);

            return builder.Close();
        }

        /// <summary>
        /// Return a polygon that ends with 3 triangles on the stack
        /// </summary>
        /// <returns></returns>
        private Polygon PolygonSquareHolesLastTriangleOnStack()
        {
            var vertices = new[]
            {
                new Vertex(-20f, -20f),

                new Vertex(0.0f, 0.0f),
                new Vertex(6.0f, 0.0f),
                new Vertex(6.0f, 6.0f),
                new Vertex(0.0f, 6.0f),

                new Vertex(5.0f, 2.5f),
                new Vertex(5.2f, 3.0f),
                new Vertex(5.5f, 2.7f),

                new Vertex(0.5f, 4.5f),
                new Vertex(1.0f, 5.0f),
                new Vertex(2.0f, 5.5f),

                new Vertex(2.5f, 3.5f),
                new Vertex(3.5f, 4.0f),
                new Vertex(4.0f, 3.7f),
            };

            var builder = Polygon
                .Build(vertices)
                .AddVertices(1, 2, 3, 4)
                .AddHole(5, 6, 7)
                .AddHole(8, 9, 10)
                .AddHole(11, 12, 13);

            return builder.Close();
        }
    }
}
