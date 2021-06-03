using DevExpress.Mvvm;
using DevExpress.XtraPrinting.Native;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Threading;
using TradersToolbox.Core;
using TradersToolbox.DataObjects;
using TradersToolbox.ViewModels;
using WebSocket4Net;

namespace TradersToolbox.DataSources
{
    public class QuotesDataSource
    {
        public ObservableCollection<QuoteDefinitionModel> Data { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ICollectionView DataCollection { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        readonly object DataLocker = new object();

        public delegate void SendMessage(string error);
        public event SendMessage Error;

        public ConcurrentDictionary<string, Pair<string, int>> requestSymbols { get;set; }
        readonly char sep = '#';

        readonly Dispatcher _dispatcher;
        public int Order { get; set; }

        public QuotesDataSource()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;

            Order = 0;

            requestSymbols = new ConcurrentDictionary<string, Pair<string, int>>();
            Data = new ObservableCollection<QuoteDefinitionModel>();
            DataCollection = CollectionViewSource.GetDefaultView(Data);
            BindingOperations.EnableCollectionSynchronization(Data, DataLocker);

            Messenger.Default.Register<QuoteReceivedMessage>(this, OnMessage);
            Messenger.Default.Register<QuoteInfoMessage>(this, OnMessage);
        }

        #region Message handlers
        void OnMessage(QuoteReceivedMessage message)
        {
            lock (DataLocker)
            {
                UpdateQuotePrice(message);
            }

            /*try
            {
                if (!_dispatcher.HasShutdownStarted)
                    _dispatcher.Invoke(() => UpdateQuotePrice(message));
            }
            catch (TaskCanceledException) { }*/
        }

        void UpdateQuotePrice(QuoteReceivedMessage message)
        {
            if (Data.FirstOrDefault(x => x.Symbol == message.Symbol) is QuoteDefinitionModel def)
            {
                if (message.Last.HasValue) def.LastPrice = message.Last ?? 0;
                if (message.PreviousClose.HasValue) def.PreviousClose = message.PreviousClose;
                if (message.Close.HasValue) def.Close = message.Close;
                if (message.LastDateTimeUTC.HasValue) def.LastDateTimeUTC = message.LastDateTimeUTC.Value;

                //if (quote.NetChangePct != default) def.NetChangePct = quote.NetChangePct ?? 0;
            }
            else if (requestSymbols.Count > 0 && requestSymbols.ContainsKey(message.Symbol))
            {
                var rs = requestSymbols[message.Symbol];
                int indexBase = rs.Second;
                int insIndex = 0;
                for (; insIndex < Data.Count; insIndex++)
                    if (indexBase < Data[insIndex].Order)
                        break;

                var ar = rs.First.Split(sep);
                var Q = new QuoteDefinitionModel(ar[0], indexBase, ar[1])
                {
                    Symbol = message.Symbol,
                    //Description = quote.Description,
                    LastPrice = message.Last ?? 0,
                    //NetChangePct = quote.NetChangePct ?? 0
                };
                if (message.PreviousClose.HasValue) Q.PreviousClose = message.PreviousClose;
                if (message.Close.HasValue) Q.Close = message.Close;
                if (message.LastDateTimeUTC.HasValue) Q.LastDateTimeUTC = message.LastDateTimeUTC.Value;
                Data.Insert(insIndex, Q);
            }
        }

        void OnMessage(QuoteInfoMessage message)
        {
            try
            {
                _dispatcher.Invoke(() =>
                {
                    if (Data.FirstOrDefault(x => x.Symbol == message.Symbol) is QuoteDefinitionModel def)
                    {
                        if (!string.IsNullOrEmpty(message.Description))
                            def.Description = message.Description;
                    }
                });
            }
            catch (TaskCanceledException ex) { }
        }
        #endregion

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
                    requestSymbols[$"@{ss}"] = new Pair<string, int>(sym.Name + sep + group, Order);
                }
                else
                    requestSymbols[sym.Name] = new Pair<string, int>(sym.Name + sep + group, Order);
                Order++;
            }

        }

        public void AppendSymbol(string name,string group)
        {
            requestSymbols[name] = new Pair<string, int>(name + sep + group, Order);
            Order++;
        }

        public void ClearSymbols()
        {
            requestSymbols.Clear();
            Order = 0;
        }

        public void RemoveSymbol(string name)
        {
            requestSymbols.TryRemove(name, out _);
        }

        internal void Close()
        {
            // todo: stop list
            MainWindowViewModel.BrokersManager.StopStreamQuotes(null);
        }

        public async Task RequestLiveDataAsync()
        {
            await MainWindowViewModel.BrokersManager.StreamQuotes(requestSymbols.Keys.ToList(), false);
        }

        private void ShowMessage(string message)
        {
            Error?.Invoke(message);
        }
    }
}
