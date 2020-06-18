namespace PolygonDisplay
{
    using System;
    using System.Data;
    using System.Drawing;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Windows.Forms;

    public partial class Form1 : Form, IPolygonForm
    {
        private readonly PolygonController controller;

        public Form1()
        {
            this.InitializeComponent();
            this.controller = new PolygonController(this);
            this.ActivateStorage(1);
        }

        private void storage_Click(object sender, EventArgs e)
        {
            var id = IdOfStorageButton(sender);
            this.ActivateStorage(id);
        }

        private void ActivateStorage(int id)
        {
            this.controller.ActivateStorage(id);
            this.polygonPanel.Polygon = this.controller.Polygon;

            if ((Control.ModifierKeys & Keys.Shift) == 0)
            {
                this.polygonPanel.AutoScale();
            }

            this.RefreshState();
        }

        private static int IdOfStorageButton(object sender)
        {
            return int.Parse((sender as Control).Name.Substring(1));
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        public void RefreshState()
        {
            this.UpdateSelectedBufferState();
            this.polygonOrderLabel.Text = string.Join(" ", this.controller.Polygon.Debug);
            this.vertexIdSelector.Maximum = this.controller.Polygon.OrderedVertexes.Count();
            this.vertexIdSelector.Value = Math.Max(0, Math.Min(this.vertexIdSelector.Maximum, this.polygonPanel.HighlightIndex));
        }

        private void UpdateSelectedBufferState()
        {
            var buttons = this.storagePanel.Controls
                .OfType<Button>()
                .Where(x => Regex.Match(x.Name, "^s[0-9*]$").Success);
            foreach (var button in buttons)
            {
                button.FlatStyle = IdOfStorageButton(button) == this.controller.ActiveStorageId ? FlatStyle.Popup : FlatStyle.Flat;
            }
        }

        private void zoomSlider_Scroll(object sender, EventArgs e)
        {
            this.polygonPanel.SetZoom(this.zoomSlider.Value);
        }

        private void vertexIdSelector_Scroll(object sender, EventArgs e)
        {
            this.polygonPanel.HighlightIndex = this.vertexIdSelector.Value;
            if ((Control.ModifierKeys & Keys.Shift) == 0)
            {
                this.polygonPanel.CenterOnHighlight();
            }

            this.vertexIdLabel.Text = this.vertexIdSelector.Value.ToString();
        }
    }
}
