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
    /// AccountBalancesDefinitionInnerCurrencyDetails
    /// </summary>
    [DataContract]
    public partial class AccountBalancesDefinitionInnerCurrencyDetails :  IEquatable<AccountBalancesDefinitionInnerCurrencyDetails>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AccountBalancesDefinitionInnerCurrencyDetails" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected AccountBalancesDefinitionInnerCurrencyDetails() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="AccountBalancesDefinitionInnerCurrencyDetails" /> class.
        /// </summary>
        /// <param name="accountOpenOrderInitMargin">The margin account balance denominated in the account currency required for entering a position on margin. (required).</param>
        /// <param name="bODAccountCashBalance">Indicates the dollar amount of Beginning Day Account Cash Balance. (required).</param>
        /// <param name="bODAccountOpenTradeEquity">Indicates the dollar amount of Beginning Day Trade Equity for the given futures account. (required).</param>
        /// <param name="bODAccountSecurities">Indicates the dollar amount of Beginning Day Account Securities. (required).</param>
        /// <param name="bODCashBalance">Indicates the dollar amount of Beginning Day Cash Balance for the given futures account. (required).</param>
        /// <param name="bODOpenTradeEquity">Indicates the dollar amount of Beginning Day Open Trade Equity. (required).</param>
        /// <param name="bODSecurities">Indicates the dollar amount of Beginning Day Securities. (required).</param>
        /// <param name="commission">The actual brokerage commission cost and routing fees (if applicable) for a trade based on the number of shares or contracts. (required).</param>
        /// <param name="currency">The base currency of the symbol. (required).</param>
        /// <param name="openOrderInitMargin">The dollar amount of Open Order Initial Margin for the given futures account. (required).</param>
        /// <param name="realTimeAccountCashBalance">Indicates the value of real-time account cash balance. (required).</param>
        /// <param name="realTimeAccountInitMargin">Indicates the value of real-time account initial margin. (required).</param>
        /// <param name="realTimeAccountMaintenanceMargin">Indicates the value of real-time account maintance margin. (required).</param>
        /// <param name="realTimeAccountMarginRequirement">Indicates the value of real-time account margin requirement. (required).</param>
        /// <param name="realTimeAccountRealizedProfitLoss">Indicates the value of real-time account realized profit or loss. (required).</param>
        /// <param name="realTimeAccountUnrealizedProfitLoss">Indicates the value of real-time account unrealized profit or loss. (required).</param>
        /// <param name="realTimeCashBalance">Indicates the value of real-time cash balance. (required).</param>
        /// <param name="realTimeInitMargin">Indicates the value of real-time initial margin. (required).</param>
        /// <param name="realTimeMaintenanceMargin">Indicates the value of real-time maintance margin. (required).</param>
        /// <param name="realTimeRealizedProfitLoss">Indicates the value of real-time realized profit or loss. (required).</param>
        /// <param name="realTimeUnrealizedProfitLoss">Indicates the value of real-time unrealized profit or loss. (required).</param>
        /// <param name="toAccountConversionRate">Indicates the rate used to convert from the currency of the symbol to the currency of the account. (required).</param>
        /// <param name="todayRealTimeUnrealizedProfitLoss">Indicates the value of today&#39;s real-time unrealized profit or loss. (required).</param>
        public AccountBalancesDefinitionInnerCurrencyDetails(decimal? accountOpenOrderInitMargin = default(decimal?), decimal? bODAccountCashBalance = default(decimal?), decimal? bODAccountOpenTradeEquity = default(decimal?), decimal? bODAccountSecurities = default(decimal?), decimal? bODCashBalance = default(decimal?), decimal? bODOpenTradeEquity = default(decimal?), decimal? bODSecurities = default(decimal?), decimal? commission = default(decimal?), string currency = default(string), decimal? openOrderInitMargin = default(decimal?), decimal? realTimeAccountCashBalance = default(decimal?), decimal? realTimeAccountInitMargin = default(decimal?), decimal? realTimeAccountMaintenanceMargin = default(decimal?), decimal? realTimeAccountMarginRequirement = default(decimal?), decimal? realTimeAccountRealizedProfitLoss = default(decimal?), decimal? realTimeAccountUnrealizedProfitLoss = default(decimal?), decimal? realTimeCashBalance = default(decimal?), decimal? realTimeInitMargin = default(decimal?), decimal? realTimeMaintenanceMargin = default(decimal?), decimal? realTimeRealizedProfitLoss = default(decimal?), decimal? realTimeUnrealizedProfitLoss = default(decimal?), decimal? toAccountConversionRate = default(decimal?), decimal? todayRealTimeUnrealizedProfitLoss = default(decimal?))
        {
            // to ensure "accountOpenOrderInitMargin" is required (not null)
            if (accountOpenOrderInitMargin == null)
            {
                throw new InvalidDataException("accountOpenOrderInitMargin is a required property for AccountBalancesDefinitionInnerCurrencyDetails and cannot be null");
            }
            else
            {
                this.AccountOpenOrderInitMargin = accountOpenOrderInitMargin;
            }
            // to ensure "bODAccountCashBalance" is required (not null)
            if (bODAccountCashBalance == null)
            {
                throw new InvalidDataException("bODAccountCashBalance is a required property for AccountBalancesDefinitionInnerCurrencyDetails and cannot be null");
            }
            else
            {
                this.BODAccountCashBalance = bODAccountCashBalance;
            }
            // to ensure "bODAccountOpenTradeEquity" is required (not null)
            if (bODAccountOpenTradeEquity == null)
            {
                throw new InvalidDataException("bODAccountOpenTradeEquity is a required property for AccountBalancesDefinitionInnerCurrencyDetails and cannot be null");
            }
            else
            {
                this.BODAccountOpenTradeEquity = bODAccountOpenTradeEquity;
            }
            // to ensure "bODAccountSecurities" is required (not null)
            if (bODAccountSecurities == null)
            {
                throw new InvalidDataException("bODAccountSecurities is a required property for AccountBalancesDefinitionInnerCurrencyDetails and cannot be null");
            }
            else
            {
                this.BODAccountSecurities = bODAccountSecurities;
            }
            // to ensure "bODCashBalance" is required (not null)
            if (bODCashBalance == null)
            {
                throw new InvalidDataException("bODCashBalance is a required property for AccountBalancesDefinitionInnerCurrencyDetails and cannot be null");
            }
            else
            {
                this.BODCashBalance = bODCashBalance;
            }
            // to ensure "bODOpenTradeEquity" is required (not null)
            if (bODOpenTradeEquity == null)
            {
                throw new InvalidDataException("bODOpenTradeEquity is a required property for AccountBalancesDefinitionInnerCurrencyDetails and cannot be null");
            }
            else
            {
                this.BODOpenTradeEquity = bODOpenTradeEquity;
            }
            // to ensure "bODSecurities" is required (not null)
            if (bODSecurities == null)
            {
                throw new InvalidDataException("bODSecurities is a required property for AccountBalancesDefinitionInnerCurrencyDetails and cannot be null");
            }
            else
            {
                this.BODSecurities = bODSecurities;
            }
            // to ensure "commission" is required (not null)
            if (commission == null)
            {
                throw new InvalidDataException("commission is a required property for AccountBalancesDefinitionInnerCurrencyDetails and cannot be null");
            }
            else
            {
                this.Commission = commission;
            }
            // to ensure "currency" is required (not null)
            if (currency == null)
            {
                throw new InvalidDataException("currency is a required property for AccountBalancesDefinitionInnerCurrencyDetails and cannot be null");
            }
            else
            {
                this.Currency = currency;
            }
            // to ensure "openOrderInitMargin" is required (not null)
            if (openOrderInitMargin == null)
            {
                throw new InvalidDataException("openOrderInitMargin is a required property for AccountBalancesDefinitionInnerCurrencyDetails and cannot be null");
            }
            else
            {
                this.OpenOrderInitMargin = openOrderInitMargin;
            }
            // to ensure "realTimeAccountCashBalance" is required (not null)
            if (realTimeAccountCashBalance == null)
            {
                throw new InvalidDataException("realTimeAccountCashBalance is a required property for AccountBalancesDefinitionInnerCurrencyDetails and cannot be null");
            }
            else
            {
                this.RealTimeAccountCashBalance = realTimeAccountCashBalance;
            }
            // to ensure "realTimeAccountInitMargin" is required (not null)
            if (realTimeAccountInitMargin == null)
            {
                throw new InvalidDataException("realTimeAccountInitMargin is a required property for AccountBalancesDefinitionInnerCurrencyDetails and cannot be null");
            }
            else
            {
                this.RealTimeAccountInitMargin = realTimeAccountInitMargin;
            }
            // to ensure "realTimeAccountMaintenanceMargin" is required (not null)
            if (realTimeAccountMaintenanceMargin == null)
            {
                throw new InvalidDataException("realTimeAccountMaintenanceMargin is a required property for AccountBalancesDefinitionInnerCurrencyDetails and cannot be null");
            }
            else
            {
                this.RealTimeAccountMaintenanceMargin = realTimeAccountMaintenanceMargin;
            }
            // to ensure "realTimeAccountMarginRequirement" is required (not null)
            if (realTimeAccountMarginRequirement == null)
            {
                throw new InvalidDataException("realTimeAccountMarginRequirement is a required property for AccountBalancesDefinitionInnerCurrencyDetails and cannot be null");
            }
            else
            {
                this.RealTimeAccountMarginRequirement = realTimeAccountMarginRequirement;
            }
            // to ensure "realTimeAccountRealizedProfitLoss" is required (not null)
            if (realTimeAccountRealizedProfitLoss == null)
            {
                throw new InvalidDataException("realTimeAccountRealizedProfitLoss is a required property for AccountBalancesDefinitionInnerCurrencyDetails and cannot be null");
            }
            else
            {
                this.RealTimeAccountRealizedProfitLoss = realTimeAccountRealizedProfitLoss;
            }
            // to ensure "realTimeAccountUnrealizedProfitLoss" is required (not null)
            if (realTimeAccountUnrealizedProfitLoss == null)
            {
                throw new InvalidDataException("realTimeAccountUnrealizedProfitLoss is a required property for AccountBalancesDefinitionInnerCurrencyDetails and cannot be null");
            }
            else
            {
                this.RealTimeAccountUnrealizedProfitLoss = realTimeAccountUnrealizedProfitLoss;
            }
            // to ensure "realTimeCashBalance" is required (not null)
            if (realTimeCashBalance == null)
            {
                throw new InvalidDataException("realTimeCashBalance is a required property for AccountBalancesDefinitionInnerCurrencyDetails and cannot be null");
            }
            else
            {
                this.RealTimeCashBalance = realTimeCashBalance;
            }
            // to ensure "realTimeInitMargin" is required (not null)
            if (realTimeInitMargin == null)
            {
                throw new InvalidDataException("realTimeInitMargin is a required property for AccountBalancesDefinitionInnerCurrencyDetails and cannot be null");
            }
            else
            {
                this.RealTimeInitMargin = realTimeInitMargin;
            }
            // to ensure "realTimeMaintenanceMargin" is required (not null)
            if (realTimeMaintenanceMargin == null)
            {
                throw new InvalidDataException("realTimeMaintenanceMargin is a required property for AccountBalancesDefinitionInnerCurrencyDetails and cannot be null");
            }
            else
            {
                this.RealTimeMaintenanceMargin = realTimeMaintenanceMargin;
            }
            // to ensure "realTimeRealizedProfitLoss" is required (not null)
            if (realTimeRealizedProfitLoss == null)
            {
                throw new InvalidDataException("realTimeRealizedProfitLoss is a required property for AccountBalancesDefinitionInnerCurrencyDetails and cannot be null");
            }
            else
            {
                this.RealTimeRealizedProfitLoss = realTimeRealizedProfitLoss;
            }
            // to ensure "realTimeUnrealizedProfitLoss" is required (not null)
            if (realTimeUnrealizedProfitLoss == null)
            {
                throw new InvalidDataException("realTimeUnrealizedProfitLoss is a required property for AccountBalancesDefinitionInnerCurrencyDetails and cannot be null");
            }
            else
            {
                this.RealTimeUnrealizedProfitLoss = realTimeUnrealizedProfitLoss;
            }
            // to ensure "toAccountConversionRate" is required (not null)
            if (toAccountConversionRate == null)
            {
                throw new InvalidDataException("toAccountConversionRate is a required property for AccountBalancesDefinitionInnerCurrencyDetails and cannot be null");
            }
            else
            {
                this.ToAccountConversionRate = toAccountConversionRate;
            }
            // to ensure "todayRealTimeUnrealizedProfitLoss" is required (not null)
            if (todayRealTimeUnrealizedProfitLoss == null)
            {
                throw new InvalidDataException("todayRealTimeUnrealizedProfitLoss is a required property for AccountBalancesDefinitionInnerCurrencyDetails and cannot be null");
            }
            else
            {
                this.TodayRealTimeUnrealizedProfitLoss = todayRealTimeUnrealizedProfitLoss;
            }
        }
        
        /// <summary>
        /// The margin account balance denominated in the account currency required for entering a position on margin.
        /// </summary>
        /// <value>The margin account balance denominated in the account currency required for entering a position on margin.</value>
        [DataMember(Name="AccountOpenOrderInitMargin", EmitDefaultValue=false)]
        public decimal? AccountOpenOrderInitMargin { get; set; }

        /// <summary>
        /// Indicates the dollar amount of Beginning Day Account Cash Balance.
        /// </summary>
        /// <value>Indicates the dollar amount of Beginning Day Account Cash Balance.</value>
        [DataMember(Name="BODAccountCashBalance", EmitDefaultValue=false)]
        public decimal? BODAccountCashBalance { get; set; }

        /// <summary>
        /// Indicates the dollar amount of Beginning Day Trade Equity for the given futures account.
        /// </summary>
        /// <value>Indicates the dollar amount of Beginning Day Trade Equity for the given futures account.</value>
        [DataMember(Name="BODAccountOpenTradeEquity", EmitDefaultValue=false)]
        public decimal? BODAccountOpenTradeEquity { get; set; }

        /// <summary>
        /// Indicates the dollar amount of Beginning Day Account Securities.
        /// </summary>
        /// <value>Indicates the dollar amount of Beginning Day Account Securities.</value>
        [DataMember(Name="BODAccountSecurities", EmitDefaultValue=false)]
        public decimal? BODAccountSecurities { get; set; }

        /// <summary>
        /// Indicates the dollar amount of Beginning Day Cash Balance for the given futures account.
        /// </summary>
        /// <value>Indicates the dollar amount of Beginning Day Cash Balance for the given futures account.</value>
        [DataMember(Name="BODCashBalance", EmitDefaultValue=false)]
        public decimal? BODCashBalance { get; set; }

        /// <summary>
        /// Indicates the dollar amount of Beginning Day Open Trade Equity.
        /// </summary>
        /// <value>Indicates the dollar amount of Beginning Day Open Trade Equity.</value>
        [DataMember(Name="BODOpenTradeEquity", EmitDefaultValue=false)]
        public decimal? BODOpenTradeEquity { get; set; }

        /// <summary>
        /// Indicates the dollar amount of Beginning Day Securities.
        /// </summary>
        /// <value>Indicates the dollar amount of Beginning Day Securities.</value>
        [DataMember(Name="BODSecurities", EmitDefaultValue=false)]
        public decimal? BODSecurities { get; set; }

        /// <summary>
        /// The actual brokerage commission cost and routing fees (if applicable) for a trade based on the number of shares or contracts.
        /// </summary>
        /// <value>The actual brokerage commission cost and routing fees (if applicable) for a trade based on the number of shares or contracts.</value>
        [DataMember(Name="Commission", EmitDefaultValue=false)]
        public decimal? Commission { get; set; }

        /// <summary>
        /// The base currency of the symbol.
        /// </summary>
        /// <value>The base currency of the symbol.</value>
        [DataMember(Name="Currency", EmitDefaultValue=false)]
        public string Currency { get; set; }

        /// <summary>
        /// The dollar amount of Open Order Initial Margin for the given futures account.
        /// </summary>
        /// <value>The dollar amount of Open Order Initial Margin for the given futures account.</value>
        [DataMember(Name="OpenOrderInitMargin", EmitDefaultValue=false)]
        public decimal? OpenOrderInitMargin { get; set; }

        /// <summary>
        /// Indicates the value of real-time account cash balance.
        /// </summary>
        /// <value>Indicates the value of real-time account cash balance.</value>
        [DataMember(Name="RealTimeAccountCashBalance", EmitDefaultValue=false)]
        public decimal? RealTimeAccountCashBalance { get; set; }

        /// <summary>
        /// Indicates the value of real-time account initial margin.
        /// </summary>
        /// <value>Indicates the value of real-time account initial margin.</value>
        [DataMember(Name="RealTimeAccountInitMargin", EmitDefaultValue=false)]
        public decimal? RealTimeAccountInitMargin { get; set; }

        /// <summary>
        /// Indicates the value of real-time account maintance margin.
        /// </summary>
        /// <value>Indicates the value of real-time account maintance margin.</value>
        [DataMember(Name="RealTimeAccountMaintenanceMargin", EmitDefaultValue=false)]
        public decimal? RealTimeAccountMaintenanceMargin { get; set; }

        /// <summary>
        /// Indicates the value of real-time account margin requirement.
        /// </summary>
        /// <value>Indicates the value of real-time account margin requirement.</value>
        [DataMember(Name="RealTimeAccountMarginRequirement", EmitDefaultValue=false)]
        public decimal? RealTimeAccountMarginRequirement { get; set; }

        /// <summary>
        /// Indicates the value of real-time account realized profit or loss.
        /// </summary>
        /// <value>Indicates the value of real-time account realized profit or loss.</value>
        [DataMember(Name="RealTimeAccountRealizedProfitLoss", EmitDefaultValue=false)]
        public decimal? RealTimeAccountRealizedProfitLoss { get; set; }

        /// <summary>
        /// Indicates the value of real-time account unrealized profit or loss.
        /// </summary>
        /// <value>Indicates the value of real-time account unrealized profit or loss.</value>
        [DataMember(Name="RealTimeAccountUnrealizedProfitLoss", EmitDefaultValue=false)]
        public decimal? RealTimeAccountUnrealizedProfitLoss { get; set; }

        /// <summary>
        /// Indicates the value of real-time cash balance.
        /// </summary>
        /// <value>Indicates the value of real-time cash balance.</value>
        [DataMember(Name="RealTimeCashBalance", EmitDefaultValue=false)]
        public decimal? RealTimeCashBalance { get; set; }

        /// <summary>
        /// Indicates the value of real-time initial margin.
        /// </summary>
        /// <value>Indicates the value of real-time initial margin.</value>
        [DataMember(Name="RealTimeInitMargin", EmitDefaultValue=false)]
        public decimal? RealTimeInitMargin { get; set; }

        /// <summary>
        /// Indicates the value of real-time maintance margin.
        /// </summary>
        /// <value>Indicates the value of real-time maintance margin.</value>
        [DataMember(Name="RealTimeMaintenanceMargin", EmitDefaultValue=false)]
        public decimal? RealTimeMaintenanceMargin { get; set; }

        /// <summary>
        /// Indicates the value of real-time realized profit or loss.
        /// </summary>
        /// <value>Indicates the value of real-time realized profit or loss.</value>
        [DataMember(Name="RealTimeRealizedProfitLoss", EmitDefaultValue=false)]
        public decimal? RealTimeRealizedProfitLoss { get; set; }

        /// <summary>
        /// Indicates the value of real-time unrealized profit or loss.
        /// </summary>
        /// <value>Indicates the value of real-time unrealized profit or loss.</value>
        [DataMember(Name="RealTimeUnrealizedProfitLoss", EmitDefaultValue=false)]
        public decimal? RealTimeUnrealizedProfitLoss { get; set; }

        /// <summary>
        /// Indicates the rate used to convert from the currency of the symbol to the currency of the account.
        /// </summary>
        /// <value>Indicates the rate used to convert from the currency of the symbol to the currency of the account.</value>
        [DataMember(Name="ToAccountConversionRate", EmitDefaultValue=false)]
        public decimal? ToAccountConversionRate { get; set; }

        /// <summary>
        /// Indicates the value of today&#39;s real-time unrealized profit or loss.
        /// </summary>
        /// <value>Indicates the value of today&#39;s real-time unrealized profit or loss.</value>
        [DataMember(Name="TodayRealTimeUnrealizedProfitLoss", EmitDefaultValue=false)]
        public decimal? TodayRealTimeUnrealizedProfitLoss { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class AccountBalancesDefinitionInnerCurrencyDetails {\n");
            sb.Append("  AccountOpenOrderInitMargin: ").Append(AccountOpenOrderInitMargin).Append("\n");
            sb.Append("  BODAccountCashBalance: ").Append(BODAccountCashBalance).Append("\n");
            sb.Append("  BODAccountOpenTradeEquity: ").Append(BODAccountOpenTradeEquity).Append("\n");
            sb.Append("  BODAccountSecurities: ").Append(BODAccountSecurities).Append("\n");
            sb.Append("  BODCashBalance: ").Append(BODCashBalance).Append("\n");
            sb.Append("  BODOpenTradeEquity: ").Append(BODOpenTradeEquity).Append("\n");
            sb.Append("  BODSecurities: ").Append(BODSecurities).Append("\n");
            sb.Append("  Commission: ").Append(Commission).Append("\n");
            sb.Append("  Currency: ").Append(Currency).Append("\n");
            sb.Append("  OpenOrderInitMargin: ").Append(OpenOrderInitMargin).Append("\n");
            sb.Append("  RealTimeAccountCashBalance: ").Append(RealTimeAccountCashBalance).Append("\n");
            sb.Append("  RealTimeAccountInitMargin: ").Append(RealTimeAccountInitMargin).Append("\n");
            sb.Append("  RealTimeAccountMaintenanceMargin: ").Append(RealTimeAccountMaintenanceMargin).Append("\n");
            sb.Append("  RealTimeAccountMarginRequirement: ").Append(RealTimeAccountMarginRequirement).Append("\n");
            sb.Append("  RealTimeAccountRealizedProfitLoss: ").Append(RealTimeAccountRealizedProfitLoss).Append("\n");
            sb.Append("  RealTimeAccountUnrealizedProfitLoss: ").Append(RealTimeAccountUnrealizedProfitLoss).Append("\n");
            sb.Append("  RealTimeCashBalance: ").Append(RealTimeCashBalance).Append("\n");
            sb.Append("  RealTimeInitMargin: ").Append(RealTimeInitMargin).Append("\n");
            sb.Append("  RealTimeMaintenanceMargin: ").Append(RealTimeMaintenanceMargin).Append("\n");
            sb.Append("  RealTimeRealizedProfitLoss: ").Append(RealTimeRealizedProfitLoss).Append("\n");
            sb.Append("  RealTimeUnrealizedProfitLoss: ").Append(RealTimeUnrealizedProfitLoss).Append("\n");
            sb.Append("  ToAccountConversionRate: ").Append(ToAccountConversionRate).Append("\n");
            sb.Append("  TodayRealTimeUnrealizedProfitLoss: ").Append(TodayRealTimeUnrealizedProfitLoss).Append("\n");
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
            return this.Equals(input as AccountBalancesDefinitionInnerCurrencyDetails);
        }

        /// <summary>
        /// Returns true if AccountBalancesDefinitionInnerCurrencyDetails instances are equal
        /// </summary>
        /// <param name="input">Instance of AccountBalancesDefinitionInnerCurrencyDetails to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(AccountBalancesDefinitionInnerCurrencyDetails input)
        {
            if (input == null)
                return false;

            return 
                (
                    this.AccountOpenOrderInitMargin == input.AccountOpenOrderInitMargin ||
                    (this.AccountOpenOrderInitMargin != null &&
                    this.AccountOpenOrderInitMargin.Equals(input.AccountOpenOrderInitMargin))
                ) && 
                (
                    this.BODAccountCashBalance == input.BODAccountCashBalance ||
                    (this.BODAccountCashBalance != null &&
                    this.BODAccountCashBalance.Equals(input.BODAccountCashBalance))
                ) && 
                (
                    this.BODAccountOpenTradeEquity == input.BODAccountOpenTradeEquity ||
                    (this.BODAccountOpenTradeEquity != null &&
                    this.BODAccountOpenTradeEquity.Equals(input.BODAccountOpenTradeEquity))
                ) && 
                (
                    this.BODAccountSecurities == input.BODAccountSecurities ||
                    (this.BODAccountSecurities != null &&
                    this.BODAccountSecurities.Equals(input.BODAccountSecurities))
                ) && 
                (
                    this.BODCashBalance == input.BODCashBalance ||
                    (this.BODCashBalance != null &&
                    this.BODCashBalance.Equals(input.BODCashBalance))
                ) && 
                (
                    this.BODOpenTradeEquity == input.BODOpenTradeEquity ||
                    (this.BODOpenTradeEquity != null &&
                    this.BODOpenTradeEquity.Equals(input.BODOpenTradeEquity))
                ) && 
                (
                    this.BODSecurities == input.BODSecurities ||
                    (this.BODSecurities != null &&
                    this.BODSecurities.Equals(input.BODSecurities))
                ) && 
                (
                    this.Commission == input.Commission ||
                    (this.Commission != null &&
                    this.Commission.Equals(input.Commission))
                ) && 
                (
                    this.Currency == input.Currency ||
                    (this.Currency != null &&
                    this.Currency.Equals(input.Currency))
                ) && 
                (
                    this.OpenOrderInitMargin == input.OpenOrderInitMargin ||
                    (this.OpenOrderInitMargin != null &&
                    this.OpenOrderInitMargin.Equals(input.OpenOrderInitMargin))
                ) && 
                (
                    this.RealTimeAccountCashBalance == input.RealTimeAccountCashBalance ||
                    (this.RealTimeAccountCashBalance != null &&
                    this.RealTimeAccountCashBalance.Equals(input.RealTimeAccountCashBalance))
                ) && 
                (
                    this.RealTimeAccountInitMargin == input.RealTimeAccountInitMargin ||
                    (this.RealTimeAccountInitMargin != null &&
                    this.RealTimeAccountInitMargin.Equals(input.RealTimeAccountInitMargin))
                ) && 
                (
                    this.RealTimeAccountMaintenanceMargin == input.RealTimeAccountMaintenanceMargin ||
                    (this.RealTimeAccountMaintenanceMargin != null &&
                    this.RealTimeAccountMaintenanceMargin.Equals(input.RealTimeAccountMaintenanceMargin))
                ) && 
                (
                    this.RealTimeAccountMarginRequirement == input.RealTimeAccountMarginRequirement ||
                    (this.RealTimeAccountMarginRequirement != null &&
                    this.RealTimeAccountMarginRequirement.Equals(input.RealTimeAccountMarginRequirement))
                ) && 
                (
                    this.RealTimeAccountRealizedProfitLoss == input.RealTimeAccountRealizedProfitLoss ||
                    (this.RealTimeAccountRealizedProfitLoss != null &&
                    this.RealTimeAccountRealizedProfitLoss.Equals(input.RealTimeAccountRealizedProfitLoss))
                ) && 
                (
                    this.RealTimeAccountUnrealizedProfitLoss == input.RealTimeAccountUnrealizedProfitLoss ||
                    (this.RealTimeAccountUnrealizedProfitLoss != null &&
                    this.RealTimeAccountUnrealizedProfitLoss.Equals(input.RealTimeAccountUnrealizedProfitLoss))
                ) && 
                (
                    this.RealTimeCashBalance == input.RealTimeCashBalance ||
                    (this.RealTimeCashBalance != null &&
                    this.RealTimeCashBalance.Equals(input.RealTimeCashBalance))
                ) && 
                (
                    this.RealTimeInitMargin == input.RealTimeInitMargin ||
                    (this.RealTimeInitMargin != null &&
                    this.RealTimeInitMargin.Equals(input.RealTimeInitMargin))
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
                    this.RealTimeUnrealizedProfitLoss == input.RealTimeUnrealizedProfitLoss ||
                    (this.RealTimeUnrealizedProfitLoss != null &&
                    this.RealTimeUnrealizedProfitLoss.Equals(input.RealTimeUnrealizedProfitLoss))
                ) && 
                (
                    this.ToAccountConversionRate == input.ToAccountConversionRate ||
                    (this.ToAccountConversionRate != null &&
                    this.ToAccountConversionRate.Equals(input.ToAccountConversionRate))
                ) && 
                (
                    this.TodayRealTimeUnrealizedProfitLoss == input.TodayRealTimeUnrealizedProfitLoss ||
                    (this.TodayRealTimeUnrealizedProfitLoss != null &&
                    this.TodayRealTimeUnrealizedProfitLoss.Equals(input.TodayRealTimeUnrealizedProfitLoss))
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
                if (this.AccountOpenOrderInitMargin != null)
                    hashCode = hashCode * 59 + this.AccountOpenOrderInitMargin.GetHashCode();
                if (this.BODAccountCashBalance != null)
                    hashCode = hashCode * 59 + this.BODAccountCashBalance.GetHashCode();
                if (this.BODAccountOpenTradeEquity != null)
                    hashCode = hashCode * 59 + this.BODAccountOpenTradeEquity.GetHashCode();
                if (this.BODAccountSecurities != null)
                    hashCode = hashCode * 59 + this.BODAccountSecurities.GetHashCode();
                if (this.BODCashBalance != null)
                    hashCode = hashCode * 59 + this.BODCashBalance.GetHashCode();
                if (this.BODOpenTradeEquity != null)
                    hashCode = hashCode * 59 + this.BODOpenTradeEquity.GetHashCode();
                if (this.BODSecurities != null)
                    hashCode = hashCode * 59 + this.BODSecurities.GetHashCode();
                if (this.Commission != null)
                    hashCode = hashCode * 59 + this.Commission.GetHashCode();
                if (this.Currency != null)
                    hashCode = hashCode * 59 + this.Currency.GetHashCode();
                if (this.OpenOrderInitMargin != null)
                    hashCode = hashCode * 59 + this.OpenOrderInitMargin.GetHashCode();
                if (this.RealTimeAccountCashBalance != null)
                    hashCode = hashCode * 59 + this.RealTimeAccountCashBalance.GetHashCode();
                if (this.RealTimeAccountInitMargin != null)
                    hashCode = hashCode * 59 + this.RealTimeAccountInitMargin.GetHashCode();
                if (this.RealTimeAccountMaintenanceMargin != null)
                    hashCode = hashCode * 59 + this.RealTimeAccountMaintenanceMargin.GetHashCode();
                if (this.RealTimeAccountMarginRequirement != null)
                    hashCode = hashCode * 59 + this.RealTimeAccountMarginRequirement.GetHashCode();
                if (this.RealTimeAccountRealizedProfitLoss != null)
                    hashCode = hashCode * 59 + this.RealTimeAccountRealizedProfitLoss.GetHashCode();
                if (this.RealTimeAccountUnrealizedProfitLoss != null)
                    hashCode = hashCode * 59 + this.RealTimeAccountUnrealizedProfitLoss.GetHashCode();
                if (this.RealTimeCashBalance != null)
                    hashCode = hashCode * 59 + this.RealTimeCashBalance.GetHashCode();
                if (this.RealTimeInitMargin != null)
                    hashCode = hashCode * 59 + this.RealTimeInitMargin.GetHashCode();
                if (this.RealTimeMaintenanceMargin != null)
                    hashCode = hashCode * 59 + this.RealTimeMaintenanceMargin.GetHashCode();
                if (this.RealTimeRealizedProfitLoss != null)
                    hashCode = hashCode * 59 + this.RealTimeRealizedProfitLoss.GetHashCode();
                if (this.RealTimeUnrealizedProfitLoss != null)
                    hashCode = hashCode * 59 + this.RealTimeUnrealizedProfitLoss.GetHashCode();
                if (this.ToAccountConversionRate != null)
                    hashCode = hashCode * 59 + this.ToAccountConversionRate.GetHashCode();
                if (this.TodayRealTimeUnrealizedProfitLoss != null)
                    hashCode = hashCode * 59 + this.TodayRealTimeUnrealizedProfitLoss.GetHashCode();
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

            yield break;
        }
    }

}
