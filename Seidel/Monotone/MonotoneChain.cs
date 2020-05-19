namespace Ruttmann.PolygonTriangulation.Seidel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class MonotoneChain
    {
        public VertexChain vnum;
        public MonotoneChain next;
        public MonotoneChain prev;
    }
}
