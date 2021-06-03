using DevExpress.Mvvm;
using DevExpress.Xpf.Charts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TradersToolbox.Core;
using TradersToolbox.Data;
using TradersToolbox.DataObjects;

namespace TradersToolbox.Brokers
{
    public class BrokersManager : IBrokerService, INotifyPropertyChanged
    {
        HttpClient httpClient;

        public List<IBrokerService> BrokersList { get; private set; }
        public readonly PolygonIO PolygonIO;
        public readonly TradeStation TradeStation;
        private readonly IBrokerService defaultBroker;

        private IBrokerService activeBroker_internal;
        public IBrokerService ActiveBroker
        {
            get => activeBroker_internal;
            private set
            {
                if (activeBroker_internal != value)
                {
                    activeBroker_internal = value;  //todo: log out previous?
                    NotifyPropertyChanged();
                }
                NotifyPropertyChanged(nameof(BrokerName));
                NotifyPropertyChanged(nameof(BrokerKey));
                NotifyPropertyChanged(nameof(IsLoggedIn));
                NotifyPropertyChanged(nameof(UserId));
            }
        }

        public string BrokerName => ActiveBroker.BrokerName;
        public string BrokerKey => ActiveBroker.BrokerKey;
        public bool IsLoggedIn => ActiveBroker.IsLoggedIn;
        public string UserId => ActiveBroker.UserId;

        public static readonly TimeZoneInfo EasternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

        /*private string _connectionState;
        public string ConnectionState
        {
            get => _connectionState;
            set
            {
                if(_connectionState!=value)
                {
                    _connectionState = value;
                    NotifyPropertyChanged();
                }
            }
        }*/

        public List<string> StreamQuotesTickersList { get; } = new List<string>();

        public BrokersManager()
        {
            httpClient = HttpClientHelper.GetHttpClient();
            //ConnectionState = "Offline";
            
            PolygonIO = new PolygonIO();
            //PolygonIO.PropertyChanged += BrokerService_PropertyChanged;
            TradeStation = new TradeStation();
            //TradeStation.PropertyChanged += BrokerService_PropertyChanged;
            ActiveBroker = defaultBroker = PolygonIO;
            BrokersList = new List<IBrokerService>() { PolygonIO, TradeStation };

            switch(Properties.Settings.Default.Broker)
            {
                default:
                case "PolygonIO": ActiveBroker = PolygonIO; break;
                case "TradeStation": ActiveBroker = TradeStation; break;
            }
        }

        /*private void BrokerService_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var broker = sender as IBrokerService;
            if(e.PropertyName == nameof(IBrokerService.IsLoggedIn))
            {
                ActiveBroker = broker.IsLoggedIn ? broker : defaultBroker;
            }
        }*/

        //todo: a function to update bar streamers (on broker changed)

        public async Task<bool> LogIn(IBrokerService broker = null)
        {
            if (broker == null)
                broker = ActiveBroker;

            bool success = await broker.LogIn();
            if (success)
            {
                if(broker != ActiveBroker)
                    await ActiveBroker.LogOut();
                ActiveBroker = broker;

                Messenger.Default.Send(new BrokerChangedLoggedIn());
            }
            return success;
        }

        public Task<bool> LogIn()
        {
            throw new NotImplementedException();
        }

        public bool LogInDialog()
        {
            throw new NotImplementedException();
        }

        public bool LogInDialog(IBrokerService broker = null)
        {
            if (broker == null)
                broker = ActiveBroker;
            
            bool success = broker.LogInDialog();
            if (success)
            {
                if(broker != ActiveBroker)
                    ActiveBroker.LogOut();
                ActiveBroker = broker;

                Messenger.Default.Send(new BrokerChangedLoggedIn());
            }
            return success;
        }

        public async Task LogOut()
        {
            if (ActiveBroker == defaultBroker)
            {
                // do not log out from default broker
            }
            else
            {
                await ActiveBroker.LogOut();

                // log into default broker
                ActiveBroker = defaultBroker;
                await ActiveBroker.LogIn();
            }
        }

        public void Shutdown()
        {
            // save settings
            if (ActiveBroker == TradeStation)
                Properties.Settings.Default.Broker = "TradeStation";
            else
                Properties.Settings.Default.Broker = "PolygonIO";
        }

        public async Task ProxySettingsChanged()
        {
            httpClient.CancelPendingRequests();
            httpClient = HttpClientHelper.GetHttpClient();

            foreach (var b in BrokersList)
                await b.ProxySettingsChanged();
        }

        /// <summary>
        /// Read stock tickers from FINRA
        /// </summary>
        public async Task<List<TickerData>> ReadStockTickers()
        {
            // Read all symbols from OATS Reportable Security Daily List
            // https://www.finra.org/filing-reporting/oats/oats-reportable-securities-list

            string resString = null;
            List<TickerData> symbols = new List<TickerData>();

            try
            {
                resString = await httpClient.GetStringAsync("http://oatsreportable.finra.org/OATSReportableSecurities-SOD.txt");
            }
            catch (Exception ex)
            {
                Logger.Current.Warn(ex, "Unable to read stock tickers from FINRA!");
                return symbols;
            }

            return await Task.Run(() =>
            {
                bool skipFirstLine = true;
                using (System.IO.StringReader reader = new System.IO.StringReader(resString))
                {
                    string line = reader.ReadLine();
                    while (line != null)
                    {
                        if (skipFirstLine)
                        {
                            skipFirstLine = false;
                            if (!line.StartsWith("A"))
                                continue;
                        }

                        var cells = line.Split('|');
                        if (cells.Length == 3)
                        {
                            bool success = true;

                            // Filter by exchange
                            //Exchange ex = Exchange.NASDAQ;
                            switch(cells[2])
                            {
                                case "NASDAQ":break;// ex = Exchange.NASDAQ; break;
                                case "NYSE":  break;// ex = Exchange.NYSE; break;
                                case "ARCA":  break;// ex = Exchange.ARCA; break;
                                case "AMEX":  break;// ex = Exchange.AMEX; break;
                                case "BATS":  break;// ex = Exchange.BATS; break;
                                default:
                                    success = false;
                                    break;
                            }

                            // Filter by ticker
                            if (success && cells[0] == "IGLD" || cells[0].Contains(' '))
                                success = false;

                            // Filter warrants
                            if(success && cells[1].ToLower().Contains("warrant"))
                                success = false;
                            
                            if (success)
                                symbols.Add(new TickerData(cells[0], cells[2], cells[1]));
                        }

                        line = reader.ReadLine();
                    }
                }

                return symbols;
            });
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        // This method is called by the Set accessor of each property.  
        // The CallerMemberName attribute that is applied to the optional propertyName  
        // parameter causes the property name of the caller to be substituted as an argument.  
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        public async Task<bool> StreamQuotes(List<string> tickers, bool isResetTickers)
        {
            return await ActiveBroker.StreamQuotes(tickers, isResetTickers);
        }

        public void StopStreamQuotes(List<string> tickers)
        {
            ActiveBroker.StopStreamQuotes(tickers);
        }

        public async Task<List<TickerData>> SuggestSymbols(string text, int maxCount)
        {
            return await ActiveBroker.SuggestSymbols(text, maxCount);
        }

        /// <summary>
        /// Creates or Updates bars streamer
        /// </summary>
        public async Task<IBarsStreamer> GetBarsStreamer(string symbol = null, ChartIntervalItem interval = null, IBarsStreamer barsStreamer = null, bool connect = true)
        {
            return await ActiveBroker.GetBarsStreamer(symbol, interval, barsStreamer, connect);
        }

        public async Task<ObservableCollection<TradingData>> GetBarsRange(string symbol = null, ChartIntervalItem interval = null,
            DateTime? startDate = null, DateTime? stopDate = null, IBarsStreamer barsStreamer = null)
        {
            return await ActiveBroker.GetBarsRange(symbol, interval, startDate, stopDate, barsStreamer);
        }

        public StocksInformation GetStockMarketData(string symbol)
        {
            return ActiveBroker.GetStockMarketData(symbol);
        }
    }
}
