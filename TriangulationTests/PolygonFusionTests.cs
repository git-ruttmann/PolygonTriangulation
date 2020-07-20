namespace TriangulationTests
{
    using System.Linq;

    using Vertex = System.Numerics.Vector2;
    using Vector3 = System.Numerics.Vector3;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using PolygonTriangulation;

    /// <summary>
    /// Test point fusion of polygons (an inner polygon touches the outer polygon in one point)
    /// </summary>
    [TestClass]
    public class PolygonFusionTests
    {
        /// <summary>
        /// Conected in on the left of the inner polygon. The fusioned musst be the same, regardles of the start point of the inner polygon
        /// </summary>
        [TestMethod]
        public void DifferentStartOfInnerPolygon()
        {
            var sortedVertices = new[]
            {
                new Vertex(0, 0),
                new Vertex(1, 2),
                new Vertex(1, 3),  // 2
                new Vertex(2, 2),
                new Vertex(3, 3),  // 4
                new Vertex(4, 2),
                new Vertex(5, 2),
                new Vertex(5, 3),  // 7
                new Vertex(6, 1),
            };

            var polygon = Polygon.Build(sortedVertices)
                .AddVertices(0, 2, 4, 7, 8)
                .ClosePartialPolygon()
                .AddVertices(4, 5, 6)
                .Close(4);

            Assert.IsTrue(SubPolygonExists(polygon, 0, 2, 4, 5, 6, 4, 7, 8), "Inner polygon must be fusioned inside the outer polygon");

            polygon = Polygon.Build(sortedVertices)
                .AddVertices(0, 2, 4, 7, 8)
                .ClosePartialPolygon()
                .AddVertices(5, 6, 4)
                .Close(4);

            Assert.IsTrue(SubPolygonExists(polygon, 0, 2, 4, 5, 6, 4, 7, 8), "Inner polygon must be fusioned inside the outer polygon");

            polygon = Polygon.Build(sortedVertices)
                .AddVertices(0, 2, 4, 7, 8)
                .ClosePartialPolygon()
                .AddVertices(6, 4, 5)
                .Close(4);

            Assert.IsTrue(SubPolygonExists(polygon, 0, 2, 4, 5, 6, 4, 7, 8), "Inner polygon must be fusioned inside the outer polygon");
        }

        /// <summary>
        /// Join three polygons at one vertex.
        /// </summary>
        [TestMethod]
        public void TripleFusionSinglePointKeepSplit()
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

            Assert.IsTrue(SubPolygonExists(polygon, 3, 0, 1), "Multiple non-overlapping but fusioned polygons must stay separate");
            Assert.IsTrue(SubPolygonExists(polygon, 3, 4, 6), "Multiple non-overlapping but fusioned polygons must stay separate");
            Assert.IsTrue(SubPolygonExists(polygon, 3, 2, 5), "Multiple non-overlapping but fusioned polygons must stay separate");
            
            polygon = Polygon.Build(sortedVertices)
                .AddVertices(3, 0, 1)
                .ClosePartialPolygon()
                .AddVertices(3, 4, 6)
                .ClosePartialPolygon()
                .AddVertices(3, 2, 5)
                .Close(3);

            Assert.IsTrue(SubPolygonExists(polygon, 3, 0, 1), "Multiple non-overlapping but fusioned polygons must stay separate");
            Assert.IsTrue(SubPolygonExists(polygon, 3, 4, 6), "Multiple non-overlapping but fusioned polygons must stay separate");
            Assert.IsTrue(SubPolygonExists(polygon, 3, 2, 5), "Multiple non-overlapping but fusioned polygons must stay separate");
        }

        /// <summary>
        /// Join three polygons at one vertex.
        /// </summary>
        [TestMethod]
        public void TripleFusionSinglePointSplitSinglePolygon()
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
                .AddVertices(3, 0, 1, 3, 2, 5, 3, 4, 6)
                .Close(3);

            Assert.IsTrue(SubPolygonExists(polygon, 3, 0, 1), "Multiple non-overlapping must be splitted in clockwise order");
            Assert.IsTrue(SubPolygonExists(polygon, 3, 4, 6), "Multiple non-overlapping must be splitted in clockwise order");
            Assert.IsTrue(SubPolygonExists(polygon, 3, 2, 5), "Multiple non-overlapping must be splitted in clockwise order");

            polygon = Polygon.Build(sortedVertices)
                .AddVertices(3, 0, 1, 3, 4, 6, 3, 2, 5)
                .Close(3);

            Assert.IsTrue(SubPolygonExists(polygon, 3, 0, 1), "Multiple non-overlapping must be splitted in clockwise order");
            Assert.IsTrue(SubPolygonExists(polygon, 3, 4, 6), "Multiple non-overlapping must be splitted in clockwise order");
            Assert.IsTrue(SubPolygonExists(polygon, 3, 2, 5), "Multiple non-overlapping must be splitted in clockwise order");
        }

        /// <summary>
        /// A top and a bottom triangle, fusioned in the middle, must be separate.
        /// </summary>
        [TestMethod]
        public void TopBottomKeepSplit()
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

            Assert.IsTrue(SubPolygonExists(polygon, 2, 0, 1), "Polygon must not fusion");
            Assert.IsTrue(SubPolygonExists(polygon, 2, 4, 3), "Polygon must not fusion");
        }

        /// <summary>
        /// A top and a bottom triangle, fusioned in the middle, must be separate.
        /// </summary>
        [TestMethod]
        public void TopBottomSplitAtFusion()
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
                .AddVertices(0, 2, 1, 4, 2, 3)
                .Close(2);

            Assert.IsTrue(SubPolygonExists(polygon, 2, 3, 0), "Single polygon must be splitted at fusion point");
            Assert.IsTrue(SubPolygonExists(polygon, 2, 1, 4), "Single polygon must be splitted at fusion point");
        }

        /// <summary>
        /// A top and a bottom triangle, fusioned in the middle, must be separate.
        /// </summary>
        [TestMethod]
        public void LeftRightKeepSplit()
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
                .AddVertices(0, 1, 2)
                .ClosePartialPolygon()
                .AddVertices(2, 4, 3)
                .Close(2);

            Assert.IsTrue(SubPolygonExists(polygon, 2, 0, 1), "Polygon must not fusion");
            Assert.IsTrue(SubPolygonExists(polygon, 2, 4, 3), "Polygon must not fusion");
        }

        /// <summary>
        /// A top and a bottom triangle, fusioned in the middle, must be separate.
        /// </summary>
        [TestMethod]
        public void LeftRightSplitAtFusion()
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
                .AddVertices(0, 1, 2, 4, 3, 2)
                .Close(2);

            Assert.IsTrue(SubPolygonExists(polygon, 2, 0, 1), "Single polygon must be splitted at fusion point");
            Assert.IsTrue(SubPolygonExists(polygon, 2, 4, 3), "Single polygon must be splitted at fusion point");
        }

        /// <summary>
        /// Join three polygons at the same left and the same right vertex.
        /// </summary>
        [TestMethod]
        public void TripleFusionLeftAndRightKeepSplits()
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
                .Close(0, 7);

            Assert.IsTrue(SubPolygonExists(polygon, 0, 2, 7, 1));
            Assert.IsTrue(SubPolygonExists(polygon, 0, 4, 7, 3));
            Assert.IsTrue(SubPolygonExists(polygon, 0, 6, 7, 5));
        }

        /// <summary>
        /// Join three polygons at the same left and the same right vertex.
        /// </summary>
        [TestMethod]
        public void TripleFusionLeftAndRightSplitPolygon()
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
                .AddVertices(0, 2, 7, 1, 0, 4, 7, 3, 0, 6, 7, 5)
                .Close(0, 7);

            Assert.IsTrue(SubPolygonExists(polygon, 0, 2, 7, 1));
            Assert.IsTrue(SubPolygonExists(polygon, 0, 4, 7, 3));
            Assert.IsTrue(SubPolygonExists(polygon, 0, 6, 7, 5));
        }

        /// <summary>
        /// Join the hole of the polygon and split the separate outer one
        /// </summary>
        [TestMethod]
        public void SwitchFusionFromInnerToOuter()
        {
            var sortedVertices = new[]
            {
                new Vertex(0, 2),
                new Vertex(1, 0),
                new Vertex(2, 4), // 2
                new Vertex(3, 2),
                new Vertex(5, 1), // 4
                new Vertex(5, 4),
                new Vertex(6, 0), // 6
                new Vertex(6, 3),
                new Vertex(8, 4),
            };

            var polygon = Polygon.Build(sortedVertices)
                .AddVertices(1, 3, 0, 2, 3, 5, 8, 6)
                .ClosePartialPolygon()
                .AddVertices(3, 4, 7)
                .Close(3);
            Assert.IsTrue(SubPolygonExists(polygon, 1, 3, 4, 7, 3, 5, 8, 6));
            Assert.IsTrue(SubPolygonExists(polygon, 0, 2, 3));
        }

        /// <summary>
        /// Join the hole of the polygon and split the separate outer one
        /// </summary>
        [TestMethod]
        public void ThreeTrianglesInsideEachOther()
        {
            var sortedVertices = new[]
            {
                new Vertex(0, 0),
                new Vertex(2, 4),
                new Vertex(3, 1),
                new Vertex(3, 3),
                new Vertex(3, 6),
                new Vertex(4, 4),
                new Vertex(5, 5),
                new Vertex(6, 2),
                new Vertex(6, 6),
                new Vertex(7, 1),
            };

            var polygon = Polygon.Build(sortedVertices)
                .AddVertices(0, 1, 5, 3, 1, 4, 8, 9)
                .ClosePartialPolygon()
                .AddVertices(1, 2, 7, 6)
                .Close(1);
            Assert.IsTrue(SubPolygonExists(polygon, 0, 1, 2, 7, 6, 1, 4, 8, 9));
            Assert.IsTrue(SubPolygonExists(polygon, 1, 5, 3));
        }

        /// <summary>
        /// Join inner polygon at the same point. Conected in on the elft of the inner polygon. (closing cusp, then transition)
        /// </summary>
        [TestMethod]
        public void InnerPolygonTopRight()
        {
            var sortedVertices = new[]
            {
                new Vertex(0, 0),
                new Vertex(1, 2),
                new Vertex(1, 3),  // 2
                new Vertex(2, 2),
                new Vertex(3, 3),  // 4
                new Vertex(4, 2),
                new Vertex(5, 2),
                new Vertex(5, 3),  // 7
                new Vertex(6, 1),
            };

            var polygon = Polygon.Build(sortedVertices)
                .AddVertices(0, 2, 4, 7, 8)
                .ClosePartialPolygon()
                .AddVertices(4, 5, 6)
                .Close(4);

            var triangluator = new PolygonTriangulator(polygon);
            var splits = string.Join(" ", triangluator.GetSplits().OrderBy(x => x.Item1).ThenBy(x => x.Item2).Select(x => $"{x.Item1}-{x.Item2}"));
            Assert.AreEqual("6-7", splits);
        }

        /// <summary>
        /// Join inner polygon at the same point. Conected in the middle of the inner polygon. (transition on outer and transition on inner)
        /// </summary>
        [TestMethod]
        public void InnerPolygonTopMiddle()
        {
            var sortedVertices = new[]
            {
                new Vertex(0, 0),
                new Vertex(1, 2),
                new Vertex(1, 3),  // 2
                new Vertex(2, 2),
                new Vertex(3, 3),  // 4
                new Vertex(4, 2),
                new Vertex(5, 2),
                new Vertex(5, 3),  // 7
                new Vertex(6, 1),
            };

            var polygon = Polygon.Build(sortedVertices)
                .AddVertices(0, 2, 4, 7, 8)
                .ClosePartialPolygon()
                .AddVertices(4, 3, 5)
                .Close(4);

            var triangluator = new PolygonTriangulator(polygon);
            var splits = string.Join(" ", triangluator.GetSplits().OrderBy(x => x.Item1).ThenBy(x => x.Item2).Select(x => $"{x.Item1}-{x.Item2}"));
            Assert.AreEqual("2-3 5-7", splits);
        }

        /// <summary>
        /// At a fusion point, a closing cusp is above a transition. HandleClosingCusp must choose the correct pair out of 3 edges with the same right vertex.
        /// </summary>
        [TestMethod]
        public void FusionWithClosingAboveTransition()
        {
            var vertices = new[]
            {
                new Vertex(-2.04168500f, 0.05391948f),
                new Vertex(-1.73525800f, -0.39220070f),
                new Vertex(-1.59090200f, 0.81340110f),
                new Vertex(-1.28913800f, -1.04169800f),
                new Vertex(-1.18307500f, 1.50051100f),
                new Vertex(-1.16461800f, -0.62563610f),
                new Vertex(-1.13713300f, 0.22282370f),
                new Vertex(-1.10918100f, 1.08569700f),
                new Vertex(-0.98826190f, -1.09449200f),
                new Vertex(-0.96441320f, 1.46214300f),
                new Vertex(-0.96191410f, 1.11436200f),
                new Vertex(-0.94439380f, -0.58277110f),
                new Vertex(-0.38140370f, -1.20097700f),
                new Vertex(-0.24038320f, -1.22572200f),
                new Vertex(-0.07887594f, -0.41430300f),
                new Vertex(-0.02343902f, 1.29703000f),
                new Vertex(0.27925600f, -0.35023050f),
                new Vertex(0.44308910f, 1.21516900f),
                new Vertex(0.85362760f, 0.61747490f),
            };

            var polygon = Polygon.Build(vertices)
                .AddVertices(11, 14, 15, 17, 18, 16, 13, 12, 8, 3, 1, 0, 2, 4, 9, 15, 10, 7, 6, 5)
                .ClosePartialPolygon()
                .Close(15);

            var triangluator = new PolygonTriangulator(polygon);
            var splits = string.Join(" ", triangluator.GetSplits().OrderBy(x => x.Item1).ThenBy(x => x.Item2).Select(x => $"{x.Item1}-{x.Item2}"));
            Assert.AreEqual("1-2 2-3 3-4 4-5 5-8 7-9 8-11 9-10 11-12 13-14 15-16 16-17", splits);
        }

        /// <summary>
        /// Join inner polygon at the same point. Conected in on the left of the inner polygon. (first transition, then opening cusp)
        /// </summary>
        [TestMethod]
        public void InnerPolygonTopLeft()
        {
            var sortedVertices = new[]
            {
                new Vertex(0, 0),
                new Vertex(1, 2),
                new Vertex(1, 3),  // 2
                new Vertex(2, 2),
                new Vertex(3, 3),  // 4
                new Vertex(4, 2),
                new Vertex(5, 2),
                new Vertex(5, 3),  // 7
                new Vertex(6, 1),
            };

            var polygon = Polygon.Build(sortedVertices)
                .AddVertices(0, 2, 4, 7, 8)
                .ClosePartialPolygon()
                .AddVertices(4, 1, 3)
                .Close(4);

            var triangluator = new PolygonTriangulator(polygon);
            var splits = string.Join(" ", triangluator.GetSplits().OrderBy(x => x.Item1).ThenBy(x => x.Item2).Select(x => $"{x.Item1}-{x.Item2}"));
            Assert.AreEqual("0-1 1-2", splits);
        }

        /// <summary>
        /// Fusion at a point, the 2 incoming edges come first, then the 2 outgoing edges. The Polygon Line Detector must take care.
        /// </summary>
        [TestMethod]
        public void SingleFusionWithTwoIncomingEdges()
        {
            var vertices = new[]
            {
                new Vector3(0, 0, 0),
                new Vector3(0, 2, 0),
                new Vector3(1, 1, 0),
                new Vector3(2, 0, 0),
                new Vector3(2, 2, 0),
            };

            var builder = PlanePolygonBuilder.CreatePolygonBuilder();
            builder.AddEdge(vertices[1], vertices[2]);
            builder.AddEdge(vertices[3], vertices[2]);
            builder.AddEdge(vertices[2], vertices[4]);
            builder.AddEdge(vertices[2], vertices[0]);
            builder.AddEdge(vertices[0], vertices[1]);
            builder.AddEdge(vertices[4], vertices[3]);

            var result = builder.BuildPolygon();
            var polygon = result.Polygon;

            Assert.IsTrue(SubPolygonExists(polygon, 2, 4, 3), "Polygon must be splitted and have the correct direction");
            Assert.IsTrue(SubPolygonExists(polygon, 2, 0, 1), "Polygon must be splitted and have the correct direction");
        }

        /// <summary>
        /// A fusion from eddges
        /// </summary>
        [TestMethod]
        public void SingleFusionFromEdges()
        {
            var vertices = new[]
            {
                new Vector3(0, 0, 0),
                new Vector3(0, 2, 0),
                new Vector3(1, 1, 0),
                new Vector3(2, 0, 0),
                new Vector3(2, 2, 0),
            };

            var builder = PlanePolygonBuilder.CreatePolygonBuilder();
            builder.AddEdge(vertices[1], vertices[2]);
            builder.AddEdge(vertices[2], vertices[4]);
            builder.AddEdge(vertices[3], vertices[2]);
            builder.AddEdge(vertices[2], vertices[0]);
            builder.AddEdge(vertices[0], vertices[1]);
            builder.AddEdge(vertices[4], vertices[3]);

            var result = builder.BuildPolygon();
            var polygon = result.Polygon;

            Assert.IsTrue(SubPolygonExists(polygon, 2, 4, 3), "Polygon must be splitted and have the correct direction");
            Assert.IsTrue(SubPolygonExists(polygon, 2, 0, 1), "Polygon must be splitted and have the correct direction");
        }

        /// <summary>
        /// Test if the sub polygon exits
        /// </summary>
        /// <param name="polygon">the polygon</param>
        /// <param name="vertices">the vertex sequence with the sub vertices</param>
        /// <returns>true if any subpolygon matches the expected vertices in the expected sequence</returns>
        public static bool SubPolygonExists(Polygon polygon, params int[] vertices)
        {
            foreach (var subPolygonId in polygon.SubPolygonIds)
            {
                var subPolygon = polygon.SubPolygonVertices(subPolygonId).ToList();
                if (subPolygon.Count != vertices.Length)
                {
                    continue;
                }

                for (int offset = 0; offset >= 0; offset = subPolygon.IndexOf(vertices[0], offset + 1))
                {
                    bool good = true;
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        var peer = (i + offset) % vertices.Length;
                        if (vertices[i] != subPolygon[peer])
                        {
                            good = false;
                            break;
                        }
                    }

                    if (good)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
