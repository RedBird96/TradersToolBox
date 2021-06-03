using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradersToolbox.ViewModels;
using TradersToolbox.Core;
using TradersToolbox.DataObjects;
using TradersToolbox.Data;

namespace TradersToolbox
{
    // Message declarations for DevExpress.Mvvm.Messenger

    public class CustomIndicatorsChangedMessage { }

    public class PortfolioChangedMessage
    {
        public IEnumerable<SimStrategy> PortfolioStrategies { get; }

        public PortfolioChangedMessage(IEnumerable<SimStrategy> portfolioStrategies)
        {
            PortfolioStrategies = portfolioStrategies;
        }
    }

    public class AddStrategiesToPortfolioMessage
    {
        public List<SimStrategy> Strategies { get; }

        public AddStrategiesToPortfolioMessage(List<SimStrategy> strategies)
        {
            Strategies = strategies;
        }
    }

    public class AddWatchlistMessage
    {
        public WatchlistViewModel Watchlist { get; }

        public AddWatchlistMessage(WatchlistViewModel watchlist)
        {
            Watchlist = watchlist;
        }
    }

    public class BrokerChangedLoggedIn
    {

    }

    public class QuoteSelectedMessage
    {
        public QuoteSelectedMessage(string symbol)
        {
            Symbol = symbol;
        }

        public string Symbol { get; }
    }

    public class QuoteReceivedMessage
    {
        public string Symbol { get; }
        public decimal? Ask { get; }
        public decimal? Bid { get; }
        public decimal? Last { get; }
        public decimal? DailyVolume { get; }

        public int? Volume { get; }

        public decimal? Close { get; }
        public decimal? PreviousClose { get; }

        public DateTime? LastDateTimeUTC { get; }

        public bool? IsMarketOpened { get; }

        public QuoteReceivedMessage(string symbol, bool? isMarketOpened=default, decimal? ask = default, decimal? bid = default, decimal? last = default, decimal? dailyVolume = default,
            decimal? close=default, decimal? prevClose = default, DateTime? lastDateTimeUTC = null, int? volume = default)
        {
            Symbol = symbol;
            IsMarketOpened = isMarketOpened;
            Ask = ask;
            Bid = bid;
            Last = last;
            DailyVolume = dailyVolume;
            Close = close;
            PreviousClose = prevClose;
            LastDateTimeUTC = lastDateTimeUTC;
            Volume = volume;
        }
    }

    public class QuoteInfoMessage
    {
        public string Symbol { get; }
        public string Description { get; }

        public QuoteInfoMessage(string symbol, string description)
        {
            Symbol = symbol;
            Description = description;
        }
    }

    public class AggregateReceivedMessage
    {
        public PolygonIOWebSocketAggregate Aggregate { get; }

        public AggregateReceivedMessage(PolygonIOWebSocketAggregate aggregate)
        {
            Aggregate = aggregate;
        }
    }

    public class ExtendedHoursChangedMessage
    {
        private bool UseExtendedHours { get; }

        public ExtendedHoursChangedMessage(bool useExtendedHours)
        {
            UseExtendedHours = useExtendedHours;
        }
    }

    /*public class BarReceivedMessage
    {
        public TradingData Data { get; }

        public BarReceivedMessage(TradingData data)
        {
            Data = data;
        }
    }*/

    public class QuotesViewErrorMessage
    {
        public string Message { get; set; }

        public QuotesViewErrorMessage(string message)
        {
            Message = message;
        }
    }
}
