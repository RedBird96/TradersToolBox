using System;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm;
using DevExpress.Mvvm.POCO;
using System.Linq;
using TradersToolbox.Views;
using System.ComponentModel;
using TradersToolbox.DataSources;
using System.Collections.ObjectModel;
using DevExpress.Xpf.Bars;
using DevExpress.Xpf.Grid;
using System.Windows.Input;
using TradersToolbox.CustomControls;
using System.Collections.Generic;
using System.Xaml;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Reflection;
using System.IO;
using System.Collections.Concurrent;
using DevExpress.XtraPrinting.Native;
using DevExpress.Xpf.Core;
using TradersToolbox.DataObjects;

namespace TradersToolbox.ViewModels
{
    [POCOViewModel]
    public class QuotesViewModel : PanelWorkspaceViewModel
    {
        protected override string WorkspaceName { get { return "LeftHost"; }  set { } }

        public virtual ObservableCollection<WatchlistViewModel> Watchlists { get; set; }
        public virtual int SelectedWatchlist { get; set; }

        protected IWindowService WindowServiceScanner { get { return this.GetService<IWindowService>(); } }

        public void InitAfterLoad()
        {
            if (Watchlists.Count > 0)
                Watchlists.RemoveAt(0);
            foreach (var wl in Watchlists)
            {
                wl.DataSource.Data.Clear();
                wl.Parent = this;
                _ = wl.DataSource.RequestLiveDataAsync();
            }
        }

        private T DeserializePOCOViewModelFromFile<T>(string fileName)
        {
            try
            {
                var xaml = File.ReadAllText(fileName);

                var m = Regex.Matches(xaml, @"<([\w]*:)?(?<dxTypeName>\w+_[\w]{8}_[\w]{4}_[\w]{4}_[\w]{4}_[\w]{12})");

                var uniqueList = new HashSet<string>();
                foreach (Match item in m)
                {
                    uniqueList.Add(item.Groups["dxTypeName"].Value);
                }

                foreach (var item in uniqueList)
                {
                    var baseTypeName = item.Substring(0, item.Length - 37);
                    var t = Assembly.GetExecutingAssembly().GetTypes().FirstOrDefault(f => f.Name == baseTypeName);

                    if (t != null)
                    {
                        xaml = xaml.Replace(item, ViewModelSource.GetPOCOType(t).Name);
                    }
                }

                var assemblyName = ViewModelSource.GetPOCOType(typeof(T)).Assembly.GetName().Name;
                xaml = Regex.Replace(xaml, @"(DevExpress\.Mvvm\.v\d+\.*\d*\.DynamicTypes\.[\w]{8}-[\w]{4}-[\w]{4}-[\w]{4}-[\w]{12})", assemblyName);

                return (T)XamlServices.Parse(xaml);
            }
            catch (Exception ex)
            {
                return default;
            }
        }

        public QuotesViewModel()
        {
            DisplayName = "Market overview";

            Watchlists = new ObservableCollection<WatchlistViewModel>();
            var model = WatchlistViewModel.Create();
            model.Title = "Default";
            model.Parent = this;
            model.DataSource.Error += (message) =>
            {
                Messenger.Default.Send(new QuotesViewErrorMessage(message));
            };
            model.DataSource.AddDefaultSymbols();
            _ = model.DataSource.RequestLiveDataAsync();
            Watchlists.Add(model);

            // message handlers
            Messenger.Default.Register<AddWatchlistMessage>(this, OnMessage);
        }

        #region Message handlers
        void OnMessage(AddWatchlistMessage message)
        {
            var vm = message.Watchlist;
            vm.Parent = this;
            Watchlists.Add(vm);

            //focus on new item
            SelectedWatchlist = Watchlists.Count - 1;
        }
        #endregion


        protected override void OnIsClosedChanged()
        {
            base.OnIsClosedChanged();
            if (IsClosed && Watchlists != null)
            { 
                foreach(var watch in Watchlists)
                {
                    watch.DataSource?.Close();
                }
                Watchlists.Clear();
            }
                
        }

        public void TabRemoved(object sender, TabControlTabHiddenEventArgs args)
        {
            var item = (args.Item as WatchlistViewModel);
            item.DataSource.Close();
            Watchlists.Remove(item);
        }

        internal void LoadAfterLogin()
        {
            foreach (var watch in Watchlists)
            {
                watch.DataSource?.Close();
                _ = watch.DataSource?.RequestLiveDataAsync();
            }
        }

        #region Commands
        public void LoadWatchlist()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog() { Filter = "Watchlist (*.wl)|*.wl|All files (*.*)|*.*" };
            var openResult = openFileDialog.ShowDialog();
            if (openResult.HasValue && openResult.Value)
            {
                var dict = DeserializePOCOViewModelFromFile<ConcurrentDictionary<string, Pair<string, int>>>(openFileDialog.FileName);

                if (dict != null)
                {
                    var vm = WatchlistViewModel.Create();
                    vm.Parent = this;
                    vm.Title = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                    vm.DataSource.Error += (message) =>
                    {
                        Messenger.Default.Send(new QuotesViewErrorMessage(message));
                    };
                    vm.DataSource.requestSymbols = dict;
                    _ = vm.DataSource.RequestLiveDataAsync();
                    Watchlists.Add(vm);
                }
            }
        }

        public void CreateWatchlist()
        {
            var model = WatchlistViewModel.Create();
            model.Title = "New";
            model.Parent = this;
            model.DataSource.Error += (message) =>
            {
                Messenger.Default.Send(new QuotesViewErrorMessage(message));
            };
            model.DataSource.ClearSymbols();
            Watchlists.Add(model);
        }

        public void SaveWatchlist()
        {
            if (SelectedWatchlist < 0)
                return;
            var wtch  = Watchlists[SelectedWatchlist];

             SaveFileDialog saveFileDialog = new SaveFileDialog() { Filter = "Watchlist (*.wl)|*.wl|All files (*.*)|*.*" };
            saveFileDialog.DefaultExt = "wl";
            var saveResult = saveFileDialog.ShowDialog();
            if (saveResult.HasValue && saveResult.Value)
            {
                XamlServices.Save(saveFileDialog.FileName, wtch.DataSource.requestSymbols);

                wtch.Title = Path.GetFileNameWithoutExtension(saveFileDialog.FileName);
            }
        }

        public void RemoveItem()
        {
            if (SelectedWatchlist < 0)
                return;
            var wtch = Watchlists[SelectedWatchlist];

            if (wtch.SelectedRow is QuoteDefinitionModel q)
            {
                wtch.DataSource?.Close();
                wtch.DataSource.Data.Clear();
                wtch.DataSource?.RemoveSymbol(q.Symbol);
                _ = wtch.DataSource?.RequestLiveDataAsync();
            }
        }

        public void ClearWatchlist()
        {
            if (SelectedWatchlist < 0)
                return;
            var wtch = Watchlists[SelectedWatchlist];

            wtch.DataSource?.Close();
            wtch.DataSource.Data.Clear();
            wtch.DataSource?.ClearSymbols();
            _ = wtch.DataSource?.RequestLiveDataAsync();
        }

        public void Scan()
        {
            var vm = ScannerViewModel.Create();
            WindowServiceScanner.Show(nameof(ScannerView), vm);
        }
        #endregion
    }

    [POCOViewModel]
    public class WatchlistViewModel
    {
        public virtual string Title { get; set; }

        public virtual bool SymbolSearchEnabled { get; set; } = true;
        public virtual bool ScanColumnVisible { get; set; }
        public virtual bool UseExtendedHours { get; set; }

        //[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public QuotesDataSource DataSource { get; set; }

        public static WatchlistViewModel Create()
        {
            return ViewModelSource.Create(() => new WatchlistViewModel());
        }
        protected WatchlistViewModel()
        {
            DataSource = new QuotesDataSource();
            UseExtendedHours = Properties.Settings.Default.UseExtendedHours;
            Messenger.Default.Register<ExtendedHoursChangedMessage>(this, OnMessage);
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        //public ObservableCollection<QuoteDefinitionModel> Quotes => DataSource.Data;
        public ICollectionView Quotes => DataSource.DataCollection;


        public QuotesViewModel Parent;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual ObservableCollection<TickerData> SearchResults { get; set; }

        public async void SearchSymbol(BarEditItem sender)
        {
            BarEdit = sender;
            if (sender.EditValue == null || string.IsNullOrWhiteSpace(sender.EditValue.ToString()))
            {
                //clear and hide
                SearchResults?.Clear();
                SearchResults = null;
                (sender.EditSettings as PopupBaseEditSettingsEx).Popup.IsPopupOpen = false;
            }
            else
            {
                if (BarEdit.EditValue.ToString().Contains(Environment.NewLine))
                {
                    if (SearchResults != null && SearchResults.Any(var => (var.Ticker.ToLower()+Environment.NewLine) == BarEdit.EditValue.ToString().ToLower()))
                    {
                        var row = SearchResults.First(var => (var.Ticker.ToLower() + Environment.NewLine) == BarEdit.EditValue.ToString().ToLower());

                        DataSource.AppendSymbol(row.Ticker, "" /*row.FutureType*/);
                        DataSource?.Close();
                        _ = DataSource?.RequestLiveDataAsync();
                        BarEdit.EditValue = null;
                    }
                    else
                    {
                        BarEdit.EditValue = BarEdit.EditValue.ToString().Replace(Environment.NewLine, string.Empty);
                    }
                }
                else
                {
                    //update and show popup
                    try
                    {
                        var result = await MainWindowViewModel.BrokersManager.SuggestSymbols(sender.EditValue.ToString(), 20);
                        SearchResults = new ObservableCollection<TickerData>(result);
                        (sender.EditSettings as PopupBaseEditSettingsEx).Popup.IsPopupOpen = true;
                    }
                    catch (Exception ex)
                    {
                        SearchResults?.Clear();
                        SearchResults = null;
                        (sender.EditSettings as PopupBaseEditSettingsEx).Popup.IsPopupOpen = false;
                    }
                }
            }

        }


        #region Message handlers
        void OnMessage(ExtendedHoursChangedMessage message)
        {
            UseExtendedHours = Properties.Settings.Default.UseExtendedHours;
        }
        #endregion


        public void SelectSymbol(object sender, MouseButtonEventArgs args)
        {
            int rowHandle = TView.GetRowHandleByMouseEventArgs(args);
            if (rowHandle == GridControl.InvalidRowHandle) return;
            if (!(sender as GridControl).IsGroupRowHandle(rowHandle))
            {
                var row = (sender as GridControl).GetRow(rowHandle) as TickerData;

                DataSource.AppendSymbol(row.Ticker, "" /*row.FutureType*/);
                DataSource?.Close();
                _ = DataSource?.RequestLiveDataAsync();
                BarEdit.EditValue = null;
            }

        }

        private BarEditItem BarEdit;
        private TableView TView;

        public void TableLoaded(object sender)
        {
            TView = sender as TableView;
        }

        public object SelectedRow { get; set; }

        public void SendMessage(string isNewWindow)
        {
            if (SelectedRow is QuoteDefinitionModel q)
            {
                Messenger.Default.Send(new QuoteSelectedMessage(q.Symbol));
            }
        }

        
    }
}