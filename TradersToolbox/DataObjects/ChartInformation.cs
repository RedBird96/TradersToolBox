using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TradersToolbox.DataObjects
{
    public class ChartInformation : INotifyPropertyChanged
    {
        private string _changeValue;

        public string changeValue
        {
            get => _changeValue;
            set
            {
                if (value != _changeValue)
                {
                    _changeValue = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private string _percentValue;

        public string percentValue
        {
            get => _percentValue;
            set
            {
                if (value != _percentValue)
                {
                    _percentValue = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private string _volumeValue;

        public string volumeValue
        {
            get => _volumeValue;
            set
            {
                if (value != _volumeValue)
                {
                    _volumeValue = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private string _averagevolumeValue;

        public string averagevolumeValue
        {
            get => _averagevolumeValue;
            set
            {
                if (value != _averagevolumeValue)
                {
                    _averagevolumeValue = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private string _sharesoutstandValue;

        public string sharesoutstandValue
        {
            get => _sharesoutstandValue;
            set
            {
                if (value != _sharesoutstandValue)
                {
                    _sharesoutstandValue = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private string _marketcapitalizationValue;

        public string marketcapitalizationValue
        {
            get => _marketcapitalizationValue;
            set
            {
                if (value != _marketcapitalizationValue)
                {
                    _marketcapitalizationValue = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private string _floatValue;

        public string floatValue
        {
            get => _floatValue;
            set
            {
                if (value != _floatValue)
                {
                    _floatValue = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private string _shortPercentValue;

        public string shortPercentValue
        {
            get => _shortPercentValue;
            set
            {
                if (value != _shortPercentValue)
                {
                    _shortPercentValue = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public ChartInformation()
        {
            changeValue ="Change : 0";
            percentValue = "Percent : 0";
            volumeValue = "Volume : 0";
            averagevolumeValue = "Average Volume : 0";
            sharesoutstandValue = "Shares : ---";
            marketcapitalizationValue = "Market Capitalization : ---";
            floatValue = "Float : ---";
            shortPercentValue = "Short : ---";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class StocksInformation
    {
        public string SharesOutstanding;

        public string MarketCapitalization;
    }
}
