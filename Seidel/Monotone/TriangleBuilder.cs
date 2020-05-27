namespace Ruttmann.PolygonTriangulation.Seidel
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Receive triangles
    /// </summary>
    public interface ITriangleCollector
    {
        /// <summary>
        /// Add a triangle
        /// </summary>
        /// <param name="v0">id of vertex 0</param>
        /// <param name="v1">id of vertex 1</param>
        /// <param name="v"">id of vertex 2</param>
        void AddTriangle(int v0, int v1, int v2);
    }

    /// <summary>
    /// Receive triangles and provide the recieved triangles
    /// </summary>
    public interface IArrayTriangleCollector : ITriangleCollector
    {
        /// <summary>
        /// Gets the triangles
        /// </summary>
        int[] Triangles { get; }
    }

    /// <summary>
    /// Converts the trapezoid structure to splits
    /// </summary>
    public class TriangleBuilder
    {
        public static IArrayTriangleCollector CreateTriangleCollecor()
        {
            return new ArrayTriangleCollector();
        }

        public static int[] SplitAndTriangluate(Polygon polygon, IEnumerable<Tuple<int, int>> splits)
        {
            var result = new ArrayTriangleCollector();
            SplitAndTriangluate(polygon, splits, result);
            return result.Triangles;
        }

        public static void SplitAndTriangluate(Polygon polygon, IEnumerable<Tuple<int, int>> splits, ITriangleCollector result)
        {
            var monotones = Polygon.Split(polygon, splits, result);
            var triangleList = new List<Polygon>();

            foreach (var monotonePolygon in monotones)
            {
                var startPoint = FindStartOfMonotonePolygon(monotonePolygon);
                if (startPoint < 0)
                {
                    triangleList.Add(monotonePolygon);
                }
                else
                {
                    TriangulateMonotonePolygon(monotonePolygon, startPoint, result);
                }
            }

            foreach (var triangle in triangleList)
            {
                var simpleIterator = triangle.Indices.GetEnumerator();
                simpleIterator.MoveNext();
                var v0 = simpleIterator.Current;
                simpleIterator.MoveNext();
                var v1 = simpleIterator.Current;
                simpleIterator.MoveNext();
                result.AddTriangle(v0, v1, simpleIterator.Current);
            }
        }

        public static void TriangulateMonotonePolygon(Polygon polygon, ITriangleCollector result)
        {
            var startPoint = FindStartOfMonotonePolygon(polygon);
            TriangulateMonotonePolygon(polygon, startPoint, result);
        }

        public static void TriangulateMonotonePolygon(Polygon polygon, int startPoint, ITriangleCollector result)
        {
            var vertexStack = new Stack<int>();

            // push the first two points
            var iterator = polygon.IndicesStartingAt(startPoint).GetEnumerator();
            iterator.MoveNext();
            var third = iterator.Current;
            iterator.MoveNext();
            var second = iterator.Current;
            iterator.MoveNext();
            var current = iterator.Current;

            while (true)
            {
                var v0 = polygon.Vertices[current];
                var v1 = polygon.Vertices[second];
                var v2 = polygon.Vertices[third];
                var cross = (v2.X - v0.X) * (v1.Y - v0.Y) - ((v2.Y - v0.Y) * (v1.X - v0.X));
                var isConvexCorner = cross > 0;

                if (isConvexCorner)
                {
                    result.AddTriangle(current, third, second);
                    if (vertexStack.Count > 0)
                    {
                        second = third;
                        third = vertexStack.Pop();
                    }
                    else if (!iterator.MoveNext())
                    {
                        break;
                    }
                    else
                    {
                        second = current;
                        current = iterator.Current;
                    }
                }
                else
                {
                    vertexStack.Push(third);
                    third = second;
                    second = current;
                    if (!iterator.MoveNext())
                    {
                        throw new InvalidOperationException("Triangle is incomplete");
                    }

                    current = iterator.Current;
                }
            }
        }

        /// <summary>
        /// Find the point in the polygon that starts at the monotone side
        /// </summary>
        /// <param name="polygon">the polygon</param>
        /// <returns>highest/lowest point in the polygon, depending if itss left hand or right hand. -1 if its a triangle.</returns>
        private static int FindStartOfMonotonePolygon(Polygon polygon)
        {
            var iterator = polygon.Indices.GetEnumerator();
            iterator.MoveNext();
            var first = iterator.Current;
            var posmax = first;
            var posmin = first;
            var ymax = polygon.Vertices[posmax];
            var ymin = ymax;

            var movedNext = iterator.MoveNext();
            var posmaxNext = iterator.Current;
            var count = 1;

            while (movedNext)
            {
                var index = iterator.Current;
                var vertex = polygon.Vertices[index];
                movedNext = iterator.MoveNext();

                if (VertexComparer.Instance.Compare(vertex, ymax) > 0)
                {
                    ymax = vertex;
                    posmax = index;
                    posmaxNext = movedNext ? iterator.Current : first;
                }

                if (VertexComparer.Instance.Compare(vertex, ymin) < 0)
                {
                    ymin = vertex;
                    posmin = index;
                }

                count++;
            }

            if (count == 3)
            {
                return -1;
            }

            if (posmin == posmaxNext)
            {
                // LHS is a single segment and it's next in the chain
                return posmaxNext;
            }
            else
            {
                return posmax;
            }
        }

        /// <summary>
        /// A simple triangle collector
        /// </summary>
        private class ArrayTriangleCollector : IArrayTriangleCollector
        {
            private List<int> list;

            public ArrayTriangleCollector()
            {
                this.list = new List<int>();
            }

            public int[] Triangles => this.list.ToArray();

            public void AddTriangle(int v0, int v1, int v2)
            {
                this.list.Add(v0);
                this.list.Add(v1);
                this.list.Add(v2);
            }
        }
    }
}