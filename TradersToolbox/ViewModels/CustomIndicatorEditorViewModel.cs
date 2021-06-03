using System;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm;
using DevExpress.Mvvm.POCO;
using System.Collections.ObjectModel;
using TradersToolbox.Core;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Windows.Data;
using TradersToolbox.Core.Serializable;
using System.Diagnostics;
using System.Threading.Tasks;

namespace TradersToolbox.ViewModels
{
    [POCOViewModel]
    public class CustomIndicatorEditorViewModel
    {
        public Dictionary<string, SignalsDescription> signalsListBool { get; } = SignalsData.SignalNames;   // base signals for boolean
        public Dictionary<string, string> signalsListComp { get; } = SignalsData.CustomSignalsNamesEL;      // values for comparative and cross signals
        public Dictionary<decimal, string> boolVal { get; } = new Dictionary<decimal, string>() { { 0, "False" }, { 1, "True" } };
        public Dictionary<int, string> crossOp { get; } = new Dictionary<int, string>() { { 0, "NONE" }, { 1, "AND" }, { 2, "OR" } };
        public string[] opComp { get; } = { "=", "!=", ">", "<", ">=", "<=" };
        public string[] opCros { get; } = { "crosses above", "crosses below" };
        public virtual NotifyObservableCollection<CustomIndicator> CustomIndicators { get; set; }
        public virtual CustomIndicator SelectedIndicator { get; set; }

        public void OnSelectedIndicatorChanged()
        {
            if(SelectedIndicator == null)
                SelectedBoolTrigger = SelectedCompCross = null;
            else if (SelectedIndicator.type == CustomIndicatorType.Boolean || SelectedIndicator.type == CustomIndicatorType.Trigger)
                SelectedBoolTrigger = SelectedIndicator;
            else if (SelectedIndicator.type == CustomIndicatorType.Comparative || SelectedIndicator.type == CustomIndicatorType.CrossOver)
                SelectedCompCross = SelectedIndicator;

            // update preview
            if (SelectedIndicator != null)
                CustomIndicators_OnItemPropertyChanged(SelectedIndicator, null);
        }

        public virtual CustomIndicator SelectedBoolTrigger { get; set; }
        public virtual CustomIndicator SelectedCompCross { get; set; }

        public virtual string PreviewString { get; set; }
        public virtual string Error { get; set; }
        public virtual bool PyTestProgress { get; set; }

        protected IOpenFileDialogService OpenFileDialogService { get { return this.GetService<IOpenFileDialogService>(); } }
        protected ISaveFileDialogService SaveFileDialogService { get { return this.GetService<ISaveFileDialogService>(); } }
        protected IMessageBoxService MessageBoxService { get { return this.GetService<IMessageBoxService>(); } }
        protected ICurrentWindowService CurrentWindowService { get { return this.GetService<ICurrentWindowService>(); } }

        /*/ todo: messenger
        #region Event
        public delegate void CustomIndicatorsChanged(object sender, EventArgs e);

        private readonly List<CustomIndicatorsChanged> CustomIndicatorsChangedDelegatedList = new List<CustomIndicatorsChanged>();

        public event CustomIndicatorsChanged OnCustomIndicatorsChanged
        {
            add { CustomIndicatorsChangedDelegatedList.Add(value); }
            remove { CustomIndicatorsChangedDelegatedList.Remove(value); }
        }

        public void EmitCustomIndicatorsChangedEvents()
        {
            foreach (CustomIndicatorsChanged d in CustomIndicatorsChangedDelegatedList)
                d(null, null);
        }
        #endregion//*/

        public static CustomIndicatorEditorViewModel Create()
        {
            return ViewModelSource.Create(() => new CustomIndicatorEditorViewModel());
        }
        protected CustomIndicatorEditorViewModel()
        {
            CustomIndicators = new NotifyObservableCollection<CustomIndicator>();

            // read current custom signals from config file
            List<CustomIndicator> list = null;
            try
            {
                if (File.Exists(Utils.IndicatorsFileName))
                    list = CustomIndicator.ReadFromFile(Utils.IndicatorsFileName);

                if (list != null)
                    foreach (var i in list)
                        CustomIndicators.Add(i);

                if (CustomIndicators.Count > 0)
                    SelectedIndicator = CustomIndicators.First();
            }
            catch(Exception ex)
            {
                Logger.Current.Debug(ex, "Can't read custom indicators file!");
            }

            // connet handler
            CustomIndicators.OnItemPropertyChanged += CustomIndicators_OnItemPropertyChanged;
        }

        private void CustomIndicators_OnItemPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // update preview
            if(SelectedIndicator == sender)
            {
                switch (SelectedIndicator.type)
                {
                    case CustomIndicatorType.Boolean:
                        {
                            string r1Base = SelectedIndicator.rule1Base;
                            string r1B = SelectedIndicator.rule1B;
                            string r2Base = SelectedIndicator.rule2Base;
                            string r2B = SelectedIndicator.rule2B;

                            r1Base = !string.IsNullOrEmpty(r1Base) && signalsListBool.ContainsKey(r1Base) ? signalsListBool[r1Base].codeEL : "";
                            r1B = !string.IsNullOrEmpty(r1B) && signalsListBool.ContainsKey(r1B) ? signalsListBool[r1B].codeEL : "";
                            r2Base = !string.IsNullOrEmpty(r2Base) && signalsListBool.ContainsKey(r2Base) ? signalsListBool[r2Base].codeEL : "";
                            r2B = !string.IsNullOrEmpty(r2B) && signalsListBool.ContainsKey(r2B) ? signalsListBool[r2B].codeEL : "";

                            if (SelectedIndicator.crossOp > 0)
                                PreviewString = string.Format("({0}) {1} {2}  {3}  ({4}) {5} {6}",
                                    r1Base,
                                    "=",//SelectedIndicator.rule1Op,
                                    string.IsNullOrEmpty(r1B) ? SelectedIndicator.rule1Val.ToString() : r1B,
                                    crossOp[SelectedIndicator.crossOp],
                                    r2Base,
                                    "=",//SelectedIndicator.rule2Op,
                                    string.IsNullOrEmpty(r2B) ? SelectedIndicator.rule2Val.ToString() : r2B);
                            else
                                PreviewString = string.Format("({0}) {1} {2}",
                                    r1Base,
                                    "=",//SelectedIndicator.rule1Op,
                                    string.IsNullOrEmpty(r1B) ? SelectedIndicator.rule1Val.ToString() : r1B);
                            break;
                        }

                    case CustomIndicatorType.Comparative:
                    case CustomIndicatorType.CrossOver:
                        {
                            string r1Base = SelectedIndicator.rule1Base;
                            string r1B = SelectedIndicator.rule1B;
                            string r2Base = SelectedIndicator.rule2Base;
                            string r2B = SelectedIndicator.rule2B;

                            r1Base = !string.IsNullOrEmpty(r1Base) && signalsListComp.ContainsKey(r1Base) ? signalsListComp[r1Base] : "";
                            r1B = !string.IsNullOrEmpty(r1B) && signalsListComp.ContainsKey(r1B) ? signalsListComp[r1B] : "";
                            r2Base = !string.IsNullOrEmpty(r2Base) && signalsListComp.ContainsKey(r2Base) ? signalsListComp[r2Base] : "";
                            r2B = !string.IsNullOrEmpty(r2B) && signalsListComp.ContainsKey(r2B) ? signalsListComp[r2B] : "";

                            string defaultOp = SelectedIndicator.type == CustomIndicatorType.Comparative ? "=" : "crosses above";

                            if (SelectedIndicator.crossOp > 0)
                                PreviewString = string.Format("{0}[{1}] {2} {3}  {4}  {5}[{6}] {7} {8}",
                                    r1Base,
                                    SelectedIndicator.rule1Offset,
                                    string.IsNullOrEmpty(SelectedIndicator.rule1Op) ? defaultOp : SelectedIndicator.rule1Op,
                                    string.IsNullOrEmpty(r1B) ? SelectedIndicator.rule1Val.ToString() : (r1B + "[" + SelectedIndicator.rule1Boffset + "]"),
                                    SelectedIndicator.crossOp == 1 ? "AND" : "OR",
                                    r2Base,
                                    SelectedIndicator.rule2Offset,
                                    string.IsNullOrEmpty(SelectedIndicator.rule2Op) ? defaultOp : SelectedIndicator.rule2Op,
                                    string.IsNullOrEmpty(r2B) ? SelectedIndicator.rule2Val.ToString() : (r2B + "[" + SelectedIndicator.rule2Boffset + "]"));
                            else
                                PreviewString = string.Format("{0}[{1}] {2} {3}",
                                    r1Base,
                                    SelectedIndicator.rule1Offset,
                                    string.IsNullOrEmpty(SelectedIndicator.rule1Op) ? defaultOp : SelectedIndicator.rule1Op,
                                    string.IsNullOrEmpty(r1B) ? SelectedIndicator.rule1Val.ToString() : (r1B + "[" + SelectedIndicator.rule1Boffset + "]"));
                            break;
                        }

                    case CustomIndicatorType.Trigger:
                        {
                            string r1Base = SelectedIndicator.rule1Base;
                            string r1B = SelectedIndicator.rule1B;

                            r1Base = !string.IsNullOrEmpty(r1Base) && signalsListBool.ContainsKey(r1Base) ? signalsListBool[r1Base].codeEL : "";
                            r1B = !string.IsNullOrEmpty(r1B) && signalsListBool.ContainsKey(r1B) ? signalsListBool[r1B].codeEL : "";

                            PreviewString = string.Format("Trigger({0}, {1}) >= 1 and ({2})",
                                r1Base,
                                SelectedIndicator.rule1Offset,
                                r1B);
                            break;
                        }

                    case CustomIndicatorType.Python:
                        PreviewString = null;
                        break;
                }

                Error = CheckCorrectness(SelectedIndicator);
            }
        }

        private string CheckCorrectness(CustomIndicator ind)
        {
            // check correctness
            string sname = ind.shortName;
            if (sname.Contains(' '))
            {
                return "Indicator's name contains spaces";
            }
            if (sname.Length < 1 || SignalsData.SignalNames.Keys.Contains(sname) || CustomIndicators.Any(x => x.shortName == sname && x != ind))
            {
                return "Non unique name";
            }

            else switch (ind.type)
                {
                    case CustomIndicatorType.Boolean:
                        {   // bool type
                            if (string.IsNullOrEmpty(ind.rule1Base) || !signalsListBool.ContainsKey(ind.rule1Base) ||
                                (!string.IsNullOrEmpty(ind.rule1B) && !signalsListBool.ContainsKey(ind.rule1B)) ||
                                (ind.crossOp > 0 && (string.IsNullOrEmpty(ind.rule2Base) || !signalsListBool.ContainsKey(ind.rule2Base) ||
                                (!string.IsNullOrEmpty(ind.rule2B) && !signalsListBool.ContainsKey(ind.rule2B)))))
                            {
                                return "Wrong base symbol";
                            }
                            break;
                        }
                    case CustomIndicatorType.Trigger:
                        {   // trigger type
                            if (string.IsNullOrEmpty(ind.rule1Base) || !signalsListBool.ContainsKey(ind.rule1Base))
                                return "Wrong trigger symbol";
                            else if (string.IsNullOrEmpty(ind.rule1B) || !signalsListBool.ContainsKey(ind.rule1B))
                                return "Wrong base symbol";
                            break;
                        }
                    case CustomIndicatorType.Comparative:
                    case CustomIndicatorType.CrossOver:
                        {   // compare and cross types
                            if (string.IsNullOrEmpty(ind.rule1Base) || !signalsListComp.ContainsKey(ind.rule1Base) ||
                                (!string.IsNullOrEmpty(ind.rule1B) && !signalsListComp.ContainsKey(ind.rule1B)) ||
                                (ind.crossOp > 0 && (string.IsNullOrEmpty(ind.rule2Base) || !signalsListComp.ContainsKey(ind.rule2Base) ||
                                (!string.IsNullOrEmpty(ind.rule2B) && !signalsListComp.ContainsKey(ind.rule2B)))))
                            {
                                return "Wrong base symbol";
                            }
                            break;
                        }
                    case CustomIndicatorType.Python:
                        {   // python
                            if (string.IsNullOrEmpty(ind.pythonFileName) || !File.Exists(ind.pythonFileName))
                            {
                                return "Python script file not found";
                            }
                            break;
                        }
                }
            return null;    // no error
        }

        #region Commands
        public void OK()
        {
             // check correctness
            if (CustomIndicators.FirstOrDefault(x => CheckCorrectness(x) != null) is CustomIndicator ind)
            {
                MessageBoxService.Show("There is an error with one or more custom indicators.\n" +
                    $"Indicator name: {ind.shortName}\n"+
                    $"Error: {CheckCorrectness(ind)}\n\n"+
                    "Please fix the error first or Cancel", "Save warning...");
                return;
            }

            //save to config file
            CustomIndicator.WriteToFile(CustomIndicators, Utils.IndicatorsFileName);

            // inform custom indicators changed
            //EmitCustomIndicatorsChangedEvents();
            Messenger.Default.Send(new CustomIndicatorsChangedMessage());

            CurrentWindowService.Close();
        }

        public void Cancel()
        {
            CurrentWindowService.Close();
        }

        public void Add()
        {
            CustomIndicator ind = new CustomIndicator();
            int i = 1;
            while (CustomIndicators.Any(x => x.shortName == $"NewIndicator_{i}"))
                i++;
            ind.shortName = $"NewIndicator_{i}";
            ind.type = CustomIndicatorType.Boolean;
            CustomIndicators.Add(ind);
            SelectedIndicator = ind;
        }

        public void Delete ()
        {
            if (SelectedIndicator != null)
                CustomIndicators.Remove(SelectedIndicator);
        }

        public void Clear()
        {
            CustomIndicators.Clear();

            /*/ test code
            Random rnd = new Random(DateTime.Now.Millisecond);
              for(int i=0; i<500; i++)
              {
                  CustomIndicator ind = new CustomIndicator();
                  ind.type = 2 + rnd.Next(2);     //compare or cross
                  ind.shortName = "TEST_IND_" + i;
                  ind.active = true;
                  ind.rule1Base = Form1.CustomSignalsNamesEL.ElementAt(rnd.Next(Form1.CustomSignalsNamesEL.Count)).Key;
                  ind.rule1Offset = rnd.Next(10);
                  ind.rule1Op = opComp[rnd.Next(opComp.Length)];
                  ind.rule1B = Form1.CustomSignalsNamesEL.ElementAt(rnd.Next(Form1.CustomSignalsNamesEL.Count)).Key;
                  ind.rule1Boffset = rnd.Next(10);

                  ind.crossOp = 1;

                  ind.rule2Base = Form1.CustomSignalsNamesEL.ElementAt(rnd.Next(Form1.CustomSignalsNamesEL.Count)).Key;
                  ind.rule2Offset = rnd.Next(10);
                  ind.rule2Op = opComp[rnd.Next(opComp.Length)];
                  ind.rule2B = Form1.CustomSignalsNamesEL.ElementAt(rnd.Next(Form1.CustomSignalsNamesEL.Count)).Key;
                  ind.rule2Boffset = rnd.Next(10);

                  indicatorsList.Add(ind);
              }
              for (int i = 0; i < 500; i++)
              {
                  CustomIndicator ind = new CustomIndicator();
                  ind.type = 1;   //boolean
                  ind.shortName = "TEST_IND_BOOL_" + i;
                  ind.active = true;
                  ind.rule1Base = Form1.SignalNames.ElementAt(rnd.Next(Form1.SignalNames.Count)).Key;
                  ind.rule1Offset = 0;
                  ind.rule1Op = opComp[rnd.Next(opComp.Length)];
                  ind.rule1B = Form1.SignalNames.ElementAt(rnd.Next(Form1.SignalNames.Count)).Key;
                  ind.rule1Boffset = 0;

                  ind.crossOp = 1;

                  ind.rule2Base = Form1.SignalNames.ElementAt(rnd.Next(Form1.SignalNames.Count)).Key;
                  ind.rule2Offset = 0;
                  ind.rule2Op = opComp[rnd.Next(opComp.Length)];
                  ind.rule2B = Form1.SignalNames.ElementAt(rnd.Next(Form1.SignalNames.Count)).Key;
                  ind.rule2Boffset = 0;

                  indicatorsList.Add(ind);
              }
              //end test code */
        }

        public void RadioButtonRule1Value()
        {
            if (SelectedIndicator != null)
                SelectedIndicator.rule1B = null;
        }

        public void RadioButtonRule2Value()
        {
            if (SelectedIndicator != null)
                SelectedIndicator.rule2B = null;
        }

        public void PyOpen()
        {
            if (OpenFileDialogService.ShowDialog())
            {
                SelectedIndicator.pythonFileName = OpenFileDialogService.File.GetFullName();
            }
        }
        public void PyCreate()
        {
            if (SaveFileDialogService.ShowDialog())
            {
                // write python indicator template
                using (StreamWriter writer = new StreamWriter(SaveFileDialogService.OpenFile()))
                {
                    writer.Write(Properties.Resources.PythonTemplate);
                }

                SelectedIndicator.pythonFileName = SaveFileDialogService.File.GetFullName();

                PyEdit();
            }
        }
        public void PyEdit()
        {
            if (!MainWindowViewModel.PYTHON_READY || !File.Exists(SelectedIndicator.pythonFileName))
            {
                MessageBoxService.Show("Given file not exists!\n\n" + SelectedIndicator.pythonFileName);
                return;
            }

            try
            {
                Process.Start(Path.Combine(MainWindowViewModel.pythonPath, "pythonw.exe"),
                    string.Format("\"{0}\" \"{1}\"", Path.Combine(MainWindowViewModel.pythonPath, @"Lib\idlelib\idle.pyw"), SelectedIndicator.pythonFileName));
            }
            catch (Exception ex)
            {
                MessageBoxService.Show("Can't run python IDLE!\n\n" + ex.Message);
            }
        }
        public async Task PyTest()
        {
            if (!MainWindowViewModel.PYTHON_READY || !File.Exists(SelectedIndicator.pythonFileName))
            {
                MessageBoxService.Show("Given file does not exists!\n\n" + SelectedIndicator.pythonFileName);
                return;
            }
            PyTestProgress = true;

            SymbolId symbolId = new SymbolId("ES", "");
            SignalPython sp = new SignalPython("PythonTestIndicator", SelectedIndicator.pythonFileName, symbolId, null) { MarketNumber = 1, ActiveForEntry = true };

            SimulationSettings sim = new SimulationSettings(new List<Signal>() { sp },
                new DateTime(2010, 01, 01), new DateTime(2010, 07, 01), true, false,
                null, null, symbolId, null, null, null, 0, 0, TradeMode.Long, 0, 0, 0, 2, 2, 2, 0, 0, 5, 5, 20,
                PositionSizingMode.Default, SlippageMode.PerTrade, CommissionMode.PerTrade,
                0, 0, "USD", 10000, new List<SymbolId>() { symbolId, symbolId }, SymbolId.Empty,
                5, 9999, FitnessFunction.PNL, 1, 0.3f, 1, 1, 1, 1, 0, DateTime.Now, 0, RebalanceMethod.ProfitFactor, 0, 0,
                0, 0, 0, 0, false, 0, 0, 0, 0, 0, 0, 0);

            // Generate Signals
            CalcSigResult ok;
            try
            {
                ok = await SignalsFactory.RunGeneratorAsync(sim, symbolId, "Data/ESRaw.txt", "Data/ESRaw.txt", "Data/ESRaw.txt", 50, false, null, null);

                if (string.IsNullOrEmpty(ok.Message))
                    PreviewString = "Script is correct.";
                else
                    PreviewString = "Errors were found! See errors log for more info.";
            }
            catch /*(Exception ex)*/
            {
                //MessageBox.Show("Can't complete signals generation!\n" + ex.Message);
                PreviewString = "Errors were found! See errors log for more info.";
            }
            finally
            {
                PyTestProgress = false;
            }
        }
        #endregion
    }

    internal class CustIndTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if(value is CustomIndicatorType type && type != CustomIndicatorType.Undefined)
            {
                return (int)type - 1;
            }
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (CustomIndicatorType)((int)value+1);
        }
    }
}