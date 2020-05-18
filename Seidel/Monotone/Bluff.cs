﻿namespace Ruttmann.PolygonTriangulation.Seidel
{
    using System;
    using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Numerics;
	using System.Runtime.CompilerServices;
	using System.Security.Cryptography;
	using System.Text;
    using System.Threading.Tasks;

	public class Bluff
	{
		private ISet<Trapezoid> visitedTrapezoids = new HashSet<Trapezoid>();
		private Dictionary<Segment, VertexChain> vertexChain = new Dictionary<Segment, VertexChain>();
		private IList<MonotoneChain> chainStarts = new List<MonotoneChain>();

		static double get_angle(Vector2 vp0, Vector2 vpnext, Vector2 vp1)
		{
			var v0 = vpnext - vp0;
			var v1 = vp1 - vp0;

			var crossSine = v0.X * v1.Y - v1.X * v0.Y;
			var v0Length = Math.Sqrt(v0.X * v0.X + v0.Y * v0.Y);
			var v1Length = Math.Sqrt(v1.X * v1.X + v1.Y * v1.Y);

			var dot = v0.X * v1.X + v0.Y * v1.Y;

			if (crossSine >= 0)    /* sine is positive */
				return dot / v0Length / v1Length;
			else
				return (-1.0 * dot / v0Length / v1Length - 2);
		}


		/* (v0, v1) is the new diagonal to be added to the polygon. Find which */
		/* chain to use and return the positions of v0 and v1 in p and q */
		static int get_vertex_positions(VertexChain v0, VertexChain v1, ref int ip, ref int iq)
		{
			VertexChain vp0, vp1;
			int i;
			double angle, temp;
			ip = iq = 0;
			vp0 = v0;
			vp1 = v1;

			/* p is identified as follows. Scan from (v0, v1) rightwards till */
			/* you hit the first segment starting from v0. That chain is the */
			/* chain of our interest */
			angle = -4.0;
			for (i = 0; i < 4; i++)
			{
				if (vp0.vnext[i] == null)
					continue;
				if ((temp = get_angle(vp0.pt, vp0.vnext[i].pt, vp1.pt)) > angle)
				{
					angle = temp;
					ip = i;
				}
			}

			/* Do similar actions for q */
			angle = -4.0;
			for (i = 0; i < 4; i++)
			{
				if (vp1.vnext[i] == null)
					continue;
				if ((temp = get_angle(vp1.pt, vp1.vnext[i].pt, vp0.pt)) > angle)
				{
					angle = temp;
					iq = i;
				}
			}

			return 0;
		}

		public int monotonate_trapezoids(Trapezoid tr_start, IEnumerable<Segment> segments)
		{
			var allSegments = segments.ToArray();

			// create one montone entry and one vertex entry for each segment
			foreach (var segment in allSegments)
			{
				var chain = new MonotoneChain();
				var vertex = new VertexChain()
				{
					id = segment.Id,
					pt = segment.v0,
					nextfree = 1,
				};

				chain.vnum = vertex;
				vertex.vpos[0] = chain;
				this.vertexChain.Add(segment, vertex);
			}

			foreach (var segment in allSegments)
			{
				// cyclic chain for the overall monotone chain
				var vertex = this.vertexChain[segment];
				var prevVertex = this.vertexChain[segment.Prev];
				var nextVertex = this.vertexChain[segment.Next];
				vertex.vpos[0].next = nextVertex.vpos[0];
				vertex.vpos[0].prev = prevVertex.vpos[0];
				// single linkes vertex list
				vertex.vnext[0] = nextVertex;
			}

			/* traverse the polygon */
			var firstMonotone = this.segmentToVertex(tr_start.lseg).vpos[0];
			this.chainStarts.Add(firstMonotone);
			if (tr_start.u[0] != null)
				traverse_polygon(tr_start, tr_start.u[0], Traverse.Up);
			else if (tr_start.d[0] != null)
				traverse_polygon(tr_start, tr_start.d[0], Traverse.Down);

			/* return the number of polygons created */
			return 0;
		}

		/* v0 and v1 are specified in anti-clockwise order with respect to
		 * the current monotone polygon mcur. Split the current polygon into
		 * two polygons using the diagonal (v0, v1)
		 */
		private void make_new_monotone_poly(VertexChain v0, VertexChain v1)
		{
			MonotoneChain p, q;
			int ip = 0, iq = 0;
			MonotoneChain i, j;
			int nf0, nf1;
			VertexChain vp0, vp1;

			vp0 = v0;
			vp1 = v1;

			get_vertex_positions(v0, v1, ref ip, ref iq);

			p = vp0.vpos[ip];
			q = vp1.vpos[iq];

			/* At this stage, we have got the positions of v0 and v1 in the */
			/* desired chain. Now modify the linked lists */

			i = new MonotoneChain();    /* for the new list */
			j = new MonotoneChain();

			this.chainStarts.Add(i);
			// this.chainStarts.Add(j);

			i.vnum = v0;
			j.vnum = v1;

			i.next = p.next;
			p.next.prev = i;
			i.prev = j;
			j.next = i;
			j.prev = q.prev;
			q.prev.next = j;

			p.next = q;
			q.prev = p;

			nf0 = vp0.nextfree;
			nf1 = vp1.nextfree;

			vp0.vnext[ip] = v1;

			vp0.vpos[nf0] = i;
			vp0.vnext[nf0] = i.next.vnum;
			vp1.vpos[nf1] = j;
			vp1.vnext[nf1] = v0;

			vp0.nextfree++;
			vp1.nextfree++;
		}

		private VertexChain segmentToVertex(Segment segment)
		{
			return this.vertexChain[segment];
		}

		private enum SplitType
		{
			NoSplit,
			SP_SIMPLE_LRUP,
			SP_2UP_2DN,
			SP_SIMPLE_LRDN,
			SP_2UP_RIGHT,
			SP_2UP_LEFT,
			SP_2DN_LEFT,
			SP_2DN_RIGHT,
		}

		private enum Traverse
		{
			Up,
			Down,
		}

		/* recursively visit all the trapezoids */
		private SplitType traverse_polygon(Trapezoid trapezoid, Trapezoid from, Traverse dir)
		{
			VertexChain v0, v1;
			SplitType retval = SplitType.NoSplit;
			bool do_switch = false;

			if ((trapezoid == null) || !this.visitedTrapezoids.Add(trapezoid))
				return retval;

			/* We have much more information available here. */
			/* rseg: goes upwards   */
			/* lseg: goes downwards */

			/* Initially assume that dir = Traverse.Down (from the left) */
			/* Switch v0 and v1 if necessary afterwards */

			int uplinkCount = (trapezoid.u[0] == null ? 0 : 1) + (trapezoid.u[1] == null ? 0 : 1);
			int downlinkCount = (trapezoid.d[0] == null ? 0 : 1) + (trapezoid.d[1] == null ? 0 : 1);

			/* special cases for triangles with cusps at the opposite ends. */
			/* take care of this first */
			if (uplinkCount == 0 && downlinkCount == 2)
			{
				/* downward opening triangle */
				v0 = segmentToVertex(trapezoid.d[1].lseg);
				v1 = segmentToVertex(trapezoid.lseg);
				if (from == trapezoid.d[1])
				{
					do_switch = true;
					make_new_monotone_poly(v1, v0);
					traverse_polygon(trapezoid.d[1], trapezoid, Traverse.Up);
					traverse_polygon(trapezoid.d[0], trapezoid, Traverse.Up);
				}
				else
				{
					make_new_monotone_poly(v0, v1);
					traverse_polygon(trapezoid.d[0], trapezoid, Traverse.Up);
					traverse_polygon(trapezoid.d[1], trapezoid, Traverse.Up);
				}
			}
			else if (uplinkCount == 0 && downlinkCount != 2)
			{
				retval = SplitType.NoSplit; /* Just traverse all neighbours */
				traverse_polygon(trapezoid.d[0], trapezoid, Traverse.Up);
				traverse_polygon(trapezoid.d[1], trapezoid, Traverse.Up);
			}
			else if (downlinkCount == 0 && uplinkCount == 2)
			{
				/* upward opening triangle */
				v0 = segmentToVertex(trapezoid.rseg);
				v1 = segmentToVertex(trapezoid.u[0].rseg);
				if (from == trapezoid.u[1])
				{
					do_switch = true;
					make_new_monotone_poly(v1, v0);
					traverse_polygon(trapezoid.u[1], trapezoid, Traverse.Down);
					traverse_polygon(trapezoid.u[0], trapezoid, Traverse.Down);
				}
				else
				{
					make_new_monotone_poly(v0, v1);
					traverse_polygon(trapezoid.u[0], trapezoid, Traverse.Down);
					traverse_polygon(trapezoid.u[1], trapezoid, Traverse.Down);
				}
			}
			else if (downlinkCount == 0 && uplinkCount != 2)
			{
				retval = SplitType.NoSplit;    /* Just traverse all neighbours */
				traverse_polygon(trapezoid.u[0], trapezoid, Traverse.Down);
				traverse_polygon(trapezoid.u[1], trapezoid, Traverse.Down);
			}
			else if (uplinkCount == 2 && downlinkCount == 2)
			{
				/* downward + upward cusps */
				v0 = segmentToVertex(trapezoid.d[1].lseg);
				v1 = segmentToVertex(trapezoid.u[0].rseg);
				retval = SplitType.SP_2UP_2DN;
				if (((dir == Traverse.Down) && (trapezoid.d[1] == from)) ||
					((dir == Traverse.Up) && (trapezoid.u[1] == from)))
				{
					do_switch = true;
					make_new_monotone_poly(v1, v0);
					traverse_polygon(trapezoid.u[1], trapezoid, Traverse.Down);
					traverse_polygon(trapezoid.d[1], trapezoid, Traverse.Up);
					traverse_polygon(trapezoid.u[0], trapezoid, Traverse.Down);
					traverse_polygon(trapezoid.d[0], trapezoid, Traverse.Up);
				}
				else
				{
					make_new_monotone_poly(v0, v1);
					traverse_polygon(trapezoid.u[0], trapezoid, Traverse.Down);
					traverse_polygon(trapezoid.d[0], trapezoid, Traverse.Up);
					traverse_polygon(trapezoid.u[1], trapezoid, Traverse.Down);
					traverse_polygon(trapezoid.d[1], trapezoid, Traverse.Up);
				}
			}
			else if (uplinkCount == 2 && downlinkCount != 2)
			{
				/* only downward cusp */
				if (VertexComparer.Instance.Equal(trapezoid.lo, trapezoid.lseg.v1))
				{
					v0 = segmentToVertex(trapezoid.u[0].rseg);
					v1 = segmentToVertex(trapezoid.lseg.Next);

					retval = SplitType.SP_2UP_LEFT;
					if ((dir == Traverse.Up) && (trapezoid.u[0] == from))
					{
						do_switch = true;
						make_new_monotone_poly(v1, v0);
						traverse_polygon(trapezoid.u[0], trapezoid, Traverse.Down);
						traverse_polygon(trapezoid.d[0], trapezoid, Traverse.Up);
						traverse_polygon(trapezoid.u[1], trapezoid, Traverse.Down);
						traverse_polygon(trapezoid.d[1], trapezoid, Traverse.Up);
					}
					else
					{
						make_new_monotone_poly(v0, v1);
						traverse_polygon(trapezoid.u[1], trapezoid, Traverse.Down);
						traverse_polygon(trapezoid.d[0], trapezoid, Traverse.Up);
						traverse_polygon(trapezoid.d[1], trapezoid, Traverse.Up);
						traverse_polygon(trapezoid.u[0], trapezoid, Traverse.Down);
					}
				}
				else
				{
					v0 = segmentToVertex(trapezoid.rseg);
					v1 = segmentToVertex(trapezoid.u[0].rseg);
					retval = SplitType.SP_2UP_RIGHT;
					if ((dir == Traverse.Up) && (trapezoid.u[1] == from))
					{
						do_switch = true;
						make_new_monotone_poly(v1, v0);
						traverse_polygon(trapezoid.u[1], trapezoid, Traverse.Down);
						traverse_polygon(trapezoid.d[1], trapezoid, Traverse.Up);
						traverse_polygon(trapezoid.d[0], trapezoid, Traverse.Up);
						traverse_polygon(trapezoid.u[0], trapezoid, Traverse.Down);
					}
					else
					{
						make_new_monotone_poly(v0, v1);
						traverse_polygon(trapezoid.u[0], trapezoid, Traverse.Down);
						traverse_polygon(trapezoid.d[0], trapezoid, Traverse.Up);
						traverse_polygon(trapezoid.d[1], trapezoid, Traverse.Up);
						traverse_polygon(trapezoid.u[1], trapezoid, Traverse.Down);
					}
				}
			}
			else if (uplinkCount == 1 && downlinkCount == 2) /* no downward cusp */
			{
				/* only upward cusp */
				if (VertexComparer.Instance.Equal(trapezoid.hi, trapezoid.lseg.v0))
				{
					v0 = segmentToVertex(trapezoid.d[1].lseg);
					v1 = segmentToVertex(trapezoid.lseg);
					retval = SplitType.SP_2DN_LEFT;
					if (!((dir == Traverse.Down) && (trapezoid.d[0] == from)))
					{
						do_switch = true;
						make_new_monotone_poly(v1, v0);
						traverse_polygon(trapezoid.u[1], trapezoid, Traverse.Down);
						traverse_polygon(trapezoid.d[1], trapezoid, Traverse.Up);
						traverse_polygon(trapezoid.u[0], trapezoid, Traverse.Down);
						traverse_polygon(trapezoid.d[0], trapezoid, Traverse.Up);
					}
					else
					{
						make_new_monotone_poly(v0, v1);
						traverse_polygon(trapezoid.d[0], trapezoid, Traverse.Up);
						traverse_polygon(trapezoid.u[0], trapezoid, Traverse.Down);
						traverse_polygon(trapezoid.u[1], trapezoid, Traverse.Down);
						traverse_polygon(trapezoid.d[1], trapezoid, Traverse.Up);
					}
				}
				else
				{
					v0 = segmentToVertex(trapezoid.d[1].lseg);
					v1 = segmentToVertex(trapezoid.rseg.Next);

					retval = SplitType.SP_2DN_RIGHT;
					if ((dir == Traverse.Down) && (trapezoid.d[1] == from))
					{
						do_switch = true;
						make_new_monotone_poly(v1, v0);
						traverse_polygon(trapezoid.d[1], trapezoid, Traverse.Up);
						traverse_polygon(trapezoid.u[1], trapezoid, Traverse.Down);
						traverse_polygon(trapezoid.u[0], trapezoid, Traverse.Down);
						traverse_polygon(trapezoid.d[0], trapezoid, Traverse.Up);
					}
					else
					{
						make_new_monotone_poly(v0, v1);
						traverse_polygon(trapezoid.u[0], trapezoid, Traverse.Down);
						traverse_polygon(trapezoid.d[0], trapezoid, Traverse.Up);
						traverse_polygon(trapezoid.u[1], trapezoid, Traverse.Down);
						traverse_polygon(trapezoid.d[1], trapezoid, Traverse.Up);
					}
				}
			}
			else if (uplinkCount == 1 && downlinkCount != 2)
			{
				/* no cusp */
				if (VertexComparer.Instance.Equal(trapezoid.hi, trapezoid.lseg.v0) &&
					VertexComparer.Instance.Equal(trapezoid.lo, trapezoid.rseg.v0))
				{
					v0 = segmentToVertex(trapezoid.rseg);
					v1 = segmentToVertex(trapezoid.lseg);
					retval = SplitType.SP_SIMPLE_LRDN;
					if (dir == Traverse.Up)
					{
						do_switch = true;
						make_new_monotone_poly(v1, v0);
						traverse_polygon(trapezoid.u[0], trapezoid, Traverse.Down);
						traverse_polygon(trapezoid.u[1], trapezoid, Traverse.Down);
						traverse_polygon(trapezoid.d[1], trapezoid, Traverse.Up);
						traverse_polygon(trapezoid.d[0], trapezoid, Traverse.Up);
					}
					else
					{
						make_new_monotone_poly(v0, v1);
						traverse_polygon(trapezoid.d[1], trapezoid, Traverse.Up);
						traverse_polygon(trapezoid.d[0], trapezoid, Traverse.Up);
						traverse_polygon(trapezoid.u[0], trapezoid, Traverse.Down);
						traverse_polygon(trapezoid.u[1], trapezoid, Traverse.Down);
					}
				}
				else if (VertexComparer.Instance.Equal(trapezoid.hi, trapezoid.rseg.v1) &&
					VertexComparer.Instance.Equal(trapezoid.lo, trapezoid.lseg.v1))
				{
					v0 = segmentToVertex(trapezoid.rseg.Next);
					v1 = segmentToVertex(trapezoid.lseg.Next);

					retval = SplitType.SP_SIMPLE_LRUP;
					if (dir == Traverse.Up)
					{
						do_switch = true;
						make_new_monotone_poly(v1, v0);
						traverse_polygon(trapezoid.u[0], trapezoid, Traverse.Down);
						traverse_polygon(trapezoid.u[1], trapezoid, Traverse.Down);
						traverse_polygon(trapezoid.d[1], trapezoid, Traverse.Up);
						traverse_polygon(trapezoid.d[0], trapezoid, Traverse.Up);
					}
					else
					{
						make_new_monotone_poly(v0, v1);
						traverse_polygon(trapezoid.d[1], trapezoid, Traverse.Up);
						traverse_polygon(trapezoid.d[0], trapezoid, Traverse.Up);
						traverse_polygon(trapezoid.u[0], trapezoid, Traverse.Down);
						traverse_polygon(trapezoid.u[1], trapezoid, Traverse.Down);
					}
				}
				else            /* no split possible */
				{
					retval = SplitType.NoSplit;
					traverse_polygon(trapezoid.u[0], trapezoid, Traverse.Down);
					traverse_polygon(trapezoid.d[0], trapezoid, Traverse.Up);
					traverse_polygon(trapezoid.u[1], trapezoid, Traverse.Down);
					traverse_polygon(trapezoid.d[1], trapezoid, Traverse.Up);
				}
			}

			return retval;
		}

		public int[] MonotonateAll()
		{
			var result = new List<int>();
			foreach (var chain in this.chainStarts)
			{
				Console.Write($"Polygon {chain.vnum.id} ");
				for (var x = chain.next; x != chain; x = x.next)
				{
					Console.Write($"{x.vnum.id} ");
				}

				Console.WriteLine();
			}

			foreach (var chain in this.chainStarts)
			{
				this.triangulate_monotone_polygone(0, chain, result);
			}

			return result.ToArray();
		}


		/* For each monotone polygon, find the ymax and ymin (to determine the */
		/* two y-monotone chains) and pass on this monotone polygon for greedy */
		/* triangulation. */
		/* Take care not to triangulate duplicate monotone polygons */
		public void triangulate_monotone_polygone(int nvert, MonotoneChain monotoneChain, IList<int> result)
		{
			Vector2 ymax, ymin;
			MonotoneChain p;
			MonotoneChain posmax;
			MonotoneChain posmin;
			VertexChain vfirst, v;
			int vcount;

			MonotoneChain activeMon = monotoneChain;
			vcount = 1;

			vfirst = activeMon.vnum;
			ymax = ymin = vfirst.pt;
			posmax = posmin = activeMon;
			posmin.marked = true;
			p = posmin.next;
			while ((v = p.vnum) != vfirst)
			{
				if (p.marked)
				{
					return;
				}
				else
				{
					p.marked = true;
				}

				if (VertexComparer.Instance.Compare(v.pt, ymax) > 0)
				{
					ymax = v.pt;
					posmax = p;
				}
				if (VertexComparer.Instance.Compare(v.pt, ymin) < 0)
				{
					ymin = v.pt;
					posmin = p;
				}
				p = p.next;
				vcount++;
			}

			/* already a triangle */
			if (vcount == 3)
			{
				result.Add(p.vnum.id);
				result.Add(p.next.vnum.id);
				result.Add(p.prev.vnum.id);
			}
			else
			{
				v = posmax.next.vnum;
				if (VertexComparer.Instance.Equal(v.pt, ymin))
				{
					/* LHS is a single line */
					triangulate_single_polygon(nvert, posmax, TriangulationSide.TRI_LHS, result);
				}
				else
				{
					triangulate_single_polygon(nvert, posmax, TriangulationSide.TRI_RHS, result);
				}
			}
		}

		enum TriangulationSide
		{
			TRI_RHS,
			TRI_LHS
		}

		/* A greedy corner-cutting algorithm to triangulate a y-monotone
		 * polygon in O(n) time.
		 * Joseph O-Rourke, Computational Geometry in C.
		 */
		int triangulate_single_polygon(int nvert, MonotoneChain posmax, TriangulationSide side, IList<int> result)
		{
			VertexChain v;
			VertexChain[] rc = new VertexChain[200];
			int ri = 0; /* reflex chain */
			MonotoneChain tmp, vpos;
			VertexChain endv;

			if (side == TriangulationSide.TRI_RHS)        /* RHS segment is a single segment */
			{
				rc[0] = posmax.vnum;
				tmp = posmax.next;
				rc[1] = tmp.vnum;
				ri = 1;

				vpos = tmp.next;
				v = vpos.vnum;

				endv = posmax.prev.vnum;
				if (endv == null)
				{
					throw new InvalidOperationException("HOBO: chain is not cyclic");
					// endv = &vertexChain[nvert];
				}
			}
			else                /* LHS is a single segment */
			{
				tmp = posmax.next;
				rc[0] = tmp.vnum;
				tmp = tmp.next;
				rc[1] = tmp.vnum;
				ri = 1;

				vpos = tmp.next;
				v = vpos.vnum;

				endv = posmax.vnum;
			}

			while ((v != endv) || (ri > 1))
			{
				if (ri > 0)     /* reflex chain is non-empty */
				{
					var v0 = v.pt;
					var v1 = rc[ri - 1].pt;
					var v2 = rc[ri].pt;
					var cross = (v1.X - v0.X) * (v2.Y - v0.Y) - ((v1.Y - v0.Y) * (v2.X - v0.X));

					if (cross > 0)
					{           /* convex corner: cut if off */
						result.Add(rc[ri - 1].id);
						result.Add(rc[ri].id);
						result.Add(v.id);
						ri--;
					}
					else        /* non-convex */
					{       /* add v to the chain */
						ri++;
						rc[ri] = v;
						vpos = vpos.next;
						v = vpos.vnum;
					}
				}
				else            /* reflex-chain empty: add v to the */
				{           /* reflex chain and advance it  */
					rc[++ri] = v;
					vpos = vpos.next;
					v = vpos.vnum;
				}
			} /* end-while */

			/* reached the bottom vertex. Add in the triangle formed */
			result.Add(rc[ri - 1].id);
			result.Add(rc[ri].id);
			result.Add(v.id);
			ri--;

			return 0;
		}
    }
}
