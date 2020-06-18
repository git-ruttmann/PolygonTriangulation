
namespace TriangulationTests
{
    using System;
    using System.Linq;

    using Vertex = System.Numerics.Vector2;
    using Plane = System.Numerics.Plane;
    using Vector3 = System.Numerics.Vector3;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using PolygonTriangulation;
    using System.Collections.Generic;

    /// <summary>
    /// Tests using 3D edges as input
    /// </summary>
    [TestClass]
    public class EdgesToPolygon
    {
        /// <summary>
        /// Two polygons touch each other. Test at edge collector level.
        /// </summary>
        [TestMethod]
        public void EdgesWithTwoTouchingPolygons()
        {
            var builder = PlanePolygonBuilder.CreatePolygonBuilder();
            builder.AddEdge(new Vector3(1.363289f, 0.325342f, 0), new Vector3(0.532633f, -0.758319f, 0));
            builder.AddEdge(new Vector3(1.874113f, 0.991756f, 0), new Vector3(1.363289f, 0.325342f, 0));
            builder.AddEdge(new Vector3(-0.884818f, 1.403969f, 0), new Vector3(-0.616898f, 1.753494f, 0));
            builder.AddEdge(new Vector3(-1.009216f, 1.241681f, 0), new Vector3(-0.884818f, 1.403969f, 0));
            builder.AddEdge(new Vector3(-0.256794f, 0.265680f, 0), new Vector3(-1.009216f, 1.241681f, 0));
            builder.AddEdge(new Vector3(0.532633f, -0.758319f, 0), new Vector3(-0.256793f, 0.265680f, 0));
            builder.AddEdge(new Vector3(0.989307f, 2.139478f, 0), new Vector3(1.874113f, 0.991756f, 0));
            builder.AddEdge(new Vector3(-0.616898f, 1.753494f, 0), new Vector3(-0.145745f, 1.866717f, 0));
            builder.AddEdge(new Vector3(-0.145745f, 1.866717f, 0), new Vector3(0.779111f, 2.088966f, 0));
            builder.AddEdge(new Vector3(0.779111f, 2.088966f, 0), new Vector3(0.989307f, 2.139478f, 0));
            builder.AddEdge(new Vector3(0.951598f, 1.471772f, 0), new Vector3(0.935504f, 1.772997f, 0));
            builder.AddEdge(new Vector3(1.010642f, 0.366718f, 0), new Vector3(0.951598f, 1.471772f, 0));
            builder.AddEdge(new Vector3(0.935504f, 1.772997f, 0), new Vector3(-0.145745f, 1.866717f, 0));
            builder.AddEdge(new Vector3(-0.094393f, 0.905628f, 0), new Vector3(-0.070607f, 0.460440f, 0));
            builder.AddEdge(new Vector3(-0.145745f, 1.866717f, 0), new Vector3(-0.094393f, 0.905628f, 0));
            builder.AddEdge(new Vector3(0.449942f, 0.415320f, 0), new Vector3(1.010642f, 0.366718f, 0));
            builder.AddEdge(new Vector3(-0.070607f, 0.460440f, 0), new Vector3(0.449942f, 0.415320f, 0));

            var polygon = builder.BuildPolygon().Polygon;
            Assert.AreEqual(1, polygon.SubPolygonIds.Count());

            var triangulator = new PolygonTriangulator(polygon);
            var splits = triangulator.GetSplits();

            Assert.AreEqual(1, splits.Count(x => x.Item1 == 4 && x.Item2 == 4), "sub polygons must connect at vertex 4");

            var triangles = triangulator.BuildTriangles();
        }

        /// <summary>
        /// Unity reports "missing edge" in dictionary, but test is not reproducing that.
        /// </summary>
        [TestMethod]
        public void MissingEdgeInDictionary()
        {
            var builder = PlanePolygonBuilder.CreatePolygonBuilder();
            builder.AddEdge(new Vector3(1.88157800f, 0.28075720f, 0), new Vector3(1.61366200f, -0.06876162f, 0));
            builder.AddEdge(new Vector3(2.00597400f, 0.44304200f, 0), new Vector3(1.88157800f, 0.28075720f, 0));
            builder.AddEdge(new Vector3(-0.36652940f, 1.35938300f, 0), new Vector3(0.46412470f, 2.44304200f, 0));
            builder.AddEdge(new Vector3(-0.87735590f, 0.69296650f, 0), new Vector3(-0.36652910f, 1.35938300f, 0));
            builder.AddEdge(new Vector3(-0.49218100f, 0.19333940f, 0), new Vector3(-0.87735590f, 0.69296650f, 0));
            builder.AddEdge(new Vector3(0.00744601f, -0.45474870f, 0), new Vector3(-0.49218100f, 0.19333940f, 0));
            builder.AddEdge(new Vector3(1.14250100f, -0.18199130f, 0), new Vector3(0.21764320f, -0.40423650f, 0));
            builder.AddEdge(new Vector3(1.61366200f, -0.06876162f, 0), new Vector3(1.14250100f, -0.18199130f, 0));
            builder.AddEdge(new Vector3(0.21764320f, -0.40423650f, 0), new Vector3(0.00744601f, -0.45474870f, 0));
            builder.AddEdge(new Vector3(0.46412470f, 2.44304200f, 0), new Vector3(1.09203400f, 1.62855300f, 0));
            builder.AddEdge(new Vector3(1.09203400f, 1.62855300f, 0), new Vector3(2.00597400f, 0.44304200f, 0));
            builder.AddEdge(new Vector3(1.14250100f, -0.18199130f, 0), new Vector3(1.06736400f, 1.22428300f, 0));
            builder.AddEdge(new Vector3(0.50145080f, 1.27333500f, 0), new Vector3(-0.01388439f, 1.31800400f, 0));
            builder.AddEdge(new Vector3(1.06736400f, 1.22428300f, 0), new Vector3(0.50145070f, 1.27333500f, 0));
            builder.AddEdge(new Vector3(0.01580662f, 0.76231190f, 0), new Vector3(0.06125379f, -0.08827398f, 0));
            builder.AddEdge(new Vector3(-0.01388440f, 1.31800400f, 0), new Vector3(0.01580662f, 0.76231190f, 0));
            builder.AddEdge(new Vector3(0.17275270f, -0.09793866f, 0), new Vector3(1.14250100f, -0.18199130f, 0));
            builder.AddEdge(new Vector3(0.06125382f, -0.08827414f, 0), new Vector3(0.17275270f, -0.09793866f, 0));

            var _ = builder.BuildPolygon();
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
