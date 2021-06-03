using DevExpress.Xpf.Charts;
using DevExpress.Xpf.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TradersToolbox.Views
{
    /// <summary>
    /// Interaction logic for StockChartView.xaml
    /// </summary>
    public partial class StockChartView : UserControl
    {
        public ChartControl Chart { get { return chart; } }

        public StockChartView()
        {
            InitializeComponent();
        }
    }
}
