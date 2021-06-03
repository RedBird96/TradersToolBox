namespace TTWinForms
{
    partial class Graph
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
            this.label2 = new System.Windows.Forms.Label();
            this.labelResults = new System.Windows.Forms.Label();
            this.labelResults2 = new System.Windows.Forms.Label();
            this.labelResults3 = new System.Windows.Forms.Label();
            this.checkBoxReal = new System.Windows.Forms.CheckBox();
            this.checkBoxRandom = new System.Windows.Forms.CheckBox();
            this.checkBoxOriginal = new System.Windows.Forms.CheckBox();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.checkBoxBaH = new System.Windows.Forms.CheckBox();
            this.checkBoxDD = new System.Windows.Forms.CheckBox();
            this.checkBoxBasket = new System.Windows.Forms.CheckBox();
            this.panelBottom = new System.Windows.Forms.Panel();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.zedGraphControl1 = new ZedGraph.ZedGraphControl();
            this.flowLayoutPanel1.SuspendLayout();
            this.panelBottom.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label2.Location = new System.Drawing.Point(3, 2);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(73, 26);
            this.label2.TabIndex = 3;
            this.label2.Text = "Results of\r\nSimulation:";
            // 
            // labelResults
            // 
            this.labelResults.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelResults.AutoSize = true;
            this.labelResults.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.labelResults.Location = new System.Drawing.Point(82, 2);
            this.labelResults.Name = "labelResults";
            this.labelResults.Size = new System.Drawing.Size(85, 91);
            this.labelResults.TabIndex = 4;
            this.labelResults.Text = "labelResults1\r\nnewLine\r\nnewLine\r\nnewLine\r\nnewLine\r\nnewLine\r\nnewLine";
            // 
            // labelResults2
            // 
            this.labelResults2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelResults2.AutoSize = true;
            this.labelResults2.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.labelResults2.Location = new System.Drawing.Point(304, 2);
            this.labelResults2.Name = "labelResults2";
            this.labelResults2.Size = new System.Drawing.Size(85, 26);
            this.labelResults2.TabIndex = 5;
            this.labelResults2.Text = "labelResults2\r\nnewLine";
            // 
            // labelResults3
            // 
            this.labelResults3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelResults3.AutoSize = true;
            this.labelResults3.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.labelResults3.Location = new System.Drawing.Point(459, 2);
            this.labelResults3.Name = "labelResults3";
            this.labelResults3.Size = new System.Drawing.Size(85, 26);
            this.labelResults3.TabIndex = 6;
            this.labelResults3.Text = "labelResults3\r\nnewLine";
            // 
            // checkBoxReal
            // 
            this.checkBoxReal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxReal.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxReal.AutoSize = true;
            this.checkBoxReal.FlatAppearance.CheckedBackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.checkBoxReal.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.checkBoxReal.Location = new System.Drawing.Point(601, 3);
            this.checkBoxReal.Name = "checkBoxReal";
            this.checkBoxReal.Size = new System.Drawing.Size(42, 23);
            this.checkBoxReal.TabIndex = 7;
            this.checkBoxReal.Text = "REAL";
            this.checkBoxReal.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.checkBoxReal.UseVisualStyleBackColor = true;
            this.checkBoxReal.CheckedChanged += new System.EventHandler(this.CheckBoxReal_CheckedChanged);
            // 
            // checkBoxRandom
            // 
            this.checkBoxRandom.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxRandom.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxRandom.AutoSize = true;
            this.checkBoxRandom.FlatAppearance.CheckedBackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.checkBoxRandom.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.checkBoxRandom.Location = new System.Drawing.Point(649, 3);
            this.checkBoxRandom.Name = "checkBoxRandom";
            this.checkBoxRandom.Size = new System.Drawing.Size(66, 23);
            this.checkBoxRandom.TabIndex = 8;
            this.checkBoxRandom.Text = "RANDOM";
            this.checkBoxRandom.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.checkBoxRandom.UseVisualStyleBackColor = true;
            this.checkBoxRandom.CheckedChanged += new System.EventHandler(this.CheckBoxRandom_CheckedChanged);
            // 
            // checkBoxOriginal
            // 
            this.checkBoxOriginal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxOriginal.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxOriginal.AutoSize = true;
            this.checkBoxOriginal.FlatAppearance.CheckedBackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.checkBoxOriginal.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.checkBoxOriginal.Location = new System.Drawing.Point(536, 3);
            this.checkBoxOriginal.Name = "checkBoxOriginal";
            this.checkBoxOriginal.Size = new System.Drawing.Size(59, 23);
            this.checkBoxOriginal.TabIndex = 7;
            this.checkBoxOriginal.Text = "Original";
            this.checkBoxOriginal.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.checkBoxOriginal.UseVisualStyleBackColor = true;
            this.checkBoxOriginal.Visible = false;
            this.checkBoxOriginal.CheckedChanged += new System.EventHandler(this.CheckBoxOriginal_CheckedChanged);
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.checkBoxRandom);
            this.flowLayoutPanel1.Controls.Add(this.checkBoxReal);
            this.flowLayoutPanel1.Controls.Add(this.checkBoxOriginal);
            this.flowLayoutPanel1.Controls.Add(this.checkBoxBaH);
            this.flowLayoutPanel1.Controls.Add(this.checkBoxDD);
            this.flowLayoutPanel1.Controls.Add(this.checkBoxBasket);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(3, 3);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Padding = new System.Windows.Forms.Padding(0, 0, 10, 0);
            this.flowLayoutPanel1.Size = new System.Drawing.Size(728, 29);
            this.flowLayoutPanel1.TabIndex = 9;
            // 
            // checkBoxBaH
            // 
            this.checkBoxBaH.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxBaH.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxBaH.AutoSize = true;
            this.checkBoxBaH.FlatAppearance.CheckedBackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.checkBoxBaH.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.checkBoxBaH.Location = new System.Drawing.Point(455, 3);
            this.checkBoxBaH.Name = "checkBoxBaH";
            this.checkBoxBaH.Size = new System.Drawing.Size(75, 23);
            this.checkBoxBaH.TabIndex = 9;
            this.checkBoxBaH.Text = "Buy && Hold";
            this.checkBoxBaH.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.checkBoxBaH.UseVisualStyleBackColor = true;
            this.checkBoxBaH.Visible = false;
            this.checkBoxBaH.CheckedChanged += new System.EventHandler(this.CheckBoxBaH_CheckedChanged);
            // 
            // checkBoxDD
            // 
            this.checkBoxDD.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxDD.AutoSize = true;
            this.checkBoxDD.FlatAppearance.CheckedBackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.checkBoxDD.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.checkBoxDD.Location = new System.Drawing.Point(375, 3);
            this.checkBoxDD.Name = "checkBoxDD";
            this.checkBoxDD.Size = new System.Drawing.Size(74, 23);
            this.checkBoxDD.TabIndex = 10;
            this.checkBoxDD.Text = "Drawdown";
            this.checkBoxDD.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.checkBoxDD.UseVisualStyleBackColor = true;
            this.checkBoxDD.Visible = false;
            this.checkBoxDD.CheckedChanged += new System.EventHandler(this.CheckBoxDD_CheckedChanged);
            // 
            // checkBoxBasket
            // 
            this.checkBoxBasket.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxBasket.AutoSize = true;
            this.checkBoxBasket.FlatAppearance.CheckedBackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.checkBoxBasket.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.checkBoxBasket.Location = new System.Drawing.Point(319, 3);
            this.checkBoxBasket.Name = "checkBoxBasket";
            this.checkBoxBasket.Size = new System.Drawing.Size(50, 23);
            this.checkBoxBasket.TabIndex = 9;
            this.checkBoxBasket.Text = "Basket";
            this.checkBoxBasket.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.checkBoxBasket.UseVisualStyleBackColor = true;
            this.checkBoxBasket.Visible = false;
            this.checkBoxBasket.CheckedChanged += new System.EventHandler(this.CheckBoxBasket_CheckedChanged);
            // 
            // panelBottom
            // 
            this.panelBottom.AutoSize = true;
            this.panelBottom.Controls.Add(this.label2);
            this.panelBottom.Controls.Add(this.labelResults);
            this.panelBottom.Controls.Add(this.labelResults2);
            this.panelBottom.Controls.Add(this.labelResults3);
            this.panelBottom.Location = new System.Drawing.Point(3, 562);
            this.panelBottom.Name = "panelBottom";
            this.panelBottom.Size = new System.Drawing.Size(547, 96);
            this.panelBottom.TabIndex = 10;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.panelBottom, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.splitContainer1, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(734, 661);
            this.tableLayoutPanel1.TabIndex = 11;
            // 
            // splitContainer1
            // 
            this.splitContainer1.BackColor = System.Drawing.SystemColors.ControlLight;
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(3, 38);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.zedGraphControl1);
            this.splitContainer1.Panel2Collapsed = true;
            this.splitContainer1.Size = new System.Drawing.Size(728, 518);
            this.splitContainer1.SplitterDistance = 400;
            this.splitContainer1.TabIndex = 11;
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
            this.zedGraphControl1.Size = new System.Drawing.Size(728, 518);
            this.zedGraphControl1.TabIndex = 1;
            // 
            // Graph
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.Controls.Add(this.tableLayoutPanel1);
            this.DoubleBuffered = true;
            this.Name = "Graph";
            this.Size = new System.Drawing.Size(734, 661);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.panelBottom.ResumeLayout(false);
            this.panelBottom.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label labelResults;
        private System.Windows.Forms.Label labelResults2;
        private System.Windows.Forms.Label labelResults3;
        private System.Windows.Forms.CheckBox checkBoxReal;
        private System.Windows.Forms.CheckBox checkBoxRandom;
        private System.Windows.Forms.CheckBox checkBoxOriginal;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.CheckBox checkBoxBaH;
        private System.Windows.Forms.CheckBox checkBoxDD;
        private System.Windows.Forms.CheckBox checkBoxBasket;
        private System.Windows.Forms.Panel panelBottom;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private ZedGraph.ZedGraphControl zedGraphControl1;
    }
}