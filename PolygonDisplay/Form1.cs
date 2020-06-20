﻿namespace PolygonDisplay
{
    using PolygonTriangulation;
    using System;
    using System.Collections.Generic;
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
            this.polygonPanel.Splits = this.controller.Splits;
            this.vertexText.Text = String.Join(Environment.NewLine,
                this.controller.Polygon.OrderedVertexes
                .Select(x => $"{x.Prev}>{x.Id}>{x.Next}")
                .ToArray());

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
            this.vertexText.Lines = this.controller.BuildTrapezoidationDebug();
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
                button.BackColor = IdOfStorageButton(button) == this.controller.ActiveStorageId ? SystemColors.MenuHighlight : this.debugButton.BackColor;
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

        private void splitButton_Click(object sender, EventArgs e)
        {
            try
            {
                this.polygonPanel.Splits =
                    new PolygonTriangulator(this.controller.Polygon).GetSplits().ToArray();
            }
            catch (Exception ex)
            {
                this.vertexText.Text = ex.ToString();
            }

            this.polygonPanel.AutoScale();
        }

        private void triangulateButton_Click(object sender, EventArgs e)
        {
            this.vertexText.Text = string.Empty;
            var collector = PolygonTriangulator.CreateTriangleCollector();
            try
            {
                new PolygonTriangulator(this.controller.Polygon).BuildTriangles(collector);
            }
            catch (Exception ex)
            {
                this.vertexText.Text = ex.ToString();
            }

            var lines = new List<string>();
            var triangles = collector.Triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                lines.Add($"{triangles[i + 0]} {triangles[i + 1]} {triangles[i + 2]} ");
            }

            this.vertexText.Text += string.Join(Environment.NewLine, lines);
            this.polygonPanel.AutoScale();
        }
    }
}