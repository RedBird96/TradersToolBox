using System;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm;
using System.Collections.ObjectModel;
using TradersToolbox.Core;
using System.IO;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using DevExpress.Xpf.Core;
using DevExpress.Mvvm.POCO;
using System.ComponentModel;
using TradersToolbox.Core.Serializable;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TradersToolbox.Brokers;
using TradersToolbox.Data;
using System.Timers;
using System.Collections.Concurrent;

namespace TradersToolbox.ViewModels
{
    [POCOViewModel]
    public class PortfolioViewModel : PanelWorkspaceViewModel
    {
        //static int ConstructorRunCount = 0; // for debugging

        bool PortfolioChangedNotSaved;

        protected override string WorkspaceName => "BottomHost";

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual List<StrategiesListVM> StrategiesDataSource { get; set; }

        readonly string currentCurrency = "USD";            //current global account currency

        protected IMessageBoxService MessageBoxService { get { return this.GetService<IMessageBoxService>(); } }

        Timer liveTimer;
        bool canStartTimer;
        ConcurrentQueue<SimStrategy> strategiesToCalc = new ConcurrentQueue<SimStrategy>();

        public PortfolioViewModel()
        {
            //if (ConstructorRunCount != 0)
            //    throw new Exception($"{nameof(PortfolioViewModel)} created multiple times!");
            //ConstructorRunCount++;

            DisplayName = "Portfolio";

            liveTimer = new Timer(1000) { AutoReset = false };
            liveTimer.Elapsed += LiveTimer_Elapsed;
            canStartTimer = true;

            // read default config
            var conf = Properties.Settings.Default.SimConfigs;
            if (conf?.Count > 0 && !string.IsNullOrEmpty(conf[0]))
            {
                try
                {
                    var s = Serializer.Deserialize(conf[0], typeof(SimulationSettings)) as SimulationSettings;
                    currentCurrency = s.AccountCurrency;
                }
                catch (Exception ex)
                {
                    Logger.Current.Debug(ex, "Can't read configuration from settings!");
                }
            }

            // todo: read async
            // todo: read after the control rendered
            if (ReadPortfolio() is List<SimStrategy> c)
            {
                // calculate live PnL
                if(MainWindowViewModel.BrokersManager.IsLoggedIn)
                    _ = CreateBarStreamers(c);

                Messenger.Default.Register<BrokerChangedLoggedIn>(this, OnMessage);

                StrategiesDataSource = new List<StrategiesListVM>() {
                    new StrategiesListVM()
                    {
                        Title = "Main",
                        Strategies = new NotifyObservableCollection<SimStrategy>(c),
                        IsPortfolio = true
                    }
                };

                StrategiesDataSource[0].Strategies.OnItemPropertyChanged += Strategies_OnItemPropertyChanged;
                StrategiesDataSource[0].Strategies.CollectionChanged += Strategies_CollectionChanged;

                // currency conversion
                ConvertData(StrategiesDataSource[0].Strategies, currentCurrency);
            }

            Messenger.Default.Register<QuoteReceivedMessage>(this, OnMessage);
        }

        private void Strategies_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if(e.NewItems is IList<SimStrategy> newlist && newlist.Count>0)
            {
                _ = CreateBarStreamers(newlist);
            }

            PortfolioChangedNotSaved = true;
            Messenger.Default.Send(new PortfolioChangedMessage(StrategiesDataSource[0].Strategies));
        }

        private void Strategies_OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(SimStrategy.LivePnL) && e.PropertyName != nameof(SimStrategy.LivePosition))
                PortfolioChangedNotSaved = true;
            
            if(e.PropertyName == nameof(SimStrategy.Enabled) && sender is SimStrategy strat && strat.Parent==null && (strat.Children == null || strat.Children.Count==0))
            {
                if(strat.Enabled)
                {
                    _ = CreateBarStreamers(new List<SimStrategy>() { strat });
                }
                else
                {
                    CloseStreamersForStrategy(strat);
                    strat.LiveEntryPrice = 0;
                    strat.LivePnL = 0;
                    strat.LivePosition = 0;
                }
            }
        }

        private void OnMessage(BrokerChangedLoggedIn message)
        {
            _ = CreateBarStreamers(StrategiesDataSource[0].Strategies);
        }

        void OnMessage(QuoteReceivedMessage message)
        {
            if(StrategiesDataSource?.Count>0 && message.Last.HasValue)
            {
                var stToUpdate = StrategiesDataSource[0].Strategies.Where(x => x.Symbol == message.Symbol).ToList();
                stToUpdate.AddRange(StrategiesDataSource[0].Strategies.Where(x => x.Children?.Count > 0).SelectMany(x => x.Children).Where(x => x.Symbol == message.Symbol));

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var st in stToUpdate)
                    {
                        if (st.LivePosition != 0)
                        {
                            st.LivePnL = st.LivePosition * st.ContractMultiplier * (st.LiveEntryPrice - decimal.ToDouble(message.Last.Value));
                        }
                    }
                });
            }
        }

        private void CloseStreamersForStrategy(SimStrategy strat)
        {
            if (strat.BarsStreamers?.Count > 0)
            {
                foreach (IBarsStreamer streamer in strat.BarsStreamers)
                {
                    streamer.Close();   //ensure closed
                    streamer.Data.CollectionChanged -= Data_CollectionChanged;  //ensure no handle
                }
                strat.BarsStreamers.Clear();
            }
            {
                if (strat.BarsStreamerM2 is IBarsStreamer streamer)
                {
                    streamer.Close();   //ensure closed
                    streamer.Data.CollectionChanged -= Data_CollectionChanged;  //ensure no handle
                    strat.BarsStreamerM2 = null;
                }
            }
            {
                if (strat.BarsStreamerM3 is IBarsStreamer streamer)
                {
                    streamer.Close();   //ensure closed
                    streamer.Data.CollectionChanged -= Data_CollectionChanged;  //ensure no handle
                    strat.BarsStreamerM3 = null;
                }
            }
        }

        public async Task CreateBarStreamers(IList<SimStrategy> list)
        {
            foreach (var strat in list)
            {
                if (strat.Enabled && strat.IndxType != StrategySimType.CUSTOM)
                {
                    // close streamers
                    CloseStreamersForStrategy(strat);

                    // create new streamers --------------------------
                    // M2 -----------------------------------------------------------
                    if (strat.MarketSymbolsIds?.Count > 0 && strat.MarketSymbolsIds[0] != SymbolId.Empty)
                    {
                        var interval = GetInterval(strat.MarketSymbolsIds[0].Timeframe);
                        var barsStreamer = await MainWindowViewModel.BrokersManager.GetBarsStreamer(strat.MarketSymbolsIds[0].Name, interval, connect: false);
                        barsStreamer.HistorySettings.Type = StockHistorySettingsType.FirstData;
                        barsStreamer.HistorySettings.TimeStart = Utils.ToDateTime(strat.SignalStartDate, strat.SignalStartTime);
                        strat.BarsStreamerM2 = barsStreamer;
                        barsStreamer.Data.CollectionChanged += Data_CollectionChanged;
                        _ = MainWindowViewModel.BrokersManager.GetBarsStreamer(strat.MarketSymbolsIds[0].Name, interval, barsStreamer);   //connect async
                    }

                    // M3 --------------------------------------------------------------
                    if (strat.MarketSymbolsIds?.Count > 1 && strat.MarketSymbolsIds[1] != SymbolId.Empty)
                    {
                        var interval = GetInterval(strat.MarketSymbolsIds[1].Timeframe);
                        var barsStreamer = await MainWindowViewModel.BrokersManager.GetBarsStreamer(strat.MarketSymbolsIds[1].Name, interval, connect: false);
                        barsStreamer.HistorySettings.Type = StockHistorySettingsType.FirstData;
                        barsStreamer.HistorySettings.TimeStart = Utils.ToDateTime(strat.SignalStartDate, strat.SignalStartTime);
                        strat.BarsStreamerM3 = barsStreamer;
                        barsStreamer.Data.CollectionChanged += Data_CollectionChanged;
                        _ = MainWindowViewModel.BrokersManager.GetBarsStreamer(strat.MarketSymbolsIds[1].Name, interval, barsStreamer);   //connect async
                    }

                    // Base (should be after M2, M3)
                    if (strat.Children?.Count > 0)
                    {
                        foreach (var str in strat.Children)
                        {
                            var interval = GetInterval(str.Timeframe);
                            var barsStreamer = await MainWindowViewModel.BrokersManager.GetBarsStreamer(str.Symbol, interval, connect: false);
                            barsStreamer.HistorySettings.Type = StockHistorySettingsType.FirstData;
                            //barsStreamer.HistorySettings.BarsBack = 2 * 300;        //todo: signal bars back
                            barsStreamer.HistorySettings.TimeStart = Utils.ToDateTime(str.SignalStartDate, str.SignalStartTime);
                            if (strat.BarsStreamers == null)
                                strat.BarsStreamers = new List<object>();
                            strat.BarsStreamers.Add(barsStreamer);
                            barsStreamer.Data.CollectionChanged += Data_CollectionChanged;
                            _ = MainWindowViewModel.BrokersManager.GetBarsStreamer(str.Symbol, interval, barsStreamer);   //connect async
                        }

                        _ = MainWindowViewModel.BrokersManager.StreamQuotes(strat.Children.Select(x => x.Symbol).ToList(), false);
                    }
                    else
                    {
                        var interval = GetInterval(strat.Timeframe);
                        var barsStreamer = await MainWindowViewModel.BrokersManager.GetBarsStreamer(strat.Symbol, interval, connect: false);
                        barsStreamer.HistorySettings.Type = StockHistorySettingsType.FirstData;
                        //barsStreamer.HistorySettings.BarsBack = 2 * 300;        //todo: signal bars back
                        barsStreamer.HistorySettings.TimeStart = Utils.ToDateTime(strat.SignalStartDate, strat.SignalStartTime);
                        if (strat.BarsStreamers == null)
                            strat.BarsStreamers = new List<object>();
                        strat.BarsStreamers.Add(barsStreamer);
                        barsStreamer.Data.CollectionChanged += Data_CollectionChanged;
                        _ = MainWindowViewModel.BrokersManager.GetBarsStreamer(strat.Symbol, interval, barsStreamer);   //connect async

                        _ = MainWindowViewModel.BrokersManager.StreamQuotes(new List<string>() { strat.Symbol }, false);
                    }
                }
            }
        }

        private DevExpress.Xpf.Charts.ChartIntervalItem GetInterval(string timeframe)
        {
            DevExpress.Xpf.Charts.ChartIntervalItem interval = new DevExpress.Xpf.Charts.ChartIntervalItem()
            {
                MeasureUnit = DevExpress.Xpf.Charts.DateTimeMeasureUnit.Day,
                MeasureUnitMultiplier = 1
            };

            if (Regex.IsMatch(timeframe, @"\d*[smhDWM]"))
            {
                switch (timeframe.Last())
                {
                    case 's': interval.MeasureUnit = DevExpress.Xpf.Charts.DateTimeMeasureUnit.Second; break;
                    case 'm': interval.MeasureUnit = DevExpress.Xpf.Charts.DateTimeMeasureUnit.Minute; break;
                    case 'h': interval.MeasureUnit = DevExpress.Xpf.Charts.DateTimeMeasureUnit.Hour; break;
                    case 'D': interval.MeasureUnit = DevExpress.Xpf.Charts.DateTimeMeasureUnit.Day; break;
                    case 'W': interval.MeasureUnit = DevExpress.Xpf.Charts.DateTimeMeasureUnit.Week; break;
                    case 'M': interval.MeasureUnit = DevExpress.Xpf.Charts.DateTimeMeasureUnit.Month; break;
                }
                interval.MeasureUnitMultiplier = int.Parse(timeframe.Substring(0, timeframe.Length - 1));
            }
            return interval;
        }

        private void Data_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems?.Count > 0 && sender is ObservableCollection<TradingData> data)
            {
                var strat = StrategiesDataSource[0].Strategies.Where(x => x.BarsStreamers?.Count > 0).FirstOrDefault(x => x.BarsStreamers.Any(y => (y as IBarsStreamer).Data == data) ||
                        (x.BarsStreamerM2 != null && (x.BarsStreamerM2 as IBarsStreamer).Data == data) ||
                        (x.BarsStreamerM3 != null && (x.BarsStreamerM3 as IBarsStreamer).Data == data));
                if (strat != null)
                {
                    if(!strategiesToCalc.Contains(strat))
                    {
                        strategiesToCalc.Enqueue(strat);
                    }
                    if (canStartTimer)
                    {
                        canStartTimer = false;
                        liveTimer.Start();
                    }
                }
            }
        }

        private void LiveTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            List<SimStrategy> list = new List<SimStrategy>();
            while (strategiesToCalc.TryDequeue(out SimStrategy s))
                list.Add(s);

            foreach (var strat in list)
            {
                int barsBack = Math.Max(strat.EntrySignals.Max(x => x.GetRequiredBarsCount()), strat.ExitSignals?.Max(x => x.GetRequiredBarsCount()) ?? 0);

                if (strat.BarsStreamers?.Count>0 &&
                    strat.BarsStreamers.All(x => (x as IBarsStreamer).Data.Count > barsBack/* && (x as IBarsStreamer).IsHistoricalDataLoadingComplete*/) &&
                    (strat.BarsStreamerM2 == null || ((strat.BarsStreamerM2 as IBarsStreamer).Data.Count > barsBack /*&& (strat.BarsStreamerM2 as IBarsStreamer).IsHistoricalDataLoadingComplete*/)) &&
                    (strat.BarsStreamerM3 == null || ((strat.BarsStreamerM3 as IBarsStreamer).Data.Count > barsBack /*&& (strat.BarsStreamerM3 as IBarsStreamer).IsHistoricalDataLoadingComplete*/)))
                {
                    List<SimStrategy> basket = strat.Children?.ToList() ?? new List<SimStrategy>() { strat };

                    var dataList = strat.BarsStreamers.Select(x => (x as IBarsStreamer).Data).ToList();
                    IList<TradingData> data2 = null;
                    IList<TradingData> data3 = null;

                    if (strat.BarsStreamerM2 is IBarsStreamer streamer2)
                        data2 = streamer2.Data;
                    if (strat.BarsStreamerM3 is IBarsStreamer streamer3)
                        data3 = streamer3.Data;

                    Task.Run(() => SimulateStrategy(basket, dataList, data2, data3)).ContinueWith((t) =>
                    {
                        if (t.IsCompleted && !t.IsCanceled)
                        {
                            SimStrategy strategy = t.Result;
                            if (strategy != null)
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    if (strategy.ResMode(strategy.TradesCount - 1) == 9)   //END -> in position realtime
                                    {
                                        strat.LivePnL = strategy.results.Last();
                                        strat.LiveEntryPrice = strategy.LiveEntryPrice;

                                        if (strategy.Children == null || strategy.Children.Count == 0)
                                        {
                                            strat.LivePosition = strategy.LivePosition;
                                        }
                                    }
                                    else
                                    {
                                        strat.LivePnL = 0;
                                        strat.LiveEntryPrice = 0;
                                        strat.LivePosition = 0;
                                    }
                                });
                            }
                        }
                    });
                }

                //todo: complete bars only
                // TS: current date < Bar date (teal time bar should have dave > current)
                // PIO: bar date + Interval < current date
            }
            canStartTimer = true;
        }


        private RAWdata GetRAWdata(IList<TradingData> data, SymbolId sid)
        {
            if (data == null) return null;

            // ensure bars order is correct (sort by datetime)
            TradingData[] dataSorted = null;

            Application.Current.Dispatcher.Invoke(() =>
            {
                dataSorted = data.OrderBy(x => x.Date).ToArray();
            });
            
            

            int[] Dates = dataSorted.Select(x => x.Date.Year * 10000 + x.Date.Month * 100 + x.Date.Day).ToArray();
            int[] Times = dataSorted.Select(x => x.Date.Hour * 10000 + x.Date.Minute * 100 + x.Date.Second).ToArray();
            float[] Opens = dataSorted.Select(x => (float)x.Open).ToArray();
            float[] Highs = dataSorted.Select(x => (float)x.High).ToArray();
            float[] Lows = dataSorted.Select(x => (float)x.Low).ToArray();
            float[] Closes = dataSorted.Select(x => (float)x.Close).ToArray();
            int[] Volumes = dataSorted.Select(x => (int)x.Volume).ToArray();

            return new RAWdata(sid, Dates, Times, Opens, Highs, Lows, Closes, Volumes, false);
        }

        private SimStrategy SimulateStrategy(List<SimStrategy> sts, List<ObservableCollection<TradingData>> data, IList<TradingData> M2data, IList<TradingData> M3data)
        {
            SimStrategy st = sts[0];

            List<Signal> signals = st.EntrySignals.Select(x=>x.MakeClone()).ToList();
            if (st.ExitSignals != null)
                signals = signals.Union(st.ExitSignals.Select(x => x.MakeClone())).ToList();

            DateTime start = Utils.ToDateTime(st.SignalStartDate, st.SignalStartTime);
            List<double> margins = new List<double>();

            // Prepare data arrays
            List<RAWdata> rawData = new List<RAWdata>();
            RAWdata M2rawData = null;
            RAWdata M3rawData = null;

            for (int i = 0; i < sts.Count; i++)
            {
                rawData.Add(GetRAWdata(data[i], sts[i].SymbolId));

                if (data[i][0].Date > start)
                    start = data[i][0].Date;
                if (Utils.ToDateTime(sts[i].SignalStartDate, sts[i].SignalStartTime) > start)
                    start = Utils.ToDateTime(sts[i].SignalStartDate, sts[i].SignalStartTime);

                margins.Add(Utils.SymbolsManager.GetSymbolMargin(sts[i].SymbolId));
            }
            if (st.MarketSymbolsIds?.Count > 0 && st.MarketSymbolsIds[0] != SymbolId.Empty)
                M2rawData = GetRAWdata(M2data, st.MarketSymbolsIds[0]);
            if (st.MarketSymbolsIds?.Count > 1 && st.MarketSymbolsIds[1] != SymbolId.Empty)
                M3rawData = GetRAWdata(M3data, st.MarketSymbolsIds[1]);

            // Prepare settings
            SimulationSettings simSettings = new SimulationSettings(signals, start, DateTime.Now, true, false,
                null, null, st.SymbolId, null, null, null, 0, st.ContractMultiplier,
                st.TradeMode, st.PT_ON, st.SL_ON, st.TL_ON, (float)st.PT_mult, (float)st.SL_mult, st.TL_mult, st.HH_ON, st.LL_ON,
                st.HHlook, st.LLlook, st.ATR_len, st.PosSizeMode, st.SlippageMode, st.CommissionMode, st.Commission, st.Slippage,
                st.Currency, st.AccountValue, st.MarketSymbolsIds.ToList(), st.InvestCash, st.MaxTime, st.ProfX, st.Fitness,
                st.OosLocation, 0, 1, 1, st.EntryOnClose, st.ExitOnClose,
                st.ForceExitOnSessionEnd ? (short)1 : (short)0,
                new DateTime(1, 1, 1, st.SessionEndTime / 100, st.SessionEndTime % 100, 0),
                st.DelayedEntry, st.RebalanceMethod, st.RebalancePeriod, st.RebalanceSymbolsCount, 0, 0, 0, 3, true, 0, 0, 0, 0, 0, 0, 0);

            List<RawDataArray> dataset = new List<RawDataArray>();
            for (int i = 0; i < sts.Count; i++)
            {
                var res = sts[i];

                // Take complete bars only ---------------------------------------------
                DateTime stopDT = DateTime.Now;
                if (MainWindowViewModel.BrokersManager.ActiveBroker == MainWindowViewModel.BrokersManager.PolygonIO)    
                {

                    DevExpress.Xpf.Charts.ChartIntervalItem interval = null;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        interval = GetInterval(res.Timeframe);
                    });

                    switch (interval.MeasureUnit)
                    {
                        case DevExpress.Xpf.Charts.DateTimeMeasureUnit.Minute:
                            stopDT = stopDT.AddMinutes(-interval.MeasureUnitMultiplier);
                            break;
                        case DevExpress.Xpf.Charts.DateTimeMeasureUnit.Hour:
                            stopDT = stopDT.AddHours(-interval.MeasureUnitMultiplier);
                            break;
                        case DevExpress.Xpf.Charts.DateTimeMeasureUnit.Day:
                            stopDT = stopDT.AddDays(-interval.MeasureUnitMultiplier);
                            break;
                        case DevExpress.Xpf.Charts.DateTimeMeasureUnit.Week:
                            stopDT = stopDT.AddDays(-7 * interval.MeasureUnitMultiplier);
                            break;
                        case DevExpress.Xpf.Charts.DateTimeMeasureUnit.Month:
                            stopDT = stopDT.AddMonths(-interval.MeasureUnitMultiplier);
                            break;
                    }
                }

                int StopDate = stopDT.Year * 10000 + stopDT.Month * 100 + stopDT.Day;
                int StopTime = stopDT.Hour * 10000 + stopDT.Minute * 100 + stopDT.Second;

                RawDataArray rda = new RawDataArray(rawData[i], res.SignalStartDate, res.SignalStartTime, StopDate, StopTime,
                    res.SymbolId, true, res.IsLong, res.Currency, res.ATR_len, res.PosSizeMode,
                    res.SlippageMode, res.CommissionMode, res.ContractMultiplier, margins[i],
                    res.AccountValue, res.EntryOnClose, res.PT_ON, res.SL_ON, res.TL_ON,
                    (float)res.PT_mult, (float)res.SL_mult, res.TL_mult, res.HHlook, res.LLlook,
                    res.Slippage, res.Commission, Utils.SymbolsManager);

                if (rda.Date == null)
                    return null;
                else
                    dataset.Add(rda);
            }

            // generate and simulate
            var output = Simulator.GenAndSim(dataset, M2rawData, M3rawData, simSettings, 0);

            if (!string.IsNullOrEmpty(output.Message))
            {
                //MessageBox.Show(output.Message);
                //throw new SimulatorException(output.Message);
                return null;
            }

            // Read results
            output = Simulator.ReadStratFromMemory();
            if (!string.IsNullOrEmpty(output.Message))
            {
                //throw new SimulatorException(output.Message);
                return null;
            }

            var strat = output.INstrategies?.FirstOrDefault();
            if (strat != null && (strat.Children == null || strat.Children.Count == 0))
            {
                float lastPosition = dataset[0].PosSizesWithoutSL.Last();
                strat.LivePosition = GenerateSignalsToTrade.FloorAndTrimPosSize(lastPosition, strat.PosSizeMode, strat.SymbolType, strat.SL_ON, (float)strat.SL_mult);
                if (strat.TradeMode == TradeMode.Short || strat.TradeMode == TradeMode.ShortRebalance)
                    strat.LivePosition = -strat.LivePosition;

                // open price
                DateTime lastEntryDT = Utils.ToDateTime(strat.resEntryDates.Last(), strat.resEntryTimes.Last());
                TradingData entryBar = data[0].FirstOrDefault(x => x.Date == lastEntryDT);
                if (entryBar != null)
                {
                    strat.LiveEntryPrice = strat.EntryOnClose > 0 ? entryBar.Close : entryBar.Open;
                }
            }

            return strat;
        }

        /// <summary>
        /// Read portfolio file and restore it from backups if needed
        /// </summary>
        public static List<SimStrategy> ReadPortfolio(string portfolioFileName = null)
        {
            string portfolioFN = portfolioFileName ?? Utils.portfolioFileName;

            if (File.Exists(portfolioFN))
            {
                List<SimStrategy> list = new List<SimStrategy>();
                int count;
                bool NoErrors;
                try
                {
                    using (BinaryReader reader = new BinaryReader(File.Open(portfolioFN, FileMode.Open)))
                    {
                        count = reader.ReadInt32();

                        for (int i = 0; i < count; i++)
                        {
                            SimStrategy res = new SimStrategy();
                            res.Read(reader, Utils.SymbolsManager);
                            list.Add(res);
                        }
                    }
                    NoErrors = true;
                }
                catch (Exception)
                {
                    //MessageBox.Show("Can't read portfolio file!\n\nError: " + e.Message, "File read error");
                    list.Clear();
                    NoErrors = false;
                }

                // Try read old format v 78------------------------
                if (!NoErrors)
                {
                    try
                    {
                        using (BinaryReader reader = new BinaryReader(File.Open(portfolioFN, FileMode.Open)))
                        {
                            count = reader.ReadInt32();

                            for (int i = 0; i < count; i++)
                            {
                                SimStrategy res = new SimStrategy();
                                res.Read(reader, Utils.SymbolsManager, 1);
                                list.Add(res);
                            }
                        }
                        NoErrors = true;
                    }
                    catch (Exception)
                    {
                        //MessageBox.Show("Can't read portfolio file!\n\nError: " + e.Message, "File read error");
                        list.Clear();
                        NoErrors = false;
                    }
                }

                // Try read old format v 71------------------------
                if (!NoErrors)
                {
                    try
                    {
                        using (BinaryReader reader = new BinaryReader(File.Open(portfolioFN, FileMode.Open)))
                        {
                            count = reader.ReadInt32();

                            for (int i = 0; i < count; i++)
                            {
                                SimStrategy res = new SimStrategy();
                                res.Read(reader, Utils.SymbolsManager, 2);
                                list.Add(res);
                            }
                        }
                        NoErrors = true;
                    }
                    catch (Exception e)
                    {
                        if (string.IsNullOrEmpty(portfolioFileName))
                            DXMessageBox.Show("Can't read portfolio file!\n\nError: " + e.Message, "File read error");
                        list.Clear();
                        NoErrors = false;
                    }
                }

                // Try to restore portfolio file------------------------
                if (!NoErrors && string.IsNullOrEmpty(portfolioFileName))
                {
                    if (DXMessageBox.Show("Do you want to restore a portfolio file from backups?", "Restore portfolio", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        string dir = "Backup";
                        try
                        {
                            if (Directory.Exists(dir))
                            {
                                // get all *.data files from backup directory
                                Dictionary<string, DateTime> filesDic = new Dictionary<string, DateTime>();
                                foreach (var fn in Directory.EnumerateFiles(dir))
                                    if (Path.GetExtension(fn) == ".data")
                                        filesDic.Add(fn, File.GetLastWriteTime(fn));

                                foreach (var kv in filesDic.OrderByDescending(x => x.Value))
                                {
                                    var restored = ReadPortfolio(kv.Key);
                                    if (restored != null)
                                    {
                                        // if restored successfull then copy backup file to original file
                                        File.Copy(kv.Key, Utils.portfolioFileName, true);
                                        DXMessageBox.Show("Portfolio file has restored successfully", "Restore portfolio");
                                        return restored;
                                    }
                                }
                                if (filesDic.Count == 0)
                                    throw new Exception("No backup files found");
                                else
                                    throw new Exception("Backups have no correct data");
                            }
                        }
                        catch (Exception ex)
                        {
                            DXMessageBox.Show("Can't restore portfolio file!\n\nError: " + ex.Message, "Restore portfolio error");
                        }
                    }
                }

                if (NoErrors) return list;
            }
            return null;
        }

        /// <summary>
        /// Backup and save portfolio file
        /// </summary>
        public void SavePortfolio(CancelEventArgs e = null)
        {
            if (PortfolioChangedNotSaved)
            {
                MessageBoxResult r = MessageBoxService.Show("Portfolio has been modified. Do you want to Save changes?", "Portfolio saving...", MessageBoxButton.YesNoCancel);
                if (r == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
                else if (r == MessageBoxResult.Yes)
                {
                    #region Backup portfolio file
                    try
                    {
                        if (File.Exists(Utils.portfolioFileName))
                        {
                            // create directory if not exists
                            string dir = "Backup";
                            if (!Directory.Exists(dir))
                                Directory.CreateDirectory(dir);

                            // find unique name and copy portfolio file
                            for (int i = 1; i < 100; i++)
                            {
                                string bakFileName = $"{dir}/{Path.GetFileNameWithoutExtension(Utils.portfolioFileName)}.bak{i}.data";
                                if (!File.Exists(bakFileName))
                                {
                                    File.Copy(Utils.portfolioFileName, bakFileName);
                                    break;
                                }
                            }

                            // get all *.data files from backup directory
                            Dictionary<string, DateTime> filesDic = new Dictionary<string, DateTime>();
                            foreach (var fn in Directory.EnumerateFiles(dir))
                                if (Path.GetExtension(fn) == ".data")
                                    filesDic.Add(fn, File.GetLastWriteTime(fn));

                            // delete old backups (keep 20 items max)
                            int maxItems = 20;
                            if (filesDic.Count > maxItems)
                            {
                                var toDelete = filesDic.OrderByDescending(x => x.Value).Skip(maxItems).Select(x => x.Key).ToList();
                                foreach (var fn in toDelete)
                                    File.Delete(fn);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBoxService.Show("Can't backup portfolio file!\n\nError: " + ex.Message, "Backup portfolio error");
                        e.Cancel = true;
                        return;
                    }
                    #endregion

                    #region Saving portfolio to file
                    var stratList = StrategiesDataSource?.FirstOrDefault(x => x.IsPortfolio)?.Strategies;
                    if (stratList != null)
                    {
                        try
                        {
                            using (BinaryWriter writer = new BinaryWriter(File.Open(Utils.portfolioFileName, FileMode.Create)))
                            {
                                writer.Write(stratList.Count);
                                foreach (SimStrategy res in stratList)
                                    res.Write(writer);
                            }
                            PortfolioChangedNotSaved = false;   // successful writing
                        }
                        catch (Exception ex)
                        {
                            MessageBoxService.Show("Can't save portfolio file!\n\nError: " + ex.Message, "File save error");
                            e.Cancel = true;
                            return;
                        }
                    }
                    #endregion
                }
            }
        }

        // todo: convert portfolio currency
        private void ConvertData(IEnumerable<SimStrategy> strategies, string currency)    //currency conversion
        {
            // Convert multisymbol portfolio with child strategies
            foreach(var st in strategies)
            {
                int r = CurrencyConvertor.GetConversationRules(st.Currency, currency, out string[] convRulesPortfolio);
                if (r != 0)
                    MessageBoxService.Show(st.Currency + " to " + currency + ". No available currency convertor found! Error code: " + r);

                if (convRulesPortfolio[0].Length > 0)
                {
                    CurrencyConvertor.ConvertCurrencyInList(convRulesPortfolio, st.resDates, st.results, false);
                    st.RecalcParams();

                    //convert child strategies
                    if (st.Children != null)
                        foreach (var ch in st.Children)
                        {
                            CurrencyConvertor.ConvertCurrencyInList(convRulesPortfolio, ch.resDates, ch.results, false);
                            ch.RecalcParams();
                        }
                }
            }
        }
    }
}