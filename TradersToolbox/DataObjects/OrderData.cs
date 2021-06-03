using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TradersToolbox.Data
{
    public enum OP_MODE
    {
        BUY = 1,
        SELL,
        LIMIT_BUY,
        LIMIT_SELL,
        STOP_BUY,
        STOP_LIMIT
    }

    public class OrderData: INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Symbol { get; set; }

        private string _order_mode;
        public string Type 
        {
            get { return _order_mode; }
            set
            {
                if (value != _order_mode)
                {
                    _order_mode = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public DateTime Open_Time { get; set; }
//        public string Close_Time { get; set; }

        private double _order_open_price;
        public double Open_Price 
        { 
            get { return _order_open_price; } 
            set
            {
                if (value != _order_open_price)
                { 
                    _order_open_price = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private double _order_close_price;
        public double Close_Price 
        {
            get { return _order_close_price; }
            set
            {
                if (value != _order_close_price)
                {
                    if (Type == TradersToolbox.Properties.Resources.LONG)
                    {
                        Profit = (value - Open_Price) * Shares;
                        NotifyPropertyChanged(nameof(Profit));
                    }
                    else if (Type == TradersToolbox.Properties.Resources.SHORT)
                    {
                        Profit = (Open_Price - value) * Shares;
                        NotifyPropertyChanged(nameof(Profit));
                    }

                    _order_close_price = value;
                    NotifyPropertyChanged();
                }
            } 
        }
        public int _amount;
        public int Shares 
        {
            get {return _amount; }
            set
            {
                if (value != _amount)
                {
                    _amount = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public double Profit { get; set; }

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
    }
    public class TransactionData
    {
        public int Id { get; set; }
        public string Symbol { get; set; }
        public string Type { get; set; }
        public DateTime Open_Time { get; set; }
        public double Open_Price { get; set; }
        public int Shares { get; set; }
        public double Profit { get; set; }

        public TransactionData()
        {

        }
        public TransactionData(OrderData od)
        {
            Id = od.Id;
            Symbol = od.Symbol;
            Type = od.Type;
            Open_Time = od.Open_Time;
            Open_Price = od.Open_Price;
            Shares = od.Shares;
            Profit = od.Profit;
        }
    }

    public class SummaryData : INotifyPropertyChanged
    {
        public DateTime Date { get; set; }
        public string Symbol { get; set; }

        private double _realized;
        public double Realized
        {
            get { return _realized; }
            set
            {
                if (value != _realized)
                {
                    _realized = value;
                    NotifyPropertyChanged(nameof(Realized));
                    NotifyPropertyChanged(nameof(Unrealized));
                    NotifyPropertyChanged(nameof(Total));
                }
            }
        }

        private double _unrealized;
        public double Unrealized
        {
            get { return _unrealized; }
            set
            {
                if (value != _unrealized)
                {
                    _unrealized = value;
                    NotifyPropertyChanged(nameof(Realized));
                    NotifyPropertyChanged(nameof(Unrealized));
                    NotifyPropertyChanged(nameof(Total));
                }
            }
        }

        public double _total;
        public double Total 
        {
            get { return  _total; } 
            set
            {
                if (_total != value)
                {
                    _total = value;
                    NotifyPropertyChanged(nameof(Realized));
                    NotifyPropertyChanged(nameof(Unrealized));
                    NotifyPropertyChanged(nameof(Total));

                }
            }
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
    }

}
