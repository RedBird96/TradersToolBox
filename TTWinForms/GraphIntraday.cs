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
    public partial class GraphIntraday : UserControl, IThemedGraph
    {
        public int STRtype = 0;
        public int STRindex = -1;
        public int selectedIndex = -1;

        public List<int[]> Args = null;
        private readonly List<string> Labels = null;

        public GraphIntraday(List<PointPairList> lines, List<string> labels, List<int[]> args)
        {
            InitializeComponent();

            Random random = new Random(6548);

            Labels = labels;
            Args = args;

            ZedInit();

            zedGraphControl1.IsShowPointValues = true;
            

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
            {   //Plotting statistic lines
                LineItem line = zedGraphControl2.GraphPane.AddCurve("Master P&L", lines[1], Color.Blue);
                line.Symbol.IsVisible = false;
                line.Line.Width = 2;
                line.Line.IsAntiAlias = true;
                line.Line.Fill = new Fill(Color.FromArgb(100, line.Line.Color));
                LineItem line2 = zedGraphControl3.GraphPane.AddCurve("Master DD", lines[2], Color.Red);
                line2.Symbol.IsVisible = false;
                line2.Line.Width = 2;
                line2.Line.IsAntiAlias = true;
                line2.Line.Fill = new Fill(Color.FromArgb(100, line2.Line.Color));
                LineItem line3 = zedGraphControl4.GraphPane.AddCurve("Master Average", lines[3], Color.Green);
                line3.Symbol.IsVisible = false;
                line3.Line.Width = 2;
                line3.Line.IsAntiAlias = true;
                line3.Line.Fill = new Fill(Color.FromArgb(100, line3.Line.Color));
            }
            {   // Intraday Testing
                for (int i = 4; i < lines.Count && i < 250; i++)
                {
                    LineItem line = zedGraphControl1.GraphPane.AddCurve(i == 1 ? "Intraday" : "", lines[i],
                        Color.FromArgb(random.Next(80, 255), random.Next(50, 255), random.Next(30, 255)));
                    line.Symbol.IsVisible = false;
                    line.Line.Width = 1;
                    line.Line.IsAntiAlias = true;
                    line.Tag = i - 4;
                }
            }

            zedGraphControl1.AxisChange();
            zedGraphControl1.Refresh();
            zedGraphControl2.AxisChange();
            zedGraphControl2.Refresh();
            zedGraphControl3.AxisChange();
            zedGraphControl3.Refresh();
            zedGraphControl4.AxisChange();
            zedGraphControl4.Refresh();
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
            zedGraphControl1.GraphPane.Border.IsVisible = false;
            zedGraphControl1.ModifyContextMenu();

            List<ZedGraphControl> zedGraphs = new List<ZedGraphControl>
            {
                zedGraphControl2,
                zedGraphControl3,
                zedGraphControl4
            };

            zedGraphControl2.GraphPane.Title.Text = "P&L";
            zedGraphControl3.GraphPane.Title.Text = "Drawdown";
            zedGraphControl4.GraphPane.Title.Text = "Average";

            foreach (ZedGraphControl zed in zedGraphs)
            {
                zed.GraphPane.Margin.All = 1;
                zed.GraphPane.Margin.Bottom = 2;
                zed.GraphPane.IsFontsScaled = false;
                zed.GraphPane.Legend.IsVisible = false;
                zed.GraphPane.Title.FontSpec.Size = 12;
                zed.GraphPane.XAxis.Scale.FontSpec.Size = 11;
                zed.GraphPane.XAxis.MajorGrid.IsVisible = true;
                zed.GraphPane.XAxis.MajorGrid.DashOn = 5;
                zed.GraphPane.XAxis.MajorGrid.Color = Color.LightGray;
                zed.GraphPane.XAxis.Title.IsVisible = false;
                //zed.GraphPane.XAxis.Scale.Min = 0;
                zed.GraphPane.XAxis.Scale.MagAuto = false;
                zed.GraphPane.XAxis.MajorTic.Size = 2;
                zed.GraphPane.XAxis.MinorTic.Size = 1;
                zed.GraphPane.YAxis.MajorGrid.IsVisible = true;
                zed.GraphPane.YAxis.MajorGrid.DashOn = 5;
                zed.GraphPane.YAxis.MajorGrid.Color = Color.LightGray;
                zed.GraphPane.YAxis.Title.IsVisible = false;
                zed.GraphPane.YAxis.Scale.MagAuto = false;
                zed.GraphPane.YAxis.Scale.FontSpec.Size = 11;
                zed.GraphPane.YAxis.MajorTic.Size = 2;
                zed.GraphPane.YAxis.MinorTic.Size = 1;
                zed.GraphPane.Border.IsVisible = false;
            }
            zedGraphControl2.GraphPane.Margin.Left = 3;
            zedGraphControl4.GraphPane.Margin.Right = 3;
        }
        

        private string ZedGraphControl1_PointValueEvent(ZedGraphControl sender, GraphPane pane, CurveItem curve, int iPt)
        {
            if (curve.Tag != null)
                return Labels[(int)curve.Tag];
            else
                return String.Format("({0:F3}, {1:F3})", curve.Points[iPt].X, curve.Points[iPt].Y);
        }

        private void ZedGraphControl1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Left)
            {
                zedGraphControl1.GraphPane.FindNearestPoint(e.Location, out CurveItem nearestItem, out _);
                if (nearestItem != null && nearestItem.Tag != null)
                {
                    Random random = new Random(6548);
                    for (int j = 1; j < zedGraphControl1.GraphPane.CurveList.Count; j++)
                    {
                        LineItem item = zedGraphControl1.GraphPane.CurveList[j] as LineItem;
                        item.Line.Width = 1;
                        if (item.Line.Color == Color.Red)
                            item.Line.Color = Color.FromArgb(random.Next(80, 255), random.Next(50, 255), random.Next(30, 255));
                        item.Line.Color = Color.FromArgb(70, item.Line.Color);
                        item.Label.Text = "";
                    }

                    LineItem it = nearestItem as LineItem;
                    it.Line.Width = 3;
                    it.Line.Color = Color.Red;
                    it.Label.Text = "Selected";

                    int i = zedGraphControl1.GraphPane.CurveList.IndexOf(nearestItem);
                    zedGraphControl1.GraphPane.CurveList.Move(i, -i + 2);
                    zedGraphControl1.Refresh();

                    selectedIndex = (int)nearestItem.Tag;
                    textBoxInfo.Text = Labels[selectedIndex].Replace(Environment.NewLine, " ").Replace(" Max Trades", Environment.NewLine + "Max Trades");
                }
            }
        }

        private void CheckBoxReal_CheckedChanged(object sender, EventArgs e)
        {
            if (zedGraphControl1.GraphPane.CurveList.Count == 0) return;

            zedGraphControl1.GraphPane.CurveList[0].IsVisible = checkBoxReal.Checked;
            zedGraphControl1.GraphPane.CurveList[1].IsVisible = checkBoxReal.Checked;
            zedGraphControl1.AxisChange();
            zedGraphControl1.Refresh();
        }

        public void UpdateTheme(string themeName)
        {
            Graph.ZedThemeUpdate(zedGraphControl1, themeName);
            Graph.ZedThemeUpdate(zedGraphControl2, themeName);
            Graph.ZedThemeUpdate(zedGraphControl3, themeName);
            Graph.ZedThemeUpdate(zedGraphControl4, themeName);

            if (themeName.Contains("Dark"))
            {
                BackColor = Graph.backColorDark;

                checkBoxReal.FlatAppearance.CheckedBackColor = Graph.accentColorDark;
            }
            else
            {
                BackColor = Graph.backColorLight;

                checkBoxReal.FlatAppearance.CheckedBackColor = Graph.accentColorLight;
            }
        }
    }
}
