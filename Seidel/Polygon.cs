namespace Ruttmann.PolygonTriangulation.Seidel
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Numerics;

	[DebuggerDisplay("{Debug}")]
	public class Polygon
	{
		private Vector2[] vertices;
		private int[] nextChain;
		private readonly int start;

		public Polygon(int start, IEnumerable<Vector2> vertices, IEnumerable<int> nextChain)
		{
			this.start = start;
			this.vertices = vertices as Vector2[] ?? vertices.ToArray();
			this.nextChain = (int[])(nextChain as int[])?.Clone() ?? nextChain.ToArray();
		}

		public bool Locked { get; set; }

		public bool IsTriangle => this.nextChain[this.nextChain[this.nextChain[this.start]]] == this.start;

		public int NextOf(int vertexId) => this.nextChain[vertexId];

		public String Debug => String.Join(" ", this.Indices);

		public IEnumerable<int> Indices => new NextChainEnumerator(this.start, this.nextChain);

		public IReadOnlyList<Vector2> Vertices => Array.AsReadOnly(this.vertices);

		/// <summary>
		/// Legacy: converts a segment chain to a polygon
		/// </summary>
		/// <param name="segments">the segments</param>
		/// <returns>a polygon</returns>
		public static Polygon FromSegments(IEnumerable<Segment> segments)
		{
			var segmentsArray = segments.ToArray();
			var vertexList = new List<Vector2>();
			var indexes = new List<int>();

			foreach (var segment in segmentsArray)
			{
				if (segment.Id >= vertexList.Count)
				{
					var itemsToAdd = segment.Id - vertexList.Count + 1;
					vertexList.AddRange(Enumerable.Repeat(default(Vector2), itemsToAdd));
					indexes.AddRange(Enumerable.Repeat(default(int), itemsToAdd));
				}

				vertexList[segment.Id] = segment.Start;
				indexes[segment.Id] = segment.Next.Id;
			}

			return new Polygon(segmentsArray.First().Id, vertexList, indexes);
		}

		/// <summary>
		/// Legacy: converts a monotone chain to a polygon
		/// </summary>
		/// <param name="monotone">the start of the monotone chain</param>
		/// <returns>a polygon</returns>
		public static Polygon FromMonotone(MonotoneChain monotone)
		{
			var vertexList = new List<Vector2>();
			var indexes = new List<int>();
			var first = true;

			for (var i = monotone; first || i != monotone; i = i.Next)
			{
				var vertexChain = i.Vnum;
				if (vertexChain.id >= vertexList.Count)
				{
					var itemsToAdd = vertexChain.id - vertexList.Count + 1;
					vertexList.AddRange(Enumerable.Repeat(default(Vector2), itemsToAdd));
					indexes.AddRange(Enumerable.Repeat(default(int), itemsToAdd));
				}

				vertexList[vertexChain.id] = vertexChain.pt;
				indexes[vertexChain.id] = i.Next.Vnum.id;
				first = false;
			}

			return new Polygon(monotone.Vnum.id, vertexList, indexes);
		}

		public static (Polygon[], Polygon[]) Split(Polygon polygon, IEnumerable<Tuple<int, int>> splits)
		{
			var sortedSplits = splits
				.Select(x => x.Item1 < x.Item2 ? x : Tuple.Create(x.Item2, x.Item1))
				.OrderBy(x => x.Item1)
				.ThenBy(x => x.Item2);

			return SplitSorted(polygon, sortedSplits);
		}

		public static (Polygon[], Polygon[]) SplitSorted(Polygon polygon, IEnumerable<Tuple<int, int>> splits)
		{
			var queue = new DumbPriorityQueue<Polygon>();

			var result = new List<Polygon>();
			queue.Add(0, polygon);

			var triangles = new List<Polygon>();

			var cacheKey = 0;
			polygon = null;
			var lastSplitFrom = -1;
			foreach (var split in splits)
			{
				var from = split.Item1;
				var to = split.Item2;

				var searchNext = false;
				if (polygon == null)
				{
					searchNext = true;
					lastSplitFrom = from;
				}
				else if (lastSplitFrom != from)
				{
					// Skip enqueue and dequeue.
					if (polygon.IsTriangle)
					{
						searchNext = true;
						triangles.Add(polygon);
					}
					else if (queue.Count > 0)
					{
						searchNext = true;
						queue.Add(polygon.NextOf(lastSplitFrom), polygon);
					}

					lastSplitFrom = from;
				}

				if (searchNext)
				{
					while (((polygon = queue.Next()) != null) && !(polygon.IsInside(from, out cacheKey) && polygon.IsStillInside(to, ref cacheKey)))
					{
						polygon.Locked = true;
						result.Add(polygon);
					}
				}

				// everything with the same split start would be inside of the same polygon, as the splits are sorted by their endpoints
				// HOBO: check if that holds true if vertexes are in random order - i fear not....
				var polygonWithLaterStart = polygon.SplitAtIndex(from, to);
				if (polygonWithLaterStart.IsTriangle)
				{
					triangles.Add(polygonWithLaterStart);
				}
				else
				{
					queue.Add(polygonWithLaterStart.NextOf(from), polygonWithLaterStart);
				}
			}

			while (queue.Count > 0)
			{
				result.Add(queue.Next());
			}

			result.Add(polygon);
			return (triangles.ToArray(), result.ToArray());
		}

		/// <summary>
		/// check whether the vertex id is inside the polygon
		/// </summary>
		/// <param name="vertexId">the id of the vertex</param>
		/// <param name="cacheId">the cache id to use with <see cref="IsStillInside"/></param>
		/// <returns>true if the vertex belongs to the polygon</returns>
		public bool IsInside(int vertexId, out int cacheId)
		{
			cacheId = this.start;
			return IsStillInside(vertexId, ref cacheId);
		}

		/// <summary>
		/// check whether the vertex id is inside the polygon
		/// </summary>
		/// <param name="vertexId">the id of the vertex</param>
		/// <param name="cacheId">the cache id to continue the search behind the last search</param>
		/// <returns>true if the vertex belongs to the polygon</returns>
		public bool IsStillInside(int vertexId, ref int cacheId)
		{
			return this.nextChain[vertexId] != -1;
		}

		/// <summary>
		/// Skip the entries from]..[to in this chained list and create a copy that contains [from..to] and starts with from
		/// </summary>
		/// <param name="from">start index of the split (part of both results)</param>
		/// <param name="to">end index of the split (part of both results)</param>
		/// <returns>the polygon with the larger start index</returns>
		public Polygon SplitAtIndex(int from, int to)
		{
			if (this.Locked)
			{
				throw new InvalidOperationException("Can't do it");
			}

			var polygon = new Polygon(from, this.vertices, this.nextChain);

			int nextId;
			for (var id = this.nextChain[from]; id != to; id = nextId)
			{
				nextId = this.nextChain[id];
				this.nextChain[id] = -1;
			}

			for (var id = polygon.nextChain[to]; id != from; id = nextId)
			{
				nextId = polygon.nextChain[id];
				polygon.nextChain[id] = -1;
			}

			this.nextChain[from] = to;
			polygon.nextChain[to] = from;

			return polygon;
		}

		/// <summary>
		/// Gets an enumerator that starts at start and loops the whole polygon once.
		/// </summary>
		/// <param name="start">The first vertex id</param>
		/// <returns>An Enumerable/Enumerator</returns>
		public IEnumerable<int> IndicesStartingAt(int start)
		{
			return new NextChainEnumerator(start, this.nextChain);
		}

		/// <summary>
		/// Internal enumerator
		/// </summary>
		private class NextChainEnumerator : IEnumerable<int>, IEnumerator<int>
		{
			private readonly int start;
			private readonly int[] nextChain;
			private bool reset;

			public NextChainEnumerator(int start, int[] nextChain)
			{
				this.start = start;
				this.Current = start;
				this.nextChain = nextChain;
				this.reset = true;
			}

			public int Current { get; private set; }

			object IEnumerator.Current => this.Current;

			public void Dispose()
			{
			}

			public IEnumerator<int> GetEnumerator()
			{
				return this;
			}

			public bool MoveNext()
			{
				if (this.reset)
				{
					this.reset = false;
					this.Current = this.start;
				}
				else
				{
					this.Current = this.nextChain[this.Current];
					if (this.Current == this.start)
					{
						return false;
					}
				}

				return true;
			}

			public void Reset()
			{
				this.reset = true;
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return this.GetEnumerator();
			}
		}
	}
}
