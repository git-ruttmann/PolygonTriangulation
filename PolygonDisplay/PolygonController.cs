namespace PolygonDisplay
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using PolygonTriangulation;

    /// <summary>
    /// the necessary abstraction for the form
    /// </summary>
    public interface IPolygonForm
    {
        /// <summary>
        /// Refresh all state dependent GUI elements.
        /// </summary>
        void RefreshState();
    }

    /// <summary>
    /// A controller for the polygon behaviour
    /// </summary>
    public class PolygonController
    {
        private readonly IPolygonForm form;

        /// <summary>
        /// Initializes a new instance of the <see cref="PolygonController"/> class.
        /// </summary>
        /// <param name="form">The form.</param>
        public PolygonController(IPolygonForm form)
        {
            this.form = form;
            this.ActiveStorageId = 1;

            PolygonSamples.GenerateDataOne();
        }

        /// <summary>
        /// Gets the id of the actived storage.
        /// </summary>
        public int ActiveStorageId { get; private set; }

        /// <summary>
        /// Gets the polygon to draw.
        /// </summary>
        public Polygon Polygon { get; private set; }

        /// <summary>
        /// Gets the splits of the polygon.
        /// </summary>
        public IReadOnlyCollection<Tuple<int, int>> Splits { get; private set; }

        /// <summary>
        /// Activates the storage.
        /// </summary>
        /// <param name="id">The identifier.</param>
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

        /// <summary>
        /// Build debug steps for the trapezoidation.
        /// </summary>
        /// <returns>the debug text</returns>
        public string[] BuildTrapezoidationDebug()
        {
            var lines = new List<string>();
            var trapezoidation = new PolygonTriangulator(this.Polygon);
            int limit = 0;
            foreach (var vertexInfo in this.Polygon.OrderedVertices)
            {
                try
                {
                    lines.Add($"{vertexInfo.PrevVertexId}>{vertexInfo.Id}>{vertexInfo.NextVertexId}");
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
