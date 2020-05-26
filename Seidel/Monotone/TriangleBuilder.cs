namespace Ruttmann.PolygonTriangulation.Seidel
{
    using System;
    using System.Collections.Generic;

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

        public static IArrayTriangleCollector SplitAndTriangluate(Polygon polygon, IEnumerable<Tuple<int, int>> splits)
        {
            var result = new ArrayTriangleCollector();
            SplitAndTriangluate(polygon, splits, result);
            return result;
        }

        public static void SplitAndTriangluate(Polygon polygon, IEnumerable<Tuple<int, int>> splits, ITriangleCollector result)
        {
            var (triangles, monotones) = Polygon.Split(polygon, splits);
            var triangleList = new List<Polygon>(triangles);

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
            vertexStack.Push(iterator.Current);
            iterator.MoveNext();
            vertexStack.Push(iterator.Current);

            bool movedNext = iterator.MoveNext();
            while (movedNext || vertexStack.Count > 2)
            {
                if (vertexStack.Count > 1)
                {
                    var lastOnStack = vertexStack.Pop();
                    var v0 = polygon.Vertices[iterator.Current];
                    var v1 = polygon.Vertices[lastOnStack];
                    var v2 = polygon.Vertices[vertexStack.Peek()];
                    var cross = (v2.X - v0.X) * (v1.Y - v0.Y) - ((v2.Y - v0.Y) * (v1.X - v0.X));
                    var isConvexCorner = cross > 0;

                    if (isConvexCorner)
                    {
                        result.AddTriangle(vertexStack.Peek(), lastOnStack, iterator.Current);
                    }
                    else
                    {
                        vertexStack.Push(lastOnStack);
                        vertexStack.Push(iterator.Current);
                        movedNext = movedNext && iterator.MoveNext();
                    }
                }
                else
                {
                    vertexStack.Push(iterator.Current);
                    movedNext = movedNext && iterator.MoveNext();
                }
            }
        }

        private static int FindStartOfMonotonePolygon(Polygon polygon)
        {
            var iterator = polygon.Indices.GetEnumerator();
            var movedNext = iterator.MoveNext();
            var posmax = iterator.Current;
            var posmin = posmax;
            var ymax = polygon.Vertices[posmax];
            var ymin = ymax;

            movedNext = iterator.MoveNext();
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
                    posmaxNext = iterator.Current;
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