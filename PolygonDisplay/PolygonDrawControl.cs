namespace PolygonDisplay
{
    using PolygonTriangulation;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Linq;
    using System.Windows.Forms;

    using Vertex = System.Numerics.Vector2;

    /// <summary>
    /// Polygon display with zoom, highlight and auto-scale
    /// </summary>
    public partial class PolygonDrawControl : UserControl
    {
        /// <summary>
        /// backing value for <see cref="Zoom"/>
        /// </summary>
        private double zoom;

        /// <summary>
        /// scale for zoom level 1 to fully fill the paint area
        /// </summary>
        private double fullScale;

        /// <summary>
        /// the center point in control coordinates
        /// </summary>
        private PointF centerPoint;

        /// <summary>
        /// the center point in polygon coordinates
        /// </summary>
        private Vertex polygonCenter;

        /// <summary>
        /// backing value for <see cref="HighlightIndex"/>
        /// </summary>
        private int highlightIndex;

        /// <summary>
        /// Backing field for <see cref="Polygon"/>
        /// </summary>
        private Polygon polygon;

        /// <summary>
        /// Drag/Drop flag
        /// </summary>
        private bool dragging;

        /// <summary>
        /// Start position of the drag
        /// </summary>
        private Point dragStart;

        /// <summary>
        /// A relative calculated center point
        /// </summary>
        private PointF dragCenter;

        /// <summary>
        /// Constructor
        /// </summary>
        public PolygonDrawControl()
        {
            this.zoom = 1;
            this.polygon = SamplePolygon();
            this.InitializeComponent();
            this.DoubleBuffered = true;
            this.AutoScale(true);
        }

        /// <summary>
        /// Get or set the current polygon
        /// </summary>
        public Polygon Polygon
        {
            get => this.polygon;
            set
            {
                this.polygon = value;
                this.Invalidate();
            }
        }

        /// <summary>
        /// Gets or sets the id of the vertex to highlight
        /// </summary>
        public int HighlightIndex 
        { 
            get => this.highlightIndex;
            set
            {
                this.highlightIndex = value;
                this.Invalidate();
            }
        }

        /// <summary>
        /// Gets or sets the splits
        /// </summary>
        public IReadOnlyCollection<Tuple<int, int>> Splits { get; set; }

        /// <summary>
        /// Center on the highlighted position
        /// </summary>
        public void CenterOnHighlight()
        {
            var value = this.HighlightIndex;
            if (this.polygon != null && value >= 0 && value < this.polygon.Vertices.Count)
            {
                this.polygonCenter = this.polygon.Vertices[value];
            }

            this.Invalidate();
        }

        /// <summary>
        /// Update the scale and the center point
        /// </summary>
        public void AutoScale()
        {
            this.AutoScale(true);
            this.Invalidate();
        }

        /// <summary>
        /// Set a zoom factor
        /// </summary>
        public void SetZoom(int zoomValue)
        {
            this.zoom = Math.Pow(1.2, zoomValue);
            this.Invalidate();
        }

        /// <summary>
        /// Handle control resizing
        /// </summary>
        /// <param name="e">event args</param>
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            this.AutoScale(false);
            this.Invalidate();
        }

        /// <summary>
        /// Begin mouse dragging
        /// </summary>
        /// <param name="e">the drag event</param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.Capture = true;
                this.dragging = true;
                this.dragStart = e.Location;
                this.dragCenter = this.centerPoint;
            }

            base.OnMouseDown(e);
        }

        /// <summary>
        /// Update during mouse drag
        /// </summary>
        /// <param name="e">the drag event</param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (this.dragging)
            {
                var dragVector = e.Location - (Size)this.dragStart;
                this.centerPoint = new PointF(this.dragCenter.X + dragVector.X, this.dragCenter.Y - dragVector.Y);
                this.Invalidate();
            }
        }

        /// <summary>
        /// End of mouse drag
        /// </summary>
        /// <param name="e">the drag event</param>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && this.dragging)
            {
                this.Capture = false;
                this.dragging = false;

                var dragVector = e.Location - (Size)this.dragStart;
                var scale = (this.fullScale * this.zoom);

                this.polygonCenter = new Vertex(
                    (float)(this.polygonCenter.X - dragVector.X / scale),
                    (float)(this.polygonCenter.Y + dragVector.Y / scale));

                this.centerPoint = this.dragCenter;
                this.Invalidate();
            }

            base.OnMouseUp(e);
        }

        /// <summary>
        /// Draw the current content
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (this.polygon == null || this.polygon.Vertices.Count < 2)
            {
                return;
            }

            var scaledVertices = this.ScaleVertices(this.polygon.Vertices);
            var g = e.Graphics;

            if ((this.Splits?.Count ?? -1) >= 0)
            {
                DrawTriangles(g, scaledVertices);
            }

            this.DrawPolygon(g, scaledVertices);

            if (this.Splits != null)
            {
                this.DrawSplits(g, scaledVertices);
            }
        }

        /// <summary>
        /// Draw the triangles and monotones
        /// </summary>
        /// <param name="g">the GDI graphics context</param>
        /// <param name="scaledVertices">the vertices, scaled to points</param>
        private void DrawTriangles(Graphics g, PointF[] scaledVertices)
        {
            var triangleCollector = PolygonTriangulator.CreateTriangleCollector();
            var montonizedPolygon = Polygon.Split(this.Polygon, this.Splits, triangleCollector);

            var colorIndex = 0;
            var colors = new[]
            {
                    Color.LightGoldenrodYellow,
                    Color.LightGreen,
                    Color.LightBlue,
                    Color.LightCoral,
                };

            foreach (var polygonId in montonizedPolygon.SubPolygonIds)
            {
                var brushColor = Color.FromArgb(127, colors[colorIndex]);
                colorIndex = (colorIndex + 1) % colors.Length;
                var brush = new SolidBrush(brushColor);

                var vertices = montonizedPolygon.SubPolygonVertices(polygonId).Select(x => scaledVertices[x]).ToArray();
                g.FillPolygon(brush, vertices);
            }

            var triangles = triangleCollector.Triangles;
            var greyBrush = new SolidBrush(Color.FromArgb(47, Color.LightGray));
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var vertices = triangles.Skip(i).Take(3).Select(x => scaledVertices[x]).ToArray();
                g.FillPolygon(greyBrush, vertices);
            }
        }

        /// <summary>
        /// Draw the polygon
        /// </summary>
        /// <param name="g">the GDI graphics context</param>
        /// <param name="scaledVertices">the vertices, scaled to points</param>
        private void DrawPolygon(Graphics g, PointF[] scaledVertices)
        {
            var pen = new Pen(Color.Black);
            pen.Width = this.LogicalToDeviceUnits(1);
            pen.SetLineCap(LineCap.Flat, LineCap.ArrowAnchor, DashCap.Flat);

            foreach (var subPolygonId in this.polygon.SubPolygonIds)
            {
                var lastVertex = this.polygon.SubPolygonVertices(subPolygonId).Last();
                var lastPoint = scaledVertices[lastVertex];
                foreach (var vertexId in this.polygon.SubPolygonVertices(subPolygonId))
                {
                    var point = scaledVertices[vertexId];
                    g.DrawLine(pen, lastPoint, point);
                    this.DrawVertexInformation(g, vertexId, point);
                    lastPoint = point;
                }
            }
        }

        /// <summary>
        /// Draw the splits
        /// </summary>
        /// <param name="g">the GDI graphics context</param>
        /// <param name="scaledVertices">the vertices, scaled to points</param>
        private void DrawSplits(Graphics g, PointF[] scaledVertices)
        {
            var pen = new Pen(Color.IndianRed);
            pen.Width = this.LogicalToDeviceUnits(2);
            pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Custom;
            pen.DashPattern = new[] { 4f, 4f, 4f, 4f };

            foreach (var split in this.Splits)
            {
                if (split.Item1 != split.Item2)
                {
                    g.DrawLine(pen, scaledVertices[split.Item1], scaledVertices[split.Item2]);
                }
                else
                {
                    var radius = this.LogicalToDeviceUnits(5);
                    var point = scaledVertices[split.Item1];
                    g.DrawEllipse(pen, point.X - radius, point.Y - radius, 2 * radius, 2 * radius);
                }
            }
        }

        /// <summary>
        /// Build a sampel polygon
        /// </summary>
        /// <returns>the polygon</returns>
        private static Polygon SamplePolygon()
        {
            var sortedVertices = new[]
            {
                new Vertex(1, 1), // 0
                new Vertex(1, 3),
                new Vertex(1.5f, 3), // 2
                new Vertex(2, 2),
                new Vertex(2, 4), // 4
                new Vertex(2.5f, 1),
                new Vertex(2.5f, 2), // 6
                new Vertex(2.5f, 3),
                new Vertex(3.5f, 2.5f), // 8
                new Vertex(3.5f, 1),
                new Vertex(4, 1.5f), // 10
                new Vertex(4, 3.5f),
                new Vertex(4, 4), // 12
            };

            var polygon = Polygon.Build(sortedVertices)
                .AddVertices(5, 0, 6, 3, 1, 4, 12, 2, 7, 11, 8, 10, 9)
                .Close();
            return polygon;
        }

        /// <summary>
        /// Convert the vertices to scaled points
        /// </summary>
        /// <returns>Array of scaled points</returns>
        private PointF[] ScaleVertices(IReadOnlyList<Vertex> vertices)
        {
            var scale = (this.fullScale * this.zoom);

            var scaledPoints = vertices
                .Select(x => new PointF(
                    (float)((x.X - this.polygonCenter.X) * scale + this.centerPoint.X),
                    this.Size.Height - (float)((x.Y - this.polygonCenter.Y) * scale + this.centerPoint.Y)))
                .ToArray();
            return scaledPoints;
        }

        /// <summary>
        /// Draw information about a vertex (id and selected state)
        /// </summary>
        /// <param name="g">the GDI context</param>
        /// <param name="vertexId">the id of the vertex</param>
        /// <param name="point">the locaion in screen coordinates</param>
        private void DrawVertexInformation(Graphics g, int vertexId, PointF point)
        {
            var radius = this.LogicalToDeviceUnits(vertexId == this.HighlightIndex ? 5 : 4);
            var circlePen = vertexId == this.HighlightIndex ? Pens.Red : Pens.CadetBlue;
            g.DrawEllipse(circlePen, point.X - radius, point.Y - radius, radius * 2, radius * 2);
            var font = this.Font;

            var width = 100;
            var offset = this.LogicalToDeviceUnits(3);
            var rect = new RectangleF(point.X - width, point.Y + offset, width - offset, font.SizeInPoints * 20);
            var stringFormat = new StringFormat() { Alignment = StringAlignment.Far };

            g.DrawString(vertexId.ToString(), font, Brushes.DarkSlateBlue, rect, stringFormat);
        }

        /// <summary>
        /// Find the size of the polygon and calculate a scale that everything fits on the control
        /// </summary>
        /// <param name="updateCenter">flag: update the current center</param>
        private void AutoScale(bool updateCenter)
        {
            if (this.polygon == null)
            {
                this.fullScale = 1.0;
                this.centerPoint = new PointF(1, 1);
                return;
            }

            var minX = this.polygon.Vertices.Select(x => x.X).Min();
            var maxX = this.polygon.Vertices.Select(x => x.X).Max();
            var minY = this.polygon.Vertices.Select(x => x.Y).Min();
            var maxY = this.polygon.Vertices.Select(x => x.Y).Max();

            var scaleX = this.Size.Width / Math.Max((maxX - minX) * 1.1, 0.1);
            var scaleY = this.Size.Height / Math.Max((maxY - minY) * 1.1, 0.1);
            this.fullScale = Math.Min(scaleX, scaleY);

            this.centerPoint = new PointF(this.Size.Width / 2, this.Size.Height / 2);

            var spanX = Math.Max(maxX - minX, 0.1f);
            var spanY = Math.Max(maxY - minY, 0.1f);

            if (updateCenter)
            {
                this.polygonCenter = new Vertex(minX + (maxX - minX) / 2, minY + (maxY - minY) / 2);
            }
        }
    }
}
