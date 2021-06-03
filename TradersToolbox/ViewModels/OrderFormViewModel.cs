using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.POCO;
using DevExpress.XtraPrinting.Native;
using Python.Runtime;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Threading;
using TradersToolbox.DataSources;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using TradersToolbox.Core;
using TradersToolbox.ViewModels;
using Dispatcher = System.Windows.Threading.Dispatcher;
using System.Windows;
using TradersToolbox.Data;
using TradersToolbox.DataObjects;
using System.Globalization;
using DevExpress.Xpf.Core;
using System.Windows.Controls;

namespace TradersToolbox.ViewModels
{
    [POCOViewModel]
    public class OrderFormViewModel
    {
        public string SelectedSymbol { get; set; }

        public delegate void SendMessage(string error);

        public event SendMessage Error;

        public ConcurrentDictionary<string, QuoteReceivedMessage> Data;

        public Dictionary<string, SymbolType> SymbolTypeDic;
        public ConcurrentDictionary<string, Pair<string, int>> requestSymbols { get; set; }
        readonly char sep = '#';

        private string OrderSymbolNameSaved;

        public int BuyChecked { get; set; }
        private int BuyCheckedSaved;

        public int SellChecked { get; set; }
        private int SellCheckedSaved;

        public QuoteReceivedMessage CurrentQuote { get; set; }
        public int marketOrderTab { get; set; }
        public int limitOrderTab { get; set; }
        public int stopOrderTab { get; set; }
        public decimal limitPrice { get; set; }
        public decimal stopPrice { get; set; }
        public string marketAmount { get; set; }
        public string limitAmount { get; set; }
        public string stopAmount { get; set; }
        public int labelAmount { get; set; }

        public virtual decimal marketPrice { get; set; }


        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual List<TickerData> SearchResults { get; set; }

        public void PlaceOrder()
        {
            var timeUtc = DateTime.UtcNow;
            TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            DateTime easternTime = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, easternZone);
            int order_id = MainWindowViewModel.tradeDB.getPositionRecordsAll().Count + MainWindowViewModel.tradeDB.getTransactionRecordsAll().Count + MainWindowViewModel.tradeDB.getOpenOrderRecordsAll().Count + 1;
            string order_symbol = SelectedSymbol;
            DateTime order_cu_time = DateTime.Now;
            int order_amount = labelAmount;
            double order_profit = 0;
            string order_mode = TradersToolbox.Properties.Resources.BUY;
            decimal order_price = 0;

            if (order_symbol == "" || order_symbol == null || (BuyChecked == 0 && SellChecked == 0) || Data.Count == 0 || order_amount == 0)
            {
                DXMessageBox.Show("Trade Rejected: Input value is incorrect.");
                return;
            }

            string ho = easternTime.ToString("HH");
            if ((!SymbolTypeDic.ContainsKey(order_symbol) || (SymbolTypeDic[order_symbol] != SymbolType.Futures && SymbolTypeDic[order_symbol] != SymbolType.FOREX)) &&
                int.Parse(ho) < 8 && int.Parse(ho) > 20)
            {
                DXMessageBox.Show("Order Rejected: Pre-market trading begins at 8 AM ET and Post-market trading ends at 8 PM ET.  Please place orders inside this time window for this symbol");
                return;
            }
            if (!Data.ContainsKey(order_symbol))
                return;

            var IData = Data[order_symbol];
            if (marketOrderTab == 1)
            {
                Logger.Current.Info("Market Order Placed successfully. Order ID({0}) {1} Order Price({2}) ", order_id, order_symbol, marketPrice);
                order_price = marketPrice;
                if (BuyChecked == 1)
                    order_mode = TradersToolbox.Properties.Resources.BUY;
                if (SellChecked == 1)
                    order_mode = TradersToolbox.Properties.Resources.SELL;
            }
            else if (limitOrderTab == 1)
            {
                Logger.Current.Info("Limit Order Placed successfully. Order ID({0}) {1} Order Price({2}) ", order_id, order_symbol, marketPrice);

                order_price = limitPrice;
                if (BuyChecked == 1)
                {
                    if (limitPrice < IData.Ask.Value)
                        order_mode = TradersToolbox.Properties.Resources.BUY_LIMIT;
                    else
                    {
                        order_mode = TradersToolbox.Properties.Resources.BUY;
                        order_price = marketPrice;
                        //                        DXMessageBox.Show("BUY LIMIT Order Failed");
                        //                        return;
                    }
                }

                if (SellChecked == 1)
                {
                    if (limitPrice > IData.Bid.Value)
                        order_mode = TradersToolbox.Properties.Resources.SELL_LIMIT;
                    else
                    {
                        order_mode = TradersToolbox.Properties.Resources.SELL;
                        order_price = marketPrice;
                        //                        DXMessageBox.Show("SELL LIMIT Order Failed");
                        //                        return;
                    }
                }

            }
            else if (stopOrderTab == 1)
            {
                Logger.Current.Info("Stop Order Placed successfully. Order ID({0}) {1} Order Price({2}) ", order_id, order_symbol, marketPrice);

                order_price = stopPrice;
                if (BuyChecked == 1)
                {
                    if (order_price > IData.Ask.Value)
                        order_mode = TradersToolbox.Properties.Resources.BUY_STOP;
                    else
                    {
                        order_mode = TradersToolbox.Properties.Resources.BUY;
                        order_price = marketPrice;
                        //                        DXMessageBox.Show("BUY STOP Order Failed");
                        //                        return;
                    }
                }
                if (SellChecked == 1)
                {
                    if (order_price < IData.Bid.Value)
                        order_mode = TradersToolbox.Properties.Resources.SELL_STOP;
                    else
                    {
                        order_mode = TradersToolbox.Properties.Resources.SELL;
                        order_price = marketPrice;
                        //DXMessageBox.Show("SELL STOP Order Failed");
                        //return;
                    }
                }
            }

            OrderData oData = new OrderData();
            oData.Id = order_id;
            oData.Symbol = SelectedSymbol;
            oData.Open_Time = order_cu_time;
            oData.Open_Price = decimal.ToDouble( order_price);
            oData.Close_Price = 0;
            oData.Shares = order_amount;
            oData.Profit = order_profit;
            oData.Type = order_mode;

            if (oData.Type == TradersToolbox.Properties.Resources.BUY)
            {
                oData.Close_Price = decimal.ToDouble(IData.Bid.Value);
            }
            else if (oData.Type == TradersToolbox.Properties.Resources.SELL)
            {
                oData.Close_Price = decimal.ToDouble(IData.Ask.Value);
            }
            DumbAggregator.BroadCast(oData);
        }


        public static OrderFormViewModel Create()
        {
            return ViewModelSource<OrderFormViewModel>.Create();
        }

        protected OrderFormViewModel()
        {
            requestSymbols = new ConcurrentDictionary<string, Pair<string, int>>();

            OrderSymbolNameSaved = "";
            Data = new ConcurrentDictionary<string, QuoteReceivedMessage>();
            SymbolTypeDic = new Dictionary<string, SymbolType>();
            BuyChecked = 1;
            BuyCheckedSaved = 0;
            SellChecked = 0;
            SellCheckedSaved = 0;
            limitPrice = 0;
            stopPrice = 0;
            marketAmount = "0";
            stopAmount = "0";
            limitAmount = "0";
            
            AddDefaultSymbols();

            Messenger.Default.Register<QuoteReceivedMessage>(this, OnMessage);

            _ = RequestLiveDataAsync();
        }

        public void AddDefaultSymbols()
        {
            foreach (var sym in Utils.SymbolsManager.Symbols.Where(x => x.IsStandard && (x.Type == SymbolType.Futures ||
                  x.Type == SymbolType.ETF || x.Type == SymbolType.FOREX)))
            {
                string group = sym.Type == SymbolType.Futures ? "Futures" : sym.Type == SymbolType.ETF ? "ETF" : "Forex";
                if (sym.Type == SymbolType.Futures)
                {
                    string ss;
                    switch (sym.Name)
                    {
                        case "BC": ss = "BTC"; break;
                        case "BU": ss = "FGBL"; break;
                        case "FD": ss = "FDAX"; break;
                        case "RT": ss = "RTY"; break;
                        default: ss = sym.Name; break;
                    }
                    requestSymbols[$"@{ss}"] = new Pair<string, int>(sym.Name + sep + group, requestSymbols.Count);
                    SymbolTypeDic.Add($"@{ss}", sym.Type);
                }
                else
                {
                    requestSymbols[sym.Name] = new Pair<string, int>(sym.Name + sep + group, requestSymbols.Count);
                    SymbolTypeDic.Add(sym.Name, sym.Type);
                }
            }

        }

        public void AppendSymbol(string name, string group)
        {
            requestSymbols[name] = new Pair<string, int>(name + sep + group, requestSymbols.Count);
            _ = RequestLiveDataAsync();
        }

        public async Task RequestLiveDataAsync()
        {
            await MainWindowViewModel.BrokersManager.StreamQuotes(requestSymbols.Keys.ToList(), false);
        }

        #region Message handlers
        void OnMessage(QuoteReceivedMessage message)
        {
            // try
            // {
            //     if (!_dispatcher.HasShutdownStarted && message.Ask.HasValue && message.Bid.HasValue)
            //         _dispatcher.Invoke(() =>
            if (message.Ask.HasValue && message.Bid.HasValue)
            {
                Data[message.Symbol] = message;

                CheckmarketPrice();
                if (message.Symbol == SelectedSymbol)
                {
                    CurrentQuote = message;
                    if (BuyChecked == 1)
                        marketPrice = message.Ask.Value;
                    else if (SellChecked == 1)
                        marketPrice = message.Bid.Value;
                }
            }

           //         });
           // }
           // catch (TaskCanceledException) { }
        }
        #endregion


        private void ShowMessage(string message)
        {
            Error?.Invoke(message);
        }

        public void CheckmarketPrice()
        {
            int Count = Data.Count;
            if (Count == 0 || SelectedSymbol == null)
                return;
            if (marketPrice == 0 || OrderSymbolNameSaved != SelectedSymbol
                || BuyCheckedSaved != BuyChecked || SellChecked != SellCheckedSaved)
            {
                if (Data.ContainsKey(SelectedSymbol))
                {
                    var iData = Data[SelectedSymbol];
                    if (BuyChecked == 1)
                        marketPrice = iData.Ask.Value;
                    else if (SellChecked == 1)
                        marketPrice = iData.Bid.Value;
                }
            }
            OrderSymbolNameSaved = SelectedSymbol;
            BuyCheckedSaved = BuyChecked;
            SellCheckedSaved = SellChecked;
        }

        private string searchString;
        private CancellationTokenSource searchCancelTokenSource;

        public async void SearchSymbol(object sender, TextChangedEventArgs e)
        {
            if(sender is DevExpress.Xpf.Grid.LookUp.LookUpEdit lookUp)
            {
                if (lookUp.AutoSearchText == searchString)
                    return;

                searchString = lookUp.AutoSearchText;

                if (string.IsNullOrEmpty(searchString))
                {
                    //clear and hide
                    //SearchResults?.Clear();
                    ////SearchResults = null;
                    //lookUp.ClosePopup();
                }
                else
                {
                    searchCancelTokenSource?.Cancel();
                    searchCancelTokenSource = new CancellationTokenSource();
                    var cancelationToken = searchCancelTokenSource.Token;

                    var grid = lookUp.GetGridControl();
                    if (grid != null)
                        grid.ShowLoadingPanel = true;

                    try
                    {
                        var res = await MainWindowViewModel.BrokersManager.SuggestSymbols(searchString, 20);
                        if (!cancelationToken.IsCancellationRequested)
                        {
                            SearchResults = res;
                            if (grid != null)
                                grid.ShowLoadingPanel = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        SearchResults?.Clear();
                        SearchResults = null;
                        if (grid != null)
                            grid.ShowLoadingPanel = false;
                    }
                }
            }
        }

        public void EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            if (sender is DevExpress.Xpf.Grid.LookUp.LookUpEdit lookUp && lookUp.EditValue is TickerData ticker)
            {
                AppendSymbol(ticker.Ticker, "");
                SelectedSymbol = ticker.Ticker;
            }
        }
    }
}