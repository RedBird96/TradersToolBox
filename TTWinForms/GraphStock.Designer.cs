namespace TTWinForms
{
    partial class GraphStock
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
            this.rbM5 = new System.Windows.Forms.RadioButton();
            this.rbM30 = new System.Windows.Forms.RadioButton();
            this.rbH1 = new System.Windows.Forms.RadioButton();
            this.rbH4 = new System.Windows.Forms.RadioButton();
            this.rbD1 = new System.Windows.Forms.RadioButton();
            this.rbW1 = new System.Windows.Forms.RadioButton();
            this.rbMN = new System.Windows.Forms.RadioButton();
            this.panel1 = new System.Windows.Forms.Panel();
            this.zedGraphControl2 = new ZedGraph.ZedGraphControl();
            this.zedGraphControl1 = new ZedGraph.ZedGraphControl();
            this.cbShowTrades = new System.Windows.Forms.CheckBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.cbShowCursorValues = new System.Windows.Forms.CheckBox();
            this.cbCrosshair = new System.Windows.Forms.CheckBox();
            this.cbVolumes = new System.Windows.Forms.CheckBox();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // rbM5
            // 
            this.rbM5.Appearance = System.Windows.Forms.Appearance.Button;
            this.rbM5.Checked = true;
            this.rbM5.Location = new System.Drawing.Point(3, 3);
            this.rbM5.Name = "rbM5";
            this.rbM5.Size = new System.Drawing.Size(37, 23);
            this.rbM5.TabIndex = 2;
            this.rbM5.TabStop = true;
            this.rbM5.Text = "M5";
            this.rbM5.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.rbM5.UseVisualStyleBackColor = true;
            this.rbM5.CheckedChanged += new System.EventHandler(this.RbTimeframe_CheckedChanged);
            // 
            // rbM30
            // 
            this.rbM30.Appearance = System.Windows.Forms.Appearance.Button;
            this.rbM30.Location = new System.Drawing.Point(42, 3);
            this.rbM30.Name = "rbM30";
            this.rbM30.Size = new System.Drawing.Size(37, 23);
            this.rbM30.TabIndex = 3;
            this.rbM30.Text = "M30";
            this.rbM30.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.rbM30.UseVisualStyleBackColor = true;
            this.rbM30.CheckedChanged += new System.EventHandler(this.RbTimeframe_CheckedChanged);
            // 
            // rbH1
            // 
            this.rbH1.Appearance = System.Windows.Forms.Appearance.Button;
            this.rbH1.Location = new System.Drawing.Point(81, 3);
            this.rbH1.Name = "rbH1";
            this.rbH1.Size = new System.Drawing.Size(37, 23);
            this.rbH1.TabIndex = 4;
            this.rbH1.Text = "H1";
            this.rbH1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.rbH1.UseVisualStyleBackColor = true;
            this.rbH1.CheckedChanged += new System.EventHandler(this.RbTimeframe_CheckedChanged);
            // 
            // rbH4
            // 
            this.rbH4.Appearance = System.Windows.Forms.Appearance.Button;
            this.rbH4.Location = new System.Drawing.Point(120, 3);
            this.rbH4.Name = "rbH4";
            this.rbH4.Size = new System.Drawing.Size(37, 23);
            this.rbH4.TabIndex = 5;
            this.rbH4.Text = "H4";
            this.rbH4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.rbH4.UseVisualStyleBackColor = true;
            this.rbH4.CheckedChanged += new System.EventHandler(this.RbTimeframe_CheckedChanged);
            // 
            // rbD1
            // 
            this.rbD1.Appearance = System.Windows.Forms.Appearance.Button;
            this.rbD1.Location = new System.Drawing.Point(159, 3);
            this.rbD1.Name = "rbD1";
            this.rbD1.Size = new System.Drawing.Size(37, 23);
            this.rbD1.TabIndex = 6;
            this.rbD1.Text = "D1";
            this.rbD1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.rbD1.UseVisualStyleBackColor = true;
            this.rbD1.CheckedChanged += new System.EventHandler(this.RbTimeframe_CheckedChanged);
            // 
            // rbW1
            // 
            this.rbW1.Appearance = System.Windows.Forms.Appearance.Button;
            this.rbW1.Location = new System.Drawing.Point(198, 3);
            this.rbW1.Name = "rbW1";
            this.rbW1.Size = new System.Drawing.Size(37, 23);
            this.rbW1.TabIndex = 7;
            this.rbW1.Text = "W1";
            this.rbW1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.rbW1.UseVisualStyleBackColor = true;
            this.rbW1.CheckedChanged += new System.EventHandler(this.RbTimeframe_CheckedChanged);
            // 
            // rbMN
            // 
            this.rbMN.Appearance = System.Windows.Forms.Appearance.Button;
            this.rbMN.Location = new System.Drawing.Point(237, 3);
            this.rbMN.Name = "rbMN";
            this.rbMN.Size = new System.Drawing.Size(37, 23);
            this.rbMN.TabIndex = 8;
            this.rbMN.Text = "MN";
            this.rbMN.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.rbMN.UseVisualStyleBackColor = true;
            this.rbMN.CheckedChanged += new System.EventHandler(this.RbTimeframe_CheckedChanged);
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.Controls.Add(this.rbM5);
            this.panel1.Controls.Add(this.rbMN);
            this.panel1.Controls.Add(this.rbM30);
            this.panel1.Controls.Add(this.rbW1);
            this.panel1.Controls.Add(this.rbH1);
            this.panel1.Controls.Add(this.rbD1);
            this.panel1.Controls.Add(this.rbH4);
            this.panel1.Enabled = false;
            this.panel1.Location = new System.Drawing.Point(703, 1);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(277, 29);
            this.panel1.TabIndex = 14;
            this.panel1.Visible = false;
            // 
            // zedGraphControl2
            // 
            this.zedGraphControl2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.zedGraphControl2.IsEnableHPan = false;
            this.zedGraphControl2.IsEnableHZoom = false;
            this.zedGraphControl2.IsEnableVPan = false;
            this.zedGraphControl2.IsEnableVZoom = false;
            this.zedGraphControl2.IsEnableWheelZoom = false;
            this.zedGraphControl2.IsShowContextMenu = false;
            this.zedGraphControl2.IsShowCopyMessage = false;
            this.zedGraphControl2.Location = new System.Drawing.Point(0, 534);
            this.zedGraphControl2.Name = "zedGraphControl2";
            this.zedGraphControl2.ScrollGrace = 0D;
            this.zedGraphControl2.ScrollMaxX = 0D;
            this.zedGraphControl2.ScrollMaxY = 0D;
            this.zedGraphControl2.ScrollMaxY2 = 0D;
            this.zedGraphControl2.ScrollMinX = 0D;
            this.zedGraphControl2.ScrollMinY = 0D;
            this.zedGraphControl2.ScrollMinY2 = 0D;
            this.zedGraphControl2.Size = new System.Drawing.Size(984, 50);
            this.zedGraphControl2.TabIndex = 1;
            this.zedGraphControl2.ZoomEvent += new ZedGraph.ZedGraphControl.ZoomEventHandler(this.ZedGraphControl2_ZoomEvent);
            // 
            // zedGraphControl1
            // 
            this.zedGraphControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.zedGraphControl1.BackColor = System.Drawing.SystemColors.Window;
            this.zedGraphControl1.IsEnableVPan = false;
            this.zedGraphControl1.IsEnableVZoom = false;
            this.zedGraphControl1.IsShowHScrollBar = true;
            this.zedGraphControl1.Location = new System.Drawing.Point(0, 30);
            this.zedGraphControl1.Name = "zedGraphControl1";
            this.zedGraphControl1.ScrollGrace = 0D;
            this.zedGraphControl1.ScrollMaxX = 0D;
            this.zedGraphControl1.ScrollMaxY = 0D;
            this.zedGraphControl1.ScrollMaxY2 = 0D;
            this.zedGraphControl1.ScrollMinX = 0D;
            this.zedGraphControl1.ScrollMinY = 0D;
            this.zedGraphControl1.ScrollMinY2 = 0D;
            this.zedGraphControl1.Size = new System.Drawing.Size(984, 570);
            this.zedGraphControl1.TabIndex = 0;
            this.zedGraphControl1.ZoomStepFraction = 0.2D;
            this.zedGraphControl1.ZoomEvent += new ZedGraph.ZedGraphControl.ZoomEventHandler(this.ZedGraphControl1_ZoomEvent);
            this.zedGraphControl1.ScrollEvent += new System.Windows.Forms.ScrollEventHandler(this.ZedGraphControl1_ScrollEvent);
            // 
            // cbShowTrades
            // 
            this.cbShowTrades.AutoSize = true;
            this.cbShowTrades.Checked = true;
            this.cbShowTrades.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbShowTrades.Location = new System.Drawing.Point(18, 8);
            this.cbShowTrades.Name = "cbShowTrades";
            this.cbShowTrades.Size = new System.Drawing.Size(59, 17);
            this.cbShowTrades.TabIndex = 10;
            this.cbShowTrades.Text = "Trades";
            this.cbShowTrades.UseVisualStyleBackColor = true;
            this.cbShowTrades.CheckedChanged += new System.EventHandler(this.CbShowTrades_CheckedChanged);
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.SystemColors.ScrollBar;
            this.panel2.Location = new System.Drawing.Point(156, 3);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(1, 23);
            this.panel2.TabIndex = 11;
            // 
            // cbShowCursorValues
            // 
            this.cbShowCursorValues.AutoSize = true;
            this.cbShowCursorValues.Location = new System.Drawing.Point(172, 8);
            this.cbShowCursorValues.Name = "cbShowCursorValues";
            this.cbShowCursorValues.Size = new System.Drawing.Size(96, 17);
            this.cbShowCursorValues.TabIndex = 12;
            this.cbShowCursorValues.Text = "Cursor Values";
            this.cbShowCursorValues.UseVisualStyleBackColor = true;
            this.cbShowCursorValues.CheckedChanged += new System.EventHandler(this.CbShowCursorValues_CheckedChanged);
            // 
            // cbCrosshair
            // 
            this.cbCrosshair.AutoSize = true;
            this.cbCrosshair.Enabled = false;
            this.cbCrosshair.Location = new System.Drawing.Point(269, 8);
            this.cbCrosshair.Name = "cbCrosshair";
            this.cbCrosshair.Size = new System.Drawing.Size(74, 17);
            this.cbCrosshair.TabIndex = 13;
            this.cbCrosshair.Text = "Crosshair";
            this.cbCrosshair.UseVisualStyleBackColor = true;
            this.cbCrosshair.CheckedChanged += new System.EventHandler(this.CbCrosshair_CheckedChanged);
            // 
            // cbVolumes
            // 
            this.cbVolumes.AutoSize = true;
            this.cbVolumes.Checked = true;
            this.cbVolumes.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbVolumes.Location = new System.Drawing.Point(84, 8);
            this.cbVolumes.Name = "cbVolumes";
            this.cbVolumes.Size = new System.Drawing.Size(69, 17);
            this.cbVolumes.TabIndex = 11;
            this.cbVolumes.Text = "Volumes";
            this.cbVolumes.UseVisualStyleBackColor = true;
            this.cbVolumes.CheckedChanged += new System.EventHandler(this.CbVolumes_CheckedChanged);
            // 
            // GraphStock
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.cbVolumes);
            this.Controls.Add(this.cbCrosshair);
            this.Controls.Add(this.cbShowCursorValues);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.cbShowTrades);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.zedGraphControl2);
            this.Controls.Add(this.zedGraphControl1);
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.Name = "GraphStock";
            this.Size = new System.Drawing.Size(984, 601);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private ZedGraph.ZedGraphControl zedGraphControl1;
        private ZedGraph.ZedGraphControl zedGraphControl2;
        private System.Windows.Forms.RadioButton rbM5;
        private System.Windows.Forms.RadioButton rbM30;
        private System.Windows.Forms.RadioButton rbH1;
        private System.Windows.Forms.RadioButton rbH4;
        private System.Windows.Forms.RadioButton rbD1;
        private System.Windows.Forms.RadioButton rbW1;
        private System.Windows.Forms.RadioButton rbMN;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.CheckBox cbShowTrades;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.CheckBox cbShowCursorValues;
        private System.Windows.Forms.CheckBox cbCrosshair;
        private System.Windows.Forms.CheckBox cbVolumes;
    }
}