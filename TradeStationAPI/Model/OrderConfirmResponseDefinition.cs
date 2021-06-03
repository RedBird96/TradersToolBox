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
    /// Order Confirm definition. The response will also contain asset-specific fields
    /// </summary>
    [DataContract]
    public partial class OrderConfirmResponseDefinition :  IEquatable<OrderConfirmResponseDefinition>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OrderConfirmResponseDefinition" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected OrderConfirmResponseDefinition() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="OrderConfirmResponseDefinition" /> class.
        /// </summary>
        /// <param name="route">The path chosen for directing a trade to a certain destination, such as an ECN, MM, Exchange, or Intelligent. (required).</param>
        /// <param name="duration">The amount of time for which an order is valid. Duration is the same as TIF (Time in Force). (required).</param>
        /// <param name="account">The number that identifies a specific TradeStation account that is being used for a particular order. (required).</param>
        /// <param name="orderConfirmId">Unique id generated per order per API Key and User (required).</param>
        /// <param name="estimatedPrice">An estimated value that is calculated using current market information. The actual cost for Market orders and orders with conditions, such as Trailing Stop or Activation Rule orders, may differ significantly from this estimate. (required).</param>
        /// <param name="estimatedPriceDisplay">Equity and Futures Orders; Estimated price formatted for display.</param>
        /// <param name="estimatedCost">The actual cost for Market orders and orders with conditions, such as Trailing Stop or Activation Rule orders..</param>
        /// <param name="estimatedCostDisplay">Equity Orders; Estimated cost formatted for display.</param>
        /// <param name="debitCreditEstimatedCost">The actual cost for Market orders and orders with conditions, such as Trailing Stop or Activation Rule orders. Takes into account wheather or not the transaction will result in a debit or credit to the user..</param>
        /// <param name="debitCreditEstimatedCostDisplay">Equity Orders; Debit credit estimated cost formatted for display.</param>
        /// <param name="estimatedCommission">An estimated value that is calculated using the published TradeStation commission schedule. Equity and Futures Orders.</param>
        /// <param name="estimatedCommissionDisplay">Equity and Futures Orders; Estimated commission formatted for display.</param>
        /// <param name="baseCurrency">Forex Orders; .</param>
        /// <param name="counterCurrency">Forex Orders; Estimated cost formatted for display.</param>
        /// <param name="initialMarginDisplay">Forex and Futures Orders; Initial margin cost formatted for display in currency of asset.</param>
        /// <param name="productCurrency">Futures Orders; .</param>
        /// <param name="accountCurrency">Futures Orders; .</param>
        /// <param name="trailingStop">trailingStop.</param>
        /// <param name="marketActivationRules">marketActivationRules.</param>
        /// <param name="timeActivationRules">timeActivationRules.</param>
        /// <param name="showOnlyQuantity">Equity and Futures Orders; Number of shares to submit to market at a time for this order.</param>
        public OrderConfirmResponseDefinition(string route = default(string), string duration = default(string), string account = default(string), string orderConfirmId = default(string), decimal? estimatedPrice = default(decimal?), string estimatedPriceDisplay = default(string), decimal? estimatedCost = default(decimal?), string estimatedCostDisplay = default(string), decimal? debitCreditEstimatedCost = default(decimal?), string debitCreditEstimatedCostDisplay = default(string), decimal? estimatedCommission = default(decimal?), string estimatedCommissionDisplay = default(string), string baseCurrency = default(string), string counterCurrency = default(string), string initialMarginDisplay = default(string), string productCurrency = default(string), string accountCurrency = default(string), TrailingStopDefinition trailingStop = default(TrailingStopDefinition), List<MarketActivationRuleDefinition> marketActivationRules = default(List<MarketActivationRuleDefinition>), List<TimeActivationRuleDefinition> timeActivationRules = default(List<TimeActivationRuleDefinition>), int? showOnlyQuantity = default(int?))
        {
            // to ensure "route" is required (not null)
            if (route == null)
            {
                throw new InvalidDataException("route is a required property for OrderConfirmResponseDefinition and cannot be null");
            }
            else
            {
                this.Route = route;
            }
            // to ensure "duration" is required (not null)
            if (duration == null)
            {
                throw new InvalidDataException("duration is a required property for OrderConfirmResponseDefinition and cannot be null");
            }
            else
            {
                this.Duration = duration;
            }
            // to ensure "account" is required (not null)
            if (account == null)
            {
                throw new InvalidDataException("account is a required property for OrderConfirmResponseDefinition and cannot be null");
            }
            else
            {
                this.Account = account;
            }
            // to ensure "orderConfirmId" is required (not null)
            if (orderConfirmId == null)
            {
                throw new InvalidDataException("orderConfirmId is a required property for OrderConfirmResponseDefinition and cannot be null");
            }
            else
            {
                this.OrderConfirmId = orderConfirmId;
            }
            // to ensure "estimatedPrice" is required (not null)
            if (estimatedPrice == null)
            {
                throw new InvalidDataException("estimatedPrice is a required property for OrderConfirmResponseDefinition and cannot be null");
            }
            else
            {
                this.EstimatedPrice = estimatedPrice;
            }
            this.EstimatedPriceDisplay = estimatedPriceDisplay;
            this.EstimatedCost = estimatedCost;
            this.EstimatedCostDisplay = estimatedCostDisplay;
            this.DebitCreditEstimatedCost = debitCreditEstimatedCost;
            this.DebitCreditEstimatedCostDisplay = debitCreditEstimatedCostDisplay;
            this.EstimatedCommission = estimatedCommission;
            this.EstimatedCommissionDisplay = estimatedCommissionDisplay;
            this.BaseCurrency = baseCurrency;
            this.CounterCurrency = counterCurrency;
            this.InitialMarginDisplay = initialMarginDisplay;
            this.ProductCurrency = productCurrency;
            this.AccountCurrency = accountCurrency;
            this.TrailingStop = trailingStop;
            this.MarketActivationRules = marketActivationRules;
            this.TimeActivationRules = timeActivationRules;
            this.ShowOnlyQuantity = showOnlyQuantity;
        }
        
        /// <summary>
        /// The path chosen for directing a trade to a certain destination, such as an ECN, MM, Exchange, or Intelligent.
        /// </summary>
        /// <value>The path chosen for directing a trade to a certain destination, such as an ECN, MM, Exchange, or Intelligent.</value>
        [DataMember(Name="Route", EmitDefaultValue=false)]
        public string Route { get; set; }

        /// <summary>
        /// The amount of time for which an order is valid. Duration is the same as TIF (Time in Force).
        /// </summary>
        /// <value>The amount of time for which an order is valid. Duration is the same as TIF (Time in Force).</value>
        [DataMember(Name="Duration", EmitDefaultValue=false)]
        public string Duration { get; set; }

        /// <summary>
        /// The number that identifies a specific TradeStation account that is being used for a particular order.
        /// </summary>
        /// <value>The number that identifies a specific TradeStation account that is being used for a particular order.</value>
        [DataMember(Name="Account", EmitDefaultValue=false)]
        public string Account { get; set; }

        /// <summary>
        /// Unique id generated per order per API Key and User
        /// </summary>
        /// <value>Unique id generated per order per API Key and User</value>
        [DataMember(Name="OrderConfirmId", EmitDefaultValue=false)]
        public string OrderConfirmId { get; set; }

        /// <summary>
        /// An estimated value that is calculated using current market information. The actual cost for Market orders and orders with conditions, such as Trailing Stop or Activation Rule orders, may differ significantly from this estimate.
        /// </summary>
        /// <value>An estimated value that is calculated using current market information. The actual cost for Market orders and orders with conditions, such as Trailing Stop or Activation Rule orders, may differ significantly from this estimate.</value>
        [DataMember(Name="EstimatedPrice", EmitDefaultValue=false)]
        public decimal? EstimatedPrice { get; set; }

        /// <summary>
        /// Equity and Futures Orders; Estimated price formatted for display
        /// </summary>
        /// <value>Equity and Futures Orders; Estimated price formatted for display</value>
        [DataMember(Name="EstimatedPriceDisplay", EmitDefaultValue=false)]
        public string EstimatedPriceDisplay { get; set; }

        /// <summary>
        /// The actual cost for Market orders and orders with conditions, such as Trailing Stop or Activation Rule orders.
        /// </summary>
        /// <value>The actual cost for Market orders and orders with conditions, such as Trailing Stop or Activation Rule orders.</value>
        [DataMember(Name="EstimatedCost", EmitDefaultValue=false)]
        public decimal? EstimatedCost { get; set; }

        /// <summary>
        /// Equity Orders; Estimated cost formatted for display
        /// </summary>
        /// <value>Equity Orders; Estimated cost formatted for display</value>
        [DataMember(Name="EstimatedCostDisplay", EmitDefaultValue=false)]
        public string EstimatedCostDisplay { get; set; }

        /// <summary>
        /// The actual cost for Market orders and orders with conditions, such as Trailing Stop or Activation Rule orders. Takes into account wheather or not the transaction will result in a debit or credit to the user.
        /// </summary>
        /// <value>The actual cost for Market orders and orders with conditions, such as Trailing Stop or Activation Rule orders. Takes into account wheather or not the transaction will result in a debit or credit to the user.</value>
        [DataMember(Name="DebitCreditEstimatedCost", EmitDefaultValue=false)]
        public decimal? DebitCreditEstimatedCost { get; set; }

        /// <summary>
        /// Equity Orders; Debit credit estimated cost formatted for display
        /// </summary>
        /// <value>Equity Orders; Debit credit estimated cost formatted for display</value>
        [DataMember(Name="DebitCreditEstimatedCostDisplay", EmitDefaultValue=false)]
        public string DebitCreditEstimatedCostDisplay { get; set; }

        /// <summary>
        /// An estimated value that is calculated using the published TradeStation commission schedule. Equity and Futures Orders
        /// </summary>
        /// <value>An estimated value that is calculated using the published TradeStation commission schedule. Equity and Futures Orders</value>
        [DataMember(Name="EstimatedCommission", EmitDefaultValue=false)]
        public decimal? EstimatedCommission { get; set; }

        /// <summary>
        /// Equity and Futures Orders; Estimated commission formatted for display
        /// </summary>
        /// <value>Equity and Futures Orders; Estimated commission formatted for display</value>
        [DataMember(Name="EstimatedCommissionDisplay", EmitDefaultValue=false)]
        public string EstimatedCommissionDisplay { get; set; }

        /// <summary>
        /// Forex Orders; 
        /// </summary>
        /// <value>Forex Orders; </value>
        [DataMember(Name="BaseCurrency", EmitDefaultValue=false)]
        public string BaseCurrency { get; set; }

        /// <summary>
        /// Forex Orders; Estimated cost formatted for display
        /// </summary>
        /// <value>Forex Orders; Estimated cost formatted for display</value>
        [DataMember(Name="CounterCurrency", EmitDefaultValue=false)]
        public string CounterCurrency { get; set; }

        /// <summary>
        /// Forex and Futures Orders; Initial margin cost formatted for display in currency of asset
        /// </summary>
        /// <value>Forex and Futures Orders; Initial margin cost formatted for display in currency of asset</value>
        [DataMember(Name="InitialMarginDisplay", EmitDefaultValue=false)]
        public string InitialMarginDisplay { get; set; }

        /// <summary>
        /// Futures Orders; 
        /// </summary>
        /// <value>Futures Orders; </value>
        [DataMember(Name="ProductCurrency", EmitDefaultValue=false)]
        public string ProductCurrency { get; set; }

        /// <summary>
        /// Futures Orders; 
        /// </summary>
        /// <value>Futures Orders; </value>
        [DataMember(Name="AccountCurrency", EmitDefaultValue=false)]
        public string AccountCurrency { get; set; }

        /// <summary>
        /// Gets or Sets TrailingStop
        /// </summary>
        [DataMember(Name="TrailingStop", EmitDefaultValue=false)]
        public TrailingStopDefinition TrailingStop { get; set; }

        /// <summary>
        /// Gets or Sets MarketActivationRules
        /// </summary>
        [DataMember(Name="MarketActivationRules", EmitDefaultValue=false)]
        public List<MarketActivationRuleDefinition> MarketActivationRules { get; set; }

        /// <summary>
        /// Gets or Sets TimeActivationRules
        /// </summary>
        [DataMember(Name="TimeActivationRules", EmitDefaultValue=false)]
        public List<TimeActivationRuleDefinition> TimeActivationRules { get; set; }

        /// <summary>
        /// Equity and Futures Orders; Number of shares to submit to market at a time for this order
        /// </summary>
        /// <value>Equity and Futures Orders; Number of shares to submit to market at a time for this order</value>
        [DataMember(Name="ShowOnlyQuantity", EmitDefaultValue=false)]
        public int? ShowOnlyQuantity { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class OrderConfirmResponseDefinition {\n");
            sb.Append("  Route: ").Append(Route).Append("\n");
            sb.Append("  Duration: ").Append(Duration).Append("\n");
            sb.Append("  Account: ").Append(Account).Append("\n");
            sb.Append("  OrderConfirmId: ").Append(OrderConfirmId).Append("\n");
            sb.Append("  EstimatedPrice: ").Append(EstimatedPrice).Append("\n");
            sb.Append("  EstimatedPriceDisplay: ").Append(EstimatedPriceDisplay).Append("\n");
            sb.Append("  EstimatedCost: ").Append(EstimatedCost).Append("\n");
            sb.Append("  EstimatedCostDisplay: ").Append(EstimatedCostDisplay).Append("\n");
            sb.Append("  DebitCreditEstimatedCost: ").Append(DebitCreditEstimatedCost).Append("\n");
            sb.Append("  DebitCreditEstimatedCostDisplay: ").Append(DebitCreditEstimatedCostDisplay).Append("\n");
            sb.Append("  EstimatedCommission: ").Append(EstimatedCommission).Append("\n");
            sb.Append("  EstimatedCommissionDisplay: ").Append(EstimatedCommissionDisplay).Append("\n");
            sb.Append("  BaseCurrency: ").Append(BaseCurrency).Append("\n");
            sb.Append("  CounterCurrency: ").Append(CounterCurrency).Append("\n");
            sb.Append("  InitialMarginDisplay: ").Append(InitialMarginDisplay).Append("\n");
            sb.Append("  ProductCurrency: ").Append(ProductCurrency).Append("\n");
            sb.Append("  AccountCurrency: ").Append(AccountCurrency).Append("\n");
            sb.Append("  TrailingStop: ").Append(TrailingStop).Append("\n");
            sb.Append("  MarketActivationRules: ").Append(MarketActivationRules).Append("\n");
            sb.Append("  TimeActivationRules: ").Append(TimeActivationRules).Append("\n");
            sb.Append("  ShowOnlyQuantity: ").Append(ShowOnlyQuantity).Append("\n");
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
            return this.Equals(input as OrderConfirmResponseDefinition);
        }

        /// <summary>
        /// Returns true if OrderConfirmResponseDefinition instances are equal
        /// </summary>
        /// <param name="input">Instance of OrderConfirmResponseDefinition to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(OrderConfirmResponseDefinition input)
        {
            if (input == null)
                return false;

            return 
                (
                    this.Route == input.Route ||
                    (this.Route != null &&
                    this.Route.Equals(input.Route))
                ) && 
                (
                    this.Duration == input.Duration ||
                    (this.Duration != null &&
                    this.Duration.Equals(input.Duration))
                ) && 
                (
                    this.Account == input.Account ||
                    (this.Account != null &&
                    this.Account.Equals(input.Account))
                ) && 
                (
                    this.OrderConfirmId == input.OrderConfirmId ||
                    (this.OrderConfirmId != null &&
                    this.OrderConfirmId.Equals(input.OrderConfirmId))
                ) && 
                (
                    this.EstimatedPrice == input.EstimatedPrice ||
                    (this.EstimatedPrice != null &&
                    this.EstimatedPrice.Equals(input.EstimatedPrice))
                ) && 
                (
                    this.EstimatedPriceDisplay == input.EstimatedPriceDisplay ||
                    (this.EstimatedPriceDisplay != null &&
                    this.EstimatedPriceDisplay.Equals(input.EstimatedPriceDisplay))
                ) && 
                (
                    this.EstimatedCost == input.EstimatedCost ||
                    (this.EstimatedCost != null &&
                    this.EstimatedCost.Equals(input.EstimatedCost))
                ) && 
                (
                    this.EstimatedCostDisplay == input.EstimatedCostDisplay ||
                    (this.EstimatedCostDisplay != null &&
                    this.EstimatedCostDisplay.Equals(input.EstimatedCostDisplay))
                ) && 
                (
                    this.DebitCreditEstimatedCost == input.DebitCreditEstimatedCost ||
                    (this.DebitCreditEstimatedCost != null &&
                    this.DebitCreditEstimatedCost.Equals(input.DebitCreditEstimatedCost))
                ) && 
                (
                    this.DebitCreditEstimatedCostDisplay == input.DebitCreditEstimatedCostDisplay ||
                    (this.DebitCreditEstimatedCostDisplay != null &&
                    this.DebitCreditEstimatedCostDisplay.Equals(input.DebitCreditEstimatedCostDisplay))
                ) && 
                (
                    this.EstimatedCommission == input.EstimatedCommission ||
                    (this.EstimatedCommission != null &&
                    this.EstimatedCommission.Equals(input.EstimatedCommission))
                ) && 
                (
                    this.EstimatedCommissionDisplay == input.EstimatedCommissionDisplay ||
                    (this.EstimatedCommissionDisplay != null &&
                    this.EstimatedCommissionDisplay.Equals(input.EstimatedCommissionDisplay))
                ) && 
                (
                    this.BaseCurrency == input.BaseCurrency ||
                    (this.BaseCurrency != null &&
                    this.BaseCurrency.Equals(input.BaseCurrency))
                ) && 
                (
                    this.CounterCurrency == input.CounterCurrency ||
                    (this.CounterCurrency != null &&
                    this.CounterCurrency.Equals(input.CounterCurrency))
                ) && 
                (
                    this.InitialMarginDisplay == input.InitialMarginDisplay ||
                    (this.InitialMarginDisplay != null &&
                    this.InitialMarginDisplay.Equals(input.InitialMarginDisplay))
                ) && 
                (
                    this.ProductCurrency == input.ProductCurrency ||
                    (this.ProductCurrency != null &&
                    this.ProductCurrency.Equals(input.ProductCurrency))
                ) && 
                (
                    this.AccountCurrency == input.AccountCurrency ||
                    (this.AccountCurrency != null &&
                    this.AccountCurrency.Equals(input.AccountCurrency))
                ) && 
                (
                    this.TrailingStop == input.TrailingStop ||
                    (this.TrailingStop != null &&
                    this.TrailingStop.Equals(input.TrailingStop))
                ) && 
                (
                    this.MarketActivationRules == input.MarketActivationRules ||
                    this.MarketActivationRules != null &&
                    this.MarketActivationRules.SequenceEqual(input.MarketActivationRules)
                ) && 
                (
                    this.TimeActivationRules == input.TimeActivationRules ||
                    this.TimeActivationRules != null &&
                    this.TimeActivationRules.SequenceEqual(input.TimeActivationRules)
                ) && 
                (
                    this.ShowOnlyQuantity == input.ShowOnlyQuantity ||
                    (this.ShowOnlyQuantity != null &&
                    this.ShowOnlyQuantity.Equals(input.ShowOnlyQuantity))
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
                if (this.Route != null)
                    hashCode = hashCode * 59 + this.Route.GetHashCode();
                if (this.Duration != null)
                    hashCode = hashCode * 59 + this.Duration.GetHashCode();
                if (this.Account != null)
                    hashCode = hashCode * 59 + this.Account.GetHashCode();
                if (this.OrderConfirmId != null)
                    hashCode = hashCode * 59 + this.OrderConfirmId.GetHashCode();
                if (this.EstimatedPrice != null)
                    hashCode = hashCode * 59 + this.EstimatedPrice.GetHashCode();
                if (this.EstimatedPriceDisplay != null)
                    hashCode = hashCode * 59 + this.EstimatedPriceDisplay.GetHashCode();
                if (this.EstimatedCost != null)
                    hashCode = hashCode * 59 + this.EstimatedCost.GetHashCode();
                if (this.EstimatedCostDisplay != null)
                    hashCode = hashCode * 59 + this.EstimatedCostDisplay.GetHashCode();
                if (this.DebitCreditEstimatedCost != null)
                    hashCode = hashCode * 59 + this.DebitCreditEstimatedCost.GetHashCode();
                if (this.DebitCreditEstimatedCostDisplay != null)
                    hashCode = hashCode * 59 + this.DebitCreditEstimatedCostDisplay.GetHashCode();
                if (this.EstimatedCommission != null)
                    hashCode = hashCode * 59 + this.EstimatedCommission.GetHashCode();
                if (this.EstimatedCommissionDisplay != null)
                    hashCode = hashCode * 59 + this.EstimatedCommissionDisplay.GetHashCode();
                if (this.BaseCurrency != null)
                    hashCode = hashCode * 59 + this.BaseCurrency.GetHashCode();
                if (this.CounterCurrency != null)
                    hashCode = hashCode * 59 + this.CounterCurrency.GetHashCode();
                if (this.InitialMarginDisplay != null)
                    hashCode = hashCode * 59 + this.InitialMarginDisplay.GetHashCode();
                if (this.ProductCurrency != null)
                    hashCode = hashCode * 59 + this.ProductCurrency.GetHashCode();
                if (this.AccountCurrency != null)
                    hashCode = hashCode * 59 + this.AccountCurrency.GetHashCode();
                if (this.TrailingStop != null)
                    hashCode = hashCode * 59 + this.TrailingStop.GetHashCode();
                if (this.MarketActivationRules != null)
                    hashCode = hashCode * 59 + this.MarketActivationRules.GetHashCode();
                if (this.TimeActivationRules != null)
                    hashCode = hashCode * 59 + this.TimeActivationRules.GetHashCode();
                if (this.ShowOnlyQuantity != null)
                    hashCode = hashCode * 59 + this.ShowOnlyQuantity.GetHashCode();
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
            yield break;
        }
    }

}
