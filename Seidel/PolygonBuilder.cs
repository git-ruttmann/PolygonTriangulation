namespace Ruttmann.PolygonTriangulation.Seidel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using System.Text;

    public class PolygonBuilder
    {
        private bool firstAvailable;
        private Vector2 lastPoint;
        private PolygonSegment firstSegment;
        private int segmentIdCounter;
        private PolygonSegment lastSegment;

        public PolygonBuilder()
        {
            this.firstAvailable = false;
            this.firstSegment = null;
            this.segmentIdCounter = 0;
        }

        public void Add(Vector2 point)
        {
            if (!this.firstAvailable)
            {
                this.firstAvailable = true;
                this.firstSegment = null;
            }
            else if (this.firstSegment == null)
            {
                this.firstSegment = new PolygonSegment(this.lastPoint, point);
                this.firstSegment.SetFirst(this.segmentIdCounter);
                this.lastSegment = this.firstSegment;
            }
            else
            {
                var segment = new PolygonSegment(this.lastPoint, point);
                this.lastSegment.SetNext(segment);
                this.lastSegment = segment;
            }

            this.segmentIdCounter++;
            this.lastPoint = point;
        }

        public Segment Close()
        {
            var segment = new PolygonSegment(this.lastSegment.End, this.firstSegment.Start);
            this.lastSegment.SetNext(segment);
            segment.SetNext(this.firstSegment);
            this.firstAvailable = false;

            return this.firstSegment;
        }

        internal class PolygonSegment : Segment
        {
            public PolygonSegment(Vector2 start, Vector2 end)
                : base(start, end)
            {
            }

            public void SetNext(PolygonSegment next)
            {
                this.Next = next;
                next.Prev = this;
                if (!next.First)
                {
                    next.Id = this.Id + 1;
                }
            }

            public void SetFirst(int id)
            {
                this.Id = id;
                this.First = true;
            }
        }
    }
}
