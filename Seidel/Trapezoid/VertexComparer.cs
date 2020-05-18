using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Numerics;

namespace Ruttmann.PolygonTriangulation.Seidel
{
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

        public bool PointIsLeftOfSegment(Vector2 vertex, Segment segnum)
        {
            if (this.EqualY(vertex, segnum.Start))
            {
                return vertex.X < segnum.Start.X;
            }
            else if (this.EqualY(vertex, segnum.End))
            {
                return vertex.X < segnum.End.X;
            }
            else if (this.EqualY(segnum.End, segnum.Start))
            {
                // Hmmm, is it ever called like that?
                throw new InvalidOperationException("Unexpected call variant - handling unclear");
                //// return vertex.X < segnum.End.X;
            }

            var segmentVector = segnum.End - segnum.Start;
            var relation = (vertex.Y - segnum.Start.Y) / segmentVector.Y;
            var xAtVertex = segnum.Start.X + relation * segmentVector.X;

            return vertex.X < xAtVertex;
        }
    }
}
