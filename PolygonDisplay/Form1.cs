namespace PolygonDisplay
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Windows.Forms;

    using PolygonTriangulation;

    /// <summary>
    /// A simple form to select polygons and host a polygon draw control
    /// </summary>
    public partial class Form1 : Form, IPolygonForm
    {
        private readonly PolygonController controller;

        /// <summary>
        /// Initializes a new instance of the <see cref="Form1"/> class.
        /// </summary>
        public Form1()
        {
            this.InitializeComponent();
            this.controller = new PolygonController(this);
            this.ActivateStorage(1);
        }

        /// <summary>
        /// Refreshes the state.
        /// </summary>
        public void RefreshState()
        {
            this.UpdateSelectedBufferState();
            this.polygonOrderLabel.Text = string.Join(" ", this.controller.Polygon.Debug);
            this.vertexIdSelector.Maximum = this.controller.Polygon.Vertices.Count() - 1;
            this.vertexIdSelector.Value = Math.Max(0, Math.Min(this.vertexIdSelector.Maximum, this.polygonPanel.HighlightIndex));
        }

        /// <summary>
        /// Gets the id of the storage button by it's name
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <returns>the id of the selected storage</returns>
        private static int IdOfStorageButton(object sender)
        {
            return int.Parse((sender as Control).Name.Substring(1));
        }

        /// <summary>
        /// Handles the Click event of the storage control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void storage_Click(object sender, EventArgs e)
        {
            var id = IdOfStorageButton(sender);
            this.ActivateStorage(id);
        }

        /// <summary>
        /// Activates the storage.
        /// </summary>
        /// <param name="id">The identifier.</param>
        private void ActivateStorage(int id)
        {
            this.controller.ActivateStorage(id);
            this.polygonPanel.Polygon = this.controller.Polygon;
            this.polygonPanel.Splits = this.controller.Splits;
            this.vertexText.Text = string.Join(
                Environment.NewLine,
                this.controller.Polygon.OrderedVertices
                    .Select(x => $"{x.PrevVertexId}>{x.Id}>{x.NextVertexId}")
                    .ToArray());

            if ((Control.ModifierKeys & Keys.Shift) == 0)
            {
                this.polygonPanel.AutoScale();
            }

            this.RefreshState();
        }

        /// <summary>
        /// Handles the Click event of the button1 control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void button1_Click(object sender, EventArgs e)
        {
            this.vertexText.Lines = this.controller.BuildTrapezoidationDebug();
        }

        /// <summary>
        /// Updates the selected state for the selected buffer.
        /// </summary>
        private void UpdateSelectedBufferState()
        {
            var buttons = this.storagePanel.Controls
                .OfType<Button>()
                .Where(x => Regex.Match(x.Name, "^s[0-9*]$").Success);
            foreach (var button in buttons)
            {
                button.BackColor = IdOfStorageButton(button) == this.controller.ActiveStorageId ? SystemColors.MenuHighlight : this.debugButton.BackColor;
            }
        }

        /// <summary>
        /// Handles the Scroll event of the zoomSlider control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void zoomSlider_Scroll(object sender, EventArgs e)
        {
            this.polygonPanel.Zoom = this.zoomSlider.Value;
        }

        /// <summary>
        /// Handles the Scroll event of the vertexIdSelector control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void vertexIdSelector_Scroll(object sender, EventArgs e)
        {
            this.polygonPanel.HighlightIndex = this.vertexIdSelector.Value;
            if ((Control.ModifierKeys & Keys.Shift) == 0)
            {
                this.polygonPanel.CenterOnHighlight();
            }

            this.vertexIdLabel.Text = this.vertexIdSelector.Value.ToString();
        }

        /// <summary>
        /// Handles the Click event of the splitButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void splitButton_Click(object sender, EventArgs e)
        {
            try
            {
                this.polygonPanel.Splits =
                    new PolygonTriangulator(this.controller.Polygon).GetSplits().ToArray();
            }
            catch (Exception ex)
            {
                if (!ExceptionHelper.CanSwallow(ex))
                {
                    throw;
                }

                this.vertexText.Text = ex.ToString();
            }

            this.polygonPanel.AutoScale();
        }

        /// <summary>
        /// Handles the Click event of the triangulateButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void triangulateButton_Click(object sender, EventArgs e)
        {
            this.vertexText.Text = string.Empty;
            var lines = new List<string>();
            var collector = PolygonTriangulator.CreateTriangleCollector();
            try
            {
                var triangulator = new PolygonTriangulator(this.controller.Polygon);
                var splits = triangulator.GetSplits();
                lines.Add("Splits");
                lines.AddRange(splits.Select(x => $"{x.Item1} - {x.Item2}"));
                lines.Add(string.Empty);

                var monotones = Polygon.Split(this.controller.Polygon, splits, PolygonTriangulator.CreateTriangleCollector());
                lines.Add("Monotones");
                lines.AddRange(monotones.SubPolygonIds.Select(x => string.Join(" ", monotones.SubPolygonVertices(x))));
                lines.Add(string.Empty);

                triangulator.BuildTriangles(collector);
            }
            catch (Exception ex)
            {
                if (!ExceptionHelper.CanSwallow(ex))
                {
                    throw;
                }

                this.vertexText.Text = ex.ToString();
            }

            var triangles = collector.Triangles;
            lines.Add("Triangles");
            for (int i = 0; i < triangles.Length; i += 3)
            {
                lines.Add($"{triangles[i + 0]} {triangles[i + 1]} {triangles[i + 2]} ");
            }

            this.vertexText.Text += string.Join(Environment.NewLine, lines);
            this.polygonPanel.AutoScale();
        }
    }
}
