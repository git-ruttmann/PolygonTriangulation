namespace PolygonDisplay
{
    partial class Form1
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.zoomSlider = new System.Windows.Forms.TrackBar();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.polygonOrderLabel = new System.Windows.Forms.Label();
            this.vertexIdLabel = new System.Windows.Forms.Label();
            this.vertexIdSelector = new System.Windows.Forms.TrackBar();
            this.polygonPanel = new PolygonDisplay.PolygonDrawControl();
            this.storagePanel = new System.Windows.Forms.Panel();
            this.triangulateButton = new System.Windows.Forms.Button();
            this.s6 = new System.Windows.Forms.Button();
            this.s5 = new System.Windows.Forms.Button();
            this.splitButton = new System.Windows.Forms.Button();
            this.debugButton = new System.Windows.Forms.Button();
            this.s4 = new System.Windows.Forms.Button();
            this.s3 = new System.Windows.Forms.Button();
            this.s2 = new System.Windows.Forms.Button();
            this.s1 = new System.Windows.Forms.Button();
            this.vertexText = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.zoomSlider)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.vertexIdSelector)).BeginInit();
            this.storagePanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // zoomSlider
            // 
            this.zoomSlider.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.zoomSlider.Location = new System.Drawing.Point(0, 1116);
            this.zoomSlider.Maximum = 55;
            this.zoomSlider.Name = "zoomSlider";
            this.zoomSlider.Size = new System.Drawing.Size(1352, 69);
            this.zoomSlider.TabIndex = 0;
            this.zoomSlider.Scroll += new System.EventHandler(this.zoomSlider_Scroll);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.polygonOrderLabel);
            this.splitContainer1.Panel1.Controls.Add(this.vertexIdLabel);
            this.splitContainer1.Panel1.Controls.Add(this.vertexIdSelector);
            this.splitContainer1.Panel1.Controls.Add(this.polygonPanel);
            this.splitContainer1.Panel1.Controls.Add(this.zoomSlider);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.storagePanel);
            this.splitContainer1.Panel2.Controls.Add(this.vertexText);
            this.splitContainer1.Size = new System.Drawing.Size(1755, 1185);
            this.splitContainer1.SplitterDistance = 1352;
            this.splitContainer1.TabIndex = 1;
            // 
            // polygonOrderLabel
            // 
            this.polygonOrderLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.polygonOrderLabel.AutoSize = true;
            this.polygonOrderLabel.Location = new System.Drawing.Point(12, 1052);
            this.polygonOrderLabel.Name = "polygonOrderLabel";
            this.polygonOrderLabel.Size = new System.Drawing.Size(83, 20);
            this.polygonOrderLabel.TabIndex = 4;
            this.polygonOrderLabel.Text = "1 2 3 4 5 6";
            // 
            // vertexIdLabel
            // 
            this.vertexIdLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.vertexIdLabel.AutoSize = true;
            this.vertexIdLabel.Location = new System.Drawing.Point(810, 1052);
            this.vertexIdLabel.Name = "vertexIdLabel";
            this.vertexIdLabel.Size = new System.Drawing.Size(18, 20);
            this.vertexIdLabel.TabIndex = 3;
            this.vertexIdLabel.Text = "1";
            // 
            // vertexIdSelector
            // 
            this.vertexIdSelector.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.vertexIdSelector.Location = new System.Drawing.Point(848, 1041);
            this.vertexIdSelector.Name = "vertexIdSelector";
            this.vertexIdSelector.Size = new System.Drawing.Size(504, 69);
            this.vertexIdSelector.TabIndex = 2;
            this.vertexIdSelector.Scroll += new System.EventHandler(this.vertexIdSelector_Scroll);
            // 
            // polygonPanel
            // 
            this.polygonPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.polygonPanel.BackColor = System.Drawing.Color.White;
            this.polygonPanel.HighlightIndex = 0;
            this.polygonPanel.Location = new System.Drawing.Point(0, 0);
            this.polygonPanel.Margin = new System.Windows.Forms.Padding(4);
            this.polygonPanel.Name = "polygonPanel";
            this.polygonPanel.Polygon = null;
            this.polygonPanel.Size = new System.Drawing.Size(1349, 1016);
            this.polygonPanel.Splits = null;
            this.polygonPanel.TabIndex = 1;
            // 
            // storagePanel
            // 
            this.storagePanel.Controls.Add(this.triangulateButton);
            this.storagePanel.Controls.Add(this.s6);
            this.storagePanel.Controls.Add(this.s5);
            this.storagePanel.Controls.Add(this.splitButton);
            this.storagePanel.Controls.Add(this.debugButton);
            this.storagePanel.Controls.Add(this.s4);
            this.storagePanel.Controls.Add(this.s3);
            this.storagePanel.Controls.Add(this.s2);
            this.storagePanel.Controls.Add(this.s1);
            this.storagePanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.storagePanel.Location = new System.Drawing.Point(0, 0);
            this.storagePanel.Name = "storagePanel";
            this.storagePanel.Size = new System.Drawing.Size(399, 121);
            this.storagePanel.TabIndex = 1;
            // 
            // triangulateButton
            // 
            this.triangulateButton.Location = new System.Drawing.Point(112, 59);
            this.triangulateButton.Name = "triangulateButton";
            this.triangulateButton.Size = new System.Drawing.Size(102, 40);
            this.triangulateButton.TabIndex = 8;
            this.triangulateButton.Text = "Triangulate";
            this.triangulateButton.UseVisualStyleBackColor = true;
            this.triangulateButton.Click += new System.EventHandler(this.triangulateButton_Click);
            // 
            // s6
            // 
            this.s6.Location = new System.Drawing.Point(272, 13);
            this.s6.Name = "s6";
            this.s6.Size = new System.Drawing.Size(48, 40);
            this.s6.TabIndex = 7;
            this.s6.Text = "6";
            this.s6.UseVisualStyleBackColor = true;
            this.s6.Click += new System.EventHandler(this.storage_Click);
            // 
            // s5
            // 
            this.s5.Location = new System.Drawing.Point(220, 13);
            this.s5.Name = "s5";
            this.s5.Size = new System.Drawing.Size(48, 40);
            this.s5.TabIndex = 6;
            this.s5.Text = "5";
            this.s5.UseVisualStyleBackColor = true;
            this.s5.Click += new System.EventHandler(this.storage_Click);
            // 
            // splitButton
            // 
            this.splitButton.Location = new System.Drawing.Point(220, 59);
            this.splitButton.Name = "splitButton";
            this.splitButton.Size = new System.Drawing.Size(100, 40);
            this.splitButton.TabIndex = 5;
            this.splitButton.Text = "Split";
            this.splitButton.UseVisualStyleBackColor = true;
            this.splitButton.Click += new System.EventHandler(this.splitButton_Click);
            // 
            // debugButton
            // 
            this.debugButton.Location = new System.Drawing.Point(4, 59);
            this.debugButton.Name = "debugButton";
            this.debugButton.Size = new System.Drawing.Size(102, 40);
            this.debugButton.TabIndex = 4;
            this.debugButton.Text = "Debug";
            this.debugButton.UseVisualStyleBackColor = true;
            this.debugButton.Click += new System.EventHandler(this.button1_Click);
            // 
            // s4
            // 
            this.s4.Location = new System.Drawing.Point(166, 13);
            this.s4.Name = "s4";
            this.s4.Size = new System.Drawing.Size(48, 40);
            this.s4.TabIndex = 3;
            this.s4.Text = "4";
            this.s4.UseVisualStyleBackColor = true;
            this.s4.Click += new System.EventHandler(this.storage_Click);
            // 
            // s3
            // 
            this.s3.Location = new System.Drawing.Point(112, 13);
            this.s3.Name = "s3";
            this.s3.Size = new System.Drawing.Size(48, 40);
            this.s3.TabIndex = 2;
            this.s3.Text = "3";
            this.s3.UseVisualStyleBackColor = true;
            this.s3.Click += new System.EventHandler(this.storage_Click);
            // 
            // s2
            // 
            this.s2.Location = new System.Drawing.Point(58, 13);
            this.s2.Name = "s2";
            this.s2.Size = new System.Drawing.Size(48, 40);
            this.s2.TabIndex = 1;
            this.s2.Text = "2";
            this.s2.UseVisualStyleBackColor = true;
            this.s2.Click += new System.EventHandler(this.storage_Click);
            // 
            // s1
            // 
            this.s1.Location = new System.Drawing.Point(4, 13);
            this.s1.Name = "s1";
            this.s1.Size = new System.Drawing.Size(48, 40);
            this.s1.TabIndex = 0;
            this.s1.Text = "1";
            this.s1.UseVisualStyleBackColor = true;
            this.s1.Click += new System.EventHandler(this.storage_Click);
            // 
            // vertexText
            // 
            this.vertexText.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.vertexText.Location = new System.Drawing.Point(0, 162);
            this.vertexText.Multiline = true;
            this.vertexText.Name = "vertexText";
            this.vertexText.Size = new System.Drawing.Size(398, 1023);
            this.vertexText.TabIndex = 0;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1755, 1185);
            this.Controls.Add(this.splitContainer1);
            this.Name = "Form1";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.zoomSlider)).EndInit();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.vertexIdSelector)).EndInit();
            this.storagePanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TrackBar zoomSlider;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Panel storagePanel;
        private System.Windows.Forms.Button debugButton;
        private System.Windows.Forms.Button s4;
        private System.Windows.Forms.Button s3;
        private System.Windows.Forms.Button s2;
        private System.Windows.Forms.Button s1;
        private System.Windows.Forms.TextBox vertexText;
        private PolygonDrawControl polygonPanel;
        private System.Windows.Forms.Label vertexIdLabel;
        private System.Windows.Forms.TrackBar vertexIdSelector;
        private System.Windows.Forms.Label polygonOrderLabel;
        private System.Windows.Forms.Button splitButton;
        private System.Windows.Forms.Button s6;
        private System.Windows.Forms.Button s5;
        private System.Windows.Forms.Button triangulateButton;
    }
}

