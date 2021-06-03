using System;
using System.ComponentModel;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.POCO;

using TradersToolbox.Views;
using DevExpress.Xpf.Charts;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Xpf.Bars;
using System.Windows;
using DevExpress.Xpf.Editors.Flyout;
using System.Windows.Controls;
using DevExpress.Utils.Commands;
using TradersToolbox.ViewModels.ChartViewModels;
using TradersToolbox.ChartIndicators;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using DevExpress.Xpf.Grid.LookUp;
using DevExpress.Xpf.Editors.Settings;
using System.Reflection;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;
using TradersToolbox.CustomControls;
using System.Linq;
using System.Windows.Markup;
using TradersToolbox.DataObjects;
using TradersToolbox.DataSources;

namespace TradersToolbox.ViewModels {
    [POCOViewModel]
    public class ChartWindowViewModel : PanelWorkspaceViewModel
    {
        public virtual StockChartGroupViewModel StockChartGroupModel { get; set; }

        public ICommand AddIndicatorCommand => new DelegateCommand<IndicatorView>((view) => {
            StockChartGroupModel.SelectedIndicator(view);
        });

        public ICommand RemoveIndicatorCommand => new DelegateCommand<IndicatorView>((view) => {
            StockChartGroupModel.RemoveIndicator(view);
        });

        public ICommand LinkMainChartCommand => new DelegateCommand<LinkData>((link) =>
        {
            SelectMainLinkChart(link);
        });
        public ICommand LinkSubChartCommand => new DelegateCommand<LinkData>((link) =>
        {
            SelectSubLinkChart(link);
        });
        protected override string WorkspaceName { get { return "RightHost"; } set { } }

        public override string DisplayName { get; set; }

        public string SymbolName;

        public virtual ObservableCollection<LinkData> LinkMainItems { get; set; }

        public ChartInformation chartInfo { get; set; }

        public virtual ObservableCollection<LinkData> LinkSubItems { get; set; }
        public bool OpenOrderVisible {get; set;}
        public LinkData SelectedlinkDataItem  { get; set; }

        public int SlectedLinkDataIndex;

        StockFloatFileManager stockFile;

        public ChartWindowViewModel()
        {
            StockChartGroupModel = StockChartGroupViewModel.Create();
            StockChartGroupModel.Parent = this;
            ID = Guid.NewGuid();

            SlectedLinkDataIndex = 12;
            SelectedlinkDataItem = new LinkData() { LinkColorName = "Transparent", LinkItemName = "Not Link", LinkSymbolName = "" };
            OpenOrderVisible = false;

            chartInfo = new ChartInformation();

        }

        public virtual ObservableCollection<TickerData> SearchResults { get; set; }
        public async void SearchSymbol(BarEditItem sender)
        {
            BarEdit = sender;
            if (sender.EditValue == null || string.IsNullOrWhiteSpace(sender.EditValue.ToString()))
            {
                //clear and hide
                SearchResults?.Clear();
                SearchResults = null;
                (sender.EditSettings as PopupBaseEditSettingsEx).Popup.IsPopupOpen = false;
            }
            else
            {
                if (BarEdit.EditValue.ToString().Contains(Environment.NewLine))
                {
                    if (SearchResults != null && SearchResults.Any(var => (var.Ticker.ToLower() + Environment.NewLine) == BarEdit.EditValue.ToString().ToLower()))
                    {
                        var row = SearchResults.First(var => (var.Ticker.ToLower() + Environment.NewLine) == BarEdit.EditValue.ToString().ToLower());

                        UpdateSymbol(row.Ticker);
                        BarEdit.EditValue = null;
                    }
                    else
                    {
                        BarEdit.EditValue = BarEdit.EditValue.ToString().Replace(Environment.NewLine, string.Empty);
                    }
                }
                else
                {
                    //update and show popup
                    try
                    {
                        var result = await MainWindowViewModel.BrokersManager.SuggestSymbols(sender.EditValue.ToString(), 20);
                        SearchResults = new ObservableCollection<TickerData>(result);
                        (sender.EditSettings as PopupBaseEditSettingsEx).Popup.IsPopupOpen = true;
                    }
                    catch (Exception ex)
                    {
                        SearchResults?.Clear();
                        SearchResults = null;
                        (sender.EditSettings as PopupBaseEditSettingsEx).Popup.IsPopupOpen = false;
                    }
                }
              
            }
            
        }

        public void SelectSymbol(object sender,MouseButtonEventArgs args)
        {
            int rowHandle = TViewe.GetRowHandleByMouseEventArgs(args);
            if (rowHandle == DataControlBase.InvalidRowHandle) return;
            if (!(sender as GridControl).IsGroupRowHandle(rowHandle))
            {
               var row =  (sender as GridControl).GetRow(rowHandle) as TickerData;
               UpdateSymbol(row.Ticker);
               BarEdit.EditValue = null;
            }
           
        }

        private BarEditItem BarEdit;
        private TableView TViewe;

        public void TableLoaded(object sender)
        {
            TViewe = sender as TableView;

        }

        public void CheckedChanged(BarCheckItem sender)
        {
            if (sender.IsChecked == true)
            {
                switch (sender.Tag as string)
                {
                    case "Horizontal":
                        StockChartGroupModel.AddAnnotation(AnnotationType.Horizontal);
                        sender.IsChecked = false;
                        break;
                    case "Vertical":
                        StockChartGroupModel.AddAnnotation(AnnotationType.Vertical);
                        sender.IsChecked = false;
                        break;
                    case "Angled":
                        StockChartGroupModel.AddAnnotation(AnnotationType.Angled);
                        sender.IsChecked = false;
                        break;
                    case "On/Off":
                        StockChartGroupModel.ChartOpenOrdersVisible = true;
                        StockChartGroupModel.SetOpenOrderChartVisible(true);
                        OpenOrderVisible = true;
                        break;
                }
            }
            else
            { 
                if (sender.Tag as string == "On/Off")
                {
                    StockChartGroupModel.ChartOpenOrdersVisible = false;
                    StockChartGroupModel.SetOpenOrderChartVisible(false);
                    OpenOrderVisible = false;
                }
            }
        }

        protected override void OnIsClosedChanged()
        {
            base.OnIsClosedChanged();
            if(IsClosed)
                StockChartGroupModel?.Close();
        }
        public void OnDestroy() {
        }

        public void UpdateSymbol(string symbol, ChartIntervalItem chartInterval = null) 
        {
            SetNewName(symbol);
            StockChartGroupModel.UpdateSymbol(symbol, chartInterval);

            StocksInformation result = MainWindowViewModel.BrokersManager.GetStockMarketData(symbol);
            if (MainWindowViewModel.BrokersManager.ActiveBroker == MainWindowViewModel.BrokersManager.PolygonIO)
            {
                stockFile = new StockFloatFileManager();
                stockFile.LoadFloatFile();
                chartInfo.marketcapitalizationValue = "Market Capitialization : " + result.MarketCapitalization;
                chartInfo.sharesoutstandValue = "Shares Amount : " + result.SharesOutstanding;
                chartInfo.floatValue = "Float : " + stockFile.GetFloatValue(symbol);
                chartInfo.shortPercentValue = "Short : " + stockFile.GetShortValue(symbol);
                if (stockFile.GetFloatValue(symbol) != "---")
                    chartInfo.shortPercentValue += "%";
            }

        }

        public void SetNewName(string symbol)
        {
            DisplayName = $"Chart {symbol}";
            SymbolName = symbol;

            LinkMainItems = new ObservableCollection<LinkData>()
            {
                new LinkData(){ LinkColorName = "Red", LinkItemName = "Red Symbol Link", LinkSymbolName = SymbolName},
                new LinkData(){ LinkColorName = "Green", LinkItemName = "Green Symbol Link", LinkSymbolName = SymbolName},
                new LinkData(){ LinkColorName = "Blue", LinkItemName = "Blue Symbol Link", LinkSymbolName = SymbolName},
                new LinkData(){ LinkColorName = "Yellow", LinkItemName = "Yellow Symbol Link", LinkSymbolName = SymbolName},
                new LinkData(){ LinkColorName = "Cyan", LinkItemName = "Cyan Symbol Link", LinkSymbolName = SymbolName},
                new LinkData(){ LinkColorName = "White", LinkItemName = "White Symbol Link", LinkSymbolName = SymbolName},
                new LinkData(){ LinkColorName = "Brown", LinkItemName = "Brown Symbol Link", LinkSymbolName = SymbolName},
                new LinkData(){ LinkColorName = "Coral", LinkItemName = "Coral Symbol Link", LinkSymbolName = SymbolName},
                new LinkData(){ LinkColorName = "Aqua", LinkItemName = "Aqua Symbol Link", LinkSymbolName = SymbolName},
                new LinkData(){ LinkColorName = "Pink", LinkItemName = "Pink Symbol Link", LinkSymbolName = SymbolName},
                new LinkData(){ LinkColorName = "DeepSkyBlue", LinkItemName = "DeepSkyBlue Symbol Link", LinkSymbolName = SymbolName},
                new LinkData(){ LinkColorName = "Khaki", LinkItemName = "Khaki Symbol Link", LinkSymbolName = SymbolName},
                new LinkData(){ LinkColorName = "Transparent", LinkItemName = "Not Link", LinkSymbolName = SymbolName}
            };

            LinkSubItems = new ObservableCollection<LinkData>()
            {
            };

            LinkMainItems[SlectedLinkDataIndex].LinkSymbolName = symbol;
            SelectedlinkDataItem = LinkMainItems[SlectedLinkDataIndex];
            Messenger.Default.Send<LinkData>(LinkMainItems[SlectedLinkDataIndex]);
        }

        public void InitAfterLoad()
        {
            StockChartGroupModel.Parent = this;
            StockChartGroupModel.InitAfterLoad();
        }

        internal void LoadAfterLogin()
        {
            StockChartGroupModel.LoadAfterLogin();
        }

        public void SelectMainLinkChart(LinkData linkData)
        {
            SelectedlinkDataItem = linkData;
            SlectedLinkDataIndex = LinkMainItems.IndexOf(linkData);

            Messenger.Default.Send<ChartWindowViewModel>(this);
        }

        public void SelectSubLinkChart(LinkData linkData)
        {

        }

    }
}
