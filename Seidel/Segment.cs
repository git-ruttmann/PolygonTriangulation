namespace Ruttmann.PolygonTriangulation.Seidel
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Numerics;

    [DebuggerDisplay("{Id} {Start} {End}")]
    public abstract class Segment : IEnumerable<Segment>
    {
        protected Segment(Vector2 start, Vector2 end)
        {
            this.Start = start;
            this.End = end;
        }

        public Segment Prev { get; protected set; }

        public Segment Next { get; protected set; }

        public bool First { get; protected set; }

        public int Id { get; protected set; }

        public Vector2 v0 => this.Start;

        public Vector2 v1 => this.End;

        public Vector2 Start { get; }

        public Vector2 End { get; }

        public bool is_inserted { get; set; }

        public IEnumerator<Segment> GetEnumerator()
        {
            return new SegmentEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        /// <summary>
        /// Iterate over segements
        /// </summary>
        private class SegmentEnumerator : IEnumerator<Segment>
        {
            private Segment first;

            public SegmentEnumerator(Segment segment)
            {
                this.first = segment;
                this.Current = null;
            }

            public Segment Current { get; private set; }

            object IEnumerator.Current => throw new NotImplementedException();

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
