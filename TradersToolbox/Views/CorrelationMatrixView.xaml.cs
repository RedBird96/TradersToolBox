using DevExpress.Xpf.Grid;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using TradersToolbox.ViewModels;

namespace TradersToolbox.Views
{
    /// <summary>
    /// Interaction logic for CorrelationMatrixView.xaml
    /// </summary>
    public partial class CorrelationMatrixView : UserControl
    {
        public CorrelationMatrixView()
        {
            InitializeComponent();
        }

        /*private void TableView_Loaded(object sender, RoutedEventArgs e)
        {
            if(sender is TableView t && t.DataContext is CorrelationMatrixViewModel vm)
            {
                DataTable dt = vm.DataTable;

                for (int i=1;i<= dt.Columns.Count;i++)
                {
                    //t.FormatConditions
                }
            }
        }*/
    }
}
