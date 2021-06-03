using DevExpress.Xpf.Charts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TradersToolbox.Data;
using TradersToolbox.DataObjects;
using PolygonIO.Api;
using PolygonIO.Client;
using System.Threading;
using DevExpress.Mvvm;
using WebSocket4Net;
using System.Security.Authentication;
using TradersToolbox.Core;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Windows;
using TradersToolbox.ViewModels;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;

namespace TradersToolbox.Brokers
{
    public class PolygonIO : IBrokerService, INotifyPropertyChanged
    {
        HttpClient httpClient;
        bool isLoggedIn;


        private readonly string AccessToken = "K6CD972EJqZjLSQ6UNjRIYu78e8_tSLK";

        public bool IsLoggedIn
        {
            get => isLoggedIn;
            private set
            {
                if(value!=isLoggedIn)
                {
                    isLoggedIn = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public string UserId { get; } = "";

        public string BrokerName => "Polygon IO";
        public string BrokerKey => "P";

        private ReferenceApi referenceApi;
        private StocksEquitiesApi stocksApi;

        /// <summary>
        /// PolygonIO connection for real-time data
        /// </summary>
        readonly WebSocket websocket;

        readonly System.Timers.Timer websocketStartTimer;
        CancellationTokenSource messageHandlerCancelationTokenSource;
        ConcurrentQueue<string> messageBuffer = new ConcurrentQueue<string>();


        public PolygonIO()
        {
            httpClient = HttpClientHelper.GetHttpClient();
            
            websocket = new WebSocket("wss://socket.polygon.io/stocks", sslProtocols: SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls, receiveBufferSize: 1024*1024);
            //websocket = new WebSocket("ws://localhost:2020/ws", receiveBufferSize: 1024 * 1024);   //local test server
            websocket.Opened += Websocket_Opened;
            websocket.Error += Websocket_Error;
            websocket.Closed += Websocket_Closed;
            websocket.MessageReceived += Websocket_MessageReceived;

            websocketStartTimer = new System.Timers.Timer(300);
            websocketStartTimer.AutoReset = false;
            websocketStartTimer.Elapsed += WebsocketStartTimer_Elapsed;

            Configuration config = new Configuration();
            config.AddApiKey("apiKey", AccessToken);

            referenceApi = new ReferenceApi(config);
            stocksApi = new StocksEquitiesApi(config);

        }

        private void WebsocketStartTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (websocket)
            {
                // Reconnect logic
                if (websocket.State == WebSocketState.Closed)
                {
                    websocket.Open();
                    int i = 0;
                    while (websocket.State == WebSocketState.Closed && i < 20)  //1 sec max
                    {
                        Task.Delay(50).Wait();
                        i++;
                    }
                }
            }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        // This method is called by the Set accessor of each property.  
        // The CallerMemberName attribute that is applied to the optional propertyName  
        // parameter causes the property name of the caller to be substituted as an argument.  
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion


        public async Task<bool> LogIn()
        {
            if (isLoggedIn) return true;

            websocket.Open();
            IsLoggedIn = true;
            return true;
        }
        public bool LogInDialog()
        {
            if (isLoggedIn) return true;

            websocket.Open();
            IsLoggedIn = true;
            return true;
        }

        public async Task LogOut()
        {
            httpClient.CancelPendingRequests();
            websocket.Close("close");
            IsLoggedIn = false;
        }

        public async Task ProxySettingsChanged()
        {
            await LogOut();
            httpClient.CancelPendingRequests();
            httpClient = HttpClientHelper.GetHttpClient();
        }

        public async Task<int> GetMarketState()
        {
            // 0-undefined, 1-open, 2-extended-hours, 3-closed
            int marketState = 0;

            try
            {
                var resp = await referenceApi.V1MarketstatusNowGetAsync();
                if (resp.Exchanges.Nasdaq == "open")
                    marketState = 1;
                else if (resp.Exchanges.Nasdaq == "extended-hours")
                    marketState = 2;
                else if (resp.Exchanges.Nasdaq == "closed")
                    marketState = 3;
            }
            catch (Exception ex) { }
            return marketState;
        }

        #region Quotes
        CancellationTokenSource StreamQuotesCancellationTokenSource;

        public List<string> StreamQuotesTickersList { get; } = new List<string>();
        //public List<IBarsStreamer> StreamBarsStreamersList { get; } = new List<IBarsStreamer>();
        

        public async Task<bool> StreamQuotes(List<string> tickers, bool isResetTickers)
        {
            // stop previous stream
            StreamQuotesCancellationTokenSource?.Cancel();
            StreamQuotesCancellationTokenSource = new CancellationTokenSource();

            if (isResetTickers)
            {
                StreamQuotesTickersList.Clear();
                StreamQuotesTickersList.AddRange(tickers);
            }
            else
            {
                var list = StreamQuotesTickersList.Union(tickers).ToList();
                StreamQuotesTickersList.Clear();
                StreamQuotesTickersList.AddRange(list);
            }

            // re-open web socket
            /*if (StreamQuotesTickersList.Count > 0)
            {
                if (websocket.State == WebSocketState.Open)
                    websocket.Close();  //close with auto restart
                else if (websocket.State == WebSocketState.Connecting || websocket.State == WebSocketState.Closing)
                {
                    //do nothing
                }
                else
                {
                    websocket.Open();
                }
            }*/

            var cancelTokenLocal = StreamQuotesCancellationTokenSource.Token; //copy value

            foreach (var s in StreamQuotesTickersList)
            {
                // Read last quote and previous close
                double? lastPrice = null;
                decimal? previousClose = null;
                decimal? close = null;
                DateTime? lastPriceDateTimeUTC = null;

                try
                {
                    var t1 = stocksApi.V2AggsTickerStocksTickerPrevGetAsync(s); // Previous Day Close
                    var t2 = stocksApi.V1LastStocksStocksTickerGetAsync(s);     // Last Trade
                    var t3 = stocksApi.V2AggsTickerStocksTickerRangeMultiplierTimespanFromToGetAsync(s, 1, "day",
                        DateTime.UtcNow.ToString("yyyy-MM-dd"), DateTime.UtcNow.ToString("yyyy-MM-dd"));            // this day bar

                    // Previous close
                    try
                    {
                        var resp1 = await t1;

                        if (resp1 != null && resp1.Status == "OK" && resp1.ResultsCount > 0 && resp1.Results.Count > 0)
                        {
                            var res = resp1.Results.FirstOrDefault();
                            if (res != null && res.C.HasValue && res.C.Value != 0)
                            {
                                previousClose = (decimal)res.C.Value;
                                lastPrice = res.C.Value;
                                //previousCloseDatetime = TimeZoneInfo.ConvertTimeFromUtc(Utils.ToDateTime(res.T.Value / 1000), easternZone);
                            }
                        }
                    }
                    catch (Exception ex) { }

                    // Last Trade
                    try
                    {
                        var resp2 = await t2;

                        if (resp2 != null && resp2.Last != null && resp2.Last.Price.HasValue && resp2.Last.Price.Value != 0)
                        {
                            lastPrice = resp2.Last.Price.Value;
                            lastPriceDateTimeUTC = Utils.ToDateTime(resp2.Last.Timestamp.Value / 1000);
                        }
                    }
                    catch (Exception ex) { }

                    // This day close value (post market only)   //todo: check on postmarket and compare with previous close
                    try
                    {
                        var resp3 = await t3;

                        if (resp3 != null && resp3.Status == "OK" && resp3.ResultsCount > 0)
                            close = (decimal)resp3.Results[0].C;
                    }
                    catch (Exception ex) { }

                    if (lastPrice.HasValue)
                        Messenger.Default.Send(new QuoteReceivedMessage(s, last: (decimal)lastPrice.Value, close: close, prevClose: previousClose,
                            lastDateTimeUTC: lastPriceDateTimeUTC));

                    if (cancelTokenLocal.IsCancellationRequested)
                        return false;
                }
                catch (Exception ex) { }
            }
            return true;
        }

        public void StopStreamQuotes(List<string> tickers)
        {
            if(tickers == null || tickers.Count==0)
            {
                StreamQuotesTickersList.Clear();
                //websocket.Close();  //reconnect
            }
        }

        private void Websocket_Opened(object sender, EventArgs e)
        {
            messageBuffer = new ConcurrentQueue<string>();
            messageHandlerCancelationTokenSource = new CancellationTokenSource();
            Task.Run(async () => await MessageHandlerTask(messageHandlerCancelationTokenSource.Token));

            Logger.Current.Debug("POLYGON.IO: Websocket connected!");
            websocket.Send($"{{\"action\":\"auth\",\"params\":\"{AccessToken}\"}}");

            //Subscript for all FINRA symbols
            List<TickerData> tickers;
            var t = MainWindowViewModel.BrokersManager.ReadStockTickers();
            t.Wait();
            if (t.IsCompleted && !t.IsFaulted)
                tickers = t.Result;
            else
                return;

            //tickers = tickers.Take(500).ToList();

            string symbolsRequestQ = string.Join(",", tickers.Select(x => $"Q.{x.Ticker}"));
            string symbolsRequestT = string.Join(",", tickers.Select(x => $"T.{x.Ticker}"));
            //string symbolsRequestA = string.Join(",", tickers.Select(x => $"A.{x.Ticker}"));

            //if (StreamQuotesTickersList.Count > 0)
            {
              //  string symbolsRequestQ = string.Join(",", StreamQuotesTickersList.Select(x => $"Q.{x}"));
                
                websocket.Send($"{{\"action\":\"subscribe\",\"params\":\"{symbolsRequestQ}\"}}");

                //string symbolsRequestT = string.Join(",", StreamQuotesTickersList.Select(x => $"T.{x}"));
                websocket.Send($"{{\"action\":\"subscribe\",\"params\":\"{symbolsRequestT}\"}}");
            }
            //if (StreamBarsStreamersList.Count > 0)
            {
                //string symbolsRequest = string.Join(",", StreamBarsStreamersList.Select(x => $"A.{x.Symbol}"));
                //websocket.Send($"{{\"action\":\"subscribe\",\"params\":\"{symbolsRequestA}\"}}");
            }
        }

        private void Websocket_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            Logger.Current.Debug($"POLYGON.IO: WebSocket Error: {e.Exception.Message}");
        }

        private void Websocket_Closed(object sender, EventArgs e)
        {
            Logger.Current.Debug("POLYGON.IO: Connection Closed...");

            messageHandlerCancelationTokenSource?.Cancel();

            if (e is ClosedEventArgs arg && arg.Reason == "close")
            {
                //do nothing, just exit (normal situation)
            }
            else
            {
                if (!websocketStartTimer.Enabled && isLoggedIn)
                    websocketStartTimer.Start();
            }
        }


        private void Websocket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            //Logger.Current.Debug($"POLYGON.IO: Message: {e.Message}");
            /*if (cancellationTokenSource.IsCancellationRequested)
                websocket.Close(123, "close");*/

            messageBuffer.Enqueue(e.Message);
        }

        private async Task MessageHandlerTask(CancellationToken cancellationToken)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            while (!cancellationToken.IsCancellationRequested)
            {
                if (messageBuffer.Count == 0)
                {
                    await Task.Delay(100);
                    continue;
                }

                do
                {
                    if (messageBuffer.TryDequeue(out string s))
                    {
                        _ = Task.Run(() => HandleMessage(s));   //messages handling order is not guaranteed
                    }
                }
                while (messageBuffer.Count > 0 && !cancellationToken.IsCancellationRequested);

                stopwatch.Stop();
                if(stopwatch.ElapsedMilliseconds>200)
                {
                    Dictionary<string, PolygonIOWebSocketQuote> quoteGlobalCache = new Dictionary<string, PolygonIOWebSocketQuote>();
                    while (quotesBuffer.TryDequeue(out PolygonIOWebSocketQuote quote))
                    {
                        if (quote.AskPrice != 0 && quote.BidPrice != 0)
                            quoteGlobalCache[quote.Symbol] = quote;
                    }

                    /*var groupedQuotes = quotesBuffer.GroupBy(x => x.Symbol,(symbol,qq)=>
                    {
                        var ordered = qq.OrderBy(x => x.Timestamp);
                        var lastQ = ordered.Last();

                        return new QuoteReceivedMessage(symbol, ask: (decimal)lastQ.AskPrice, bid: (decimal)lastQ.BidPrice,
                            lastDateTimeUTC: Utils.ToDateTime(lastQ.Timestamp / 1000));
                    }).ToList();
                    while (!quotesBuffer.IsEmpty)
                    {
                        quotesBuffer.TryTake(out _);
                    }*/

                    Dictionary<string, List<PolygonIOWebSocketTrade>> tradeGlobalCache = new Dictionary<string, List<PolygonIOWebSocketTrade>>();
                    while (tradesBuffer.TryDequeue(out PolygonIOWebSocketTrade trade))
                    {
                        if (tradeGlobalCache.ContainsKey(trade.Symbol))
                            tradeGlobalCache[trade.Symbol].Add(trade);
                        else
                            tradeGlobalCache[trade.Symbol] = new List<PolygonIOWebSocketTrade>() { trade };
                    }
                    List<QuoteReceivedMessage> messages = new List<QuoteReceivedMessage>();
                    foreach (var kv in tradeGlobalCache)
                    {
                        var ordered = kv.Value.OrderBy(x => x.Timestamp);
                        var lastT = ordered.Last();

                        messages.Add(new QuoteReceivedMessage(lastT.Symbol,
                            lastT.Conditions?.Contains(16),  // market official opened flag
                            last: lastT.Price,
                            lastDateTimeUTC: Utils.ToDateTime(lastT.Timestamp / 1000),
                            volume: kv.Value.Sum(x => x.Size)));
                    }


                    /*var groupedTrades = tradesBuffer.GroupBy(x => x.Symbol, (symbol, tt) =>
                    {
                        var ordered = tt.OrderBy(x => x.Timestamp);
                        var lastT = ordered.Last();

                        return new QuoteReceivedMessage(symbol,
                            lastT.Conditions?.Contains(16),  // market official opened flag
                            last: lastT.Price,
                            lastDateTimeUTC: Utils.ToDateTime(lastT.Timestamp / 1000),
                            volume: tt.Sum(x=>x.Size));
                    }).ToList();*/
                    

                    _= Task.Run(() =>
                    {
                        foreach (var quote in quoteGlobalCache.Values /*groupedQuotes*/)
                            //Messenger.Default.Send(m);
                            Messenger.Default.Send(new QuoteReceivedMessage(quote.Symbol, ask: (decimal)quote.AskPrice, bid: (decimal)quote.BidPrice,
                                lastDateTimeUTC: Utils.ToDateTime(quote.Timestamp / 1000)));
                        foreach (var m in messages /*groupedTrades*/)
                            Messenger.Default.Send(m);
                    });

                    stopwatch.Reset();
                }
                stopwatch.Start();
            }
        }

        ConcurrentQueue<PolygonIOWebSocketQuote> quotesBuffer = new ConcurrentQueue<PolygonIOWebSocketQuote>();
        ConcurrentQueue<PolygonIOWebSocketTrade> tradesBuffer = new ConcurrentQueue<PolygonIOWebSocketTrade>();

        private void HandleMessage(string s)
        {
            if (Regex.IsMatch(s, @"""ev"":\s*""Q"""))   //Quotes
            {
                try
                {
                    var quotes = JsonSerializer.Deserialize<PolygonIOWebSocketQuote[]>(s);

                    foreach (var quote in quotes)
                    {
                        //Messenger.Default.Send(new QuoteReceivedMessage(quote.Symbol, ask: (decimal)quote.AskPrice, bid: (decimal)quote.BidPrice,
                        //    lastDateTimeUTC: Utils.ToDateTime(quote.Timestamp / 1000)));
                        quotesBuffer.Enqueue(quote);
                    }
                }
                catch (Exception ex) { }
            }
            else if (Regex.IsMatch(s, @"""ev"":\s*""T"""))   //Trades
            {
                try
                {
                    var trades = JsonSerializer.Deserialize<PolygonIOWebSocketTrade[]>(s);

                    foreach (var trade in trades)
                    {
                        //Messenger.Default.Send(new QuoteReceivedMessage(trade.Symbol,
                        //    trade.Conditions.Contains(16),  // market official opened flag
                        //    last: trade.Price,
                        //    lastDateTimeUTC: Utils.ToDateTime(trade.Timestamp / 1000), volume: trade.Size));
                        tradesBuffer.Enqueue(trade);
                    }
                }
                catch (Exception ex) { }
            }
            /*else if (Regex.IsMatch(s, @"""ev"":\s*""A"""))   //Aggregates
            {
                try
                {
                    var aggregates = JsonSerializer.Deserialize<List<PolygonIOWebSocketAggregate>>(s);

                    foreach (var aggr in aggregates)
                    {
                        Messenger.Default.Send(new AggregateReceivedMessage(aggr));
                    }
                }
                catch (Exception ex) { }
            }*/
        }
        #endregion

        public async Task<List<TickerData>> SuggestSymbols(string text, int maxCount)
        {
            List<TickerData> symbols = new List<TickerData>();

            var firstResp = await referenceApi.V2ReferenceTickersGetAsync("ticker", null, "stocks", "us", text, maxCount);

            foreach (var item in firstResp.Tickers)
            {
                if (item.Active.HasValue && item.Active.Value && FilterExchange(item.PrimaryExch))
                    symbols.Add(new TickerData(item.Ticker, item.PrimaryExch, item.Name));
            }

            return symbols;
        }

        bool FilterExchange(string exchange)
        {
            if (/*IsNasdaq && */exchange == "NASDAQ") return true;
            if (/*IsNYSE && */exchange == "NYE") return true;
            if (/*IsAMEX && */exchange == "AMX") return true;
            if (/*IsARCA && */exchange == "ARCA") return true;
            if (/*IsBATS && */exchange == "BATS") return true;
            return false;
        }


        public async Task<IBarsStreamer> GetBarsStreamer(string symbol = null, ChartIntervalItem interval = null,
            IBarsStreamer barsStreamer = null, bool connect = true)
        {
            if (!IsLoggedIn) return null;

            if (barsStreamer == null)   // create new
            {
                barsStreamer = new PolygonBarsStreamer();
            }
            else      // update
            {
                barsStreamer.Close();       //todo: reconnect without clear()
                barsStreamer.Data.Clear();
                //(barsStreamer as PolygonBarsStreamer).IsHistoricalDataLoadingComplete = false;
            }
            if (symbol != null)
                barsStreamer.Symbol = symbol;
            if (interval != null)
                barsStreamer.Interval = interval;

            if (!connect)
                return barsStreamer;

            // subscribe for realtime data
         //   if (websocket.State == WebSocketState.Open)
         //   {
         //       StreamBarsStreamersList.Add(barsStreamer);
         //       websocket.Send($"{{\"action\":\"subscribe\",\"params\":\"A.{barsStreamer.Symbol}\"}}");
         //   }
         //   else
         //       return null;

            // read history from server
            int intervalVal = barsStreamer.Interval.MeasureUnitMultiplier;
            string unit = "Daily";
            switch (barsStreamer.Interval.MeasureUnit)
            {
                case DateTimeMeasureUnit.Minute: unit = "minute"; break;
                case DateTimeMeasureUnit.Hour:   unit = "hour"; break;
                case DateTimeMeasureUnit.Day:    unit = "day"; break;
                case DateTimeMeasureUnit.Week:   unit = "week"; break;
                case DateTimeMeasureUnit.Month:  unit = "month"; break;
            }

            DateTime startDT;
            int barsBack = 50000;   //max

            if (barsStreamer.HistorySettings.Type == StockHistorySettingsType.FirstData)
                startDT = barsStreamer.HistorySettings.TimeStart;
            else if (barsStreamer.HistorySettings.Type == StockHistorySettingsType.NumberYearsBack)
                startDT = DateTime.UtcNow.AddYears(-(int)barsStreamer.HistorySettings.YearsBack);
            else
                startDT = StockHistorySettings.GetMinusTimeStartFromChunkSize(DateTime.UtcNow, barsStreamer.Interval, barsStreamer.HistorySettings.BarsBack);

            try
            {
                var resp = await stocksApi.V2AggsTickerStocksTickerRangeMultiplierTimespanFromToGetAsync(symbol, intervalVal, unit,
                    startDT.ToString("yyyy-MM-dd"), DateTime.UtcNow.ToString("yyyy-MM-dd"), null, "desc");    //reversed

                if(resp.Status == "OK" && resp.ResultsCount>0)
                {
                    //var last = resp.Results.Last();
                    foreach (var item in resp.Results)
                    {
                        //if(item==last)
                        //    (barsStreamer as PolygonBarsStreamer).IsHistoricalDataLoadingComplete = true;

                        var td = new TradingData(TimeZoneInfo.ConvertTimeFromUtc(Utils.ToDateTime((item.T ?? 0) / 1000), TimeZoneInfo.Local),
                            item.O ?? 0, item.H ?? 0, item.L ?? 0, item.C ?? 0, item.V ?? 0);
                        barsStreamer.Data.Insert(0, td);
                    }

                    // run streamer
                    //_ = StreamBarsInternal(barsStreamer);

                    return barsStreamer;
                }
            }
            catch (Exception ex) { }

            return null;
        }


        private Task StreamBarsInternal(IBarsStreamer barsStreamer)
        {
            return Task.Run(async () => {
                while(isLoggedIn && !barsStreamer.cancellationToken.IsCancellationRequested)
                {
                    // read history from server
                    int intervalVal = barsStreamer.Interval.MeasureUnitMultiplier;
                    string unit = "Daily";
                    switch (barsStreamer.Interval.MeasureUnit)
                    {
                        case DateTimeMeasureUnit.Minute: unit = "minute"; break;
                        case DateTimeMeasureUnit.Hour: unit = "hour"; break;
                        case DateTimeMeasureUnit.Day: unit = "day"; break;
                        case DateTimeMeasureUnit.Week: unit = "week"; break;
                        case DateTimeMeasureUnit.Month: unit = "month"; break;
                    }

                    DateTime startDT = barsStreamer.Data[0].Date;    //desc order sorted

                    try
                    {
                        var resp = await stocksApi.V2AggsTickerStocksTickerRangeMultiplierTimespanFromToGetAsync(barsStreamer.Symbol, intervalVal, unit,
                            startDT.ToString("yyyy-MM-dd"), DateTime.UtcNow.ToString("yyyy-MM-dd"));

                        if (resp.Status == "OK" && resp.ResultsCount > 0)
                        {
                            foreach (var item in resp.Results)
                            {
                                var td = new TradingData(TimeZoneInfo.ConvertTimeFromUtc(Utils.ToDateTime((item.T ?? 0) / 1000), TimeZoneInfo.Local),
                                     item.O ?? 0, item.H ?? 0, item.L ?? 0, item.C ?? 0, item.V ?? 0);

                                Application.Current?.Dispatcher.Invoke(() =>
                                {
                                    TradingData pt = barsStreamer.Data.FirstOrDefault(x => x.Date == td.Date);

                                    if (pt != default)
                                    {
                                        var index = barsStreamer.Data.IndexOf(pt);
                                        barsStreamer.Data[index] = td;
                                    }
                                    else
                                    {
                                        barsStreamer.Data.Insert(0, td);
                                    }
                                });
                            }
                        }
                    }
                    catch (Exception ex) { }

                    await Task.Delay(1000);
                }
            });
        }


        public async Task<ObservableCollection<TradingData>> GetBarsRange(string symbol, ChartIntervalItem interval,
            DateTime? startDate, DateTime? stopDate, IBarsStreamer barsStreamer)
        {
            if (startDate == null)
                throw new ArgumentNullException(nameof(startDate));
            if (stopDate == null)
                stopDate = DateTime.UtcNow;

            // 0-undefined, 1-open, 2-extended-hours, 3-closed
            int marketState = await GetMarketState();

            var response = await stocksApi.V2AggsTickerStocksTickerRangeMultiplierTimespanFromToGetAsync(symbol, 1, "day",
                        startDate.Value.ToString("yyyy-MM-dd"), stopDate.Value.ToString("yyyy-MM-dd"));

            if (response?.Status == "OK" && response.ResultsCount > 0)
            {
                ObservableCollection<TradingData> data = new ObservableCollection<TradingData>();

                foreach (var item in response.Results)
                {
                    var td = new TradingData(TimeZoneInfo.ConvertTimeFromUtc(Utils.ToDateTime((item.T ?? 0) / 1000), TimeZoneInfo.Local),
                        item.O ?? 0, item.H ?? 0, item.L ?? 0, item.C ?? 0, item.V ?? 0);
                    data.Add(td);
                }

                bool IsExtendedHours = Properties.Settings.Default.UseExtendedHours;

                if (IsExtendedHours && marketState > 1)  //if extended hours or closed then add additional bar
                {
                    var extraResp = await stocksApi.V2AggsTickerStocksTickerRangeMultiplierTimespanFromToGetAsync(symbol, 10, "minute",
                        Utils.ToDateTime(response.Results.Last().T.Value / 1000).ToString("yyyy-MM-dd"),    //UTC
                        stopDate.Value.ToString("yyyy-MM-dd"));

                    if (extraResp.Status == "OK" && extraResp.ResultsCount > 0)
                    {
                        TimeSpan marketStopTime = new TimeSpan(16, 0, 0);   //EST
                        //DateTime extraLastDT_EST = TimeZoneInfo.ConvertTimeFromUtc(Utils.ToDateTime(extraResp.Results.Last().T.Value / 1000), BrokersManager.EasternZone).Date;
                        //bool isPreMarket = Utils.ToDateTime(response.Results.Last().T.Value / 1000).Date < extraLastDT_EST;

                        double? Open = default;
                        double High = 0, Low = double.MaxValue, Close = 0;
                        DateTime dt = DateTime.MinValue;
                        double Volume = 0;

                        //skip todays premarket and open
                        int i = 0;
                        for (; i < extraResp.ResultsCount; ++i)
                        {
                            var v = extraResp.Results[i];
                            dt = TimeZoneInfo.ConvertTimeFromUtc(Utils.ToDateTime(v.T.Value / 1000), BrokersManager.EasternZone);
                            if (dt.TimeOfDay < marketStopTime)
                                continue;
                            break;
                        }

                        for (; i < extraResp.ResultsCount; ++i)
                        {
                            var v = extraResp.Results[i];
                            dt = TimeZoneInfo.ConvertTimeFromUtc(Utils.ToDateTime(v.T.Value / 1000), BrokersManager.EasternZone);
                            //if (isPreMarket && dt.Date < extraLastDT_EST)    //skip postmarket bars 
                            //    continue;

                            if (Open.HasValue == false)
                                Open = v.O;

                            High = Math.Max(High, v.H ?? 0);
                            Low = Math.Min(Low, v.L ?? double.MaxValue);
                            Close = v.C ?? 0;
                            Volume += v.V ?? 0;
                        }

                        if (Open.HasValue)
                        {
                            dt = TimeZoneInfo.ConvertTime(dt, BrokersManager.EasternZone, TimeZoneInfo.Local);
                            TradingData extraBar = new TradingData(dt, Open.Value, High, Low, Close, Volume);
                            data.Add(extraBar);
                        }
                    }
                }

                return data;
            }
            return null;
        }

        public StocksInformation GetStockMarketData(string symbol)
        {
            StocksInformation sif = new StocksInformation();
            var data = referenceApi.V2ReferenceFinancialsStocksTickerGet(symbol);
            sif.MarketCapitalization = "---";
            sif.SharesOutstanding = "---";
            if (data.Results.Count == 0)
                return sif;
            if (data.Results[0].MarketCapitalization.HasValue)
            {
                sif.MarketCapitalization = (data.Results[0].MarketCapitalization??0).ToString("N0", CultureInfo.InvariantCulture);
            }

            if (data.Results[0].Shares.HasValue)
            {
                sif.SharesOutstanding = (data.Results[0].Shares??0).ToString("N0", CultureInfo.InvariantCulture);
            }
            return sif;

        }
    }
}
