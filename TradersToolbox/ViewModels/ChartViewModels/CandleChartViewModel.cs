using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Printing.Core.PdfExport.Metafile;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Editors.Internal;
using DevExpress.XtraPrinting.Native;
using SciChart.Charting;
using SciChart.Charting.ChartModifiers;
using SciChart.Charting.Model.ChartData;
using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Visuals.Annotations;
using SciChart.Charting.Visuals.RenderableSeries;
using SciChart.Charting.Visuals.TradeChart;
using SciChart.Core.Extensions;
using SciChart.Data.Model;
using StockPatternSearch;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using TradersToolbox.ChartIndicators;
using TradersToolbox.CustomAnnotations;
using TradersToolbox.Data;
using TradersToolbox.DataSources;
using TradersToolbox.SciChartModifiers;
using TradersToolbox.SeriesInfos;
using TradersToolbox.ViewModels.DialogsViewModels;

namespace TradersToolbox.ViewModels.ChartViewModels
{
    [POCOViewModel]
    public class CandleChartViewModel : IChartViewModel, IChildPane
    {
        private OhlcDataSeries<DateTime, double> CandleDataSeries { get; set; }
        private XyDataSeries<DateTime, double> LineDataSeries { get; set; }

        public ChartViewModelType Type => ChartViewModelType.StockChart;

        public virtual string Title { get; set; }
        public virtual ICommand ClosePaneCommand { get; set; }

        public virtual StockChartGroupViewModel Parent { get; set; }
        private ObservableCollection<TradingData> _chartDataSource { get; set; }

        public virtual Dictionary<IndicatorView, Pair<IndicatorInputType, Pair<IndicatorGraphType, List<IIndicatorField>>>> OverlayIndicators { get; set; }

        public IRange YRange {get;set;}

        private AxisMarkerAnnotation AxisAnnotation;

        public virtual List<AnnotationData> AnnotationDatas { get; set; }

        public virtual IndicatorView IndicatorView { get; set; }

        public ObservableCollection<OrderData> OpenOrders_lists;

        public TimeSpan marketOpen = new TimeSpan(9, 30, 0);

        public TimeSpan marketClose = new TimeSpan(16, 0, 0);

        public TimeZoneInfo EasternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

        public CandleChartViewModel(StockChartGroupViewModel Parent, ObservableCollection<TradingData> ChartDataSource)
        {
            _chartDataSource = ChartDataSource;
            _chartDataSource.CollectionChanged -= ChartDataSource_CollectionChanged;
            _chartDataSource.CollectionChanged += ChartDataSource_CollectionChanged;
            CandleDataSeries = new OhlcDataSeries<DateTime, double>();
            LineDataSeries = new XyDataSeries<DateTime, double>();
            this.Parent = Parent;

            PatternStyles = new Dictionary<IndicatorView, Pair<PatternValues, List<PatternResult>>>();

            AxisAnnotation = new AxisMarkerAnnotation();
            AxisAnnotation.Foreground = new SolidColorBrush(Colors.White);
            AxisAnnotation.AnnotationCanvas = AnnotationCanvas.YAxis;
            AxisAnnotation.Y1 = 0.0;

            OverlayIndicators = new Dictionary<IndicatorView, Pair<IndicatorInputType, Pair<IndicatorGraphType, List<IIndicatorField>>>>();
            AnnotationDatas = new List<AnnotationData>();

            OpenOrders_lists = new ObservableCollection<OrderData>();

            Messenger.Default.Register<QuoteReceivedMessage>(this, OnMessage);

       //     _ = RequestLiveDataAsync();

        }

        public virtual bool IsSubGraph { get; set; }
        public bool IsMainGraph => !IsSubGraph;
        public CandleChartViewModel(StockChartGroupViewModel Parent, ObservableCollection<TradingData> ChartDataSource, IndicatorView indicatorView)
        {
            IndicatorView = indicatorView;
            Title = "VIX";

            IsSubGraph = true;

            ClosePaneCommand = new SerializableActionCommand(() => {
                Parent.RemoveIndicator(indicatorView);
            });


            _chartDataSource = ChartDataSource;
            _chartDataSource.CollectionChanged -= ChartDataSource_CollectionChanged;
            _chartDataSource.CollectionChanged += ChartDataSource_CollectionChanged;
            CandleDataSeries = new OhlcDataSeries<DateTime, double>();
            this.Parent = Parent;

            AxisAnnotation = new AxisMarkerAnnotation();
            AxisAnnotation.Foreground = new SolidColorBrush(Colors.White);
            AxisAnnotation.AnnotationCanvas = AnnotationCanvas.YAxis;
            AxisAnnotation.Y1 = 0.0;

            OverlayIndicators = new Dictionary<IndicatorView, Pair<IndicatorInputType, Pair<IndicatorGraphType, List<IIndicatorField>>>>();
            AnnotationDatas = new List<AnnotationData>();
        }

        public CandleChartViewModel()
        {
            OverlayIndicators = new Dictionary<IndicatorView, Pair<IndicatorInputType, Pair<IndicatorGraphType, List<IIndicatorField>>>>();
            AnnotationDatas = new List<AnnotationData>();
        }

        private bool IsReplaced = false;
        private ObservableCollection<TradingData> _tradingData = new ObservableCollection<TradingData>();
        private void ChartDataSource_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (sender is ObservableCollection<TradingData> tradingData && tradingData.Count > 0)
            {
                _tradingData = tradingData;
                
                Parent.Parent.chartInfo.volumeValue = "Volume : " + getDayVolume(tradingData[_tradingData.Count - 1].Date).ToString("N0", CultureInfo.InvariantCulture);
                Parent.Parent.chartInfo.averagevolumeValue = "Average Volume : " + getAverageDayVolume(tradingData[_tradingData.Count - 1].Date).ToString("N0", CultureInfo.InvariantCulture);

                double lastTradePrice = getDayClose(tradingData[_tradingData.Count - 1].Date);
                double preDailyPrice = getDayClose(tradingData[_tradingData.Count - 1].Date.AddDays(-1));
                Parent.Parent.chartInfo.changeValue = "Change : " + (lastTradePrice - preDailyPrice).ToString("N2", CultureInfo.InvariantCulture);
                Parent.Parent.chartInfo.percentValue = "Percent : " + ((lastTradePrice - preDailyPrice) / preDailyPrice * 100).ToString("N2", CultureInfo.InvariantCulture) + "%";

                foreach (var indicator in OverlayIndicators)
                {
                    if (indicator.Value.Second.First != IndicatorGraphType.MultiLine3)
                    {
                        var renderSeries = Surface.RenderableSeries.First(var => var is BaseRenderableSeries && (string)(var as BaseRenderableSeries).Tag != null && ((string)(var as BaseRenderableSeries).Tag).Contains(indicator.Key.SubtTitle + indicator.Key.ID.ToString()));
                        Console.Write(renderSeries);
                        renderSeries.DataSeries = IndicatorUtils.GetDataSeriesForIndicator(indicator.Key.Type, tradingData, indicator.Value.First, indicator.Value.Second.Second).First();
                    }
                    else
                    {
                        var renderSeries = Surface.RenderableSeries.Where(var => var is BaseRenderableSeries && (string)(var as BaseRenderableSeries).Tag != null && ((string)(var as BaseRenderableSeries).Tag).Contains(indicator.Key.SubtTitle + indicator.Key.ID.ToString())).ToList();
                        var data = IndicatorUtils.GetDataSeriesForIndicator(indicator.Key.Type, tradingData, indicator.Value.First, indicator.Value.Second.Second);
                        renderSeries[0].DataSeries = data[0];
                        renderSeries[1].DataSeries = data[1];
                        renderSeries[2].DataSeries = data[2];
                    }
                }
            }
            if (Parent.StartShadeMode == "None")
            {
                var va = ((ObservableCollection<TradingData>)sender);
                if (va.Count != 0)
                {
                    TimeSpan tisp = TimeZoneInfo.ConvertTime(va[0].Date, EasternZone).TimeOfDay;

                    if (Parent.SelectedIntervalIndex < 10 && (tisp < marketOpen || tisp > marketClose))
                        return;
                }
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (Parent.ChartData.Type != MainChartDataType.Line)
                    {
                        foreach (TradingData data in e.NewItems)
                        {
                            int index = CandleDataSeries.FindIndex(data.Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);
                            var ind = (index == -1) ? 0 : (index == 0 && CandleDataSeries.XValues[index] > data.Date) ? index : index + 1;
                            CandleDataSeries.Insert(ind, data.Date, data.Open, data.High, data.Low, data.Close);
                        }

                        if (!IsReplaced)
                        {
                            if ((Parent.SharedXRange as IndexRange).Min > CandleDataSeries.Count)
                            { 
                                (Parent.SharedXRange as IndexRange).Min = 0;
                                Parent.SharedYRange.Min = 0;
                            }
                            (Parent.SharedXRange as IndexRange).Max = CandleDataSeries.Count;
                            Parent.SharedYRange.SetMinMax(CandleDataSeries.YValues.Min(), CandleDataSeries.YValues.Max());

                            Surface?.ZoomExtents();
                        }
                        else
                        {
                            (Parent.SharedXRange as IndexRange).Min++;
                            (Parent.SharedXRange as IndexRange).Max++;
                            Parent.SharedYRange.SetMinMax(CandleDataSeries.YValues.Min(), CandleDataSeries.YValues.Max());
                        }

                        {
                            bool isGreen = CandleDataSeries.CloseValues.LastOrDefault() >= CandleDataSeries.OpenValues.LastOrDefault();
                            AxisAnnotation.IsEnabled = true;
                            AxisAnnotation.Background = isGreen ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
                            AxisAnnotation.Y1 = CandleDataSeries.CloseValues.LastOrDefault();

                        }
                    }
                    else
                    {
                        foreach (TradingData data in e.NewItems)
                        {
                            int index = LineDataSeries.FindIndex(data.Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);
                            var ind = (index == -1) ? 0 : (index == 0 && LineDataSeries.XValues[index] > data.Date) ? index : index + 1;
                            LineDataSeries.Insert(ind, data.Date, data.Close);
                        }

                        if (!IsReplaced)
                        {
                            if ((Parent.SharedXRange as IndexRange).Min > LineDataSeries.Count)
                                (Parent.SharedXRange as IndexRange).Min = 0;
                            (Parent.SharedXRange as IndexRange).Max = LineDataSeries.Count;
                            Parent.SharedYRange.SetMinMax(CandleDataSeries.YValues.Min(), CandleDataSeries.YValues.Max());
                            Surface?.ZoomExtents();
                        }
                        else
                        {
                            (Parent.SharedXRange as IndexRange).Min++;
                            (Parent.SharedXRange as IndexRange).Max++;
                           Parent.SharedYRange.SetMinMax(CandleDataSeries.YValues.Min(), CandleDataSeries.YValues.Max());
                        }
                        {
                            AxisAnnotation.IsEnabled = true;
                            AxisAnnotation.Y1 = LineDataSeries.YValues.LastOrDefault();

                        }
                    }

                    Task.Run(() => {
                        if (PatternStyles != null && PatternStyles.Count > 0)
                        {
                            foreach (var pattern in PatternStyles)
                            {
                                var values = pattern.Value.First;
                                pattern.Value.Second = StockFactory.SearchPattern(_tradingData.OrderBy(var => var.Date).ToList(), values.RetraceZ, values.RetracePerO, values.UseVol, values.MinWidth, values.MaxWidth, values.LoosePercent, values.SwingStrength);

                                App.Current.Dispatcher.Invoke(() =>
                                {
                                    (Surface?.Annotations?.FirstOrDefault(var => var.DataContext != null && var.DataContext is PatternCompositeViewModels && (var.DataContext as PatternCompositeViewModels).PatternType == pattern.Key.PatternType).DataContext as PatternCompositeViewModels)?.UpdateAnnotations(pattern.Value.Second);
                                });
                            }
                        }
                    });

                    break;
                case NotifyCollectionChangedAction.Replace:

                    if (Parent.ChartData.Type != MainChartDataType.Line)
                    {
                        foreach (TradingData data in e.NewItems)
                        {
                            CandleDataSeries.Update(data.Date, data.Open, data.High, data.Low, data.Close);

                            //int index = CandleDataSeries.FindIndex(data.Date, SciChart.Charting.Common.Extensions.SearchMode.Exact);
                            //CandleDataSeries.RemoveAt(index);
                            //if (index < CandleDataSeries.Count)
                            //    CandleDataSeries.Insert(index, data.Date, data.Open, data.High, data.Low, data.Close);
                            //else
                            //    CandleDataSeries.Append(data.Date, data.Open, data.High, data.Low, data.Close);
                        }
                        IsReplaced = true;
                        {
                            bool isGreen = CandleDataSeries.CloseValues.LastOrDefault() >= CandleDataSeries.OpenValues.LastOrDefault();
                            AxisAnnotation.IsEnabled = true;
                            AxisAnnotation.Background = isGreen ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
                            AxisAnnotation.Y1 = CandleDataSeries.CloseValues.LastOrDefault();
                        }
                    }
                    else
                    {
                        foreach (TradingData data in e.NewItems)
                        {
                            LineDataSeries.Update(data.Date, data.Close);

                            //int index = LineDataSeries.FindIndex(data.Date, SciChart.Charting.Common.Extensions.SearchMode.Exact);
                            //LineDataSeries.RemoveAt(index);
                            //if (index < LineDataSeries.Count)
                            //    LineDataSeries.Insert(index, data.Date, data.Close);
                            //else
                            //    LineDataSeries.Append(data.Date, data.Close);
                        }
                        IsReplaced = true;
                        {
                            AxisAnnotation.IsEnabled = true;
                            AxisAnnotation.Y1 = LineDataSeries.YValues.LastOrDefault();
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    Application.Current?.Dispatcher.Invoke(() => {
                        AxisAnnotation.IsEnabled = false;
                        CandleDataSeries.Clear();
                        LineDataSeries.Clear();
                        Surface?.Annotations?.Clear();
                        Surface?.Annotations?.Add(AxisAnnotation);

                        var patterns = PatternStyles.ToList();
                        foreach (var pattern in patterns)
                        {
                            //Parent.RemoveIndicator(pattern.Key);
                        }

                        (Parent.SharedXRange as IndexRange).Min = 0;
                        (Parent.SharedXRange as IndexRange).Max = 1;
                        Parent.SharedYRange.Min = 0;
                        Parent.SharedYRange.Max = 1;

                        IsReplaced = false;
//                        YRange = null;
                    });
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (Parent.ChartData.Type != MainChartDataType.Line)
                        foreach (TradingData data in e.OldItems)
                        {
                            CandleDataSeries.Remove(data.Date);
                        }
                    else
                    {
                        foreach (TradingData data in e.OldItems)
                        {
                            LineDataSeries.Remove(data.Date);
                        }
                    }
                    break;
            }
        }

        public void SetShadeMode(string ShadeMode)
        {
            if (ShadeMode == "Shaded")
            {
                Surface.RenderableSeries[0].PaletteProvider = new TransparencyFillPaletteProvider(1);
            }
            else
            {
                Surface.RenderableSeries[0].PaletteProvider = new TransparencyFillPaletteProvider(0);
            }
        }
        private SciStockChart Surface;
        private CursorModifier CursorModifier;
        private MasterRolloverModifier MasterRolloverModifier;
        public void Loaded(object surface)
        {
            if (surface is SciStockChart stock)
            {
                Surface = stock;

                VisualXcceleratorEngine.SetIsEnabled(Surface, true);

                CursorModifier = (Surface.ChartModifier as ModifierGroup).ChildModifiers.First(var => var is CursorModifier) as CursorModifier;

                MasterRolloverModifier = (Surface.ChartModifier as ModifierGroup).ChildModifiers.First(var => var is MasterRolloverModifier) as MasterRolloverModifier;

                Surface.Annotations = new SciChart.Charting.Visuals.Annotations.AnnotationCollection();
                Surface.Annotations.Add(AxisAnnotation);

                Surface.Background = new SolidColorBrush(Parent.ChartData.BackgroundColor);

                /*Surface.XAxis.DrawMajorBands = false;
                Surface.XAxis.DrawMajorGridLines = false;
                Surface.XAxis.DrawMinorGridLines = false;

                Surface.YAxis.DrawMajorBands = false;
                Surface.YAxis.DrawMajorGridLines = false;
                Surface.YAxis.DrawMinorGridLines = false;*/

                Console.WriteLine("ChartCandle" + IsMainGraph.ToString() + " DataSeries Count :" + CandleDataSeries.Count.ToString());
                //TODO VIX
                if (IsMainGraph)
                {
                    switch (Parent.ChartData.Type)
                    {
                        case MainChartDataType.Candle:
                            {
                                var candleRender = new FastCandlestickRenderableSeries() { DataSeries = CandleDataSeries, AntiAliasing = false };
                                MasterRolloverModifier.SetTooltipTemplate(candleRender, Surface.TryFindResource("OhlcTooltipTemplate") as DataTemplate);
                                MasterRolloverModifier.SetTooltipContainerStyle(candleRender, Surface.TryFindResource("TooltipContainerStyle") as Style);

                                candleRender.FillUp = new SolidColorBrush(Parent.ChartData.UpColor);
                                candleRender.FillDown = new SolidColorBrush(Parent.ChartData.DownColor);
                                candleRender.StrokeUp = Parent.ChartData.UpColor;
                                candleRender.StrokeDown = Parent.ChartData.DownColor;

                                if (Parent.StartShadeMode == "Shaded")
                                    candleRender.PaletteProvider = new TransparencyFillPaletteProvider(1);
                                else
                                    candleRender.PaletteProvider = new TransparencyFillPaletteProvider();

                                Surface.RenderableSeries.Insert(0, candleRender);
                                if (Surface.RenderableSeries.Count > 1)
                                    Surface.RenderableSeries.RemoveAt(1);
                            }
                            break;
                        case MainChartDataType.HollowCandle:
                            {
                                var candleRender = new FastCandlestickRenderableSeries() { DataSeries = CandleDataSeries, AntiAliasing = false };
                                MasterRolloverModifier.SetTooltipTemplate(candleRender, Surface.TryFindResource("OhlcTooltipTemplate") as DataTemplate);
                                MasterRolloverModifier.SetTooltipContainerStyle(candleRender, Surface.TryFindResource("TooltipContainerStyle") as Style);

                                candleRender.FillUp = new SolidColorBrush(Colors.Transparent);
                                candleRender.FillDown = new SolidColorBrush(Colors.Transparent);
                                candleRender.StrokeUp = Parent.ChartData.UpColor;
                                candleRender.StrokeDown = Parent.ChartData.DownColor;

                                candleRender.PaletteProvider = new HollowFillPaletteProvider();
                                Surface.RenderableSeries.Insert(0, candleRender);
                                if (Surface.RenderableSeries.Count > 1)
                                    Surface.RenderableSeries.RemoveAt(1);
                            }
                            break;
                        case MainChartDataType.PriceBar:
                            {
                                var candleRender = new FastOhlcRenderableSeries() { DataSeries = CandleDataSeries, AntiAliasing = false };
                                MasterRolloverModifier.SetTooltipTemplate(candleRender, Surface.TryFindResource("OhlcTooltipTemplate") as DataTemplate);
                                MasterRolloverModifier.SetTooltipContainerStyle(candleRender, Surface.TryFindResource("TooltipContainerStyle") as Style);

                                candleRender.StrokeUp = Parent.ChartData.UpColor;
                                candleRender.StrokeDown = Parent.ChartData.DownColor;

                                candleRender.PaletteProvider = new TransparencyFillPaletteProvider();

                                Surface.RenderableSeries.Insert(0,candleRender);
                                if (Surface.RenderableSeries.Count > 1)
                                    Surface.RenderableSeries.RemoveAt(1);
                            }
                            break;
                        case MainChartDataType.Line:
                            {
                                var renderSeries = new FastLineRenderableSeries() { DataSeries = LineDataSeries, AntiAliasing = false };
                                MasterRolloverModifier.SetTooltipTemplate(renderSeries, Surface.TryFindResource("LineCloseTooltipTemplate") as DataTemplate);
                                MasterRolloverModifier.SetTooltipContainerStyle(renderSeries, Surface.TryFindResource("TooltipContainerStyle") as Style);

                                renderSeries.Stroke = Parent.ChartData.LineColor;

                                //renderSeries.PaletteProvider = new TransparencyFillPaletteProvider();
                                Surface.RenderableSeries.Insert(0, renderSeries);
                                if (Surface.RenderableSeries.Count > 1)
                                    Surface.RenderableSeries.RemoveAt(1);
                            }
                            break;
                    }
                }
                else
                {
                    var candleRender = new FastCandlestickRenderableSeries() { DataSeries = CandleDataSeries, AntiAliasing = false };

                    Surface.RenderableSeries.Clear();
                    Surface.RenderableSeries.Add(candleRender);
                }

                //TODO add patterns
                foreach (var pattern in PatternStyles)
                    AddOverlayPattern(pattern.Key, pattern.Value.First, false);

                if (Surface.RenderableSeries.Count - 1 != OverlayIndicators.Count)
                {
                    foreach (var indicator in OverlayIndicators)
                    {
                        AddOverlay(indicator.Key, indicator.Value.First, indicator.Value.Second.First, indicator.Value.Second.Second, false);
                    }
                }

                foreach (var data in AnnotationDatas)
                    AddAnnotation(data.Type, data);
            }
        }

        public void AddRolloverModifyer(RolloverModifier modifier)
        {
            MasterRolloverModifier?.RegisterChildModifiers(modifier);
        }
        public void RemoveRolloverModifyer(RolloverModifier modifier)
        {
            MasterRolloverModifier?.RemoveChildModifiers(modifier);
        }


        public void InitAfterLoad(StockChartGroupViewModel Parent, ObservableCollection<TradingData> ChartDataSource)
        {
            _chartDataSource = ChartDataSource;
            _chartDataSource.CollectionChanged -= ChartDataSource_CollectionChanged;
            _chartDataSource.CollectionChanged += ChartDataSource_CollectionChanged;
            CandleDataSeries = new OhlcDataSeries<DateTime, double>();
            LineDataSeries = new XyDataSeries<DateTime, double>();

            if (Parent.ChartData.Type != MainChartDataType.Line)
                foreach (var data in _chartDataSource.OrderBy(var => var.Date))
                    CandleDataSeries.Append(data.Date, data.Open, data.High, data.Low, data.Close);
            else
                foreach (var data in _chartDataSource.OrderBy(var => var.Date))
                    LineDataSeries.Append(data.Date, data.Close);

            this.Parent = Parent;


            AxisAnnotation = new AxisMarkerAnnotation();
            AxisAnnotation.Foreground = new SolidColorBrush(Colors.White);
            AxisAnnotation.AnnotationCanvas = AnnotationCanvas.YAxis;
            AxisAnnotation.Y1 = 0.0;

            if (IndicatorView != null)
                ClosePaneCommand = new SerializableActionCommand(() => {
                    Parent.RemoveIndicator(IndicatorView);
                });
        }

        public void RemoveLineAnnotation(string tag)
        {
            Surface?.Annotations.RemoveWhere(var => (var is LineAnnotation) && (var as LineAnnotation).Tag.ToString() == tag);
            AnnotationDatas.RemoveWhere(var=>var.Tag == tag);
        }

        public void ShowOrdersAnnotation(bool visible)
        {
            if (visible)
                AddOrdersAnnotataion();
            else
                ClearOrdersAnnotation();
        }
        
        public void ReDrawOrdersAnnotation()
        {
            ClearOrdersAnnotation();
            AddOrdersAnnotataion();
        }
        public void ClearOrdersAnnotation()
        {
            if (Surface?.Annotations == null)
                return;

            var annotation_list = new AnnotationCollection(Surface?.Annotations);
            Surface?.Annotations.Clear();
            for(int index = 0; index < annotation_list.Count; index++)
            {
                var annotation = annotation_list[index];
                if (annotation is HorizontalLineAnnotation)
                    if (((HorizontalLineAnnotation)annotation).Tag == "OpenOrders")
                        continue;
                if (annotation is AxisMarkerAnnotation)
                    if (((AxisMarkerAnnotation)annotation).Tag == "OpenOrders")
                        continue;
                if (annotation is TextAnnotation)
                    if (((TextAnnotation)annotation).Tag == "OpenOrders")
                        continue;

                Surface?.Annotations.Add(annotation);
            }
        }

        public void AddOrdersAnnotataion()
        {
            string tag = "OpenOrders";
            int order_index;
            OpenOrders_lists = MainWindowViewModel.tradeDB.getOpenOrderRecordsAll();

            for (order_index = 0; order_index < OpenOrders_lists.Count; order_index++)
            {
                OrderData od = OpenOrders_lists[order_index];
                if (od.Symbol != Parent.SymbolName)
                    continue;

                double yValue = od.Open_Price;
                Color annotation_color;
                string label_string;
                if (od.Type.Contains("Limit"))
                    label_string = "L:" + od.Shares.ToString();
                else
                    label_string = "S:" + od.Shares.ToString();
                if (od.Type.Contains("Buy"))
                    annotation_color = Colors.Green;
                else
                    annotation_color = Colors.Red;

                double yMaxVal = (YRange as Range<double>).Max;
                double yMinVal = (YRange as Range<double>).Min;
                if (yMaxVal < yValue)
                    (YRange as Range<double>).Max = yValue;
                if (yMinVal > yValue)
                    (YRange as Range<double>).Min = yValue;

                var lineAnnotation = new HorizontalLineAnnotation() { Tag = tag, Stroke = new SolidColorBrush(annotation_color), StrokeThickness = 1.0, Y1 = yValue, X1 = (Parent.SharedXRange as IndexRange).Max - 5 };
                var txtAnnotation = new TextAnnotation() { Tag = tag, Text = label_string, FontSize=12, Foreground = new SolidColorBrush(annotation_color), X1 = (Parent.SharedXRange as IndexRange).Max - 5, Y1 = yValue};
                AxisMarkerAnnotation orderAxisAnnotation;

                orderAxisAnnotation = new AxisMarkerAnnotation();
                orderAxisAnnotation.Background = new SolidColorBrush(annotation_color);
                orderAxisAnnotation.Y1 = yValue;
                orderAxisAnnotation.IsEnabled = true;
                orderAxisAnnotation.Tag = tag;
                orderAxisAnnotation.Foreground = new SolidColorBrush(Colors.White);
                lineAnnotation.HorizontalAlignment = HorizontalAlignment.Right;

                Surface?.Annotations.Add(lineAnnotation);
                Surface?.Annotations.Add(txtAnnotation);
                Surface?.Annotations.Add(orderAxisAnnotation);
                
                Surface?.ZoomExtentsY();
            }
        }
        public string AddAnnotation(AnnotationType type, AnnotationData annotation = null)
        {
            string tag = Guid.NewGuid().ToString();
            switch (type)
            {
                case AnnotationType.Horizontal:
                    var horizontalAnnotation = new HorizontalLineAnnotation() { Tag = tag, IsEditable = true, Stroke = new SolidColorBrush(Colors.Orange), StrokeThickness = 2.0, Y1 = GetYCenter(), AnnotationLabels = new ObservableCollection<AnnotationLabel>() { new AnnotationLabel { LabelPlacement = LabelPlacement.Axis, FontSize = 16, Foreground = new SolidColorBrush(Colors.White) } } };
                    {
                        horizontalAnnotation.DragStarted += (s, e) =>
                        {
                            CursorModifier.IsEnabled = false;
                        };
                        horizontalAnnotation.MouseDoubleClick += (s, e) => {

                            List<UICommand> commands = new List<UICommand>();

                            commands.Add(new UICommand(1, "Ok", null, true, false));
                            commands.Add(new UICommand(2, "Cancel", null, false, false));
                            ThemedWindow window = new ThemedWindow();
                            window.Width = 200;
                            window.Height = 400;
                            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
                            var picker = new ColorEdit();
                            picker.Color = horizontalAnnotation.Stroke.ExtractColor();
                            window.Content = picker;
                            window.ShowDialog(commands);

                            if (window.DialogButtonCommandResult != null)
                            {
                                if ((int)window.DialogButtonCommandResult.Id ==  1)
                                {
                                    horizontalAnnotation.Stroke = new SolidColorBrush(picker.Color);
                                }
                            }
                        };
                        horizontalAnnotation.DragEnded += (s, e) =>
                        {
                            CursorModifier.IsEnabled = true;

                            int? index = Surface?.Annotations.IndexOf(horizontalAnnotation) - 1;
                            if (index < 0)
                                index = 0;
                            AnnotationDatas[index.Value].Y1 = (double)horizontalAnnotation.Y1;
                        };
                        Application.Current.MainWindow.KeyUp += (s, e) =>
                        {
                            if (e.Key == Key.Delete && horizontalAnnotation.IsSelected)
                            {
                                Parent.RemoveIndicator(Parent.SelectedIndicatorViews.First(var => var.Tag == horizontalAnnotation.Tag.ToString()));
                                CursorModifier.IsEnabled = true;
                            }
                        };

                        if (annotation != null)
                        {
                            horizontalAnnotation.Y1 = annotation.Y1;
                        }
                        else
                        {
                            AnnotationDatas.Add(new AnnotationData()
                            {
                                Type = type,
                                Y1 = (double)horizontalAnnotation.Y1,
                                Tag = tag,
                            });
                        }

                        Surface?.Annotations.Add(horizontalAnnotation);
                    }
                    break;
                case AnnotationType.Vertical:
                    var verticalAnnotation = new VerticalLineAnnotation() { Tag = tag, IsEditable = true, Stroke = new SolidColorBrush(Colors.Orange), StrokeThickness = 2.0, X1 = GetXCenter(), AnnotationLabels = new ObservableCollection<AnnotationLabel>() { new AnnotationLabel { LabelPlacement = LabelPlacement.Axis, FontSize = 16, Foreground = new SolidColorBrush(Colors.White) } } };
                    {

                        verticalAnnotation.DragStarted += (s, e) =>
                        {
                            CursorModifier.IsEnabled = false;
                        };
                        verticalAnnotation.MouseDoubleClick += (s, e) => {

                            List<UICommand> commands = new List<UICommand>();

                            commands.Add(new UICommand(1, "Ok", null, true, false));
                            commands.Add(new UICommand(2, "Cancel", null, false, false));
                            ThemedWindow window = new ThemedWindow();
                            window.Width = 200;
                            window.Height = 400;
                            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
                            var picker = new ColorEdit();
                            picker.Color = verticalAnnotation.Stroke.ExtractColor();
                            window.Content = picker;
                            window.ShowDialog(commands);

                            if (window.DialogButtonCommandResult != null)
                            {
                                if ((int)window.DialogButtonCommandResult.Id == 1)
                                {
                                    verticalAnnotation.Stroke = new SolidColorBrush(picker.Color);
                                }
                            }
                        };
                        verticalAnnotation.DragEnded += (s, e) =>
                        {
                            CursorModifier.IsEnabled = true;

                            int? index = Surface?.Annotations.IndexOf(verticalAnnotation) - 1;
                            if (index < 0)
                                index = 0;
                            AnnotationDatas[index.Value].X1 = (int)verticalAnnotation.X1;
                        };
                        Application.Current.MainWindow.KeyUp += (s, e) =>
                        {
                            if (e.Key == Key.Delete && verticalAnnotation.IsSelected)
                            {
                                Parent.RemoveIndicator(Parent.SelectedIndicatorViews.First(var => var.Tag == verticalAnnotation.Tag.ToString()));
                                CursorModifier.IsEnabled = true;
                            }
                        };

                        if (annotation != null)
                        {
                            verticalAnnotation.X1 = annotation.X1;
                        }
                        else
                        {
                            AnnotationDatas.Add(new AnnotationData()
                            {
                                Type = type,
                                X1 = (int)verticalAnnotation.X1,
                                Tag = tag,
                            });
                        }

                        Surface?.Annotations.Add(verticalAnnotation);
                    }
                    break;
                case AnnotationType.Angled:
                    var lineAnnotation = new LineAnnotation() { Tag = tag, IsEditable = true, Stroke = new SolidColorBrush(Colors.Orange), StrokeThickness = 2.0, X1 = (Parent.SharedXRange as IndexRange).Min, Y1 = (YRange as Range<double>).Min, X2 = GetXCenter(), Y2 = GetYCenter() };
                    {

                        lineAnnotation.DragStarted += (s, e) =>
                        {
                            CursorModifier.IsEnabled = false;
                        };
                        lineAnnotation.MouseDoubleClick += (s, e) => {

                            List<UICommand> commands = new List<UICommand>();

                            commands.Add(new UICommand(1, "Ok", null, true, false));
                            commands.Add(new UICommand(2, "Cancel", null, false, false));
                            ThemedWindow window = new ThemedWindow();
                            window.Width = 200;
                            window.Height = 400;
                            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
                            var picker = new ColorEdit();
                            picker.Color = lineAnnotation.Stroke.ExtractColor();
                            window.Content = picker;
                            window.ShowDialog(commands);

                            if (window.DialogButtonCommandResult != null)
                            {
                                if ((int)window.DialogButtonCommandResult.Id == 1)
                                {
                                    lineAnnotation.Stroke = new SolidColorBrush(picker.Color);
                                }
                            }
                        };
                        lineAnnotation.DragEnded += (s, e) =>
                        {
                            CursorModifier.IsEnabled = true;

                            int? index = Surface?.Annotations.IndexOf(lineAnnotation) - 1;
                            if (index < 0)
                                index = 0;
                            AnnotationDatas[index.Value].X1 = (int)lineAnnotation.X1;
                            AnnotationDatas[index.Value].X2 = (int)lineAnnotation.X2;
                            AnnotationDatas[index.Value].Y1 = (double)lineAnnotation.Y1;
                            AnnotationDatas[index.Value].Y2 = (double)lineAnnotation.Y2;
                        };

                        Application.Current.MainWindow.KeyUp += (s, e) =>
                        {
                            if (e.Key == Key.Delete && lineAnnotation.IsSelected)
                            {

                                Parent.RemoveIndicator(Parent.SelectedIndicatorViews.First(var => var.Tag == lineAnnotation.Tag.ToString()));
                                CursorModifier.IsEnabled = true;
                            }
                        };

                        if (annotation != null)
                        {
                            lineAnnotation.X1 = annotation.X1;
                            lineAnnotation.X2 = annotation.X2;
                            lineAnnotation.Y1 = annotation.Y1;
                            lineAnnotation.Y2 = annotation.Y2;
                        }
                        else
                        {
                            AnnotationDatas.Add(new AnnotationData()
                            {
                                Type = type,
                                X1 = (int)lineAnnotation.X1,
                                X2 = (int)lineAnnotation.X2,
                                Y1 = (double)lineAnnotation.Y1,
                                Y2 = (double)lineAnnotation.Y2,
                                Tag = tag,
                            });
                        }

                        Surface?.Annotations.Add(lineAnnotation);
                    }
                    break;
            }
            return tag;
        }

        public void UpdateFormatData(ObservableCollection<TradingData> data, MainChartData chartdata)
        {
            _chartDataSource.CollectionChanged -= ChartDataSource_CollectionChanged;
            _chartDataSource = data;
            _chartDataSource.CollectionChanged += ChartDataSource_CollectionChanged;
            Parent.ChartData = chartdata;
            Loaded(Surface);
        }

        private IComparable GetYCenter()
        {
            return ((YRange as Range<double>).Max + (YRange as Range<double>).Min) / 2;
        }

        private int GetXCenter()
        {
            return ((Parent.SharedXRange as IndexRange).Max + (Parent.SharedXRange as IndexRange).Min) / 2;
        }

        public void ZoomExtents()
        {
        }

        public void AddOverlay(IndicatorView indicatorView, IndicatorInputType inputType, IndicatorGraphType indicatorGraphType, List<IIndicatorField> fields, bool IsAddToCollection = true)
        {
            indicatorView.SubtTitle = "Overlay-" + indicatorGraphType.ToString() + "-" + inputType.ToString();
            if (_chartDataSource.Count == 0)
                return;
            var indicatorDataSeries = IndicatorUtils.GetDataSeriesForIndicator(indicatorView.Type, _chartDataSource, inputType, fields);
            if (IsAddToCollection)
                OverlayIndicators.Add(indicatorView, new Pair<IndicatorInputType, Pair<IndicatorGraphType, List<IIndicatorField>>>(inputType, new Pair<IndicatorGraphType, List<IIndicatorField>>(indicatorGraphType, fields)));

            switch (indicatorGraphType)
            {
                case IndicatorGraphType.Mountain:
                    {
                        var renderSeries = new FastMountainRenderableSeries() { DataSeries = indicatorDataSeries.First(), Tag = indicatorView.SubtTitle + indicatorView.ID.ToString() + "!" + indicatorView.Type, RolloverMarkerTemplate = Surface.TryFindResource("MarkerTemplate") as ControlTemplate };
                        MasterRolloverModifier.SetTooltipTemplate(renderSeries, Surface.TryFindResource("EmptyTooltipTemplate") as DataTemplate);
                        Surface.RenderableSeries.Add(renderSeries);
                    }
                    break;
                case IndicatorGraphType.Line:
                    {
                        var renderSeries = new FastLineRenderableSeries() { DataSeries = indicatorDataSeries.First(), Tag = indicatorView.SubtTitle + indicatorView.ID.ToString() + "!" + indicatorView.Type, RolloverMarkerTemplate = Surface.TryFindResource("MarkerTemplate") as ControlTemplate };
                        MasterRolloverModifier.SetTooltipTemplate(renderSeries, Surface.TryFindResource("EmptyTooltipTemplate") as DataTemplate);
                        Surface.RenderableSeries.Add(renderSeries);
                    }
                    break;
                case IndicatorGraphType.StackedMountain:
                    {
                        var renderSeries = new StackedMountainRenderableSeries() { DataSeries = indicatorDataSeries.First(), Tag = indicatorView.SubtTitle + indicatorView.ID.ToString() + "!" + indicatorView.Type, RolloverMarkerTemplate = Surface.TryFindResource("MarkerTemplate") as ControlTemplate };
                        MasterRolloverModifier.SetTooltipTemplate(renderSeries, Surface.TryFindResource("EmptyTooltipTemplate") as DataTemplate);
                        Surface.RenderableSeries.Add(renderSeries);
                    }
                    break;
                case IndicatorGraphType.Column:
                    {
                        var renderSeries = new FastColumnRenderableSeries() { DataSeries = indicatorDataSeries.First(), Tag = indicatorView.SubtTitle + indicatorView.ID.ToString() + "!" + indicatorView.Type, RolloverMarkerTemplate = Surface.TryFindResource("MarkerTemplate") as ControlTemplate };
                        MasterRolloverModifier.SetTooltipTemplate(renderSeries, Surface.TryFindResource("EmptyTooltipTemplate") as DataTemplate);
                        Surface.RenderableSeries.Add(renderSeries);
                    }
                    break;
                case IndicatorGraphType.MultiLine3:
                    {
                        var renderSeries = new FastLineRenderableSeries() { DataSeries = indicatorDataSeries[0], Tag = indicatorView.SubtTitle + indicatorView.ID.ToString() + "!" + indicatorView.Type + " upper:", RolloverMarkerTemplate = Surface.TryFindResource("MarkerTemplate") as ControlTemplate };
                        MasterRolloverModifier.SetTooltipTemplate(renderSeries, Surface.TryFindResource("EmptyTooltipTemplate") as DataTemplate);
                        Surface.RenderableSeries.Add(renderSeries);
                    }
                    {
                        var renderSeries = new FastLineRenderableSeries() { DataSeries = indicatorDataSeries[1], Tag = indicatorView.SubtTitle + indicatorView.ID.ToString() + "!" + indicatorView.Type + " lower:", RolloverMarkerTemplate = Surface.TryFindResource("MarkerTemplate") as ControlTemplate };
                        MasterRolloverModifier.SetTooltipTemplate(renderSeries, Surface.TryFindResource("EmptyTooltipTemplate") as DataTemplate);
                        Surface.RenderableSeries.Add(renderSeries);
                    }
                    {
                        var renderSeries = new FastLineRenderableSeries() { DataSeries = indicatorDataSeries[2], Tag = indicatorView.SubtTitle + indicatorView.ID.ToString() + "!" + indicatorView.Type + " middle:", RolloverMarkerTemplate = Surface.TryFindResource("MarkerTemplate") as ControlTemplate };
                        MasterRolloverModifier.SetTooltipTemplate(renderSeries, Surface.TryFindResource("EmptyTooltipTemplate") as DataTemplate);
                        Surface.RenderableSeries.Add(renderSeries);
                    }
                    break;
            }
        }

        public void RemoveOverlay(IndicatorView indicatorView)
        {
            OverlayIndicators.Remove(indicatorView);
            Surface.RenderableSeries.RemoveWhere(var => var is BaseRenderableSeries && (string)(var as BaseRenderableSeries).Tag != null && ((string)(var as BaseRenderableSeries).Tag).Contains(indicatorView.SubtTitle + indicatorView.ID.ToString()));
        }

        public IEnumerable<SciChart.Charting.Model.ChartData.SeriesInfo> GetCustomSeriesInfo(IEnumerable<SciChart.Charting.Model.ChartData.SeriesInfo> toReplace)
        {
            List<SciChart.Charting.Model.ChartData.SeriesInfo> result = new List<SciChart.Charting.Model.ChartData.SeriesInfo>();

            if (Parent.ChartData.Type != MainChartDataType.Line)
            {
                var ohlc = toReplace.FirstOrDefault(var => var is OhlcSeriesInfo) as OhlcSeriesInfo;
                
                if (ohlc == null)
                    return new List<SciChart.Charting.Model.ChartData.SeriesInfo>();

                List<Pair<string, string>> data = GetAdditionalData(toReplace.Where(var => !(var is OhlcSeriesInfo)));

                var dateTime = CandleDataSeries.XValues[ohlc.DataSeriesIndex];

                result.Add(new CustomOhlcSeriesInfo(ohlc.RenderableSeries)
                {
                    PointMetadata = ohlc.PointMetadata,
                    IsHit = ohlc.IsHit,
                    XyCoordinate = ohlc.XyCoordinate,
                    AdditionalData = data,
                    Volume = _tradingData.FirstOrDefault(var => var.Date == dateTime)?.Volume.ToString(),
                    DateValue = dateTime.ToShortDateString(),
                    TimeValue = dateTime.ToShortTimeString(),
                    FormattedOpenValue = ohlc.OpenValue.ToString("F5", CultureInfo.InvariantCulture),
                    FormattedLowValue = ohlc.LowValue.ToString("F5", CultureInfo.InvariantCulture),
                    FormattedHighValue = ohlc.HighValue.ToString("F5", CultureInfo.InvariantCulture),
                    FormattedCloseValue = ohlc.CloseValue.ToString("F5", CultureInfo.InvariantCulture)
                });

                return result.AsEnumerable();
            }
            else
            {
                var line = toReplace.FirstOrDefault(var => var is XySeriesInfo) as XySeriesInfo;

                if (line == null || line.PointMetadata == null)
                    return new List<SciChart.Charting.Model.ChartData.SeriesInfo>();

                List<Pair<string, string>> data = GetAdditionalData(toReplace.Where(var => (var != line)));

                var dateTime = LineDataSeries.XValues[line.DataSeriesIndex];

                result.Add(new CustomLineSeriesInfo(line.RenderableSeries)
                {
                    PointMetadata = line.PointMetadata,
                    IsHit = line.IsHit,
                    XyCoordinate = line.XyCoordinate,
                    AdditionalData = data,
                    DateValue = dateTime.ToShortDateString(),
                    TimeValue = dateTime.ToShortTimeString(),
                    FormattedCloseValue = ((double)line.YValue).ToString("F5", CultureInfo.InvariantCulture)
                });

                return result.AsEnumerable();
            }


        }

        private List<Pair<string, string>> GetAdditionalData(IEnumerable<SeriesInfo> enumerable)
        {
            List<Pair<string, string>> pairs = new List<Pair<string, string>>();

            foreach (var info in enumerable)
            {
                if (info is XySeriesInfo xySeries && xySeries.RenderableSeries is BaseRenderableSeries renderSeries && renderSeries.Tag is string tag && tag.Contains("!"))
                {
                    pairs.Add(new Pair<string, string>(tag.Split('!')[1], xySeries.FormattedYValue));
                }
            }

            return pairs;
        }

        public void UpdateOverlayGraphType(KeyValuePair<IndicatorView, Pair<IndicatorInputType, Pair<IndicatorGraphType, List<IIndicatorField>>>> indicator, IndicatorGraphType type)
        {
            RemoveOverlay(indicator.Key);
            AddOverlay(indicator.Key, indicator.Value.First, type, indicator.Value.Second.Second);
        }


        public Dictionary<IndicatorView, Pair<PatternValues, List<PatternResult>>> PatternStyles { get; set; }

        public void AddOverlayPattern(IndicatorView indicatorView, PatternValues values, bool IsAddToCollection = true)
        {
            if (PatternStyles == null)
            {
                PatternStyles = new Dictionary<IndicatorView, Pair<PatternValues, List<PatternResult>>>();
            }

            if (IsAddToCollection)
                PatternStyles.Add(indicatorView, new Pair<PatternValues, List<PatternResult>>(values, StockFactory.SearchPattern(_tradingData.OrderBy(var => var.Date).ToList(), values.RetraceZ, values.RetracePerO, values.UseVol, values.MinWidth, values.MaxWidth, values.LoosePercent, values.SwingStrength)));

            Surface?.Annotations.Add(new PatternCompositeAnnotation() { DataContext = new PatternCompositeViewModels(this, indicatorView.PatternType, PatternStyles[indicatorView].Second, PatternStyles[indicatorView].First) });
        }

        public void RemoveOverlayPattern(IndicatorView indicatorView)
        {
            if (PatternStyles != null && PatternStyles.ContainsKey(indicatorView))
            {
                PatternStyles.Remove(indicatorView);
                Surface?.Annotations.RemoveWhere(var => var.DataContext != null && var.DataContext is PatternCompositeViewModels && (var.DataContext as PatternCompositeViewModels).PatternType == indicatorView.PatternType);
            }
        }

        private void OnMessage(QuoteReceivedMessage message)
        {
            try
            {
                if (message.Symbol != Parent.SymbolName)
                    return;

                if (!message.Last.HasValue || !message.PreviousClose.HasValue || !message.DailyVolume.HasValue)
                    return;

                DateTime preDate = DateTime.Now;
                preDate = preDate.AddDays(-30);
                

            }
            catch (TaskCanceledException) { }
        }

        public double getDayVolume(DateTime day)
        {
            double result_vol = 0;

            TradingData[] tradeArr = _tradingData.Where(var => var.Date.Day == day.Day).ToArray();

            foreach (TradingData trade in tradeArr)
                result_vol += trade.Volume;

            return result_vol;
        }

        public double getAverageDayVolume(DateTime day)
        {
            DateTime startDay = day.AddDays(-30);

            double sum_vol = 0;
            double result_vol = 0;
            int vol_day = 0;
            for(DateTime dt = startDay; dt < day; dt = dt.AddDays(1))
            { 
                sum_vol += getDayVolume(dt);
                if (getDayVolume(dt) != 0)
                    vol_day++;
            }
            result_vol = sum_vol / vol_day;

            return (int)result_vol;
        }

        public double getDayClose(DateTime day)
        {
            TradingData tradeArr = _tradingData.LastOrDefault(var => var.Date.Day == day.Day);
            if (tradeArr == null)
                tradeArr = _tradingData[0];
            return tradeArr.Close;
        }
    }

    public class MainChartData
    {
        public MainChartDataType Type { get; set; }
        public Color BackgroundColor { get; set; }
        public Color LineColor { get; set; }
        public Color UpColor { get; set; }
        public Color DownColor { get; set; }
    }

    public enum MainChartDataType
    {
        Candle, HollowCandle, PriceBar, Line
    }
    
    public class AnnotationData
    {
        public AnnotationData()
        { }
        public AnnotationType Type { get; set; }
        public int X1 { get; set; }
        public int X2 { get; set; }
        public double Y1 { get; set; }
        public double Y2 { get; set; }

        public string Tag { get; set; }
    }

    public enum AnnotationType
    {
        Horizontal,
        Vertical,
        Angled
    }


    public class SerializableActionCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;
        private Action action;

        public SerializableActionCommand()
        {

        }
        public SerializableActionCommand(Action action)
        {
            this.action = action;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            action.Invoke();
        }
    }

}
