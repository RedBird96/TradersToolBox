using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.POCO;
using DevExpress.Xpf.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using TradersToolbox.Core;
using TradersToolbox.Core.Serializable;

namespace TradersToolbox.ViewModels
{
    [POCOViewModel]
    public class AddRuleViewModel
    {
        readonly ManualResetEventSlim manualReset = new ManualResetEventSlim();

        //protected IMessageBoxService MessageBoxService { get { return this.GetService<IMessageBoxService>(/*ServiceSearchMode.PreferParents*/); } }
        protected IDocumentManagerService DocumentManagerService { get { return this.GetService<IDocumentManagerService>(); } }

        public virtual bool IsWaitIndicatorVisible { get; set; }
        public virtual string ProgressText { get; set; }


        public IReadonlySimStrategy simStrategy_internal;

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
                }

                foreach (var signal in Signals_internal)
                    signal.ActiveForEntry = signal.ActiveForExit = false;

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

        public static AddRuleViewModel Create()
        {
            return ViewModelSource.Create(() => new AddRuleViewModel());
        }
        protected AddRuleViewModel()
        {
            Messenger.Default.Register<CustomIndicatorsChangedMessage>(this, OnMessage);
            Messenger.Default.Register<PortfolioChangedMessage>(this, OnMessage);
        }

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
            manualReset.Set();
        }

        public async Task Simulate()
        {
            IsWaitIndicatorVisible = true;
            ProgressText = "Simulation...";
            manualReset.Reset();

            await Task.Run(SimulationCore);

            IsWaitIndicatorVisible = false;
        }
        #endregion

        void SimulationCore()
        {
            try
            {
                // Define input params
                SimSettings.Signals = Signals.Where(s => (s.ActiveForEntry || s.ActiveForExit) && s.Type != Signal.SignalTypes.Undefined).ToList();
                SignalsFactory.RealizeParametricSignals(SimSettings.Signals);

                if (SimSettings.Signals.Count(x => x.Type == Signal.SignalTypes.Ensemble || x.Type == Signal.SignalTypes.Strategy) > 0 &&
                    (SimSettings.EntryOnClose | SimSettings.ExitOnClose) > 0)
                {
                    DXMessageBox.Show("Using Strategy Signals with This Bar Close Entry or Exit is not permitted" + Environment.NewLine +
                        "Please change Entry and Exit to Next Bar Open or de-select Strategy Signals Before Simulating");
                    return;
                }

                // prepare raw data
                var data = RawDataArray.GetRawDataForStrategy(simStrategy_internal, 0, 0, 0, 0, 0, 0,
                    simStrategy_internal.ATR_len, (float)simStrategy_internal.PT_mult, (float)simStrategy_internal.SL_mult,
                    simStrategy_internal.TL_mult, simStrategy_internal.HHlook, simStrategy_internal.LLlook, Utils.SymbolsManager);

                if (data == null) return;

                // generate and simulate
                SimDataOutput output = Simulator.GenAndSim(data[0], SimSettings, 0);       //todo: separate thread??

                if (!string.IsNullOrEmpty(output.Message))
                {
                    DXMessageBox.Show(output.Message);
                    return;
                }

                ProgressText = "Preparing results...";

                // Read results
                output = Simulator.ReadStratFromMemory();
                if (!string.IsNullOrEmpty(output.Message))
                {
                    DXMessageBox.Show(output.Message);
                    return;
                }

                var inResults = new NotifyObservableCollection<SimStrategy>(output.INstrategies);
                var outResults = new NotifyObservableCollection<SimStrategy>(output.OUTstrategies);
                var allResults = new NotifyObservableCollection<SimStrategy>(output.ALLstrategies);

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
                    vm.Title = "Add Rule results";
                    vm.Title += " - " + string.Join(", ", SimSettings.BaseSymbolsIds.Select(x => x.ToString())) + $" ({SimSettings.TradeMode})";
                    if (MainWindowViewModel.IsTrial)
                        vm.Title += " - < T_R_I_A_L   V_E_R_S_I_O_N >";
                    CreateDocument(vm);
                });
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message, "Simulation error");
            }
        }
    }
}