namespace TTWinForms
{
    partial class GraphRobustness
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
            this.checkBoxRandom = new System.Windows.Forms.CheckBox();
            this.checkBoxLiquidity = new System.Windows.Forms.CheckBox();
            this.checkBoxSlippage = new System.Windows.Forms.CheckBox();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.checkBoxEntries = new System.Windows.Forms.CheckBox();
            this.panelMeta = new System.Windows.Forms.Panel();
            this.checkBoxMeta = new System.Windows.Forms.CheckBox();
            this.comboBoxMeta = new System.Windows.Forms.ComboBox();
            this.panelSeasonality = new System.Windows.Forms.Panel();
            this.checkBoxSeasonality = new System.Windows.Forms.CheckBox();
            this.comboBoxSeasonality = new System.Windows.Forms.ComboBox();
            this.panelNews = new System.Windows.Forms.Panel();
            this.comboBoxNews = new System.Windows.Forms.ComboBox();
            this.checkBoxNews = new System.Windows.Forms.CheckBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.zedGraphControl3 = new ZedGraph.ZedGraphControl();
            this.zedGraphControl2 = new ZedGraph.ZedGraphControl();
            this.flowLayoutPanel1.SuspendLayout();
            this.panelMeta.SuspendLayout();
            this.panelSeasonality.SuspendLayout();
            this.panelNews.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // zedGraphControl1
            // 
            this.zedGraphControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.zedGraphControl1.Location = new System.Drawing.Point(0, 64);
            this.zedGraphControl1.Margin = new System.Windows.Forms.Padding(0);
            this.zedGraphControl1.Name = "zedGraphControl1";
            this.zedGraphControl1.ScrollGrace = 0D;
            this.zedGraphControl1.ScrollMaxX = 0D;
            this.zedGraphControl1.ScrollMaxY = 0D;
            this.zedGraphControl1.ScrollMaxY2 = 0D;
            this.zedGraphControl1.ScrollMinX = 0D;
            this.zedGraphControl1.ScrollMinY = 0D;
            this.zedGraphControl1.ScrollMinY2 = 0D;
            this.zedGraphControl1.Size = new System.Drawing.Size(270, 523);
            this.zedGraphControl1.TabIndex = 0;
            // 
            // checkBoxRandom
            // 
            this.checkBoxRandom.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxRandom.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxRandom.FlatAppearance.CheckedBackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.checkBoxRandom.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.checkBoxRandom.Location = new System.Drawing.Point(674, 3);
            this.checkBoxRandom.Name = "checkBoxRandom";
            this.checkBoxRandom.Size = new System.Drawing.Size(111, 22);
            this.checkBoxRandom.TabIndex = 7;
            this.checkBoxRandom.Text = "Randomized OOS";
            this.checkBoxRandom.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.checkBoxRandom.UseVisualStyleBackColor = true;
            this.checkBoxRandom.CheckedChanged += new System.EventHandler(this.CheckBoxRandom_CheckedChanged);
            // 
            // checkBoxLiquidity
            // 
            this.checkBoxLiquidity.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxLiquidity.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxLiquidity.FlatAppearance.CheckedBackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.checkBoxLiquidity.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.checkBoxLiquidity.Location = new System.Drawing.Point(557, 3);
            this.checkBoxLiquidity.Name = "checkBoxLiquidity";
            this.checkBoxLiquidity.Size = new System.Drawing.Size(111, 22);
            this.checkBoxLiquidity.TabIndex = 8;
            this.checkBoxLiquidity.Text = "Liquidity Test";
            this.checkBoxLiquidity.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.checkBoxLiquidity.UseVisualStyleBackColor = true;
            this.checkBoxLiquidity.CheckedChanged += new System.EventHandler(this.CheckBoxLiquidity_CheckedChanged);
            // 
            // checkBoxSlippage
            // 
            this.checkBoxSlippage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxSlippage.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxSlippage.FlatAppearance.CheckedBackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.checkBoxSlippage.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.checkBoxSlippage.Location = new System.Drawing.Point(440, 3);
            this.checkBoxSlippage.Name = "checkBoxSlippage";
            this.checkBoxSlippage.Size = new System.Drawing.Size(111, 22);
            this.checkBoxSlippage.TabIndex = 7;
            this.checkBoxSlippage.Text = "Slippage Test";
            this.checkBoxSlippage.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.checkBoxSlippage.UseVisualStyleBackColor = true;
            this.checkBoxSlippage.CheckedChanged += new System.EventHandler(this.CheckBoxSlippage_CheckedChanged);
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.AutoSize = true;
            this.flowLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanel1.SetColumnSpan(this.flowLayoutPanel1, 3);
            this.flowLayoutPanel1.Controls.Add(this.checkBoxRandom);
            this.flowLayoutPanel1.Controls.Add(this.checkBoxLiquidity);
            this.flowLayoutPanel1.Controls.Add(this.checkBoxSlippage);
            this.flowLayoutPanel1.Controls.Add(this.checkBoxEntries);
            this.flowLayoutPanel1.Controls.Add(this.panelMeta);
            this.flowLayoutPanel1.Controls.Add(this.panelSeasonality);
            this.flowLayoutPanel1.Controls.Add(this.panelNews);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(12, 3);
            this.flowLayoutPanel1.Margin = new System.Windows.Forms.Padding(12, 3, 12, 3);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(788, 58);
            this.flowLayoutPanel1.TabIndex = 9;
            // 
            // checkBoxEntries
            // 
            this.checkBoxEntries.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxEntries.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxEntries.FlatAppearance.CheckedBackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.checkBoxEntries.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.checkBoxEntries.Location = new System.Drawing.Point(323, 3);
            this.checkBoxEntries.Name = "checkBoxEntries";
            this.checkBoxEntries.Size = new System.Drawing.Size(111, 22);
            this.checkBoxEntries.TabIndex = 10;
            this.checkBoxEntries.Text = "Delayed Test";
            this.checkBoxEntries.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.checkBoxEntries.UseVisualStyleBackColor = true;
            this.checkBoxEntries.CheckedChanged += new System.EventHandler(this.CheckBoxEntries_CheckedChanged);
            // 
            // panelMeta
            // 
            this.panelMeta.Controls.Add(this.checkBoxMeta);
            this.panelMeta.Controls.Add(this.comboBoxMeta);
            this.panelMeta.Location = new System.Drawing.Point(89, 3);
            this.panelMeta.Name = "panelMeta";
            this.panelMeta.Size = new System.Drawing.Size(228, 23);
            this.panelMeta.TabIndex = 12;
            // 
            // checkBoxMeta
            // 
            this.checkBoxMeta.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxMeta.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxMeta.FlatAppearance.CheckedBackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.checkBoxMeta.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.checkBoxMeta.Location = new System.Drawing.Point(0, 0);
            this.checkBoxMeta.Name = "checkBoxMeta";
            this.checkBoxMeta.Size = new System.Drawing.Size(111, 22);
            this.checkBoxMeta.TabIndex = 9;
            this.checkBoxMeta.Text = "Meta Systems";
            this.checkBoxMeta.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.checkBoxMeta.UseVisualStyleBackColor = false;
            this.checkBoxMeta.CheckedChanged += new System.EventHandler(this.CheckBoxMeta_CheckedChanged);
            // 
            // comboBoxMeta
            // 
            this.comboBoxMeta.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxMeta.FormattingEnabled = true;
            this.comboBoxMeta.Items.AddRange(new object[] {
            "Two",
            "Three",
            "Average"});
            this.comboBoxMeta.Location = new System.Drawing.Point(117, 1);
            this.comboBoxMeta.Name = "comboBoxMeta";
            this.comboBoxMeta.Size = new System.Drawing.Size(110, 21);
            this.comboBoxMeta.TabIndex = 10;
            this.comboBoxMeta.SelectedIndexChanged += new System.EventHandler(this.ComboBoxMeta_SelectedIndexChanged);
            // 
            // panelSeasonality
            // 
            this.panelSeasonality.Controls.Add(this.checkBoxSeasonality);
            this.panelSeasonality.Controls.Add(this.comboBoxSeasonality);
            this.panelSeasonality.Location = new System.Drawing.Point(557, 32);
            this.panelSeasonality.Name = "panelSeasonality";
            this.panelSeasonality.Size = new System.Drawing.Size(228, 23);
            this.panelSeasonality.TabIndex = 13;
            // 
            // checkBoxSeasonality
            // 
            this.checkBoxSeasonality.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxSeasonality.FlatAppearance.CheckedBackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.checkBoxSeasonality.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.checkBoxSeasonality.Location = new System.Drawing.Point(0, 0);
            this.checkBoxSeasonality.Name = "checkBoxSeasonality";
            this.checkBoxSeasonality.Size = new System.Drawing.Size(111, 22);
            this.checkBoxSeasonality.TabIndex = 9;
            this.checkBoxSeasonality.Text = "Seasonality";
            this.checkBoxSeasonality.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.checkBoxSeasonality.UseVisualStyleBackColor = false;
            this.checkBoxSeasonality.CheckedChanged += new System.EventHandler(this.CheckBoxSeasonality_CheckedChanged);
            // 
            // comboBoxSeasonality
            // 
            this.comboBoxSeasonality.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxSeasonality.FormattingEnabled = true;
            this.comboBoxSeasonality.Items.AddRange(new object[] {
            "Odd / Even Days",
            "Day of week",
            "Week number",
            "Month",
            "Quarter",
            "Year"});
            this.comboBoxSeasonality.Location = new System.Drawing.Point(117, 1);
            this.comboBoxSeasonality.Name = "comboBoxSeasonality";
            this.comboBoxSeasonality.Size = new System.Drawing.Size(110, 21);
            this.comboBoxSeasonality.TabIndex = 10;
            this.comboBoxSeasonality.SelectedIndexChanged += new System.EventHandler(this.ComboBoxSeasonality_SelectedIndexChanged);
            // 
            // panelNews
            // 
            this.panelNews.Controls.Add(this.comboBoxNews);
            this.panelNews.Controls.Add(this.checkBoxNews);
            this.panelNews.Location = new System.Drawing.Point(323, 32);
            this.panelNews.Name = "panelNews";
            this.panelNews.Size = new System.Drawing.Size(228, 23);
            this.panelNews.TabIndex = 7;
            // 
            // comboBoxNews
            // 
            this.comboBoxNews.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxNews.FormattingEnabled = true;
            this.comboBoxNews.Location = new System.Drawing.Point(117, 1);
            this.comboBoxNews.Name = "comboBoxNews";
            this.comboBoxNews.Size = new System.Drawing.Size(110, 21);
            this.comboBoxNews.TabIndex = 10;
            this.comboBoxNews.SelectedIndexChanged += new System.EventHandler(this.ComboBoxNews_SelectedIndexChanged);
            // 
            // checkBoxNews
            // 
            this.checkBoxNews.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxNews.FlatAppearance.CheckedBackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.checkBoxNews.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.checkBoxNews.Location = new System.Drawing.Point(0, 0);
            this.checkBoxNews.Name = "checkBoxNews";
            this.checkBoxNews.Size = new System.Drawing.Size(111, 22);
            this.checkBoxNews.TabIndex = 7;
            this.checkBoxNews.Text = "News";
            this.checkBoxNews.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.checkBoxNews.UseVisualStyleBackColor = true;
            this.checkBoxNews.CheckedChanged += new System.EventHandler(this.CheckBoxNews_CheckedChanged);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 3;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel1.Controls.Add(this.zedGraphControl3, 2, 1);
            this.tableLayoutPanel1.Controls.Add(this.zedGraphControl2, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.zedGraphControl1, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(812, 587);
            this.tableLayoutPanel1.TabIndex = 11;
            // 
            // zedGraphControl3
            // 
            this.zedGraphControl3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.zedGraphControl3.Location = new System.Drawing.Point(540, 64);
            this.zedGraphControl3.Margin = new System.Windows.Forms.Padding(0);
            this.zedGraphControl3.Name = "zedGraphControl3";
            this.zedGraphControl3.ScrollGrace = 0D;
            this.zedGraphControl3.ScrollMaxX = 0D;
            this.zedGraphControl3.ScrollMaxY = 0D;
            this.zedGraphControl3.ScrollMaxY2 = 0D;
            this.zedGraphControl3.ScrollMinX = 0D;
            this.zedGraphControl3.ScrollMinY = 0D;
            this.zedGraphControl3.ScrollMinY2 = 0D;
            this.zedGraphControl3.Size = new System.Drawing.Size(272, 523);
            this.zedGraphControl3.TabIndex = 11;
            // 
            // zedGraphControl2
            // 
            this.zedGraphControl2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.zedGraphControl2.Location = new System.Drawing.Point(270, 64);
            this.zedGraphControl2.Margin = new System.Windows.Forms.Padding(0);
            this.zedGraphControl2.Name = "zedGraphControl2";
            this.zedGraphControl2.ScrollGrace = 0D;
            this.zedGraphControl2.ScrollMaxX = 0D;
            this.zedGraphControl2.ScrollMaxY = 0D;
            this.zedGraphControl2.ScrollMaxY2 = 0D;
            this.zedGraphControl2.ScrollMinX = 0D;
            this.zedGraphControl2.ScrollMinY = 0D;
            this.zedGraphControl2.ScrollMinY2 = 0D;
            this.zedGraphControl2.Size = new System.Drawing.Size(270, 523);
            this.zedGraphControl2.TabIndex = 10;
            // 
            // GraphRobustness
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.Controls.Add(this.tableLayoutPanel1);
            this.DoubleBuffered = true;
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.Name = "GraphRobustness";
            this.Size = new System.Drawing.Size(812, 587);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.panelMeta.ResumeLayout(false);
            this.panelSeasonality.ResumeLayout(false);
            this.panelNews.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private ZedGraph.ZedGraphControl zedGraphControl1;
        private System.Windows.Forms.CheckBox checkBoxRandom;
        private System.Windows.Forms.CheckBox checkBoxLiquidity;
        private System.Windows.Forms.CheckBox checkBoxSlippage;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.CheckBox checkBoxMeta;
        private System.Windows.Forms.CheckBox checkBoxEntries;
        private System.Windows.Forms.Panel panelMeta;
        private System.Windows.Forms.ComboBox comboBoxMeta;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private ZedGraph.ZedGraphControl zedGraphControl3;
        private ZedGraph.ZedGraphControl zedGraphControl2;
        private System.Windows.Forms.Panel panelSeasonality;
        private System.Windows.Forms.CheckBox checkBoxSeasonality;
        private System.Windows.Forms.ComboBox comboBoxSeasonality;
        private System.Windows.Forms.CheckBox checkBoxNews;
        private System.Windows.Forms.Panel panelNews;
        private System.Windows.Forms.ComboBox comboBoxNews;
    }
}