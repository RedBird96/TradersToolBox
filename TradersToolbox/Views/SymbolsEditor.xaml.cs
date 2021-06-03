using DevExpress.Xpf.Core;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using TradersToolbox.Core;

namespace TradersToolbox.Views
{
    /// <summary>
    /// Логика взаимодействия для SymbolsEditor.xaml
    /// </summary>
    public partial class SymbolsEditor : UserControl
    {
        public static Dictionary<string, string> TimeframeUnits => new Dictionary<string, string>() {
            { "s", "s" },
            { "m", "m" },
            { "h", "h" },
            { "D", "D" },
            { "W", "W" },
            { "M", "M" },
        };

        public SymbolsEditor()
        {
            InitializeComponent();

            dataGrid.ItemsSource = Utils.SymbolsManager.Symbols;
        }

        private void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            if (dataGrid.View.HasValidationError)
            {
                ThemedMessageBox.Show("Please complete symbol editing first!", "Symbols editor", MessageBoxButton.OK);
                return;
            }

            OpenFileDialog op = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
            };
            if (op.ShowDialog() == true)
            {
                var slist = dataGrid.ItemsSource as ObservableCollection<Symbol>;
                int index = 1;
                string sname = "UD";
                
                foreach (string f in op.FileNames)
                {
                    while (slist.Any(x => x.Name == $"{sname}{index}" && string.IsNullOrEmpty(x.Timeframe)))
                        index++;

                    // $"User Data {index}"
                    Symbol ns = new Symbol($"{sname}{index}", "D", 1, Path.GetFileNameWithoutExtension(f), f, "USD", SymbolType.Custom, 0, 1, 0);
                    slist.Add(ns);
                    index++;
                }
            }
        }

        
        private void Button_Restore(object sender, RoutedEventArgs e)
        {
            if (DXMessageBox.Show(this, "Are you really want to restore default symbols settings?", "Please confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                if (dataGrid.ItemsSource is ObservableCollection<Symbol> slist)
                {
                    var todelete = slist.Where(x => x.IsStandard == false).ToList();
                    foreach (var v in todelete)
                        slist.Remove(v);
                }
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            dataGrid.View.CommitEditing();
            dataGrid.View.CancelRowEdit();

            // save to settings
            Properties.Settings.Default.SymbolsXML = Utils.SymbolsManager.SaveToXML();
        }

        private void BarButtonItem_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            if (DXMessageBox.Show(this, "Are you sure you want to delete selected symbols?", "Symbols editor", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                foreach (var r in tableView.GetSelectedRows())
                {
                    tableView.DeleteRow(r.RowHandle);
                }
            }
        }
    }
}
