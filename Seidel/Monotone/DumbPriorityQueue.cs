namespace Ruttmann.PolygonTriangulation.Seidel
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A very crude priority queue with Add() and Next()
    /// </summary>
    public class DumbPriorityQueue<TItem>
    {
        private List<Tuple<int, TItem>> data;

        public DumbPriorityQueue()
        {
            this.data = new List<Tuple<int, TItem>>();
        }

        public void Add(int key, TItem item)
        {
            this.data.Add(Tuple.Create(key, item));
        }

        public int Count => this.data.Count;

        public TItem Next()
        {
            var bestIndex = 0;
            var best = this.data[0];
            for (int i = 1; i < this.data.Count; i++)
            {
                if (data[i].Item1 < best.Item1)
                {
                    best = data[i];
                    bestIndex = i;
                }
            }

            this.data.RemoveAt(bestIndex);
            return best.Item2;
        }
    }
}
