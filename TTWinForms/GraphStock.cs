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
    public partial class GraphStock : UserControl, IThemedGraph
    {
        BoxObj boxMarker;
        LineObj HLine, VLine;
        readonly JapaneseCandleStickItem candleM5original;
        readonly JapaneseCandleStickItem candleM30;
        readonly JapaneseCandleStickItem candleH1;
        readonly JapaneseCandleStickItem candleH4;
        readonly JapaneseCandleStickItem candleD1;
        readonly JapaneseCandleStickItem candleW1;
        readonly JapaneseCandleStickItem candleMN;
        readonly LineItem tradesM5original;
        readonly LineItem tradesM30;
        readonly LineItem tradesH1;
        readonly LineItem tradesH4;
        readonly LineItem tradesD1;
        readonly LineItem tradesW1;
        readonly LineItem tradesMN;
        readonly BarItem volumeBars;

        public GraphStock(List<DateTime> dateTimes, List<float> open, List<float> high, List<float> low, List<float> close, List<int> volumes,
            List<int> entryDates, List<int> entryTimes, List<float> entryValues, List<int> exitDates, List<int> exitTimesModes, List<float> exitValues)
        {
            InitializeComponent();

            panel1.Visible = true;
            panel1.Enabled = true;
            ZedInit();
            ObjectsInit();

            double globalMinY = double.MaxValue, globalMaxY = double.MinValue;

            StockPointList M5 = new StockPointList(),
                M30 = new StockPointList(),
                H1 = new StockPointList(),
                H4 = new StockPointList(),
                D1 = new StockPointList(),
                W1 = new StockPointList(),
                MN = new StockPointList();
            List<double> x = new List<double>(), y = new List<double>();

            #region Prepare timeframes data
            {
                DateTime dt;
                double OADate;

                long deltaM5 = 5 * TimeSpan.TicksPerMinute;
                long deltaM30 = 30 * TimeSpan.TicksPerMinute;
                long deltaH1 = TimeSpan.TicksPerHour;
                long deltaH4 = 4 * TimeSpan.TicksPerHour;
                long deltaD1 = TimeSpan.TicksPerDay;
                long deltaW1 = 7 * TimeSpan.TicksPerDay;

                long lastM5 = deltaM5 * (dateTimes[0].Ticks / deltaM5);
                long lastM30 = deltaM30 * (dateTimes[0].Ticks / deltaM30);
                long lastH1 = deltaH1 * (dateTimes[0].Ticks / deltaH1);
                long lastH4 = deltaH4 * (dateTimes[0].Ticks / deltaH4);
                long lastD1 = deltaD1 * (dateTimes[0].Ticks / deltaD1);
                long lastW1 = deltaW1 * (dateTimes[0].Ticks / deltaW1);
                int lastMN = 0;

                StockPt barM5 = new StockPt(),
                    barM30 = new StockPt(),
                    barH1 = new StockPt(),
                    barH4 = new StockPt(),
                    barD1 = new StockPt(),
                    barW1 = new StockPt(),
                    barMN = new StockPt();

                int countM5 = 0, countM30 = 0, countH1 = 0, countH4 = 0, countD1 = 0, countW1 = 0, countMN = 0;

                for (int i = 0; i < dateTimes.Count; i++)
                {
                    dt = dateTimes[i];

                    // date line data
                    OADate = dt.ToOADate();
                    x.Add(OADate);
                    y.Add(volumes[i]);

                    if (volumes[i] < globalMinY) globalMinY = volumes[i];
                    if (volumes[i] > globalMaxY) globalMaxY = volumes[i];

                    // candle sticks data
                    if (dt.Month != lastMN)    // check month ----------------
                    {
                        //save last bars
                        if (countMN > 0)
                        {
                            MN.Add(barMN); W1.Add(barW1); D1.Add(barD1); H4.Add(barH4); H1.Add(barH1); M30.Add(barM30); M5.Add(barM5);
                        }
                        //start current bars
                        barMN.Date = barW1.Date = barD1.Date = barH4.Date = barH1.Date = barM30.Date = barM5.Date = OADate;
                        barMN.High = barW1.High = barD1.High = barH4.High = barH1.High = barM30.High = barM5.High = high[i];
                        barMN.Low = barW1.Low = barD1.Low = barH4.Low = barH1.Low = barM30.Low = barM5.Low = low[i];
                        barMN.Open = barW1.Open = barD1.Open = barH4.Open = barH1.Open = barM30.Open = barM5.Open = open[i];
                        //update limits
                        lastMN = dt.Month;
                        lastW1 = deltaW1 * (1 + dt.Ticks / deltaW1);
                        lastD1 = deltaD1 * (1 + dt.Ticks / deltaD1);
                        lastH4 = deltaH4 * (1 + dt.Ticks / deltaH4);
                        lastH1 = deltaH1 * (1 + dt.Ticks / deltaH1);
                        lastM30 = deltaM30 * (1 + dt.Ticks / deltaM30);
                        lastM5 = deltaM5 * (1 + dt.Ticks / deltaM5);
                        countMN = countW1 = countD1 = countH4 = countH1 = countM30 = countM5 = 1;
                    }
                    else
                    {
                        //update current bar
                        if (high[i] > barMN.High) barMN.High = high[i];
                        if (low[i] < barMN.Low) barMN.Low = low[i];
                        countMN++;

                        if (dt.Ticks > lastW1)    // check week ------------------
                        {
                            //save last bars
                            if (countW1 > 0)
                            {
                                W1.Add(barW1); D1.Add(barD1); H4.Add(barH4); H1.Add(barH1); M30.Add(barM30); M5.Add(barM5);
                            }
                            //start current bars
                            barW1.Date = barD1.Date = barH4.Date = barH1.Date = barM30.Date = barM5.Date = OADate;
                            barW1.High = barD1.High = barH4.High = barH1.High = barM30.High = barM5.High = high[i];
                            barW1.Low = barD1.Low = barH4.Low = barH1.Low = barM30.Low = barM5.Low = low[i];
                            barW1.Open = barD1.Open = barH4.Open = barH1.Open = barM30.Open = barM5.Open = open[i];
                            //update limits
                            lastW1 = deltaW1 * (1 + dt.Ticks / deltaW1);
                            lastD1 = deltaD1 * (1 + dt.Ticks / deltaD1);
                            lastH4 = deltaH4 * (1 + dt.Ticks / deltaH4);
                            lastH1 = deltaH1 * (1 + dt.Ticks / deltaH1);
                            lastM30 = deltaM30 * (1 + dt.Ticks / deltaM30);
                            lastM5 = deltaM5 * (1 + dt.Ticks / deltaM5);
                            countW1 = countD1 = countH4 = countH1 = countM30 = countM5 = 1;
                        }
                        else
                        {
                            //update current bar
                            if (high[i] > barW1.High) barW1.High = high[i];
                            if (low[i] < barW1.Low) barW1.Low = low[i];
                            countW1++;

                            if (dt.Ticks > lastD1)    // check day ------------------
                            {
                                //save last bars
                                if (countD1 > 0)
                                {
                                    D1.Add(barD1); H4.Add(barH4); H1.Add(barH1); M30.Add(barM30); M5.Add(barM5);
                                }
                                //start current bars
                                barD1.Date = barH4.Date = barH1.Date = barM30.Date = barM5.Date = OADate;
                                barD1.High = barH4.High = barH1.High = barM30.High = barM5.High = high[i];
                                barD1.Low = barH4.Low = barH1.Low = barM30.Low = barM5.Low = low[i];
                                barD1.Open = barH4.Open = barH1.Open = barM30.Open = barM5.Open = open[i];
                                //update limits
                                lastD1 = deltaD1 * (1 + dt.Ticks / deltaD1);
                                lastH4 = deltaH4 * (1 + dt.Ticks / deltaH4);
                                lastH1 = deltaH1 * (1 + dt.Ticks / deltaH1);
                                lastM30 = deltaM30 * (1 + dt.Ticks / deltaM30);
                                lastM5 = deltaM5 * (1 + dt.Ticks / deltaM5);
                                countD1 = countH4 = countH1 = countM30 = countM5 = 1;
                            }
                            else
                            {
                                //update current bar
                                if (high[i] > barD1.High) barD1.High = high[i];
                                if (low[i] < barD1.Low) barD1.Low = low[i];
                                countD1++;

                                if (dt.Ticks > lastH4)    // check H4 ------------------
                                {
                                    //save last bars
                                    if (countH4 > 0)
                                    {
                                        H4.Add(barH4); H1.Add(barH1); M30.Add(barM30); M5.Add(barM5);
                                    }
                                    //start current bars
                                    barH4.Date = barH1.Date = barM30.Date = barM5.Date = OADate;
                                    barH4.High = barH1.High = barM30.High = barM5.High = high[i];
                                    barH4.Low = barH1.Low = barM30.Low = barM5.Low = low[i];
                                    barH4.Open = barH1.Open = barM30.Open = barM5.Open = open[i];
                                    //update limits
                                    lastH4 = deltaH4 * (1 + dt.Ticks / deltaH4);
                                    lastH1 = deltaH1 * (1 + dt.Ticks / deltaH1);
                                    lastM30 = deltaM30 * (1 + dt.Ticks / deltaM30);
                                    lastM5 = deltaM5 * (1 + dt.Ticks / deltaM5);
                                    countH4 = countH1 = countM30 = countM5 = 1;
                                }
                                else
                                {
                                    //update current bar
                                    if (high[i] > barH4.High) barH4.High = high[i];
                                    if (low[i] < barH4.Low) barH4.Low = low[i];
                                    countH4++;

                                    if (dt.Ticks > lastH1)    // check H1 ------------------
                                    {
                                        //save last bars
                                        if (countH1 > 0)
                                        {
                                            H1.Add(barH1); M30.Add(barM30); M5.Add(barM5);
                                        }
                                        //start current bars
                                        barH1.Date = barM30.Date = barM5.Date = OADate;
                                        barH1.High = barM30.High = barM5.High = high[i];
                                        barH1.Low = barM30.Low = barM5.Low = low[i];
                                        barH1.Open = barM30.Open = barM5.Open = open[i];
                                        //update limits
                                        lastH1 = deltaH1 * (1 + dt.Ticks / deltaH1);
                                        lastM30 = deltaM30 * (1 + dt.Ticks / deltaM30);
                                        lastM5 = deltaM5 * (1 + dt.Ticks / deltaM5);
                                        countH1 = countM30 = countM5 = 1;
                                    }
                                    else
                                    {
                                        //update current bar
                                        if (high[i] > barH1.High) barH1.High = high[i];
                                        if (low[i] < barH1.Low) barH1.Low = low[i];
                                        countH1++;

                                        if (dt.Ticks > lastM30)    // check M30 ------------------
                                        {
                                            //save last bars
                                            if (countM30 > 0)
                                            {
                                                M30.Add(barM30); M5.Add(barM5);
                                            }
                                            //start current bars
                                            barM30.Date = barM5.Date = OADate;
                                            barM30.High = barM5.High = high[i];
                                            barM30.Low = barM5.Low = low[i];
                                            barM30.Open = barM5.Open = open[i];
                                            //update limits
                                            lastM30 = deltaM30 * (1 + dt.Ticks / deltaM30);
                                            lastM5 = deltaM5 * (1 + dt.Ticks / deltaM5);
                                            countM30 = countM5 = 1;
                                        }
                                        else
                                        {
                                            //update current bar
                                            if (high[i] > barM30.High) barM30.High = high[i];
                                            if (low[i] < barM30.Low) barM30.Low = low[i];
                                            countM30++;

                                            if (dt.Ticks > lastM5)    // check M30 ------------------
                                            {
                                                //save last bars
                                                if (countM5 > 0)
                                                {
                                                    M5.Add(barM5);
                                                }
                                                //start current bars
                                                barM5.Date = OADate;
                                                barM5.High = high[i];
                                                barM5.Low = low[i];
                                                barM5.Open = open[i];
                                                //update limits
                                                lastM5 = deltaM5 * (1 + dt.Ticks / deltaM5);
                                                countM5 = 1;
                                            }
                                            else
                                            {
                                                //update current bar
                                                if (high[i] > barM5.High) barM5.High = high[i];
                                                if (low[i] < barM5.Low) barM5.Low = low[i];
                                                countM5++;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    barMN.Close = barW1.Close = barD1.Close = barH4.Close = barH1.Close = barM30.Close = barM5.Close = close[i];
                }
            }
            #endregion

            // Construct CandleSticks lines
            candleM5original = ConstructCandleItems(M5);
            candleM30 = ConstructCandleItems(M30);
            candleH1 = ConstructCandleItems(H1);
            candleH4 = ConstructCandleItems(H4);
            candleD1 = ConstructCandleItems(D1);
            candleW1 = ConstructCandleItems(W1);
            candleMN = ConstructCandleItems(MN);

            // Construct trades line
            if (entryDates != null && entryTimes != null && entryValues != null && exitDates != null && exitTimesModes != null && exitValues != null)
            {
                tradesM5original = ConstructTradesLine(M5, entryDates, entryTimes, entryValues, exitDates, exitTimesModes, exitValues);
                tradesM30 = ConstructTradesLine(M30, entryDates, entryTimes, entryValues, exitDates, exitTimesModes, exitValues);
                tradesH1 = ConstructTradesLine(H1, entryDates, entryTimes, entryValues, exitDates, exitTimesModes, exitValues);
                tradesH4 = ConstructTradesLine(H4, entryDates, entryTimes, entryValues, exitDates, exitTimesModes, exitValues);
                tradesD1 = ConstructTradesLine(D1, entryDates, entryTimes, entryValues, exitDates, exitTimesModes, exitValues);
                tradesW1 = ConstructTradesLine(W1, entryDates, entryTimes, entryValues, exitDates, exitTimesModes, exitValues);
                tradesMN = ConstructTradesLine(MN, entryDates, entryTimes, entryValues, exitDates, exitTimesModes, exitValues);
            }

            // set default lines M5
            zedGraphControl1.GraphPane.CurveList.Add(tradesM5original);
            zedGraphControl1.GraphPane.CurveList.Add(candleM5original);

            tradesM5original.IsVisible = cbShowTrades.Checked;

            zedGraphControl1.ScrollMinX = 0;
            zedGraphControl1.ScrollMaxX = zedGraphControl1.GraphPane.CurveList.Last().NPts;

            #region Construct date line
            FilteredPointList points = new FilteredPointList(x.ToArray(), y.ToArray());
            if (points.Count > 1000)
                points.SetBounds(0, points[points.Count-1].X, 1000);

            LineItem dline = zedGraphControl2.GraphPane.AddCurve("date", points, Color.FromArgb(120,155,175), SymbolType.None);
            dline.Line.IsAntiAlias = true;
            dline.Line.Fill = new Fill(Color.FromArgb(30, 120, 155, 175), Color.FromArgb(100, 120, 155, 175), -90);

            zedGraphControl2.GraphPane.YAxis.Scale.Min = globalMinY;
            zedGraphControl2.GraphPane.YAxis.Scale.Max = globalMaxY;
            #endregion

            zedGraphControl1.AxisChange();
            zedGraphControl1.Refresh();
            zedGraphControl2.AxisChange();
            zedGraphControl2.Refresh();
        }

        public GraphStock(StockPointList bars, IReadOnlyList<int> entryDates, IReadOnlyList<int> entryTimes, IReadOnlyList<float> entryValues,
            IReadOnlyList<int> exitDates, IReadOnlyList<int> exitTimesModes, IReadOnlyList<float> exitValues)
        {
            InitializeComponent();
            ZedInit();
            ObjectsInit();

            List<double> x = new List<double>(), y = new List<double>(), vol = new List<double>();

            for (int i = 0; i < bars.Count; i++)
            {
                x.Add(bars[i].X);
                y.Add((bars[i] as StockPt).Close);
                vol.Add((bars[i] as StockPt).Vol);
            }

            candleM5original = ConstructCandleItems(bars);
            if (entryDates != null && entryTimes != null && entryValues != null && exitDates != null && exitTimesModes != null && exitValues != null)
            {
                tradesM5original = ConstructTradesLine(bars, entryDates, entryTimes, entryValues, exitDates, exitTimesModes, exitValues);
            }
            else tradesM5original = new LineItem("empty");

            // set default lines
            zedGraphControl1.GraphPane.CurveList.Add(tradesM5original);
            zedGraphControl1.GraphPane.CurveList.Add(candleM5original);

            tradesM5original.IsVisible = cbShowTrades.Checked;

            zedGraphControl1.ScrollMinX = 0;
            zedGraphControl1.ScrollMaxX = zedGraphControl1.GraphPane.CurveList.Last().NPts;

            // construct volume line
            volumeBars = zedGraphControl1.GraphPane.AddBar("volume", x.ToArray(), vol.ToArray(), Color.FromArgb(120, 155, 175));
            volumeBars.Bar.Border.Color = Color.FromArgb(120, 155, 175);
            volumeBars.Bar.Fill = new Fill(Color.FromArgb(154, 187, 200));

            zedGraphControl1.GraphPane.YAxis.Scale.Min = 0;
            zedGraphControl1.GraphPane.YAxis.Scale.MaxGrace = 5;

            // Construct date line
            FilteredPointList points = new FilteredPointList(x.ToArray(), y.ToArray());
            if (points.Count > 1000)
                points.SetBounds(0, points[points.Count - 1].X, 1000);

            LineItem dline = zedGraphControl2.GraphPane.AddCurve("date", points, Color.FromArgb(120, 155, 175), SymbolType.None);
            dline.Line.IsAntiAlias = true;
            dline.Line.Fill = new Fill(Color.FromArgb(30, 120, 155, 175), Color.FromArgb(100, 120, 155, 175), -90);

            zedGraphControl2.GraphPane.YAxis.Scale.MinGrace = 0;
            zedGraphControl2.GraphPane.YAxis.Scale.MaxGrace = 0;

            zedGraphControl1.AxisChange();
            zedGraphControl1.Refresh();
            zedGraphControl2.AxisChange();
            zedGraphControl2.Refresh();
        }

        private void ZedInit()
        {
            zedGraphControl1.GraphPane.Legend.IsVisible = false;
            zedGraphControl1.GraphPane.Border.IsVisible = false;
            zedGraphControl1.GraphPane.Title.IsVisible = false;
            zedGraphControl1.GraphPane.Chart.Fill = new Fill(Color.FromArgb(1, Color.Black), Color.FromArgb(20, Color.Black), -90);
            zedGraphControl1.GraphPane.IsFontsScaled = false;
            zedGraphControl1.GraphPane.Margin.Bottom = (int)(1.1 * zedGraphControl2.Height);
            zedGraphControl1.GraphPane.Margin.Left = SystemInformation.HorizontalScrollBarArrowWidth;
            zedGraphControl1.GraphPane.BarSettings.MinClusterGap = 0.5f;

            zedGraphControl1.IsAutoScrollRange = true;
            zedGraphControl1.GraphPane.IsBoundedRanges = true;

            zedGraphControl1.ContextMenuBuilder += new ZedGraphControl.ContextMenuBuilderEventHandler(MyContextMenuBuilder);
            zedGraphControl1.CursorValueEvent += ZedGraphControl1_CursorValueEvent;

            zedGraphControl1.GraphPane.XAxis.Type = AxisType.DateAsOrdinal;
            zedGraphControl1.GraphPane.XAxis.Title.IsVisible = false;
            zedGraphControl1.GraphPane.XAxis.Scale.FontSpec.Size = 10;
            zedGraphControl1.GraphPane.XAxis.Scale.IsSkipCrossLabel = true;
            zedGraphControl1.GraphPane.XAxis.Scale.IsSkipFirstLabel = true;
            zedGraphControl1.GraphPane.XAxis.Scale.IsSkipLastLabel = true;
            zedGraphControl1.GraphPane.XAxis.Scale.dateSourceCurveIndex = 1;
            zedGraphControl1.GraphPane.XAxis.MajorGrid.IsVisible = true;
            zedGraphControl1.GraphPane.XAxis.MajorGrid.DashOn = 5;
            zedGraphControl1.GraphPane.XAxis.MajorGrid.Color = Color.LightGray;

            zedGraphControl1.GraphPane.YAxis.IsVisible = false;
            zedGraphControl1.GraphPane.YAxis.Scale.FormatAuto = true;
            zedGraphControl1.GraphPane.YAxis.Scale.MaxAuto = true;
            zedGraphControl1.GraphPane.YAxis.Scale.MinAuto = true;
            zedGraphControl1.GraphPane.YAxis.Title.IsVisible = false;
            zedGraphControl1.GraphPane.YAxis.Scale.MagAuto = false;

            zedGraphControl1.GraphPane.Y2Axis.IsVisible = true;
            zedGraphControl1.GraphPane.Y2Axis.Title.IsVisible = false;
            zedGraphControl1.GraphPane.Y2Axis.Scale.MagAuto = false;
            zedGraphControl1.GraphPane.Y2Axis.Scale.MaxAuto = true;
            zedGraphControl1.GraphPane.Y2Axis.Scale.MinAuto = true;
            zedGraphControl1.GraphPane.Y2Axis.Scale.FontSpec.Size = 10;
            zedGraphControl1.GraphPane.Y2Axis.MajorGrid.DashOn = 3;
            zedGraphControl1.GraphPane.Y2Axis.MajorGrid.Color = Color.LightGray;
            zedGraphControl1.GraphPane.Y2Axis.MajorGrid.IsVisible = true;
            zedGraphControl1.GraphPane.Y2Axis.MinSpace = 50;                // right margin
            //zedGraphControl1.GraphPane.Y2Axis.Scale.IsSkipCrossLabel = true;
            //zedGraphControl1.GraphPane.Y2Axis.Scale.IsSkipFirstLabel = true;
            //zedGraphControl1.GraphPane.Y2Axis.Scale.IsSkipLastLabel = true;

            zedGraphControl1.ModifyContextMenu();

            zedGraphControl2.GraphPane.Legend.IsVisible = false;
            zedGraphControl2.GraphPane.Border.IsVisible = false;
            zedGraphControl2.GraphPane.Title.IsVisible = false;
            zedGraphControl2.GraphPane.XAxis.Type = AxisType.DateAsOrdinal;
            zedGraphControl2.GraphPane.XAxis.Title.IsVisible = false;
            zedGraphControl2.GraphPane.XAxis.IsVisible = false;
            zedGraphControl2.GraphPane.YAxis.Title.IsVisible = false;
            zedGraphControl2.GraphPane.YAxis.Scale.IsVisible = false;
            zedGraphControl2.GraphPane.YAxis.IsVisible = false;
            zedGraphControl2.GraphPane.IsFontsScaled = false;
            zedGraphControl2.GraphPane.Margin.Left = SystemInformation.HorizontalScrollBarArrowWidth;
            zedGraphControl2.GraphPane.Margin.Top = 0;
            zedGraphControl2.GraphPane.Margin.Bottom = 0;
            zedGraphControl2.GraphPane.Border.IsVisible = false;
            zedGraphControl2.GraphPane.Chart.Border.IsVisible = false;
            zedGraphControl2.ModifyContextMenu();
        }

        private void ObjectsInit()
        {
            boxMarker = new BoxObj(0, 0.01, 1, 1, Color.LightGray, Color.FromArgb(50, 200, 200, 200));
            boxMarker.Location.CoordinateFrame = CoordType.ChartFraction;
            // boxMarker.IsClippedToChartRect = true;
            zedGraphControl2.GraphPane.GraphObjList.Add(boxMarker);

            HLine = new LineObj(Color.Black, 0, 0.5, 1, 0.5);
            HLine.Location.CoordinateFrame = CoordType.XChartFractionYScale;
            HLine.IsVisible = cbShowTrades.Checked && cbCrosshair.Checked;
            zedGraphControl1.GraphPane.GraphObjList.Add(HLine);

            VLine = new LineObj(Color.Black, 0.5, 0, 0.5, 1);
            VLine.Location.CoordinateFrame = CoordType.XScaleYChartFraction;
            VLine.IsVisible = cbShowTrades.Checked && cbCrosshair.Checked;
            zedGraphControl1.GraphPane.GraphObjList.Add(VLine);
        }

        private JapaneseCandleStickItem ConstructCandleItems (StockPointList stList)
        {
            JapaneseCandleStickItem candle = new JapaneseCandleStickItem("stock", stList);
            candle.Stick.IsAutoSize = true;
            candle.dimatsiDrawMode = 1;
            candle.IsY2Axis = true;
            candle.Stick.FallingFill.Color = Color.FromArgb(255, 50, 50);
            candle.Stick.RisingFill.Color = Color.FromArgb(65, 210, 65);
            candle.Stick.Color = Color.FromArgb(0, 150, 0);
            candle.Stick.FallingColor = Color.FromArgb(220, 0, 0);
            candle.Stick.FallingBorder.Color = Color.FromArgb(220, 0, 0);
            candle.Stick.RisingBorder.Color = Color.FromArgb(0, 150, 0);
            return candle;
        }

        private LineItem ConstructTradesLine (StockPointList stockList, IReadOnlyList<int> entryDates, IReadOnlyList<int> entryTimes, IReadOnlyList<float> entryValues,
            IReadOnlyList<int> exitDates, IReadOnlyList<int> exitTimesModes, IReadOnlyList<float> exitValues)
        {
            PointPairList tradesLineList = new PointPairList();

            for (int tradeIdx = 0, dtC = 0; tradeIdx < entryDates.Count; tradeIdx++)
            {
                if (entryValues[tradeIdx] < 0.000001f || exitValues[tradeIdx] < 0.000001f) continue;

                int date = entryDates[tradeIdx];
                int time = entryTimes[tradeIdx];
                DateTime entryDT = new DateTime(date / 10000, (date % 10000) / 100, date % 100, time / 10000, (time % 10000) / 100, time % 100, DateTimeKind.Utc);
                date = exitDates[tradeIdx];
                time = exitTimesModes[tradeIdx] & 0xffffff;
                DateTime exitDT = new DateTime(date / 10000, (date % 10000) / 100, date % 100, time / 10000, (time % 10000) / 100, time % 100, DateTimeKind.Utc);
                double entryOA = entryDT.ToOADate();
                double exitOA = exitDT.ToOADate();

                while (dtC < stockList.Count && stockList[dtC].X < entryOA) dtC++;
                tradesLineList.Add(dtC + 1, entryValues[tradeIdx]);
                while (dtC < stockList.Count && stockList[dtC].X < exitOA) dtC++;
                tradesLineList.Add(dtC + 1, exitValues[tradeIdx]);
                if (dtC > 0) dtC--;
            }

            LineItem tradesLine = new LineItem("trades", tradesLineList, Color.Blue, SymbolType.Default);
            tradesLine.Line.Width = 2;
            tradesLine.Line.IsAntiAlias = true;
            tradesLine.Symbol.Size = 11;
            tradesLine.Symbol.IsAntiAlias = true;
            tradesLine.Symbol.Border.Color = Color.White;
            tradesLine.Symbol.Type = SymbolType.EntryTriangle;
            tradesLine.Symbol.Type2 = SymbolType.ExitTriangle;
            tradesLine.Symbol.Fill = new Fill(Color.Blue);
            tradesLine.Symbol.Fill2 = new Fill(Color.Orange);
            tradesLine.tradesMode = true;
            tradesLine.dimatsiDrawMode = 1;
            tradesLine.IsY2Axis = true;
            tradesLine.IsOverrideOrdinal = true;

            return tradesLine;
        }

        private void GraphStock_Shown(object sender, EventArgs e)
        {
            zedGraphControl2.GraphPane.Margin.Right = zedGraphControl1.Width - (int)zedGraphControl1.GraphPane.Chart.Rect.Right + 1;
            zedGraphControl1.HScrollMarginRight = zedGraphControl1.Width - (int)zedGraphControl1.GraphPane.Chart.Rect.Right - SystemInformation.HorizontalScrollBarArrowWidth;
            zedGraphControl1.Width += 1;
            zedGraphControl1.Width -= 1;
        }

        private string ZedGraphControl1_CursorValueEvent(ZedGraphControl sender, GraphPane pane, Point mousePt)
        {
            pane.ReverseTransform(mousePt, out double x, out _, out double y, out _);

            if (cbCrosshair.Checked)
            {
                HLine.Location.Y = HLine.Location.Y1 = y;
                VLine.Location.X = VLine.Location.X1 = x;
                Refresh();
            }

            string resStr = string.Empty;
            int tmpIndex = (int)(x - 0.5);
            if (tmpIndex >= 0 && pane.CurveList.Count > 1 && pane.CurveList[1].Points.Count > tmpIndex)
            {
                StockPt pt = pane.CurveList[1].Points[tmpIndex] as StockPt;
                resStr = XDate.XLDateToDateTime(pt.X).ToString();

                resStr = string.Format("{0}{1}Open: {2:F5}{1}High:  {3:F5}{1}Low:   {4:F5}{1}Close: {5:F5}{1}Volume: {6}",
                    resStr, Environment.NewLine, pt.Open, pt.High, pt.Low, pt.Close, pt.Vol);
            }
            return resStr;
        }

        private void MyContextMenuBuilder(ZedGraphControl control, ContextMenuStrip menuStrip, Point mousePt, ZedGraphControl.ContextMenuObjectState objState)
        {
            foreach (ToolStripMenuItem item in menuStrip.Items)
            {
                if ((string)item.Tag == "show_val")
                {
                    // remove the menu item
                    menuStrip.Items.Remove(item);
                    // or, just disable the item with this
                    //item.Enabled = false; 

                    break;
                }
            }
        }

        private void ZedGraphControl1_ZoomEvent(ZedGraphControl sender, ZoomState oldState, ZoomState newState)
        {
            int minBars = 50;
            if(zedGraphControl1.GraphPane.XAxis.Scale.Max - zedGraphControl1.GraphPane.XAxis.Scale.Min < minBars)
            {
                if (zedGraphControl1.GraphPane.XAxis.Scale.Min + minBars < GetCurrentCandleStickItem().NPts)
                    zedGraphControl1.GraphPane.XAxis.Scale.Max = zedGraphControl1.GraphPane.XAxis.Scale.Min + minBars;
                else
                    zedGraphControl1.GraphPane.XAxis.Scale.Min = zedGraphControl1.GraphPane.XAxis.Scale.Max - minBars;
            }

            if (zedGraphControl1.GraphPane.XAxis.Scale.Min < 0) zedGraphControl1.GraphPane.XAxis.Scale.Min = 0;
            if (zedGraphControl1.GraphPane.XAxis.Scale.Max > GetCurrentCandleStickItem().NPts) zedGraphControl1.GraphPane.XAxis.Scale.Max = GetCurrentCandleStickItem().NPts;

            MoveBox();
        }

        private void MoveBox()
        {
            boxMarker.Location.X = zedGraphControl1.GraphPane.XAxis.Scale.Min / GetCurrentCandleStickItem().NPts;
            boxMarker.Location.Width = (zedGraphControl1.GraphPane.XAxis.Scale.Max - zedGraphControl1.GraphPane.XAxis.Scale.Min) / GetCurrentCandleStickItem().NPts;

            if (boxMarker.Location.X < 0) boxMarker.Location.X = 0;
            if (boxMarker.Location.X2 > 1) boxMarker.Location.X = 1 - boxMarker.Location.Width;

            zedGraphControl2.Refresh();
        }

        private void ZedGraphControl1_ScrollEvent(object sender, ScrollEventArgs e)
        {
            MoveBox();

            // fit YAxis bounds and immidiately redraw
            zedGraphControl1.AxisChange();
            zedGraphControl1.Refresh();
        }

        private void ZedGraphControl2_ZoomEvent(ZedGraphControl sender, ZoomState oldState, ZoomState newState)
        {
            //int i = 0;
        }

        private void RbTimeframe_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton rb = sender as RadioButton;
            if (!rb.Checked) return;

            zedGraphControl1.GraphPane.CurveList.Clear();
            if (rb == rbM5)        { zedGraphControl1.GraphPane.CurveList.Add(tradesM5original);  zedGraphControl1.GraphPane.CurveList.Add(candleM5original);  }
            else if (rb == rbM30)  { zedGraphControl1.GraphPane.CurveList.Add(tradesM30); zedGraphControl1.GraphPane.CurveList.Add(candleM30); }
            else if (rb == rbH1)   { zedGraphControl1.GraphPane.CurveList.Add(tradesH1);  zedGraphControl1.GraphPane.CurveList.Add(candleH1);  }
            else if (rb == rbH4)   { zedGraphControl1.GraphPane.CurveList.Add(tradesH4);  zedGraphControl1.GraphPane.CurveList.Add(candleH4);  }
            else if (rb == rbD1)   { zedGraphControl1.GraphPane.CurveList.Add(tradesD1);  zedGraphControl1.GraphPane.CurveList.Add(candleD1);  }
            else if (rb == rbW1)   { zedGraphControl1.GraphPane.CurveList.Add(tradesW1);  zedGraphControl1.GraphPane.CurveList.Add(candleW1);  }
            else if (rb == rbMN)   { zedGraphControl1.GraphPane.CurveList.Add(tradesMN);  zedGraphControl1.GraphPane.CurveList.Add(candleMN);  }
            zedGraphControl1.GraphPane.CurveList.Add(volumeBars);

            if (zedGraphControl1.GraphPane.CurveList[0] != null)
                zedGraphControl1.GraphPane.CurveList[0].IsVisible = cbShowTrades.Checked;
            zedGraphControl1.ScrollMaxX = zedGraphControl1.GraphPane.CurveList[1].NPts;
            zedGraphControl1.RestoreScale(zedGraphControl1.GraphPane);
            zedGraphControl1.AxisChange();
            zedGraphControl1.Refresh();
        }

        private JapaneseCandleStickItem GetCurrentCandleStickItem()
        {
            return zedGraphControl1.GraphPane.CurveList.First(x => (x is JapaneseCandleStickItem) && x.IsVisible) as JapaneseCandleStickItem;
        }

        private void CbShowTrades_CheckedChanged(object sender, EventArgs e)
        {
            if (zedGraphControl1.GraphPane.CurveList[0] != null)
            {
                zedGraphControl1.GraphPane.CurveList[0].IsVisible = cbShowTrades.Checked;
                zedGraphControl1.Refresh();
            }
        }

        private void CbShowCursorValues_CheckedChanged(object sender, EventArgs e)
        {
            zedGraphControl1.IsShowCursorValues = cbShowCursorValues.Checked;
            cbCrosshair.Enabled = cbShowCursorValues.Checked;
            CbCrosshair_CheckedChanged(null, null);
        }

        private void CbCrosshair_CheckedChanged(object sender, EventArgs e)
        {
            HLine.IsVisible = cbCrosshair.Enabled && cbCrosshair.Checked;
            VLine.IsVisible = cbCrosshair.Enabled && cbCrosshair.Checked;
            zedGraphControl1.Refresh();
        }

        private void CbVolumes_CheckedChanged(object sender, EventArgs e)
        {
            CurveItem c = zedGraphControl1.GraphPane.CurveList.First(x => x is BarItem);
            if (c != null)
            {
                c.IsVisible = cbVolumes.Checked;
                zedGraphControl1.AxisChange();
                zedGraphControl1.Refresh();
            }
        }

        public void UpdateTheme(string themeName)
        {
            Graph.ZedThemeUpdate(zedGraphControl1, themeName);
            Graph.ZedThemeUpdate(zedGraphControl2, themeName);

            if (themeName.Contains("Dark"))
            {
                BackColor = Graph.backColorDark;

                if (HLine != null)
                    HLine.Line.Color = VLine.Line.Color = Color.White;
            }
            else
            {
                BackColor = Graph.backColorLight;

                if (HLine != null)
                    HLine.Line.Color = VLine.Line.Color = Color.Black;
            }
        }
    }
}
