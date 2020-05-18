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
            var segments = this.PolygonOne().ToArray();
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
        public void AddSegmentsSimplceConcave()
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
        }

        [TestMethod]
        public void AddSegments()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            var segments = this.PolygonOne().ToArray();
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

            for (int i = 0; i < result.Length; i += 3)
            {
                Console.WriteLine($"{result[i + 0]} {result[i + 1]} {result[i + 2]}");
            }

            Assert.AreEqual(23, firstInsideTriangle.Id);
        }

        [TestMethod]
        public void LocatePointAfterInitializeTree()
        {
            var segments = this.PolygonOne().ToArray();
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
            var first = this.PolygonOne();
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

        private Segment PolygonOne()
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

        private Segment PolygonDoubleTriangleWithConcave()
        {
            var builder = new PolygonBuilder();
            builder.Add(new Vector2(0.5f, 0.0f));
            builder.Add(new Vector2(1.5f, 2.0f));
            builder.Add(new Vector2(0.0f, 3.0f));
            builder.Add(new Vector2(1.0f, 1.5f));
            return builder.Close();
        }
    }
}
