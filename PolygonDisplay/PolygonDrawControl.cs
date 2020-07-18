namespace PolygonDisplay
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Linq;
    using System.Windows.Forms;

    using PolygonTriangulation;
    using Vertex = System.Numerics.Vector2;

    /// <summary>
    /// Polygon display with zoom, highlight and auto-scale
    /// </summary>
    public partial class PolygonDrawControl : UserControl
    {
        private enum VertexTextPosition
        {
            Left,
            TopLeft,
            BottomLeft,
            Right,
            TopRight,
            BottomRight,
            Top,
            Bottom,
        }

        /// <summary>
        /// The drawn vertex id's
        /// </summary>
        private readonly HashSet<int> drawnVertexIds;

        /// <summary>
        /// backing value for <see cref="Zoom"/>
        /// </summary>
        private double zoom;

        /// <summary>
        /// zoom as integer factor
        /// </summary>
        private int zoomAsInt;

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
            this.drawnVertexIds = new HashSet<int>();
            this.zoom = 1;
            this.zoomAsInt = 0;
            this.polygon = SamplePolygon();
            this.InitializeComponent();
            this.DoubleBuffered = true;
            this.HighlightIndex = -1;
            this.ShowSplits = true;
            this.ShowMonotones = false;
            this.ShowMonotones = true;
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
        /// Get's or sets a zoom identifier
        /// </summary>
        public int Zoom
        {
            get => this.zoomAsInt;
            set
            {
                this.zoomAsInt = Math.Min(55, Math.Max(0, value));
                this.zoom = Math.Pow(1.2, this.zoomAsInt);
                this.Invalidate();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating that splits are drawn
        /// </summary>
        public bool ShowSplits { get; set; }

        /// <summary>
        /// Gets or sets a value indicating that monotones are drawn in a different color.
        /// </summary>
        public bool ShowMonotones { get; set; }

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
        /// Handle control resizing
        /// </summary>
        /// <param name="e">event args</param>
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            this.AutoScale(false);
            this.Invalidate();
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (this.dragging)
            {
                var dragVector = e.Location - (Size)this.dragStart;
                this.centerPoint = new PointF(this.dragCenter.X + dragVector.X, this.dragCenter.Y - dragVector.Y);
                this.Invalidate();
            }
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            this.Zoom = this.zoomAsInt + e.Delta / SystemInformation.MouseWheelScrollDelta;
            base.OnMouseWheel(e);
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

            this.drawnVertexIds.Clear();
            var scaledVertices = this.ScaleVertices(this.polygon.Vertices);
            var g = e.Graphics;

            if ((this.Splits?.Count ?? -1) >= 0)
            {
                this.DrawTriangles(g, scaledVertices);
            }

            this.DrawPolygon(g, scaledVertices);

            for (int i = 0; i < scaledVertices.Length; i++)
            {
                this.DrawVertexInformation(g, i, scaledVertices[i], VertexTextPosition.BottomLeft);
            }

            if (this.ShowSplits && this.Splits != null)
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
            var greyBrush = new SolidBrush(Color.FromArgb(47, Color.LightGray));

            Polygon montonizedPolygon;
            try
            {
                montonizedPolygon = Polygon.Split(this.Polygon, this.Splits, triangleCollector);
            }
            catch (InvalidOperationException)
            {
                return;
            }

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
                var brush = this.ShowMonotones ? new SolidBrush(brushColor) : greyBrush;

                var vertices = montonizedPolygon.SubPolygonVertices(polygonId).Select(x => scaledVertices[x]).ToArray();
                g.FillPolygon(brush, vertices);
            }

            var triangles = triangleCollector.Triangles;
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
            pen.SetLineCap(LineCap.Flat, LineCap.Custom, DashCap.Flat);
            pen.CustomEndCap = this.CreateArrowCap();

            foreach (var subPolygonId in this.polygon.SubPolygonIds)
            {
                var vertexIds = this.polygon.SubPolygonVertices(subPolygonId).ToArray();
                for (var i = 0; i < vertexIds.Length; i++)
                {
                    var vertexId = vertexIds[i];
                    var prev = i == 0 ? vertexIds.Last() : vertexIds[i - 1];
                    var next = i == vertexIds.Length - 1 ? vertexIds.First() : vertexIds[i + 1];
                    g.DrawLine(pen, scaledVertices[prev], scaledVertices[vertexId]);

                    var vertexTextPosition = this.GetVertexTextPosition(vertexId, prev, next);
                    this.DrawVertexInformation(g, vertexId, scaledVertices[vertexId], vertexTextPosition);
                }
            }
        }

        /// <summary>
        /// Create a working arrow cap
        /// </summary>
        /// <returns></returns>
        private CustomLineCap CreateArrowCap()
        {
            var hPath = new GraphicsPath();

            var x = this.LogicalToDeviceUnits(1);
            var y = this.LogicalToDeviceUnits(4);
            hPath.AddLine(new Point(0, 0), new Point(-x, -y));
            hPath.AddLine(new Point(-x, -y), new Point(x, -y));
            hPath.AddLine(new Point(x, -y), new Point(0, 0));

            var arrowCap = new CustomLineCap(null, hPath);

            arrowCap.SetStrokeCaps(LineCap.Round, LineCap.Round);
            return arrowCap;
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

            return Polygon.Build(sortedVertices)
                .AddVertices(5, 0, 6, 3, 1, 4, 12, 2, 7, 11, 8, 10, 9)
                .Close();
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
        /// <param name="vertexTextPosition">the vertex text position</param>
        private void DrawVertexInformation(Graphics g, int vertexId, PointF point, VertexTextPosition vertexTextPosition)
        {
            if (!this.drawnVertexIds.Add(vertexId))
            {
                return;
            }

            var radius = this.LogicalToDeviceUnits(vertexId == this.HighlightIndex ? 5 : 4);
            var circlePen = vertexId == this.HighlightIndex ? Pens.Red : Pens.CadetBlue;
            g.DrawEllipse(circlePen, point.X - radius, point.Y - radius, radius * 2, radius * 2);

            var (rect, stringFormat) = this.GetVertexTextRectangle(point, vertexTextPosition);
            g.DrawString(vertexId.ToString(), this.Font, Brushes.DarkSlateBlue, rect, stringFormat);
        }

        /// <summary>
        /// Calculate the text rectangle and the text alignment for a vertex text, depending on it's position
        /// </summary>
        /// <param name="point">the drawing location of the vertex</param>
        /// <param name="vertexTextPosition">the vertex text position</param>
        /// <returns>the text rectancle and the text alignment</returns>
        private (RectangleF, StringFormat) GetVertexTextRectangle(PointF point, VertexTextPosition vertexTextPosition)
        {
            var offset = this.LogicalToDeviceUnits(3);
            var offset2 = this.LogicalToDeviceUnits(5);
            var width = 100;
            var height = this.Font.SizeInPoints * 20;
            var stringFormat = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Near };
            var fontHeight = this.LogicalToDeviceUnits(Convert.ToInt32(this.Font.SizeInPoints));
            RectangleF rect;

            switch (vertexTextPosition)
            {
                case VertexTextPosition.Left:
                    stringFormat.LineAlignment = StringAlignment.Center;
                    stringFormat.Alignment = StringAlignment.Far;
                    rect = new RectangleF(point.X - width, point.Y - height / 2, width - offset2, height);
                    break;
                case VertexTextPosition.TopLeft:
                    stringFormat.Alignment = StringAlignment.Far;
                    rect = new RectangleF(point.X - width, point.Y - offset2 - fontHeight, width - offset, height);
                    break;
                case VertexTextPosition.BottomLeft:
                    stringFormat.Alignment = StringAlignment.Far;
                    rect = new RectangleF(point.X - width, point.Y + offset, width - offset, height);
                    break;
                case VertexTextPosition.Right:
                    stringFormat.LineAlignment = StringAlignment.Center;
                    stringFormat.Alignment = StringAlignment.Near;
                    rect = new RectangleF(point.X + offset2, point.Y - height / 2, width - offset2, height);
                    break;
                case VertexTextPosition.TopRight:
                    stringFormat.Alignment = StringAlignment.Near;
                    rect = new RectangleF(point.X + offset2, point.Y - offset2 - fontHeight, width - offset, height);
                    break;
                case VertexTextPosition.BottomRight:
                    stringFormat.Alignment = StringAlignment.Near;
                    rect = new RectangleF(point.X + offset, point.Y + offset, width - offset, height);
                    break;
                case VertexTextPosition.Top:
                    stringFormat.Alignment = StringAlignment.Center;
                    rect = new RectangleF(point.X - width / 2, point.Y - offset2 * 2 - fontHeight, width, height);
                    break;
                case VertexTextPosition.Bottom:
                    stringFormat.Alignment = StringAlignment.Center;
                    rect = new RectangleF(point.X - width / 2, point.Y + offset2, width, height);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(vertexTextPosition));
            }

            return (rect, stringFormat);
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

            if (updateCenter)
            {
                this.polygonCenter = new Vertex(minX + (maxX - minX) / 2, minY + (maxY - minY) / 2);
            }
        }

        /// <summary>
        /// Choose a text position for 3 connected vertices, so the text is not overlapped by the line
        /// </summary>
        /// <param name="vertexId">the vertex with the text</param>
        /// <param name="prevId">the previous vertex</param>
        /// <param name="nextId">the next vertex</param>
        /// <returns>the text position</returns>
        private VertexTextPosition GetVertexTextPosition(int vertexId, int prevId, int nextId)
        {
            var vertex = this.polygon.Vertices[vertexId];
            var prev = this.polygon.Vertices[prevId];
            var next = this.polygon.Vertices[nextId];

            var prevRight = prev.X > vertex.X;
            var prevAbove = prev.Y > vertex.Y;
            var nextRight = next.X > vertex.X;
            var nextAbove = next.Y > vertex.Y;

            if (prevRight == nextRight)
            {
                return prevRight ? VertexTextPosition.Left : VertexTextPosition.Right;
            }

            if (prevAbove == nextAbove)
            {
                return prevAbove ? VertexTextPosition.Bottom : VertexTextPosition.Top;
            }

            if (prevRight)
            {
                return prevAbove ? VertexTextPosition.BottomRight : VertexTextPosition.BottomLeft;
            }
            else
            {
                return prevAbove ? VertexTextPosition.TopRight : VertexTextPosition.TopLeft;
            }
        }
    }
}
