using DevExpress.Mvvm;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xaml.Schema;
using TradersToolbox.Core;
using TradersToolbox.ViewModels;

namespace TradersToolbox.Views
{
    /// <summary>
    /// Interaction logic for QuotesView.xaml
    /// </summary>
    public partial class QuotesView : UserControl
    {
     
        // temporary copy of QuotesView(bool hardwareAccelerated = true)
        public QuotesView()
        {
            InitializeComponent();

            Messenger.Default.Register<QuotesViewErrorMessage>(this,(msg)=> {

                Dispatcher.Invoke(() => {
                    TbError.Text = msg.Message;
                    BorderError.Visibility = Visibility.Visible;
                });
            });
        }

        private void ButtonDeleteError_Click(object sender, RoutedEventArgs e)
        {
            BorderError.Visibility = Visibility.Hidden;
        }

        /*public QuotesView(bool hardwareAccelerated = true)
        {
            InitializeComponent();

            if (!hardwareAccelerated) RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;

            int order = 0;
            foreach (var sym in MainForm.symbolsManager.Symbols.Where(x => x.IsStandard && (x.Type == SymbolType.Futures ||
                x.Type == SymbolType.ETF || x.Type == SymbolType.FOREX)))
            {
                if (sym.Type == SymbolType.Futures)
                {
                    string ss;
                    switch (sym.Name)
                    {
                        case "BC": ss = "BTC"; break;
                        case "BU": ss = "FGBL"; break;
                        case "FD": ss = "FDAX"; break;
                        case "RT": ss = "RTY"; break;
                        default: ss = sym.Name; break;
                    }
                    requestSymbols[$"@{ss}"] = new KeyValuePair<string, int>(sym.Name, order);
                }
                else
                    requestSymbols[sym.Name] = new KeyValuePair<string, int>(sym.Name, order);
                order++;
            }

            grid.ItemsSource = model;

            marketdataApi = new TradeStationAPI.Api.MarketdataApi();
            marketdataApi.Configuration.ApiClient.ConfigureWebRequest((httpWebRequest) =>
            {
                httpWebRequest.AutomaticDecompression = DecompressionMethods.None;  // do not use compression
            });
            cancellationTokenSource = new CancellationTokenSource();
        }



        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(MainForm.TSAccessToken)) return;
            
            tbo.Text = "";
            try
            {
                SymbolSearchDefinition s = await marketdataApi.SearchSymbolsAsync(MainForm.TSAccessToken, tbs.Text);
                if (s != null)
                    foreach (var d in s)
                    {
                        tbo.AppendText($"{d.Name}, {d.Category}, {d.Description}, {d.SectorName}");
                        tbo.AppendText(Environment.NewLine);
                    }
            }
            catch (ApiException ex)
            {
                tbo.Text = ex.Message;
            }
        }

        
        private void Datagrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(e.ChangedButton == MouseButton.Left && sender is DataGridCell cell)
            {
                // open chart in universal window
                var quote = cell.DataContext as QuoteDefinitionModel;
                OpenLiveChart(quote.quote.Symbol, quote.Symbol, quote.quote.Description);

                //  (this.DataContext as QuotesViewModel).CreateDocument(sender);
            }
        }

        private void MenuItemOpen_Click(object sender, RoutedEventArgs e)
        {
            if(sender is MenuItem item && item.DataContext is QuoteDefinitionModel quote)
            {
                // open chart in universal window
                OpenLiveChart(quote.quote.Symbol, quote.Symbol, quote.quote.Description);
            }
        }

        private void MenuItemOpenNew_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item && item.DataContext is QuoteDefinitionModel quote)
            {
                // open chart in universal window
                OpenLiveChart(quote.quote.Symbol, quote.Symbol, quote.quote.Description, true);
            }
        }

        private void OpenLiveChart(string symbol, string printSymbol, string description, bool newWindow = false)
        {
            /*if (liveChart != null && liveWindow != null && !newWindow)
            {
                liveWindow.Title = $"Live Chart - {printSymbol}";
                liveChart.UpdateChart(symbol, printSymbol, description);
                //liveWindow.Show();
                liveWindow.WindowState = WindowState.Normal;
                liveWindow.Activate();
            }
            else{
            
                Window w = new Window()
                {
                    Title = "Live Chart",
                    Width = 800,
                    Height = 500,
                    MinWidth = 200,
                    MinHeight = 200
                };
                //ElementHost.EnableModelessKeyboardInterop(w);
                LiveChart view = new LiveChart(symbol, printSymbol,description, Properties.Settings.Default.DirectXGUI > 0);
                w.Content = view;
                w.Show();

                if (!newWindow)
                {
                    liveChart = view;
                    liveWindow = w;
                    w.Closed += (a, b) => { liveChart = null; liveWindow = null; };
                




                // test = DX chart
                //DXLiveChartWindow chartWindow = new DXLiveChartWindow();
                //ElementHost.EnableModelessKeyboardInterop(chartWindow);
                //chartWindow.Show();
            
        }
*/
    }
}
