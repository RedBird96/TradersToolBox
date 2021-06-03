using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using DevExpress.Mvvm.POCO;
using TradersToolbox.DataSources;
using DevExpress.Xpf.Charts;
using TradersToolbox.Data;
using DevExpress.Mvvm;
using TradersToolbox.Brokers;

namespace TradersToolbox.ViewModels {
    public class StockChartViewModel {
        const int initialVisiblePointsCount = 180;
        const int maxVisiblePointsCount = 800;

        public static StockChartViewModel Create() {
            return ViewModelSource.Create(() => new StockChartViewModel());
        }

        readonly IBarsStreamer dataSource;

        bool initRange = false;

        public ObservableCollection<TradingData> ChartDataSource { get { return dataSource.Data; } }

        public virtual object MinVisibleDate { get; set; }
        public virtual ChartIntervalItem SelectedInterval { get; set; }
        public List<ChartIntervalItem> IntervalsSource { get; private set; }
        public virtual string CrosshairCurrentFinancialText { get; set; }
        public virtual string CrosshairCurrentVolumeText { get; set; }
        public virtual string SymbolName { get; set; }
        public virtual double CurrentPrice { get; set; }
        public virtual Color PriceIndicatorColor{ get; set; }

        public StockChartViewModel() {
            // just create new
            var t = MainWindowViewModel.BrokersManager.GetBarsStreamer(connect: false);
            t.Wait();
            dataSource = t.Result;

            IntervalsSource = new List<ChartIntervalItem>();
            InitIntervals();
            SelectedInterval = IntervalsSource[0];

            Messenger.Default.Register<string>(this, OnMessage);
        }

        private void OnMessage(string obj)
        {
            if (obj?.Length > 1)
                _ = MainWindowViewModel.BrokersManager.GetBarsStreamer(obj.Substring(1), SelectedInterval, dataSource, true);
        }

        void InitIntervals() {
            IntervalsSource.Add(new ChartIntervalItem() { Caption = "1 minutes", MeasureUnit = DateTimeMeasureUnit.Minute, MeasureUnitMultiplier = 1 });
            IntervalsSource.Add(new ChartIntervalItem() { Caption = "2 minutes", MeasureUnit = DateTimeMeasureUnit.Minute, MeasureUnitMultiplier = 2 });
            IntervalsSource.Add(new ChartIntervalItem() { Caption = "5 minutes", MeasureUnit = DateTimeMeasureUnit.Minute, MeasureUnitMultiplier = 5 });
            IntervalsSource.Add(new ChartIntervalItem() { Caption = "15 minutes", MeasureUnit = DateTimeMeasureUnit.Minute, MeasureUnitMultiplier = 15 });
            IntervalsSource.Add(new ChartIntervalItem() { Caption = "30 minutes", MeasureUnit = DateTimeMeasureUnit.Minute, MeasureUnitMultiplier = 30 });
            //IntervalsSource.Add(new ChartIntervalItem() { Caption = "45 minutes", MeasureUnit = DateTimeMeasureUnit.Minute, MeasureUnitMultiplier = 45 });
            IntervalsSource.Add(new ChartIntervalItem() { Caption = "1 hour", MeasureUnit = DateTimeMeasureUnit.Hour, MeasureUnitMultiplier = 1 });
            IntervalsSource.Add(new ChartIntervalItem() { Caption = "2 hour", MeasureUnit = DateTimeMeasureUnit.Hour, MeasureUnitMultiplier = 2 });
            IntervalsSource.Add(new ChartIntervalItem() { Caption = "4 hour", MeasureUnit = DateTimeMeasureUnit.Hour, MeasureUnitMultiplier = 4 });
            IntervalsSource.Add(new ChartIntervalItem() { Caption = "6 hour", MeasureUnit = DateTimeMeasureUnit.Hour, MeasureUnitMultiplier = 6 });
            IntervalsSource.Add(new ChartIntervalItem() { Caption = "12 hour", MeasureUnit = DateTimeMeasureUnit.Hour, MeasureUnitMultiplier = 12 });
            IntervalsSource.Add(new ChartIntervalItem() { Caption = "1 day", MeasureUnit = DateTimeMeasureUnit.Day, MeasureUnitMultiplier = 1 });
            IntervalsSource.Add(new ChartIntervalItem() { Caption = "1 week", MeasureUnit = DateTimeMeasureUnit.Week, MeasureUnitMultiplier = 1 });
            IntervalsSource.Add(new ChartIntervalItem() { Caption = "1 month", MeasureUnit = DateTimeMeasureUnit.Month, MeasureUnitMultiplier = 1 });
        }
        
        void GenerateInitialData() {
            if (!string.IsNullOrEmpty(SymbolName))
                _ = MainWindowViewModel.BrokersManager.GetBarsStreamer(SymbolName, SelectedInterval, dataSource, true);
        }        
        void InitChartRange(ChartControl chart) {
            if (!initRange) {
                ((XYDiagram2D)chart.Diagram).ActualAxisX.ActualVisualRange.SetAuto();
                MinVisibleDate =  DateTime.Now - DateTimeHelper.ConvertInterval(SelectedInterval, initialVisiblePointsCount);
                initRange = true;
            }
        }
        void ReinitChartRange() {
            initRange = false;
        }
        void UpdateCrosshairText() {
            TradingData lastPoint = dataSource.Data.Last();
            CrosshairCurrentFinancialText = string.Format("O{0:f2}\tH{1:f2}\tL{2:f2}\tC{3:f2}\t", lastPoint.Open, lastPoint.High, lastPoint.Low, lastPoint.Close);
            CrosshairCurrentVolumeText = string.Format("{0:f2}", lastPoint.Volume);
        }
        protected void OnSelectedIntervalChanged() {
            ReinitChartRange();
            GenerateInitialData();
        }

        public void DataChanged(RoutedEventArgs e) {
            ChartControl chart = e.Source as ChartControl;
            if (chart != null)
                InitChartRange(chart);
        }
        public void ChartScroll(XYDiagram2DScrollEventArgs eventArgs) {
            //if(eventArgs.AxisX != null) {
            //    if ((DateTime)eventArgs.AxisX.ActualVisualRange.ActualMinValue < (DateTime)eventArgs.AxisX.ActualWholeRange.ActualMinValue)
            //        AppendChartData();
            //}
        }
        public void ChartZoom(XYDiagram2DZoomEventArgs eventArgs) {
            ManualDateTimeScaleOptions scaleOptions = eventArgs.AxisX.DateTimeScaleOptions as ManualDateTimeScaleOptions;
            if(scaleOptions != null) {
                TimeSpan measureUnitInterval = DateTimeHelper.GetInterval(scaleOptions.MeasureUnit, scaleOptions.MeasureUnitMultiplier);
                DateTime max = (DateTime)eventArgs.AxisX.ActualVisualRange.ActualMaxValue;
                DateTime min = (DateTime)eventArgs.AxisX.ActualVisualRange.ActualMinValue;
                TimeSpan duration = max - min;
                double visibleUnitsCount = duration.TotalSeconds / measureUnitInterval.TotalSeconds;
                if (visibleUnitsCount > maxVisiblePointsCount)
                    eventArgs.AxisX.VisualRange.SetMinMaxValues(eventArgs.OldXRange.MinValue, eventArgs.OldXRange.MaxValue);                
            }
        }
        /*public void UpdateData(double price, double vol) {
            //dataSource.UpdateLastPoint(price, vol);
            UpdateCrosshairText();
            CurrentPrice = price;
            PriceIndicatorColor = dataSource.Data.Last().VolumeColor;
        }*/
        public void Init(string newSymbol) {
            GenerateInitialData();
            ReinitChartRange();
            SymbolName = newSymbol;
            CurrentPrice = dataSource.Data.Count > 0 ? dataSource.Data.Last().Close : 0;
        }
        public void CustomDrawCrosshair(CustomDrawCrosshairEventArgs e) {
            foreach (CrosshairLegendElement legendElement in e.CrosshairLegendElements) {
                Color color = ((TradingData)legendElement.SeriesPoint.Tag).VolumeColor;
                color.A = 255;
                legendElement.Foreground = new SolidColorBrush(color);
            }
        }

        public void TestFunc()
        {
        }
    }
}
