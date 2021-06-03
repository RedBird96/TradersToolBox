using DevExpress.Mvvm.DataAnnotations;
using SciChart.Charting;
using SciChart.Charting.ChartModifiers;
using SciChart.Charting.Model.ChartSeries;
using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Visuals;
using SciChart.Charting.Visuals.Annotations;
using SciChart.Charting.Visuals.PaletteProviders;
using SciChart.Charting.Visuals.RenderableSeries;
using SciChart.Charting.Visuals.TradeChart;
using SciChart.Data.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using TradersToolbox.ChartIndicators;
using TradersToolbox.Data;
using TradersToolbox.ViewModels.DialogsViewModels;

namespace TradersToolbox.ViewModels.ChartViewModels
{
    [POCOViewModel]
    public class SubgraphViewModel : IChartViewModel, IChildPane
    {
        public virtual string Title { get; set; }
        public ICommand ClosePaneCommand { get; set; }
        public virtual ChartViewModelType Type => ChartViewModelType.Subgraph;

        public virtual StockChartGroupViewModel Parent { get; set; }
        private ObservableCollection<TradingData> _chartDataSource { get; set; }

        public virtual IRange YRange { get; set; }

        private IRenderableSeries savedSessionSubgraphView;

        public virtual IndicatorType IndicatorType { get; set; }

        private IDataSeries DataSeries { get; set; }

        private Brush DataColors { get; set; }

        public virtual IndicatorInputType InputType { get; set; }
        public virtual IndicatorView Indicator { get; set; }
        public virtual IndicatorGraphType GraphType { get; set; }

        public List<IIndicatorField> Fields { get; set; }

        private AxisMarkerAnnotation AxisAnnotation;

        public bool IsFirstTime;

        public SubgraphViewModel(StockChartGroupViewModel Parent, ObservableCollection<TradingData> ChartDataSource, IndicatorView indicator, IndicatorInputType inputType, IndicatorGraphType graphType, List<IIndicatorField> fields)
        {
            indicator.SubtTitle = "Subgraph-" + graphType.ToString() + "-" + inputType.ToString();
            this.Fields = fields;
            ClosePaneCommand = new SerializableActionCommand(() => {
                Parent.RemoveRolloverModifyer((chartSurface.ChartModifier as ModifierGroup).ChildModifiers.First(var => var is RolloverModifier) as RolloverModifier);
                Parent.RemoveIndicator(indicator);
            });

            this.Indicator = indicator;

            InputType = inputType;
            Title = indicator.Name;
            IndicatorType = indicator.Type;

            this.GraphType = graphType;

            DataSeries = IndicatorUtils.GetDataSeriesForIndicator(IndicatorType, ChartDataSource, InputType, fields).First();
            _chartDataSource = ChartDataSource;
            _chartDataSource.CollectionChanged += ChartDataSource_CollectionChanged;

            DataColors = new SolidColorBrush(Colors.Red);

            AxisAnnotation = new AxisMarkerAnnotation();
            AxisAnnotation.Foreground = new SolidColorBrush(Colors.White);
            AxisAnnotation.AnnotationCanvas = AnnotationCanvas.YAxis;
            AxisAnnotation.Y1 = 0.0;

            this.Parent = Parent;

            IsFirstTime = true;
        }

        public SubgraphViewModel()
        {
            AxisAnnotation = new AxisMarkerAnnotation();
            AxisAnnotation.Foreground = new SolidColorBrush(Colors.White);
            AxisAnnotation.AnnotationCanvas = AnnotationCanvas.YAxis;
            AxisAnnotation.Y1 = 0.0;
        }

        private void ChartDataSource_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            
            if (sender is ObservableCollection<TradingData> data)
            {
                DataSeries = IndicatorUtils.GetDataSeriesForIndicator(IndicatorType, data, InputType, Fields).First();
                if (chartSurface != null)
                { 
                    chartSurface.RenderableSeries[0].DataSeries = DataSeries;
                    AxisAnnotation.Y1 = chartSurface.RenderableSeries[0].DataSeries.LatestYValue;
                    Title = Indicator.Name + " - " + chartSurface.RenderableSeries[0].DataSeries.LatestYValue;

                    chartSurface?.ZoomExtents();
                }
            }

        }

        private SciChartSurface chartSurface;
        public void Loaded(object surface)
        {

            Console.WriteLine("Volume" + DataSeries.Count.ToString());

            if (surface is SciChartSurface chart)
            {
                chart.RenderableSeries = new RenderableSeriesSourceCollection(new List<IRenderableSeriesViewModel>());
                switch (GraphType)
                {
                    case IndicatorGraphType.Mountain:
                        chart.RenderableSeries.Add(new FastMountainRenderableSeries() { DataSeries = DataSeries });
                        break;
                    case IndicatorGraphType.Line:
                        chart.RenderableSeries.Add(new FastLineRenderableSeries() { DataSeries = DataSeries });
                        break;
                    case IndicatorGraphType.StackedMountain:
                        chart.RenderableSeries.Add(new StackedMountainRenderableSeries() { DataSeries = DataSeries });
                        break;
                    case IndicatorGraphType.Column:
                        if (IndicatorType == IndicatorType.Volume)
                            chart.RenderableSeries.Add(new FastColumnRenderableSeries() { PaletteProvider = new ColumnPaletteProvider(this), DataSeries = DataSeries });
                        else
                            chart.RenderableSeries.Add(new FastColumnRenderableSeries() { DataSeries = DataSeries });
                        break;
                }
                savedSessionSubgraphView = chart.RenderableSeries.ElementAt(0);
                chartSurface = chart;

                VisualXcceleratorEngine.SetIsEnabled(chartSurface, true);

                chart.Annotations = new SciChart.Charting.Visuals.Annotations.AnnotationCollection();
                chart.Annotations.Add(AxisAnnotation);

                Parent.AddRolloverModifyer((chart.ChartModifier as ModifierGroup).ChildModifiers.First(var => var is RolloverModifier) as RolloverModifier);

            }
        }
        public void ZoomExtents()
        {

        }

        public void Updated()
        {
        }

        public void InitAfterLoad(StockChartGroupViewModel Parent, ObservableCollection<TradingData> ChartDataSource)
        {
            if (ChartDataSource.Count > 0)
                DataSeries = IndicatorUtils.GetDataSeriesForIndicator(IndicatorType, ChartDataSource, InputType, Fields).First();
            _chartDataSource = ChartDataSource;
            _chartDataSource.CollectionChanged += ChartDataSource_CollectionChanged;

            AxisAnnotation = new AxisMarkerAnnotation();
            AxisAnnotation.Foreground = new SolidColorBrush(Colors.White);
            AxisAnnotation.AnnotationCanvas = AnnotationCanvas.YAxis;
            AxisAnnotation.Y1 = 0.0;


            this.Parent = Parent;

            ClosePaneCommand = new SerializableActionCommand(() => {
                Parent.RemoveRolloverModifyer((chartSurface.ChartModifier as ModifierGroup).ChildModifiers.First(var => var is RolloverModifier) as RolloverModifier);
                Parent.RemoveIndicator(Indicator);
            });
        }

        public void UpdateGraphType(IndicatorGraphType type)
        {
            GraphType = type;

            chartSurface.RenderableSeries = new RenderableSeriesSourceCollection(new List<IRenderableSeriesViewModel>());
            switch (GraphType)
            {
                case IndicatorGraphType.Mountain:
                    chartSurface.RenderableSeries.Add(new FastMountainRenderableSeries() { DataSeries = DataSeries });
                    break;
                case IndicatorGraphType.Line:
                    chartSurface.RenderableSeries.Add(new FastLineRenderableSeries() { DataSeries = DataSeries });
                    break;
                case IndicatorGraphType.StackedMountain:
                    chartSurface.RenderableSeries.Add(new StackedMountainRenderableSeries() { DataSeries = DataSeries });
                    break;
                case IndicatorGraphType.Column:
                    if (IndicatorType == IndicatorType.Volume)
                        chartSurface.RenderableSeries.Add(new FastColumnRenderableSeries() { PaletteProvider = new ColumnPaletteProvider(this), DataSeries = DataSeries });
                    else
                        chartSurface.RenderableSeries.Add(new FastColumnRenderableSeries() { DataSeries = DataSeries });
                    break;
            }
        }
    }

    class ColumnPaletteProvider : IFillPaletteProvider, IStrokePaletteProvider
    {
        SubgraphViewModel SubGraphParent;
        public ColumnPaletteProvider(SubgraphViewModel subParent)
        {
            SubGraphParent = subParent;
        }
        public void OnBeginSeriesDraw(IRenderableSeries rSeries)
        {
        }

        public Brush OverrideFillBrush(IRenderableSeries rSeries, int index, IPointMetadata metadata)
        {
            Brush brush = new SolidColorBrush(SubGraphParent.Parent.ChartData.UpColor);
            if (index == 0)
            { 
                return brush;
            }

            try
            {
                double[] yArray = ((ISciList<double>)rSeries.DataSeries.YValues).ItemsArray;
                if (yArray[index] > yArray[index - 1])
                    brush = new SolidColorBrush(SubGraphParent.Parent.ChartData.UpColor);
                else
                    brush = new SolidColorBrush(SubGraphParent.Parent.ChartData.DownColor);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            
            return brush;
        }

        public Color? OverrideStrokeColor(IRenderableSeries rSeries, int index, IPointMetadata metadata)
        {
            Color color = new Color();
            color = Colors.Green;
            if (index == 0)
            {
                return color;
            }
            
            try
            {
                double[] yArray = ((ISciList<double>)rSeries.DataSeries.YValues).ItemsArray;
                if (yArray[index] > yArray[index - 1])
                    color = SubGraphParent.Parent.ChartData.UpColor;
                else
                    color = SubGraphParent.Parent.ChartData.DownColor;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }

            return color;
        }
    }


}
