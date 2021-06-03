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
    /// AccountOrdersDefinitionInner
    /// </summary>
    [DataContract]
    public partial class AccountOrdersDefinitionInner :  IEquatable<AccountOrdersDefinitionInner>, IValidatableObject
    {
        /// <summary>
        /// Will display a value when the order has advanced order rules associated with it or is part of a bracket order.
        /// </summary>
        /// <value>Will display a value when the order has advanced order rules associated with it or is part of a bracket order.</value>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum AdvancedOptionsEnum
        {
            
            /// <summary>
            /// Enum ActivationRule for value: Activation Rule
            /// </summary>
            [EnumMember(Value = "Activation Rule")]
            ActivationRule = 1,
            
            /// <summary>
            /// Enum AllorNone for value: All or None
            /// </summary>
            [EnumMember(Value = "All or None")]
            AllorNone = 2,
            
            /// <summary>
            /// Enum TrailingStop for value: Trailing Stop
            /// </summary>
            [EnumMember(Value = "Trailing Stop")]
            TrailingStop = 3,
            
            /// <summary>
            /// Enum IfTouched for value: If Touched
            /// </summary>
            [EnumMember(Value = "If Touched")]
            IfTouched = 4,
            
            /// <summary>
            /// Enum ShowOnly for value: Show Only
            /// </summary>
            [EnumMember(Value = "Show Only")]
            ShowOnly = 5,
            
            /// <summary>
            /// Enum Discretionary for value: Discretionary
            /// </summary>
            [EnumMember(Value = "Discretionary")]
            Discretionary = 6,
            
            /// <summary>
            /// Enum NonDisplay for value: Non-Display
            /// </summary>
            [EnumMember(Value = "Non-Display")]
            NonDisplay = 7,
            
            /// <summary>
            /// Enum Peg for value: Peg
            /// </summary>
            [EnumMember(Value = "Peg")]
            Peg = 8,
            
            /// <summary>
            /// Enum BookOnly for value: Book Only
            /// </summary>
            [EnumMember(Value = "Book Only")]
            BookOnly = 9,
            
            /// <summary>
            /// Enum AddLiquidity for value: Add Liquidity
            /// </summary>
            [EnumMember(Value = "Add Liquidity")]
            AddLiquidity = 10
        }

        /// <summary>
        /// Will display a value when the order has advanced order rules associated with it or is part of a bracket order.
        /// </summary>
        /// <value>Will display a value when the order has advanced order rules associated with it or is part of a bracket order.</value>
        [DataMember(Name="AdvancedOptions", EmitDefaultValue=false)]
        public AdvancedOptionsEnum AdvancedOptions { get; set; }
        /// <summary>
        /// * FU &#x3D; Future * EQ &#x3D; Equity                   * OP &#x3D; Stock Option                   * FX &#x3D; Forex 
        /// </summary>
        /// <value>* FU &#x3D; Future * EQ &#x3D; Equity                   * OP &#x3D; Stock Option                   * FX &#x3D; Forex </value>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum AssetTypeEnum
        {
            
            /// <summary>
            /// Enum FU for value: FU
            /// </summary>
            [EnumMember(Value = "FU")]
            FU = 1,
            
            /// <summary>
            /// Enum EQ for value: EQ
            /// </summary>
            [EnumMember(Value = "EQ")]
            EQ = 2,
            
            /// <summary>
            /// Enum OP for value: OP
            /// </summary>
            [EnumMember(Value = "OP")]
            OP = 3,
            
            /// <summary>
            /// Enum FX for value: FX
            /// </summary>
            [EnumMember(Value = "FX")]
            FX = 4
        }

        /// <summary>
        /// * FU &#x3D; Future * EQ &#x3D; Equity                   * OP &#x3D; Stock Option                   * FX &#x3D; Forex 
        /// </summary>
        /// <value>* FU &#x3D; Future * EQ &#x3D; Equity                   * OP &#x3D; Stock Option                   * FX &#x3D; Forex </value>
        [DataMember(Name="AssetType", EmitDefaultValue=false)]
        public AssetTypeEnum AssetType { get; set; }
        /// <summary>
        /// * OPN, ACK, UCN &#x3D; Open Orders - - New order request pending, cancel request pending. * FLL, FLP &#x3D; Filled Orders - - Partially-Filled and remaining canceled. * FPR &#x3D; Partially Filled Orders. * OUT &#x3D; Canceled Orders. * REJ, TSC &#x3D; Rejected Orders - - It is an internal server(s) unsolicited cancel, not final status. * EXP &#x3D; Expired Orders. * BRO &#x3D; Broken Orders - - Not necessarily the order’s final status since later it may be reinstated to open. * CAN &#x3D; Exch. Canceled Orders. * LAT &#x3D; Too Late Orders - - Not the order’s final status. * DON &#x3D; Queued Orders. 
        /// </summary>
        /// <value>* OPN, ACK, UCN &#x3D; Open Orders - - New order request pending, cancel request pending. * FLL, FLP &#x3D; Filled Orders - - Partially-Filled and remaining canceled. * FPR &#x3D; Partially Filled Orders. * OUT &#x3D; Canceled Orders. * REJ, TSC &#x3D; Rejected Orders - - It is an internal server(s) unsolicited cancel, not final status. * EXP &#x3D; Expired Orders. * BRO &#x3D; Broken Orders - - Not necessarily the order’s final status since later it may be reinstated to open. * CAN &#x3D; Exch. Canceled Orders. * LAT &#x3D; Too Late Orders - - Not the order’s final status. * DON &#x3D; Queued Orders. </value>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum StatusEnum
        {
            
            /// <summary>
            /// Enum OPN for value: OPN
            /// </summary>
            [EnumMember(Value = "OPN")]
            OPN = 1,
            
            /// <summary>
            /// Enum ACK for value: ACK
            /// </summary>
            [EnumMember(Value = "ACK")]
            ACK = 2,
            
            /// <summary>
            /// Enum UCN for value: UCN
            /// </summary>
            [EnumMember(Value = "UCN")]
            UCN = 3,
            
            /// <summary>
            /// Enum FLL for value: FLL
            /// </summary>
            [EnumMember(Value = "FLL")]
            FLL = 4,
            
            /// <summary>
            /// Enum FLP for value: FLP
            /// </summary>
            [EnumMember(Value = "FLP")]
            FLP = 5,
            
            /// <summary>
            /// Enum FPR for value: FPR
            /// </summary>
            [EnumMember(Value = "FPR")]
            FPR = 6,
            
            /// <summary>
            /// Enum OUT for value: OUT
            /// </summary>
            [EnumMember(Value = "OUT")]
            OUT = 7,
            
            /// <summary>
            /// Enum REJ for value: REJ
            /// </summary>
            [EnumMember(Value = "REJ")]
            REJ = 8,
            
            /// <summary>
            /// Enum TSC for value: TSC
            /// </summary>
            [EnumMember(Value = "TSC")]
            TSC = 9,
            
            /// <summary>
            /// Enum Exp for value: Exp
            /// </summary>
            [EnumMember(Value = "Exp")]
            Exp = 10,
            
            /// <summary>
            /// Enum BRO for value: BRO
            /// </summary>
            [EnumMember(Value = "BRO")]
            BRO = 11,
            
            /// <summary>
            /// Enum CAN for value: CAN
            /// </summary>
            [EnumMember(Value = "CAN")]
            CAN = 12,
            
            /// <summary>
            /// Enum LAT for value: LAT
            /// </summary>
            [EnumMember(Value = "LAT")]
            LAT = 13,
            
            /// <summary>
            /// Enum DON for value: DON
            /// </summary>
            [EnumMember(Value = "DON")]
            DON = 14
        }

        /// <summary>
        /// * OPN, ACK, UCN &#x3D; Open Orders - - New order request pending, cancel request pending. * FLL, FLP &#x3D; Filled Orders - - Partially-Filled and remaining canceled. * FPR &#x3D; Partially Filled Orders. * OUT &#x3D; Canceled Orders. * REJ, TSC &#x3D; Rejected Orders - - It is an internal server(s) unsolicited cancel, not final status. * EXP &#x3D; Expired Orders. * BRO &#x3D; Broken Orders - - Not necessarily the order’s final status since later it may be reinstated to open. * CAN &#x3D; Exch. Canceled Orders. * LAT &#x3D; Too Late Orders - - Not the order’s final status. * DON &#x3D; Queued Orders. 
        /// </summary>
        /// <value>* OPN, ACK, UCN &#x3D; Open Orders - - New order request pending, cancel request pending. * FLL, FLP &#x3D; Filled Orders - - Partially-Filled and remaining canceled. * FPR &#x3D; Partially Filled Orders. * OUT &#x3D; Canceled Orders. * REJ, TSC &#x3D; Rejected Orders - - It is an internal server(s) unsolicited cancel, not final status. * EXP &#x3D; Expired Orders. * BRO &#x3D; Broken Orders - - Not necessarily the order’s final status since later it may be reinstated to open. * CAN &#x3D; Exch. Canceled Orders. * LAT &#x3D; Too Late Orders - - Not the order’s final status. * DON &#x3D; Queued Orders. </value>
        [DataMember(Name="Status", EmitDefaultValue=false)]
        public StatusEnum Status { get; set; }
        /// <summary>
        /// Type of order.
        /// </summary>
        /// <value>Type of order.</value>
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
        /// Type of order.
        /// </summary>
        /// <value>Type of order.</value>
        [DataMember(Name="Type", EmitDefaultValue=false)]
        public TypeEnum Type { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="AccountOrdersDefinitionInner" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected AccountOrdersDefinitionInner() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="AccountOrdersDefinitionInner" /> class.
        /// </summary>
        /// <param name="accountID">ID that identifies a specific TradeStation account that is being used for a particular order. (required).</param>
        /// <param name="advancedOptions">Will display a value when the order has advanced order rules associated with it or is part of a bracket order. (required).</param>
        /// <param name="alias">A user specified name that identifies a TradeStation account. (required).</param>
        /// <param name="assetType">* FU &#x3D; Future * EQ &#x3D; Equity                   * OP &#x3D; Stock Option                   * FX &#x3D; Forex  (required).</param>
        /// <param name="commissionFee">Commission paid in an order. (required).</param>
        /// <param name="contractExpireDate">Displays the contract expiration date for orders specified in contracts. (required).</param>
        /// <param name="conversionRate">Conversion rate used to translate the position’s market value and PNL from symbol currency to account currency. (required).</param>
        /// <param name="costBasisCalculation">Only applies to margin accounts based in Japan. When set to &#39;FSA&#39; an additional property, RealizedExpenses, will be included in the payload.  For other account types, the value will always be &#39;None&#39; (required).</param>
        /// <param name="country">The country of origin for the symbol..</param>
        /// <param name="denomination">Currency used to complete the order. (required).</param>
        /// <param name="displayName">Displays the Monex user ID. Used only by Monex..</param>
        /// <param name="displayType">Number of decimal points to display for the price. (required).</param>
        /// <param name="duration">The amount of time for which an order is valid. (required).</param>
        /// <param name="executeQuantity">Number of shares that have been executed. (required).</param>
        /// <param name="filledCanceled">The time the order was filled or canceled. (required).</param>
        /// <param name="filledPrice">At the top level, this is the average fill price. For expanded levels, this is the actual execution price. (required).</param>
        /// <param name="filledPriceText">String value for FilledPrice attribute. (required).</param>
        /// <param name="groupName">It can be used to identify orders that are part of the same bracket. (required).</param>
        /// <param name="legs">legs (required).</param>
        /// <param name="limitPrice">The limit price for Limit and Stop Limit orders. (required).</param>
        /// <param name="limitPriceText">String value of LimitPrice attribute. (required).</param>
        /// <param name="marketActivationRules">Set of market-based activation rules that must be met before order is sent to the exchange. .</param>
        /// <param name="minMove">Multiplying factor using the display type to determine the minimum price increment the asset trades in. For options the MinMove may vary. If the MinMove is negative, then the MinMove is dependent on the price. The whole number portion of the min move is the threshold. The leftmost two digits to the right of the decimal (X.XXXX) indicate the min move beneath the threshold, and the rightmost two digits (X.XXXX) indicate the min move above the threshold.. (required).</param>
        /// <param name="orderID">ID of the current order. (required).</param>
        /// <param name="originator">Identifies the TradeStation account that originated a particular order. (required).</param>
        /// <param name="quantity">The requested number of shares or contracts for a particular order. (required).</param>
        /// <param name="quantityLeft">In a partially filled order, this is the number of shares or contracts that were unfilled. (required).</param>
        /// <param name="realizedExpenses">Only applies to margin accounts based in Japan.  When CostBasisCalculation is to &#39;FSA&#39;, RealizedExpenses, will be included in the payload, and will convey realized interest expenses and commissions for closing orders..</param>
        /// <param name="rejectReason">If an order has been rejected, this will display the rejection reason. (required).</param>
        /// <param name="routing">Identifies the routing selection made by the customer when placing the order. (required).</param>
        /// <param name="showOnlyQuantity">This option allows you to hide the true number of shares that you wish to buy or sell..</param>
        /// <param name="spread">The spread type for an option order. (required).</param>
        /// <param name="status">* OPN, ACK, UCN &#x3D; Open Orders - - New order request pending, cancel request pending. * FLL, FLP &#x3D; Filled Orders - - Partially-Filled and remaining canceled. * FPR &#x3D; Partially Filled Orders. * OUT &#x3D; Canceled Orders. * REJ, TSC &#x3D; Rejected Orders - - It is an internal server(s) unsolicited cancel, not final status. * EXP &#x3D; Expired Orders. * BRO &#x3D; Broken Orders - - Not necessarily the order’s final status since later it may be reinstated to open. * CAN &#x3D; Exch. Canceled Orders. * LAT &#x3D; Too Late Orders - - Not the order’s final status. * DON &#x3D; Queued Orders.  (required).</param>
        /// <param name="statusDescription">Description for Status attribute. (required).</param>
        /// <param name="stopPrice">The stop price for stop orders. (required).</param>
        /// <param name="stopPriceText">String value for StopPrice attribute. (required).</param>
        /// <param name="symbol">Symbol to trade. (required).</param>
        /// <param name="timeActivationRules">Set of time-based activation rules that must be met before order is sent to the exchange. .</param>
        /// <param name="timeStamp">Time the order was placed. (required).</param>
        /// <param name="trailingStop">trailingStop.</param>
        /// <param name="triggeredBy">Will display a value if a stop limit or stop market order has been triggered. (required).</param>
        /// <param name="type">Type of order. (required).</param>
        /// <param name="unbundledRouteFee">Will contain a value if the order has received a routing fee. (required).</param>
        public AccountOrdersDefinitionInner(string accountID = default(string), AdvancedOptionsEnum advancedOptions = default(AdvancedOptionsEnum), string alias = default(string), AssetTypeEnum assetType = default(AssetTypeEnum), decimal? commissionFee = default(decimal?), string contractExpireDate = default(string), decimal? conversionRate = default(decimal?), string costBasisCalculation = default(string), string country = default(string), string denomination = default(string), string displayName = default(string), decimal? displayType = default(decimal?), string duration = default(string), decimal? executeQuantity = default(decimal?), string filledCanceled = default(string), decimal? filledPrice = default(decimal?), string filledPriceText = default(string), string groupName = default(string), List<AccountOrdersDefinitionInnerLegs> legs = default(List<AccountOrdersDefinitionInnerLegs>), decimal? limitPrice = default(decimal?), string limitPriceText = default(string), List<MarketActivationRuleDefinition> marketActivationRules = default(List<MarketActivationRuleDefinition>), decimal? minMove = default(decimal?), decimal? orderID = default(decimal?), decimal? originator = default(decimal?), decimal? quantity = default(decimal?), decimal? quantityLeft = default(decimal?), decimal? realizedExpenses = default(decimal?), string rejectReason = default(string), string routing = default(string), int? showOnlyQuantity = default(int?), string spread = default(string), StatusEnum status = default(StatusEnum), string statusDescription = default(string), decimal? stopPrice = default(decimal?), string stopPriceText = default(string), string symbol = default(string), List<TimeActivationRuleDefinition> timeActivationRules = default(List<TimeActivationRuleDefinition>), string timeStamp = default(string), TrailingStopDefinition trailingStop = default(TrailingStopDefinition), string triggeredBy = default(string), TypeEnum type = default(TypeEnum), decimal? unbundledRouteFee = default(decimal?))
        {
            // to ensure "accountID" is required (not null)
            if (accountID == null)
            {
                throw new InvalidDataException("accountID is a required property for AccountOrdersDefinitionInner and cannot be null");
            }
            else
            {
                this.AccountID = accountID;
            }
            // to ensure "advancedOptions" is required (not null)
            if (advancedOptions == null)
            {
                throw new InvalidDataException("advancedOptions is a required property for AccountOrdersDefinitionInner and cannot be null");
            }
            else
            {
                this.AdvancedOptions = advancedOptions;
            }
            // to ensure "alias" is required (not null)
            if (alias == null)
            {
                throw new InvalidDataException("alias is a required property for AccountOrdersDefinitionInner and cannot be null");
            }
            else
            {
                this.Alias = alias;
            }
            // to ensure "assetType" is required (not null)
            if (assetType == null)
            {
                throw new InvalidDataException("assetType is a required property for AccountOrdersDefinitionInner and cannot be null");
            }
            else
            {
                this.AssetType = assetType;
            }
            // to ensure "commissionFee" is required (not null)
            if (commissionFee == null)
            {
                throw new InvalidDataException("commissionFee is a required property for AccountOrdersDefinitionInner and cannot be null");
            }
            else
            {
                this.CommissionFee = commissionFee;
            }
            // to ensure "contractExpireDate" is required (not null)
            if (contractExpireDate == null)
            {
                throw new InvalidDataException("contractExpireDate is a required property for AccountOrdersDefinitionInner and cannot be null");
            }
            else
            {
                this.ContractExpireDate = contractExpireDate;
            }
            // to ensure "conversionRate" is required (not null)
            if (conversionRate == null)
            {
                throw new InvalidDataException("conversionRate is a required property for AccountOrdersDefinitionInner and cannot be null");
            }
            else
            {
                this.ConversionRate = conversionRate;
            }
            // to ensure "costBasisCalculation" is required (not null)
            if (costBasisCalculation == null)
            {
                throw new InvalidDataException("costBasisCalculation is a required property for AccountOrdersDefinitionInner and cannot be null");
            }
            else
            {
                this.CostBasisCalculation = costBasisCalculation;
            }
            // to ensure "denomination" is required (not null)
            if (denomination == null)
            {
                throw new InvalidDataException("denomination is a required property for AccountOrdersDefinitionInner and cannot be null");
            }
            else
            {
                this.Denomination = denomination;
            }
            // to ensure "displayType" is required (not null)
            if (displayType == null)
            {
                throw new InvalidDataException("displayType is a required property for AccountOrdersDefinitionInner and cannot be null");
            }
            else
            {
                this.DisplayType = displayType;
            }
            // to ensure "duration" is required (not null)
            if (duration == null)
            {
                throw new InvalidDataException("duration is a required property for AccountOrdersDefinitionInner and cannot be null");
            }
            else
            {
                this.Duration = duration;
            }
            // to ensure "executeQuantity" is required (not null)
            if (executeQuantity == null)
            {
                throw new InvalidDataException("executeQuantity is a required property for AccountOrdersDefinitionInner and cannot be null");
            }
            else
            {
                this.ExecuteQuantity = executeQuantity;
            }
            // to ensure "filledCanceled" is required (not null)
            if (filledCanceled == null)
            {
                throw new InvalidDataException("filledCanceled is a required property for AccountOrdersDefinitionInner and cannot be null");
            }
            else
            {
                this.FilledCanceled = filledCanceled;
            }
            // to ensure "filledPrice" is required (not null)
            if (filledPrice == null)
            {
                throw new InvalidDataException("filledPrice is a required property for AccountOrdersDefinitionInner and cannot be null");
            }
            else
            {
                this.FilledPrice = filledPrice;
            }
            // to ensure "filledPriceText" is required (not null)
            if (filledPriceText == null)
            {
                throw new InvalidDataException("filledPriceText is a required property for AccountOrdersDefinitionInner and cannot be null");
            }
            else
            {
                this.FilledPriceText = filledPriceText;
            }
            // to ensure "groupName" is required (not null)
            if (groupName == null)
            {
                throw new InvalidDataException("groupName is a required property for AccountOrdersDefinitionInner and cannot be null");
            }
            else
            {
                this.GroupName = groupName;
            }
            // to ensure "legs" is required (not null)
            if (legs == null)
            {
                throw new InvalidDataException("legs is a required property for AccountOrdersDefinitionInner and cannot be null");
            }
            else
            {
                this.Legs = legs;
            }
            // to ensure "limitPrice" is required (not null)
            if (limitPrice == null)
            {
                throw new InvalidDataException("limitPrice is a required property for AccountOrdersDefinitionInner and cannot be null");
            }
            else
            {
                this.LimitPrice = limitPrice;
            }
            // to ensure "limitPriceText" is required (not null)
            if (limitPriceText == null)
            {
                throw new InvalidDataException("limitPriceText is a required property for AccountOrdersDefinitionInner and cannot be null");
            }
            else
            {
                this.LimitPriceText = limitPriceText;
            }
            // to ensure "minMove" is required (not null)
            if (minMove == null)
            {
                throw new InvalidDataException("minMove is a required property for AccountOrdersDefinitionInner and cannot be null");
            }
            else
            {
                this.MinMove = minMove;
            }
            // to ensure "orderID" is required (not null)
            if (orderID == null)
            {
                throw new InvalidDataException("orderID is a required property for AccountOrdersDefinitionInner and cannot be null");
            }
            else
            {
                this.OrderID = orderID;
            }
            // to ensure "originator" is required (not null)
            if (originator == null)
            {
                throw new InvalidDataException("originator is a required property for AccountOrdersDefinitionInner and cannot be null");
            }
            else
            {
                this.Originator = originator;
            }
            // to ensure "quantity" is required (not null)
            if (quantity == null)
            {
                throw new InvalidDataException("quantity is a required property for AccountOrdersDefinitionInner and cannot be null");
            }
            else
            {
                this.Quantity = quantity;
            }
            // to ensure "quantityLeft" is required (not null)
            if (quantityLeft == null)
            {
                throw new InvalidDataException("quantityLeft is a required property for AccountOrdersDefinitionInner and cannot be null");
            }
            else
            {
                this.QuantityLeft = quantityLeft;
            }
            // to ensure "rejectReason" is required (not null)
            if (rejectReason == null)
            {
                throw new InvalidDataException("rejectReason is a required property for AccountOrdersDefinitionInner and cannot be null");
            }
            else
            {
                this.RejectReason = rejectReason;
            }
            // to ensure "routing" is required (not null)
            if (routing == null)
            {
                throw new InvalidDataException("routing is a required property for AccountOrdersDefinitionInner and cannot be null");
            }
            else
            {
                this.Routing = routing;
            }
            // to ensure "spread" is required (not null)
            if (spread == null)
            {
                throw new InvalidDataException("spread is a required property for AccountOrdersDefinitionInner and cannot be null");
            }
            else
            {
                this.Spread = spread;
            }
            // to ensure "status" is required (not null)
            if (status == null)
            {
                throw new InvalidDataException("status is a required property for AccountOrdersDefinitionInner and cannot be null");
            }
            else
            {
                this.Status = status;
            }
            // to ensure "statusDescription" is required (not null)
            if (statusDescription == null)
            {
                throw new InvalidDataException("statusDescription is a required property for AccountOrdersDefinitionInner and cannot be null");
            }
            else
            {
                this.StatusDescription = statusDescription;
            }
            // to ensure "stopPrice" is required (not null)
            if (stopPrice == null)
            {
                throw new InvalidDataException("stopPrice is a required property for AccountOrdersDefinitionInner and cannot be null");
            }
            else
            {
                this.StopPrice = stopPrice;
            }
            // to ensure "stopPriceText" is required (not null)
            if (stopPriceText == null)
            {
                throw new InvalidDataException("stopPriceText is a required property for AccountOrdersDefinitionInner and cannot be null");
            }
            else
            {
                this.StopPriceText = stopPriceText;
            }
            // to ensure "symbol" is required (not null)
            if (symbol == null)
            {
                throw new InvalidDataException("symbol is a required property for AccountOrdersDefinitionInner and cannot be null");
            }
            else
            {
                this.Symbol = symbol;
            }
            // to ensure "timeStamp" is required (not null)
            if (timeStamp == null)
            {
                throw new InvalidDataException("timeStamp is a required property for AccountOrdersDefinitionInner and cannot be null");
            }
            else
            {
                this.TimeStamp = timeStamp;
            }
            // to ensure "triggeredBy" is required (not null)
            if (triggeredBy == null)
            {
                throw new InvalidDataException("triggeredBy is a required property for AccountOrdersDefinitionInner and cannot be null");
            }
            else
            {
                this.TriggeredBy = triggeredBy;
            }
            // to ensure "type" is required (not null)
            if (type == null)
            {
                throw new InvalidDataException("type is a required property for AccountOrdersDefinitionInner and cannot be null");
            }
            else
            {
                this.Type = type;
            }
            // to ensure "unbundledRouteFee" is required (not null)
            if (unbundledRouteFee == null)
            {
                throw new InvalidDataException("unbundledRouteFee is a required property for AccountOrdersDefinitionInner and cannot be null");
            }
            else
            {
                this.UnbundledRouteFee = unbundledRouteFee;
            }
            this.Country = country;
            this.DisplayName = displayName;
            this.MarketActivationRules = marketActivationRules;
            this.RealizedExpenses = realizedExpenses;
            this.ShowOnlyQuantity = showOnlyQuantity;
            this.TimeActivationRules = timeActivationRules;
            this.TrailingStop = trailingStop;
        }
        
        /// <summary>
        /// ID that identifies a specific TradeStation account that is being used for a particular order.
        /// </summary>
        /// <value>ID that identifies a specific TradeStation account that is being used for a particular order.</value>
        [DataMember(Name="AccountID", EmitDefaultValue=false)]
        public string AccountID { get; set; }


        /// <summary>
        /// A user specified name that identifies a TradeStation account.
        /// </summary>
        /// <value>A user specified name that identifies a TradeStation account.</value>
        [DataMember(Name="Alias", EmitDefaultValue=false)]
        public string Alias { get; set; }


        /// <summary>
        /// Commission paid in an order.
        /// </summary>
        /// <value>Commission paid in an order.</value>
        [DataMember(Name="CommissionFee", EmitDefaultValue=false)]
        public decimal? CommissionFee { get; set; }

        /// <summary>
        /// Displays the contract expiration date for orders specified in contracts.
        /// </summary>
        /// <value>Displays the contract expiration date for orders specified in contracts.</value>
        [DataMember(Name="ContractExpireDate", EmitDefaultValue=false)]
        public string ContractExpireDate { get; set; }

        /// <summary>
        /// Conversion rate used to translate the position’s market value and PNL from symbol currency to account currency.
        /// </summary>
        /// <value>Conversion rate used to translate the position’s market value and PNL from symbol currency to account currency.</value>
        [DataMember(Name="ConversionRate", EmitDefaultValue=false)]
        public decimal? ConversionRate { get; set; }

        /// <summary>
        /// Only applies to margin accounts based in Japan. When set to &#39;FSA&#39; an additional property, RealizedExpenses, will be included in the payload.  For other account types, the value will always be &#39;None&#39;
        /// </summary>
        /// <value>Only applies to margin accounts based in Japan. When set to &#39;FSA&#39; an additional property, RealizedExpenses, will be included in the payload.  For other account types, the value will always be &#39;None&#39;</value>
        [DataMember(Name="CostBasisCalculation", EmitDefaultValue=false)]
        public string CostBasisCalculation { get; set; }

        /// <summary>
        /// The country of origin for the symbol.
        /// </summary>
        /// <value>The country of origin for the symbol.</value>
        [DataMember(Name="Country", EmitDefaultValue=false)]
        public string Country { get; set; }

        /// <summary>
        /// Currency used to complete the order.
        /// </summary>
        /// <value>Currency used to complete the order.</value>
        [DataMember(Name="Denomination", EmitDefaultValue=false)]
        public string Denomination { get; set; }

        /// <summary>
        /// Displays the Monex user ID. Used only by Monex.
        /// </summary>
        /// <value>Displays the Monex user ID. Used only by Monex.</value>
        [DataMember(Name="DisplayName", EmitDefaultValue=false)]
        public string DisplayName { get; set; }

        /// <summary>
        /// Number of decimal points to display for the price.
        /// </summary>
        /// <value>Number of decimal points to display for the price.</value>
        [DataMember(Name="DisplayType", EmitDefaultValue=false)]
        public decimal? DisplayType { get; set; }

        /// <summary>
        /// The amount of time for which an order is valid.
        /// </summary>
        /// <value>The amount of time for which an order is valid.</value>
        [DataMember(Name="Duration", EmitDefaultValue=false)]
        public string Duration { get; set; }

        /// <summary>
        /// Number of shares that have been executed.
        /// </summary>
        /// <value>Number of shares that have been executed.</value>
        [DataMember(Name="ExecuteQuantity", EmitDefaultValue=false)]
        public decimal? ExecuteQuantity { get; set; }

        /// <summary>
        /// The time the order was filled or canceled.
        /// </summary>
        /// <value>The time the order was filled or canceled.</value>
        [DataMember(Name="FilledCanceled", EmitDefaultValue=false)]
        public string FilledCanceled { get; set; }

        /// <summary>
        /// At the top level, this is the average fill price. For expanded levels, this is the actual execution price.
        /// </summary>
        /// <value>At the top level, this is the average fill price. For expanded levels, this is the actual execution price.</value>
        [DataMember(Name="FilledPrice", EmitDefaultValue=false)]
        public decimal? FilledPrice { get; set; }

        /// <summary>
        /// String value for FilledPrice attribute.
        /// </summary>
        /// <value>String value for FilledPrice attribute.</value>
        [DataMember(Name="FilledPriceText", EmitDefaultValue=false)]
        public string FilledPriceText { get; set; }

        /// <summary>
        /// It can be used to identify orders that are part of the same bracket.
        /// </summary>
        /// <value>It can be used to identify orders that are part of the same bracket.</value>
        [DataMember(Name="GroupName", EmitDefaultValue=false)]
        public string GroupName { get; set; }

        /// <summary>
        /// Gets or Sets Legs
        /// </summary>
        [DataMember(Name="Legs", EmitDefaultValue=false)]
        public List<AccountOrdersDefinitionInnerLegs> Legs { get; set; }

        /// <summary>
        /// The limit price for Limit and Stop Limit orders.
        /// </summary>
        /// <value>The limit price for Limit and Stop Limit orders.</value>
        [DataMember(Name="LimitPrice", EmitDefaultValue=false)]
        public decimal? LimitPrice { get; set; }

        /// <summary>
        /// String value of LimitPrice attribute.
        /// </summary>
        /// <value>String value of LimitPrice attribute.</value>
        [DataMember(Name="LimitPriceText", EmitDefaultValue=false)]
        public string LimitPriceText { get; set; }

        /// <summary>
        /// Set of market-based activation rules that must be met before order is sent to the exchange. 
        /// </summary>
        /// <value>Set of market-based activation rules that must be met before order is sent to the exchange. </value>
        [DataMember(Name="MarketActivationRules", EmitDefaultValue=false)]
        public List<MarketActivationRuleDefinition> MarketActivationRules { get; set; }

        /// <summary>
        /// Multiplying factor using the display type to determine the minimum price increment the asset trades in. For options the MinMove may vary. If the MinMove is negative, then the MinMove is dependent on the price. The whole number portion of the min move is the threshold. The leftmost two digits to the right of the decimal (X.XXXX) indicate the min move beneath the threshold, and the rightmost two digits (X.XXXX) indicate the min move above the threshold..
        /// </summary>
        /// <value>Multiplying factor using the display type to determine the minimum price increment the asset trades in. For options the MinMove may vary. If the MinMove is negative, then the MinMove is dependent on the price. The whole number portion of the min move is the threshold. The leftmost two digits to the right of the decimal (X.XXXX) indicate the min move beneath the threshold, and the rightmost two digits (X.XXXX) indicate the min move above the threshold..</value>
        [DataMember(Name="MinMove", EmitDefaultValue=false)]
        public decimal? MinMove { get; set; }

        /// <summary>
        /// ID of the current order.
        /// </summary>
        /// <value>ID of the current order.</value>
        [DataMember(Name="OrderID", EmitDefaultValue=false)]
        public decimal? OrderID { get; set; }

        /// <summary>
        /// Identifies the TradeStation account that originated a particular order.
        /// </summary>
        /// <value>Identifies the TradeStation account that originated a particular order.</value>
        [DataMember(Name="Originator", EmitDefaultValue=false)]
        public decimal? Originator { get; set; }

        /// <summary>
        /// The requested number of shares or contracts for a particular order.
        /// </summary>
        /// <value>The requested number of shares or contracts for a particular order.</value>
        [DataMember(Name="Quantity", EmitDefaultValue=false)]
        public decimal? Quantity { get; set; }

        /// <summary>
        /// In a partially filled order, this is the number of shares or contracts that were unfilled.
        /// </summary>
        /// <value>In a partially filled order, this is the number of shares or contracts that were unfilled.</value>
        [DataMember(Name="QuantityLeft", EmitDefaultValue=false)]
        public decimal? QuantityLeft { get; set; }

        /// <summary>
        /// Only applies to margin accounts based in Japan.  When CostBasisCalculation is to &#39;FSA&#39;, RealizedExpenses, will be included in the payload, and will convey realized interest expenses and commissions for closing orders.
        /// </summary>
        /// <value>Only applies to margin accounts based in Japan.  When CostBasisCalculation is to &#39;FSA&#39;, RealizedExpenses, will be included in the payload, and will convey realized interest expenses and commissions for closing orders.</value>
        [DataMember(Name="RealizedExpenses", EmitDefaultValue=false)]
        public decimal? RealizedExpenses { get; set; }

        /// <summary>
        /// If an order has been rejected, this will display the rejection reason.
        /// </summary>
        /// <value>If an order has been rejected, this will display the rejection reason.</value>
        [DataMember(Name="RejectReason", EmitDefaultValue=false)]
        public string RejectReason { get; set; }

        /// <summary>
        /// Identifies the routing selection made by the customer when placing the order.
        /// </summary>
        /// <value>Identifies the routing selection made by the customer when placing the order.</value>
        [DataMember(Name="Routing", EmitDefaultValue=false)]
        public string Routing { get; set; }

        /// <summary>
        /// This option allows you to hide the true number of shares that you wish to buy or sell.
        /// </summary>
        /// <value>This option allows you to hide the true number of shares that you wish to buy or sell.</value>
        [DataMember(Name="ShowOnlyQuantity", EmitDefaultValue=false)]
        public int? ShowOnlyQuantity { get; set; }

        /// <summary>
        /// The spread type for an option order.
        /// </summary>
        /// <value>The spread type for an option order.</value>
        [DataMember(Name="Spread", EmitDefaultValue=false)]
        public string Spread { get; set; }


        /// <summary>
        /// Description for Status attribute.
        /// </summary>
        /// <value>Description for Status attribute.</value>
        [DataMember(Name="StatusDescription", EmitDefaultValue=false)]
        public string StatusDescription { get; set; }

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
        [DataMember(Name="StopPriceText", EmitDefaultValue=false)]
        public string StopPriceText { get; set; }

        /// <summary>
        /// Symbol to trade.
        /// </summary>
        /// <value>Symbol to trade.</value>
        [DataMember(Name="Symbol", EmitDefaultValue=false)]
        public string Symbol { get; set; }

        /// <summary>
        /// Set of time-based activation rules that must be met before order is sent to the exchange. 
        /// </summary>
        /// <value>Set of time-based activation rules that must be met before order is sent to the exchange. </value>
        [DataMember(Name="TimeActivationRules", EmitDefaultValue=false)]
        public List<TimeActivationRuleDefinition> TimeActivationRules { get; set; }

        /// <summary>
        /// Time the order was placed.
        /// </summary>
        /// <value>Time the order was placed.</value>
        [DataMember(Name="TimeStamp", EmitDefaultValue=false)]
        public string TimeStamp { get; set; }

        /// <summary>
        /// Gets or Sets TrailingStop
        /// </summary>
        [DataMember(Name="TrailingStop", EmitDefaultValue=false)]
        public TrailingStopDefinition TrailingStop { get; set; }

        /// <summary>
        /// Will display a value if a stop limit or stop market order has been triggered.
        /// </summary>
        /// <value>Will display a value if a stop limit or stop market order has been triggered.</value>
        [DataMember(Name="TriggeredBy", EmitDefaultValue=false)]
        public string TriggeredBy { get; set; }


        /// <summary>
        /// Will contain a value if the order has received a routing fee.
        /// </summary>
        /// <value>Will contain a value if the order has received a routing fee.</value>
        [DataMember(Name="UnbundledRouteFee", EmitDefaultValue=false)]
        public decimal? UnbundledRouteFee { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class AccountOrdersDefinitionInner {\n");
            sb.Append("  AccountID: ").Append(AccountID).Append("\n");
            sb.Append("  AdvancedOptions: ").Append(AdvancedOptions).Append("\n");
            sb.Append("  Alias: ").Append(Alias).Append("\n");
            sb.Append("  AssetType: ").Append(AssetType).Append("\n");
            sb.Append("  CommissionFee: ").Append(CommissionFee).Append("\n");
            sb.Append("  ContractExpireDate: ").Append(ContractExpireDate).Append("\n");
            sb.Append("  ConversionRate: ").Append(ConversionRate).Append("\n");
            sb.Append("  CostBasisCalculation: ").Append(CostBasisCalculation).Append("\n");
            sb.Append("  Country: ").Append(Country).Append("\n");
            sb.Append("  Denomination: ").Append(Denomination).Append("\n");
            sb.Append("  DisplayName: ").Append(DisplayName).Append("\n");
            sb.Append("  DisplayType: ").Append(DisplayType).Append("\n");
            sb.Append("  Duration: ").Append(Duration).Append("\n");
            sb.Append("  ExecuteQuantity: ").Append(ExecuteQuantity).Append("\n");
            sb.Append("  FilledCanceled: ").Append(FilledCanceled).Append("\n");
            sb.Append("  FilledPrice: ").Append(FilledPrice).Append("\n");
            sb.Append("  FilledPriceText: ").Append(FilledPriceText).Append("\n");
            sb.Append("  GroupName: ").Append(GroupName).Append("\n");
            sb.Append("  Legs: ").Append(Legs).Append("\n");
            sb.Append("  LimitPrice: ").Append(LimitPrice).Append("\n");
            sb.Append("  LimitPriceText: ").Append(LimitPriceText).Append("\n");
            sb.Append("  MarketActivationRules: ").Append(MarketActivationRules).Append("\n");
            sb.Append("  MinMove: ").Append(MinMove).Append("\n");
            sb.Append("  OrderID: ").Append(OrderID).Append("\n");
            sb.Append("  Originator: ").Append(Originator).Append("\n");
            sb.Append("  Quantity: ").Append(Quantity).Append("\n");
            sb.Append("  QuantityLeft: ").Append(QuantityLeft).Append("\n");
            sb.Append("  RealizedExpenses: ").Append(RealizedExpenses).Append("\n");
            sb.Append("  RejectReason: ").Append(RejectReason).Append("\n");
            sb.Append("  Routing: ").Append(Routing).Append("\n");
            sb.Append("  ShowOnlyQuantity: ").Append(ShowOnlyQuantity).Append("\n");
            sb.Append("  Spread: ").Append(Spread).Append("\n");
            sb.Append("  Status: ").Append(Status).Append("\n");
            sb.Append("  StatusDescription: ").Append(StatusDescription).Append("\n");
            sb.Append("  StopPrice: ").Append(StopPrice).Append("\n");
            sb.Append("  StopPriceText: ").Append(StopPriceText).Append("\n");
            sb.Append("  Symbol: ").Append(Symbol).Append("\n");
            sb.Append("  TimeActivationRules: ").Append(TimeActivationRules).Append("\n");
            sb.Append("  TimeStamp: ").Append(TimeStamp).Append("\n");
            sb.Append("  TrailingStop: ").Append(TrailingStop).Append("\n");
            sb.Append("  TriggeredBy: ").Append(TriggeredBy).Append("\n");
            sb.Append("  Type: ").Append(Type).Append("\n");
            sb.Append("  UnbundledRouteFee: ").Append(UnbundledRouteFee).Append("\n");
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
            return this.Equals(input as AccountOrdersDefinitionInner);
        }

        /// <summary>
        /// Returns true if AccountOrdersDefinitionInner instances are equal
        /// </summary>
        /// <param name="input">Instance of AccountOrdersDefinitionInner to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(AccountOrdersDefinitionInner input)
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
                    this.AdvancedOptions == input.AdvancedOptions ||
                    (this.AdvancedOptions != null &&
                    this.AdvancedOptions.Equals(input.AdvancedOptions))
                ) && 
                (
                    this.Alias == input.Alias ||
                    (this.Alias != null &&
                    this.Alias.Equals(input.Alias))
                ) && 
                (
                    this.AssetType == input.AssetType ||
                    (this.AssetType != null &&
                    this.AssetType.Equals(input.AssetType))
                ) && 
                (
                    this.CommissionFee == input.CommissionFee ||
                    (this.CommissionFee != null &&
                    this.CommissionFee.Equals(input.CommissionFee))
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
                    this.Denomination == input.Denomination ||
                    (this.Denomination != null &&
                    this.Denomination.Equals(input.Denomination))
                ) && 
                (
                    this.DisplayName == input.DisplayName ||
                    (this.DisplayName != null &&
                    this.DisplayName.Equals(input.DisplayName))
                ) && 
                (
                    this.DisplayType == input.DisplayType ||
                    (this.DisplayType != null &&
                    this.DisplayType.Equals(input.DisplayType))
                ) && 
                (
                    this.Duration == input.Duration ||
                    (this.Duration != null &&
                    this.Duration.Equals(input.Duration))
                ) && 
                (
                    this.ExecuteQuantity == input.ExecuteQuantity ||
                    (this.ExecuteQuantity != null &&
                    this.ExecuteQuantity.Equals(input.ExecuteQuantity))
                ) && 
                (
                    this.FilledCanceled == input.FilledCanceled ||
                    (this.FilledCanceled != null &&
                    this.FilledCanceled.Equals(input.FilledCanceled))
                ) && 
                (
                    this.FilledPrice == input.FilledPrice ||
                    (this.FilledPrice != null &&
                    this.FilledPrice.Equals(input.FilledPrice))
                ) && 
                (
                    this.FilledPriceText == input.FilledPriceText ||
                    (this.FilledPriceText != null &&
                    this.FilledPriceText.Equals(input.FilledPriceText))
                ) && 
                (
                    this.GroupName == input.GroupName ||
                    (this.GroupName != null &&
                    this.GroupName.Equals(input.GroupName))
                ) && 
                (
                    this.Legs == input.Legs ||
                    this.Legs != null &&
                    this.Legs.SequenceEqual(input.Legs)
                ) && 
                (
                    this.LimitPrice == input.LimitPrice ||
                    (this.LimitPrice != null &&
                    this.LimitPrice.Equals(input.LimitPrice))
                ) && 
                (
                    this.LimitPriceText == input.LimitPriceText ||
                    (this.LimitPriceText != null &&
                    this.LimitPriceText.Equals(input.LimitPriceText))
                ) && 
                (
                    this.MarketActivationRules == input.MarketActivationRules ||
                    this.MarketActivationRules != null &&
                    this.MarketActivationRules.SequenceEqual(input.MarketActivationRules)
                ) && 
                (
                    this.MinMove == input.MinMove ||
                    (this.MinMove != null &&
                    this.MinMove.Equals(input.MinMove))
                ) && 
                (
                    this.OrderID == input.OrderID ||
                    (this.OrderID != null &&
                    this.OrderID.Equals(input.OrderID))
                ) && 
                (
                    this.Originator == input.Originator ||
                    (this.Originator != null &&
                    this.Originator.Equals(input.Originator))
                ) && 
                (
                    this.Quantity == input.Quantity ||
                    (this.Quantity != null &&
                    this.Quantity.Equals(input.Quantity))
                ) && 
                (
                    this.QuantityLeft == input.QuantityLeft ||
                    (this.QuantityLeft != null &&
                    this.QuantityLeft.Equals(input.QuantityLeft))
                ) && 
                (
                    this.RealizedExpenses == input.RealizedExpenses ||
                    (this.RealizedExpenses != null &&
                    this.RealizedExpenses.Equals(input.RealizedExpenses))
                ) && 
                (
                    this.RejectReason == input.RejectReason ||
                    (this.RejectReason != null &&
                    this.RejectReason.Equals(input.RejectReason))
                ) && 
                (
                    this.Routing == input.Routing ||
                    (this.Routing != null &&
                    this.Routing.Equals(input.Routing))
                ) && 
                (
                    this.ShowOnlyQuantity == input.ShowOnlyQuantity ||
                    (this.ShowOnlyQuantity != null &&
                    this.ShowOnlyQuantity.Equals(input.ShowOnlyQuantity))
                ) && 
                (
                    this.Spread == input.Spread ||
                    (this.Spread != null &&
                    this.Spread.Equals(input.Spread))
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
                    this.StopPrice == input.StopPrice ||
                    (this.StopPrice != null &&
                    this.StopPrice.Equals(input.StopPrice))
                ) && 
                (
                    this.StopPriceText == input.StopPriceText ||
                    (this.StopPriceText != null &&
                    this.StopPriceText.Equals(input.StopPriceText))
                ) && 
                (
                    this.Symbol == input.Symbol ||
                    (this.Symbol != null &&
                    this.Symbol.Equals(input.Symbol))
                ) && 
                (
                    this.TimeActivationRules == input.TimeActivationRules ||
                    this.TimeActivationRules != null &&
                    this.TimeActivationRules.SequenceEqual(input.TimeActivationRules)
                ) && 
                (
                    this.TimeStamp == input.TimeStamp ||
                    (this.TimeStamp != null &&
                    this.TimeStamp.Equals(input.TimeStamp))
                ) && 
                (
                    this.TrailingStop == input.TrailingStop ||
                    (this.TrailingStop != null &&
                    this.TrailingStop.Equals(input.TrailingStop))
                ) && 
                (
                    this.TriggeredBy == input.TriggeredBy ||
                    (this.TriggeredBy != null &&
                    this.TriggeredBy.Equals(input.TriggeredBy))
                ) && 
                (
                    this.Type == input.Type ||
                    (this.Type != null &&
                    this.Type.Equals(input.Type))
                ) && 
                (
                    this.UnbundledRouteFee == input.UnbundledRouteFee ||
                    (this.UnbundledRouteFee != null &&
                    this.UnbundledRouteFee.Equals(input.UnbundledRouteFee))
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
                if (this.AdvancedOptions != null)
                    hashCode = hashCode * 59 + this.AdvancedOptions.GetHashCode();
                if (this.Alias != null)
                    hashCode = hashCode * 59 + this.Alias.GetHashCode();
                if (this.AssetType != null)
                    hashCode = hashCode * 59 + this.AssetType.GetHashCode();
                if (this.CommissionFee != null)
                    hashCode = hashCode * 59 + this.CommissionFee.GetHashCode();
                if (this.ContractExpireDate != null)
                    hashCode = hashCode * 59 + this.ContractExpireDate.GetHashCode();
                if (this.ConversionRate != null)
                    hashCode = hashCode * 59 + this.ConversionRate.GetHashCode();
                if (this.CostBasisCalculation != null)
                    hashCode = hashCode * 59 + this.CostBasisCalculation.GetHashCode();
                if (this.Country != null)
                    hashCode = hashCode * 59 + this.Country.GetHashCode();
                if (this.Denomination != null)
                    hashCode = hashCode * 59 + this.Denomination.GetHashCode();
                if (this.DisplayName != null)
                    hashCode = hashCode * 59 + this.DisplayName.GetHashCode();
                if (this.DisplayType != null)
                    hashCode = hashCode * 59 + this.DisplayType.GetHashCode();
                if (this.Duration != null)
                    hashCode = hashCode * 59 + this.Duration.GetHashCode();
                if (this.ExecuteQuantity != null)
                    hashCode = hashCode * 59 + this.ExecuteQuantity.GetHashCode();
                if (this.FilledCanceled != null)
                    hashCode = hashCode * 59 + this.FilledCanceled.GetHashCode();
                if (this.FilledPrice != null)
                    hashCode = hashCode * 59 + this.FilledPrice.GetHashCode();
                if (this.FilledPriceText != null)
                    hashCode = hashCode * 59 + this.FilledPriceText.GetHashCode();
                if (this.GroupName != null)
                    hashCode = hashCode * 59 + this.GroupName.GetHashCode();
                if (this.Legs != null)
                    hashCode = hashCode * 59 + this.Legs.GetHashCode();
                if (this.LimitPrice != null)
                    hashCode = hashCode * 59 + this.LimitPrice.GetHashCode();
                if (this.LimitPriceText != null)
                    hashCode = hashCode * 59 + this.LimitPriceText.GetHashCode();
                if (this.MarketActivationRules != null)
                    hashCode = hashCode * 59 + this.MarketActivationRules.GetHashCode();
                if (this.MinMove != null)
                    hashCode = hashCode * 59 + this.MinMove.GetHashCode();
                if (this.OrderID != null)
                    hashCode = hashCode * 59 + this.OrderID.GetHashCode();
                if (this.Originator != null)
                    hashCode = hashCode * 59 + this.Originator.GetHashCode();
                if (this.Quantity != null)
                    hashCode = hashCode * 59 + this.Quantity.GetHashCode();
                if (this.QuantityLeft != null)
                    hashCode = hashCode * 59 + this.QuantityLeft.GetHashCode();
                if (this.RealizedExpenses != null)
                    hashCode = hashCode * 59 + this.RealizedExpenses.GetHashCode();
                if (this.RejectReason != null)
                    hashCode = hashCode * 59 + this.RejectReason.GetHashCode();
                if (this.Routing != null)
                    hashCode = hashCode * 59 + this.Routing.GetHashCode();
                if (this.ShowOnlyQuantity != null)
                    hashCode = hashCode * 59 + this.ShowOnlyQuantity.GetHashCode();
                if (this.Spread != null)
                    hashCode = hashCode * 59 + this.Spread.GetHashCode();
                if (this.Status != null)
                    hashCode = hashCode * 59 + this.Status.GetHashCode();
                if (this.StatusDescription != null)
                    hashCode = hashCode * 59 + this.StatusDescription.GetHashCode();
                if (this.StopPrice != null)
                    hashCode = hashCode * 59 + this.StopPrice.GetHashCode();
                if (this.StopPriceText != null)
                    hashCode = hashCode * 59 + this.StopPriceText.GetHashCode();
                if (this.Symbol != null)
                    hashCode = hashCode * 59 + this.Symbol.GetHashCode();
                if (this.TimeActivationRules != null)
                    hashCode = hashCode * 59 + this.TimeActivationRules.GetHashCode();
                if (this.TimeStamp != null)
                    hashCode = hashCode * 59 + this.TimeStamp.GetHashCode();
                if (this.TrailingStop != null)
                    hashCode = hashCode * 59 + this.TrailingStop.GetHashCode();
                if (this.TriggeredBy != null)
                    hashCode = hashCode * 59 + this.TriggeredBy.GetHashCode();
                if (this.Type != null)
                    hashCode = hashCode * 59 + this.Type.GetHashCode();
                if (this.UnbundledRouteFee != null)
                    hashCode = hashCode * 59 + this.UnbundledRouteFee.GetHashCode();
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

            // AssetType (string) minLength
            //if(this.AssetType != null && this.AssetType.Length < 1)
            //{
            //    yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for AssetType, length must be greater than 1.", new [] { "AssetType" });
            //}

            // ContractExpireDate (string) minLength
            if(this.ContractExpireDate != null && this.ContractExpireDate.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for ContractExpireDate, length must be greater than 1.", new [] { "ContractExpireDate" });
            }

            // Denomination (string) minLength
            if(this.Denomination != null && this.Denomination.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Denomination, length must be greater than 1.", new [] { "Denomination" });
            }

            // Duration (string) minLength
            if(this.Duration != null && this.Duration.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Duration, length must be greater than 1.", new [] { "Duration" });
            }

            // FilledCanceled (string) minLength
            if(this.FilledCanceled != null && this.FilledCanceled.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for FilledCanceled, length must be greater than 1.", new [] { "FilledCanceled" });
            }

            // FilledPriceText (string) minLength
            if(this.FilledPriceText != null && this.FilledPriceText.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for FilledPriceText, length must be greater than 1.", new [] { "FilledPriceText" });
            }

            // LimitPriceText (string) minLength
            if(this.LimitPriceText != null && this.LimitPriceText.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for LimitPriceText, length must be greater than 1.", new [] { "LimitPriceText" });
            }

            // Routing (string) minLength
            if(this.Routing != null && this.Routing.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Routing, length must be greater than 1.", new [] { "Routing" });
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

            // StopPriceText (string) minLength
            if(this.StopPriceText != null && this.StopPriceText.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for StopPriceText, length must be greater than 1.", new [] { "StopPriceText" });
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

            // Type (string) minLength
            //if(this.Type != null && this.Type.Length < 1)
            //{
            //    yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Type, length must be greater than 1.", new [] { "Type" });
            //}

            yield break;
        }
    }

}
