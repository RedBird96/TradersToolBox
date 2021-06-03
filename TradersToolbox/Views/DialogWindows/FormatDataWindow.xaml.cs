using DevExpress.XtraPrinting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TradersToolbox.Brokers;
using TradersToolbox.DataSources;
using TradersToolbox.ViewModels;
using TradersToolbox.ViewModels.ChartViewModels;
using TradersToolbox.ViewModels.DialogsViewModels;

namespace TradersToolbox.Views.DialogWindows
{
    /// <summary>
    /// Interaction logic for FormatDataWindow.xaml
    /// </summary>
    public partial class FormatDataWindow : UserControl
    {
        public FormatDataWindow()
        {
            InitializeComponent();
        }

        private void Candle_RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            SetChartTypeView(MainChartDataType.Candle);
        }

        private void HollowCandle_RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            SetChartTypeView(MainChartDataType.HollowCandle);
        }

        private void PriceBar_RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            SetChartTypeView(MainChartDataType.PriceBar);
        }

        private void Line_RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            SetChartTypeView(MainChartDataType.Line);
        }

        private void SetChartTypeView(MainChartDataType type)
        {
            switch (type)
            {
                case MainChartDataType.Candle:
                case MainChartDataType.HollowCandle:
                case MainChartDataType.PriceBar:
                    Line_Grid.Visibility = Visibility.Collapsed;
                    UpDown_Grid.Visibility = Visibility.Visible;
                    break;
                case MainChartDataType.Line:
                    Line_Grid.Visibility = Visibility.Visible;
                    UpDown_Grid.Visibility = Visibility.Collapsed;
                    break;
            }

            (DataContext as FormatDataViewModel).CurrentChartData.Type = type;
        }
        private void FirstData_RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            SetStartDataView(StockHistorySettingsType.FirstData);
        }

        private void BarsBack_RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            SetStartDataView(StockHistorySettingsType.NumberBarsBack);
        }

        private void YearsBack_RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            SetStartDataView(StockHistorySettingsType.NumberYearsBack);
        }

        private void SetStartDataView(StockHistorySettingsType type)
        {
            switch (type)
            {
                case StockHistorySettingsType.FirstData:
                    DateTimeStart_Grid.Visibility = Visibility.Visible;
                    IntBars_Grid.Visibility = Visibility.Collapsed;
                    YearsBack_Grid.Visibility = Visibility.Collapsed;
                    break;
                case StockHistorySettingsType.NumberBarsBack:
                    DateTimeStart_Grid.Visibility = Visibility.Collapsed;
                    IntBars_Grid.Visibility = Visibility.Visible;
                    YearsBack_Grid.Visibility = Visibility.Collapsed;
                    break;
                case StockHistorySettingsType.NumberYearsBack:
                    DateTimeStart_Grid.Visibility = Visibility.Collapsed;
                    IntBars_Grid.Visibility = Visibility.Collapsed;
                    YearsBack_Grid.Visibility = Visibility.Visible;
                    break;
            }

           (DataContext as FormatDataViewModel).CurrentHistorySettings.Type = type;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            switch ((DataContext as FormatDataViewModel).CurrentHistorySettings.Type)
            {
                case StockHistorySettingsType.FirstData:
                    FirstData_RadioButton.IsChecked = true;
                    break;
                case StockHistorySettingsType.NumberBarsBack:
                    BarsBack_RadioButton.IsChecked = true;
                    break;
                case StockHistorySettingsType.NumberYearsBack:
                    YearsBack_RadioButton.IsChecked = true;
                    break;
            }

            switch ((DataContext as FormatDataViewModel).CurrentChartData.Type)
            {
                case MainChartDataType.Candle:
                    Candle_RadioButton.IsChecked = true;
                    break;
                case MainChartDataType.HollowCandle:
                    HollowCandle_RadioButton.IsChecked = true;
                    break;
                case MainChartDataType.PriceBar:
                    PriceBar_RadioButton.IsChecked = true;
                    break;
                case MainChartDataType.Line:
                    Line_RadioButton.IsChecked = true;
                    break;
            }
        }
    }
}
