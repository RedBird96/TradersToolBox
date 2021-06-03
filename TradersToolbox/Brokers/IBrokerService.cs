using DevExpress.Xpf.Charts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TradersToolbox.Data;
using TradersToolbox.DataObjects;

namespace TradersToolbox.Brokers
{
    public interface IBrokerService
    {
        string BrokerName { get; }
        string BrokerKey { get; }
        bool IsLoggedIn { get; }
        string UserId { get; }

        Task<bool> LogIn();
        bool LogInDialog();
        Task LogOut();

        Task ProxySettingsChanged();


        #region Quotes
        List<string> StreamQuotesTickersList { get; }
        Task<bool> StreamQuotes(List<string> tickers, bool isResetTickers);
        void StopStreamQuotes(List<string> tickers);

        #endregion

        StocksInformation GetStockMarketData(string symbol);

        Task<List<TickerData>> SuggestSymbols(string text, int maxCount);

        #region Bars
        Task<IBarsStreamer> GetBarsStreamer(string symbol, ChartIntervalItem interval, IBarsStreamer barsStreamer, bool connect);

        Task<ObservableCollection<TradingData>> GetBarsRange(string symbol, ChartIntervalItem interval,
            DateTime? startDate, DateTime? stopDate, IBarsStreamer barsStreamer);


        /*Task<ObservableCollection<TradingData>> StreamBarsRange(string symbol, string unit, int interval, DateTime startDate, DateTime stopDate);

        Task<ObservableCollection<TradingData>> StreamBarsFromStartDate(string symbol, string unit, int interval, DateTime startDate);

        //Task<ObservableCollection<TradingData>> StreamBarsDaysBack(string symbol, string unit, int interval, int daysBack);

        Task<ObservableCollection<TradingData>> StreamBarsBarsBack(string symbol, string unit, int interval, int barsBack, DateTime lastDate);

        void StopStreamBars(ObservableCollection<TradingData> collection);*/
        #endregion

    }
}
