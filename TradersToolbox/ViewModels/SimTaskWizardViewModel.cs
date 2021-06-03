using System;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm;
using System.Collections.ObjectModel;
using TradersToolbox.Core;
using DevExpress.Mvvm.POCO;
using TradersToolbox.Core.Serializable;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Windows.Data;
using DevExpress.Xpf.Core;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using DevExpress.Mvvm.UI;
using TradersToolbox.Views;
using System.Collections;
using System.Globalization;

namespace TradersToolbox.ViewModels
{
    [POCOViewModel]
    public class SimTaskWizardViewModel
    {
        //todo: move to tasks

        readonly ManualResetEventSlim manualReset = new ManualResetEventSlim();

        protected IMessageBoxService MessageBoxService { get { return this.GetService<IMessageBoxService>(/*ServiceSearchMode.PreferParents*/); } }
        protected IDocumentManagerService DocumentManagerService { get { return this.GetService<IDocumentManagerService>(); } }
        protected IOpenFileDialogService OpenFileDialogService { get { return this.GetService<IOpenFileDialogService>(); } }
        protected ISaveFileDialogService SaveFileDialogService { get { return this.GetService<ISaveFileDialogService>(); } }
        //protected ICurrentWindowService CurrentWindowService { get { return this.GetService<ICurrentWindowService>(); } }
        //protected ICurrentDialogService CurrentDialogService { get { return this.GetService<ICurrentDialogService>(); } }


        public void CreateDocument(object vm)
        {
            IDocument doc = DocumentManagerService.FindDocument(vm);
            if (doc == null)
            {
                doc = DocumentManagerService.CreateDocument("StrategiesView", vm);
                doc.Id = DocumentManagerService.Documents.Count();
            }
            doc.Show();
        }

        //Todo: SimSettings.PT_ON, SL_ON, TL_ON are one way binding now. Need to think about two way binding (if required)

        #region Edit Values
        public bool PT_ON {
            get => SimSettings.PT_ON > 0;
            set
            {
                byte v = (value == false) ? (byte)0 : (PT_Fixed == false ? (byte)1 : (byte)2);
                if (SimSettings.PT_ON != v)
                {
                    SimSettings.PT_ON = v;
                    this.RaisePropertyChanged(x => x.PT_ON);
                }
            }
        }
        public bool SL_ON
        {
            get => SimSettings.SL_ON > 0;
            set
            {
                byte v = (value == false) ? (byte)0 : (SL_Fixed == false ? (byte)1 : (byte)2);
                if (SimSettings.SL_ON != v)
                {
                    SimSettings.SL_ON = v;
                    this.RaisePropertyChanged(x => x.SL_ON);
                }
            }
        }
        public bool TL_ON
        {
            get => SimSettings.TL_ON > 0;
            set
            {
                byte v = (value == false) ? (byte)0 : (TL_Fixed == false ? (byte)1 : (byte)2);
                if (SimSettings.TL_ON != v)
                {
                    SimSettings.TL_ON = v;
                    this.RaisePropertyChanged(x => x.TL_ON);
                }
            }
        }

        public virtual bool PT_Fixed { get; set; }
        public virtual bool SL_Fixed { get; set; }
        public virtual bool TL_Fixed { get; set; }

        protected void OnPT_FixedChanged()
        {
            if (PT_ON)
                SimSettings.PT_ON = PT_Fixed ? (byte)2 : (byte)1;
        }
        protected void OnSL_FixedChanged()
        {
            if (SL_ON)
                SimSettings.SL_ON = SL_Fixed ? (byte)2 : (byte)1;
        }
        protected void OnTL_FixedChanged()
        {
            if (TL_ON)
                SimSettings.TL_ON = TL_Fixed ? (byte)2 : (byte)1;
        }

        public virtual bool M2Enabled { get; set; }
        public virtual bool M3Enabled { get; set; }
        public virtual SymbolId M2SymbolId { get; set; }
        public virtual SymbolId M3SymbolId { get; set; }

        protected void OnM2SymbolIdChanged()
        {
            M2Enabled = !M2SymbolId.IsEmpty();

            if (!M2Enabled)
            {
                foreach (Signal s in Signals)
                    if (s.MarketNumber == 2)
                    {
                        s.ActiveForEntry = s.ActiveForExit = false;
                    }
            }

            // save selection
            if (SimSettings.MarketSymbolsIds == null)
                SimSettings.MarketSymbolsIds = new List<SymbolId>() { M2SymbolId };
            else if (SimSettings.MarketSymbolsIds.Count == 0)
                SimSettings.MarketSymbolsIds.Add(M2SymbolId);
            else
                SimSettings.MarketSymbolsIds[0] = M2SymbolId;
        }

        protected void OnM3SymbolIdChanged()
        {
            M3Enabled = !M3SymbolId.IsEmpty();

            if (!M3Enabled)
            {
                foreach (Signal s in Signals)
                    if (s.MarketNumber == 3)
                    {
                        s.ActiveForEntry = s.ActiveForExit = false;
                    }
            }

            // save selection
            if (SimSettings.MarketSymbolsIds == null)
                SimSettings.MarketSymbolsIds = new List<SymbolId>() { SymbolId.Empty, M3SymbolId };
            else if (SimSettings.MarketSymbolsIds.Count == 0)
            {
                SimSettings.MarketSymbolsIds.Add(SymbolId.Empty);
                SimSettings.MarketSymbolsIds.Add(M3SymbolId);
            }
            else if (SimSettings.MarketSymbolsIds.Count == 1)
                SimSettings.MarketSymbolsIds.Add(M3SymbolId);
            else
                SimSettings.MarketSymbolsIds[1] = M3SymbolId;
        }


        NotifyObservableCollection<Signal> Signals_internal;

        public NotifyObservableCollection<Signal> Signals {
            get
            {
                if (Signals_internal == null)
                {
                    // try to load from settings first
                    if (!string.IsNullOrEmpty(Properties.Settings.Default.SignalsXML))
                    {
                        Signals_internal = LoadSignalsFromXML(Properties.Settings.Default.SignalsXML);
                    }
                    // read default signals
                    if (Signals_internal == null)
                    {
                        List<SymbolId> viewSymbols = new List<SymbolId>() { new SymbolId("ES", ""), new SymbolId("ES", ""), new SymbolId("ES", ""), new SymbolId("Vix", "") };
                        Signals_internal = SignalsFactory.GetAllSignals(viewSymbols, Utils.portfolioFileName, MainWindowViewModel.PYTHON_READY);

                        var Cnot0 = Signals_internal.First(x => x.Key == "Cnot0");
                        Cnot0.ActiveForEntry = true;
                        Cnot0.CanEdit = false;
                    }
                }

                var v = CollectionViewSource.GetDefaultView(Signals_internal);
                if (v.GroupDescriptions?.Count == 0)
                    v.GroupDescriptions.Add(new PropertyGroupDescription("GroupId"));

                return Signals_internal;
            }
            private set
            {
                Signals_internal = value;
                this.RaisePropertiesChanged();
            }
        }

        public virtual SimulationSettings SimSettings { get; set; }

        protected void OnSimSettingsChanged()
        {
            // manually update market 2
            if (SimSettings.MarketSymbolsIds?.Count > 0)
                M2SymbolId = SimSettings.MarketSymbolsIds[0];
            else
                M2SymbolId = SymbolId.Empty;

            // manually update market 3
            if (SimSettings.MarketSymbolsIds?.Count > 1)
                M3SymbolId = SimSettings.MarketSymbolsIds[1];
            else
                M3SymbolId = SymbolId.Empty;

            // manually update PT, SL TL
            this.RaisePropertyChanged(x => x.PT_ON);
            this.RaisePropertyChanged(x => x.SL_ON);
            this.RaisePropertyChanged(x => x.TL_ON);
            PT_Fixed = (SimSettings.PT_ON == 2);
            SL_Fixed = (SimSettings.SL_ON == 2);
            TL_Fixed = (SimSettings.TL_ON == 2);
        }
        #endregion

        public virtual bool IsWaitIndicatorVisible { get; set; }
        public virtual string ProgressText { get; set; }
        public virtual string StageText { get; set; }
        public virtual int ProgressValue { get; set; }

        public virtual bool IsReRun{ get; set; }
        public virtual bool IsContinuousRunning { get; set; }

        public virtual string CurrentConfigFileName { get; set; }
        string CurrentConfigFileFullName;
        public virtual ObservableCollection<KeyValuePair<string,string>> RecentConfigFiles { get; set; }

        public static SimTaskWizardViewModel Create()
        {
            return ViewModelSource.Create(() => new SimTaskWizardViewModel());
        }
        protected SimTaskWizardViewModel()
        {
            SimSettings = LoadSimSettings();

            Messenger.Default.Register<CustomIndicatorsChangedMessage>(this, OnMessage);
            Messenger.Default.Register<PortfolioChangedMessage>(this, OnMessage);

            RecentConfigFiles = new ObservableCollection<KeyValuePair<string, string>>();
            UpdateRecentConfFilesMenu();
        }

        public static SimulationSettings LoadSimSettings()
        {
            var c = Properties.Settings.Default.SimConfigs;
            if (c?.Count > 0 && !string.IsNullOrEmpty(c[0]))
            {
                try
                {
                    var s = Serializer.Deserialize(c[0], typeof(SimulationSettings)) as SimulationSettings;
                    return s;
                }
                catch (Exception ex)
                {
                    Logger.Current.Warn(ex, "Can't read configuration from settings!");
                    return new SimulationSettings();
                }
            }
            else
                return new SimulationSettings();
        }

        private void Simulator_StaticPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            IsContinuousRunning = Simulator.ContinuousSimThread != null;
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
        public void ResetConfigToDefaults()
        {
            ResetConfiguration_internal();
        }

        /*public void ResetConfigToPreset()
        {

        }*/

        public void OpenRecentConfig(object o)
        {
            OpenRecentConfig_internal(0);
        }

        public void SaveConfiguration()
        {
            SaveConfiguration_internal();
        }

        public void SaveConfigurationAs()
        {
            SaveConfigurationAs_internal();
        }

        public void LoadConfiguration()
        {
            LoadConfiguration_internal();
        }

        /// <summary>
        /// Run simulation
        /// </summary>
        [AsyncCommand(AllowMultipleExecution = false, UseCommandManager = false)]
        public async Task RegularSimulation()
        {
            ProgressValue = 0;
            IsWaitIndicatorVisible = true;
            manualReset.Reset();

            // todo: dependency property
            SimSettings.Signals = Signals.Where(s => s.ActiveForEntry || s.ActiveForExit).ToList();
            SimSettings.InMemory = false;

            await Task.Run(RegularSimulationCore);  // run task

            IsWaitIndicatorVisible = false;
        }

        [AsyncCommand(AllowMultipleExecution = false, UseCommandManager = false)]
        public async Task ContinuousSimulation()
        {
            if (Simulator.ContinuousSimThread != null)
            {
                if (MessageBoxService.Show("Continuous Simulation is now running. Do you want to stop simulation?", "Continuous Simulation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    await Task.Run(() =>
                    {
                        CppSimulatorInterface.CancelContinuousSimulation();
                        Simulator.ContinuousSimThread.Join();
                        Simulator.ContinuousSimThread = null;
                    });
                    //IsContinuousRunning = false;
                }
                return;
            }
            
            // checks
            SymbolId M2symbolId = SimSettings.MarketSymbolsIds?.Count > 0 ? SimSettings.MarketSymbolsIds[0] : SymbolId.Empty;
            SymbolId M3symbolId = SimSettings.MarketSymbolsIds?.Count > 1 ? SimSettings.MarketSymbolsIds[1] : SymbolId.Empty;

            if (SimSettings.BaseSymbolsIds == null || SimSettings.BaseSymbolsIds.Count <= 0)
            {
                MessageBoxService.Show("No symbols chosen for simulation!", "Trader's Toolbox");
                return;
            }
            if (SimSettings.BaseSymbolsIds.Contains(M2symbolId) || SimSettings.BaseSymbolsIds.Contains(M3symbolId) || (M2symbolId.IsEmpty() == false && M2symbolId == M3symbolId))
            {
                MessageBoxService.Show("Please select different symbols for Base, Market2, Market3 inputs!", "Trader's Toolbox");
                return;
            }

            // prepare
            ProgressValue = 0;
            IsWaitIndicatorVisible = true;
            manualReset.Reset();

            // todo: dependency property
            SimSettings.Signals = Signals.Where(s => s.ActiveForEntry || s.ActiveForExit).ToList();
            SimSettings.InMemory = false;

            await Task.Run(ContinuousSimulationCore);  // run task

            IsWaitIndicatorVisible = false;
        }

        public async Task SelectPositive(object o)
        {
            bool isEntry = o.ToString() == "entry";

            ProgressValue = 0;
            IsWaitIndicatorVisible = true;
            manualReset.Reset();

            SimSettings.InMemory = true;

            await Task.Run(SelectPositiveCore);  // run task

            IsWaitIndicatorVisible = false;
        }

        public void Cancel()
        {
            ProgressText = "Canceling...";
            manualReset.Set();
        }

        public void Loaded()
        {
            if (!IsReRun)
            {
                IsContinuousRunning = Simulator.ContinuousSimThread != null;
                Simulator.StaticPropertyChanged += Simulator_StaticPropertyChanged;
                UpdateRecentConfFilesMenu();
            }
        }
        public void Unloaded()
        {
            if (!IsReRun)
            {
                Simulator.StaticPropertyChanged -= Simulator_StaticPropertyChanged;
                // save settings
                PrepareConfigSave();
            }
        }
        #endregion

        void ProgressUpdate(SimulatorProgressMessage x)
        {
            if (!string.IsNullOrEmpty(x.Message))
            {
                ProgressText = x.Message;
                StageText = string.Empty;
            }
            if (!string.IsNullOrEmpty(x.StageProgressString))
                StageText = x.StageProgressString;
            if (x.OverallProgress != null)
                ProgressValue = (int)Math.Round((float)x.OverallProgress);
        }

        async Task RegularSimulationCore()
        {
            // access signals in UI thread
            Application.Current?.Dispatcher.Invoke(() =>
            {
                PrepareConfigSave();    // save signals to properties XML for further tests
            });

            //todo: prevent modifying SimSettings (or copy) until c++ simulation will started

            IProgress<SimulatorProgressMessage> progress = new Progress<SimulatorProgressMessage>(ProgressUpdate);

            try
            {
                await Simulator.RegularSimulation(SimSettings, progress, manualReset);
            }
            catch (AggregateException ex)
            {
                if (!manualReset.IsSet)
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        MessageBoxService.Show(ex.InnerException.Message, "Simulator error");
                    });
                }
                return;
            }
            catch (Exception ex)
            {
                if (!manualReset.IsSet)
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        MessageBoxService.Show(ex.Message, "Simulator error");
                    });
                }
                return;
            }


            ProgressText = "Preparing results...";
            StageText = string.Empty;

            //read target strategies
            var inResults = new NotifyObservableCollection<SimStrategy>();
            var outResults = new NotifyObservableCollection<SimStrategy>();
            var allResults = new NotifyObservableCollection<SimStrategy>();

            try
            {   //read main simulation data
                int stop_date = SimSettings.StopDateTime.Year * 10000 + SimSettings.StopDateTime.Month * 100 + SimSettings.StopDateTime.Day;
                using (BinaryReader reader = new BinaryReader(File.Open("Data/simOut.data", FileMode.Open)))
                {
                    int r3count = reader.ReadInt32();
                    int oosCount = reader.ReadInt32();
                    int allCount = reader.ReadInt32();

                    Simulator.ReadToList(reader, r3count, inResults, stop_date, SimSettings.SlipMode, SimSettings.CommissMode, SimSettings.AccountCurrency, SimSettings.InvestCashSymId, SimSettings.MarketSymbolsIds);
                    Simulator.ReadToList(reader, oosCount, outResults, stop_date, SimSettings.SlipMode, SimSettings.CommissMode, SimSettings.AccountCurrency, SimSettings.InvestCashSymId, SimSettings.MarketSymbolsIds);
                    Simulator.ReadToList(reader, allCount, allResults, stop_date, SimSettings.SlipMode, SimSettings.CommissMode, SimSettings.AccountCurrency, SimSettings.InvestCashSymId, SimSettings.MarketSymbolsIds);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Can't read input file!\n\nError: " + e.Message, "File read error");
                inResults.Clear();
                outResults.Clear();
                allResults.Clear();
            }

            // read custom strategies
            if (SimSettings.UseCustomStrategies)
            {
                ReadCustomStrategies(SimSettings.AccountCurrency, out List<SimStrategy> customStrategies);
                foreach (var s in customStrategies)
                {
                    inResults.Add(s);
                    outResults.Add(s);
                    allResults.Add(s);
                }
            }

            // read random strategies
            List<SimStrategy> RandomInResults = new List<SimStrategy>();
            List<SimStrategy> RandomOutResults = new List<SimStrategy>();
            List<SimStrategy> RandomAllResults = new List<SimStrategy>();

            // import Random data from file
            if (SimSettings.GenerateRandom)
            {
                Logger.Current.Debug("{0}: start reading random results", nameof(SimTaskWizardViewModel));

                try
                {
                    int stop_date = SimSettings.StopDateTime.Year * 10000 + SimSettings.StopDateTime.Month * 100 + SimSettings.StopDateTime.Day;
                    using (BinaryReader reader = new BinaryReader(File.Open("Data/RandomSimOut.data", FileMode.Open)))
                    {
                        int randomR3count = reader.ReadInt32();
                        int randomOosCount = reader.ReadInt32();
                        int randomAllCount = reader.ReadInt32();

                        Simulator.ReadToList(reader, randomR3count, RandomInResults, stop_date, SimSettings.SlipMode, SimSettings.CommissMode, SimSettings.AccountCurrency, SimSettings.InvestCashSymId, SimSettings.MarketSymbolsIds);
                        Simulator.ReadToList(reader, randomOosCount, RandomOutResults, stop_date, SimSettings.SlipMode, SimSettings.CommissMode, SimSettings.AccountCurrency, SimSettings.InvestCashSymId, SimSettings.MarketSymbolsIds);
                        Simulator.ReadToList(reader, randomAllCount, RandomAllResults, stop_date, SimSettings.SlipMode, SimSettings.CommissMode, SimSettings.AccountCurrency, SimSettings.InvestCashSymId, SimSettings.MarketSymbolsIds);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show("Can't read input file!\n\nError: " + e.Message, "File read error");
                    RandomInResults.Clear();
                    RandomOutResults.Clear();
                    RandomAllResults.Clear();
                }

                Logger.Current.Debug("{0}: reading random results finished", nameof(SimTaskWizardViewModel));
            }


            /*  
                  ResForm res = new ResForm(sim.AccountCurrency, sim.tradeMode, symbols, stopDate,
                      marketSymbolsIds, icSymbolId, sim.SL_ON, sim.sl_mult,
                      (byte)sim.ATR_lookback, sim.AccountValue, vsOther1SymbolId, vsOther2SymbolId, vsOther3SymbolId, false, sim.generateRandom);
            */

            ProgressValue = 100;

            Application.Current?.Dispatcher.Invoke(() =>
            {
                var vm = StrategiesViewModel.Create();
                List<StrategiesListVM> list = new List<StrategiesListVM>()
                {
                    new StrategiesListVM() { Title="IN", Strategies = inResults },
                    new StrategiesListVM() { Title="OUT", Strategies = outResults },
                    new StrategiesListVM() { Title="ALL", Strategies = allResults },
                };
                vm.Parameter = list;
                vm.RandomInResults = RandomInResults;
                vm.RandomOutResults = RandomOutResults;
                vm.RandomAllResults = RandomAllResults;
                vm.Title = IsReRun ? "Re-Run results" : "Simulation results";
                vm.Title += " - " + string.Join(", ", SimSettings.BaseSymbolsIds.Select(x => x.ToString())) + $" ({SimSettings.TradeMode})";
                if(MainWindowViewModel.IsTrial)
                    vm.Title += " - < T_R_I_A_L   V_E_R_S_I_O_N >";
                CreateDocument(vm);
            });

            //try { CurrentWindowService.Close(); } catch { }
            //try { CurrentDialogService.Close(); } catch { }
        }

        async Task ContinuousSimulationCore()
        {
            // access signals from UI thread
            Application.Current?.Dispatcher.Invoke(() =>
            {
                PrepareConfigSave();    // save signals to properties XML for further tests
            });

            //todo: prevent modifying SimSettings (or copy) until c++ simulation will started

            IProgress<SimulatorProgressMessage> progress = new Progress<SimulatorProgressMessage>(ProgressUpdate);

            try
            {
                await Simulator.ContinuousSimulation(SimSettings, progress, manualReset);
            }
            catch (AggregateException ex)
            {
                if (!manualReset.IsSet)
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        MessageBoxService.Show(ex.InnerException.Message, "Simulator error");
                    });
                }
                return;
            }
            catch (Exception ex)
            {
                if (!manualReset.IsSet)
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        MessageBoxService.Show(ex.Message, "Simulator error");
                    });
                }
                return;
            }

            ProgressText = "Continuous Simulation...";
            StageText = string.Empty;

            var inResults = new NotifyObservableCollection<SimStrategy>();
            var outResults = new NotifyObservableCollection<SimStrategy>();
            var allResults = new NotifyObservableCollection<SimStrategy>();

            // read custom strategies
            if (SimSettings.UseCustomStrategies)
            {
                ReadCustomStrategies(SimSettings.AccountCurrency, out List<SimStrategy> customStrategies);
                foreach(var s in customStrategies)
                {
                    inResults.Add(s);
                    outResults.Add(s);
                    allResults.Add(s);
                }
            }

            // read random strategies
            List<SimStrategy> RandomInResults = new List<SimStrategy>();
            List<SimStrategy> RandomOutResults = new List<SimStrategy>();
            List<SimStrategy> RandomAllResults = new List<SimStrategy>();

            // import Random data from file
            if (SimSettings.GenerateRandom)
            {
                Logger.Current.Debug("{0}: start reading random results", nameof(SimTaskWizardViewModel));

                try
                {
                    int stop_date = SimSettings.StopDateTime.Year * 10000 + SimSettings.StopDateTime.Month * 100 + SimSettings.StopDateTime.Day;
                    using (BinaryReader reader = new BinaryReader(File.Open("Data/RandomSimOut.data", FileMode.Open)))
                    {
                        int randomR3count = reader.ReadInt32();
                        int randomOosCount = reader.ReadInt32();
                        int randomAllCount = reader.ReadInt32();

                        Simulator.ReadToList(reader, randomR3count, RandomInResults, stop_date, SimSettings.SlipMode, SimSettings.CommissMode, SimSettings.AccountCurrency, SimSettings.InvestCashSymId, SimSettings.MarketSymbolsIds);
                        Simulator.ReadToList(reader, randomOosCount, RandomOutResults, stop_date, SimSettings.SlipMode, SimSettings.CommissMode, SimSettings.AccountCurrency, SimSettings.InvestCashSymId, SimSettings.MarketSymbolsIds);
                        Simulator.ReadToList(reader, randomAllCount, RandomAllResults, stop_date, SimSettings.SlipMode, SimSettings.CommissMode, SimSettings.AccountCurrency, SimSettings.InvestCashSymId, SimSettings.MarketSymbolsIds);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show("Can't read input file!\n\nError: " + e.Message, "File read error");
                    RandomInResults.Clear();
                    RandomOutResults.Clear();
                    RandomAllResults.Clear();
                }

                Logger.Current.Debug("{0}: reading random results finished", nameof(SimTaskWizardViewModel));
            }


            /*Dictionary<SymbolId, SymbolResData> symbols = new Dictionary<SymbolId, SymbolResData>();
            for (int i = 0; i < checkedSymbolIds.Count; ++i)
                symbols.Add(checkedSymbolIds[i], new SymbolResData(checkedSymbolIds[i].Name, checkedSymbolIds[i].Timeframe, symbolFiles[i], contrMultExternal[i]));

            var marketSymbolsIds = new List<SymbolId>(2);
            if (!M2symbolId.IsEmpty()) marketSymbolsIds.Add(M2symbolId);
            if (!M3symbolId.IsEmpty())
            {
                if (marketSymbolsIds.Count == 0) marketSymbolsIds.Add(SymbolId.Empty);
                marketSymbolsIds.Add(M3symbolId);
            }*/

            /*
            ResForm res = new ResForm(sim.AccountCurrency, sim.tradeMode, symbols, stopDate,
                marketSymbolsIds, icSymbolId, sim.SL_ON, sim.sl_mult,
                (byte)sim.ATR_lookback, sim.AccountValue, vsOther1SymbolId, vsOther2SymbolId, vsOther3SymbolId, true, sim.generateRandom);
            */

            Application.Current?.Dispatcher.Invoke(() =>
            {
                var vm = StrategiesViewModel.Create();
                List<StrategiesListVM> list = new List<StrategiesListVM>()
                {
                    new StrategiesListVM() { Title="IN", Strategies = inResults },
                    new StrategiesListVM() { Title="OUT", Strategies = outResults },
                    new StrategiesListVM() { Title="ALL", Strategies = allResults },
                };
                vm.Parameter = list;
                vm.IsContinuous = true;
                vm.RandomInResults = RandomInResults;
                vm.RandomOutResults = RandomOutResults;
                vm.RandomAllResults = RandomAllResults;
                vm.Title = "Continuous Simulation results";
                vm.Title += " - " + string.Join(", ", SimSettings.BaseSymbolsIds.Select(x => x.ToString())) + $" ({SimSettings.TradeMode})";
                if (MainWindowViewModel.IsTrial)
                    vm.Title += " - < T_R_I_A_L   V_E_R_S_I_O_N >";
                CreateDocument(vm);
            });
        }

        async Task SelectPositiveCore()
        {
            IProgress<SimulatorProgressMessage> progress = new Progress<SimulatorProgressMessage>(ProgressUpdate);

            try
            {
                await Simulator.SelectPositiveSignals(SimSettings, Signals, progress, manualReset);
            }
            catch (AggregateException ex)
            {
                if (!manualReset.IsSet)
                {
                    MessageBoxService.Show(ex.InnerException.Message, "Simulator error");
                }
                return;
            }
            catch (Exception ex)
            {
                if (!manualReset.IsSet)
                {
                    MessageBoxService.Show(ex.Message, "Simulator error");
                }
                return;
            }

            ProgressText = "Preparing signals...";
            StageText = string.Empty;
        }

        public void SetReRunParams(TradeMode tradeMode, IReadOnlyList<SymbolId> baseSymbols, IReadOnlyList<SymbolId> marketSymbols, int startDate, int stopDate, byte PT_ON, float pt_mult, float pt_step,
            byte SL_ON, float sl_mult, float sl_step, byte TL_ON, float tl_mult, float tl_step, byte HH_ON, int hh_look, int hh_step, byte LL_ON, int ll_look, int ll_step, FitnessFunction fitness,
            int max_time, int maxTime_step, int prof_x, int profX_step, int inSampleTrades, int outOfSampleTrades, int oos_percent, IReadOnlyList<Signal> signals, IReadOnlyList<Signal> exitSignals)
        {
            SimSettings.TradeMode = tradeMode;
            SimSettings.BaseSymbolsIds = baseSymbols.ToList();
            SimSettings.MarketSymbolsIds = marketSymbols.ToList();
            SimSettings.StartDateTime = Utils.ToDateTime(startDate, 0);
            SimSettings.StopDateTime = Utils.ToDateTime(stopDate, 235959);
            SimSettings.PT_ON = PT_ON;
            SimSettings.PT_mult = pt_mult;
            SimSettings.PT_delta = pt_step;
            SimSettings.SL_ON = SL_ON;
            SimSettings.SL_mult = sl_mult;
            SimSettings.SL_delta = sl_step;
            SimSettings.TL_ON = TL_ON;
            SimSettings.TL_mult = tl_mult;
            SimSettings.TL_delta = tl_step;
            SimSettings.HH_ON = HH_ON;
            SimSettings.HH_look = hh_look;
            SimSettings.HH_delta = hh_step;
            SimSettings.LL_ON = LL_ON;
            SimSettings.LL_look = ll_look;
            SimSettings.LL_delta = ll_step;
            SimSettings.Fitness = fitness;
            SimSettings.MaxHoldTime = max_time;
            SimSettings.MaxHoldTime_delta = maxTime_step;
            SimSettings.ProfX = prof_x;
            SimSettings.ProfX_delta = profX_step;
            SimSettings.MinInTrades = inSampleTrades;
            SimSettings.MinOutTrades = outOfSampleTrades;
            SimSettings.OutOfSamplePercent = oos_percent / 100.0f;

            //base signals
            SignalGroup gr = new SignalGroup("Base", -1) { IsExpanded = true };
            int pos = 0;
            if (signals != null)
                foreach (var s in signals)
                {
                    var sig = s.MakeClone();
                    sig.GroupId = gr;
                    if (sig is SignalStrategy || sig is SignalEnsemble)
                        sig.CanEdit = false;
                    else
                    {
                        sig.ActiveForEntry = true;
                        gr.CountEntry++;
                    }
                    Signals.Insert(pos++, sig);
                }
            if (exitSignals != null)
                foreach (var s in exitSignals)
                {
                    var sig = s.MakeClone();
                    sig.GroupId = gr;
                    if (sig is SignalStrategy || sig is SignalEnsemble)
                        sig.CanEdit = false;
                    else
                    {
                        sig.ActiveForExit = true;
                        gr.CountExit++;
                    }
                    Signals.Insert(pos++, sig);
                }
        }

        private void ReadCustomStrategies(string currency, out List<SimStrategy> outputList)
        {
            outputList = new List<SimStrategy>();

            if (Properties.Settings.Default.CustomStrategies == null || Properties.Settings.Default.CustomStrategies.Count == 0)
                return;

            List<int> entryDates;
            List<int> entryTimes;
            List<int> exitDates;
            List<int> exitTimes;
            List<float> results;
            List<SimStrategy> resList = new List<SimStrategy>();
            string stratName, symbolName = "", symbolTimeframe = "";
            float cm = 1;
            RAWdata rawdata = null;
            ushort ind = 1;
            Random rnd = new Random(DateTime.Now.Millisecond);

            bool addRes(string symbol, string timeframe, float CM, RAWdata rdata)
            {
                SimStrategy res = new SimStrategy
                {
                    //nameOriginal = stratName;
                    Name = stratName,
                    Symbol = symbol,
                    Timeframe = timeframe,
                    IndxType = StrategySimType.CUSTOM,
                    Indx = ind++,
                    Enabled = true,
                    TradeMode = (SimSettings.TradeMode == TradeMode.LongRebalance ? TradeMode.Long :
                        (SimSettings.TradeMode == TradeMode.ShortRebalance ? TradeMode.Short : SimSettings.TradeMode)),
                    resEntryDates = entryDates.ToArray(),
                    resEntryTimes = entryTimes.ToArray(),
                    resDates = exitDates.ToArray(),
                    results = results.ToArray(),
                    resultsOriginal = results.ToArray(),
                    TradesCount = results.Count,
                    EntrySignalsValues = new BitArray(0),
                    Currency = currency,
                    SignalStartDate = entryDates[0],
                    SignalStartTime = entryTimes[0],
                    SignalStopDate = exitDates.Last(),
                    SignalStopTime = exitTimes.Last(),
                    StopDate = exitDates.Last(),
                    EntryOnClose = 1,   //entry on close by default
                    ExitOnClose = 1,    //exit on close by default
                    // investCash = string.Empty;
                    // Market2 = string.Empty;
                    // Market3 = string.Empty;
                    ForceExitOnSessionEnd = false,
                    SessionEndTime = (ushort)(100 * SimSettings.SessionEndTime.Hour + SimSettings.SessionEndTime.Minute),
                    ATR_len = 20,   //by default
                    // exitSignalsOriginalStr = string.Empty;
                    MaxTime = 9999,
                    ProfX = 9999,
                    ContractMultiplier = CM
                };
                res.SetResTimes(exitTimes.ToArray());

                //pnls
                if (rdata != null && rdata.Closes.Length > 0 && rdata.Dates.Length > 0)
                {
                    List<float> pnls = new List<float>();
                    List<int> pnlDates = new List<int>();
                    List<int> pnlTimes = new List<int>();
                    List<bool> signals = new List<bool>();
                    int i = 0, j = 0;
                    while (i < rdata.Dates.Length && SimStrategy.CompareDateTimes(rdata.Dates[i], rdata.Times[i], entryDates[0], entryTimes[0]) < 0) i++;
                    //   /*if (i > 0)*/ i--;    // use previous date
                    while (i < rdata.Dates.Length && SimStrategy.CompareDateTimes(rdata.Dates[i], rdata.Times[i], exitDates.Last(), exitTimes.Last()) <= 0)
                    {
                        while (j < entryTimes.Count - 1 && SimStrategy.CompareDateTimes(rdata.Dates[i], rdata.Times[i], exitDates[j], exitTimes[j]) > 0)
                            j++;
                        signals.Add((rdata.Dates[i] == entryDates[j] && rdata.Times[i] == entryTimes[j]) || rnd.Next(2) > 0);   //reconstruction with random signal
                        if (rdata.Dates[i] >= entryDates[j] && rdata.Dates[i] <= exitDates[j])
                        {
                            if ((rdata.Dates[i] == entryDates[j] && rdata.Times[i] < entryTimes[j]) || (rdata.Dates[i] == exitDates[j] && rdata.Times[i] > exitTimes[j]))
                                pnls.Add(0);
                            else  //in position
                                pnls.Add(i == 0 ? 0 : (res.IsLong ? rdata.Closes[i] - rdata.Closes[i - 1] : rdata.Closes[i - 1] - rdata.Closes[i]));
                        }
                        else //not in position
                            pnls.Add(0);
                        pnlDates.Add(rdata.Dates[i]);
                        pnlTimes.Add(rdata.Times[i]);
                        i++;
                    }
                    res.daily_pnls = SimStrategy.IntradayToDaily(pnlDates, pnlTimes, pnls).ToArray();
                    res.EntrySignalsValues = new BitArray(signals.ToArray());
                    res.SignalsCount = signals.Count;
                }
                else
                    MessageBox.Show("ERROR! Can't create custom strategy. Dates and Closes lists have no data");
                //eratio
                res.ERatio = StrategiesViewModel.ERatioValueForStrategy(res);
                // ExpScorePerfectProfitForStrategy
                StrategiesViewModel.ExpScorePerfectProfitForStrategy(res, currency,
                    out List<float> expScore, out List<float> perfectProfitPercentage, out List<float> perfectProfitCorrelation);
                res.ExpectancyScore = expScore.LastOrDefault();
                res.PerfectProfitPercentage = perfectProfitPercentage.LastOrDefault();
                res.PerfectProfitCorrelation = perfectProfitCorrelation.LastOrDefault();
                // reset
                entryDates = null; exitDates = null; results = null;
                stratName = ""; symbolName = ""; symbolTimeframe = "";
                res.RecalcParams();
                resList.Add(res);
                return true;
            }

            foreach (string file in Properties.Settings.Default.CustomStrategies)
            {
                if (file[0] == '0') continue;

                entryDates = null; entryTimes = null; exitDates = null; exitTimes = null; results = null;
                stratName = "";
                string line = "";
                int lineN = 0;
                try
                {
                    using (StreamReader reader = new StreamReader(File.OpenRead(file.Substring(1))))
                    {
                        while (!reader.EndOfStream)
                        {
                            line = reader.ReadLine();
                            lineN++;
                            if (line.Length < 3) continue;
                            if (line[0] == '#')    //first custom strategy info line
                            {
                                if (results != null)   //save pervious strategy
                                    addRes(symbolName, symbolTimeframe, cm, rawdata);

                                string[] strat = line.Substring(1).Split(',');
                                if (strat.Length >= 3)
                                {
                                    strat[0] = strat[0].Trim();
                                    strat[1] = strat[1].Trim();
                                    strat[2] = strat[2].Trim();

                                    SymbolId sid = new SymbolId(strat[0], "");

                                    //if same symbol and contract type
                                    if (RAWdataStorage.GetRawData(Utils.SymbolsManager.GetSymbolFileNameMult(sid, out double mult), sid) is RAWdata rdata &&
                                        (strat[2][0] == 'L' || strat[2][0] == 'l') == SimSettings.TradeMode.ToString().StartsWith("L"))
                                    {
                                        stratName = strat[1];
                                        int indexOfLast_ = strat[0].LastIndexOf('_');
                                        symbolName = indexOfLast_ >= 0 ? strat[0].Substring(0, indexOfLast_) : strat[0];
                                        symbolTimeframe = (indexOfLast_ >= 0 && indexOfLast_ + 1 < strat[0].Length) ? strat[0].Substring(indexOfLast_ + 1) : string.Empty;
                                        cm = (float)mult;
                                        rawdata = rdata;
                                    }
                                }
                                else throw (new Exception("Wrong strategy header line format"));
                            }
                            else if (!string.IsNullOrEmpty(stratName))
                            {
                                string[] values = line.Split(',');
                                if (values.Length >= 5)
                                {
                                    if (results == null)
                                    {
                                        entryDates = new List<int>(); entryTimes = new List<int>();
                                        exitDates = new List<int>(); exitTimes = new List<int>();
                                        results = new List<float>();
                                    }
                                    DateTime dateTime = DateTime.ParseExact(values[0].Trim(), "yyyyMMdd", CultureInfo.InvariantCulture);
                                    //if (dateTime.DayOfWeek == DayOfWeek.Saturday) dateTime.AddDays(-1);
                                    //else if (dateTime.DayOfWeek == DayOfWeek.Sunday) dateTime.AddDays(-2);
                                    entryDates.Add(dateTime.Year * 10000 + dateTime.Month * 100 + dateTime.Day);
                                    entryTimes.Add(int.Parse(values[1]));

                                    DateTime dateTimeEx = DateTime.ParseExact(values[2].Trim(), "yyyyMMdd", CultureInfo.InvariantCulture);
                                    //if (dateTimeEx.DayOfWeek == DayOfWeek.Saturday) dateTimeEx.AddDays(2);
                                    //else if (dateTimeEx.DayOfWeek == DayOfWeek.Sunday) dateTimeEx.AddDays(1);
                                    exitDates.Add(dateTimeEx.Year * 10000 + dateTimeEx.Month * 100 + dateTimeEx.Day);
                                    exitTimes.Add(int.Parse(values[3]));
                                    results.Add(float.Parse(values[4].Trim('"'), CultureInfo.InvariantCulture));
                                }
                                else throw (new Exception("Wrong strategy value line format"));
                            }
                        }
                    }
                    lineN = 0;
                    if (!string.IsNullOrEmpty(stratName) && results != null)   //save last strategy
                        try
                        {
                            addRes(symbolName, symbolTimeframe, cm, rawdata);
                        }
                        catch (Exception ex) { throw (new Exception("Adding custom strategy cause an error\n" + ex.Message)); }
                }
                catch (Exception ex)
                {
                    string l = "";
                    if (lineN > 0)
                        l = string.Format("\n\nLine ({0}): {1}", lineN, line);
                    MessageBoxService.Show(string.Format("Can't read input file!\n\n{0}{1}\n\nError: {2}", file.Substring(1), l, ex.Message), "Custom Strategy file read error");
                }
            }

            outputList = resList;
        }

        public static /*async*/ NotifyObservableCollection<Signal> LoadSignalsFromXML(string xml, bool defaultCnot0 = true)
        {
            NotifyObservableCollection<Signal> signalsList = /*await*/ SignalsFactory.DeserializeSignals(xml);
            if (signalsList != null)
            {
                if (defaultCnot0 && signalsList.Count > 0 && signalsList[0].Key == "Cnot0")
                    signalsList[0].CanEdit = false;

                List<SymbolId> viewSymbols = new List<SymbolId>() { new SymbolId("ES", ""), new SymbolId("ES", ""), new SymbolId("ES", ""), new SymbolId("Vix", "") };

                SignalsFactory.UpdateCustomSignals(viewSymbols, signalsList, MainWindowViewModel.PYTHON_READY);
                SignalsFactory.UpdatePortfolioSignals(signalsList, Utils.portfolioFileName);    //todo: read from memory not file
            }
            return signalsList;
        }

        #region Load and Save Configuration
        private void UpdateRecentConfFilesMenu()
        {
            if (Properties.Settings.Default.RecentConfigFiles?.Count > 0)
            {
                RecentConfigFiles.Clear();

                for (int j = Properties.Settings.Default.RecentConfigFiles.Count - 1; j >= 0; --j)
                {
                    string fileName = Properties.Settings.Default.RecentConfigFiles[j];
                    RecentConfigFiles.Add(new KeyValuePair<string, string>(Path.GetFileName(fileName), fileName));
                }
            }
        }

        private void OpenRecentConfig_internal(object o)
        {
            if (o is string fname)
            {
                Configuration conf = Configuration.Load(fname);

                if (conf != null)
                {
                    conf.CopyToAppSettings();

                    LoadSimSettings();

                    //force load signals
                    Signals = null;

                    //reload symbols
                    Utils.SymbolsManager.LoadSymbols(Properties.Settings.Default.SymbolsXML);

                    CurrentConfigFileName = fname;

                    CurrentConfigFileName = Path.GetFileName(fname);
                    CurrentConfigFileFullName = fname;

                    Properties.Settings.Default.RecentConfigFiles.Remove(fname);
                    Properties.Settings.Default.RecentConfigFiles.Add(fname);

                    UpdateRecentConfFilesMenu();
                }
                else
                    MessageBoxService.Show("Can't load configuration from given file!", "Trader's Toolbox");
            }
        }

        private void LoadConfiguration_internal()
        {
            OpenFileDialogService.Filter = "Configuration files (*.config)|*.config|All files (*.*)|*.*";
            if (OpenFileDialogService.ShowDialog())
            {
                IFileInfo file = OpenFileDialogService.Files.First();

                try
                {
                    Configuration conf = Configuration.Load(file.GetFullName());
                    if (conf != null)
                    {
                        conf.CopyToAppSettings();

                        LoadSimSettings();

                        //force load signals
                        Signals = null;

                        //reload symbols
                        Utils.SymbolsManager.LoadSymbols(Properties.Settings.Default.SymbolsXML);
                        //UpdateCheckBoxesOnSymbolsUpdated();
                        // force select basesymbols,m2,m3,..., signalsview m2/m3 enabled


                        CurrentConfigFileName = file.Name;
                        CurrentConfigFileFullName = file.GetFullName();
                        if (Properties.Settings.Default.RecentConfigFiles == null)
                            Properties.Settings.Default.RecentConfigFiles = new System.Collections.Specialized.StringCollection();
                        if (Properties.Settings.Default.RecentConfigFiles.Contains(file.Name))
                            Properties.Settings.Default.RecentConfigFiles.Remove(file.Name);

                        Properties.Settings.Default.RecentConfigFiles.Add(file.Name);
                        while (Properties.Settings.Default.RecentConfigFiles.Count > 10)
                            Properties.Settings.Default.RecentConfigFiles.RemoveAt(0);

                        UpdateRecentConfFilesMenu();
                    }
                    else
                        MessageBoxService.Show("Can't load configuration from given file!", "Trader's Toolbox");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Reading configuration throws an exception!\n\n" + ex.Message);
                }
            }
        }

        private void SaveConfiguration_internal()
        {
            if (string.IsNullOrEmpty(CurrentConfigFileFullName))
                SaveConfigurationAs_internal();
            else
            {
                PrepareConfigSave();

                Configuration conf = new Configuration();
                conf.CopyFromAppSettings();
                if (conf.Save(CurrentConfigFileFullName) == false)
                    MessageBoxService.Show("Can't save configuration to file!", "Trader's Toolbox");
            }
        }

        private void SaveConfigurationAs_internal()
        {
            SaveFileDialogService.DefaultFileName = "AppConfig";
            SaveFileDialogService.DefaultExt = ".config";
            SaveFileDialogService.Filter = "Configuration files (*.config)|*.config|All files (*.*)|*.*";

            if (SaveFileDialogService.ShowDialog())
            {
                string fn = SaveFileDialogService.File.GetFullName();

                PrepareConfigSave();

                Configuration conf = new Configuration();
                conf.CopyFromAppSettings();
                if (conf.Save(fn) == false)
                    MessageBoxService.Show("Can't save configuration to selected file!", "Trader's Toolbox");
                else
                {   //successfull
                    CurrentConfigFileName = SaveFileDialogService.File.Name;
                    CurrentConfigFileFullName = fn;
                    if (Properties.Settings.Default.RecentConfigFiles == null)
                        Properties.Settings.Default.RecentConfigFiles = new System.Collections.Specialized.StringCollection();
                    if (Properties.Settings.Default.RecentConfigFiles.Contains(fn))
                        Properties.Settings.Default.RecentConfigFiles.Remove(fn);

                    Properties.Settings.Default.RecentConfigFiles.Add(fn);
                    while (Properties.Settings.Default.RecentConfigFiles.Count > 10)
                        Properties.Settings.Default.RecentConfigFiles.RemoveAt(0);

                    UpdateRecentConfFilesMenu();
                }
            }
        }

        private void ResetConfiguration_internal()
        {
            if (MessageBoxService.Show("Do you really want to reset configuration?", "Trader's Toolbox", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                Configuration conf = new Configuration();
                conf.CopyToAppSettings();

                LoadSimSettings();

                Signals = null;
                Utils.SymbolsManager.LoadSymbols();
                //UpdateCheckBoxesOnSymbolsUpdated();

                CurrentConfigFileName = CurrentConfigFileFullName = null;
            }
        }

        private void PrepareConfigSave()
        {
            // save favorite signals
            var favCol = new System.Collections.Specialized.StringCollection();
            foreach (Signal sig in Signals)
                if (sig.IsFavorite)
                    favCol.Add(sig.Key);
            Properties.Settings.Default.FavoriteSignals = favCol;

            // save all signals (including custom parametric signals groups)
            try
            {
                Properties.Settings.Default.SignalsXML = Serializer.Serialize(Signals, true);
            }
            catch (Exception ex)
            {
                Logger.Current.Warn(ex, "Can't serialize signals to settings");
            }

            // save simulation settings
            try
            {
                var s = Serializer.Serialize(SimSettings);
                if (Properties.Settings.Default.SimConfigs == null)
                    Properties.Settings.Default.SimConfigs = new System.Collections.Specialized.StringCollection() { s };
                else if (Properties.Settings.Default.SimConfigs.Count == 0)
                    Properties.Settings.Default.SimConfigs.Add(s);
                else
                    Properties.Settings.Default.SimConfigs[0] = s;
            }
            catch (Exception ex)
            {
                Logger.Current.Warn(ex, "Can't serialize configuration to settings");
            }
        }
        #endregion
    }
}