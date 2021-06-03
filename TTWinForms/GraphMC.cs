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
    public partial class GraphMC : UserControl, IThemedGraph
    {
        readonly List<LineItem> LinesOriginal = new List<LineItem>();
        readonly List<LineItem> LinesResample = new List<LineItem>();
        readonly List<LineItem> LinesRandomized = new List<LineItem>();
        readonly List<LineItem> Lines1_10 = new List<LineItem>();
        
        readonly List<CurveItem> LinesConsecOriginal = new List<CurveItem>();
        readonly List<CurveItem> LinesConsecResample = new List<CurveItem>();
        readonly List<CurveItem> LinesConsecRandomized = new List<CurveItem>();
        readonly List<CurveItem> LinesConsec1_10 = new List<CurveItem>();

        bool originalManualUpdate = false;
        bool resampleManualUpdate = false;
        bool randomizedManualUpdate = false;
        bool r1_10ManualUpdate = false;

        public GraphMC(List<PointPairList> linesOriginal, List<PointPairList> linesResample, List<PointPairList> linesRandomized, List<PointPairList> lines1_10, string results1, string results2,
            List<PointPairList> consecOriginal, List<PointPairList> consecResample, List<PointPairList> consecRandomized, List<PointPairList> consec1_10)
        {
            InitializeComponent();

            Random random = new Random(DateTime.Now.Millisecond);

            ZedInit(zedGraphControl1);
            ZedInit(zedGraphControl2);

            labelResults.Text = results1;
            labelResults2.Text = results2;
            checkBoxOriginal.Checked = true;

            if (linesOriginal.Count < 2) checkBoxOriginal.Visible = false;
            if (linesResample.Count < 1) checkBoxResample.Visible = false;
            if (linesRandomized.Count < 1) checkBoxRandomized.Visible = false;
            if (lines1_10.Count < 1) checkBox1_10.Visible = false;

            Color[] colors = { Color.FromArgb(0, 255, 255, 255), Color.FromArgb(255, 255, 255, 255), Color.FromArgb(100, 255, 255, 255) };
            float[] positions = { 0, 0.01f, 1 };
            zedGraphControl1.GraphPane.Legend.Fill = new Fill(colors, positions);
            //zedGraphControl1.GraphPane.Legend.Position = LegendPos.InsideTopLeft;
            zedGraphControl1.GraphPane.Legend.Position = LegendPos.Float;
            zedGraphControl1.GraphPane.Legend.Location.CoordinateFrame = CoordType.ChartFraction;
            zedGraphControl1.GraphPane.Legend.Location.X = 0.01;
            zedGraphControl1.GraphPane.Legend.Location.Y = 0.01;

            zedGraphControl2.GraphPane.Legend.Fill = new Fill(colors, positions);
            //zedGraphControl2.GraphPane.Legend.Position = LegendPos.InsideTopLeft;
            zedGraphControl2.GraphPane.Legend.Position = LegendPos.Float;
            zedGraphControl2.GraphPane.Legend.Location.CoordinateFrame = CoordType.ChartFraction;
            zedGraphControl2.GraphPane.Legend.Location.X = 0.01;
            zedGraphControl2.GraphPane.Legend.Location.Y = 0.01;

            {   // Plotting real line
                LineItem line = zedGraphControl1.GraphPane.AddCurve("Real", linesOriginal[0], Color.Blue);
                line.Symbol.IsVisible = false;
                line.Line.Width = 3;
                line.Line.IsAntiAlias = true;
                LineItem lineW = zedGraphControl1.GraphPane.AddCurve("", linesOriginal[0], Color.FromArgb(230, 230, 230));
                lineW.Symbol.IsVisible = false;
                lineW.Line.Width = 7;
                lineW.Line.IsAntiAlias = true;
            }

            //original
            for (int i = 1; i < linesOriginal.Count && i < 250; i++)
            {
                LineItem line = zedGraphControl1.GraphPane.AddCurve("", linesOriginal[i],
                    Color.FromArgb(random.Next(80, 255), random.Next(50, 255), random.Next(30, 255)));
                line.Symbol.IsVisible = false;
                line.Line.Width = 1;
                line.Line.IsAntiAlias = true;
                LinesOriginal.Add(line);
            }
            //resample
            for (int i = 0; i < linesResample.Count && i < 250; i++)
            {
                LineItem line = zedGraphControl1.GraphPane.AddCurve(i==0?"Resample":"", linesResample[i],
                    Color.FromArgb(random.Next(80, 255), random.Next(50, 255), random.Next(30, 255)));
                line.Symbol.IsVisible = false;
                line.Line.Width = 1;
                line.IsVisible = false;
                line.Line.IsAntiAlias = true;
                LinesResample.Add(line);
            }
            //randomized
            for (int i = 0; i < linesRandomized.Count && i < 250; i++)
            {
                LineItem line = zedGraphControl1.GraphPane.AddCurve(i == 0 ? "Random" : "", linesRandomized[i],
                    Color.FromArgb(random.Next(80, 255), random.Next(50, 255), random.Next(30, 255)));
                line.Symbol.IsVisible = false;
                line.Line.Width = 1;
                line.IsVisible = false;
                line.Line.IsAntiAlias = true;
                LinesRandomized.Add(line);
            }
            //1-10
            for (int i = 0; i < lines1_10.Count && i < 250; i++)
            {
                LineItem line = zedGraphControl1.GraphPane.AddCurve((i+1).ToString(), lines1_10[i],
                    Color.FromArgb(random.Next(80, 255), random.Next(50, 255), random.Next(30, 255)));
                line.Symbol.IsVisible = false;
                line.Line.Width = 1;
                line.IsVisible = false;
                line.Line.IsAntiAlias = true;
                Lines1_10.Add(line);
            }

            //original consecutive
            //string[] names = new string[] { "AverageLoss", "AverageConsecutiveLoss", "MinLoss", "MinConsecutiveLoss",
            //    "AverageWin", "AverageConsecutiveWin", "MaxWin", "MaxConsecutiveWin" };
            string[] names = new string[] { "Avg Max Win", "Max Consecutive Wins", "Avg Consecutive Wins",
                "Avg Max Loss", "Max Consecutive Losses", "Avg Consecutive Losses" };
            Color[] lineColors = new Color[] { Color.Green, Color.Blue, Color.Cyan,
                Color.Red, Color.Gold, Color.Magenta };
            //Color[] lineColors = new Color[] { Color.Blue, Color.Blue, Color.Red, Color.Red,
            //    Color.Magenta, Color.Magenta, Color.Green, Color.Green };
            /*Color[] lineColors = new Color[] {
                Color.FromArgb(62,181,241), Color.Blue,
                Color.Red, Color.Red,
                Color.Yellow, Color.Orange,
                Color.Green, Color.Green };*/
            //bool[] isBars = new bool[] { true, false, true, false, true, false, true, false };

            void CreateConsecLines(List<CurveItem> listCurves, List<PointPairList> listPoints, bool isOriginal)
            {
                //for (int i = (listPoints?.Count ?? 0) -1; i >= 0; --i)
                /*for (int i = 0; i < listPoints?.Count; ++i)
                {
                    if (isBars[i] == false)
                    {
                        LineItem line = zedGraphControl2.GraphPane.AddCurve(isOriginal ? names[i] : "", listPoints[i], lineColors[i]);
                        line.Symbol.IsVisible = false;
                        line.IsVisible = isOriginal;
                        line.Line.Width = 2;
                        line.Line.IsAntiAlias = true;
                        listCurves.Add(line);
                    }
                }*/
                for (int i = 0; i < listPoints?.Count; ++i)
                {
                    //if (isBars[i])
                    //{
                    //    var bar = zedGraphControl2.GraphPane.AddBar(isOriginal ? names[i] : "", listPoints[i], lineColors[i]);
                    //    bar.Bar.Border.IsVisible = false;
                    //    bar.Bar.Fill = new Fill(Color.FromArgb(250, lineColors[i]));
                    //    bar.IsVisible = isOriginal;
                    //    listCurves.Add(bar);
                    //}
                    //else
                    {
                        LineItem line = zedGraphControl2.GraphPane.AddCurve(isOriginal /*&& isBars[i]*/ ? names[i] : "", listPoints[i], lineColors[i]);
                        line.Symbol.IsVisible = false;
                        line.IsVisible = isOriginal;
                        line.IsY2Axis = i == 0 || i == 3;
                        line.Line.Width = 2;
                       //line.Line.Color = Color.Transparent;
                        line.Line.IsAntiAlias = true;
                        if (i > 2)
                        {
                            //line.Line.Style = System.Drawing.Drawing2D.DashStyle.Custom;
                            //line.Line.DashOn = 1;
                            //line.Line.DashOff = 1;
                            line.Line.Fill= new Fill(Color.FromArgb(15, lineColors[i]));
                        }
                        //  line.Line.Fill = new Fill(Color.FromArgb(isBars[i] ? (i == 0||i==4 ? 210 : 110) : 15, lineColors[i]));
                        listCurves.Add(line);
                    }
                }
            }
            CreateConsecLines(LinesConsecOriginal, consecOriginal, true);
            CreateConsecLines(LinesConsecResample, consecResample, false);
            CreateConsecLines(LinesConsecRandomized, consecRandomized, false);
            CreateConsecLines(LinesConsec1_10, consec1_10, false);

            Print(LinesConsecOriginal, "Original");

            //zedGraphControl2.GraphPane.BarSettings.Type = BarType.SortedOverlay;
            //zedGraphControl2.GraphPane.BarSettings.MinClusterGap = 0.5f;

            zedGraphControl1.AxisChange();
            zedGraphControl1.Refresh();
            zedGraphControl2.AxisChange();
            zedGraphControl2.Refresh();
        }

        private void ZedInit(ZedGraphControl zed)
        {
            zed.GraphPane.IsFontsScaled = false;
            zed.GraphPane.Legend.Border.IsVisible = false;
            zed.GraphPane.Title.IsVisible = false;
            zed.GraphPane.XAxis.MajorGrid.IsVisible = true;
            zed.GraphPane.XAxis.MajorGrid.DashOn = 5;
            zed.GraphPane.XAxis.MajorGrid.Color = Color.LightGray;
            zed.GraphPane.XAxis.Title.IsVisible = false;
            //zed.GraphPane.XAxis.Scale.Min = 0;
            zed.GraphPane.XAxis.Scale.MagAuto = false;
            zed.GraphPane.XAxis.Scale.MinGrace = 0;
            //zed.GraphPane.XAxis.Scale.MaxGrace = 0;
            zed.GraphPane.YAxis.MajorGrid.IsVisible = true;
            zed.GraphPane.YAxis.MajorGrid.DashOn = 5;
            zed.GraphPane.YAxis.MajorGrid.Color = Color.LightGray;
            zed.GraphPane.YAxis.Title.IsVisible = false;
            zed.GraphPane.YAxis.Scale.MagAuto = false;
            zed.GraphPane.BarSettings.MinClusterGap = 0;
            zed.GraphPane.Border.IsVisible = false;

            zed.ModifyContextMenu();
        }

        private void Print(List<CurveItem> list, string header, string currency = "USD")
        {
            if (list?.Count >= 6)
            {
                label3.Text = $@"{header}
Avg Max Win: {currency} {list[0].Points[list[0].NPts - 1].Y:F2}
Max Consec Wins: {list[1].Points[list[1].NPts - 1].Y}
Avg Consec Wins: {list[2].Points[list[2].NPts - 1].Y:F2}";
                label4.Text = $@"{header}
Avg Max Loss:  {currency} {list[3].Points[list[3].NPts - 1].Y:F2}
Max Consec Losses: {list[4].Points[list[4].NPts - 1].Y}
Avg Consec Losses: {list[5].Points[list[5].NPts - 1].Y:F2}";
            }
        }

        private void CheckBoxRandomized_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxRandomized.Checked)
            {
                if (checkBoxOriginal.Checked) { originalManualUpdate = true; checkBoxOriginal.Checked = false; }
                if (checkBoxResample.Checked) { resampleManualUpdate = true; checkBoxResample.Checked = false; }
                if (checkBox1_10.Checked) { r1_10ManualUpdate = true; checkBox1_10.Checked = false; }
                Print(LinesConsecRandomized, "Randomized");
                Application.DoEvents();
            }

            for (int i = 0; i < LinesRandomized.Count; i++)
                LinesRandomized[i].IsVisible = checkBoxRandomized.Checked;
            for (int i = 0; i < LinesConsecRandomized.Count; i++)
                LinesConsecRandomized[i].IsVisible = checkBoxRandomized.Checked;

            if (!randomizedManualUpdate)
            {
                zedGraphControl1.AxisChange();
                zedGraphControl1.Refresh();
                zedGraphControl2.AxisChange();
                zedGraphControl2.Refresh();
            }
            else randomizedManualUpdate = false;
        }

        private void CheckBox1_10_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1_10.Checked)
            {
                if (checkBoxOriginal.Checked) { originalManualUpdate = true; checkBoxOriginal.Checked = false; }
                if (checkBoxResample.Checked) { resampleManualUpdate = true; checkBoxResample.Checked = false; }
                if (checkBoxRandomized.Checked) { randomizedManualUpdate = true; checkBoxRandomized.Checked = false; }
                Print(LinesConsec1_10, "1-10");
                Application.DoEvents();
            }

            for (int i = 0; i < Lines1_10.Count; i++)
                Lines1_10[i].IsVisible = checkBox1_10.Checked;
            for (int i = 0; i < LinesConsec1_10.Count; i++)
                LinesConsec1_10[i].IsVisible = checkBox1_10.Checked;

            if (!r1_10ManualUpdate)
            {
                zedGraphControl1.AxisChange();
                zedGraphControl1.Refresh();
                zedGraphControl2.AxisChange();
                zedGraphControl2.Refresh();
            }
            else r1_10ManualUpdate = false;
        }

        private void CheckBoxOriginal_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxOriginal.Checked)
            {
                if (checkBoxResample.Checked) { resampleManualUpdate = true; checkBoxResample.Checked = false; }
                if (checkBoxRandomized.Checked) { randomizedManualUpdate = true; checkBoxRandomized.Checked = false; }
                if (checkBox1_10.Checked) { r1_10ManualUpdate = true; checkBox1_10.Checked = false; }
                Print(LinesConsecOriginal, "Original");
                Application.DoEvents();
            }

            for (int i = 0; i < LinesOriginal.Count; i++)
                LinesOriginal[i].IsVisible= checkBoxOriginal.Checked;
            for (int i = 0; i < LinesConsecOriginal.Count; i++)
                LinesConsecOriginal[i].IsVisible = checkBoxOriginal.Checked;

            if (!originalManualUpdate)
            {
                zedGraphControl1.AxisChange();
                zedGraphControl1.Refresh();
                zedGraphControl2.AxisChange();
                zedGraphControl2.Refresh();
            }
            else originalManualUpdate = false;
        }

        private void CheckBoxResample_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxResample.Checked)
            {
                if (checkBoxOriginal.Checked) { originalManualUpdate = true; checkBoxOriginal.Checked = false; }
                if (checkBoxRandomized.Checked) { randomizedManualUpdate = true; checkBoxRandomized.Checked = false; }
                if (checkBox1_10.Checked) { r1_10ManualUpdate = true; checkBox1_10.Checked = false; }
                Print(LinesConsecResample, "Resample");
                Application.DoEvents();
            }

            for (int i = 0; i < LinesResample.Count; i++)
                LinesResample[i].IsVisible = checkBoxResample.Checked;
            for (int i = 0; i < LinesConsecResample.Count; i++)
                LinesConsecResample[i].IsVisible = checkBoxResample.Checked;

            if (!resampleManualUpdate)
            {
                zedGraphControl1.AxisChange();
                zedGraphControl1.Refresh();
                zedGraphControl2.AxisChange();
                zedGraphControl2.Refresh();
            }
            else resampleManualUpdate = false;
        }

        private void GraphMC_Shown(object sender, EventArgs e)
        {
            bool v = false;
            foreach (Control c in flowLayoutPanel1.Controls)
            {
                if (c.Visible) { v = true; break; }
            }
            if (v == false)
            {
                flowLayoutPanel1.Visible = false;
                splitContainer1.Location = new Point(0, 0);
                splitContainer1.Height += flowLayoutPanel1.Height;
            }

            var h = splitContainer1.Height;
            var loc = tableLayoutPanel1.Location;
            ClientSize = new Size(ClientSize.Width, tableLayoutPanel1.Top + labelResults.Height + 1);
            tableLayoutPanel1.Location = loc;
            splitContainer1.Height = h;
        }

        public void UpdateTheme(string themeName)
        {
            Graph.ZedThemeUpdate(zedGraphControl1, themeName);
            Graph.ZedThemeUpdate(zedGraphControl2, themeName);

            if (themeName.Contains("Dark"))
            {
                BackColor = Graph.backColorDark;
                splitContainer1.BackColor = Color.FromArgb(60, 60, 60);

                checkBox1_10.FlatAppearance.CheckedBackColor = Graph.accentColorDark;
                checkBoxOriginal.FlatAppearance.CheckedBackColor = Graph.accentColorDark;
                checkBoxRandomized.FlatAppearance.CheckedBackColor = Graph.accentColorDark;
                checkBoxResample.FlatAppearance.CheckedBackColor = Graph.accentColorDark;
            }
            else
            {
                BackColor = Graph.backColorLight;
                splitContainer1.BackColor = SystemColors.ControlLight;

                checkBox1_10.FlatAppearance.CheckedBackColor = Graph.accentColorLight;
                checkBoxOriginal.FlatAppearance.CheckedBackColor = Graph.accentColorLight;
                checkBoxRandomized.FlatAppearance.CheckedBackColor = Graph.accentColorLight;
                checkBoxResample.FlatAppearance.CheckedBackColor = Graph.accentColorLight;
            }
        }
    }
}
