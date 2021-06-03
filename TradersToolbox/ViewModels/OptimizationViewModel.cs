using System;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm;
using DevExpress.Mvvm.POCO;
using TradersToolbox.Core;
using System.Threading;
using TradersToolbox.Core.Serializable;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Globalization;
using System.Windows;
using System.Collections.ObjectModel;

namespace TradersToolbox.ViewModels
{
    [POCOViewModel]
    public class OptimizationViewModel
    {
        const string emptyPage = @"
<html lang=""en"" xmlns=""http://www.w3.org/1999/xhtml"">
<head>
<meta http-equiv=""X-UA-Compatible"" content=""IE=Edge"" />
    <meta charset=""utf-8"" />
    <title>Plotly</title>
    <style>
    .center{
      position: fixed;
      top: 50%;
      left: 50%;
      transform: translate(-50%, -50%);
    }
    </style>
    <script>
        document.oncontextmenu = function(e){
            return false;
        }
    </script>
</head>
<body style=""background-color: lightgray"" scroll=""no"">
<div class=""center"">
<span>Sensitivity graph will be displayed here after the simulation completes</span>
</div>
</body>
</html>";


        IReadonlySimStrategy simStrategy;
        StrategySimType simType;
        string currentCurrency;

        Optimizer sensitivityOptimizer;

        protected IMessageBoxService MessageBoxService { get { return this.GetService<IMessageBoxService>(/*ServiceSearchMode.PreferParents*/); } }

        readonly ManualResetEventSlim manualReset = new ManualResetEventSlim();

        public virtual bool IsWaitIndicatorVisible { get; set; }
        public virtual string ProgressText { get; set; }
        public virtual string StageText { get; set; }
        public virtual int ProgressValue { get; set; }

        public virtual bool IsChartClosed { get; set; } = true;

        public virtual string WebPageHTML { get; set; } = emptyPage;

        public virtual List<Signal> EntrySignalsCopy { get; set; } = new List<Signal>();
        public virtual List<Signal> ExitSignalsCopy { get; set; } = new List<Signal>();
        public virtual List<Signal> ParamsSignalCopy { get; set; } = new List<Signal>();

        public virtual double WindowWidth { get; set; } = 900;
        public virtual double WindowHeight { get; set; } = 600;

        protected IDocumentManagerService DocumentManagerService { get { return this.GetService<IDocumentManagerService>(); } }


        public virtual FitnessFunction FitnessFunction { get; set; } = FitnessFunction.PNL;
        /// <summary>
        /// max=0, min=1
        /// </summary>
        public virtual int MaxMinDirection { get; set; }
        public virtual int TestsCount { get; set; } = 100;
        public virtual int MaxIterations { get; set; } = 1000;
        public virtual double Tolerance { get; set; } = 0.01;
        public virtual int SensitivityPointsX { get; set; } = 10;
        public virtual int SensitivityPointsY { get; set; } = 10;
        public virtual int NoiseOpen { get; set; } = 20;
        public virtual int NoiseHigh { get; set; } = 20;
        public virtual int NoiseLow { get; set; } = 20;
        public virtual int NoiseClose { get; set; } = 20;
        public virtual int NoiseMax { get; set; } = 20;
        public virtual int NoiseSamples { get; set; }


        public static OptimizationViewModel Create(IReadonlySimStrategy simStrategy, StrategySimType simType)
        {
            var vm = ViewModelSource.Create(() => new OptimizationViewModel());
            vm.simStrategy = simStrategy;
            vm.simType = simType;

            // read current currency from settings
            var c = Properties.Settings.Default.SimConfigs;
            if (c?.Count > 0 && !string.IsNullOrEmpty(c[0]))
            {
                try
                {
                    var s = Serializer.Deserialize(c[0], typeof(SimulationSettings)) as SimulationSettings;
                    vm.currentCurrency = s.AccountCurrency;
                }
                catch (Exception ex)
                {
                    Logger.Current.Debug(ex, "Can't read configuration from settings!");
                }
            }
            if(string.IsNullOrEmpty( vm.currentCurrency))
                vm.currentCurrency = simStrategy.Currency;

            // make copies
            if (simStrategy.ReadonlyEntrySignals != null)
                foreach (var s in simStrategy.ReadonlyEntrySignals)
                {
                    var sigCopy = s.MakeClone(false);
                    sigCopy.ActiveForEntry = sigCopy.IsRequired = true;
                    foreach (var a in sigCopy.AllUserArgs)
                    {
                        a.Value.IncrementStep = a.Value.BaseValue;
                        a.Value.OptimizeMin = a.Value.Min;
                        a.Value.LastValue = a.Value.Max;
                        a.Value.IsOptimize = false;
                    }
                    vm.EntrySignalsCopy.Add(sigCopy);
                }
            if (simStrategy.ReadonlyExitSignals != null)
                foreach (var s in simStrategy.ReadonlyExitSignals)
                {
                    var sigCopy = s.MakeClone(false);
                    sigCopy.ActiveForExit = true;
                    foreach (var a in sigCopy.AllUserArgs)
                    {
                        a.Value.IncrementStep = a.Value.BaseValue;
                        a.Value.OptimizeMin = a.Value.Min;
                        a.Value.LastValue = a.Value.Max;
                        a.Value.IsOptimize = false;
                    }
                    vm.ExitSignalsCopy.Add(sigCopy);
                }
            // create params signal
            {
                Signal sig = new Signal("Standard Exits", simStrategy.SymbolId)
                {
                    Args = new List<SignalArg>()
                    {
                        // user multiple for exit params only
                        new SignalArg("Max holding time", SignalArg.ArgType.UserMultiple, 0, 9999, simStrategy.MaxTime),
                        new SignalArg("Profitable closes", SignalArg.ArgType.UserMultiple, 0, 9999, simStrategy.ProfX),
                        new SignalArg("Delayed entry", SignalArg.ArgType.UserMultiple, 0, 255, simStrategy.DelayedEntry),
                        new SignalArg("ATR exit length", SignalArg.ArgType.UserMultiple, 1, 255, simStrategy.ATR_len),
                    }
                };
                if (simStrategy.LL_ON > 0) sig.Args.Insert(0, new SignalArg("LL lookback", SignalArg.ArgType.UserMultiple, 0, 1000000, simStrategy.LLlook));
                if (simStrategy.HH_ON > 0) sig.Args.Insert(0, new SignalArg("HH lookback", SignalArg.ArgType.UserMultiple, 0, 1000000, simStrategy.HHlook));
                if (simStrategy.TL_ON > 0) sig.Args.Insert(0, new SignalArg("TL mult", SignalArg.ArgType.UserMultiple, 0, 1000000, (decimal)simStrategy.TL_mult));
                if (simStrategy.SL_ON > 0) sig.Args.Insert(0, new SignalArg("SL mult", SignalArg.ArgType.UserMultiple, 0, 1000000, (decimal)simStrategy.SL_mult));
                if (simStrategy.PT_ON > 0) sig.Args.Insert(0, new SignalArg("PT mult", SignalArg.ArgType.UserMultiple, 0, 1000000, (decimal)simStrategy.PT_mult));

                foreach (var a in sig.AllUserArgs)
                {
                    a.Value.IncrementStep = a.Value.BaseValue;
                    a.Value.OptimizeMin = a.Value.Min;
                    a.Value.LastValue = a.Value.Max;
                    a.Value.IsOptimize = false;
                }
                vm.ParamsSignalCopy.Add(sig);
            }

            return vm;
        }
        protected OptimizationViewModel()
        {
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


        #region Commands
        public async Task Optimization()
        {
            IsWaitIndicatorVisible = true;
            ProgressValue = 0;
            ProgressText = "Optimization...";
            StageText = string.Empty;
            manualReset.Reset();

            IProgress<SimulatorProgressMessage> progress = new Progress<SimulatorProgressMessage>(ProgressUpdate);

            try
            {
                Optimizer optimizer = new Optimizer(simStrategy, EntrySignalsCopy, ExitSignalsCopy, ParamsSignalCopy,
                    FitnessFunction, MaxMinDirection, simType, currentCurrency, NoiseSamples, NoiseOpen, NoiseHigh, NoiseLow, NoiseClose, NoiseMax);

                SimDataOutput output = await optimizer.Optimization(TestsCount, Tolerance, MaxIterations, progress, manualReset);

                if (manualReset.IsSet)
                    return;

                if(string.IsNullOrEmpty(output.Message))
                {
                    var vm = StrategiesViewModel.Create();
                    List<StrategiesListVM> list = new List<StrategiesListVM>()
                        {
                            new StrategiesListVM() { Title="IN", Strategies = new NotifyObservableCollection<SimStrategy>(output.INstrategies) },
                            new StrategiesListVM() { Title="OUT", Strategies = new NotifyObservableCollection<SimStrategy>(output.OUTstrategies) },
                            new StrategiesListVM() { Title="ALL", Strategies = new NotifyObservableCollection<SimStrategy>(output.ALLstrategies) },
                        };
                    vm.Parameter = list;
                    vm.Title = "Optimization results";
                    if (MainWindowViewModel.IsTrial)
                        vm.Title += " - < T_R_I_A_L   V_E_R_S_I_O_N >";
                    CreateDocument(vm);

                    // force to updae view to get best initial parameters from optimization result
                    this.RaisePropertyChanged(x => x.EntrySignalsCopy);
                    this.RaisePropertyChanged(x => x.ExitSignalsCopy);
                    this.RaisePropertyChanged(x => x.ParamsSignalCopy);
                }
            }
            catch (AggregateException ex)
            {
                if (!manualReset.IsSet)
                {
                    MessageBoxService.Show(ex.InnerException.Message, "Optimization error");
                }
                return;
            }
            catch (Exception ex)
            {
                if (!manualReset.IsSet)
                {
                    MessageBoxService.Show(ex.Message, "Optimization error");
                }
                return;
            }
            finally
            {
                IsWaitIndicatorVisible = false;
            }
        }

        public async Task Sensitivity()
        {
            IsWaitIndicatorVisible = true;
            ProgressValue = 0;
            ProgressText = "Sensitivity...";
            StageText = string.Empty;
            manualReset.Reset();

            IProgress<SimulatorProgressMessage> progress = new Progress<SimulatorProgressMessage>(ProgressUpdate);

            try
            {
                sensitivityOptimizer = new Optimizer(simStrategy, EntrySignalsCopy, ExitSignalsCopy, ParamsSignalCopy,
                    FitnessFunction, MaxMinDirection, simType, currentCurrency, NoiseSamples, NoiseOpen, NoiseHigh, NoiseLow, NoiseClose, NoiseMax);

                SensitivityResult res = await sensitivityOptimizer.Sensitivity(SensitivityPointsX, SensitivityPointsY, progress, manualReset);

                if (manualReset.IsSet)
                    return;

                var Xs = res.Xs;
                var Ys = res.Ys;
                var Zs = res.Zs;

                #region Draw 3D surface
                string appPath = AppDomain.CurrentDomain.BaseDirectory;
                string jsPath = "plotly\\dist\\plotly.min.js";
                jsPath = Path.Combine(appPath, jsPath);
                if (!File.Exists(jsPath))
                {
                    Logger.Current.Error("Sensitivity(): Plotly script file not found! ({0})", jsPath);
                    MessageBoxService.Show($"Plotly script file not found!\n\n{jsPath}", "Sensitivity");
                    return;
                }

                string plot_html = DevExpress.Xpf.Core.ThemeManager.ActualApplicationThemeName.Contains("Dark") ?
                    Properties.Resources.PlotlyHomePage_dark : Properties.Resources.PlotlyHomePage;
                plot_html = plot_html.Replace("../bin/Release/plotly/dist/plotly.min.js", jsPath);

                StringBuilder dataString = new StringBuilder();

                dataString.AppendLine("var x1 = [");
                for (int i = 0; i < Xs.Length; ++i)
                    dataString.Append($"{Xs[i].ToString(CultureInfo.InvariantCulture)},");
                dataString.Length -= 1;
                dataString.AppendLine("];");

                dataString.AppendLine("var y1 = [");
                for (int i = 0; i < Ys.Length; ++i)
                    dataString.Append($"{Ys[i].ToString(CultureInfo.InvariantCulture)},");
                dataString.Length -= 1;
                dataString.AppendLine("];");

                dataString.AppendLine("var z1 = [");
                for (int i = 0; i < Ys.Length; ++i)
                {
                    dataString.Append("[");
                    for (int j = 0; j < Xs.Length; ++j)
                        dataString.Append($"{Zs[j, i].ToString(CultureInfo.InvariantCulture)},");
                    dataString.Length -= 1;
                    dataString.AppendLine("],");
                }
                dataString.Length -= 1;
                dataString.AppendLine("];");

                plot_html = plot_html.Substring(0, plot_html.IndexOf("//#data_start")) + dataString.ToString() + plot_html.Substring(plot_html.IndexOf("//#data_stop") + 12);

                // update webBrowser
                WebPageHTML = plot_html;
                IsChartClosed = false;
                #endregion
            }
            catch (AggregateException ex)
            {
                if (!manualReset.IsSet)
                {
                    //Application.Current?.Dispatcher.Invoke(() =>
                    //{
                        MessageBoxService.Show(ex.InnerException.Message, "Sensitivity simulation error");
                    //});
                }
                return;
            }
            catch (Exception ex)
            {
                if (!manualReset.IsSet)
                {
                    //Application.Current?.Dispatcher.Invoke(() =>
                    //{
                        MessageBoxService.Show(ex.Message, "Sensitivity simulation error");
                    //});
                }
                return;
            }
            finally
            {
                IsWaitIndicatorVisible = false;
            }
        }

        public async Task GenStrategy(object p)
        {
            if (p is Point point && WebPageHTML.Contains("selCopyX") && point.X != -2222222222 && point.Y != -2222222222)
            {
                var init = new MathNet.Numerics.LinearAlgebra.Double.DenseVector(new double[] { point.X, point.Y });

                try
                {
                    SimDataOutput output = await sensitivityOptimizer.CalculateAndRead(new List<MathNet.Numerics.LinearAlgebra.Vector<double>>() { init },
                        simStrategy.StopDate, simStrategy.SlippageMode, simStrategy.CommissionMode, currentCurrency, simStrategy.InvestCash, simStrategy.ReadonlyMarketSymbolsIds);

                    if (string.IsNullOrEmpty(output.Message))  // show results window
                    {
                        var vm = StrategiesViewModel.Create();
                        List<StrategiesListVM> list = new List<StrategiesListVM>()
                        {
                            new StrategiesListVM() { Title="IN", Strategies = new NotifyObservableCollection<SimStrategy>(output.INstrategies) },
                            new StrategiesListVM() { Title="OUT", Strategies = new NotifyObservableCollection<SimStrategy>(output.OUTstrategies) },
                            new StrategiesListVM() { Title="ALL", Strategies = new NotifyObservableCollection<SimStrategy>(output.ALLstrategies) },
                        };
                        vm.Parameter = list;
                        vm.Title = "Sensitivity result";
                        if (MainWindowViewModel.IsTrial)
                            vm.Title += " - < T_R_I_A_L   V_E_R_S_I_O_N >";
                        CreateDocument(vm);
                    }
                }
                catch (AggregateException ex)
                {
                    MessageBoxService.Show(ex.InnerException.Message, "Simulation error");
                }
                catch (Exception ex)
                {
                    MessageBoxService.Show(ex.Message, "Simulation error");
                }
            }
        }

        public void ChartVisibility()
        {
            IsChartClosed = !IsChartClosed;
        }

        public void Cancel()
        {
            ProgressText = "Canceling...";
            manualReset.Set();
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
    }
}