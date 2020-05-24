namespace Ruttmann.PolygonTriangulation.Seidel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class MonotoneChain
    {
        private MonotoneChain next;

        public MonotoneChain(VertexChain vnum)
        {
            this.Vnum = vnum;
        }

        public VertexChain Vnum { get; }

        public MonotoneChain Next
        {
            get
            {
                return this.next;
            }

            set
            {
                this.next = value;
                value.Prev = this;
            }
        }

        public MonotoneChain Prev { get; private set; }
    }
}
