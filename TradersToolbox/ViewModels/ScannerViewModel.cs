using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.POCO;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Linq;
using TradersToolbox.DataSources;
using TradersToolbox.Data;
using TradersToolbox.Core;
using TradersToolbox.Core.Serializable;
using DevExpress.Xpf.Charts;
using System.Threading;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using System.Net.Http;
using TradersToolbox.DataObjects;
using TradersToolbox.Brokers;

namespace TradersToolbox.ViewModels
{
    [POCOViewModel]
    public class ScannerViewModel
    {
        protected IMessageBoxService MessageBoxService { get { return this.GetService<IMessageBoxService>(); } }
        //protected IDocumentManagerService DocumentManagerService { get { return this.GetService<IDocumentManagerService>(); } }

        public ObservableCollection<ChartIntervalItem> IntervalsSource { get; private set; }
        public ChartIntervalItem Interval;

        //public virtual bool IsExtendedHours { get; set; }


        public virtual bool IsWaitIndicatorVisible { get; set; }
        public virtual string ProgressText { get; set; }
        public virtual string StageText { get; set; }


        public virtual bool M2Enabled { get; set; }
        public virtual bool M3Enabled { get; set; }

        NotifyObservableCollection<Signal> Signals_internal;

        public NotifyObservableCollection<Signal> Signals
        {
            get
            {
                if (Signals_internal == null)
                {
                    // try to load from settings first
                    if (!string.IsNullOrEmpty(Properties.Settings.Default.SignalsXML))
                    {
                        Signals_internal = SimTaskWizardViewModel.LoadSignalsFromXML(Properties.Settings.Default.SignalsXML, false);
                    }
                    // read default signals
                    if (Signals_internal == null)
                    {
                        List<SymbolId> viewSymbols = new List<SymbolId>() { new SymbolId("ES", ""), new SymbolId("ES", ""), new SymbolId("ES", ""), new SymbolId("Vix", "") };
                        Signals_internal = SignalsFactory.GetAllSignals(viewSymbols, Utils.portfolioFileName, MainWindowViewModel.PYTHON_READY);
                    }

                    foreach (var signal in Signals_internal)
                        signal.ActiveForEntry = signal.ActiveForExit = false;

                    var v = CollectionViewSource.GetDefaultView(Signals_internal);
                    if (v.GroupDescriptions?.Count == 0)
                        v.GroupDescriptions.Add(new PropertyGroupDescription("GroupId"));
                }
                return Signals_internal;
            }
            private set
            {
                Signals_internal = value;
                this.RaisePropertiesChanged();
            }
        }


        private static List<TickerData> tickers_cache;
        public virtual List<TickerData> Tickers { get; set; }
        public List<TickerData> SelectedTickers { get; set; } = new List<TickerData>();
        public virtual bool IsTickersLoading { get; set; }


        private int barsBack;

        public virtual SimulationSettings SimSettings { get; set; }

        protected void OnSimSettingsChanged()
        {
            // manually update market 2
            if (SimSettings?.MarketSymbolsIds?.Count > 0)
                M2Enabled = !SimSettings.MarketSymbolsIds[0].IsEmpty();
            else
                M2Enabled = false;

            // manually update market 3
            if (SimSettings?.MarketSymbolsIds?.Count > 1)
                M3Enabled = !SimSettings.MarketSymbolsIds[1].IsEmpty();
            else
                M3Enabled = false;
        }


        CancellationTokenSource cancellationTokenSource;


        private WatchlistViewModel TargetWatchlist;


        public static ScannerViewModel Create()
        {
            return ViewModelSource.Create(() => new ScannerViewModel());
        }
        protected ScannerViewModel()
        {
            Interval = new ChartIntervalItem() { Tag = Guid.NewGuid(), Caption = "1 day", MeasureUnit = DateTimeMeasureUnit.Day, MeasureUnitMultiplier = 1 };
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
                Interval,
                new ChartIntervalItem() { Tag = Guid.NewGuid(), Caption = "1 week", MeasureUnit = DateTimeMeasureUnit.Week, MeasureUnitMultiplier = 1 },
                new ChartIntervalItem() { Tag = Guid.NewGuid(), Caption = "1 month", MeasureUnit = DateTimeMeasureUnit.Month, MeasureUnitMultiplier = 1 }
            };

            // default sim settings
            SimSettings = SimTaskWizardViewModel.LoadSimSettings();
            SimSettings.InMemory = true;

            // message handlers
            Messenger.Default.Register<CustomIndicatorsChangedMessage>(this, OnMessage);
            Messenger.Default.Register<PortfolioChangedMessage>(this, OnMessage);

            // Load tickers
            _ = ReadTickersOnLoad();
        }

        #region Message handlers
        void OnMessage(CustomIndicatorsChangedMessage message)
        {
            //update custom signals
            SignalsFactory.UpdateCustomSignals(new List<SymbolId>() { new SymbolId("ES", ""), new SymbolId("ES", ""), new SymbolId("ES", ""), new SymbolId("Vix", "") },
                Signals, MainWindowViewModel.PYTHON_READY);
        }
        void OnMessage(PortfolioChangedMessage message)
        {
            //update portfolio signals
            Application.Current?.Dispatcher.Invoke(() =>
            {
                SignalsFactory.UpdatePortfolioSignals(Signals, message.PortfolioStrategies);
            });
        }

        #endregion

        #region Commands
        public void Cancel()
        {
            ProgressText = "Canceling...";
            //manualReset.Set();
            cancellationTokenSource?.Cancel();
        }

        public async Task Scan()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = new CancellationTokenSource();

            ProgressText = "Scanner init...";
            StageText = "";
            IsWaitIndicatorVisible = true;

            // prepare signals
            SimSettings.Signals = Signals.Where(s => s.ActiveForEntry || s.ActiveForExit).ToList();
            SignalsFactory.RealizeParametricSignals(SimSettings.Signals);

            if (SimSettings.Signals.Count == 0)
            {
                IsWaitIndicatorVisible = false;
                MessageBoxService.ShowMessage("No signals selected to scan");
                return;
            }

            if (SelectedTickers.Count == 0)
            {
                IsWaitIndicatorVisible = false;
                MessageBoxService.ShowMessage("No tickers selected to scan");
                return;
            }

            barsBack = SimSettings.Signals.Max(x => x.GetRequiredBarsCount()) + 10; // required bars count

            // create watchlist for output
            TargetWatchlist = CreateWatchlist();
            Messenger.Default.Send(new AddWatchlistMessage(TargetWatchlist));
            
            try
            {
                await Task.Run(ScanCore);  // run task
            }
            catch(AggregateException ex)
            {
                MessageBox.Show(ex.InnerException.Message);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                IsWaitIndicatorVisible = false;
            }
        }
        #endregion

        async Task ScanCore()
        {
            //PolygonIO.Client.Configuration config = new PolygonIO.Client.Configuration();
            //config.AddApiKey("apiKey", ""/*MainWindowViewModel.BrokersManager.PolygonIO.AccessToken*/);
            //
            //PolygonIO.Api.ReferenceApi referenceApi = new PolygonIO.Api.ReferenceApi(config);
            //PolygonIO.Api.StocksEquitiesApi stocksApi = new PolygonIO.Api.StocksEquitiesApi(config);

            //ConcurrentBag<PolygonIO.Model.TickersTickers> symbols = new ConcurrentBag<PolygonIO.Model.TickersTickers>();

            /*
            try
            {
                ProgressText = "Reading symbols list...";

                string tickerType = null;// "cs"; //common stocks

                int notactivecount = 0;
                int notsuccessfullcount = 0;

                var firstResp = referenceApi.V2ReferenceTickersGet("ticker", tickerType, "stocks", "us", null, 50);

                int totalCount = firstResp.Count ?? 0;

#if DEBUG
                totalCount = Math.Min(totalCount, 2000);      //for debugging
#endif

                if (totalCount > 0)
                {
                    foreach (var item in firstResp.Tickers)
                    {
                        if (item.Active.HasValue && item.Active.Value)
                            symbols.Add(item);
                        else
                            notactivecount++;
                    }

                    StageText = $"({symbols.Count + notactivecount}/{totalCount})";

                    List<Task> tasks = new List<Task>();

                    int pageNumber = 2;
                    for (; 50 * (pageNumber - 1) < totalCount; pageNumber++)
                    {
                        var gett = referenceApi.V2ReferenceTickersGetAsync("ticker", tickerType, "stocks", "us", null, 50, pageNumber);
                        var addt = gett.ContinueWith((x) =>
                        {
                            if (x.Result.Status == "OK")
                            {
                                foreach (var item in x.Result.Tickers)
                                {
                                    if (item.Active.HasValue && item.Active.Value)
                                        symbols.Add(item);
                                    else
                                        notactivecount++;
                                }

                                StageText = $"({symbols.Count + notactivecount}/{totalCount})";
                            }
                            else
                            {
                                notsuccessfullcount++;
                            }
                        });
                        tasks.Add(addt);
                    }

                    await Task.WhenAll(tasks);
                }
            }
            catch
            {
                throw;
            }

            // filter exchange
            List<PolygonIO.Model.TickersTickers> filteredSymbols = symbols.Where(x => FilterExchange(x.PrimaryExch)).OrderBy(x=>x.Ticker).ToList();*/

            List<TickerData> filteredSymbols = SelectedTickers.OrderBy(x => x.Ticker).ToList();

            ProgressText = "Reading symbols data...";
            StageText = "";

            if (filteredSymbols.Count > 0)
            {
                int ind = 0;
                foreach (var s in filteredSymbols)
                {
                    TargetWatchlist.DataSource.AppendSymbol(s.Ticker, "");

                    var q = new QuoteDefinitionModel(s.Ticker, ind++, "")
                    {
                        Symbol = s.Ticker
                    };
                    TargetWatchlist.DataSource.Data.Add(q);
                }
                _ = TargetWatchlist.DataSource.RequestLiveDataAsync();

                int z = 0;

                try
                {
                    List<Task> tasks = new List<Task>();

                    foreach (var s in filteredSymbols)
                    {
                        if (cancellationTokenSource.IsCancellationRequested)
                            return;

                        /*// Read last quote and previous close
                        double? lastPrice = null;
                        double? previousClose = null;
                        DateTime previousCloseDatetime = DateTime.MinValue; //EST
                        DateTime lastTradeDatetime = DateTime.MinValue;     //EST

                        try
                        {   // Previous Close
                            var resp = stocksApi.V2AggsTickerStocksTickerPrevGet(s.Ticker);
                            if (resp != null && resp.Status == "OK" && resp.ResultsCount > 0 && resp.Results.Count > 0)
                            {
                                var res = resp.Results.FirstOrDefault();
                                if (res != null && res.C.HasValue && res.C.Value != 0)
                                {
                                    previousClose = 0.01 + res.C.Value;
                                    lastPrice = previousClose;
                                    previousCloseDatetime = TimeZoneInfo.ConvertTimeFromUtc(Utils.ToDateTime(res.T.Value / 1000), easternZone);
                                }
                            }
                        }
                        catch (Exception ex) { }

                        try
                        {
                            var resp = stocksApi.V1LastStocksStocksTickerGet(s.Ticker);
                            if (resp != null && resp.Last != null && resp.Last.Price.HasValue && resp.Last.Price.Value != 0)
                            {
                                lastPrice = resp.Last.Price.Value;
                                lastTradeDatetime = TimeZoneInfo.ConvertTimeFromUtc(Utils.ToDateTime(resp.Last.Timestamp.Value / 1000), easternZone);
                            }
                        }
                        catch (Exception ex) { }

                        if (TargetWatchlist.DataSource.Data.FirstOrDefault(x => x.Name == s.Ticker) is QuoteDefinitionModel qt)
                        {
                            if (qt.LastPrice == 0 && lastPrice.HasValue)
                                qt.LastPrice = (decimal)lastPrice;
                            if (previousClose.HasValue)
                                qt.PreviousClose = (decimal)previousClose;
                        }
                        else
                        {
                            int f = 0;
                        }*/


                        if (MainWindowViewModel.BrokersManager.ActiveBroker == MainWindowViewModel.BrokersManager.TradeStation)
                        {
                            if (tasks.Count >= 8)  //limit max parallel requests count
                            {
                                //var completedTask = await Task.WhenAny(tasks);
                                //tasks.Remove(completedTask);
                                await Task.WhenAll(tasks);
                                tasks.Clear();
                            }
                        }

                        // Request Bars
                        Application.Current?.Dispatcher.Invoke(() =>
                        {
                            Task t = MainWindowViewModel.BrokersManager.GetBarsRange(s.Ticker, new ChartIntervalItem() { MeasureUnit = DateTimeMeasureUnit.Day, MeasureUnitMultiplier = 1 },
                                new DateTime(2015, 1, 1)).ContinueWith(async (ta) =>
                            {
                                if (ta.Status != TaskStatus.RanToCompletion) return;

                                int result = 3; // no data
                                if (ta.Result is ObservableCollection<TradingData> data && data.Count() > barsBack)
                                {
                                    var res = await SimulateSymbol(s.Ticker, data, barsBack);
                                    result = res ? 2 : 1;
                                }

                                _ = Application.Current?.Dispatcher.InvokeAsync(() =>
                                {
                                    if (TargetWatchlist.DataSource.Data.FirstOrDefault(x => x.Symbol == s.Ticker) is QuoteDefinitionModel q)
                                    {
                                        q.ScanResult = result;       //todo: if no data!
                                    }
                                    else
                                    {
                                        int f = 0;
                                    }

                                    z++;
                                    StageText = $"{s.Ticker} ({z}/{filteredSymbols.Count})";
                                });
                            });

                            tasks.Add(t);
                        });


                        /*
                        var response = stocksApi.V2AggsTickerStocksTickerRangeMultiplierTimespanFromToGet(s.Ticker, 1, "day", "2015-01-01", DateTime.UtcNow.ToString("yyyy-MM-dd"), true);

                        if (response?.Status == "OK")
                        {
                            if (response.ResultsCount > barsBack && Data.ContainsKey(s.Ticker))
                            {
                                TradingData extraBar = null;

                                if (IsExtendedHours && marketState>1 )  //if extended hours or closed then add additional bar
                                {
                                    var extraResp = stocksApi.V2AggsTickerStocksTickerRangeMultiplierTimespanFromToGet(s.Ticker, 30, "minute",
                                        Utils.ToDateTime(response.Results.Last().T.Value / 1000).ToString("yyyy-MM-dd"),    //UTC
                                        DateTime.UtcNow.ToString("yyyy-MM-dd"), true);

                                    if (extraResp.Status == "OK" && extraResp.ResultsCount>0)
                                    {
                                        TimeSpan marketStopTime = new TimeSpan(16, 0, 0);   //EST
                                        bool isPreMarket = Utils.ToDateTime(response.Results.Last().T.Value / 1000).Date < Utils.ToDateTime(extraResp.Results.First().T.Value / 1000).Date;

                                        double? Open = default;
                                        double High = 0, Low = double.MaxValue, Close = 0;
                                        DateTime dt = DateTime.MinValue;
                                        double Volume = 0;

                                        foreach(var v in extraResp.Results)
                                        {
                                            dt = TimeZoneInfo.ConvertTimeFromUtc(Utils.ToDateTime(v.T.Value / 1000), easternZone);
                                            if (isPreMarket == false && dt.TimeOfDay <= marketStopTime)    //skip previous bars for postmarket
                                                continue;

                                            if (Open.HasValue == false)
                                                Open = v.O;

                                            High = Math.Max(High, v.H ?? 0);
                                            Low = Math.Min(Low, v.L ?? double.MaxValue);
                                            Close = v.C ?? 0;
                                            Volume += v.V ?? 0;
                                        }

                                        if(Open.HasValue)
                                            extraBar = new TradingData(dt, Open.Value, High, Low, Close, Volume);
                                    }
                                }

                                _ = Application.Current?.Dispatcher.InvokeAsync(async () =>
                                {
                                    foreach (var item in response.Results)
                                    {
                                        var td = new TradingData(TimeZoneInfo.ConvertTimeFromUtc(Utils.ToDateTime((item.T ?? 0) / 1000),easternZone),   //EST
                                            item.O ?? 0, item.H ?? 0, item.L ?? 0, item.C ?? 0, item.V ?? 0);
                                        Data[s.Ticker].Add(td);
                                    }

                                    if (extraBar != null)
                                        Data[s.Ticker].Add(extraBar);

                                    var res = await SimulateSymbol(s.Ticker, Data[s.Ticker], barsBack);
                                    if (res)
                                    {
                                        if (TargetWatchlist.DataSource.Data.FirstOrDefault(x => x.Name == s.Ticker) is QuoteDefinitionModel q)
                                            q.ScanResult = 2;       //todo: if no data!
                                        else
                                        {
                                            int f = 0;
                                        }
                                    }
                                    else
                                    {
                                        if (TargetWatchlist.DataSource.Data.FirstOrDefault(x => x.Name == s.Ticker) is QuoteDefinitionModel q)
                                            q.ScanResult = 1;       //todo: if no data!
                                        else
                                        {
                                            int f = 0;
                                        }
                                    }
                                });
                            }
                            else
                            {
                                if (TargetWatchlist.DataSource.Data.FirstOrDefault(x => x.Name == s.Ticker) is QuoteDefinitionModel q)
                                    q.ScanResult = 3;       //todo: if no data!
                                else
                                {
                                    int f = 0;
                                }
                            }
                        }
                        else
                        {
                            int i = 0;
                        }*/
                    }

                    await Task.WhenAll(tasks);
                }
                catch (Exception ex) { }

            }




#if fskfjbskf
            //------------------------------------------------
            List<string> symbolNames = new List<string>();

            {
                ProgressText = "Reading symbol lists...";

                SymbolListsDefinition symbolLists = null;

                try
                {
                    symbolLists = Api.GetSymbolLists(MainWindowViewModel.TSAccessToken);
                }
                catch (Exception ex)
                {
                    throw new Exception("Can't read symbols description from TradeStation server!", ex);
                }

                if (symbolLists != null)
                {
                    int z = 0;

                    foreach (var slist in symbolLists)
                    {
                        if (cancellationTokenSource.IsCancellationRequested)
                            return;

                        if (slist.ID == "SP500")
                        {
                            try
                            {
                                List<SymbolListSymbolsDefinitionInner> symbols2 = Api.GetSymbolListSymbolsByID(MainWindowViewModel.TSAccessToken, slist.ID);

                                symbolNames.AddRange(symbols2.Where(x => !x.Name.StartsWith("$")).Select(x => x.Name));      //skip $xxxx symbols
                                symbolNames = symbolNames.Distinct().ToList();

                                //MessageBox.Show($"New: {symbols.Count}. Total: {symbolNames.Count}. Last Group name: {slist.Name}");
                            }
                            catch
                            {
                                //MessageBox.Show("Exception on " + slist.Name);
                            }
                        }

                        z++;
                        StageText = $"({z}/{symbolLists.Count})";
                    }
                }
            }

            ProgressText = "Reading symbols data...";
            StageText = "";

            if (symbolNames.Count > 0)
            {
                foreach (var symbolName in symbolNames)
                    TargetWatchlist.DataSource.AppendSymbol(symbolName, "");
                _ = TargetWatchlist.DataSource.RequestLiveDataAsync();

                int z = 0;

                foreach (var symbolName in symbolNames)
                {
                    if (cancellationTokenSource.IsCancellationRequested)
                        return;

                    Data.Add(symbolName, new List<TradingData>());

                    var v = await Api.StreamBarchartsBarsBackAsync(MainWindowViewModel.TSAccessToken, symbolName, 1, "Daily", barsBack, DateTime.Now.ToString("MM-dd-yyyy"),
                        OnDataReceived, null, cancellationTokenSource.Token, "USEQPreAndPost");

                    z++;
                    StageText = $"{symbolName} ({z}/{symbolNames.Count})";

                    //if (z == 10)
                    //    break;
                }

                // force to reload quotes
                _ = TargetWatchlist.DataSource.RequestLiveDataAsync();
            }
#endif
        }


        private static WatchlistViewModel CreateWatchlist()
        {
            var model = WatchlistViewModel.Create();
            model.Title = "Scan results";
            model.SymbolSearchEnabled = false;
            model.ScanColumnVisible = true;
            model.DataSource.Error += (message) =>
            {
                Messenger.Default.Send(new QuotesViewErrorMessage(message));
            };
            model.DataSource.ClearSymbols();
            return model;
        }

        private async Task ReadTickersOnLoad()
        {
            if (tickers_cache == null)
            {
                IsTickersLoading = true;
                tickers_cache = await MainWindowViewModel.BrokersManager.ReadStockTickers();
                IsTickersLoading = false;
            }
            Tickers = tickers_cache;
        }

        private async Task<bool> SimulateSymbol(string symbol, IList<TradingData> data, int barsBack)
        {
            if (data == null || data.Count < barsBack) return false;

            var baseSymbolId = new SymbolId(symbol, "");

            // ensure bars order is correct (sort by datetime)
            var dataSorted = data.OrderBy(x => x.Date);

            int[] Dates = dataSorted.Select(x => x.Date.Year * 10000 + x.Date.Month * 100 + x.Date.Day).ToArray();
            int[] Times = dataSorted.Select(x => x.Date.Hour * 10000 + x.Date.Minute * 100 + x.Date.Second).ToArray();
            float[] Opens = dataSorted.Select(x => (float)x.Open).ToArray();
            float[] Highs = dataSorted.Select(x => (float)x.High).ToArray();
            float[] Lows = dataSorted.Select(x => (float)x.Low).ToArray();
            float[] Closes = dataSorted.Select(x => (float)x.Close).ToArray();
            int[] Volumes = dataSorted.Select(x => (int)x.Volume).ToArray();

            RAWdata rawData = new RAWdata(baseSymbolId, Dates, Times, Opens, Highs, Lows, Closes, Volumes, false);

            // copy settings for parallel processing
            var simSettingsLocal = SimSettings.Clone() as SimulationSettings;

            // Generate Signals
            try
            {
                Dictionary<SymbolId, KeyValuePair<bool, string>> sdf = new Dictionary<SymbolId, KeyValuePair<bool, string>>() {
                            { new SymbolId("Vix", ""), new KeyValuePair<bool, string>(true, "Data/VixData.txt") },
                            { baseSymbolId, new KeyValuePair<bool, string>(false, "") }    // do not load, just keep filename for noise test and optimizations for python signals
                        };
                //for (int z = 0, k = 0; z < simSettings.MarketSymbolsIds?.Count; z++)
                //    if (!simSettings.MarketSymbolsIds[z].IsEmpty() && !sdf.ContainsKey(simSettings.MarketSymbolsIds[z]))
                //        sdf.Add(simSettings.MarketSymbolsIds[z], new KeyValuePair<bool, string>(true, marketFiles[k++]));

                // replace main, m2/3 symbols with current symbols
                List<SymbolId> syd = new List<SymbolId>() { sdf.Keys.First(), baseSymbolId };
                //if (simSettings.MarketSymbolsIds != null)
                //    foreach (var symId in simSettings.MarketSymbolsIds)
                //        syd.Add(symId);     //do not skip empty items
                var syd_array = syd.ToArray();
                foreach (var s in simSettingsLocal.Signals)
                    s.SetSymbolId(syd_array);

                //double margin = 0;// Utils.SymbolsManager.GetSymbolMargin(symbolIds[i]);

                simSettingsLocal.SetSymbolSpecificArgs(sdf, baseSymbolId, 1, 0, null, null, null);

                RawDataArray rDataArray = new RawDataArray(rawData, Dates.First(), Times.First(), Dates.Last(), Times.Last(), baseSymbolId,
                    false, true, simSettingsLocal.AccountCurrency, simSettingsLocal.ATR_lookback,
                    simSettingsLocal.PosMode, simSettingsLocal.SlipMode, simSettingsLocal.CommissMode,
                    1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, Utils.SymbolsManager);



                //TEST signals symbols
                //if(simSettingsLocal.Signals.Any(x=> x.AllChildren.Any(y=> x.SymbolId!=y.SymbolId)) || rDataArray.symbolId != simSettingsLocal.Signals.First().SymbolId)
                //{
                //    Logger.Current.Warn($"SYMBOLS MISMATCH SS: {rDataArray.symbolId}");
                //}





                var res = await SignalsFactory.CalculateSignalsAsync(simSettingsLocal, rDataArray, null, null);

                if (res.Message != null)
                    throw new Exception($"Signals generator warning: {res.Message}");

                return res.EntrySignalOutput.Last() > 0;

                // save signals
                //var quote = TargetWatchlist.DataSource.Data.FirstOrDefault(x => x.Name == symbol);
                //if (quote != null)
                //{
                //    if (res.EntrySignalOutput.Last() > 0)
                //    {
                //        quote.ScanResult = 2;   //calculated positive
                //    }
                //    else
                //        quote.ScanResult = 1;   //calculated negative
                //}
            }
            catch
            {
            }
            return false;
        }
    }
}