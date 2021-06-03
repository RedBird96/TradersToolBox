using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using TradersToolbox.Core;
using TradersToolbox.Core.Serializable;
using TradersToolbox.ViewModels;

namespace TradersToolbox.Views
{
    public partial class CheckedComboBox : UserControl
    {
        internal class Item : INotifyPropertyChanged
        {
            public SymbolId SymbolId { get; set; }

            private bool _isChecked;
            private string _description;
            private string _type;

            public bool IsChecked
            {
                get { return _isChecked; }
                set
                {
                    if (_isChecked != value)
                    {
                        _isChecked = value;
                        OnPropertyChanged(nameof(IsChecked));
                    }
                }
            }

            public string Description {
                get => _description;
                set
                {
                    if (_description != value)
                    {
                        _description = value;
                        OnPropertyChanged(nameof(Description));
                    }
                }
            }

            public string Type { 
                get => _type;
                set
                {
                    if (_type != value)
                    {
                        _type = value;
                        OnPropertyChanged(nameof(Type));
                    }
                }
            }

            public override string ToString()
            {
                return SymbolId.ToString();
            }

            public event PropertyChangedEventHandler PropertyChanged;
            public void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        Item lastChecked;
        readonly ObservableCollection<Item> localSource = new ObservableCollection<Item>();

        public bool IsMultiChecked { get; set; }

        public List<SymbolId> SelectedSymbolIds
        {
            get { return (List<SymbolId>)GetValue(SelectedSymbolIdsProperty); }
            set { SetValue(SelectedSymbolIdsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedSymbolIds.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedSymbolIdsProperty =
            DependencyProperty.Register("SelectedSymbolIds", typeof(List<SymbolId>), typeof(CheckedComboBox), new PropertyMetadata((s, e) =>
            {
                if (e.NewValue != e.OldValue)
                {
                    (s as CheckedComboBox).SetItemsCheckedInternal(e.NewValue as List<SymbolId>);
                }
            }));

        public void SetItemsCheckedInternal(List<SymbolId> toCheck)
        {
            // clear all
            foreach (Item i in localSource)
                i.IsChecked = false;
            lastChecked = null;

            if (toCheck?.Count>0)
            {
                List<SymbolId> newList = new List<SymbolId>();
                foreach (var id in toCheck)
                {
                    if (localSource.FirstOrDefault(x => x.SymbolId == id) is Item item)
                    {
                        item.IsChecked = true;
                        lastChecked = item;
                        newList.Add(id);
                        if (!IsMultiChecked)
                            break;
                    }
                }
                if (newList.Count != toCheck.Count)
                    SelectedSymbolIds = newList;
            }
            UpdateText();
        }

        public SymbolId SelectedSymbolId
        {
            get { return (SymbolId)GetValue(SelectedSymbolIdProperty); }
            set { SetValue(SelectedSymbolIdProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedSymbolId.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedSymbolIdProperty =
            DependencyProperty.Register("SelectedSymbolId", typeof(SymbolId), typeof(CheckedComboBox), new PropertyMetadata((s, e) =>
            {
                if (s is CheckedComboBox ccb && ccb.IsMultiChecked == false && e.NewValue != e.OldValue)
                {
                    ccb.SetItemCheckedInternal((SymbolId)e.NewValue);
                }
            }));

        public void SetItemCheckedInternal(SymbolId toCheck)
        {
            // clear all
            foreach (Item i in localSource)
                i.IsChecked = false;
            lastChecked = null;

            if (!toCheck.IsEmpty())
            {
                if (localSource.FirstOrDefault(x => x.SymbolId == toCheck) is Item item)
                {
                    item.IsChecked = true;
                    lastChecked = item;
                }
                else
                    SelectedSymbolId = SymbolId.Empty;
            }
            UpdateText();
        }


        public CheckedComboBox()
        {
            InitializeComponent();

            var col = CollectionViewSource.GetDefaultView(localSource);
            col.GroupDescriptions.Add(new PropertyGroupDescription("Type"));

            comboBox.ItemsSource = localSource;

            var arg = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
            Update(Utils.SymbolsManager.Symbols, arg);

            Utils.SymbolsManager.Symbols.CollectionChanged += Symbols_CollectionChanged;
            Utils.SymbolsManager.Symbols.OnItemPropertyChanged += Symbols_OnItemPropertyChanged;
        }

        ~CheckedComboBox()
        {
            Utils.SymbolsManager.Symbols.CollectionChanged -= Symbols_CollectionChanged;
            Utils.SymbolsManager.Symbols.OnItemPropertyChanged -= Symbols_OnItemPropertyChanged;
        }

        /*public CheckedComboBox(string[] list, bool hardwareAccelerated = true)
        {
            InitializeComponent();

            Logger.Current.Debug("{0}: Process ID: {1}", nameof(CheckedComboBox), Process.GetCurrentProcess().Id);

            if (!hardwareAccelerated) RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;

            labelList.Text = "Select...";

            string group = "Futures";
            foreach(string s in list)
            {
                if (string.IsNullOrEmpty(s))
                {
                    group = " ";
                }
                else if (s[0] == ' ')
                {
                    group = s.Trim();
                    if (string.IsNullOrEmpty(s))
                        group = " ";
                }
                else {
                    Item i = new Item
                    {
                        SymbolId = new SymbolId(s.Split('-')[0].Trim(), ""),
                        Description = s.Split('-')[1].Trim(),
                        Type = group
                    };
                    localSource.Add(i);
                }
            }

            ListCollectionView lcv = new ListCollectionView(localSource);
            lcv.GroupDescriptions.Add(new PropertyGroupDescription("Type"));

            comboBox.ItemsSource = lcv;
        }

        public CheckedComboBox(IList<Symbol> list, bool multiChecked, bool hardwareAccelerated = true)
        {
            InitializeComponent();

            Logger.Current.Debug("{0}: Process ID: {1}", nameof(CheckedComboBox), Process.GetCurrentProcess().Id);

            if(!hardwareAccelerated) RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;

            IsMultiChecked = multiChecked;
            labelList.Text = "Select...";

            foreach (var s in list)
            {
                Item i = new Item
                {
                    SymbolId = s.Id,
                    Description = s.Description,
                    Type = s.TypeString
                };
                localSource.Add(i);
            }

            ListCollectionView lcv = new ListCollectionView(localSource);
            lcv.GroupDescriptions.Add(new PropertyGroupDescription("Type"));

            comboBox.ItemsSource = lcv;
        }*/

        private void Update(IList<Symbol> symbols, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        foreach (Symbol s in e.NewItems)
                        {
                            Item i = new Item
                            {
                                SymbolId = s.Id,
                                Description = s.Description,
                                Type = s.Type.ToString()
                            };
                            localSource.Add(i);
                        }
                        break;
                    }
                case NotifyCollectionChangedAction.Remove:
                    {
                        if (e.OldItems is IList<Symbol> list)
                        {
                            List<Item> toDelete = localSource.Where(x => list.Any(y => x.SymbolId == y.Id)).ToList();

                            foreach (var i in toDelete)
                            {
                                if (lastChecked == i)
                                    lastChecked = null;
                                localSource.Remove(i);
                            }
                            if (toDelete.Any(x => x.IsChecked))
                            {
                                if (IsMultiChecked)
                                    SelectedSymbolIds = GetCheckedItems();
                                else
                                    SelectedSymbolId = SymbolId.Empty;
                            }
                        }
                        break;
                    }
                case NotifyCollectionChangedAction.Reset:
                    {
                        // delete
                        List<Item> toDelete = new List<Item>();
                        foreach (var s in localSource)
                        {
                            if (symbols.FirstOrDefault(x => x.Id == s.SymbolId) is Symbol sym)
                            {
                                // update description and type
                                s.Description = sym.Description;
                                s.Type = sym.Type.ToString();
                                continue;
                            }
                            else toDelete.Add(s);
                        }
                        foreach (var s in toDelete)
                        {
                            if (lastChecked == s)
                                lastChecked = null;
                            localSource.Remove(s);
                        }
                        if (toDelete.Any(x => x.IsChecked))
                        {
                            if (IsMultiChecked)
                                SelectedSymbolIds = GetCheckedItems();
                            else
                                SelectedSymbolId = SymbolId.Empty;
                        }

                        // add new
                        for (int index = 0; index < symbols.Count; index++)
                        {
                            var s = symbols[index];
                            if (localSource.Any(x => x.SymbolId == s.Id))
                                continue;
                            else
                            {
                                Item i = new Item
                                {
                                    SymbolId = s.Id,
                                    Description = s.Description,
                                    Type = s.Type.ToString()
                                };
                                //localSource.Add(i);
                                localSource.Insert(index, i);   // preserve position
                            }
                        }
                        break;
                    }
                case NotifyCollectionChangedAction.Replace:
                    throw new NotImplementedException($"{nameof(CheckedComboBox)}.{nameof(ObservableCollection<Item>)} - Replace action is not realized!");
                case NotifyCollectionChangedAction.Move:
                    throw new NotImplementedException($"{nameof(CheckedComboBox)}.{nameof(ObservableCollection<Item>)} - Move action is not realized!");
            }

            UpdateText();
        }


        private void UpdateText()
        {
            labelList.Text = "Select...";
            IEnumerable<Item> ch = localSource.Where(x => x.IsChecked);
            if (ch.Count() > 0)
            {
                StringBuilder sb = new StringBuilder();
                if (ch.Count() > 1) sb.AppendFormat("({0}) ", ch.Count());
                foreach (Item i in ch)
                    sb.AppendFormat("{0}, ", i.SymbolId);
                sb.Remove(sb.Length - 2, 2);
                labelList.Text = sb.ToString();
            }
        }

        private void CheckBox_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            if (IsMultiChecked)
                SelectedSymbolIds = GetCheckedItems();
            else
            {
                var item = (e.Source as CheckBox).DataContext as Item;
                SelectedSymbolId = item.IsChecked ? item.SymbolId : SymbolId.Empty;
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comboBox.SelectedIndex = -1;
        }

        public List<SymbolId> GetCheckedItems()
        {
            return localSource?.Where(x => x.IsChecked).Select(x => x.SymbolId).ToList();
        }

        /*public int GetCheckedItemIndex()
        {
            for (int i = 0; i < localSource.Count; ++i)
                if (localSource[i].IsChecked)
                    return i;
            return -1;
        }

        public void SetItemChecked(int index)
        {
            if (index >= 0 && (index + 1) < localSource.Count())
            {
                localSource[index].IsChecked = true;
                CheckBox_Unchecked(null, null);
            }
        }*/

        /*public void SetItemChecked(SymbolId symbolId)
        {
            Item i = localSource.FirstOrDefault(x => x.SymbolId == symbolId);
            if (i != null)
            {
                i.IsChecked = true;
                if (IsMultiChecked == false)
                {
                    if (lastChecked != null && lastChecked != i) lastChecked.IsChecked = false;
                    lastChecked = i;
                }
                CheckBox_Unchecked(null, null);
            }
        }*/

        /*public void ClearItemChecked()
        {
            foreach (Item i in localSource)
                i.IsChecked = false;
            CheckBox_Unchecked(null, null);
        }*/

        private void Symbols_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Update(Utils.SymbolsManager.Symbols, e);
        }

        private void Symbols_OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Symbol.Name) || e.PropertyName == nameof(Symbol.Timeframe) ||
                e.PropertyName == nameof(Symbol.Description) || e.PropertyName == nameof(Symbol.Type))
            {
                var arg = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                Update(Utils.SymbolsManager.Symbols, arg);

                // refresh grouping
                if (e.PropertyName == nameof(Symbol.Type))
                {
                    var col = CollectionViewSource.GetDefaultView(localSource);
                    col.Refresh();
                }
            }
        }

        private void ButtonClearSelection_Click(object sender, RoutedEventArgs e)
        {
            if (IsMultiChecked)
                SelectedSymbolIds = null;
            else
                SelectedSymbolId = SymbolId.Empty;
        }
    }
}
