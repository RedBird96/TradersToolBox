using System;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm;
using System.ComponentModel;
using System.Collections.ObjectModel;
using TradersToolbox.Core;
using DevExpress.Mvvm.POCO;
using System.Collections.Generic;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors.Internal;
using System.Linq;
using System.Text;
using TradersToolbox.Core.Serializable;
using System.Globalization;
using TradersToolbox.Views;
using ZedGraph;
using TTWinForms;
using System.Threading.Tasks;
using System.Collections;
using System.Data;
using System.IO;
using System.Threading;
using TradersToolbox.Data;
using System.Windows;
using System.Windows.Controls;
using DevExpress.Xpf.Grid;

namespace TradersToolbox.ViewModels
{
    [POCOViewModel]
    public class StrategiesViewModel : IDocumentContent, ISupportParameter
    {
        public virtual List<StrategiesListVM> StrategiesLists { get; set; }

        StrategiesListVM SelectedStratList
        {
            get
            {
                if (StrategiesLists != null && TabSelectedIndex >= 0 && TabSelectedIndex < StrategiesLists.Count)
                    return StrategiesLists[TabSelectedIndex];
                else
                    return null;
            }
        }

        ObservableCollection<SimStrategy> SelectedCollection => SelectedStratList?.Strategies;
        ObservableCollection<SimStrategy> SelectedStrategies => SelectedStratList?.SelectedStrategies;


        readonly TradeMode tradeMode = TradeMode.Long;      //current global trade mode
        readonly string currentCurrency = "USD";            //current global account currency
        readonly PositionSizingMode PosModeGlobal;
        readonly SlippageMode SlipModeGlobal;
        readonly CommissionMode CommissModeGlobal;

        //readonly bool continuousMode;
        readonly SymbolId continuousInvestCash;
        readonly IReadOnlyList<SymbolId> continuousMarketSymbols;
        readonly int continuousStopDate;

        public List<SimStrategy> RandomInResults = new List<SimStrategy>(),
            RandomOutResults = new List<SimStrategy>(),
            RandomAllResults = new List<SimStrategy>();
        public SymbolId vsOther1Symbol, vsOther2Symbol, vsOther3Symbol;


        public virtual HeaderLocation TabHeaderLocation { get; set; }
        public virtual int TabSelectedIndex { get; set; }

        public virtual bool IsContinuous { get; set; }
        public virtual bool IsContinuousRunning { get; set; }
        public virtual string ContinuousPerfString { get; set; } = "0 strategies/sec";

        ulong ContinuousPerformance;
        int ContinuousLastProgress;
        bool continuousError;
        DateTime ContinuousPerfUpdateDt = DateTime.Now;
        readonly System.Timers.Timer ContinuousTimer;

        public virtual double ResWindowWidth { get; set; }
        public virtual double ResWindowHeight { get; set; }

        protected void OnResWindowWidthChanged()
        {
            Properties.Settings.Default.ResultsWindowWidth = ResWindowWidth;
        }

        protected void OnResWindowHeightChanged()
        {
            Properties.Settings.Default.ResultsWindowHeight = ResWindowHeight;
        }

        protected IMessageBoxService MessageBoxService { get { return this.GetService<IMessageBoxService>(/*ServiceSearchMode.PreferParents*/); } }
        protected IDocumentManagerService DocumentManagerService { get { return this.GetService<IDocumentManagerService>(); } }
        protected ISaveFileDialogService SaveFileDialogService { get { return this.GetService<ISaveFileDialogService>(); } }
        protected IDialogService DialogServiceSettings { get { return this.GetService<IDialogService>("TestSettingsDialog"); } }
        protected IDialogService DialogServiceReRun { get { return this.GetService<IDialogService>("ReRunDialog"); } }
        protected IDialogService DialogServiceFavorites { get { return this.GetService<IDialogService>("AddToFavoritesDialog"); } }
        protected IDialogService DialogServiceAddRule { get { return this.GetService<IDialogService>("AddRuleDialog"); } }
        protected IWindowService WindowServiceOptimize { get { return this.GetService<IWindowService>(); } }


        public static StrategiesViewModel Create()
        {
            return ViewModelSource.Create(() => new StrategiesViewModel());
        }
        protected StrategiesViewModel()
        {
            ResWindowWidth = Properties.Settings.Default.ResultsWindowWidth;
            ResWindowHeight = Properties.Settings.Default.ResultsWindowHeight;

            ContinuousTimer = new System.Timers.Timer(5000);
            ContinuousTimer.Elapsed += ContinuousTimer_Elapsed;
            ContinuousTimer.AutoReset = false;

            // register messenger
            Messenger.Default.Register<AddStrategiesToPortfolioMessage>(this, OnMessage);

            // read default config
            var c = Properties.Settings.Default.SimConfigs;
            if (c?.Count > 0 && !string.IsNullOrEmpty(c[0]))
            {
                try
                {
                    var s = Serializer.Deserialize(c[0], typeof(SimulationSettings)) as SimulationSettings;
                    currentCurrency = s.AccountCurrency;
                    tradeMode = s.TradeMode;
                    PosModeGlobal = s.PosMode;
                    SlipModeGlobal = s.SlipMode;
                    CommissModeGlobal = s.CommissMode;
                    continuousInvestCash = s.InvestCashSymId;
                    continuousMarketSymbols = s.MarketSymbolsIds;
                    continuousStopDate = s.StopDateTime.Year * 10000 + s.StopDateTime.Month * 100 + s.StopDateTime.Day;
                }
                catch (Exception ex)
                {
                    Logger.Current.Debug(ex, "Can't read configuration from settings!");
                }
            }
        }

        private async void ContinuousTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // Handle continuous simulation
            if (IsContinuous)
            {
                int p = CppSimulatorInterface.GetContinuousProgress();
                if (p != ContinuousLastProgress)
                {
                    Logger.Current.Debug("Continuous Simulation: max progress {0}", p);
                    ContinuousLastProgress = p;
                }

                try
                {
                    if (Simulator.ContinuousSimThread != null)   // if thread alive
                    {
                        //if (toolStripLabelSim.Visible)  // if controls visible
                        {
                            double s = (DateTime.Now - ContinuousPerfUpdateDt).TotalSeconds;
                            if (s >= 0.9)
                            {
                                ContinuousPerfUpdateDt = DateTime.Now;
                                if (IsContinuousRunning)
                                    ContinuousPerformance = (ulong)(0.8 * ContinuousPerformance + 0.2 * CppSimulatorInterface.GetTradeFuncPerformance() / s);   // per one sec clearly
                                else
                                    ContinuousPerformance = 0;

                                double v = ContinuousPerformance;
                                string format = "{0} strategies/sec";
                                if (1000 < v && v < 1000000) { v /= 1000; format = "{0:F2}K strat/sec"; }
                                else if (v > 1000000) { v /= 1000000; format = "{0:F2}M strat/sec"; }

                                ContinuousPerfString = string.Format(format, v);
                            }
                        }

                        if (IsContinuousRunning)
                        {
                            // read continuous results in separate thread
                            await ReadContinuousResults();
                        }
                    }
                    ContinuousTimer.Start();    //restart timer
                }
                catch (Exception ex)
                {
                    MessageBoxService.Show(ex.Message, "Continuous results read error!");
                }
            }
        }

        async Task ReadContinuousResults()
        {
            await Task.Run(() =>
            {
                int continuousIdx = 1;
                List<SimStrategy> newR3 = new List<SimStrategy>(),
                    newOOS = new List<SimStrategy>(), newALL = new List<SimStrategy>();

                //read data from simulator through named pipe
                IntPtr ptr = IntPtr.Zero;
                ulong ulen = 0;
                long len = 0;

                try
                {
                    ulen = CppSimulatorInterface.ContinuousBeginRead(ref ptr);
                }
                catch
                {
                    if (!continuousError)
                    {
                        continuousError = true;
                        MessageBoxService.Show(@"Can't read continuous simulation results!

ContinuousBeginRead()
Writing data to memory buffer unsuccessful.
Probably not enough RAM", "Cross-process communication error");
                    }
                }

                try
                {
                    len = checked((long)ulen);
                }
                catch
                {
                    if (!continuousError)
                    {
                        continuousError = true;
                        MessageBoxService.Show("Can't read continuous simulation results!\n\nResults buffer is too long", "Cross-process communication error");
                    }
                }

                if (ptr != IntPtr.Zero && len > 0)
                {
                    unsafe
                    {
                        try
                        {
                            using (UnmanagedMemoryStream ms = new UnmanagedMemoryStream((byte*)ptr.ToPointer(), len))
                            {
                                BinaryReader reader = new BinaryReader(ms);
                                while (ms.Position < ms.Length)
                                {
                                    int r3count = reader.ReadInt32();
                                    int oosCount = reader.ReadInt32();
                                    int allCount = reader.ReadInt32();

                                    Simulator.ReadToList(reader, r3count, newR3, continuousStopDate, SlipModeGlobal, CommissModeGlobal, currentCurrency, continuousInvestCash, continuousMarketSymbols, continuousIdx);
                                    Simulator.ReadToList(reader, oosCount, newOOS, continuousStopDate, SlipModeGlobal, CommissModeGlobal, currentCurrency, continuousInvestCash, continuousMarketSymbols, continuousIdx);
                                    Simulator.ReadToList(reader, allCount, newALL, continuousStopDate, SlipModeGlobal, CommissModeGlobal, currentCurrency, continuousInvestCash, continuousMarketSymbols, continuousIdx);
                                    continuousIdx++;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            if (!continuousError)
                            {
                                continuousError = true;
                                MessageBoxService.Show("Can't read continuous simulation results!\n\nError: " + ex.Message, "Cross-process communication error");
                            }
                        }
                    }
                    // clear memory on c++ side
                    CppSimulatorInterface.ContinuousEndRead();

                    //convert currency for new data
                    /*{
                        List<IList<SimResult>> listToConvert = new List<IList<SimResult>>();
                        listToConvert.Add(newR3);
                        listToConvert.Add(newOOS);
                        listToConvert.Add(newALL);

                        string[] convRules;
                        int r = CurrencyConvertor.getConversationRulesForSymbol(continuousSymbol, currentCurrency, out convRules);
                        if (r != 0)
                            MessageBox.Show(continuousSymbol + " to " + currentCurrency + ". No available currency convertor found! Error code: " + r);

                        CurrencyConvertor.convertCurrencyInListOfList(convRules, listToConvert);

                        foreach (IList<SimResult> listStr in listToConvert)
                            for (int i = 0; i < listStr.Count; i++)
                                listStr[i] = recalcParams(listStr[i]);
                    }*/

                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        //insert new results
                        if (newR3.Count > 0 || newOOS.Count > 0 || newALL.Count > 0)
                        {
                            var baseList = StrategiesLists[0].Strategies;
                            var customStrategies = baseList.Where(x => x.IndxType == StrategySimType.CUSTOM).ToList();
                            baseList.Clear();
                            foreach (var item in newR3)
                                baseList.Add(item);
                            foreach (var item in customStrategies)
                                baseList.Add(item);
                            //for (int i=0;i< baseList.Count;i++)
                            //{
                            //    for (int j=0; j < newR3.Count; j++)
                            //    {
                            //        if(baseList[i].SortKey() > )
                            //    }
                            //}

                            baseList = StrategiesLists[1].Strategies;
                            baseList.Clear();
                            foreach (var item in newOOS)
                                baseList.Add(item);
                            foreach (var item in customStrategies)
                                baseList.Add(item);

                            baseList = StrategiesLists[2].Strategies;
                            baseList.Clear();
                            foreach (var item in newALL)
                                baseList.Add(item);
                            foreach (var item in customStrategies)
                                baseList.Add(item);
                        }
                    });
                }
            });
        }

        /// <summary>
        /// Dictionary<string, ObservableCollection<SimResult>>
        /// </summary>
        public virtual object Parameter { get; set; }

        public void OnParameterChanged()
        {
            if (Parameter is List<StrategiesListVM> list)
            {
                StrategiesLists = list;
                TabHeaderLocation = list.Count > 1 ? HeaderLocation.Top  : HeaderLocation.None;
                TabSelectedIndex = list.Count - 1;
            }
        }

        public void CreateDocument(string viewTypeName, object vm)
        {
            IDocument doc = DocumentManagerService.FindDocument(vm);
            if (doc == null)
            {
                doc = DocumentManagerService.CreateDocument(viewTypeName, vm);
                doc.Id = DocumentManagerService.Documents.Count();
            }
            doc.Show();
        }

        #region IDocumentContent
        public IDocumentOwner DocumentOwner { get; set; }

        public object Title { get; set; } = "Simulation results";

        public void OnClose(CancelEventArgs e)
        {
        }

        public void OnDestroy()
        {
        }
        #endregion

        #region Messenger
        public void OnMessage(AddStrategiesToPortfolioMessage message)
        {
            StrategiesListVM portfolioListVM = StrategiesLists?.FirstOrDefault(x => x.IsPortfolio);

            if (portfolioListVM == null)    // exit if no portfolio found
                return;

            foreach (var res in message.Strategies)
            {
                ConvertStrategyToDaily(res);

                res.Parent = null;  // reset parent for child strategy

                portfolioListVM.Strategies.Add(res);
            }

            //toolStripLabel2.Text = "Strategies in portfolio: " + portfolioResults.Count;
            //if (dataGridView1.SelectedRows.Count > 1)
            //    ShowMessage(dataGridView1.SelectedRows.Count.ToString() + " items added to portfolio");
            //else
            //    ShowMessage("1 item added to portfolio");
        }

        #endregion

        static void ConvertStrategyToDaily(SimStrategy strategy)
        {
            //todo: copy from old code (chikd processing + basket reconstruct)

            if (strategy == null) return;

            // sum intraday results ------------------------------
            List<float> sumTrades = new List<float>();
            List<float> sumTradesOriginal = new List<float>();
            List<int> sumEntryDates = new List<int>();
            List<int> sumExitDates = new List<int>();
            List<int> sumEntryTimes = new List<int>();
            List<int> sumExitTimes = new List<int>();
            float trade = 0;
            float tradeOriginal = 0;
            int todayTrades = 0;
            int prev_date = 0;
            int prev_time = 0;
            int start_date = 0;
            int start_time = 0;
            int prev_end_date = 0;
            int prev_end_time = 0;
            for (int j = 0; j < strategy.results.Length; j++)
            {
                int d = strategy.resEntryDates[j];
                int t = strategy.resEntryTimes[j];
                int session_end = strategy.SessionEndTime * 100;

                if ((d > prev_date && (prev_time < session_end || t >= session_end)) ||
                    (d == prev_date && prev_time < session_end && t >= session_end))
                {
                    if (todayTrades > 0)
                    {
                        sumTrades.Add(trade);
                        sumTradesOriginal.Add(tradeOriginal);
                        sumEntryDates.Add(start_date);
                        sumExitDates.Add(prev_end_date);
                        sumEntryTimes.Add(start_time);
                        sumExitTimes.Add(prev_end_time);
                    }
                    todayTrades = 0;
                    trade = 0;
                    tradeOriginal = 0;
                    start_date = strategy.resEntryDates[j];
                    start_time = strategy.resEntryTimes[j];
                }
                if (strategy.intradayTestResults != null && strategy.intradayTestResults.Count == 5)
                {
                    if (t < strategy.intradayTestResults[2] || t > strategy.intradayTestResults[3] || todayTrades >= strategy.intradayTestResults[4] ||
                        trade > strategy.intradayTestResults[0] || trade < strategy.intradayTestResults[1])
                        continue;
                }
                todayTrades++;
                trade += strategy.results[j];
                tradeOriginal += strategy.resultsOriginal[j];
                prev_date = d;
                prev_time = t;
                prev_end_date = strategy.resDates[j];
                prev_end_time = strategy.ResTime(j);
            }
            if (todayTrades > 0)    // add last trade
            {
                sumTrades.Add(trade);
                sumTradesOriginal.Add(tradeOriginal);
                sumEntryDates.Add(start_date);
                sumExitDates.Add(prev_end_date);
                sumEntryTimes.Add(start_time);
                sumExitTimes.Add(prev_end_time);
            }
            if (strategy.results.Length != sumTrades.Count)
            {
                strategy.results = sumTrades.ToArray();
                strategy.resultsOriginal = sumTradesOriginal.ToArray();
                strategy.resEntryDates = sumEntryDates.ToArray();
                strategy.resEntryTimes = sumEntryTimes.ToArray();
                strategy.resDates = sumExitDates.ToArray();
                strategy.SetResTimes(sumExitTimes.ToArray());
                strategy.RecalcParams();
            }
            strategy.Expanding &= 0xF9;  //make not child and collapsed

            strategy.Indx = 0;

            // process child strategies
            if (strategy.Children != null)
                foreach (var ch in strategy.Children)
                    ConvertStrategyToDaily(ch);
        }

        #region Commands

        public async Task EquityCurve()
        {
            if (SelectedStrategies==null || SelectedStrategies.Count == 0)
            {
                MessageBoxService.Show("No strategy selected");
                return;
            }
            //Cursor = Cursors.WaitCursor;

            try
            {
                SimStrategy res = SelectedStrategies[0];
                List<IReadOnlyList<float>> values = new List<IReadOnlyList<float>>();
                List<IReadOnlyList<float>> values_rand = new List<IReadOnlyList<float>>();
                List<IReadOnlyList<int>> dates = new List<IReadOnlyList<int>>();
                List<IReadOnlyList<int>> dates_rand = new List<IReadOnlyList<int>>();
                List<IReadOnlyList<int>> times_modes = new List<IReadOnlyList<int>>();
                List<IReadOnlyList<int>> times_rand_modes = new List<IReadOnlyList<int>>();
                string name = "{ NAME_IS_HERE }";

                void load(IList<SimStrategy> list_normal, IList<SimStrategy> list_random)
                {
                    values.Add(res.results);
                    dates.Add(res.resDates);
                    times_modes.Add(res.GetResTimes());
                    name = res.Name;
                    for (int i = 0, count = 0; count < 100 && i < list_normal.Count; i++)
                    {
                        // skip duplicates (equity curve)
                        if (values.Count(x => x.Zip(list_normal[i].results, (y, z) => Math.Abs(y - z)).Sum() < 0.01) == 0)
                        {
                            values.Add(list_normal[i].results);
                            dates.Add(list_normal[i].resDates);
                            times_modes.Add(list_normal[i].GetResTimes());
                            count++;
                        }
                    }
                    for (int i = 0; list_random!=null && i < 100 && i < list_random.Count; i++)
                    {
                        values_rand.Add(list_random[i].results);
                        dates_rand.Add(list_random[i].resDates);
                        times_rand_modes.Add(list_random[i].GetResTimes());
                    }
                }

                if(SelectedStratList.IsPortfolio)
                {
                    values.Add(res.results);
                    dates.Add(res.resDates);
                    times_modes.Add(res.GetResTimes());
                    name = res.Name;
                }
                else if (TabSelectedIndex == 0) load(SelectedCollection, RandomInResults);
                else if (TabSelectedIndex == 1) load(SelectedCollection, RandomOutResults);
                else if (TabSelectedIndex == 2) load(SelectedCollection, RandomAllResults);
                

                if (values[0].Count < 1)
                {
                    ShowMessage("Selected strategy has no results");
                    return;
                }

                List<PointPairList> ppls = new List<PointPairList>();
                List<PointPairList> ppls_rand = new List<PointPairList>();
                for (int j = 0; j < values.Count; j++)
                {
                    PointPairList ppl = new PointPairList();
                    double sum = 0;
                    for (int i = 0; i < values[j].Count; i++)
                    {
                        sum += values[j][i];
                        ppl.Add(i, sum, Security.ToDateTime(dates[j][i], times_modes[j][i] & 0xffffff).ToOADate());
                    }
                    ppls.Add(ppl);
                }
                for (int j = 0; j < values_rand.Count; j++)
                {
                    PointPairList ppl = new PointPairList();
                    double sum = 0;
                    for (int i = 0; i < values_rand[j].Count; i++)
                    {
                        sum += values_rand[j][i];
                        ppl.Add(i, sum, Security.ToDateTime(dates_rand[j][i], times_rand_modes[j][i] & 0xffffff).ToOADate());
                    }
                    ppls_rand.Add(ppl);
                }
                // Select OOS part of graph
                int oos_index = 0;
                if (res?.OosTradeIndex > 0)
                {
                    if (res.OosLocation == 0)   //begin
                        oos_index = -(res.OosTradeIndex - 1);
                    else
                        oos_index = res.OosTradeIndex;
                }

                #region Basket items
                List<PointPairList> pplBasket = null;
                List<string> basketLabels = null;
                List<int> basketOOSindexes = null;
                if (res.ChildStrategiesCount > 0)
                {
                    pplBasket = new List<PointPairList>(res.ChildStrategiesCount);
                    basketLabels = new List<string>(res.ChildStrategiesCount);
                    basketOOSindexes = new List<int>(res.ChildStrategiesCount);

                    foreach (var ch in res.ReadonlyChildren)
                    {
                        // construct line
                        PointPairList ppl = new PointPairList();
                        double sum = 0;
                        for (int j = 0; j < ch.ReadonlyResults.Count; ++j)
                        {
                            sum += ch.ReadonlyResults[j];
                            ppl.Add(j, sum, Security.ToDateTime(ch.ReadonlyResDates[j], ch.ResTime(j)).ToOADate());
                        }
                        pplBasket.Add(ppl);

                        basketLabels.Add(ch.SymbolId.ToString());
                        if (ch.OosTradeIndex > 0)
                        {
                            if (ch.OosLocation == 0)   //begin
                                basketOOSindexes.Add(-(ch.OosTradeIndex - 1));
                            else
                                basketOOSindexes.Add(ch.OosTradeIndex);
                        }
                        else
                            basketOOSindexes.Add(0);
                    }
                }
                #endregion

                #region vs.Others
                List<PointPairList> pplOthers = new List<PointPairList>();
                List<string> vsLabels = new List<string>();

                SymbolId[] vsIds = { vsOther1Symbol, vsOther2Symbol, vsOther3Symbol };
                foreach (var vss in vsIds)
                {
                    if (!vss.IsEmpty() && await Simulator.VsOthers(res, vss) is SimStrategy VsRes)
                    {
                        pplOthers.Add(GetEquityCurvePointList(VsRes.results, VsRes.resDates, VsRes.GetResTimes().Select(x => x & 0xffffff).ToArray()));
                        vsLabels.Add(vss.ToString());
                    }
                }
                #endregion

                //  buy and hold - Underlying line -------------------------------------
                PointPairList ppl_bah = null;
                if (res.ChildStrategiesCount == 0)
                {
                    ppl_bah = GetBuyAndHoldLine(res, res.Currency, 1, out _);   //todo: check currency (current Selected currency?)
                    if (ppl_bah == null)
                    {
                        MessageBoxService.Show("No strategy data was found. Can't chart buy and hold line. Please check user data settings.",
                                "Individual Equity Curve");
                    }
                }

                // show graph window
                Graph g = new Graph(ppls, ppls_rand, pplOthers, vsLabels, oos_index, ppl_bah, pplBasket, basketLabels, basketOOSindexes);
                var vm = GraphViewModel.Create();
                vm.Title = $"Equity Curve - {name}";
                vm.Graph = g;
                CreateDocument(nameof(GraphView), vm);
            }
            catch (Exception ex)
            {
                MessageBoxService.Show("Can't complete operation!\n\n" + ex.Message, "Equity Curve");
            }
            finally
            {
                //Cursor = Cursors.Default;
            }
        }

        public async Task Metrics()
        {
            if (SelectedStratList.IsPortfolio)   //portfolio mode
            {
                //Cursor = Cursors.WaitCursor;

                try
                {
                    int activeCount = SelectedCollection.Count(x => x.Enabled) + SelectedCollection.Where(x => x.ChildStrategiesCount > 0).Sum(x => x.Children.Count(c => c.Enabled));
                    //Cumulative Equity Curve
                    IReadonlySimStrategy res = GetPortfolioStrategyResult(SelectedCollection, SelectedStrategies.FirstOrDefault(), true);
                    if (res == null)
                        return;

                    PointPairList ppl = new PointPairList();
                    double sum = 0;
                    for (int i = 0; i < res.ReadonlyResults.Count; i++)
                    {
                        sum += res.ReadonlyResults[i];
                        ppl.Add(Security.ToDateTime(res.ReadonlyResDates[i], 0).ToOADate(), sum);
                    }

                    //  buy and hold - Underlying line
                    PointPairList ppl_buyAndHold = null;
                    if (res.ChildStrategiesCount == 0)
                    {
                        ppl_buyAndHold = GetBuyAndHoldLine(res, res.Currency, 1, out _);
                        if (ppl_buyAndHold == null)
                        {
                            MessageBoxService.Show("No strategy data was found. Can't chart buy and hold line. Please check user data settings.",
                                "Portfolio Equity Curve");
                        }
                        else
                            ppl_buyAndHold.ForEach(x => { x.X = x.Z; x.Z = 0; });
                    }

                    // construct "Market" line
                    SimStrategy marketRes = new SimStrategy()
                    {
                        Symbol = res.SymbolType == Core.SymbolType.ETF ? "SPY" : "ES",
                        SymbolType = res.SymbolType == Core.SymbolType.ETF ? Core.SymbolType.ETF : Core.SymbolType.Futures,
                        PosSizeMode = res.PosSizeMode,
                        AccountValue = res.AccountValue,
                        Currency = res.Currency,
                        TradeMode = res.TradeMode,
                        resDates = res.ReadonlyResDates.ToArray(),
                        ATR_len = res.ATR_len,
                        SessionEndTime = res.SessionEndTime,
                        SL_ON = res.SL_ON,
                        SL_mult = res.SL_mult
                    };
                    PointPairList ppl_market = GetBuyAndHoldLine(marketRes, res.Currency, activeCount, out _);
                    ppl_market.ForEach(x => { x.X = x.Z; x.Z = 0; });

                    // show graph window
                    Graph g = new Graph(1, ppl, ppl_buyAndHold, ppl_market);
                    var vm = GraphViewModel.Create();
                    vm.Title = $"Equity Curve - Cumulative results (Market: {marketRes.Symbol}, Underlying: {res.Symbol})";
                    vm.Graph = g;
                    CreateDocument(nameof(GraphView), vm);
                }
                catch (Exception ex)
                {
                    MessageBoxService.Show("Can't complete operation!\n\n" + ex.Message, "Cumulative Equity Curve");
                }
                finally
                {
                    //Cursor = Cursors.Default;
                }
            }
            else
            {  //single mode
                //E-Ratio - Vs. Random
                if (SelectedStrategies.Count == 0)
                {
                    ShowMessage("No strategy selected");
                    return;
                }

                //Cursor = Cursors.WaitCursor;

                try
                {
                    IReadonlySimStrategy res = res = SelectedStrategies.First();
                    IReadOnlyList<SimStrategy> realList = SelectedCollection, randList = null;
                    int histStepInPercent = 10;

                    if (TabSelectedIndex == 0) { randList = RandomInResults; }
                    else if (TabSelectedIndex == 1) { randList = RandomOutResults; }
                    else if (TabSelectedIndex == 2) { randList = RandomAllResults; }

                    List<PointPairList> pplVsRandList = new List<PointPairList>();
                    List<PointPairList> pplInVsOutList = new List<PointPairList>();
                    List<PointPairList> pplInTimeList = new List<PointPairList>();

                    {   //ERatio
                        ERatioForStrategy(res, out List<float> ERatios, out List<float> randERatios);
                        PointPairList ERList = new PointPairList();
                        for (int i = 0; i < ERatios.Count; i++)
                            ERList.Add(i, ERatios[i]);
                        PointPairList randERList = new PointPairList();
                        for (int i = 0; i < randERatios.Count; i++)
                            randERList.Add(i, randERatios[i]);
                        pplVsRandList.Add(ERList);
                        pplVsRandList.Add(randERList);
                    }

                    Task<PointPairList>[] tasksVsRand = new Task<PointPairList>[]
                    {
                        CalcHistogram(realList.Select(r => r.Net_PnL), histStepInPercent),
                        CalcHistogram(randList.Select(r => r.Net_PnL), histStepInPercent),
                        CalcHistogram(realList.Select(r => r.Drawdown), histStepInPercent),
                        CalcHistogram(randList.Select(r => r.Drawdown), histStepInPercent),
                        CalcHistogram(realList.Select(r => r.Drawdown == 0 ? 0 : r.Net_PnL / r.Drawdown), histStepInPercent),
                        CalcHistogram(randList.Select(r => r.Drawdown == 0 ? 0 : r.Net_PnL / r.Drawdown), histStepInPercent),
                        CalcHistogram(realList.Select(r => r.WinPercentage), histStepInPercent),
                        CalcHistogram(randList.Select(r => r.WinPercentage), histStepInPercent),
                        CalcHistogram(realList.Select(r => r.RatioWL), histStepInPercent),
                        CalcHistogram(randList.Select(r => r.RatioWL), histStepInPercent),
                        CalcHistogram(realList.Select(r => r.ProfitFactor), histStepInPercent),
                        CalcHistogram(randList.Select(r => r.ProfitFactor), histStepInPercent),
                        CalcHistogram(realList.Select(r => r.Net_PnL / r.TradesCount), histStepInPercent),
                        CalcHistogram(randList.Select(r => r.Net_PnL / r.TradesCount), histStepInPercent),
                        CalcHistogram(realList.Select(r => (double)r.TradesCount), histStepInPercent),
                        CalcHistogram(randList.Select(r => (double)r.TradesCount), histStepInPercent),
                        CalcHistogram(realList.Select(r => (double)r.Sharpe), histStepInPercent),
                        CalcHistogram(randList.Select(r => (double)r.Sharpe), histStepInPercent),
                        CalcHistogram(realList.Select(r => (double)r.Sortino), histStepInPercent),
                        CalcHistogram(randList.Select(r => (double)r.Sortino), histStepInPercent),
                        CalcHistogram(realList.Select(r => r.CPCRatio), histStepInPercent),
                        CalcHistogram(randList.Select(r => r.CPCRatio), histStepInPercent),
                        CalcHistogram(realList.Select(r => r.CoerCoef), histStepInPercent),
                        CalcHistogram(randList.Select(r => r.CoerCoef), histStepInPercent),
                        CalcHistogram(realList.Select(r => r.TTest), histStepInPercent),
                        CalcHistogram(randList.Select(r => r.TTest), histStepInPercent),
                        CalcHistogram(realList.Select(r => r.CAGR), histStepInPercent),
                        CalcHistogram(randList.Select(r => r.CAGR), histStepInPercent),
                        CalcHistogram(realList.Select(r => (double)r.KRatio), histStepInPercent),
                        CalcHistogram(randList.Select(r => (double)r.KRatio), histStepInPercent),
                        CalcHistogram(realList.Select(r => (double)r.ExpectancyScore), histStepInPercent),
                        CalcHistogram(randList.Select(r => (double)r.ExpectancyScore), histStepInPercent),
                        CalcHistogram(realList.Select(r => (double)r.PerfectProfitPercentage), histStepInPercent),
                        CalcHistogram(randList.Select(r => (double)r.PerfectProfitPercentage), histStepInPercent),
                        CalcHistogram(realList.Select(r => (double)r.PerfectProfitCorrelation), histStepInPercent),
                        CalcHistogram(randList.Select(r => (double)r.PerfectProfitCorrelation), histStepInPercent)
                    };

                    // In vs. Out --------------------------
                    {
                        var r3Results = StrategiesLists[0].Strategies;
                        var oosResults = StrategiesLists[1].Strategies;

                        for (int i = 0; i < 18; i++)
                            pplInVsOutList.Add(new PointPairList());

                        for (int i = 0, j = 0; i < r3Results.Count && j < oosResults.Count;)
                        {
                            if (r3Results[i].Indx == oosResults[j].Indx)
                            {
                                pplInVsOutList[0].Add(r3Results[i].Net_PnL, oosResults[j].Net_PnL);
                                pplInVsOutList[1].Add(r3Results[i].Drawdown, oosResults[j].Drawdown);
                                pplInVsOutList[2].Add(r3Results[i].Drawdown == 0 ? 0 : r3Results[i].Net_PnL / r3Results[i].Drawdown, oosResults[j].Drawdown == 0 ? 0 : oosResults[j].Net_PnL / oosResults[j].Drawdown);
                                pplInVsOutList[3].Add(r3Results[i].WinPercentage, oosResults[j].WinPercentage);
                                pplInVsOutList[4].Add(r3Results[i].RatioWL, oosResults[j].RatioWL);
                                pplInVsOutList[5].Add(r3Results[i].ProfitFactor, oosResults[j].ProfitFactor);
                                pplInVsOutList[6].Add(r3Results[i].Net_PnL / r3Results[i].TradesCount, oosResults[j].Net_PnL / oosResults[j].TradesCount);
                                pplInVsOutList[7].Add(r3Results[i].TradesCount, oosResults[j].TradesCount);
                                pplInVsOutList[8].Add(r3Results[i].Sharpe, oosResults[j].Sharpe);
                                pplInVsOutList[9].Add(r3Results[i].Sortino, oosResults[j].Sortino);
                                pplInVsOutList[10].Add(r3Results[i].CPCRatio, oosResults[j].CPCRatio);
                                pplInVsOutList[11].Add(r3Results[i].CoerCoef, oosResults[j].CoerCoef);
                                pplInVsOutList[12].Add(r3Results[i].TTest, oosResults[j].TTest);
                                pplInVsOutList[13].Add(r3Results[i].CAGR, oosResults[j].CAGR);
                                pplInVsOutList[14].Add(r3Results[i].KRatio, oosResults[j].KRatio);
                                pplInVsOutList[15].Add(r3Results[i].ExpectancyScore, oosResults[j].ExpectancyScore);
                                pplInVsOutList[16].Add(r3Results[i].PerfectProfitPercentage, oosResults[j].PerfectProfitPercentage);
                                pplInVsOutList[17].Add(r3Results[i].PerfectProfitCorrelation, oosResults[j].PerfectProfitCorrelation);
                                i++; j++;
                            }
                            else if (r3Results[i].Indx > oosResults[j].Indx) j++;
                            else i++;
                        }

                        for (int i = 0; i < 18; i++)
                            if (pplInVsOutList[i].Count > 1)
                                pplInVsOutList.Add(pplInVsOutList[i].LinearRegression(pplInVsOutList[i], 20));
                    }

                    // Rolling metrics
                    for (int i = 0; i < 18; i++)
                        pplInTimeList.Add(new PointPairList());

                    await Task.Run(() =>
                    {

                        float[] trades = res.ReadonlyResults.ToArray();
                        List<float> w_trades = new List<float>();
                        List<float> l_trades = new List<float>();
                        float trade = 0, sum = 0, max_pnl = 0, max_dd = 0;
                        for (int i = 0; i < res.ReadonlyResults.Count; i++)
                        {
                            trade = res.ReadonlyResults[i];
                            if (trade > 0) w_trades.Add(trade);
                            else l_trades.Add(-trade);

                            sum += trade;
                            pplInTimeList[0].Add(i, sum);

                            if (sum > max_pnl) { max_pnl = sum; }
                            else if (max_pnl - sum > max_dd) { max_dd = max_pnl - sum; }
                            pplInTimeList[1].Add(i, max_dd);

                            pplInTimeList[2].Add(i, max_dd == 0 ? 0 : sum / max_dd);
                            pplInTimeList[3].Add(i, w_trades.Count / (i + 1.0));     // winPercentage

                            float lSum = l_trades.Sum();
                            float wSum = w_trades.Sum();
                            float mean_res = (wSum - lSum) / (i + 1);
                            float mean_w_trades = (w_trades.Count == 0 ? 0 : (wSum / w_trades.Count));
                            float mean_l_trades = (l_trades.Count == 0 ? 0 : (lSum / l_trades.Count));
                            float ratioWL = mean_l_trades == 0 ? 0 : mean_w_trades / mean_l_trades;

                            pplInTimeList[4].Add(i, ratioWL);
                            pplInTimeList[5].Add(i, (lSum == 0) ? 0 : wSum / lSum);     // profitFactor
                            pplInTimeList[6].Add(i, mean_res);
                            pplInTimeList[7].Add(i, i + 1);
                            pplInTimeList[10].Add(i, (lSum == 0) ? 0 : (ratioWL * (w_trades.Count / (i + 1.0)) * (wSum / lSum)));    // cpcRatio

                            double cc = SimStrategy.GetCoerCoef(trades, i + 1);
                            pplInTimeList[11].Add(i, cc == double.MinValue ? 0 : cc);

                            double std_res = SimStrategy.Std(trades);
                            pplInTimeList[12].Add(i, std_res == 0 ? 0 : Math.Sqrt(i + 1) * (mean_res / std_res));

                            DateTime s1dt = Security.ToDateTime(res.ReadonlyResEntryDates[0]);
                            DateTime s2dt = Security.ToDateTime(res.ReadonlyResDates[i]);
                            int years = (int)(s2dt.Subtract(s1dt).TotalDays) / 365;
                            if (res.AccountValue != 0 && years != 0 && sum >= 0)
                                pplInTimeList[13].Add(i, (Math.Pow(((sum + res.AccountValue) / res.AccountValue), (1.0 / years)) - 1.0) * 100.00);
                            else pplInTimeList[13].Add(i, 0);
                        }

                        List<float> daily_pnls = new List<float>();
                        List<float> losses = new List<float>();
                        float sq252 = (float)Math.Sqrt(252.0);
                        for (int i = 0; i < res.ReadonlyDaily_pnls.Count; i++)
                        {
                            daily_pnls.Add(res.ReadonlyDaily_pnls[i]);
                            losses.Add(res.ReadonlyDaily_pnls[i] < 0 ? -res.ReadonlyDaily_pnls[i] : 0);

                            float std_pnl = (float)SimStrategy.Std(daily_pnls);                         //todo: rolling calculation
                            float mean_pnl = daily_pnls.Count > 0 ? daily_pnls.Average() : 0;
                            pplInTimeList[8].Add(i, std_pnl != 0 ? sq252 * mean_pnl / std_pnl : 0); //sharpe

                            //sortino (recalc for custom strategies)
                            if (losses.Count > 0)
                            {
                                float v = (float)SimStrategy.Std(losses);                               //todo: rolling calc
                                pplInTimeList[9].Add(i, v != 0 ? sq252 * mean_pnl / v : 0);
                            }
                            else pplInTimeList[9].Add(i, 0);

                            pplInTimeList[14].Add(i, SimStrategy.GetKRatio(daily_pnls));   //kratio     //todo: rolling calc
                        }

                        // expectancyScore, perfectProfitPercentage, perfectProfitCorrelation
                        ExpScorePerfectProfitForStrategy(res, currentCurrency, out List<float> expScore, out List<float> perfectProfitPercentage, out List<float> perfectProfitCorrelation);
                        for (int i = 0; i < expScore.Count; i++)
                            pplInTimeList[15].Add(i, expScore[i]);
                        for (int i = 0; i < perfectProfitPercentage.Count; i++)
                            pplInTimeList[16].Add(i, perfectProfitPercentage[i]);
                        for (int i = 0; i < perfectProfitCorrelation.Count; i++)
                            pplInTimeList[17].Add(i, perfectProfitCorrelation[i]);
                    });

                    await Task.WhenAll(tasksVsRand);
                    foreach (var t in tasksVsRand)
                        pplVsRandList.Add(t.Result);

                    // Show Histogams
                    GraphHistogram gist = new GraphHistogram(pplVsRandList, pplInVsOutList, pplInTimeList);
                    var vm = GraphHistorgamViewModel.Create();
                    vm.Title = "Metrics - " + res.Name;
                    vm.Graph = gist;
                    CreateDocument(nameof(GraphView), vm);
                }
                catch (Exception ex)
                {
                    MessageBoxService.Show("Can't complete operation!\n\n" + ex.Message, "Metrics");
                }
                finally
                {
                    //Cursor = Cursors.Default;
                }
            }
        }

        public void MCEquityBands()
        {
            if (SelectedStratList.IsPortfolio)   //Portfolio mode
            {
                IReadonlySimStrategy res = GetPortfolioStrategyResult(SelectedCollection, SelectedStrategies.FirstOrDefault(), true);
                if (res == null)
                    return;

                ECBands ec = new ECBands(res.ReadonlyResults,
                    decimal.ToInt32(Properties.Settings.Default.MCE_ToPick),
                    decimal.ToInt32(Properties.Settings.Default.MCE_Iterations),
                    decimal.ToDouble(Properties.Settings.Default.MCE_Lower),
                    decimal.ToDouble(Properties.Settings.Default.MCE_Upper));

                //rearrange for multicolor
                PointPairList p1 = new PointPairList();
                PointPairList p2 = new PointPairList();
                {
                    int i = 0;
                    while (i < ec.ppl.Count && i < ec.ppl2.Count && ec.ppl[i].Y == ec.ppl2[i].Y)
                        p1.Add(ec.ppl[i++]);
                    for (int j = ec.ppl.Count - 1; j >= i; j--)
                        p2.Add(ec.ppl[j]);
                    p2.Add(ec.ppl[i - 1]);
                    for (; i < ec.ppl2.Count; i++)
                        p2.Add(ec.ppl2[i]);
                }

                // pick 20,50,100 tests
                PointPairList p3 = new PointPairList();
                int[] toPick = { 20, 50, 100 };

                for (int i = 0; i < toPick.Length; i++)
                {
                    ec = new ECBands(res.ReadonlyResults, toPick[i],
                        decimal.ToInt32(Properties.Settings.Default.MCE_Iterations),
                        decimal.ToDouble(Properties.Settings.Default.MCE_Lower),
                        decimal.ToDouble(Properties.Settings.Default.MCE_Upper));

                    PointPair pp3 = new PointPair
                    {
                        X = toPick[i],
                        Y = ec.ppl.Last().Y,
                        Z = ec.ppl2.Last().Y,
                        Tag = res.Currency
                    };
                    p3.Add(pp3);
                }

                // show graph window
                Graph g = new Graph(3, p1, p2, p3);
                var vm = GraphViewModel.Create();
                vm.Title = "Monte Carlo Equity Bands - Cumulative results";
                vm.Graph = g;
                CreateDocument(nameof(GraphView), vm);
            }
            else
            {  //Single mode
                IReadonlySimStrategy res = SelectedStrategies.FirstOrDefault();
                if (res == null)
                    return;

                if (res.ReadonlyResults.Count < 1)
                {
                    ShowMessage("Selected strategy has no results");
                    return;
                }
                ECBands ec = new ECBands(res.ReadonlyResults,
                    decimal.ToInt32(Properties.Settings.Default.MCE_ToPick),
                    decimal.ToInt32(Properties.Settings.Default.MCE_Iterations),
                    decimal.ToDouble(Properties.Settings.Default.MCE_Lower),
                    decimal.ToDouble(Properties.Settings.Default.MCE_Upper));

                //rearrange for multicolor
                PointPairList p1 = new PointPairList();
                PointPairList p2 = new PointPairList();
                {
                    int i = 0;
                    while (i < ec.ppl.Count && i < ec.ppl2.Count && ec.ppl[i].Y == ec.ppl2[i].Y)
                        p1.Add(ec.ppl[i++]);
                    for (int j = ec.ppl.Count - 1; j >= i; j--)
                        p2.Add(ec.ppl[j]);
                    p2.Add(ec.ppl[i - 1]);
                    for (; i < ec.ppl2.Count; i++)
                        p2.Add(ec.ppl2[i]);
                }

                // pick 20,50,100 tests
                PointPairList p3 = new PointPairList();
                int[] toPick = { 20, 50, 100 };

                for (int i = 0; i < toPick.Length; i++)
                {
                    ec = new ECBands(res.ReadonlyResults, toPick[i],
                        decimal.ToInt32(Properties.Settings.Default.MCE_Iterations),
                        decimal.ToDouble(Properties.Settings.Default.MCE_Lower),
                        decimal.ToDouble(Properties.Settings.Default.MCE_Upper));

                    PointPair pp3 = new PointPair
                    {
                        X = toPick[i],
                        Y = ec.ppl.Last().Y,
                        Z = ec.ppl2.Last().Y,
                        Tag = res.Currency
                    };
                    p3.Add(pp3);
                }

                // show graph window
                Graph g = new Graph(3, p1, p2, p3);
                var vm = GraphViewModel.Create();
                vm.Title = "Monte Carlo Equity Bands - " + res.Name;
                vm.Graph = g;
                CreateDocument(nameof(GraphView), vm);
            }
        }

        public void MCDrawdown()
        {
            //Cursor = Cursors.WaitCursor;

            try
            {
                if (SelectedStratList.IsPortfolio)
                {
                    IReadonlySimStrategy res = GetPortfolioStrategyResult(SelectedCollection, SelectedStrategies.FirstOrDefault(), true);
                    if (res == null)
                        return;
                    MonteDrawdown d = new MonteDrawdown(res.ReadonlyResults,
                        decimal.ToInt32(Properties.Settings.Default.MCDD_Iterations),
                        decimal.ToDouble(Properties.Settings.Default.MCDD_AcctSize));

                    // show graph window
                    Graph g = new Graph(4, d.ppl2, null, d.ppl);
                    var vm = GraphViewModel.Create();
                    vm.Title = "Monte Carlo Drawdown - Cumulative results";
                    vm.Graph = g;
                    CreateDocument(nameof(GraphView), vm);
                }
                else
                {
                    IReadonlySimStrategy res = SelectedStrategies.FirstOrDefault();
                    if (res == null)
                        return;
                    if (res.ReadonlyResults.Count < 1)
                    {
                        ShowMessage("Selected strategy has no results");
                        return;
                    }
                    MonteDrawdown d = new MonteDrawdown(res.ReadonlyResults,
                        decimal.ToInt32(Properties.Settings.Default.MCDD_Iterations),
                        decimal.ToDouble(Properties.Settings.Default.MCDD_AcctSize));

                    // show graph window
                    Graph g = new Graph(4, d.ppl2, null, d.ppl);
                    var vm = GraphViewModel.Create();
                    vm.Title = "Monte Carlo Drawdown - " + res.Name;
                    vm.Graph = g;
                    CreateDocument(nameof(GraphView), vm);
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.Show("Can't complete operation!\n\n" + ex.Message);
            }
            finally
            {
                //Cursor = Cursors.Default;
            }
        }

        public void MCAnalysis()
        {
            IReadonlySimStrategy res = SelectedStratList.IsPortfolio ?
                GetPortfolioStrategyResult(SelectedCollection, SelectedStrategies.FirstOrDefault(), true) : SelectedStrategies.FirstOrDefault();
            if (res == null)
                return;

            if (res.ReadonlyResults.Count < 1)
            {
                ShowMessage("Selected strategy has no results");
                return;
            }

            //Cursor = Cursors.WaitCursor;

            try
            {
                const int iterations = 200;

                Random rnd = new Random(DateTime.Now.Millisecond);
                List<PointPairList> linesOriginal = new List<PointPairList>();
                List<PointPairList> linesResample = new List<PointPairList>();
                List<PointPairList> linesRandomized = new List<PointPairList>();
                List<PointPairList> lines1_10 = new List<PointPairList>();

                // Add original strategy Equity curve 
                linesOriginal.Add(GetEquityCurvePointList(res.ReadonlyResults));

                if (RAWdataStorage.GetRawData(Utils.SymbolsManager.GetSymbolFileNameMult(res.SymbolId, out _), res.SymbolId) is RAWdata rdata)
                {
                    int[] Dates = rdata.Dates;
                    int[] Times = rdata.Times;
                    float[] Closes = rdata.Closes;

                    // calculate this with portfilio result settings (for portfolio mode) or take data from original (for current sim results)
                    float[] PosSizes = null, Slippages = null, Commissions = null;
                    CalcPSforStrategy(res, ref PosSizes, ref Slippages, ref Commissions);

                    if (PosSizes != null && Slippages != null && Commissions != null)
                    {
                        int startIndex = 0;
                        int stopIndex = Dates.Length - 1;
                        int sOffset = 0;
                        while (startIndex < Dates.Length && SimStrategy.CompareDateTimes(Dates[startIndex], Times[startIndex], res.SignalStartDate, res.SignalStartTime) < 0)
                            startIndex++;
                        while (startIndex < Dates.Length && SimStrategy.CompareDateTimes(Dates[startIndex], Times[startIndex], res.ReadonlyResEntryDates[0], res.ReadonlyResEntryTimes[0]) < 0)
                        { startIndex++; sOffset++; }
                        while (stopIndex >= startIndex && SimStrategy.CompareDateTimes(Dates[stopIndex], Times[stopIndex], res.ReadonlyResDates.Last(), res.ResTime(res.ReadonlyResults.Count - 1)) > 0)
                            stopIndex--;
                        int inDataLen = stopIndex - startIndex + 1;

                        // -------------------------------------------
                        //  Run Only 1 through 10
                        for (int i = 1; i <= 10; i++)
                        {
                            PointPairList EquityCurve = new PointPairList { { 0, res.ReadonlyResults[0] } };
                            int z = 0;
                            double running_pnl = 0, pnl = 0;
                            for (int j = startIndex, k = sOffset; j <= stopIndex && k < res.SignalsCount && (z + 1) < res.ReadonlyResults.Count; j++, k++)
                            {
                                if (res.EntrySignalsValues[k])
                                {
                                    if (i + j < Closes.Length)
                                    {
                                        float pSize = GenerateSignalsToTrade.FloorAndTrimPosSize(PosSizes[j], res.PosSizeMode, res.SymbolType, res.SL_ON, (float)res.SL_mult);    // floor and trim according to modes
                                        if (tradeMode.ToString().StartsWith("L"))
                                            pnl = (Closes[j + i] - Closes[j]) * res.ContractMultiplier * pSize - Slippages[j] - Commissions[j];
                                        else
                                            pnl = (Closes[j] - Closes[j + i]) * res.ContractMultiplier * pSize - Slippages[j] - Commissions[j];
                                        
                                        
                                        
                                        //todo: currency conversion
                                        //pnl *= Closes[j + i] / ClosesOriginal[j + i];
                                    }
                                    running_pnl += pnl;
                                    if (SimStrategy.CompareDateTimes(Dates[j], Times[j], res.ReadonlyResDates[z], res.ResTime(z)) >= 0)
                                        EquityCurve.Add(++z, running_pnl);
                                }
                            }
                            lines1_10.Add(EquityCurve);
                        }

                        // -----------------------------------------
                        // Run Simulations
                        for (int i = 0; i < iterations; i++)
                        {
                            PointPairList EquityCurve = new PointPairList { { 0, res.ReadonlyResults[0] } };
                            int z = 0;
                            double running_pnl = 0, pnl = 0;
                            int exit = 0;
                            for (int j = startIndex, k = sOffset; j <= stopIndex && k < res.EntrySignalsValues.Count && (z + 1) < res.ReadonlyResults.Count; j++, k++)
                            {
                                if (res.EntrySignalsValues[k])
                                {
                                    exit = (int)Math.Round(Uniform(rnd, 1, 10));
                                    if (exit + j < Closes.Length)
                                    {
                                        float pSize = GenerateSignalsToTrade.FloorAndTrimPosSize(PosSizes[j], res.PosSizeMode, res.SymbolType, res.SL_ON, (float)res.SL_mult);    // floor and trim according to modes
                                        if (tradeMode.ToString().StartsWith("L"))
                                            pnl = (Closes[j + exit] - Closes[j]) * res.ContractMultiplier * pSize - Slippages[j] - Commissions[j];
                                        else
                                            pnl = (Closes[j] - Closes[j + exit]) * res.ContractMultiplier * pSize - Slippages[j] - Commissions[j];

                                        //todo: currency conversion
                                        //pnl *= Closes[j + exit] / ClosesOriginal[j + exit];
                                    }
                                    running_pnl += pnl;
                                    if (SimStrategy.CompareDateTimes(Dates[j], Times[j], res.ReadonlyResDates[z], res.ResTime(z)) >= 0)
                                        EquityCurve.Add(++z, running_pnl);
                                }
                            }
                            linesRandomized.Add(EquityCurve);
                        }
                    }
                }

                // ------------------------------------------
                // add shuffled results
                double bestDrawdown = double.MaxValue;
                double worstDrawdowm = 0;
                double avrDrawdown = 0;
                for (int i = 0; i < iterations; i++)
                {
                    List<float> values = res.ReadonlyResults.OrderBy(x => (rnd.Next())).ToList();
                    double running_pnl = 0;
                    double max_pnl = 0, drawdown = 0;
                    PointPairList EquityCurve = new PointPairList();
                    for (int j = 0; j < values.Count; j++)
                    {
                        running_pnl += values[j];
                        EquityCurve.Add(j, running_pnl);
                        if (running_pnl > max_pnl)
                            max_pnl = running_pnl;
                        else if (max_pnl - running_pnl > drawdown)
                            drawdown = max_pnl - running_pnl;
                    }
                    bestDrawdown = Math.Min(bestDrawdown, drawdown);
                    worstDrawdowm = Math.Max(worstDrawdowm, drawdown);
                    avrDrawdown += drawdown;
                    linesOriginal.Add(EquityCurve);
                }
                avrDrawdown /= iterations;

                // ------------------------------------------
                // add resample results
                double bestDrawdown2 = double.MaxValue;
                double worstDrawdowm2 = 0;
                double avrDrawdown2 = 0;
                for (int i = 0; i < iterations; i++)
                {
                    List<float> values = new List<float>();
                    for (int j = 0; j < res.ReadonlyResults.Count; j++)
                        values.Add(res.ReadonlyResults[rnd.Next(res.ReadonlyResults.Count)]);

                    double running_pnl = 0;
                    double max_pnl = 0, drawdown = 0;
                    PointPairList EquityCurve = new PointPairList();
                    for (int j = 0; j < res.ReadonlyResults.Count; j++)
                    {
                        running_pnl += values[j];
                        EquityCurve.Add(j, running_pnl);
                        if (running_pnl > max_pnl)
                            max_pnl = running_pnl;
                        else if (max_pnl - running_pnl > drawdown)
                            drawdown = max_pnl - running_pnl;
                    }
                    bestDrawdown2 = Math.Min(bestDrawdown2, drawdown);
                    worstDrawdowm2 = Math.Max(worstDrawdowm2, drawdown);
                    avrDrawdown2 += drawdown;
                    linesResample.Add(EquityCurve);
                }
                avrDrawdown2 /= iterations;

                // consecutive metrics
                List<PointPairList> consecOriginal = MCconsecutive(linesOriginal);
                List<PointPairList> consecResample = MCconsecutive(linesResample);
                List<PointPairList> consecRandomized = MCconsecutive(linesRandomized);
                List<PointPairList> consec1_10 = MCconsecutive(lines1_10);

                StringBuilder s = new StringBuilder();
                s.AppendLine("Original");
                s.AppendFormat("Best Drawdown:    {0} {1:F2}{2}", currentCurrency, bestDrawdown, Environment.NewLine);
                s.AppendFormat("Worst Drawdown:   {0} {1:F2}{2}", currentCurrency, worstDrawdowm, Environment.NewLine);
                s.AppendFormat("Average Drawdown: {0} {1:F2}", currentCurrency, avrDrawdown);

                StringBuilder s2 = new StringBuilder();
                s2.AppendLine("Resample");
                s2.AppendFormat("Best Drawdown:    {0} {1:F2}{2}", currentCurrency, bestDrawdown2, Environment.NewLine);
                s2.AppendFormat("Worst Drawdown:   {0} {1:F2}{2}", currentCurrency, worstDrawdowm2, Environment.NewLine);
                s2.AppendFormat("Average Drawdown: {0} {1:F2}", currentCurrency, avrDrawdown2);

                //-------
                GraphMC grMC = new GraphMC(linesOriginal, linesResample, linesRandomized, lines1_10, s.ToString(), s2.ToString(),
                    consecOriginal, consecResample, consecRandomized, consec1_10);
                var vm = GraphViewModel.Create();
                vm.Title = "MC Analysis - " + res.Name;
                vm.Graph = grMC;
                CreateDocument(nameof(GraphView), vm);
            }
            catch (Exception ex)
            {
                MessageBoxService.Show("Can't complete operation!\n\n" + ex.Message);
            }
            finally
            {
                //Cursor = Cursors.Default;
            }
        }

        public void Variance()
        {
            //Cursor = Cursors.WaitCursor;

            try
            {
                IReadonlySimStrategy res = SelectedStratList.IsPortfolio ?
                    GetPortfolioStrategyResult(SelectedCollection, SelectedStrategies.FirstOrDefault(), true) : SelectedStrategies.FirstOrDefault();
                if (res == null)
                    return;

                // ----------------------------------------
                //  Configurations
                //
                int iterations = Decimal.ToInt32(Properties.Settings.Default.VT_Iterations);
                int no_of_trades = Decimal.ToInt32(Properties.Settings.Default.VT_Trades);
                double win_var = Decimal.ToDouble(Properties.Settings.Default.VT_Variations);   //winning percentage variation
                double ruin = Decimal.ToDouble(Properties.Settings.Default.VT_Ruin);            //ruin amount
                double win_rate = res.WinPercentage;
                double win_size = res.Mean_w_trades;
                double loss_size = res.Mean_l_trades;
                double win_amt_var = res.Std_w_trades;
                double loss_amt_var = res.Std_l_trades;

                int ruin_count = 0;
                int win_count = 0;

                List<PointPairList> lines = new List<PointPairList>(iterations);

                {   // Add original strategy Equity curve
                    PointPairList ppl = new PointPairList();
                    double sum = 0;
                    for (int i = 0; i < res.ReadonlyResults.Count && i < no_of_trades; i++)
                    {
                        sum += res.ReadonlyResults[i];
                        ppl.Add(i, sum);
                    }
                    lines.Add(ppl);
                }
                // ----------------------------------------
                //  Run Simulation
                //
                double dd_total = 0, pnl_total = 0, dd_sqr = 0, pnl_sqr = 0;
                Random rnd = new Random(DateTime.Now.Millisecond);
                for (int i = 0; i < iterations; i++)
                {
                    List<double> EquityCurve = new List<double>();
                    double running_pnl = 0;
                    int stopped = 0;
                    double max = 0, drawdown = 0;

                    double win_perc = Uniform(rnd, win_rate - win_var, win_rate + win_var);

                    for (int j = 0; j < no_of_trades; j++)
                    {
                        int trade = rnd.NextDouble() < win_perc ? 1 : 0;
                        if (trade == 1)
                            running_pnl += GetWin(rnd, rnd.NextDouble(), win_size, win_amt_var);
                        else
                            running_pnl -= GetLoss(rnd, rnd.NextDouble(), loss_size, loss_amt_var);

                        if (running_pnl > max)
                            max = running_pnl;
                        if (max - running_pnl > drawdown)
                            drawdown = max - running_pnl;

                        if (running_pnl <= -ruin && stopped == 0)
                        {
                            ruin_count += 1;
                            stopped = 1;
                            continue;
                        }
                        EquityCurve.Add(running_pnl);
                    }
                    dd_total += drawdown;
                    dd_sqr += drawdown * drawdown;
                    pnl_total += EquityCurve.Last();
                    pnl_sqr += EquityCurve.Last() * EquityCurve.Last();

                    if (EquityCurve.Last() > 0)
                        win_count += 1;

                    // Plot
                    PointPairList ppl = new PointPairList();
                    for (int j = 0; j < EquityCurve.Count; j++)
                        ppl.Add(j, EquityCurve[j]);
                    lines.Add(ppl);
                }

                // --------------------------------------
                //  brief calculations added Oct.20.2016
                //
                double pnl_mean = pnl_total / iterations;
                double pnl_std = Math.Sqrt(pnl_sqr / iterations - pnl_mean * pnl_mean);

                double dd_mean = dd_total / iterations;
                double dd_std = Math.Sqrt(dd_sqr / iterations - dd_mean * dd_mean);
                double dd_lower_bound = dd_mean - 2 * dd_std;
                if (dd_lower_bound < 0)
                    dd_lower_bound = 0;

                // -------------------------------------
                //  Display Results
                //
                string resStr1 = "";
                string resStr2 = "";
                resStr1 += String.Format("Make Money Count:           {0}\n", win_count);
                resStr1 += String.Format("Make Money:                 {0:F2} %\n", win_count * 100.0 / iterations);
                resStr2 += String.Format("Ruin Count: {0}\n", ruin_count);
                resStr2 += String.Format("Ruin Odds:  {0:F2} %\n", ruin_count * 100.0 / iterations);

                // -------------------------------------------
                //   added Oct.20.2016
                //
                string resStr3 = "";
                resStr1 += String.Format("Average PNLDD:              {0:F2}\n", pnl_mean / dd_mean);
                resStr1 += String.Format("Average Profit:         {0} {1:F2}\n", currentCurrency, pnl_mean);
                resStr1 += String.Format("Average Drawdown:       {0} {1:F2}\n", currentCurrency, dd_mean);
                resStr1 += String.Format("95% Confident Profit:   {0} {1:F2} - {2:F2}\n", currentCurrency, pnl_mean - 2 * pnl_std, pnl_mean + 2 * pnl_std);
                resStr1 += String.Format("95% Confident Drawdown: {0} {1:F2} - {2:F2}", currentCurrency, dd_lower_bound, dd_mean + 2 * dd_mean);

                // show graph window
                Graph g = new Graph(lines, resStr1, resStr2, resStr3);
                var vm = GraphViewModel.Create();
                vm.Title = $"Variance Testing - {res.Name}";
                vm.Graph = g;
                CreateDocument(nameof(GraphView), vm);
            }
            catch (Exception ex)
            {
                MessageBoxService.Show($"Can't complete operation!\n\n{ex.Message}");
            }
            finally
            {
                //Cursor = Cursors.Default;
            }
        }

        public async Task Robustness()
        {
            //Cursor = Cursors.WaitCursor;

            try
            {
                List<PointPairList> ppll = new List<PointPairList>();
                List<string> labels = new List<string>();
                List<string> newsEvents = new List<string>() { "ALL" };
                IReadonlySimStrategy res = SelectedStratList.IsPortfolio ?
                    GetPortfolioStrategyResult(SelectedCollection, SelectedStrategies.FirstOrDefault(), true) : SelectedStrategies.FirstOrDefault();
                if (res == null)
                    return;

                await Task.Run(() =>
                {
                    // prepare data
                    int[] Dates = null, Times = null;
                    float[] OpensOriginal = null, HighsOriginal = null, LowsOriginal = null, ClosesOriginal = null;
                    float[] OpensConverted = null, HighsConverted = null, LowsConverted = null, ClosesConverted = null;
                    float[] PosSizes = null, Slippages = null, Commissions = null;

                    if (!SelectedStratList.IsPortfolio && RAWdataStorage.GetRawData(Utils.SymbolsManager.GetSymbolFileNameMult(res.SymbolId, out _), res.SymbolId) is RAWdata rdata)  //in single mode only
                    {
                        Dates = rdata.Dates;
                        Times = rdata.Times;
                        OpensOriginal = rdata.Opens;
                        HighsOriginal = rdata.Highs;
                        LowsOriginal = rdata.Lows;
                        ClosesOriginal = rdata.Closes;

                        // currency conversion
                        int r = CurrencyConvertor.GetConversationRulesForSymbol(res.SymbolId, currentCurrency, out string[] convRules, Utils.SymbolsManager);
                        //if (r != 0)
                        //    MessageBoxService.Show(res.Symbol + " to " + currentCurrency + ". No available currency convertor found! Error code: " + r);

                        OpensConverted = CurrencyConvertor.ConvertCurrencyInList(convRules, Dates, OpensOriginal, true);
                        HighsConverted = CurrencyConvertor.ConvertCurrencyInList(convRules, Dates, HighsOriginal, true);
                        LowsConverted = CurrencyConvertor.ConvertCurrencyInList(convRules, Dates, LowsOriginal, true);
                        ClosesConverted = CurrencyConvertor.ConvertCurrencyInList(convRules, Dates, ClosesOriginal, true);

                        CalcPSforStrategy(res, ref PosSizes, ref Slippages, ref Commissions);
                    }

                    bool plotWithSum(List<float> lst, string lb)
                    {
                        if (lst.Count == 0) return false;

                        PointPairList ppl = new PointPairList();
                        float sum = 0;
                        for (int i = 0; i < lst.Count; i++)
                        {
                            sum += lst[i];
                            ppl.Add(i, sum);
                        }
                        labels.Add(lb);
                        ppll.Add(ppl);
                        return true;
                    }

                    bool plotWithoutSum(List<float> lst, string lb)
                    {
                        if (lst.Count == 0) return false;

                        PointPairList ppl = new PointPairList();
                        for (int i = 0; i < lst.Count; i++)
                            ppl.Add(i, lst[i]);
                        labels.Add(lb);
                        ppll.Add(ppl);
                        return true;
                    }

                    #region DelayedEntryExits
                    if (!SelectedStratList.IsPortfolio && ClosesOriginal != null)  //in single mode only
                    {
                        int startDateIndex = 0;
                        while (startDateIndex < Dates.Length && SimStrategy.CompareDateTimes(Dates[startDateIndex], Times[startDateIndex], res.ReadonlyResEntryDates[0], res.ReadonlyResEntryTimes[0]) < 0)
                            startDateIndex++;
                        for (int j = 0; j < 4; j++)
                            for (int k = j; k < 4; k++)
                            {
                                PointPairList ppl_Returns = new PointPairList();
                                float my_sum = 0, entry = 0, in_trade_ = 0;
                                int ind = 0, entryIdx = 0;

                                if (j == 0 && k == 0)
                                {   //original equity curve
                                    ppl_Returns = GetEquityCurvePointList(res.ReadonlyResults);
                                }
                                else
                                    for (int i = startDateIndex; i < ClosesOriginal.Length && ind < res.ReadonlyResults.Count; i++)
                                    {
                                        if (in_trade_ == 0 && (Dates[i] > res.ReadonlyResEntryDates[ind] || (Dates[i] == res.ReadonlyResEntryDates[ind] && Times[i] >= res.ReadonlyResEntryTimes[ind])))
                                        {
                                            in_trade_ = 1;
                                            if (i + j < ClosesOriginal.Length)
                                            {
                                                entry = ClosesOriginal[i + j];
                                                entryIdx = i + j;
                                            }
                                        }
                                        else if (in_trade_ == 1 && (Dates[i] > res.ReadonlyResDates[ind] || (Dates[i] == res.ReadonlyResDates[ind] && Times[i] >= res.ResTime(ind))))
                                        {
                                            in_trade_ = 0;
                                            if (i + k < ClosesOriginal.Length)
                                            {
                                                // floor and trim according to modes
                                                float pSize = GenerateSignalsToTrade.FloorAndTrimPosSize(PosSizes[entryIdx], res.PosSizeMode, res.SymbolType, res.SL_ON, (float)res.SL_mult);
                                                if (tradeMode.ToString().StartsWith("L"))
                                                    my_sum += ((ClosesOriginal[i + k] - entry) * res.ContractMultiplier * pSize - Slippages[entryIdx] - Commissions[entryIdx]) * ClosesConverted[i + k] / ClosesOriginal[i + k];
                                                else
                                                    my_sum += ((entry - ClosesOriginal[i + k]) * res.ContractMultiplier * pSize - Slippages[entryIdx] - Commissions[entryIdx]) * ClosesConverted[i + k] / ClosesOriginal[i + k];

                                                ppl_Returns.Add(ind, my_sum);
                                                i--;    //test next entry data by the same date
                                            }
                                            ind++;
                                        }
                                    }

                                if (j == 0 && k == 0)
                                    labels.Add("1Original");
                                else
                                    labels.Add(string.Format("1Entry {0} Exit {1}", j, k));
                                ppll.Add(ppl_Returns);
                            }
                    }
                    #endregion

                    #region Meta Systems Tests
                    if (res.ChildStrategiesCount == 0)  // skip basket
                    {
                        // Two Trades
                        List<float> last_win = new List<float>();
                        List<float> last_loss = new List<float>();
                        List<float> one_down_one_up = new List<float>();
                        List<float> one_up_one_down = new List<float>();
                        List<float> two_wins = new List<float>();
                        List<float> two_losses = new List<float>();

                        // Three Trades
                        List<float> two_up_one_dn = new List<float>();
                        List<float> two_dn_one_up = new List<float>();
                        List<float> thrup = new List<float>();
                        List<float> thrdn = new List<float>();
                        List<float> one_up_two_dn = new List<float>();
                        List<float> one_dn_two_up = new List<float>();
                        List<float> one_up_one_dn_one_up = new List<float>();
                        List<float> one_dn_one_up_one_dn = new List<float>();

                        // Moving Average of Equity
                        Queue<float> sma5 = new Queue<float>(5);
                        Queue<float> sma10 = new Queue<float>(10);
                        Queue<float> sma20 = new Queue<float>(20);
                        List<float> below_sma5 = new List<float>();
                        List<float> below_sma10 = new List<float>();
                        List<float> below_sma20 = new List<float>();
                        List<float> above_sma5 = new List<float>();
                        List<float> above_sma10 = new List<float>();
                        List<float> above_sma20 = new List<float>();
                        List<float> equity_curve = new List<float>();

                        float my_sum2 = 0;

                        for (int i = 0; i < res.ReadonlyResults.Count; i++)
                        {
                            float r = res.ReadonlyResults[i];
                            if (i < 2)
                            {
                                last_win.Add(0);
                                last_loss.Add(0);
                                one_down_one_up.Add(0);
                                one_up_one_down.Add(0);
                                two_wins.Add(0);
                                two_losses.Add(0);
                            }
                            else
                            {
                                if (res.ReadonlyResults[i - 1] > 0)
                                {
                                    last_win.Add(r);
                                    last_loss.Add(0);
                                }
                                else
                                {
                                    last_win.Add(0);
                                    last_loss.Add(r);
                                }
                                if (res.ReadonlyResults[i - 1] > 0 && res.ReadonlyResults[i - 2] <= 0)
                                    one_down_one_up.Add(r);
                                else
                                    one_down_one_up.Add(0);
                                if (res.ReadonlyResults[i - 1] < 0 && res.ReadonlyResults[i - 2] >= 0)
                                    one_up_one_down.Add(r);
                                else
                                    one_up_one_down.Add(0);
                                if (res.ReadonlyResults[i - 1] > 0 && res.ReadonlyResults[i - 2] > 0)
                                    two_wins.Add(r);
                                else
                                    two_wins.Add(0);
                                if (res.ReadonlyResults[i - 1] <= 0 && res.ReadonlyResults[i - 2] <= 0)
                                    two_losses.Add(r);
                                else
                                    two_losses.Add(0);
                            }
                            if (i < 3)
                            {
                                two_up_one_dn.Add(0);
                                two_dn_one_up.Add(0);
                                thrup.Add(0);
                                thrdn.Add(0);
                                one_up_two_dn.Add(0);
                                one_dn_two_up.Add(0);
                                one_up_one_dn_one_up.Add(0);
                                one_dn_one_up_one_dn.Add(0);
                            }
                            else
                            {
                                if (res.ReadonlyResults[i - 3] > 0 && res.ReadonlyResults[i - 2] > 0 && res.ReadonlyResults[i - 1] <= 0) two_up_one_dn.Add(r);
                                else two_up_one_dn.Add(0);
                                if (res.ReadonlyResults[i - 3] <= 0 && res.ReadonlyResults[i - 2] <= 0 && res.ReadonlyResults[i - 1] > 0) two_dn_one_up.Add(r);
                                else two_dn_one_up.Add(0);
                                if (res.ReadonlyResults[i - 3] > 0 && res.ReadonlyResults[i - 2] > 0 && res.ReadonlyResults[i - 1] > 0) thrup.Add(r);
                                else thrup.Add(0);
                                if (res.ReadonlyResults[i - 3] <= 0 && res.ReadonlyResults[i - 2] <= 0 && res.ReadonlyResults[i - 1] <= 0) thrdn.Add(r);
                                else thrdn.Add(0);
                                if (res.ReadonlyResults[i - 3] > 0 && res.ReadonlyResults[i - 2] <= 0 && res.ReadonlyResults[i - 1] <= 0) one_up_two_dn.Add(r);
                                else one_up_two_dn.Add(0);
                                if (res.ReadonlyResults[i - 3] <= 0 && res.ReadonlyResults[i - 2] > 0 && res.ReadonlyResults[i - 1] > 0) one_dn_two_up.Add(r);
                                else one_dn_two_up.Add(0);
                                if (res.ReadonlyResults[i - 3] > 0 && res.ReadonlyResults[i - 2] <= 0 && res.ReadonlyResults[i - 1] > 0) one_up_one_dn_one_up.Add(r);
                                else one_up_one_dn_one_up.Add(0);
                                if (res.ReadonlyResults[i - 3] <= 0 && res.ReadonlyResults[i - 2] > 0 && res.ReadonlyResults[i - 1] <= 0) one_dn_one_up_one_dn.Add(r);
                                else one_dn_one_up_one_dn.Add(0);
                            }
                            if (i < 5)
                            {
                                below_sma5.Add(0);
                                above_sma5.Add(0);
                            }
                            if (i < 10)
                            {
                                below_sma10.Add(0);
                                above_sma10.Add(0);
                            }
                            if (i < 20)
                            {
                                below_sma20.Add(0);
                                above_sma20.Add(0);
                            }
                            if (i >= 5)
                            {
                                if (equity_curve.Last() > sma5.Sum() / sma5.Count)
                                {
                                    above_sma5.Add(r);
                                    below_sma5.Add(0);
                                }
                                else
                                {
                                    above_sma5.Add(0);
                                    below_sma5.Add(r);
                                }
                            }
                            if (i >= 10)
                            {
                                if (equity_curve.Last() > sma10.Sum() / sma10.Count)
                                {
                                    above_sma10.Add(r);
                                    below_sma10.Add(0);
                                }
                                else
                                {
                                    above_sma10.Add(0);
                                    below_sma10.Add(r);
                                }
                            }
                            if (i >= 20)
                            {
                                if (equity_curve.Last() > sma20.Sum() / sma20.Count)
                                {
                                    above_sma20.Add(r);
                                    below_sma20.Add(0);
                                }
                                else
                                {
                                    above_sma20.Add(0);
                                    below_sma20.Add(r);
                                }
                            }
                            my_sum2 += r;
                            equity_curve.Add(my_sum2);
                            if (sma5.Count == 5) sma5.Dequeue();
                            sma5.Enqueue(my_sum2);
                            if (sma10.Count == 10) sma10.Dequeue();
                            sma10.Enqueue(my_sum2);
                            if (sma20.Count == 20) sma20.Dequeue();
                            sma20.Enqueue(my_sum2);
                        }

                        //plot original
                        plotWithoutSum(equity_curve, "20Original");

                        // Plot Two's
                        plotWithSum(last_win, "21LastTrdWin");
                        plotWithSum(last_loss, "21LastTrdLoss");
                        plotWithSum(one_down_one_up, "211Dn1Up");
                        plotWithSum(one_up_one_down, "211Up1Dn");
                        plotWithSum(two_wins, "21TwoWins");
                        plotWithSum(two_losses, "21TwoLosses");

                        // Plot Three's
                        plotWithSum(two_up_one_dn, "222Up1Dn");
                        plotWithSum(two_dn_one_up, "222Dn1Up");
                        plotWithSum(thrup, "22ThreeWins");
                        plotWithSum(thrdn, "22ThreeLosses");
                        plotWithSum(one_up_two_dn, "221Up2Dn");
                        plotWithSum(one_dn_two_up, "221Dn2Up");
                        plotWithSum(one_up_one_dn_one_up, "221Up1Dn1Up");
                        plotWithSum(one_dn_one_up_one_dn, "221Dn1Up1Dn");

                        // Plot Equity Vs. MA of Equity
                        plotWithSum(below_sma5, "23Below5SMA");
                        plotWithSum(above_sma5, "23Above5SMA");
                        plotWithSum(below_sma10, "23Below10SMA");
                        plotWithSum(above_sma10, "23Above10SMA");
                        plotWithSum(below_sma20, "23Below20SMA");
                        plotWithSum(above_sma20, "23Above20SMA");
                    }
                    #endregion

                    #region SlippageTest
                    if (!SelectedStratList.IsPortfolio && Dates != null && Times != null && ClosesOriginal != null && Slippages != null)  //in single mode only
                    {
                        int startDateIndex = 0;
                        while (startDateIndex < Dates.Length && SimStrategy.CompareDateTimes(Dates[startDateIndex], Times[startDateIndex], res.ReadonlyResEntryDates[0], res.ReadonlyResEntryTimes[0]) < 0)
                            startDateIndex++;

                        float[] slippageAr = { 0, 0.5f, 1, 1.5f, 2, 3, 5 };
                        foreach (float s in slippageAr)
                        {
                            PointPairList Ret = new PointPairList { { 0, 0 } };
                            for (int i = startDateIndex, ind = 0; i < Dates.Length && ind < res.ReadonlyResults.Count; i++)
                            {
                                if (SimStrategy.CompareDateTimes(Dates[i], Times[i], res.ReadonlyResDates[ind], res.ResTime(ind)) >= 0)
                                {
                                    Ret.Add(Ret.Count - 1, Ret.Last().Y + res.ReadonlyResults[ind] - (s - 1) * Slippages[i] * ClosesConverted[i] / ClosesOriginal[i]);   // todo: previous bar conversion for exit on next open??
                                    ind++;
                                    i--;
                                }
                            }
                            labels.Add("3" + (s == 1 ? "Original" : s.ToString()));
                            ppll.Add(Ret);
                        }
                    }
                    #endregion

                    #region Liquidity  Test
                    if (!SelectedStratList.IsPortfolio && ClosesOriginal != null)  //in single mode only
                    {
                        List<float> OHLC = new List<float>(ClosesOriginal.Length);
                        List<float> AvgOHLC = new List<float>(ClosesOriginal.Length);
                        List<float> AvgOHLC2 = new List<float>(ClosesOriginal.Length);
                        List<float> Mid = new List<float>(ClosesOriginal.Length);
                        List<float> NextMid = new List<float>(ClosesOriginal.Length);
                        List<float> NextMid2 = new List<float>(ClosesOriginal.Length);
                        List<float> NextCls = new List<float>(ClosesOriginal.Length);
                        List<float> NextCls2 = new List<float>(ClosesOriginal.Length);
                        for (int i = 0; i < ClosesConverted.Length; i++)
                        {
                            //  Average OHLC
                            OHLC.Add((OpensConverted[i] + HighsConverted[i] + LowsConverted[i] + ClosesConverted[i]) / 4);
                            //  Average Next Mid
                            Mid.Add(LowsConverted[i] + ((HighsConverted[i] + LowsConverted[i]) / 2));
                            //  Average Next Closes
                            NextCls.Add(i < ClosesConverted.Length - 1 ? (ClosesConverted[i] + ClosesConverted[i + 1]) / 2 : ClosesConverted.Last());
                            NextCls2.Add(i < ClosesConverted.Length - 2 ? (ClosesConverted[i] + ClosesConverted[i + 1] + ClosesConverted[i + 2]) / 3 : ClosesConverted[i]);
                        }
                        for (int i = 0; i < ClosesConverted.Length; i++)
                        {
                            AvgOHLC.Add(i < ClosesConverted.Length - 1 ? (ClosesConverted[i] + OHLC[i + 1]) / 2 : ClosesConverted.Last());
                            AvgOHLC2.Add(i < ClosesConverted.Length - 2 ? (ClosesConverted[i] + OHLC[i + 1] + OHLC[i + 2]) / 3 : AvgOHLC2[i - 1]);

                            NextMid.Add(i < ClosesConverted.Length - 1 ? (ClosesConverted[i] + Mid[i + 1]) / 2 : Mid.Last());
                            NextMid2.Add(i < ClosesConverted.Length - 2 ? (ClosesConverted[i] + Mid[i + 1] + Mid[i + 2]) / 3 : NextMid2[i - 1]);
                        }

                        List<float> NextMidCurve = new List<float>();
                        List<float> NextMid2Curve = new List<float>();
                        List<float> NextOHLCCurve = new List<float>();
                        List<float> NextOHLC2Curve = new List<float>();
                        List<float> NextClsCurve = new List<float>();
                        List<float> NextCls2Curve = new List<float>();
                        List<float> Original = new List<float>();
                        //float orig_sum = 0;

                        float next_mid_sum = 0;
                        float next_mid2_sum = 0;
                        float next_ohlc_sum = 0;
                        float next_ohlc2_sum = 0;
                        float next_cls_sum = 0;
                        float next_cls2_sum = 0;

                        float orig_entry = 0;
                        float next_mid_entry = 0;
                        float next_mid2_entry = 0;
                        float next_ohlc_entry = 0;
                        float next_ohlc2_entry = 0;
                        float next_cls_entry = 0;
                        float next_cls2_entry = 0;

                        int entryIdx = 0;
                        int in_trade = 0;
                        int inde = 0;
                        for (int i = 0; i < ClosesOriginal.Length - 2 && inde < res.ReadonlyResults.Count; i++)
                        {
                            if (in_trade == 0 && Dates[i] >= res.ReadonlyResEntryDates[inde])
                            {
                                in_trade = 1;
                                orig_entry = ClosesConverted[i];
                                next_mid_entry = NextMid[i];
                                next_mid2_entry = NextMid2[i];
                                next_ohlc_entry = AvgOHLC[i];
                                next_ohlc2_entry = AvgOHLC2[i];
                                next_cls_entry = NextCls[i];
                                next_cls2_entry = NextCls2[i];
                                entryIdx = i;
                            }
                            else if (in_trade == 1 && Dates[i] >= res.ReadonlyResDates[inde])
                            {
                                in_trade = 0;
                                float pSize = GenerateSignalsToTrade.FloorAndTrimPosSize(PosSizes[entryIdx], res.PosSizeMode, res.SymbolType, res.SL_ON, (float)res.SL_mult);    // floor and trim according to modes
                                float conv = ClosesConverted[i] / ClosesOriginal[i];

                                if (tradeMode.ToString().StartsWith("L"))
                                {
                                    // orig_sum += ((ClosesOriginal[i] - orig_entry) * contract_multiplier * pSize - slippage - commision) * conv;
                                    next_mid_sum += ((NextMid[i] - next_mid_entry) * res.ContractMultiplier * pSize - Slippages[entryIdx] - Commissions[entryIdx]) * conv;
                                    next_mid2_sum += ((NextMid2[i] - next_mid2_entry) * res.ContractMultiplier * pSize - Slippages[entryIdx] - Commissions[entryIdx]) * conv;
                                    next_ohlc_sum += ((AvgOHLC[i] - next_ohlc_entry) * res.ContractMultiplier * pSize - Slippages[entryIdx] - Commissions[entryIdx]) * conv;
                                    next_ohlc2_sum += ((AvgOHLC2[i] - next_ohlc2_entry) * res.ContractMultiplier * pSize - Slippages[entryIdx] - Commissions[entryIdx]) * conv;
                                    next_cls_sum += ((NextCls[i] - next_cls_entry) * res.ContractMultiplier * pSize - Slippages[entryIdx] - Commissions[entryIdx]) * conv;
                                    next_cls2_sum += ((NextCls2[i] - next_cls2_entry) * res.ContractMultiplier * pSize - Slippages[entryIdx] - Commissions[entryIdx]) * conv;
                                }
                                else
                                {
                                    //  orig_sum += ((orig_entry - ClosesOriginal[i]) * contract_multiplier * pSize - slippage - commision) * conv
                                    next_mid_sum += ((next_mid_entry - NextMid[i]) * res.ContractMultiplier * pSize - Slippages[entryIdx] - Commissions[entryIdx]) * conv;
                                    next_mid2_sum += ((next_mid2_entry - NextMid2[i]) * res.ContractMultiplier * pSize - Slippages[entryIdx] - Commissions[entryIdx]) * conv;
                                    next_ohlc_sum += ((next_ohlc_entry - AvgOHLC[i]) * res.ContractMultiplier * pSize - Slippages[entryIdx] - Commissions[entryIdx]) * conv;
                                    next_ohlc2_sum += ((next_ohlc2_entry - AvgOHLC2[i]) * res.ContractMultiplier * pSize - Slippages[entryIdx] - Commissions[entryIdx]) * conv;
                                    next_cls_sum += ((next_cls_entry - NextCls[i]) * res.ContractMultiplier * pSize - Slippages[entryIdx] - Commissions[entryIdx]) * conv;
                                    next_cls2_sum += ((next_cls2_entry - NextCls2[i]) * res.ContractMultiplier * pSize - Slippages[entryIdx] - Commissions[entryIdx]) * conv;
                                }
                                Original.Add(/*orig_sum*/res.ReadonlyResults[inde]);
                                NextMidCurve.Add(next_mid_sum);
                                NextMid2Curve.Add(next_mid2_sum);
                                NextOHLCCurve.Add(next_ohlc_sum);
                                NextOHLC2Curve.Add(next_ohlc2_sum);
                                NextClsCurve.Add(next_cls_sum);
                                NextCls2Curve.Add(next_cls2_sum);
                                i--;    //test next entry data by the same date
                                inde++;
                            }
                        }

                        plotWithSum(Original, "4Original");
                        plotWithoutSum(NextMidCurve, "4Next Midpoint");
                        plotWithoutSum(NextMid2Curve, "4Next 2 Midpoints");
                        plotWithoutSum(NextOHLCCurve, "4Next Avg OHLC");
                        plotWithoutSum(NextOHLC2Curve, "4Next 2 Avg OHLC");
                        plotWithoutSum(NextClsCurve, "4Next Close");
                        plotWithoutSum(NextCls2Curve, "4Next 2 Closes");
                    }
                    #endregion

                    #region Seasonality
                    {
                        List<float>[] oddEvenDays = { new List<float>(), new List<float>() };
                        List<float>[] dayOfWeek = { new List<float>(), new List<float>(), new List<float>(), new List<float>(), new List<float>(), new List<float>(), new List<float>() };
                        List<float>[] weekNumber = { new List<float>(), new List<float>(), new List<float>(), new List<float>(), new List<float>(), new List<float>() };
                        List<float>[] month = { new List<float>(), new List<float>(), new List<float>(), new List<float>(), new List<float>(), new List<float>(), new List<float>(), new List<float>(), new List<float>(), new List<float>(), new List<float>(), new List<float>() };
                        List<float>[] quarter = { new List<float>(), new List<float>(), new List<float>(), new List<float>() };
                        Dictionary<int, List<float>> year = new Dictionary<int, List<float>>();
                        int date, dof, wn, mon, y;

                        for (int i = 0; i < res.ReadonlyResults.Count; i++)
                        {
                            date = res.ReadonlyResEntryDates[i];
                            dof = GenerateSignalsToTrade.DayOfWeek(date);   // 0-6 (Sun-Sat)
                            if (dof == 0) dof = 7;                          // 1-7 (Mon-Sun)
                            mon = GenerateSignalsToTrade.Month(date);       // 1-12
                            y = GenerateSignalsToTrade.Year(date);
                            wn = ((int)(13 + (date % 100) - dof) / 7) - 1;
                            {   // skip week if month started at Sat or Sun
                                int dd = GenerateSignalsToTrade.DayOfWeek(y * 10000 + mon * 100 + 1);
                                if (dd == 0 || dd == 6)
                                    wn--;
                                if (wn < 0)
                                    wn = 0;
                            }
                            mon -= 1;   // 0-11

                            oddEvenDays[date % 2].Add(res.ReadonlyResults[i]);
                            dayOfWeek[dof - 1].Add(res.ReadonlyResults[i]);
                            weekNumber[wn].Add(res.ReadonlyResults[i]);
                            month[mon].Add(res.ReadonlyResults[i]);
                            quarter[mon / 3].Add(res.ReadonlyResults[i]);

                            if (!year.ContainsKey(y))
                                year.Add(y, new List<float>());

                            year[y].Add(res.ReadonlyResults[i]);
                        }
                        plotWithSum(oddEvenDays[0], "61Even");
                        plotWithSum(oddEvenDays[1], "61Odd");
                        plotWithSum(dayOfWeek[0], "62Monday");
                        plotWithSum(dayOfWeek[1], "62Tuesday");
                        plotWithSum(dayOfWeek[2], "62Wednesday");
                        plotWithSum(dayOfWeek[3], "62Thursday");
                        plotWithSum(dayOfWeek[4], "62Friday");
                        plotWithSum(dayOfWeek[5], "62Saturday");
                        plotWithSum(dayOfWeek[6], "62Sunday");
                        plotWithSum(weekNumber[0], "63Week1");
                        plotWithSum(weekNumber[1], "63Week2");
                        plotWithSum(weekNumber[2], "63Week3");
                        plotWithSum(weekNumber[3], "63Week4");
                        plotWithSum(weekNumber[4], "63Week5");
                        plotWithSum(weekNumber[5], "63Week6");
                        plotWithSum(month[0], "64Jan");
                        plotWithSum(month[1], "64Feb");
                        plotWithSum(month[2], "64Mar");
                        plotWithSum(month[3], "64Apr");
                        plotWithSum(month[4], "64May");
                        plotWithSum(month[5], "64Jun");
                        plotWithSum(month[6], "64Jul");
                        plotWithSum(month[7], "64Aug");
                        plotWithSum(month[8], "64Sep");
                        plotWithSum(month[9], "64Oct");
                        plotWithSum(month[10], "64Nov");
                        plotWithSum(month[11], "64Dec");
                        plotWithSum(quarter[0], "65Q1");
                        plotWithSum(quarter[1], "65Q2");
                        plotWithSum(quarter[2], "65Q3");
                        plotWithSum(quarter[3], "65Q4");
                        foreach (KeyValuePair<int, List<float>> kv in year)
                            plotWithSum(kv.Value, "66" + kv.Key);
                    }
                    #endregion

                    #region News
                    {
                        Dictionary<string, List<DateTime>> NewsDateTimes = new Dictionary<string, List<DateTime>>();
                        foreach (var kv in AdditionalData.NewsLabels)
                        {
                            AdditionalData.GetData(kv.Key, out List<DateTime> dt, out _);
                            if (dt != null)
                            {
                                NewsDateTimes.Add(kv.Key, dt);
                                newsEvents.Add(kv.Value);
                            }
                            else
                                Logger.Current.Warn("Robustness: Data not found for news signal '{0}'!", kv.Key);
                        }

                        List<float>[] resWithNews = new List<float>[NewsDateTimes.Count + 1];
                        List<float>[] resWithoutNews = new List<float>[NewsDateTimes.Count + 1];

                        for (int i = 0; i <= NewsDateTimes.Count; ++i)
                        {
                            resWithNews[i] = new List<float>();
                            resWithoutNews[i] = new List<float>();
                        }

                        for (int i = 0; i < res.ReadonlyResults.Count; ++i)
                        {
                            DateTime date = Security.ToDateTime(res.ReadonlyResEntryDates[i], 0);
                            if (res.EntryOnClose == 1)
                                date.AddDays(1);
                            //int date = res.EntryOnClose == 0 ? res.ReadonlyResEntryDates[i] : (res.ReadonlyResEntryDates[i] + 1);   //todo: match next bar more accurate (read raw data first and iterate through it)

                            //all news togeather
                            {
                                List<float> rWith = resWithNews[0];
                                List<float> rWithout = resWithoutNews[0];

                                if (NewsDateTimes.Values.Any(x => x.Any(d => d.Date == date)))    //same date for all news
                                {
                                    //if (NewsDateTimes[date].Contains(250000) || NewsDateTimes[date].Contains(res.resEntryTimes[i])) //undefined time or same time
                                    rWith.Add(res.ReadonlyResults[i]);
                                }
                                else rWithout.Add(res.ReadonlyResults[i]);
                            }
                            // news one by one
                            for (int j = 1; j <= NewsDateTimes.Count; ++j)
                            {
                                List<float> rWith = resWithNews[j];
                                List<float> rWithout = resWithoutNews[j];

                                if (NewsDateTimes.ElementAt(j - 1).Value.Any(d => d.Date == date))    //same date
                                {
                                    //if (NewsDateTimes[date].Contains(250000) || NewsDateTimes[date].Contains(res.resEntryTimes[i])) //undefined time or same time
                                    rWith.Add(res.ReadonlyResults[i]);
                                }
                                else rWithout.Add(res.ReadonlyResults[i]);
                            }
                        }

                        //plotWithSum(res.results, "700Original");

                        for (int i = 0, j = 1; i < resWithNews.Length; ++i, ++j)
                        {
                            plotWithSum(resWithNews[i], string.Format("7{0:D2}Trade on days with news only", j));
                            plotWithSum(resWithoutNews[i], string.Format("7{0:D2}Trade on days without news", j));
                        }
                    }
                    #endregion
                });

                //-------
                GraphRobustness gr = new GraphRobustness(ppll, labels, newsEvents, false);
                var vm = GraphViewModel.Create();
                vm.Title = $"Robustness Testing - {res.Name}";
                vm.Graph = gr;
                CreateDocument(nameof(GraphView), vm);
            }
            catch (Exception ex)
            {
                Logger.Current.Warn(ex, "Robustness test error");
                MessageBoxService.Show("Can't complete operation!\n\n" + ex.Message);
            }
            finally
            {
                //Cursor = Cursors.Default;
            }
        }

        public void MinVariance()
        {
            //Cursor = Cursors.WaitCursor;
            try
            {
                List<IReadonlySimStrategy> activeRes = null;

                if (SelectedStratList.IsPortfolio)
                {
                    int activeCount = SelectedCollection.Count(x => x.Enabled) + SelectedCollection.Where(x => x.ChildStrategiesCount > 0).Sum(x => x.Children.Count(c => c.Enabled));
                    if (activeCount < 1)
                    {
                        ShowMessage("There are no enabled strategies in the portfolio");
                        return;
                    }

                    // sorted by symbol for faster raw data load
                    activeRes = SelectedCollection.Where(v => v.Enabled).OrderBy(x => x.Symbol).Cast<IReadonlySimStrategy>().ToList();
                    if (activeRes.Count(x => x.ChildStrategiesCount > 0) > 0)
                    {
                        ShowMessage("Min Variance function can't process basket strategies. Selected basket strategies will be skipped");
                        activeRes = activeRes.Where(x => x.ChildStrategiesCount == 0).ToList();
                    }
                    foreach (var v in SelectedCollection.Where(x => x.ChildStrategiesCount > 0))
                        activeRes.AddRange(v.Children.Where(c => c.Enabled));

                    List<IReadonlySimStrategy> activeResFileExists = activeRes.Where(v => RAWdataStorage.GetRawData(Utils.SymbolsManager.GetSymbolFileNameMult(v.SymbolId, out _), v.SymbolId) != null).ToList();
                    if (activeRes.Count != activeResFileExists.Count)
                    {
                        MessageBoxService.Show("One or more strategy's data files were not found. Please check user data files settings. Trader's Toolbox will process strategies with valid data files only.",
                            "Minimum Variance Testing");
                        if (activeResFileExists.Count == 0) return;
                        activeRes = activeResFileExists;
                    }
                }

                List<string> stratNames = activeRes.Select(x => x.Name).ToList();

                // construct regular data
                List<List<float>> pnlsRegular = MatchStrategiesPnls(activeRes);

                MinVariance(pnlsRegular, out List<List<double>> weights, out List<double> prets, out List<double> pvols, out double min_shrp, out double max_shrp);

                PointPairList ppl = new PointPairList();
                for (int i = 0; i < prets.Count; i++)
                    ppl.Add(pvols[i], prets[i], prets[i] / pvols[i]);


                //-------
                GraphPoints gr = new GraphPoints(ppl, weights, stratNames, min_shrp, max_shrp);
                var vm = GraphViewModel.Create();
                vm.Title = "Min Variance testing - Portfolio results";
                vm.Graph = gr;
                CreateDocument(nameof(GraphView), vm);
            }
            catch (Exception ex)
            {
                MessageBoxService.Show("Can't complete operation!\n\n" + ex.Message);
            }
            finally
            {
                //Cursor = Cursors.Default;
            }
        }

        public async Task Noise()     // + check correlation
        {
            if (SelectedStratList.IsPortfolio)   //check correlation
            {
                //Cursor = Cursors.WaitCursor;

                try
                {
                    int activeCount = SelectedCollection.Count(x => x.Enabled) + SelectedCollection.Where(x => x.ChildStrategiesCount > 0).Sum(x => x.Children.Count(c => c.Enabled));
                    if (activeCount < 1)
                    {
                        ShowMessage("There are no enabled strategies in the portfolio");
                        return;
                    }

                    List<IReadonlySimStrategy> active = SelectedCollection.Where(v => v.Enabled).Cast<IReadonlySimStrategy>().ToList();
                    if (active.Count(x => x.ChildStrategiesCount > 0) > 0)
                    {
                        ShowMessage("Check Correlation function can't process basket strategies. Selected basket strategies will be skipped");
                        active = active.Where(x => x.ChildStrategiesCount == 0).ToList();
                    }
                    foreach (var v in SelectedCollection.Where(x => x.ChildStrategiesCount > 0))
                        active.AddRange(v.Children.Where(c => c.Enabled));

                    #region Create SimStrategy for Market and Underling lines
                    //  Underlying line
                    IReadonlySimStrategy r_temp = /*(allResults.Count > 0) ? allResults.First() :*/ active.First();
                    if (r_temp.ChildStrategiesCount > 0)
                        r_temp = r_temp.ReadonlyChildren.First();
                    SimStrategy rU = new SimStrategy()
                    {
                        Name = "Underlying_" + r_temp.Symbol,
                        //nameOriginal = "Underlying_" + r_temp.symbol,
                        Symbol = r_temp.Symbol,
                        TradeMode = r_temp.TradeMode,
                        SymbolType = r_temp.SymbolType,
                        PosSizeMode = r_temp.PosSizeMode,
                        AccountValue = r_temp.AccountValue,
                        Currency = r_temp.Currency,
                        ATR_len = r_temp.ATR_len,
                        resDates = r_temp.ReadonlyResDates.ToArray(),
                        SessionEndTime = r_temp.SessionEndTime,
                        SignalStartDate = r_temp.SignalStartDate,
                        StopDate = 29990101,
                        SL_ON = r_temp.SL_ON
                    };
                    GetBuyAndHoldLine(rU, currentCurrency, 1, out rU.daily_pnls, true);
                    if (rU.daily_pnls == null)
                    {
                        MessageBoxService.Show("NO strategy data was found. Can't chart the Underlying. Please check user data settings.",
                                "Check Correlation");
                    }
                    else
                    {
                        active.Add(rU);
                    }

                    // construct "Market" line
                    SimStrategy rM = new SimStrategy()
                    {
                        Name = "Market_" + ((rU.SymbolType == Core.SymbolType.ETF) ? "SPY" : "ES"),
                        //nameOriginal = "Market_" + ((rU.SymbolType == SymbolType.ETF) ? "SPY" : "ES"),
                        Symbol = (rU.SymbolType == Core.SymbolType.ETF) ? "SPY" : "ES",
                        TradeMode = rU.TradeMode,
                        SymbolType = (rU.SymbolType == Core.SymbolType.ETF) ? Core.SymbolType.ETF : Core.SymbolType.Futures,
                        PosSizeMode = rU.PosSizeMode,
                        AccountValue = rU.AccountValue,
                        Currency = rU.Currency,
                        ATR_len = rU.ATR_len,
                        resDates = rU.resDates,
                        SessionEndTime = rU.SessionEndTime,
                        SignalStartDate = rU.SignalStartDate,
                        StopDate = 29990101,
                        SL_ON = rU.SL_ON,
                        SL_mult = rU.SL_mult
                    };
                    GetBuyAndHoldLine(rM, currentCurrency, 1, out rM.daily_pnls, true);

                    active.Add(rM);
                    activeCount = active.Count;
                    #endregion

                    /* Form form = new Form
                     {
                         Text = "Correlation Matrix",
                         Icon = this.Icon,
                         ClientSize = new Size(600, 600),
                         MinimumSize = new Size(200, 100)
                     };

                     DataGridView gv = new DataGridView
                     {
                         BorderStyle = BorderStyle.None,
                         ReadOnly = true,
                         AllowUserToAddRows = false,
                         AllowUserToDeleteRows = false,
                         AllowUserToOrderColumns = false,
                         AllowUserToResizeColumns = false,
                         AllowUserToResizeRows = false,
                         Size = form.ClientSize,
                         Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                         ColumnHeadersVisible = false,
                         RowHeadersVisible = false,
                         AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
                         AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
                         SelectionMode = DataGridViewSelectionMode.CellSelect,
                         MultiSelect = false
                     };
                     gv.CellStateChanged += Gv_CellStateChanged;
                     form.Controls.Add(gv);*/

                    DataTable dt = new DataTable();
                    var MyRowHeaderColumn = dt.Columns.Add("Strategy");
                    // add columns
                    for (int i = 1; i <= activeCount; i++)
                        dt.Columns.Add(i.ToString());

                    // add rows
                    var MyColumnHeaderRow = dt.NewRow();
                    MyColumnHeaderRow[0] = "Strategy";
                    dt.Rows.Add(MyColumnHeaderRow);
                    for (int i = 0; i < activeCount; i++)
                    {
                        MyColumnHeaderRow[i + 1] = active[i].Name;
                        var r = dt.NewRow();
                        r[0] = active[i].Name;
                        dt.Rows.Add(r);
                    }

                    /*DataGridViewTextBoxCell cellTemplate = new DataGridViewTextBoxCell();

                    DataGridViewColumn myRowHeader = new DataGridViewColumn(cellTemplate);
                    myRowHeader.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    myRowHeader.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                    //myRowHeader.DefaultCellStyle.BackColor = SystemColors.Control;
                    gv.Columns.Add(myRowHeader);
                    DataGridViewRow myColumnHeader = new DataGridViewRow();
                    myColumnHeader.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    myColumnHeader.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                    //myColumnHeader.DefaultCellStyle.BackColor = SystemColors.Control;
                    gv.Rows.Add(myColumnHeader);
                    gv.Rows[0].Cells[0].Value = "Strategy";

                    //create colunms and rows
                    for (int i = 0; i < activeCount; i++)
                    {
                        DataGridViewColumn c = new DataGridViewColumn(cellTemplate);
                        c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                        c.DefaultCellStyle.Format = "N2";
                        gv.Columns.Add(c);
                        gv.Rows[0].Cells[i + 1].Value = active[i].Name;//.Replace(" ", Environment.NewLine);


                        gv.Rows.Add();
                        gv.Rows[i + 1].Cells[0].Value = active[i].Name;
                    }*/

                    //fill data
                    List<List<float>> pnls_matrix = MatchStrategiesPnls(active);

                    for (int i = 0; i < activeCount; i++)
                    {
                        //   float zzz = 1;
                        for (int j = 0; j < activeCount; j++)
                        {
                            if (i == j)
                            {
                                dt.Rows[i + 1][j + 1] = 1;
                                //dt.Rows[i + 1][j + 1].Style.BackColor = Color.FromArgb(255, 0, 0); //0, 255, 120);
                                continue;
                            }

                            List<float> list1 = pnls_matrix[i];
                            List<float> list2 = pnls_matrix[j];

                            if (list1.Count < 2 || list2.Count < 2)
                                dt.Rows[i + 1][j + 1] = "- - -";
                            else
                            {
                                float cor = Correlation(list1, list2);
                                if (float.IsNaN(cor) || cor < -1 || cor > 1)
                                    dt.Rows[i + 1][j + 1] = "- - -";
                                else
                                {
                                    dt.Rows[i + 1][j + 1] = cor;
                                    cor = 1 - Math.Abs(cor);
                                    //t.Rows[i + 1][j + 1].Style.BackColor = Color.FromArgb(Math.Min(255, (int)(550 * (1 - cor))), Math.Min(255, (int)(550 * cor)), (int)(120 * cor));
                                    /*    gv.Rows[i + 1].Cells[j + 1].Value = zzz;
                                        gv.Rows[i + 1].Cells[j + 1].Style.BackColor = Color.FromArgb(Math.Min(255, (int)(600 * (1 - zzz))), Math.Min(255, (int)(600 * zzz)), (int)(120 * zzz));
                                        zzz -= 0.1f;*/
                                }
                            }
                        }
                    }

                    CreateDocument(nameof(CorrelationMatrixView), new CorrelationMatrixViewModel()
                    {
                        DataTable = dt
                    });

                    /*form.Show();
                    //calc window size
                    int width = 1, height = 1;
                    for (int i = 0; i < gv.ColumnCount; i++)
                    {
                        width += gv.Columns[i].Width; height += gv.Rows[i].Height;
                        if (gv.Rows[i].Height == gv.Rows[gv.Rows.Count - 1].Height)
                            gv.Rows[i].DefaultCellStyle.WrapMode = DataGridViewTriState.False;
                    }
                    if (width > 1000 || width < 50) width = 0;
                    if (height > 700 || height < 20) height = 0;
                    form.ClientSize = new Size(width > 0 ? width : form.ClientSize.Width, height > 0 ? height : form.ClientSize.Height);*/
                }
                catch (Exception ex)
                {
                    MessageBoxService.Show("Can't complete operation!\n\n" + ex.Message, "Check correlation");
                }
                finally
                {
                    //Cursor = Cursors.Default;
                }
            }
            else    // NOISE TEST (not portfolio mode)
            {
                SimStrategy res = SelectedStrategies.FirstOrDefault();
                if (res == null) return;

                if (res.IndxType == StrategySimType.CUSTOM)
                {
                    ShowMessage("Can't perform Noise Test for custom strategy");
                    return;
                }
                if (res.ReadonlyEntrySignals == null || res.ReadonlyEntrySignals.Count == 0)
                {
                    ShowMessage("Selected strategy has no supported signals");
                    return;
                }

                //Cursor = Cursors.WaitCursor;

                int prob_chg_open = decimal.ToInt32(Properties.Settings.Default.NoiseTestOpen);
                int prob_chg_high = decimal.ToInt32(Properties.Settings.Default.NoiseTestHigh);
                int prob_chg_low = decimal.ToInt32(Properties.Settings.Default.NoiseTestLow);
                int prob_chg_close = decimal.ToInt32(Properties.Settings.Default.NoiseTestClose);
                float max_chg_amt = 0.01f * decimal.ToSingle(Properties.Settings.Default.NoiseTestMax);
                uint testCount = decimal.ToUInt32(Properties.Settings.Default.NoiseTestCount);

                try
                {
                    Logger.Current.Info("Noise Test started. Tests count: {0}", testCount);

                    var tasks = new List<Task>();

                    for (int i = 0; i < testCount; ++i)
                        tasks.Add(NoiseTestTask(res, 1, prob_chg_open, prob_chg_high, prob_chg_low, prob_chg_close, max_chg_amt, currentCurrency));

                    Logger.Current.Info("Noise Test: threads are started", testCount);

                    await Task.WhenAll(tasks);

                    Logger.Current.Info("Noise Test: calculation complete", testCount);

                    List<IReadonlySimStrategy> resList = new List<IReadonlySimStrategy>() { res };

                    //read results
                    Logger.Current.Info("Noise Test: reading results", testCount);
                    var output = Simulator.ReadStratFromMemory();
                    if (!string.IsNullOrEmpty(output.Message))
                    {
                        Logger.Current.Info("Noise Test read error: {0}. Results count: {1}", output.Message, output.INstrategies?.Count);
                        MessageBoxService.Show(output.Message, "Noise Test read error");
                    }
                    else
                        //always return IN strategy, because oos_percent is hardcoded to zero
                        resList.AddRange(output.INstrategies);

                    Logger.Current.Info("Noise Test: reading complete. Results count: {0}", output.INstrategies?.Count);

                    // PLOT EQUITY CURVES (first is original)
                    List<PointPairList> ppll = resList.Where(x => x != null).
                        Select(x => GetEquityCurvePointList(x.ReadonlyResults, x.ReadonlyResDates, x.GetResTimes().Select(v => v & 0xffffff).ToList())).ToList();

                    Logger.Current.Info("Noise Test: preparing charts complete. Count: {0}", ppll.Count);

                    // prevent longer trades
                    double lastOrigDate = ppll[0].Last().Z;
                    for (int i = 1; i < ppll.Count; ++i)
                    {
                        int StartIndexToDelete = 0;
                        while (StartIndexToDelete < ppll[i].Count && ppll[i][StartIndexToDelete].Z <= lastOrigDate)
                            StartIndexToDelete++;

                        if (StartIndexToDelete < ppll[i].Count)
                            ppll[i].RemoveRange(StartIndexToDelete, ppll[i].Count - StartIndexToDelete);

                        //    if (ppll[i].Count > ppll[0].Count)
                        //        ppll[i].RemoveRange(ppll[0].Count, ppll[i].Count - ppll[0].Count);
                    }

                    Logger.Current.Info("Noise Test: results cutting complete");

                    if (ppll.Count > 0)
                    {
                        Graph gr = new Graph(ppll, "1", "2", "NoiseTest");
                        var vm = GraphViewModel.Create();
                        vm.Title = $"Noise Test - {res.Name}";
                        vm.Graph = gr;
                        CreateDocument(nameof(GraphView), vm);
                    }

                    Logger.Current.Info("Noise Test complete");
                }
                catch (Exception ex)
                {
                    Logger.Current.Info(ex, "Noise Test error");
                    MessageBoxService.Show(ex.Message, "Noise Test error");
                }
                finally
                {
                    //Cursor = Cursors.Default;
                }
            }
        }

        public async Task IntradayChecks()
        {
            if (!SelectedStratList.IsPortfolio)  //single mode
            {
                SimStrategy res = SelectedStrategies.FirstOrDefault();
                if (res == null)
                    return;

                {   // TEST DATA HAVE INTRADAY TRADES
                    int prev_date = -1;
                    int i = 0;
                    for (; i < res.ReadonlyResEntryDates.Count && prev_date != res.ReadonlyResEntryDates[i]; i++)
                        prev_date = res.ReadonlyResEntryDates[i];
                    if (i == res.ReadonlyResEntryDates.Count)
                    {
                        ShowMessage("Selected strategy has no intraday trades");
                        return;
                    }
                }

                //Cursor = Cursors.WaitCursor;

                try
                {
                    if (RAWdataStorage.GetRawData(Utils.SymbolsManager.GetSymbolFileNameMult(res.SymbolId, out _), res.SymbolId) is RAWdata rdata)
                    {
                        int[] Dates = rdata.Dates;
                        int[] Times = rdata.Times;

                        string str = "";

                        // Intraday check start
                        SortedDictionary<float, IntraRes> master_cur = new SortedDictionary<float, IntraRes>();
                        List<double> master_pnl = new List<double>();
                        List<double> master_dd = new List<double>();
                        List<double> master_average = new List<double>();

                        await Task.Run(() =>
                        {
                            int master_count = 100;

                            // ----------------------------------------
                            // Configurations
                            //
                            bool max_pnl_on = Properties.Settings.Default.TTmaxPNLEnabled;
                            int mx_one = decimal.ToInt32(Properties.Settings.Default.TTmaxPNLstart);
                            int mx_two = decimal.ToInt32(Properties.Settings.Default.TTmaxPNLend);
                            int mx_itr = decimal.ToInt32(Properties.Settings.Default.TTmaxPNLstep);
                            if (max_pnl_on == false)
                            { mx_one = 999999; mx_two = 999999 + 1; mx_itr = 1; }

                            bool min_pnl_on = Properties.Settings.Default.TTminPNLenabled;
                            int mi_one = decimal.ToInt32(Properties.Settings.Default.TTminPNLstart);
                            int mi_two = decimal.ToInt32(Properties.Settings.Default.TTminPNLend);
                            int mi_itr = decimal.ToInt32(Properties.Settings.Default.TTminPNLstep);
                            if (min_pnl_on == false)
                            { mi_one = -999999; mi_two = -999999 + 1; mi_itr = 1; }

                            bool max_trd_on = Properties.Settings.Default.TTmaxTradesEnabled;
                            int mx_trd_one = decimal.ToInt32(Properties.Settings.Default.TTmaxTradesStart);
                            int mx_trd_two = decimal.ToInt32(Properties.Settings.Default.TTmaxTradesEnd);
                            int mx_trd_itr = decimal.ToInt32(Properties.Settings.Default.TTmaxTradesStep);
                            if (max_trd_on == false)
                            { mx_trd_one = 999999; mx_trd_two = 999999 + 1; mx_trd_itr = 1; }

                            bool start_on = Properties.Settings.Default.TTstartTimeEnabled;
                            int strt_one = Properties.Settings.Default.TTstartTimeStart.Hour * 10000 + Properties.Settings.Default.TTstartTimeStart.Minute * 100;
                            int strt_two = Properties.Settings.Default.TTstartTimeEnd.Hour * 10000 + Properties.Settings.Default.TTstartTimeEnd.Minute * 100;
                            int strt_itr = Properties.Settings.Default.TTstartTimeStep.Hour * 10000 + Properties.Settings.Default.TTstartTimeStep.Minute * 100;
                            if (start_on == false)
                            { strt_one = 0; strt_two = 1; strt_itr = 1; }

                            bool end_on = Properties.Settings.Default.TTendTimeEnabled;
                            int end_one = Properties.Settings.Default.TTendTimeStart.Hour * 10000 + Properties.Settings.Default.TTendTimeStart.Minute * 100;
                            int end_two = Properties.Settings.Default.TTendTimeEnd.Hour * 10000 + Properties.Settings.Default.TTendTimeEnd.Minute * 100;
                            int end_itr = Properties.Settings.Default.TTendTimeStep.Hour * 10000 + Properties.Settings.Default.TTendTimeStep.Minute * 100;
                            if (end_on == false)
                            { end_one = 235959; end_two = 240000; end_itr = 1; }

                            int count = 0;

                            // ---------------------------------------------
                            //   Test Logic
                            //
                            for (int mp = mx_one; mp <= mx_two; mp += mx_itr)
                            {
                                int max_pnl = max_pnl_on ? mp : 9999999;
                                for (int pm = mi_one; pm <= mi_two; pm += mi_itr)
                                {
                                    int min_pnl = min_pnl_on ? pm : -9999999;
                                    for (int xt = mx_trd_one; xt <= mx_trd_two; xt += mx_trd_itr)
                                    {
                                        int max_trades = max_trd_on ? xt : 9999999;
                                        for (int st = strt_one; st <= strt_two; st += strt_itr)
                                        {
                                            if ((st % 10000) / 100 >= 60) continue;  //skip minutes 60-99
                                            int start_time = start_on ? st : 0;
                                            for (int et = end_one; et <= end_two; et += end_itr) // what if < start_time?
                                            {
                                                if ((et % 10000) / 100 >= 60) continue;  //skip minutes 60-99

                                                int end_time = end_on ? et : 235959;
                                                int prev_date = 0;
                                                int prev_time = 0;
                                                float pnl_today = 0;
                                                int trades_today = 0;
                                                List<float> pnl = new List<float>();
                                                //List<int> dts = new List<int>();
                                                //List<short> tms = new List<short>();
                                                int entry_idx = 0;

                                                for (int i = 0; i < res.ReadonlyResults.Count; i++)
                                                {
                                                    int d = res.ReadonlyResEntryDates[i];
                                                    int t = res.ReadonlyResEntryTimes[i];
                                                    float r = res.ReadonlyResults[i];
                                                    int session_end = res.SessionEndTime * 100;

                                                    if (res.EntryOnClose == 0)   //next bar open
                                                    {
                                                        entry_idx = FindRawBarIndexByDateTime(Dates, Times, d, t, (entry_idx > 0 ? entry_idx : 0));
                                                        if (entry_idx > 0)
                                                        {   // take previous bar date/time
                                                            entry_idx--;
                                                            d = Dates[entry_idx];
                                                            t = Times[entry_idx];
                                                        }
                                                    }

                                                    if ((d > prev_date && (prev_time < session_end || t >= session_end)) ||
                                                        (d == prev_date && (prev_time < session_end && t >= session_end)))
                                                    {
                                                        trades_today = 0;
                                                        pnl_today = 0;
                                                        //starting_pnl = 0;
                                                        //for (int i = 0; i < pnl.Count; i++) starting_pnl += pnl[i];
                                                    }
                                                    if (t >= start_time && t <= end_time && trades_today < max_trades && pnl_today > min_pnl && pnl_today < max_pnl)
                                                    {
                                                        pnl.Add(r); //dts.Add(d); tms.Add(t);
                                                        trades_today += 1;
                                                        pnl_today += r;
                                                    }
                                                    prev_date = d;
                                                    prev_time = t;
                                                }
                                                if (pnl.Count > 0)  //if have trades
                                                {
                                                    count += 1;

                                                    float cumsum = 0;
                                                    float max_dd = 0;
                                                    float hwm = 0;
                                                    for (int i = 0; i < pnl.Count; i++)
                                                    {
                                                        cumsum += pnl[i];
                                                        hwm = cumsum > hwm ? cumsum : hwm;
                                                        max_dd = cumsum - hwm <= max_dd ? cumsum - hwm : max_dd;
                                                    }
                                                    // add to statistic
                                                    master_pnl.Add(cumsum);
                                                    master_dd.Add(max_dd);
                                                    master_average.Add(cumsum / pnl.Count);

                                                    // select best lines
                                                    int zz = 0;
                                                    while (master_cur.ContainsKey(cumsum) && zz < master_count) // to prevent multiple keys
                                                    {
                                                        List<float> prev = master_cur[cumsum].values;
                                                        if (prev.Count == pnl.Count)
                                                        {   // skip equal lines
                                                            bool equal = true;
                                                            for (int i = 0; i < pnl.Count; i += 2)
                                                                if (pnl[i] != prev[i])
                                                                {
                                                                    equal = false; break;
                                                                }
                                                            if (equal) { zz = master_count; break; }
                                                        }
                                                        cumsum += 0.001f; zz++;
                                                    }
                                                    if (zz < master_count)
                                                    {
                                                        int[] param = new int[5] { max_pnl, min_pnl, start_time, end_time, max_trades };

                                                        str = string.Format("Max PL: {0:F2} Min PL: {1:F2}{5}Start: {2} End: {3}{5}Max Trades: {4}{5}", max_pnl, min_pnl, start_time, end_time, max_trades, Environment.NewLine);
                                                        str += string.Format("Cumsum {0:F2} Avg {1:F2} Count {2}", cumsum, cumsum / pnl.Count, pnl.Count);

                                                        IntraRes r = new IntraRes
                                                        {
                                                            param = param,
                                                            label = str,
                                                            values = pnl
                                                        };

                                                        master_cur.Add(cumsum, r);
                                                        if (master_cur.Count > master_count)
                                                            master_cur.Remove(master_cur.ElementAt(0).Key); //remove first element with lowest cumsum
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        });

                        if (master_cur.Count < 2)
                        {
                            ShowMessage("No intraday checking results. Please check tests settings or select another strategy");
                            return;
                        }

                        List<int[]> args = new List<int[]>();
                        List<string> labels = new List<string>();

                        List<PointPairList> ppll = new List<PointPairList> {
                            new PointPairList(),                // for original e-curve
                            await CalcHistogram(master_pnl, 10),      // for master_pnl
                            await CalcHistogram(master_dd, 10),       // for master_dd
                            await CalcHistogram(master_average, 10)   // for master_avg
                        };

                        // original e-curve
                        double sum = 0;
                        for (int i = 0; i < res.ReadonlyResults.Count; i++)
                        {
                            sum += res.ReadonlyResults[i];
                            DateTime dateTime = DateTime.ParseExact(res.ReadonlyResDates[i].ToString(), "yyyyMMdd", CultureInfo.InvariantCulture);
                            ppll[0].Add(i, sum, dateTime.ToOADate());
                        }
                        // filtered curves
                        foreach (IntraRes r in master_cur.Values)
                        {
                            PointPairList ppl = new PointPairList();
                            float pnl = 0;
                            for (int i = 0; i < r.values.Count; i++)
                            {
                                pnl += r.values[i];
                                ppl.Add(i, pnl);    // cumsum
                            }
                            ppll.Add(ppl);          // cumsum line
                            labels.Add(r.label);
                            args.Add(r.param);
                        }

                        // locate stategy (find index)
                        int indexInList = SelectedCollection.IndexOf(res);
                        if (indexInList < 0)
                        {
                            MessageBoxService.Show("Error! Can't find strategy index", "Intraday checks");
                            return;
                        }

                        //--------
                        GraphIntraday gr = new GraphIntraday(ppll, labels, args)
                        {
                            STRtype = (int)res.IndxType,
                            STRindex = indexInList
                        };

                        var vm = GraphIntradayViewModel.Create();
                        vm.Title = "Intraday Checks - " + res.Name;
                        vm.Graph = gr;
                        vm.strategy = res;
                        CreateDocument(nameof(GraphView), vm);
                    }
                    else
                        ShowMessage("Intraday Checks function can't identify strategy's raw data");
                }
                catch (OutOfMemoryException ex)
                {
                    MessageBoxService.Show("Not enough memory to complete the operation\n\n" + ex.Message, "Intraday Checks Error");
                }
                catch(AggregateException ex)
                {
                    MessageBoxService.Show("Can't complete the operation\n\n" + ex.InnerException.Message, "Intraday Checks Error");
                }
                catch(Exception ex)
                {
                    MessageBoxService.Show("Can't complete the operation\n\n" + ex.Message, "Intraday Checks Error");
                }
                finally
                {
                    //Cursor = Cursors.Default;
                }
            }
        }

        public async Task IntradayEdge()
        {
            /*if (buttonIntradayEdge.Text.Contains("STOP"))    // stop simulation
            {
                CppSimulatorInterface.CancelSimulation();
                return;
            }*/

            IReadonlySimStrategy res = SelectedStrategies.FirstOrDefault();
            if (res == null) return;

            if (res.IndxType == StrategySimType.CUSTOM)
            {
                ShowMessage("Intraday Edge function can't process custom strategies");
                return;
            }
            if (res.Indx == 0)   //current simulation result only
            {
                ShowMessage("Intraday Edge function works on current simulation IN, OUT, ALL results only");
                return;
            }
            if (res.IndxType == StrategySimType.IN || res.IndxType == StrategySimType.OUT)
            {
                res = GetAllStrategy(res);
            }
            if (res == null || res.EntrySignalsValues == null)
            {
                ShowMessage("Intraday Edge function didn't find corresponding All result for selected strategy");
                return;
            }
            if (res.ChildStrategiesCount > 0)
            {
                ShowMessage("Intraday Edge function can't process basket strategies");
                return;
            }
            if (res.EntryOnClose != 0)
            {
                ShowMessage("Intraday Edge function can't process strategies with EnterOnThisBarClose mode");
                return;
            }
            if (!res.InvestCash.IsEmpty())
            {
                ShowMessage("Intraday Edge function can't process strategies with cash investing");
                return;
            }

            Core.Symbol IntraSymbol = null;

            if (!string.IsNullOrEmpty(Properties.Settings.Default.IntraSymbol))
            {
                IntraSymbol = Utils.SymbolsManager.Symbols.FirstOrDefault(x => x.Id.ToString() == Properties.Settings.Default.IntraSymbol);
            }
            if (IntraSymbol == null)
            {
                ShowMessage("Intraday Edge function can't find raw file with intraday data");
                return;
            }
            if (IntraSymbol.Id == res.SymbolId)
            {
                ShowMessage("Intraday Edge symbol should not be same as strategy's base symbol");
                return;
            }

            //Cursor = Cursors.WaitCursor;
            //buttonIntradayEdge.Text = "Intraday Edge... STOP";

            try
            {
                await Simulator.IntradayEdge(res, IntraSymbol, 
                    (ushort)(Properties.Settings.Default.IntraEndOfDay.Hour*100 + Properties.Settings.Default.IntraEndOfDay.Minute),
                    Properties.Settings.Default.IntraExcludeExits);
            }
            catch (Exception ex)
            {
                MessageBoxService.Show("Can't complete operation!\n\n" + ex.Message, "Intraday Edge");
            }
            finally
            {
                //Cursor = Cursors.Default;
                //buttonIntradayEdge.Text = "Intraday Edge";
            }

            // open new results window
            /*Dictionary<SymbolId, SymbolResData> symbolsData = new Dictionary<SymbolId, SymbolResData>
                {
                    { IntraSymbol.Id, new SymbolResData(IntraSymbol.Name, IntraSymbol.Timeframe, IntraSymbol.Filename, IntraSymbol.CM) }
                };

            ResForm resForm = new ResForm(currentCurrency, res.TradeMode, symbolsData, res.StopDate, newMarketSymbolsIds,
                res.InvestCash, res.SL_ON, (float)res.SL_mult, res.ATR_len, res.AccountValue, new SymbolId(), new SymbolId(), new SymbolId(), false, false)
            {
                Text = "Intraday Edge results"
            };
            resForm.Show();*/

            //read strategies
            var r3Results = new NotifyObservableCollection<SimStrategy>();
            var oosResults = new NotifyObservableCollection<SimStrategy>();
            var allResults = new NotifyObservableCollection<SimStrategy>();

            List<SymbolId> newMarketSymbolsIds = new List<SymbolId>() { res.SymbolId }; //base symbol become m2
            if (res.ReadonlyMarketSymbolsIds != null)
                newMarketSymbolsIds.AddRange(res.ReadonlyMarketSymbolsIds);             //m2->m3, m3->m4,...

            try
            {   //read main simulation data
                using (BinaryReader reader = new BinaryReader(File.Open("Data/simOut.data", FileMode.Open)))
                {
                    int r3count = reader.ReadInt32();
                    int oosCount = reader.ReadInt32();
                    int allCount = reader.ReadInt32();

                    Simulator.ReadToList(reader, r3count, r3Results, res.StopDate, res.SlippageMode, res.CommissionMode, res.Currency, res.InvestCash, newMarketSymbolsIds);
                    Simulator.ReadToList(reader, oosCount, oosResults, res.StopDate, res.SlippageMode, res.CommissionMode, res.Currency, res.InvestCash, newMarketSymbolsIds);
                    Simulator.ReadToList(reader, allCount, allResults, res.StopDate, res.SlippageMode, res.CommissionMode, res.Currency, res.InvestCash, newMarketSymbolsIds);
                }
            }
            catch (Exception e)
            {
                MessageBoxService.Show("Can't read input file!\n\nError: " + e.Message, "File read error");
                r3Results.Clear();
                oosResults.Clear();
                allResults.Clear();
            }

            // show
            var vm = Create();
            List<StrategiesListVM> list = new List<StrategiesListVM>()
                {
                    new StrategiesListVM() { Title="IN", Strategies = r3Results },
                    new StrategiesListVM() { Title="OUT", Strategies = oosResults },
                    new StrategiesListVM() { Title="ALL", Strategies = allResults },
                };
            vm.Parameter = list;
            CreateDocument(nameof(StrategiesView), vm);
        }

        public async Task SignalBreakdown()
        {
            SimStrategy res = SelectedStrategies.FirstOrDefault();
            if (res == null) return;

            if (res.ChildStrategiesCount > 0)
            {
                ShowMessage("Signal Breakdown function can't process basket strategies");
                return;
            }
            if (res.IndxType == StrategySimType.CUSTOM)
            {
                ShowMessage("Signal Breakdown function can't process custom strategies");
                return;
            }

            //Cursor = Cursors.WaitCursor;
            //buttonSignalBreakdown.Enabled = false;

            try
            {
                // get files names
                string symbolFile = Utils.SymbolsManager.GetSymbolFileNameMult(res.SymbolId, out double contrMultExternal);
                string market2File = Utils.SymbolsManager.GetSymbolFileNameMult(res.GetMarketSymbolId(2), out _);
                string market3File = Utils.SymbolsManager.GetSymbolFileNameMult(res.GetMarketSymbolId(3), out _);
                if (!File.Exists(symbolFile))
                {
                    MessageBoxService.Show("No symbol data file was found! Please check app settings.\n\n" + symbolFile, "Signal Breakdown");
                    return;
                }
                if (!string.IsNullOrEmpty(market2File) && !File.Exists(market2File))
                {
                    MessageBoxService.Show("No symbol data file was found! Please check app settings.\n\n" + market2File, "Signal Breakdown");
                    return;
                }
                if (!string.IsNullOrEmpty(market3File) && !File.Exists(market3File))
                {
                    MessageBoxService.Show("No symbol data file was found! Please check app settings.\n\n" + market3File, "Signal Breakdown");
                    return;
                }

                #region Create new form with 4 columns table
                

                /*

                DataGridViewTextBoxColumn c1 = new DataGridViewTextBoxColumn
                {
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                    FillWeight = 40,
                    Name = "sigWin",
                    HeaderText = "Signals"
                };
                c1.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                //       c1.DefaultCellStyle.BackColor = Color.FromArgb(242,255,242);
                table.Columns.Add(c1);

                DataGridViewTextBoxColumn c2 = new DataGridViewTextBoxColumn
                {
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                    FillWeight = 10,
                    Name = "sigWinValue",
                    HeaderText = "Win, %"
                };
                c2.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                c2.DefaultCellStyle.BackColor = Color.FromArgb(242, 255, 242);
                c2.DefaultCellStyle.Format = "F3";
                table.Columns.Add(c2);*/

                /*DataGridViewTextBoxColumn c3 = new DataGridViewTextBoxColumn();
                c3.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                c3.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                //       c3.DefaultCellStyle.BackColor = Color.FromArgb(255,242,242);
                Padding p = c3.DefaultCellStyle.Padding;
                p.Left = 10;
                c3.DefaultCellStyle.Padding = p;
                c3.FillWeight = 40;
                c3.Name = "sigLos";
                c3.HeaderText = "Losing trades signals";
                table.Columns.Add(c3);*/

                /*DataGridViewTextBoxColumn c4 = new DataGridViewTextBoxColumn
                {
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                    FillWeight = 10,
                    Name = "sigLosValue",
                    HeaderText = "Los, %"
                };
                c4.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                c4.DefaultCellStyle.BackColor = Color.FromArgb(255, 242, 242);
                c4.DefaultCellStyle.Format = "F3";
                table.Columns.Add(c4);

                DataGridViewTextBoxColumn c5 = new DataGridViewTextBoxColumn
                {
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                    FillWeight = 10,
                    Name = "sigWinLosDiffValue",
                    HeaderText = "Diff, %"
                };
                c5.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                c5.DefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245);
                c5.DefaultCellStyle.Format = "F3";
                table.Columns.Add(c5);*/
                #endregion

                // prepare signals
                Dictionary<SymbolId, KeyValuePair<bool, string>> sdf = new Dictionary<SymbolId, KeyValuePair<bool, string>>() {
                    { new SymbolId("Vix", ""), new KeyValuePair<bool,string>(true, "Data/VixData.txt") },
                    { res.SymbolId, new KeyValuePair<bool,string>(true, symbolFile) }
                };
                if (!res.GetMarketSymbolId(2).IsEmpty() && !sdf.ContainsKey(res.GetMarketSymbolId(2)))
                    sdf.Add(res.GetMarketSymbolId(2), new KeyValuePair<bool, string>(true, market2File));
                if (!res.GetMarketSymbolId(3).IsEmpty() && !sdf.ContainsKey(res.GetMarketSymbolId(3)))
                    sdf.Add(res.GetMarketSymbolId(3), new KeyValuePair<bool, string>(true, market3File));

                // replace main, m2/3 symbols with current symbols
                SymbolId[] syd = new SymbolId[] { sdf.Keys.First(), res.SymbolId, res.GetMarketSymbolId(2), res.GetMarketSymbolId(3) };

                // get all available signals
                var signalsList = /*await*/ SignalsFactory.DeserializeSignals(Properties.Settings.Default.SignalsXML).ToList();
                foreach (var sig in signalsList)
                {
                    sig.ActiveForEntry = sig.ActiveForExit = false;
                    sig.SetSymbolId(syd);
                    if (sig.GroupId != null)
                    {
                        if (sig.MarketNumber == 1 || (sig.MarketNumber == 2 && !res.GetMarketSymbolId(2).IsEmpty()) || (sig.MarketNumber == 3 && !res.GetMarketSymbolId(3).IsEmpty()))
                            sig.ActiveForEntry = true;
                        // turn off if some child signals are from M2/M3
                        if ((res.GetMarketSymbolId(2).IsEmpty() && sig.AllChildren.Any(x => x.MarketNumber == 2)) || (res.GetMarketSymbolId(3).IsEmpty() && sig.AllChildren.Any(x => x.MarketNumber == 3)))
                            sig.ActiveForEntry = false;
                        //turn off custom python signals
                        if (sig.Type == Signal.SignalTypes.CustomIndicator && sig is SignalPython)
                            sig.ActiveForEntry = false;
                    }
                }
                SignalsFactory.RealizeParametricSignals(signalsList);

                double margin = Utils.SymbolsManager.GetSymbolMargin(res.SymbolId);

                string signalsFileName = "Data/PythonSignals_" + res.Symbol + ".txt";
                string namesFileName = "Data/PythonNames_" + res.Symbol + ".txt";
                string dataFileName = "Data/PythonData_" + res.Symbol + ".txt";

                //todo: skip strategies signals and custom signals??

                // Settings
                SimulationSettings sim = new SimulationSettings(signalsList,
                    Security.ToDateTime(res.SignalStartDate, res.SignalStartTime),
                    Security.ToDateTime(res.StopDate, 235959),
                    false, false, new List<SymbolId>() { res.SymbolId },
                    sdf, res.SymbolId, dataFileName, namesFileName, signalsFileName, margin, res.ContractMultiplier,
                    res.TradeMode, res.PT_ON, res.SL_ON, res.TL_ON,
                    (float)res.PT_mult, (float)res.SL_mult, res.TL_mult,
                    res.HH_ON, res.LL_ON, res.HHlook, res.LLlook, res.ATR_len,
                    res.PosSizeMode, res.SlippageMode, res.CommissionMode, res.Commission, res.Slippage,
                    res.Currency, res.AccountValue, res.MarketSymbolsIds, res.InvestCash,
                    res.MaxTime, res.ProfX, res.Fitness, res.OosLocation, 0/*oos_percent*/, 1, 1,
                    res.EntryOnClose, res.ExitOnClose, res.ForceExitOnSessionEnd ? (short)1 : (short)0,
                    new DateTime(1,1,1,res.SessionEndTime/100, res.SessionEndTime%100,0),
                    res.DelayedEntry, res.RebalanceMethod, res.RebalancePeriod, res.RebalanceSymbolsCount,
                    0, 0, 0, 0, false, 0, 0, 0, 0, 0, 0, 0);

                // Generate Signals
                var output = await SignalsFactory.CalculateSignalsAsync(sim, null, null, null);

                if (output.Message != null)
                    throw new Exception(output.Message);

                // read signals and match dates with raw data
                if (ReadGeneratorOutput(namesFileName, dataFileName, signalsFileName, out List<int> dates, out List<int> times, out List<BitArray> signals, out List<string> signalNames) is string error)
                {
                    MessageBoxService.Show(error, "File read error");
                    return;
                }

                // todo: сигнал с учетом ATR length (можно упростить на первое время)

                //iterate through trades and dates with signal to choose Win and Los signals
                Dictionary<int, int[]> winLosSignals = new Dictionary<int, int[]>();
                //Dictionary<string, int> losSignals = new Dictionary<string, int>();
                int date, time;
                for (int t = 0, dt = 0; t < res.ReadonlyResults.Count; ++t)
                {
                    date = res.ReadonlyResEntryDates[t];
                    time = res.ReadonlyResEntryTimes[t];

                    for (; dt < dates.Count && dates[dt] < date; dt++) ;
                    for (; dt < times.Count && dates[dt] == date && times[dt] < time; dt++) ;

                    if (dt < times.Count && dates[dt] == date && times[dt] == time)     // date time found, test signals
                    {
                        if (res.EntryOnClose == 0 && dt > 0) --dt;    //take previous bar if nextBarOpen entry used

                        for (int z = 0; z < signals.Count; z++)
                        {
                            if (signals[z][dt])
                            {
                                if (res.ReadonlyResults[t] > 0)    //win trade
                                {
                                    if (winLosSignals.ContainsKey(z))
                                        winLosSignals[z][0] += 1;
                                    else
                                        winLosSignals.Add(z, new int[2] { 1, 0 });
                                }
                                else   // loss trade
                                {
                                    if (winLosSignals.ContainsKey(z))
                                        winLosSignals[z][1] += 1;
                                    else
                                        winLosSignals.Add(z, new int[2] { 0, 1 });
                                }
                            }
                        }
                    }
                    if (dt > 0) --dt;   // analize last bar twice
                }

                // get percentage
                int winTradesCount = res.ReadonlyResults.Count(x => x > 0);
                int losTradesCount = res.ReadonlyResults.Count - winTradesCount;
                var percSig = winLosSignals.Select(x => new KeyValuePair<string, float[]>(signalNames[x.Key],
                    new float[2] { winTradesCount > 0 ? x.Value[0] * 100.0f / winTradesCount : 0,
                                   losTradesCount > 0 ? x.Value[1] * 100.0f / losTradesCount : 0 })).OrderByDescending(x => x.Value[0]).ToList();
                //List<KeyValuePair<float, string>> winPercSig = winSignals.Select(x => new KeyValuePair<float, string>(x.Value * 100.0f / winTradesCount, x.Key)).OrderByDescending(x => x.Key).ToList();
                //List<KeyValuePair<float, string>> losPercSig = losSignals.Select(x => new KeyValuePair<float, string>(x.Value * 100.0f / losTradesCount, x.Key)).OrderByDescending(x => x.Key).ToList();

                // print results
                //string ws, ls;
                ObservableCollection<SignalBreakdownItem> items = new ObservableCollection<SignalBreakdownItem>();
                for (int i = 0; i < 5000 && i < percSig.Count; ++i)
                {
                    //ws = i < percSig.Count ? SignalsData.SignalNames[percSig[i].Key].codeEL : string.Empty;
                    //ls = i < percSig.Count ? SignalsData.SignalNames[percSig[i].Key].codeEL : string.Empty;

                    //if (ws == string.Empty && ls == string.Empty) break;

                    //table.Rows.Add(ws, ws == string.Empty ? string.Empty : string.Format("{0, 7:F3}", percSig[i].Value[0]),
                    //    ls, ls == string.Empty ? string.Empty : string.Format("{0, 7:F3}", percSig[i].Value[1]));

                    Signal sig = Serializer.Deserialize(percSig[i].Key, typeof(Signal)) as Signal;

                    items.Add(new SignalBreakdownItem()
                    {
                        SignalName = sig?.TextVisual ?? "UNKNOWN_SIGNAL",
                        Win = percSig[i].Value[0],
                        Loss = percSig[i].Value[1]
                    });
                }

                if (items.Count == 0)
                {
                    ShowMessage("Signal Breakdown function can't calculate signals for selected strategy");
                    return;
                }

                // show
                var vm = new SignalBreakdownViewModel()
                {
                    Items = items,
                    Title = "Signal Breakdown - " + res.Name
                };
                CreateDocument(nameof(SignalBreakdownView), vm);
            }
            catch (Exception ex)
            {
                MessageBoxService.Show("Can't complete operation!\n\n" + ex.Message, "Signal Breakdown");
            }
            finally
            {
                //buttonSignalBreakdown.Enabled = true;
                //Cursor = Cursors.Default;
            }
        }

        public void ELcodeGen()
        {
            List<IReadonlySimStrategy> activeResults = new List<IReadonlySimStrategy>();

            // get first selected strategy
            if (SelectedStrategies?.Count > 0)
                activeResults.Add(SelectedStrategies[0]);
            else
                return;

            List<string> codegenOutput = Codegen.EL(activeResults, out var extraCode);

            var toVM = TextOutputViewModel.Create();
            toVM.Title = $"EL code - {(activeResults.Count == 1 ? activeResults[0].Name : "multiple strategies")}";
            toVM.MainFormattedText = codegenOutput[0];
            toVM.ExtraCode = extraCode;
            CreateDocument(nameof(TextOutputView), toVM);
        }

        public void NTcodeGen()
        {
            List<IReadonlySimStrategy> activeResults = new List<IReadonlySimStrategy>();

            // get first selected strategy
            if (SelectedStrategies?.Count > 0)
                activeResults.Add(SelectedStrategies[0]);
            else
                return;

            List<string> codegenOutput = Codegen.NT(activeResults, out var extraCode);

            var toVM = TextOutputViewModel.Create();
            toVM.Title = $"NT8 code - {(activeResults.Count == 1 ? activeResults[0].Name : "multiple strategies")}";
            toVM.MainFormattedText = codegenOutput[0];
            toVM.ExtraCode = extraCode;
            CreateDocument(nameof(TextOutputView), toVM);
        }
        public void MTcodeGen()
        {
            List<IReadonlySimStrategy> activeResults = new List<IReadonlySimStrategy>();

            // get first selected strategy
            if (SelectedStrategies?.Count > 0)
                activeResults.Add(SelectedStrategies[0]);
            else
                return;

            List<string> codegenOutput = Codegen.MT4(activeResults, out var extraCode);

            var toVM = TextOutputViewModel.Create();
            toVM.Title = $"MT4 code - {(activeResults.Count == 1 ? activeResults[0].Name : "multiple strategies")}";
            toVM.MainFormattedText = codegenOutput[0];
            toVM.ExtraCode = extraCode;
            CreateDocument(nameof(TextOutputView), toVM);
        }
        public void PythonCodeGen()
        {
            List<IReadonlySimStrategy> activeResults = new List<IReadonlySimStrategy>();

            // get first selected strategy
            if (SelectedStrategies?.Count > 0)
                activeResults.Add(SelectedStrategies[0]);
            else
                return;

            List<string> codegenOutput = Codegen.Python(activeResults);
            if (codegenOutput != null)
            {
                var toVM = TextOutputViewModel.Create();
                toVM.Title = $"Python code - {(activeResults.Count == 1 ? activeResults[0].Name : "multiple strategies")}";
                toVM.MainFormattedText = codegenOutput[0];
                CreateDocument(nameof(TextOutputView), toVM);
            }
        }
        public void PRTcodeGen()
        {
            List<IReadonlySimStrategy> activeResults = new List<IReadonlySimStrategy>();

            // get first selected strategy
            if (SelectedStrategies?.Count > 0)
                activeResults.Add(SelectedStrategies[0]);
            else
                return;

            List<string> codegenOutput = Codegen.PRT(activeResults, out var extraCode);

            var toVM = TextOutputViewModel.Create();
            toVM.Title = $"PRT code - {(activeResults.Count == 1 ? activeResults[0].Name : "multiple strategies")}";
            toVM.MainFormattedText = codegenOutput[0];
            toVM.ExtraCode = extraCode;
            CreateDocument(nameof(TextOutputView), toVM);
        }

        public async Task Export()
        {
            try
            {
                SaveFileDialogService.DefaultExt = "csv";
                SaveFileDialogService.Filter = "CSV files (*.csv)|*.csv|All Files (*.*)|*.*";
                SaveFileDialogService.FilterIndex = 1;

                if (SaveFileDialogService.ShowDialog())
                {
                    string plainText = ExportTableToCSV(SelectedCollection);
                        using (var stream = new StreamWriter(SaveFileDialogService.OpenFile()))
                        {
                            await stream.WriteAsync(plainText);
                        }
                    ShowMessage("Results were exported to CSV file");
                }
            }
            catch
            {
                ShowMessage("Can't complete export to CSV file");
            }
        }

        public void TestSettings()
        {
            var c = new UICommand()
            {
                Id = MessageBoxResult.OK,
                Caption = "          OK          ",
                Alignment = DialogButtonAlignment.Center
            };
            DialogServiceSettings.ShowDialog(MessageButton.OK, "Tests Settings", nameof(TestSettingsView), TestSettingsViewModel.Create());
        }

        public void AddToPortfolio()
        {
            if (SelectedStratList.IsPortfolio)
                return;

            var selectedCopy = SelectedStrategies.Select(x => x.Clone() as SimStrategy).ToList();
            Messenger.Default.Send(new AddStrategiesToPortfolioMessage(selectedCopy));
        }
        public void DeleteStrategy()
        {
            if (MessageBoxService.Show("Do you really want to delete selected strategies?", "", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
               return;

            var toDelete = SelectedStrategies.ToList();

            toDelete.RemoveAll(x => x.Parent != null && SelectedStrategies.Contains(x.Parent));

            if (toDelete.Count > 0)
            {
                foreach (var st in toDelete)
                {
                    if (st.Parent != null)   //child
                    {
                        st.Parent.Children.Remove(st);  //remove from parent

                        if (st.Parent.Children.Count > 0)
                        {
                            // reconstruct and recalc basket
                            SimStrategy r = SimStrategy.ConstructBasket(st.Parent.Children);
                            int i = SelectedCollection.IndexOf(st.Parent);
                            SelectedCollection[i] = r;
                        }
                        else
                            SelectedCollection.Remove(st.Parent);          //remove basket with no children
                    }
                    else SelectedCollection.Remove(st);     // usual strategy
                }

                //if (indexes.Count > 1)
                //    ShowMessage(indexes.Count.ToString() + " items deleted successfully");
                //else
                //    ShowMessage("1 item deleted successfully");
            }
            else
                ShowMessage("No items available for delete");
        }
        public void EnableSelected()
        {
            foreach (var st in SelectedStrategies)
                st.Enabled = true;
        }
        public void DisableSelected()
        {
            foreach (var st in SelectedStrategies)
                st.Enabled = false;
        }
        public void AddToFavorites()
        {
            if (SelectedStrategies.FirstOrDefault() is SimStrategy strategy)
            {
                if (strategy.EntrySignals == null || strategy.EntrySignals.Count == 0)
                {
                    ShowMessage("Can't identify strategy's signals");
                    return;
                }

                var vm = AddSignalsToFavoritesViewModel.Create();
                vm.Signals = strategy.EntrySignals;
                DialogServiceFavorites.ShowDialog(null, "Add Signals to Favorites", nameof(AddSignalsToFavoritesView), vm);
            }
        }
        public void ShowTrades() {
            //Cursor = Cursors.WaitCursor;
            try
            {
                foreach (SimStrategy res in SelectedStrategies)
                {
                    if (!(RAWdataStorage.GetRawData(Utils.SymbolsManager.GetSymbolFileNameMult(res.SymbolId, out _), res.SymbolId) is RAWdata rdata))
                        continue;

                    float[] PosSizes = null, Slippages = null, Commissions = null;
                    CalcPSforStrategy(res, ref PosSizes, ref Slippages, ref Commissions);

                    bool IsRawColumnVisible = false;
                    
                    List<float> entryValues = null, exitvalues = null, resPosSizes = null;
                    if (res.Indx > 0 && res.ChildStrategiesCount == 0 && res.IndxType != StrategySimType.CUSTOM)       // current simulation results (single, not custom)
                    {
                        IsRawColumnVisible = true;

                        int simStartDate = 0;
                        int simStartTime = 0;
                        if (!SelectedStratList.IsPortfolio && StrategiesLists[2].Strategies.FirstOrDefault() is SimStrategy strat)
                        {
                            simStartDate = strat.SignalStartDate;
                            simStartTime = strat.SignalStartTime;
                        }
                        GetTradesEntryExit(res, rdata, PosSizes, simStartDate, simStartTime, out entryValues, out exitvalues);

                        resPosSizes = GetPosSizesForStrategyResults(rdata.Dates, rdata.Times, PosSizes, res.SymbolType, res.PosSizeMode,
                            res.ReadonlyResEntryDates, res.ReadonlyResEntryTimes, entryValues, res.SL_ON, (float)res.SL_mult, res.EntryOnClose);
                    }

                    ObservableCollection<TradesListItem> trades = new ObservableCollection<TradesListItem>();

                    for (int i = 0; i < res.ReadonlyResults.Count; i++)  //cycle per trades
                    {
                        int t = res.ResTime(i);
                        int mode = res.ResMode(i);

                        trades.Add(new TradesListItem()
                        {
                            Index = i + 1,
                            Mode = mode,
                            EntryDT = Utils.ToDateTime(res.ReadonlyResEntryDates[i], res.ReadonlyResEntryTimes[i]),
                            ExitDT = Utils.ToDateTime(res.ReadonlyResDates[i], t),
                            EntryValue = entryValues == null ? 0 : entryValues[i],  // mode == 5 ? "cash"
                            ExitValue = exitvalues == null ? 0 : exitvalues[i],     // mode == 5 ? "cash"
                            PosSize = resPosSizes == null ? 0 : resPosSizes[i],     // mode == 5 ? "default"
                            Result = res.ReadonlyResults[i]
                        });

                        //todo: Nov 2020: check mistake. Coloring
/*#if DEBUG               
                        // check mistake 0.1%
                        if (entryValues != null && exitvalues != null && resPosSizes != null)
                        {
                            // NO currency conversion
                            float eps = res.IsLong ? exitvalues[i] - entryValues[i] : entryValues[i] - exitvalues[i];
                            eps *= resPosSizes[i] * res.ContractMultiplier;
                            eps = (eps - res.ReadonlyResults[i]) / res.ReadonlyResults[i];
                            if (eps < -0.001 || eps > 0.001)
                            {
                                trades.dataGridView1.Rows[trades.dataGridView1.RowCount - 1].DefaultCellStyle.BackColor = Color.Pink;
                                // trades.Text = "Mismatch: " + i;
                                Logger.Current.Warn("Strategy '{0}' have a mismatched trade N{1}: {2} ({3:F2}%)", res.Name, i, res.ReadonlyResEntryDates[i], eps * 100);
                            }
                        }
#endif*/
                    }


                    // show
                    var vm = new TradesListViewModel()
                    {
                        Trades = trades,
                        IsRawColumnVisible = IsRawColumnVisible,
                        Title = "Trades - " + res.Name
                    };
                    CreateDocument(nameof(TradesListView), vm);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Can't complete operation!\n\n" + ex.Message);
            }
            finally
            {
                //Cursor = Cursors.Default;
            }
        }
        public void ShowChart()
        {
            //Cursor = Cursors.WaitCursor;
            try
            {
                foreach (IReadonlySimStrategy res in SelectedStrategies)
                {
                    if (res == null) continue;

                    if (RAWdataStorage.GetRawData(Utils.SymbolsManager.GetSymbolFileNameMult(res.SymbolId, out _), res.SymbolId) is RAWdata rdata)
                    {
                        StockPointList bars = new StockPointList();
                        int startIdx = 0;
                        for (; startIdx < rdata.Dates.Length && (rdata.Dates[startIdx] < res.SignalStartDate || (rdata.Dates[startIdx] == res.SignalStartDate && rdata.Times[startIdx] < res.SignalStartTime)); startIdx++) ;

                        for (int i = startIdx; i < rdata.Dates.Length; i++)
                        {
                            if (rdata.Dates[i] > res.StopDate) break;
                            bars.Add(Security.ToDateTime(rdata.Dates[i], rdata.Times[i]).ToOADate(), rdata.Highs[i], rdata.Lows[i], rdata.Opens[i], rdata.Closes[i], rdata.Volumes[i]);
                        }

                        
                        if (res.Indx > 0)       // current simulation results
                        {
                            float[] PosSizes = null, Slippages = null, Commissions = null;
                            CalcPSforStrategy(res, ref PosSizes, ref Slippages, ref Commissions);

                            int simStartDate = 0;
                            int simStartTime = 0;
                            if (!SelectedStratList.IsPortfolio && StrategiesLists[2].Strategies.FirstOrDefault() is SimStrategy strat)
                            {
                                simStartDate = strat.SignalStartDate;
                                simStartTime = strat.SignalStartTime;
                            }

                            GetTradesEntryExit(res, rdata, PosSizes, simStartDate, simStartTime, out List<float> entryValues, out List<float> exitValues);

                            // show
                            GraphStock stock = new GraphStock(bars, res.ReadonlyResEntryDates, res.ReadonlyResEntryTimes, entryValues, res.ReadonlyResDates, res.GetResTimes(), exitValues);
                            var vm = GraphStockViewModel.Create();
                            vm.Title = $"Chart - {res.Name}";
                            vm.Graph = stock;
                            CreateDocument(nameof(GraphView), vm);
                        }
                        else
                        {
                            // show
                            GraphStock stock = new GraphStock(bars, null, null, null, null, null, null);
                            var vm = GraphStockViewModel.Create();
                            vm.Title = $"Chart - {res.Name}";
                            vm.Graph = stock;
                            CreateDocument(nameof(GraphView), vm);
                        }
                    }
                    else
                    {
                        ShowMessage("Show trades function can't identify strategy's raw data");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.Show("Can't complete operation!\n\n" + ex.Message);
            }
            finally
            {
                //Cursor = Cursors.Default;
            }
        }
        
        public void ShowLast15()
        {
            //Cursor = Cursors.WaitCursor;
            try
            {
                foreach (IReadonlySimStrategy res in SelectedStrategies)
                {
                    if (res == null) continue;

                    // if current simulation results
                    if (res.Indx > 0 && RAWdataStorage.GetRawData(Utils.SymbolsManager.GetSymbolFileNameMult(res.SymbolId, out _), res.SymbolId) is RAWdata rdata)
                    {
                        float[] PosSizes = null, Slippages = null, Commissions = null;
                        CalcPSforStrategy(res, ref PosSizes, ref Slippages, ref Commissions);

                        int simStartDate = 0;
                        int simStartTime = 0;
                        if (!SelectedStratList.IsPortfolio && StrategiesLists[2].Strategies.FirstOrDefault() is SimStrategy strat)
                        {
                            simStartDate = strat.SignalStartDate;
                            simStartTime = strat.SignalStartTime;
                        }

                        GetTradesEntryExit(res, rdata, PosSizes, simStartDate, simStartTime, out List<float> entryValues, out List<float> exitValues);

                        int last15index = Math.Max(0, res.ReadonlyResults.Count - 15);
                        int lastTradesCount = res.ReadonlyResults.Count - last15index;
                        List<StockPointList> last15StockLists = new List<StockPointList>();
                        List<PointPairList> last15Entries = new List<PointPairList>();
                        int startIdx = 0;

                        for (int t = last15index; t < res.ReadonlyResults.Count; t++)
                        {
                            for (; startIdx < rdata.Dates.Length && (rdata.Dates[startIdx] < res.ReadonlyResEntryDates[t] ||
                                (rdata.Dates[startIdx] == res.ReadonlyResEntryDates[t] && rdata.Times[startIdx] < res.ReadonlyResEntryTimes[t])); startIdx++) ;
                            startIdx = Math.Max(0, startIdx - 10); // show 10 bars before entry if possible

                            StockPointList stockPoints = new StockPointList();
                            PointPairList entryPoints = new PointPairList();
                            for (int j = startIdx, z = 1; j <= startIdx + 60 && j < rdata.Dates.Length; j++, z++)
                            {
                                stockPoints.Add(Security.ToDateTime(rdata.Dates[j], rdata.Times[j]).ToOADate(), rdata.Highs[j], rdata.Lows[j], rdata.Opens[j], rdata.Closes[j], rdata.Volumes[j]);
                                if (rdata.Dates[j] == res.ReadonlyResEntryDates[t] && rdata.Times[j] == res.ReadonlyResEntryTimes[t])
                                    entryPoints.Add(z, entryValues[t]);
                            }
                            last15StockLists.Add(stockPoints);
                            last15Entries.Add(entryPoints);
                        }

                        //
                        GraphLast15Trades gr = new GraphLast15Trades(last15StockLists, last15Entries);
                        var vm = GraphLast15ViewModel.Create();
                        vm.Title = $"Last 15 trades - {res.Name}";
                        vm.Graph = gr;
                        CreateDocument(nameof(GraphView), vm);
                    }
                    else
                    {
                        ShowMessage("Show last 15 trades function works on current simulation IN, OUT, ALL single results only");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.Show("Can't complete operation!\n\n" + ex.Message);
            }
            finally
            {
                //Cursor = Cursors.Default;
            }
        }

        public void Breakdown()
        {
            List<SimStrategy> active = new List<SimStrategy>();
            if (SelectedStratList.IsPortfolio)
                active.Add(GetPortfolioStrategyResult(SelectedCollection, SelectedStrategies.FirstOrDefault(), true));
            else
                active.AddRange(SelectedStrategies);

            foreach (IReadonlySimStrategy res in active)
            {
                if (res == null)
                    continue;

                List<PointPairList> lines = new List<PointPairList> {
                    new PointPairList(), new PointPairList(), new PointPairList(), new PointPairList()
                };

                DateTime dateTimeM = DateTime.MinValue, dateTimeQ = DateTime.MinValue, dateTimeY = DateTime.MinValue;
                for (int i = 0; i < res.ReadonlyResults.Count; i++)
                {
                    DateTime dateTime = Security.ToDateTime(res.ReadonlyResDates[i], 0);
                    lines[0].Add(dateTime.ToOADate(), res.ReadonlyResults[i]);

                    if (i > 0 && dateTimeM.Month == dateTime.Month && dateTimeM.Year == dateTime.Year)
                        lines[1].Last().Y += res.ReadonlyResults[i];
                    else
                        lines[1].Add(dateTime.AddDays(1 - dateTime.Day).ToOADate(), res.ReadonlyResults[i]);
                    dateTimeM = dateTime;

                    if (i > 0 && ((dateTimeQ.Month - 1) / 3) == ((dateTime.Month - 1) / 3) && dateTimeQ.Year == dateTime.Year)
                        lines[2].Last().Y += res.ReadonlyResults[i];
                    else
                        lines[2].Add(new DateTime(dateTime.Year, 1 + 3 * ((dateTime.Month - 1) / 3), 1).ToOADate(), res.ReadonlyResults[i]);
                    dateTimeQ = dateTime;

                    if (i > 0 && dateTimeY.Year == dateTime.Year)
                        lines[3].Last().Y += res.ReadonlyResults[i];
                    else
                        lines[3].Add(new DateTime(dateTime.Year, 1, 1).ToOADate(), res.ReadonlyResults[i]);
                    dateTimeY = dateTime;
                }

                // show
                FourGistForm gr = new FourGistForm(lines, 0);
                var vm = GraphBreakdownViewModel.Create();
                vm.Title = $"Breakdown - {res.Name}";
                vm.Graph = gr;
                CreateDocument(nameof(GraphView), vm);
            }
        }
        public void Seasonality()
        {
            List<SimStrategy> active = new List<SimStrategy>();
            if (SelectedStratList.IsPortfolio)
                active.Add(GetPortfolioStrategyResult(SelectedCollection, SelectedStrategies.FirstOrDefault(), true));
            else
                active.AddRange(SelectedStrategies);

            foreach (IReadonlySimStrategy res in active)
            {
                if (res == null)
                    continue;

                List<PointPairList> lines = new List<PointPairList> {
                    new PointPairList(), new PointPairList(), new PointPairList(), new PointPairList()
                };

                for (int i = 0; i < 5; i++) lines[0].Add(i, 0);
                for (int i = 0; i < 12; i++) lines[1].Add(i, 0);
                for (int i = 0; i < 4; i++) lines[2].Add(i, 0);

                DateTime dateTimeY = DateTime.MinValue;
                for (int i = 0; i < res.ReadonlyResults.Count; i++)
                {
                    DateTime dateTime = Security.ToDateTime(res.ReadonlyResEntryDates[i], 0);
                    if (dateTime.DayOfWeek == DayOfWeek.Saturday || dateTime.DayOfWeek == DayOfWeek.Sunday)
                        lines[0][(int)DayOfWeek.Friday - 1].Y += res.ReadonlyResults[i];    //catch weekend deals
                    else
                        lines[0][(int)dateTime.DayOfWeek - 1].Y += res.ReadonlyResults[i];  //normal way
                    lines[1][dateTime.Month - 1].Y += res.ReadonlyResults[i];
                    lines[2][(dateTime.Month - 1) / 3].Y += res.ReadonlyResults[i];

                    if (i > 0 && dateTimeY.Year == dateTime.Year)
                        lines[3].Last().Y += res.ReadonlyResults[i];
                    else
                        lines[3].Add(new DateTime(dateTime.Year, 1, 1).ToOADate(), res.ReadonlyResults[i]);
                    dateTimeY = dateTime;
                }

                // show
                FourGistForm gr = new FourGistForm(lines, 1);
                var vm = GraphBreakdownViewModel.Create();
                vm.Title = $"Seasonality - {res.Name}";
                vm.Graph = gr;
                CreateDocument(nameof(GraphView), vm);
            }
        }
        public void RandomizedOOS()
        {
            List<PointPairList> ppll = new List<PointPairList>();
            List<string> labels = new List<string>();
            IReadonlySimStrategy res = SelectedStratList.IsPortfolio ? GetPortfolioStrategyResult(SelectedCollection, SelectedStrategies.FirstOrDefault(), true) : SelectedStrategies.FirstOrDefault();
            if (res == null)
                return;

            //Cursor = Cursors.WaitCursor;
            try
            {
                #region Randomized OOS
                IReadonlySimStrategy resAll = null;
                {
                    if (res.Name.StartsWith("Cumulative") && SelectedStratList.IsPortfolio)
                    {//find at leat one ALL result in portfolio results
                        foreach (var r in SelectedCollection)
                            if (r.Enabled && (r.IndxType == StrategySimType.ALL || r.IndxType == StrategySimType.CUSTOM))
                            {
                                resAll = res; break;
                            }
                    }
                    else if (res.IndxType == StrategySimType.IN || res.IndxType == StrategySimType.OUT)
                    {//find all result
                        resAll = GetAllStrategy(res);
                    }
                    else if (res.IndxType == StrategySimType.ALL || res.IndxType == StrategySimType.CUSTOM)
                        resAll = res;
                }
                if (res != null && resAll.ReadonlyResDates != null && resAll.ReadonlyResDates.Count > 0)
                {
                    if (RAWdataStorage.GetRawData(Utils.SymbolsManager.GetSymbolFileNameMult(res.SymbolId, out _), res.SymbolId) is RAWdata rdata)
                    {
                        int[] Dates = rdata.Dates;
                        int[] Times = rdata.Times;

                        // calc oos start date
                        bool oosEnd = resAll.OosLocation == 1;
                        int dates_count = 0;
                        int startInd = 0;
                        int std = resAll.SignalStartDate;
                        int stt = resAll.SignalStartTime;
                        int stl = /*stopDate > 0 ? stopDate :*/ resAll.ReadonlyResDates.Last();
                        while (startInd < Dates.Length && (Dates[startInd] < std || (Dates[startInd] == std && Times[startInd] < stt))) startInd++;
                        while (startInd + dates_count < Dates.Length && Dates[startInd + dates_count] <= stl) dates_count++;

                        List<float> OrigIn = new List<float>();
                        List<float> OrigOut = new List<float>();
                        if (oosEnd)
                        {
                            int oos_start = (int)(startInd + dates_count * (1.0 - resAll.OutOfSample / 100.0));

                            for (int i = 0; i < resAll.ReadonlyResults.Count; i++)
                            {
                                if (SimStrategy.CompareDateTimes(resAll.ReadonlyResDates[i], resAll.ResTime(i), Dates[oos_start], Times[oos_start]) < 0)
                                    OrigIn.Add(resAll.ReadonlyResults[i]);
                                else
                                    OrigOut.Add(resAll.ReadonlyResults[i]);
                            }
                        }
                        else
                        {
                            int oos_end = (int)(startInd + dates_count * resAll.OutOfSample / 100.0);

                            for (int i = 0; i < resAll.ReadonlyResults.Count; i++)
                            {
                                if (SimStrategy.CompareDateTimes(resAll.ReadonlyResDates[i], resAll.ResTime(i), Dates[oos_end], Times[oos_end]) < 0)
                                    OrigOut.Add(resAll.ReadonlyResults[i]);
                                else
                                    OrigIn.Add(resAll.ReadonlyResults[i]);
                            }
                        }

                        //  Plot Originals and All
                        ppll.Add(GetEquityCurvePointList(OrigIn));
                        labels.Add(oosEnd ? "51OrigIn" : "52OrigOut");
                        ppll.Add(GetEquityCurvePointList(OrigOut));
                        labels.Add(oosEnd ? "52OrigOut" : "51OrigIn");
                        ppll.Add(GetEquityCurvePointList(resAll.ReadonlyResults));
                        labels.Add("53OrigAll");

                        // --------------------------------------------
                        //  get random OOS
                        Random rnd = new Random(DateTime.Now.Millisecond);
                        for (int k = 0; k < 100; k++)
                        {
                            List<string> random_oos = new List<string>();
                            while (random_oos.Count < dates_count * resAll.OutOfSample / 100.0)
                            {
                                int idx = startInd + rnd.Next(dates_count);
                                string s = string.Format("{0}{1}", Dates[idx], Times[idx]);
                                if (!random_oos.Contains(s))
                                    random_oos.Add(s);
                            }

                            //   in sample
                            List<float> InRet = new List<float>();
                            for (int i = 0; i < resAll.ReadonlyResults.Count/*Closes.Count*/; i++)
                            {
                                if (!random_oos.Contains(string.Format("{0}{1}", resAll.ReadonlyResDates[i], resAll.ResTime(i))))
                                {
                                    if (InRet.Count >= OrigIn.Count)
                                        continue;
                                    InRet.Add(resAll.ReadonlyResults[i]);
                                }
                            }

                            //  OOS
                            List<float> OutRet = new List<float>();
                            for (int i = 0; i < resAll.ReadonlyResults.Count/*Closes.Count*/; i++)
                            {
                                if (random_oos.Contains(string.Format("{0}{1}", resAll.ReadonlyResDates[i], resAll.ResTime(i))))
                                {
                                    if (OutRet.Count >= OrigOut.Count)
                                        continue;
                                    OutRet.Add(resAll.ReadonlyResults[i]);
                                }
                            }

                            ppll.Add(GetEquityCurvePointList(InRet));
                            labels.Add(oosEnd ? (k == 0 ? "51InRet" : "51") : (k == 0 ? "52OutRet" : "52"));
                            ppll.Add(GetEquityCurvePointList(OutRet));
                            labels.Add(oosEnd ? (k == 0 ? "52OutRet" : "52") : (k == 0 ? "51InRet" : "51"));
                        }

                        //-------
                        GraphRobustness gr = new GraphRobustness(ppll, labels, null, true);
                        var vm = GraphRandOOSViewModel.Create();
                        vm.Title = $"Randomized OOS - {res.Name}";
                        vm.Graph = gr;
                        CreateDocument(nameof(GraphView), vm);
                    }
                    else
                    {
                        ShowMessage("Randomized OOS function can't identify strategy's raw data");
                    }
                }
                else
                    ShowMessage("No \"All\" strategy results found");
                #endregion
            }
            catch (Exception ex)
            {
                MessageBoxService.Show("Can't complete operation!\n\n" + ex.Message);
            }
            finally
            {
                //Cursor = Cursors.Default;
            }
        }
        public void ReRun()
        {
            IReadonlySimStrategy res = SelectedStrategies.FirstOrDefault();
            if (res == null) return;

            if (res.Indx > 0)   // current simulation result
            {
                if (res.IndxType == StrategySimType.IN || res.IndxType == StrategySimType.OUT)
                    res = GetAllStrategy(res);

                if (res == null)
                {
                    ShowMessage("Re-Run can't find ALL result for selected strategy");
                    return;
                }

                List<SymbolId> symbols = new List<SymbolId>();
                if (res.ReadonlyChildren != null && res.ReadonlyChildren.Count() > 0)
                    symbols = res.ReadonlyChildren.Select(x => x.SymbolId).ToList();
                else
                    symbols.Add(res.SymbolId);

                var vm = SimTaskWizardViewModel.Create();
                vm.IsReRun = true;
                vm.SetReRunParams(res.TradeMode, symbols, res.ReadonlyMarketSymbolsIds, res.SignalStartDate, res.StopDate, res.PT_ON, (float)res.PT_mult, 0,
                        res.SL_ON, (float)res.SL_mult, 0, res.TL_ON, res.TL_mult, 0, res.HH_ON, res.HHlook, 0,
                        res.LL_ON, res.LLlook, 0, res.Fitness, res.MaxTime, 0, res.ProfX, 0, 1, 1, res.OutOfSample,
                        res.ReadonlyEntrySignals, res.ReadonlyExitSignals);
                DialogServiceReRun.ShowDialog(null, "Re-Run", nameof(SimTaskWizardView), vm);
            }
            else
                ShowMessage("Re-Run works on current simulation IN, OUT, ALL results only");
        }
        public void AddRule()
        {
            IReadonlySimStrategy res = SelectedStrategies.FirstOrDefault();
            if (res == null) return;

            if (res.Indx > 0)   //current simulation result
            {
                if (res.IndxType ==  StrategySimType.IN || res.IndxType == StrategySimType.OUT)
                {
                    IReadonlySimStrategy res2 = StrategiesLists[2].Strategies.FirstOrDefault(x => x.IndxType == StrategySimType.ALL && x.Indx == res.Indx);
                    if (res2 != null && res.Symbol != res2.Symbol)
                        res = res2.ReadonlyChildren?.FirstOrDefault(x => x.Symbol == res.Symbol);
                    else
                        res = res2;
                }

                if (res == null || res.EntrySignalsValues == null)
                {
                    ShowMessage("AddRule function didn't find corresponding All result for selected strategy");
                    return;
                }

                IEnumerable<IReadonlySimStrategy> resList = res.ReadonlyChildren;
                if (resList == null || resList.Count() == 0) resList = new List<IReadonlySimStrategy>() { res };

                List<SymbolId> symbolIds = resList.Select(x => x.SymbolId).ToList();

                //-----------------------------------------------------------------
                AddRuleViewModel vm = AddRuleViewModel.Create();

                // add base signals
                SignalGroup gr = new SignalGroup("Base", -1) { IsExpanded = true };
                int ins = 0;
                if (res.ReadonlyEntrySignals != null)
                    foreach (var s in res.ReadonlyEntrySignals)
                    {
                        var sig = s.MakeClone();
                        sig.GroupId = gr;
                        sig.ActiveForEntry = true;
                        sig.IsRequired = true;
                        sig.CanEdit = false;
                        vm.Signals.Insert(ins++, sig);
                        gr.CountEntry++;
                    }
                if (res.ReadonlyExitSignals != null)
                    foreach (var s in res.ReadonlyExitSignals)
                    {
                        var sig = s.MakeClone();
                        sig.GroupId = gr;
                        sig.ActiveForExit = true;
                        sig.CanEdit = false;
                        vm.Signals.Insert(ins++, sig);
                        gr.CountExit++;
                    }

                // simulation settings
                DateTime SessionEndTime = DateTime.MinValue.AddHours(res.SessionEndTime / 100).AddMinutes(res.SessionEndTime % 100);

                vm.SimSettings = new SimulationSettings(null,
                    Utils.ToDateTime(res.SignalStartDate, res.SignalStartTime),
                    Utils.ToDateTime(res.StopDate, 235959),
                    true, false, symbolIds, null, symbolIds[0], null, null, null, 0,
                    0, res.TradeMode, res.PT_ON, res.SL_ON, res.TL_ON, (float)res.PT_mult, (float)res.SL_mult, res.TL_mult,
                    res.HH_ON, res.LL_ON, res.HHlook, res.LLlook, res.ATR_len, res.PosSizeMode,
                    res.SlippageMode, res.CommissionMode, res.Commission, res.Slippage, currentCurrency, res.AccountValue,
                    res.ReadonlyMarketSymbolsIds.ToList(), res.InvestCash, res.MaxTime, res.ProfX,
                    res.Fitness, res.OosLocation, res.OutOfSample / 100.0f, 1, 1, res.EntryOnClose, res.ExitOnClose,
                    res.ForceExitOnSessionEnd ? (short)1 : (short)0, SessionEndTime, res.DelayedEntry, res.RebalanceMethod,
                    res.RebalancePeriod, res.RebalanceSymbolsCount, 0, 0, 0, 3, false, 0, 0, 0, 0, 0, 0, 0);

                vm.simStrategy_internal = res;

                DialogServiceAddRule.ShowDialog(null, "Add Rule", nameof(AddRuleView), vm);
            }
            else
                ShowMessage("AddRule function works on current simulation IN, OUT, ALL results only");
        }
        public void Optimization()
        {
            IReadonlySimStrategy res = SelectedStrategies.FirstOrDefault();
            if (res == null) return;
            StrategySimType originalMode = res.IndxType;

            if (res.IndxType == StrategySimType.CUSTOM)
            {
                ShowMessage("Optimization function can't process custom strategies");
                return;
            }
            if (res.Indx == 0)   //current simulation result only
            {
                ShowMessage("Optimization function works on current simulation IN, OUT, ALL results only");
                return;
            }
            if (res.IndxType == StrategySimType.IN || res.IndxType == StrategySimType.OUT)
            {
                res = GetAllStrategy(res);
            }
            if (res == null)
            {
                ShowMessage("Optimization function didn't find corresponding All result for selected strategy");
                return;
            }
            if (res.ReadonlyEntrySignals?.Count > 0 || res.ReadonlyExitSignals?.Count > 0)
            {
                var vm = OptimizationViewModel.Create(res, originalMode);
                WindowServiceOptimize.Show(nameof(OptimizationView), vm);
            }
            else
                ShowMessage("Selected strategy has no availablve signals for optimization");
        }

        public async Task RowDoubleClick(RowDoubleClickEventArgs args)
        {
            if(args.ChangedButton == System.Windows.Input.MouseButton.Left && args.HitInfo.InRow && args.HitInfo.Column?.FieldName != "Enabled")
            {
                await EquityCurve();
            }
        }

        public void Loaded()
        {
            if(IsContinuous)
            {
                IsContinuous = IsContinuousRunning = Simulator.ContinuousSimThread != null;
                Simulator.StaticPropertyChanged += Simulator_StaticPropertyChanged;
                ContinuousTimer.Start();
            }
        }

        public async Task Unloaded()
        {
            if(Simulator.ContinuousSimThread != null && Simulator.ContinuousSimThread.IsAlive)
            {
                await Task.Run(() =>
                {
                    CppSimulatorInterface.CancelContinuousSimulation();
                    Simulator.ContinuousSimThread.Join();
                    Simulator.ContinuousSimThread = null;

                    CppSimulatorInterface.ContinuousClean();
                });
            }
            Simulator.StaticPropertyChanged -= Simulator_StaticPropertyChanged;
        }

        public void ContinuousRun()
        {
            if(IsContinuousRunning == false && Simulator.ContinuousSimThread != null && Simulator.ContinuousSimThread.IsAlive)
            {
                CppSimulatorInterface.PauseContinuousSimulation(0);
                IsContinuousRunning = true;
            }
        }
        public void ContinuousPause()
        {
            if(IsContinuousRunning && Simulator.ContinuousSimThread!=null && Simulator.ContinuousSimThread.IsAlive)
            {
                CppSimulatorInterface.PauseContinuousSimulation(1);
                ContinuousPerformance = 0;
                ContinuousPerfString = "0 strategies/sec";
                IsContinuousRunning = false;
            }
        }
        public async Task ContinuousStop()
        {
            if (MessageBoxService.Show("Do you really want to stop continuous simulation?", "Continuous simulation",MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                await Task.Run(() =>
                {
                    CppSimulatorInterface.CancelContinuousSimulation();
                    Simulator.ContinuousSimThread.Join();
                    Simulator.ContinuousSimThread = null;
                });
                IsContinuousRunning = IsContinuous = false;
            }
        }
        #endregion

        private void Simulator_StaticPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (IsContinuous)
            {
                IsContinuous = IsContinuousRunning = Simulator.ContinuousSimThread != null;
            }
        }

        private void ShowMessage(string msg)
        {
            //toolTip1.Show(msg, this, 10, Height - 50, 2000);
            MessageBoxService.Show(msg);
        }

        private static PointPairList GetBuyAndHoldLine(IReadonlySimStrategy res, string displayCurrency, int countMult, out float[] dailyTrades, bool calcDailyPnlsOnly = false)
        {
            dailyTrades = SymbolsManager.GetBuyAndHold(res.SymbolId, res.SymbolType, res.TradeMode, res.PosSizeMode, res.SL_ON, (float)res.SL_mult, res.AccountValue, res.ATR_len,
                res.ReadonlyResDates[0], res.ReadonlyResDates.Last(), res.SessionEndTime * 100, res.Currency, displayCurrency, countMult, out var tradeDates, calcDailyPnlsOnly);

            if (calcDailyPnlsOnly || dailyTrades == null || tradeDates == null) return null;

            //construct equity curve
            return GetEquityCurvePointList(dailyTrades, tradeDates);
        }

        private static PointPairList GetEquityCurvePointList(IReadOnlyList<float> values)
        {
            if (values == null) return null;

            PointPairList ppl = new PointPairList();
            double sum = 0;
            for (int j = 0; j < values.Count; j++)
            {
                sum += values[j];
                ppl.Add(j, sum);
            }
            return ppl;
        }

        private static PointPairList GetEquityCurvePointList(IReadOnlyList<float> values, IReadOnlyList<int> dates)
        {
            if (values == null || dates == null || values.Count != dates.Count) return null;

            PointPairList ppl = new PointPairList();
            double sum = 0;
            for (int j = 0; j < values.Count; j++)
            {
                sum += values[j];
                ppl.Add(j, sum, Security.ToDateTime(dates[j], 0).ToOADate());
            }
            return ppl;
        }

        private static PointPairList GetEquityCurvePointList(IReadOnlyList<float> values, IReadOnlyList<int> dates, IReadOnlyList<int> times)
        {
            if (values == null || dates == null || times == null || values.Count != dates.Count || values.Count != times.Count) return null;

            PointPairList ppl = new PointPairList();
            double sum = 0;
            for (int j = 0; j < values.Count; j++)
            {
                sum += values[j];
                ppl.Add(j, sum, Security.ToDateTime(dates[j], times[j]).ToOADate());
            }
            return ppl;
        }

        private SimStrategy GetPortfolioStrategyResult(IList<SimStrategy> portfolioResults, SimStrategy selectedStrat, bool isCumulative)
        {
            List<SimStrategy> activeResults = null;
            SimStrategy res = null;

            try
            {   // get first active strategy from portfolio
                activeResults = portfolioResults.Where(x => x.Enabled).ToList();
                foreach (var v in portfolioResults.Where(x => x.ChildStrategiesCount > 0))
                    activeResults.AddRange(v.Children.Where(c => c.Enabled));

                if (selectedStrat != null && selectedStrat.Enabled)
                    res = selectedStrat;
                else
                    res = activeResults.First();
            }
            catch
            {
                ShowMessage("There are no enabled strategies in the portfolio");
                return null;
            }
            if (isCumulative == false) return res;    //return first active portfolio result

            // create cumulative strategy
            res = res.Clone() as SimStrategy;
            int activeCount = 0;
            Dictionary<int, float> cumulative = new Dictionary<int, float>();
            foreach (var r in activeResults)
            {
                activeCount++;
                //identify minimal start date and time
                if (SimStrategy.CompareDateTimes(res.SignalStartDate, res.SignalStartTime, r.SignalStartDate, r.SignalStartTime) > 0)
                {
                    res.SignalStartDate = r.SignalStartDate;
                    res.SignalStartTime = r.SignalStartTime;
                }
                res.StopDate = Math.Max(res.StopDate, r.StopDate);      //identify maximum stop date
                for (int i = 0; i < r.results.Length; i++)
                    if (cumulative.ContainsKey(r.resDates[i]))          //no need to check Time, because portfolio results summed intraday
                        cumulative[r.resDates[i]] += r.results[i];
                    else
                        cumulative.Add(r.resDates[i], r.results[i]);
            }

            if (cumulative.Count < 1)
            {
                ShowMessage("There are no cumulative results in the portfolio");
                return null;
            }
            List<int> keys = cumulative.Keys.ToList();
            keys.Sort();
            float[] valuesSortedByDate = new float[cumulative.Count];
            for (int i = 0; i < keys.Count; ++i)
                valuesSortedByDate[i] = cumulative[keys[i]];

            res.results = valuesSortedByDate;
            res.resDates = keys.ToArray();
            res.resEntryDates = keys.ToArray();
            int[] times = new int[keys.Count];
            for (int i = 0; i < keys.Count; i++) times[i] = 170000;
            res.resEntryTimes = times;
            res.SetResTimes(times);
            res.EntrySignalsValues = new BitArray(0);
            res.daily_pnls = new float[0];
            res.SignalsCount = 0;

            List<float> w_trades = new List<float>();
            List<float> l_trades = new List<float>();
            float trade = 0;

            for (int i = 0; i < valuesSortedByDate.Length; i++)
            {
                trade = valuesSortedByDate[i];
                if (trade > 0) w_trades.Add(trade);
                else l_trades.Add(-trade);
            }

            //calc params
            res.Mean_w_trades = (w_trades.Count == 0 ? 0 : (w_trades.Sum() / w_trades.Count));
            res.Mean_l_trades = (l_trades.Count == 0 ? 0 : (l_trades.Sum() / l_trades.Count));
            res.WinPercentage = (valuesSortedByDate.Length <= 1 ? 0 : (w_trades.Count / (double)(valuesSortedByDate.Length - 1)));

            for (int i = 0; i < w_trades.Count; i++)
                w_trades[i] *= w_trades[i];
            for (int i = 0; i < l_trades.Count; i++)
                l_trades[i] *= l_trades[i];

            res.Std_w_trades = Math.Sqrt(w_trades.Sum() / w_trades.Count - res.Mean_w_trades * res.Mean_w_trades);
            res.Std_l_trades = Math.Sqrt(l_trades.Sum() / l_trades.Count - res.Mean_l_trades * res.Mean_l_trades);

            res.Name = $"Cumulative ({activeCount})";
            return res;
        }

        public static void ERatioForStrategy(IReadonlySimStrategy res, out List<float> ERatios, out List<float> randERatios)
        {
            if (res.ChildStrategiesCount == 0)
            {
                ERatio er = new ERatio(Utils.SymbolsManager.GetSymbolFileNameMult(res.SymbolId, out _), res.EntrySignalsValues, res.SignalStartDate, res.SignalStartTime, res.StopDate,
                    (res.OosLocation == 1 && res.IndxType == StrategySimType.OUT) || (res.OosLocation == 0 && res.IndxType == StrategySimType.IN));
                er.Update(res.IsLong, out ERatios, out randERatios);
            }
            else   //basket
            {
                ERatios = randERatios = null;

                foreach (var ch in res.ReadonlyChildren)
                {
                    ERatio er = new ERatio(Utils.SymbolsManager.GetSymbolFileNameMult(ch.SymbolId, out _), ch.EntrySignalsValues,
                    ch.SignalStartDate, ch.SignalStartTime, ch.StopDate,
                    (ch.OosLocation == 1 && ch.IndxType == StrategySimType.OUT) || (ch.OosLocation == 0 && ch.IndxType == StrategySimType.IN));
                    er.Update(res.IsLong, out List<float> chER, out List<float> randChER);

                    if (ERatios == null)
                    {
                        ERatios = chER;
                        randERatios = randChER;
                    }
                    else
                    {
                        for (int j = 0; j < ERatios.Count && j < chER.Count; ++j) ERatios[j] += chER[j];
                        for (int j = 0; j < randERatios.Count && j < randChER.Count; ++j) randERatios[j] += randChER[j];
                    }
                }
                for (int j = 0; j < ERatios.Count; ++j) ERatios[j] /= res.ChildStrategiesCount;
                for (int j = 0; j < randERatios.Count; ++j) randERatios[j] /= res.ChildStrategiesCount;
            }
        }

        public static float ERatioValueForStrategy(IReadonlySimStrategy res)
        {
            //todo: check signal existance
            ERatioForStrategy(res, out List<float> ERatios, out _);
            if (ERatios.Count > 10)
                return ERatios.Take(10).Max();
            else if (ERatios.Count > 0)
                return ERatios.Max();
            else
                return 0;
        }

        public static void ExpScorePerfectProfitForStrategy(IReadonlySimStrategy res, string targetCurrency,
            out List<float> expScore, out List<float> perfectProfitPercentage, out List<float> perfectProfitCorrelation)
        {
            expScore = new List<float>();
            perfectProfitPercentage = new List<float>();
            perfectProfitCorrelation = new List<float>();

            List<float> trades = new List<float>();
            List<float> w_trades = new List<float>();
            List<float> l_trades = new List<float>();
            float trade = 0, sum = 0;
            if (res.ChildStrategiesCount == 0)  //single
            {
                if (RAWdataStorage.GetRawData(Utils.SymbolsManager.GetSymbolFileNameMult(res.SymbolId, out _), res.SymbolId) is RAWdata rdata)
                {
                    var Dates = rdata.Dates;
                    var Times = rdata.Times;
                    var Opens = rdata.Opens;
                    var Closes = rdata.Closes;

                    float[] posSizes = null, slip = null, comm = null;
                    CalcPSforStrategy(res, ref posSizes, ref slip, ref comm);  // need calculate PS because SL can be different

                    // perfect profit
                    float[] PP = new float[Dates.Length];
                    if (PP.Length > 0) PP[0] = 0;
                    for (int i = 1, i_prev = 0; i < Dates.Length; i_prev = i++)
                    {
                        float pp_entry = res.EntryOnClose == 0 ? Opens[i] : Closes[i_prev];
                        float pp_exit = (res.ExitOnClose == 0 && i + 1 < Dates.Length) ? Opens[i + 1] : Closes[i];

                        if (res.IsLong)
                            PP[i] = pp_exit > pp_entry ? (pp_exit - pp_entry) * res.ContractMultiplier * posSizes[i_prev] : 0;
                        else
                            PP[i] = pp_exit < pp_entry ? (pp_entry - pp_exit) * res.ContractMultiplier * posSizes[i_prev] : 0;
                    }

                    // currency conversion
                    int r = CurrencyConvertor.GetConversationRulesForSymbol(res.SymbolId, targetCurrency, out string[] convRules, Utils.SymbolsManager);
                    if (r != 0)
                        //MessageBoxService.Show(res.Symbol + " to " + targetCurrency + ". No available currency convertor found! Error code: " + r);
                        Logger.Current.Warn(res.Symbol + " to " + targetCurrency + ". No available currency convertor found! Error code: " + r);

                    CurrencyConvertor.ConvertCurrencyInList(convRules, Dates, PP, false);

                    float sumPP = 0, sumPP2 = 0, sumT = 0, sumT2 = 0, sumPPxT = 0;
                    float perfectProfitValue = 0;

                    int start = 0, count = 0;
                    while (start < Dates.Length && SimStrategy.CompareDateTimes(Dates[start], Times[start], res.SignalStartDate, res.SignalStartTime) < 0) start++;

                    for (int i = start, t = 0; i < Dates.Length && count < res.SignalsCount && t < res.TradesCount; ++i, ++count)
                    {
                        perfectProfitValue += PP[i];

                        //todo: в оригинале нужно использовать "==". Однако в случае отсутствия соответствия делать что-то (делить на кол-во сработавших?)
                        // или использовать ">=". (В перву. очередь это касается кастомных стратегий)
                        if (SimStrategy.CompareDateTimes(Dates[i], Times[i], res.ReadonlyResDates[t], res.ResTime(t)) >= 0)
                        {
                            trade = res.ReadonlyResults[t];
                            trades.Add(trade);
                            if (trade > 0) w_trades.Add(trade);
                            else l_trades.Add(-trade);
                            sum += trade;
                            double es = SimStrategy.GetExpectancyScore(count, trades.Count, w_trades.Count == 0 ? 0 : w_trades.Average(),
                                l_trades.Count == 0 ? 0 : l_trades.Average(), w_trades.Count / (double)trades.Count);
                            expScore.Add((float)es);                                       //expectancyScore

                            sumPP += perfectProfitValue;
                            sumPP2 += perfectProfitValue * perfectProfitValue;
                            sumT += sum;
                            sumT2 += sum * sum;
                            sumPPxT += perfectProfitValue * sum;

                            if (perfectProfitValue != 0)
                                perfectProfitPercentage.Add(Math.Min(100, 100 * sum / perfectProfitValue));     // perfectProfit percentage
                            else perfectProfitPercentage.Add(0);

                            int n = trades.Count;
                            int n2 = n * n;
                            double stdPP = Math.Sqrt(sumPP2 / n - sumPP * sumPP / n2);
                            double stdT = Math.Sqrt(sumT2 / n - sumT * sumT / n2);
                            double covariance = (sumPPxT / n - sumPP * sumT / n2);
                            if (stdPP != 0 && stdT != 0)
                                perfectProfitCorrelation.Add((float)(covariance / stdPP / stdT));            // perfectProfit Correlation
                            else perfectProfitCorrelation.Add(0);

                            t++;
                        }
                    }
                }
            }
            else   // basket
            {
                List<RAWdata> syms = new List<RAWdata>();
                List<float[]> PPs = new List<float[]>();
                List<Trade> sortedTrades = new List<Trade>();
                List<int> indexes = new List<int>();
                foreach (var ch in res.ReadonlyChildren)
                    if (RAWdataStorage.GetRawData(Utils.SymbolsManager.GetSymbolFileNameMult(ch.SymbolId, out _), ch.SymbolId) is RAWdata rdata)
                    {
                        syms.Add(rdata);

                        float[] psl = null, slip = null, comm = null;
                        CalcPSforStrategy(ch, ref psl, ref slip, ref comm);  // need calculate PS because SL can be different

                        // perfect profit
                        float[] PP = new float[syms.Last().Dates.Length];
                        if (PP.Length > 0) PP[0] = 0;
                        for (int i = 1, i_prev = 0; i < syms.Last().Dates.Length; i_prev = i++)
                        {
                            float pp_entry = res.EntryOnClose == 0 ? syms.Last().Opens[i] : syms.Last().Closes[i_prev];
                            float pp_exit = (res.ExitOnClose == 0 && i + 1 < syms.Last().Dates.Length) ? syms.Last().Opens[i + 1] : syms.Last().Closes[i];

                            if (res.IsLong)
                                PP[i] = pp_exit > pp_entry ? (pp_exit - pp_entry) * ch.ContractMultiplier * psl[i_prev] : 0;
                            else
                                PP[i] = pp_exit < pp_entry ? (pp_entry - pp_exit) * ch.ContractMultiplier * psl[i_prev] : 0;
                        }

                        // currency conversion
                        int r = CurrencyConvertor.GetConversationRulesForSymbol(ch.SymbolId, targetCurrency, out string[] convRules, Utils.SymbolsManager);
                        if (r != 0)
                            //MessageBoxService.Show(ch.Symbol + " to " + targetCurrency + ". No available currency convertor found! Error code: " + r);
                            Logger.Current.Warn(ch.Symbol + " to " + targetCurrency + ". No available currency convertor found! Error code: " + r);

                        CurrencyConvertor.ConvertCurrencyInList(convRules, syms.Last().Dates, PP, false);
                        PPs.Add(PP);

                        // add trades for current symbol
                        for (int i = 0; i < ch.TradesCount; ++i)
                            sortedTrades.Add(ch.GetTrade(i));

                        // find start indexes for each symbol
                        int start = 0;
                        while (start < syms.Last().Dates.Length &&
                            SimStrategy.CompareDateTimes(syms.Last().Dates[start], syms.Last().Times[start], ch.SignalStartDate, ch.SignalStartTime) < 0) start++;
                        indexes.Add(start);
                    }

                //sort trades by exit date
                sortedTrades = sortedTrades.OrderBy(x => x.exitDT).ToList();

                float sumPP = 0, sumPP2 = 0, sumT = 0, sumT2 = 0, sumPPxT = 0;
                float perfectProfitValue = 0;
                int count = 0;

                // cycle through trades
                for (int t = 0; t < sortedTrades.Count; ++t)
                {
                    for (int s = 0; s < syms.Count; ++s)
                    {
                        int iii = indexes[s];
                        while (iii < syms[s].Dates.Length && Security.ToDateTime(syms[s].Dates[iii], syms[s].Times[iii]) < sortedTrades[t].exitDT)
                        {
                            perfectProfitValue += PPs[s][iii];
                            iii++;
                            count++;
                        }
                        indexes[s] = iii;
                    }

                    trade = sortedTrades[t].px;
                    trades.Add(trade);
                    if (trade > 0) w_trades.Add(trade);
                    else l_trades.Add(-trade);
                    sum += trade;
                    double es = SimStrategy.GetExpectancyScore(count, trades.Count, w_trades.Count == 0 ? 0 : w_trades.Average(),
                        l_trades.Count == 0 ? 0 : l_trades.Average(), w_trades.Count / (double)trades.Count);
                    expScore.Add((float)es);                                       //expectancyScore

                    sumPP += perfectProfitValue;
                    sumPP2 += perfectProfitValue * perfectProfitValue;
                    sumT += sum;
                    sumT2 += sum * sum;
                    sumPPxT += perfectProfitValue * sum;

                    if (perfectProfitValue != 0)
                        perfectProfitPercentage.Add(Math.Min(100, 100 * sum / perfectProfitValue));     // perfectProfit percentage
                    else perfectProfitPercentage.Add(0);

                    int n = trades.Count;
                    int n2 = n * n;
                    double stdPP = Math.Sqrt(sumPP2 / n - sumPP * sumPP / n2);
                    double stdT = Math.Sqrt(sumT2 / n - sumT * sumT / n2);
                    double covariance = (sumPPxT / n - sumPP * sumT / n2);
                    if (stdPP != 0 && stdT != 0)
                        perfectProfitCorrelation.Add((float)(covariance / stdPP / stdT));            // perfectProfit Correlation
                    else perfectProfitCorrelation.Add(0);
                }
            }
        }

        private static void CalcPSforStrategy(IReadonlySimStrategy res, ref float[] PosSizes, ref float[] Slippages, ref float[] Commissions)
        {
            int[] bhDates, bhTimes;
            float[] bhOpens, bhHighs, bhLows, bhCloses;

            string RAW_file_name = Utils.SymbolsManager.GetSymbolFileNameMult(res.SymbolId, out double mult);
            if (mult == 0) mult = 1;

            RAWdata raw = RAWdataStorage.GetRawData(RAW_file_name, res.SymbolId);
            if (raw != null)   //reading successfull
            {
                bhDates = raw.Dates; bhTimes = raw.Times; bhOpens = raw.Opens; bhHighs = raw.Highs; bhLows = raw.Lows; bhCloses = raw.Closes;
            }
            else
            {
                PosSizes = Slippages = Commissions = null;
                return;
            }

            // Calc posSizes, slippages and commissions for raw data
            double margin = Utils.SymbolsManager.GetSymbolMargin(res.SymbolId);

            PosSizes = GenerateSignalsToTrade.CalcPositionSizes(res.PosSizeMode, res.SymbolId, res.ContractMultiplier, margin, res.AccountValue, res.Currency,
                bhDates, bhCloses, GenerateSignalsToTrade.ATR_Func(bhHighs, bhLows, bhCloses, res.ATR_len), out float LastConversion, Utils.SymbolsManager);

            float[] PosSizesSL = PosSizes.Select(x => GenerateSignalsToTrade.FloorAndTrimPosSize(x, res.PosSizeMode, res.SymbolType, res.SL_ON, (float)res.SL_mult)).ToArray();
            Slippages = GenerateSignalsToTrade.CalcSlippage(res.SlippageMode, res.PosSizeMode, res.Slippage, res.SymbolId, /*res.AccountValue,*/ res.Currency,
                bhDates, PosSizesSL, Utils.SymbolsManager.GetSymbolType(res.SymbolId));
            Commissions = GenerateSignalsToTrade.CalcCommissions(res.CommissionMode, res.PosSizeMode, res.Commission, res.SymbolId, margin, res.Currency, res.EntryOnClose, bhDates,
                (res.EntryOnClose == 1 ? bhCloses : bhOpens), PosSizesSL, Utils.SymbolsManager);
        }

        private static Task<PointPairList> CalcHistogram(IEnumerable<double> values, int stepInPercent)
        {
            return Task.Run(() =>
            {
                PointPairList ppl = new PointPairList();

                IEnumerable<double> ie = values.Where(x => !double.IsInfinity(x) && x > double.MinValue && x < double.MaxValue);
                if (ie.Count() == 0) return ppl;
                double min = ie.Min();
                double max = ie.Max();
                double step = (max - min) * stepInPercent / 100.0;
                int count = 100 / stepInPercent;
                if (count * stepInPercent < 100) count++;

                for (int i = 0; i < count; i++)
                    ppl.Add(min + step * i, 0);

                foreach (double d in values)
                {
                    if (double.IsInfinity(d))
                        continue;
                    else if (d == max)
                        ppl.Last().Y++;
                    else
                        ppl[Math.Max((int)((d - min) / step), 0)].Y++;
                }

                ppl.Insert(0, min, 0);
                for (int i = 0, i2 = 2; i < count - 1; i++, i2 += 2)
                    ppl.Insert(i2, ppl[i2].X, ppl[i2 - 1].Y);
                ppl.Add(max, ppl.Last().Y);
                ppl.Add(max, 0);

                return ppl;
            });
        }
        
        private List<PointPairList> MCconsecutive(IList<PointPairList> pointsList)
        {
            if (pointsList == null || pointsList.Count == 0) return null;

            var avMaxWin = new PointPairList();
            var maxConsecWin = new PointPairList();
            var avConsecWin = new PointPairList();
            var avMaxLoss = new PointPairList();
            var maxConsecLoss = new PointPairList();
            var avConsecLoss = new PointPairList();

            int[] ConsecWinsPerSample = new int[pointsList.Count];
            int[] ConsecLossPerSample = new int[pointsList.Count];
            int[] maxConsecWinsPerSample = new int[pointsList.Count];
            int[] maxConsecLossPerSample = new int[pointsList.Count];
            double[] maxWinPerSample = new double[pointsList.Count];
            double[] maxLosPerSample = new double[pointsList.Count];

            for (int i = 0; i < pointsList[0].Count; ++i)  // cycle through points (trades)
            {
                double[] trades;
                if (i == 0)
                    trades = pointsList.Select(x => x[0].Y).ToArray();
                else
                    trades = pointsList.Select(x => x[i].Y - x[i - 1].Y).ToArray();

                //current trade metrics
                var lossTrades = trades.Where(x => x < 0);
                var winTrades = trades.Where(x => x > 0);

                for (int j = 0; j < trades.Length; ++j)
                {
                    if (trades[j] >= 0)
                    {
                        ConsecWinsPerSample[j]++;
                        maxWinPerSample[j] = Math.Max(maxWinPerSample[j], trades[j]);
                    }
                    else
                        ConsecWinsPerSample[j] = 0;

                    if (trades[j] <= 0)
                    {
                        ConsecLossPerSample[j]++;
                        maxLosPerSample[j] = Math.Max(maxLosPerSample[j], -trades[j]);
                    }
                    else
                        ConsecLossPerSample[j] = 0;

                    maxConsecWinsPerSample[j] = Math.Max(maxConsecWinsPerSample[j], ConsecWinsPerSample[j]);
                    maxConsecLossPerSample[j] = Math.Max(maxConsecLossPerSample[j], ConsecLossPerSample[j]);
                }

                avMaxWin.Add(i, maxWinPerSample.Average());
                maxConsecWin.Add(i, maxConsecWinsPerSample.Max());
                avConsecWin.Add(i, maxConsecWinsPerSample.Average());
                avMaxLoss.Add(i, maxLosPerSample.Average());
                maxConsecLoss.Add(i, maxConsecLossPerSample.Max());
                avConsecLoss.Add(i, maxConsecLossPerSample.Average());
            }

            return new List<PointPairList>() { avMaxWin, maxConsecWin, avConsecWin, avMaxLoss, maxConsecLoss, avConsecLoss };
        }

        [Command(isCommand:false)]
        public static double Uniform(Random random, double minValue, double maxValue) => random.NextDouble() * (maxValue - minValue) + minValue;

        [Command(isCommand: false)]
        public static double GetWin(Random rnd, double x, double win, double var)
        {
            if (x <= .05)
                return Uniform(rnd, win - 3 * var, win - 2 * var);
            if (x >= .95)
                return Uniform(rnd, win + 2 * var, win + 3 * var);
            if (x > .05 && x < .3)
                return Uniform(rnd, win - 2 * var, win - var);
            if (x > .8 && x < .95)
                return Uniform(rnd, win + var, win + 2 * var);
            return Uniform(rnd, win - var, win + var);
        }

        [Command(isCommand: false)]
        public static double GetLoss(Random rnd, double x, double loss, double var)
        {
            if (x <= .05)
                return Uniform(rnd, loss + 2 * var, loss + 3 * var);
            if (x >= .95)
                return Uniform(rnd, loss - 3 * var, loss - 2 * var);
            if (x > .05 && x < .3)
                return Uniform(rnd, loss + var, loss + 2 * var);
            if (x > .8 && x < .95)
                return Uniform(rnd, loss - 2 * var, loss - var);
            return Uniform(rnd, loss - var, loss + var);
        }

        [Command(isCommand: false)]
        public static float Correlation(List<float> Xs, List<float> Ys)
        {
            float sumX = 0;
            float sumX2 = 0;
            float sumY = 0;
            float sumY2 = 0;
            float sumXY = 0;

            int n = Xs.Count < Ys.Count ? Xs.Count : Ys.Count;
            int n2 = n * n;

            for (int i = 0; i < n; ++i)
            {
                float x = Xs[i];
                float y = Ys[i];

                sumX += x;
                sumX2 += x * x;
                sumY += y;
                sumY2 += y * y;
                sumXY += x * y;
            }

            float stdX = (float)Math.Sqrt(sumX2 / n - sumX * sumX / n2);
            float stdY = (float)Math.Sqrt(sumY2 / n - sumY * sumY / n2);
            float covariance = (sumXY / n - sumX * sumY / n2);

            return covariance / stdX / stdY;
        }

        [Command(isCommand: false)]
        public static double Covariance(List<float> Xs, List<float> Ys, bool bias)
        {
            double sumX = 0;
            double sumY = 0;
            double sumXY = 0;

            int n = Xs.Count < Ys.Count ? Xs.Count : Ys.Count;
            double x, y;

            for (int i = 0; i < n; ++i)
            {
                x = Xs[i];
                y = Ys[i];
                sumX += x;
                sumY += y;
                sumXY += x * y;
            }

            if (bias)
                return (sumXY - sumX * sumY / n) / n;
            else
                return (sumXY - sumX * sumY / n) / (n - 1);
        }

        [Command(isCommand: false)]
        public static double[,] CovarianceMatrix(List<List<float>> activeRes)
        {
            double[,] cov = new double[activeRes.Count, activeRes.Count];

            //fill data
            for (int i = 0; i < activeRes.Count; i++)
            {
                for (int j = 0; j < activeRes.Count; j++)
                {
                    /*if (i == j)
                    {
                        cov[i, j] = Covariance(activeRes[i], activeRes[j], false); // activeRes[i] == activeRes[j]
                        continue;
                    }
                    else*/
                    if (activeRes[i].Count < 2 || activeRes[j].Count < 2)
                    {
                        cov[i, j] = 0;
                    }
                    else
                    {
                        double c = Covariance(activeRes[i], activeRes[j], false);
                        if (double.IsNaN(c))
                            cov[i, j] = 0;
                        else
                            cov[i, j] = c;
                    }
                }
            }
            return cov;
        }

        [Command(isCommand: false)]
        public static List<List<float>> MatchStrategiesPnls(List<IReadonlySimStrategy> activeRes)
        {
            int[][] DatesArray = new int[activeRes.Count][];
            int[][] TimesArray = new int[activeRes.Count][];
            IReadOnlyList<float>[] pnlsArray = new IReadOnlyList<float>[activeRes.Count];

            for (int i = 0; i < activeRes.Count; i++)
            {
                RAWdata raw = RAWdataStorage.GetRawData(Utils.SymbolsManager.GetSymbolFileNameMult(activeRes[i].SymbolId, out _), activeRes[i].SymbolId);
                if (raw != null)
                {
                    DatesArray[i] = raw.Dates;
                    TimesArray[i] = raw.Times;
                }
                pnlsArray[i] = activeRes[i].ReadonlyDaily_pnls;
            }

            int maxStartDate = activeRes.Max(x => x.SignalStartDate);
            int minStopDate = activeRes.Min(x => x.StopDate);

            /*/test/////////////////////////////////////
            string sss = "";
            sss += "Start at: " + activeRes[0].signalStartDate + "  " + activeRes[0].signalStartTime + Environment.NewLine + Environment.NewLine;
            for (int i = 0; i < activeRes[0].daily_pnls.Count; i++)
            {
                sss += string.Format("{0}{1}", Security.TTdateToDecimal((ushort)activeRes[0].daily_pnls[i]), Environment.NewLine);
            }
            TextOutput to1 = new TextOutput(sss);
            to1.Show();
            //----------------------------------------*/

            // pnls is daily data, no need to keep time
            SortedDictionary<int, float[]> pnlsRegularDict = new SortedDictionary<int, float[]>();

            for (int i = 0; i < activeRes.Count; i++)
            {
                if (DatesArray[i] == null) continue;   // if no raw data found

                int d = DatesArray[i][0], t, dprev;
                bool firstBarInDates = true;
                int ses = 100 * activeRes[i].SessionEndTime;

                for (int j = 1, k = -1; j < DatesArray[i].Length; j++)
                {
                    dprev = d;
                    d = DatesArray[i][j];
                    t = TimesArray[i][j];
                    if (d > minStopDate) break;
                    if (d < activeRes[i].SignalStartDate || (d == activeRes[i].SignalStartDate && t < activeRes[i].SignalStartTime))
                        continue;

                    if (firstBarInDates) { firstBarInDates = false; continue; }

                    if ((d > dprev && (TimesArray[i][j - 1] < ses || t >= ses)) || (d == dprev && (TimesArray[i][j - 1] < ses && t >= ses)))
                    {
                        k++;
                        if (k >= pnlsArray[i].Count) break;
                        if (d < maxStartDate) continue; //skip daily_pnls before global start date

                        if (!pnlsRegularDict.ContainsKey(dprev))
                            pnlsRegularDict.Add(dprev, Enumerable.Repeat(float.NaN, activeRes.Count).ToArray());
                        pnlsRegularDict[dprev][i] = pnlsArray[i][k];
                    }
                }
            }

            //process empty data strategies
            for (int i = 0; i < activeRes.Count; i++)
            {
                if (DatesArray[i] == null)   // if no raw data found
                {
                    foreach (var kv in pnlsRegularDict)
                    {
                        kv.Value[i] = 0;    // force skipping strategies with no data file found
                    }
                }
            }

            List<List<float>> pnlsRegular = new List<List<float>>();
            for (int j = 0; j < activeRes.Count; j++)
                pnlsRegular.Add(new List<float>());
            foreach (float[] v in pnlsRegularDict.Values)    //don't use ElementAt(). foreach is much more faster
                if (!v.Contains(float.NaN))
                    for (int j = 0; j < activeRes.Count; j++)
                        pnlsRegular[j].Add(v[j]);

            if (pnlsRegular[0].Count > pnlsArray.Min(x => x.Count))
                throw new Exception("Regular data contains more rows than minimal original strategy");

            /*/test: check dates
            string ss = "";
            foreach(KeyValuePair<int,float[]> kv in pnlsRegularDict)
            {
                if (!kv.Value.Contains(float.NaN))
                {
                    int k1 = kv.Key;
                    int k2 = Security.TTdateToDecimal((ushort)kv.Value[0]);

                    ss += "[ " + kv.Key + " ]  ";
                    for (int i = 0; i < kv.Value.Length; i++)
                        ss += Security.TTdateToDecimal((ushort)kv.Value[i]).ToString() + ", ";

                    if (k1 != k2)
                        ss += "     <-------------!";
                    ss += Environment.NewLine;
                }
            }
            TextOutput to = new TextOutput(ss);
            to.Show();
            //endtest*/

            /*/test: write python script
            string py = @"
import pandas as pd
import numpy as np
import matplotlib.pyplot as plt

d = {{ {0} }}
data = pd.DataFrame(data=d)

# -----------------------------
#  Calculations
#
prets = []
pvols = []
max_shrp = 0
optimal_weights = []

for p in range(2500):
    weights = np.random.random(len(data.columns))
    weights /= np.sum(weights)
    expected_return = np.sum(data.mean() * weights) * 252
    expected_vol = np.sqrt(np.dot(weights.T, np.dot(data.cov() * 252, weights)))
    shrp = expected_return / expected_vol
    if shrp > max_shrp:
        max_shrp = shrp
        optimal_weights = weights
    prets.append(expected_return)
    pvols.append(expected_vol)

prets = np.array(prets)
pvols = np.array(pvols)

optimal_weights = [int(x * 100) for x in optimal_weights]
#print max_shrp, optimal_weights

# ----------------------------------
#   Even Weight
#
weights = [1.0 / len(data.columns) for i in range(len(data.columns))]
weights = np.array(weights)
weights /= np.sum(weights)
expected_ret = np.sum(data.mean() * weights) * 252
expected_vol = np.sqrt(np.dot(weights.T, np.dot(data.cov() * 252, weights)))
shrp = expected_ret / expected_vol

# -----------------------------------
# Plotting
#
plt.figure()
plt.scatter(pvols, prets, c = prets / pvols, marker = 'o')
plt.scatter(expected_vol, expected_ret, c = shrp, marker = 'D', s = 100)
plt.grid(True)
plt.xlabel('Expected Volatility')
plt.ylabel('Expected Return')
plt.colorbar(label = 'Sharpe Ratio')
plt.show()";

            string dat = "";
            int col = 0;
            foreach(List<float> f in pnlsRegular)
            {
                dat += "'col"+col+ "':[ ";
                col++;
                foreach (float ff in f)
                    dat += ff.ToString(CultureInfo.InvariantCulture) + ", ";
                dat = dat.Substring(0, dat.Length - 2);
                dat += " ], ";
            }
            dat = dat.Substring(0, dat.Length - 2);

            TextOutput to2 = new TextOutput(string.Format(py, dat));
            to2.Show();
            //end test*/

            return pnlsRegular;
        }

        [Command(isCommand: false)]
        public static void MinVariance(List<List<float>> activeRes, out List<List<double>> weights, out List<double> prets, out List<double> pvols, out double min_shrp, out double max_shrp)
        {
            weights = new List<List<double>>();

            double[,] cov252 = CovarianceMatrix(activeRes);
            int cov_size = activeRes.Count;
            for (int i = cov_size - 1; i >= 0; i--)
                for (int j = cov_size - 1; j >= 0; j--)
                    cov252[i, j] *= 252;

            // Calculate data mean
            List<double> dm = new List<double>();
            for (int i = 0; i < activeRes.Count; i++)
                dm.Add(activeRes[i].Average());

            prets = new List<double>();
            pvols = new List<double>();
            max_shrp = double.MinValue;
            min_shrp = double.MaxValue;
            List<double> optimal_weights = null;
            Random rnd = new Random(DateTime.Now.Millisecond);

            // ----------------------------------
            //   Even Weight
            //
            List<double> we = new List<double>(activeRes.Count);
            for (int i = 0; i < activeRes.Count; i++)
                we.Add(1.0 / activeRes.Count);
            // Normalze (already normalized by default)
            //double summ = we.Sum();
            //for (int i = 0; i < we.Count; i++)
            //    we[i] /= summ;
            double expected_ret = (dm.Sum() / dm.Count) * 252; //np.sum(data.mean() * weights) * 252;
            double expected_vol = Math.Sqrt(Dot(we, Multiply(cov252, we, activeRes.Count))); //np.sqrt(np.dot(weights.T, np.dot(data.cov() * 252, weights)));
            prets.Add(expected_ret);
            pvols.Add(expected_vol);
            weights.Add(we);

            //Randomized
            for (int p = 0; p < 2500; p++)
            {
                List<double> _weights = new List<double>(activeRes.Count);
                for (int i = 0; i < activeRes.Count; i++)
                    _weights.Add(rnd.NextDouble());
                double sum = _weights.Sum();
                for (int i = 0; i < activeRes.Count; i++)     //normalize
                    _weights[i] /= sum;

                sum = 0;
                for (int i = 0; i < activeRes.Count; i++)
                    sum += _weights[i] * dm[i];

                double expected_return = sum * 252; // np.sum(data.mean() * _weights) * 252;
                double expected_v = Math.Sqrt(Dot(_weights, Multiply(cov252, _weights, activeRes.Count))); // np.sqrt(np.dot(_weights.T, np.dot(data.cov() * 252, _weights)));
                double shr = expected_return / expected_v;

                if (shr > max_shrp)
                {
                    max_shrp = shr;
                    optimal_weights = _weights;
                }
                if (shr < min_shrp)
                    min_shrp = shr;
                prets.Add(expected_return);
                pvols.Add(expected_v);
                weights.Add(_weights);
            }

            if (optimal_weights != null)
                for (int i = 0; i < optimal_weights.Count; i++)
                    optimal_weights[i] *= 100;
        }

        [Command(isCommand: false)]
        public static double[] Multiply(double[,] matrix, List<double> vector, int size)
        {
            double[] aux = new double[size];
            for (int i = 0; i < size; i++)
            {
                aux[i] = 0;
                for (int j = 0; j < size; j++)
                    aux[i] += matrix[i, j] * vector[j];
            }
            return aux;
        }

        [Command(isCommand: false)]
        public static double Dot(List<double> vec1, double[] vec2)
        {
            double res = 0;
            for (int i = 0; i < vec1.Count; i++)
                res += vec1[i] * vec2[i];
            return res;
        }

        static async Task NoiseTestTask(SimStrategy simStrategy, int samples, int prob_chg_open, int prob_chg_high, int prob_chg_low, int prob_chg_close, float max_chg_amt, string currency)
        {
            await Task.Run(() =>
            {
                // Define input params
                List<Signal> signalsToCalculate = simStrategy.ReadonlyEntrySignals.Select(x => x.MakeClone(false)).ToList();
                if (simStrategy.ReadonlyExitSignals?.Count > 0)
                    signalsToCalculate.AddRange(simStrategy.ReadonlyExitSignals.Select(x => x.MakeClone(false)));
                //signalsToCalculate = signalsToCalculate.Union(simStrategy.ReadonlyExitSignals).ToList();

                // Settings
                SimulationSettings sim = new SimulationSettings(signalsToCalculate,
                    Security.ToDateTime(simStrategy.SignalStartDate, simStrategy.SignalStartTime),
                    Security.ToDateTime(simStrategy.StopDate, 235959), true, false, null,
                    null, new SymbolId(), null, null, null, 0, simStrategy.ContractMultiplier,
                    simStrategy.TradeMode, simStrategy.PT_ON, simStrategy.SL_ON, simStrategy.TL_ON,
                    (float)simStrategy.PT_mult, (float)simStrategy.SL_mult, simStrategy.TL_mult,
                    simStrategy.HH_ON, simStrategy.LL_ON, simStrategy.HHlook, simStrategy.LLlook, simStrategy.ATR_len,
                    simStrategy.PosSizeMode, simStrategy.SlippageMode, simStrategy.CommissionMode, simStrategy.Commission, simStrategy.Slippage,
                    currency, simStrategy.AccountValue, simStrategy.MarketSymbolsIds, simStrategy.InvestCash,
                    simStrategy.MaxTime, simStrategy.ProfX, simStrategy.Fitness, simStrategy.OosLocation, 0/*oos_percent*/, 1, 1,
                    simStrategy.EntryOnClose, simStrategy.ExitOnClose, simStrategy.ForceExitOnSessionEnd ? (short)1 : (short)0,
                    new DateTime(1, 1, 1, simStrategy.SessionEndTime / 100, simStrategy.SessionEndTime % 100, 0),
                    simStrategy.DelayedEntry, simStrategy.RebalanceMethod, simStrategy.RebalancePeriod, simStrategy.RebalanceSymbolsCount,
                    0, 0, 0, 0, false, 0, 0, 0, 0, 0, 0, 0);

                // prepare raw data
                var data = RawDataArray.GetRawDataForStrategy(simStrategy, samples, prob_chg_open, prob_chg_high, prob_chg_low, prob_chg_close, max_chg_amt,
                    simStrategy.ATR_len, (float)simStrategy.PT_mult, (float)simStrategy.SL_mult,
                    simStrategy.TL_mult, simStrategy.HHlook, simStrategy.LLlook, Utils.SymbolsManager);

                if (data == null) return;

                // generate and simulate
                var output = Simulator.GenAndSim(data[1], sim, 0);       // '1' is noise data set. Todo: cycle by samples

                //if (!string.IsNullOrEmpty(output.Message))
                //{
                //    //Program.logger?.Warn("Noise Task 1: " + output.Message);
                //    return null;
                //}
            });
        }

        struct IntraRes
        {
            public int[] param;
            public string label;
            public List<float> values;
        }

        private static int FindRawBarIndexByDateTime(int[] Dates, int[] Times, int date, int time, int startIdx)
        {
            if (Dates == null || Dates.Length == 0 || Times == null || Times.Length == 0)
                return -1;

            int idx = startIdx;
            for (; idx < Dates.Length && Dates[idx] < date; idx++) ;
            for (; idx < Times.Length && Dates[idx] == date && Times[idx] < time; idx++) ;

            if (idx < Times.Length && Dates[idx] == date && Times[idx] == time)
                return idx;
            else
                return -1;
        }

        private static string ReadGeneratorOutput(string namesFileName, string dataFileName, string signalsFileName,
            out List<int> dates, out List<int> times, out List<BitArray> signals, out List<string> signalNames)
        {
            dates = new List<int>();
            times = new List<int>();
            signals = new List<BitArray>();
            signalNames = new List<string>();

            try
            {   // read names
                using (StreamReader reader = new StreamReader(namesFileName))
                {
                    int pos;
                    for (string s = reader.ReadLine(); s != null; s = reader.ReadLine())
                    {
                        if (s.Length < 3) continue;
                        pos = 0;
                        while (s[pos] != ' ') ++pos;    // skip index
                        while (s[pos] == ' ') ++pos;
                        while (s[pos] != ' ') ++pos;    // skip start date
                        while (s[pos] == ' ') ++pos;
                        while (s[pos] != ' ') ++pos;    // skip actual start date
                        while (s[pos] == ' ') ++pos;
                        signalNames.Add(s.Substring(pos));
                    }
                }
            }
            catch (Exception ex)
            {
                return "Reading signals names... Can't read input file!\n\nError: " + ex.Message; //, "File read error");
            }
            try
            {   // read data
                using (StreamReader reader = new StreamReader(dataFileName))
                {
                    char[] sep = new char[] { ' ' };
                    for (string s = reader.ReadLine(); s != null; s = reader.ReadLine())
                    {
                        if (s.Length < 20) continue;
                        string[] ss = s.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                        dates.Add(int.Parse(ss[1]));
                        times.Add(int.Parse(ss[2]));
                    }
                }
            }
            catch (Exception ex)
            {
                return "Reading signals data... Can't read input file!\n\nError: " + ex.Message; //, "File read error");
            }
            try
            {   //Read signals
                using (BinaryReader reader = new BinaryReader(File.Open(signalsFileName, FileMode.Open)))
                {
                    int s_count = reader.ReadInt32();

                    for (int i = 0; i < signalNames.Count; i++)
                    {
                        BitArray ba = new BitArray(s_count);
                        for (int j = 0; j < s_count; j += 8)
                        {
                            byte b = reader.ReadByte();
                            ba.Set(j, (b & 128) != 0);
                            if (j + 1 < s_count) ba.Set(j + 1, (b & 64) != 0);
                            if (j + 2 < s_count) ba.Set(j + 2, (b & 32) != 0);
                            if (j + 3 < s_count) ba.Set(j + 3, (b & 16) != 0);
                            if (j + 4 < s_count) ba.Set(j + 4, (b & 8) != 0);
                            if (j + 5 < s_count) ba.Set(j + 5, (b & 4) != 0);
                            if (j + 6 < s_count) ba.Set(j + 6, (b & 2) != 0);
                            if (j + 7 < s_count) ba.Set(j + 7, (b & 1) != 0);
                        }
                        signals.Add(ba);
                    }
                }
            }
            catch (Exception ex)
            {
                return "Reading signals... Can't read input file!\n\nError: " + ex.Message; //, "File read error");
            }
            return null;
        }

        private static string ExportTableToCSV(IEnumerable<IReadonlySimStrategy> list)
        {
            StringBuilder s = new StringBuilder();
            if (list != null)
            {
                // print header
                s.AppendFormat("{0}, ", "Index, Symbol, Type, ContractType, EntrySignals, ExitSignals");
                s.AppendFormat("{0}, ", "Exit_mode, PT_mode, PT_mult, SL_mode, SL_mult, TL_mode, TL_mult, HH_mode, HH_look, LL_mode, LL_look, Max_time, Prof_X");
                s.AppendFormat("{0}, ", "NetPNL, Drawdown, WinPercentage, Trades, MeanWTrades, MeanLTrades, StdWTrades, StdLTrades");
                s.AppendFormat("{0}, ", "AvarageTrade, ProfitFactor, CoerCoef, CPC_Ratio, RatioWL, RobustIndex, Sharpe, Sortino");
                s.AppendFormat("{0}", "CAGR, SQM, SQN, TTest, ERatio, KRatio, ExpectancyScore, PerfectProfitPercentage, PerfectProfitCorrelation");
                s.AppendLine();

                // cycle through strategies
                foreach (var strat in list)
                {
                    s.AppendFormat("{0}, ", strat.Indx);
                    s.AppendFormat("{0}, ", strat.Symbol);
                    s.AppendFormat("{0}, ", strat.IndxType == StrategySimType.IN ? "IN" : (strat.IndxType == StrategySimType.OUT ? "OUT" : (strat.IndxType == StrategySimType.ALL ? "ALL" : "CST")));
                    s.AppendFormat("{0}, ", strat.ContractType);

                    // entry signals
                    //if (strat.haveCustomName)           //todo: use short name (keys only)
                    s.AppendFormat("{0}, ", strat.Name.Replace(',', ';'));
                    //else
                    //{
                    //    foreach (string ss in strat.nameOriginal.Split(new char[] { '\r' }, StringSplitOptions.RemoveEmptyEntries))
                    //        s.Append(ss.Split('\t')[0] + "; ");        // '\t' is splitter for ENSEMLE and STRATEGY signals data
                    //    if (s[s.Length - 2] == ';') s.Remove(s.Length - 2, 2);
                    //    s.Append(", ");
                    //}

                    // exit signals
                    if (strat.ReadonlyExitSignals != null)
                        foreach (string ss in strat.ReadonlyExitSignals.Select(x => x.Key))
                            s.Append(ss.Split('\t')[0] + "; ");        // '\t' is splitter for ENSEMLE and STRATEGY signals data
                    if (s[s.Length - 2] == ';') s.Remove(s.Length - 2, 2);
                    s.Append(", ");

                    s.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", strat.ExitMode == 1 ? "single" : strat.ExitMode == 2 ? "best" : strat.ExitMode == 3 ? "all" : "none");

                    s.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", strat.PT_ON == 0 ? "PT_OFF" : strat.PT_ON == 1 ? "PT_ATR" : "PT_FIXED");
                    s.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", strat.PT_mult);

                    s.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", strat.SL_ON == 0 ? "SL_OFF" : strat.SL_ON == 1 ? "SL_ATR" : "SL_FIXED");
                    s.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", strat.SL_mult);

                    s.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", strat.TL_ON == 0 ? "TL_OFF" : strat.TL_ON == 1 ? "TL_ATR" : "TL_FIXED");
                    s.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", strat.TL_mult);

                    s.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", strat.HH_ON == 0 ? "HH_OFF" : "HH_ON");
                    s.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", strat.HHlook);

                    s.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", strat.LL_ON == 0 ? "LL_OFF" : "LL_ON");
                    s.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", strat.LLlook);

                    s.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", strat.MaxTime);
                    s.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", strat.ProfX);

                    s.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", strat.Net_PnL);
                    s.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", strat.Drawdown);
                    s.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", strat.WinPercentage);
                    s.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", strat.TradesCount);
                    s.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", strat.Mean_w_trades);
                    s.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", strat.Mean_l_trades);
                    s.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", strat.Std_w_trades);
                    s.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", strat.Std_l_trades);
                    s.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", strat.AvrTrade);
                    s.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", strat.ProfitFactor);
                    s.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", strat.CoerCoef);
                    s.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", strat.CPCRatio);
                    s.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", strat.RatioWL);
                    s.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", strat.RobustIndex);
                    s.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", strat.Sharpe);
                    s.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", strat.Sortino);
                    s.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", strat.CAGR);
                    s.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", strat.SQM);
                    s.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", strat.SQN);
                    s.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", strat.TTest);
                    s.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", strat.ERatio);
                    s.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", strat.KRatio);
                    s.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", strat.ExpectancyScore);
                    s.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", strat.PerfectProfitPercentage);
                    s.AppendFormat(CultureInfo.InvariantCulture, "{0}", strat.PerfectProfitCorrelation);
                    s.AppendLine();
                }
            }
            return s.ToString();
        }

        private static void GetTradesEntryExit(IReadonlySimStrategy res, RAWdata rdata, float[] PosSizes, int simStartDate, int simStartTime, out List<float> entryValues, out List<float> exitValues)
        {
            // current simulation results only
            if (res.Indx > 0)
            {
                entryValues = new List<float>(res.ReadonlyResults.Count);
                exitValues = new List<float>(res.ReadonlyResults.Count);

                // calc PT, SL, HH, LL for raw data (do it every time because of possible delta settings)
                float[] PT, SL, TL, HH, LL;
                {
                    GenerateSignalsToTrade.CalcPT_SL_TL_HH_LL(res.TradeMode.ToString().StartsWith("L"), res.SymbolId, res.SymbolType, res.PosSizeMode,
                        rdata.Highs, rdata.Lows, rdata.Closes, PosSizes,
                        out PT, out SL, out TL, out HH, out LL, res.ContractMultiplier, res.ATR_len, res.PT_ON, res.SL_ON, res.TL_ON,
                        (float)res.PT_mult, (float)res.SL_mult, res.TL_mult, res.HHlook, res.LLlook);

                    // ATR shoud be zero for first N bars from start datetime
                    int sigStDate = simStartDate != 0 ? simStartDate : res.SignalStartDate;
                    int sigStTime = simStartTime != 0 ? simStartTime : res.SignalStartTime;
                    for (int i = 0, z = 1; i < rdata.Dates.Length && z <= res.ATR_len; i++)
                    {
                        if (SimStrategy.CompareDateTimes(rdata.Dates[i], rdata.Times[i], sigStDate, sigStTime) <= 0)
                            PT[i] = SL[i] = TL[i] = 0;
                        else
                        {
                            if (res.SL_ON == 1)  //ATR based
                                SL[i] = rdata.Closes[i];
                            if (res.PT_ON == 1)  //ATR based
                                PT[i] = rdata.Closes[i];
                            if (res.TL_ON == 1)  //ATR based
                                TL[i] = rdata.Closes[i];
                            z++;
                        }
                    }
                }

                int entry_idx = 0, exit_idx;

                for (int i = 0; i < res.ReadonlyResults.Count; i++)
                {
                    int mode = res.ResMode(i);

                    float entry = 0, exit = 0;

                    if (mode != 5)  //not cash
                    {
                        entry_idx = FindRawBarIndexByDateTime(rdata.Dates, rdata.Times, res.ReadonlyResEntryDates[i], res.ReadonlyResEntryTimes[i], (entry_idx > 0 ? entry_idx : 0));
                        exit_idx = FindRawBarIndexByDateTime(rdata.Dates, rdata.Times, res.ReadonlyResDates[i], res.ResTime(i), (entry_idx > 0 ? entry_idx : 0));

                        if (entry_idx >= 0)
                            entry = res.EntryOnClose > 0 ? rdata.Closes[entry_idx] : rdata.Opens[entry_idx];
                        else entry = 0;

                        if (entry_idx > 0 && res.EntryOnClose == 0) //if entry on next open then take previous bar PT,SL,TL
                            entry_idx--;

                        //check mode, select PT,SL,TL,HH,LL or cash
                        if (exit_idx < 0)
                            exit = 0;
                        else
                            switch (mode)
                            {
                                case 0:
                                case 6:
                                case 8:     // Rebalance
                                    {
                                        exit = res.ExitOnClose > 0 ? rdata.Closes[exit_idx] : rdata.Opens[exit_idx];
                                        break;
                                    }
                                case 9:     // end of data. Using Close always
                                    {
                                        exit = rdata.Closes[exit_idx];
                                        break;
                                    }
                                case 1:     //PT
                                    {
                                        if (res.IsLong)
                                            exit = Math.Max(rdata.Opens[exit_idx],
                                                res.PT_ON == 2 ? GenerateSignalsToTrade.RoundMethod(entry + PT[entry_idx], res.Symbol) : PT[entry_idx]);
                                        else
                                            exit = Math.Min(rdata.Opens[exit_idx],
                                                res.PT_ON == 2 ? GenerateSignalsToTrade.RoundMethod(entry - PT[entry_idx], res.Symbol) : PT[entry_idx]);
                                        break;
                                    }
                                case 2:     //SL
                                    {
                                        if (res.IsLong)
                                            exit = Math.Min(rdata.Opens[exit_idx],
                                                res.SL_ON == 2 ? GenerateSignalsToTrade.RoundMethod(entry - SL[entry_idx], res.Symbol) : SL[entry_idx]);
                                        else
                                            exit = Math.Max(rdata.Opens[exit_idx],
                                                res.SL_ON == 2 ? GenerateSignalsToTrade.RoundMethod(entry + SL[entry_idx], res.Symbol) : SL[entry_idx]);
                                        break;
                                    }
                                case 3:     //HH
                                    {
                                        exit = Math.Max(rdata.Opens[exit_idx], HH[exit_idx - 1]);
                                        break;
                                    }
                                case 4:     //LL
                                    {
                                        exit = Math.Min(rdata.Opens[exit_idx], LL[exit_idx - 1]);
                                        break;
                                    }
                                case 7:     //TL
                                    {
                                        float TLv;
                                        if (res.TL_ON == 1)
                                        {
                                            //find best TL from entry to exit
                                            TLv = TL[entry_idx];
                                            if (res.IsLong)
                                            {
                                                for (int j = entry_idx + 1; j < exit_idx; ++j)
                                                    TLv = Math.Max(TLv, TL[j]);
                                                exit = Math.Min(rdata.Opens[exit_idx], TLv);
                                            }
                                            else
                                            {
                                                for (int j = entry_idx + 1; j < exit_idx; ++j)
                                                    TLv = Math.Min(TLv, TL[j]);
                                                exit = Math.Max(rdata.Opens[exit_idx], TLv);
                                            }
                                        }
                                        else
                                        {
                                            //find best TL from entry to exit
                                            if (res.IsLong)
                                            {
                                                TLv = GenerateSignalsToTrade.RoundMethod(entry - TL[entry_idx], res.Symbol);
                                                for (int j = entry_idx + 1, z = 1; j <= exit_idx; ++j, ++z)
                                                {
                                                    if (rdata.Opens[j] <= TLv)
                                                    {
                                                        TLv = rdata.Opens[j];
                                                        break;
                                                    }
                                                    else
                                                    {
                                                        float thisTL = -9999;
                                                        if (rdata.Highs[j] - rdata.Opens[j] < rdata.Opens[j] - rdata.Lows[j])
                                                            thisTL = rdata.Highs[j] - TL[entry_idx];
                                                        if (thisTL != -9999)
                                                            TLv = Math.Max(TLv, GenerateSignalsToTrade.RoundMethod(thisTL, res.Symbol));

                                                        if (rdata.Lows[j] > TLv)
                                                            TLv = Math.Max(TLv, GenerateSignalsToTrade.RoundMethod(rdata.Highs[j] - TL[entry_idx], res.Symbol));
                                                    }
                                                }
                                                exit = TLv;
                                            }
                                            else
                                            {
                                                TLv = GenerateSignalsToTrade.RoundMethod(entry + TL[entry_idx], res.Symbol);
                                                for (int j = entry_idx + 1, z = 1; j <= exit_idx; ++j, ++z)
                                                {
                                                    if (rdata.Opens[j] >= TLv)
                                                    {
                                                        TLv = rdata.Opens[j];
                                                        break;
                                                    }
                                                    else
                                                    {
                                                        float thisTL = -9999;
                                                        if (rdata.Highs[j] - rdata.Opens[j] > rdata.Opens[j] - rdata.Lows[j])
                                                            thisTL = rdata.Lows[j] + TL[entry_idx];
                                                        if (thisTL != -9999)
                                                            TLv = Math.Min(TLv, GenerateSignalsToTrade.RoundMethod(thisTL, res.Symbol));

                                                        if (rdata.Highs[j] < TLv)
                                                            TLv = Math.Min(TLv, GenerateSignalsToTrade.RoundMethod(rdata.Lows[j] + TL[entry_idx], res.Symbol));
                                                    }
                                                }
                                                exit = TLv;
                                            }
                                        }
                                        break;
                                    }
                            }

                        entry_idx = exit_idx - 1;    // use previous bar for (entryOnClose + exitOnNextOpen)
                    }

                    entryValues.Add(entry);
                    exitValues.Add(exit);
                }
            }
            else
            {
                entryValues = exitValues = null;
            }
        }

        private static List<float> GetPosSizesForStrategyResults(int[] Dates, int[] Times, float[] PosSizes, Core.SymbolType SymbolType, PositionSizingMode PosMode,
            IReadOnlyList<int> resEntryDates, IReadOnlyList<int> resEntryTimes, IReadOnlyList<float> entryValues, byte SL_ON, float sl_mult, byte entryOnClose)
        {
            List<float> posSizes = new List<float>(resEntryDates.Count);
            int entry_idx = 0;

            for (int i = 0; i < resEntryDates.Count; i++)
            {
                if (entryValues[i] > 0.000001f)    //normal trade
                {
                    entry_idx = FindRawBarIndexByDateTime(Dates, Times, resEntryDates[i], resEntryTimes[i], (entry_idx > 0 ? entry_idx : 0));
                    if (entryOnClose == 0 && entry_idx > 0) entry_idx--;     // if enter on next open
                    posSizes.Add(entry_idx > 0 ? PosSizes[entry_idx] : 0);

                    // floor and trim according to modes
                    posSizes[i] = GenerateSignalsToTrade.FloorAndTrimPosSize(posSizes[i], PosMode, SymbolType, SL_ON, sl_mult);
                }
                else     // invest cash trade
                    posSizes.Add(0);
            }
            return posSizes;
        }

        private SimStrategy GetAllStrategy(IReadonlySimStrategy res)
        {
            var allListVM = StrategiesLists.FirstOrDefault(x => !x.IsPortfolio && x.Strategies?.Count > 0 && x.Strategies.First().IndxType == StrategySimType.ALL);
            if (allListVM == null)
                return null;

            SimStrategy resAll = allListVM.Strategies.FirstOrDefault(x => x.IndxType == StrategySimType.ALL && x.Indx == res.Indx);

            if (resAll != null && res.SymbolId != resAll.SymbolId)
                resAll = resAll.Children?.FirstOrDefault(x => x.SymbolId == res.SymbolId);

            return resAll;
        }
    }

    public class StrategiesListVM
    {
        public string Title { get; set; }
        public bool IsPortfolio { get; set; }

        public NotifyObservableCollection<SimStrategy> Strategies { get; set; }

        public ObservableCollection<SimStrategy> SelectedStrategies { get; set; } = new ObservableCollection<SimStrategy>();
    }


#if SDSDSDSD
    // for caching
    class SymbolResData
    {
        string _filename = null;
        double _cm = -1;
        RAWdata _data = null;
        float[] _closesInCurrency = null;
        float[] _posSizes = null;
        float[] _slippages = null;
        float[] _commissions = null;

        public readonly SymbolId SymbolId;

        //for current simulation settings only
        public string RAWfileName
        {
            get
            {
                if (_filename == null)
                {
                    string s = Utils.SymbolsManager.GetSymbolFileNameMult(SymbolId, out _cm);
                    if (!string.IsNullOrEmpty(s))
                        _filename = s;
                    else
                    {
                        _filename = null;
                        _cm = -1;
                    }
                }
                return _filename;
            }
        }
        //for current simulation settings only
        public double ContractMultiplier
        {
            get
            {
                if (_cm < 0)
                {
                    string _ = RAWfileName; //force load
                }
                return _cm;
            }
        }

        public RAWdata Data
        {
            get
            {
                if(_data == null)
                {
                    _data = RAWdataStorage.GetRawData(RAWfileName, SymbolId);
                }
                return _data;
            }
        }
        //raw data closes converted to target currency    //for current simulation settings only (cache)
        public float[] ClosesInCurrency;
        //public List<float> HH = null;                                                                       //for current simulation settings only (cache)
        //public List<float> LL = null;                                                                       //for current simulation settings only (cache)
        //public List<float> PT = null;                                                                       //for current simulation settings only (cache)
        //public List<float> SL = null;                                                                       //for current simulation settings only (cache)
        //public List<float> TL = null;                                                                       //for current simulation settings only (cache)

        //need to divide by SL before use                 //for current simulation settings only (cache)
        public float[] PosSizes;
        //for current simulation settings only (cache)
        public float[] Slippages;
        //for current simulation settings only (cache)
        public float[] Commissions;

        public SymbolResData(SymbolId symbolId)
        {
            SymbolId = symbolId;
        }
    }
#endif
}