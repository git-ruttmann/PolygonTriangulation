namespace Ruttmann.PolygonTriangulation.Seidel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using System.Text;
    using System.Threading.Tasks;

    public class VertexChain
    {
        public int id;
        public Vector2 pt;
        public VertexChain[] vnext = new VertexChain[4];     /* next vertices for the 4 chains */
        public MonotoneChain[] vpos = new MonotoneChain[4];         /* position of v in the 4 chains */
        public int nextfree;
    }
}
