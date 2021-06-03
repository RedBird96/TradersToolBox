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
using System.ComponentModel;

namespace TradeStationAPI.Model
{
    /// <summary>
    /// SymbolSearchDefinitionInner
    /// </summary>
    [DataContract]
    public partial class SymbolSearchDefinitionInner :  IEquatable<SymbolSearchDefinitionInner>, IValidatableObject
    {
        /// <summary>
        /// The country of the exchange where the symbol is listed.
        /// </summary>
        /// <value>The country of the exchange where the symbol is listed.</value>
        [JsonConverter(typeof(MyEnumConverter))]
        public enum CountryEnum
        {
            All = 0,
            
            /// <summary>
            /// Enum US for value: US
            /// </summary>
            [EnumMember(Value = "United States")]
            US = 1,
            
            /// <summary>
            /// Enum DE for value: DE
            /// </summary>
            [EnumMember(Value = "Germany")]
            DE = 2,
            
            /// <summary>
            /// Enum CA for value: CA
            /// </summary>
            [EnumMember(Value = "Canada")]
            CA = 3
        }

        /// <summary>
        /// The country of the exchange where the symbol is listed.
        /// </summary>
        /// <value>The country of the exchange where the symbol is listed.</value>
        [DataMember(Name="Country", EmitDefaultValue=false)]
        public CountryEnum Country { get; set; }
        /// <summary>
        /// Displays the type of base currency for the selected symbol.
        /// </summary>
        /// <value>Displays the type of base currency for the selected symbol.</value>
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
            SGD = 13
        }

        /// <summary>
        /// Displays the type of base currency for the selected symbol.
        /// </summary>
        /// <value>Displays the type of base currency for the selected symbol.</value>
        [DataMember(Name="Currency", EmitDefaultValue=false)]
        public CurrencyEnum Currency { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="SymbolSearchDefinitionInner" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected SymbolSearchDefinitionInner() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="SymbolSearchDefinitionInner" /> class.
        /// </summary>
        /// <param name="category">The type of financial instrument that the symbol represents, such as a stock, index, or mutual fund. (required).</param>
        /// <param name="country">The country of the exchange where the symbol is listed. (required).</param>
        /// <param name="currency">Displays the type of base currency for the selected symbol. (required).</param>
        /// <param name="description">Displays the full name of the symbol. (required).</param>
        /// <param name="displayType">Symbol&#39;s price display type based on the following list:  * &#x60;0&#x60; \&quot;Automatic\&quot; Not used * &#x60;1&#x60; 0 Decimals &#x3D;&gt; 1 * &#x60;2&#x60; 1 Decimals &#x3D;&gt; .1 * &#x60;3&#x60; 2 Decimals &#x3D;&gt; .01 * &#x60;4&#x60; 3 Decimals &#x3D;&gt; .001 * &#x60;5&#x60; 4 Decimals &#x3D;&gt; .0001 * &#x60;6&#x60; 5 Decimals &#x3D;&gt; .00001 * &#x60;7&#x60; Simplest Fraction * &#x60;8&#x60; 1/2-Halves &#x3D;&gt; .5 * &#x60;9&#x60; 1/4-Fourths &#x3D;&gt; .25 * &#x60;10&#x60; 1/8-Eights &#x3D;&gt; .125 * &#x60;11&#x60; 1/16-Sixteenths &#x3D;&gt; .0625 * &#x60;12&#x60; 1/32-ThirtySeconds &#x3D;&gt; .03125 * &#x60;13&#x60; 1/64-SixtyFourths &#x3D;&gt; .015625 * &#x60;14&#x60; 1/128-OneTwentyEigths &#x3D;&gt; .0078125 * &#x60;15&#x60; 1/256-TwoFiftySixths &#x3D;&gt; .003906250 * &#x60;16&#x60; 10ths and Quarters &#x3D;&gt; .025 * &#x60;17&#x60; 32nds and Halves &#x3D;&gt; .015625 * &#x60;18&#x60; 32nds and Quarters &#x3D;&gt; .0078125 * &#x60;19&#x60; 32nds and Eights &#x3D;&gt; .00390625 * &#x60;20&#x60; 32nds and Tenths &#x3D;&gt; .003125 * &#x60;21&#x60; 64ths and Halves &#x3D;&gt; .0078125 * &#x60;22&#x60; 64ths and Tenths &#x3D;&gt; .0015625 * &#x60;23&#x60; 6 Decimals &#x3D;&gt; .000001  (required).</param>
        /// <param name="error">Element that references error..</param>
        /// <param name="exchange">Name of exchange where this symbol is traded in. (required).</param>
        /// <param name="exchangeID">A unique numerical identifier for the Exchange. (required).</param>
        /// <param name="expirationDate">Displays the expiration date for a futures or options contract in UTC formatted time. (required).</param>
        /// <param name="expirationType">For options only. It indicates whether the option is a monthly, weekly, quarterly or end of month expiration. * W - Weekly * M - Monthly * Q - Quartely * E - End of the month * \&quot;\&quot; - The term not be identified .</param>
        /// <param name="futureType">Displays the type of future contract the symbol represents. (required).</param>
        /// <param name="industryCode">(Japan Only) Displays a digit code that categorize companies by the type of business activities they engage in..</param>
        /// <param name="industryName">(Japan Only) Displays the Reuters assigned industry name to which the equity symbol belongs..</param>
        /// <param name="lotSize">(Japan Only) The currency amount associated with a lot (contract) in the specified account. (required).</param>
        /// <param name="minMove">Multiplying factor using the display type to determine the minimum price increment the asset trades in. For options the MinMove may vary. If the MinMove is negative, then the MinMove is dependent on the price. The whole number portion of the min move is the threshold. The leftmost two digits to the right of the decimal (X.XXXX) indicate the min move beneath the threshold, and the rightmost two digits (X.XXXX) indicate the min move above the threshold. (required).</param>
        /// <param name="name">A unique series of letters assigned to a security for trading purposes. (required).</param>
        /// <param name="optionType">Displays the type of options contract the symbol represents. Valid options include: Puts, Calls. (required).</param>
        /// <param name="pointValue">Symbol&#x60;s point value. (required).</param>
        /// <param name="root">Displays the symbol of the stock on the stock exchange. (required).</param>
        /// <param name="sectorName">(Japan Only) Displays the assigned economic sector to which the equity symbol belongs..</param>
        /// <param name="strikePrice">Displays strike price of an options contract; For Options symbols only. (required).</param>
        /// <param name="underlying">The financial instrument on which an option contract is based or derived. (required).</param>
        public SymbolSearchDefinitionInner(string category = default, CountryEnum country = default, CurrencyEnum currency = default, string description = default, decimal? displayType = default, string error = default, string exchange = default, decimal? exchangeID = default, string expirationDate = default, string expirationType = default, string futureType = default, string industryCode = default, string industryName = default, decimal? lotSize = default, decimal? minMove = default, string name = default, string optionType = default, decimal? pointValue = default, string root = default, string sectorName = default, decimal? strikePrice = default, string underlying = default)
        {
            // to ensure "category" is required (not null)
            if (category == null)
            {
                throw new InvalidDataException("category is a required property for SymbolSearchDefinitionInner and cannot be null");
            }
            else
            {
                this.Category = category;
            }
            // to ensure "description" is required (not null)
            if (description == null)
            {
                throw new InvalidDataException("description is a required property for SymbolSearchDefinitionInner and cannot be null");
            }
            else
            {
                this.Description = description;
            }
            // to ensure "displayType" is required (not null)
            if (displayType == null)
            {
                throw new InvalidDataException("displayType is a required property for SymbolSearchDefinitionInner and cannot be null");
            }
            else
            {
                this.DisplayType = displayType;
            }
            // to ensure "exchange" is required (not null)
            if (exchange == null)
            {
                throw new InvalidDataException("exchange is a required property for SymbolSearchDefinitionInner and cannot be null");
            }
            else
            {
                this.Exchange = exchange;
            }
            // to ensure "exchangeID" is required (not null)
            if (exchangeID == null)
            {
                throw new InvalidDataException("exchangeID is a required property for SymbolSearchDefinitionInner and cannot be null");
            }
            else
            {
                this.ExchangeID = exchangeID;
            }
            // to ensure "expirationDate" is required (not null)
            if (expirationDate == null)
            {
                throw new InvalidDataException("expirationDate is a required property for SymbolSearchDefinitionInner and cannot be null");
            }
            else
            {
                this.ExpirationDate = expirationDate;
            }
            // to ensure "futureType" is required (not null)
            if (futureType == null)
            {
                throw new InvalidDataException("futureType is a required property for SymbolSearchDefinitionInner and cannot be null");
            }
            else
            {
                this.FutureType = futureType;
            }
            // to ensure "lotSize" is required (not null)
            if (lotSize == null)
            {
                throw new InvalidDataException("lotSize is a required property for SymbolSearchDefinitionInner and cannot be null");
            }
            else
            {
                this.LotSize = lotSize;
            }
            // to ensure "minMove" is required (not null)
            if (minMove == null)
            {
                throw new InvalidDataException("minMove is a required property for SymbolSearchDefinitionInner and cannot be null");
            }
            else
            {
                this.MinMove = minMove;
            }
            // to ensure "name" is required (not null)
            if (name == null)
            {
                throw new InvalidDataException("name is a required property for SymbolSearchDefinitionInner and cannot be null");
            }
            else
            {
                this.Name = name;
            }
            // to ensure "optionType" is required (not null)
            if (optionType == null)
            {
                throw new InvalidDataException("optionType is a required property for SymbolSearchDefinitionInner and cannot be null");
            }
            else
            {
                this.OptionType = optionType;
            }
            // to ensure "pointValue" is required (not null)
            if (pointValue == null)
            {
                throw new InvalidDataException("pointValue is a required property for SymbolSearchDefinitionInner and cannot be null");
            }
            else
            {
                this.PointValue = pointValue;
            }
            // to ensure "root" is required (not null)
            if (root == null)
            {
                throw new InvalidDataException("root is a required property for SymbolSearchDefinitionInner and cannot be null");
            }
            else
            {
                this.Root = root;
            }
            // to ensure "strikePrice" is required (not null)
            if (strikePrice == null)
            {
                throw new InvalidDataException("strikePrice is a required property for SymbolSearchDefinitionInner and cannot be null");
            }
            else
            {
                this.StrikePrice = strikePrice;
            }
            // to ensure "underlying" is required (not null)
            if (underlying == null)
            {
                throw new InvalidDataException("underlying is a required property for SymbolSearchDefinitionInner and cannot be null");
            }
            else
            {
                this.Underlying = underlying;
            }
            Error = error;
            ExpirationType = expirationType;
            IndustryCode = industryCode;
            IndustryName = industryName;
            SectorName = sectorName;
        }
        
        /// <summary>
        /// The type of financial instrument that the symbol represents, such as a stock, index, or mutual fund.
        /// </summary>
        /// <value>The type of financial instrument that the symbol represents, such as a stock, index, or mutual fund.</value>
        [DataMember(Name="Category", EmitDefaultValue=false)]
        public string Category { get; set; }



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
        /// Element that references error.
        /// </summary>
        /// <value>Element that references error.</value>
        [DataMember(Name="Error", EmitDefaultValue=false)]
        public string Error { get; set; }

        /// <summary>
        /// Name of exchange where this symbol is traded in.
        /// </summary>
        /// <value>Name of exchange where this symbol is traded in.</value>
        [DataMember(Name="Exchange", EmitDefaultValue=false)]
        public string Exchange { get; set; }

        /// <summary>
        /// A unique numerical identifier for the Exchange.
        /// </summary>
        /// <value>A unique numerical identifier for the Exchange.</value>
        [DataMember(Name="ExchangeID", EmitDefaultValue=false)]
        public decimal? ExchangeID { get; set; }

        /// <summary>
        /// Displays the expiration date for a futures or options contract in UTC formatted time.
        /// </summary>
        /// <value>Displays the expiration date for a futures or options contract in UTC formatted time.</value>
        [DataMember(Name="ExpirationDate", EmitDefaultValue=false)]
        public string ExpirationDate { get; set; }

        /// <summary>
        /// For options only. It indicates whether the option is a monthly, weekly, quarterly or end of month expiration. * W - Weekly * M - Monthly * Q - Quartely * E - End of the month * \&quot;\&quot; - The term not be identified 
        /// </summary>
        /// <value>For options only. It indicates whether the option is a monthly, weekly, quarterly or end of month expiration. * W - Weekly * M - Monthly * Q - Quartely * E - End of the month * \&quot;\&quot; - The term not be identified </value>
        [DataMember(Name="ExpirationType", EmitDefaultValue=false)]
        public string ExpirationType { get; set; }

        /// <summary>
        /// Displays the type of future contract the symbol represents.
        /// </summary>
        /// <value>Displays the type of future contract the symbol represents.</value>
        [DataMember(Name="FutureType", EmitDefaultValue=false)]
        public string FutureType { get; set; }

        /// <summary>
        /// (Japan Only) Displays a digit code that categorize companies by the type of business activities they engage in.
        /// </summary>
        /// <value>(Japan Only) Displays a digit code that categorize companies by the type of business activities they engage in.</value>
        [DataMember(Name="IndustryCode", EmitDefaultValue=false)]
        public string IndustryCode { get; set; }

        /// <summary>
        /// (Japan Only) Displays the Reuters assigned industry name to which the equity symbol belongs.
        /// </summary>
        /// <value>(Japan Only) Displays the Reuters assigned industry name to which the equity symbol belongs.</value>
        [DataMember(Name="IndustryName", EmitDefaultValue=false)]
        public string IndustryName { get; set; }

        /// <summary>
        /// (Japan Only) The currency amount associated with a lot (contract) in the specified account.
        /// </summary>
        /// <value>(Japan Only) The currency amount associated with a lot (contract) in the specified account.</value>
        [DataMember(Name="LotSize", EmitDefaultValue=false)]
        public decimal? LotSize { get; set; }

        /// <summary>
        /// Multiplying factor using the display type to determine the minimum price increment the asset trades in. For options the MinMove may vary. If the MinMove is negative, then the MinMove is dependent on the price. The whole number portion of the min move is the threshold. The leftmost two digits to the right of the decimal (X.XXXX) indicate the min move beneath the threshold, and the rightmost two digits (X.XXXX) indicate the min move above the threshold.
        /// </summary>
        /// <value>Multiplying factor using the display type to determine the minimum price increment the asset trades in. For options the MinMove may vary. If the MinMove is negative, then the MinMove is dependent on the price. The whole number portion of the min move is the threshold. The leftmost two digits to the right of the decimal (X.XXXX) indicate the min move beneath the threshold, and the rightmost two digits (X.XXXX) indicate the min move above the threshold.</value>
        [DataMember(Name="MinMove", EmitDefaultValue=false)]
        public decimal? MinMove { get; set; }

        /// <summary>
        /// A unique series of letters assigned to a security for trading purposes.
        /// </summary>
        /// <value>A unique series of letters assigned to a security for trading purposes.</value>
        [DataMember(Name="Name", EmitDefaultValue=false)]
        public string Name { get; set; }

        /// <summary>
        /// Displays the type of options contract the symbol represents. Valid options include: Puts, Calls.
        /// </summary>
        /// <value>Displays the type of options contract the symbol represents. Valid options include: Puts, Calls.</value>
        [DataMember(Name="OptionType", EmitDefaultValue=false)]
        public string OptionType { get; set; }

        /// <summary>
        /// Symbol&#x60;s point value.
        /// </summary>
        /// <value>Symbol&#x60;s point value.</value>
        [DataMember(Name="PointValue", EmitDefaultValue=false)]
        public decimal? PointValue { get; set; }

        /// <summary>
        /// Displays the symbol of the stock on the stock exchange.
        /// </summary>
        /// <value>Displays the symbol of the stock on the stock exchange.</value>
        [DataMember(Name="Root", EmitDefaultValue=false)]
        public string Root { get; set; }

        /// <summary>
        /// (Japan Only) Displays the assigned economic sector to which the equity symbol belongs.
        /// </summary>
        /// <value>(Japan Only) Displays the assigned economic sector to which the equity symbol belongs.</value>
        [DataMember(Name="SectorName", EmitDefaultValue=false)]
        public string SectorName { get; set; }

        /// <summary>
        /// Displays strike price of an options contract; For Options symbols only.
        /// </summary>
        /// <value>Displays strike price of an options contract; For Options symbols only.</value>
        [DataMember(Name="StrikePrice", EmitDefaultValue=false)]
        public decimal? StrikePrice { get; set; }

        /// <summary>
        /// The financial instrument on which an option contract is based or derived.
        /// </summary>
        /// <value>The financial instrument on which an option contract is based or derived.</value>
        [DataMember(Name="Underlying", EmitDefaultValue=false)]
        public string Underlying { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class SymbolSearchDefinitionInner {\n");
            sb.Append("  Category: ").Append(Category).Append("\n");
            sb.Append("  Country: ").Append(Country).Append("\n");
            sb.Append("  Currency: ").Append(Currency).Append("\n");
            sb.Append("  Description: ").Append(Description).Append("\n");
            sb.Append("  DisplayType: ").Append(DisplayType).Append("\n");
            sb.Append("  Error: ").Append(Error).Append("\n");
            sb.Append("  Exchange: ").Append(Exchange).Append("\n");
            sb.Append("  ExchangeID: ").Append(ExchangeID).Append("\n");
            sb.Append("  ExpirationDate: ").Append(ExpirationDate).Append("\n");
            sb.Append("  ExpirationType: ").Append(ExpirationType).Append("\n");
            sb.Append("  FutureType: ").Append(FutureType).Append("\n");
            sb.Append("  IndustryCode: ").Append(IndustryCode).Append("\n");
            sb.Append("  IndustryName: ").Append(IndustryName).Append("\n");
            sb.Append("  LotSize: ").Append(LotSize).Append("\n");
            sb.Append("  MinMove: ").Append(MinMove).Append("\n");
            sb.Append("  Name: ").Append(Name).Append("\n");
            sb.Append("  OptionType: ").Append(OptionType).Append("\n");
            sb.Append("  PointValue: ").Append(PointValue).Append("\n");
            sb.Append("  Root: ").Append(Root).Append("\n");
            sb.Append("  SectorName: ").Append(SectorName).Append("\n");
            sb.Append("  StrikePrice: ").Append(StrikePrice).Append("\n");
            sb.Append("  Underlying: ").Append(Underlying).Append("\n");
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
            return this.Equals(input as SymbolSearchDefinitionInner);
        }

        /// <summary>
        /// Returns true if SymbolSearchDefinitionInner instances are equal
        /// </summary>
        /// <param name="input">Instance of SymbolSearchDefinitionInner to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(SymbolSearchDefinitionInner input)
        {
            if (input == null)
                return false;

            return 
                (
                    this.Category == input.Category ||
                    (this.Category != null &&
                    this.Category.Equals(input.Category))
                ) && 
                (
                    this.Country == input.Country ||
                    this.Country.Equals(input.Country)
                ) && 
                (
                    this.Currency == input.Currency ||
                    this.Currency.Equals(input.Currency)
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
                    this.ExchangeID == input.ExchangeID ||
                    (this.ExchangeID != null &&
                    this.ExchangeID.Equals(input.ExchangeID))
                ) && 
                (
                    this.ExpirationDate == input.ExpirationDate ||
                    (this.ExpirationDate != null &&
                    this.ExpirationDate.Equals(input.ExpirationDate))
                ) && 
                (
                    this.ExpirationType == input.ExpirationType ||
                    (this.ExpirationType != null &&
                    this.ExpirationType.Equals(input.ExpirationType))
                ) && 
                (
                    this.FutureType == input.FutureType ||
                    (this.FutureType != null &&
                    this.FutureType.Equals(input.FutureType))
                ) && 
                (
                    this.IndustryCode == input.IndustryCode ||
                    (this.IndustryCode != null &&
                    this.IndustryCode.Equals(input.IndustryCode))
                ) && 
                (
                    this.IndustryName == input.IndustryName ||
                    (this.IndustryName != null &&
                    this.IndustryName.Equals(input.IndustryName))
                ) && 
                (
                    this.LotSize == input.LotSize ||
                    (this.LotSize != null &&
                    this.LotSize.Equals(input.LotSize))
                ) && 
                (
                    this.MinMove == input.MinMove ||
                    (this.MinMove != null &&
                    this.MinMove.Equals(input.MinMove))
                ) && 
                (
                    this.Name == input.Name ||
                    (this.Name != null &&
                    this.Name.Equals(input.Name))
                ) && 
                (
                    this.OptionType == input.OptionType ||
                    (this.OptionType != null &&
                    this.OptionType.Equals(input.OptionType))
                ) && 
                (
                    this.PointValue == input.PointValue ||
                    (this.PointValue != null &&
                    this.PointValue.Equals(input.PointValue))
                ) && 
                (
                    this.Root == input.Root ||
                    (this.Root != null &&
                    this.Root.Equals(input.Root))
                ) && 
                (
                    this.SectorName == input.SectorName ||
                    (this.SectorName != null &&
                    this.SectorName.Equals(input.SectorName))
                ) && 
                (
                    this.StrikePrice == input.StrikePrice ||
                    (this.StrikePrice != null &&
                    this.StrikePrice.Equals(input.StrikePrice))
                ) && 
                (
                    this.Underlying == input.Underlying ||
                    (this.Underlying != null &&
                    this.Underlying.Equals(input.Underlying))
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
                if (this.Category != null)
                    hashCode = hashCode * 59 + this.Category.GetHashCode();
                //if (this.Country != null)
                    hashCode = hashCode * 59 + this.Country.GetHashCode();
                //if (this.Currency != null)
                    hashCode = hashCode * 59 + this.Currency.GetHashCode();
                if (this.Description != null)
                    hashCode = hashCode * 59 + this.Description.GetHashCode();
                if (this.DisplayType != null)
                    hashCode = hashCode * 59 + this.DisplayType.GetHashCode();
                if (this.Error != null)
                    hashCode = hashCode * 59 + this.Error.GetHashCode();
                if (this.Exchange != null)
                    hashCode = hashCode * 59 + this.Exchange.GetHashCode();
                if (this.ExchangeID != null)
                    hashCode = hashCode * 59 + this.ExchangeID.GetHashCode();
                if (this.ExpirationDate != null)
                    hashCode = hashCode * 59 + this.ExpirationDate.GetHashCode();
                if (this.ExpirationType != null)
                    hashCode = hashCode * 59 + this.ExpirationType.GetHashCode();
                if (this.FutureType != null)
                    hashCode = hashCode * 59 + this.FutureType.GetHashCode();
                if (this.IndustryCode != null)
                    hashCode = hashCode * 59 + this.IndustryCode.GetHashCode();
                if (this.IndustryName != null)
                    hashCode = hashCode * 59 + this.IndustryName.GetHashCode();
                if (this.LotSize != null)
                    hashCode = hashCode * 59 + this.LotSize.GetHashCode();
                if (this.MinMove != null)
                    hashCode = hashCode * 59 + this.MinMove.GetHashCode();
                if (this.Name != null)
                    hashCode = hashCode * 59 + this.Name.GetHashCode();
                if (this.OptionType != null)
                    hashCode = hashCode * 59 + this.OptionType.GetHashCode();
                if (this.PointValue != null)
                    hashCode = hashCode * 59 + this.PointValue.GetHashCode();
                if (this.Root != null)
                    hashCode = hashCode * 59 + this.Root.GetHashCode();
                if (this.SectorName != null)
                    hashCode = hashCode * 59 + this.SectorName.GetHashCode();
                if (this.StrikePrice != null)
                    hashCode = hashCode * 59 + this.StrikePrice.GetHashCode();
                if (this.Underlying != null)
                    hashCode = hashCode * 59 + this.Underlying.GetHashCode();
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
            // Category (string) minLength
            if(this.Category != null && this.Category.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Category, length must be greater than 1.", new [] { "Category" });
            }

            // Country (string) minLength
            //if(this.Country != null && this.Country.Length < 1)
            //{
            //    yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Country, length must be greater than 1.", new [] { "Country" });
            //}

            // Currency (string) minLength
            //if(this.Currency != null && this.Currency.Length < 1)
            //{
            //    yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Currency, length must be greater than 1.", new [] { "Currency" });
            //}

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

            // FutureType (string) minLength
            if(this.FutureType != null && this.FutureType.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for FutureType, length must be greater than 1.", new [] { "FutureType" });
            }

            // Name (string) minLength
            if(this.Name != null && this.Name.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Name, length must be greater than 1.", new [] { "Name" });
            }

            // OptionType (string) minLength
            if(this.OptionType != null && this.OptionType.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for OptionType, length must be greater than 1.", new [] { "OptionType" });
            }

            // Root (string) minLength
            if(this.Root != null && this.Root.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Root, length must be greater than 1.", new [] { "Root" });
            }

            yield break;
        }
    }

    public class MyEnumConverter : StringEnumConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            try
            {
                return base.ReadJson(reader, objectType, existingValue, serializer);
            }
            catch (JsonSerializationException)
            {
                return Enum.ToObject(objectType, 0);
            }
        }
    }
}
