//#define SKIP_DATA_UPDATE

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Threading;
using DevExpress.Mvvm;
using DevExpress.Mvvm.POCO;
using DevExpress.Xpf.Bars;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Docking;
using DevExpress.Mvvm.DataAnnotations;
using System.Windows.Media;
using System.Collections.Specialized;
using System.Windows.Input;
using System.Linq;
using DevExpress.Data.Async.Helpers;
using System.Windows;
using TradersToolbox.Views;
using TradersToolbox.Core;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Runtime.Serialization.Json;
using Microsoft.Win32;
using Python.Runtime;
using System.IO;
using System.Globalization;
using System.ComponentModel;
using System.Threading.Tasks;


using SciChart.Core.Extensions;
using DevExpress.Xpf.Editors;
using System.Text.RegularExpressions;
using DevExpress.Xpf.Charts;
using TradersToolbox.Brokers;
using TradersToolbox.Data;
using TradersToolbox.DataObjects;
using TradersToolbox.DataSources;

namespace TradersToolbox.ViewModels
{
    [POCOViewModel]
    public class MainWindowViewModel
    {
        public static string pythonPath = string.Empty;
        public static bool PYTHON_READY;
        public static DBManager tradeDB = new DBManager();
        public static readonly string hostPath = "http://buildalpha.com/traderstoolbox/DataUnified/";
        public static readonly string LocalAppSettingsPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        public static readonly string LocalAppDataPath = Path.Combine(LocalAppSettingsPath, "Build_Alpha\\TradersToolboxData");

        // Security
        public static string serverAnswerString;
        public static DateTime serverAnswerDT;

        public static MainWindowViewModel main_form;

        public static BrokersManager BrokersManager { get; private set; }

        static HttpClient httpClient_internal;
        public static HttpClient HttpClient
        {
            get
            {
                if (httpClient_internal == null)
                    httpClient_internal = HttpClientHelper.GetHttpClient();
                return httpClient_internal;
            }
            set
            {
                httpClient_internal = value;
            }
        }

        public static bool IsTrial => serverAnswerDT != null && serverAnswerDT > DateTime.Now && (serverAnswerDT - DateTime.Now).TotalDays < 30;
        public string Title => $"Trader's Toolbox{(IsTrial ? " - < T_R_I_A_L   V_E_R_S_I_O_N >" : "")}"
#if DRW
            + " - Private"
#endif
            ;

        #region Data updater properties
        /// <summary>
        /// Error message or DateTime of successful update
        /// </summary>
        public virtual string DBupdaterMessage { get; set; }

        /// <summary>
        /// -1 - error occured while downloading data, show error message
        /// 0 - no update required, show info message
        /// (0,100) - update in progress
        /// 100 - update complete, show message
        /// </summary>
        public virtual int DBupdaterProgress { get; set; }
        #endregion


        //protected IDocumentManagerService DocumentManagerService { get { return this.GetService<IDocumentManagerService>(); } }
        protected IWindowService WindowServiceInd { get { return this.GetService<IWindowService>("WindowServiceIndicators"); } }
        protected IWindowService WindowServiceStr { get { return this.GetService<IWindowService>("WindowServiceStrategies"); } }
        protected IWindowService WindowServiceSimTask { get { return this.GetService<IWindowService>("WindowServiceSimTask"); } }
        protected IWindowService WindowServiceSimTrading { get { return this.GetService<IWindowService>("WindowServiceSimTrading"); } }
        protected IWindowService WindowServiceSettings { get { return this.GetService<IWindowService>("WindowServiceSettings"); } }
        protected IOpenFileDialogService OpenFileDialogService { get { return this.GetService<IOpenFileDialogService>(); } }
        protected IDialogService DialogServiceSettings { get { return this.GetService<IDialogService>("Order"); } }
        protected IMessageBoxService MessageBoxService { get { return this.GetService<IMessageBoxService>(/*ServiceSearchMode.PreferParents*/); } }


        //CommandViewModel openSimTask;
        //CommandViewModel openConfig;
        //CommandViewModel saveConfig;
        //CommandViewModel saveConfigAs;
        CommandViewModel saveWorkspace;
        CommandViewModel loadWorkspace;
        CommandViewModel marketOverview;
        //CommandViewModel chart;
        CommandViewModel portfolio;
        CommandViewModel logger;
        CommandViewModel tradeview;
        ChartWindowViewModel chartWindowViewModel;
        ObservableCollection<WorkspaceViewModel> workspaces;

        

        public MainWindowViewModel()
        {
            main_form = this;

            // Set up PATH for dynamic load correct dll for Python
            try
            {
                pythonPath = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\TradersToolbox", "TTPython36Path", "");
                if (!string.IsNullOrEmpty(pythonPath))
                {
                    var path = $"{pythonPath};{Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine)}";
                    Environment.SetEnvironmentVariable("PATH", path, EnvironmentVariableTarget.Process);
                    PythonEngine.PythonHome = pythonPath;
                    PYTHON_READY = true;
                }
            }
            catch { pythonPath = string.Empty; }
            //---------------------------

            // Install Plotly on first run (unzip archive)
            if (File.Exists("plotly.zip") && !Directory.Exists("plotly"))
            {
                try
                {
                    System.IO.Compression.ZipFile.ExtractToDirectory("plotly.zip", "plotly");
                }
                catch (Exception ex)
                {
                    Logger.Current.Warn(ex, "Can't unzip plotly library");
                }
            }

            // initialize symbols manager with custom symbols from settings
            Utils.SymbolsManager.LoadSymbols(Properties.Settings.Default.SymbolsXML);

            // load old style signals
            SignalsData.LoadSignals(Properties.Resources.SignalsDescription, Properties.Settings.Default.FavoriteSignals);

            Messenger.Default.Register<QuoteSelectedMessage>(this, OnMessage);

            // initialize global brokers manager
            BrokersManager = new BrokersManager();
            
            // layout
            QuotesViewModel = CreatePanelWorkspaceViewModel<QuotesViewModel>();
            PortfolioViewModel = CreatePanelWorkspaceViewModel<PortfolioViewModel>();
            LogViewModel = CreatePanelWorkspaceViewModel<LogViewModel>();
            TradeViewModel = CreatePanelWorkspaceViewModel<TradeViewModel>();
            SimTaskWizardViewModel = SimTaskWizardViewModel.Create();
            SimulatedTradingViewModel = SimulatedTradingViewModel.Create();
            Bars = new ReadOnlyCollection<BarModel>(CreateBars());
            InitDefaultLayout();

            tradeDB.initializeDatabase();

            // security
            if (GenerateSignalsToTrade.m1 == null)
            {
                if (System.Threading.Mutex.TryOpenExisting("GUID-1245-6722-A25F-TTSEC-Mutex", out GenerateSignalsToTrade.m1) == false)
                {
                    GenerateSignalsToTrade.m1 = new System.Threading.Mutex(true, "GUID-1245-6722-A25F-TTSEC-Mutex", out bool mutexWasCreated);
#if DEBUG
                    Debug.WriteLine(mutexWasCreated ? "Mutex1 created successfully" : "ERROR: can't create mutex1!");
#endif
                }
            }

            Messenger.Default.Register<LinkData>(this, (action) => ReceiveDoSomethingMessage(action));
        }

        public ReadOnlyCollection<BarModel> Bars { get; private set; }
        public QuotesViewModel QuotesViewModel { get; set; }
        public PortfolioViewModel PortfolioViewModel { get; set; }
        public LogViewModel LogViewModel { get; set; }
        public TradeViewModel TradeViewModel { get; set; }
        public SimTaskWizardViewModel SimTaskWizardViewModel { get; set; }
        public SimulatedTradingViewModel SimulatedTradingViewModel { get; set; }
        public CustomStrategiesEditorViewModel CustomStrategiesEditorViewModel { get; set; }
        public CustomIndicatorEditorViewModel customIndicatorEditorViewModel { get; set; }
        public ChartWindowViewModel TabViewModel
        {
            get
            {
                if (chartWindowViewModel == null)
                {
                    chartWindowViewModel = CreatePanelWorkspaceViewModel<ChartWindowViewModel>();
                }
                return chartWindowViewModel;
            }
        }

        public ChartWindowViewModel CreateChartWindow()
        {
            return CreatePanelWorkspaceViewModel<ChartWindowViewModel>();
        }

        public QuotesViewModel CreateQuotesWindow()
        {
            return CreatePanelWorkspaceViewModel<QuotesViewModel>();
        }

        public ObservableCollection<WorkspaceViewModel> Workspaces
        {
            get
            {
                if (workspaces == null)
                {
                    workspaces = new ObservableCollection<WorkspaceViewModel>();
                    workspaces.CollectionChanged += OnWorkspacesChanged;
                }
                return workspaces;
            }
        }
        protected virtual IDockingSerializationDialogService SaveLoadLayoutService { get { return null; } }

        void OnMessage(QuoteSelectedMessage message)
        {
            var newChart = CreateChartWindow();   // todo: open in floating group
            newChart.UpdateSymbol(message.Symbol);
            newChart.DestroyOnClose = true; 
            OpenOrCloseWorkspace(newChart);
        }


        List<BarModel> CreateBars()
        {
            return new List<BarModel>() {
                new BarModel("Main") { IsMainMenu = true, Commands = CreateCommands() },
                //new BarModel("Standard") { Commands = CreateToolbarCommands() }
            };
        }
        List<CommandViewModel> CreateCommands()
        {
            return new List<CommandViewModel> {
                new CommandViewModel("File", CreateFileCommands()),
                new CommandViewModel("View", CreateViewCommands()),
                new CommandViewModel("Trade", CreateTradeCommands()),
                new CommandViewModel("Workspaces", CreateLayoutCommands()),
                new CommandViewModel("Simulation", CreateSimulationCommands()),
                new CommandViewModel("Live Mode", CreateLiveModeCommands()),
                new CommandViewModel("Tools", CreateToolsCommands()),
                new CommandViewModel("Help", CreateAboutCommands())
            };
        }

        List<CommandViewModel> CreateFileCommands()
        {
            CommandViewModel customStrategiesEditorCommand = new CommandViewModel("Custom Strategies Editor", new DelegateCommand(ShowCustomStrategiesEditor))
            { /*Glyph = Images.Save,*/ KeyGesture = new KeyGesture(Key.F3) };
            CommandViewModel customIndicatorsEditorCommand = new CommandViewModel("Custom Indicators Editor", new DelegateCommand(ShowCustomIndicatorsEditor))
            { /*Glyph = Images.Save,*/ KeyGesture = new KeyGesture(Key.F4) };

            //CommandViewModel openCommand = new CommandViewModel("Open") { IsSubItem = true, };
            //openSimTask = new CommandViewModel("Simulation Task...")
            //{
            //    //Glyph = Images.OpenSolution,
            //    IsEnabled = false,
            //    KeyGesture = new KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Shift),
            //};
            //openConfig = new CommandViewModel("Load Configuration"/*, fileOpenCommand*/) { /*Glyph = Images.OpenFile,*/ KeyGesture = new KeyGesture(Key.L, ModifierKeys.Control) };
            ////openCommand.Commands = new List<CommandViewModel>() { openSimTask, openConfig };
            //
            //saveConfig = new CommandViewModel("Save Configuration") { /*Glyph = Images.Save,*/ KeyGesture = new KeyGesture(Key.S, ModifierKeys.Control) };
            //saveConfigAs = new CommandViewModel("Save Configuration As...") { /*Glyph = Images.SaveAll,*/ KeyGesture = new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift) };
            //
            //var recentConfigs = new CommandViewModel("Recent Configurations") { IsSubItem = true, IsEnabled = false };
            //var resetConfig = new CommandViewModel("Reset Configuration");

            var quitCommand = new CommandViewModel("Quit", new DelegateCommand(() => { Application.Current.Shutdown(); }))
            { /*Glyph = Images.Save,*/ KeyGesture = new KeyGesture(Key.F4, ModifierKeys.Alt) };

            return new List<CommandViewModel>() { customStrategiesEditorCommand, customIndicatorsEditorCommand, GetSeparator(),
                /*openCommand, GetSeparator(),*/ /*openConfig, saveConfig, saveConfigAs, recentConfigs, resetConfig, GetSeparator(),*/ quitCommand };
        }

        protected virtual List<CommandViewModel> CreateToolsCommands()
        {
            var pythonIDECommand = new CommandViewModel("Python Dev Environment", new DelegateCommand(() =>
            {
                try
                {
                    Process.Start(Path.Combine(pythonPath, "pythonw.exe"), "\"" + Path.Combine(pythonPath, @"Lib\idlelib\idle.pyw") + "\"");
                }
                catch (Exception ex)
                {
                    MessageBoxService.Show("Can't run python IDLE!\n\n" + ex.Message);
                }
            }))
            { /*Glyph = Images.Save,*/ /*KeyGesture = new KeyGesture(Key.F4, ModifierKeys.Alt)*/ IsEnabled = PYTHON_READY };
            var pythonConsolCommand = new CommandViewModel("Python Console Interpreter", new DelegateCommand(() =>
            {
                try
                {
                    Process.Start(Path.Combine(pythonPath, "python.exe"));
                }
                catch (Exception ex)
                {
                    MessageBoxService.Show("Can't run python interpreter!\n\n" + ex.Message);
                }
            }))
            { /*Glyph = Images.Save,*/ /*KeyGesture = new KeyGesture(Key.F4, ModifierKeys.Alt)*/ IsEnabled = PYTHON_READY };


            var settingsCommand = new CommandViewModel("Settings...", new DelegateCommand(() =>
            {
                WindowServiceSettings.Show(nameof(SettingsView), SettingsViewModel.Create());
            }))
            { KeyGesture = new KeyGesture(Key.F8) };

            // todo: mvvm for proxy settings
            var proxySettingsCommand = new CommandViewModel("Proxy Settings...", new DelegateCommand(() =>
            {
                ProxySettingsWindow ps = new ProxySettingsWindow();
                ps.ShowDialog();
                if (HttpClientHelper.IsUsingProxy() && ps.IsOk)
                {
                    HttpClient = null;
                    _ = BrokersManager.ProxySettingsChanged();
                }
            }));

            //var logCommand = new CommandViewModel("Application Log");
            return new List<CommandViewModel>() { pythonIDECommand, pythonConsolCommand, GetSeparator(), settingsCommand, GetSeparator(), proxySettingsCommand/*, logCommand*/ };
        }
        protected virtual List<CommandViewModel> CreateLayoutCommands()
        {
            loadWorkspace = new CommandViewModel("Load Workspace...", new DelegateCommand(OnLoadLayout)) { /*Glyph = Images.LoadLayout*/ };
            saveWorkspace = new CommandViewModel("Save Workspace...", new DelegateCommand(OnSaveLayout)) { /*Glyph = Images.SaveLayout*/ };
            return new List<CommandViewModel>() { loadWorkspace, saveWorkspace };
        }
        protected virtual List<CommandViewModel> CreateSimulationCommands()
        {
            CommandViewModel simTaskWizardCommand = new CommandViewModel("Simulation Task Wizard", new DelegateCommand(ShowSimTaskWizard))
            { /*Glyph = Images.Save,*/ KeyGesture = new KeyGesture(Key.N, ModifierKeys.Control) };

            return new List<CommandViewModel>() { simTaskWizardCommand };
        }
        protected virtual List<CommandViewModel> CreateViewCommands()
        {
            marketOverview = GetShowCommand(QuotesViewModel);
            //chart = GetShowCommand(TabViewModel);
            portfolio = GetShowCommand(PortfolioViewModel);
            logger = GetShowCommand(LogViewModel);
            tradeview = GetShowCommand(TradeViewModel);

            var themesCommands = new List<CommandViewModel>()
            {
                new CommandViewModel("Dark", new DelegateCommand<Theme>((t) => ApplicationThemeHelper.ApplicationThemeName = Theme.VS2019DarkName)),
                new CommandViewModel("Light", new DelegateCommand<Theme>((t) => ApplicationThemeHelper.ApplicationThemeName = Theme.VS2019LightName))
            };
            CommandViewModel themesSwitcher = new CommandViewModel("Themes", themesCommands) { IsEnabled = true, IsSubItem = true };
            return new List<CommandViewModel>() { marketOverview,/* chart,*/ portfolio, logger, tradeview, GetSeparator(), themesSwitcher };
        }

        protected virtual List<CommandViewModel> CreateTradeCommands()
        {
            /*
            var orderTradeCommand = new CommandViewModel("New Order", new DelegateCommand(() =>
            {
                WindowServiceSettings.Show(nameof(OrerSimulator), OrerSimulatorViewModel.Create());
            }))
            { KeyGesture = new KeyGesture(Key.F7) };
            */

            var orderTradeCommand = new CommandViewModel("New Order", new DelegateCommand(() =>
            {
                var result = BrokersManager.SuggestSymbols("", 10000);
                OrderSimulator orderDlg = new OrderSimulator();
                orderDlg.ShowDialog();

            }));
            return new List<CommandViewModel>() { orderTradeCommand };
        }

        protected virtual List<CommandViewModel> CreateLiveModeCommands()
        {
            var TSLogInCommand = new DelegateCommand(TradeStationLogIn);
            return new List<CommandViewModel>() { new CommandViewModel("TradeStation Log In", TSLogInCommand) { /*Glyph = Images.About*/ } };
        }
        protected virtual List<CommandViewModel> CreateAboutCommands()
        {
            //var showAboutCommnad = new DelegateCommand(ShowAbout);
            var cmdList = new List<CommandViewModel>() { new CommandViewModel(
                "BuildAlpha © 2016-2021, v" + System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString()
                /*"About"*//*, showAboutCommnad*/) { /*Glyph = Images.About*/ } };

            // Add licence info to about menu item
            if (!string.IsNullOrEmpty(serverAnswerString) && serverAnswerDT != null)
            {
                CommandViewModel licenceInfoCommand = new CommandViewModel($"License valid until: {serverAnswerDT.ToShortDateString()}") { IsEnabled = false };
                cmdList.Add(GetSeparator());
                cmdList.Add(licenceInfoCommand);
            }
            return cmdList;
        }

        CommandViewModel GetSeparator()
        {
            return new CommandViewModel() { IsSeparator = true };
        }
        CommandViewModel GetShowCommand(PanelWorkspaceViewModel viewModel)
        {
            return new CommandViewModel(viewModel, new DelegateCommand(() => OpenOrCloseWorkspace(viewModel)));
        }

        void InitDefaultLayout()
        {
            var panels = new List<PanelWorkspaceViewModel> { QuotesViewModel, PortfolioViewModel, LogViewModel, TradeViewModel };

            foreach (var panel in panels)
            {
                OpenOrCloseWorkspace(panel, false);
            }
        }

        protected T CreatePanelWorkspaceViewModel<T>() where T : PanelWorkspaceViewModel
        {
            return ViewModelSource<T>.Create();
        }

        protected void OpenOrCloseWorkspace(PanelWorkspaceViewModel workspace, bool activateOnOpen = true)
        {
            if (Workspaces.Contains(workspace))
            {
                workspace.IsClosed = !workspace.IsClosed;
                if (workspace.IsClosed)
                {
                    Workspaces.Remove(workspace);
                }
            }
            else
            {
                Workspaces.Add(workspace);
                workspace.IsClosed = false;
            }
            if (activateOnOpen && workspace.IsOpened)
                SetActiveWorkspace(workspace);
                       
        }

        private void ReceiveDoSomethingMessage(LinkData msg)
        {
            foreach(PanelWorkspaceViewModel view in Workspaces)
            {
                if (view is ChartWindowViewModel chartview)
                {

                    if (chartview.SelectedlinkDataItem.LinkColorName == msg.LinkColorName && msg.LinkColorName != "Transparent" && chartview.SelectedlinkDataItem.LinkSymbolName != msg.LinkSymbolName)
                    {
                        chartview.UpdateSymbol(msg.LinkSymbolName);
                    }

                }
            }
        }

        void SetActiveWorkspace(WorkspaceViewModel workspace)
        {
            workspace.IsActive = true;
        }
        //bool ActivateDocument(string path)
        //{
        //    var document = GetDocument(path);
        //    bool isFound = document != null;
        //    if (isFound) document.IsActive = true;
        //    return isFound;
        //}

        /*     void UpdateOnTimer(object sender, EventArgs e)
             {
                 dataProvider.UpdateData();
                 if (DocumentManagerService != null)
                 {
                     foreach (var document in DocumentManagerService.Documents)
                     {
                         TabViewModel tabModel = document.Content as TabViewModel;
                         if (tabModel != null)
                             tabModel.UpdateData();
                     }
                     if (DocumentManagerService.ActiveDocument != null)
                     {
                         TradingDataSource tradingSource = dataProvider.GetDataSource(((TabViewModel)DocumentManagerService.ActiveDocument.Content).Symbol);
                         InformationPanelModel.UpdateData(tradingSource.PreviousPrice,
                                                                  tradingSource.CurrentPrice,
                                                                  tradingSource.PriceDayAgo,
                                                                  tradingSource.Change24,
                                                                  tradingSource.High24,
                                                                  tradingSource.Low24,
                                                                  tradingSource.Volume24);
                     }
                 }
                 CurrentTime = DateTime.Now;
             }*/

        void OnLoadLayout()
        {
            var models = SaveLoadLayoutService.LoadLayout<PanelWorkspaceViewModel>();

            if (models != null)
            {
                DXSplashScreen.Show<WorkspaceLoadingSplashScreen>();

                Workspaces.Clear();

                foreach (var model in models)
                {
                    if (model is ChartWindowViewModel m)
                        m.InitAfterLoad();
                    if (model is QuotesViewModel q)
                        q.InitAfterLoad();

                    Workspaces.Add(model);
                }

                SaveLoadLayoutService.RestoreLastLoadedLayout();

                DXSplashScreen.Close();
            }
        }
        void OnSaveLayout()
        {
            SaveLoadLayoutService.SaveLayout(this.workspaces.Where(var => var is PanelWorkspaceViewModel && !var.IsClosed).Select(var=>var as PanelWorkspaceViewModel).ToList());
        }

        void OnWorkspacesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null && e.NewItems.Count != 0)
                foreach (WorkspaceViewModel workspace in e.NewItems)
                    workspace.RequestClose += OnWorkspaceRequestClose;
            if (e.OldItems != null && e.OldItems.Count != 0)
                foreach (WorkspaceViewModel workspace in e.OldItems)
                    workspace.RequestClose -= OnWorkspaceRequestClose;
        }
        void OnWorkspaceRequestClose(object sender, EventArgs e)
        {
            if (sender is PanelWorkspaceViewModel workspace)
            {
                workspace.IsClosed = true;
                if (workspace.DestroyOnClose)
                {
                    workspace.Dispose();
                    Workspaces.Remove(workspace);
                }
                //if (workspace is DocumentViewModel)
                //{
                //    workspace.Dispose();
                //    Workspaces.Remove(workspace);
                //}
            }
        }

        public void Loaded()
        {
            SplashScreenManager.CloseAll();

            // try to log in
            _ = BrokerLogIn();     // non-awaitable async call

            // Update data
#if SKIP_DATA_UPDATE
            DBupdaterMessage = "Updates are turned off";
            if (Properties.Settings.Default.DataVersionDate.Year == 1900)
                DBupdaterMessage += "\nDatabase version: default";
            else if (Properties.Settings.Default.DataVersionDate.Year == 1899)
                DBupdaterMessage += "\nLast database update failed";
            else
                DBupdaterMessage += "\nDatabase version: " + Properties.Settings.Default.DataVersionDate;
#else
            if (!File.Exists("TurnOffUpdates.please"))
            {
                Logger.Current.Info("DatabaseUpdatePeriod: {0}", Properties.Settings.Default.DatabaseUpdatePeriod);
                Logger.Current.Info("DatabaseVersionDate: {0}", Properties.Settings.Default.DataVersionDate);

                if (Properties.Settings.Default.DatabaseUpdatePeriod < 0 ||
                    (Properties.Settings.Default.DatabaseUpdatePeriod > 0 && (DateTime.Now >= Properties.Settings.Default.DataVersionDate) &&
                    (DateTime.Now - Properties.Settings.Default.DataVersionDate).TotalHours > Properties.Settings.Default.DatabaseUpdatePeriod)) //todo: time zone
                {
                    // data requires update
                    _ = UpdateRAWData();
                }
                else
                {
                    // update is not needed
                    if (Properties.Settings.Default.DataVersionDate.Year == 1900)
                        DBupdaterMessage = "Database version: default";
                    else if (Properties.Settings.Default.DataVersionDate.Year == 1899)
                        DBupdaterMessage = "Last database update failed";
                    else
                        DBupdaterMessage = "Database version: " + Properties.Settings.Default.DataVersionDate;
                }
            }
            else
            {
                Logger.Current.Info("UPDATES ARE TURNED OFF");
                DBupdaterMessage = "Updates are turned off";
                if (Properties.Settings.Default.DataVersionDate.Year == 1900)
                    DBupdaterMessage += "\nDatabase version: default";
                else if (Properties.Settings.Default.DataVersionDate.Year == 1899)
                    DBupdaterMessage += "\nLast database update failed";
                else
                    DBupdaterMessage += "\nDatabase version: " + Properties.Settings.Default.DataVersionDate;
            }
#endif
        }

        public void Closing(CancelEventArgs arg)
        {
            PortfolioViewModel?.SavePortfolio(arg);
        }

        public void Closed()
        {
            // reset DB date if updating is in proccess
            if (DBupdaterProgress > 0 && DBupdaterProgress < 100)
                Properties.Settings.Default.DataVersionDate = new DateTime(1899, 1, 1);

            // save theme in settings
            ApplicationThemeHelper.SaveApplicationThemeName();

            // save data providers
            BrokersManager.Shutdown();

            // save settings
            Properties.Settings.Default.Save();

            if (GenerateSignalsToTrade.m1 != null) GenerateSignalsToTrade.m1.ReleaseMutex();
            if (GenerateSignalsToTrade.m2 != null) GenerateSignalsToTrade.m2.ReleaseMutex();

            // clean up
            try
            {
                string[] filePaths = Directory.GetFiles("Data/", "*.txt");
                foreach (string f in filePaths)
                    if (f.StartsWith("Data/Python") || f.StartsWith("Data/Random") || f.StartsWith("Data/vsOther") || f.StartsWith("Data/Continuous"))
                        File.Delete(f);
            }
            catch { }

            // ensure continuous thread is closed
            if (Simulator.ContinuousSimThread != null)
            {
                try
                {
                    CppSimulatorInterface.CancelContinuousSimulation();
                    Simulator.ContinuousSimThread.Join(5000);
                    Simulator.ContinuousSimThread.Abort();
                }
                catch (Exception ex)
                {
                    Logger.Current.Warn(ex, "Can't finalize continuous simulation thread!");
                }
                finally
                {
                    Simulator.ContinuousSimThread = null;
                }
            }
        }

        public void ShowCustomIndicatorsEditor()
        {
            if (customIndicatorEditorViewModel == null)
                customIndicatorEditorViewModel = CustomIndicatorEditorViewModel.Create();
            WindowServiceInd.Show(nameof(CustomIndicatorEditor), customIndicatorEditorViewModel);
            WindowServiceInd.Activate();
        }
        public void ShowCustomStrategiesEditor()
        {
            if (CustomStrategiesEditorViewModel == null)
                CustomStrategiesEditorViewModel = CustomStrategiesEditorViewModel.Create();
            WindowServiceStr.Show(nameof(CustomStrategiesEditorView), CustomStrategiesEditorViewModel);
            WindowServiceStr.Activate();
        }
        public void ShowSimTaskWizard()
        {
            WindowServiceSimTask.Show(nameof(SimTaskWizardView), SimTaskWizardViewModel);
            WindowServiceSimTask.Activate();
        }

        /*public void CreateDocument(object arg)
        {
            IDocument doc = DocumentManagerService.FindDocument(arg);
            if (doc == null)
            {
                doc = DocumentManagerService.CreateDocument("DetailedView", arg);
                doc.Id = DocumentManagerService.Documents.Count<IDocument>();
            }
            doc.Show();
        }*/

        private async Task UpdateRAWData()
        {
            DBupdaterProgress = 1;

            DateTime versionDateRemote = new DateTime(1900, 1, 1);
            DateTime versionDateCurrent = Properties.Settings.Default.DataVersionDate;
            if (versionDateCurrent < versionDateRemote) versionDateCurrent = versionDateRemote;
            try
            {   //read remote data version
                HttpResponseMessage httpResponse = await HttpClient.GetAsync(hostPath + "Version.txt" + "?" + DateTime.Now.Ticks);
                if (httpResponse.IsSuccessStatusCode)
                {
                    string version = await httpResponse.Content.ReadAsStringAsync();
                    versionDateRemote = DateTime.ParseExact(version, "M/d/yyyy H:m:s", CultureInfo.InvariantCulture);
                }
                //WebClient client = new WebClient();
                //client.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                //client.Headers.Add("Cache-Control", "no-cache");
                //string s = await client.DownloadStringTaskAsync(new Uri(hostPath + "Version.txt" + "?" + DateTime.Now.Ticks));
                //versionDateRemote = DateTime.ParseExact(s, "M/d/yyyy H:m:s", CultureInfo.InvariantCulture);
            }
            catch (WebException ex)
            {
                Logger.Current.Debug(ex, "Unable to read database version from server");
                DBupdaterProgress = -1;

                if (HttpClientHelper.IsUsingProxy())
                {
                    MessageBoxService.Show("Proxy authentication failed.\nCheck proxy settings", "Data updater");
                    DBupdaterMessage = "Proxy authentication failed. Check proxy settings";
                }
                else
                {
                    MessageBoxService.Show("Unable to connect to server", "Data updater");
                    DBupdaterMessage = "Unable to connect to server";
                }
            }
            catch (Exception ex)
            {
                Logger.Current.Debug(ex, "Unable to read database version from server");

                DBupdaterProgress = -1;
                DBupdaterMessage = "Database update failed";
            }

            Logger.Current.Debug("{0}(): current DB version {1}, remote DB version {2}", nameof(UpdateRAWData), versionDateCurrent, versionDateRemote);

            if (ABCDE() < 0)
            {
                Logger.Current.Debug("{0}(): database update failed (error code 1485)", nameof(UpdateRAWData));
                DBupdaterProgress = -1;
                DBupdaterMessage = "Database update failed";
                return;
            }

            if (versionDateCurrent < versionDateRemote)
            {
                //                webClient = new WebClient();
                //                webClient.DownloadProgressChanged += WebClientDownloadProgressChanged;
                //                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(WebClientDownloadCompleted);
                //                webClient.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                //                webClient.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 6.3; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/45.0.2454.85 Safari/537.36";
                //                webClient.Headers.Add("Cache-Control", "no-cache");
                //panelUpdating.Visible = true;
                //button2.Enabled = buttonContinuousSim.Enabled = false;
                //simulateToolStripMenuItem.Enabled = continuousSimulationToolStripMenuItem.Enabled = false;
                //proxySettingsToolStripMenuItem.Enabled = false;
                //labelDBversion.Visible = true;

                //remoteDBversion = versionDateRemote;

                List<string> FilesToDownload = Utils.SymbolsManager.Symbols.Where(x => x.IsStandard).Select(x => x.Filename).ToList();
                FilesToDownload.Add("Data/VixData.txt");
                FilesToDownload.Add("Data/Margins.csv");
                FilesToDownload.Add("Data/AdditionalData.zip");

                List<string> faildFiles = new List<string>();

                try
                {
                    int processed = 0;
                    foreach (string filename in FilesToDownload)
                    {
                        Logger.Current.Trace("Start downloading ({0})...", filename);

                        HttpResponseMessage httpResponse = await HttpClient.GetAsync(hostPath + Path.GetFileName(filename) + "?" + DateTime.Now.Ticks);
                        if (httpResponse.IsSuccessStatusCode)
                        {
                            byte[] data = await httpResponse.Content.ReadAsByteArrayAsync();

                            using (var fs = File.Create(filename))
                            {
                                await fs.WriteAsync(data, 0, data.Length);
                            }

                            Logger.Current.Trace("Downloading complete ({0})", filename);
                        }
                        else
                            faildFiles.Add(filename);

                        processed++;
                        DBupdaterProgress = (int)Math.Round(processed * 100.0 / FilesToDownload.Count);
                    }

                    if (faildFiles.Count > 0)
                        throw new Exception("Missed files: " + string.Join(", ", faildFiles));

                    Logger.Current.Info("Database has been updated successfully. Version: {0}", versionDateRemote.ToString("MM/dd/yyyy hh:mm:ss", CultureInfo.InvariantCulture));

                    Properties.Settings.Default.DataVersionDate = versionDateRemote;
                    DBupdaterMessage = "Database version: " + versionDateRemote;
                    DBupdaterProgress = 100;    //complete
                }
                catch (Exception ex)
                {
                    Logger.Current.Warn(ex, "Error occurred while downloading data file from server");
                    Logger.Current.Info("Last database update failed!");

                    Properties.Settings.Default.DataVersionDate = new DateTime(1899, 1, 1);
                    DBupdaterProgress = -1;
                    DBupdaterMessage = "Database update failed";
                }
            }
            else
            {
                DBupdaterProgress = 100;
                DBupdaterMessage = "Database version: " + versionDateCurrent;
            }
        }

        public static int ABCDE()   //check license
        {
            if (IsTrial) return 1;
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;
            if (0 == comparer.Compare(serverAnswerString, Security.ActivationCode))
                return 1;
            else return -1;
        }

        public void TradeStationLogIn()
        {
            if (BrokersManager.ActiveBroker == BrokersManager.TradeStation && BrokersManager.ActiveBroker.IsLoggedIn)
                return;

            bool success = BrokersManager.LogInDialog(BrokersManager.TradeStation);

            if(success)
            {
                Workspaces.ForEachDo((workspace) =>
                {
                    if (workspace is QuotesViewModel qvw)
                    {
                        qvw.LoadAfterLogin();
                    }
                    else if (workspace is ChartWindowViewModel cvw)
                    {
                        cvw.LoadAfterLogin();
                    }
                    else if (workspace is TradeViewModel tv)
                    {
                        tv.LoadAfterLogin();
                    }
                });
            }
        }

        public void BrokerLogOut()
        {
            if (BrokersManager.ActiveBroker == BrokersManager.TradeStation && BrokersManager.ActiveBroker.IsLoggedIn)
            {
                if (MessageBoxService.Show("Do you really want to sign out from Trade Station account?", "Trade Station account", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _ = BrokersManager.LogOut();
                }
            }
        }

        // Log in using refresh token
        public async Task BrokerLogIn(bool fromDataSource = false)
        {
            //if (BrokersManager.ActiveBroker == BrokersManager.TradeStation && BrokersManager.ActiveBroker.IsLoggedIn)
            //    return;
            //
            //bool success = await BrokersManager.LogIn(BrokersManager.TradeStation);

            bool success = await BrokersManager.LogIn(null);

            if (success && !fromDataSource)
            {
                Workspaces.ForEachDo((workspace) =>
                {
                    if (workspace is QuotesViewModel qvw)
                    {
                        qvw.LoadAfterLogin();
                    }
                    else if (workspace is ChartWindowViewModel cvw)
                    {
                        cvw.LoadAfterLogin();
                    }
                    else if (workspace is TradeViewModel tv)
                    {
                        tv.LoadAfterLogin();
                    }
                });
            }


            // test --------------
            /*if(success)
            {
                var barsStreamer = await BrokersManager.GetBarsStreamer("AAL", new ChartIntervalItem() { MeasureUnit = DateTimeMeasureUnit.Day, MeasureUnitMultiplier = 1 });
                barsStreamer.Data.CollectionChanged += (s, e) =>
                {
                    Title = $"Collection count: {barsStreamer.Data.Count}";
                };
            }*/
        }

        public void SearchKeyDown(SearchControl sender, KeyEventArgs args)
        {
            if (args.Key == Key.Enter && !string.IsNullOrWhiteSpace(sender.SearchText))
            {
                var workspace = Workspaces.FirstOrDefault(var => (var is ChartWindowViewModel) && var.IsActive) as ChartWindowViewModel;
                if (workspace != null)
                {
                    Regex rx = new Regex(@"^[A-z0-9]{1,}\s[0-9]{1,}\s[A-z]{1,}$");
                    Regex rx2 = new Regex(@"^[A-z0-9]{1,}\s[0-9]{1,}$");

                    if (rx.IsMatch(sender.SearchText))
                    {
                        var param = sender.SearchText.Split(' ');

                        workspace.UpdateSymbol("@" + param[0], new ChartIntervalItem() { MeasureUnitMultiplier = int.Parse(param[1]), MeasureUnit = GetMeasureUnit(param[2]) });
                    }
                    else if (rx2.IsMatch(sender.SearchText))
                    {
                        var param = sender.SearchText.Split(' ');

                        workspace.UpdateSymbol("@" + param[0], new ChartIntervalItem() { MeasureUnitMultiplier = int.Parse(param[1]), MeasureUnit = DateTimeMeasureUnit.Minute });
                    }
                }
            }
        }


        private DateTimeMeasureUnit GetMeasureUnit(string v)
        {
            switch (v.ToLower())
            {
                case "m":
                    return DateTimeMeasureUnit.Month;
                case "d":
                    return DateTimeMeasureUnit.Day;
                case "w":
                    return DateTimeMeasureUnit.Week;
                case "h":
                    return DateTimeMeasureUnit.Hour;
            }

            return DateTimeMeasureUnit.Minute;
        }
    }


    abstract public class PanelWorkspaceViewModel : WorkspaceViewModel, IMVVMDockingProperties
    {
        string _targetName;

        protected PanelWorkspaceViewModel()
        {
            _targetName = WorkspaceName;
        }

        protected virtual string WorkspaceName { get; set; }
        string IMVVMDockingProperties.TargetName
        {
            get { return _targetName; }
            set { _targetName = value; }
        }

        public virtual void OpenItemByPath(string path) { }
    }
    public abstract class WorkspaceViewModel : ViewModel
    {
        protected WorkspaceViewModel()
        {
            IsClosed = true;
        }

        public event EventHandler RequestClose;

        public virtual bool IsActive { get; set; }
        [BindableProperty(OnPropertyChangedMethodName = "OnIsClosedChanged")]
        public virtual bool IsClosed { get; set; }
        public virtual bool IsOpened { get; set; }
        public virtual bool DestroyOnClose { get; set; }

        public void Close()
        {
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
        protected virtual void OnIsClosedChanged()
        {
            IsOpened = !IsClosed;
        }
    }

    public abstract class ViewModel : IDisposable
    {
        public string BindableName { get { return GetBindableName(DisplayName); } }
        public virtual string DisplayName { get; set; }
        public virtual ImageSource Glyph { get; set; }

        public virtual Guid ID { get; set; }
        string GetBindableName(string name)
        {
            return "_" + Regex.Replace(string.IsNullOrWhiteSpace(name) ? "" : name, @"\W", "") + ((ID != Guid.Empty) ? ID.ToString().Replace("-", "_") : "");
        }

        #region IDisposable Members
        public void Dispose()
        {
            OnDispose();
        }
        protected virtual void OnDispose() { }
#if DEBUG
        ~ViewModel()
        {
            string msg = string.Format("{0} ({1}) ({2}) Finalized", GetType().Name, DisplayName, GetHashCode());
            System.Diagnostics.Debug.WriteLine(msg);
        }
#endif
        #endregion 
    }
    
    public static class DumbAggregator
    {
        public static void BroadCast(OrderData od)
        {
            if (OnMessageTransmitted != null)
                OnMessageTransmitted(od);
        }

        public static Action<OrderData> OnMessageTransmitted;

    }

    #region Bars
    public class BarModel : ViewModel
    {
        public BarModel(string displayName)
        {
            DisplayName = displayName;
        }
        public List<CommandViewModel> Commands { get; set; }
        public bool IsMainMenu { get; set; }
    }

    public class CommandViewModel : ViewModel
    {
        public CommandViewModel() { }
        public CommandViewModel(string displayName, List<CommandViewModel> subCommands)
            : this(displayName, null, null, subCommands)
        {
        }
        public CommandViewModel(string displayName, ICommand command = null)
            : this(displayName, null, command, null)
        {
        }
        public CommandViewModel(WorkspaceViewModel owner, ICommand command)
            : this(string.Empty, owner, command)
        {
        }
        private CommandViewModel(string displayName, WorkspaceViewModel owner = null, ICommand command = null, List<CommandViewModel> subCommands = null)
        {
            IsEnabled = true;
            Owner = owner;
            if (Owner != null)
            {
                DisplayName = Owner.DisplayName;
                Glyph = Owner.Glyph;
            }
            else DisplayName = displayName;
            Command = command;
            Commands = subCommands;
        }

        public ICommand Command { get; private set; }
        public List<CommandViewModel> Commands { get; set; }
        public BarItemDisplayMode DisplayMode { get; set; }
        public bool IsComboBox { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsSeparator { get; set; }
        public bool IsSubItem { get; set; }
        public KeyGesture KeyGesture { get; set; }
        public WorkspaceViewModel Owner { get; private set; }
    }
    #endregion
}