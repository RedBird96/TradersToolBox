/* 
 * Tradestation API
 *
 * This document describes the resources that make up the official TradeStation API. If you have any problems or requests please contact [support](mailto:webapi@tradestation.com).  Overview ========  The TradeStation API is reachable at the base-url: ``` https://api.tradestation.com/v2 ```  Current Version - -- -- -- -- -- -- --  The latest version is 20160101, but currently we are in transition, so by default all requests receive the 20101026 version for backwards compatibility.  Always explicitly request this version by adding the `APIVersion` querystring parameter as shown below:  ``` https://api.tradestation.com/v2/data/quote/msft?APIVersion=20160101 ```  Note: This will ensure your application will not be broken when we deprecate the 20101026 version in favor of 20160101 or newer versions.  SIM vs LIVE - -- -- -- -- --  We also offer a Simulator(SIM) API for \"Paper Trading\" that is identical to the Live API in all ways except it uses fake trading accounts seeded with fake money and orders are not actually executed - only simulated executions occur with instant \"fills\".  To access the SIM environment, you must change your base-url to: ``` https://sim-api.tradestation.com/v2 ```  **WARNING:** TradeStation is not liable for mistakes made by applications that allow users to switch between SIM and Live environments.  ### Why offer a Simulator?  Transactional API calls such as Order Execution offers users or applications the ability to experiment within a Simulated trading system so that real accounts and money are not affected and trades are not actually executed.  Other potential use-cases: - Learning how to use applications via Paper Trading. - Exploring TradeStation API behavior without financial ramifications - Testing apps and websites before making them Live to customers - Enabling users to \"Try-before-they-buy\" with apps that use the TradeStation API - Hosting trading competitions or games   HTTP Requests - -- -- -- -- -- --  All API access is over HTTPS, and accessed from the https://api.tradestation.com. All data is sent and received as JSON, with some limited support for XML.  Example Request: ``` curl -i https://api.tradestation.com/v2/data/quote/msft?APIVersion=20160101  HTTP/1.1 200 OK Cache-Control: private Content-Length: 1545 Content-Type: application/default+json; charset=utf-8 Access-Control-Allow-Origin: * APIVersion: 20160101 Date: Wed, 30 Nov 2016 01:51:45 GMT  [ ...json... ] ```  ### Common Conventions  - Blank fields may either be included as null or omitted, so please support both. - All timestamps are returned in [Epoch time](https://en.wikipedia.org/wiki/Unix_time) format unless stated otherwise.    HTTP Streaming - -- -- -- -- -- -- -  The TradeStation API offers HTTP Streaming responses for some specialized resources including intraday barcharts, quote changes, and quote snapshots. These streams conform to RFC2616 for HTTP/1.1 Streaming with some slight modifications.  > The HTTP streaming mechanism keeps a request open indefinitely.  It > never terminates the request or closes the connection, even after the > server pushes data to the client.  This mechanism significantly > reduces the network latency because the client and the server do not > need to open and close the connection. > > The basic life cycle of an application using HTTP streaming is as > follows: > > 1.  The client makes an initial request and then waits for a >     response. > > 2.  The server defers the response to a poll request until an update >     is available, or until a particular status or timeout has >     occurred. > > 3.  Whenever an update is available, the server sends it back to the >     client as a part of the response. > > 4.  The data sent by the server does not terminate the request or the >     connection.  The server returns to step 3. > > The HTTP streaming mechanism is based on the capability of the server > to send several pieces of information in the same response, without > terminating the request or the connection.  Source: [RFC6202, Page 7](https://tools.ietf.org/html/rfc6202#page-7).  HTTP Streaming resources are identified under in this documentation as such, all other resources conform to the HTTP Request pattern instead.  The HTTP Streaming response is returned with the following headers:    ```   Transfer-Encoding: chunked   Content-Type: application/vnd.tradestation.streams+json   ```  Note: The `Content-Length` header is typically omitted since the response body size is unknown.  Streams consist of a series of chunks that contain individual JSON objects to be parsed separately rather than as a whole response body.  One unique thing about TradeStation's HTTP Streams is they also can terminate unlike a canonical HTTP/1.1 Stream.  Streams terminate with a non-JSON string prefixed with one of the following:    - `END`   - `ERROR`  In the case of `ERROR`, it will often be followed by an error message like:  ``` ERROR - A Timeout Occurred after waiting 15000ms ```  In either case, the HTTP client must terminate the HTTP Stream and end the HTTP Request lifetime as a result of these messages. In the case of `ERROR` the client application may add a delay before re-requesting the HTTP Stream.  ### How to handle HTTP Chunked Encoded Streams  Healthy chunked-encoded streams emit variable length chunks that contain parsable JSON.  For example: ``` GET https://sim.api.tradestation.com/v2/stream/barchart/$DJI/1/Minute/12-26-2016/01-24-2017?access_token=…  HTTP/1.1 200 OK Date: Wed, 14 Jun 2017 01:17:36 GMT Content-Type: application/vnd.tradestation.streams+json Transfer-Encoding: chunked Connection: keep-alive Access-Control-Allow-Origin: * Cache-Control: private  114 {\"Close\":19956.09,\"DownTicks\":26,\"DownVolume\":940229,\"High\":19961.77,\"Low\":19943.46,\"Open\":19943.46,\"Status\":13,\"TimeStamp\":\"/Date(1482849060000)/\",\"TotalTicks\":59,\"TotalVolume\":3982533,\"UnchangedTicks\":0,\"UnchangedVolume\":0,\"UpTicks\":33,\"UpVolume\":3042304,\"OpenInterest\":0}   112 {\"Close\":19950.82,\"DownTicks\":32,\"DownVolume\":440577,\"High\":19959.15,\"Low\":19947.34,\"Open\":19955.64,\"Status\":13,\"TimeStamp\":\"/Date(1482849120000)/\",\"TotalTicks\":60,\"TotalVolume\":761274,\"UnchangedTicks\":0,\"UnchangedVolume\":0,\"UpTicks\":28,\"UpVolume\":320697,\"OpenInterest\":0}   END ```  Typically this will stream forever, unless a network interruption or service disruption occurs. It is up to the client to properly handle stream lifetime and connection closing.  ### How to parse JSON chunks  In order to process these chunks, API consumers should first read the response buffer, then de-chunk the plain-text strings, and finally identify new JSON objects by applying tokenizing techniques to the resulting text stream using either a streaming JSON parser, Regex, a lexer/parser, or brute-force string indexing logic.  A simple but effective technique is after de-chunking to simply parse based upon the `\\n` (newline character) delimiter written to the end of each JSON object. However, a more robust solution is less likely to break later.  #### Variable Length JSON Chunking  As a developer, be careful with how you parse HTTP Streams, because the API’s or intermediate proxies may chunk JSON objects many different ways.  > Using HTTP streaming, several application > messages can be sent within a single HTTP response.  The > separation of the response stream into application messages needs > to be performed at the application level and not at the HTTP > level.  In particular, it is not possible to use the HTTP chunks > as application message delimiters, since intermediate proxies > might “re-chunk” the message stream (for example, by combining > different chunks into a longer one).  This issue does not affect > the HTTP long polling technique, which provides a canonical > framing technique: each application message can be sent in a > different HTTP response.  Source: [RFC6202, Section 3.2](https://tools.ietf.org/html/rfc6202#section-3.2)  Translation: Be prepared for JSON objects that span chunks. You may see chunks with varying numbers of JSON objects, including:  - \"exactly 1\" JSON object per chunk - “at least 1” JSON object per chunk - 1 JSON object split across 2 or more chunks  Example of 2 JSON objects in 1 chunk: ``` GET https://sim.api.tradestation.com/v2/stream/barchart/$DJI/1/Minute/12-26-2016/01-24-2017?access_token=…  HTTP/1.1 200 OK Date: Wed, 14 Jun 2017 01:17:36 GMT Content-Type: application/vnd.tradestation.streams+json Transfer-Encoding: chunked Connection: keep-alive Access-Control-Allow-Origin: * Cache-Control: private  22d {\"Close\":19956.09,\"DownTicks\":26,\"DownVolume\":940229,\"High\":19961.77,\"Low\":19943.46,\"Open\":19943.46,\"Status\":13,\"TimeStamp\":\"/Date(1482849060000)/\",\"TotalTicks\":59,\"TotalVolume\":3982533,\"UnchangedTicks\":0,\"UnchangedVolume\":0,\"UpTicks\":33,\"UpVolume\":3042304,\"OpenInterest\":0} {\"Close\":19950.82,\"DownTicks\":32,\"DownVolume\":440577,\"High\":19959.15,\"Low\":19947.34,\"Open\":19955.64,\"Status\":13,\"TimeStamp\":\"/Date(1482849120000)/\",\"TotalTicks\":60,\"TotalVolume\":761274,\"UnchangedTicks\":0,\"UnchangedVolume\":0,\"UpTicks\":28,\"UpVolume\":320697,\"OpenInterest\":0} END ```  Example of 1 JSON objects split across 2 chunks: ``` GET https://sim.api.tradestation.com/v2/stream/barchart/$DJI/1/Minute/12-26-2016/01-24-2017?access_token=…  HTTP/1.1 200 OK Date: Wed, 14 Jun 2017 01:17:36 GMT Content-Type: application/vnd.tradestation.streams+json Transfer-Encoding: chunked Connection: keep-alive Access-Control-Allow-Origin: * Cache-Control: private  40 {\"Close\":71.65,\"DownTicks\":45,\"DownVolume\":5406,\"High\":71.67,\"Lo C2 w\":71.65,\"Open\":71.66,\"Status\":13,\"TimeStamp\":\"/Date(1497016260000)/\",\"TotalTicks\":77,\"TotalVolume\":17270,\"UnchangedTicks\":0,\"UnchangedVolume\":0,\"UpTicks\":32,\"UpVolume\":11864,\"OpenInterest\":0}  END ```  This is allowed by the HTTP/1.1 specification, but can be confusing or lead to bugs in client applications if you try to depend parsing JSON along the HTTP chunk-boundaries because even if it works during testing, later if users connect from a different network, it may change the chunking behavior.  For example, if you are at a coffee shop with wifi which employs an HTTP Proxy, then it may buffer the stream and change the chunking boundary from 1 JSON object per chunk, to splitting each JSON object across 2 or 3.  In fact, the HTTP/1.1 spec clearly advises developers of proxies to always “re-chunk” HTTP Streams, so this is almost a guarantee to happen in the wild.  HTTP Streaming API consumers should be prepared to support all variations.     Rate Limiting - -- -- -- -- -- --  The TradeStation API Rate Limits on the number of requests a given user & client can make to the API in order to ensure fairness between users and prevent abuse to our network. Each API Key is allocated quota settings upon creation. These settings are applied on a per-user basis. If the quota is exceeded, an HTTP response of `403 Quota Exceeded` will be returned. Quotas are reset on a 5-minute interval based on when the user issued the first request.  ## Resource Categories  The rate limit applies to the following resource-categories:  | Resource-Category                             | Quota | Interval | | - -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- | - -- -- | - -- -- -- - | | Accounts                                      | 250   | 5-minute | | Order Details                                 | 250   | 5-minute | | Balances                                      | 250   | 5-minute | | Positions                                     | 250   | 5-minute | | Data Quotes                                   | 250   | 5-minute | | Quote Change Stream                           | 500   | 5-minute | | Quote Snapshot Stream                         | 500   | 5-minute | | Barchart Stream                               | 500   | 5-minute | | TickBar Stream                                | 500   | 5-minute |    ## Intervals  Quotas have \"Windows\" that last for a limited time interval (generally 5-minutes). Once the user has exceeded the maximum request count, all future requests will fail with a `403` error until the interval expires. Rate Limit intervals do not slide based upon the number of requests, they are fixed at a point in time starting from the very first request for that category of resource.  After the interval expires, the cycle will start over at zero and the user can make more requests.  ### Example A  A user logs into the TradeStation WebAPI with your application and issues a request to `/v2/EliteTrader/accounts`. As a result, the request quota is incremented by one for the `Accounts` resource-category. The user then issues 250 more requests immediately to `/v2/EliteTrader/accounts`. The last request fails with `403 Quota Exceeded`. All subsequent requests continue to fail until the 5-minute interval expires from the time of the very first request.  ### Example B  A user logs into the TradeStation WebAPI with your application and issues a request to `/v2/data/quote/IBM,NFLX,MSFT,AMZN,AAPL`. As a result, the request quota is incremented by one for the `Data Quotes` resource-category. The user then immediately issues the same request 250 more times. The last request fails with `403 Quota Exceeded`. All subsequent requests continue to fail until the 5-minute interval expires from the time of the first request.  **Example Throttled Request** ``` GET https://api.tradestation.com/v2/data/quotes/IBM,NFLX,MSFT,AMZN,AAPL HTTP/1.1 Host: api.tradestation.com Authorization: bearer eE45VkdQSnlBcmI0Q2RqTi82SFdMSVE0SXMyOFo5Z3dzVzdzdk Accept: application/json ``` **Example Failed Response** ``` HTTP/1.1 403 Quota Exceeded Content-Length: 15 Server: Microsoft-IIS/7.5 X-AspNet-Version: 4.0.30319 Date: Tue, 06 Dec 2011 20:50:32 GMT  <html><body>Quota Exceeded</body></html> ``` 
 *
 * OpenAPI spec version: 20160101
 * Contact: webapi@tradestation.com
 * Generated by: https://github.com/swagger-api/swagger-codegen.git
 */

using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;
using SwaggerDateConverter = TradeStationAPI.Client.SwaggerDateConverter;

namespace TradeStationAPI.Model
{
    /// <summary>
    /// AccountBalancesDefinitionInner
    /// </summary>
    [DataContract]
    public partial class AccountBalancesDefinitionInner :  IEquatable<AccountBalancesDefinitionInner>, IValidatableObject
    {
        /// <summary>
        /// Status of a specific account. * A - Active * X - Closed * C - Closing Transaction Only * F - Margin Call - Closing Transactions Only * I - Inactive * L - Liquidating Transactions Only * R - Restricted * D - 90 Day Restriction-Closing Transaction Only 
        /// </summary>
        /// <value>Status of a specific account. * A - Active * X - Closed * C - Closing Transaction Only * F - Margin Call - Closing Transactions Only * I - Inactive * L - Liquidating Transactions Only * R - Restricted * D - 90 Day Restriction-Closing Transaction Only </value>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum StatusEnum
        {
            
            /// <summary>
            /// Enum A for value: A
            /// </summary>
            [EnumMember(Value = "A")]
            A = 1,
            
            /// <summary>
            /// Enum X for value: X
            /// </summary>
            [EnumMember(Value = "X")]
            X = 2,
            
            /// <summary>
            /// Enum C for value: C
            /// </summary>
            [EnumMember(Value = "C")]
            C = 3,
            
            /// <summary>
            /// Enum F for value: F
            /// </summary>
            [EnumMember(Value = "F")]
            F = 4,
            
            /// <summary>
            /// Enum I for value: I
            /// </summary>
            [EnumMember(Value = "I")]
            I = 5,
            
            /// <summary>
            /// Enum L for value: L
            /// </summary>
            [EnumMember(Value = "L")]
            L = 6,
            
            /// <summary>
            /// Enum R for value: R
            /// </summary>
            [EnumMember(Value = "R")]
            R = 7,
            
            /// <summary>
            /// Enum D for value: D
            /// </summary>
            [EnumMember(Value = "D")]
            D = 8
        }

        /// <summary>
        /// Status of a specific account. * A - Active * X - Closed * C - Closing Transaction Only * F - Margin Call - Closing Transactions Only * I - Inactive * L - Liquidating Transactions Only * R - Restricted * D - 90 Day Restriction-Closing Transaction Only 
        /// </summary>
        /// <value>Status of a specific account. * A - Active * X - Closed * C - Closing Transaction Only * F - Margin Call - Closing Transactions Only * I - Inactive * L - Liquidating Transactions Only * R - Restricted * D - 90 Day Restriction-Closing Transaction Only </value>
        [DataMember(Name="Status", EmitDefaultValue=false)]
        public StatusEnum Status { get; set; }
        /// <summary>
        /// The type of the account. * C - Cash * M - Margin * D - DVP * F - Futures 
        /// </summary>
        /// <value>The type of the account. * C - Cash * M - Margin * D - DVP * F - Futures </value>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum TypeEnum
        {
            
            /// <summary>
            /// Enum C for value: C
            /// </summary>
            [EnumMember(Value = "C")]
            C = 1,
            
            /// <summary>
            /// Enum M for value: M
            /// </summary>
            [EnumMember(Value = "M")]
            M = 2,
            
            /// <summary>
            /// Enum D for value: D
            /// </summary>
            [EnumMember(Value = "D")]
            D = 3,
            
            /// <summary>
            /// Enum F for value: F
            /// </summary>
            [EnumMember(Value = "F")]
            F = 4
        }

        /// <summary>
        /// The type of the account. * C - Cash * M - Margin * D - DVP * F - Futures 
        /// </summary>
        /// <value>The type of the account. * C - Cash * M - Margin * D - DVP * F - Futures </value>
        [DataMember(Name="Type", EmitDefaultValue=false)]
        public TypeEnum Type { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="AccountBalancesDefinitionInner" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected AccountBalancesDefinitionInner() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="AccountBalancesDefinitionInner" /> class.
        /// </summary>
        /// <param name="alias">A user specified name that identifies a TradeStation account. (required).</param>
        /// <param name="bODAccountBalance">(Equities) Deprecated. The amount of cash in the account at the beginning of the day. Redundant with BODNetCash..</param>
        /// <param name="bODDayTradingMarginableEquitiesBuyingPower">(Equities) The Intraday Buying Power (Day Trading Rule 431) with which the account started the trading day..</param>
        /// <param name="bODEquity">The total amount of equity with which you started the current trading day. Sum of the beginning day cash balance for your account and the market value of all positions brought into the current trading day (those that were held overnight) minus the outstanding margin debit balance for the account at the start of the current trading day. (Cash balance) + (Long market value) + (Short Credit) - (Margin Debit) + (Short Market Value). For cash accounts, also includes UnsettledFunds.  For futures accounts, also includes securities on deposit. (required).</param>
        /// <param name="bODNetCash">The amount of cash in the account at the beginning of the day. (required).</param>
        /// <param name="bODOpenTradeEquity">(Futures) Unrealized profit and loss at the beginning of the day. (required).</param>
        /// <param name="bODOptionBuyingPower">(Equities) Option buying power at the start of the trading day..</param>
        /// <param name="bODOptionValue">(Equities) Liquidation value of options at the start of the trading day..</param>
        /// <param name="bODOvernightBuyingPower">(Equities) Overnight Buying Power (Regulation T) at the start of the trading day..</param>
        /// <param name="canDayTrade">(Equities) Indicates whether day trading is allowed in the account..</param>
        /// <param name="closedPositions">closedPositions (required).</param>
        /// <param name="commission">(Futures) The brokerage commission cost and routing fees (if applicable) for a trade based on the number of shares or contracts. (required).</param>
        /// <param name="dayTradeExcess">(Equities) (Buying Power Available - Buying Power Used) / Buying Power Multiplier, (Futures) (Cash + UnrealizedGains) - Buying Power Used.</param>
        /// <param name="dayTradeOpenOrderMargin">(Futures) Money field representing the current amount of money reserved for open orders..</param>
        /// <param name="dayTrades">(Equities) The number of day trades placed in the account within the previous 4 trading days. A day trade refers to buying then selling or selling short then buying to cover the same security on the same trading day..</param>
        /// <param name="dayTradingQualified">(Equities) Indicates if the account is qualified to day trade as per compliance suitability in TradeStation..</param>
        /// <param name="currency">(Futures) The base currency of the symbol. (required).</param>
        /// <param name="currencyDetails">(Futures) Currency details of the symbol. (required).</param>
        /// <param name="displayName">The friendly name of the specific TradeStation account. (required).</param>
        /// <param name="key">Key is the unique identifier for the requested account. (required).</param>
        /// <param name="marketValue">Market value of open positions. (required).</param>
        /// <param name="name">name of the account. (required).</param>
        /// <param name="optionApprovalLevel">(Equities) The option approval level will determine what options strategies you will be able to employ in the account. In general terms, the levels are defined as follows * Level 0 - No options trading allowed. * Level 1 - Writing of Covered Calls, Buying Protective Puts. * Level 2 - Level 1 + Buying Calls, Buying Puts, Writing Covered Puts. * Level 3 - level 2+ Stock Option Spreads, Index Option Spreads, Butterfly Spreads, Condor Spreads, Iron Butterfly Spreads, Iron Condor Spreads. * Level 4 - Level 3 + Writing of Naked Puts (Stock Options). * Level 5 - Level 4 + Writing of Naked Puts (Index Options), Writing of Naked Calls (Stock Options), Writing of Naked Calls (Index Options). .</param>
        /// <param name="openOrderMargin">(Futures) The dollar amount of Open Order Margin for the given futures account. (required).</param>
        /// <param name="patternDayTrader">(Equities) Indicates whether you are considered a pattern day trader. As per FINRA rules, you will be considered a pattern day trader if you trade 4 or more times in 5 business days and your day-trading activities are greater than 6 percent of your total trading activity for that same five-day period. A pattern day trader must maintain a minimum equity of $25,000 on any day that the customer day trades. If the account falls below the $25,000 requirement, the pattern day trader will not be permitted to day trade until the account is restored to the $25,000 minimum equity level. .</param>
        /// <param name="realTimeAccountBalance">Current cash balance of the account. (required).</param>
        /// <param name="realTimeBuyingPower">Indicates the value of real-time buying power. (required).</param>
        /// <param name="realTimeCostOfPositions">(Equities) Total real-time cost of all open positions. Positions are based on the actual entry price..</param>
        /// <param name="realTimeDayTradeMargin">(Futures) Money field representing the current total amount of futures day trade margin..</param>
        /// <param name="realTimeDayTradingMarginableEquitiesBuyingPower">(Equities) The intraday buying power for trading marginable equities..</param>
        /// <param name="realTimeEquity">The real-time cash reserves for your account. At the beginning of the trading day, this value will equal the beginning day cash balance. This figure is calculated by taking the real-time cash balance plus the market value of any long positions minus the market value of any short position. (Real-time cash balance) + (Market value of long positions) – (Market value of short positions) + (Liquidation value of options) (required).</param>
        /// <param name="realTimeInitialMargin">(Futures) Sum (Initial Margins of all positions in the given account) (required).</param>
        /// <param name="realTimeOptionBuyingPower">(Equities) The intraday buying power for options..</param>
        /// <param name="realTimeOptionValue">(Equities) Intraday liquidation value of option positions..</param>
        /// <param name="realTimeOvernightBuyingPower">(Equities) Real-time Overnight Marginable Equities Buying Power..</param>
        /// <param name="realTimeMaintenanceMargin">(Futures) The dollar amount of Real-time Maintenance Margin for the given futures account. (required).</param>
        /// <param name="realTimeRealizedProfitLoss">Indicates any gain or loss as a result of closing a position during the current trading day. This value also includes all commissions and routing fees incurred during the current trading day. (required).</param>
        /// <param name="realTimeTradeEquity">(Futures) The dollar amount of unrealized profit and loss for the given futures account.  Same value as RealTimeUnrealizedGains. (required).</param>
        /// <param name="realTimeUnrealizedGains">Unrealized profit and loss, for the current trading day, of all open positions. (required).</param>
        /// <param name="realTimeUnrealizedProfitLoss">Unrealized profit or loss of all open positions using current market prices. (required).</param>
        /// <param name="securityOnDeposit">(Futures) The value of special securities that are deposited by the customer with the clearing firm for the sole purpose of increasing purchasing power in their trading account. This number will be reset daily by the account balances clearing file. The entire value of this field will increase purchasing power. (required).</param>
        /// <param name="status">Status of a specific account. * A - Active * X - Closed * C - Closing Transaction Only * F - Margin Call - Closing Transactions Only * I - Inactive * L - Liquidating Transactions Only * R - Restricted * D - 90 Day Restriction-Closing Transaction Only  (required).</param>
        /// <param name="statusDescription">String value for Status attribute. (required).</param>
        /// <param name="todayRealTimeTradeEquity">(Futures) The unrealized P/L for today. Unrealized P/L - BODOpenTradeEquity (required).</param>
        /// <param name="type">The type of the account. * C - Cash * M - Margin * D - DVP * F - Futures  (required).</param>
        /// <param name="typeDescription">String value for Type Attribute. (required).</param>
        /// <param name="unclearedDeposit">The total of uncleared checks received by TradeStation for deposit. (required).</param>
        /// <param name="unsettledFund">(Equities) Funds received by TradeStation that are not settled from a transaction in the account..</param>
        public AccountBalancesDefinitionInner(string alias = default(string), decimal? bODAccountBalance = default(decimal?), decimal? bODDayTradingMarginableEquitiesBuyingPower = default(decimal?), decimal? bODEquity = default(decimal?), decimal? bODNetCash = default(decimal?), decimal? bODOpenTradeEquity = default(decimal?), decimal? bODOptionBuyingPower = default(decimal?), decimal? bODOptionValue = default(decimal?), decimal? bODOvernightBuyingPower = default(decimal?), bool? canDayTrade = default(bool?), List<AccountBalancesDefinitionInnerClosedPositions> closedPositions = default(List<AccountBalancesDefinitionInnerClosedPositions>), decimal? commission = default(decimal?), decimal? dayTradeExcess = default(decimal?), decimal? dayTradeOpenOrderMargin = default(decimal?), decimal? dayTrades = default(decimal?), bool? dayTradingQualified = default(bool?), string currency = default(string), List<AccountBalancesDefinitionInnerCurrencyDetails> currencyDetails = default(List<AccountBalancesDefinitionInnerCurrencyDetails>), string displayName = default(string), decimal? key = default(decimal?), decimal? marketValue = default(decimal?), string name = default(string), decimal? optionApprovalLevel = default(decimal?), decimal? openOrderMargin = default(decimal?), bool? patternDayTrader = default(bool?), decimal? realTimeAccountBalance = default(decimal?), decimal? realTimeBuyingPower = default(decimal?), decimal? realTimeCostOfPositions = default(decimal?), decimal? realTimeDayTradeMargin = default(decimal?), decimal? realTimeDayTradingMarginableEquitiesBuyingPower = default(decimal?), decimal? realTimeEquity = default(decimal?), decimal? realTimeInitialMargin = default(decimal?), decimal? realTimeOptionBuyingPower = default(decimal?), decimal? realTimeOptionValue = default(decimal?), decimal? realTimeOvernightBuyingPower = default(decimal?), decimal? realTimeMaintenanceMargin = default(decimal?), decimal? realTimeRealizedProfitLoss = default(decimal?), decimal? realTimeTradeEquity = default(decimal?), decimal? realTimeUnrealizedGains = default(decimal?), decimal? realTimeUnrealizedProfitLoss = default(decimal?), decimal? securityOnDeposit = default(decimal?), StatusEnum status = default(StatusEnum), string statusDescription = default(string), decimal? todayRealTimeTradeEquity = default(decimal?), TypeEnum type = default(TypeEnum), string typeDescription = default(string), decimal? unclearedDeposit = default(decimal?), decimal? unsettledFund = default(decimal?))
        {
            // to ensure "alias" is required (not null)
            if (alias == null)
            {
                throw new InvalidDataException("alias is a required property for AccountBalancesDefinitionInner and cannot be null");
            }
            else
            {
                this.Alias = alias;
            }
            // to ensure "bODEquity" is required (not null)
            if (bODEquity == null)
            {
                throw new InvalidDataException("bODEquity is a required property for AccountBalancesDefinitionInner and cannot be null");
            }
            else
            {
                this.BODEquity = bODEquity;
            }
            // to ensure "bODNetCash" is required (not null)
            if (bODNetCash == null)
            {
                throw new InvalidDataException("bODNetCash is a required property for AccountBalancesDefinitionInner and cannot be null");
            }
            else
            {
                this.BODNetCash = bODNetCash;
            }
            // to ensure "bODOpenTradeEquity" is required (not null)
            if (bODOpenTradeEquity == null)
            {
                throw new InvalidDataException("bODOpenTradeEquity is a required property for AccountBalancesDefinitionInner and cannot be null");
            }
            else
            {
                this.BODOpenTradeEquity = bODOpenTradeEquity;
            }
            // to ensure "closedPositions" is required (not null)
            if (closedPositions == null)
            {
                throw new InvalidDataException("closedPositions is a required property for AccountBalancesDefinitionInner and cannot be null");
            }
            else
            {
                this.ClosedPositions = closedPositions;
            }
            // to ensure "commission" is required (not null)
            if (commission == null)
            {
                throw new InvalidDataException("commission is a required property for AccountBalancesDefinitionInner and cannot be null");
            }
            else
            {
                this.Commission = commission;
            }
            // to ensure "currency" is required (not null)
            if (currency == null)
            {
                throw new InvalidDataException("currency is a required property for AccountBalancesDefinitionInner and cannot be null");
            }
            else
            {
                this.Currency = currency;
            }
            // to ensure "currencyDetails" is required (not null)
            if (currencyDetails == null)
            {
                throw new InvalidDataException("currencyDetails is a required property for AccountBalancesDefinitionInner and cannot be null");
            }
            else
            {
                this.CurrencyDetails = currencyDetails;
            }
            // to ensure "displayName" is required (not null)
            if (displayName == null)
            {
                throw new InvalidDataException("displayName is a required property for AccountBalancesDefinitionInner and cannot be null");
            }
            else
            {
                this.DisplayName = displayName;
            }
            // to ensure "key" is required (not null)
            if (key == null)
            {
                throw new InvalidDataException("key is a required property for AccountBalancesDefinitionInner and cannot be null");
            }
            else
            {
                this.Key = key;
            }
            // to ensure "marketValue" is required (not null)
            if (marketValue == null)
            {
                throw new InvalidDataException("marketValue is a required property for AccountBalancesDefinitionInner and cannot be null");
            }
            else
            {
                this.MarketValue = marketValue;
            }
            // to ensure "name" is required (not null)
            if (name == null)
            {
                throw new InvalidDataException("name is a required property for AccountBalancesDefinitionInner and cannot be null");
            }
            else
            {
                this.Name = name;
            }
            // to ensure "openOrderMargin" is required (not null)
            if (openOrderMargin == null)
            {
                throw new InvalidDataException("openOrderMargin is a required property for AccountBalancesDefinitionInner and cannot be null");
            }
            else
            {
                this.OpenOrderMargin = openOrderMargin;
            }
            // to ensure "realTimeAccountBalance" is required (not null)
            if (realTimeAccountBalance == null)
            {
                throw new InvalidDataException("realTimeAccountBalance is a required property for AccountBalancesDefinitionInner and cannot be null");
            }
            else
            {
                this.RealTimeAccountBalance = realTimeAccountBalance;
            }
            // to ensure "realTimeBuyingPower" is required (not null)
            if (realTimeBuyingPower == null)
            {
                throw new InvalidDataException("realTimeBuyingPower is a required property for AccountBalancesDefinitionInner and cannot be null");
            }
            else
            {
                this.RealTimeBuyingPower = realTimeBuyingPower;
            }
            // to ensure "realTimeEquity" is required (not null)
            if (realTimeEquity == null)
            {
                throw new InvalidDataException("realTimeEquity is a required property for AccountBalancesDefinitionInner and cannot be null");
            }
            else
            {
                this.RealTimeEquity = realTimeEquity;
            }
            // to ensure "realTimeInitialMargin" is required (not null)
            if (realTimeInitialMargin == null)
            {
                throw new InvalidDataException("realTimeInitialMargin is a required property for AccountBalancesDefinitionInner and cannot be null");
            }
            else
            {
                this.RealTimeInitialMargin = realTimeInitialMargin;
            }
            // to ensure "realTimeMaintenanceMargin" is required (not null)
            if (realTimeMaintenanceMargin == null)
            {
                throw new InvalidDataException("realTimeMaintenanceMargin is a required property for AccountBalancesDefinitionInner and cannot be null");
            }
            else
            {
                this.RealTimeMaintenanceMargin = realTimeMaintenanceMargin;
            }
            // to ensure "realTimeRealizedProfitLoss" is required (not null)
            if (realTimeRealizedProfitLoss == null)
            {
                throw new InvalidDataException("realTimeRealizedProfitLoss is a required property for AccountBalancesDefinitionInner and cannot be null");
            }
            else
            {
                this.RealTimeRealizedProfitLoss = realTimeRealizedProfitLoss;
            }
            // to ensure "realTimeTradeEquity" is required (not null)
            if (realTimeTradeEquity == null)
            {
                throw new InvalidDataException("realTimeTradeEquity is a required property for AccountBalancesDefinitionInner and cannot be null");
            }
            else
            {
                this.RealTimeTradeEquity = realTimeTradeEquity;
            }
            // to ensure "realTimeUnrealizedGains" is required (not null)
            if (realTimeUnrealizedGains == null)
            {
                throw new InvalidDataException("realTimeUnrealizedGains is a required property for AccountBalancesDefinitionInner and cannot be null");
            }
            else
            {
                this.RealTimeUnrealizedGains = realTimeUnrealizedGains;
            }
            // to ensure "realTimeUnrealizedProfitLoss" is required (not null)
            if (realTimeUnrealizedProfitLoss == null)
            {
                throw new InvalidDataException("realTimeUnrealizedProfitLoss is a required property for AccountBalancesDefinitionInner and cannot be null");
            }
            else
            {
                this.RealTimeUnrealizedProfitLoss = realTimeUnrealizedProfitLoss;
            }
            // to ensure "securityOnDeposit" is required (not null)
            if (securityOnDeposit == null)
            {
                throw new InvalidDataException("securityOnDeposit is a required property for AccountBalancesDefinitionInner and cannot be null");
            }
            else
            {
                this.SecurityOnDeposit = securityOnDeposit;
            }
            // to ensure "status" is required (not null)
            if (status == null)
            {
                throw new InvalidDataException("status is a required property for AccountBalancesDefinitionInner and cannot be null");
            }
            else
            {
                this.Status = status;
            }
            // to ensure "statusDescription" is required (not null)
            if (statusDescription == null)
            {
                throw new InvalidDataException("statusDescription is a required property for AccountBalancesDefinitionInner and cannot be null");
            }
            else
            {
                this.StatusDescription = statusDescription;
            }
            // to ensure "todayRealTimeTradeEquity" is required (not null)
            if (todayRealTimeTradeEquity == null)
            {
                throw new InvalidDataException("todayRealTimeTradeEquity is a required property for AccountBalancesDefinitionInner and cannot be null");
            }
            else
            {
                this.TodayRealTimeTradeEquity = todayRealTimeTradeEquity;
            }
            // to ensure "type" is required (not null)
            if (type == null)
            {
                throw new InvalidDataException("type is a required property for AccountBalancesDefinitionInner and cannot be null");
            }
            else
            {
                this.Type = type;
            }
            // to ensure "typeDescription" is required (not null)
            if (typeDescription == null)
            {
                throw new InvalidDataException("typeDescription is a required property for AccountBalancesDefinitionInner and cannot be null");
            }
            else
            {
                this.TypeDescription = typeDescription;
            }
            // to ensure "unclearedDeposit" is required (not null)
            if (unclearedDeposit == null)
            {
                throw new InvalidDataException("unclearedDeposit is a required property for AccountBalancesDefinitionInner and cannot be null");
            }
            else
            {
                this.UnclearedDeposit = unclearedDeposit;
            }
            this.BODAccountBalance = bODAccountBalance;
            this.BODDayTradingMarginableEquitiesBuyingPower = bODDayTradingMarginableEquitiesBuyingPower;
            this.BODOptionBuyingPower = bODOptionBuyingPower;
            this.BODOptionValue = bODOptionValue;
            this.BODOvernightBuyingPower = bODOvernightBuyingPower;
            this.CanDayTrade = canDayTrade;
            this.DayTradeExcess = dayTradeExcess;
            this.DayTradeOpenOrderMargin = dayTradeOpenOrderMargin;
            this.DayTrades = dayTrades;
            this.DayTradingQualified = dayTradingQualified;
            this.OptionApprovalLevel = optionApprovalLevel;
            this.PatternDayTrader = patternDayTrader;
            this.RealTimeCostOfPositions = realTimeCostOfPositions;
            this.RealTimeDayTradeMargin = realTimeDayTradeMargin;
            this.RealTimeDayTradingMarginableEquitiesBuyingPower = realTimeDayTradingMarginableEquitiesBuyingPower;
            this.RealTimeOptionBuyingPower = realTimeOptionBuyingPower;
            this.RealTimeOptionValue = realTimeOptionValue;
            this.RealTimeOvernightBuyingPower = realTimeOvernightBuyingPower;
            this.UnsettledFund = unsettledFund;
        }
        
        /// <summary>
        /// A user specified name that identifies a TradeStation account.
        /// </summary>
        /// <value>A user specified name that identifies a TradeStation account.</value>
        [DataMember(Name="Alias", EmitDefaultValue=false)]
        public string Alias { get; set; }

        /// <summary>
        /// (Equities) Deprecated. The amount of cash in the account at the beginning of the day. Redundant with BODNetCash.
        /// </summary>
        /// <value>(Equities) Deprecated. The amount of cash in the account at the beginning of the day. Redundant with BODNetCash.</value>
        [DataMember(Name="BODAccountBalance", EmitDefaultValue=false)]
        public decimal? BODAccountBalance { get; set; }

        /// <summary>
        /// (Equities) The Intraday Buying Power (Day Trading Rule 431) with which the account started the trading day.
        /// </summary>
        /// <value>(Equities) The Intraday Buying Power (Day Trading Rule 431) with which the account started the trading day.</value>
        [DataMember(Name="BODDayTradingMarginableEquitiesBuyingPower", EmitDefaultValue=false)]
        public decimal? BODDayTradingMarginableEquitiesBuyingPower { get; set; }

        /// <summary>
        /// The total amount of equity with which you started the current trading day. Sum of the beginning day cash balance for your account and the market value of all positions brought into the current trading day (those that were held overnight) minus the outstanding margin debit balance for the account at the start of the current trading day. (Cash balance) + (Long market value) + (Short Credit) - (Margin Debit) + (Short Market Value). For cash accounts, also includes UnsettledFunds.  For futures accounts, also includes securities on deposit.
        /// </summary>
        /// <value>The total amount of equity with which you started the current trading day. Sum of the beginning day cash balance for your account and the market value of all positions brought into the current trading day (those that were held overnight) minus the outstanding margin debit balance for the account at the start of the current trading day. (Cash balance) + (Long market value) + (Short Credit) - (Margin Debit) + (Short Market Value). For cash accounts, also includes UnsettledFunds.  For futures accounts, also includes securities on deposit.</value>
        [DataMember(Name="BODEquity", EmitDefaultValue=false)]
        public decimal? BODEquity { get; set; }

        /// <summary>
        /// The amount of cash in the account at the beginning of the day.
        /// </summary>
        /// <value>The amount of cash in the account at the beginning of the day.</value>
        [DataMember(Name="BODNetCash", EmitDefaultValue=false)]
        public decimal? BODNetCash { get; set; }

        /// <summary>
        /// (Futures) Unrealized profit and loss at the beginning of the day.
        /// </summary>
        /// <value>(Futures) Unrealized profit and loss at the beginning of the day.</value>
        [DataMember(Name="BODOpenTradeEquity", EmitDefaultValue=false)]
        public decimal? BODOpenTradeEquity { get; set; }

        /// <summary>
        /// (Equities) Option buying power at the start of the trading day.
        /// </summary>
        /// <value>(Equities) Option buying power at the start of the trading day.</value>
        [DataMember(Name="BODOptionBuyingPower", EmitDefaultValue=false)]
        public decimal? BODOptionBuyingPower { get; set; }

        /// <summary>
        /// (Equities) Liquidation value of options at the start of the trading day.
        /// </summary>
        /// <value>(Equities) Liquidation value of options at the start of the trading day.</value>
        [DataMember(Name="BODOptionValue", EmitDefaultValue=false)]
        public decimal? BODOptionValue { get; set; }

        /// <summary>
        /// (Equities) Overnight Buying Power (Regulation T) at the start of the trading day.
        /// </summary>
        /// <value>(Equities) Overnight Buying Power (Regulation T) at the start of the trading day.</value>
        [DataMember(Name="BODOvernightBuyingPower", EmitDefaultValue=false)]
        public decimal? BODOvernightBuyingPower { get; set; }

        /// <summary>
        /// (Equities) Indicates whether day trading is allowed in the account.
        /// </summary>
        /// <value>(Equities) Indicates whether day trading is allowed in the account.</value>
        [DataMember(Name="CanDayTrade", EmitDefaultValue=false)]
        public bool? CanDayTrade { get; set; }

        /// <summary>
        /// Gets or Sets ClosedPositions
        /// </summary>
        [DataMember(Name="ClosedPositions", EmitDefaultValue=false)]
        public List<AccountBalancesDefinitionInnerClosedPositions> ClosedPositions { get; set; }

        /// <summary>
        /// (Futures) The brokerage commission cost and routing fees (if applicable) for a trade based on the number of shares or contracts.
        /// </summary>
        /// <value>(Futures) The brokerage commission cost and routing fees (if applicable) for a trade based on the number of shares or contracts.</value>
        [DataMember(Name="Commission", EmitDefaultValue=false)]
        public decimal? Commission { get; set; }

        /// <summary>
        /// (Equities) (Buying Power Available - Buying Power Used) / Buying Power Multiplier, (Futures) (Cash + UnrealizedGains) - Buying Power Used
        /// </summary>
        /// <value>(Equities) (Buying Power Available - Buying Power Used) / Buying Power Multiplier, (Futures) (Cash + UnrealizedGains) - Buying Power Used</value>
        [DataMember(Name="DayTradeExcess", EmitDefaultValue=false)]
        public decimal? DayTradeExcess { get; set; }

        /// <summary>
        /// (Futures) Money field representing the current amount of money reserved for open orders.
        /// </summary>
        /// <value>(Futures) Money field representing the current amount of money reserved for open orders.</value>
        [DataMember(Name="DayTradeOpenOrderMargin", EmitDefaultValue=false)]
        public decimal? DayTradeOpenOrderMargin { get; set; }

        /// <summary>
        /// (Equities) The number of day trades placed in the account within the previous 4 trading days. A day trade refers to buying then selling or selling short then buying to cover the same security on the same trading day.
        /// </summary>
        /// <value>(Equities) The number of day trades placed in the account within the previous 4 trading days. A day trade refers to buying then selling or selling short then buying to cover the same security on the same trading day.</value>
        [DataMember(Name="DayTrades", EmitDefaultValue=false)]
        public decimal? DayTrades { get; set; }

        /// <summary>
        /// (Equities) Indicates if the account is qualified to day trade as per compliance suitability in TradeStation.
        /// </summary>
        /// <value>(Equities) Indicates if the account is qualified to day trade as per compliance suitability in TradeStation.</value>
        [DataMember(Name="DayTradingQualified", EmitDefaultValue=false)]
        public bool? DayTradingQualified { get; set; }

        /// <summary>
        /// (Futures) The base currency of the symbol.
        /// </summary>
        /// <value>(Futures) The base currency of the symbol.</value>
        [DataMember(Name="Currency", EmitDefaultValue=false)]
        public string Currency { get; set; }

        /// <summary>
        /// (Futures) Currency details of the symbol.
        /// </summary>
        /// <value>(Futures) Currency details of the symbol.</value>
        [DataMember(Name="CurrencyDetails", EmitDefaultValue=false)]
        public List<AccountBalancesDefinitionInnerCurrencyDetails> CurrencyDetails { get; set; }

        /// <summary>
        /// The friendly name of the specific TradeStation account.
        /// </summary>
        /// <value>The friendly name of the specific TradeStation account.</value>
        [DataMember(Name="DisplayName", EmitDefaultValue=false)]
        public string DisplayName { get; set; }

        /// <summary>
        /// Key is the unique identifier for the requested account.
        /// </summary>
        /// <value>Key is the unique identifier for the requested account.</value>
        [DataMember(Name="Key", EmitDefaultValue=false)]
        public decimal? Key { get; set; }

        /// <summary>
        /// Market value of open positions.
        /// </summary>
        /// <value>Market value of open positions.</value>
        [DataMember(Name="MarketValue", EmitDefaultValue=false)]
        public decimal? MarketValue { get; set; }

        /// <summary>
        /// name of the account.
        /// </summary>
        /// <value>name of the account.</value>
        [DataMember(Name="Name", EmitDefaultValue=false)]
        public string Name { get; set; }

        /// <summary>
        /// (Equities) The option approval level will determine what options strategies you will be able to employ in the account. In general terms, the levels are defined as follows * Level 0 - No options trading allowed. * Level 1 - Writing of Covered Calls, Buying Protective Puts. * Level 2 - Level 1 + Buying Calls, Buying Puts, Writing Covered Puts. * Level 3 - level 2+ Stock Option Spreads, Index Option Spreads, Butterfly Spreads, Condor Spreads, Iron Butterfly Spreads, Iron Condor Spreads. * Level 4 - Level 3 + Writing of Naked Puts (Stock Options). * Level 5 - Level 4 + Writing of Naked Puts (Index Options), Writing of Naked Calls (Stock Options), Writing of Naked Calls (Index Options). 
        /// </summary>
        /// <value>(Equities) The option approval level will determine what options strategies you will be able to employ in the account. In general terms, the levels are defined as follows * Level 0 - No options trading allowed. * Level 1 - Writing of Covered Calls, Buying Protective Puts. * Level 2 - Level 1 + Buying Calls, Buying Puts, Writing Covered Puts. * Level 3 - level 2+ Stock Option Spreads, Index Option Spreads, Butterfly Spreads, Condor Spreads, Iron Butterfly Spreads, Iron Condor Spreads. * Level 4 - Level 3 + Writing of Naked Puts (Stock Options). * Level 5 - Level 4 + Writing of Naked Puts (Index Options), Writing of Naked Calls (Stock Options), Writing of Naked Calls (Index Options). </value>
        [DataMember(Name="OptionApprovalLevel", EmitDefaultValue=false)]
        public decimal? OptionApprovalLevel { get; set; }

        /// <summary>
        /// (Futures) The dollar amount of Open Order Margin for the given futures account.
        /// </summary>
        /// <value>(Futures) The dollar amount of Open Order Margin for the given futures account.</value>
        [DataMember(Name="OpenOrderMargin", EmitDefaultValue=false)]
        public decimal? OpenOrderMargin { get; set; }

        /// <summary>
        /// (Equities) Indicates whether you are considered a pattern day trader. As per FINRA rules, you will be considered a pattern day trader if you trade 4 or more times in 5 business days and your day-trading activities are greater than 6 percent of your total trading activity for that same five-day period. A pattern day trader must maintain a minimum equity of $25,000 on any day that the customer day trades. If the account falls below the $25,000 requirement, the pattern day trader will not be permitted to day trade until the account is restored to the $25,000 minimum equity level. 
        /// </summary>
        /// <value>(Equities) Indicates whether you are considered a pattern day trader. As per FINRA rules, you will be considered a pattern day trader if you trade 4 or more times in 5 business days and your day-trading activities are greater than 6 percent of your total trading activity for that same five-day period. A pattern day trader must maintain a minimum equity of $25,000 on any day that the customer day trades. If the account falls below the $25,000 requirement, the pattern day trader will not be permitted to day trade until the account is restored to the $25,000 minimum equity level. </value>
        [DataMember(Name="PatternDayTrader", EmitDefaultValue=false)]
        public bool? PatternDayTrader { get; set; }

        /// <summary>
        /// Current cash balance of the account.
        /// </summary>
        /// <value>Current cash balance of the account.</value>
        [DataMember(Name="RealTimeAccountBalance", EmitDefaultValue=false)]
        public decimal? RealTimeAccountBalance { get; set; }

        /// <summary>
        /// Indicates the value of real-time buying power.
        /// </summary>
        /// <value>Indicates the value of real-time buying power.</value>
        [DataMember(Name="RealTimeBuyingPower", EmitDefaultValue=false)]
        public decimal? RealTimeBuyingPower { get; set; }

        /// <summary>
        /// (Equities) Total real-time cost of all open positions. Positions are based on the actual entry price.
        /// </summary>
        /// <value>(Equities) Total real-time cost of all open positions. Positions are based on the actual entry price.</value>
        [DataMember(Name="RealTimeCostOfPositions", EmitDefaultValue=false)]
        public decimal? RealTimeCostOfPositions { get; set; }

        /// <summary>
        /// (Futures) Money field representing the current total amount of futures day trade margin.
        /// </summary>
        /// <value>(Futures) Money field representing the current total amount of futures day trade margin.</value>
        [DataMember(Name="RealTimeDayTradeMargin", EmitDefaultValue=false)]
        public decimal? RealTimeDayTradeMargin { get; set; }

        /// <summary>
        /// (Equities) The intraday buying power for trading marginable equities.
        /// </summary>
        /// <value>(Equities) The intraday buying power for trading marginable equities.</value>
        [DataMember(Name="RealTimeDayTradingMarginableEquitiesBuyingPower", EmitDefaultValue=false)]
        public decimal? RealTimeDayTradingMarginableEquitiesBuyingPower { get; set; }

        /// <summary>
        /// The real-time cash reserves for your account. At the beginning of the trading day, this value will equal the beginning day cash balance. This figure is calculated by taking the real-time cash balance plus the market value of any long positions minus the market value of any short position. (Real-time cash balance) + (Market value of long positions) – (Market value of short positions) + (Liquidation value of options)
        /// </summary>
        /// <value>The real-time cash reserves for your account. At the beginning of the trading day, this value will equal the beginning day cash balance. This figure is calculated by taking the real-time cash balance plus the market value of any long positions minus the market value of any short position. (Real-time cash balance) + (Market value of long positions) – (Market value of short positions) + (Liquidation value of options)</value>
        [DataMember(Name="RealTimeEquity", EmitDefaultValue=false)]
        public decimal? RealTimeEquity { get; set; }

        /// <summary>
        /// (Futures) Sum (Initial Margins of all positions in the given account)
        /// </summary>
        /// <value>(Futures) Sum (Initial Margins of all positions in the given account)</value>
        [DataMember(Name="RealTimeInitialMargin", EmitDefaultValue=false)]
        public decimal? RealTimeInitialMargin { get; set; }

        /// <summary>
        /// (Equities) The intraday buying power for options.
        /// </summary>
        /// <value>(Equities) The intraday buying power for options.</value>
        [DataMember(Name="RealTimeOptionBuyingPower", EmitDefaultValue=false)]
        public decimal? RealTimeOptionBuyingPower { get; set; }

        /// <summary>
        /// (Equities) Intraday liquidation value of option positions.
        /// </summary>
        /// <value>(Equities) Intraday liquidation value of option positions.</value>
        [DataMember(Name="RealTimeOptionValue", EmitDefaultValue=false)]
        public decimal? RealTimeOptionValue { get; set; }

        /// <summary>
        /// (Equities) Real-time Overnight Marginable Equities Buying Power.
        /// </summary>
        /// <value>(Equities) Real-time Overnight Marginable Equities Buying Power.</value>
        [DataMember(Name="RealTimeOvernightBuyingPower", EmitDefaultValue=false)]
        public decimal? RealTimeOvernightBuyingPower { get; set; }

        /// <summary>
        /// (Futures) The dollar amount of Real-time Maintenance Margin for the given futures account.
        /// </summary>
        /// <value>(Futures) The dollar amount of Real-time Maintenance Margin for the given futures account.</value>
        [DataMember(Name="RealTimeMaintenanceMargin", EmitDefaultValue=false)]
        public decimal? RealTimeMaintenanceMargin { get; set; }

        /// <summary>
        /// Indicates any gain or loss as a result of closing a position during the current trading day. This value also includes all commissions and routing fees incurred during the current trading day.
        /// </summary>
        /// <value>Indicates any gain or loss as a result of closing a position during the current trading day. This value also includes all commissions and routing fees incurred during the current trading day.</value>
        [DataMember(Name="RealTimeRealizedProfitLoss", EmitDefaultValue=false)]
        public decimal? RealTimeRealizedProfitLoss { get; set; }

        /// <summary>
        /// (Futures) The dollar amount of unrealized profit and loss for the given futures account.  Same value as RealTimeUnrealizedGains.
        /// </summary>
        /// <value>(Futures) The dollar amount of unrealized profit and loss for the given futures account.  Same value as RealTimeUnrealizedGains.</value>
        [DataMember(Name="RealTimeTradeEquity", EmitDefaultValue=false)]
        public decimal? RealTimeTradeEquity { get; set; }

        /// <summary>
        /// Unrealized profit and loss, for the current trading day, of all open positions.
        /// </summary>
        /// <value>Unrealized profit and loss, for the current trading day, of all open positions.</value>
        [DataMember(Name="RealTimeUnrealizedGains", EmitDefaultValue=false)]
        public decimal? RealTimeUnrealizedGains { get; set; }

        /// <summary>
        /// Unrealized profit or loss of all open positions using current market prices.
        /// </summary>
        /// <value>Unrealized profit or loss of all open positions using current market prices.</value>
        [DataMember(Name="RealTimeUnrealizedProfitLoss", EmitDefaultValue=false)]
        public decimal? RealTimeUnrealizedProfitLoss { get; set; }

        /// <summary>
        /// (Futures) The value of special securities that are deposited by the customer with the clearing firm for the sole purpose of increasing purchasing power in their trading account. This number will be reset daily by the account balances clearing file. The entire value of this field will increase purchasing power.
        /// </summary>
        /// <value>(Futures) The value of special securities that are deposited by the customer with the clearing firm for the sole purpose of increasing purchasing power in their trading account. This number will be reset daily by the account balances clearing file. The entire value of this field will increase purchasing power.</value>
        [DataMember(Name="SecurityOnDeposit", EmitDefaultValue=false)]
        public decimal? SecurityOnDeposit { get; set; }


        /// <summary>
        /// String value for Status attribute.
        /// </summary>
        /// <value>String value for Status attribute.</value>
        [DataMember(Name="StatusDescription", EmitDefaultValue=false)]
        public string StatusDescription { get; set; }

        /// <summary>
        /// (Futures) The unrealized P/L for today. Unrealized P/L - BODOpenTradeEquity
        /// </summary>
        /// <value>(Futures) The unrealized P/L for today. Unrealized P/L - BODOpenTradeEquity</value>
        [DataMember(Name="TodayRealTimeTradeEquity", EmitDefaultValue=false)]
        public decimal? TodayRealTimeTradeEquity { get; set; }


        /// <summary>
        /// String value for Type Attribute.
        /// </summary>
        /// <value>String value for Type Attribute.</value>
        [DataMember(Name="TypeDescription", EmitDefaultValue=false)]
        public string TypeDescription { get; set; }

        /// <summary>
        /// The total of uncleared checks received by TradeStation for deposit.
        /// </summary>
        /// <value>The total of uncleared checks received by TradeStation for deposit.</value>
        [DataMember(Name="UnclearedDeposit", EmitDefaultValue=false)]
        public decimal? UnclearedDeposit { get; set; }

        /// <summary>
        /// (Equities) Funds received by TradeStation that are not settled from a transaction in the account.
        /// </summary>
        /// <value>(Equities) Funds received by TradeStation that are not settled from a transaction in the account.</value>
        [DataMember(Name="UnsettledFund", EmitDefaultValue=false)]
        public decimal? UnsettledFund { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class AccountBalancesDefinitionInner {\n");
            sb.Append("  Alias: ").Append(Alias).Append("\n");
            sb.Append("  BODAccountBalance: ").Append(BODAccountBalance).Append("\n");
            sb.Append("  BODDayTradingMarginableEquitiesBuyingPower: ").Append(BODDayTradingMarginableEquitiesBuyingPower).Append("\n");
            sb.Append("  BODEquity: ").Append(BODEquity).Append("\n");
            sb.Append("  BODNetCash: ").Append(BODNetCash).Append("\n");
            sb.Append("  BODOpenTradeEquity: ").Append(BODOpenTradeEquity).Append("\n");
            sb.Append("  BODOptionBuyingPower: ").Append(BODOptionBuyingPower).Append("\n");
            sb.Append("  BODOptionValue: ").Append(BODOptionValue).Append("\n");
            sb.Append("  BODOvernightBuyingPower: ").Append(BODOvernightBuyingPower).Append("\n");
            sb.Append("  CanDayTrade: ").Append(CanDayTrade).Append("\n");
            sb.Append("  ClosedPositions: ").Append(ClosedPositions).Append("\n");
            sb.Append("  Commission: ").Append(Commission).Append("\n");
            sb.Append("  DayTradeExcess: ").Append(DayTradeExcess).Append("\n");
            sb.Append("  DayTradeOpenOrderMargin: ").Append(DayTradeOpenOrderMargin).Append("\n");
            sb.Append("  DayTrades: ").Append(DayTrades).Append("\n");
            sb.Append("  DayTradingQualified: ").Append(DayTradingQualified).Append("\n");
            sb.Append("  Currency: ").Append(Currency).Append("\n");
            sb.Append("  CurrencyDetails: ").Append(CurrencyDetails).Append("\n");
            sb.Append("  DisplayName: ").Append(DisplayName).Append("\n");
            sb.Append("  Key: ").Append(Key).Append("\n");
            sb.Append("  MarketValue: ").Append(MarketValue).Append("\n");
            sb.Append("  Name: ").Append(Name).Append("\n");
            sb.Append("  OptionApprovalLevel: ").Append(OptionApprovalLevel).Append("\n");
            sb.Append("  OpenOrderMargin: ").Append(OpenOrderMargin).Append("\n");
            sb.Append("  PatternDayTrader: ").Append(PatternDayTrader).Append("\n");
            sb.Append("  RealTimeAccountBalance: ").Append(RealTimeAccountBalance).Append("\n");
            sb.Append("  RealTimeBuyingPower: ").Append(RealTimeBuyingPower).Append("\n");
            sb.Append("  RealTimeCostOfPositions: ").Append(RealTimeCostOfPositions).Append("\n");
            sb.Append("  RealTimeDayTradeMargin: ").Append(RealTimeDayTradeMargin).Append("\n");
            sb.Append("  RealTimeDayTradingMarginableEquitiesBuyingPower: ").Append(RealTimeDayTradingMarginableEquitiesBuyingPower).Append("\n");
            sb.Append("  RealTimeEquity: ").Append(RealTimeEquity).Append("\n");
            sb.Append("  RealTimeInitialMargin: ").Append(RealTimeInitialMargin).Append("\n");
            sb.Append("  RealTimeOptionBuyingPower: ").Append(RealTimeOptionBuyingPower).Append("\n");
            sb.Append("  RealTimeOptionValue: ").Append(RealTimeOptionValue).Append("\n");
            sb.Append("  RealTimeOvernightBuyingPower: ").Append(RealTimeOvernightBuyingPower).Append("\n");
            sb.Append("  RealTimeMaintenanceMargin: ").Append(RealTimeMaintenanceMargin).Append("\n");
            sb.Append("  RealTimeRealizedProfitLoss: ").Append(RealTimeRealizedProfitLoss).Append("\n");
            sb.Append("  RealTimeTradeEquity: ").Append(RealTimeTradeEquity).Append("\n");
            sb.Append("  RealTimeUnrealizedGains: ").Append(RealTimeUnrealizedGains).Append("\n");
            sb.Append("  RealTimeUnrealizedProfitLoss: ").Append(RealTimeUnrealizedProfitLoss).Append("\n");
            sb.Append("  SecurityOnDeposit: ").Append(SecurityOnDeposit).Append("\n");
            sb.Append("  Status: ").Append(Status).Append("\n");
            sb.Append("  StatusDescription: ").Append(StatusDescription).Append("\n");
            sb.Append("  TodayRealTimeTradeEquity: ").Append(TodayRealTimeTradeEquity).Append("\n");
            sb.Append("  Type: ").Append(Type).Append("\n");
            sb.Append("  TypeDescription: ").Append(TypeDescription).Append("\n");
            sb.Append("  UnclearedDeposit: ").Append(UnclearedDeposit).Append("\n");
            sb.Append("  UnsettledFund: ").Append(UnsettledFund).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }
  
        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public virtual string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// Returns true if objects are equal
        /// </summary>
        /// <param name="input">Object to be compared</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object input)
        {
            return this.Equals(input as AccountBalancesDefinitionInner);
        }

        /// <summary>
        /// Returns true if AccountBalancesDefinitionInner instances are equal
        /// </summary>
        /// <param name="input">Instance of AccountBalancesDefinitionInner to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(AccountBalancesDefinitionInner input)
        {
            if (input == null)
                return false;

            return 
                (
                    this.Alias == input.Alias ||
                    (this.Alias != null &&
                    this.Alias.Equals(input.Alias))
                ) && 
                (
                    this.BODAccountBalance == input.BODAccountBalance ||
                    (this.BODAccountBalance != null &&
                    this.BODAccountBalance.Equals(input.BODAccountBalance))
                ) && 
                (
                    this.BODDayTradingMarginableEquitiesBuyingPower == input.BODDayTradingMarginableEquitiesBuyingPower ||
                    (this.BODDayTradingMarginableEquitiesBuyingPower != null &&
                    this.BODDayTradingMarginableEquitiesBuyingPower.Equals(input.BODDayTradingMarginableEquitiesBuyingPower))
                ) && 
                (
                    this.BODEquity == input.BODEquity ||
                    (this.BODEquity != null &&
                    this.BODEquity.Equals(input.BODEquity))
                ) && 
                (
                    this.BODNetCash == input.BODNetCash ||
                    (this.BODNetCash != null &&
                    this.BODNetCash.Equals(input.BODNetCash))
                ) && 
                (
                    this.BODOpenTradeEquity == input.BODOpenTradeEquity ||
                    (this.BODOpenTradeEquity != null &&
                    this.BODOpenTradeEquity.Equals(input.BODOpenTradeEquity))
                ) && 
                (
                    this.BODOptionBuyingPower == input.BODOptionBuyingPower ||
                    (this.BODOptionBuyingPower != null &&
                    this.BODOptionBuyingPower.Equals(input.BODOptionBuyingPower))
                ) && 
                (
                    this.BODOptionValue == input.BODOptionValue ||
                    (this.BODOptionValue != null &&
                    this.BODOptionValue.Equals(input.BODOptionValue))
                ) && 
                (
                    this.BODOvernightBuyingPower == input.BODOvernightBuyingPower ||
                    (this.BODOvernightBuyingPower != null &&
                    this.BODOvernightBuyingPower.Equals(input.BODOvernightBuyingPower))
                ) && 
                (
                    this.CanDayTrade == input.CanDayTrade ||
                    (this.CanDayTrade != null &&
                    this.CanDayTrade.Equals(input.CanDayTrade))
                ) && 
                (
                    this.ClosedPositions == input.ClosedPositions ||
                    this.ClosedPositions != null &&
                    this.ClosedPositions.SequenceEqual(input.ClosedPositions)
                ) && 
                (
                    this.Commission == input.Commission ||
                    (this.Commission != null &&
                    this.Commission.Equals(input.Commission))
                ) && 
                (
                    this.DayTradeExcess == input.DayTradeExcess ||
                    (this.DayTradeExcess != null &&
                    this.DayTradeExcess.Equals(input.DayTradeExcess))
                ) && 
                (
                    this.DayTradeOpenOrderMargin == input.DayTradeOpenOrderMargin ||
                    (this.DayTradeOpenOrderMargin != null &&
                    this.DayTradeOpenOrderMargin.Equals(input.DayTradeOpenOrderMargin))
                ) && 
                (
                    this.DayTrades == input.DayTrades ||
                    (this.DayTrades != null &&
                    this.DayTrades.Equals(input.DayTrades))
                ) && 
                (
                    this.DayTradingQualified == input.DayTradingQualified ||
                    (this.DayTradingQualified != null &&
                    this.DayTradingQualified.Equals(input.DayTradingQualified))
                ) && 
                (
                    this.Currency == input.Currency ||
                    (this.Currency != null &&
                    this.Currency.Equals(input.Currency))
                ) && 
                (
                    this.CurrencyDetails == input.CurrencyDetails ||
                    this.CurrencyDetails != null &&
                    this.CurrencyDetails.SequenceEqual(input.CurrencyDetails)
                ) && 
                (
                    this.DisplayName == input.DisplayName ||
                    (this.DisplayName != null &&
                    this.DisplayName.Equals(input.DisplayName))
                ) && 
                (
                    this.Key == input.Key ||
                    (this.Key != null &&
                    this.Key.Equals(input.Key))
                ) && 
                (
                    this.MarketValue == input.MarketValue ||
                    (this.MarketValue != null &&
                    this.MarketValue.Equals(input.MarketValue))
                ) && 
                (
                    this.Name == input.Name ||
                    (this.Name != null &&
                    this.Name.Equals(input.Name))
                ) && 
                (
                    this.OptionApprovalLevel == input.OptionApprovalLevel ||
                    (this.OptionApprovalLevel != null &&
                    this.OptionApprovalLevel.Equals(input.OptionApprovalLevel))
                ) && 
                (
                    this.OpenOrderMargin == input.OpenOrderMargin ||
                    (this.OpenOrderMargin != null &&
                    this.OpenOrderMargin.Equals(input.OpenOrderMargin))
                ) && 
                (
                    this.PatternDayTrader == input.PatternDayTrader ||
                    (this.PatternDayTrader != null &&
                    this.PatternDayTrader.Equals(input.PatternDayTrader))
                ) && 
                (
                    this.RealTimeAccountBalance == input.RealTimeAccountBalance ||
                    (this.RealTimeAccountBalance != null &&
                    this.RealTimeAccountBalance.Equals(input.RealTimeAccountBalance))
                ) && 
                (
                    this.RealTimeBuyingPower == input.RealTimeBuyingPower ||
                    (this.RealTimeBuyingPower != null &&
                    this.RealTimeBuyingPower.Equals(input.RealTimeBuyingPower))
                ) && 
                (
                    this.RealTimeCostOfPositions == input.RealTimeCostOfPositions ||
                    (this.RealTimeCostOfPositions != null &&
                    this.RealTimeCostOfPositions.Equals(input.RealTimeCostOfPositions))
                ) && 
                (
                    this.RealTimeDayTradeMargin == input.RealTimeDayTradeMargin ||
                    (this.RealTimeDayTradeMargin != null &&
                    this.RealTimeDayTradeMargin.Equals(input.RealTimeDayTradeMargin))
                ) && 
                (
                    this.RealTimeDayTradingMarginableEquitiesBuyingPower == input.RealTimeDayTradingMarginableEquitiesBuyingPower ||
                    (this.RealTimeDayTradingMarginableEquitiesBuyingPower != null &&
                    this.RealTimeDayTradingMarginableEquitiesBuyingPower.Equals(input.RealTimeDayTradingMarginableEquitiesBuyingPower))
                ) && 
                (
                    this.RealTimeEquity == input.RealTimeEquity ||
                    (this.RealTimeEquity != null &&
                    this.RealTimeEquity.Equals(input.RealTimeEquity))
                ) && 
                (
                    this.RealTimeInitialMargin == input.RealTimeInitialMargin ||
                    (this.RealTimeInitialMargin != null &&
                    this.RealTimeInitialMargin.Equals(input.RealTimeInitialMargin))
                ) && 
                (
                    this.RealTimeOptionBuyingPower == input.RealTimeOptionBuyingPower ||
                    (this.RealTimeOptionBuyingPower != null &&
                    this.RealTimeOptionBuyingPower.Equals(input.RealTimeOptionBuyingPower))
                ) && 
                (
                    this.RealTimeOptionValue == input.RealTimeOptionValue ||
                    (this.RealTimeOptionValue != null &&
                    this.RealTimeOptionValue.Equals(input.RealTimeOptionValue))
                ) && 
                (
                    this.RealTimeOvernightBuyingPower == input.RealTimeOvernightBuyingPower ||
                    (this.RealTimeOvernightBuyingPower != null &&
                    this.RealTimeOvernightBuyingPower.Equals(input.RealTimeOvernightBuyingPower))
                ) && 
                (
                    this.RealTimeMaintenanceMargin == input.RealTimeMaintenanceMargin ||
                    (this.RealTimeMaintenanceMargin != null &&
                    this.RealTimeMaintenanceMargin.Equals(input.RealTimeMaintenanceMargin))
                ) && 
                (
                    this.RealTimeRealizedProfitLoss == input.RealTimeRealizedProfitLoss ||
                    (this.RealTimeRealizedProfitLoss != null &&
                    this.RealTimeRealizedProfitLoss.Equals(input.RealTimeRealizedProfitLoss))
                ) && 
                (
                    this.RealTimeTradeEquity == input.RealTimeTradeEquity ||
                    (this.RealTimeTradeEquity != null &&
                    this.RealTimeTradeEquity.Equals(input.RealTimeTradeEquity))
                ) && 
                (
                    this.RealTimeUnrealizedGains == input.RealTimeUnrealizedGains ||
                    (this.RealTimeUnrealizedGains != null &&
                    this.RealTimeUnrealizedGains.Equals(input.RealTimeUnrealizedGains))
                ) && 
                (
                    this.RealTimeUnrealizedProfitLoss == input.RealTimeUnrealizedProfitLoss ||
                    (this.RealTimeUnrealizedProfitLoss != null &&
                    this.RealTimeUnrealizedProfitLoss.Equals(input.RealTimeUnrealizedProfitLoss))
                ) && 
                (
                    this.SecurityOnDeposit == input.SecurityOnDeposit ||
                    (this.SecurityOnDeposit != null &&
                    this.SecurityOnDeposit.Equals(input.SecurityOnDeposit))
                ) && 
                (
                    this.Status == input.Status ||
                    (this.Status != null &&
                    this.Status.Equals(input.Status))
                ) && 
                (
                    this.StatusDescription == input.StatusDescription ||
                    (this.StatusDescription != null &&
                    this.StatusDescription.Equals(input.StatusDescription))
                ) && 
                (
                    this.TodayRealTimeTradeEquity == input.TodayRealTimeTradeEquity ||
                    (this.TodayRealTimeTradeEquity != null &&
                    this.TodayRealTimeTradeEquity.Equals(input.TodayRealTimeTradeEquity))
                ) && 
                (
                    this.Type == input.Type ||
                    (this.Type != null &&
                    this.Type.Equals(input.Type))
                ) && 
                (
                    this.TypeDescription == input.TypeDescription ||
                    (this.TypeDescription != null &&
                    this.TypeDescription.Equals(input.TypeDescription))
                ) && 
                (
                    this.UnclearedDeposit == input.UnclearedDeposit ||
                    (this.UnclearedDeposit != null &&
                    this.UnclearedDeposit.Equals(input.UnclearedDeposit))
                ) && 
                (
                    this.UnsettledFund == input.UnsettledFund ||
                    (this.UnsettledFund != null &&
                    this.UnsettledFund.Equals(input.UnsettledFund))
                );
        }

        /// <summary>
        /// Gets the hash code
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hashCode = 41;
                if (this.Alias != null)
                    hashCode = hashCode * 59 + this.Alias.GetHashCode();
                if (this.BODAccountBalance != null)
                    hashCode = hashCode * 59 + this.BODAccountBalance.GetHashCode();
                if (this.BODDayTradingMarginableEquitiesBuyingPower != null)
                    hashCode = hashCode * 59 + this.BODDayTradingMarginableEquitiesBuyingPower.GetHashCode();
                if (this.BODEquity != null)
                    hashCode = hashCode * 59 + this.BODEquity.GetHashCode();
                if (this.BODNetCash != null)
                    hashCode = hashCode * 59 + this.BODNetCash.GetHashCode();
                if (this.BODOpenTradeEquity != null)
                    hashCode = hashCode * 59 + this.BODOpenTradeEquity.GetHashCode();
                if (this.BODOptionBuyingPower != null)
                    hashCode = hashCode * 59 + this.BODOptionBuyingPower.GetHashCode();
                if (this.BODOptionValue != null)
                    hashCode = hashCode * 59 + this.BODOptionValue.GetHashCode();
                if (this.BODOvernightBuyingPower != null)
                    hashCode = hashCode * 59 + this.BODOvernightBuyingPower.GetHashCode();
                if (this.CanDayTrade != null)
                    hashCode = hashCode * 59 + this.CanDayTrade.GetHashCode();
                if (this.ClosedPositions != null)
                    hashCode = hashCode * 59 + this.ClosedPositions.GetHashCode();
                if (this.Commission != null)
                    hashCode = hashCode * 59 + this.Commission.GetHashCode();
                if (this.DayTradeExcess != null)
                    hashCode = hashCode * 59 + this.DayTradeExcess.GetHashCode();
                if (this.DayTradeOpenOrderMargin != null)
                    hashCode = hashCode * 59 + this.DayTradeOpenOrderMargin.GetHashCode();
                if (this.DayTrades != null)
                    hashCode = hashCode * 59 + this.DayTrades.GetHashCode();
                if (this.DayTradingQualified != null)
                    hashCode = hashCode * 59 + this.DayTradingQualified.GetHashCode();
                if (this.Currency != null)
                    hashCode = hashCode * 59 + this.Currency.GetHashCode();
                if (this.CurrencyDetails != null)
                    hashCode = hashCode * 59 + this.CurrencyDetails.GetHashCode();
                if (this.DisplayName != null)
                    hashCode = hashCode * 59 + this.DisplayName.GetHashCode();
                if (this.Key != null)
                    hashCode = hashCode * 59 + this.Key.GetHashCode();
                if (this.MarketValue != null)
                    hashCode = hashCode * 59 + this.MarketValue.GetHashCode();
                if (this.Name != null)
                    hashCode = hashCode * 59 + this.Name.GetHashCode();
                if (this.OptionApprovalLevel != null)
                    hashCode = hashCode * 59 + this.OptionApprovalLevel.GetHashCode();
                if (this.OpenOrderMargin != null)
                    hashCode = hashCode * 59 + this.OpenOrderMargin.GetHashCode();
                if (this.PatternDayTrader != null)
                    hashCode = hashCode * 59 + this.PatternDayTrader.GetHashCode();
                if (this.RealTimeAccountBalance != null)
                    hashCode = hashCode * 59 + this.RealTimeAccountBalance.GetHashCode();
                if (this.RealTimeBuyingPower != null)
                    hashCode = hashCode * 59 + this.RealTimeBuyingPower.GetHashCode();
                if (this.RealTimeCostOfPositions != null)
                    hashCode = hashCode * 59 + this.RealTimeCostOfPositions.GetHashCode();
                if (this.RealTimeDayTradeMargin != null)
                    hashCode = hashCode * 59 + this.RealTimeDayTradeMargin.GetHashCode();
                if (this.RealTimeDayTradingMarginableEquitiesBuyingPower != null)
                    hashCode = hashCode * 59 + this.RealTimeDayTradingMarginableEquitiesBuyingPower.GetHashCode();
                if (this.RealTimeEquity != null)
                    hashCode = hashCode * 59 + this.RealTimeEquity.GetHashCode();
                if (this.RealTimeInitialMargin != null)
                    hashCode = hashCode * 59 + this.RealTimeInitialMargin.GetHashCode();
                if (this.RealTimeOptionBuyingPower != null)
                    hashCode = hashCode * 59 + this.RealTimeOptionBuyingPower.GetHashCode();
                if (this.RealTimeOptionValue != null)
                    hashCode = hashCode * 59 + this.RealTimeOptionValue.GetHashCode();
                if (this.RealTimeOvernightBuyingPower != null)
                    hashCode = hashCode * 59 + this.RealTimeOvernightBuyingPower.GetHashCode();
                if (this.RealTimeMaintenanceMargin != null)
                    hashCode = hashCode * 59 + this.RealTimeMaintenanceMargin.GetHashCode();
                if (this.RealTimeRealizedProfitLoss != null)
                    hashCode = hashCode * 59 + this.RealTimeRealizedProfitLoss.GetHashCode();
                if (this.RealTimeTradeEquity != null)
                    hashCode = hashCode * 59 + this.RealTimeTradeEquity.GetHashCode();
                if (this.RealTimeUnrealizedGains != null)
                    hashCode = hashCode * 59 + this.RealTimeUnrealizedGains.GetHashCode();
                if (this.RealTimeUnrealizedProfitLoss != null)
                    hashCode = hashCode * 59 + this.RealTimeUnrealizedProfitLoss.GetHashCode();
                if (this.SecurityOnDeposit != null)
                    hashCode = hashCode * 59 + this.SecurityOnDeposit.GetHashCode();
                if (this.Status != null)
                    hashCode = hashCode * 59 + this.Status.GetHashCode();
                if (this.StatusDescription != null)
                    hashCode = hashCode * 59 + this.StatusDescription.GetHashCode();
                if (this.TodayRealTimeTradeEquity != null)
                    hashCode = hashCode * 59 + this.TodayRealTimeTradeEquity.GetHashCode();
                if (this.Type != null)
                    hashCode = hashCode * 59 + this.Type.GetHashCode();
                if (this.TypeDescription != null)
                    hashCode = hashCode * 59 + this.TypeDescription.GetHashCode();
                if (this.UnclearedDeposit != null)
                    hashCode = hashCode * 59 + this.UnclearedDeposit.GetHashCode();
                if (this.UnsettledFund != null)
                    hashCode = hashCode * 59 + this.UnsettledFund.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// To validate all properties of the instance
        /// </summary>
        /// <param name="validationContext">Validation context</param>
        /// <returns>Validation Result</returns>
        IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            // Currency (string) minLength
            if(this.Currency != null && this.Currency.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Currency, length must be greater than 1.", new [] { "Currency" });
            }

            // DisplayName (string) minLength
            if(this.DisplayName != null && this.DisplayName.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for DisplayName, length must be greater than 1.", new [] { "DisplayName" });
            }

            // Name (string) minLength
            if(this.Name != null && this.Name.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Name, length must be greater than 1.", new [] { "Name" });
            }

            // Status (string) minLength
            //if(this.Status != null && this.Status.Length < 1)
            //{
            //    yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Status, length must be greater than 1.", new [] { "Status" });
            //}

            // StatusDescription (string) minLength
            if(this.StatusDescription != null && this.StatusDescription.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for StatusDescription, length must be greater than 1.", new [] { "StatusDescription" });
            }

            // Type (string) minLength
            //if(this.Type != null && this.Type.Length < 1)
            //{
            //    yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Type, length must be greater than 1.", new [] { "Type" });
            //}

            // TypeDescription (string) minLength
            if(this.TypeDescription != null && this.TypeDescription.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for TypeDescription, length must be greater than 1.", new [] { "TypeDescription" });
            }

            yield break;
        }
    }

}
