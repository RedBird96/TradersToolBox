using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Visuals.Annotations;
using SciChart.Charting.Visuals.PaletteProviders;
using SciChart.Charting.Visuals.RenderableSeries;
using StockPatternSearch;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using TradersToolbox.ViewModels;
using TradersToolbox.ViewModels.ChartViewModels;

namespace TradersToolbox.CustomAnnotations
{
    public class PatternCompositeViewModels : INotifyPropertyChanged
    {
        private CandleChartViewModel vm;
        public PatternCompositeViewModels(CandleChartViewModel vm, PatternStyle patternType, List<PatternResult> results, PatternValues values)
        {
            this.values = values;
            this.vm = vm;
            PatternType = patternType;
            Results = results;
            Annotations = new ObservableCollection<IAnnotation>();

            UpdateAnnotations(Results);
        }

        public void UpdateAnnotations(List<PatternResult> results)
        {
            Results = results;
            Annotations.Clear();
            GenerateAnnotations();
        }

        private void GenerateAnnotations()
        {
            if (Results == null)
                return;

            var results = Results.Where(var => var.pattern == PatternType).ToList();

            for (int i = 0; i < results.Count; i++)
            {
                PatternResult res = results[i];
                for (int j = 0; j < res.trendLines.Count; j += 2)
                {
                    DateTime x0 = StockFactory.IntToDate(res.trendLines[j].date, res.trendLines[j].time);
                    DateTime x1 = StockFactory.IntToDate(res.trendLines[j + 1].date, res.trendLines[j + 1].time);

                    Annotations.Add(new LineAnnotation() { Stroke = new SolidColorBrush(values.Color), StrokeThickness = 1.0, IsEditable = false, X1 = x0, X2 = x1, Y1 = res.trendLines[j].price, Y2 = res.trendLines[j + 1].price });
                }
            }
        }

        private PatternValues values;

        public ObservableCollection<IAnnotation> Annotations { get; set; }
        public PatternStyle PatternType { get; }
        public List<PatternResult> Results { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }




    public class PatternValues
    {
        public double RetraceZ { get; set; }
        public double RetracePerO { get; set; }
        public double LoosePercent { get; set; }
        public int MinWidth { get; set; }
        public int MaxWidth { get; set; }
        public bool UseVol { get; set; }
        public int SwingStrength { get; set; }
        public Color Color { get; set; }
    }


    /*public class HollowFillPaletteProvider : IStrokePaletteProvider, IFillPaletteProvider
    {
        private OhlcDataSeries<DateTime, double> _theSourceData;

        public void OnBeginSeriesDraw(IRenderableSeries series)
        {
            _theSourceData = (OhlcDataSeries<DateTime, double>)series.DataSeries;
        }

        public Brush OverrideFillBrush(IRenderableSeries series, int index, IPointMetadata metadata)
        {
            bool rise = _theSourceData.CloseValues[index] > _theSourceData.OpenValues[index];

            if (rise)
                return new SolidColorBrush(Colors.Transparent);
            else
            {
                var color = OverrideStrokeColor(series, index, metadata);
                return color.HasValue ? new SolidColorBrush(color.Value) : null;
            }

        }

        public Color? OverrideStrokeColor(IRenderableSeries series, int index, IPointMetadata metadata)
        {
            if (index > 0)
            {
                bool up = _theSourceData.CloseValues[index] > _theSourceData.CloseValues[index - 1];

                if (up)
                {
                    return (series as FastCandlestickRenderableSeries).StrokeUp;
                }
                else
                {
                    return (series as FastCandlestickRenderableSeries).StrokeDown;
                }
            }
            else
            {
                return null;
            }
        }
    }*/

    public class HollowFillPaletteProvider : TransparencyFillPaletteProvider
    {
        public override Brush OverrideFillBrush(IRenderableSeries series, int index, IPointMetadata metadata)
        {
            bool rise = _closeValues[index] > _openValues[index];

            if (rise)
                return new SolidColorBrush(Colors.Transparent);
            else
            {
                var color = OverrideStrokeColor(series, index, metadata);
                return color.HasValue ? new SolidColorBrush(color.Value) : null;
            }
        }
    }

    public class TransparencyFillPaletteProvider : IStrokePaletteProvider, IFillPaletteProvider
    {
        protected IList<DateTime> _dateTimes;
        protected IList<double> _closeValues;
        protected IList<double> _openValues;
        bool isTS;
        public static readonly TimeZoneInfo EasternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        TimeSpan marketPreOpen = new TimeSpan(4, 0, 0);
        TimeSpan marketOpen = new TimeSpan(9, 30, 0);
        TimeSpan marketClose = new TimeSpan(16, 0, 0);
        TimeSpan marketPostClose = new TimeSpan(20, 0, 0);
        int ShadeMode;
        public TransparencyFillPaletteProvider()
        {
            ShadeMode = 0;
        }
        public TransparencyFillPaletteProvider(int Mode)
        {
            ShadeMode = Mode;
        }
        public void OnBeginSeriesDraw(IRenderableSeries series)
        {
            if (series.DataSeries is OhlcDataSeries<DateTime, double> ohlc)
            {
                _dateTimes = ohlc.XValues;
                _closeValues = ohlc.CloseValues;
                _openValues = ohlc.OpenValues;
            }
            else if (series.DataSeries is XyDataSeries<DateTime, double> line)
            {
                _dateTimes = line.XValues;
                _closeValues = line.YValues;
            }
            isTS = MainWindowViewModel.BrokersManager.ActiveBroker == MainWindowViewModel.BrokersManager.TradeStation;
        }

        public virtual Brush OverrideFillBrush(IRenderableSeries series, int index, IPointMetadata metadata)
        {
            var color = OverrideStrokeColor(series, index, metadata);
            return color.HasValue ? new SolidColorBrush(color.Value) : null;
        }

        public Color? OverrideStrokeColor(IRenderableSeries series, int index, IPointMetadata metadata)
        {
            if (index > 0)
            {
                bool up = _closeValues[index] > _closeValues[index - 1];

                Color inMarketColor;

                if (series is FastCandlestickRenderableSeries fastCandlestick)
                    inMarketColor = up ? fastCandlestick.StrokeUp : fastCandlestick.StrokeDown;
                else if (series is FastOhlcRenderableSeries fastOhlc)
                    inMarketColor = up ? fastOhlc.StrokeUp : fastOhlc.StrokeDown;
                else if (series is FastLineRenderableSeries fastLine)
                    inMarketColor = fastLine.Stroke;
                else
                    throw new Exception("TransparencyFillPaletteProvider: Unsupported Series Type!");

                DateTime dt = _dateTimes[index];
                DateTime dte = TimeZoneInfo.ConvertTime(dt, EasternZone);
                TimeSpan time = dte.TimeOfDay;

                //var ExColor = Color.Add(inMarketColor, Color.FromRgb(0,0,80));// Color.FromArgb(100, inMarketColor.R, inMarketColor.G, inMarketColor.B);
                //ExColor = Color.Multiply(ExColor, 0.35f);
                //ExColor.A = 255;

                var ExColor = Color.Multiply(inMarketColor, 0.1f);
                var PostExColor = Color.Multiply(inMarketColor, 0.6f);
                ExColor.A = 255;
                PostExColor.A = 255;
                ExColor = Color.Add(ExColor, Color.FromRgb(35, 35, 45));
                PostExColor = Color.Add(PostExColor, Color.FromRgb(60, 60, 70));

                if (ShadeMode == 0)
                    PostExColor = ExColor;

                if (isTS)  //TradeStation
                {
                    if (time > marketOpen && time <= marketClose)
                        return inMarketColor;
                    else
                        return ExColor;
                }
                else
                {   // Polygon
                    if (time >= marketOpen && time < marketClose)
                        return inMarketColor;
                    //if (time >= marketClose)
                    //    return ExColor;

                    DateTime pre_dt = _dateTimes[index - 1];

                    //1d, 1w, 1m
                    if ((dt - pre_dt).TotalHours >= 23 && Math.Abs((pre_dt.TimeOfDay - dt.TimeOfDay).TotalHours) <= 1)
                        return inMarketColor;

                    if (index + 1 < _dateTimes.Count)   //time before market open and not a last bar
                    {
                        DateTime next_dt = _dateTimes[index + 1];
                        DateTime next_dte = TimeZoneInfo.ConvertTime(next_dt, EasternZone);
                        TimeSpan next_time = next_dte.TimeOfDay;

                        if ((next_time > marketOpen && (time < marketOpen || (next_time < marketClose && next_dte.Date > dte.Date))) || time == marketClose)
                            return inMarketColor;
                        else if (time > marketClose)
                            return PostExColor;
                        else
                            return ExColor;
                    }
                    else
                        return inMarketColor;   //last bar
                }
            }
            else
            {
                return null;
            }
        }
    }
}
