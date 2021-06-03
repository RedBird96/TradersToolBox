using TradersToolbox.Data;
using DevExpress.Xpf.Charts;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Net;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using TradersToolbox.ViewModels;
using TradersToolbox.Core;

namespace TradersToolbox.DataSources
{
#if sfsfsdfsddfsdf // To Delete
    public class StockDataSource
    {
        CancellationTokenSource cancellationTokenSource;

        public string Symbol;
        public ChartIntervalItem Interval;

        readonly Dispatcher _dispatcher;

        public ObservableCollection<TradingData> Data { get; set; }

        TradeStationAPI.Api.MarketdataApi Api => MainWindowViewModel.marketdataApi;

        private TimeZoneInfo TimeZone;
       
        public StockHistorySettings HistorySettings { get; set; }

        public StockDataSource()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            Data = new ObservableCollection<TradingData>();
            TimeZone = TimeZoneInfo.Local;
            HistorySettings = new StockHistorySettings() {
                Type = StockHistorySettingsType.NumberBarsBack,
                YearsBack = 1,
                BarsBack = 300,
                TimeStart = DateTime.Now.ToUniversalTime().Date.AddYears(-1)
            };
        }

        public void UpdateSymbol(string symbol, ChartIntervalItem interval/*, string printSymbol, string description*/)
        {
            Data.Clear();

            Symbol = symbol;
            Interval = interval;

            _ = RequestDataAsync();     // do not await async call
        }

        private async Task RequestDataAsync()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = new CancellationTokenSource();
            for (int i = 0; i < 10; ++i)
            {
                HttpStatusCode statusCode = HttpStatusCode.Unauthorized;
                var startTime = DateTime.Now.ToUniversalTime();
                if (!string.IsNullOrEmpty(MainWindowViewModel.TSAccessToken))
                {
                    try
                    {
                        string startDate = "01-01-2015";
                        
                        HistorySettings.SetChunkCount(Interval);
                        //
                        //startDate = startTime.ToString(@"MM-dd-yyyy\thh:mm:ss");

                        int intervalVal = 1;
                        string unit = "Daily";
                        switch (Interval.MeasureUnit)
                        {
                            case DateTimeMeasureUnit.Minute:
                                intervalVal = Interval.MeasureUnitMultiplier;
                                unit = "Minute";
                                startDate = startTime.AddMinutes(-10).ToString(@"MM-dd-yyyy\thh:mm:ss");
                                break;
                            case DateTimeMeasureUnit.Hour:
                                intervalVal = 60 * Interval.MeasureUnitMultiplier;
                                unit = "Minute";
                                startDate = startTime.AddMinutes(-600).ToString(@"MM-dd-yyyy\thh:mm:ss");
                                break;
                            case DateTimeMeasureUnit.Day:
                                unit = "Daily";
                                startDate = startTime.AddDays(-1).ToString(@"MM-dd-yyyy\thh:mm:ss");
                                break;
                            case DateTimeMeasureUnit.Week:
                                unit = "Weekly";
                                startDate = startTime.AddDays(-7).ToString(@"MM-dd-yyyy\thh:mm:ss");
                                break;
                            case DateTimeMeasureUnit.Month:
                                unit = "Monthly";
                                startDate = startTime.AddMonths(-1).ToString(@"MM-dd-yyyy\thh:mm:ss");
                                break;
                        }
                        statusCode = await Api.StreamBarchartsFromStartDateAsync(MainWindowViewModel.TSAccessToken,
                            Symbol, intervalVal, unit, startDate, OnDataReceived, null, cancellationTokenSource.Token, "USEQPreAndPost");
                    }
                    catch
                    {
                        //ShowMessage(ex.Message);
                        continue;
                    }
                }

                switch (statusCode)
                {
                    case HttpStatusCode.OK:
                        // do nothing on successful connection
                        _ = Task.Run(async () =>
                        {
                            int intervalVal = 1;
                            string unit = "Daily";
                            switch (Interval.MeasureUnit)
                            {
                                case DateTimeMeasureUnit.Minute:
                                    intervalVal = Interval.MeasureUnitMultiplier;
                                    unit = "Minute";
                                    break;
                                case DateTimeMeasureUnit.Hour:
                                    intervalVal = 60 * Interval.MeasureUnitMultiplier;
                                    unit = "Minute";
                                    break;
                                case DateTimeMeasureUnit.Day:
                                    unit = "Daily";
                                    break;
                                case DateTimeMeasureUnit.Week:
                                    unit = "Weekly";
                                    break;
                                case DateTimeMeasureUnit.Month:
                                    unit = "Monthly";
                                    break;
                            }

                            if (HistorySettings.Type == StockHistorySettingsType.FirstData)
                            {
                                var code = await Api.StreamBarchartsFromStartDateToEndDateAsync(MainWindowViewModel.TSAccessToken,
                                   Symbol, intervalVal, unit, HistorySettings.TimeStart.ToString(@"MM-dd-yyyy\thh:mm:ss"), startTime.ToString(@"MM-dd-yyyy\thh:mm:ss"),
                                   OnDataReceived, null, cancellationTokenSource.Token, "USEQPreAndPost");

                            }
                            else if (HistorySettings.Type == StockHistorySettingsType.NumberYearsBack)
                            {
                                var code = await Api.StreamBarchartsFromStartDateToEndDateAsync(MainWindowViewModel.TSAccessToken,
                                   Symbol, intervalVal, unit, startTime.AddYears(-(int)HistorySettings.YearsBack).ToString(@"MM-dd-yyyy\thh:mm:ss"),
                                   startTime.ToString(@"MM-dd-yyyy\thh:mm:ss"), OnDataReceived, null, cancellationTokenSource.Token, "USEQPreAndPost");
                            }
                            else
                            {
                                for (int j = 1; j <= HistorySettings.ChunkCount; j++)
                                {
                                    var currentHistory = HistorySettings.GetCurrentChunk(startTime, j, Interval);

                                    var lastDate = currentHistory.TimeStart.ToString(@"MM-dd-yyyy\thh:mm:ss");

                                    var code = await Api.StreamBarchartsBarsBackAsync(MainWindowViewModel.TSAccessToken,
                                        Symbol, intervalVal, unit, (int?)currentHistory.BarsBack, lastDate, OnDataReceived,
                                        null, cancellationTokenSource.Token, "USEQPreAndPost");
                                }
                            }
                        });
                        return;
                    case HttpStatusCode.NotFound:
                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        // try to re-authorize
                        await Task.Delay(5000);
                        _ = MainWindowViewModel.main_form.TS_login(true);
                        break;
                    default:
                        // print error message. F5 to reconnect.
                        await Task.Delay(5000);
                        //ShowMessage(statusCode.ToString());
                        break;
                }
            }

        }

        private void OnDataReceived(string symbol, BarchartDefinition bar, object targetList, string unit, int interval, Exception ex)
        {
            if (cancellationTokenSource?.IsCancellationRequested == true)
                return;
            else if (string.IsNullOrWhiteSpace(MainWindowViewModel.TSAccessToken))
            {
                cancellationTokenSource.Cancel();
            }


            if (ex != null)
            {
                //TODO chunk size to 1
                if (ex.Message != "END of stream message received" &&
                   ex.Message != "ERROR message received (ERROR - No data available.)" &&
                   ex.Message != "ERROR message received (ERROR - Value was either too large or too small for a UInt32.)")
                    _ = RequestDataAsync();     // do not await async call
                return;
            }

            _dispatcher.InvokeAsync(() =>
            {
                if (bar != null && !string.IsNullOrEmpty(bar.TimeStamp) && bar.Open != default && bar.High != default && bar.Low != default && bar.Close != default && bar.TotalVolume != default)
                {
                    var match = Regex.Match(bar.TimeStamp, @"\d\d*");
                    if (match.Success)
                    {
                        var unixDt = long.Parse(match.Value) / 1000;

                        DateTime dt = TimeZoneInfo.ConvertTimeFromUtc(Utils.ToDateTime(unixDt).ToUniversalTime(), TimeZone);

                        BarchartStatusFlags statusFlags = (BarchartStatusFlags)bar.Status;
                        //Debug.WriteLine(dt.ToShortDateString() + "-" + dt.ToShortTimeString() + " Status:  " + statusFlags.ToString());
                        if ((statusFlags == BarchartStatusFlags.REAL_TIME_DATA || statusFlags == BarchartStatusFlags.HISTORICAL_DATA) && Data.Count > 0)
                        {
                            var mDate = dt;

                            var maxDate = Data.Max(x => x.Date);

                            switch (Interval.MeasureUnit)
                            {
                                case DateTimeMeasureUnit.Minute:
                                    if (mDate.Minute >= (maxDate.Minute + Interval.MeasureUnitMultiplier) || (mDate.Minute == 0 && maxDate.Minute > 1))
                                    {
                                        //Add bar Interval.MeasureUnitMultiplier
                                        Data.Add(new TradingData(maxDate.AddMinutes(Interval.MeasureUnitMultiplier), decimal.ToDouble(bar.Open.Value), decimal.ToDouble(bar.High.Value), decimal.ToDouble(bar.Low.Value),
                                            decimal.ToDouble(bar.Close.Value), decimal.ToDouble(bar.TotalVolume.Value)));
                                    }
                                    else
                                    {
                                        UpdateBar(maxDate, bar);
                                        //Update bar
                                    }
                                    break;
                                case DateTimeMeasureUnit.Hour:
                                    if (mDate.Hour >= (maxDate.Hour + Interval.MeasureUnitMultiplier) || (mDate.Hour == 0 && maxDate.Hour > 1))
                                    {
                                        //Add bar Interval.MeasureUnitMultiplier
                                        Data.Add(new TradingData(maxDate.AddHours(Interval.MeasureUnitMultiplier), decimal.ToDouble(bar.Open.Value), decimal.ToDouble(bar.High.Value), decimal.ToDouble(bar.Low.Value),
                                            decimal.ToDouble(bar.Close.Value), decimal.ToDouble(bar.TotalVolume.Value)));
                                    }
                                    else
                                    {
                                        UpdateBar(maxDate, bar);
                                        //Update bar
                                    }
                                    break;
                                case DateTimeMeasureUnit.Day:
                                    if (mDate.Day > maxDate.Day || (mDate.Day == 1 && maxDate.Day > 1))
                                    {
                                        //Add bar
                                        Data.Add(new TradingData(maxDate.AddDays(1), decimal.ToDouble(bar.Open.Value), decimal.ToDouble(bar.High.Value), decimal.ToDouble(bar.Low.Value),
                                            decimal.ToDouble(bar.Close.Value), decimal.ToDouble(bar.TotalVolume.Value)));
                                    }
                                    else
                                    {
                                        UpdateBar(maxDate, bar);
                                        //Update bar

                                    }
                                    break;
                                case DateTimeMeasureUnit.Week:
                                    if (mDate.Day > maxDate.Day || (mDate.Month == 1 && maxDate.Month == 12))
                                    {
                                        //add + 7
                                        Data.Add(new TradingData(maxDate.AddDays(7), decimal.ToDouble(bar.Open.Value), decimal.ToDouble(bar.High.Value), decimal.ToDouble(bar.Low.Value),
                                            decimal.ToDouble(bar.Close.Value), decimal.ToDouble(bar.TotalVolume.Value)));
                                    }
                                    else
                                    {
                                        UpdateBar(maxDate, bar);
                                        //update
                                    }
                                    break;
                                case DateTimeMeasureUnit.Month:
                                    if (mDate.Month > maxDate.Month || (mDate.Month == 1 && maxDate.Month == 12))
                                    {
                                        //add + 1
                                        Data.Add(new TradingData(maxDate.AddMonths(1), decimal.ToDouble(bar.Open.Value), decimal.ToDouble(bar.High.Value), decimal.ToDouble(bar.Low.Value),
                                            decimal.ToDouble(bar.Close.Value), decimal.ToDouble(bar.TotalVolume.Value)));
                                    }
                                    else
                                    {
                                        //update
                                        UpdateBar(maxDate, bar);
                                    }
                                    break;
                            }
                        }
                        else if (bar.Status.ToString().Contains("536870") || (statusFlags == (BarchartStatusFlags.HISTORICAL_DATA | BarchartStatusFlags.EXTENDED_BAR)) || statusFlags == BarchartStatusFlags.END_OF_HISTORY_STREAM || (statusFlags == (BarchartStatusFlags.NEW | BarchartStatusFlags.HISTORICAL_DATA | BarchartStatusFlags.STANDARD_CLOSE)) || (statusFlags == (BarchartStatusFlags.UPDATE_CORPACTION | BarchartStatusFlags.EXTENDED_BAR)))
                        {

                            //todo: it's possible to improve performance by mixing FirstOrDefault() and IndexOf() calls

                            TradingData pt = Data.FirstOrDefault(x => x.Date == dt);

                            if (pt != default)
                            {
                                var index = Data.IndexOf(pt);
                                Data[index] = new TradingData(pt.Date, decimal.ToDouble(bar.Open.Value), decimal.ToDouble(bar.High.Value), decimal.ToDouble(bar.Low.Value),
                                        decimal.ToDouble(bar.Close.Value), decimal.ToDouble(bar.TotalVolume.Value));
                            }
                            else
                            {
                                Data.Add(new TradingData(dt, decimal.ToDouble(bar.Open.Value), decimal.ToDouble(bar.High.Value), decimal.ToDouble(bar.Low.Value),
                                        decimal.ToDouble(bar.Close.Value), decimal.ToDouble(bar.TotalVolume.Value)));
                            }
                        }
                        else
                        {

                        }
                    }
                }
            });
        }

        private void UpdateBar(DateTime maxDate, BarchartDefinition bar)
        {
            TradingData pt = Data.FirstOrDefault(x => x.Date == maxDate);

            if (pt != default)
            {
                var index = Data.IndexOf(pt);
                Data[index] = new TradingData(pt.Date, decimal.ToDouble(bar.Open.Value), decimal.ToDouble(bar.High.Value), decimal.ToDouble(bar.Low.Value),
                        decimal.ToDouble(bar.Close.Value), decimal.ToDouble(bar.TotalVolume.Value));
            }
        }

        internal void Close()
        {
            cancellationTokenSource?.Cancel();
        }

        internal TimeZoneInfo GetCurrentTimeZone()
        {
            return TimeZone;
        }

        internal void SetCurrentTimeZone(TimeZoneInfo curentTimeZone)
        {
            TimeZone = curentTimeZone;
        }
    }

    //https://tradestation.github.io/webapi-docs-v2/en/objects/intraday-bar-data/status/
    [Flags]
    enum BarchartStatusFlags
    {
        NEW = 0,
        REAL_TIME_DATA = 1,
        HISTORICAL_DATA = 2,
        STANDARD_CLOSE = 3,
        END_OF_SESSION_CLOSE = 4,
        UPDATE_CORPACTION = 5,
        UPDATE_CORRECTION = 6,
        ANALYSIS_BAR = 7,
        EXTENDED_BAR = 8,
        PREV_DAY_CORRECTION = 19,
        AFTER_MARKET_CORRECTION = 23,
        PHANTOM_BAR = 24,
        EMPTY_BAR = 25,
        BACKFILL_DATA = 26,
        ARCHIVE_DATA = 27,
        GHOST_BAR = 28,
        END_OF_HISTORY_STREAM = 29,
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

      

        static DateTime GetMinusTimeStartFromChunkSize(DateTime timeStart,ChartIntervalItem interval, uint size)
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
#endif
}
