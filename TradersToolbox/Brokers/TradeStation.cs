using DevExpress.Mvvm;
using DevExpress.Xpf.Charts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TradersToolbox.Core;
using TradersToolbox.Data;
using TradersToolbox.DataObjects;
using TradersToolbox.Views;
using TradeStationAPI.Model;

namespace TradersToolbox.Brokers
{
    public class TradeStation : IBrokerService, INotifyPropertyChanged
    {
        HttpClient httpClient;
        bool isLoggedIn;
        private bool loggingIn; // is logging in right now

        public static TradeStationAPI.Api.MarketdataApi MarketdataApi { get; private set; }

        public string AccessToken;

        public bool IsLoggedIn
        {
            get => isLoggedIn;
            private set
            {
                if (value != isLoggedIn)
                {
                    isLoggedIn = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string UserId { get; private set; }

        public string BrokerName => "Trade Station";
        public string BrokerKey => "TS";

        public TradeStation()
        {
            httpClient = HttpClientHelper.GetHttpClient();

            //API
            MarketdataApi = new TradeStationAPI.Api.MarketdataApi(new TradeStationAPI.Client.Configuration());
            MarketdataApi.Configuration.ApiClient.ConfigureWebRequest((httpWebRequest) =>
            {
                httpWebRequest.AutomaticDecompression = DecompressionMethods.None;  // do not use compression
            });
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


        public async Task<bool> LogIn()
        {
            if(!string.IsNullOrEmpty(AccessToken))
            {
                // test access token
                try
                {
                    var resp = await MarketdataApi.GetSymbolAsync(AccessToken, "MSFT");
                    IsLoggedIn = true;
                    return true;
                }
                catch(TradeStationAPI.Client.ApiException ex)
                {
                    IsLoggedIn = false;
                    AccessToken = null;
                }
            }

            if (!IsLoggedIn && !loggingIn)
            {
                loggingIn = true;
                Logger.Current.Info("Requesting TradeStation access token using refresh token...");

                // log in with refresh token
                try
                {
                    if (!string.IsNullOrEmpty(Properties.Settings.Default.TradeStationRefreshToken))
                    {
                        var t1 = Encryption.AESThenHMAC.SimpleDecryptWithPasswordAsync(Properties.Settings.Default.TradeStationRefreshToken, Security.HardwareID);
                        var t2 = Encryption.AESThenHMAC.SimpleDecryptWithPasswordAsync(TradeStationLogInWindow.TSClientId_code, TradeStationLogInWindow.LocalAppName);
                        var t3 = Encryption.AESThenHMAC.SimpleDecryptWithPasswordAsync(TradeStationLogInWindow.TSClientSecret_code, TradeStationLogInWindow.LocalAppName);

                        string refreshToken = await t1;
                        string TSClientId = await t2;
                        string TSClientSecret = await t3;

                        Logger.Current.Info("Sending a request to TradeStation server...");

                        HttpContent x = new StringContent("grant_type=refresh_token&" +
                            $"client_id={TSClientId}&redirect_uri={TradeStationLogInWindow.RedirectUrl}&client_secret={TSClientSecret}&refresh_token={refreshToken}&response_type=token",
                            Encoding.UTF8, "application/x-www-form-urlencoded");

                        var response = await httpClient.PostAsync(TradeStationLogInWindow.TSAuthorizationURL2, x);
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            Logger.Current.Info("Loading data...");

                            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(TradeStationAPI.Model.UserCredentials));
                            if (serializer.ReadObject(await response.Content.ReadAsStreamAsync()) is TradeStationAPI.Model.UserCredentials user)
                            {
                                Logger.Current.Info("Access token received");

                                // successful
                                AccessToken = user.access_token;
                                UserId = user.userid;
                                IsLoggedIn = true;
                                return true;
                            }
                            else
                                throw new Exception("Authentication data is not found");
                        }
                        else
                        {
                            throw new Exception(await response.Content.ReadAsStringAsync());
                        }
                    }
                    else
                        Logger.Current.Info("TradeStation refresh token is empty");
                }
                catch (Exception ex)
                {
                    Logger.Current.Info("Gen't get access token. {0}", ex.Message);
                }
                finally
                {
                    loggingIn = false;
                }
            }
            return false;
        }

        public bool LogInDialog()
        {
            if (IsLoggedIn)
                return true;

            TradeStationLogInWindow form = new TradeStationLogInWindow();
            form.ShowDialog();

            if (!string.IsNullOrEmpty(form.AccessToken))
            {
                // loggen in
                AccessToken = form.AccessToken;
                UserId = form.UserId;
                Properties.Settings.Default.TradeStationRefreshToken = Encryption.AESThenHMAC.SimpleEncryptWithPassword(form.RefreshToken, Security.HardwareID);
                IsLoggedIn = true;
            }
            else
            {
                // logging unsuccessful
                AccessToken = null;
                UserId = "Offline";
                Properties.Settings.Default.TradeStationRefreshToken = string.Empty;
                IsLoggedIn = false;
            }
            return IsLoggedIn;
        }

        public async Task LogOut()
        {
            if (!IsLoggedIn)
                return;

            httpClient.CancelPendingRequests();

            AccessToken = null;
            UserId = "Offline";
            Properties.Settings.Default.TradeStationRefreshToken = string.Empty;
            IsLoggedIn = false;
        }

        public async Task ProxySettingsChanged()
        {
            await LogOut();
            httpClient.CancelPendingRequests();
            httpClient = HttpClientHelper.GetHttpClient();
        }

        #region Quotes
        CancellationTokenSource StreamQuotesCancellationTokenSource;

        public List<string> StreamQuotesTickersList { get; } = new List<string>();

        public async Task<bool> StreamQuotes(List<string> tickers, bool isResetTickers)
        {
            // stop previous stream
            StreamQuotesCancellationTokenSource?.Cancel();
            StreamQuotesCancellationTokenSource = new CancellationTokenSource();

            if (isResetTickers)
            {
                StreamQuotesTickersList.Clear();
                StreamQuotesTickersList.AddRange(tickers);
            }
            else
            {
                var list = StreamQuotesTickersList.Union(tickers).ToList();
                StreamQuotesTickersList.Clear();
                StreamQuotesTickersList.AddRange(list);
            }

            // create new stream
            for (int i = 0; i < 10; ++i)
            {
                HttpStatusCode statusCode = HttpStatusCode.Unauthorized;

                if (IsLoggedIn && !string.IsNullOrEmpty(AccessToken))
                {
                    try
                    {
                        statusCode = await MarketdataApi.StreamQuotesChangesAsync(AccessToken,
                            string.Join(",", StreamQuotesTickersList),
                            OnQuoteReceived,
                            StreamQuotesCancellationTokenSource.Token);
                    }
                    catch (Exception ex)
                    {
                        //ShowMessage(ex.Message);
                        continue;
                    }
                }

                switch (statusCode)
                {
                    case HttpStatusCode.OK:
                        // do nothing on successful connection
                        return true;
                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        // try to re-authorize
                        await Task.Delay(5000);
                        await LogIn();
                        break;
                    default:
                        // print error message. F5 to reconnect.
                        await Task.Delay(5000);
                        //ShowMessage(statusCode.ToString());
                        break;
                }
            }
            return false;
        }

        public void OnQuoteReceived(QuoteDefinitionInner quote, Exception ex)
        {
            if (StreamQuotesCancellationTokenSource?.IsCancellationRequested == true)
                return;
            else if (!IsLoggedIn || string.IsNullOrWhiteSpace(AccessToken))
            {
                StopStreamQuotes();
            }


            if (ex != null)
            {
                //Program.logger.Warn(ex.Message);  //todo: Thread-safe logger
                //ShowMessage(ex.Message);       // force stream closing
                _ = StreamQuotes(StreamQuotesTickersList, true);      // try to reconnect (non-awaitable async call)
                return;
            }
            if (!string.IsNullOrEmpty(quote.Error))
            {
                //Program.logger.Warn("Stream Quote Changes warn. {0} - {1}", quote.Symbol, quote.Error);   //todo: Thread-safe logger
                //ShowMessage($"{quote.Symbol} - {quote.Error}");
                return;
            }

            Messenger.Default.Send(new QuoteReceivedMessage(quote.Symbol, ask: quote.Ask, bid: quote.Bid, last: quote.Last, volume: (int?)quote.Volume, prevClose: quote.PreviousClose));
            Messenger.Default.Send(new QuoteInfoMessage(quote.Symbol, quote.Description));
        }

        public void StopStreamQuotes(List<string> tickers = null)
        {
            StreamQuotesCancellationTokenSource?.Cancel();

            if (tickers == null)
            {
                // full stop
                StreamQuotesTickersList.Clear();
            }
            else
            {
                // partial stop
                _ = StreamQuotes(StreamQuotesTickersList.Except(tickers).ToList(), true);
            }
        }
        #endregion

        public async Task<List<TickerData>> SuggestSymbols(string text, int maxCount)
        {
            var result = await MarketdataApi.SuggestsymbolsAsync(maxCount, "", AccessToken, text);

            if (result is List<SymbolSuggestDefinitionInner> list)
            {
                return list.Select(x => new TickerData(x.Name, x.Exchange, x.Description)).ToList();
            }
            return new List<TickerData>();  //empty
        }


        #region Bars
        public async Task<IBarsStreamer> GetBarsStreamer(string symbol = null, ChartIntervalItem interval = null,
            IBarsStreamer barsStreamer = null, bool connect = true)
        {
            if (barsStreamer == null)   // create new
            {
                barsStreamer = new TSBarsStreamer(1);
            }
            else      // update
            {
                barsStreamer.Close();       //todo: reconnect without clear()
                barsStreamer.Data.Clear();
            }
            if (symbol != null)
                barsStreamer.Symbol = symbol;
            if (interval != null)
                barsStreamer.Interval = interval;

            //var tsBarsStreamer = barsStreamer as TSBarsStreamer;
            //tsBarsStreamer.requestsSent = 0;
            //tsBarsStreamer.responseReceived = 0;

            if (!connect)
                return barsStreamer;

            // read from server
            for (int i = 0; i < 10; ++i)
            {
                HttpStatusCode statusCode = HttpStatusCode.Unauthorized;
                var startTime = DateTime.Now.ToUniversalTime();
                if (IsLoggedIn && !string.IsNullOrEmpty(AccessToken))
                {
                    try
                    {
                        string startDate = "01-01-2015";

                        barsStreamer.HistorySettings.SetChunkCount(barsStreamer.Interval);
                        //
                        //startDate = startTime.ToString(@"MM-dd-yyyy\thh:mm:ss");

                        int intervalVal = 1;
                        string unit = "Daily";
                        switch (barsStreamer.Interval.MeasureUnit)
                        {
                            case DateTimeMeasureUnit.Minute:
                                intervalVal = barsStreamer.Interval.MeasureUnitMultiplier;
                                unit = "Minute";
                                startDate = startTime.AddMinutes(-10).ToString(@"MM-dd-yyyy\thh:mm:ss");
                                break;
                            case DateTimeMeasureUnit.Hour:
                                intervalVal = 60 * barsStreamer.Interval.MeasureUnitMultiplier;
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
                        statusCode = await MarketdataApi.StreamBarchartsFromStartDateAsync(AccessToken,
                            barsStreamer.Symbol, intervalVal, unit, startDate, BarchartReceived, barsStreamer, barsStreamer.cancellationToken, "USEQPreAndPost");
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
                        {
                            //tsBarsStreamer.requestsSent++;

                            HttpStatusCode httpStatusCode2 = HttpStatusCode.Unauthorized;

                            _ = Task.Run(async () =>
                            {
                                int intervalVal = 1;
                                string unit = "Daily";
                                switch (barsStreamer.Interval.MeasureUnit)
                                {
                                    case DateTimeMeasureUnit.Minute:
                                        intervalVal = barsStreamer.Interval.MeasureUnitMultiplier;
                                        unit = "Minute";
                                        break;
                                    case DateTimeMeasureUnit.Hour:
                                        intervalVal = 60 * barsStreamer.Interval.MeasureUnitMultiplier;
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

                                if (barsStreamer.HistorySettings.Type == StockHistorySettingsType.FirstData)
                                {
                                    //tsBarsStreamer.requestsSent++;
                                    httpStatusCode2 = await MarketdataApi.StreamBarchartsFromStartDateToEndDateAsync(AccessToken,
                                       barsStreamer.Symbol, intervalVal, unit, barsStreamer.HistorySettings.TimeStart.ToString(@"MM-dd-yyyy\thh:mm:ss"),
                                       startTime.ToString(@"MM-dd-yyyy\thh:mm:ss"),
                                       BarchartReceived, barsStreamer, barsStreamer.cancellationToken, "USEQPreAndPost");

                                }
                                else if (barsStreamer.HistorySettings.Type == StockHistorySettingsType.NumberYearsBack)
                                {
                                    //tsBarsStreamer.requestsSent++;
                                    httpStatusCode2 = await MarketdataApi.StreamBarchartsFromStartDateToEndDateAsync(AccessToken,
                                       barsStreamer.Symbol, intervalVal, unit, startTime.AddYears(-(int)barsStreamer.HistorySettings.YearsBack).ToString(@"MM-dd-yyyy\thh:mm:ss"),
                                       startTime.ToString(@"MM-dd-yyyy\thh:mm:ss"), BarchartReceived, barsStreamer, barsStreamer.cancellationToken, "USEQPreAndPost");
                                }
                                else
                                {
                                    for (int j = 1; j <= barsStreamer.HistorySettings.ChunkCount; j++)
                                    {
                                        var currentHistory = barsStreamer.HistorySettings.GetCurrentChunk(startTime, j, barsStreamer.Interval);

                                        var lastDate = currentHistory.TimeStart.ToString(@"MM-dd-yyyy\thh:mm:ss");

                                        //tsBarsStreamer.requestsSent++;

                                        httpStatusCode2 = await MarketdataApi.StreamBarchartsBarsBackAsync(AccessToken,
                                            barsStreamer.Symbol, intervalVal, unit, (int?)currentHistory.BarsBack, lastDate, BarchartReceived,
                                            barsStreamer, barsStreamer.cancellationToken, "USEQPreAndPost");

                                        if (httpStatusCode2 != HttpStatusCode.OK)
                                            break;
                                    }
                                }
                            });
                            return barsStreamer;
                        }
                    case HttpStatusCode.NotFound:
                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        // try to re-authorize
                        await Task.Delay(5000);
                        await LogIn();
                        break;
                    default:
                        // print error message. F5 to reconnect.
                        await Task.Delay(5000);
                        //ShowMessage(statusCode.ToString());
                        break;
                }
            }
            return null;
        }

        public async Task<ObservableCollection<TradingData>> GetBarsRange(string symbol = null, ChartIntervalItem interval = null,
            DateTime? startDate = null, DateTime? stopDate = null, IBarsStreamer barsStreamer = null)
        {
            if (barsStreamer == null)   // create new
            {
                barsStreamer = new TSBarsStreamer(2);
            }
            else      // update
            {
                barsStreamer.Close();           //todo: reconnect without clear
                barsStreamer.Data.Clear();
            }

            var tsBarsStreamer = barsStreamer as TSBarsStreamer;
            if (symbol != null)
                barsStreamer.Symbol = symbol;
            if (interval != null)
                barsStreamer.Interval = interval;
            if (startDate != null)
                tsBarsStreamer.RangeStartDT = startDate.Value;
            if (stopDate != null)
                tsBarsStreamer.RangeStopDT = stopDate.Value;
            else
                tsBarsStreamer.RangeStopDT = DateTime.UtcNow;

            bool IsExtendedHours = Properties.Settings.Default.UseExtendedHours;

            // read from server
            for (int i = 0; i < 10; ++i)
            {
                HttpStatusCode statusCode = HttpStatusCode.Unauthorized;
                if (IsLoggedIn && !string.IsNullOrEmpty(AccessToken))
                {
                    try
                    {
                        int intervalVal = 1;
                        string unit = "Daily";
                        switch (barsStreamer.Interval.MeasureUnit)
                        {
                            case DateTimeMeasureUnit.Minute:
                                intervalVal = barsStreamer.Interval.MeasureUnitMultiplier;
                                unit = "Minute";
                                break;
                            case DateTimeMeasureUnit.Hour:
                                intervalVal = 60 * barsStreamer.Interval.MeasureUnitMultiplier;
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
                        statusCode = await MarketdataApi.StreamBarchartsFromStartDateToEndDateAsync(AccessToken,
                            barsStreamer.Symbol, intervalVal, unit,
                            tsBarsStreamer.RangeStartDT.ToString(@"MM-dd-yyyy\thh:mm:ss"),
                            tsBarsStreamer.RangeStopDT.ToString(@"MM-dd-yyyy\thh:mm:ss"),
                            BarchartReceived, barsStreamer, barsStreamer.cancellationToken, "USEQPreAndPost");
                    }
                    catch(Exception ex)
                    {
                        //ShowMessage(ex.Message);
                        continue;
                    }
                }

                switch (statusCode)
                {
                    case HttpStatusCode.OK:
                        // do nothing on successful connection
                        {
                            int count = barsStreamer.Data.Count;
                            for (int j = 0; j < 1000; j++)   //100 sec max waiting time
                            {
                                if (barsStreamer.cancellationToken.IsCancellationRequested)
                                    break;

                                await Task.Delay(100, barsStreamer.cancellationToken);

                                if (tsBarsStreamer.IsEndOfStream && count == barsStreamer.Data.Count)
                                    break;
                                else
                                    count = barsStreamer.Data.Count;
                            }

                            // add extra bar for extended hours
                            if (IsExtendedHours && tsBarsStreamer.Data.Count > 0)
                            {
                                var extraBarsStreamer = new TSBarsStreamer(0);
                                extraBarsStreamer.Symbol = barsStreamer.Symbol;
                                extraBarsStreamer.Interval = barsStreamer.Interval;
                                extraBarsStreamer.TimeZone = barsStreamer.TimeZone;

                                HttpStatusCode statusCode2 = HttpStatusCode.Unauthorized;
                                try
                                {
                                    statusCode2 = await MarketdataApi.StreamBarchartsFromStartDateToEndDateAsync(AccessToken,
                                        extraBarsStreamer.Symbol, 10, "Minute",
                                        TimeZoneInfo.ConvertTime(tsBarsStreamer.Data.Last().Date, extraBarsStreamer.TimeZone, TimeZoneInfo.Utc).ToString(@"MM-dd-yyyy\thh:mm:ss"),
                                        DateTime.UtcNow.ToString(@"MM-dd-yyyy\thh:mm:ss"),
                                        BarchartReceived, extraBarsStreamer, extraBarsStreamer.cancellationToken, "USEQPreAndPost");
                                }
                                catch (Exception ex)
                                {
                                    //ShowMessage(ex.Message);
                                    continue;
                                }

                                if(statusCode2 == HttpStatusCode.OK)
                                {
                                    int count2 = extraBarsStreamer.Data.Count;
                                    for (int j = 0; j < 1000; j++)   //100 sec max waiting time
                                    {
                                        if (extraBarsStreamer.cancellationToken.IsCancellationRequested)
                                            break;

                                        await Task.Delay(100, extraBarsStreamer.cancellationToken);

                                        if (extraBarsStreamer.IsEndOfStream && count2 == extraBarsStreamer.Data.Count)
                                            break;
                                        else
                                            count2 = extraBarsStreamer.Data.Count;
                                    }
                                }

                                if(extraBarsStreamer.Data.Count>0)
                                {
                                    double? Open = default;
                                    double High = 0, Low = double.MaxValue, Close = 0;
                                    DateTime dt = DateTime.MinValue;
                                    double Volume = 0;

                                    var lastRegular = barsStreamer.Data.Last();

                                    foreach (var b in extraBarsStreamer.Data)
                                    {
                                        if (b.Date < lastRegular.Date)
                                            continue;

                                        dt = b.Date;

                                        if (Open.HasValue == false)
                                            Open = b.Open;

                                        High = Math.Max(High, b.High);
                                        Low = Math.Min(Low, b.Low);
                                        Close = b.Close;
                                        Volume += b.Volume;
                                    }
                                    if (Open.HasValue)
                                    {
                                        TradingData extraBar = new TradingData(dt, Open.Value, High, Low, Close, Volume);
                                        barsStreamer.Data.Add(extraBar);
                                    }
                                }
                            }

                            return barsStreamer.Data;
                        }
                    case HttpStatusCode.NotFound:
                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        // try to re-authorize
                        await Task.Delay(5000);
                        await LogIn();
                        break;
                    default:
                        // print error message. F5 to reconnect.
                        await Task.Delay(5000);
                        //ShowMessage(statusCode.ToString());
                        break;
                }
            }
            return null;
        }

        private void BarchartReceived(string symbol, BarchartDefinition bar, object obj, Exception ex)
        {
            if (obj is TSBarsStreamer barsStreamer)
            {
                if (barsStreamer.cancellationToken.IsCancellationRequested == true)
                    return;
                else if (!IsLoggedIn || string.IsNullOrWhiteSpace(AccessToken))
                {
                    barsStreamer.cancellationTokenSource.Cancel();
                    return;
                }


                if (ex != null)
                {
                    if (ex.Message == "END of stream message received")
                    {
                        barsStreamer.IsEndOfStream = true;
                        //barsStreamer.responseReceived++;
                    }

                    else if (ex.Message != "ERROR message received (ERROR - No data available.)" &&
                        ex.Message != "ERROR message received (ERROR - Value was either too large or too small for a UInt32.)")
                    {
                        // reconnect
                        switch (barsStreamer.ReconnectFunc)
                        {
                            case 1: _ = GetBarsStreamer(barsStreamer: barsStreamer, connect: true); break;
                            case 2: _ = GetBarsRange(barsStreamer: barsStreamer); break;
                        }
                    }
                    return;
                }

                if (bar != null && !string.IsNullOrEmpty(bar.TimeStamp) && bar.Open != default && bar.High != default && bar.Low != default && bar.Close != default && bar.TotalVolume != default)
                {
                    var match = Regex.Match(bar.TimeStamp, @"\d\d*");
                    if (match.Success)
                    {
                        var unixDt = long.Parse(match.Value) / 1000;

                        DateTime dt = TimeZoneInfo.ConvertTimeFromUtc(Utils.ToDateTime(unixDt).ToUniversalTime(), barsStreamer.TimeZone);

                        _ = Application.Current?.Dispatcher.InvokeAsync(() =>
                        {
                            BarchartStatusFlags statusFlags = (BarchartStatusFlags)bar.Status;
                            //Debug.WriteLine(dt.ToShortDateString() + "-" + dt.ToShortTimeString() + " Status:  " + statusFlags.ToString());
                            if ((statusFlags == BarchartStatusFlags.REAL_TIME_DATA || statusFlags == BarchartStatusFlags.HISTORICAL_DATA) && barsStreamer.Data.Count > 0)
                            {
                                var mDate = dt;

                                var maxDate = barsStreamer.Data.Max(x => x.Date);

                                switch (barsStreamer.Interval.MeasureUnit)
                                {
                                    case DateTimeMeasureUnit.Minute:
                                        if (mDate.Minute >= (maxDate.Minute + barsStreamer.Interval.MeasureUnitMultiplier) || (mDate.Minute == 0 && maxDate.Minute > 1))
                                        {
                                            //do
                                            //{
                                            //    maxDate = maxDate.AddMinutes(barsStreamer.Interval.MeasureUnitMultiplier);
                                            //}
                                            //while ((mDate - maxDate).TotalMinutes >= barsStreamer.Interval.MeasureUnitMultiplier);

                                            //Add bar Interval.MeasureUnitMultiplier
                                            barsStreamer.Data.Add(new TradingData(maxDate.AddMinutes(barsStreamer.Interval.MeasureUnitMultiplier), decimal.ToDouble(bar.Open.Value), decimal.ToDouble(bar.High.Value), decimal.ToDouble(bar.Low.Value),
                                                        decimal.ToDouble(bar.Close.Value), decimal.ToDouble(bar.TotalVolume.Value)));
                                        }
                                        else
                                        {
                                            barsStreamer.UpdateBar(maxDate, bar);
                                            //Update bar
                                        }
                                        break;
                                    case DateTimeMeasureUnit.Hour:
                                        if (mDate.Hour >= (maxDate.Hour + barsStreamer.Interval.MeasureUnitMultiplier) || (mDate.Hour == 0 && maxDate.Hour > 1))
                                        {
                                            //do
                                            //{
                                            //    maxDate = maxDate.AddHours(barsStreamer.Interval.MeasureUnitMultiplier);
                                            //}
                                            //while ((mDate - maxDate).TotalHours >= barsStreamer.Interval.MeasureUnitMultiplier);

                                            //Add bar Interval.MeasureUnitMultiplier
                                            barsStreamer.Data.Add(new TradingData(maxDate.AddHours(barsStreamer.Interval.MeasureUnitMultiplier), decimal.ToDouble(bar.Open.Value), decimal.ToDouble(bar.High.Value), decimal.ToDouble(bar.Low.Value),
                                                decimal.ToDouble(bar.Close.Value), decimal.ToDouble(bar.TotalVolume.Value)));
                                        }
                                        else
                                        {
                                            barsStreamer.UpdateBar(maxDate, bar);
                                            //Update bar
                                        }
                                        break;
                                    case DateTimeMeasureUnit.Day:
                                        if (mDate.Day > maxDate.Day || (mDate.Day == 1 && maxDate.Day > 1))
                                        {
                                            //do
                                            //{
                                            //    maxDate = maxDate.AddDays(barsStreamer.Interval.MeasureUnitMultiplier);
                                            //}
                                            //while ((mDate - maxDate).TotalDays >= barsStreamer.Interval.MeasureUnitMultiplier);

                                            //Add bar
                                            barsStreamer.Data.Add(new TradingData(maxDate.AddDays(1), decimal.ToDouble(bar.Open.Value), decimal.ToDouble(bar.High.Value), decimal.ToDouble(bar.Low.Value),
                                                decimal.ToDouble(bar.Close.Value), decimal.ToDouble(bar.TotalVolume.Value)));
                                        }
                                        else
                                        {
                                            barsStreamer.UpdateBar(maxDate, bar);
                                            //Update bar

                                        }
                                        break;
                                    case DateTimeMeasureUnit.Week:
                                        if (mDate.Day > maxDate.Day || (mDate.Month == 1 && maxDate.Month == 12))
                                        {
                                            //do
                                            //{
                                            //    maxDate = maxDate.AddDays(7 * barsStreamer.Interval.MeasureUnitMultiplier);
                                            //}
                                            //while ((mDate - maxDate).TotalDays >= 7 * barsStreamer.Interval.MeasureUnitMultiplier);

                                            //add + 7
                                            barsStreamer.Data.Add(new TradingData(maxDate.AddDays(7), decimal.ToDouble(bar.Open.Value), decimal.ToDouble(bar.High.Value), decimal.ToDouble(bar.Low.Value),
                                                decimal.ToDouble(bar.Close.Value), decimal.ToDouble(bar.TotalVolume.Value)));
                                        }
                                        else
                                        {
                                            barsStreamer.UpdateBar(maxDate, bar);
                                            //update
                                        }
                                        break;
                                    case DateTimeMeasureUnit.Month:
                                        if (mDate.Month > maxDate.Month || (mDate.Month == 1 && maxDate.Month == 12))
                                        {
                                            //add + 1
                                            barsStreamer.Data.Add(new TradingData(maxDate.AddMonths(1), decimal.ToDouble(bar.Open.Value), decimal.ToDouble(bar.High.Value), decimal.ToDouble(bar.Low.Value),
                                                decimal.ToDouble(bar.Close.Value), decimal.ToDouble(bar.TotalVolume.Value)));
                                        }
                                        else
                                        {
                                            //update
                                            barsStreamer.UpdateBar(maxDate, bar);
                                        }
                                        break;
                                }
                            }
                            else if (bar.Status.ToString().Contains("536870") ||
                                    (statusFlags == (BarchartStatusFlags.HISTORICAL_DATA | BarchartStatusFlags.EXTENDED_BAR)) ||
                                     statusFlags == BarchartStatusFlags.END_OF_HISTORY_STREAM ||
                                    (statusFlags == (BarchartStatusFlags.NEW | BarchartStatusFlags.HISTORICAL_DATA | BarchartStatusFlags.STANDARD_CLOSE)) ||
                                    (statusFlags == (BarchartStatusFlags.UPDATE_CORPACTION | BarchartStatusFlags.EXTENDED_BAR)))
                            {

                                //todo: it's possible to improve performance by mixing FirstOrDefault() and IndexOf() calls

                                TradingData pt = barsStreamer.Data.FirstOrDefault(x => x.Date == dt);

                                if (pt != default)
                                {
                                    var index = barsStreamer.Data.IndexOf(pt);
                                    barsStreamer.Data[index] = new TradingData(pt.Date, decimal.ToDouble(bar.Open.Value), decimal.ToDouble(bar.High.Value), decimal.ToDouble(bar.Low.Value),
                                            decimal.ToDouble(bar.Close.Value), decimal.ToDouble(bar.TotalVolume.Value));
                                }
                                else
                                {
                                    barsStreamer.AddBar(dt, bar);
                                    //barsStreamer.Data.Add(new TradingData(dt, decimal.ToDouble(bar.Open.Value), decimal.ToDouble(bar.High.Value), decimal.ToDouble(bar.Low.Value),
                                    //        decimal.ToDouble(bar.Close.Value), decimal.ToDouble(bar.TotalVolume.Value)));
                                }
                            }
                            else
                            {

                            }
                        });
                    }
                }
            }
        }

        public StocksInformation GetStockMarketData(string symbol)
        {
            return new StocksInformation();
        }
        #endregion
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
}
