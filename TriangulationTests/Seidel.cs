namespace TriangulationTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Vertex = System.Numerics.Vector2;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Ruttmann.PolygonTriangulation.Seidel;

    [TestClass]
    public class Seidel
    {
        [TestMethod]
        public void SplitSimpleConcave()
        {
            var polygon = this.PolygonDoubleTriangleWithConcave();

            Assert.AreEqual("0 1 2 3", String.Join(" ", polygon.Indices));


            var triangleCollector = TriangleBuilder.CreateTriangleCollecor();
            var result = Polygon.Split(polygon, new[] { Tuple.Create(1, 3) }, triangleCollector);

            Assert.AreEqual(0, result.Length);
            Assert.AreEqual(6, triangleCollector.Triangles.Length);
        }

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

        [TestMethod]
        public void AddSegmentsSquareWithThreeNonOverlappingHoles()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            var polygon = this.PolygonSquareWithThreeNonOverlappingHoles();
            var segments = polygon.AllSegments.ToArray();
            var trapezoidBuilder = new TrapezoidBuilder(segments[0]);
            foreach (var segment in segments.Skip(1))
            {
                trapezoidBuilder.AddSegment(segment);
            }

            var firstInsideTriangle = trapezoidBuilder.GetFirstInsideTriangle();
            var splits = TrapezoidToSplits.ExtractSplits(firstInsideTriangle);
            var result = TriangleBuilder.SplitAndTriangluate(polygon, splits);

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
