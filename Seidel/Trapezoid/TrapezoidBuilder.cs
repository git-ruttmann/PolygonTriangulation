namespace Ruttmann.PolygonTriangulation.Seidel
{
    using System;
    using System.Numerics;

    public class TrapezoidBuilder
    {
        public TrapezoidBuilder(Segment segment)
        {
            var (low, high, _) = SortSegment(segment);
            var left = CreateTrapezoid();     /* middle left */
            var right = CreateTrapezoid();     /* middle right */
            var bottomMost = CreateTrapezoid();
            var topMost = CreateTrapezoid();
            topMost.hi = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            bottomMost.lo = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

            left.hi = right.hi = topMost.lo = high;
            left.lo = right.lo = bottomMost.hi = low;
            left.rseg = right.lseg = segment;
            left.u[0] = right.u[0] = topMost;
            left.d[0] = right.d[0] = bottomMost;
            topMost.d[0] = bottomMost.u[0] = left;
            topMost.d[1] = bottomMost.u[1] = right;

            segment.is_inserted = true;

            this.Tree = LocationNode.CreateRoot(left, right, topMost, bottomMost, low, high, segment);
        }

        /// <summary>
        /// Get's the storage tree
        /// </summary>
        public LocationNode Tree { get; }

        public void AddSegment(Segment segment)
        {
            Trapezoid topRight = null, bottomRight = null;

            var (low, high, is_swapped) = SortSegment(segment);

            var highWasInsertedByNeighbor = is_swapped ? segment.Next.is_inserted : segment.Prev.is_inserted;
            var (_, topLeft) = this.FindOrInsertVertex(high, low, highWasInsertedByNeighbor);
            var lowWasInsertedByNeighbor = is_swapped ? segment.Prev.is_inserted : segment.Next.is_inserted;
            var (bottomLeft, _) = this.FindOrInsertVertex(low, high, lowWasInsertedByNeighbor);

            // Console.WriteLine($"### seg {segment.Id} first: hi:{tfirst.High.X:0.00} {tfirst.High.Y:0.00} lo:{tfirst.Low.X:0.00} {tfirst.Low.Y:0.00} last: hi:{tlast.High.X:0.00} {tlast.High.Y:0.00} lo:{tlast.Low.X:0.00} {tlast.Low.Y:0.00}");
            // this.Tree.DumpTree();

            /* Thread the segment into the query tree creating a new X-node */
            /* First, split all the trapezoids which are intersected by s into two */

            /* traverse top down */
            Trapezoid tnext;
            for (var left = topLeft; left != null; left = tnext)
            {
                if (VertexComparer.Instance.Compare(left.lo, bottomLeft.lo) < 0)
                {
                    break;
                }

                var right = this.CloneTrapezoid(left);
                left.TreeNode.SplitX(right, segment);

                bottomRight = right;
                if (left == topLeft)
                    topRight = right;

                /* 
                handle up links
                */
                if (nodeCount(left.u) == 2)
                {
                    if (left.Third != null)
                    {
                        // Console.WriteLine("upperHandleTriple");
                        upperHandleTriple(left, right);
                    }
                    else
                    {
                        // Console.WriteLine("upperHandleDual");
                        upperHandleDual(left, right);
                    }
                }
                else
                {
                    if (nodeCount(left.u[0].d) == 2)
                    {
                        // Console.WriteLine("upperHandleUpwardCusp");
                        upperHandleUpwardCusp(left, right, low);
                    }
                    else
                    {
                        // Console.WriteLine("upperHandleFreshSegment");
                        upperHandleFreshSegment(left, right);
                    }
                }

                /*
                handle down links
                */
                var bottomIsTriangle = lowWasInsertedByNeighbor && VertexComparer.Instance.Equal(left.lo, bottomLeft.lo);
                int downlinkNodeCount = nodeCount(left.d);

                if (downlinkNodeCount == 0) /* case must not arise */
                {
                    throw new InvalidOperationException("both downlink channels are null");
                }
                else if (bottomIsTriangle)
                {
                    if (downlinkNodeCount == 2)
                    {
                        // Console.WriteLine("lowerHandleBottomTriangletWithDualDownlink");
                        tnext = lowerHandleBottomTriangletWithDualDownlink(left, right);
                    }
                    else
                    {
                        int dx = downlinkNodeCount == 1 ? 0 : 1;
                        // Console.WriteLine("lowerHandleBottomTriangleWithSingleDownlink");
                        tnext = lowerHandleBottomTriangleWithSingleDownlink(dx, is_swapped, left, right, segment, high);
                    }
                }
                else if (downlinkNodeCount == 1 || downlinkNodeCount == -1)
                {
                    /* only one trapezoid below. partition t into two and make the */
                    /* two resulting trapezoids t and tn as the upper neighbours of */
                    /* the sole lower trapezoid */
                    int dx = downlinkNodeCount == 1 ? 0 : 1;
                    // Console.WriteLine("lowerHandleTrapezoidWithSingleDownlink");
                    tnext = lowerHandleTrapezoidWithSingleDownlink(dx, left, right);
                }
                /* two trapezoids below, intersecting the one at d[0]. proceed down that one */
                else if (lowerIntersectsAtIndex0(left, low, high))
                {
                    // Console.WriteLine("lowerHandleDonwlink0Intersect");
                    tnext = lowerHandleDonwlink0Intersect(left, right);
                }
                else
                {
                    // Console.WriteLine("lowerHandleDonwlink1Intersect");
                    tnext = lowerHandleDonwlink1Intersect(left, right);
                }

                left.rseg = right.lseg = segment;
            }

            MergeTrapezoids(segment, topLeft, bottomLeft, true);
            MergeTrapezoids(segment, topRight, bottomRight, false);

            segment.is_inserted = true;
        }

        private static (Vector2, Vector2, bool) SortSegment(Segment segment)
        {
            var swap = VertexComparer.Instance.Compare(segment.End, segment.Start) > 0;
            var high = swap ? segment.End : segment.Start;
            var low = swap ? segment.Start : segment.End;
            return (low, high, swap);
        }

        private (Trapezoid, Trapezoid) FindOrInsertVertex(in Vector2 vertex, in Vector2 other, bool vertexWasInsertedByNeighboringSegment)
        {
            var t = this.Tree.LocateEndpoint(vertex, other);
            if (vertexWasInsertedByNeighboringSegment)
            {
                return (t, t);
            }

            var newLower = this.CloneTrapezoid(t);

            t.d[0] = newLower;
            t.d[1] = null;
            newLower.u[0] = t;
            newLower.u[1] = null;

            if (newLower.d[0]?.u[0] == t)
                newLower.d[0].u[0] = newLower;
            if (newLower.d[0]?.u[1] == t)
                newLower.d[0].u[1] = newLower;
            if (newLower.d[1]?.u[0] == t)
                newLower.d[1].u[0] = newLower;
            if (newLower.d[1]?.u[1] == t)
                newLower.d[1].u[1] = newLower;

            t.lo = newLower.hi = vertex;
            t.TreeNode.SplitY(newLower, vertex);

            return (t, newLower);
        }

        private void MergeTrapezoids(Segment segment, Trapezoid tfirst, Trapezoid tlast, bool leftSide)
        {
            /* Thread in the segment into the existing trapezoidation. The
             * limiting trapezoids are given by tfirst and tlast (which are the
             * trapezoids containing the two endpoints of the segment. 
             * 
             * Merges all possible trapezoids which flank this segment and have 
             * been recently divided because of its insertion
             */
            Trapezoid t, tnext;
            t = tfirst;
            while ((t != null) && VertexComparer.Instance.Compare(t.lo, tlast.lo) >= 0)
            {
                tnext = t.GetDownlinkWithSameSegment(segment, leftSide);

                if ((tnext != null)
                && (t.lseg == tnext.lseg)
                && (t.rseg == tnext.rseg))
                {
                    // merge same neighbors
                    tnext.TreeNode.ReplaceSinkAtParent(t.TreeNode);
                    t.ReplaceDownlink(tnext);
                    t.lo = tnext.lo;

                    // hobo
                    tnext.Invalidate();
                }
                else
                {
                    t = tnext;
                }
            }
        }

        private bool lowerIntersectsAtIndex0(Trapezoid t, Vector2 low, Vector2 high)
        {
            if (VertexComparer.Instance.EqualY(t.lo, low))
            {
                return t.lo.X > low.X;
            }

            var vertex = t.lo;
            var segmentVector = high - low;
            var relation = (vertex.Y - low.Y) / segmentVector.Y;
            var xAtVertex = low.X + relation * segmentVector.X;

            return xAtVertex < vertex.X;
        }

        private Trapezoid lowerHandleDonwlink1Intersect(Trapezoid t, Trapezoid tn)
        {
            t.d[0].u[0] = t;
            t.d[0].u[1] = null;
            t.d[1].u[0] = t;
            t.d[1].u[1] = tn;

            tn.d[0] = t.d[1];
            tn.d[1] = null;

            return t.d[1];
        }

        private Trapezoid lowerHandleDonwlink0Intersect(Trapezoid t, Trapezoid tn)
        {
            t.d[0].u[0] = t;
            t.d[0].u[1] = tn;
            t.d[1].u[0] = tn;
            t.d[1].u[1] = null;

            t.d[1] = null;
            return t.d[0];
        }

        private Trapezoid lowerHandleTrapezoidWithSingleDownlink(int dx, Trapezoid t, Trapezoid tn)
        {
            if (nodeCount(t.d[dx].u) == 2)
            {
                if (t.d[dx].u[0] == t) /* passes thru LHS */
                {
                    t.d[dx].Third = t.d[dx].u[1];
                    t.d[dx].ThirdFromLeft = true;
                }
                else
                {
                    t.d[dx].Third = t.d[dx].u[0];
                    t.d[dx].ThirdFromLeft = false;
                }
            }
            t.d[dx].u[0] = t;
            t.d[dx].u[1] = tn;

            return t.d[dx];
        }

        private Trapezoid lowerHandleBottomTriangleWithSingleDownlink(int dx, bool is_swapped, Trapezoid t, Trapezoid tn, Segment segments, Vector2 vertex)
        {
            var tmptriseg = is_swapped ? segments.Prev : segments.Next;
            if (VertexComparer.Instance.PointIsLeftOfSegment(vertex, tmptriseg))
            {
                /* L-R downward cusp */
                t.d[dx].u[0] = t;
                tn.d[0] = tn.d[1] = null;
            }
            else
            {
                /* R-L downward cusp */
                tn.d[dx].u[1] = tn;
                t.d[0] = t.d[1] = null;
            }

            return t.d[dx];
        }

        private Trapezoid lowerHandleBottomTriangletWithDualDownlink(Trapezoid t, Trapezoid tn)
        {
            /* this case arises only at the lowest trapezoid.. i.e.
            tlast, if the lower endpoint of the segment is
            already inserted in the structure */

            t.d[0].u[0] = t;
            t.d[0].u[1] = null;
            t.d[1].u[0] = tn;
            t.d[1].u[1] = null;

            tn.d[0] = t.d[1];
            t.d[1] = tn.d[1] = null;

            return t.d[1];
        }

        private void upperHandleFreshSegment(Trapezoid t, Trapezoid tn)
        {
            t.u[0].d[0] = t;
            t.u[0].d[1] = tn;
        }

        private void upperHandleUpwardCusp(Trapezoid t, Trapezoid tn, Vector2 vertex)
        {
            var tmp_u = t.u[0];
            var td0 = tmp_u.d[0];
            if ((td0.rseg != null) && !VertexComparer.Instance.PointIsLeftOfSegment(vertex, td0.rseg))
            {
                /* upward cusp */
                t.u[0] = t.u[1] = tn.u[1] = null;
                tn.u[0].d[1] = tn;
            }
            else
            {
                /* cusp going leftwards */
                tn.u[0] = tn.u[1] = t.u[1] = null;
                t.u[0].d[0] = t;
            }
        }

        private void upperHandleDual(Trapezoid t, Trapezoid tn)
        {
            tn.u[0] = t.u[1];
            t.u[1] = tn.u[1] = null;
            tn.u[0].d[0] = tn;
        }

        private void upperHandleTriple(Trapezoid t, Trapezoid tn)
        {
            if (t.ThirdFromLeft)
            {
                tn.u[0] = t.u[1];
                t.u[1] = null;
                tn.u[1] = t.Third;

                t.u[0].d[0] = t;
                tn.u[0].d[0] = tn;
                tn.u[1].d[0] = tn;
            }
            else        /* intersects in the right */
            {
                tn.u[1] = null;
                tn.u[0] = t.u[1];
                t.u[1] = t.u[0];
                t.u[0] = t.Third;

                t.u[0].d[0] = t;
                t.u[1].d[0] = t;
                tn.u[0].d[0] = tn;
            }

            t.Third = tn.Third = null;
        }

        private static int nodeCount(Trapezoid[] data)
        {
            if (data[0] == null)
            {
                return data[1] == null ? 0 : -1;
            }

            return data[1] == null ? 1 : 2;
        }

        private Trapezoid CreateTrapezoid()
        {
            return new Trapezoid();
        }

        private Trapezoid CloneTrapezoid(Trapezoid t)
        {
            var item = this.CreateTrapezoid();

            item.lo = t.lo;
            item.hi = t.hi;
            item.d[0] = t.d[0];
            item.d[1] = t.d[1];
            item.u[0] = t.u[0];
            item.u[1] = t.u[1];
            item.lseg = t.lseg;
            item.rseg = t.rseg;

            return item;
        }
    }
}
