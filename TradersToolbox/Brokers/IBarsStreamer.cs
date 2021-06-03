using DevExpress.Mvvm;
using DevExpress.Xpf.Charts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TradersToolbox.Core;
using TradersToolbox.Data;
using TradeStationAPI.Model;

namespace TradersToolbox.Brokers
{
    public interface IBarsStreamer
    {
        string Symbol { get; set; }
        ChartIntervalItem Interval { get; set; }
        StockHistorySettings HistorySettings { get; set; }
        TimeZoneInfo TimeZone { get; set; }

        // not sorted data
        ObservableCollection<TradingData> Data { get; }

        CancellationToken cancellationToken { get; }

        void Close();

        //bool IsHistoricalDataLoadingComplete { get; }
    }

    class PolygonBarsStreamer : IBarsStreamer
    {
        public string Symbol { get; set; }
        public ChartIntervalItem Interval { get; set; }
        public StockHistorySettings HistorySettings { get; set; }
        public TimeZoneInfo TimeZone { get; set; }

        public ObservableCollection<TradingData> Data { get; }

        public CancellationToken cancellationToken => cancellationTokenSource.Token;
        public CancellationTokenSource cancellationTokenSource { get; private set; }

        //public bool IsHistoricalDataLoadingComplete { get; set; }

        public PolygonBarsStreamer()
        {
            Data = new ObservableCollection<TradingData>();
            HistorySettings = new StockHistorySettings()
            {
                Type = StockHistorySettingsType.NumberBarsBack,
                YearsBack = 1,
                BarsBack = 300,
                TimeStart = DateTime.Now.ToUniversalTime().Date.AddYears(-1)
            };
            cancellationTokenSource = new CancellationTokenSource();
            TimeZone = TimeZoneInfo.Local;

            //Messenger.Default.Register<AggregateReceivedMessage>(this, OnMessage);
            Messenger.Default.Register<QuoteReceivedMessage>(this, OnMessage);
        }

        /*void OnMessage(AggregateReceivedMessage message)
        {
            try
            {
                var agg = message.Aggregate;
                if (agg.Symbol == Symbol)
                {
                    var ts = TimeZoneInfo.ConvertTimeFromUtc(Utils.ToDateTime(agg.AggregateStartTime / 1000), TimeZone);
                    var te = TimeZoneInfo.ConvertTimeFromUtc(Utils.ToDateTime(agg.AggregateEndTime / 1000), TimeZone);

                    var td = new TradingData(TimeZoneInfo.ConvertTimeFromUtc(Utils.ToDateTime(agg.AggregateStartTime / 1000), TimeZone),
                        (double)agg.Open, (double)agg.High, (double)agg.Low, (double)agg.Close, agg.Volume);

                    Application.Current?.Dispatcher.InvokeAsync(() =>
                    {
                        Data.Add(td);
                    });
                }
            }
            catch (TaskCanceledException) { }
        }*/

        void OnMessage(QuoteReceivedMessage message)
        {
            //todo: get history data on new each session open for daily and larger timeframes

            try
            {
                if (message.Symbol == Symbol && message.Last.HasValue && message.Volume.HasValue && message.LastDateTimeUTC.HasValue)
                {
                    //var dt = GetBarsTimeLocal(message.LastDateTimeUTC.Value);
                    var mDate = TimeZoneInfo.ConvertTimeFromUtc(message.LastDateTimeUTC.Value, TimeZone);

                    Application.Current?.Dispatcher.InvokeAsync(() =>
                    {
                        if (Data.Count > 0)
                        {
                            var maxDate = Data.Last().Date; //Data.Max(x => x.Date);        //data should be ordered

                            switch (Interval.MeasureUnit)
                            {
                                case DateTimeMeasureUnit.Minute:
                                    if ((mDate - maxDate).TotalMinutes >= Interval.MeasureUnitMultiplier)
                                    {
                                        do
                                        {
                                            maxDate = maxDate.AddMinutes(Interval.MeasureUnitMultiplier);
                                        }
                                        while ((mDate - maxDate).TotalMinutes >= Interval.MeasureUnitMultiplier);

                                        //Add bar Interval.MeasureUnitMultiplier
                                        Data.Add(new TradingData(maxDate, (double)message.Last.Value, (double)message.Last.Value,
                                            (double)message.Last.Value, (double)message.Last.Value, message.Volume.Value));
                                    }
                                    else
                                    {
                                        UpdateBar(maxDate, message);
                                    }
                                    break;
                                case DateTimeMeasureUnit.Hour:
                                    if ((mDate - maxDate).TotalHours >= Interval.MeasureUnitMultiplier)
                                    {
                                        do
                                        {
                                            maxDate = maxDate.AddHours(Interval.MeasureUnitMultiplier);
                                        }
                                        while ((mDate - maxDate).TotalHours >= Interval.MeasureUnitMultiplier);

                                        //Add bar Interval.MeasureUnitMultiplier
                                        Data.Add(new TradingData(maxDate, (double)message.Last.Value, (double)message.Last.Value,
                                            (double)message.Last.Value, (double)message.Last.Value, message.Volume.Value));
                                    }
                                    else
                                    {
                                        UpdateBar(maxDate, message);
                                    }
                                    break;
                                case DateTimeMeasureUnit.Day:
                                    if (message.IsMarketOpened.HasValue && message.IsMarketOpened.Value)
                                    {
                                        if ((mDate - maxDate).TotalDays >= Interval.MeasureUnitMultiplier)
                                        {
                                            do
                                            {
                                                maxDate = maxDate.AddDays(Interval.MeasureUnitMultiplier);
                                            }
                                            while ((mDate - maxDate).TotalDays >= Interval.MeasureUnitMultiplier);

                                            if (message.IsMarketOpened.Value)
                                            {
                                                Data.Add(new TradingData(maxDate, (double)message.Last.Value, (double)message.Last.Value,
                                                    (double)message.Last.Value, (double)message.Last.Value, message.Volume.Value));
                                            }
                                        }
                                        else
                                        {
                                            UpdateBar(maxDate, message);
                                        }
                                    }
                                    break;
                                case DateTimeMeasureUnit.Week:
                                    if (message.IsMarketOpened.HasValue && message.IsMarketOpened.Value)
                                    {
                                        if ((mDate - maxDate).TotalDays >= 7 * Interval.MeasureUnitMultiplier)
                                        {
                                            do
                                            {
                                                maxDate = maxDate.AddDays(7 * Interval.MeasureUnitMultiplier);
                                            }
                                            while ((mDate - maxDate).TotalDays >= 7 * Interval.MeasureUnitMultiplier);

                                            //add + 7
                                            Data.Add(new TradingData(maxDate, (double)message.Last.Value, (double)message.Last.Value,
                                               (double)message.Last.Value, (double)message.Last.Value, message.Volume.Value));
                                        }
                                        else
                                        {
                                            UpdateBar(maxDate, message);
                                        }
                                    }
                                    break;
                                case DateTimeMeasureUnit.Month:
                                    if (message.IsMarketOpened.HasValue && message.IsMarketOpened.Value)
                                    {
                                        if (mDate.Month > maxDate.Month || (mDate.Month == 1 && maxDate.Month == 12))
                                        {
                                            //add + 1
                                            Data.Add(new TradingData(maxDate.AddMonths(1), (double)message.Last.Value, (double)message.Last.Value,
                                               (double)message.Last.Value, (double)message.Last.Value, message.Volume.Value));
                                        }
                                        else
                                        {

                                            UpdateBar(maxDate, message);
                                        }
                                    }
                                    break;
                            }
                        }



                        /*if (Data.FirstOrDefault(x=>x.Date==dt) is TradingData bar)
                        {
                            bar.Close = (double)message.Last.Value;
                            bar.High = Math.Max(bar.High, bar.Close);
                            bar.Low = Math.Min(bar.Low, bar.Close);
                            bar.Volume += message.Volume.Value;

                            int ind = Data.IndexOf(bar);
                            Data[ind] = bar;    //force update
                        }
                        else
                        {
                            var ESTtime = TimeZoneInfo.ConvertTime(dt, TimeZone, BrokersManager.EasternZone);
                            if ((Interval.MeasureUnit == DateTimeMeasureUnit.Day || Interval.MeasureUnit == DateTimeMeasureUnit.Week || Interval.MeasureUnit == DateTimeMeasureUnit.Month) &&
                                (ESTtime.TimeOfDay < new TimeSpan(9,30,0)|| ESTtime.TimeOfDay >= new TimeSpan(16, 0, 0)))
                                return;
                            else
                            {
                                var td = new TradingData(dt, (double)message.Last.Value, (double)message.Last.Value, (double)message.Last.Value, (double)message.Last.Value, message.Volume.Value);
                                Data.Add(td);
                            }
                        }*/
                    });
                }
            }
            catch (TaskCanceledException) { }
        }

        private void UpdateBar(DateTime maxDate, QuoteReceivedMessage message)
        {
            if (Data.FirstOrDefault(x => x.Date == maxDate) is TradingData bar)
            {
                bar.Close = (double)message.Last.Value;
                bar.High = Math.Max(bar.High, bar.Close);

                //todo: check trade contition on big change of low value

                bar.Low = Math.Min(bar.Low, bar.Close);
                bar.Volume += message.Volume.Value;

                var index = Data.IndexOf(bar);
                Data[index] = bar;
            }
        }

        /// <summary>
        /// Get bar starting time by tick time
        /// </summary>
        /*DateTime GetBarsTimeLocal(DateTime dtUTC)
        {
            var ldt = TimeZoneInfo.ConvertTimeFromUtc(dtUTC, TimeZone);

            switch (Interval.MeasureUnit)
            {
                case DateTimeMeasureUnit.Minute:
                    {
                        int totalMinutes = (int)Math.Floor((ldt - ldt.Date).TotalMinutes);
                        totalMinutes /= Interval.MeasureUnitMultiplier;
                        totalMinutes *= Interval.MeasureUnitMultiplier;
                        return ldt.Date.AddMinutes(totalMinutes);
                    }
                case DateTimeMeasureUnit.Hour:
                    {
                        int totalHours = (int)Math.Floor((ldt - ldt.Date).TotalHours);
                        totalHours /= Interval.MeasureUnitMultiplier;
                        totalHours *= Interval.MeasureUnitMultiplier;
                        return ldt.Date.AddHours(totalHours);
                    }
                default:
                case DateTimeMeasureUnit.Day:
                    {
                        return ldt.Date;
                    }
                case DateTimeMeasureUnit.Week:
                    {
                        while (ldt.DayOfWeek != DayOfWeek.Monday)
                            ldt.AddDays(-1);
                        return ldt.Date;
                    }
                case DateTimeMeasureUnit.Month:
                    {
                        return ldt.AddDays(-ldt.Day + 1).Date;
                    }
            }
        }*/

        public void Close()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = new CancellationTokenSource();
        }

        ~PolygonBarsStreamer()
        {
            cancellationTokenSource?.Cancel();
        }
    }

    class TSBarsStreamer : IBarsStreamer
    {
        public string Symbol { get; set; }
        public ChartIntervalItem Interval { get; set; }
        public StockHistorySettings HistorySettings { get; set; }
        public TimeZoneInfo TimeZone { get; set; }

        // not sorted data
        public ObservableCollection<TradingData> Data { get; }

        public CancellationToken cancellationToken => cancellationTokenSource.Token;
        public CancellationTokenSource cancellationTokenSource { get; private set; }

        // For streaming bars
        //public bool IsHistoricalDataLoadingComplete => requestsSent > 1 && (requestsSent - responseReceived < 1);   // 1 = realtime data
        //public int requestsSent;
        //public int responseReceived;

        // For get range
        public bool IsEndOfStream;

        /// <summary>
        /// 0 - do not reconnect
        /// 1 - GetBarsStreamer
        /// 2 - GetBarsRange
        /// </summary>
        public int ReconnectFunc { get; private set; }

        public DateTime RangeStartDT, RangeStopDT;

        public TSBarsStreamer(int reconnectFunc)
        {
            Data = new ObservableCollection<TradingData>();
            HistorySettings = new StockHistorySettings()
            {
                Type = StockHistorySettingsType.NumberBarsBack,
                YearsBack = 1,
                BarsBack = 300,
                TimeStart = DateTime.Now.ToUniversalTime().Date.AddYears(-1)
            };
            cancellationTokenSource = new CancellationTokenSource();
            TimeZone = TimeZoneInfo.Local;
            ReconnectFunc = reconnectFunc;
        }

        public void AddBar(DateTime dt, BarchartDefinition bar)
        {
            var t = new TradingData(dt, decimal.ToDouble(bar.Open.Value), decimal.ToDouble(bar.High.Value), decimal.ToDouble(bar.Low.Value),
                decimal.ToDouble(bar.Close.Value), decimal.ToDouble(bar.TotalVolume.Value));

            if (Data.Count == 0 || Data.Last().Date <= dt)
                Data.Add(t);
            else
            {   // sorted
                int i = 0;
                for (; i < Data.Count; ++i)
                    if (Data[i].Date > dt)
                        break;
                
                Data.Insert(i, t);
            }
        }

        public void UpdateBar(DateTime maxDate, BarchartDefinition bar)
        {
            TradingData pt = Data.FirstOrDefault(x => x.Date == maxDate);

            if (pt != default)
            {
                var index = Data.IndexOf(pt);
                Data[index] = new TradingData(pt.Date, decimal.ToDouble(bar.Open.Value), decimal.ToDouble(bar.High.Value), decimal.ToDouble(bar.Low.Value),
                        decimal.ToDouble(bar.Close.Value), decimal.ToDouble(bar.TotalVolume.Value));
            }
        }

        public void Close()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = new CancellationTokenSource();
        }

        ~TSBarsStreamer()
        {
            cancellationTokenSource?.Cancel();
        }
    }

    public class StockHistorySettings
    {
        public StockHistorySettingsType Type { get; set; }
        public DateTime TimeStart { get; set; }
        public uint BarsBack { get; set; }
        public uint YearsBack { get; set; }


        public uint TotalBarchartCount { get; set; }
        public uint ChunkCount { get; set; }

        private uint ChunkSize = 50;

        public void SetChunkCount(ChartIntervalItem Interval)
        {
            TotalBarchartCount = GetBarchartsCount(Interval);

            ChunkCount = TotalBarchartCount / ChunkSize;
            if (TotalBarchartCount % ChunkSize != 0)
            {
                ChunkCount++;
            }
        }

        public StockHistorySettings GetCurrentChunk(DateTime startDate, int currentChunk, ChartIntervalItem interval)
        {
            var setting = new StockHistorySettings();

            //TODO fix chunk size in case of end/final chunks
            switch (interval.MeasureUnit)
            {
                case DateTimeMeasureUnit.Minute:
                    ChunkSize = 57600;
                    break;
                case DateTimeMeasureUnit.Hour:
                    ChunkSize = 57600;
                    break;
                case DateTimeMeasureUnit.Day:
                    ChunkSize = 300;
                    break;
                case DateTimeMeasureUnit.Week:
                    ChunkSize = 100;
                    break;
                case DateTimeMeasureUnit.Month:
                    ChunkSize = 10;
                    break;
            }


            setting.Type = StockHistorySettingsType.NumberBarsBack;

            setting.TimeStart = startDate;
            for (int i = 2; i <= currentChunk; i++)
                setting.TimeStart = GetMinusTimeStartFromChunkSize(setting.TimeStart, interval, ChunkSize);


            if (currentChunk == ChunkCount)
            {
                if (TotalBarchartCount % ChunkSize != 0)
                {
                    setting.BarsBack = TotalBarchartCount % ChunkSize;
                }
                else
                {
                    setting.BarsBack = ChunkSize;
                }
            }
            else
            {
                setting.BarsBack = ChunkSize;
            }

            return setting;
        }



        public static DateTime GetMinusTimeStartFromChunkSize(DateTime timeStart, ChartIntervalItem interval, uint size)
        {
            switch (interval.MeasureUnit)
            {
                case DateTimeMeasureUnit.Minute:
                    return timeStart.AddMinutes(-(interval.MeasureUnitMultiplier * size));
                case DateTimeMeasureUnit.Hour:
                    return timeStart.AddHours(-(interval.MeasureUnitMultiplier * size));
                case DateTimeMeasureUnit.Day:
                    return timeStart.AddDays(-(interval.MeasureUnitMultiplier * size));
                case DateTimeMeasureUnit.Week:
                    return timeStart.AddDays(-(interval.MeasureUnitMultiplier * size * 7));
                case DateTimeMeasureUnit.Month:
                    return timeStart.AddMonths(-(int)(interval.MeasureUnitMultiplier * size));
                default:
                    return DateTime.Now;
            }
        }

        private uint GetBarchartsCount(ChartIntervalItem Interval)
        {
            switch (Type)
            {
                case StockHistorySettingsType.NumberBarsBack:
                    return BarsBack;
                case StockHistorySettingsType.NumberYearsBack:
                    switch (Interval.MeasureUnit)
                    {
                        case DateTimeMeasureUnit.Minute:
                            return (uint)((DateTime.Now - DateTime.Now.AddYears(-(int)YearsBack)).TotalMinutes / Interval.MeasureUnitMultiplier);
                        case DateTimeMeasureUnit.Hour:
                            return (uint)((DateTime.Now - DateTime.Now.AddYears(-(int)YearsBack)).TotalHours / Interval.MeasureUnitMultiplier);
                        case DateTimeMeasureUnit.Day:
                            return (uint)((DateTime.Now - DateTime.Now.AddYears(-(int)YearsBack)).TotalDays / Interval.MeasureUnitMultiplier);
                        case DateTimeMeasureUnit.Week:
                            return (uint)((DateTime.Now - DateTime.Now.AddYears(-(int)YearsBack)).TotalDays / 7 / Interval.MeasureUnitMultiplier);
                        case DateTimeMeasureUnit.Month:
                            return (uint)((DateTime.Now - DateTime.Now.AddYears(-(int)YearsBack)).TotalDays / 28 / Interval.MeasureUnitMultiplier);
                        default:
                            return 0;
                    }
                case StockHistorySettingsType.FirstData:
                    switch (Interval.MeasureUnit)
                    {
                        //TODO fix timezone
                        case DateTimeMeasureUnit.Minute:
                            return (uint)((DateTime.Now - TimeStart).TotalMinutes / Interval.MeasureUnitMultiplier);
                        case DateTimeMeasureUnit.Hour:
                            return (uint)((DateTime.Now - TimeStart).TotalHours / Interval.MeasureUnitMultiplier);
                        case DateTimeMeasureUnit.Day:
                            return (uint)((DateTime.Now - TimeStart).TotalDays / Interval.MeasureUnitMultiplier);
                        case DateTimeMeasureUnit.Week:
                            return (uint)((DateTime.Now - TimeStart).TotalDays / 7 / Interval.MeasureUnitMultiplier);
                        case DateTimeMeasureUnit.Month:
                            return (uint)((DateTime.Now - TimeStart).TotalDays / 28 / Interval.MeasureUnitMultiplier);
                        default:
                            return 0;
                    }
                default:
                    return 0;
            }
        }
    }

    public enum StockHistorySettingsType
    {
        FirstData, NumberBarsBack, NumberYearsBack
    }
}
