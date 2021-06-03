using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.POCO;
using System;
namespace TradersToolbox.ViewModels
{
    public enum OrderType { Market, Limit }
    public enum OrderDirection { Buy, Sell }
    public enum OrderDuration { Day, GTC }

    [POCOViewModel]
    public class OrderEntryViewModel
    {
        public virtual string Symbol { get; set; }
        public virtual string Description { get; set; }

        //todo: icon
        //todo: realtime marker
        //todo: market state
        //todo: Day range

        public virtual decimal Bid { get; set; }
        public virtual decimal Ask { get; set; }
        public virtual decimal Spread { get; set; }

        public virtual decimal Position { get; set; }
        public virtual OrderDuration Duration { get; set; }

        public static OrderEntryViewModel Create()
        {
            return ViewModelSource.Create(() => new OrderEntryViewModel());
        }
        protected OrderEntryViewModel()
        {

        }

        [Command]
        public void PlaceOrder()
        {

        }
    }
}