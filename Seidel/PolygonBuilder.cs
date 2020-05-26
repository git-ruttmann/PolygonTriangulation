namespace Ruttmann.PolygonTriangulation.Seidel
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Numerics;

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

        public ISegment Close()
        {
            var segment = new PolygonSegment(this.lastSegment.End, this.firstSegment.Start);
            this.lastSegment.SetNext(segment);
            segment.SetNext(this.firstSegment);
            this.firstAvailable = false;

            return this.firstSegment;
        }

        [DebuggerDisplay("{Id} {Start} {End}")]
        internal class PolygonSegment : ISegment
        {
            public ISegment Prev { get; private set; }

            public ISegment Next { get; private set; }

            public bool First { get; private set; }

            public int Id { get; private set; }

            public Vector2 v0 => this.Start;

            public Vector2 v1 => this.End;
            
            public Vector2 Start { get; private set; }

            public Vector2 End { get; private set; }

            public PolygonSegment(Vector2 start, Vector2 end)
            {
                this.Start = start;
                this.End = end;
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

            public IEnumerator<ISegment> GetEnumerator()
            {
                return new SegmentEnumerator(this);
            }

            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

            /// <summary>
            /// Iterate over segements
            /// </summary>
            private class SegmentEnumerator : IEnumerator<ISegment>
            {
                private ISegment first;

                public SegmentEnumerator(ISegment segment)
                {
                    this.first = segment;
                    this.Current = null;
                }

                public ISegment Current { get; private set; }

                object IEnumerator.Current => this.Current;

                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    if (this.Current == null)
                    {
                        this.Current = first;
                        return true;
                    }

                    if (this.Current.Next.First)
                    {
                        return false;
                    }

                    this.Current = this.Current.Next;
                    return true;
                }

                public void Reset()
                {
                    this.Current = this.first;
                }
            }
        }
    }
}
