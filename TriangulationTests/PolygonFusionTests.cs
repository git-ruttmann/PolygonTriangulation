namespace TriangulationTests
{
    using System;
    using System.Linq;

    using Vertex = System.Numerics.Vector2;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using PolygonTriangulation;

    /// <summary>
    /// Test point fusion of polygons (an inner polygon touches the outer polygon in one point)
    /// </summary>
    [TestClass]
    public class PolygonFusionTests
    {
        /// <summary>
        /// Conected in on the left of the inner polygon. (first transition, then opening cusp)
        /// </summary>
        [TestMethod]
        public void InnerPolygonOnLeft()
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

            Assert.AreEqual("0 2 4 5 6 4 7 8", polygon.Debug, "Inner polygon must be fusioned inside the outer polygon");

            polygon = Polygon.Build(sortedVertices)
                .AddVertices(0, 2, 4, 7, 8)
                .ClosePartialPolygon()
                .AddVertices(5, 6, 4)
                .Close(4);

            Assert.AreEqual("0 2 4 5 6 4 7 8", polygon.Debug, "Inner polygon must be fusioned inside the outer polygon");

            polygon = Polygon.Build(sortedVertices)
                .AddVertices(0, 2, 4, 7, 8)
                .ClosePartialPolygon()
                .AddVertices(6, 4, 5)
                .Close(4);

            Assert.AreEqual("0 2 4 5 6 4 7 8", polygon.Debug, "Inner polygon must be fusioned inside the outer polygon");
            var s = string.Join(" ", polygon.OrderedVertexes.Select(x => $"{x.Prev}>{x.Id}>{x.Next}"));
        }


        /// <summary>
        /// Join three polygons at one vertex.
        /// </summary>
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

            Assert.AreEqual("3 0 1 3 2 5 3 4 6", polygon.Debug, "Multiple polygons must fusion in clockwise order");
            
            polygon = Polygon.Build(sortedVertices)
                .AddVertices(3, 0, 1)
                .ClosePartialPolygon()
                .AddVertices(3, 4, 6)
                .ClosePartialPolygon()
                .AddVertices(3, 2, 5)
                .Close(3);

            Assert.AreEqual("3 0 1 3 2 5 3 4 6", polygon.Debug, "Multiple polygons must fusion in clockwise order");
        }

        /// <summary>
        /// Join three polygons at the same left and the same right vertex.
        /// </summary>
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
                .Close(0, 7);

            Assert.AreEqual("0 2 7 1 0 6 7 5 0 4 7 3", polygon.Debug, "Multiple polygons must fusion in clockwise order");
        }
    }
}
