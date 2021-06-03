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
    public interface IThemedGraph
    {
        void UpdateTheme(string themeName);
    }

    public partial class Graph : UserControl, IThemedGraph
    {
        enum Mode { NotDefined, VarianceTest, NoiseTest };

        readonly Mode mode = Mode.NotDefined;
        readonly List<LineItem> linesReal = new List<LineItem>();
        readonly List<LineItem> linesRandom = new List<LineItem>();
        readonly List<LineItem> linesOthers = new List<LineItem>();
        readonly List<LineItem> linesBasket = new List<LineItem>();
        readonly ZedGraphControl zed2 = null;
        private readonly LineItem bahLine = null;
        private readonly LineItem bahLine_dd = null;
        LinkLabel llDatesTrades;

        public Graph(int type, PointPairList line1, PointPairList line2, PointPairList bars)
        {
            InitializeComponent();

            ZedInit();
            zedGraphControl1.GraphPane.Legend.Fill = new Fill(Color.FromArgb(127, 255, 255, 255));
            panelBottom.Visible = false;

            if (line1 != null)
            {
                string name;
                switch (type)
                {
                    case 1: name = "Equity Curve";
                        if (line1[0].X != 0)
                        {
                            zedGraphControl1.GraphPane.XAxis.Type = AxisType.Date;
                            zedGraphControl1.GraphPane.XAxis.Scale.Min = line1[0].X;
                        }
                        break;
                    case 2: name = "E-Ratio"; break;
                    case 3: name = "Monte Carlo Equity Curve 1"; break;
                    case 4: name = "Cumulative Drawdowns"; break;
                    default: name = "Line 1"; break;
                }
                LineItem line = zedGraphControl1.GraphPane.AddCurve(name, line1, Color.Blue);
                line.Symbol.IsVisible = false;
                line.Line.Width = 2;
                line.Line.IsAntiAlias = true;
            }
            if (line2 != null)
            {
                string name;
                Color c = Color.Red;
                switch (type)
                {
                    case 1: name = "Underlying";
                        c = Color.FromArgb(80, 80, 80);
                        //zedGraphControl1.GraphPane.XAxis.Scale.Min = Math.Min(line1[0].X, line2[0].X);
                        LineItem lineM = zedGraphControl1.GraphPane.AddCurve("Market", bars, Color.Brown);
                        lineM.Symbol.IsVisible = false;
                        lineM.Line.Width = 2;
                        lineM.Line.IsAntiAlias = true;
                        break;
                    case 2: name = "Baseline"; break;
                    case 3: name = "Monte Carlo Equity Curve 2"; break;
                    default: name = "Line 2"; break;
                }
                LineItem line = zedGraphControl1.GraphPane.AddCurve(name, line2, c);
                line.Symbol.IsVisible = false;
                line.Line.Width = 2;
                line.Line.IsAntiAlias = true;
            }

            if (type > 1)
            {
                checkBoxRandom.Visible = false;
                checkBoxReal.Visible = false;
            }
            else
            {
                checkBoxReal.Text = "Market";
                checkBoxReal.Checked = true;
                checkBoxRandom.Text = "Underlying";
                checkBoxRandom.Checked = true;
            }

            if (bars != null)
            {
                if (type == 3)
                {
                    labelResults.Text = labelResults2.Text = labelResults3.Text = "";
                    for (int i = 0; i < bars.Count; i++)
                        labelResults.Text += string.Format("After {0,-3} Trades: {1} {2:F2} to {1} {3:F2}{4}", bars[i].X, bars[i].Tag as string, bars[i].Y, bars[i].Z, Environment.NewLine);

                    //panelBottom.Height = labelResults.Height + 1;
                    label2.Visible = labelResults.Visible = true;
                }
                else if (type == 4)
                {
                    zedGraphControl1.GraphPane.XAxis.Scale.Min = 0;
                    BarItem bar = zedGraphControl1.GraphPane.AddBar("Drawdown % PMF", bars, Color.Green);
                    bar.Bar.Border.IsVisible = false;
                }
            }

            zedGraphControl1.AxisChange();
            zedGraphControl1.Refresh();
        }

        /// <summary>
        /// Plot graph for Variance Testing and Noise Test
        /// </summary>
        /// <param name="lines">List of lines. Should contain at least one item for REAL curve and up to 250 items for RANDOM curves</param>
        /// <param name="results1">Text label 1</param>
        /// <param name="results2">Text label 2</param>
        /// <param name="results3">Text label 3</param>
        public Graph(List<PointPairList> lines, string results1, string results2, string results3)
        {
            InitializeComponent();

            Random random = new Random(DateTime.Now.Millisecond);

            ZedInit();

            Color[] colors = { Color.FromArgb(0, 255, 255, 255), Color.FromArgb(255, 255, 255, 255), Color.FromArgb(100, 255, 255, 255) };
            float[] positions = { 0, 0.01f, 1 };
            zedGraphControl1.GraphPane.Legend.Fill = new Fill(colors, positions);
            //zedGraphControl1.GraphPane.Legend.Position = LegendPos.InsideTopLeft;
            zedGraphControl1.GraphPane.Legend.Position = LegendPos.Float;
            zedGraphControl1.GraphPane.Legend.Location.CoordinateFrame = CoordType.ChartFraction;
            zedGraphControl1.GraphPane.Legend.Location.X = 0.01;
            zedGraphControl1.GraphPane.Legend.Location.Y = 0.01;

            {   // Plotting real line
                LineItem line = zedGraphControl1.GraphPane.AddCurve("Real", lines[0], Color.Blue);
                line.Symbol.IsVisible = false;
                line.Line.Width = 3;
                line.Line.IsAntiAlias = true;
                LineItem lineW = zedGraphControl1.GraphPane.AddCurve("", lines[0], Color.FromArgb(230, 230, 230));
                lineW.Symbol.IsVisible = false;
                lineW.Line.Width = 7;
                lineW.Line.IsAntiAlias = true;
            }
            {   // Variance testing and Noise testing
                checkBoxRandom.Visible = false;
                checkBoxReal.Checked = true;

                labelResults.Text = results1;
                labelResults2.Text = results2;
                labelResults3.Text = results3;

                if (results3 == "NoiseTest")
                {
                    panelBottom.Visible = false;
                    TurnOnDatesMode();
                    mode = Mode.NoiseTest;
                }
                else mode = Mode.VarianceTest;

                for (int i = 1; i < lines.Count && i < 250; i++)
                {
                    LineItem line = zedGraphControl1.GraphPane.AddCurve(i == 1 ? "Random" : "", lines[i],
                        Color.FromArgb(random.Next(80, 255), random.Next(50, 255), random.Next(30, 255)));
                    line.Symbol.IsVisible = false;
                    line.Line.Width = 1;
                    line.Line.IsAntiAlias = true;
                }
            }

            zedGraphControl1.AxisChange();
            zedGraphControl1.Refresh();
        }

        /// <summary>
        /// Plot graph for Equity Curve with random, vs Others and basket comparation
        /// </summary>
        /// <param name="pplReal">Real lines. Should contain at least one item</param>
        /// <param name="pplRandom">Random lines</param>
        public Graph(List<PointPairList> pplReal, List<PointPairList> pplRandom, List<PointPairList> pplOthers, List<string> vsLabels, int oosIndex, PointPairList BaH,
            List<PointPairList> pplBasket, List<string> basketLabels, List<int> basketOOSindexes)
        {
            InitializeComponent();

            const int oosSelectionWidth = 6;

            panelBottom.Visible = false;

            if (pplReal.Count < 2) checkBoxReal.Visible = false;
            if (pplRandom.Count < 1) checkBoxRandom.Visible = false;
            if (pplOthers.Count > 0) { checkBoxOriginal.Visible = true; checkBoxOriginal.Text = "Vs. Others"; }
            checkBoxDD.Visible = true;
            if (BaH != null && BaH.Count > 0) checkBoxBaH.Visible = true;
            if (pplBasket != null && pplBasket.Count > 0) checkBoxBasket.Visible = true;

            ZedInit();

            TurnOnDatesMode();

            /*GraphPane gp = new GraphPane();
            zedGraphControl1.MasterPane.Add(gp);
            using (Graphics g = this.CreateGraphics())
            {
                zedGraphControl1.MasterPane.SetLayout(g, PaneLayout.SingleColumn);
                zedGraphControl1.MasterPane.AxisChange(g);
            }*/
            
            zed2 = new ZedGraphControl();
            zed2.GraphPane.IsFontsScaled = false;
            zed2.GraphPane.Legend.Border.IsVisible = false;
            zed2.GraphPane.Title.IsVisible = false;
            zed2.GraphPane.XAxis.MajorGrid.IsVisible = true;
            zed2.GraphPane.XAxis.MajorGrid.DashOn = 5;
            zed2.GraphPane.XAxis.MajorGrid.Color = Color.LightGray;
            zed2.GraphPane.XAxis.Title.IsVisible = false;
            zed2.GraphPane.XAxis.Scale.Min = 0;
            zed2.GraphPane.XAxis.Scale.MagAuto = false;
            zed2.GraphPane.XAxis.Scale.MinGrace = 0;
            zed2.GraphPane.XAxis.Scale.MaxGrace = 0;
            zed2.GraphPane.YAxis.MajorGrid.IsVisible = true;
            zed2.GraphPane.YAxis.MajorGrid.DashOn = 3;
            zed2.GraphPane.YAxis.MajorGrid.Color = Color.LightGray;
            zed2.GraphPane.YAxis.Title.IsVisible = false;
            zed2.GraphPane.YAxis.Scale.MagAuto = false;
            zed2.GraphPane.Border.IsVisible = false;
            //zed2.Visible = false;
            zed2.ModifyContextMenu();
            zed2.Dock = DockStyle.Fill;
            //tableLayoutPanel1.Controls.Add(zed2);
            //tableLayoutPanel1.SetRow(zed2, 2);
            splitContainer1.Panel2.Controls.Add(zed2);

            {   //create drawdown line at second graph
                double max = 0.0;
                double pnl;
                PointPairList ddl = new PointPairList();
                for (int i = 0; i < pplReal[0].Count; i++)
                {
                    pnl = pplReal[0][i].Y;
                    if (pnl > max) { max = pnl; }
                    ddl.Add(i, pnl - max, pplReal[0][i].Z);
                }
                LineItem line = zed2.GraphPane.AddCurve("", ddl, Color.Blue);
                line.Symbol.IsVisible = false;
                line.Line.Width = 2;
                line.Line.IsAntiAlias = true;
                //line.dimatsiDrawMode = 1;

                max = 0;
                PointPairList ddlbah = new PointPairList();
                if (BaH != null)
                    for (int i = 0; i < BaH.Count; i++)
                    {
                        pnl = BaH[i].Y;
                        if (pnl > max) { max = pnl; }
                        ddlbah.Add(i, pnl - max, BaH[i].Z);
                    }
                bahLine_dd = zed2.GraphPane.AddCurve("", ddlbah, Color.Brown);
                bahLine_dd.Symbol.IsVisible = false;
                bahLine_dd.Line.Width = 2;
                bahLine_dd.Line.IsAntiAlias = true;
                bahLine_dd.IsVisible = false;
                //bahLine_dd.dimatsiDrawMode = 1;
            }

            Color[] colors = { Color.FromArgb(0, 255, 255, 255), Color.FromArgb(255, 255, 255, 255), Color.FromArgb(100, 255, 255, 255) };
            float[] positions = { 0, 0.01f, 1 };
            zedGraphControl1.GraphPane.Legend.Fill = new Fill(colors, positions);
            //zedGraphControl1.GraphPane.Legend.Position = LegendPos.InsideTopLeft;
            zedGraphControl1.GraphPane.Legend.Position = LegendPos.Float;
            zedGraphControl1.GraphPane.Legend.Location.CoordinateFrame = CoordType.ChartFraction;
            zedGraphControl1.GraphPane.Legend.Location.X = 0.01;
            zedGraphControl1.GraphPane.Legend.Location.Y = 0.01;

            {
                LineItem line = zedGraphControl1.GraphPane.AddCurve("Selected", pplReal[0], Color.Blue);
                line.Symbol.IsVisible = false;
                line.Line.Width = 2;
                line.Line.IsAntiAlias = true;
                line.Tag = 0;
                //line.dimatsiDrawMode = 1;

                if (oosIndex != 0)
                {
                    PointPairList ppl = new PointPairList();
                    if (oosIndex > 0)
                    {   //end
                        for (int i = 0; i < pplReal[0].Count; i++)
                            if (i >= oosIndex)
                                ppl.Add(pplReal[0][i]);
                    }
                    else //beginning
                    {
                        for (int i = 0; i < pplReal[0].Count; i++)
                            if (i <= -oosIndex)
                                ppl.Add(pplReal[0][i]);
                    }
                    line = zedGraphControl1.GraphPane.AddCurve("", ppl, Color.Cyan);
                    // line.Symbol.IsVisible = false;
                    line.Symbol.Fill.Type = FillType.Solid;
                    //  line.Symbol.Fill.Color = Color.Purple;
                    line.Symbol.Type = SymbolType.Circle;
                    line.Symbol.IsAntiAlias = true;
                    line.Symbol.Size = oosSelectionWidth;
                    line.Line.Width = oosSelectionWidth;
                    line.Line.IsAntiAlias = true;
                    if (oosIndex > 0)
                        line.Tag = oosIndex;
                    else line.Tag = 0;
                }

                LineItem lineW = zedGraphControl1.GraphPane.AddCurve("", pplReal[0], Color.FromArgb(230, 230, 230));
                lineW.Symbol.IsVisible = false;
                lineW.IsVisible = false;
                lineW.Line.Width = 6;
                lineW.Line.IsAntiAlias = true;
                lineW.Tag = 0;
                //lineW.dimatsiDrawMode = 1;

                bahLine = zedGraphControl1.GraphPane.AddCurve("Buy & Hold", BaH, Color.Brown);
                bahLine.Symbol.IsVisible = false;
                bahLine.Line.Width = 2;
                bahLine.Line.IsAntiAlias = true;
                bahLine.IsVisible = false;
                //bahLine.dimatsiDrawMode = 1;
            }

            Color c = Color.FromArgb(70, 0, 130, 255);
            for (int i = 1; i < pplReal.Count && i <= 100; i++)
            {
                LineItem line = zedGraphControl1.GraphPane.AddCurve(i == 1 ? "Real" : "", pplReal[i], Color.FromArgb(70, 0, 80 + (80 * i) / pplReal.Count, 255));
                line.Symbol.IsVisible = false;
                line.Line.Width = 1;
                line.IsVisible = false;
                line.Line.IsAntiAlias = true;
                //line.dimatsiDrawMode = 1;
                linesReal.Add(line);
            }

            c = Color.FromArgb(70, 255, 70, 90);
            for (int i = 0; i < pplRandom.Count && i <= 100; i++)
            {
                LineItem line = zedGraphControl1.GraphPane.AddCurve(i == 0 ? "Random" : "", pplRandom[i], c/*Color.FromArgb(70, c)*/);
                line.Symbol.IsVisible = false;
                line.Line.Width = 1;
                line.IsVisible = false;
                line.Line.IsAntiAlias = true;
                //line.dimatsiDrawMode = 1;
                linesRandom.Add(line);
            }

            //others
            c = Color.FromArgb(255, 255, 70, 90);
            Color c1 = Color.FromArgb(255, 0, 255, 0);
            Color c2 = Color.FromArgb(255, 149, 141, 25);
            Color c3 = Color.FromArgb(255, 218, 111, 91);
            for (int i = 0; i < pplOthers.Count && i <= 100; i++)
            {
                LineItem line = zedGraphControl1.GraphPane.AddCurve(vsLabels[i], pplOthers[i], i == 0 ? c1 : i == 1 ? c2 : i == 2 ? c3 : c);
                line.Symbol.IsVisible = false;
                line.Line.Width = 1.5f;
                line.IsVisible = false;
                line.Line.IsAntiAlias = true;
                //line.dimatsiDrawMode = 1;
                linesOthers.Add(line);
            }

            #region Basket
            if (pplBasket != null && pplBasket.Count > 0)
            {
                Random rnd = new Random(3435);

                for (int i = 0; i < pplBasket.Count; ++i)
                {
                    LineItem lineB = zedGraphControl1.GraphPane.AddCurve(basketLabels[i], pplBasket[i], Color.FromArgb(rnd.Next(255), rnd.Next(255), rnd.Next(255)));
                    lineB.Symbol.IsVisible = false;
                    lineB.Line.Width = 2;
                    lineB.IsVisible = false;
                    lineB.Line.IsAntiAlias = true;
                    //lineB.dimatsiDrawMode = 1;
                    linesBasket.Add(lineB);

                    if (basketOOSindexes[i] != 0)
                    {
                        PointPairList ppl = new PointPairList();
                        if (basketOOSindexes[i] > 0)
                        {   //end
                            for (int j = 0; j < pplBasket[i].Count; j++)
                                if (j >= basketOOSindexes[i])
                                    ppl.Add(pplBasket[i][j]);
                        }
                        else //beginning
                        {
                            for (int j = 0; j < pplBasket[i].Count; j++)
                                if (j <= -basketOOSindexes[i])
                                    ppl.Add(pplBasket[i][j]);
                        }
                        LineItem line = zedGraphControl1.GraphPane.AddCurve("", ppl, Color.Cyan);
                        line.Symbol.Fill.Type = FillType.Solid;
                        line.Symbol.Type = SymbolType.Circle;
                        line.Symbol.IsAntiAlias = true;
                        line.Symbol.Size = oosSelectionWidth;
                        line.Line.Width = oosSelectionWidth;
                        line.IsVisible = false;
                        line.Line.IsAntiAlias = true;
                        if (basketOOSindexes[i] > 0)
                            line.Tag = basketOOSindexes[i];
                        linesBasket.Add(line);
                    }
                }
            }
            #endregion

            zedGraphControl1.AxisChange();
            zedGraphControl1.Refresh();

            zed2.GraphPane.XAxis.Scale.Min = zedGraphControl1.GraphPane.XAxis.Scale.Min;
            zed2.GraphPane.XAxis.Scale.Max = zedGraphControl1.GraphPane.XAxis.Scale.Max;
            zed2.AxisChange();
        }

        private void TurnOnDatesMode()
        {
            // add button for XAxis mode
            llDatesTrades = new LinkLabel
            {
                Text = "Trade number",
                AutoSize = false
            };
            int llHeight = TextRenderer.MeasureText(llDatesTrades.Text, llDatesTrades.Font).Height;
            llDatesTrades.Dock = DockStyle.Bottom;
            llDatesTrades.TextAlign = ContentAlignment.BottomRight;
            llDatesTrades.BackColor = Color.Transparent;
            llDatesTrades.LinkClicked += LlDatesTrades_LinkClicked;
            llDatesTrades.ForeColor = Color.Black;
            llDatesTrades.Padding = new Padding(20, 0, 20, 0);
            llDatesTrades.LinkColor = Color.Black;
            zedGraphControl1.Controls.Add(llDatesTrades);

            zedGraphControl1.GraphPane.Margin.Bottom = llHeight;
            //zedGraphControl1.GraphPane.Fill.Color = Color.WhiteSmoke;
        }

        private void LlDatesTrades_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (sender == null) return;
            LinkLabel ll = sender as LinkLabel;

            if (ll.Text.StartsWith("D")) //Date. Switch to trade
            {
                zedGraphControl1.GraphPane.XAxis.Type = AxisType.Linear;
                if (zed2 != null) zed2.GraphPane.XAxis.Type = AxisType.Linear;
                ll.Text = "Trade number";
            }
            else // Trade number. Switch to date
            {
                zedGraphControl1.GraphPane.XAxis.Type = AxisType.Date;
                if (zed2 != null) zed2.GraphPane.XAxis.Type = AxisType.Date;
                ll.Text = "Date";
            }

            // Update lines data
            if (zedGraphControl1.GraphPane.XAxis.Type == AxisType.Date)
            {
                foreach (CurveItem line in zedGraphControl1.GraphPane.CurveList)
                    for (int i = 0; i < line.Points.Count; i++)
                        line.Points[i].X = line.Points[i].Z;

                if (zed2 != null)
                    foreach (CurveItem line in zed2.GraphPane.CurveList)
                        for (int i = 0; i < line.Points.Count; i++)
                            line.Points[i].X = line.Points[i].Z;
            }
            else
            {
                foreach (CurveItem line in zedGraphControl1.GraphPane.CurveList)
                {
                    int offset = line.Tag != null ? (int)line.Tag : 0;
                    for (int i = 0; i < line.Points.Count; i++)
                        line.Points[i].X = i + offset;
                }

                if (zed2 != null)
                    foreach (CurveItem line in zed2.GraphPane.CurveList)
                    {
                        int offset = line.Tag != null ? (int)line.Tag : 0;
                        for (int i = 0; i < line.Points.Count; i++)
                            line.Points[i].X = i + offset;
                    }
            }
            zedGraphControl1.ZoomOutAll(zedGraphControl1.GraphPane);
            zedGraphControl1.AxisChange();
            zedGraphControl1.Refresh();
            if (zed2 != null)
            {
                zed2.ZoomOutAll(zed2.GraphPane);
                zed2.GraphPane.XAxis.Scale.Min = zedGraphControl1.GraphPane.XAxis.Scale.Min;
                zed2.GraphPane.XAxis.Scale.Max = zedGraphControl1.GraphPane.XAxis.Scale.Max;
                zed2.AxisChange();
                zed2.Refresh();
            }
        }

        /// <summary>
        /// Basic zedGraph control initialization
        /// </summary>
        private void ZedInit()
        {
            zedGraphControl1.GraphPane.IsFontsScaled = false;
            zedGraphControl1.GraphPane.Legend.Border.IsVisible = false;
            zedGraphControl1.GraphPane.Title.IsVisible = false;
            zedGraphControl1.GraphPane.XAxis.MajorGrid.IsVisible = true;
            zedGraphControl1.GraphPane.XAxis.MajorGrid.DashOn = 5;
            zedGraphControl1.GraphPane.XAxis.MajorGrid.Color = Color.LightGray;
            zedGraphControl1.GraphPane.XAxis.Title.IsVisible = false;
            //zedGraphControl1.GraphPane.XAxis.Scale.Min = 0;
            zedGraphControl1.GraphPane.XAxis.Scale.MagAuto = false;
            zedGraphControl1.GraphPane.XAxis.Scale.MinGrace = 0;
            //zedGraphControl1.GraphPane.XAxis.Scale.MaxGrace = 0;
            zedGraphControl1.GraphPane.YAxis.MajorGrid.IsVisible = true;
            zedGraphControl1.GraphPane.YAxis.MajorGrid.DashOn = 5;
            zedGraphControl1.GraphPane.YAxis.MajorGrid.Color = Color.LightGray;
            zedGraphControl1.GraphPane.YAxis.Title.IsVisible = false;
            zedGraphControl1.GraphPane.YAxis.Scale.MagAuto = false;
            zedGraphControl1.GraphPane.BarSettings.MinClusterGap = 0;
            zedGraphControl1.GraphPane.Border.IsVisible = false;

            zedGraphControl1.ModifyContextMenu();

            //zedGraphControl1.GraphPane.Legend.Fill.Type = FillType.None;
            //zedGraphControl1.GraphPane.Chart.Fill.Type = FillType.None;
            //zedGraphControl1.GraphPane.Fill.Color = System.Drawing.Color.Gray;
            //zedGraphControl1.GraphPane.Fill = new Fill(Color.Gray, Color.White, Color.Gray);
        }

        private void CheckBoxReal_CheckedChanged(object sender, EventArgs e)
        {
            if (zedGraphControl1.GraphPane.CurveList.Count == 0) return;

            if (mode == Mode.VarianceTest || mode == Mode.NoiseTest)    //for variance testing and noise test
            {
                zedGraphControl1.GraphPane.CurveList[0].IsVisible = checkBoxReal.Checked;
                zedGraphControl1.GraphPane.CurveList[1].IsVisible = checkBoxReal.Checked;
            }

            if(checkBoxReal.Text[0] == 'M')  // for portfolio equity curve
                if (zedGraphControl1.GraphPane.CurveList.Count > 1 && zedGraphControl1.GraphPane.CurveList[1] != null)
                    zedGraphControl1.GraphPane.CurveList[1].IsVisible = checkBoxReal.Checked;   // for portfolio equity curve

            foreach (LineItem l in linesReal)   // for equity curve
                l.IsVisible = checkBoxReal.Checked;

            zedGraphControl1.AxisChange();
            zedGraphControl1.Refresh();
        }

        private void CheckBoxRandom_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxRandom.Text[0] != 'R')  // for portfolio equity curve
            {
                for (int i = 2; i <= 11 && i < zedGraphControl1.GraphPane.CurveList.Count; i++)
                    zedGraphControl1.GraphPane.CurveList[i].IsVisible = checkBoxRandom.Checked;
            }

            foreach (LineItem l in linesRandom)     // for equity curve
                l.IsVisible = checkBoxRandom.Checked;
            
            zedGraphControl1.AxisChange();
            zedGraphControl1.Refresh();
        }

        private void CheckBoxOriginal_CheckedChanged(object sender, EventArgs e)
        {
            foreach (LineItem l in linesOthers)     // for equity curve
                l.IsVisible = checkBoxOriginal.Checked;
            
            zedGraphControl1.AxisChange();
            zedGraphControl1.Refresh();
        }

        private void Graph_Shown(object sender, EventArgs e)
        {
            bool v = false;
            foreach (Control c in flowLayoutPanel1.Controls)
            {
                if (c.Visible) { v = true; break; }
            }
            if (v == false)
            {
                flowLayoutPanel1.Visible = false;
                zedGraphControl1.Location = new Point(0, 0);
                zedGraphControl1.Height += flowLayoutPanel1.Height;
            }
        }

        private void CheckBoxBaH_CheckedChanged(object sender, EventArgs e)
        {
            if (bahLine != null)
            {
                bahLine.IsVisible = checkBoxBaH.Checked;
                zedGraphControl1.AxisChange();
                zedGraphControl1.Refresh();
            }
            if (bahLine_dd != null)
            {
                bahLine_dd.IsVisible = checkBoxBaH.Checked;
                zed2.GraphPane.XAxis.Scale.Min = zedGraphControl1.GraphPane.XAxis.Scale.Min;
                zed2.GraphPane.XAxis.Scale.Max = zedGraphControl1.GraphPane.XAxis.Scale.Max;
                zed2.AxisChange();
                zed2.Refresh();
            }
        }

        private void CheckBoxDD_CheckedChanged(object sender, EventArgs e)
        {
            if (zed2 == null) return;

            if (!checkBoxDD.Checked) {
                //zed2.Hide();
                //tableLayoutPanel1.RowStyles[2].SizeType = SizeType.AutoSize;
                splitContainer1.Panel2Collapsed = true;
             }
            else {
                //zed2.Visible = true;
                //tableLayoutPanel1.RowStyles[2].SizeType = SizeType.Percent;
                //tableLayoutPanel1.RowStyles[2].Height = 100;
                splitContainer1.Panel2Collapsed = false;
                //splitContainer1.SplitterDistance = splitContainer1.Height * 0.8;
            }
        }

        private void CheckBoxBasket_CheckedChanged(object sender, EventArgs e)
        {
            //hide original with highlighting
            foreach (CurveItem c in zedGraphControl1.GraphPane.CurveList.Where(x => x.Tag != null))
                c.IsVisible = !checkBoxBasket.Checked;

            foreach (LineItem l in linesBasket)
                l.IsVisible = checkBoxBasket.Checked;

            zedGraphControl1.AxisChange();
            zedGraphControl1.Refresh();
        }

        public static Color backColorDark = Color.FromArgb(45, 45, 48);
        public static Color backColorLight = SystemColors.Window;

        public static Color backColor2Dark = Color.FromArgb(37, 37, 38);
        public static Color backColor2Light = SystemColors.Window;

        public static Color foreColorDark = Color.White;
        public static Color foreColorLight = Color.Black;

        public static Color accentColorDark = Color.FromArgb(0,122,204);
        public static Color accentColorLight = SystemColors.GradientActiveCaption;

        public void UpdateTheme(string themeName)
        {
            ZedThemeUpdate(zedGraphControl1, themeName);
            if (zed2 != null)
                ZedThemeUpdate(zed2, themeName);

            if (themeName.Contains("Dark"))
            {
                BackColor = backColorDark;
                splitContainer1.BackColor = Color.FromArgb(60,60,60);

                checkBoxBaH.FlatAppearance.CheckedBackColor = accentColorDark;
                checkBoxBasket.FlatAppearance.CheckedBackColor = accentColorDark;
                checkBoxDD.FlatAppearance.CheckedBackColor = accentColorDark;
                checkBoxOriginal.FlatAppearance.CheckedBackColor = accentColorDark;
                checkBoxRandom.FlatAppearance.CheckedBackColor = accentColorDark;
                checkBoxReal.FlatAppearance.CheckedBackColor = accentColorDark;

                if (llDatesTrades != null)
                {
                    llDatesTrades.ForeColor = foreColorDark;
                    llDatesTrades.LinkColor = foreColorDark;
                }
            }
            else
            {
                BackColor = backColorLight;
                splitContainer1.BackColor = SystemColors.ControlLight;

                checkBoxBaH.FlatAppearance.CheckedBackColor = accentColorLight;
                checkBoxBasket.FlatAppearance.CheckedBackColor = accentColorLight;
                checkBoxDD.FlatAppearance.CheckedBackColor = accentColorLight;
                checkBoxOriginal.FlatAppearance.CheckedBackColor = accentColorLight;
                checkBoxRandom.FlatAppearance.CheckedBackColor = accentColorLight;
                checkBoxReal.FlatAppearance.CheckedBackColor = accentColorLight;

                if (llDatesTrades != null)
                {
                    llDatesTrades.ForeColor = foreColorLight;
                    llDatesTrades.LinkColor = foreColorLight;
                }
            }
        }

        public static void ZedThemeUpdate(ZedGraphControl zed, string themeName)
        {
            var pane = zed.GraphPane;

            if (themeName.Contains("Dark"))
            {
                pane.Chart.Border.Color = Color.LightGray;

                pane.Fill.Color = backColorDark;

                pane.Chart.Fill.Type = FillType.Solid;
                pane.Chart.Fill.Color = backColor2Dark;

                // Установим цвет осей
                pane.XAxis.Color = Color.LightGray;
                pane.YAxis.Color = Color.LightGray;
                pane.Y2Axis.Color = Color.LightGray;

                // Установим цвет для сетки
                pane.XAxis.MajorGrid.Color = Color.Gray;
                pane.YAxis.MajorGrid.Color = Color.Gray;
                pane.Y2Axis.MajorGrid.Color = Color.Gray;

                zed.GraphPane.XAxis.MajorTic.Color = Color.LightGray;
                zed.GraphPane.XAxis.MinorTic.Color = Color.LightGray;
                zed.GraphPane.YAxis.MajorTic.Color = Color.LightGray;
                zed.GraphPane.YAxis.MinorTic.Color = Color.LightGray;
                zed.GraphPane.Y2Axis.MajorTic.Color = Color.LightGray;
                zed.GraphPane.Y2Axis.MinorTic.Color = Color.LightGray;

                // Установим цвет для подписей рядом с осями
                pane.XAxis.Title.FontSpec.FontColor = foreColorDark;
                pane.YAxis.Title.FontSpec.FontColor = foreColorDark;
                pane.Y2Axis.Title.FontSpec.FontColor = foreColorDark;

                // Установим цвет подписей под метками
                pane.XAxis.Scale.FontSpec.FontColor = foreColorDark;
                pane.YAxis.Scale.FontSpec.FontColor = foreColorDark;
                pane.Y2Axis.Scale.FontSpec.FontColor = foreColorDark;

                // Установим цвет заголовка над графиком
                pane.Title.FontSpec.FontColor = foreColorDark;
            }
            else
            {
                pane.Chart.Border.Color = Color.Black;

                pane.Fill.Color = backColorLight;

                pane.Chart.Fill.Type = FillType.Solid;
                pane.Chart.Fill.Color = backColor2Light;

                // Установим цвет осей
                pane.XAxis.Color = Color.Black;
                pane.YAxis.Color = Color.Black;
                pane.Y2Axis.Color = Color.Black;

                // Установим цвет для сетки
                pane.XAxis.MajorGrid.Color = Color.LightGray;
                pane.YAxis.MajorGrid.Color = Color.LightGray;
                pane.Y2Axis.MajorGrid.Color = Color.LightGray;

                zed.GraphPane.XAxis.MajorTic.Color = Color.Black;
                zed.GraphPane.XAxis.MinorTic.Color = Color.Black;
                zed.GraphPane.YAxis.MajorTic.Color = Color.Black;
                zed.GraphPane.YAxis.MinorTic.Color = Color.Black;
                zed.GraphPane.Y2Axis.MajorTic.Color = Color.Black;
                zed.GraphPane.Y2Axis.MinorTic.Color = Color.Black;

                // Установим цвет для подписей рядом с осями
                pane.XAxis.Title.FontSpec.FontColor = foreColorLight;
                pane.YAxis.Title.FontSpec.FontColor = foreColorLight;
                pane.Y2Axis.Title.FontSpec.FontColor = foreColorLight;

                // Установим цвет подписей под метками
                pane.XAxis.Scale.FontSpec.FontColor = foreColorLight;
                pane.YAxis.Scale.FontSpec.FontColor = foreColorLight;
                pane.Y2Axis.Scale.FontSpec.FontColor = foreColorLight;

                // Установим цвет заголовка над графиком
                pane.Title.FontSpec.FontColor = foreColorLight;
            }

            zed.Refresh();
        }
    }
}
