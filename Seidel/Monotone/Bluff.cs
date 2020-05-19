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
		private List<Tuple<Segment, Segment>> segmentSplits = new List<Tuple<Segment, Segment>>();
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
		static int GetVertexPosition(VertexChain vertexChain, Vector2 neighborVertex)
		{
			double angle, temp;
			var bestIndex = 0;

			/* p is identified as follows. Scan from (v0, v1) rightwards till */
			/* you hit the first segment starting from v0. That chain is the */
			/* chain of our interest */
			angle = -4.0;
			for (var i = 0; i < 4; i++)
			{
				if (vertexChain.vnext[i] == null)
				{
					break;
				}

				if ((temp = get_angle(vertexChain.pt, vertexChain.vnext[i].pt, neighborVertex)) > angle)
				{
					angle = temp;
					bestIndex = i;
				}
			}

			return bestIndex;
		}

		public int monotonate_trapezoids(Trapezoid tr_start, IEnumerable<Segment> segments)
		{
			if (tr_start.u[0] != null)
				traverse_polygon(tr_start, tr_start.u[0], Traverse.Up);
			else if (tr_start.d[0] != null)
				traverse_polygon(tr_start, tr_start.d[0], Traverse.Down);

			this.SplitPolygon(tr_start, segments);

			/* return the number of polygons created */
			return 0;
		}

		private void SplitPolygon(Trapezoid tr_start, IEnumerable<Segment> segments)
		{
			var allSegments = segments.ToArray();
			var vertexChain = new Dictionary<Segment, VertexChain>();

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
				vertexChain.Add(segment, vertex);
			}

			foreach (var segment in allSegments)
			{
				// cyclic chain for the overall monotone chain
				var vertex = vertexChain[segment];
				var prevVertex = vertexChain[segment.Prev];
				var nextVertex = vertexChain[segment.Next];
				vertex.vpos[0].next = nextVertex.vpos[0];
				vertex.vpos[0].prev = prevVertex.vpos[0];
				// single linkes vertex list
				vertex.vnext[0] = nextVertex;
			}

			/* traverse the polygon */
			var firstMonotone = vertexChain[tr_start.lseg].vpos[0];
			this.chainStarts.Add(firstMonotone);

			foreach (var split in this.segmentSplits)
			{
				var v0 = vertexChain[split.Item1];
				var v1 = vertexChain[split.Item2];
				this.SplitAtVertex2(v0, v1);
			}
		}

		/* v0 and v1 are specified in anti-clockwise order with respect to
		 * the current monotone polygon mcur. Split the current polygon into
		 * two polygons using the diagonal (v0, v1)
		 */
		private void SplitAtVertex2(VertexChain v0, VertexChain v1)
		{
			MonotoneChain chain0, chain1;
			int i0 = 0, i1 = 0;
			MonotoneChain i, j;
			int nf0, nf1;
			VertexChain vp0, vp1;

			vp0 = v0;
			vp1 = v1;

			i0 = GetVertexPosition(v0, v1.pt);
			i1 = GetVertexPosition(v1, v0.pt);

			chain0 = vp0.vpos[i0];
			chain1 = vp1.vpos[i1];

			/* At this stage, we have got the positions of v0 and v1 in the */
			/* desired chain. Now modify the linked lists */

			i = new MonotoneChain();    /* for the new list */
			j = new MonotoneChain();

			this.chainStarts.Add(i);
			// this.chainStarts.Add(j);

			i.vnum = v0;
			j.vnum = v1;

			i.next = chain0.next;
			chain0.next.prev = i;
			i.prev = j;
			j.next = i;
			j.prev = chain1.prev;
			chain1.prev.next = j;

			chain0.next = chain1;
			chain1.prev = chain0;

			nf0 = vp0.nextfree;
			nf1 = vp1.nextfree;

			vp0.vnext[i0] = v1;

			vp0.vpos[nf0] = i;
			vp0.vnext[nf0] = i.next.vnum;
			vp1.vpos[nf1] = j;
			vp1.vnext[nf1] = v0;

			vp0.nextfree++;
			vp1.nextfree++;
		}

		private enum Traverse
		{
			Up,
			Down,
		}

		/* recursively visit all the trapezoids */
		private void traverse_polygon(Trapezoid trapezoid, Trapezoid from, Traverse dir)
		{
			Segment s0 = null, s1 = null;

			if (trapezoid == null)
			{
				return;
			}

			/* We have much more information available here. */
			/* rseg: goes upwards   */
			/* lseg: goes downwards */

			/* Initially assume that dir = Traverse.Down (from the left) */
			/* Switch v0 and v1 if necessary afterwards */

			int uplinkCount = (trapezoid.u[0] == null ? 0 : 1) + (trapezoid.u[1] == null ? 0 : 1);
			int downlinkCount = (trapezoid.d[0] == null ? 0 : 1) + (trapezoid.d[1] == null ? 0 : 1);

			bool invertSplitDirection = false;
			if (!this.visitedTrapezoids.Add(trapezoid))
			{
				return;
			}

			switch(uplinkCount * 4 + downlinkCount)
			{
				// downward opening triangle
				case 0 * 4 + 2:
					s0 = trapezoid.d[1].lseg;
					s1 = trapezoid.lseg;
					invertSplitDirection = from == trapezoid.d[1];
					break;

				// upward opening triangle
				case 2 * 4 + 0:
					s0 = trapezoid.rseg;
					s1 = trapezoid.u[0].rseg;
					invertSplitDirection = from == trapezoid.u[1];
					break;

				// downward + upward cusps
				case 2 * 4 + 2:
					s0 = trapezoid.d[1].lseg;
					s1 = trapezoid.u[0].rseg;
					if (((dir == Traverse.Down) && (trapezoid.d[1] == from)) ||
						((dir == Traverse.Up) && (trapezoid.u[1] == from)))
					{
						invertSplitDirection = true;
					}
					break;

				// only downward cusp
				case 2 * 4 + 1:
					if (VertexComparer.Instance.Equal(trapezoid.lo, trapezoid.lseg.v1))
					{
						s0 = trapezoid.u[0].rseg;
						s1 = trapezoid.lseg.Next;
						invertSplitDirection = (dir == Traverse.Up) && (trapezoid.u[0] == from);
					}
					else
					{
						s0 = trapezoid.rseg;
						s1 = trapezoid.u[0].rseg;
						invertSplitDirection = (dir == Traverse.Up) && (trapezoid.u[1] == from);
					}
					break;

				// only upward cusp
				case 1 * 4 + 2:
					if (VertexComparer.Instance.Equal(trapezoid.hi, trapezoid.lseg.v0))
					{
						s0 = trapezoid.d[1].lseg;
						s1 = trapezoid.lseg;
						invertSplitDirection = !((dir == Traverse.Down) && (trapezoid.d[0] == from));
					}
					else
					{
						s0 = trapezoid.d[1].lseg;
						s1 = trapezoid.rseg.Next;
						invertSplitDirection = (dir == Traverse.Down) && (trapezoid.d[1] == from);
					}
					break;

				// no cusp
				case 1 * 4 + 1:
					if (VertexComparer.Instance.Equal(trapezoid.hi, trapezoid.lseg.v0) &&
						VertexComparer.Instance.Equal(trapezoid.lo, trapezoid.rseg.v0))
					{
						s0 = trapezoid.rseg;
						s1 = trapezoid.lseg;
						invertSplitDirection = dir == Traverse.Up;
					}
					else if (VertexComparer.Instance.Equal(trapezoid.hi, trapezoid.rseg.v1) &&
						VertexComparer.Instance.Equal(trapezoid.lo, trapezoid.lseg.v1))
					{
						s0 = trapezoid.rseg.Next;
						s1 = trapezoid.lseg.Next;
						invertSplitDirection = dir == Traverse.Up;
					}
					else
					{
						// no split possible
					}
					break;

				case 1 * 4 + 0:
					break;

				case 0 * 4 + 1:
					break;

				default:
					throw new InvalidOperationException("Bad UL/DL count combination");
			}

			if (s0 != null && s1 != null)
			{
				if (invertSplitDirection)
				{
					this.segmentSplits.Add(Tuple.Create(s1, s0));
				}
				else
				{
					this.segmentSplits.Add(Tuple.Create(s0, s1));
				}
			}

			traverse_polygon(trapezoid.d[1], trapezoid, Traverse.Up);
			traverse_polygon(trapezoid.d[0], trapezoid, Traverse.Up);
			traverse_polygon(trapezoid.u[1], trapezoid, Traverse.Down);
			traverse_polygon(trapezoid.u[0], trapezoid, Traverse.Down);
		}

		public int[] MonotonateAll()
		{
			var result = new List<int>();
#if false
			foreach (var chain in this.chainStarts)
			{
				Console.Write($"Polygon {chain.vnum.id} ");
				for (var x = chain.next; x != chain; x = x.next)
				{
					Console.Write($"{x.vnum.id} ");
				}

				Console.WriteLine();
			}
#endif

			foreach (var chain in this.chainStarts)
			{
				this.TriangulateMonotonePolygon(chain, result);
			}

			return result.ToArray();
		}


		/* For each monotone polygon, find the ymax and ymin (to determine the */
		/* two y-monotone chains) and pass on this monotone polygon for greedy */
		/* triangulation. */
		/* Take care not to triangulate duplicate monotone polygons */
		public void TriangulateMonotonePolygon(MonotoneChain chainStart, IList<int> result)
		{
			Vector2 ymax, ymin;
			MonotoneChain posmax;
			
			var firstVertex = chainStart.vnum;
			ymax = ymin = firstVertex.pt;
			posmax = chainStart;
			var chain = chainStart.next;
			var vcount = 1;

			VertexChain vertex;
			while ((vertex = chain.vnum) != firstVertex)
			{
				if (VertexComparer.Instance.Compare(vertex.pt, ymax) > 0)
				{
					ymax = vertex.pt;
					posmax = chain;
				}

				if (VertexComparer.Instance.Compare(vertex.pt, ymin) < 0)
				{
					ymin = vertex.pt;
				}

				chain = chain.next;
				vcount++;
			}

			if (vcount == 3)
			{
				/* already a triangle */
				result.Add(chain.vnum.id);
				result.Add(chain.next.vnum.id);
				result.Add(chain.prev.vnum.id);
			}
			else
			{
				vertex = posmax.next.vnum;
				if (VertexComparer.Instance.Equal(vertex.pt, ymin))
				{
					/* LHS is a single line */
					TriangulateSinglePolygon(posmax, TriangulationSide.TRI_LHS, result);
				}
				else
				{
					TriangulateSinglePolygon(posmax, TriangulationSide.TRI_RHS, result);
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
		private static int TriangulateSinglePolygon(MonotoneChain posmax, TriangulationSide side, IList<int> result)
		{
			var vertexStack = new Stack<VertexChain>();
			VertexChain vertex;
			MonotoneChain chain;

			/* RHS segment is a single segment, start there */
			if (side == TriangulationSide.TRI_RHS)
			{
				chain = posmax;
			}
			else
			{
				/* LHS is a single segment and it's next in the chain */
				chain = posmax.next;
			}

			var endVertex = chain.prev.vnum;
			vertexStack.Push(chain.vnum);

			chain = chain.next;
			vertexStack.Push(chain.vnum);

			chain = chain.next;
			vertex = chain.vnum;

			while ((vertex != endVertex) || vertexStack.Count > 2)
			{
				// start after 2 points are on the stack
				if (vertexStack.Count > 1)
				{
					var lastOnStack = vertexStack.Pop();
					var v0 = vertex.pt;
					var v1 = lastOnStack.pt;
					var v2 = vertexStack.Peek().pt;
					var cross = (v2.X - v0.X) * (v1.Y - v0.Y) - ((v2.Y - v0.Y) * (v1.X - v0.X));

					// convex corner: cut if off
					if (cross > 0)
					{
						result.Add(vertexStack.Peek().id);
						result.Add(lastOnStack.id);
						result.Add(vertex.id);
					}
					else
					{
						// concave: push the last and the current and advance
						vertexStack.Push(lastOnStack);
						vertexStack.Push(vertex);
						chain = chain.next;
						vertex = chain.vnum;
					}
				}
				else
				{
					vertexStack.Push(vertex);
					chain = chain.next;
					vertex = chain.vnum;
				}
			}

			var secondLast = vertexStack.Pop();
			result.Add(vertexStack.Pop().id);
			result.Add(secondLast.id);
			result.Add(vertex.id);

			return 0;
		}
    }
}
