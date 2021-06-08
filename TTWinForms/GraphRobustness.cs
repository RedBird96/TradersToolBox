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
    public partial class GraphRobustness : UserControl, IThemedGraph
    {
        readonly List<PointPairList> lines = new List<PointPairList>();
        readonly List<string> labels = new List<string>();
        readonly Random random = new Random(23245);

        public GraphRobustness(List<PointPairList> _lines, List<string> _labels, List<string> newsEvents, bool isRandom)
        {
            InitializeComponent();

            ZedInit();
            lines = _lines; labels = _labels;

            if (isRandom)
            {
                flowLayoutPanel1.Visible = false;
                checkBoxRandom.Checked = true;
            }
            else {
                comboBoxMeta.SelectedIndex = 0;
                comboBoxSeasonality.SelectedIndex = 0;
                checkBoxRandom.Visible = false;
                zedGraphControl2.Visible = false;
                zedGraphControl3.Visible = false;
                tableLayoutPanel1.ColumnStyles[1].SizeType = SizeType.Absolute;
                tableLayoutPanel1.ColumnStyles[1].Width = 0;
                tableLayoutPanel1.ColumnStyles[2].SizeType = SizeType.Absolute;
                tableLayoutPanel1.ColumnStyles[2].Width = 0;

                checkBoxEntries.Enabled   = _labels.Any(s => s[0] == '1');
                panelMeta.Enabled         = _labels.Any(s => s[0] == '2');
                checkBoxSlippage.Enabled  = _labels.Any(s => s[0] == '3');
                checkBoxLiquidity.Enabled = _labels.Any(s => s[0] == '4');
                panelSeasonality.Enabled  = _labels.Any(s => s[0] == '6');
                panelNews.Enabled         = _labels.Any(s => s[0] == '7');

                if (panelMeta.Enabled) checkBoxMeta.Checked = true;
                else if (checkBoxEntries.Enabled) checkBoxEntries.Checked = true;
                else if (checkBoxSlippage.Enabled) checkBoxSlippage.Checked = true;
                else if (checkBoxLiquidity.Enabled) checkBoxLiquidity.Checked = true;
                else if (panelSeasonality.Enabled) checkBoxSeasonality.Checked = true;
                else if (panelNews.Enabled) checkBoxNews.Checked = true;

                if (newsEvents != null && newsEvents.Count > 0)
                {
                    foreach (string s in newsEvents)
                        comboBoxNews.Items.Add(s);
                    comboBoxNews.SelectedIndex = 0;
                    comboBoxNews.DropDownWidth *= 2;
                }
            }

            /*
            {   // Monte Carlo Randomized

                Color[] colors = { Color.FromArgb(0, 255, 255, 255), Color.FromArgb(255, 255, 255, 255), Color.FromArgb(100, 255, 255, 255) };
                float[] positions = { 0, 0.01f, 1 };
                zedGraphControl1.GraphPane.Legend.Fill = new Fill(colors, positions);
                zedGraphControl1.GraphPane.Legend.Position = LegendPos.InsideTopLeft;
                zedGraphControl1.GraphPane.Margin.Top = checkBoxSlippage.Height * 1.4f;

                int count = (lines.Count - 11) / 2;

                for (int i = 1; i < 11; i++)
                {
                    LineItem line = zedGraphControl1.GraphPane.AddCurve(i.ToString(), lines[i],
                        Color.FromArgb(random.Next(80, 255), random.Next(50, 255), random.Next(30, 255)));
                    line.Symbol.IsVisible = false;
                    line.Line.Width = 1;
                    line.IsVisible = false;
                    line.Line.IsAntiAlias = true;
                }
                for (int i = 11 + count - 1, j = 0; i >= 11 && j < 250; i--, j++)
                {
                    LineItem line = zedGraphControl1.GraphPane.AddCurve(j == 0 ? "Random" : "", lines[i],
                        Color.FromArgb(random.Next(80, 255), random.Next(50, 255), random.Next(30, 255)));
                    line.Symbol.IsVisible = false;
                    line.Line.Width = 1;
                    line.IsVisible = false;
                    line.Line.IsAntiAlias = true;
                }
                for (int i = 11 + count, j = 0; i < lines.Count && j < 250; i++, j++)
                {
                    LineItem line = zedGraphControl1.GraphPane.AddCurve("", lines[i],
                        Color.FromArgb(random.Next(80, 255), random.Next(50, 255), random.Next(30, 255)));
                    line.Symbol.IsVisible = false;
                    line.Line.Width = 1;
                    line.Line.IsAntiAlias = true;
                }
            }
          
            {   // Variance testing
                for (int i = 1; i < lines.Count && i < 250; i++)
                {
                    LineItem line = zedGraphControl1.GraphPane.AddCurve(i == 1 ? "Random" : "", lines[i],
                        Color.FromArgb(random.Next(80, 255), random.Next(50, 255), random.Next(30, 255)));
                    line.Symbol.IsVisible = false;
                    line.Line.Width = 1;
                    line.Line.IsAntiAlias = true;
                }
            }*/
        }

        /// <summary>
        /// Basic zedGraph control initialization
        /// </summary>
        private void ZedInit()
        {
            zedGraphControl1.GraphPane.IsFontsScaled = false;
            zedGraphControl1.GraphPane.Legend.Border.IsVisible = false;
            zedGraphControl1.GraphPane.Legend.Fill = new Fill(Color.FromArgb(127, 255, 255, 255));
            zedGraphControl1.GraphPane.Title.IsVisible = false;
            zedGraphControl1.GraphPane.XAxis.MajorGrid.IsVisible = true;
            zedGraphControl1.GraphPane.XAxis.MajorGrid.DashOn = 5;
            zedGraphControl1.GraphPane.XAxis.MajorGrid.Color = Color.LightGray;
            zedGraphControl1.GraphPane.XAxis.Title.IsVisible = false;
            zedGraphControl1.GraphPane.XAxis.Scale.Min = 0;
            zedGraphControl1.GraphPane.XAxis.Scale.MagAuto = false;
            zedGraphControl1.GraphPane.YAxis.MajorGrid.IsVisible = true;
            zedGraphControl1.GraphPane.YAxis.MajorGrid.DashOn = 5;
            zedGraphControl1.GraphPane.YAxis.MajorGrid.Color = Color.LightGray;
            zedGraphControl1.GraphPane.YAxis.Title.IsVisible = false;
            zedGraphControl1.GraphPane.YAxis.Scale.MagAuto = false;
            zedGraphControl1.GraphPane.BarSettings.MinClusterGap = 0;
            zedGraphControl1.GraphPane.Border.IsVisible = false;
            zedGraphControl1.GraphPane.Margin.All = 0;
            zedGraphControl1.ModifyContextMenu();

            //zedGraphControl1.GraphPane.Legend.Fill.Type = FillType.None;
            //zedGraphControl1.GraphPane.Chart.Fill.Type = FillType.None;
            //zedGraphControl1.GraphPane.Fill.Color = System.Drawing.Color.Gray;
            //zedGraphControl1.GraphPane.Fill = new Fill(Color.Gray, Color.White, Color.Gray);

            zedGraphControl2.GraphPane.IsFontsScaled = false;
            zedGraphControl2.GraphPane.Legend.Border.IsVisible = false;
            zedGraphControl2.GraphPane.Legend.Fill = new Fill(Color.FromArgb(127, 255, 255, 255));
            zedGraphControl2.GraphPane.Title.IsVisible = false;
            zedGraphControl2.GraphPane.XAxis.MajorGrid.IsVisible = true;
            zedGraphControl2.GraphPane.XAxis.MajorGrid.DashOn = 5;
            zedGraphControl2.GraphPane.XAxis.MajorGrid.Color = Color.LightGray;
            zedGraphControl2.GraphPane.XAxis.Title.IsVisible = false;
            zedGraphControl2.GraphPane.XAxis.Scale.Min = 0;
            zedGraphControl2.GraphPane.XAxis.Scale.MagAuto = false;
            zedGraphControl2.GraphPane.YAxis.MajorGrid.IsVisible = true;
            zedGraphControl2.GraphPane.YAxis.MajorGrid.DashOn = 5;
            zedGraphControl2.GraphPane.YAxis.MajorGrid.Color = Color.LightGray;
            zedGraphControl2.GraphPane.YAxis.Title.IsVisible = false;
            zedGraphControl2.GraphPane.YAxis.Scale.MagAuto = false;
            zedGraphControl2.GraphPane.BarSettings.MinClusterGap = 0;
            zedGraphControl2.GraphPane.Border.IsVisible = false;
            zedGraphControl2.GraphPane.Margin.All = 0;
            zedGraphControl2.ModifyContextMenu();

            zedGraphControl3.GraphPane.IsFontsScaled = false;
            zedGraphControl3.GraphPane.Legend.Border.IsVisible = false;
            zedGraphControl3.GraphPane.Legend.Fill = new Fill(Color.FromArgb(127, 255, 255, 255));
            zedGraphControl3.GraphPane.Title.IsVisible = false;
            zedGraphControl3.GraphPane.XAxis.MajorGrid.IsVisible = true;
            zedGraphControl3.GraphPane.XAxis.MajorGrid.DashOn = 5;
            zedGraphControl3.GraphPane.XAxis.MajorGrid.Color = Color.LightGray;
            zedGraphControl3.GraphPane.XAxis.Title.IsVisible = false;
            zedGraphControl3.GraphPane.XAxis.Scale.Min = 0;
            zedGraphControl3.GraphPane.XAxis.Scale.MagAuto = false;
            zedGraphControl3.GraphPane.YAxis.MajorGrid.IsVisible = true;
            zedGraphControl3.GraphPane.YAxis.MajorGrid.DashOn = 5;
            zedGraphControl3.GraphPane.YAxis.MajorGrid.Color = Color.LightGray;
            zedGraphControl3.GraphPane.YAxis.Title.IsVisible = false;
            zedGraphControl3.GraphPane.YAxis.Scale.MagAuto = false;
            zedGraphControl3.GraphPane.BarSettings.MinClusterGap = 0;
            zedGraphControl3.GraphPane.Border.IsVisible = false;
            zedGraphControl3.GraphPane.Margin.All = 0;
            zedGraphControl3.ModifyContextMenu();
        }

        private void CheckBoxRandomOld_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxLiquidity.Text[0] != 'R')  // for MC Randomized and portfolio equity curve
            {
                for (int i = 2; i <= 11 && i < zedGraphControl1.GraphPane.CurveList.Count; i++)
                    zedGraphControl1.GraphPane.CurveList[i].IsVisible = checkBoxLiquidity.Checked;
            }
        }

        private void CheckBoxRandom_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxRandom.Checked)
                checkBoxEntries.Checked = checkBoxLiquidity.Checked = checkBoxMeta.Checked = checkBoxSlippage.Checked = checkBoxSeasonality.Checked = checkBoxNews.Checked= false;
            else { /*checkBoxRandom.Checked = true;*/ return; }

           /* tableLayoutPanel1.ColumnStyles[1].SizeType = SizeType.Percent;
            tableLayoutPanel1.ColumnStyles[1].Width = 33;
            tableLayoutPanel1.ColumnStyles[2].SizeType = SizeType.Percent;
            tableLayoutPanel1.ColumnStyles[2].Width = 33;
            zedGraphControl2.Visible = true;
            zedGraphControl3.Visible = true;*/

            zedGraphControl1.GraphPane.CurveList.Clear();
            zedGraphControl2.GraphPane.CurveList.Clear();
            zedGraphControl3.GraphPane.CurveList.Clear();

            for (int i = 0; i < lines.Count; i++)
                if (labels[i][0] == '5')
                {
                    string name = labels[i].Substring(2);
                    Color c = Color.FromArgb(random.Next(80, 255), random.Next(50, 255), random.Next(30, 255));
                    LineItem line;
                    if (labels[i][1] == '1')
                    {
                        line = zedGraphControl1.GraphPane.AddCurve(name, lines[i], name.StartsWith("Orig") ? Color.Blue : c);
                    }
                    else if (labels[i][1] == '2')
                    {
                        line = zedGraphControl2.GraphPane.AddCurve(name, lines[i], name.StartsWith("Orig") ? Color.Blue : c);
                    }
                    else
                    {
                        line = zedGraphControl3.GraphPane.AddCurve(name, lines[i], name.StartsWith("Orig") ? Color.Blue : c);
                    }
                    line.Symbol.IsVisible = false;
                    line.Line.Width = name.StartsWith("Orig") ? 3 : 1;
                    line.Line.IsAntiAlias = true;
                }

            ZoomResetUpdate();
            zedGraphControl2.ZoomOutAll(zedGraphControl2.GraphPane);
            zedGraphControl2.AxisChange();
            zedGraphControl2.Refresh();
            zedGraphControl3.ZoomOutAll(zedGraphControl3.GraphPane);
            zedGraphControl3.AxisChange();
            zedGraphControl3.Refresh();
        }

        private void CheckBoxLiquidity_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxLiquidity.Checked)
                checkBoxEntries.Checked = checkBoxRandom.Checked = checkBoxMeta.Checked = checkBoxSlippage.Checked = checkBoxSeasonality.Checked = checkBoxNews.Checked= false;
            else { /*checkBoxLiquidity.Checked = true;*/ return; }

           /* tableLayoutPanel1.ColumnStyles[1].SizeType = SizeType.Absolute;
            tableLayoutPanel1.ColumnStyles[1].Width = 0;
            tableLayoutPanel1.ColumnStyles[2].SizeType = SizeType.Absolute;
            tableLayoutPanel1.ColumnStyles[2].Width = 0;
            zedGraphControl2.Visible = false;
            zedGraphControl3.Visible = false;*/

            zedGraphControl1.GraphPane.CurveList.Clear();

            for (int i = 0; i < lines.Count; i++)
                if (labels[i][0] == '4')
                {
                    string name = labels[i].Substring(1);
                    Color c = Color.FromArgb(random.Next(80, 255), random.Next(50, 255), random.Next(30, 255));
                    LineItem line = zedGraphControl1.GraphPane.AddCurve(name, lines[i], name == "Original" ? Color.Blue : c);
                    line.Symbol.IsVisible = false;
                    line.Line.Width = name == "Original" ? 3 : 1;
                    line.Line.IsAntiAlias = true;
                }

            ZoomResetUpdate();
        }

        private void CheckBoxSlippage_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxSlippage.Checked)
                checkBoxEntries.Checked = checkBoxLiquidity.Checked = checkBoxMeta.Checked = checkBoxRandom.Checked = checkBoxSeasonality.Checked = checkBoxNews.Checked= false;
            else { /*checkBoxSlippage.Checked = true;*/ return; }

           /* tableLayoutPanel1.ColumnStyles[1].SizeType = SizeType.Absolute;
            tableLayoutPanel1.ColumnStyles[1].Width = 0;
            tableLayoutPanel1.ColumnStyles[2].SizeType = SizeType.Absolute;
            tableLayoutPanel1.ColumnStyles[2].Width = 0;
            zedGraphControl2.Visible = false;
            zedGraphControl3.Visible = false;*/

            zedGraphControl1.GraphPane.CurveList.Clear();

            for (int i = 0; i < lines.Count; i++)
                if (labels[i][0] == '3')
                {
                    string name = labels[i].Substring(1);
                    Color c = Color.FromArgb(random.Next(80, 255), random.Next(50, 255), random.Next(30, 255));
                    LineItem line = zedGraphControl1.GraphPane.AddCurve(name, lines[i], name == "Original" ? Color.Blue : c);
                    line.Symbol.IsVisible = false;
                    line.Line.Width = name == "Original" ? 3 : 1;
                    line.Line.IsAntiAlias = true;
                }

            ZoomResetUpdate();
        }

        private void CheckBoxEntries_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxEntries.Checked)
                checkBoxRandom.Checked = checkBoxLiquidity.Checked = checkBoxMeta.Checked = checkBoxSlippage.Checked = checkBoxSeasonality.Checked = checkBoxNews.Checked= false;
            else { /*checkBoxEntries.Checked = true;*/ return; }

           /* tableLayoutPanel1.ColumnStyles[1].SizeType = SizeType.Absolute;
            tableLayoutPanel1.ColumnStyles[1].Width = 0;
            tableLayoutPanel1.ColumnStyles[2].SizeType = SizeType.Absolute;
            tableLayoutPanel1.ColumnStyles[2].Width = 0;
            zedGraphControl2.Visible = false;
            zedGraphControl3.Visible = false;*/

            zedGraphControl1.GraphPane.CurveList.Clear();

            for (int i = 0; i < lines.Count; i++)
                if (labels[i][0] == '1')
                {
                    string name = labels[i].Substring(1);
                    Color c = Color.FromArgb(random.Next(80, 255), random.Next(50, 255), random.Next(30, 255));
                    LineItem line = zedGraphControl1.GraphPane.AddCurve(name, lines[i], name == "Original" ? Color.Blue : c);
                    line.Symbol.IsVisible = false;
                    line.Line.Width = name == "Original" ? 3 : 1;
                    line.Line.IsAntiAlias = true;
                }

            ZoomResetUpdate();
        }

        private void CheckBoxMeta_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxMeta.Checked)
                checkBoxEntries.Checked = checkBoxLiquidity.Checked = checkBoxRandom.Checked = checkBoxSlippage.Checked = checkBoxSeasonality.Checked = checkBoxNews.Checked= false;
            else { /*checkBoxMeta.Checked = true;*/ return; }

          /*  tableLayoutPanel1.ColumnStyles[1].SizeType = SizeType.Absolute;
            tableLayoutPanel1.ColumnStyles[1].Width = 0;
            tableLayoutPanel1.ColumnStyles[2].SizeType = SizeType.Absolute;
            tableLayoutPanel1.ColumnStyles[2].Width = 0;
            zedGraphControl2.Visible = false;
            zedGraphControl3.Visible = false;*/

            zedGraphControl1.GraphPane.CurveList.Clear();

            for (int i = 0; i < lines.Count; i++)
                if (labels[i][0] == '2')
                {
                    if (labels[i][1] == '0' || labels[i][1] == (comboBoxMeta.SelectedIndex + 1).ToString()[0])
                    {
                        string name = labels[i].Substring(2);
                        Color c = Color.FromArgb(random.Next(80, 255), random.Next(50, 255), random.Next(30, 255));
                        LineItem line = zedGraphControl1.GraphPane.AddCurve(name, lines[i], name == "Original" ? Color.Blue : c);
                        line.Symbol.IsVisible = false;
                        line.Line.Width = name == "Original" ? 3 : 1;
                        line.Line.IsAntiAlias = true;
                    }
                }

            ZoomResetUpdate();
        }

        void ZoomResetUpdate()
        {
            zedGraphControl1.ZoomOutAll(zedGraphControl1.GraphPane);
            zedGraphControl1.AxisChange();
            zedGraphControl1.Refresh();
        }

        private void ComboBoxMeta_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (checkBoxMeta.Checked)
                CheckBoxMeta_CheckedChanged(null, null);
        }

        private void ComboBoxSeasonality_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (checkBoxSeasonality.Checked)
                CheckBoxSeasonality_CheckedChanged(null, null);
        }

        private void ComboBoxNews_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (checkBoxNews.Checked)
                CheckBoxNews_CheckedChanged(null, null);
        }

        private void CheckBoxSeasonality_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxSeasonality.Checked)
                checkBoxEntries.Checked = checkBoxLiquidity.Checked = checkBoxRandom.Checked = checkBoxSlippage.Checked = checkBoxMeta.Checked = checkBoxNews.Checked= false;
            else { /*checkBoxSeasonality.Checked = true;*/ return; }

            zedGraphControl1.GraphPane.CurveList.Clear();

            for (int i = 0; i < lines.Count; i++)
                if (labels[i][0] == '6')
                {
                    if (labels[i][1] == '0' || labels[i][1] == (comboBoxSeasonality.SelectedIndex + 1).ToString()[0])
                    {
                        string name = labels[i].Substring(2);
                        Color c = Color.FromArgb(random.Next(80, 255), random.Next(50, 255), random.Next(30, 255));
                        LineItem line = zedGraphControl1.GraphPane.AddCurve(name, lines[i], name == "Original" ? Color.Blue : c);
                        line.Symbol.IsVisible = false;
                        line.Line.Width = name == "Original" ? 3 : 1;
                        line.Line.IsAntiAlias = true;
                    }
                }

            ZoomResetUpdate();
        }

        private void CheckBoxNews_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxNews.Checked)
                checkBoxEntries.Checked = checkBoxLiquidity.Checked = checkBoxRandom.Checked = checkBoxSlippage.Checked = checkBoxMeta.Checked = checkBoxSeasonality.Checked = false;
            else { /*checkBoxSeasonality.Checked = true;*/ return; }

            zedGraphControl1.GraphPane.CurveList.Clear();

            for (int i = 0; i < lines.Count; i++)
                if (labels[i][0] == '7')
                {
                    string s = labels[i].Substring(1, 2);
                    if (s == "00" || s == (comboBoxNews.SelectedIndex + 1).ToString("D2"))
                    {
                        string name = labels[i].Substring(3);
                        Color c = Color.FromArgb(random.Next(80, 255), random.Next(50, 255), random.Next(30, 255));
                        LineItem line = zedGraphControl1.GraphPane.AddCurve(name, lines[i], name == "Original" ? Color.Blue : c);
                        line.Symbol.IsVisible = false;
                        line.Line.Width = name == "Original" ? 3 : 1;
                        line.Line.IsAntiAlias = true;
                    }
                }

            ZoomResetUpdate();
        }

        public void UpdateTheme(string themeName)
        {
            Graph.ZedThemeUpdate(zedGraphControl1, themeName);
            Graph.ZedThemeUpdate(zedGraphControl2, themeName);
            Graph.ZedThemeUpdate(zedGraphControl3, themeName);

            if (themeName.Contains("Dark"))
            {
                BackColor = Graph.backColorDark;

                checkBoxEntries.FlatAppearance.CheckedBackColor = Graph.accentColorDark;
                checkBoxLiquidity.FlatAppearance.CheckedBackColor = Graph.accentColorDark;
                checkBoxMeta.FlatAppearance.CheckedBackColor = Graph.accentColorDark;
                checkBoxNews.FlatAppearance.CheckedBackColor = Graph.accentColorDark;
                checkBoxRandom.FlatAppearance.CheckedBackColor = Graph.accentColorDark;
                checkBoxSeasonality.FlatAppearance.CheckedBackColor = Graph.accentColorDark;
                checkBoxSlippage.FlatAppearance.CheckedBackColor = Graph.accentColorDark;

                comboBoxMeta.BackColor = Graph.backColor2Dark;
                comboBoxMeta.ForeColor = Graph.foreColorDark;
                comboBoxNews.BackColor = Graph.backColor2Dark;
                comboBoxNews.ForeColor = Graph.foreColorDark;
                comboBoxSeasonality.BackColor = Graph.backColor2Dark;
                comboBoxSeasonality.ForeColor = Graph.foreColorDark;
            }
            else
            {
                BackColor = Graph.backColorLight;

                checkBoxEntries.FlatAppearance.CheckedBackColor = Graph.accentColorLight;
                checkBoxLiquidity.FlatAppearance.CheckedBackColor = Graph.accentColorLight;
                checkBoxMeta.FlatAppearance.CheckedBackColor = Graph.accentColorLight;
                checkBoxNews.FlatAppearance.CheckedBackColor = Graph.accentColorLight;
                checkBoxRandom.FlatAppearance.CheckedBackColor = Graph.accentColorLight;
                checkBoxSeasonality.FlatAppearance.CheckedBackColor = Graph.accentColorLight;
                checkBoxSlippage.FlatAppearance.CheckedBackColor = Graph.accentColorLight;

                comboBoxMeta.BackColor = Graph.backColor2Light;
                comboBoxMeta.ForeColor = Graph.foreColorLight;
                comboBoxNews.BackColor = Graph.backColor2Light;
                comboBoxNews.ForeColor = Graph.foreColorLight;
                comboBoxSeasonality.BackColor = Graph.backColor2Light;
                comboBoxSeasonality.ForeColor = Graph.foreColorLight;
            }
        }

        private void zedGraphControl1_MouseClick(object sender, MouseEventArgs e)
        {
            GraphPane graphingPane = zedGraphControl1.GraphPane;

            object clickedObject;
            int index = int.MinValue;
            Graphics g = this.CreateGraphics();
            LineItem nearestCurve = null;

            bool isObjectFound = graphingPane.FindNearestObject(e.Location, g, out clickedObject, out index);
            if (isObjectFound)
            {
                if (clickedObject is Legend)
                    {
                        Legend legend = graphingPane.Legend;
                        if (legend != null && index >= 0)
                        {
                            int index_cur;

                            ((LineItem)graphingPane.CurveList.ElementAt(0)).Line.Width = 3;
                            for (index_cur = 1; index_cur < graphingPane.CurveList.Count; index_cur++)
                            {
                                ((LineItem)graphingPane.CurveList.ElementAt(index_cur)).Line.Width = 1;
                            }
                            nearestCurve = (LineItem)graphingPane.CurveList[index];
                            nearestCurve.Line.Width = index == 0? 7 : 5;
                        }
                    }
            }
            ZoomResetUpdate();
        }
    }
}
