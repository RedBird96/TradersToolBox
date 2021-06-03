namespace TTWinForms
{
    partial class GraphMC
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.zedGraphControl1 = new ZedGraph.ZedGraphControl();
            this.labelResults = new System.Windows.Forms.Label();
            this.labelResults2 = new System.Windows.Forms.Label();
            this.checkBoxRandomized = new System.Windows.Forms.CheckBox();
            this.checkBox1_10 = new System.Windows.Forms.CheckBox();
            this.checkBoxOriginal = new System.Windows.Forms.CheckBox();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.checkBoxResample = new System.Windows.Forms.CheckBox();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.zedGraphControl2 = new ZedGraph.ZedGraphControl();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.tableLayoutMain = new System.Windows.Forms.TableLayoutPanel();
            this.flowLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutMain.SuspendLayout();
            this.SuspendLayout();
            // 
            // zedGraphControl1
            // 
            this.zedGraphControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.zedGraphControl1.Location = new System.Drawing.Point(0, 0);
            this.zedGraphControl1.Name = "zedGraphControl1";
            this.zedGraphControl1.ScrollGrace = 0D;
            this.zedGraphControl1.ScrollMaxX = 0D;
            this.zedGraphControl1.ScrollMaxY = 0D;
            this.zedGraphControl1.ScrollMaxY2 = 0D;
            this.zedGraphControl1.ScrollMinX = 0D;
            this.zedGraphControl1.ScrollMinY = 0D;
            this.zedGraphControl1.ScrollMinY2 = 0D;
            this.zedGraphControl1.Size = new System.Drawing.Size(728, 273);
            this.zedGraphControl1.TabIndex = 2;
            // 
            // labelResults
            // 
            this.labelResults.AutoSize = true;
            this.labelResults.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.labelResults.Location = new System.Drawing.Point(3, 0);
            this.labelResults.Name = "labelResults";
            this.labelResults.Size = new System.Drawing.Size(85, 52);
            this.labelResults.TabIndex = 4;
            this.labelResults.Text = "labelResults1\r\nnewLine\r\nnewLine\r\nnewLine";
            // 
            // labelResults2
            // 
            this.labelResults2.AutoSize = true;
            this.labelResults2.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.labelResults2.Location = new System.Drawing.Point(94, 0);
            this.labelResults2.Name = "labelResults2";
            this.labelResults2.Size = new System.Drawing.Size(85, 26);
            this.labelResults2.TabIndex = 5;
            this.labelResults2.Text = "labelResults2\r\nnewLine";
            // 
            // checkBoxRandomized
            // 
            this.checkBoxRandomized.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxRandomized.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxRandomized.FlatAppearance.CheckedBackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.checkBoxRandomized.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.checkBoxRandomized.Location = new System.Drawing.Point(549, 3);
            this.checkBoxRandomized.Name = "checkBoxRandomized";
            this.checkBoxRandomized.Size = new System.Drawing.Size(80, 23);
            this.checkBoxRandomized.TabIndex = 3;
            this.checkBoxRandomized.Text = "Randomized";
            this.checkBoxRandomized.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.checkBoxRandomized.UseVisualStyleBackColor = true;
            this.checkBoxRandomized.CheckedChanged += new System.EventHandler(this.CheckBoxRandomized_CheckedChanged);
            // 
            // checkBox1_10
            // 
            this.checkBox1_10.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBox1_10.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBox1_10.FlatAppearance.CheckedBackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.checkBox1_10.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.checkBox1_10.Location = new System.Drawing.Point(635, 3);
            this.checkBox1_10.Name = "checkBox1_10";
            this.checkBox1_10.Size = new System.Drawing.Size(80, 23);
            this.checkBox1_10.TabIndex = 4;
            this.checkBox1_10.Text = "1-10";
            this.checkBox1_10.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.checkBox1_10.UseVisualStyleBackColor = true;
            this.checkBox1_10.CheckedChanged += new System.EventHandler(this.CheckBox1_10_CheckedChanged);
            // 
            // checkBoxOriginal
            // 
            this.checkBoxOriginal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxOriginal.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxOriginal.FlatAppearance.CheckedBackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.checkBoxOriginal.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.checkBoxOriginal.Location = new System.Drawing.Point(377, 3);
            this.checkBoxOriginal.Name = "checkBoxOriginal";
            this.checkBoxOriginal.Size = new System.Drawing.Size(80, 23);
            this.checkBoxOriginal.TabIndex = 1;
            this.checkBoxOriginal.Text = "Original";
            this.checkBoxOriginal.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.checkBoxOriginal.UseVisualStyleBackColor = true;
            this.checkBoxOriginal.CheckedChanged += new System.EventHandler(this.CheckBoxOriginal_CheckedChanged);
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPanel1.Controls.Add(this.checkBox1_10);
            this.flowLayoutPanel1.Controls.Add(this.checkBoxRandomized);
            this.flowLayoutPanel1.Controls.Add(this.checkBoxResample);
            this.flowLayoutPanel1.Controls.Add(this.checkBoxOriginal);
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(3, 3);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Padding = new System.Windows.Forms.Padding(0, 0, 10, 0);
            this.flowLayoutPanel1.Size = new System.Drawing.Size(728, 29);
            this.flowLayoutPanel1.TabIndex = 1;
            // 
            // checkBoxResample
            // 
            this.checkBoxResample.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxResample.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxResample.FlatAppearance.CheckedBackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.checkBoxResample.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.checkBoxResample.Location = new System.Drawing.Point(463, 3);
            this.checkBoxResample.Name = "checkBoxResample";
            this.checkBoxResample.Size = new System.Drawing.Size(80, 23);
            this.checkBoxResample.TabIndex = 2;
            this.checkBoxResample.Text = "Resample";
            this.checkBoxResample.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.checkBoxResample.UseVisualStyleBackColor = true;
            this.checkBoxResample.CheckedChanged += new System.EventHandler(this.CheckBoxResample_CheckedChanged);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.BackColor = System.Drawing.SystemColors.ControlLight;
            this.splitContainer1.Location = new System.Drawing.Point(3, 38);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.zedGraphControl1);
            this.splitContainer1.Panel1MinSize = 70;
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.zedGraphControl2);
            this.splitContainer1.Panel2MinSize = 70;
            this.splitContainer1.Size = new System.Drawing.Size(728, 562);
            this.splitContainer1.SplitterDistance = 273;
            this.splitContainer1.TabIndex = 6;
            // 
            // zedGraphControl2
            // 
            this.zedGraphControl2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.zedGraphControl2.Location = new System.Drawing.Point(0, 0);
            this.zedGraphControl2.Name = "zedGraphControl2";
            this.zedGraphControl2.ScrollGrace = 0D;
            this.zedGraphControl2.ScrollMaxX = 0D;
            this.zedGraphControl2.ScrollMaxY = 0D;
            this.zedGraphControl2.ScrollMaxY2 = 0D;
            this.zedGraphControl2.ScrollMinX = 0D;
            this.zedGraphControl2.ScrollMinY = 0D;
            this.zedGraphControl2.ScrollMinY2 = 0D;
            this.zedGraphControl2.Size = new System.Drawing.Size(728, 285);
            this.zedGraphControl2.TabIndex = 3;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.AutoSize = true;
            this.tableLayoutPanel1.ColumnCount = 4;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Controls.Add(this.label3, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.label4, 3, 0);
            this.tableLayoutPanel1.Controls.Add(this.labelResults, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.labelResults2, 1, 0);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 606);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(728, 52);
            this.tableLayoutPanel1.TabIndex = 7;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label3.Location = new System.Drawing.Point(185, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(85, 39);
            this.label3.TabIndex = 7;
            this.label3.Text = "labelResults3\r\nnewLine\r\nnewLine";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label4.Location = new System.Drawing.Point(276, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(85, 39);
            this.label4.TabIndex = 6;
            this.label4.Text = "labelResults4\r\nnewLine\r\nnewLine";
            // 
            // tableLayoutMain
            // 
            this.tableLayoutMain.ColumnCount = 1;
            this.tableLayoutMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutMain.Controls.Add(this.splitContainer1, 0, 1);
            this.tableLayoutMain.Controls.Add(this.flowLayoutPanel1, 0, 0);
            this.tableLayoutMain.Controls.Add(this.tableLayoutPanel1, 0, 2);
            this.tableLayoutMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutMain.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutMain.Name = "tableLayoutMain";
            this.tableLayoutMain.RowCount = 3;
            this.tableLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutMain.Size = new System.Drawing.Size(734, 661);
            this.tableLayoutMain.TabIndex = 3;
            // 
            // GraphMC
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.Controls.Add(this.tableLayoutMain);
            this.DoubleBuffered = true;
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.Name = "GraphMC";
            this.Size = new System.Drawing.Size(734, 661);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.tableLayoutMain.ResumeLayout(false);
            this.tableLayoutMain.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private ZedGraph.ZedGraphControl zedGraphControl1;
        private System.Windows.Forms.Label labelResults;
        private System.Windows.Forms.Label labelResults2;
        private System.Windows.Forms.CheckBox checkBoxRandomized;
        private System.Windows.Forms.CheckBox checkBox1_10;
        private System.Windows.Forms.CheckBox checkBoxOriginal;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.CheckBox checkBoxResample;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private ZedGraph.ZedGraphControl zedGraphControl2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TableLayoutPanel tableLayoutMain;
    }
}