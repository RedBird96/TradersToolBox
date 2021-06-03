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
    /// AccountOrdersDefinitionInnerLegs
    /// </summary>
    [DataContract]
    public partial class AccountOrdersDefinitionInnerLegs :  IEquatable<AccountOrdersDefinitionInnerLegs>, IValidatableObject
    {
        /// <summary>
        /// Identifies the order type of the order. 
        /// </summary>
        /// <value>Identifies the order type of the order. </value>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum OrderTypeEnum
        {
            
            /// <summary>
            /// Enum Market for value: Market
            /// </summary>
            [EnumMember(Value = "Market")]
            Market = 1,
            
            /// <summary>
            /// Enum Limit for value: Limit
            /// </summary>
            [EnumMember(Value = "Limit")]
            Limit = 2,
            
            /// <summary>
            /// Enum StopLimit for value: Stop Limit
            /// </summary>
            [EnumMember(Value = "Stop Limit")]
            StopLimit = 3,
            
            /// <summary>
            /// Enum StopMarket for value: Stop Market
            /// </summary>
            [EnumMember(Value = "Stop Market")]
            StopMarket = 4
        }

        /// <summary>
        /// Identifies the order type of the order. 
        /// </summary>
        /// <value>Identifies the order type of the order. </value>
        [DataMember(Name="OrderType", EmitDefaultValue=false)]
        public OrderTypeEnum OrderType { get; set; }
        /// <summary>
        /// Type of order
        /// </summary>
        /// <value>Type of order</value>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum TypeEnum
        {
            
            /// <summary>
            /// Enum Sell for value: Sell
            /// </summary>
            [EnumMember(Value = "Sell")]
            Sell = 1,
            
            /// <summary>
            /// Enum Buy for value: Buy
            /// </summary>
            [EnumMember(Value = "Buy")]
            Buy = 2
        }

        /// <summary>
        /// Type of order
        /// </summary>
        /// <value>Type of order</value>
        [DataMember(Name="Type", EmitDefaultValue=false)]
        public TypeEnum Type { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="AccountOrdersDefinitionInnerLegs" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected AccountOrdersDefinitionInnerLegs() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="AccountOrdersDefinitionInnerLegs" /> class.
        /// </summary>
        /// <param name="ask">The price at which a security, futures, or other financial instrument is offered for sale. (required).</param>
        /// <param name="baseSymbol">Symbol of the underlying stock for an option trade. (required).</param>
        /// <param name="bid">The highest price a prospective buyer is prepared to pay at a particular time for a trading unit of a given symbol. (required).</param>
        /// <param name="execPrice">The execution price for the order. (required).</param>
        /// <param name="execQuantity">Number of shares executed. (required).</param>
        /// <param name="expireDate">The expiration date of the future or option symbol. (required).</param>
        /// <param name="leaves">It will display the number of shares that have not yet filled if the entire quantity has not been filled. (required).</param>
        /// <param name="legNumber">For an equity purchase, the value will be 1.  For option spreads, there will be a leg for each option. (required).</param>
        /// <param name="limitPrice">The limit price for Limit orders (required).</param>
        /// <param name="limitPriceDisplay">String value for LimitPrice attribute. (required).</param>
        /// <param name="month">Expiration month for options or futures. (required).</param>
        /// <param name="openOrClose">Identifies whether the order is an Opening or Closing trade. (required).</param>
        /// <param name="orderID">ID of the current order. (required).</param>
        /// <param name="orderType">Identifies the order type of the order.  (required).</param>
        /// <param name="pointValue">Number of shares the order represents. Equities will normally display 1. Options will display 100. (required).</param>
        /// <param name="priceUsedForBuyingPower">Price used for the buying power calculation of the order. (required).</param>
        /// <param name="putOrCall">For an option order, identifies whether the order is for a Put or Call. (required).</param>
        /// <param name="quantity">Number of shares or contracts being purchased. (required).</param>
        /// <param name="side">Identifies whether the order is a buy or sell. (required).</param>
        /// <param name="stopPrice">The stop price for stop orders. (required).</param>
        /// <param name="stopPriceDisplay">String value for StopPrice attribute. (required).</param>
        /// <param name="strikePrice">For an option order, the strike price for the Put or Call. (required).</param>
        /// <param name="symbol">Symbol to trade. (required).</param>
        /// <param name="timeExecuted">The time the order was filled or canceled. (required).</param>
        /// <param name="type">Type of order (required).</param>
        /// <param name="year">Represents the expiration year if the order is an option. (required).</param>
        public AccountOrdersDefinitionInnerLegs(decimal? ask = default(decimal?), string baseSymbol = default(string), decimal? bid = default(decimal?), decimal? execPrice = default(decimal?), decimal? execQuantity = default(decimal?), string expireDate = default(string), decimal? leaves = default(decimal?), decimal? legNumber = default(decimal?), decimal? limitPrice = default(decimal?), string limitPriceDisplay = default(string), decimal? month = default(decimal?), string openOrClose = default(string), decimal? orderID = default(decimal?), OrderTypeEnum orderType = default(OrderTypeEnum), decimal? pointValue = default(decimal?), decimal? priceUsedForBuyingPower = default(decimal?), string putOrCall = default(string), decimal? quantity = default(decimal?), string side = default(string), decimal? stopPrice = default(decimal?), string stopPriceDisplay = default(string), decimal? strikePrice = default(decimal?), string symbol = default(string), string timeExecuted = default(string), TypeEnum type = default(TypeEnum), decimal? year = default(decimal?))
        {
            // to ensure "ask" is required (not null)
            if (ask == null)
            {
                throw new InvalidDataException("ask is a required property for AccountOrdersDefinitionInnerLegs and cannot be null");
            }
            else
            {
                this.Ask = ask;
            }
            // to ensure "baseSymbol" is required (not null)
            if (baseSymbol == null)
            {
                throw new InvalidDataException("baseSymbol is a required property for AccountOrdersDefinitionInnerLegs and cannot be null");
            }
            else
            {
                this.BaseSymbol = baseSymbol;
            }
            // to ensure "bid" is required (not null)
            if (bid == null)
            {
                throw new InvalidDataException("bid is a required property for AccountOrdersDefinitionInnerLegs and cannot be null");
            }
            else
            {
                this.Bid = bid;
            }
            // to ensure "execPrice" is required (not null)
            if (execPrice == null)
            {
                throw new InvalidDataException("execPrice is a required property for AccountOrdersDefinitionInnerLegs and cannot be null");
            }
            else
            {
                this.ExecPrice = execPrice;
            }
            // to ensure "execQuantity" is required (not null)
            if (execQuantity == null)
            {
                throw new InvalidDataException("execQuantity is a required property for AccountOrdersDefinitionInnerLegs and cannot be null");
            }
            else
            {
                this.ExecQuantity = execQuantity;
            }
            // to ensure "expireDate" is required (not null)
            if (expireDate == null)
            {
                throw new InvalidDataException("expireDate is a required property for AccountOrdersDefinitionInnerLegs and cannot be null");
            }
            else
            {
                this.ExpireDate = expireDate;
            }
            // to ensure "leaves" is required (not null)
            if (leaves == null)
            {
                throw new InvalidDataException("leaves is a required property for AccountOrdersDefinitionInnerLegs and cannot be null");
            }
            else
            {
                this.Leaves = leaves;
            }
            // to ensure "legNumber" is required (not null)
            if (legNumber == null)
            {
                throw new InvalidDataException("legNumber is a required property for AccountOrdersDefinitionInnerLegs and cannot be null");
            }
            else
            {
                this.LegNumber = legNumber;
            }
            // to ensure "limitPrice" is required (not null)
            if (limitPrice == null)
            {
                throw new InvalidDataException("limitPrice is a required property for AccountOrdersDefinitionInnerLegs and cannot be null");
            }
            else
            {
                this.LimitPrice = limitPrice;
            }
            // to ensure "limitPriceDisplay" is required (not null)
            if (limitPriceDisplay == null)
            {
                throw new InvalidDataException("limitPriceDisplay is a required property for AccountOrdersDefinitionInnerLegs and cannot be null");
            }
            else
            {
                this.LimitPriceDisplay = limitPriceDisplay;
            }
            // to ensure "month" is required (not null)
            if (month == null)
            {
                throw new InvalidDataException("month is a required property for AccountOrdersDefinitionInnerLegs and cannot be null");
            }
            else
            {
                this.Month = month;
            }
            // to ensure "openOrClose" is required (not null)
            if (openOrClose == null)
            {
                throw new InvalidDataException("openOrClose is a required property for AccountOrdersDefinitionInnerLegs and cannot be null");
            }
            else
            {
                this.OpenOrClose = openOrClose;
            }
            // to ensure "orderID" is required (not null)
            if (orderID == null)
            {
                throw new InvalidDataException("orderID is a required property for AccountOrdersDefinitionInnerLegs and cannot be null");
            }
            else
            {
                this.OrderID = orderID;
            }
            // to ensure "orderType" is required (not null)
            if (orderType == null)
            {
                throw new InvalidDataException("orderType is a required property for AccountOrdersDefinitionInnerLegs and cannot be null");
            }
            else
            {
                this.OrderType = orderType;
            }
            // to ensure "pointValue" is required (not null)
            if (pointValue == null)
            {
                throw new InvalidDataException("pointValue is a required property for AccountOrdersDefinitionInnerLegs and cannot be null");
            }
            else
            {
                this.PointValue = pointValue;
            }
            // to ensure "priceUsedForBuyingPower" is required (not null)
            if (priceUsedForBuyingPower == null)
            {
                throw new InvalidDataException("priceUsedForBuyingPower is a required property for AccountOrdersDefinitionInnerLegs and cannot be null");
            }
            else
            {
                this.PriceUsedForBuyingPower = priceUsedForBuyingPower;
            }
            // to ensure "putOrCall" is required (not null)
            if (putOrCall == null)
            {
                throw new InvalidDataException("putOrCall is a required property for AccountOrdersDefinitionInnerLegs and cannot be null");
            }
            else
            {
                this.PutOrCall = putOrCall;
            }
            // to ensure "quantity" is required (not null)
            if (quantity == null)
            {
                throw new InvalidDataException("quantity is a required property for AccountOrdersDefinitionInnerLegs and cannot be null");
            }
            else
            {
                this.Quantity = quantity;
            }
            // to ensure "side" is required (not null)
            if (side == null)
            {
                throw new InvalidDataException("side is a required property for AccountOrdersDefinitionInnerLegs and cannot be null");
            }
            else
            {
                this.Side = side;
            }
            // to ensure "stopPrice" is required (not null)
            if (stopPrice == null)
            {
                throw new InvalidDataException("stopPrice is a required property for AccountOrdersDefinitionInnerLegs and cannot be null");
            }
            else
            {
                this.StopPrice = stopPrice;
            }
            // to ensure "stopPriceDisplay" is required (not null)
            if (stopPriceDisplay == null)
            {
                throw new InvalidDataException("stopPriceDisplay is a required property for AccountOrdersDefinitionInnerLegs and cannot be null");
            }
            else
            {
                this.StopPriceDisplay = stopPriceDisplay;
            }
            // to ensure "strikePrice" is required (not null)
            if (strikePrice == null)
            {
                throw new InvalidDataException("strikePrice is a required property for AccountOrdersDefinitionInnerLegs and cannot be null");
            }
            else
            {
                this.StrikePrice = strikePrice;
            }
            // to ensure "symbol" is required (not null)
            if (symbol == null)
            {
                throw new InvalidDataException("symbol is a required property for AccountOrdersDefinitionInnerLegs and cannot be null");
            }
            else
            {
                this.Symbol = symbol;
            }
            // to ensure "timeExecuted" is required (not null)
            if (timeExecuted == null)
            {
                throw new InvalidDataException("timeExecuted is a required property for AccountOrdersDefinitionInnerLegs and cannot be null");
            }
            else
            {
                this.TimeExecuted = timeExecuted;
            }
            // to ensure "type" is required (not null)
            if (type == null)
            {
                throw new InvalidDataException("type is a required property for AccountOrdersDefinitionInnerLegs and cannot be null");
            }
            else
            {
                this.Type = type;
            }
            // to ensure "year" is required (not null)
            if (year == null)
            {
                throw new InvalidDataException("year is a required property for AccountOrdersDefinitionInnerLegs and cannot be null");
            }
            else
            {
                this.Year = year;
            }
        }
        
        /// <summary>
        /// The price at which a security, futures, or other financial instrument is offered for sale.
        /// </summary>
        /// <value>The price at which a security, futures, or other financial instrument is offered for sale.</value>
        [DataMember(Name="Ask", EmitDefaultValue=false)]
        public decimal? Ask { get; set; }

        /// <summary>
        /// Symbol of the underlying stock for an option trade.
        /// </summary>
        /// <value>Symbol of the underlying stock for an option trade.</value>
        [DataMember(Name="BaseSymbol", EmitDefaultValue=false)]
        public string BaseSymbol { get; set; }

        /// <summary>
        /// The highest price a prospective buyer is prepared to pay at a particular time for a trading unit of a given symbol.
        /// </summary>
        /// <value>The highest price a prospective buyer is prepared to pay at a particular time for a trading unit of a given symbol.</value>
        [DataMember(Name="Bid", EmitDefaultValue=false)]
        public decimal? Bid { get; set; }

        /// <summary>
        /// The execution price for the order.
        /// </summary>
        /// <value>The execution price for the order.</value>
        [DataMember(Name="ExecPrice", EmitDefaultValue=false)]
        public decimal? ExecPrice { get; set; }

        /// <summary>
        /// Number of shares executed.
        /// </summary>
        /// <value>Number of shares executed.</value>
        [DataMember(Name="ExecQuantity", EmitDefaultValue=false)]
        public decimal? ExecQuantity { get; set; }

        /// <summary>
        /// The expiration date of the future or option symbol.
        /// </summary>
        /// <value>The expiration date of the future or option symbol.</value>
        [DataMember(Name="ExpireDate", EmitDefaultValue=false)]
        public string ExpireDate { get; set; }

        /// <summary>
        /// It will display the number of shares that have not yet filled if the entire quantity has not been filled.
        /// </summary>
        /// <value>It will display the number of shares that have not yet filled if the entire quantity has not been filled.</value>
        [DataMember(Name="Leaves", EmitDefaultValue=false)]
        public decimal? Leaves { get; set; }

        /// <summary>
        /// For an equity purchase, the value will be 1.  For option spreads, there will be a leg for each option.
        /// </summary>
        /// <value>For an equity purchase, the value will be 1.  For option spreads, there will be a leg for each option.</value>
        [DataMember(Name="LegNumber", EmitDefaultValue=false)]
        public decimal? LegNumber { get; set; }

        /// <summary>
        /// The limit price for Limit orders
        /// </summary>
        /// <value>The limit price for Limit orders</value>
        [DataMember(Name="LimitPrice", EmitDefaultValue=false)]
        public decimal? LimitPrice { get; set; }

        /// <summary>
        /// String value for LimitPrice attribute.
        /// </summary>
        /// <value>String value for LimitPrice attribute.</value>
        [DataMember(Name="LimitPriceDisplay", EmitDefaultValue=false)]
        public string LimitPriceDisplay { get; set; }

        /// <summary>
        /// Expiration month for options or futures.
        /// </summary>
        /// <value>Expiration month for options or futures.</value>
        [DataMember(Name="Month", EmitDefaultValue=false)]
        public decimal? Month { get; set; }

        /// <summary>
        /// Identifies whether the order is an Opening or Closing trade.
        /// </summary>
        /// <value>Identifies whether the order is an Opening or Closing trade.</value>
        [DataMember(Name="OpenOrClose", EmitDefaultValue=false)]
        public string OpenOrClose { get; set; }

        /// <summary>
        /// ID of the current order.
        /// </summary>
        /// <value>ID of the current order.</value>
        [DataMember(Name="OrderID", EmitDefaultValue=false)]
        public decimal? OrderID { get; set; }


        /// <summary>
        /// Number of shares the order represents. Equities will normally display 1. Options will display 100.
        /// </summary>
        /// <value>Number of shares the order represents. Equities will normally display 1. Options will display 100.</value>
        [DataMember(Name="PointValue", EmitDefaultValue=false)]
        public decimal? PointValue { get; set; }

        /// <summary>
        /// Price used for the buying power calculation of the order.
        /// </summary>
        /// <value>Price used for the buying power calculation of the order.</value>
        [DataMember(Name="PriceUsedForBuyingPower", EmitDefaultValue=false)]
        public decimal? PriceUsedForBuyingPower { get; set; }

        /// <summary>
        /// For an option order, identifies whether the order is for a Put or Call.
        /// </summary>
        /// <value>For an option order, identifies whether the order is for a Put or Call.</value>
        [DataMember(Name="PutOrCall", EmitDefaultValue=false)]
        public string PutOrCall { get; set; }

        /// <summary>
        /// Number of shares or contracts being purchased.
        /// </summary>
        /// <value>Number of shares or contracts being purchased.</value>
        [DataMember(Name="Quantity", EmitDefaultValue=false)]
        public decimal? Quantity { get; set; }

        /// <summary>
        /// Identifies whether the order is a buy or sell.
        /// </summary>
        /// <value>Identifies whether the order is a buy or sell.</value>
        [DataMember(Name="Side", EmitDefaultValue=false)]
        public string Side { get; set; }

        /// <summary>
        /// The stop price for stop orders.
        /// </summary>
        /// <value>The stop price for stop orders.</value>
        [DataMember(Name="StopPrice", EmitDefaultValue=false)]
        public decimal? StopPrice { get; set; }

        /// <summary>
        /// String value for StopPrice attribute.
        /// </summary>
        /// <value>String value for StopPrice attribute.</value>
        [DataMember(Name="StopPriceDisplay", EmitDefaultValue=false)]
        public string StopPriceDisplay { get; set; }

        /// <summary>
        /// For an option order, the strike price for the Put or Call.
        /// </summary>
        /// <value>For an option order, the strike price for the Put or Call.</value>
        [DataMember(Name="StrikePrice", EmitDefaultValue=false)]
        public decimal? StrikePrice { get; set; }

        /// <summary>
        /// Symbol to trade.
        /// </summary>
        /// <value>Symbol to trade.</value>
        [DataMember(Name="Symbol", EmitDefaultValue=false)]
        public string Symbol { get; set; }

        /// <summary>
        /// The time the order was filled or canceled.
        /// </summary>
        /// <value>The time the order was filled or canceled.</value>
        [DataMember(Name="TimeExecuted", EmitDefaultValue=false)]
        public string TimeExecuted { get; set; }


        /// <summary>
        /// Represents the expiration year if the order is an option.
        /// </summary>
        /// <value>Represents the expiration year if the order is an option.</value>
        [DataMember(Name="Year", EmitDefaultValue=false)]
        public decimal? Year { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class AccountOrdersDefinitionInnerLegs {\n");
            sb.Append("  Ask: ").Append(Ask).Append("\n");
            sb.Append("  BaseSymbol: ").Append(BaseSymbol).Append("\n");
            sb.Append("  Bid: ").Append(Bid).Append("\n");
            sb.Append("  ExecPrice: ").Append(ExecPrice).Append("\n");
            sb.Append("  ExecQuantity: ").Append(ExecQuantity).Append("\n");
            sb.Append("  ExpireDate: ").Append(ExpireDate).Append("\n");
            sb.Append("  Leaves: ").Append(Leaves).Append("\n");
            sb.Append("  LegNumber: ").Append(LegNumber).Append("\n");
            sb.Append("  LimitPrice: ").Append(LimitPrice).Append("\n");
            sb.Append("  LimitPriceDisplay: ").Append(LimitPriceDisplay).Append("\n");
            sb.Append("  Month: ").Append(Month).Append("\n");
            sb.Append("  OpenOrClose: ").Append(OpenOrClose).Append("\n");
            sb.Append("  OrderID: ").Append(OrderID).Append("\n");
            sb.Append("  OrderType: ").Append(OrderType).Append("\n");
            sb.Append("  PointValue: ").Append(PointValue).Append("\n");
            sb.Append("  PriceUsedForBuyingPower: ").Append(PriceUsedForBuyingPower).Append("\n");
            sb.Append("  PutOrCall: ").Append(PutOrCall).Append("\n");
            sb.Append("  Quantity: ").Append(Quantity).Append("\n");
            sb.Append("  Side: ").Append(Side).Append("\n");
            sb.Append("  StopPrice: ").Append(StopPrice).Append("\n");
            sb.Append("  StopPriceDisplay: ").Append(StopPriceDisplay).Append("\n");
            sb.Append("  StrikePrice: ").Append(StrikePrice).Append("\n");
            sb.Append("  Symbol: ").Append(Symbol).Append("\n");
            sb.Append("  TimeExecuted: ").Append(TimeExecuted).Append("\n");
            sb.Append("  Type: ").Append(Type).Append("\n");
            sb.Append("  Year: ").Append(Year).Append("\n");
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
            return this.Equals(input as AccountOrdersDefinitionInnerLegs);
        }

        /// <summary>
        /// Returns true if AccountOrdersDefinitionInnerLegs instances are equal
        /// </summary>
        /// <param name="input">Instance of AccountOrdersDefinitionInnerLegs to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(AccountOrdersDefinitionInnerLegs input)
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
                    this.BaseSymbol == input.BaseSymbol ||
                    (this.BaseSymbol != null &&
                    this.BaseSymbol.Equals(input.BaseSymbol))
                ) && 
                (
                    this.Bid == input.Bid ||
                    (this.Bid != null &&
                    this.Bid.Equals(input.Bid))
                ) && 
                (
                    this.ExecPrice == input.ExecPrice ||
                    (this.ExecPrice != null &&
                    this.ExecPrice.Equals(input.ExecPrice))
                ) && 
                (
                    this.ExecQuantity == input.ExecQuantity ||
                    (this.ExecQuantity != null &&
                    this.ExecQuantity.Equals(input.ExecQuantity))
                ) && 
                (
                    this.ExpireDate == input.ExpireDate ||
                    (this.ExpireDate != null &&
                    this.ExpireDate.Equals(input.ExpireDate))
                ) && 
                (
                    this.Leaves == input.Leaves ||
                    (this.Leaves != null &&
                    this.Leaves.Equals(input.Leaves))
                ) && 
                (
                    this.LegNumber == input.LegNumber ||
                    (this.LegNumber != null &&
                    this.LegNumber.Equals(input.LegNumber))
                ) && 
                (
                    this.LimitPrice == input.LimitPrice ||
                    (this.LimitPrice != null &&
                    this.LimitPrice.Equals(input.LimitPrice))
                ) && 
                (
                    this.LimitPriceDisplay == input.LimitPriceDisplay ||
                    (this.LimitPriceDisplay != null &&
                    this.LimitPriceDisplay.Equals(input.LimitPriceDisplay))
                ) && 
                (
                    this.Month == input.Month ||
                    (this.Month != null &&
                    this.Month.Equals(input.Month))
                ) && 
                (
                    this.OpenOrClose == input.OpenOrClose ||
                    (this.OpenOrClose != null &&
                    this.OpenOrClose.Equals(input.OpenOrClose))
                ) && 
                (
                    this.OrderID == input.OrderID ||
                    (this.OrderID != null &&
                    this.OrderID.Equals(input.OrderID))
                ) && 
                (
                    this.OrderType == input.OrderType ||
                    (this.OrderType != null &&
                    this.OrderType.Equals(input.OrderType))
                ) && 
                (
                    this.PointValue == input.PointValue ||
                    (this.PointValue != null &&
                    this.PointValue.Equals(input.PointValue))
                ) && 
                (
                    this.PriceUsedForBuyingPower == input.PriceUsedForBuyingPower ||
                    (this.PriceUsedForBuyingPower != null &&
                    this.PriceUsedForBuyingPower.Equals(input.PriceUsedForBuyingPower))
                ) && 
                (
                    this.PutOrCall == input.PutOrCall ||
                    (this.PutOrCall != null &&
                    this.PutOrCall.Equals(input.PutOrCall))
                ) && 
                (
                    this.Quantity == input.Quantity ||
                    (this.Quantity != null &&
                    this.Quantity.Equals(input.Quantity))
                ) && 
                (
                    this.Side == input.Side ||
                    (this.Side != null &&
                    this.Side.Equals(input.Side))
                ) && 
                (
                    this.StopPrice == input.StopPrice ||
                    (this.StopPrice != null &&
                    this.StopPrice.Equals(input.StopPrice))
                ) && 
                (
                    this.StopPriceDisplay == input.StopPriceDisplay ||
                    (this.StopPriceDisplay != null &&
                    this.StopPriceDisplay.Equals(input.StopPriceDisplay))
                ) && 
                (
                    this.StrikePrice == input.StrikePrice ||
                    (this.StrikePrice != null &&
                    this.StrikePrice.Equals(input.StrikePrice))
                ) && 
                (
                    this.Symbol == input.Symbol ||
                    (this.Symbol != null &&
                    this.Symbol.Equals(input.Symbol))
                ) && 
                (
                    this.TimeExecuted == input.TimeExecuted ||
                    (this.TimeExecuted != null &&
                    this.TimeExecuted.Equals(input.TimeExecuted))
                ) && 
                (
                    this.Type == input.Type ||
                    (this.Type != null &&
                    this.Type.Equals(input.Type))
                ) && 
                (
                    this.Year == input.Year ||
                    (this.Year != null &&
                    this.Year.Equals(input.Year))
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
                if (this.BaseSymbol != null)
                    hashCode = hashCode * 59 + this.BaseSymbol.GetHashCode();
                if (this.Bid != null)
                    hashCode = hashCode * 59 + this.Bid.GetHashCode();
                if (this.ExecPrice != null)
                    hashCode = hashCode * 59 + this.ExecPrice.GetHashCode();
                if (this.ExecQuantity != null)
                    hashCode = hashCode * 59 + this.ExecQuantity.GetHashCode();
                if (this.ExpireDate != null)
                    hashCode = hashCode * 59 + this.ExpireDate.GetHashCode();
                if (this.Leaves != null)
                    hashCode = hashCode * 59 + this.Leaves.GetHashCode();
                if (this.LegNumber != null)
                    hashCode = hashCode * 59 + this.LegNumber.GetHashCode();
                if (this.LimitPrice != null)
                    hashCode = hashCode * 59 + this.LimitPrice.GetHashCode();
                if (this.LimitPriceDisplay != null)
                    hashCode = hashCode * 59 + this.LimitPriceDisplay.GetHashCode();
                if (this.Month != null)
                    hashCode = hashCode * 59 + this.Month.GetHashCode();
                if (this.OpenOrClose != null)
                    hashCode = hashCode * 59 + this.OpenOrClose.GetHashCode();
                if (this.OrderID != null)
                    hashCode = hashCode * 59 + this.OrderID.GetHashCode();
                if (this.OrderType != null)
                    hashCode = hashCode * 59 + this.OrderType.GetHashCode();
                if (this.PointValue != null)
                    hashCode = hashCode * 59 + this.PointValue.GetHashCode();
                if (this.PriceUsedForBuyingPower != null)
                    hashCode = hashCode * 59 + this.PriceUsedForBuyingPower.GetHashCode();
                if (this.PutOrCall != null)
                    hashCode = hashCode * 59 + this.PutOrCall.GetHashCode();
                if (this.Quantity != null)
                    hashCode = hashCode * 59 + this.Quantity.GetHashCode();
                if (this.Side != null)
                    hashCode = hashCode * 59 + this.Side.GetHashCode();
                if (this.StopPrice != null)
                    hashCode = hashCode * 59 + this.StopPrice.GetHashCode();
                if (this.StopPriceDisplay != null)
                    hashCode = hashCode * 59 + this.StopPriceDisplay.GetHashCode();
                if (this.StrikePrice != null)
                    hashCode = hashCode * 59 + this.StrikePrice.GetHashCode();
                if (this.Symbol != null)
                    hashCode = hashCode * 59 + this.Symbol.GetHashCode();
                if (this.TimeExecuted != null)
                    hashCode = hashCode * 59 + this.TimeExecuted.GetHashCode();
                if (this.Type != null)
                    hashCode = hashCode * 59 + this.Type.GetHashCode();
                if (this.Year != null)
                    hashCode = hashCode * 59 + this.Year.GetHashCode();
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
            // LimitPriceDisplay (string) minLength
            if(this.LimitPriceDisplay != null && this.LimitPriceDisplay.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for LimitPriceDisplay, length must be greater than 1.", new [] { "LimitPriceDisplay" });
            }

            // OpenOrClose (string) minLength
            if(this.OpenOrClose != null && this.OpenOrClose.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for OpenOrClose, length must be greater than 1.", new [] { "OpenOrClose" });
            }

            // OrderType (string) minLength
            //if(this.OrderType != null && this.OrderType.Length < 1)
            //{
            //    yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for OrderType, length must be greater than 1.", new [] { "OrderType" });
            //}

            // Side (string) minLength
            if(this.Side != null && this.Side.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Side, length must be greater than 1.", new [] { "Side" });
            }

            // Symbol (string) minLength
            if(this.Symbol != null && this.Symbol.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Symbol, length must be greater than 1.", new [] { "Symbol" });
            }

            // TimeExecuted (string) minLength
            if(this.TimeExecuted != null && this.TimeExecuted.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for TimeExecuted, length must be greater than 1.", new [] { "TimeExecuted" });
            }

            // Type (string) minLength
            //if(this.Type != null && this.Type.Length < 1)
            //{
            //    yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Type, length must be greater than 1.", new [] { "Type" });
            //}

            yield break;
        }
    }

}
