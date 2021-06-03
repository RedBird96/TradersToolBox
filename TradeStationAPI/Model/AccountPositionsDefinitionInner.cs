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
    /// AccountPositionsDefinitionInner
    /// </summary>
    [DataContract]
    public partial class AccountPositionsDefinitionInner :  IEquatable<AccountPositionsDefinitionInner>, IValidatableObject
    {
        /// <summary>
        /// Indicates the asset type of the position * EQ - Equity * OP - Option * Fu - Future 
        /// </summary>
        /// <value>Indicates the asset type of the position * EQ - Equity * OP - Option * Fu - Future </value>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum AssetTypeEnum
        {
            
            /// <summary>
            /// Enum EQ for value: EQ
            /// </summary>
            [EnumMember(Value = "EQ")]
            EQ = 1,
            
            /// <summary>
            /// Enum Op for value: Op
            /// </summary>
            [EnumMember(Value = "Op")]
            Op = 2,
            
            /// <summary>
            /// Enum Fu for value: Fu
            /// </summary>
            [EnumMember(Value = "Fu")]
            Fu = 3
        }

        /// <summary>
        /// Indicates the asset type of the position * EQ - Equity * OP - Option * Fu - Future 
        /// </summary>
        /// <value>Indicates the asset type of the position * EQ - Equity * OP - Option * Fu - Future </value>
        [DataMember(Name="AssetType", EmitDefaultValue=false)]
        public AssetTypeEnum AssetType { get; set; }
        /// <summary>
        /// Specifies if the position is Long or Short. * A Long position is when the holder buys an option to open a position, and where the number or price of options bought exceeds the number or price of options sold. * A Short position is when the writer sells an option to open a position, and where the number or price of options sold exceeds the number or price of options bought. 
        /// </summary>
        /// <value>Specifies if the position is Long or Short. * A Long position is when the holder buys an option to open a position, and where the number or price of options bought exceeds the number or price of options sold. * A Short position is when the writer sells an option to open a position, and where the number or price of options sold exceeds the number or price of options bought. </value>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum LongShortEnum
        {
            
            /// <summary>
            /// Enum Long for value: Long
            /// </summary>
            [EnumMember(Value = "Long")]
            Long = 1,
            
            /// <summary>
            /// Enum Short for value: Short
            /// </summary>
            [EnumMember(Value = "Short")]
            Short = 2
        }

        /// <summary>
        /// Specifies if the position is Long or Short. * A Long position is when the holder buys an option to open a position, and where the number or price of options bought exceeds the number or price of options sold. * A Short position is when the writer sells an option to open a position, and where the number or price of options sold exceeds the number or price of options bought. 
        /// </summary>
        /// <value>Specifies if the position is Long or Short. * A Long position is when the holder buys an option to open a position, and where the number or price of options bought exceeds the number or price of options sold. * A Short position is when the writer sells an option to open a position, and where the number or price of options sold exceeds the number or price of options bought. </value>
        [DataMember(Name="LongShort", EmitDefaultValue=false)]
        public LongShortEnum LongShort { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="AccountPositionsDefinitionInner" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected AccountPositionsDefinitionInner() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="AccountPositionsDefinitionInner" /> class.
        /// </summary>
        /// <param name="accountID">TradeStation account that holds the position. (required).</param>
        /// <param name="accountMarketValue">The actual market value denominated in the account currency of the open position. (required).</param>
        /// <param name="accountOpenProfitLoss">The unrealized profit or loss denominated in the account currency on the position held, calculated based on the average price of the position. (required).</param>
        /// <param name="accountTotalCost">The total cost denominated in the account currency of the open position. (required).</param>
        /// <param name="alias">A user specified name that identifies a TradeStation account. (required).</param>
        /// <param name="askPrice">The price at which a security, futures contract, or other financial instrument is offered for sale. (required).</param>
        /// <param name="askPriceDisplay">Formatted text representation of the AskPrice for easy display (required).</param>
        /// <param name="assetType">Indicates the asset type of the position * EQ - Equity * OP - Option * Fu - Future  (required).</param>
        /// <param name="averagePrice">Average price of all positions for the current symbol. (required).</param>
        /// <param name="averagePriceDisplay">String value for AveragePrice attribute. (required).</param>
        /// <param name="bidPrice">The highest price a prospective buyer is prepared to pay at a particular time for a trading unit of a given symbol. (required).</param>
        /// <param name="bidPriceDisplay">Formatted text representation of the BidPrice for easy display (required).</param>
        /// <param name="bigPointValue">Dollar value for a one point movement. (required).</param>
        /// <param name="contractExpireDate">Contract expiration date for positions specified in contracts. (required).</param>
        /// <param name="conversionRate">The currency conversion rate that is used in order to convert from the currency of the symbol to the currency of the account. (required).</param>
        /// <param name="costBasisCalculation">Only applies to margin accounts based in Japan. When set to &#39;FSA&#39; an additional property, UnrealizedExpenses, will be included in the payload.  For other account types, the value will always be &#39;None&#39; (required).</param>
        /// <param name="country">The country of the exchange where the symbol is listed. (required).</param>
        /// <param name="currency">The base currency of the symbol. (required).</param>
        /// <param name="dayTradeMargin">(Futures) DayTradeMargin used on open positions.  Currently only calculated for futures positions.  Other asset classes will have a 0 for this value..</param>
        /// <param name="description">Displays the full name of the symbol. (required).</param>
        /// <param name="displayName">DO NOT USE - Marked for deprecation. (required).</param>
        /// <param name="initialMargin">The margin account balance denominated in the account currency required for entering a position on margin. (required).</param>
        /// <param name="key">A unique identifier for the position. (required).</param>
        /// <param name="lastPrice">The last price at which the symbol traded. (required).</param>
        /// <param name="lastPriceDisplay">Formatted text representation of the LastPrice for easy display (required).</param>
        /// <param name="longShort">Specifies if the position is Long or Short. * A Long position is when the holder buys an option to open a position, and where the number or price of options bought exceeds the number or price of options sold. * A Short position is when the writer sells an option to open a position, and where the number or price of options sold exceeds the number or price of options bought.  (required).</param>
        /// <param name="maintenanceMargin">The margin account balance denominated in the account currency required for maintaining a position on margin. (required).</param>
        /// <param name="marketValue">The actual market value denominated in the symbol currency of the open position. This value is updated in real-time. (required).</param>
        /// <param name="markToMarketPrice">Only applies to equity and option positions.  When AssetType is &#39;EQ&#39; or &#39;OP&#39;, this value will be included in the payload to convey the cost-basis price for a position. This is used to calculate the profit and loss incurred during the current session. (required).</param>
        /// <param name="openProfitLoss">The unrealized profit or loss denominated in the symbol currency on the position held, calculated based on the average price of the position. (required).</param>
        /// <param name="openProfitLossPercent">The unrealized profit or loss on the position expressed as a percentage of the initial value of the position. (required).</param>
        /// <param name="openProfitLossQty">The unrealized profit or loss denominated in the account currency divided by the number of shares, contracts or units held. (required).</param>
        /// <param name="quantity">The requested number of shares or contracts for a particular order. (required).</param>
        /// <param name="requiredMargin">(Forex) The margin account balance denominated in the account currency required for entering and maintaining a position on margin. (required).</param>
        /// <param name="settlePrice">The average price at which a contract trades, calculated at both the open and close of each trading day, and it is important because it determines whether a trader is required to post additional margins. (required).</param>
        /// <param name="strikePrice">The strike price is the stated price per share or per contract for which the underlying asset may be purchased, or sold, by the option holder upon exercise of the option contract. (required).</param>
        /// <param name="strikePriceDisplay">Formatted text representation of the StrikePrice for easy display (required).</param>
        /// <param name="symbol">Symbol of the current position. (required).</param>
        /// <param name="timeStamp">Time the position was placed. (required).</param>
        /// <param name="todaysProfitLoss">Only applies to equity and option positions.  When AssetType is &#39;EQ&#39; or &#39;OP&#39;, this value will be included in the payload to convey the unrealized profit or loss denominated in the account currency on the position held, calculated using the MarkToMarketPrice. (required).</param>
        /// <param name="totalCost">The total cost denominated in the account currency of the open position. (required).</param>
        /// <param name="unrealizedExpenses">Only applies to margin accounts based in Japan.  When CostBasisCalculation is to &#39;FSA&#39;, UnrealizedExpenses, will be included in the payload, and will convey interest expenses and buy order commissions..</param>
        public AccountPositionsDefinitionInner(string accountID = default(string), decimal? accountMarketValue = default(decimal?), decimal? accountOpenProfitLoss = default(decimal?), decimal? accountTotalCost = default(decimal?), string alias = default(string), decimal? askPrice = default(decimal?), string askPriceDisplay = default(string), AssetTypeEnum assetType = default(AssetTypeEnum), decimal? averagePrice = default(decimal?), string averagePriceDisplay = default(string), decimal? bidPrice = default(decimal?), string bidPriceDisplay = default(string), decimal? bigPointValue = default(decimal?), string contractExpireDate = default(string), decimal? conversionRate = default(decimal?), string costBasisCalculation = default(string), string country = default(string), string currency = default(string), string dayTradeMargin = default(string), string description = default(string), string displayName = default(string), decimal? initialMargin = default(decimal?), decimal? key = default(decimal?), decimal? lastPrice = default(decimal?), string lastPriceDisplay = default(string), LongShortEnum longShort = default(LongShortEnum), decimal? maintenanceMargin = default(decimal?), decimal? marketValue = default(decimal?), decimal? markToMarketPrice = default(decimal?), decimal? openProfitLoss = default(decimal?), decimal? openProfitLossPercent = default(decimal?), decimal? openProfitLossQty = default(decimal?), decimal? quantity = default(decimal?), decimal? requiredMargin = default(decimal?), decimal? settlePrice = default(decimal?), decimal? strikePrice = default(decimal?), string strikePriceDisplay = default(string), string symbol = default(string), string timeStamp = default(string), decimal? todaysProfitLoss = default(decimal?), decimal? totalCost = default(decimal?), decimal? unrealizedExpenses = default(decimal?))
        {
            // to ensure "accountID" is required (not null)
            if (accountID == null)
            {
                throw new InvalidDataException("accountID is a required property for AccountPositionsDefinitionInner and cannot be null");
            }
            else
            {
                this.AccountID = accountID;
            }
            // to ensure "accountMarketValue" is required (not null)
            if (accountMarketValue == null)
            {
                throw new InvalidDataException("accountMarketValue is a required property for AccountPositionsDefinitionInner and cannot be null");
            }
            else
            {
                this.AccountMarketValue = accountMarketValue;
            }
            // to ensure "accountOpenProfitLoss" is required (not null)
            if (accountOpenProfitLoss == null)
            {
                throw new InvalidDataException("accountOpenProfitLoss is a required property for AccountPositionsDefinitionInner and cannot be null");
            }
            else
            {
                this.AccountOpenProfitLoss = accountOpenProfitLoss;
            }
            // to ensure "accountTotalCost" is required (not null)
            if (accountTotalCost == null)
            {
                throw new InvalidDataException("accountTotalCost is a required property for AccountPositionsDefinitionInner and cannot be null");
            }
            else
            {
                this.AccountTotalCost = accountTotalCost;
            }
            // to ensure "alias" is required (not null)
            if (alias == null)
            {
                throw new InvalidDataException("alias is a required property for AccountPositionsDefinitionInner and cannot be null");
            }
            else
            {
                this.Alias = alias;
            }
            // to ensure "askPrice" is required (not null)
            if (askPrice == null)
            {
                throw new InvalidDataException("askPrice is a required property for AccountPositionsDefinitionInner and cannot be null");
            }
            else
            {
                this.AskPrice = askPrice;
            }
            // to ensure "askPriceDisplay" is required (not null)
            if (askPriceDisplay == null)
            {
                throw new InvalidDataException("askPriceDisplay is a required property for AccountPositionsDefinitionInner and cannot be null");
            }
            else
            {
                this.AskPriceDisplay = askPriceDisplay;
            }
            // to ensure "assetType" is required (not null)
            if (assetType == null)
            {
                throw new InvalidDataException("assetType is a required property for AccountPositionsDefinitionInner and cannot be null");
            }
            else
            {
                this.AssetType = assetType;
            }
            // to ensure "averagePrice" is required (not null)
            if (averagePrice == null)
            {
                throw new InvalidDataException("averagePrice is a required property for AccountPositionsDefinitionInner and cannot be null");
            }
            else
            {
                this.AveragePrice = averagePrice;
            }
            // to ensure "averagePriceDisplay" is required (not null)
            if (averagePriceDisplay == null)
            {
                throw new InvalidDataException("averagePriceDisplay is a required property for AccountPositionsDefinitionInner and cannot be null");
            }
            else
            {
                this.AveragePriceDisplay = averagePriceDisplay;
            }
            // to ensure "bidPrice" is required (not null)
            if (bidPrice == null)
            {
                throw new InvalidDataException("bidPrice is a required property for AccountPositionsDefinitionInner and cannot be null");
            }
            else
            {
                this.BidPrice = bidPrice;
            }
            // to ensure "bidPriceDisplay" is required (not null)
            if (bidPriceDisplay == null)
            {
                throw new InvalidDataException("bidPriceDisplay is a required property for AccountPositionsDefinitionInner and cannot be null");
            }
            else
            {
                this.BidPriceDisplay = bidPriceDisplay;
            }
            // to ensure "bigPointValue" is required (not null)
            if (bigPointValue == null)
            {
                throw new InvalidDataException("bigPointValue is a required property for AccountPositionsDefinitionInner and cannot be null");
            }
            else
            {
                this.BigPointValue = bigPointValue;
            }
            // to ensure "contractExpireDate" is required (not null)
            if (contractExpireDate == null)
            {
                throw new InvalidDataException("contractExpireDate is a required property for AccountPositionsDefinitionInner and cannot be null");
            }
            else
            {
                this.ContractExpireDate = contractExpireDate;
            }
            // to ensure "conversionRate" is required (not null)
            if (conversionRate == null)
            {
                throw new InvalidDataException("conversionRate is a required property for AccountPositionsDefinitionInner and cannot be null");
            }
            else
            {
                this.ConversionRate = conversionRate;
            }
            // to ensure "costBasisCalculation" is required (not null)
            if (costBasisCalculation == null)
            {
                throw new InvalidDataException("costBasisCalculation is a required property for AccountPositionsDefinitionInner and cannot be null");
            }
            else
            {
                this.CostBasisCalculation = costBasisCalculation;
            }
            // to ensure "country" is required (not null)
            if (country == null)
            {
                throw new InvalidDataException("country is a required property for AccountPositionsDefinitionInner and cannot be null");
            }
            else
            {
                this.Country = country;
            }
            // to ensure "currency" is required (not null)
            if (currency == null)
            {
                throw new InvalidDataException("currency is a required property for AccountPositionsDefinitionInner and cannot be null");
            }
            else
            {
                this.Currency = currency;
            }
            // to ensure "description" is required (not null)
            if (description == null)
            {
                throw new InvalidDataException("description is a required property for AccountPositionsDefinitionInner and cannot be null");
            }
            else
            {
                this.Description = description;
            }
            // to ensure "displayName" is required (not null)
            if (displayName == null)
            {
                throw new InvalidDataException("displayName is a required property for AccountPositionsDefinitionInner and cannot be null");
            }
            else
            {
                this.DisplayName = displayName;
            }
            // to ensure "initialMargin" is required (not null)
            if (initialMargin == null)
            {
                throw new InvalidDataException("initialMargin is a required property for AccountPositionsDefinitionInner and cannot be null");
            }
            else
            {
                this.InitialMargin = initialMargin;
            }
            // to ensure "key" is required (not null)
            if (key == null)
            {
                throw new InvalidDataException("key is a required property for AccountPositionsDefinitionInner and cannot be null");
            }
            else
            {
                this.Key = key;
            }
            // to ensure "lastPrice" is required (not null)
            if (lastPrice == null)
            {
                throw new InvalidDataException("lastPrice is a required property for AccountPositionsDefinitionInner and cannot be null");
            }
            else
            {
                this.LastPrice = lastPrice;
            }
            // to ensure "lastPriceDisplay" is required (not null)
            if (lastPriceDisplay == null)
            {
                throw new InvalidDataException("lastPriceDisplay is a required property for AccountPositionsDefinitionInner and cannot be null");
            }
            else
            {
                this.LastPriceDisplay = lastPriceDisplay;
            }
            // to ensure "longShort" is required (not null)
            if (longShort == null)
            {
                throw new InvalidDataException("longShort is a required property for AccountPositionsDefinitionInner and cannot be null");
            }
            else
            {
                this.LongShort = longShort;
            }
            // to ensure "maintenanceMargin" is required (not null)
            if (maintenanceMargin == null)
            {
                throw new InvalidDataException("maintenanceMargin is a required property for AccountPositionsDefinitionInner and cannot be null");
            }
            else
            {
                this.MaintenanceMargin = maintenanceMargin;
            }
            // to ensure "marketValue" is required (not null)
            if (marketValue == null)
            {
                throw new InvalidDataException("marketValue is a required property for AccountPositionsDefinitionInner and cannot be null");
            }
            else
            {
                this.MarketValue = marketValue;
            }
            // to ensure "markToMarketPrice" is required (not null)
            if (markToMarketPrice == null)
            {
                throw new InvalidDataException("markToMarketPrice is a required property for AccountPositionsDefinitionInner and cannot be null");
            }
            else
            {
                this.MarkToMarketPrice = markToMarketPrice;
            }
            // to ensure "openProfitLoss" is required (not null)
            if (openProfitLoss == null)
            {
                throw new InvalidDataException("openProfitLoss is a required property for AccountPositionsDefinitionInner and cannot be null");
            }
            else
            {
                this.OpenProfitLoss = openProfitLoss;
            }
            // to ensure "openProfitLossPercent" is required (not null)
            if (openProfitLossPercent == null)
            {
                throw new InvalidDataException("openProfitLossPercent is a required property for AccountPositionsDefinitionInner and cannot be null");
            }
            else
            {
                this.OpenProfitLossPercent = openProfitLossPercent;
            }
            // to ensure "openProfitLossQty" is required (not null)
            if (openProfitLossQty == null)
            {
                throw new InvalidDataException("openProfitLossQty is a required property for AccountPositionsDefinitionInner and cannot be null");
            }
            else
            {
                this.OpenProfitLossQty = openProfitLossQty;
            }
            // to ensure "quantity" is required (not null)
            if (quantity == null)
            {
                throw new InvalidDataException("quantity is a required property for AccountPositionsDefinitionInner and cannot be null");
            }
            else
            {
                this.Quantity = quantity;
            }
            // to ensure "requiredMargin" is required (not null)
            if (requiredMargin == null)
            {
                throw new InvalidDataException("requiredMargin is a required property for AccountPositionsDefinitionInner and cannot be null");
            }
            else
            {
                this.RequiredMargin = requiredMargin;
            }
            // to ensure "settlePrice" is required (not null)
            if (settlePrice == null)
            {
                throw new InvalidDataException("settlePrice is a required property for AccountPositionsDefinitionInner and cannot be null");
            }
            else
            {
                this.SettlePrice = settlePrice;
            }
            // to ensure "strikePrice" is required (not null)
            if (strikePrice == null)
            {
                throw new InvalidDataException("strikePrice is a required property for AccountPositionsDefinitionInner and cannot be null");
            }
            else
            {
                this.StrikePrice = strikePrice;
            }
            // to ensure "strikePriceDisplay" is required (not null)
            if (strikePriceDisplay == null)
            {
                throw new InvalidDataException("strikePriceDisplay is a required property for AccountPositionsDefinitionInner and cannot be null");
            }
            else
            {
                this.StrikePriceDisplay = strikePriceDisplay;
            }
            // to ensure "symbol" is required (not null)
            if (symbol == null)
            {
                throw new InvalidDataException("symbol is a required property for AccountPositionsDefinitionInner and cannot be null");
            }
            else
            {
                this.Symbol = symbol;
            }
            // to ensure "timeStamp" is required (not null)
            if (timeStamp == null)
            {
                throw new InvalidDataException("timeStamp is a required property for AccountPositionsDefinitionInner and cannot be null");
            }
            else
            {
                this.TimeStamp = timeStamp;
            }
            // to ensure "todaysProfitLoss" is required (not null)
            if (todaysProfitLoss == null)
            {
                throw new InvalidDataException("todaysProfitLoss is a required property for AccountPositionsDefinitionInner and cannot be null");
            }
            else
            {
                this.TodaysProfitLoss = todaysProfitLoss;
            }
            // to ensure "totalCost" is required (not null)
            if (totalCost == null)
            {
                throw new InvalidDataException("totalCost is a required property for AccountPositionsDefinitionInner and cannot be null");
            }
            else
            {
                this.TotalCost = totalCost;
            }
            this.DayTradeMargin = dayTradeMargin;
            this.UnrealizedExpenses = unrealizedExpenses;
        }
        
        /// <summary>
        /// TradeStation account that holds the position.
        /// </summary>
        /// <value>TradeStation account that holds the position.</value>
        [DataMember(Name="AccountID", EmitDefaultValue=false)]
        public string AccountID { get; set; }

        /// <summary>
        /// The actual market value denominated in the account currency of the open position.
        /// </summary>
        /// <value>The actual market value denominated in the account currency of the open position.</value>
        [DataMember(Name="AccountMarketValue", EmitDefaultValue=false)]
        public decimal? AccountMarketValue { get; set; }

        /// <summary>
        /// The unrealized profit or loss denominated in the account currency on the position held, calculated based on the average price of the position.
        /// </summary>
        /// <value>The unrealized profit or loss denominated in the account currency on the position held, calculated based on the average price of the position.</value>
        [DataMember(Name="AccountOpenProfitLoss", EmitDefaultValue=false)]
        public decimal? AccountOpenProfitLoss { get; set; }

        /// <summary>
        /// The total cost denominated in the account currency of the open position.
        /// </summary>
        /// <value>The total cost denominated in the account currency of the open position.</value>
        [DataMember(Name="AccountTotalCost", EmitDefaultValue=false)]
        public decimal? AccountTotalCost { get; set; }

        /// <summary>
        /// A user specified name that identifies a TradeStation account.
        /// </summary>
        /// <value>A user specified name that identifies a TradeStation account.</value>
        [DataMember(Name="Alias", EmitDefaultValue=false)]
        public string Alias { get; set; }

        /// <summary>
        /// The price at which a security, futures contract, or other financial instrument is offered for sale.
        /// </summary>
        /// <value>The price at which a security, futures contract, or other financial instrument is offered for sale.</value>
        [DataMember(Name="AskPrice", EmitDefaultValue=false)]
        public decimal? AskPrice { get; set; }

        /// <summary>
        /// Formatted text representation of the AskPrice for easy display
        /// </summary>
        /// <value>Formatted text representation of the AskPrice for easy display</value>
        [DataMember(Name="AskPriceDisplay", EmitDefaultValue=false)]
        public string AskPriceDisplay { get; set; }


        /// <summary>
        /// Average price of all positions for the current symbol.
        /// </summary>
        /// <value>Average price of all positions for the current symbol.</value>
        [DataMember(Name="AveragePrice", EmitDefaultValue=false)]
        public decimal? AveragePrice { get; set; }

        /// <summary>
        /// String value for AveragePrice attribute.
        /// </summary>
        /// <value>String value for AveragePrice attribute.</value>
        [DataMember(Name="AveragePriceDisplay", EmitDefaultValue=false)]
        public string AveragePriceDisplay { get; set; }

        /// <summary>
        /// The highest price a prospective buyer is prepared to pay at a particular time for a trading unit of a given symbol.
        /// </summary>
        /// <value>The highest price a prospective buyer is prepared to pay at a particular time for a trading unit of a given symbol.</value>
        [DataMember(Name="BidPrice", EmitDefaultValue=false)]
        public decimal? BidPrice { get; set; }

        /// <summary>
        /// Formatted text representation of the BidPrice for easy display
        /// </summary>
        /// <value>Formatted text representation of the BidPrice for easy display</value>
        [DataMember(Name="BidPriceDisplay", EmitDefaultValue=false)]
        public string BidPriceDisplay { get; set; }

        /// <summary>
        /// Dollar value for a one point movement.
        /// </summary>
        /// <value>Dollar value for a one point movement.</value>
        [DataMember(Name="BigPointValue", EmitDefaultValue=false)]
        public decimal? BigPointValue { get; set; }

        /// <summary>
        /// Contract expiration date for positions specified in contracts.
        /// </summary>
        /// <value>Contract expiration date for positions specified in contracts.</value>
        [DataMember(Name="ContractExpireDate", EmitDefaultValue=false)]
        public string ContractExpireDate { get; set; }

        /// <summary>
        /// The currency conversion rate that is used in order to convert from the currency of the symbol to the currency of the account.
        /// </summary>
        /// <value>The currency conversion rate that is used in order to convert from the currency of the symbol to the currency of the account.</value>
        [DataMember(Name="ConversionRate", EmitDefaultValue=false)]
        public decimal? ConversionRate { get; set; }

        /// <summary>
        /// Only applies to margin accounts based in Japan. When set to &#39;FSA&#39; an additional property, UnrealizedExpenses, will be included in the payload.  For other account types, the value will always be &#39;None&#39;
        /// </summary>
        /// <value>Only applies to margin accounts based in Japan. When set to &#39;FSA&#39; an additional property, UnrealizedExpenses, will be included in the payload.  For other account types, the value will always be &#39;None&#39;</value>
        [DataMember(Name="CostBasisCalculation", EmitDefaultValue=false)]
        public string CostBasisCalculation { get; set; }

        /// <summary>
        /// The country of the exchange where the symbol is listed.
        /// </summary>
        /// <value>The country of the exchange where the symbol is listed.</value>
        [DataMember(Name="Country", EmitDefaultValue=false)]
        public string Country { get; set; }

        /// <summary>
        /// The base currency of the symbol.
        /// </summary>
        /// <value>The base currency of the symbol.</value>
        [DataMember(Name="Currency", EmitDefaultValue=false)]
        public string Currency { get; set; }

        /// <summary>
        /// (Futures) DayTradeMargin used on open positions.  Currently only calculated for futures positions.  Other asset classes will have a 0 for this value.
        /// </summary>
        /// <value>(Futures) DayTradeMargin used on open positions.  Currently only calculated for futures positions.  Other asset classes will have a 0 for this value.</value>
        [DataMember(Name="DayTradeMargin", EmitDefaultValue=false)]
        public string DayTradeMargin { get; set; }

        /// <summary>
        /// Displays the full name of the symbol.
        /// </summary>
        /// <value>Displays the full name of the symbol.</value>
        [DataMember(Name="Description", EmitDefaultValue=false)]
        public string Description { get; set; }

        /// <summary>
        /// DO NOT USE - Marked for deprecation.
        /// </summary>
        /// <value>DO NOT USE - Marked for deprecation.</value>
        [DataMember(Name="DisplayName", EmitDefaultValue=false)]
        public string DisplayName { get; set; }

        /// <summary>
        /// The margin account balance denominated in the account currency required for entering a position on margin.
        /// </summary>
        /// <value>The margin account balance denominated in the account currency required for entering a position on margin.</value>
        [DataMember(Name="InitialMargin", EmitDefaultValue=false)]
        public decimal? InitialMargin { get; set; }

        /// <summary>
        /// A unique identifier for the position.
        /// </summary>
        /// <value>A unique identifier for the position.</value>
        [DataMember(Name="Key", EmitDefaultValue=false)]
        public decimal? Key { get; set; }

        /// <summary>
        /// The last price at which the symbol traded.
        /// </summary>
        /// <value>The last price at which the symbol traded.</value>
        [DataMember(Name="LastPrice", EmitDefaultValue=false)]
        public decimal? LastPrice { get; set; }

        /// <summary>
        /// Formatted text representation of the LastPrice for easy display
        /// </summary>
        /// <value>Formatted text representation of the LastPrice for easy display</value>
        [DataMember(Name="LastPriceDisplay", EmitDefaultValue=false)]
        public string LastPriceDisplay { get; set; }


        /// <summary>
        /// The margin account balance denominated in the account currency required for maintaining a position on margin.
        /// </summary>
        /// <value>The margin account balance denominated in the account currency required for maintaining a position on margin.</value>
        [DataMember(Name="MaintenanceMargin", EmitDefaultValue=false)]
        public decimal? MaintenanceMargin { get; set; }

        /// <summary>
        /// The actual market value denominated in the symbol currency of the open position. This value is updated in real-time.
        /// </summary>
        /// <value>The actual market value denominated in the symbol currency of the open position. This value is updated in real-time.</value>
        [DataMember(Name="MarketValue", EmitDefaultValue=false)]
        public decimal? MarketValue { get; set; }

        /// <summary>
        /// Only applies to equity and option positions.  When AssetType is &#39;EQ&#39; or &#39;OP&#39;, this value will be included in the payload to convey the cost-basis price for a position. This is used to calculate the profit and loss incurred during the current session.
        /// </summary>
        /// <value>Only applies to equity and option positions.  When AssetType is &#39;EQ&#39; or &#39;OP&#39;, this value will be included in the payload to convey the cost-basis price for a position. This is used to calculate the profit and loss incurred during the current session.</value>
        [DataMember(Name="MarkToMarketPrice", EmitDefaultValue=false)]
        public decimal? MarkToMarketPrice { get; set; }

        /// <summary>
        /// The unrealized profit or loss denominated in the symbol currency on the position held, calculated based on the average price of the position.
        /// </summary>
        /// <value>The unrealized profit or loss denominated in the symbol currency on the position held, calculated based on the average price of the position.</value>
        [DataMember(Name="OpenProfitLoss", EmitDefaultValue=false)]
        public decimal? OpenProfitLoss { get; set; }

        /// <summary>
        /// The unrealized profit or loss on the position expressed as a percentage of the initial value of the position.
        /// </summary>
        /// <value>The unrealized profit or loss on the position expressed as a percentage of the initial value of the position.</value>
        [DataMember(Name="OpenProfitLossPercent", EmitDefaultValue=false)]
        public decimal? OpenProfitLossPercent { get; set; }

        /// <summary>
        /// The unrealized profit or loss denominated in the account currency divided by the number of shares, contracts or units held.
        /// </summary>
        /// <value>The unrealized profit or loss denominated in the account currency divided by the number of shares, contracts or units held.</value>
        [DataMember(Name="OpenProfitLossQty", EmitDefaultValue=false)]
        public decimal? OpenProfitLossQty { get; set; }

        /// <summary>
        /// The requested number of shares or contracts for a particular order.
        /// </summary>
        /// <value>The requested number of shares or contracts for a particular order.</value>
        [DataMember(Name="Quantity", EmitDefaultValue=false)]
        public decimal? Quantity { get; set; }

        /// <summary>
        /// (Forex) The margin account balance denominated in the account currency required for entering and maintaining a position on margin.
        /// </summary>
        /// <value>(Forex) The margin account balance denominated in the account currency required for entering and maintaining a position on margin.</value>
        [DataMember(Name="RequiredMargin", EmitDefaultValue=false)]
        public decimal? RequiredMargin { get; set; }

        /// <summary>
        /// The average price at which a contract trades, calculated at both the open and close of each trading day, and it is important because it determines whether a trader is required to post additional margins.
        /// </summary>
        /// <value>The average price at which a contract trades, calculated at both the open and close of each trading day, and it is important because it determines whether a trader is required to post additional margins.</value>
        [DataMember(Name="SettlePrice", EmitDefaultValue=false)]
        public decimal? SettlePrice { get; set; }

        /// <summary>
        /// The strike price is the stated price per share or per contract for which the underlying asset may be purchased, or sold, by the option holder upon exercise of the option contract.
        /// </summary>
        /// <value>The strike price is the stated price per share or per contract for which the underlying asset may be purchased, or sold, by the option holder upon exercise of the option contract.</value>
        [DataMember(Name="StrikePrice", EmitDefaultValue=false)]
        public decimal? StrikePrice { get; set; }

        /// <summary>
        /// Formatted text representation of the StrikePrice for easy display
        /// </summary>
        /// <value>Formatted text representation of the StrikePrice for easy display</value>
        [DataMember(Name="StrikePriceDisplay", EmitDefaultValue=false)]
        public string StrikePriceDisplay { get; set; }

        /// <summary>
        /// Symbol of the current position.
        /// </summary>
        /// <value>Symbol of the current position.</value>
        [DataMember(Name="Symbol", EmitDefaultValue=false)]
        public string Symbol { get; set; }

        /// <summary>
        /// Time the position was placed.
        /// </summary>
        /// <value>Time the position was placed.</value>
        [DataMember(Name="TimeStamp", EmitDefaultValue=false)]
        public string TimeStamp { get; set; }

        /// <summary>
        /// Only applies to equity and option positions.  When AssetType is &#39;EQ&#39; or &#39;OP&#39;, this value will be included in the payload to convey the unrealized profit or loss denominated in the account currency on the position held, calculated using the MarkToMarketPrice.
        /// </summary>
        /// <value>Only applies to equity and option positions.  When AssetType is &#39;EQ&#39; or &#39;OP&#39;, this value will be included in the payload to convey the unrealized profit or loss denominated in the account currency on the position held, calculated using the MarkToMarketPrice.</value>
        [DataMember(Name="TodaysProfitLoss", EmitDefaultValue=false)]
        public decimal? TodaysProfitLoss { get; set; }

        /// <summary>
        /// The total cost denominated in the account currency of the open position.
        /// </summary>
        /// <value>The total cost denominated in the account currency of the open position.</value>
        [DataMember(Name="TotalCost", EmitDefaultValue=false)]
        public decimal? TotalCost { get; set; }

        /// <summary>
        /// Only applies to margin accounts based in Japan.  When CostBasisCalculation is to &#39;FSA&#39;, UnrealizedExpenses, will be included in the payload, and will convey interest expenses and buy order commissions.
        /// </summary>
        /// <value>Only applies to margin accounts based in Japan.  When CostBasisCalculation is to &#39;FSA&#39;, UnrealizedExpenses, will be included in the payload, and will convey interest expenses and buy order commissions.</value>
        [DataMember(Name="UnrealizedExpenses", EmitDefaultValue=false)]
        public decimal? UnrealizedExpenses { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class AccountPositionsDefinitionInner {\n");
            sb.Append("  AccountID: ").Append(AccountID).Append("\n");
            sb.Append("  AccountMarketValue: ").Append(AccountMarketValue).Append("\n");
            sb.Append("  AccountOpenProfitLoss: ").Append(AccountOpenProfitLoss).Append("\n");
            sb.Append("  AccountTotalCost: ").Append(AccountTotalCost).Append("\n");
            sb.Append("  Alias: ").Append(Alias).Append("\n");
            sb.Append("  AskPrice: ").Append(AskPrice).Append("\n");
            sb.Append("  AskPriceDisplay: ").Append(AskPriceDisplay).Append("\n");
            sb.Append("  AssetType: ").Append(AssetType).Append("\n");
            sb.Append("  AveragePrice: ").Append(AveragePrice).Append("\n");
            sb.Append("  AveragePriceDisplay: ").Append(AveragePriceDisplay).Append("\n");
            sb.Append("  BidPrice: ").Append(BidPrice).Append("\n");
            sb.Append("  BidPriceDisplay: ").Append(BidPriceDisplay).Append("\n");
            sb.Append("  BigPointValue: ").Append(BigPointValue).Append("\n");
            sb.Append("  ContractExpireDate: ").Append(ContractExpireDate).Append("\n");
            sb.Append("  ConversionRate: ").Append(ConversionRate).Append("\n");
            sb.Append("  CostBasisCalculation: ").Append(CostBasisCalculation).Append("\n");
            sb.Append("  Country: ").Append(Country).Append("\n");
            sb.Append("  Currency: ").Append(Currency).Append("\n");
            sb.Append("  DayTradeMargin: ").Append(DayTradeMargin).Append("\n");
            sb.Append("  Description: ").Append(Description).Append("\n");
            sb.Append("  DisplayName: ").Append(DisplayName).Append("\n");
            sb.Append("  InitialMargin: ").Append(InitialMargin).Append("\n");
            sb.Append("  Key: ").Append(Key).Append("\n");
            sb.Append("  LastPrice: ").Append(LastPrice).Append("\n");
            sb.Append("  LastPriceDisplay: ").Append(LastPriceDisplay).Append("\n");
            sb.Append("  LongShort: ").Append(LongShort).Append("\n");
            sb.Append("  MaintenanceMargin: ").Append(MaintenanceMargin).Append("\n");
            sb.Append("  MarketValue: ").Append(MarketValue).Append("\n");
            sb.Append("  MarkToMarketPrice: ").Append(MarkToMarketPrice).Append("\n");
            sb.Append("  OpenProfitLoss: ").Append(OpenProfitLoss).Append("\n");
            sb.Append("  OpenProfitLossPercent: ").Append(OpenProfitLossPercent).Append("\n");
            sb.Append("  OpenProfitLossQty: ").Append(OpenProfitLossQty).Append("\n");
            sb.Append("  Quantity: ").Append(Quantity).Append("\n");
            sb.Append("  RequiredMargin: ").Append(RequiredMargin).Append("\n");
            sb.Append("  SettlePrice: ").Append(SettlePrice).Append("\n");
            sb.Append("  StrikePrice: ").Append(StrikePrice).Append("\n");
            sb.Append("  StrikePriceDisplay: ").Append(StrikePriceDisplay).Append("\n");
            sb.Append("  Symbol: ").Append(Symbol).Append("\n");
            sb.Append("  TimeStamp: ").Append(TimeStamp).Append("\n");
            sb.Append("  TodaysProfitLoss: ").Append(TodaysProfitLoss).Append("\n");
            sb.Append("  TotalCost: ").Append(TotalCost).Append("\n");
            sb.Append("  UnrealizedExpenses: ").Append(UnrealizedExpenses).Append("\n");
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
            return this.Equals(input as AccountPositionsDefinitionInner);
        }

        /// <summary>
        /// Returns true if AccountPositionsDefinitionInner instances are equal
        /// </summary>
        /// <param name="input">Instance of AccountPositionsDefinitionInner to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(AccountPositionsDefinitionInner input)
        {
            if (input == null)
                return false;

            return 
                (
                    this.AccountID == input.AccountID ||
                    (this.AccountID != null &&
                    this.AccountID.Equals(input.AccountID))
                ) && 
                (
                    this.AccountMarketValue == input.AccountMarketValue ||
                    (this.AccountMarketValue != null &&
                    this.AccountMarketValue.Equals(input.AccountMarketValue))
                ) && 
                (
                    this.AccountOpenProfitLoss == input.AccountOpenProfitLoss ||
                    (this.AccountOpenProfitLoss != null &&
                    this.AccountOpenProfitLoss.Equals(input.AccountOpenProfitLoss))
                ) && 
                (
                    this.AccountTotalCost == input.AccountTotalCost ||
                    (this.AccountTotalCost != null &&
                    this.AccountTotalCost.Equals(input.AccountTotalCost))
                ) && 
                (
                    this.Alias == input.Alias ||
                    (this.Alias != null &&
                    this.Alias.Equals(input.Alias))
                ) && 
                (
                    this.AskPrice == input.AskPrice ||
                    (this.AskPrice != null &&
                    this.AskPrice.Equals(input.AskPrice))
                ) && 
                (
                    this.AskPriceDisplay == input.AskPriceDisplay ||
                    (this.AskPriceDisplay != null &&
                    this.AskPriceDisplay.Equals(input.AskPriceDisplay))
                ) && 
                (
                    this.AssetType == input.AssetType ||
                    (this.AssetType != null &&
                    this.AssetType.Equals(input.AssetType))
                ) && 
                (
                    this.AveragePrice == input.AveragePrice ||
                    (this.AveragePrice != null &&
                    this.AveragePrice.Equals(input.AveragePrice))
                ) && 
                (
                    this.AveragePriceDisplay == input.AveragePriceDisplay ||
                    (this.AveragePriceDisplay != null &&
                    this.AveragePriceDisplay.Equals(input.AveragePriceDisplay))
                ) && 
                (
                    this.BidPrice == input.BidPrice ||
                    (this.BidPrice != null &&
                    this.BidPrice.Equals(input.BidPrice))
                ) && 
                (
                    this.BidPriceDisplay == input.BidPriceDisplay ||
                    (this.BidPriceDisplay != null &&
                    this.BidPriceDisplay.Equals(input.BidPriceDisplay))
                ) && 
                (
                    this.BigPointValue == input.BigPointValue ||
                    (this.BigPointValue != null &&
                    this.BigPointValue.Equals(input.BigPointValue))
                ) && 
                (
                    this.ContractExpireDate == input.ContractExpireDate ||
                    (this.ContractExpireDate != null &&
                    this.ContractExpireDate.Equals(input.ContractExpireDate))
                ) && 
                (
                    this.ConversionRate == input.ConversionRate ||
                    (this.ConversionRate != null &&
                    this.ConversionRate.Equals(input.ConversionRate))
                ) && 
                (
                    this.CostBasisCalculation == input.CostBasisCalculation ||
                    (this.CostBasisCalculation != null &&
                    this.CostBasisCalculation.Equals(input.CostBasisCalculation))
                ) && 
                (
                    this.Country == input.Country ||
                    (this.Country != null &&
                    this.Country.Equals(input.Country))
                ) && 
                (
                    this.Currency == input.Currency ||
                    (this.Currency != null &&
                    this.Currency.Equals(input.Currency))
                ) && 
                (
                    this.DayTradeMargin == input.DayTradeMargin ||
                    (this.DayTradeMargin != null &&
                    this.DayTradeMargin.Equals(input.DayTradeMargin))
                ) && 
                (
                    this.Description == input.Description ||
                    (this.Description != null &&
                    this.Description.Equals(input.Description))
                ) && 
                (
                    this.DisplayName == input.DisplayName ||
                    (this.DisplayName != null &&
                    this.DisplayName.Equals(input.DisplayName))
                ) && 
                (
                    this.InitialMargin == input.InitialMargin ||
                    (this.InitialMargin != null &&
                    this.InitialMargin.Equals(input.InitialMargin))
                ) && 
                (
                    this.Key == input.Key ||
                    (this.Key != null &&
                    this.Key.Equals(input.Key))
                ) && 
                (
                    this.LastPrice == input.LastPrice ||
                    (this.LastPrice != null &&
                    this.LastPrice.Equals(input.LastPrice))
                ) && 
                (
                    this.LastPriceDisplay == input.LastPriceDisplay ||
                    (this.LastPriceDisplay != null &&
                    this.LastPriceDisplay.Equals(input.LastPriceDisplay))
                ) && 
                (
                    this.LongShort == input.LongShort ||
                    (this.LongShort != null &&
                    this.LongShort.Equals(input.LongShort))
                ) && 
                (
                    this.MaintenanceMargin == input.MaintenanceMargin ||
                    (this.MaintenanceMargin != null &&
                    this.MaintenanceMargin.Equals(input.MaintenanceMargin))
                ) && 
                (
                    this.MarketValue == input.MarketValue ||
                    (this.MarketValue != null &&
                    this.MarketValue.Equals(input.MarketValue))
                ) && 
                (
                    this.MarkToMarketPrice == input.MarkToMarketPrice ||
                    (this.MarkToMarketPrice != null &&
                    this.MarkToMarketPrice.Equals(input.MarkToMarketPrice))
                ) && 
                (
                    this.OpenProfitLoss == input.OpenProfitLoss ||
                    (this.OpenProfitLoss != null &&
                    this.OpenProfitLoss.Equals(input.OpenProfitLoss))
                ) && 
                (
                    this.OpenProfitLossPercent == input.OpenProfitLossPercent ||
                    (this.OpenProfitLossPercent != null &&
                    this.OpenProfitLossPercent.Equals(input.OpenProfitLossPercent))
                ) && 
                (
                    this.OpenProfitLossQty == input.OpenProfitLossQty ||
                    (this.OpenProfitLossQty != null &&
                    this.OpenProfitLossQty.Equals(input.OpenProfitLossQty))
                ) && 
                (
                    this.Quantity == input.Quantity ||
                    (this.Quantity != null &&
                    this.Quantity.Equals(input.Quantity))
                ) && 
                (
                    this.RequiredMargin == input.RequiredMargin ||
                    (this.RequiredMargin != null &&
                    this.RequiredMargin.Equals(input.RequiredMargin))
                ) && 
                (
                    this.SettlePrice == input.SettlePrice ||
                    (this.SettlePrice != null &&
                    this.SettlePrice.Equals(input.SettlePrice))
                ) && 
                (
                    this.StrikePrice == input.StrikePrice ||
                    (this.StrikePrice != null &&
                    this.StrikePrice.Equals(input.StrikePrice))
                ) && 
                (
                    this.StrikePriceDisplay == input.StrikePriceDisplay ||
                    (this.StrikePriceDisplay != null &&
                    this.StrikePriceDisplay.Equals(input.StrikePriceDisplay))
                ) && 
                (
                    this.Symbol == input.Symbol ||
                    (this.Symbol != null &&
                    this.Symbol.Equals(input.Symbol))
                ) && 
                (
                    this.TimeStamp == input.TimeStamp ||
                    (this.TimeStamp != null &&
                    this.TimeStamp.Equals(input.TimeStamp))
                ) && 
                (
                    this.TodaysProfitLoss == input.TodaysProfitLoss ||
                    (this.TodaysProfitLoss != null &&
                    this.TodaysProfitLoss.Equals(input.TodaysProfitLoss))
                ) && 
                (
                    this.TotalCost == input.TotalCost ||
                    (this.TotalCost != null &&
                    this.TotalCost.Equals(input.TotalCost))
                ) && 
                (
                    this.UnrealizedExpenses == input.UnrealizedExpenses ||
                    (this.UnrealizedExpenses != null &&
                    this.UnrealizedExpenses.Equals(input.UnrealizedExpenses))
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
                if (this.AccountID != null)
                    hashCode = hashCode * 59 + this.AccountID.GetHashCode();
                if (this.AccountMarketValue != null)
                    hashCode = hashCode * 59 + this.AccountMarketValue.GetHashCode();
                if (this.AccountOpenProfitLoss != null)
                    hashCode = hashCode * 59 + this.AccountOpenProfitLoss.GetHashCode();
                if (this.AccountTotalCost != null)
                    hashCode = hashCode * 59 + this.AccountTotalCost.GetHashCode();
                if (this.Alias != null)
                    hashCode = hashCode * 59 + this.Alias.GetHashCode();
                if (this.AskPrice != null)
                    hashCode = hashCode * 59 + this.AskPrice.GetHashCode();
                if (this.AskPriceDisplay != null)
                    hashCode = hashCode * 59 + this.AskPriceDisplay.GetHashCode();
                if (this.AssetType != null)
                    hashCode = hashCode * 59 + this.AssetType.GetHashCode();
                if (this.AveragePrice != null)
                    hashCode = hashCode * 59 + this.AveragePrice.GetHashCode();
                if (this.AveragePriceDisplay != null)
                    hashCode = hashCode * 59 + this.AveragePriceDisplay.GetHashCode();
                if (this.BidPrice != null)
                    hashCode = hashCode * 59 + this.BidPrice.GetHashCode();
                if (this.BidPriceDisplay != null)
                    hashCode = hashCode * 59 + this.BidPriceDisplay.GetHashCode();
                if (this.BigPointValue != null)
                    hashCode = hashCode * 59 + this.BigPointValue.GetHashCode();
                if (this.ContractExpireDate != null)
                    hashCode = hashCode * 59 + this.ContractExpireDate.GetHashCode();
                if (this.ConversionRate != null)
                    hashCode = hashCode * 59 + this.ConversionRate.GetHashCode();
                if (this.CostBasisCalculation != null)
                    hashCode = hashCode * 59 + this.CostBasisCalculation.GetHashCode();
                if (this.Country != null)
                    hashCode = hashCode * 59 + this.Country.GetHashCode();
                if (this.Currency != null)
                    hashCode = hashCode * 59 + this.Currency.GetHashCode();
                if (this.DayTradeMargin != null)
                    hashCode = hashCode * 59 + this.DayTradeMargin.GetHashCode();
                if (this.Description != null)
                    hashCode = hashCode * 59 + this.Description.GetHashCode();
                if (this.DisplayName != null)
                    hashCode = hashCode * 59 + this.DisplayName.GetHashCode();
                if (this.InitialMargin != null)
                    hashCode = hashCode * 59 + this.InitialMargin.GetHashCode();
                if (this.Key != null)
                    hashCode = hashCode * 59 + this.Key.GetHashCode();
                if (this.LastPrice != null)
                    hashCode = hashCode * 59 + this.LastPrice.GetHashCode();
                if (this.LastPriceDisplay != null)
                    hashCode = hashCode * 59 + this.LastPriceDisplay.GetHashCode();
                if (this.LongShort != null)
                    hashCode = hashCode * 59 + this.LongShort.GetHashCode();
                if (this.MaintenanceMargin != null)
                    hashCode = hashCode * 59 + this.MaintenanceMargin.GetHashCode();
                if (this.MarketValue != null)
                    hashCode = hashCode * 59 + this.MarketValue.GetHashCode();
                if (this.MarkToMarketPrice != null)
                    hashCode = hashCode * 59 + this.MarkToMarketPrice.GetHashCode();
                if (this.OpenProfitLoss != null)
                    hashCode = hashCode * 59 + this.OpenProfitLoss.GetHashCode();
                if (this.OpenProfitLossPercent != null)
                    hashCode = hashCode * 59 + this.OpenProfitLossPercent.GetHashCode();
                if (this.OpenProfitLossQty != null)
                    hashCode = hashCode * 59 + this.OpenProfitLossQty.GetHashCode();
                if (this.Quantity != null)
                    hashCode = hashCode * 59 + this.Quantity.GetHashCode();
                if (this.RequiredMargin != null)
                    hashCode = hashCode * 59 + this.RequiredMargin.GetHashCode();
                if (this.SettlePrice != null)
                    hashCode = hashCode * 59 + this.SettlePrice.GetHashCode();
                if (this.StrikePrice != null)
                    hashCode = hashCode * 59 + this.StrikePrice.GetHashCode();
                if (this.StrikePriceDisplay != null)
                    hashCode = hashCode * 59 + this.StrikePriceDisplay.GetHashCode();
                if (this.Symbol != null)
                    hashCode = hashCode * 59 + this.Symbol.GetHashCode();
                if (this.TimeStamp != null)
                    hashCode = hashCode * 59 + this.TimeStamp.GetHashCode();
                if (this.TodaysProfitLoss != null)
                    hashCode = hashCode * 59 + this.TodaysProfitLoss.GetHashCode();
                if (this.TotalCost != null)
                    hashCode = hashCode * 59 + this.TotalCost.GetHashCode();
                if (this.UnrealizedExpenses != null)
                    hashCode = hashCode * 59 + this.UnrealizedExpenses.GetHashCode();
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
            // AccountID (string) minLength
            if(this.AccountID != null && this.AccountID.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for AccountID, length must be greater than 1.", new [] { "AccountID" });
            }

            // AskPriceDisplay (string) minLength
            if(this.AskPriceDisplay != null && this.AskPriceDisplay.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for AskPriceDisplay, length must be greater than 1.", new [] { "AskPriceDisplay" });
            }

            // AssetType (string) minLength
            //if(this.AssetType != null && this.AssetType.Length < 1)
            //{
            //    yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for AssetType, length must be greater than 1.", new [] { "AssetType" });
            //}

            // AveragePriceDisplay (string) minLength
            if(this.AveragePriceDisplay != null && this.AveragePriceDisplay.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for AveragePriceDisplay, length must be greater than 1.", new [] { "AveragePriceDisplay" });
            }

            // BidPriceDisplay (string) minLength
            if(this.BidPriceDisplay != null && this.BidPriceDisplay.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for BidPriceDisplay, length must be greater than 1.", new [] { "BidPriceDisplay" });
            }

            // ContractExpireDate (string) minLength
            if(this.ContractExpireDate != null && this.ContractExpireDate.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for ContractExpireDate, length must be greater than 1.", new [] { "ContractExpireDate" });
            }

            // Country (string) minLength
            if(this.Country != null && this.Country.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Country, length must be greater than 1.", new [] { "Country" });
            }

            // Currency (string) minLength
            if(this.Currency != null && this.Currency.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Currency, length must be greater than 1.", new [] { "Currency" });
            }

            // DayTradeMargin (string) minLength
            if(this.DayTradeMargin != null && this.DayTradeMargin.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for DayTradeMargin, length must be greater than 1.", new [] { "DayTradeMargin" });
            }

            // Description (string) minLength
            if(this.Description != null && this.Description.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Description, length must be greater than 1.", new [] { "Description" });
            }

            // DisplayName (string) minLength
            if(this.DisplayName != null && this.DisplayName.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for DisplayName, length must be greater than 1.", new [] { "DisplayName" });
            }

            // LastPriceDisplay (string) minLength
            if(this.LastPriceDisplay != null && this.LastPriceDisplay.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for LastPriceDisplay, length must be greater than 1.", new [] { "LastPriceDisplay" });
            }

            // LongShort (string) minLength
            //if(this.LongShort != null && this.LongShort.Length < 1)
            //{
            //    yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for LongShort, length must be greater than 1.", new [] { "LongShort" });
            //}

            // StrikePriceDisplay (string) minLength
            if(this.StrikePriceDisplay != null && this.StrikePriceDisplay.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for StrikePriceDisplay, length must be greater than 1.", new [] { "StrikePriceDisplay" });
            }

            // Symbol (string) minLength
            if(this.Symbol != null && this.Symbol.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Symbol, length must be greater than 1.", new [] { "Symbol" });
            }

            // TimeStamp (string) minLength
            if(this.TimeStamp != null && this.TimeStamp.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for TimeStamp, length must be greater than 1.", new [] { "TimeStamp" });
            }

            yield break;
        }
    }

}
