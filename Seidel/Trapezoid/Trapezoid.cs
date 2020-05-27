namespace Ruttmann.PolygonTriangulation.Seidel
{
    using Vertex = System.Numerics.Vector2;

    public class Trapezoid
    {
        // HOBO
        static int count = 0;

        public Trapezoid()
        {
            this.u = new Trapezoid[2];
            this.d = new Trapezoid[2];
        }

        public Trapezoid[] u { get; }

        public Trapezoid[] d { get; }

        public LocationNode TreeNode { get; set; }

        public Trapezoid Third { get; set; }

        public bool ThirdFromLeft { get; set; }

        public Vertex hi { get; set; }

        public Vertex lo { get; set; }

        public ISegment lseg { get; set; }

        public ISegment rseg { get; set; }

        public int Id { get; } = ++count;

        internal void Invalidate()
        {
        }

        public Trapezoid GetDownlinkWithSameSegment(ISegment segment, bool leftSide)
        {
            for (int i = 0; i < 2; i++)
            {
                var activeSegment = leftSide ? this.d[i]?.rseg : this.d[i]?.lseg;
                if (activeSegment == segment)
                {
                    return d[i];
                }
            }

            return null;
        }

        internal void ReplaceDownlink(Trapezoid replaced)
        {
            for (int i = 0; i < 2; i++)
            {
                var down = replaced.d[i];
                this.d[i] = down;
                if (down != null)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        if (down.u[j] == replaced)
                        {
                            down.u[j] = this;
                        }
                    }
                }
            }
        }
    }
}
