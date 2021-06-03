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
    public partial class FourGistForm : UserControl, IThemedGraph
    {
        List<ZedGraphControl> zedGraphs;

        public FourGistForm(List<PointPairList> lines, int mode)
        {
            InitializeComponent();

            zedGraphs = new List<ZedGraphControl>
            {
                zedGraphControl1,
                zedGraphControl2,
                zedGraphControl3,
                zedGraphControl4
            };

            zedGraphControl1.GraphPane.Title.Text = "Days";
            zedGraphControl2.GraphPane.Title.Text = "Months";
            zedGraphControl3.GraphPane.Title.Text = "Quarters";
            zedGraphControl4.GraphPane.Title.Text = "Years";

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
                zed.GraphPane.BarSettings.Type = BarType.Overlay;
                zed.GraphPane.Border.IsVisible = false;

                zed.GraphPane.XAxis.Type = AxisType.Date;
                zed.ModifyContextMenu();
            }

            if (mode == 1)
            {
                zedGraphControl1.GraphPane.XAxis.Type = AxisType.Text;
                zedGraphControl1.GraphPane.XAxis.Scale.TextLabels = new string[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" };

                zedGraphControl2.GraphPane.XAxis.Type = AxisType.Text;
                zedGraphControl2.GraphPane.XAxis.Scale.TextLabels = new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };

                zedGraphControl3.GraphPane.XAxis.Type = AxisType.Text;
                zedGraphControl3.GraphPane.XAxis.Scale.TextLabels = new string[] { "Quarter 1", "Quarter 2", "Quarter 3", "Quarter 4" };
            }

            for (int i = 0; i < lines.Count; i++)
            {
                PointPairList positive = lines[i].Clone();
                PointPairList negative = lines[i].Clone();

                for (int j = 0; j < positive.Count; j++)
                {
                    if (positive[j].Y < 0) positive[j].Y = 0;
                    if (negative[j].Y > 0) negative[j].Y = 0;
                }

                BarItem barPos = zedGraphs[i].GraphPane.AddBar("linePos", positive, Color.Green);
                barPos.Bar.Border.IsVisible = false;

                BarItem barNeg = zedGraphs[i].GraphPane.AddBar("lineNeg", negative, Color.Red);
                barNeg.Bar.Border.IsVisible = false;

                if (zedGraphs[i].GraphPane.XAxis.Type != AxisType.Text)
                {
                    double min = double.MaxValue;
                    double max = double.MinValue;
                    if (lines[i].Count > 0) { min = lines[i].First().X; max = lines[i].Last().X; }
                    // if (lines[i + 1].Count > 0) { min = Math.Min(min, lines[i + 1].First().X); max = Math.Max(max, lines[i + 1].Last().X); }
                    if (min < double.MaxValue && min != max)
                    {
                        zedGraphs[i].GraphPane.XAxis.Scale.Min = min;
                        zedGraphs[i].GraphPane.XAxis.Scale.Max = max;
                    }
                }
            }

            foreach (ZedGraphControl zed in zedGraphs)
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
