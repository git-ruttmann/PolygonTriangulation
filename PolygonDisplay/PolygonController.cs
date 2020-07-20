namespace PolygonDisplay
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using PolygonTriangulation;

    public interface IPolygonForm
    {
        void RefreshState();
    }

    public class PolygonController
    {
        private readonly IPolygonForm form;

        public PolygonController(IPolygonForm form)
        {
            this.form = form;
            this.ActiveStorageId = 1;

            PolygonSamples.GenerateDataOne();
        }

        public int ActiveStorageId { get; private set; }

        public Polygon Polygon { get; private set; }

        public IReadOnlyCollection<Tuple<int, int>> Splits { get; private set; }

        public void ActivateStorage(int id)
        {
            this.ActiveStorageId = id;

            //// this.Polygon = PolygonSamples.Constructed(id);
            //// this.Polygon = PolygonSamples.UnityErrors(id);
            //// this.Polygon = PolygonSamples.InnerFusionSingle(id);
            //// this.Polygon = PolygonSamples.MultiTouch(id);
            this.Polygon = PolygonSamples.MoreFusionTests(id);

            try
            {
                this.Splits = new PolygonTriangulator(this.Polygon).GetSplits().ToArray();
            }
            catch (Exception e)
            {
                if (!ExceptionHelper.CanSwallow(e))
                {
                    throw;
                }

                this.Splits = new Tuple<int, int>[0];
            }
        }

        public string[] BuildTrapezoidationDebug()
        {
            var lines = new List<string>();
            var trapezoidation = new PolygonTriangulator(this.Polygon);
            int limit = 0;
            foreach (var vertexInfo in this.Polygon.OrderedVertices)
            {
                try
                {
                    lines.Add($"{vertexInfo.Prev}>{vertexInfo.Id}>{vertexInfo.Next}");
                    lines.AddRange(trapezoidation.GetEdgesAfterPartialTrapezoidation(++limit));
                    lines.Add(string.Empty);
                }
                catch (Exception e)
                {
                    if (!ExceptionHelper.CanSwallow(e))
                    {
                        throw;
                    }

                    lines.Add(string.Empty);
                    lines.Add(e.ToString());
                    break;
                }
            }

            return lines.ToArray();
        }
    }
}
