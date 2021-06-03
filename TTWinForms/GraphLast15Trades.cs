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
    public partial class GraphLast15Trades : UserControl, IThemedGraph
    {
        readonly List<ZedGraphControl> zedGraphs = new List<ZedGraphControl>();

        public GraphLast15Trades(List<StockPointList> candles, List<PointPairList> entries)
        {
            InitializeComponent();

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

            foreach (ZedGraphControl zed in zedGraphs)
            {
                zed.GraphPane.Chart.Fill = new Fill(Color.FromArgb(1, Color.Black), Color.FromArgb(20, Color.Black), -90);
                zed.GraphPane.IsBoundedRanges = true;
                zed.GraphPane.Margin.All = 1;
                zed.GraphPane.IsFontsScaled = false;
                zed.GraphPane.Legend.FontSpec.Size = 10;
                zed.GraphPane.Legend.Border.IsVisible = false;
                zed.GraphPane.Legend.IsVisible = false;
                zed.GraphPane.Title.IsVisible = false;
                zed.GraphPane.Title.FontSpec.Size = 11;
                zed.GraphPane.XAxis.Type = AxisType.DateAsOrdinal;
                zed.GraphPane.XAxis.Scale.dateSourceCurveIndex = 1;
                zed.GraphPane.XAxis.MajorGrid.IsVisible = true;
                zed.GraphPane.XAxis.MajorGrid.DashOn = 5;
                zed.GraphPane.XAxis.MajorGrid.Color = Color.LightGray;
                zed.GraphPane.XAxis.Title.IsVisible = false;
                //zed.GraphPane.XAxis.Scale.IsVisible = false;
                zed.GraphPane.XAxis.Scale.FontSpec.Size = 10;
                zed.GraphPane.XAxis.Scale.Min = 0;
                zed.GraphPane.XAxis.Scale.Max = 61;         //maxX
                zed.GraphPane.XAxis.Scale.MagAuto = false;
                zed.GraphPane.XAxis.Scale.IsSkipFirstLabel = true;
                zed.GraphPane.XAxis.Scale.IsSkipLastLabel = true;
                zed.GraphPane.XAxis.Scale.Format = "dd MMM yyyy";
                zed.GraphPane.XAxis.MajorTic.Size = 2;
                zed.GraphPane.XAxis.MinorTic.Size = 1;
                zed.GraphPane.YAxis.MajorGrid.IsVisible = true;
                zed.GraphPane.YAxis.MajorGrid.DashOn = 5;
                zed.GraphPane.YAxis.MajorGrid.Color = Color.LightGray;
                zed.GraphPane.YAxis.Title.IsVisible = false;
                zed.GraphPane.YAxis.Scale.IsVisible = false;
                zed.GraphPane.YAxis.Scale.MagAuto = false;
                zed.GraphPane.YAxis.Scale.FontSpec.Size = 10;
                zed.GraphPane.YAxis.Scale.MinGrace = 0;
                zed.GraphPane.YAxis.Scale.MaxGrace = 0;
                zed.GraphPane.YAxis.MajorTic.Size = 2;
                zed.GraphPane.YAxis.MinorTic.Size = 1;
                zed.GraphPane.BarSettings.MinClusterGap = 0.5f;
                zed.GraphPane.Border.IsVisible = false;
                //zed.MouseDoubleClick += Zed_MouseDoubleClick;
            }

            for (int i = 0; i < candles.Count && i < 15; i++)
            {
                LineItem tradesLine = zedGraphs[i].GraphPane.AddCurve("", entries[i], Color.Blue, SymbolType.EntryTriangle);
                tradesLine.dimatsiDrawMode = 1;
                tradesLine.tradesMode = true;
                tradesLine.IsOverrideOrdinal = true;
                tradesLine.Line.IsVisible = false;
                tradesLine.Symbol.Size = 12;
                tradesLine.Symbol.IsAntiAlias = true;
                tradesLine.Symbol.Border.Color = Color.White;
                tradesLine.Symbol.Type = SymbolType.EntryTriangle;
                tradesLine.Symbol.Type2 = SymbolType.ExitTriangle;
                tradesLine.Symbol.Fill = new Fill(Color.Blue);
                tradesLine.Symbol.Fill2 = new Fill(Color.Orange);

                JapaneseCandleStickItem candle = zedGraphs[i].GraphPane.AddJapaneseCandleStick("", candles[i]);
                candle.Stick.FallingFill.Color = Color.FromArgb(255, 50, 50);
                candle.Stick.RisingFill.Color = Color.FromArgb(65, 210, 65);
                candle.Stick.Color = Color.FromArgb(0, 150, 0);
                candle.Stick.FallingColor = Color.FromArgb(220, 0, 0);
                candle.Stick.FallingBorder.Color = Color.FromArgb(220, 0, 0);
                candle.Stick.RisingBorder.Color = Color.FromArgb(0, 150, 0);
                candle.Stick.IsAutoSize = true;
            }

            foreach(ZedGraphControl zed in zedGraphs)
            {
                zed.AxisChange();
                zed.Refresh();
            }
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

            }
            else
            {
                BackColor = Graph.backColorLight;

            }
        }
    }
}
