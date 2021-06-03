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
    /// QuoteDefinitionInner
    /// </summary>
    [DataContract]
    public partial class QuoteDefinitionInner :  IEquatable<QuoteDefinitionInner>, IValidatableObject
    {
        /// <summary>
        /// The name of asset type for a symbol
        /// </summary>
        /// <value>The name of asset type for a symbol</value>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum AssetTypeEnum
        {
            
            /// <summary>
            /// Enum INDEX for value: INDEX
            /// </summary>
            [EnumMember(Value = "INDEX")]
            INDEX = 1,
            
            /// <summary>
            /// Enum STOCK for value: STOCK
            /// </summary>
            [EnumMember(Value = "STOCK")]
            STOCK = 2,
            
            /// <summary>
            /// Enum STOCKOPTION for value: STOCKOPTION
            /// </summary>
            [EnumMember(Value = "STOCKOPTION")]
            STOCKOPTION = 3,
            
            /// <summary>
            /// Enum FUTURE for value: FUTURE
            /// </summary>
            [EnumMember(Value = "FUTURE")]
            FUTURE = 4,
            
            /// <summary>
            /// Enum FOREX for value: FOREX
            /// </summary>
            [EnumMember(Value = "FOREX")]
            FOREX = 5,
            
            /// <summary>
            /// Enum UNKNOWN for value: UNKNOWN
            /// </summary>
            [EnumMember(Value = "UNKNOWN")]
            UNKNOWN = 6
        }

        /// <summary>
        /// The name of asset type for a symbol
        /// </summary>
        /// <value>The name of asset type for a symbol</value>
        [DataMember(Name="AssetType", EmitDefaultValue=false)]
        public AssetTypeEnum AssetType { get; set; }
        /// <summary>
        /// The base currency of the symbol.
        /// </summary>
        /// <value>The base currency of the symbol.</value>
        [JsonConverter(typeof(MyEnumConverter))]
        public enum CurrencyEnum
        {
            All = 0,

            /// <summary>
            /// Enum USD for value: USD
            /// </summary>
            [EnumMember(Value = "USD")]
            USD = 1,
            
            /// <summary>
            /// Enum AUD for value: AUD
            /// </summary>
            [EnumMember(Value = "AUD")]
            AUD = 2,
            
            /// <summary>
            /// Enum CAD for value: CAD
            /// </summary>
            [EnumMember(Value = "CAD")]
            CAD = 3,
            
            /// <summary>
            /// Enum CHF for value: CHF
            /// </summary>
            [EnumMember(Value = "CHF")]
            CHF = 4,
            
            /// <summary>
            /// Enum DKK for value: DKK
            /// </summary>
            [EnumMember(Value = "DKK")]
            DKK = 5,
            
            /// <summary>
            /// Enum EUR for value: EUR
            /// </summary>
            [EnumMember(Value = "EUR")]
            EUR = 6,
            
            /// <summary>
            /// Enum DBP for value: DBP
            /// </summary>
            [EnumMember(Value = "DBP")]
            DBP = 7,
            
            /// <summary>
            /// Enum HKD for value: HKD
            /// </summary>
            [EnumMember(Value = "HKD")]
            HKD = 8,
            
            /// <summary>
            /// Enum JPY for value: JPY
            /// </summary>
            [EnumMember(Value = "JPY")]
            JPY = 9,
            
            /// <summary>
            /// Enum NOK for value: NOK
            /// </summary>
            [EnumMember(Value = "NOK")]
            NOK = 10,
            
            /// <summary>
            /// Enum NZD for value: NZD
            /// </summary>
            [EnumMember(Value = "NZD")]
            NZD = 11,
            
            /// <summary>
            /// Enum SEK for value: SEK
            /// </summary>
            [EnumMember(Value = "SEK")]
            SEK = 12,
            
            /// <summary>
            /// Enum SGD for value: SGD
            /// </summary>
            [EnumMember(Value = "SGD")]
            SGD = 13,

            [EnumMember(Value = "GBP")]
            GBP = 14,

            [EnumMember(Value = "MXN")]
            MXN = 15
        }

        /// <summary>
        /// The base currency of the symbol.
        /// </summary>
        /// <value>The base currency of the symbol.</value>
        [DataMember(Name="Currency", EmitDefaultValue=false)]
        public CurrencyEnum Currency { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="QuoteDefinitionInner" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        public QuoteDefinitionInner() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="QuoteDefinitionInner" /> class.
        /// </summary>
        /// <param name="ask">The price at which a security, futures contract, or other financial instrument is offered for sale. (required).</param>
        /// <param name="askPriceDisplay">Ask price formatted for display. (required).</param>
        /// <param name="askSize">The number of trading units that prospective sellers are prepared to sell. (required).</param>
        /// <param name="assetType">The name of asset type for a symbol (required).</param>
        /// <param name="bid">The highest price a prospective buyer is prepared to pay at a particular time for a trading unit of a given symbol. (required).</param>
        /// <param name="bidPriceDisplay">Bid price formatted for display. (required).</param>
        /// <param name="bidSize">The number of trading units that prospective buyers are prepared to purchase for a symbol. (required).</param>
        /// <param name="close">The net Realized Profit or Loss denominated in the symbol currency for the current trading session. This value includes any gain or loss as a result of closing a position during the current trading session (required).</param>
        /// <param name="closePriceDisplay">Daily running close price formatted for display. (required).</param>
        /// <param name="countryCode">The country of the exchange where the symbol is listed. (required).</param>
        /// <param name="currency">The base currency of the symbol. (required).</param>
        /// <param name="dailyOpenInterest">The total number of open or outstanding (not closed or delivered) options and/or futures contracts that exist on a given day, delivered on a particular day. (required).</param>
        /// <param name="dataFeed">The data quote feed provider. (required).</param>
        /// <param name="description">Displays the full name of the symbol. (required).</param>
        /// <param name="displayType">Symbol&#39;s price display type based on the following list:  * &#x60;0&#x60; \&quot;Automatic\&quot; Not used * &#x60;1&#x60; 0 Decimals &#x3D;&gt; 1 * &#x60;2&#x60; 1 Decimals &#x3D;&gt; .1 * &#x60;3&#x60; 2 Decimals &#x3D;&gt; .01 * &#x60;4&#x60; 3 Decimals &#x3D;&gt; .001 * &#x60;5&#x60; 4 Decimals &#x3D;&gt; .0001 * &#x60;6&#x60; 5 Decimals &#x3D;&gt; .00001 * &#x60;7&#x60; Simplest Fraction * &#x60;8&#x60; 1/2-Halves &#x3D;&gt; .5 * &#x60;9&#x60; 1/4-Fourths &#x3D;&gt; .25 * &#x60;10&#x60; 1/8-Eights &#x3D;&gt; .125 * &#x60;11&#x60; 1/16-Sixteenths &#x3D;&gt; .0625 * &#x60;12&#x60; 1/32-ThirtySeconds &#x3D;&gt; .03125 * &#x60;13&#x60; 1/64-SixtyFourths &#x3D;&gt; .015625 * &#x60;14&#x60; 1/128-OneTwentyEigths &#x3D;&gt; .0078125 * &#x60;15&#x60; 1/256-TwoFiftySixths &#x3D;&gt; .003906250 * &#x60;16&#x60; 10ths and Quarters &#x3D;&gt; .025 * &#x60;17&#x60; 32nds and Halves &#x3D;&gt; .015625 * &#x60;18&#x60; 32nds and Quarters &#x3D;&gt; .0078125 * &#x60;19&#x60; 32nds and Eights &#x3D;&gt; .00390625 * &#x60;20&#x60; 32nds and Tenths &#x3D;&gt; .003125 * &#x60;21&#x60; 64ths and Halves &#x3D;&gt; .0078125 * &#x60;22&#x60; 64ths and Tenths &#x3D;&gt; .0015625 * &#x60;23&#x60; 6 Decimals &#x3D;&gt; .000001  (required).</param>
        /// <param name="error">Error message received from exchange or Tradestation..</param>
        /// <param name="exchange">Name of exchange where this symbol is traded in. (required).</param>
        /// <param name="expirationDate">The UTC expiration date of a contract in ISO 8601 date format (yyyy-mm-dd). Valid only on options and futures symbols. For other categories this field is empty..</param>
        /// <param name="firstNoticeDate">The day after which an investor who has purchased a futures contract may be required to take physical delivery of the contracts underlying commodity..</param>
        /// <param name="fractionalDisplay">Determine whether fractional price display is required. (required).</param>
        /// <param name="halted">A temporary suspension of trading for a particular security or securities at one exchange or across numerous exchanges..</param>
        /// <param name="high">Highest price of the day. (required).</param>
        /// <param name="high52Week">The highest price of the past 52 weeks. This is a grid-based indicator. (required).</param>
        /// <param name="high52WeekPriceDisplay">High52Week price formatted for display. (required).</param>
        /// <param name="high52WeekTimeStamp">Date and time of the highest price in the past 52 week. (required).</param>
        /// <param name="highPriceDisplay">High price formatted for display. (required).</param>
        /// <param name="isDelayed">True if the quote is a delayed quote and False if the quote is a real-time quote (required).</param>
        /// <param name="last">The last price at which the symbol traded. (required).</param>
        /// <param name="lastPriceDisplay">Last price formatted for display. (required).</param>
        /// <param name="lastTradingDate">The final day that a futures contract may trade or be closed out before the delivery of the underlying asset or cash settlement must occur..</param>
        /// <param name="low">Lowest price of stock. (required).</param>
        /// <param name="low52Week">The lowest price of the past 52 weeks. (required).</param>
        /// <param name="low52WeekPriceDisplay">Low52Week price formatted for display. (required).</param>
        /// <param name="low52WeekTimeStamp">Date and time of the lowest price of the past 52 weeks. (required).</param>
        /// <param name="lowPriceDisplay">Low price formatted for display. (required).</param>
        /// <param name="maxPrice">The maximum price a commodity futures contract may be traded for the current session..</param>
        /// <param name="maxPriceDisplay">MaxPrice formatted for display..</param>
        /// <param name="minMove">Multiplying factor using the display type to determine the minimum price increment the asset trades in. For options the MinMove may vary. If the MinMove is negative, then the MinMove is dependent on the price. The whole number portion of the min move is the threshold. The leftmost two digits to the right of the decimal (X.XXXX) indicate the min move beneath the threshold, and the rightmost two digits (X.XXXX) indicate the min move above the threshold. (required).</param>
        /// <param name="minPrice">The minimum price a commodity futures contract may be traded for the current session..</param>
        /// <param name="minPriceDisplay">MinPrice formatted for display..</param>
        /// <param name="nameExt">If the Quote is delayed this property will be set to &#x60;D&#x60; (required).</param>
        /// <param name="netChange">The difference between the last displayed price and the previous day&#x60;s close. (required).</param>
        /// <param name="netChangePct">The difference between the current price and the previous day&#x60;s close, expressed in a percentage. (required).</param>
        /// <param name="open">The unrealized profit or loss denominated in the symbol currency on the position held, calculated based on the average price of the position. (required).</param>
        /// <param name="openPriceDisplay">Open price formatted for display. (required).</param>
        /// <param name="pointValue">The currency value represented by a full point of price movement. In the case of stocks, the Big Point Value is usually 1, in that 1 point of movement represents 1 dollar, however it may vary. (required).</param>
        /// <param name="previousClose">The closing price of the previous day. (required).</param>
        /// <param name="previousClosePriceDisplay">PreviousClose price formatted for display. (required).</param>
        /// <param name="previousVolume">Daily volume of the previous day. (required).</param>
        /// <param name="restrictions">A symbol specific restrictions for Japanese equities. (required).</param>
        /// <param name="strikePrice">The price at which the underlying contract will be delivered in the event an option is exercised. (required).</param>
        /// <param name="strikePriceDisplay">StrikePrice formatted for display. (required).</param>
        /// <param name="symbol">The name identifying the financial instrument for which the data is displayed. (required).</param>
        /// <param name="symbolRoot">The symbol used to identify the option&#x60;s financial instrument for a specific underlying asset that is traded on the various trading exchanges. (required).</param>
        /// <param name="tickSizeTier">Trading increment based on a level group. (required).</param>
        /// <param name="tradeTime">Trade execution time. (required).</param>
        /// <param name="underlying">The financial instrument on which an option contract is based or derived. (required).</param>
        /// <param name="volume">The number of shares or contracts traded in a security or an entire market during a given period of time. (required).</param>
        public QuoteDefinitionInner(decimal? ask = default, string askPriceDisplay = default, decimal? askSize = default, AssetTypeEnum assetType = default, decimal? bid = default, string bidPriceDisplay = default, decimal? bidSize = default, decimal? close = default, string closePriceDisplay = default, string countryCode = default, CurrencyEnum currency = default, decimal? dailyOpenInterest = default, string dataFeed = default, string description = default, decimal? displayType = default, string error = default, string exchange = default, string expirationDate = default, string firstNoticeDate = default, bool? fractionalDisplay = default, bool? halted = default, decimal? high = default, decimal? high52Week = default, string high52WeekPriceDisplay = default, string high52WeekTimeStamp = default, string highPriceDisplay = default, bool? isDelayed = default, decimal? last = default, string lastPriceDisplay = default, string lastTradingDate = default, decimal? low = default, decimal? low52Week = default, string low52WeekPriceDisplay = default, string low52WeekTimeStamp = default, string lowPriceDisplay = default, decimal? maxPrice = default, string maxPriceDisplay = default, decimal? minMove = default, decimal? minPrice = default, string minPriceDisplay = default, string nameExt = default, decimal? netChange = default, decimal? netChangePct = default, decimal? open = default, string openPriceDisplay = default, decimal? pointValue = default, decimal? previousClose = default, string previousClosePriceDisplay = default, decimal? previousVolume = default, List<string> restrictions = default, decimal? strikePrice = default, string strikePriceDisplay = default, string symbol = default, string symbolRoot = default, decimal? tickSizeTier = default, string tradeTime = default, string underlying = default, decimal? volume = default)
        {
            // to ensure "ask" is required (not null)
            if (ask == null)
            {
                throw new InvalidDataException("ask is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.Ask = ask;
            }
            // to ensure "askPriceDisplay" is required (not null)
            if (askPriceDisplay == null)
            {
                throw new InvalidDataException("askPriceDisplay is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.AskPriceDisplay = askPriceDisplay;
            }
            // to ensure "askSize" is required (not null)
            if (askSize == null)
            {
                throw new InvalidDataException("askSize is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.AskSize = askSize;
            }
            // to ensure "bid" is required (not null)
            if (bid == null)
            {
                throw new InvalidDataException("bid is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.Bid = bid;
            }
            // to ensure "bidPriceDisplay" is required (not null)
            if (bidPriceDisplay == null)
            {
                throw new InvalidDataException("bidPriceDisplay is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.BidPriceDisplay = bidPriceDisplay;
            }
            // to ensure "bidSize" is required (not null)
            if (bidSize == null)
            {
                throw new InvalidDataException("bidSize is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.BidSize = bidSize;
            }
            // to ensure "close" is required (not null)
            if (close == null)
            {
                throw new InvalidDataException("close is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.Close = close;
            }
            // to ensure "closePriceDisplay" is required (not null)
            if (closePriceDisplay == null)
            {
                throw new InvalidDataException("closePriceDisplay is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.ClosePriceDisplay = closePriceDisplay;
            }
            // to ensure "countryCode" is required (not null)
            if (countryCode == null)
            {
                throw new InvalidDataException("countryCode is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.CountryCode = countryCode;
            }
            // to ensure "dailyOpenInterest" is required (not null)
            if (dailyOpenInterest == null)
            {
                throw new InvalidDataException("dailyOpenInterest is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.DailyOpenInterest = dailyOpenInterest;
            }
            // to ensure "dataFeed" is required (not null)
            if (dataFeed == null)
            {
                throw new InvalidDataException("dataFeed is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.DataFeed = dataFeed;
            }
            // to ensure "description" is required (not null)
            if (description == null)
            {
                throw new InvalidDataException("description is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.Description = description;
            }
            // to ensure "displayType" is required (not null)
            if (displayType == null)
            {
                throw new InvalidDataException("displayType is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.DisplayType = displayType;
            }
            // to ensure "exchange" is required (not null)
            if (exchange == null)
            {
                throw new InvalidDataException("exchange is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.Exchange = exchange;
            }
            // to ensure "fractionalDisplay" is required (not null)
            if (fractionalDisplay == null)
            {
                throw new InvalidDataException("fractionalDisplay is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.FractionalDisplay = fractionalDisplay;
            }
            // to ensure "high" is required (not null)
            if (high == null)
            {
                throw new InvalidDataException("high is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.High = high;
            }
            // to ensure "high52Week" is required (not null)
            if (high52Week == null)
            {
                throw new InvalidDataException("high52Week is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.High52Week = high52Week;
            }
            // to ensure "high52WeekPriceDisplay" is required (not null)
            if (high52WeekPriceDisplay == null)
            {
                throw new InvalidDataException("high52WeekPriceDisplay is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.High52WeekPriceDisplay = high52WeekPriceDisplay;
            }
            // to ensure "high52WeekTimeStamp" is required (not null)
            if (high52WeekTimeStamp == null)
            {
                throw new InvalidDataException("high52WeekTimeStamp is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.High52WeekTimeStamp = high52WeekTimeStamp;
            }
            // to ensure "highPriceDisplay" is required (not null)
            if (highPriceDisplay == null)
            {
                throw new InvalidDataException("highPriceDisplay is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.HighPriceDisplay = highPriceDisplay;
            }
            // to ensure "isDelayed" is required (not null)
            if (isDelayed == null)
            {
                throw new InvalidDataException("isDelayed is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.IsDelayed = isDelayed;
            }
            // to ensure "last" is required (not null)
            if (last == null)
            {
                throw new InvalidDataException("last is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.Last = last;
            }
            // to ensure "lastPriceDisplay" is required (not null)
            if (lastPriceDisplay == null)
            {
                throw new InvalidDataException("lastPriceDisplay is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.LastPriceDisplay = lastPriceDisplay;
            }
            // to ensure "low" is required (not null)
            if (low == null)
            {
                throw new InvalidDataException("low is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.Low = low;
            }
            // to ensure "low52Week" is required (not null)
            if (low52Week == null)
            {
                throw new InvalidDataException("low52Week is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.Low52Week = low52Week;
            }
            // to ensure "low52WeekPriceDisplay" is required (not null)
            if (low52WeekPriceDisplay == null)
            {
                throw new InvalidDataException("low52WeekPriceDisplay is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.Low52WeekPriceDisplay = low52WeekPriceDisplay;
            }
            // to ensure "low52WeekTimeStamp" is required (not null)
            if (low52WeekTimeStamp == null)
            {
                throw new InvalidDataException("low52WeekTimeStamp is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.Low52WeekTimeStamp = low52WeekTimeStamp;
            }
            // to ensure "lowPriceDisplay" is required (not null)
            if (lowPriceDisplay == null)
            {
                throw new InvalidDataException("lowPriceDisplay is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.LowPriceDisplay = lowPriceDisplay;
            }
            // to ensure "minMove" is required (not null)
            if (minMove == null)
            {
                throw new InvalidDataException("minMove is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.MinMove = minMove;
            }
            // to ensure "nameExt" is required (not null)
            if (nameExt == null)
            {
                throw new InvalidDataException("nameExt is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.NameExt = nameExt;
            }
            // to ensure "netChange" is required (not null)
            if (netChange == null)
            {
                throw new InvalidDataException("netChange is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.NetChange = netChange;
            }
            // to ensure "netChangePct" is required (not null)
            if (netChangePct == null)
            {
                throw new InvalidDataException("netChangePct is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.NetChangePct = netChangePct;
            }
            // to ensure "open" is required (not null)
            if (open == null)
            {
                throw new InvalidDataException("open is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.Open = open;
            }
            // to ensure "openPriceDisplay" is required (not null)
            if (openPriceDisplay == null)
            {
                throw new InvalidDataException("openPriceDisplay is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.OpenPriceDisplay = openPriceDisplay;
            }
            // to ensure "pointValue" is required (not null)
            if (pointValue == null)
            {
                throw new InvalidDataException("pointValue is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.PointValue = pointValue;
            }
            // to ensure "previousClose" is required (not null)
            if (previousClose == null)
            {
                throw new InvalidDataException("previousClose is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.PreviousClose = previousClose;
            }
            // to ensure "previousClosePriceDisplay" is required (not null)
            if (previousClosePriceDisplay == null)
            {
                throw new InvalidDataException("previousClosePriceDisplay is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.PreviousClosePriceDisplay = previousClosePriceDisplay;
            }
            // to ensure "previousVolume" is required (not null)
            if (previousVolume == null)
            {
                throw new InvalidDataException("previousVolume is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.PreviousVolume = previousVolume;
            }
            // to ensure "restrictions" is required (not null)
            if (restrictions == null)
            {
                throw new InvalidDataException("restrictions is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.Restrictions = restrictions;
            }
            // to ensure "strikePrice" is required (not null)
            if (strikePrice == null)
            {
                throw new InvalidDataException("strikePrice is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.StrikePrice = strikePrice;
            }
            // to ensure "strikePriceDisplay" is required (not null)
            if (strikePriceDisplay == null)
            {
                throw new InvalidDataException("strikePriceDisplay is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.StrikePriceDisplay = strikePriceDisplay;
            }
            // to ensure "symbol" is required (not null)
            if (symbol == null)
            {
                throw new InvalidDataException("symbol is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.Symbol = symbol;
            }
            // to ensure "symbolRoot" is required (not null)
            if (symbolRoot == null)
            {
                throw new InvalidDataException("symbolRoot is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.SymbolRoot = symbolRoot;
            }
            // to ensure "tickSizeTier" is required (not null)
            if (tickSizeTier == null)
            {
                throw new InvalidDataException("tickSizeTier is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.TickSizeTier = tickSizeTier;
            }
            // to ensure "tradeTime" is required (not null)
            if (tradeTime == null)
            {
                throw new InvalidDataException("tradeTime is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.TradeTime = tradeTime;
            }
            // to ensure "underlying" is required (not null)
            if (underlying == null)
            {
                throw new InvalidDataException("underlying is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.Underlying = underlying;
            }
            // to ensure "volume" is required (not null)
            if (volume == null)
            {
                throw new InvalidDataException("volume is a required property for QuoteDefinitionInner and cannot be null");
            }
            else
            {
                this.Volume = volume;
            }
            this.Error = error;
            this.ExpirationDate = expirationDate;
            this.FirstNoticeDate = firstNoticeDate;
            this.Halted = halted;
            this.LastTradingDate = lastTradingDate;
            this.MaxPrice = maxPrice;
            this.MaxPriceDisplay = maxPriceDisplay;
            this.MinPrice = minPrice;
            this.MinPriceDisplay = minPriceDisplay;
        }
        
        /// <summary>
        /// The price at which a security, futures contract, or other financial instrument is offered for sale.
        /// </summary>
        /// <value>The price at which a security, futures contract, or other financial instrument is offered for sale.</value>
        [DataMember(Name="Ask", EmitDefaultValue=false)]
        public decimal? Ask { get; set; }

        /// <summary>
        /// Ask price formatted for display.
        /// </summary>
        /// <value>Ask price formatted for display.</value>
        [DataMember(Name="AskPriceDisplay", EmitDefaultValue=false)]
        public string AskPriceDisplay { get; set; }

        /// <summary>
        /// The number of trading units that prospective sellers are prepared to sell.
        /// </summary>
        /// <value>The number of trading units that prospective sellers are prepared to sell.</value>
        [DataMember(Name="AskSize", EmitDefaultValue=false)]
        public decimal? AskSize { get; set; }


        /// <summary>
        /// The highest price a prospective buyer is prepared to pay at a particular time for a trading unit of a given symbol.
        /// </summary>
        /// <value>The highest price a prospective buyer is prepared to pay at a particular time for a trading unit of a given symbol.</value>
        [DataMember(Name="Bid", EmitDefaultValue=false)]
        public decimal? Bid { get; set; }

        /// <summary>
        /// Bid price formatted for display.
        /// </summary>
        /// <value>Bid price formatted for display.</value>
        [DataMember(Name="BidPriceDisplay", EmitDefaultValue=false)]
        public string BidPriceDisplay { get; set; }

        /// <summary>
        /// The number of trading units that prospective buyers are prepared to purchase for a symbol.
        /// </summary>
        /// <value>The number of trading units that prospective buyers are prepared to purchase for a symbol.</value>
        [DataMember(Name="BidSize", EmitDefaultValue=false)]
        public decimal? BidSize { get; set; }

        /// <summary>
        /// The net Realized Profit or Loss denominated in the symbol currency for the current trading session. This value includes any gain or loss as a result of closing a position during the current trading session
        /// </summary>
        /// <value>The net Realized Profit or Loss denominated in the symbol currency for the current trading session. This value includes any gain or loss as a result of closing a position during the current trading session</value>
        [DataMember(Name="Close", EmitDefaultValue=false)]
        public decimal? Close { get; set; }

        /// <summary>
        /// Daily running close price formatted for display.
        /// </summary>
        /// <value>Daily running close price formatted for display.</value>
        [DataMember(Name="ClosePriceDisplay", EmitDefaultValue=false)]
        public string ClosePriceDisplay { get; set; }

        /// <summary>
        /// The country of the exchange where the symbol is listed.
        /// </summary>
        /// <value>The country of the exchange where the symbol is listed.</value>
        [DataMember(Name="CountryCode", EmitDefaultValue=false)]
        public string CountryCode { get; set; }


        /// <summary>
        /// The total number of open or outstanding (not closed or delivered) options and/or futures contracts that exist on a given day, delivered on a particular day.
        /// </summary>
        /// <value>The total number of open or outstanding (not closed or delivered) options and/or futures contracts that exist on a given day, delivered on a particular day.</value>
        [DataMember(Name="DailyOpenInterest", EmitDefaultValue=false)]
        public decimal? DailyOpenInterest { get; set; }

        /// <summary>
        /// The data quote feed provider.
        /// </summary>
        /// <value>The data quote feed provider.</value>
        [DataMember(Name="DataFeed", EmitDefaultValue=false)]
        public string DataFeed { get; set; }

        /// <summary>
        /// Displays the full name of the symbol.
        /// </summary>
        /// <value>Displays the full name of the symbol.</value>
        [DataMember(Name="Description", EmitDefaultValue=false)]
        public string Description { get; set; }

        /// <summary>
        /// Symbol&#39;s price display type based on the following list:  * &#x60;0&#x60; \&quot;Automatic\&quot; Not used * &#x60;1&#x60; 0 Decimals &#x3D;&gt; 1 * &#x60;2&#x60; 1 Decimals &#x3D;&gt; .1 * &#x60;3&#x60; 2 Decimals &#x3D;&gt; .01 * &#x60;4&#x60; 3 Decimals &#x3D;&gt; .001 * &#x60;5&#x60; 4 Decimals &#x3D;&gt; .0001 * &#x60;6&#x60; 5 Decimals &#x3D;&gt; .00001 * &#x60;7&#x60; Simplest Fraction * &#x60;8&#x60; 1/2-Halves &#x3D;&gt; .5 * &#x60;9&#x60; 1/4-Fourths &#x3D;&gt; .25 * &#x60;10&#x60; 1/8-Eights &#x3D;&gt; .125 * &#x60;11&#x60; 1/16-Sixteenths &#x3D;&gt; .0625 * &#x60;12&#x60; 1/32-ThirtySeconds &#x3D;&gt; .03125 * &#x60;13&#x60; 1/64-SixtyFourths &#x3D;&gt; .015625 * &#x60;14&#x60; 1/128-OneTwentyEigths &#x3D;&gt; .0078125 * &#x60;15&#x60; 1/256-TwoFiftySixths &#x3D;&gt; .003906250 * &#x60;16&#x60; 10ths and Quarters &#x3D;&gt; .025 * &#x60;17&#x60; 32nds and Halves &#x3D;&gt; .015625 * &#x60;18&#x60; 32nds and Quarters &#x3D;&gt; .0078125 * &#x60;19&#x60; 32nds and Eights &#x3D;&gt; .00390625 * &#x60;20&#x60; 32nds and Tenths &#x3D;&gt; .003125 * &#x60;21&#x60; 64ths and Halves &#x3D;&gt; .0078125 * &#x60;22&#x60; 64ths and Tenths &#x3D;&gt; .0015625 * &#x60;23&#x60; 6 Decimals &#x3D;&gt; .000001 
        /// </summary>
        /// <value>Symbol&#39;s price display type based on the following list:  * &#x60;0&#x60; \&quot;Automatic\&quot; Not used * &#x60;1&#x60; 0 Decimals &#x3D;&gt; 1 * &#x60;2&#x60; 1 Decimals &#x3D;&gt; .1 * &#x60;3&#x60; 2 Decimals &#x3D;&gt; .01 * &#x60;4&#x60; 3 Decimals &#x3D;&gt; .001 * &#x60;5&#x60; 4 Decimals &#x3D;&gt; .0001 * &#x60;6&#x60; 5 Decimals &#x3D;&gt; .00001 * &#x60;7&#x60; Simplest Fraction * &#x60;8&#x60; 1/2-Halves &#x3D;&gt; .5 * &#x60;9&#x60; 1/4-Fourths &#x3D;&gt; .25 * &#x60;10&#x60; 1/8-Eights &#x3D;&gt; .125 * &#x60;11&#x60; 1/16-Sixteenths &#x3D;&gt; .0625 * &#x60;12&#x60; 1/32-ThirtySeconds &#x3D;&gt; .03125 * &#x60;13&#x60; 1/64-SixtyFourths &#x3D;&gt; .015625 * &#x60;14&#x60; 1/128-OneTwentyEigths &#x3D;&gt; .0078125 * &#x60;15&#x60; 1/256-TwoFiftySixths &#x3D;&gt; .003906250 * &#x60;16&#x60; 10ths and Quarters &#x3D;&gt; .025 * &#x60;17&#x60; 32nds and Halves &#x3D;&gt; .015625 * &#x60;18&#x60; 32nds and Quarters &#x3D;&gt; .0078125 * &#x60;19&#x60; 32nds and Eights &#x3D;&gt; .00390625 * &#x60;20&#x60; 32nds and Tenths &#x3D;&gt; .003125 * &#x60;21&#x60; 64ths and Halves &#x3D;&gt; .0078125 * &#x60;22&#x60; 64ths and Tenths &#x3D;&gt; .0015625 * &#x60;23&#x60; 6 Decimals &#x3D;&gt; .000001 </value>
        [DataMember(Name="DisplayType", EmitDefaultValue=false)]
        public decimal? DisplayType { get; set; }

        /// <summary>
        /// Error message received from exchange or Tradestation.
        /// </summary>
        /// <value>Error message received from exchange or Tradestation.</value>
        [DataMember(Name="Error", EmitDefaultValue=false)]
        public string Error { get; set; }

        /// <summary>
        /// Name of exchange where this symbol is traded in.
        /// </summary>
        /// <value>Name of exchange where this symbol is traded in.</value>
        [DataMember(Name="Exchange", EmitDefaultValue=false)]
        public string Exchange { get; set; }

        /// <summary>
        /// The UTC expiration date of a contract in ISO 8601 date format (yyyy-mm-dd). Valid only on options and futures symbols. For other categories this field is empty.
        /// </summary>
        /// <value>The UTC expiration date of a contract in ISO 8601 date format (yyyy-mm-dd). Valid only on options and futures symbols. For other categories this field is empty.</value>
        [DataMember(Name="ExpirationDate", EmitDefaultValue=false)]
        public string ExpirationDate { get; set; }

        /// <summary>
        /// The day after which an investor who has purchased a futures contract may be required to take physical delivery of the contracts underlying commodity.
        /// </summary>
        /// <value>The day after which an investor who has purchased a futures contract may be required to take physical delivery of the contracts underlying commodity.</value>
        [DataMember(Name="FirstNoticeDate", EmitDefaultValue=false)]
        public string FirstNoticeDate { get; set; }

        /// <summary>
        /// Determine whether fractional price display is required.
        /// </summary>
        /// <value>Determine whether fractional price display is required.</value>
        [DataMember(Name="FractionalDisplay", EmitDefaultValue=false)]
        public bool? FractionalDisplay { get; set; }

        /// <summary>
        /// A temporary suspension of trading for a particular security or securities at one exchange or across numerous exchanges.
        /// </summary>
        /// <value>A temporary suspension of trading for a particular security or securities at one exchange or across numerous exchanges.</value>
        [DataMember(Name="Halted", EmitDefaultValue=false)]
        public bool? Halted { get; set; }

        /// <summary>
        /// Highest price of the day.
        /// </summary>
        /// <value>Highest price of the day.</value>
        [DataMember(Name="High", EmitDefaultValue=false)]
        public decimal? High { get; set; }

        /// <summary>
        /// The highest price of the past 52 weeks. This is a grid-based indicator.
        /// </summary>
        /// <value>The highest price of the past 52 weeks. This is a grid-based indicator.</value>
        [DataMember(Name="High52Week", EmitDefaultValue=false)]
        public decimal? High52Week { get; set; }

        /// <summary>
        /// High52Week price formatted for display.
        /// </summary>
        /// <value>High52Week price formatted for display.</value>
        [DataMember(Name="High52WeekPriceDisplay", EmitDefaultValue=false)]
        public string High52WeekPriceDisplay { get; set; }

        /// <summary>
        /// Date and time of the highest price in the past 52 week.
        /// </summary>
        /// <value>Date and time of the highest price in the past 52 week.</value>
        [DataMember(Name="High52WeekTimeStamp", EmitDefaultValue=false)]
        public string High52WeekTimeStamp { get; set; }

        /// <summary>
        /// High price formatted for display.
        /// </summary>
        /// <value>High price formatted for display.</value>
        [DataMember(Name="HighPriceDisplay", EmitDefaultValue=false)]
        public string HighPriceDisplay { get; set; }

        /// <summary>
        /// True if the quote is a delayed quote and False if the quote is a real-time quote
        /// </summary>
        /// <value>True if the quote is a delayed quote and False if the quote is a real-time quote</value>
        [DataMember(Name="IsDelayed", EmitDefaultValue=false)]
        public bool? IsDelayed { get; set; }

        /// <summary>
        /// The last price at which the symbol traded.
        /// </summary>
        /// <value>The last price at which the symbol traded.</value>
        [DataMember(Name="Last", EmitDefaultValue=false)]
        public decimal? Last { get; set; }

        /// <summary>
        /// Last price formatted for display.
        /// </summary>
        /// <value>Last price formatted for display.</value>
        [DataMember(Name="LastPriceDisplay", EmitDefaultValue=false)]
        public string LastPriceDisplay { get; set; }

        /// <summary>
        /// The final day that a futures contract may trade or be closed out before the delivery of the underlying asset or cash settlement must occur.
        /// </summary>
        /// <value>The final day that a futures contract may trade or be closed out before the delivery of the underlying asset or cash settlement must occur.</value>
        [DataMember(Name="LastTradingDate", EmitDefaultValue=false)]
        public string LastTradingDate { get; set; }

        /// <summary>
        /// Lowest price of stock.
        /// </summary>
        /// <value>Lowest price of stock.</value>
        [DataMember(Name="Low", EmitDefaultValue=false)]
        public decimal? Low { get; set; }

        /// <summary>
        /// The lowest price of the past 52 weeks.
        /// </summary>
        /// <value>The lowest price of the past 52 weeks.</value>
        [DataMember(Name="Low52Week", EmitDefaultValue=false)]
        public decimal? Low52Week { get; set; }

        /// <summary>
        /// Low52Week price formatted for display.
        /// </summary>
        /// <value>Low52Week price formatted for display.</value>
        [DataMember(Name="Low52WeekPriceDisplay", EmitDefaultValue=false)]
        public string Low52WeekPriceDisplay { get; set; }

        /// <summary>
        /// Date and time of the lowest price of the past 52 weeks.
        /// </summary>
        /// <value>Date and time of the lowest price of the past 52 weeks.</value>
        [DataMember(Name="Low52WeekTimeStamp", EmitDefaultValue=false)]
        public string Low52WeekTimeStamp { get; set; }

        /// <summary>
        /// Low price formatted for display.
        /// </summary>
        /// <value>Low price formatted for display.</value>
        [DataMember(Name="LowPriceDisplay", EmitDefaultValue=false)]
        public string LowPriceDisplay { get; set; }

        /// <summary>
        /// The maximum price a commodity futures contract may be traded for the current session.
        /// </summary>
        /// <value>The maximum price a commodity futures contract may be traded for the current session.</value>
        [DataMember(Name="MaxPrice", EmitDefaultValue=false)]
        public decimal? MaxPrice { get; set; }

        /// <summary>
        /// MaxPrice formatted for display.
        /// </summary>
        /// <value>MaxPrice formatted for display.</value>
        [DataMember(Name="MaxPriceDisplay", EmitDefaultValue=false)]
        public string MaxPriceDisplay { get; set; }

        /// <summary>
        /// Multiplying factor using the display type to determine the minimum price increment the asset trades in. For options the MinMove may vary. If the MinMove is negative, then the MinMove is dependent on the price. The whole number portion of the min move is the threshold. The leftmost two digits to the right of the decimal (X.XXXX) indicate the min move beneath the threshold, and the rightmost two digits (X.XXXX) indicate the min move above the threshold.
        /// </summary>
        /// <value>Multiplying factor using the display type to determine the minimum price increment the asset trades in. For options the MinMove may vary. If the MinMove is negative, then the MinMove is dependent on the price. The whole number portion of the min move is the threshold. The leftmost two digits to the right of the decimal (X.XXXX) indicate the min move beneath the threshold, and the rightmost two digits (X.XXXX) indicate the min move above the threshold.</value>
        [DataMember(Name="MinMove", EmitDefaultValue=false)]
        public decimal? MinMove { get; set; }

        /// <summary>
        /// The minimum price a commodity futures contract may be traded for the current session.
        /// </summary>
        /// <value>The minimum price a commodity futures contract may be traded for the current session.</value>
        [DataMember(Name="MinPrice", EmitDefaultValue=false)]
        public decimal? MinPrice { get; set; }

        /// <summary>
        /// MinPrice formatted for display.
        /// </summary>
        /// <value>MinPrice formatted for display.</value>
        [DataMember(Name="MinPriceDisplay", EmitDefaultValue=false)]
        public string MinPriceDisplay { get; set; }

        /// <summary>
        /// If the Quote is delayed this property will be set to &#x60;D&#x60;
        /// </summary>
        /// <value>If the Quote is delayed this property will be set to &#x60;D&#x60;</value>
        [DataMember(Name="NameExt", EmitDefaultValue=false)]
        public string NameExt { get; set; }

        /// <summary>
        /// The difference between the last displayed price and the previous day&#x60;s close.
        /// </summary>
        /// <value>The difference between the last displayed price and the previous day&#x60;s close.</value>
        [DataMember(Name="NetChange", EmitDefaultValue=false)]
        public decimal? NetChange { get; set; }

        /// <summary>
        /// The difference between the current price and the previous day&#x60;s close, expressed in a percentage.
        /// </summary>
        /// <value>The difference between the current price and the previous day&#x60;s close, expressed in a percentage.</value>
        [DataMember(Name="NetChangePct", EmitDefaultValue=false)]
        public decimal? NetChangePct { get; set; }

        /// <summary>
        /// The unrealized profit or loss denominated in the symbol currency on the position held, calculated based on the average price of the position.
        /// </summary>
        /// <value>The unrealized profit or loss denominated in the symbol currency on the position held, calculated based on the average price of the position.</value>
        [DataMember(Name="Open", EmitDefaultValue=false)]
        public decimal? Open { get; set; }

        /// <summary>
        /// Open price formatted for display.
        /// </summary>
        /// <value>Open price formatted for display.</value>
        [DataMember(Name="OpenPriceDisplay", EmitDefaultValue=false)]
        public string OpenPriceDisplay { get; set; }

        /// <summary>
        /// The currency value represented by a full point of price movement. In the case of stocks, the Big Point Value is usually 1, in that 1 point of movement represents 1 dollar, however it may vary.
        /// </summary>
        /// <value>The currency value represented by a full point of price movement. In the case of stocks, the Big Point Value is usually 1, in that 1 point of movement represents 1 dollar, however it may vary.</value>
        [DataMember(Name="PointValue", EmitDefaultValue=false)]
        public decimal? PointValue { get; set; }

        /// <summary>
        /// The closing price of the previous day.
        /// </summary>
        /// <value>The closing price of the previous day.</value>
        [DataMember(Name="PreviousClose", EmitDefaultValue=false)]
        public decimal? PreviousClose { get; set; }

        /// <summary>
        /// PreviousClose price formatted for display.
        /// </summary>
        /// <value>PreviousClose price formatted for display.</value>
        [DataMember(Name="PreviousClosePriceDisplay", EmitDefaultValue=false)]
        public string PreviousClosePriceDisplay { get; set; }

        /// <summary>
        /// Daily volume of the previous day.
        /// </summary>
        /// <value>Daily volume of the previous day.</value>
        [DataMember(Name="PreviousVolume", EmitDefaultValue=false)]
        public decimal? PreviousVolume { get; set; }

        /// <summary>
        /// A symbol specific restrictions for Japanese equities.
        /// </summary>
        /// <value>A symbol specific restrictions for Japanese equities.</value>
        [DataMember(Name="Restrictions", EmitDefaultValue=false)]
        public List<string> Restrictions { get; set; }

        /// <summary>
        /// The price at which the underlying contract will be delivered in the event an option is exercised.
        /// </summary>
        /// <value>The price at which the underlying contract will be delivered in the event an option is exercised.</value>
        [DataMember(Name="StrikePrice", EmitDefaultValue=false)]
        public decimal? StrikePrice { get; set; }

        /// <summary>
        /// StrikePrice formatted for display.
        /// </summary>
        /// <value>StrikePrice formatted for display.</value>
        [DataMember(Name="StrikePriceDisplay", EmitDefaultValue=false)]
        public string StrikePriceDisplay { get; set; }

        /// <summary>
        /// The name identifying the financial instrument for which the data is displayed.
        /// </summary>
        /// <value>The name identifying the financial instrument for which the data is displayed.</value>
        [DataMember(Name="Symbol", EmitDefaultValue=false)]
        public string Symbol { get; set; }

        /// <summary>
        /// The symbol used to identify the option&#x60;s financial instrument for a specific underlying asset that is traded on the various trading exchanges.
        /// </summary>
        /// <value>The symbol used to identify the option&#x60;s financial instrument for a specific underlying asset that is traded on the various trading exchanges.</value>
        [DataMember(Name="SymbolRoot", EmitDefaultValue=false)]
        public string SymbolRoot { get; set; }

        /// <summary>
        /// Trading increment based on a level group.
        /// </summary>
        /// <value>Trading increment based on a level group.</value>
        [DataMember(Name="TickSizeTier", EmitDefaultValue=false)]
        public decimal? TickSizeTier { get; set; }

        /// <summary>
        /// Trade execution time.
        /// </summary>
        /// <value>Trade execution time.</value>
        [DataMember(Name="TradeTime", EmitDefaultValue=false)]
        public string TradeTime { get; set; }

        /// <summary>
        /// The financial instrument on which an option contract is based or derived.
        /// </summary>
        /// <value>The financial instrument on which an option contract is based or derived.</value>
        [DataMember(Name="Underlying", EmitDefaultValue=false)]
        public string Underlying { get; set; }

        /// <summary>
        /// The number of shares or contracts traded in a security or an entire market during a given period of time.
        /// </summary>
        /// <value>The number of shares or contracts traded in a security or an entire market during a given period of time.</value>
        [DataMember(Name="Volume", EmitDefaultValue=false)]
        public decimal? Volume { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class QuoteDefinitionInner {\n");
            sb.Append("  Ask: ").Append(Ask).Append("\n");
            sb.Append("  AskPriceDisplay: ").Append(AskPriceDisplay).Append("\n");
            sb.Append("  AskSize: ").Append(AskSize).Append("\n");
            sb.Append("  AssetType: ").Append(AssetType).Append("\n");
            sb.Append("  Bid: ").Append(Bid).Append("\n");
            sb.Append("  BidPriceDisplay: ").Append(BidPriceDisplay).Append("\n");
            sb.Append("  BidSize: ").Append(BidSize).Append("\n");
            sb.Append("  Close: ").Append(Close).Append("\n");
            sb.Append("  ClosePriceDisplay: ").Append(ClosePriceDisplay).Append("\n");
            sb.Append("  CountryCode: ").Append(CountryCode).Append("\n");
            sb.Append("  Currency: ").Append(Currency).Append("\n");
            sb.Append("  DailyOpenInterest: ").Append(DailyOpenInterest).Append("\n");
            sb.Append("  DataFeed: ").Append(DataFeed).Append("\n");
            sb.Append("  Description: ").Append(Description).Append("\n");
            sb.Append("  DisplayType: ").Append(DisplayType).Append("\n");
            sb.Append("  Error: ").Append(Error).Append("\n");
            sb.Append("  Exchange: ").Append(Exchange).Append("\n");
            sb.Append("  ExpirationDate: ").Append(ExpirationDate).Append("\n");
            sb.Append("  FirstNoticeDate: ").Append(FirstNoticeDate).Append("\n");
            sb.Append("  FractionalDisplay: ").Append(FractionalDisplay).Append("\n");
            sb.Append("  Halted: ").Append(Halted).Append("\n");
            sb.Append("  High: ").Append(High).Append("\n");
            sb.Append("  High52Week: ").Append(High52Week).Append("\n");
            sb.Append("  High52WeekPriceDisplay: ").Append(High52WeekPriceDisplay).Append("\n");
            sb.Append("  High52WeekTimeStamp: ").Append(High52WeekTimeStamp).Append("\n");
            sb.Append("  HighPriceDisplay: ").Append(HighPriceDisplay).Append("\n");
            sb.Append("  IsDelayed: ").Append(IsDelayed).Append("\n");
            sb.Append("  Last: ").Append(Last).Append("\n");
            sb.Append("  LastPriceDisplay: ").Append(LastPriceDisplay).Append("\n");
            sb.Append("  LastTradingDate: ").Append(LastTradingDate).Append("\n");
            sb.Append("  Low: ").Append(Low).Append("\n");
            sb.Append("  Low52Week: ").Append(Low52Week).Append("\n");
            sb.Append("  Low52WeekPriceDisplay: ").Append(Low52WeekPriceDisplay).Append("\n");
            sb.Append("  Low52WeekTimeStamp: ").Append(Low52WeekTimeStamp).Append("\n");
            sb.Append("  LowPriceDisplay: ").Append(LowPriceDisplay).Append("\n");
            sb.Append("  MaxPrice: ").Append(MaxPrice).Append("\n");
            sb.Append("  MaxPriceDisplay: ").Append(MaxPriceDisplay).Append("\n");
            sb.Append("  MinMove: ").Append(MinMove).Append("\n");
            sb.Append("  MinPrice: ").Append(MinPrice).Append("\n");
            sb.Append("  MinPriceDisplay: ").Append(MinPriceDisplay).Append("\n");
            sb.Append("  NameExt: ").Append(NameExt).Append("\n");
            sb.Append("  NetChange: ").Append(NetChange).Append("\n");
            sb.Append("  NetChangePct: ").Append(NetChangePct).Append("\n");
            sb.Append("  Open: ").Append(Open).Append("\n");
            sb.Append("  OpenPriceDisplay: ").Append(OpenPriceDisplay).Append("\n");
            sb.Append("  PointValue: ").Append(PointValue).Append("\n");
            sb.Append("  PreviousClose: ").Append(PreviousClose).Append("\n");
            sb.Append("  PreviousClosePriceDisplay: ").Append(PreviousClosePriceDisplay).Append("\n");
            sb.Append("  PreviousVolume: ").Append(PreviousVolume).Append("\n");
            sb.Append("  Restrictions: ").Append(Restrictions).Append("\n");
            sb.Append("  StrikePrice: ").Append(StrikePrice).Append("\n");
            sb.Append("  StrikePriceDisplay: ").Append(StrikePriceDisplay).Append("\n");
            sb.Append("  Symbol: ").Append(Symbol).Append("\n");
            sb.Append("  SymbolRoot: ").Append(SymbolRoot).Append("\n");
            sb.Append("  TickSizeTier: ").Append(TickSizeTier).Append("\n");
            sb.Append("  TradeTime: ").Append(TradeTime).Append("\n");
            sb.Append("  Underlying: ").Append(Underlying).Append("\n");
            sb.Append("  Volume: ").Append(Volume).Append("\n");
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
            return this.Equals(input as QuoteDefinitionInner);
        }

        /// <summary>
        /// Returns true if QuoteDefinitionInner instances are equal
        /// </summary>
        /// <param name="input">Instance of QuoteDefinitionInner to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(QuoteDefinitionInner input)
        {
            if (input == null)
                return false;

            return 
                (
                    this.Ask == input.Ask ||
                    (this.Ask != null &&
                    this.Ask.Equals(input.Ask))
                ) && 
                (
                    this.AskPriceDisplay == input.AskPriceDisplay ||
                    (this.AskPriceDisplay != null &&
                    this.AskPriceDisplay.Equals(input.AskPriceDisplay))
                ) && 
                (
                    this.AskSize == input.AskSize ||
                    (this.AskSize != null &&
                    this.AskSize.Equals(input.AskSize))
                ) && 
                (
                    this.AssetType == input.AssetType ||
                    this.AssetType.Equals(input.AssetType)
                ) && 
                (
                    this.Bid == input.Bid ||
                    (this.Bid != null &&
                    this.Bid.Equals(input.Bid))
                ) && 
                (
                    this.BidPriceDisplay == input.BidPriceDisplay ||
                    (this.BidPriceDisplay != null &&
                    this.BidPriceDisplay.Equals(input.BidPriceDisplay))
                ) && 
                (
                    this.BidSize == input.BidSize ||
                    (this.BidSize != null &&
                    this.BidSize.Equals(input.BidSize))
                ) && 
                (
                    this.Close == input.Close ||
                    (this.Close != null &&
                    this.Close.Equals(input.Close))
                ) && 
                (
                    this.ClosePriceDisplay == input.ClosePriceDisplay ||
                    (this.ClosePriceDisplay != null &&
                    this.ClosePriceDisplay.Equals(input.ClosePriceDisplay))
                ) && 
                (
                    this.CountryCode == input.CountryCode ||
                    (this.CountryCode != null &&
                    this.CountryCode.Equals(input.CountryCode))
                ) && 
                (
                    this.Currency == input.Currency ||
                    this.Currency.Equals(input.Currency)
                ) && 
                (
                    this.DailyOpenInterest == input.DailyOpenInterest ||
                    (this.DailyOpenInterest != null &&
                    this.DailyOpenInterest.Equals(input.DailyOpenInterest))
                ) && 
                (
                    this.DataFeed == input.DataFeed ||
                    (this.DataFeed != null &&
                    this.DataFeed.Equals(input.DataFeed))
                ) && 
                (
                    this.Description == input.Description ||
                    (this.Description != null &&
                    this.Description.Equals(input.Description))
                ) && 
                (
                    this.DisplayType == input.DisplayType ||
                    (this.DisplayType != null &&
                    this.DisplayType.Equals(input.DisplayType))
                ) && 
                (
                    this.Error == input.Error ||
                    (this.Error != null &&
                    this.Error.Equals(input.Error))
                ) && 
                (
                    this.Exchange == input.Exchange ||
                    (this.Exchange != null &&
                    this.Exchange.Equals(input.Exchange))
                ) && 
                (
                    this.ExpirationDate == input.ExpirationDate ||
                    (this.ExpirationDate != null &&
                    this.ExpirationDate.Equals(input.ExpirationDate))
                ) && 
                (
                    this.FirstNoticeDate == input.FirstNoticeDate ||
                    (this.FirstNoticeDate != null &&
                    this.FirstNoticeDate.Equals(input.FirstNoticeDate))
                ) && 
                (
                    this.FractionalDisplay == input.FractionalDisplay ||
                    (this.FractionalDisplay != null &&
                    this.FractionalDisplay.Equals(input.FractionalDisplay))
                ) && 
                (
                    this.Halted == input.Halted ||
                    (this.Halted != null &&
                    this.Halted.Equals(input.Halted))
                ) && 
                (
                    this.High == input.High ||
                    (this.High != null &&
                    this.High.Equals(input.High))
                ) && 
                (
                    this.High52Week == input.High52Week ||
                    (this.High52Week != null &&
                    this.High52Week.Equals(input.High52Week))
                ) && 
                (
                    this.High52WeekPriceDisplay == input.High52WeekPriceDisplay ||
                    (this.High52WeekPriceDisplay != null &&
                    this.High52WeekPriceDisplay.Equals(input.High52WeekPriceDisplay))
                ) && 
                (
                    this.High52WeekTimeStamp == input.High52WeekTimeStamp ||
                    (this.High52WeekTimeStamp != null &&
                    this.High52WeekTimeStamp.Equals(input.High52WeekTimeStamp))
                ) && 
                (
                    this.HighPriceDisplay == input.HighPriceDisplay ||
                    (this.HighPriceDisplay != null &&
                    this.HighPriceDisplay.Equals(input.HighPriceDisplay))
                ) && 
                (
                    this.IsDelayed == input.IsDelayed ||
                    (this.IsDelayed != null &&
                    this.IsDelayed.Equals(input.IsDelayed))
                ) && 
                (
                    this.Last == input.Last ||
                    (this.Last != null &&
                    this.Last.Equals(input.Last))
                ) && 
                (
                    this.LastPriceDisplay == input.LastPriceDisplay ||
                    (this.LastPriceDisplay != null &&
                    this.LastPriceDisplay.Equals(input.LastPriceDisplay))
                ) && 
                (
                    this.LastTradingDate == input.LastTradingDate ||
                    (this.LastTradingDate != null &&
                    this.LastTradingDate.Equals(input.LastTradingDate))
                ) && 
                (
                    this.Low == input.Low ||
                    (this.Low != null &&
                    this.Low.Equals(input.Low))
                ) && 
                (
                    this.Low52Week == input.Low52Week ||
                    (this.Low52Week != null &&
                    this.Low52Week.Equals(input.Low52Week))
                ) && 
                (
                    this.Low52WeekPriceDisplay == input.Low52WeekPriceDisplay ||
                    (this.Low52WeekPriceDisplay != null &&
                    this.Low52WeekPriceDisplay.Equals(input.Low52WeekPriceDisplay))
                ) && 
                (
                    this.Low52WeekTimeStamp == input.Low52WeekTimeStamp ||
                    (this.Low52WeekTimeStamp != null &&
                    this.Low52WeekTimeStamp.Equals(input.Low52WeekTimeStamp))
                ) && 
                (
                    this.LowPriceDisplay == input.LowPriceDisplay ||
                    (this.LowPriceDisplay != null &&
                    this.LowPriceDisplay.Equals(input.LowPriceDisplay))
                ) && 
                (
                    this.MaxPrice == input.MaxPrice ||
                    (this.MaxPrice != null &&
                    this.MaxPrice.Equals(input.MaxPrice))
                ) && 
                (
                    this.MaxPriceDisplay == input.MaxPriceDisplay ||
                    (this.MaxPriceDisplay != null &&
                    this.MaxPriceDisplay.Equals(input.MaxPriceDisplay))
                ) && 
                (
                    this.MinMove == input.MinMove ||
                    (this.MinMove != null &&
                    this.MinMove.Equals(input.MinMove))
                ) && 
                (
                    this.MinPrice == input.MinPrice ||
                    (this.MinPrice != null &&
                    this.MinPrice.Equals(input.MinPrice))
                ) && 
                (
                    this.MinPriceDisplay == input.MinPriceDisplay ||
                    (this.MinPriceDisplay != null &&
                    this.MinPriceDisplay.Equals(input.MinPriceDisplay))
                ) && 
                (
                    this.NameExt == input.NameExt ||
                    (this.NameExt != null &&
                    this.NameExt.Equals(input.NameExt))
                ) && 
                (
                    this.NetChange == input.NetChange ||
                    (this.NetChange != null &&
                    this.NetChange.Equals(input.NetChange))
                ) && 
                (
                    this.NetChangePct == input.NetChangePct ||
                    (this.NetChangePct != null &&
                    this.NetChangePct.Equals(input.NetChangePct))
                ) && 
                (
                    this.Open == input.Open ||
                    (this.Open != null &&
                    this.Open.Equals(input.Open))
                ) && 
                (
                    this.OpenPriceDisplay == input.OpenPriceDisplay ||
                    (this.OpenPriceDisplay != null &&
                    this.OpenPriceDisplay.Equals(input.OpenPriceDisplay))
                ) && 
                (
                    this.PointValue == input.PointValue ||
                    (this.PointValue != null &&
                    this.PointValue.Equals(input.PointValue))
                ) && 
                (
                    this.PreviousClose == input.PreviousClose ||
                    (this.PreviousClose != null &&
                    this.PreviousClose.Equals(input.PreviousClose))
                ) && 
                (
                    this.PreviousClosePriceDisplay == input.PreviousClosePriceDisplay ||
                    (this.PreviousClosePriceDisplay != null &&
                    this.PreviousClosePriceDisplay.Equals(input.PreviousClosePriceDisplay))
                ) && 
                (
                    this.PreviousVolume == input.PreviousVolume ||
                    (this.PreviousVolume != null &&
                    this.PreviousVolume.Equals(input.PreviousVolume))
                ) && 
                (
                    this.Restrictions == input.Restrictions ||
                    this.Restrictions != null &&
                    this.Restrictions.SequenceEqual(input.Restrictions)
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
                    this.SymbolRoot == input.SymbolRoot ||
                    (this.SymbolRoot != null &&
                    this.SymbolRoot.Equals(input.SymbolRoot))
                ) && 
                (
                    this.TickSizeTier == input.TickSizeTier ||
                    (this.TickSizeTier != null &&
                    this.TickSizeTier.Equals(input.TickSizeTier))
                ) && 
                (
                    this.TradeTime == input.TradeTime ||
                    (this.TradeTime != null &&
                    this.TradeTime.Equals(input.TradeTime))
                ) && 
                (
                    this.Underlying == input.Underlying ||
                    (this.Underlying != null &&
                    this.Underlying.Equals(input.Underlying))
                ) && 
                (
                    this.Volume == input.Volume ||
                    (this.Volume != null &&
                    this.Volume.Equals(input.Volume))
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
                if (this.Ask != null)
                    hashCode = hashCode * 59 + this.Ask.GetHashCode();
                if (this.AskPriceDisplay != null)
                    hashCode = hashCode * 59 + this.AskPriceDisplay.GetHashCode();
                if (this.AskSize != null)
                    hashCode = hashCode * 59 + this.AskSize.GetHashCode();
                //if (this.AssetType != null)
                    hashCode = hashCode * 59 + this.AssetType.GetHashCode();
                if (this.Bid != null)
                    hashCode = hashCode * 59 + this.Bid.GetHashCode();
                if (this.BidPriceDisplay != null)
                    hashCode = hashCode * 59 + this.BidPriceDisplay.GetHashCode();
                if (this.BidSize != null)
                    hashCode = hashCode * 59 + this.BidSize.GetHashCode();
                if (this.Close != null)
                    hashCode = hashCode * 59 + this.Close.GetHashCode();
                if (this.ClosePriceDisplay != null)
                    hashCode = hashCode * 59 + this.ClosePriceDisplay.GetHashCode();
                if (this.CountryCode != null)
                    hashCode = hashCode * 59 + this.CountryCode.GetHashCode();
                //if (this.Currency != null)
                    hashCode = hashCode * 59 + this.Currency.GetHashCode();
                if (this.DailyOpenInterest != null)
                    hashCode = hashCode * 59 + this.DailyOpenInterest.GetHashCode();
                if (this.DataFeed != null)
                    hashCode = hashCode * 59 + this.DataFeed.GetHashCode();
                if (this.Description != null)
                    hashCode = hashCode * 59 + this.Description.GetHashCode();
                if (this.DisplayType != null)
                    hashCode = hashCode * 59 + this.DisplayType.GetHashCode();
                if (this.Error != null)
                    hashCode = hashCode * 59 + this.Error.GetHashCode();
                if (this.Exchange != null)
                    hashCode = hashCode * 59 + this.Exchange.GetHashCode();
                if (this.ExpirationDate != null)
                    hashCode = hashCode * 59 + this.ExpirationDate.GetHashCode();
                if (this.FirstNoticeDate != null)
                    hashCode = hashCode * 59 + this.FirstNoticeDate.GetHashCode();
                if (this.FractionalDisplay != null)
                    hashCode = hashCode * 59 + this.FractionalDisplay.GetHashCode();
                if (this.Halted != null)
                    hashCode = hashCode * 59 + this.Halted.GetHashCode();
                if (this.High != null)
                    hashCode = hashCode * 59 + this.High.GetHashCode();
                if (this.High52Week != null)
                    hashCode = hashCode * 59 + this.High52Week.GetHashCode();
                if (this.High52WeekPriceDisplay != null)
                    hashCode = hashCode * 59 + this.High52WeekPriceDisplay.GetHashCode();
                if (this.High52WeekTimeStamp != null)
                    hashCode = hashCode * 59 + this.High52WeekTimeStamp.GetHashCode();
                if (this.HighPriceDisplay != null)
                    hashCode = hashCode * 59 + this.HighPriceDisplay.GetHashCode();
                if (this.IsDelayed != null)
                    hashCode = hashCode * 59 + this.IsDelayed.GetHashCode();
                if (this.Last != null)
                    hashCode = hashCode * 59 + this.Last.GetHashCode();
                if (this.LastPriceDisplay != null)
                    hashCode = hashCode * 59 + this.LastPriceDisplay.GetHashCode();
                if (this.LastTradingDate != null)
                    hashCode = hashCode * 59 + this.LastTradingDate.GetHashCode();
                if (this.Low != null)
                    hashCode = hashCode * 59 + this.Low.GetHashCode();
                if (this.Low52Week != null)
                    hashCode = hashCode * 59 + this.Low52Week.GetHashCode();
                if (this.Low52WeekPriceDisplay != null)
                    hashCode = hashCode * 59 + this.Low52WeekPriceDisplay.GetHashCode();
                if (this.Low52WeekTimeStamp != null)
                    hashCode = hashCode * 59 + this.Low52WeekTimeStamp.GetHashCode();
                if (this.LowPriceDisplay != null)
                    hashCode = hashCode * 59 + this.LowPriceDisplay.GetHashCode();
                if (this.MaxPrice != null)
                    hashCode = hashCode * 59 + this.MaxPrice.GetHashCode();
                if (this.MaxPriceDisplay != null)
                    hashCode = hashCode * 59 + this.MaxPriceDisplay.GetHashCode();
                if (this.MinMove != null)
                    hashCode = hashCode * 59 + this.MinMove.GetHashCode();
                if (this.MinPrice != null)
                    hashCode = hashCode * 59 + this.MinPrice.GetHashCode();
                if (this.MinPriceDisplay != null)
                    hashCode = hashCode * 59 + this.MinPriceDisplay.GetHashCode();
                if (this.NameExt != null)
                    hashCode = hashCode * 59 + this.NameExt.GetHashCode();
                if (this.NetChange != null)
                    hashCode = hashCode * 59 + this.NetChange.GetHashCode();
                if (this.NetChangePct != null)
                    hashCode = hashCode * 59 + this.NetChangePct.GetHashCode();
                if (this.Open != null)
                    hashCode = hashCode * 59 + this.Open.GetHashCode();
                if (this.OpenPriceDisplay != null)
                    hashCode = hashCode * 59 + this.OpenPriceDisplay.GetHashCode();
                if (this.PointValue != null)
                    hashCode = hashCode * 59 + this.PointValue.GetHashCode();
                if (this.PreviousClose != null)
                    hashCode = hashCode * 59 + this.PreviousClose.GetHashCode();
                if (this.PreviousClosePriceDisplay != null)
                    hashCode = hashCode * 59 + this.PreviousClosePriceDisplay.GetHashCode();
                if (this.PreviousVolume != null)
                    hashCode = hashCode * 59 + this.PreviousVolume.GetHashCode();
                if (this.Restrictions != null)
                    hashCode = hashCode * 59 + this.Restrictions.GetHashCode();
                if (this.StrikePrice != null)
                    hashCode = hashCode * 59 + this.StrikePrice.GetHashCode();
                if (this.StrikePriceDisplay != null)
                    hashCode = hashCode * 59 + this.StrikePriceDisplay.GetHashCode();
                if (this.Symbol != null)
                    hashCode = hashCode * 59 + this.Symbol.GetHashCode();
                if (this.SymbolRoot != null)
                    hashCode = hashCode * 59 + this.SymbolRoot.GetHashCode();
                if (this.TickSizeTier != null)
                    hashCode = hashCode * 59 + this.TickSizeTier.GetHashCode();
                if (this.TradeTime != null)
                    hashCode = hashCode * 59 + this.TradeTime.GetHashCode();
                if (this.Underlying != null)
                    hashCode = hashCode * 59 + this.Underlying.GetHashCode();
                if (this.Volume != null)
                    hashCode = hashCode * 59 + this.Volume.GetHashCode();
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

            // BidPriceDisplay (string) minLength
            if(this.BidPriceDisplay != null && this.BidPriceDisplay.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for BidPriceDisplay, length must be greater than 1.", new [] { "BidPriceDisplay" });
            }

            // ClosePriceDisplay (string) minLength
            if(this.ClosePriceDisplay != null && this.ClosePriceDisplay.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for ClosePriceDisplay, length must be greater than 1.", new [] { "ClosePriceDisplay" });
            }

            // CountryCode (string) minLength
            if(this.CountryCode != null && this.CountryCode.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for CountryCode, length must be greater than 1.", new [] { "CountryCode" });
            }

            // Currency (string) minLength
            //if(this.Currency != null && this.Currency.Length < 1)
            //{
            //    yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Currency, length must be greater than 1.", new [] { "Currency" });
            //}

            // DataFeed (string) minLength
            if(this.DataFeed != null && this.DataFeed.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for DataFeed, length must be greater than 1.", new [] { "DataFeed" });
            }

            // Description (string) minLength
            if(this.Description != null && this.Description.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Description, length must be greater than 1.", new [] { "Description" });
            }

            // Exchange (string) minLength
            if(this.Exchange != null && this.Exchange.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Exchange, length must be greater than 1.", new [] { "Exchange" });
            }

            // ExpirationDate (string) minLength
            if(this.ExpirationDate != null && this.ExpirationDate.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for ExpirationDate, length must be greater than 1.", new [] { "ExpirationDate" });
            }

            // High52WeekPriceDisplay (string) minLength
            if(this.High52WeekPriceDisplay != null && this.High52WeekPriceDisplay.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for High52WeekPriceDisplay, length must be greater than 1.", new [] { "High52WeekPriceDisplay" });
            }

            // High52WeekTimeStamp (string) minLength
            if(this.High52WeekTimeStamp != null && this.High52WeekTimeStamp.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for High52WeekTimeStamp, length must be greater than 1.", new [] { "High52WeekTimeStamp" });
            }

            // HighPriceDisplay (string) minLength
            if(this.HighPriceDisplay != null && this.HighPriceDisplay.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for HighPriceDisplay, length must be greater than 1.", new [] { "HighPriceDisplay" });
            }

            // LastPriceDisplay (string) minLength
            if(this.LastPriceDisplay != null && this.LastPriceDisplay.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for LastPriceDisplay, length must be greater than 1.", new [] { "LastPriceDisplay" });
            }

            // Low52WeekPriceDisplay (string) minLength
            if(this.Low52WeekPriceDisplay != null && this.Low52WeekPriceDisplay.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Low52WeekPriceDisplay, length must be greater than 1.", new [] { "Low52WeekPriceDisplay" });
            }

            // Low52WeekTimeStamp (string) minLength
            if(this.Low52WeekTimeStamp != null && this.Low52WeekTimeStamp.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Low52WeekTimeStamp, length must be greater than 1.", new [] { "Low52WeekTimeStamp" });
            }

            // LowPriceDisplay (string) minLength
            if(this.LowPriceDisplay != null && this.LowPriceDisplay.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for LowPriceDisplay, length must be greater than 1.", new [] { "LowPriceDisplay" });
            }

            // OpenPriceDisplay (string) minLength
            if(this.OpenPriceDisplay != null && this.OpenPriceDisplay.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for OpenPriceDisplay, length must be greater than 1.", new [] { "OpenPriceDisplay" });
            }

            // PreviousClosePriceDisplay (string) minLength
            if(this.PreviousClosePriceDisplay != null && this.PreviousClosePriceDisplay.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for PreviousClosePriceDisplay, length must be greater than 1.", new [] { "PreviousClosePriceDisplay" });
            }

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

            // SymbolRoot (string) minLength
            if(this.SymbolRoot != null && this.SymbolRoot.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for SymbolRoot, length must be greater than 1.", new [] { "SymbolRoot" });
            }

            // TradeTime (string) minLength
            if(this.TradeTime != null && this.TradeTime.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for TradeTime, length must be greater than 1.", new [] { "TradeTime" });
            }

            yield break;
        }
    }

}
