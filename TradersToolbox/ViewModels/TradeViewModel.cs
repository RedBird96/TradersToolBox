using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.XtraPrinting.Native;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using TradersToolbox.Core;
using TradersToolbox.ViewModels;
using Dispatcher = System.Windows.Threading.Dispatcher;
using TradersToolbox.Data;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;
using System.Globalization;
using TradersToolbox.DataObjects;
using DevExpress.Mvvm.POCO;
using DevExpress.Xpf.Core;
using System.Windows.Data;

namespace TradersToolbox.ViewModels
{
    [POCOViewModel]
    public class TradeViewModel : PanelWorkspaceViewModel
    {
        protected override string WorkspaceName { get { return "VerticalBottomHost"; } set { } }


        public delegate void SendMessage(string error);

        public event SendMessage Error;


        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual ObservableCollection<TransactionData> Transaction_list { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual ObservableCollection<OrderData> Position_list { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual ObservableCollection<SummaryData> Summary_list { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual ObservableCollection<OrderData> OpenOrders_lists { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual ICollectionView TransactionCollection { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual ICollectionView PositionCollection { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual ICollectionView SummaryCollection { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual ICollectionView OpenOrdersCollection { get; set; }

        protected void OnTransaction_listChanged()
        {
            TransactionCollection = CollectionViewSource.GetDefaultView(Transaction_list);
            BindingOperations.EnableCollectionSynchronization(Transaction_list, DataLocker);
        }

        protected void OnPosition_listChanged()
        {
            PositionCollection = CollectionViewSource.GetDefaultView(Position_list);
            BindingOperations.EnableCollectionSynchronization(Position_list, DataLocker);
        }

        protected void OnSummary_listChanged()
        {
            SummaryCollection = CollectionViewSource.GetDefaultView(Summary_list);
            BindingOperations.EnableCollectionSynchronization(Summary_list, DataLocker);
        }

        protected void OnOpenOrders_listsChanged()
        {
            OpenOrdersCollection = CollectionViewSource.GetDefaultView(OpenOrders_lists);
            BindingOperations.EnableCollectionSynchronization(OpenOrders_lists, DataLocker);
        }

        readonly object DataLocker = new object();


        protected TradeViewModel()
        {
            DisplayName = "Trade View";
            DumbAggregator.OnMessageTransmitted += OnMessageReceived;

            Position_list = new ObservableCollection<OrderData>();
            Transaction_list = new ObservableCollection<TransactionData>();
            Summary_list = new ObservableCollection<SummaryData>();
            OpenOrders_lists = new ObservableCollection<OrderData>();
            Position_list = MainWindowViewModel.tradeDB.getPositionRecordsAll();
            Transaction_list = MainWindowViewModel.tradeDB.getTransactionRecordsAll();
            Summary_list = MainWindowViewModel.tradeDB.getSummaryRecordsAll();
            OpenOrders_lists = MainWindowViewModel.tradeDB.getOpenOrderRecordsAll();

            Messenger.Default.Register<QuoteReceivedMessage>(this, OnMessage);

            _ = RequestLiveDataAsync();
        }

        internal void LoadAfterLogin()
        {
            _ = RequestLiveDataAsync();
        }

        #region Message handlers
        void OnMessage(QuoteReceivedMessage message)
        {
            try
            {
                //if (!_dispatcher.HasShutdownStarted && message.Ask.HasValue && message.Bid.HasValue)
                //    _dispatcher.Invoke(() =>
                //    {
                //        UpdatePositionProfit(message);
                //    });
                if (message.Ask.HasValue && message.Bid.HasValue)
                {
                    lock (DataLocker)
                    {
                        UpdatePositionProfit(message);
                    }
                }
            }
            catch (TaskCanceledException) { }
        }
        #endregion

        public async Task RequestLiveDataAsync()
        {
            await MainWindowViewModel.BrokersManager.StreamQuotes(new List<string>(), false);
        }

        private void UpdatePositionProfit(QuoteReceivedMessage quote)
        {
            if (Position_list.Count == 0 && OpenOrders_lists.Count == 0)
                return;

            double BidPriceDisplay = decimal.ToDouble(quote.Bid.Value);
            double AskPriceDisplay = decimal.ToDouble(quote.Ask.Value);

            IEnumerable<OrderData> postition_symbol = Position_list.Where(x => x.Symbol == quote.Symbol && (x.Type == TradersToolbox.Properties.Resources.LONG || x.Type == TradersToolbox.Properties.Resources.SHORT));
            foreach (OrderData no in postition_symbol)
            {
                no.Close_Price = no.Type == TradersToolbox.Properties.Resources.LONG ? BidPriceDisplay : AskPriceDisplay;
                MakeupUnRealizeSummaryRecord(no);
            }
            IEnumerable<OrderData> orders_symbol = OpenOrders_lists.Where(x => x.Symbol == quote.Symbol);
            foreach (OrderData no in orders_symbol)
            {
                no.Close_Price = (no.Type == TradersToolbox.Properties.Resources.BUY_LIMIT || no.Type == TradersToolbox.Properties.Resources.BUY_STOP) ? BidPriceDisplay : AskPriceDisplay;
            }

            IEnumerable<OrderData> postition_symbol_buy_limit_order = OpenOrders_lists.Where(x => x.Symbol == quote.Symbol && x.Type == TradersToolbox.Properties.Resources.BUY_LIMIT &&
                x.Open_Price >= AskPriceDisplay);
            foreach (OrderData no in postition_symbol_buy_limit_order)
            {
                no.Type = TradersToolbox.Properties.Resources.BUY;
                MainWindowViewModel.tradeDB.removeOpenOrderRecord(no);
                OpenOrders_lists.Remove(no);
                no.Close_Price = no.Type == TradersToolbox.Properties.Resources.BUY ? BidPriceDisplay : AskPriceDisplay;
                MakeupPosition(no);
                postition_symbol_buy_limit_order = OpenOrders_lists.Where(x => x.Symbol == quote.Symbol && x.Type == TradersToolbox.Properties.Resources.BUY_LIMIT &&
                    x.Open_Price >= AskPriceDisplay);
                if (postition_symbol_buy_limit_order.Count() == 0)
                    return;
            }
            IEnumerable<OrderData> postition_symbol_buy_stop_order = OpenOrders_lists.Where(x => x.Symbol == quote.Symbol && x.Type == TradersToolbox.Properties.Resources.BUY_STOP &&
                x.Open_Price <= AskPriceDisplay);
            foreach (OrderData no in postition_symbol_buy_stop_order)
            {
                no.Type = TradersToolbox.Properties.Resources.BUY;
                MainWindowViewModel.tradeDB.removeOpenOrderRecord(no);
                OpenOrders_lists.Remove(no);
                no.Close_Price = no.Type == TradersToolbox.Properties.Resources.BUY ? BidPriceDisplay : AskPriceDisplay;
                MakeupPosition(no);
                postition_symbol_buy_stop_order = OpenOrders_lists.Where(x => x.Symbol == quote.Symbol && x.Type == TradersToolbox.Properties.Resources.BUY_STOP &&
                    x.Open_Price <= AskPriceDisplay);
                if (postition_symbol_buy_stop_order.Count() == 0)
                    return;
            }
            IEnumerable<OrderData> postition_symbol_sell_limit_order = OpenOrders_lists.Where(x => x.Symbol == quote.Symbol && x.Type == TradersToolbox.Properties.Resources.SELL_LIMIT &&
                x.Open_Price <= BidPriceDisplay);
            foreach (OrderData no in postition_symbol_sell_limit_order)
            {
                no.Type = TradersToolbox.Properties.Resources.SELL;
                MainWindowViewModel.tradeDB.removeOpenOrderRecord(no);
                OpenOrders_lists.Remove(no);
                no.Close_Price = no.Type == TradersToolbox.Properties.Resources.BUY ? BidPriceDisplay : AskPriceDisplay;
                MakeupPosition(no);
                postition_symbol_sell_limit_order = OpenOrders_lists.Where(x => x.Symbol == quote.Symbol && x.Type == TradersToolbox.Properties.Resources.SELL_LIMIT &&
                    x.Open_Price <= BidPriceDisplay);
                if (postition_symbol_sell_limit_order.Count() == 0)
                    return;
            }
            IEnumerable<OrderData> postition_symbol_sell_stop_order = OpenOrders_lists.Where(x => x.Symbol == quote.Symbol && x.Type == TradersToolbox.Properties.Resources.SELL_STOP &&
                x.Open_Price >= BidPriceDisplay);
            foreach (OrderData no in postition_symbol_sell_stop_order)
            {
                no.Type = TradersToolbox.Properties.Resources.SELL;
                MainWindowViewModel.tradeDB.removeOpenOrderRecord(no);
                OpenOrders_lists.Remove(no);
                no.Close_Price = no.Type == TradersToolbox.Properties.Resources.BUY ? BidPriceDisplay : AskPriceDisplay;
                MakeupPosition(no);
                postition_symbol_sell_stop_order = OpenOrders_lists.Where(x => x.Symbol == quote.Symbol && x.Type == TradersToolbox.Properties.Resources.SELL_STOP &&
                    x.Open_Price >= BidPriceDisplay);
                if (postition_symbol_sell_stop_order.Count() == 0)
                    return;
            }
        }

        private void ClosePosition(int OrderID)
        {
            OrderData opData = new OrderData();
            IEnumerable<OrderData> postition_symbol = Position_list.Where(x => x.Id == OrderID);
            foreach (OrderData no in postition_symbol)
            {
                DateTime date_backup = no.Open_Time;
                opData = no;
                opData.Open_Time = DateTime.Now;
                opData.Open_Price = no.Close_Price;
                opData.Type = no.Type == TradersToolbox.Properties.Resources.LONG ? TradersToolbox.Properties.Resources.SELL : TradersToolbox.Properties.Resources.BUY;

                Position_list.Remove(no);
                MainWindowViewModel.tradeDB.removePositionRecord(no);

                TransactionData tod = new TransactionData(opData);
                Transaction_list.Add(tod);
                MainWindowViewModel.tradeDB.addTransactionRecord(tod);

                //tod.Open_Time = date_backup;
                no.Open_Time = date_backup;
                MakeupRealizeSummaryRecord(tod);
                no.Profit = 0;
                MakeupUnRealizeSummaryRecord(no);
                Logger.Current.Info("Close Order Placed successfully. Order ID({0}) {1} Close Price({2}) ", no.Id, no.Symbol, no.Close_Price);
                return;
            }
        }

        private void CancelOrder(int OrderID)
        {
            IEnumerable<OrderData> postition_symbol = OpenOrders_lists.Where(x => x.Id == OrderID);
            foreach (OrderData no in postition_symbol)
            {
                OpenOrders_lists.Remove(no);
                MainWindowViewModel.tradeDB.removeOpenOrderRecord(no);
                Logger.Current.Info("Delete Order Placed successfully. Order ID({0}) {1}", no.Id,  no.Symbol);
                break;
            }
        }

        private void ShowMessage(string message)
        {
            Error?.Invoke(message);
        }

        private void OnMessageReceived(OrderData oData)
        {
            MakeupPosition(oData);
        }

        public object SelectedRow { get; set; }
        public object SelectedOpenOrderRow { get; set; }

        public void MakeupPosition(OrderData oData)
        {
            if (oData.Type != TradersToolbox.Properties.Resources.BUY && oData.Type != TradersToolbox.Properties.Resources.SELL)
            {
                OpenOrders_lists.Add(oData);
                MainWindowViewModel.tradeDB.addOpenOrderRecord(oData);
                Messenger.Default.Send<string>(oData.Symbol);
                return;
            }

            IEnumerable<OrderData> postition_symbol = Position_list.Where(x => x.Symbol == oData.Symbol);
            foreach (OrderData no in postition_symbol)
            {
                if (no.Type == oData.Type || (no.Type == TradersToolbox.Properties.Resources.LONG && oData.Type == TradersToolbox.Properties.Resources.BUY) || (no.Type == TradersToolbox.Properties.Resources.SHORT && oData.Type == TradersToolbox.Properties.Resources.SELL))
                {
                    if (oData.Type == TradersToolbox.Properties.Resources.LONG)
                        oData.Type = TradersToolbox.Properties.Resources.BUY;
                    else if (oData.Type == TradersToolbox.Properties.Resources.SHORT)
                        oData.Type = TradersToolbox.Properties.Resources.SELL;

                    no.Open_Price = (no.Open_Price * no.Shares + oData.Open_Price * oData.Shares) / (no.Shares + oData.Shares);
                    no.Shares += oData.Shares;
                    TransactionData tOd = new TransactionData(oData);
                    Transaction_list.Add(tOd);
                    MainWindowViewModel.tradeDB.addTransactionRecord(tOd);
                    MainWindowViewModel.tradeDB.updatePositionRecord(no, "Shares", no.Shares.ToString());
                    MainWindowViewModel.tradeDB.updatePositionRecord(no, "OpenPrice", no.Open_Price.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    if (oData.Type == TradersToolbox.Properties.Resources.LONG)
                        oData.Type = TradersToolbox.Properties.Resources.BUY;
                    else if (oData.Type == TradersToolbox.Properties.Resources.SHORT)
                        oData.Type = TradersToolbox.Properties.Resources.SELL;

                    int DiffAmount = no.Shares - oData.Shares;
                    if (DiffAmount >= 0)
                    {
                        if (oData.Type == TradersToolbox.Properties.Resources.BUY)
                        {
                            oData.Profit = (no.Open_Price - oData.Open_Price) * oData.Shares;
                            no.Shares = DiffAmount;
                        }
                        else
                        {
                            oData.Profit = (oData.Open_Price - no.Open_Price) * oData.Shares;
                            no.Shares = DiffAmount;
                        }
                        TransactionData tOd1 = new TransactionData(oData);
                        Transaction_list.Add(tOd1);
                        MainWindowViewModel.tradeDB.addTransactionRecord(tOd1);
                        MakeupRealizeSummaryRecord(tOd1);
                        if (DiffAmount == 0)
                        {
                            Position_list.Remove(no);
                            MainWindowViewModel.tradeDB.removePositionRecord(no);
                            no.Profit = 0;
                            MakeupUnRealizeSummaryRecord(no);
                        }
                    }
                    else
                    {
                        OrderData oData_temp = oData;
                        if (oData.Type == TradersToolbox.Properties.Resources.BUY)
                        {
                            oData_temp.Profit = (no.Open_Price - oData.Open_Price) * no.Shares;
                        }
                        else
                        {
                            oData_temp.Profit = (oData.Open_Price - no.Open_Price) * no.Shares;
                        }
                        TransactionData tOd2 = new TransactionData(oData_temp);
                        Transaction_list.Add(tOd2);
                        MainWindowViewModel.tradeDB.addTransactionRecord(tOd2);
                        MakeupRealizeSummaryRecord(tOd2);
                        oData.Shares = Math.Abs(DiffAmount);
                        Position_list.Remove(no);
                        if (oData.Type == TradersToolbox.Properties.Resources.BUY)
                            oData.Type = TradersToolbox.Properties.Resources.LONG;
                        else if (oData.Type == TradersToolbox.Properties.Resources.SELL)
                            oData.Type = TradersToolbox.Properties.Resources.SHORT;
                        Position_list.Add(oData);
                    }
                }
                return;
            }

            if (oData.Type == TradersToolbox.Properties.Resources.LONG)
                oData.Type = TradersToolbox.Properties.Resources.BUY;
            else if (oData.Type == TradersToolbox.Properties.Resources.SHORT)
                oData.Type = TradersToolbox.Properties.Resources.SELL;

            TransactionData tOd3 = new TransactionData(oData);
            Transaction_list.Add(tOd3);
            MainWindowViewModel.tradeDB.addTransactionRecord(tOd3);

            if (oData.Type == TradersToolbox.Properties.Resources.BUY)
            {
                oData.Profit = (oData.Close_Price - oData.Open_Price) * oData.Shares;
            }
            else if (oData.Type == TradersToolbox.Properties.Resources.SELL)
            {
                oData.Profit = (oData.Open_Price -oData.Close_Price) * oData.Shares;
            }

            if (oData.Type == TradersToolbox.Properties.Resources.BUY)
                oData.Type = TradersToolbox.Properties.Resources.LONG;
            else if (oData.Type == TradersToolbox.Properties.Resources.SELL)
                oData.Type = TradersToolbox.Properties.Resources.SHORT;
            Position_list.Add(oData);
            MainWindowViewModel.tradeDB.addPositionRecord(oData);
            MakeupUnRealizeSummaryRecord(oData);
        }

        public void MakeupUnRealizeSummaryRecord(OrderData position_data)
        {
            DateTime ps_date = position_data.Open_Time.Date;
            IEnumerable<SummaryData> day_position = Summary_list.Where(x => x.Date.Date == ps_date && x.Symbol == position_data.Symbol);
            if (day_position.Count() == 0)
            {
                SummaryData sd = new SummaryData();
                sd.Date = ps_date;
                sd.Symbol = position_data.Symbol;
                sd.Unrealized = position_data.Profit;
                sd.Realized = 0;
                sd.Total = position_data.Profit;
                Summary_list.Add(sd);
                MainWindowViewModel.tradeDB.addSummaryRecord(sd);
                return;
            }
            foreach (SummaryData od in day_position)
            {
                od.Unrealized = position_data.Profit;
                od.Total = od.Unrealized + od.Realized;
            }
        }

        public void MakeupRealizeSummaryRecord(TransactionData transaction_data)
        {
            DateTime ps_date = transaction_data.Open_Time.Date;
            IEnumerable<SummaryData> day_position = Summary_list.Where(x => x.Date.Date == ps_date && x.Symbol == transaction_data.Symbol);
            if (day_position.Count() == 0)
            {
                SummaryData sd = new SummaryData();
                sd.Date = ps_date;
                sd.Symbol = transaction_data.Symbol;
                sd.Realized = transaction_data.Profit;
                sd.Unrealized = 0;
                sd.Total = transaction_data.Profit;
                MainWindowViewModel.tradeDB.addSummaryRecord(sd);
                Summary_list.Add(sd);
                return;
            }
            foreach (SummaryData od in day_position)
            {
                od.Realized = od.Realized + transaction_data.Profit;
                od.Total = od.Unrealized + od.Realized;
                MainWindowViewModel.tradeDB.updateSummaryRecord(od, "Realized", od.Realized.ToString(CultureInfo.InvariantCulture));
            }
        }

        public void ClosePosition()
        {
            if (SelectedRow is OrderData q)
            {
                if (!MainWindowViewModel.BrokersManager.IsLoggedIn)
                {
                    var Res = DXMessageBox.Show("To close the position please sign in", "Order Close", MessageBoxButton.YesNo);
                    return;
                }

                var Result = DXMessageBox.Show("Do you want to close this position?", "Order Close", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (Result == MessageBoxResult.Yes)
                {
                    ClosePosition(q.Id);
                }
            }
        }
        
        public void ReversePosition()
        {
            if (SelectedRow is OrderData q)
            {
                if (!MainWindowViewModel.BrokersManager.IsLoggedIn)
                {
                    var Res = DXMessageBox.Show("To reverse the position please sign in", "Order Close", MessageBoxButton.YesNo);
                    return;
                }

                var Result = DXMessageBox.Show("Do you want to reverse this position?", "Order Close", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (Result == MessageBoxResult.Yes)
                {
                    string type;
                    if (q.Type == TradersToolbox.Properties.Resources.LONG)
                        type = TradersToolbox.Properties.Resources.SELL;
                    else
                        type = TradersToolbox.Properties.Resources.BUY;
                    ClosePosition(q.Id);

                    int order_id = MainWindowViewModel.tradeDB.getPositionRecordsAll().Count + MainWindowViewModel.tradeDB.getTransactionRecordsAll().Count + MainWindowViewModel.tradeDB.getOpenOrderRecordsAll().Count + 1;

                    OrderData newOrder = new OrderData()
                    {
                        Symbol = q.Symbol,
                        Shares = q.Shares,
                        Type = type,
                        Open_Time = DateTime.Now,
                        Open_Price = q.Close_Price,
                        Close_Price = q.Close_Price,
                        Profit = 0,
                        Id = order_id
                    };
                    
                    //var IData = Data.Single(x => x.Symbol == newOrder.Symbol);
                    //if (newOrder.Type == TradersToolbox.Properties.Resources.BUY)
                    //{
                    //    newOrder.Close_Price = IData.Bid.Value.ToString(CultureInfo.InvariantCulture);
                    //}
                    //else if (newOrder.Type == TradersToolbox.Properties.Resources.SELL)
                    //{
                    //    newOrder.Close_Price = IData.Ask.Value.ToString(CultureInfo.InvariantCulture);
                    //}
                    MakeupPosition(newOrder);
                }
            }
        }

        [Command]
        public void CancelOrder()
        {
            if (SelectedOpenOrderRow is OrderData q)
            {
                //if (!MainWindowViewModel.BrokersManager.IsLoggedIn)
                //{
                //    var Res = DXMessageBox.Show("To cancel the order please sign in", "Order Cancel", MessageBoxButton.YesNo);
                //    return;
                //}
                var Result = DXMessageBox.Show("Do you want to cancel this order?", "Order Cancel", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (Result == MessageBoxResult.Yes)
                {
                    CancelOrder(q.Id);
                    Messenger.Default.Send<string>(q.Symbol);
                }
            }
        }
    }
}