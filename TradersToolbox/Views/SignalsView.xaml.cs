using DevExpress.Xpf.Editors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TradersToolbox.Core;
using TradersToolbox.Core.Serializable;

namespace TradersToolbox.Views
{
    /// <summary>
    /// Логика взаимодействия для SignalsView.xaml
    /// </summary>
    public partial class SignalsView : UserControl
    {
        bool expand = true;
        bool optimizeMode;
        readonly Timer updateSignalsSelectionStatTimer;



        public ObservableCollection<Signal> ItemsSourceTest
        {
            get { return (ObservableCollection<Signal>)GetValue(ItemsSourceTestProperty); }
            set { SetValue(ItemsSourceTestProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ItemsSourceTest.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemsSourceTestProperty =
            DependencyProperty.Register("ItemsSourceTest", typeof(ObservableCollection<Signal>), typeof(SignalsView), new PropertyMetadata(null));


        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(SignalsView), new PropertyMetadata((s,e)=>
            {
                if (e.NewValue is NotifyObservableCollection<Signal> sigCollection && s is SignalsView sv)
                {
                    var v = CollectionViewSource.GetDefaultView(sigCollection);
                    if (v.Filter == null)
                        v.Filter = sv.SigFilter;

                    sigCollection.CollectionChanged += sv.SigCollection_CollectionChanged;
                    sigCollection.OnItemPropertyChanged += sv.SigCollection_OnItemPropertyChanged;

                    // update selection statistic (async execution required to put command to the end of GUI thread's message queue)
                    sv.Dispatcher.InvokeAsync(()=> { sv.UpdateSelectionStatistics(); });
                }
            }));

        public bool M2Enabled
        {
            get { return (bool)GetValue(M2EnabledProperty); }
            set { SetValue(M2EnabledProperty, value); }
        }

        public static readonly DependencyProperty M2EnabledProperty =
            DependencyProperty.Register("M2Enabled", typeof(bool), typeof(SignalsView), new PropertyMetadata((s, e) =>
            {
                (s as SignalsView).UpdateFilteredView();
            }));

        public bool M3Enabled
        {
            get { return (bool)GetValue(M3EnabledProperty); }
            set { SetValue(M3EnabledProperty, value); }
        }

        public static readonly DependencyProperty M3EnabledProperty =
            DependencyProperty.Register("M3Enabled", typeof(bool), typeof(SignalsView), new PropertyMetadata((s,e)=>
            {
                (s as SignalsView).UpdateFilteredView();
            }));

        public ICommand SelectPositiveCommand
        {
            get { return (ICommand)GetValue(SelectPositiveCommandProperty); }
            set { SetValue(SelectPositiveCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectPositiveCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectPositiveCommandProperty =
            DependencyProperty.Register("SelectPositiveCommand", typeof(ICommand), typeof(SignalsView), new PropertyMetadata(null));

        public bool ShowSelectionControls
        {
            get { return (bool)GetValue(ShowSelectionControlsProperty); }
            set { SetValue(ShowSelectionControlsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShowSelectionControls.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowSelectionControlsProperty =
            DependencyProperty.Register("ShowSelectionControls", typeof(bool), typeof(SignalsView), new PropertyMetadata(true));


        public SignalsView()
        {
            InitializeComponent();

            //test
            ItemsSourceTest = new ObservableCollection<Signal>()
            {
                new Signal("sig1", SymbolId.Empty, groupId: new SignalGroup("testGroup", 1)) { ActiveForEntry=true, CanEdit=false },
                new Signal("sig2", SymbolId.Empty, groupId: new SignalGroup("testGroup", 1)) {IsRequired=true },
                new Signal("sig3", SymbolId.Empty, groupId: new SignalGroup("testGroup", 1)) {ActiveForExit=true },
                new Signal("sig1-2", SymbolId.Empty, groupId: new SignalGroup("testGroup2", 2, true)),
                new Signal("sig2-2", SymbolId.Empty, groupId: new SignalGroup("testGroup2", 2)),
                new Signal("sig3-2", SymbolId.Empty, groupId: new SignalGroup("testGroup2", 2))
            };
            //---------------------


            SignalsData.OnFavoriteSignalsChanged += SignalsData_onFavoriteSignalsChanged;

            updateSignalsSelectionStatTimer = new Timer(100) { AutoReset = false };
            updateSignalsSelectionStatTimer.Elapsed += UpdateSignalsSelectionStatTimer_Elapsed;
        }

        public SignalsView(bool hardwareAccelerated = true)
        {
            InitializeComponent();

            if (!hardwareAccelerated) RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
        }

        ~SignalsView()
        {
            SignalsData.OnFavoriteSignalsChanged -= SignalsData_onFavoriteSignalsChanged;
        }

        public void SetMode(bool contextMenu)
        {
            if(listView.View is GridView gv)
            {
                // change first column to display visual text with m2/3
                var tv = new MemoryStream(Encoding.UTF8.GetBytes(@"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                                                                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
                <TextBlock Text=""{Binding " + nameof(Signal.TextVisual) + @"}""/>
                </DataTemplate>"));
                var template1 = (DataTemplate)System.Windows.Markup.XamlReader.Load(tv);

                gv.Columns[0].CellTemplate = template1;

                gv.Columns.RemoveAt(1);
                gv.Columns.RemoveAt(1);
                gv.Columns.RemoveAt(1);
                //gv.Columns.Move(1, 0);
                //gv.Columns[0].Header = "X";
                //gv.Columns[0].Width = 30;
                gv.Columns[0].Width = 250;
            }
            SelectionStat.Visibility = Visibility.Collapsed;
            gbEntrySelection.Visibility = Visibility.Collapsed;
            gbExitSelection.Visibility = Visibility.Collapsed;
            tbExpCol.Visibility = Visibility.Collapsed;
            //spUp.HorizontalAlignment = HorizontalAlignment.Left;

            if(!contextMenu)
                listView.ContextMenu = null;
            cbSigType.IsEnabled = false;
            tbSigFilter.IsEnabled = false;
            tbExpCol.IsEnabled = false;
            listView.SetValue(Grid.RowProperty, 0);
            listView.SetValue(Grid.RowSpanProperty, 5);
            mainGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            mainGrid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);

            optimizeMode = true;

            colMin.FieldName = $"Value.{nameof(SignalArg.OptimizeMin)}";
            //(colMin.Binding as Binding).Path = new PropertyPath($"Value.{nameof(SignalArg.OptimizeMin)}");      //todo: check re-binding
            //(colStep.Binding as Binding).Path = new PropertyPath($"Value.{nameof(SignalArg.BaseValue)}");
            colStep.Header = "Init";
            colCheck.Visible = true;

            listView.SelectionMode = SelectionMode.Single;
            if (listView.ItemsSource != null)
                foreach (Signal sig in listView.ItemsSource)
                {
                    listView.SelectedValue = sig;
                    break;
                }
        }

        public void SigCollection_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if(updateSignalsSelectionStatTimer.Enabled == false)
                updateSignalsSelectionStatTimer.Start();
        }
        public void SigCollection_OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (updateSignalsSelectionStatTimer.Enabled == false)
                updateSignalsSelectionStatTimer.Start();
        }

        private void UpdateSignalsSelectionStatTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.InvokeAsync(() => UpdateSelectionStatistics());
        }

        private void MenuItem_AddToFavorites_Click(object sender, RoutedEventArgs e)
        {
            if (listView.ItemsSource != null)
            {
                List<string> toAdd = new List<string>();
                foreach (Signal sig in listView.SelectedItems)
                        toAdd.Add(sig.Key);
                SignalsData.AddToFavorites(toAdd);
            }
        }

        private void MenuItem_RemoveFromFavorites_Click(object sender, RoutedEventArgs e)
        {
            if (listView.ItemsSource != null)
            {
                List<string> toRemove = new List<string>();
                foreach (Signal sig in listView.SelectedItems)
                    toRemove.Add(sig.Key);
                SignalsData.RemoveFromFavorites(toRemove);
            }
        }

        /// <summary>
        /// Copy signals group
        /// </summary>
        private void MenuItem_CopyClick(object sender, RoutedEventArgs e)
        {
            if (listView.ItemsSource != null)
            {
                ObservableCollection<Signal> slist = listView.ItemsSource as ObservableCollection<Signal>;

                List<Signal> newSigs = new List<Signal>();

                if (listView.SelectedItem is Signal ss && ss.Type == Signal.SignalTypes.Parametric)
                {
                    int ind = slist.IndexOf(ss);
                    foreach (var sig in slist.Where(x => x.Type == Signal.SignalTypes.Parametric && x.GroupId == ss.GroupId))
                    {
                        var ns = sig.MakeClone();
                        //ns.isCopy = true;
                        newSigs.Add(ns);
                    }

                    SignalGroup ng = new SignalGroup(ss.GroupId.HeaderText + (ss.GroupId.HeaderText.Contains(" *") ? " " : " *"), ss.GroupId.VisualOrder, true);
                    int max_attempts = 100;
                    while (slist.Any(x => x.GroupId == ng) && max_attempts > 0)
                    {
                        ng = new SignalGroup(ng.HeaderText + " ", ng.VisualOrder);
                        max_attempts--;
                    }

                    foreach (var sig in newSigs)
                    {
                        sig.GroupId = ng;
                        slist.Insert(ind, sig);
                        ind++;

                        if (sig.ActiveForEntry) ng.CountEntry++;
                        if (sig.ActiveForExit) ng.CountExit++;
                    }
                }

                /*foreach (Signal sig in listView.SelectedItems)
                {
                    if (sig.Type == Signal.SignalTypes.Parametric)
                    {
                        int ind = slist.IndexOf(sig);
                        var ns = sig.MakeClone(null);
                        ns.isCopy = true;
                        slist.Insert(ind, ns);
                    }
                }*/
            }
        }

        /// <summary>
        /// Delete signals group
        /// </summary>
        private void MenuItem_DeleteClick(object sender, RoutedEventArgs e)
        {
            if (listView.ItemsSource != null)
            {
                ObservableCollection<Signal> slist = listView.ItemsSource as ObservableCollection<Signal>;

                List<Signal> toDelete = new List<Signal>();

                foreach (Signal ss in listView.SelectedItems)
                {
                    if (ss != null && ss.Type == Signal.SignalTypes.Parametric/* && ss.isCopy*/)
                    {
                        foreach (var sig in slist.Where(x => x.Type == Signal.SignalTypes.Parametric && x.GroupId == ss.GroupId))
                            toDelete.Add(sig);
                    }
                }

                foreach (Signal sig in toDelete)
                    slist.Remove(sig);
            }
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var s = (listView.SelectedItem as Signal);
            if (s != null && (s.Type == Signal.SignalTypes.Parametric || optimizeMode))
            {
                var sb = (Storyboard)FindResource("ArgsDataContextChangedAnimation");
                sb.Begin();

                dataGridArgs.IsEnabled = true;
                dataGridArgs.ItemsSource = s.AllUserArgs;   //user args only
            }
            else
            {
                dataGridArgs.ItemsSource = null;
                dataGridArgs.IsEnabled = false;
            }
        }

        public void UpdateSelectionStatistics()
        {
            int BaseEn = 0;
            int ParametricEn = 0;
            int CustomEn = 0;
            int StrategyEn = 0;
            int RequiredEn = 0;
            int BaseEx = 0;
            int ParametricEx = 0;
            int CustomEx = 0;
            int StrategyEx = 0;
            int TotalEn = 0;
            int TotalEx = 0;

            if (listView.ItemsSource != null)
            {
                IList<Signal> slist = listView.ItemsSource as IList<Signal>;

                foreach (Signal sig in slist)
                {
                    if (sig.ActiveForEntry)
                    {
                        TotalEn++;
                        switch (sig.Type)
                        {
                            case Signal.SignalTypes.BaseBoolean: BaseEn++; break;
                            case Signal.SignalTypes.Parametric: ParametricEn++; break;
                            case Signal.SignalTypes.CustomIndicator: CustomEn++; break;
                            case Signal.SignalTypes.Ensemble:
                            case Signal.SignalTypes.Strategy: StrategyEn++; break;
                        }
                        if (sig.IsRequired) RequiredEn++;
                    }
                    if (sig.ActiveForExit)
                    {
                        TotalEx++;
                        switch (sig.Type)
                        {
                            case Signal.SignalTypes.BaseBoolean: BaseEx++; break;
                            case Signal.SignalTypes.Parametric: ParametricEx++; break;
                            case Signal.SignalTypes.CustomIndicator: CustomEx++; break;
                            case Signal.SignalTypes.Ensemble:
                            case Signal.SignalTypes.Strategy: StrategyEx++; break;
                        }
                    }
                }
            }

            tbBase.Text = string.Format("{0} entry, {1} exit", BaseEn, BaseEx);
            tbParam.Text = string.Format("{0} entry, {1} exit", ParametricEn, ParametricEx);
            tbCustom.Text = string.Format("{0} entry, {1} exit", CustomEn, CustomEx);
            tbStrat.Text = string.Format("{0} entry, {1} exit", StrategyEn, StrategyEx);
            tbRequired.Text = string.Format("{0}", RequiredEn);
            tbTotal.Text = string.Format("{0} entry, {1} exit", TotalEn, TotalEx);
        }

        /*private void Polygon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            foreach (var s in ((sender as Polygon).DataContext as CollectionViewGroup).Items)
            {
                //                (s as Signal).act
            }
            e.Handled = true;
        }*/

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var cb = (sender as CheckBox);
            if (cb != null && cb.Tag != null && (bool)cb.Tag && listView.ItemsSource != null)
            {
                bool b = cb.IsChecked ?? false;
                foreach (Signal sig in listView.SelectedItems)
                    sig.ActiveForEntry = b;
            }
        }

        private void CheckBox_Checked_Out(object sender, RoutedEventArgs e)
        {
            var cb = (sender as CheckBox);
            if (cb != null && cb.Tag != null && (bool)cb.Tag && listView.ItemsSource != null)
            {
                bool b = cb.IsChecked ?? false;
                foreach (Signal sig in listView.SelectedItems)
                    sig.ActiveForExit = b;
            }
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(listView.ItemsSource);
            if (view != null && view.Groups != null)
            {
                foreach (var g in view.Groups)
                    if((g as CollectionViewGroup).Name is SignalGroup sg)
                        sg.IsExpanded = expand;
                expand = !expand;
            }
        }

        private void DataGridArgs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //foreach (var a in e.AddedItems)
            //{
            //    ((KeyValuePair<string, SignalArg>)a).Value.TextDynamic = "W";
            //    ((KeyValuePair<string, SignalArg>)a).Value.isSelected = true;
            //    //(a as SignalArg).TextDynamic = "W";
            //    //(a as SignalArg).isSelected = true;
            //}
            //foreach (var a in e.RemovedItems)
            //    //(a as SignalArg).isSelected = false;
            //    ((KeyValuePair<string, SignalArg>)a).Value.isSelected = false;
        }

        private void ButtonSelectSignals_Click(object sender, RoutedEventArgs e)
        {
            if (listView.ItemsSource is ObservableCollection<Signal> signalsList && sender is Button b)
            {
                string tagStr = b.Tag.ToString();
                bool isEntry = b.Name.Contains("Entry");

                // get all nodes in current filter
                CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(signalsList);
                var currentVisibleSignals = signalsList.Where(x => view.PassesFilter(x)).ToList();

                if (int.TryParse(tagStr, out int countToSelect))
                {
                    if (countToSelect < 0 || countToSelect >= currentVisibleSignals.Count)     // select all
                    {
                        if (isEntry)
                            foreach (var s in currentVisibleSignals)
                                s.ActiveForEntry = true;
                        else
                            foreach (var s in currentVisibleSignals)
                                s.ActiveForExit = true;
                    }
                    else
                    {
                        // clear selection
                        if (isEntry)
                            foreach (var s in currentVisibleSignals)
                                s.ActiveForEntry = false;
                        else
                            foreach (var s in currentVisibleSignals)
                                s.ActiveForExit = false;

                        if (countToSelect > 0)      // select random
                        {
                            Random rnd = new Random(DateTime.Now.Millisecond);
                            int checkCount = 0;

                            if (isEntry)  // entry
                            {
                                checkCount = 1; // Cnot0 always active

                                // random selection
                                while (checkCount < countToSelect && checkCount < currentVisibleSignals.Count)
                                {
                                    int i = rnd.Next(currentVisibleSignals.Count);
                                    if (currentVisibleSignals[i].ActiveForEntry)
                                        continue;
                                    currentVisibleSignals[i].ActiveForEntry = true;
                                    checkCount++;
                                }
                            }
                            else
                            {
                                // random selection
                                while (checkCount < countToSelect && checkCount < currentVisibleSignals.Count)
                                {
                                    int i = rnd.Next(currentVisibleSignals.Count);
                                    if (currentVisibleSignals[i].ActiveForExit)
                                        continue;
                                    currentVisibleSignals[i].ActiveForExit = true;
                                    checkCount++;
                                }
                            }
                        }
                    }
                }
            }
        }

        public void UpdateFilteredView()
        {
            if (ItemsSource != null)
            {
                // Update signals view to apply filter
                try
                {
                    ICollectionView view = CollectionViewSource.GetDefaultView(ItemsSource);
                    view.Refresh();
                }
                catch { }
            }
        }

        private void ListView_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            string list = "1234567890ABCDEFGHIJKLMNOPQRSTUVWXYZ*()[]{};:'\",.<>/?-_=+\\|";
            if (e.Text.Length == 1 && list.IndexOf(e.Text.ToUpper()) >= 0)
            {
                tbSigFilter.Focus();
                //tbSigFilter.Text += e.Text;
            }
        }

        public bool SigFilter(object O)
        {
            if (O is Signal sig && (cbSigType.SelectedItem is ComboBoxEditItem selectedItem))
            {
                return SigFilter(sig, M2Enabled, M3Enabled, selectedItem.Content.ToString(), tbSigFilter.Text);
            }
            return false;
        }

        public static bool SigFilter(Signal s, bool m2, bool m3, string text1, string text2)
        {
            if (s.GroupId == null)
                return false;
            else if ((s.MarketNumber == 2 && m2 == false) || (s.MarketNumber == 3 && m3 == false))
                return false;
            else
            {
                bool isVisible = false;
                switch (text1)
                {
                    case "All": isVisible = true; break;
                    case "Selected": isVisible = s.ActiveForEntry || s.ActiveForExit; break;
                    case "Base Symbol": isVisible = (s.MarketNumber == 1); break;
                    case "Market 2 Symbol": isVisible = (s.MarketNumber == 2); break;
                    case "Market 3 Symbol": isVisible = (s.MarketNumber == 3); break;
                    case "Parametric": isVisible = (s.Type == Signal.SignalTypes.Parametric); break;
                    case "Legacy": isVisible = (s.Type == Signal.SignalTypes.BaseBoolean); break;
                    case "Favorites": isVisible = s.IsFavorite; break;
                    case "Custom": isVisible = (s.Type == Signal.SignalTypes.CustomIndicator); break;
                    case "Portfolio Strategies": isVisible = (s.Type == Signal.SignalTypes.Strategy || s.Type == Signal.SignalTypes.Ensemble); break;
                }
                if (isVisible && string.IsNullOrEmpty(text2) == false)
                {
                    isVisible = (s.Text.IndexOf(text2, StringComparison.InvariantCultureIgnoreCase) >= 0) ||
                        (s.GroupId.HeaderText.IndexOf(text2, StringComparison.InvariantCultureIgnoreCase) >= 0);
                }
                return isVisible;
            }
        }

        private void cbSigType_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            UpdateFilteredView();
        }

        private void tbSigFilter_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            if (e.OldValue?.ToString() == "selectpositive" && string.IsNullOrEmpty(e.NewValue?.ToString()) && Keyboard.Modifiers == (ModifierKeys.Alt | ModifierKeys.Control))
            {
                Console.Beep();
                ButtonEntrySelectPositive.Visibility = Visibility.Visible;
            }
            UpdateFilteredView();
        }

        private void SignalsData_onFavoriteSignalsChanged(object sender, EventArgs e)
        {
            UpdateFilteredView();
        }

        private void GridControl_SelectionChanged(object sender, DevExpress.Xpf.Grid.GridSelectionChangedEventArgs e)
        {

        }

        private void GridControl_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {

        }

        private void BarButtonItem_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {

        }

        private void BarButtonItem_ItemClick_1(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {

        }

        private void BarButtonItem_ItemClick_2(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {

        }

        private void BarButtonItem_ItemClick_3(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {

        }
        /*
        private void PART_Editor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // keep selection
            sigGrid.SelectionMode = DevExpress.Xpf.Grid.MultiSelectMode.MultipleRow;
        }

        private void PART_Editor_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // restore selection mode
            sigGrid.SelectionMode = DevExpress.Xpf.Grid.MultiSelectMode.Row;

            // select
            if (sender is CheckEdit cb && cb.Tag is string modeStr)
            {
                bool b = cb.IsChecked ?? false; // current state
                b = !b;                         // new state

                switch(modeStr)
                {
                    case "entry":
                        foreach (Signal sig in sigGrid.SelectedItems)
                            sig.ActiveForEntry = b;
                        break;
                    case "required":
                        cb.EditValue = b;       // todo: check
                        break;
                    case "exit":
                        foreach (Signal sig in sigGrid.SelectedItems)
                            sig.ActiveForExit = b;
                        break;
                    default:
                        throw new Exception("Undefined mode!");
                }                
            }
        }

        private void PART_Editor_MouseLeave(object sender, MouseEventArgs e)
        {
            sigGrid.SelectionMode = DevExpress.Xpf.Grid.MultiSelectMode.Row;
        }*/
    }

    public static class GridViewConstraints
    {
        public static readonly DependencyProperty MinColumnWidthProperty =
            DependencyProperty.RegisterAttached("MinColumnWidth", typeof(double), typeof(GridViewConstraints), new PropertyMetadata(75d, (s, e) =>
            {
                if (s is ListView listView)
                {
                    listView.Loaded += (lvs, lve) =>
                    {
                        if (listView.View is GridView view)
                        {
                            foreach (var column in view.Columns)
                            {
                                SetMinWidth(listView, column);

                                ((System.ComponentModel.INotifyPropertyChanged)column).PropertyChanged += (cs, ce) =>
                                {
                                    if (ce.PropertyName == nameof(GridViewColumn.ActualWidth))
                                        SetMinWidth(listView, column);
                                };
                            }
                        }
                    };
                }
            }));

        private static void SetMinWidth(ListView listView, GridViewColumn column)
        {
            double minWidth = (double)listView.GetValue(MinColumnWidthProperty);

            if (column.Width < minWidth)
                column.Width = minWidth;
        }

        public static double GetMinColumnWidth(DependencyObject obj) => (double)obj.GetValue(MinColumnWidthProperty);

        public static void SetMinColumnWidth(DependencyObject obj, double value) => obj.SetValue(MinColumnWidthProperty, value);
    }

    public class MultiValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool result = true;
            foreach (var o in values)
                result &= (bool)o;
            return result;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ArithmeticConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double d = (double)value;
            double p = double.Parse(parameter.ToString());
            return Math.Max(0, d + p);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    //[ValueConversion(typeof(bool?), typeof(bool))]
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //if (targetType != typeof(bool?))
            //{
            //    throw new InvalidOperationException("The target must be a nullable boolean");
            //}
            bool? b = (bool?)value;
            return b.HasValue && !b.Value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return !(value as bool?);
        }
    }
}
