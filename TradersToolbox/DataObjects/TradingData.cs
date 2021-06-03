using StockPatternSearch;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace TradersToolbox.Data {
    public class TradingData : INotifyPropertyChanged {
        private double open;
        private double high;
        private double low;
        private double close;
        private double volume;
        private Color volumeColor;

        public DateTime Date { get; set; }
        public double Open {
            get { return open; }
            set { 
                if(open != value) {
                    open = value;
                    RaisePropertyChanged();
                    UpdateVolumeColor();
                }
            }
        }
        public double High {
            get { return high; }
            set {
                if (high != value) {
                    high = value;
                    RaisePropertyChanged();
                }
            }
        }
        public double Low {
            get { return low; }
            set { 
                if(low != value) {
                    low = value;
                    RaisePropertyChanged();
                }
            }
        }
        public double Close {
            get { return close; }
            set { 
                if(close != value) {
                    close = value;
                    RaisePropertyChanged();
                    UpdateVolumeColor();
                }
            }
        }
        public double Volume {
            get { return volume; }
            set {
                if(volume != value) {
                    volume = value;
                    RaisePropertyChanged();
                }
            }
        }
        public Color VolumeColor {
            get {
                return volumeColor;
            }
            set {
                if(volumeColor != value) {
                    volumeColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        public TradingData(DateTime date, double open, double high, double low, double close, double volume) {
            Date = date;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Volume = volume;
            UpdateVolumeColor();
        }

        //For save/load workspace
        public TradingData() { }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        void UpdateVolumeColor() {
            VolumeColor = close >= open ? Color.FromRgb(65, 210, 65) : Color.FromRgb(255, 50, 50);
        }

        public int DateInt { get => StockFactory.DateToInt(Date); }
        public int Time { get => TimeStringToInt(Date); }
        public string TimeStr { get => StockFactory.TimeToStr(Time); }
        public bool isPattern { get; set; }

        int TimeStringToInt(DateTime time)
        {
            int hour = time.Hour;
            int minute = time.Minute;
            return hour * 100 + minute;
        }
    }
}
