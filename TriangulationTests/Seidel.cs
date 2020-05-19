namespace TriangulationTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using System.Text;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Ruttmann.PolygonTriangulation.Seidel;

    [TestClass]
    public class Seidel
    {
        [TestMethod]
        public void PointIsLeftOfSegmentComparer()
        {
            var segments = this.PolygonTripleStart().ToArray();
            Assert.IsTrue(VertexComparer.Instance.PointIsLeftOfSegment(new Vector2(1, 4), segments[9]));
            Assert.IsFalse(VertexComparer.Instance.PointIsLeftOfSegment(new Vector2(3, 4), segments[9]));
            Assert.IsTrue(VertexComparer.Instance.PointIsLeftOfSegment(new Vector2(1, 4.5f), segments[9]));
            Assert.IsFalse(VertexComparer.Instance.PointIsLeftOfSegment(new Vector2(3, 4.5f), segments[9]));
            Assert.IsTrue(VertexComparer.Instance.PointIsLeftOfSegment(new Vector2(1, 3.5f), segments[9]));
            Assert.IsFalse(VertexComparer.Instance.PointIsLeftOfSegment(new Vector2(3, 3.5f), segments[9]));
            Assert.IsTrue(VertexComparer.Instance.PointIsLeftOfSegment(new Vector2(1, 3.5f), segments[9]));

            Assert.IsFalse(VertexComparer.Instance.PointIsLeftOfSegment(new Vector2(2.51f, 3.5f), segments[9]), "Close after lower");
            Assert.IsTrue(VertexComparer.Instance.PointIsLeftOfSegment(new Vector2(2.49f, 3.5f), segments[9]), "Close before lower");
            Assert.IsFalse(VertexComparer.Instance.PointIsLeftOfSegment(new Vector2(1.51f, 4.5f), segments[9]), "Close after upper");
            Assert.IsTrue(VertexComparer.Instance.PointIsLeftOfSegment(new Vector2(1.49f, 4.5f), segments[9]), "Close before lower");
            Assert.IsFalse(VertexComparer.Instance.PointIsLeftOfSegment(new Vector2(2.01f, 4.0f), segments[9]), "Close after center");
            Assert.IsTrue(VertexComparer.Instance.PointIsLeftOfSegment(new Vector2(1.9f, 4.0f), segments[9]), "Close before center");
        }

        [TestMethod]
        public void AddSegmentsSimpleConcave()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            var segments = this.PolygonDoubleTriangleWithConcave().ToArray();
            var trapezoidBuilder = new TrapezoidBuilder(segments[2]);
            trapezoidBuilder.AddSegment(segments[0]);
            trapezoidBuilder.AddSegment(segments[1]);
            trapezoidBuilder.AddSegment(segments[3]);

            trapezoidBuilder.Tree.DumpTree();

            var firstInsideTriangle = trapezoidBuilder.Tree.Trapezoids
                .GroupBy(x => x.Id, (key, group) => group.First())
                .Where(x => x.rseg != null && x.lseg != null)
                .Where(x => (x.u[0] == null && x.u[1] == null) || (x.d[0] == null && x.d[1] == null))
                .Where(x => VertexComparer.Instance.Compare(x.rseg.End, x.rseg.Start) > 0)
                .OrderBy(x => x.Id)
                .First();

            Assert.AreEqual(2, firstInsideTriangle.Id);

            var buff = new Bluff();
            buff.monotonate_trapezoids(firstInsideTriangle, segments);
            var result = buff.MonotonateAll();
            Assert.IsTrue(VerifyTriangle(result, 1, 2, 4));
            Assert.IsTrue(VerifyTriangle(result, 2, 3, 4));
            Assert.AreEqual(2 * 3, result.Length);
        }

        [TestMethod]
        public void AddSegmentsSquareWithThreeNonOverlappingHoles()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            var segments = this.PolygonSquareWithThreeNonOverlappingHoles().SelectMany(x => x).ToArray();
            var trapezoidBuilder = new TrapezoidBuilder(segments[0]);
            foreach (var segment in segments.Skip(1))
            {
                trapezoidBuilder.AddSegment(segment);
            }

            var firstInsideTriangle = trapezoidBuilder.Tree.Trapezoids
                .GroupBy(x => x.Id, (key, group) => group.First())
                .Where(x => x.rseg != null && x.lseg != null)
                .Where(x => (x.u[0] == null && x.u[1] == null) || (x.d[0] == null && x.d[1] == null))
                .Where(x => VertexComparer.Instance.Compare(x.rseg.End, x.rseg.Start) > 0)
                .OrderBy(x => x.Id)
                .First();

            var buff = new Bluff();
            buff.monotonate_trapezoids(firstInsideTriangle, segments);
            var result = buff.MonotonateAll();

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
            var segments = this.PolygonTripleStart().ToArray();
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

            var firstInsideTriangle = trapezoidBuilder.Tree.Trapezoids
                .GroupBy(x => x.Id, (key, group) => group.First())
                .Where(x => x.rseg != null && x.lseg != null)
                .Where(x => (x.u[0] == null && x.u[1] == null) || (x.d[0] == null && x.d[1] == null))
                .Where(x => VertexComparer.Instance.Compare(x.rseg.End, x.rseg.Start) > 0)
                .OrderBy(x => x.Id)
                .First();

            var bluff = new Bluff();
            bluff.monotonate_trapezoids(firstInsideTriangle, segments);
            var result = bluff.MonotonateAll();

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

            for (int i = 0; i < result.Length; i += 3)
            {
                Console.WriteLine($"{result[i + 0]} {result[i + 1]} {result[i + 2]}");
            }
        }

        [TestMethod]
        public void LocatePointAfterInitializeTree()
        {
            var segments = this.PolygonTripleStart().ToArray();
            var locationTree = new TrapezoidBuilder(segments[9]).Tree;

            var trapezoid = locationTree.LocateEndpoint(new Vector2(1, 4), new Vector2(0, 4));
            Assert.IsNull(trapezoid.lseg);
            Assert.AreEqual(trapezoid.rseg, segments[9], "Must find the left trapezoid and the segment is right to it");

            trapezoid = locationTree.LocateEndpoint(new Vector2(3, 4), new Vector2(3, 4));
            Assert.AreEqual(trapezoid.lseg, segments[9], "Must find the left trapezoid and the segment is right to it");
            Assert.IsNull(trapezoid.rseg);

            trapezoid = locationTree.LocateEndpoint(new Vector2(2.5f, 3.5f), new Vector2(3, 4));
            Assert.AreEqual(trapezoid.lseg, segments[9], "Joined at point but more to the right");
            Assert.IsNull(trapezoid.rseg);

            trapezoid = locationTree.LocateEndpoint(new Vector2(2.5f, 3.5f), new Vector2(3, 1));
            Assert.IsNull(trapezoid.lseg);
            Assert.IsNull(trapezoid.rseg, "Joined at point but more to the left => must be treated as 'lower than the existing trapezoids'");
        }

        [TestMethod]
        public void ConstructSegments()
        {
            var first = this.PolygonTripleStart();
            Assert.IsTrue(first.First);
            Assert.AreEqual(1, first.Id);

            var segmentCount = 0;
            foreach (var segment in first)
            {
                segmentCount++;
                Assert.AreEqual(segmentCount, segment.Id);
            }

            Assert.AreEqual(18, segmentCount);
            Assert.AreEqual(1, first.Id);
            Assert.AreEqual(1, first.First().Id);
            Assert.AreEqual(18, first.Count());
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

        private Segment PolygonTripleStart()
        {
            var builder = new PolygonBuilder();
            builder.Add(new Vector2(4.0f, 1.0f));
            builder.Add(new Vector2(4.0f, 2.0f));
            builder.Add(new Vector2(5.0f, 0.0f));
            builder.Add(new Vector2(6.0f, 1.0f));
            builder.Add(new Vector2(6.0f, 5.0f));
            builder.Add(new Vector2(5.0f, 6.0f));
            builder.Add(new Vector2(5.5f, 4.5f));
            builder.Add(new Vector2(4.5f, 3.0f));
            builder.Add(new Vector2(3.5f, 4.5f));
            builder.Add(new Vector2(2.5f, 3.5f));
            builder.Add(new Vector2(1.5f, 4.5f));
            builder.Add(new Vector2(1.0f, 5.5f));
            builder.Add(new Vector2(0.0f, 4.5f));
            builder.Add(new Vector2(0.0f, 1.0f));
            builder.Add(new Vector2(1.0f, 0.0f));
            builder.Add(new Vector2(2.0f, 1.0f));
            builder.Add(new Vector2(2.0f, 2.0f));
            builder.Add(new Vector2(3.0f, 0.0f));
            return builder.Close();
        }

        /// <summary>
        /// Return a simple polygon with two triangles and one concave vertex
        /// </summary>
        /// <returns></returns>
        private Segment PolygonDoubleTriangleWithConcave()
        {
            var builder = new PolygonBuilder();
            builder.Add(new Vector2(0.5f, 0.0f));
            builder.Add(new Vector2(1.5f, 2.0f));
            builder.Add(new Vector2(0.0f, 3.0f));
            builder.Add(new Vector2(1.0f, 1.5f));
            return builder.Close();
        }

        /// <summary>
        /// Return a simple polygon with two triangles and one concave vertex
        /// </summary>
        /// <returns></returns>
        private IEnumerable<Segment> PolygonSquareWithThreeNonOverlappingHoles()
        {
            var segments = new List<Segment>();
            var builder = new PolygonBuilder();
            builder.Add(new Vector2(0.0f, 0.0f));
            builder.Add(new Vector2(6.0f, 0.0f));
            builder.Add(new Vector2(6.0f, 6.0f));
            builder.Add(new Vector2(0.0f, 6.0f));
            segments.Add(builder.Close());

            builder.Add(new Vector2(0.5f, 1.0f));
            builder.Add(new Vector2(1.0f, 2.0f));
            builder.Add(new Vector2(2.0f, 1.5f));
            segments.Add(builder.Close());

            builder.Add(new Vector2(0.5f, 4.0f));
            builder.Add(new Vector2(1.0f, 5.0f));
            builder.Add(new Vector2(2.0f, 4.5f));
            segments.Add(builder.Close());

            builder.Add(new Vector2(3.0f, 3.0f));
            builder.Add(new Vector2(5.0f, 3.5f));
            builder.Add(new Vector2(5.0f, 2.5f));
            segments.Add(builder.Close());

            return segments;
        }
    }
}
