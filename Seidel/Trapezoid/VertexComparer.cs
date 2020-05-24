namespace Ruttmann.PolygonTriangulation.Seidel
{
    using System;
    using System.Collections.Generic;
    using System.Numerics;

    public class VertexComparer : IComparer<Vector2>
    {
        const float epsilon = 0.1E-5f;

        public static VertexComparer Instance { get; } = new VertexComparer();

        public bool Equal(Vector2 a, Vector2 b)
        {
            if (Math.Abs(a.Y - b.Y) < epsilon)
            {
                if (Math.Abs(a.X - b.X) < epsilon)
                {
                    return true;
                }
            }

            return false;
        }

        public bool EqualY(in Vector2 a, in Vector2 b)
        {
            return Math.Abs(a.Y - b.Y) < epsilon;
        }

        /// <inheritdoc/>
        public int Compare(Vector2 a, Vector2 b)
        {
            if (a.Y < b.Y - epsilon)
            {
                return -1;
            }
            else if (a.Y > b.Y + epsilon)
            {
                return 1;
            }
            else if (a.X < b.X - epsilon)
            {
                return -1;
            }
            else if (a.X > b.X + epsilon)
            {
                return 1;
            }

            return 0;
        }

        public bool PointIsLeftOfSegment(Vector2 vertex, Segment segment)
        {
            if (this.EqualY(vertex, segment.Start))
            {
                return vertex.X < segment.Start.X;
            }
            else if (this.EqualY(vertex, segment.End))
            {
                return vertex.X < segment.End.X;
            }
            else if (this.EqualY(segment.End, segment.Start))
            {
                return vertex.X < segment.End.X;
            }

            var segmentVector = segment.End - segment.Start;
            var relation = (vertex.Y - segment.Start.Y) / segmentVector.Y;
            var xAtVertex = segment.Start.X + relation * segmentVector.X;

            return vertex.X < xAtVertex;
        }
    }
}
