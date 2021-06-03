using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ZedGraph;

namespace TTWinForms
{
    public partial class GraphHistogram : UserControl, IThemedGraph
    {
        readonly List<LineItem> vsRandLines = new List<LineItem>();
        readonly List<LineItem> inVsOutLines = new List<LineItem>();
        readonly List<LineItem> inTimeLines = new List<LineItem>();
        readonly List<ZedGraphControl> zedGraphs = new List<ZedGraphControl>();

        public GraphHistogram(List<PointPairList> linesVsRand, List<PointPairList> linesInVsOut, List<PointPairList> linesInTime)
        {
            InitializeComponent();

            Bitmap b = new Bitmap(30, 3);
            Graphics gr = Graphics.FromImage(b);
            gr.FillRectangle(new SolidBrush(Color.Blue), 0, 0, 30, 3);
            gr.Flush();
            label3.Image = b;

            Bitmap b2 = new Bitmap(30, 3);
            gr = Graphics.FromImage(b2);
            gr.FillRectangle(new SolidBrush(Color.Red), 0, 0, 30, 3);
            gr.Flush();
            label1.Image = b2;

            zedGraphs.Add(zedGraphControl1);
            zedGraphs.Add(zedGraphControl2);
            zedGraphs.Add(zedGraphControl3);
            zedGraphs.Add(zedGraphControl4);
            zedGraphs.Add(zedGraphControl5);
            zedGraphs.Add(zedGraphControl6);
            zedGraphs.Add(zedGraphControl7);
            zedGraphs.Add(zedGraphControl8);
            zedGraphs.Add(zedGraphControl9);
            zedGraphs.Add(zedGraphControl10);
            zedGraphs.Add(zedGraphControl11);
            zedGraphs.Add(zedGraphControl12);
            zedGraphs.Add(zedGraphControl13);
            zedGraphs.Add(zedGraphControl14);
            zedGraphs.Add(zedGraphControl15);
            zedGraphs.Add(zedGraphControl16);
            zedGraphs.Add(zedGraphControl17);
            zedGraphs.Add(zedGraphControl18);
            zedGraphs.Add(zedGraphControl19);

            zedGraphControl1.GraphPane.Title.Text = "E-Ratio";
            zedGraphControl2.GraphPane.Title.Text = "Net PNL";
            zedGraphControl3.GraphPane.Title.Text = "Drawdown";
            zedGraphControl4.GraphPane.Title.Text = "PNL/DD";
            zedGraphControl5.GraphPane.Title.Text = "WinPercentage";
            zedGraphControl6.GraphPane.Title.Text = "RatioWL";
            zedGraphControl7.GraphPane.Title.Text = "Profit Factor";
            zedGraphControl8.GraphPane.Title.Text = "AvgTrade";
            zedGraphControl9.GraphPane.Title.Text = "Trades";
            zedGraphControl10.GraphPane.Title.Text = "Sharpe";
            zedGraphControl11.GraphPane.Title.Text = "Sortino";
            zedGraphControl12.GraphPane.Title.Text = "CPC-Ratio";
            zedGraphControl13.GraphPane.Title.Text = "Corr Coef";
            zedGraphControl14.GraphPane.Title.Text = "T-Test";
            zedGraphControl15.GraphPane.Title.Text = "CAGR";
            zedGraphControl16.GraphPane.Title.Text = "K-Ratio";
            zedGraphControl17.GraphPane.Title.Text = "Expectancy Score";
            zedGraphControl18.GraphPane.Title.Text = "PerfectProfit Percentage";
            zedGraphControl19.GraphPane.Title.Text = "PerfectProfit Correlation";

            foreach (ZedGraphControl zed in zedGraphs)
            {
                zed.GraphPane.Margin.All = 1;
                zed.GraphPane.IsFontsScaled = false;
                zed.GraphPane.Legend.FontSpec.Size = 10;
                zed.GraphPane.Legend.Border.IsVisible = false;
                zed.GraphPane.Legend.IsVisible = false;
                //  zed.GraphPane.Title.IsVisible = false;
                zed.GraphPane.Title.FontSpec.Size = 11;
                zed.GraphPane.XAxis.Scale.FontSpec.Size = 10;
                zed.GraphPane.XAxis.MajorGrid.IsVisible = true;
                zed.GraphPane.XAxis.MajorGrid.DashOn = 5;
                zed.GraphPane.XAxis.MajorGrid.Color = Color.LightGray;
                zed.GraphPane.XAxis.Title.IsVisible = false;
                zed.GraphPane.XAxis.Scale.Min = 0;
                zed.GraphPane.XAxis.Scale.MagAuto = false;
                zed.GraphPane.XAxis.MajorTic.Size = 2;
                zed.GraphPane.XAxis.MinorTic.Size = 1;
                zed.GraphPane.YAxis.MajorGrid.IsVisible = true;
                zed.GraphPane.YAxis.MajorGrid.DashOn = 5;
                zed.GraphPane.YAxis.MajorGrid.Color = Color.LightGray;
                zed.GraphPane.YAxis.Title.IsVisible = false;
                zed.GraphPane.YAxis.Scale.MagAuto = false;
                zed.GraphPane.YAxis.Scale.FontSpec.Size = 10;
                zed.GraphPane.YAxis.MajorTic.Size = 2;
                zed.GraphPane.YAxis.MinorTic.Size = 1;
                zed.GraphPane.BarSettings.MinClusterGap = 0;
                zed.GraphPane.Border.IsVisible = false;
                zed.MouseDoubleClick += Zed_MouseDoubleClick;
            }

            for (int i = 0, j = 0; i < linesVsRand.Count; i += 2, j++)
            {
                LineItem line = zedGraphs[j].GraphPane.AddCurve("line", linesVsRand[i], Color.Blue);
                line.Symbol.IsVisible = false;
                line.Line.Width = 2;
                line.Line.Fill = new Fill(Color.FromArgb(100, line.Line.Color));
                vsRandLines.Add(line);

                line = zedGraphs[j].GraphPane.AddCurve("line2", linesVsRand[i + 1], Color.Red);
                line.Symbol.IsVisible = false;
                line.Line.Width = 2;
                line.Line.Fill = new Fill(Color.FromArgb(100, line.Line.Color));
                vsRandLines.Add(line);

                /*double min = double.MaxValue;
                double max = double.MinValue;
                if (linesVsRand[i].Count > 0) { min = linesVsRand[i].First().X; max = linesVsRand[i].Last().X; }
                if (linesVsRand[i + 1].Count > 0) { min = Math.Min(min, linesVsRand[i + 1].First().X); max = Math.Max(max, linesVsRand[i + 1].Last().X); }
                if (min < double.MaxValue && min != max)
                {
                    zedGraphs[j].GraphPane.XAxis.Scale.Min = min;
                    zedGraphs[j].GraphPane.XAxis.Scale.Max = max;
                }*/
            }

            for (int i = 0; i < 18; i++)
            {
                if (linesInVsOut.Count > 18)
                {
                    LineItem lineReg = zedGraphs[i + 1].GraphPane.AddCurve("", linesInVsOut[i + 18], Color.Red);
                    lineReg.Symbol.IsVisible = false;
                    lineReg.Line.Width = 3;
                    lineReg.Line.IsAntiAlias = true;
                    lineReg.IsVisible = false;
                    inVsOutLines.Add(lineReg);
                }

                LineItem linePoint = zedGraphs[i + 1].GraphPane.AddCurve("", linesInVsOut[i], Color.Blue, SymbolType.Circle);
                linePoint.Line.IsVisible = false;
                linePoint.Symbol.IsAntiAlias = true;
                linePoint.Symbol.Size = 4;
                linePoint.Symbol.Fill.Color = Color.Blue;
                linePoint.Symbol.Fill.Type = FillType.Solid;
                linePoint.IsVisible = false;
                inVsOutLines.Add(linePoint);
            }

            for (int i = 0; i < 18; i++)
            {
                LineItem line = zedGraphs[i + 1].GraphPane.AddCurve("", linesInTime[i], Color.Blue);
                line.Symbol.IsVisible = false;
                line.Line.Width = 2;
                line.Line.IsAntiAlias = true;
                line.IsVisible = false;
                inTimeLines.Add(line);
            }

            FitGraphs();
            UpdateGraphs();
        }

        private void UpdateGraphs()
        {
            foreach (ZedGraphControl zed in zedGraphs)
            {
                zed.ZoomOutAll(zed.GraphPane);
                zed.AxisChange();
                zed.Refresh();
            }
        }

        private void FitGraphs()
        {
            foreach (ZedGraphControl zed in zedGraphs)
            {
                double min = double.MaxValue;
                double max = double.MinValue;
                foreach(CurveItem curve in zed.GraphPane.CurveList)
                    if(curve.IsVisible && curve.NPts > 0)
                    {
                        min = Math.Min(min, curve.Points[0].X);
                        max = Math.Max(max, curve.Points[curve.NPts - 1].X);
                    }
                if (min < double.MaxValue && min != max)
                {
                    zed.GraphPane.XAxis.Scale.Min = min;
                    zed.GraphPane.XAxis.Scale.Max = max;
                }
            }
        }

        private void Zed_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ZedGraphControl zed = sender as ZedGraphControl;

                tableLayoutPanel1.SuspendLayout();

                if (zed.Tag == null)
                {
                    zed.Tag = tableLayoutPanel1.GetCellPosition(zed);
                    tableLayoutPanel1.SetCellPosition(zed, new TableLayoutPanelCellPosition(0, 1));
                    tableLayoutPanel1.SetRowSpan(zed, 4);
                    tableLayoutPanel1.SetColumnSpan(zed, 5);
                    if(zed!= zedGraphControl1) zedGraphControl1.Visible = false;
                    if(zed!= zedGraphControl2) zedGraphControl2.Visible = false;
                    if (zed!= zedGraphControl3) zedGraphControl3.Visible = false;
                    if (zed!= zedGraphControl4) zedGraphControl4.Visible = false;
                    if (zed!= zedGraphControl5) zedGraphControl5.Visible = false;
                    if (zed!= zedGraphControl6) zedGraphControl6.Visible = false;
                    if (zed!= zedGraphControl7) zedGraphControl7.Visible = false;
                    if (zed!= zedGraphControl8) zedGraphControl8.Visible = false;
                    if (zed!= zedGraphControl9) zedGraphControl9.Visible = false;
                    if (zed!= zedGraphControl10) zedGraphControl10.Visible = false;
                    if (zed!= zedGraphControl11) zedGraphControl11.Visible = false;
                    if (zed!= zedGraphControl12) zedGraphControl12.Visible = false;
                    if (zed!= zedGraphControl13) zedGraphControl13.Visible = false;
                    if (zed!= zedGraphControl14) zedGraphControl14.Visible = false;
                    if (zed!= zedGraphControl15) zedGraphControl15.Visible = false;
                    if (zed != zedGraphControl16) zedGraphControl16.Visible = false;
                    if (zed != zedGraphControl17) zedGraphControl17.Visible = false;
                    if (zed != zedGraphControl18) zedGraphControl18.Visible = false;
                    if (zed != zedGraphControl19) zedGraphControl19.Visible = false;
                }
                else
                {
                    tableLayoutPanel1.SetRowSpan(zed, 1);
                    tableLayoutPanel1.SetColumnSpan(zed, 1);
                    tableLayoutPanel1.SetCellPosition(zed, (TableLayoutPanelCellPosition)zed.Tag);
                    zed.Tag = null;
                    if (zed != zedGraphControl1) zedGraphControl1.Visible = true;
                    if (zed != zedGraphControl2) zedGraphControl2.Visible = true;
                    if (zed != zedGraphControl3) zedGraphControl3.Visible = true;
                    if (zed != zedGraphControl4) zedGraphControl4.Visible = true;
                    if (zed != zedGraphControl5) zedGraphControl5.Visible = true;
                    if (zed != zedGraphControl6) zedGraphControl6.Visible = true;
                    if (zed != zedGraphControl7) zedGraphControl7.Visible = true;
                    if (zed != zedGraphControl8) zedGraphControl8.Visible = true;
                    if (zed != zedGraphControl9) zedGraphControl9.Visible = true;
                    if (zed != zedGraphControl10) zedGraphControl10.Visible = true;
                    if (zed != zedGraphControl11) zedGraphControl11.Visible = true;
                    if (zed != zedGraphControl12) zedGraphControl12.Visible = true;
                    if (zed != zedGraphControl13) zedGraphControl13.Visible = true;
                    if (zed != zedGraphControl14) zedGraphControl14.Visible = true;
                    if (zed != zedGraphControl15) zedGraphControl15.Visible = true;
                    if (zed != zedGraphControl16) zedGraphControl16.Visible = true;
                    if (zed != zedGraphControl17) zedGraphControl17.Visible = true;
                    if (zed != zedGraphControl18) zedGraphControl18.Visible = true;
                    if (zed != zedGraphControl19) zedGraphControl19.Visible = true;
                }

                tableLayoutPanel1.ResumeLayout();
            }
        }

        private void VisibilityUpdate()
        {
            for (int i = 2; i < vsRandLines.Count; i++)     // except first two lines - e-ratio
                vsRandLines[i].IsVisible = checkBoxRand.Checked;

            for (int i = 0; i < inVsOutLines.Count; i++)
                inVsOutLines[i].IsVisible = checkBoxInVsOut.Checked;

            for (int i = 0; i < inTimeLines.Count; i++)
                inTimeLines[i].IsVisible = checkBoxTime.Checked;

            FitGraphs();
            UpdateGraphs();
        }

        private void CheckBoxRand_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxRand.Checked)
            {
                checkBoxInVsOut.Checked = checkBoxTime.Checked = false;
                VisibilityUpdate();
            }
            else if(!checkBoxInVsOut.Checked && !checkBoxTime.Checked)
                checkBoxRand.Checked = true;
        }

        private void CheckBoxInVsOut_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxInVsOut.Checked)
            {
                checkBoxRand.Checked = checkBoxTime.Checked = false;
                VisibilityUpdate();
            }
            else if (!checkBoxRand.Checked && !checkBoxTime.Checked)
                checkBoxInVsOut.Checked = true;
        }

        private void CheckBoxTime_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxTime.Checked)
            {
                checkBoxInVsOut.Checked = checkBoxRand.Checked = false;
                VisibilityUpdate();
            }
            else if (!checkBoxInVsOut.Checked && !checkBoxRand.Checked)
                checkBoxTime.Checked = true;
        }

        public void UpdateTheme(string themeName)
        {
            foreach (var zed in zedGraphs)
            {
                Graph.ZedThemeUpdate(zed, themeName);
            }

            if (themeName.Contains("Dark"))
            {
                BackColor = Graph.backColorDark;

                checkBoxInVsOut.FlatAppearance.CheckedBackColor = Graph.accentColorDark;
                checkBoxRand.FlatAppearance.CheckedBackColor = Graph.accentColorDark;
                checkBoxTime.FlatAppearance.CheckedBackColor = Graph.accentColorDark;
            }
            else
            {
                BackColor = Graph.backColorLight;

                checkBoxInVsOut.FlatAppearance.CheckedBackColor = Graph.accentColorLight;
                checkBoxRand.FlatAppearance.CheckedBackColor = Graph.accentColorLight;
                checkBoxTime.FlatAppearance.CheckedBackColor = Graph.accentColorLight;
            }
        }
    }
}
