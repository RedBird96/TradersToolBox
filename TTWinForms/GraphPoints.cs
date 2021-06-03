using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ZedGraph;

namespace TTWinForms
{
    public partial class GraphPoints : UserControl, IThemedGraph
    {
        readonly List<List<double>> weights = null;
        readonly List<string> strategies = null;

        public GraphPoints(PointPairList ppl, List<List<double>> w, List<string> stratNames, double minZ, double maxZ)
        {
            InitializeComponent();

            weights = w;
            strategies = stratNames;

            zedGraphControl1.GraphPane.XAxis.Title.Text = "Expected Volatility";
            zedGraphControl1.GraphPane.YAxis.Title.Text = "Expected Return";
            zedGraphControl1.GraphPane.Y2Axis.Title.Text = "Sharpe Ratio";

            // zedGraphControl1.GraphPane.Legend.Fill = new Fill(Color.Black, Color.White, 90);
            // zedGraphControl1.GraphPane.Legend.Fill.Type = FillType.None;
            // zedGraphControl1.GraphPane.Legend.FontSpec.Size = 12;
            zedGraphControl1.GraphPane.Legend.IsHStack = false;
            zedGraphControl1.GraphPane.Legend.Position = LegendPos.InsideTopRight;

            zedGraphControl1.GraphPane.IsFontsScaled = false;
            //zedGraphControl1.GraphPane.Legend.Border.IsVisible = false;
            zedGraphControl1.GraphPane.Title.IsVisible = false;
            zedGraphControl1.GraphPane.XAxis.MajorGrid.IsVisible = true;
            zedGraphControl1.GraphPane.XAxis.MajorGrid.DashOn = 5;
            zedGraphControl1.GraphPane.XAxis.MajorGrid.Color = Color.LightGray;
            //zedGraphControl1.GraphPane.XAxis.Title.IsVisible = false;
            //zedGraphControl1.GraphPane.XAxis.Scale.Min = 0;
            zedGraphControl1.GraphPane.XAxis.Scale.MagAuto = false;
            zedGraphControl1.GraphPane.YAxis.MajorGrid.IsVisible = true;
            zedGraphControl1.GraphPane.YAxis.MajorGrid.DashOn = 5;
            zedGraphControl1.GraphPane.YAxis.MajorGrid.Color = Color.LightGray;
            //zedGraphControl1.GraphPane.YAxis.Title.IsVisible = false;
            zedGraphControl1.GraphPane.YAxis.Scale.MagAuto = false;
            //zedGraphControl1.GraphPane.BarSettings.MinClusterGap = 0;
            zedGraphControl1.GraphPane.Border.IsVisible = false;

            zedGraphControl1.GraphPane.Y2Axis.IsVisible = true;
            zedGraphControl1.GraphPane.Y2Axis.MajorTic.IsAllTics = false;
            zedGraphControl1.GraphPane.Y2Axis.MinorTic.IsAllTics = false;
            zedGraphControl1.GraphPane.Y2Axis.Scale.IsVisible = false;
            zedGraphControl1.GraphPane.Y2Axis.Scale.MagAuto = false;
            zedGraphControl1.GraphPane.Y2Axis.MajorGrid.IsVisible = false;
            zedGraphControl1.GraphPane.Y2Axis.MinorGrid.IsVisible = false;

            zedGraphControl1.ModifyContextMenu();

            PointPairList pb = new PointPairList { ppl[0] };
            LineItem linePoint = zedGraphControl1.GraphPane.AddCurve("", pb, Color.Black, SymbolType.Circle);
            linePoint.Line.IsVisible = false;
            linePoint.Symbol.Fill.Type = FillType.GradientByZ;
            linePoint.Symbol.Fill.RangeMin = minZ;
            linePoint.Symbol.Fill.RangeMax = maxZ;
            linePoint.Symbol.Fill.Color = Color.Black;
            linePoint.Symbol.Fill.SecondaryValueGradientColor = Color.Empty;
            linePoint.Symbol.IsAntiAlias = true;
            linePoint.Symbol.Size = 6;
            //linePoint.Symbol.Border.Width = 3;

            LineItem lineBase = zedGraphControl1.GraphPane.AddCurve("", pb, Color.Red, SymbolType.Star);
            lineBase.Line.IsVisible = false;
            lineBase.Symbol.IsAntiAlias = true;
            lineBase.Symbol.Border.Width = 3;
            lineBase.Symbol.Size = 20;

            LineItem line = zedGraphControl1.GraphPane.AddCurve("", ppl, Color.Black, SymbolType.Circle);
            line.Line.IsVisible = false;
            line.Symbol.Fill.Type = FillType.GradientByZ;
            line.Symbol.Fill.RangeMin = minZ;
            line.Symbol.Fill.RangeMax = maxZ;
            line.Symbol.Fill.Color = Color.Black;
            line.Symbol.Fill.SecondaryValueGradientColor = Color.Empty;
            line.Symbol.IsAntiAlias = true;

            PointPairList pe = new PointPairList();
            for (int i = 0; i <= 10; i++)
            {
                int g = (int)(255 * i / 10.0);
                LineItem l = zedGraphControl1.GraphPane.AddCurve(String.Format("{0:F3}", maxZ - (maxZ - minZ) * i / 10.0), pe, Color.FromArgb(g, g, g), SymbolType.None);
                l.IsVisible = false;
                l.Line.Width = 14;
            }

            zedGraphControl1.AxisChange();
            zedGraphControl1.Refresh();
        }

        public void UpdateTheme(string themeName)
        {
            Graph.ZedThemeUpdate(zedGraphControl1, themeName);

            if (themeName.Contains("Dark"))
            {
                BackColor = Graph.backColorDark;

            }
            else
            {
                BackColor = Graph.backColorLight;

            }
        }

        private string ZedGraphControl1_PointValueEvent(ZedGraphControl sender, GraphPane pane, CurveItem curve, int iPt)
        {
            string w = "";
            if (weights != null && strategies!=null)
            {
                w = "\nWeights: {\n";
                for (int i = 0; i < weights[iPt].Count && i<strategies.Count; i++)
                    w += String.Format("    {1:F3}:  {0}\n", strategies[i], weights[iPt][i]);
                //w = w.Substring(0, w.Length - 1) + " }";
                w += "}";
            }
            return String.Format("Return: {0:F3}\nVolatility: {1:F3}\nSharpe: {2:F3}{3}", curve.Points[iPt].Y, curve.Points[iPt].X, curve.Points[iPt].Z, w);
        }
    }
}
