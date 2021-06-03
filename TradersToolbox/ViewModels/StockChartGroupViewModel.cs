using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using DevExpress.Mvvm.POCO;
using DevExpress.Xpf.Bars;
using DevExpress.Xpf.Charts;
using DevExpress.Xpf.Core;
using SciChart.Charting.ChartModifiers;
using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Visuals.PaletteProviders;
using SciChart.Charting.Visuals.RenderableSeries;
using SciChart.Core.Extensions;
using SciChart.Data.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using TradersToolbox.Brokers;
using TradersToolbox.ChartIndicators;
using TradersToolbox.CustomAnnotations;
using TradersToolbox.DataSources;
using TradersToolbox.ViewModels.ChartViewModels;
using TradersToolbox.ViewModels.DialogsViewModels;
using TradersToolbox.Views.DialogWindows;

namespace TradersToolbox.ViewModels
{
    [POCOViewModel]
    public class StockChartGroupViewModel 
    {
        public static StockChartGroupViewModel Create()
        {
            var result = ViewModelSource.Create(() => new StockChartGroupViewModel());
            var stockView = ViewModelSource.Create(() => new CandleChartViewModel(result, result.DataSource.Data));
            result.ChartData = new MainChartData() { Type = MainChartDataType.Candle, BackgroundColor = Colors.Black ,LineColor = Colors.Blue, UpColor = Colors.Green, DownColor = Colors.Red };
            result.ChartPaneViewModels.Add(stockView);
            var view = new IndicatorView() { Type = IndicatorType.Volume, ViewType = IndicatorViewType.Subgraph, Name = "Volume" };
            result.ChartPaneViewModels.Add(ViewModelSource.Create(() => new SubgraphViewModel(result, result.DataSource.Data, view, IndicatorInputType.None, IndicatorGraphType.Column, result.GetIndicatorValues(view.Type))));

            return result;
        }

        public void RemoveVolumeDefault()
        {
//            var view = new IndicatorView() { Type = IndicatorType.Volume, ViewType = IndicatorViewType.Subgraph, Name = "Volume" };
            ChartPaneViewModels.RemoveWhere(var => var is SubgraphViewModel subgraph && subgraph.Indicator.Name == "Volume");
        }
        public virtual ObservableCollection<IChartViewModel> ChartPaneViewModels { get; set; }

        public virtual ObservableCollection<IndicatorView> IndicatorViews { get; set; }
        public virtual ObservableCollection<IndicatorView> SelectedIndicatorViews { get; set; }

        public bool ChartOpenOrdersVisible { get; set; }

        public virtual IRange SharedXRange { get; set; }

        public virtual IndexRange SharedYRange { get; set; }

        public virtual string ChartGroupId { get; set; }

        public virtual int SelectedIntervalIndex { get; set; }

        public virtual int SelectedLoopbackIntervalIndex { get; set; }

        public virtual int SelectedShadeModeIndex { get; set; }
        public ObservableCollection<ChartIntervalItem> IntervalsSource { get; private set; }

        public ObservableCollection<ChartIntervalItem> LoopbackIntervalsSource { get; set; }

        public ObservableCollection<string> ShadeModeSource { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual IBarsStreamer DataSource { get; set; }

        public virtual string SymbolName { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<IBarsStreamer> DataSources = new List<IBarsStreamer>();

        

        public StockChartGroupViewModel()
        {
            var t = MainWindowViewModel.BrokersManager.GetBarsStreamer(connect: false);
            t.Wait();
            DataSource = t.Result;

            ChartPaneViewModels = new ObservableCollection<IChartViewModel>();

            InitIntervals();
            InitIndicators();
            SelectedIntervalIndex = 10;

            SelectedShadeModeIndex = 1;

            SelectedLoopbackIntervalIndex = 13;

            ChartGroupId = Guid.NewGuid().ToString();

            ChartOpenOrdersVisible = false;

            Messenger.Default.Register<string>(this, (action) => ReceiveDoSomethingMessage(action));

        }

        public ChartIntervalItem StartInterval => IntervalsSource[SelectedIntervalIndex];

        public ChartIntervalItem StartLoopbackInterval => LoopbackIntervalsSource[SelectedLoopbackIntervalIndex];

        public string StartShadeMode => ShadeModeSource[SelectedShadeModeIndex];

        public void IntervalChanged(BarEditItem sender)
        {
            var index = IntervalsSource.IndexOf(var=>var.Caption == (sender.EditValue as ChartIntervalItem).Caption );
            SelectedIntervalIndex = index;
            UpdateSymbol(SymbolName);
        }

        public void LoopbackIntervalsChanged(BarEditItem sender)
        {
            var index = LoopbackIntervalsSource.IndexOf(var => var.Caption == (sender.EditValue as ChartIntervalItem).Caption);
            SelectedLoopbackIntervalIndex = index;
            UpdateSymbol(SymbolName);
        }

        public void ShadeModeChagned(BarEditItem sender)
        {
            var index = ShadeModeSource.IndexOf(var => var == sender.EditValue as string);
            SelectedShadeModeIndex = index;
            if (SelectedShadeModeIndex != 0)
                (ChartPaneViewModels[0] as CandleChartViewModel)?.SetShadeMode(sender.EditValue as string);
            UpdateSymbol(SymbolName);
        }

        public void UpdateSymbol(string symbol, ChartIntervalItem chartInterval = null)
        {
            SymbolName = symbol;
            SharedXRange = new IndexRange();
            SharedYRange = new IndexRange();

            StockHistorySettings stockBarStartDate = new StockHistorySettings();
            if (LoopbackIntervalsSource[SelectedLoopbackIntervalIndex].Caption == "YTD")
            {
                stockBarStartDate.TimeStart = new DateTime(DateTime.Now.Year, 1, 1);
            }
            else
            {
                if (LoopbackIntervalsSource[SelectedLoopbackIntervalIndex].MeasureUnit == DateTimeMeasureUnit.Minute)
                    stockBarStartDate.TimeStart = DateTime.Now.AddMinutes(-1 * LoopbackIntervalsSource[SelectedLoopbackIntervalIndex].MeasureUnitMultiplier);
                else if (LoopbackIntervalsSource[SelectedLoopbackIntervalIndex].MeasureUnit == DateTimeMeasureUnit.Day)
                    stockBarStartDate.TimeStart = DateTime.Now.AddDays(-1 * LoopbackIntervalsSource[SelectedLoopbackIntervalIndex].MeasureUnitMultiplier);
                else if (LoopbackIntervalsSource[SelectedLoopbackIntervalIndex].MeasureUnit == DateTimeMeasureUnit.Month)
                    stockBarStartDate.TimeStart = DateTime.Now.AddMonths(-1 * LoopbackIntervalsSource[SelectedLoopbackIntervalIndex].MeasureUnitMultiplier);
                else if (LoopbackIntervalsSource[SelectedLoopbackIntervalIndex].MeasureUnit == DateTimeMeasureUnit.Year)
                    stockBarStartDate.TimeStart = DateTime.Now.AddYears(-1 * LoopbackIntervalsSource[SelectedLoopbackIntervalIndex].MeasureUnitMultiplier);
            }
            
            DataSource.HistorySettings = stockBarStartDate;
            _ = MainWindowViewModel.BrokersManager.GetBarsStreamer(SymbolName, chartInterval ?? IntervalsSource[SelectedIntervalIndex], DataSource, true); Parent.OpenOrderVisible = false;
            Parent.OpenOrderVisible = false;
            Messenger.Default.Send(Parent);
        }

        public void SelectedIndicator(IndicatorView selectedIndicatorView)
        {
            if (selectedIndicatorView.Type == IndicatorType.Pattern)
            {
                PatternValues values = GetPatternValues();

                if (values == null)
                    return;

                var indicatorView = new IndicatorView() { Name = selectedIndicatorView.Name, PatternType = selectedIndicatorView.PatternType, Type = selectedIndicatorView.Type, ViewType = selectedIndicatorView.ViewType, ID = Guid.NewGuid() };

                if ((ChartPaneViewModels[0] as CandleChartViewModel)?.PatternStyles?.Any(var => var.Key.PatternType == indicatorView.PatternType) == true)
                {
                    RemoveIndicator(indicatorView);
                }

                SelectedIndicatorViews.Add(indicatorView);
                (ChartPaneViewModels[0] as CandleChartViewModel)?.AddOverlayPattern(indicatorView, values);
            }
            else
            {
                var values = GetIndicatorValues(selectedIndicatorView.Type);

                if (values == null)
                    return;

                var indicatorView = new IndicatorView() { Name = selectedIndicatorView.Name, Type = selectedIndicatorView.Type, ViewType = selectedIndicatorView.ViewType, ID = Guid.NewGuid() };

                SelectedIndicatorViews.Add(indicatorView);

                if (indicatorView.ViewType == IndicatorViewType.Subgraph)
                {
                    CreateSubgraph(indicatorView, values);
                }
                else if (indicatorView.ViewType == IndicatorViewType.OverlayOrSubgraph)
                {
                    List<UICommand> commands = new List<UICommand>
                    {
                        new UICommand(1, "SUBGRAPH", null, true, false),
                        new UICommand(2, "OVERLAY", null, false, false)
                    };

                    ThemedWindow window = new ThemedWindow
                    {
                        Width = 200,
                        Height = 100,
                        WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen
                    };
                    window.ShowDialog(commands);

                    if (window.DialogButtonCommandResult != null)
                    {
                        switch ((int)window.DialogButtonCommandResult.Id)
                        {
                            case 1:
                                CreateSubgraph(indicatorView, values);
                                break;
                            case 2:
                                CreateOverlay(indicatorView, values);
                                break;
                        }
                    }
                }
                else if (indicatorView.ViewType == IndicatorViewType.Overlay)
                {
                    CreateOverlay(indicatorView, values);
                }
            }
        }

        private PatternValues GetPatternValues()
        {
            PatternValues patternValues = new PatternValues()
            {
                RetraceZ = 1,
                RetracePerO = -1,
                LoosePercent = 0,
                MinWidth = 10,
                MaxWidth = 1000,
                UseVol = false,
                SwingStrength = 2,
                Color = Colors.Orange,
            };

            UICommand okCommand = new UICommand()
            {
                Caption = "Ok",
                IsCancel = false,
                IsDefault = true,
            };

            UICommand cancelCommand = new UICommand()
            {
                Id = MessageBoxResult.Cancel,
                Caption = "Cancel",
                IsCancel = true,
                IsDefault = false,
            };

            ThemedWindow window = new ThemedWindow
            {
                Width = 350,
                Height = 450,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen,
                Title = "Set pattern values",
                Content = new PatternValuesWindow() { DataContext = patternValues }
            };

            var result = window.ShowDialog(new List<UICommand>() { okCommand, cancelCommand });

            if (result == okCommand)
            {
                return patternValues;
            }
            else
            {
                return null;
            }
        }

        private void CreateOverlay(IndicatorView indicatorView, List<IIndicatorField> fields)
        {
            if (indicatorView.Type == IndicatorType.ConsecutiveHigher || indicatorView.Type == IndicatorType.ConsecutiveLower || indicatorView.Type == IndicatorType.Highest || indicatorView.Type == IndicatorType.Lowest || indicatorView.Type == IndicatorType.ValueChart)
            {
                List<UICommand> commands = new List<UICommand>
                {
                    new UICommand(1, "OPEN", null, true, false),
                    new UICommand(2, "CLOSE", null, false, false),
                    new UICommand(3, "HIGH", null, false, false),
                    new UICommand(4, "LOW", null, false, false)
                };

                ThemedWindow window = new ThemedWindow
                {
                    WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen,
                    Width = 350,
                    Height = 100
                };
                window.ShowDialog(commands);

                if (window.DialogButtonCommandResult != null)
                {
                    IndicatorInputType indicatorInputType = IndicatorInputType.None;

                    switch ((int)window.DialogButtonCommandResult.Id)
                    {
                        case 1:
                            indicatorInputType = IndicatorInputType.Open;
                            break;
                        case 2:
                            indicatorInputType = IndicatorInputType.Close;
                            break;
                        case 3:
                            indicatorInputType = IndicatorInputType.High;
                            break;
                        case 4:
                            indicatorInputType = IndicatorInputType.Low;
                            break;
                    }

                    (ChartPaneViewModels[0] as CandleChartViewModel)?.AddOverlay(indicatorView, indicatorInputType, GetGraphType(), fields);
                }
            }
            else if (indicatorView.Type == IndicatorType.BollingerBands || indicatorView.Type == IndicatorType.KeltnerChannels)
                (ChartPaneViewModels[0] as CandleChartViewModel)?.AddOverlay(indicatorView, IndicatorInputType.None, IndicatorGraphType.MultiLine3, fields);
            else

                (ChartPaneViewModels[0] as CandleChartViewModel)?.AddOverlay(indicatorView, IndicatorInputType.None, GetGraphType(), fields);
        }

        private void CreateSubgraph(IndicatorView indicatorView, List<IIndicatorField> fields)
        {
            if (indicatorView.Type == IndicatorType.ConsecutiveHigher || indicatorView.Type == IndicatorType.ConsecutiveLower || indicatorView.Type == IndicatorType.Highest || indicatorView.Type == IndicatorType.Lowest || indicatorView.Type == IndicatorType.ValueChart)
            {
                List<UICommand> commands = new List<UICommand>
                {
                    new UICommand(1, "OPEN", null, true, false),
                    new UICommand(2, "CLOSE", null, false, false),
                    new UICommand(3, "HIGH", null, false, false),
                    new UICommand(4, "LOW", null, false, false)
                };

                ThemedWindow window = new ThemedWindow
                {
                    Width = 350,
                    Height = 100,
                    WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen
                };
                window.ShowDialog(commands);

                if (window.DialogButtonCommandResult != null)
                {
                    IndicatorInputType indicatorInputType = IndicatorInputType.None;

                    switch ((int)window.DialogButtonCommandResult.Id)
                    {
                        case 1:
                            indicatorInputType = IndicatorInputType.Open;
                            break;
                        case 2:
                            indicatorInputType = IndicatorInputType.Close;
                            break;
                        case 3:
                            indicatorInputType = IndicatorInputType.High;
                            break;
                        case 4:
                            indicatorInputType = IndicatorInputType.Low;
                            break;
                    }

                    ChartPaneViewModels.Add(ViewModelSource.Create(() => new SubgraphViewModel(this, DataSource.Data, indicatorView, indicatorInputType, GetGraphType(), fields)));
                }
            }
            else
            {
                if (indicatorView.Type == IndicatorType.Vix)
                {
                    var t = MainWindowViewModel.BrokersManager.GetBarsStreamer(connect: false);
                    t.Wait();
                    var dataSource = t.Result;

                    _ = MainWindowViewModel.BrokersManager.GetBarsStreamer("$Vix.x", IntervalsSource[SelectedIntervalIndex], dataSource, true);

                    DataSources.Add(dataSource);
                    ChartPaneViewModels.Add(ViewModelSource.Create(() => new CandleChartViewModel(this, dataSource.Data, indicatorView)));
                }
                else
                    ChartPaneViewModels.Add(ViewModelSource.Create(() => new SubgraphViewModel(this, DataSource.Data, indicatorView, IndicatorInputType.None, GetGraphType(), fields)));
            }
        }

        private List<IIndicatorField> GetIndicatorValues(IndicatorType type)
        {
            if (type == IndicatorType.Vix)
                return new List<IIndicatorField>();

            List<IIndicatorField> TypeFields = IndicatorUtils.GetFieldsForIndicator(type);

            if (TypeFields.Count == 0)
                return TypeFields;

            var idicatorLengthViewModel = new IdicatorLengthViewModel
            {
                Fields = TypeFields
            };

            UICommand okCommand = new UICommand()
            {
                Caption = "Ok",
                IsCancel = false,
                IsDefault = true,
            };

            UICommand cancelCommand = new UICommand()
            {
                Id = MessageBoxResult.Cancel,
                Caption = "Cancel",
                IsCancel = true,
                IsDefault = false,
            };

            ThemedWindow window = new ThemedWindow
            {
                Width = 350,
                Height = 300,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen,
                Title = "Indicator Values Dialog",
                Content = new IndicatorLenghtWindow() { DataContext = idicatorLengthViewModel }
            };

            var result = window.ShowDialog(new List<UICommand>() { okCommand, cancelCommand });

            if (result == okCommand)
            {
                return idicatorLengthViewModel.Fields;
            }
            else
                return null;
        }

        private IndicatorGraphType GetGraphType()
        {
            IndicatorGraphType graphType = IndicatorGraphType.None;

            List<UICommand> commands = new List<UICommand>
            {
                new UICommand(2, "Column", null, false, false),
                new UICommand(3, "Mountain", null, false, false),
                new UICommand(4, "Line", null, false, false),
                new UICommand(5, "StackedMountain", null, false, false)
            };

            ThemedWindow window = new ThemedWindow
            {
                Width = 400,
                Height = 100,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen
            };
            window.ShowDialog(commands);

            if (window.DialogButtonCommandResult != null)
            {

                switch ((int)window.DialogButtonCommandResult.Id)
                {
                    case 2:
                        graphType = IndicatorGraphType.Column;
                        break;
                    case 3:
                        graphType = IndicatorGraphType.Mountain;
                        break;
                    case 4:
                        graphType = IndicatorGraphType.Line;
                        break;
                    case 5:
                        graphType = IndicatorGraphType.StackedMountain;
                        break;
                }
            }

            return graphType;
        }

        public void RemoveIndicator(IndicatorView indicatorView)
        {
            SelectedIndicatorViews.Remove(indicatorView);

            if (indicatorView.ViewType == IndicatorViewType.Subgraph)
            {
                if (indicatorView.Type == IndicatorType.Vix)
                    ChartPaneViewModels.Remove(ChartPaneViewModels.Last(var => var is CandleChartViewModel));
                else
                {
                    var panel = ChartPaneViewModels.FirstOrDefault(var => var is SubgraphViewModel subgraph && subgraph.Indicator.ID == indicatorView.ID);

                    if (panel != null)
                    {
                        ChartPaneViewModels.Remove(panel);
                        (panel as SubgraphViewModel).ClosePaneCommand.Execute(null);
                    }
                }
            }
            else if (indicatorView.Type == IndicatorType.Pattern)
            {
                (ChartPaneViewModels[0] as CandleChartViewModel)?.RemoveOverlayPattern(indicatorView);
            }
            else if (indicatorView.ViewType == IndicatorViewType.LineAnnotation)
            {
                (ChartPaneViewModels[0] as CandleChartViewModel)?.RemoveLineAnnotation(indicatorView.Tag);
            }
            else
            {
                ChartPaneViewModels.RemoveWhere(var => var is SubgraphViewModel subgraph && subgraph.Indicator.ID == indicatorView.ID);
                (ChartPaneViewModels[0] as CandleChartViewModel).RemoveOverlay(indicatorView);
            }

        }

        private void InitIntervals()
        {
            IntervalsSource = new ObservableCollection<ChartIntervalItem>
            {
                new ChartIntervalItem() { Tag = Guid.NewGuid(), Caption = "1 minutes", MeasureUnit = DateTimeMeasureUnit.Minute, MeasureUnitMultiplier = 1 },
                new ChartIntervalItem() { Tag = Guid.NewGuid(), Caption = "2 minutes", MeasureUnit = DateTimeMeasureUnit.Minute, MeasureUnitMultiplier = 2 },
                new ChartIntervalItem() { Tag = Guid.NewGuid(), Caption = "5 minutes", MeasureUnit = DateTimeMeasureUnit.Minute, MeasureUnitMultiplier = 5 },
                new ChartIntervalItem() { Tag = Guid.NewGuid(), Caption = "15 minutes", MeasureUnit = DateTimeMeasureUnit.Minute, MeasureUnitMultiplier = 15 },
                new ChartIntervalItem() { Tag = Guid.NewGuid(), Caption = "30 minutes", MeasureUnit = DateTimeMeasureUnit.Minute, MeasureUnitMultiplier = 30 },
                //new ChartIntervalItem() { Caption = "45 minutes", MeasureUnit = DateTimeMeasureUnit.Minute, MeasureUnitMultiplier = 45 },
                new ChartIntervalItem() { Tag = Guid.NewGuid(), Caption = "1 hour", MeasureUnit = DateTimeMeasureUnit.Hour, MeasureUnitMultiplier = 1 },
                new ChartIntervalItem() { Tag = Guid.NewGuid(), Caption = "2 hour", MeasureUnit = DateTimeMeasureUnit.Hour, MeasureUnitMultiplier = 2 },
                new ChartIntervalItem() { Tag = Guid.NewGuid(), Caption = "4 hour", MeasureUnit = DateTimeMeasureUnit.Hour, MeasureUnitMultiplier = 4 },
                new ChartIntervalItem() { Tag = Guid.NewGuid(), Caption = "6 hour", MeasureUnit = DateTimeMeasureUnit.Hour, MeasureUnitMultiplier = 6 },
                new ChartIntervalItem() { Tag = Guid.NewGuid(), Caption = "12 hour", MeasureUnit = DateTimeMeasureUnit.Hour, MeasureUnitMultiplier = 12 },
                new ChartIntervalItem() { Tag = Guid.NewGuid(), Caption = "1 day", MeasureUnit = DateTimeMeasureUnit.Day, MeasureUnitMultiplier = 1 },
                new ChartIntervalItem() { Tag = Guid.NewGuid(), Caption = "1 week", MeasureUnit = DateTimeMeasureUnit.Week, MeasureUnitMultiplier = 1 },
                new ChartIntervalItem() { Tag = Guid.NewGuid(), Caption = "1 month", MeasureUnit = DateTimeMeasureUnit.Month, MeasureUnitMultiplier = 1 }
            };

            LoopbackIntervalsSource = new ObservableCollection<ChartIntervalItem>
            {
                new ChartIntervalItem() { Tag = Guid.NewGuid(), Caption = "30 minutes", MeasureUnit = DateTimeMeasureUnit.Minute, MeasureUnitMultiplier = 30 },
                new ChartIntervalItem() { Tag = Guid.NewGuid(), Caption = "1 hour", MeasureUnit = DateTimeMeasureUnit.Hour, MeasureUnitMultiplier = 1 },
                new ChartIntervalItem() { Tag = Guid.NewGuid(), Caption = "2 hours", MeasureUnit = DateTimeMeasureUnit.Hour, MeasureUnitMultiplier = 2 },
                new ChartIntervalItem() { Tag = Guid.NewGuid(), Caption = "4 hours", MeasureUnit = DateTimeMeasureUnit.Hour, MeasureUnitMultiplier = 4 },
                new ChartIntervalItem() { Tag = Guid.NewGuid(), Caption = "1 day", MeasureUnit = DateTimeMeasureUnit.Day, MeasureUnitMultiplier = 1 },
                new ChartIntervalItem() { Tag = Guid.NewGuid(), Caption = "2 days", MeasureUnit = DateTimeMeasureUnit.Day, MeasureUnitMultiplier = 2 },
                new ChartIntervalItem() { Tag = Guid.NewGuid(), Caption = "5 days", MeasureUnit = DateTimeMeasureUnit.Day, MeasureUnitMultiplier = 5 },
                new ChartIntervalItem() { Tag = Guid.NewGuid(), Caption = "10 days", MeasureUnit = DateTimeMeasureUnit.Day, MeasureUnitMultiplier = 10 },
                new ChartIntervalItem() { Tag = Guid.NewGuid(), Caption = "15 days", MeasureUnit = DateTimeMeasureUnit.Day, MeasureUnitMultiplier = 15 },
                new ChartIntervalItem() { Tag = Guid.NewGuid(), Caption = "1 month", MeasureUnit = DateTimeMeasureUnit.Month, MeasureUnitMultiplier = 1 },
                new ChartIntervalItem() { Tag = Guid.NewGuid(), Caption = "2 months", MeasureUnit = DateTimeMeasureUnit.Month, MeasureUnitMultiplier = 2 },
                new ChartIntervalItem() { Tag = Guid.NewGuid(), Caption = "3 months", MeasureUnit = DateTimeMeasureUnit.Month, MeasureUnitMultiplier = 3 },
                new ChartIntervalItem() { Tag = Guid.NewGuid(), Caption = "6 months", MeasureUnit = DateTimeMeasureUnit.Month, MeasureUnitMultiplier = 6 },
                new ChartIntervalItem() { Tag = Guid.NewGuid(), Caption = "YTD", MeasureUnit = DateTimeMeasureUnit.Day, MeasureUnitMultiplier = 1 },
                new ChartIntervalItem() { Tag = Guid.NewGuid(), Caption = "1 year", MeasureUnit = DateTimeMeasureUnit.Year, MeasureUnitMultiplier = 1 },
                new ChartIntervalItem() { Tag = Guid.NewGuid(), Caption = "2 years", MeasureUnit = DateTimeMeasureUnit.Year, MeasureUnitMultiplier = 2 },
                new ChartIntervalItem() { Tag = Guid.NewGuid(), Caption = "3 years", MeasureUnit = DateTimeMeasureUnit.Year, MeasureUnitMultiplier = 3 },
                new ChartIntervalItem() { Tag = Guid.NewGuid(), Caption = "5 years", MeasureUnit = DateTimeMeasureUnit.Year, MeasureUnitMultiplier = 5 },
                new ChartIntervalItem() { Tag = Guid.NewGuid(), Caption = "10 years", MeasureUnit = DateTimeMeasureUnit.Year, MeasureUnitMultiplier = 10 },
                new ChartIntervalItem() { Tag = Guid.NewGuid(), Caption = "20 years", MeasureUnit = DateTimeMeasureUnit.Year, MeasureUnitMultiplier = 20 },
            };

            ShadeModeSource = new ObservableCollection<string>
            {
                "None",
                "Transparent",
                "Shaded"
            };
        }

        private void InitIndicators(bool isFromLoad = false)
        {
            if(!isFromLoad)
                SelectedIndicatorViews = new ObservableCollection<IndicatorView>();
            IndicatorViews = new ObservableCollection<IndicatorView>
            {
                new IndicatorView() { Type = IndicatorType.ATR, ViewType = IndicatorViewType.Subgraph, Name = "ATR" },
                new IndicatorView() { Type = IndicatorType.Autocor, ViewType = IndicatorViewType.Subgraph, Name = "Autocor" },
                new IndicatorView() { Type = IndicatorType.ConsecutiveHigher, ViewType = IndicatorViewType.Subgraph, Name = "ConsecutiveHigher" },
                new IndicatorView() { Type = IndicatorType.ConsecutiveLower, ViewType = IndicatorViewType.Subgraph, Name = "ConsecutiveLower" },
                new IndicatorView() { Type = IndicatorType.DayOfMonth, ViewType = IndicatorViewType.Subgraph, Name = "DayOfMonth" },
                new IndicatorView() { Type = IndicatorType.DayOfWeek, ViewType = IndicatorViewType.Subgraph, Name = "DayOfWeek" },
                new IndicatorView() { Type = IndicatorType.Doji, ViewType = IndicatorViewType.Subgraph, Name = "Doji" },
                new IndicatorView() { Type = IndicatorType.EMA, ViewType = IndicatorViewType.OverlayOrSubgraph, Name = "EMA" },
                new IndicatorView() { Type = IndicatorType.Hammer, ViewType = IndicatorViewType.Subgraph, Name = "Hammer" },
                new IndicatorView() { Type = IndicatorType.Highest, ViewType = IndicatorViewType.OverlayOrSubgraph, Name = "Highest" },
                new IndicatorView() { Type = IndicatorType.Hurst, ViewType = IndicatorViewType.Subgraph, Name = "Hurst" },
                new IndicatorView() { Type = IndicatorType.IBS, ViewType = IndicatorViewType.Subgraph, Name = "IBS" },
                new IndicatorView() { Type = IndicatorType.Inverted, ViewType = IndicatorViewType.Subgraph, Name = "Inverted" },
                new IndicatorView() { Type = IndicatorType.KaufmanER, ViewType = IndicatorViewType.Subgraph, Name = "KaufmanER" },
                new IndicatorView() { Type = IndicatorType.Lowest, ViewType = IndicatorViewType.OverlayOrSubgraph, Name = "Lowest" },
                new IndicatorView() { Type = IndicatorType.MaxRange, ViewType = IndicatorViewType.Subgraph, Name = "MaxRange" },
                new IndicatorView() { Type = IndicatorType.MinRange, ViewType = IndicatorViewType.Subgraph, Name = "MinRange" },
                new IndicatorView() { Type = IndicatorType.Month, ViewType = IndicatorViewType.Subgraph, Name = "Month" },
                new IndicatorView() { Type = IndicatorType.PercentChange, ViewType = IndicatorViewType.Subgraph, Name = "PercentChange" },
                new IndicatorView() { Type = IndicatorType.PerformanceMTD, ViewType = IndicatorViewType.Subgraph, Name = "PerformanceMTD" },
                new IndicatorView() { Type = IndicatorType.PerformanceQTD, ViewType = IndicatorViewType.Subgraph, Name = "PerformanceQTD" },
                new IndicatorView() { Type = IndicatorType.PerformanceYTD, ViewType = IndicatorViewType.Subgraph, Name = "PerformanceYTD" },
                new IndicatorView() { Type = IndicatorType.PivotPP, ViewType = IndicatorViewType.OverlayOrSubgraph, Name = "PivotPP" },
                new IndicatorView() { Type = IndicatorType.PivotR1, ViewType = IndicatorViewType.OverlayOrSubgraph, Name = "PivotR1" },
                new IndicatorView() { Type = IndicatorType.PivotR2, ViewType = IndicatorViewType.OverlayOrSubgraph, Name = "PivotR2" },
                new IndicatorView() { Type = IndicatorType.PivotS1, ViewType = IndicatorViewType.OverlayOrSubgraph, Name = "PivotS1" },
                new IndicatorView() { Type = IndicatorType.PivotS2, ViewType = IndicatorViewType.OverlayOrSubgraph, Name = "PivotS2" },
                new IndicatorView() { Type = IndicatorType.Quarter, ViewType = IndicatorViewType.Subgraph, Name = "Quarter" },
                new IndicatorView() { Type = IndicatorType.Range, ViewType = IndicatorViewType.Subgraph, Name = "Range" },
                new IndicatorView() { Type = IndicatorType.RSI, ViewType = IndicatorViewType.Subgraph, Name = "RSI" },
                new IndicatorView() { Type = IndicatorType.SMA, ViewType = IndicatorViewType.OverlayOrSubgraph, Name = "SMA" },
                new IndicatorView() { Type = IndicatorType.TDLM, ViewType = IndicatorViewType.Subgraph, Name = "TDLM" },
                new IndicatorView() { Type = IndicatorType.TDOM, ViewType = IndicatorViewType.Subgraph, Name = "TDOM" },
                new IndicatorView() { Type = IndicatorType.ValueChart, ViewType = IndicatorViewType.Subgraph, Name = "ValueChart" },
                new IndicatorView() { Type = IndicatorType.Volume, ViewType = IndicatorViewType.Subgraph, Name = "Volume" },
                new IndicatorView() { Type = IndicatorType.Vix, ViewType = IndicatorViewType.Subgraph, Name = "Vix" },
                new IndicatorView() { Type = IndicatorType.WeekNumber, ViewType = IndicatorViewType.Subgraph, Name = "WeekNumber" },
                new IndicatorView() { Type = IndicatorType.WinsLast, ViewType = IndicatorViewType.Subgraph, Name = "WinsLast" },

                new IndicatorView() { Type = IndicatorType.ParabolicSAR, ViewType = IndicatorViewType.OverlayOrSubgraph, Name = "ParabolicSAR" },
                new IndicatorView() { Type = IndicatorType.CCI, ViewType = IndicatorViewType.Subgraph, Name = "CCI" },
                new IndicatorView() { Type = IndicatorType.ADX, ViewType = IndicatorViewType.Subgraph, Name = "ADX" },
                new IndicatorView() { Type = IndicatorType.DMIpositive, ViewType = IndicatorViewType.Subgraph, Name = "DMIpositive" },
                new IndicatorView() { Type = IndicatorType.DMInegative, ViewType = IndicatorViewType.Subgraph, Name = "DMInegative" },
                new IndicatorView() { Type = IndicatorType.CompositeRSI, ViewType = IndicatorViewType.Subgraph, Name = "CompositeRSI" },
                new IndicatorView() { Type = IndicatorType.CompositeATR, ViewType = IndicatorViewType.Subgraph, Name = "CompositeATR" },
                new IndicatorView() { Type = IndicatorType.CompositeHurst, ViewType = IndicatorViewType.Subgraph, Name = "CompositeHurst" },
                new IndicatorView() { Type = IndicatorType.CompositeStochastic, ViewType = IndicatorViewType.Subgraph, Name = "CompositeStochastic" },
                new IndicatorView() { Type = IndicatorType.CompositeSuperSmooth, ViewType = IndicatorViewType.Subgraph, Name = "CompositeSuperSmooth" },
                new IndicatorView() { Type = IndicatorType.CompositeSMA, ViewType = IndicatorViewType.OverlayOrSubgraph, Name = "CompositeSMA" },
                new IndicatorView() { Type = IndicatorType.CompositeEMA, ViewType = IndicatorViewType.OverlayOrSubgraph, Name = "CompositeEMA" },
                new IndicatorView() { Type = IndicatorType.MACD, ViewType = IndicatorViewType.Subgraph, Name = "MACD" },
                new IndicatorView() { Type = IndicatorType.MACDhist, ViewType = IndicatorViewType.Subgraph, Name = "MACDhist" },
                new IndicatorView() { Type = IndicatorType.RateOfChange, ViewType = IndicatorViewType.Subgraph, Name = "RateOfChange" },
                new IndicatorView() { Type = IndicatorType.Momentum, ViewType = IndicatorViewType.Subgraph, Name = "Momentum" },
                new IndicatorView() { Type = IndicatorType.Stochastics, ViewType = IndicatorViewType.Subgraph, Name = "Stochastics" },

                new IndicatorView() { Type = IndicatorType.Median, ViewType = IndicatorViewType.OverlayOrSubgraph, Name = "Median" },


                new IndicatorView() { Type = IndicatorType.BollingerBands, ViewType = IndicatorViewType.Overlay, Name = "BollingerBands" },
                new IndicatorView() { Type = IndicatorType.KeltnerChannels, ViewType = IndicatorViewType.Overlay, Name = "KeltnerChannels" }
            };


            var names = Enum.GetNames(typeof(StockPatternSearch.PatternStyle)).Reverse().Skip(1).Reverse();
            foreach (var name in names)
            {
                IndicatorViews.Add(new IndicatorView() { Type = IndicatorType.Pattern, Name = "Pattern: " + name, PatternType = (StockPatternSearch.PatternStyle)Enum.Parse(typeof(StockPatternSearch.PatternStyle), name), ViewType = IndicatorViewType.Overlay, });
            }


           IndicatorViews = IndicatorViews.OrderBy(var => var.Name).ToObservableCollection();

            
        }

        internal void LoadAfterLogin()
        {
            Close();
            if (SymbolName != null && DataSource!=null)
            {
                _ = MainWindowViewModel.BrokersManager.GetBarsStreamer(SymbolName, IntervalsSource[SelectedIntervalIndex], DataSource, true);
            }
        }

        internal void InitAfterLoad()
        {
            LoadAfterLogin();
            
            InitIntervals();
            InitIndicators(true);

            foreach (var chart in ChartPaneViewModels)
            {
                chart.InitAfterLoad(this, DataSource.Data);
            }

        }

        public void Close()
        {
            DataSource.Close();
            foreach (var source in DataSources)
                source.Close();
        }


        public void AddAnnotation(AnnotationType type)
        {
            var tag = (ChartPaneViewModels[0] as CandleChartViewModel)?.AddAnnotation(type);
            SelectedIndicatorViews.Add(new IndicatorView { ViewType = IndicatorViewType.LineAnnotation, Name = type.ToString(), Tag = tag });
        }

        public void SetOpenOrderChartVisible(bool visible)
        {
            var tag = (ChartPaneViewModels[0] as CandleChartViewModel);
            tag.ShowOrdersAnnotation(visible);
        }
        public void AddRolloverModifyer(RolloverModifier modifier)
        {
            (ChartPaneViewModels[0] as CandleChartViewModel)?.AddRolloverModifyer(modifier);
        }
        public void RemoveRolloverModifyer(RolloverModifier modifier)
        {
            (ChartPaneViewModels[0] as CandleChartViewModel)?.RemoveRolloverModifyer(modifier);
        }

        public void FormatIndicator(SubgraphViewModel sender)
        {
            var type = GetGraphType();
            if (type != IndicatorGraphType.None)
                sender.UpdateGraphType(type);
        }

        public ICommand FormatDataCommand => new DelegateCommand(() => {

            var formatDataViewModel = new FormatDataViewModel
            {
                SymbolName = DataSource.Symbol,
                CurentTimeZone = DataSource.TimeZone,
                SelectedInterval = DataSource.Interval,
                CurrentHistorySettings = DataSource.HistorySettings,
                CurrentChartData = ChartData
            };

            UICommand okCommand = new UICommand()
            {
                Caption = "Apply",
                IsCancel = false,
                IsDefault = true,
            };

            UICommand cancelCommand = new UICommand()
            {
                Id = MessageBoxResult.Cancel,
                Caption = "Cancel",
                IsCancel = true,
                IsDefault = false,
            };

            ThemedWindow window = new ThemedWindow
            {
                Width = 400,
                Height = 500,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Title = "Format Data",
                Content = new FormatDataWindow() { DataContext = formatDataViewModel }
            };

            var result = window.ShowDialog(new List<UICommand>() { okCommand, cancelCommand });

            if (result == okCommand)
            {
                Close();

                //formatDataViewModel.CurrentHistorySettings.SetChunkCount(formatDataViewModel.SelectedInterval);

                SymbolName = formatDataViewModel.SymbolName;
                Parent.SetNewName(SymbolName);
                DataSource.Interval = formatDataViewModel.SelectedInterval;
                DataSource.TimeZone = formatDataViewModel.CurentTimeZone;
                DataSource.HistorySettings = formatDataViewModel.CurrentHistorySettings;
                ChartData = formatDataViewModel.CurrentChartData;

                _ = MainWindowViewModel.BrokersManager.GetBarsStreamer(SymbolName, DataSource.Interval, DataSource, true);

                Parent.OpenOrderVisible = false;
                Messenger.Default.Send(Parent);
                //ChartPaneViewModels.Clear();
                (ChartPaneViewModels[0] as CandleChartViewModel).UpdateFormatData(DataSource.Data, ChartData);
                
                //ChartPaneViewModels.Add(ViewModelSource.Create(() => new CandleChartViewModel(this, DataSource.Data)));


                //  ThemedMessageBox.Show("Too many bars requested. Please limit your search range.", "Warning!");
                //  FormatDataCommand.Execute(null);

            }
        });

        public void FormatOverlaysIndicators()
        {
            var titles = (ChartPaneViewModels[0] as CandleChartViewModel).OverlayIndicators.Where(var => var.Value.Second.First != IndicatorGraphType.MultiLine3).Select(var => var.Key.Title).ToList();

            if (titles.Count < 1)
                return;

            string title = GetOverlayTitle(titles);

            if (string.IsNullOrWhiteSpace(title))
                return;

            var indicator = (ChartPaneViewModels[0] as CandleChartViewModel).OverlayIndicators.First(var => var.Key.Title == title);

            var type = GetGraphType();

            (ChartPaneViewModels[0] as CandleChartViewModel).UpdateOverlayGraphType(indicator, type);
        }

        private void ReceiveDoSomethingMessage(string symbol)
        {
            if (symbol != Parent.SymbolName)
                return;

            var tag = (ChartPaneViewModels[0] as CandleChartViewModel);
            tag.ReDrawOrdersAnnotation();
        }

        public MainChartData ChartData { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ChartWindowViewModel Parent { get; set; }

        private string GetOverlayTitle(List<string> titles)
        {
            var overlayTitlesViewModel = new OverlayTitlesViewModel
            {
                Titles = titles
            };

            UICommand okCommand = new UICommand()
            {
                Caption = "Ok",
                IsCancel = false,
                IsDefault = true,
            };

            UICommand cancelCommand = new UICommand()
            {
                Id = MessageBoxResult.Cancel,
                Caption = "Cancel",
                IsCancel = true,
                IsDefault = false,
            };

            ThemedWindow window = new ThemedWindow
            {
                Width = 350,
                Height = 300,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen,
                Title = "Select Indicator to change",
                Content = new OverlayTitlesWindow() { DataContext = overlayTitlesViewModel }
            };

            var result = window.ShowDialog(new List<UICommand>() { okCommand, cancelCommand });

            if (result == okCommand)
            {
                return overlayTitlesViewModel.SelectedTitle;
            }
            else
            {
                return string.Empty;
            }
        }

        public enum LinkData
        {
            NonLink = 0,
            BlueLink,
            GreenLink,
            RedLink

        }
    }
}
