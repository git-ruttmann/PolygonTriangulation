namespace Ruttmann.PolygonTriangulation.Seidel
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Numerics;

    [DebuggerDisplay("{Debug()}")]
    public class LocationNode
    {
        private enum NodeType
        {
            X,
            Y,
            Sink,
        };

        private NodeType nodeType;
        private LocationNode left;
        private LocationNode right;
        private Vector2 yval;
        private Segment segnum;

        // HOBO
        static int count = 0;
        private readonly int id = ++count;

        private LocationNode(Trapezoid trapezoid)
        {
            this.nodeType = NodeType.Sink;
            this.Trapezoid = trapezoid;
            trapezoid.TreeNode = this;
        }

        private LocationNode(Segment segment)
        {
            this.nodeType = NodeType.X;
            this.segnum = segment;
        }

        private LocationNode(Vector2 yValue)
        {
            this.nodeType = NodeType.Y;
            this.yval = yValue;
        }

        public Trapezoid Trapezoid { get; set; }

        private LocationNode Left
        {
            get => left;
            set
            {
                this.VerifyChildType(value);
                this.left = value;
                if (value != null)
                {
                    value.Parent = this;
                }
            }
        }

        private LocationNode Right
        {
            get => right;
            set
            {
                this.VerifyChildType(value);
                this.right = value;
                if (value != null)
                {
                    value.Parent = this;
                }
            }
        }

        private LocationNode Parent { get; set; }

        public static LocationNode CreateRoot(Trapezoid left, Trapezoid right, Trapezoid topMost, Trapezoid bottomMost, Vector2 low, Vector2 high, Segment segnum)
        {
            var segmentNode = new LocationNode(segnum);
            segmentNode.Left = new LocationNode(left);
            segmentNode.Right = new LocationNode(right);

            var lowNode = new LocationNode(low);
            lowNode.Left = new LocationNode(bottomMost);
            lowNode.Right = segmentNode;

            var highNode = new LocationNode(high);
            highNode.Right = new LocationNode(topMost);
            highNode.Left = lowNode;
            return highNode;
        }

        public IEnumerable<Trapezoid> Trapezoids
        {
            get
            {
                return new TrapezoidEnumerator(this);
            }
        }

        public Trapezoid LocateEndpoint(Vector2 vertex, Vector2 vo)
        {
            var node = this;
            while (true)
            {
                switch (node.nodeType)
                {
                    case NodeType.Y:
                        var yCompare = VertexComparer.Instance.Compare(vertex, node.yval);
                        if (yCompare > 0)
                        {
                            node = node.Right;
                        }
                        else if (yCompare < 0)
                        {
                            node = node.Left;
                        }
                        else if (VertexComparer.Instance.Compare(vo, node.yval) > 0)
                        {
                            node = node.Right;
                        }
                        else
                        {
                            node = node.Left;
                        }
                        break;

                    case NodeType.X:
                        if (VertexComparer.Instance.Equal(vertex, node.segnum.Start) || VertexComparer.Instance.Equal(vertex, node.segnum.End))
                        {
                            if (VertexComparer.Instance.EqualY(vertex, vo)) /* horizontal segment */
                            {
                                node = (vo.X < vertex.X) ? node.Left : node.Right;
                            }
                            else
                            {
                                node = VertexComparer.Instance.PointIsLeftOfSegment(vo, node.segnum) ? node.Left : node.Right;
                            }
                        }
                        else
                        {
                            node = VertexComparer.Instance.PointIsLeftOfSegment(vertex, node.segnum) ? node.Left : node.Right;
                        }
                        break;

                    case NodeType.Sink:
                        return node.Trapezoid;

                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        public void DumpTree()
        {
            Console.WriteLine(this.Debug());
            if (this.nodeType == NodeType.Sink)
            {
                return;
            }

            this.Left.DumpTree();
            this.Right.DumpTree();
        }

        public String Debug()
        {
            if (this.nodeType == NodeType.Sink)
            {
                var t = this.Trapezoid;
                return $"{this.id} Sink {t.Id} lo: {t.lo.X:0.00} {t.lo.Y:0.00} hi: {t.hi.X:0.00} {t.hi.Y:0.00} d {t.d[0]?.Id ?? 0} {t.d[1]?.Id ?? 0} u {t.u[0]?.Id ?? 0} {t.u[1]?.Id ?? 0}";
            }
            else if (this.nodeType == NodeType.Y)
            {
                return $"{this.id} Y {this.yval.X:0.00} {this.yval.Y:0.00}";
            }
            else if (this.nodeType == NodeType.X)
            {
                return $"{this.id} Seg {this.segnum.Id}";
            }

            return "Unkown node type";
        }

        public void SplitY(Trapezoid lowerTrapezoid, Vector2 vertex)
        {
            var upperSink = new LocationNode(this.Trapezoid);      /* Upper trapezoid sink */
            var lowerSink = new LocationNode(lowerTrapezoid);      /* Lower trapezoid sink */

            this.nodeType = NodeType.Y;
            this.Trapezoid = null;
            this.yval = vertex;

            upperSink.Trapezoid.lo = lowerSink.Trapezoid.hi = vertex;

            this.Left = lowerSink;
            this.Right = upperSink;
        }

        public void SplitX(Trapezoid lowerTrapezoid, Segment segment)
        {
            var i1 = new LocationNode(this.Trapezoid);      /* Upper trapezoid sink */
            var i2 = new LocationNode(lowerTrapezoid);      /* Lower trapezoid sink */

            this.nodeType = NodeType.X;
            this.Trapezoid = null;
            this.segnum = segment;

            // TODO: correct order?
            this.Left = i1;
            this.Right = i2;
        }

        public void ReplaceSinkAtParent(LocationNode newSinkNode)
        {
            if (this.Parent.Left == this)
                this.Parent.Left = newSinkNode;
            else
                this.Parent.Right = newSinkNode;
        }

        private void VerifyChildType(LocationNode child)
        {
            if (this.nodeType == NodeType.Sink)
            {
                throw new InvalidOperationException("Sink can't have a child");
            }

            if (this.nodeType == NodeType.X)
            {
                if (child.nodeType != NodeType.Sink)
                {
                    throw new InvalidOperationException("A segment can only have a sink as child.");
                }
            }

            if (this.nodeType == NodeType.Y && child.nodeType == NodeType.Sink)
            {
                if (!float.IsInfinity(child.Trapezoid.hi.X) && !float.IsInfinity(child.Trapezoid.lo.X))
                {
                    // this is used temporary...
                    // throw new InvalidOperationException("A Y line can have only childs which point to infinity.");
                }
            }
        }

        private class TrapezoidEnumerator : IEnumerable<Trapezoid>, IEnumerator<Trapezoid>
        {
            private readonly LocationNode rootNode;
            private readonly Stack<LocationNode> stack;

            public TrapezoidEnumerator(LocationNode locationNode)
            {
                this.rootNode = locationNode;
                this.Current = null;
                this.stack = new Stack<LocationNode>();
                this.stack.Push(this.rootNode);
            }

            public Trapezoid Current { get; private set; }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                this.stack.Clear();
            }

            public IEnumerator<Trapezoid> GetEnumerator()
            {
                return new TrapezoidEnumerator(this.rootNode);
            }

            public bool MoveNext()
            {
                while(true)
                {
                    if (this.stack.Count == 0)
                    {
                        return false;
                    }

                    var current = this.stack.Pop();
                    if (current.nodeType == NodeType.Sink)
                    {
                        this.Current = current.Trapezoid;
                        return true;
                    }
                    else
                    {
                        this.stack.Push(current.Right);
                        this.stack.Push(current.Left);
                    }
                }
            }

            public void Reset()
            {
                this.stack.Clear();
                this.stack.Push(this.rootNode);
                this.Current = null;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
