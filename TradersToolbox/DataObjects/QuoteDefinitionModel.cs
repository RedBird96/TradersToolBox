using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using TradersToolbox.Brokers;

namespace TradersToolbox.DataObjects
{
    public class QuoteDefinitionModel : INotifyPropertyChanged
    {
        private bool isMarketOpened;


        public string Symbol { get; set; }

        public int Order { get; set; }


        //public decimal PriceDif { get; set; }


        private decimal _lastPrice;
        public decimal LastPrice
        {
            get => _lastPrice;
            set
            {
                if (value != _lastPrice)
                {
                    //LastPriceColor = LastPrice == 0 ? Colors.Transparent : (value > _lastPrice ? Colors.Green : Colors.Red);
                    //PriceDif = value - _lastPrice;
                    _lastPrice = value;
                    NotifyPropertyChanged();
                    //NotifyPropertyChanged(nameof(LastPriceColor));
                    //NotifyPropertyChanged(nameof(PriceDif));
                    NotifyPropertyChanged(nameof(NetChangePct));
                    NotifyPropertyChanged(nameof(MarketOpenedNetChangePct));
                    NotifyPropertyChanged(nameof(MarketOpenedLastPrice));
                }
            }
        }

        //public Color LastPriceColor { get; set; }

        private DateTime _lastDateTimeUTC;
        public DateTime LastDateTimeUTC
        {
            get => _lastDateTimeUTC;
            set
            {
                if(value!=_lastDateTimeUTC)
                {
                    bool isNextDay = _lastDateTimeUTC == default ? false : (_lastDateTimeUTC.Date < value.Date);

                    _lastDateTimeUTC = value;       //todo: reset close/prevClose on new day

                    var dt = TimeZoneInfo.ConvertTimeFromUtc(_lastDateTimeUTC, BrokersManager.EasternZone);
                    isMarketOpened = dt.TimeOfDay >= TimeSpan.FromMinutes(570) && dt.TimeOfDay < TimeSpan.FromMinutes(960);    //9:30-16:00

                    if (isMarketOpened)
                        _close = _lastPrice;    //update close value while market opened to have actual close price on market close

                    if(isNextDay)
                    {
                        _previousClose = _close;
                        _close = null;
                    }

                    NotifyPropertyChanged();
                    //NotifyPropertyChanged(nameof(LastPriceColor));
                    //NotifyPropertyChanged(nameof(PriceDif));
                    NotifyPropertyChanged(nameof(NetChangePct));
                    NotifyPropertyChanged(nameof(MarketOpenedNetChangePct));
                    NotifyPropertyChanged(nameof(MarketOpenedLastPrice));
                }
            }
        }

        private decimal? _close;
        public decimal? Close {
            get => _close;
            set
            {
                if (value != _close)
                {
                    _close = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged(nameof(NetChangePct));
                    NotifyPropertyChanged(nameof(MarketOpenedNetChangePct));
                    NotifyPropertyChanged(nameof(MarketOpenedLastPrice));
                }
            }
        }


        private decimal? _previousClose;
        public decimal? PreviousClose
        {
            get => _previousClose;
            set
            {
                if (value != _previousClose && value != 0)
                {
                    _previousClose = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged(nameof(NetChangePct));
                    NotifyPropertyChanged(nameof(MarketOpenedNetChangePct));
                    NotifyPropertyChanged(nameof(MarketOpenedLastPrice));
                }
            }
        }


        public decimal MarketOpenedLastPrice
        {
            get
            {
                if (_lastDateTimeUTC != default)
                {
                    var dt = TimeZoneInfo.ConvertTimeFromUtc(_lastDateTimeUTC, BrokersManager.EasternZone);
                    bool IsMarketOpened = dt.TimeOfDay >= TimeSpan.FromMinutes(570) && dt.TimeOfDay < TimeSpan.FromMinutes(960);    //9:30-16:00
                    if (!IsMarketOpened)
                        return _close.HasValue ? _close.Value : (_previousClose.HasValue ? _previousClose.Value : _lastPrice);
                }
                return _lastPrice;
            }
        }

        public decimal MarketOpenedNetChangePct
        {
            get
            {
                if (PreviousClose == null || MarketOpenedLastPrice == 0)
                    return _netChangePct;
                else
                {
                    return 100 * (MarketOpenedLastPrice - PreviousClose.Value) / PreviousClose.Value;
                }
            }
        }


        private decimal _netChangePct;
        public decimal NetChangePct
        {
            get
            {
                if (PreviousClose == null || LastPrice == 0)
                    return _netChangePct;
                else
                {
                    return 100 * (LastPrice - (decimal)PreviousClose) / (decimal)PreviousClose;
                }
            }
            set
            {
                if (value != _netChangePct)
                {
                    _netChangePct = value;
                    NotifyPropertyChanged();
                }
            }
        }


        private string _description;
        public string Description
        {
            get => _description ?? Symbol;
            set
            {
                if (value != _description)
                {
                    _description = value;
                    NotifyPropertyChanged();
                }
            }
        }


        public string Group { get; set; }


        private int _scanResult;
        public int ScanResult
        {
            get => _scanResult;
            set
            {
                if (value != _scanResult)
                {
                    _scanResult = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged(nameof(ScanString));
                }
            }
        }

        public string ScanString
        {
            get
            {
                switch (ScanResult)
                {
                    default:
                    case 0: return string.Empty;
                    case 1: return "negative";
                    case 2: return "positive";
                    case 3: return "no data";
                }
            }
        }


        public QuoteDefinitionModel()
        {
        }

        public QuoteDefinitionModel(string _symbol, int _order, string group)
        {
            Symbol = _symbol;
            Order = _order;
            Group = group;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        // This method is called by the Set accessor of each property.  
        // The CallerMemberName attribute that is applied to the optional propertyName  
        // parameter causes the property name of the caller to be substituted as an argument.  
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
