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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using RestSharp;
using TradeStationAPI.Client;
using TradeStationAPI.Model;

namespace TradeStationAPI.Api
{
    /// <summary>
    /// Represents a collection of functions to interact with the API endpoints
    /// </summary>
    public interface IOrderExecutionApi : IApiAccessor
    {
        #region Synchronous Operations
        /// <summary>
        /// Cancel Order 
        /// </summary>
        /// <remarks>
        /// Cancels an open order. You cannot cancel an order that has been filled. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="orderId">An existing Order ID</param>
        /// <returns>OrderResponseDefinition</returns>
        OrderResponseDefinition CancelOrder (string accessToken, string orderId);

        /// <summary>
        /// Cancel Order 
        /// </summary>
        /// <remarks>
        /// Cancels an open order. You cannot cancel an order that has been filled. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="orderId">An existing Order ID</param>
        /// <returns>ApiResponse of OrderResponseDefinition</returns>
        ApiResponse<OrderResponseDefinition> CancelOrderWithHttpInfo (string accessToken, string orderId);
        /// <summary>
        /// Update Order 
        /// </summary>
        /// <remarks>
        /// Updates (Cancels &amp; Replaces) an open order. You cannot update an order that has been filled.  Rules:   | Original order type              | Fields to update                 | Can only change order type to |  | - -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- - | - -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- - | - -- -- -- -- -- -- -- -- -- -- -- -- -- -- |  | Limit Orders                     | Quantity, Stop Price             | Market                        |  | Stop Orders                      | Quantity, Stop Price             | Market                        |  | Stop Limit Orders                | Quantity, Stop Price, Stop Limit | Market                        |  | Stop Market Trailing Stop Orders | Quantity                         | Market                        |  | Trailing Stop Orders             | Offset type and value            | Market                        | 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="orderId">An existing Order ID</param>
        /// <param name="body"></param>
        /// <returns>OrderResponseDefinition</returns>
        OrderResponseDefinition CancelReplaceOrder (string accessToken, string orderId, CancelReplaceDefinition body);

        /// <summary>
        /// Update Order 
        /// </summary>
        /// <remarks>
        /// Updates (Cancels &amp; Replaces) an open order. You cannot update an order that has been filled.  Rules:   | Original order type              | Fields to update                 | Can only change order type to |  | - -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- - | - -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- - | - -- -- -- -- -- -- -- -- -- -- -- -- -- -- |  | Limit Orders                     | Quantity, Stop Price             | Market                        |  | Stop Orders                      | Quantity, Stop Price             | Market                        |  | Stop Limit Orders                | Quantity, Stop Price, Stop Limit | Market                        |  | Stop Market Trailing Stop Orders | Quantity                         | Market                        |  | Trailing Stop Orders             | Offset type and value            | Market                        | 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="orderId">An existing Order ID</param>
        /// <param name="body"></param>
        /// <returns>ApiResponse of OrderResponseDefinition</returns>
        ApiResponse<OrderResponseDefinition> CancelReplaceOrderWithHttpInfo (string accessToken, string orderId, CancelReplaceDefinition body);
        /// <summary>
        ///  Request Available Activation Triggers 
        /// </summary>
        /// <remarks>
        /// To place orders with activation triggers, a valid TriggerKey must be sent with the order.  This resource provides the available trigger methods with their corresponding key. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <returns>InlineResponse200</returns>
        InlineResponse200 GetActivationTriggers (string accessToken);

        /// <summary>
        ///  Request Available Activation Triggers 
        /// </summary>
        /// <remarks>
        /// To place orders with activation triggers, a valid TriggerKey must be sent with the order.  This resource provides the available trigger methods with their corresponding key. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <returns>ApiResponse of InlineResponse200</returns>
        ApiResponse<InlineResponse200> GetActivationTriggersWithHttpInfo (string accessToken);
        /// <summary>
        /// Request Available Exchanges 
        /// </summary>
        /// <remarks>
        /// Returns a list of valid exchanges that a client can specify when posting an order. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <returns>InlineResponse2001</returns>
        InlineResponse2001 GetExchanges (string accessToken);

        /// <summary>
        /// Request Available Exchanges 
        /// </summary>
        /// <remarks>
        /// Returns a list of valid exchanges that a client can specify when posting an order. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <returns>ApiResponse of InlineResponse2001</returns>
        ApiResponse<InlineResponse2001> GetExchangesWithHttpInfo (string accessToken);
        /// <summary>
        /// Submit Order 
        /// </summary>
        /// <remarks>
        /// Submits 1 or more orders 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="body"></param>
        /// <returns>OrderResponseDefinition</returns>
        OrderResponseDefinition PostOrder (string accessToken, OrderRequestDefinition body);

        /// <summary>
        /// Submit Order 
        /// </summary>
        /// <remarks>
        /// Submits 1 or more orders 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="body"></param>
        /// <returns>ApiResponse of OrderResponseDefinition</returns>
        ApiResponse<OrderResponseDefinition> PostOrderWithHttpInfo (string accessToken, OrderRequestDefinition body);
        /// <summary>
        /// Confirm Order 
        /// </summary>
        /// <remarks>
        /// Returns estimated cost and commission information for an order without the order actually being placed. The fields that are returned in the response depend on the order type. The following shows the different fields that will be returned.  **Base Confirmation**  (All confirmations will have these fields) * Route * Duration * Account * SummaryMessage * OrderConfirmId  **Equity Confirmation** (Base Confirmation fields + the following) * EstimatedPrice * EstimatedPriceDisplay * EstimatedCost * EstimatedCostDisplay * EstimatedCommission * EstimatedCommissionDisplay * DebitCreditEstimatedCost * DebitCreditEstimatedCostDisplay  **Forex Confirmation** (Base Confirmation fields + the following) * BaseCurrency * CounterCurrency * InitialMarginDisplay  **Futures Confirmation** (Base Confirmation fields + the following) * ProductCurrency * AccountCurrency * EstimatedCost * EstimatedPrice * EstimatedPriceDisplay * InitialMarginDisplay * EstimatedCommission * EstimatedCommissionDisplay 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="body"></param>
        /// <returns>OrderConfirmResponseDefinition</returns>
        OrderConfirmResponseDefinition PostOrderConfirm (string accessToken, OrderConfirmRequestDefinition body);

        /// <summary>
        /// Confirm Order 
        /// </summary>
        /// <remarks>
        /// Returns estimated cost and commission information for an order without the order actually being placed. The fields that are returned in the response depend on the order type. The following shows the different fields that will be returned.  **Base Confirmation**  (All confirmations will have these fields) * Route * Duration * Account * SummaryMessage * OrderConfirmId  **Equity Confirmation** (Base Confirmation fields + the following) * EstimatedPrice * EstimatedPriceDisplay * EstimatedCost * EstimatedCostDisplay * EstimatedCommission * EstimatedCommissionDisplay * DebitCreditEstimatedCost * DebitCreditEstimatedCostDisplay  **Forex Confirmation** (Base Confirmation fields + the following) * BaseCurrency * CounterCurrency * InitialMarginDisplay  **Futures Confirmation** (Base Confirmation fields + the following) * ProductCurrency * AccountCurrency * EstimatedCost * EstimatedPrice * EstimatedPriceDisplay * InitialMarginDisplay * EstimatedCommission * EstimatedCommissionDisplay 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="body"></param>
        /// <returns>ApiResponse of OrderConfirmResponseDefinition</returns>
        ApiResponse<OrderConfirmResponseDefinition> PostOrderConfirmWithHttpInfo (string accessToken, OrderConfirmRequestDefinition body);
        /// <summary>
        /// Submit Group Order 
        /// </summary>
        /// <remarks>
        /// Submits a group order such as Bracket and OCO Orders.  #### Order Cancels Order (OCO) An OCO order is a group of orders whereby if one of the orders is filled or partially-filled, then all of the other orders in the group are cancelled.  #### Bracket OCO Orders A bracket order is a special instance of an OCO (Order Cancel Order). Bracket orders are used to exit an existing position. They are designed to limit loss and lock in profit by “bracketing” an order with a simultaneous stop and limit order.  Bracket orders are limited so that the orders are all for the same symbol and are on the same side of the market (either all to sell or all to cover), and they are restricted to closing transactions.  The reason that they follow these rules is because the orders need to be able to auto decrement when a partial fill occurs with one of the orders. For example, if the customer has a sell limit order for 1000 shares and a sell stop order for 1000 shares, and the limit order is partially filled for 500 shares, then the customer would want the stop to remain open, but it should automatically decrement the order to 500 shares to match the remaining open position.  ### Errors When a submitted order fails, it can be seen in two different ways: - Immediately rejected during submit to the Orders API with a 400 HTTP status code. - Accepted by the API with a 200 HTTP status code, but you will see the order with a status of “REJ” (rejected) when you fetch the Order from the /v2/accounts/{id}/orders API  #### NOTE When a group order is submitted, the order execution system treats each sibling order as an individual order. Thus, the system does not validate that each order has the same Quantity, and currently it is not able to update a bracket order as one transaction, instead you must update each order within a bracket.  In order to prevent errors, please validate the data on the client side. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="body"></param>
        /// <returns>GroupOrderResponseDefinition</returns>
        GroupOrderResponseDefinition PostOrderGroup (string accessToken, GroupOrderRequestDefinition body);

        /// <summary>
        /// Submit Group Order 
        /// </summary>
        /// <remarks>
        /// Submits a group order such as Bracket and OCO Orders.  #### Order Cancels Order (OCO) An OCO order is a group of orders whereby if one of the orders is filled or partially-filled, then all of the other orders in the group are cancelled.  #### Bracket OCO Orders A bracket order is a special instance of an OCO (Order Cancel Order). Bracket orders are used to exit an existing position. They are designed to limit loss and lock in profit by “bracketing” an order with a simultaneous stop and limit order.  Bracket orders are limited so that the orders are all for the same symbol and are on the same side of the market (either all to sell or all to cover), and they are restricted to closing transactions.  The reason that they follow these rules is because the orders need to be able to auto decrement when a partial fill occurs with one of the orders. For example, if the customer has a sell limit order for 1000 shares and a sell stop order for 1000 shares, and the limit order is partially filled for 500 shares, then the customer would want the stop to remain open, but it should automatically decrement the order to 500 shares to match the remaining open position.  ### Errors When a submitted order fails, it can be seen in two different ways: - Immediately rejected during submit to the Orders API with a 400 HTTP status code. - Accepted by the API with a 200 HTTP status code, but you will see the order with a status of “REJ” (rejected) when you fetch the Order from the /v2/accounts/{id}/orders API  #### NOTE When a group order is submitted, the order execution system treats each sibling order as an individual order. Thus, the system does not validate that each order has the same Quantity, and currently it is not able to update a bracket order as one transaction, instead you must update each order within a bracket.  In order to prevent errors, please validate the data on the client side. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="body"></param>
        /// <returns>ApiResponse of GroupOrderResponseDefinition</returns>
        ApiResponse<GroupOrderResponseDefinition> PostOrderGroupWithHttpInfo (string accessToken, GroupOrderRequestDefinition body);
        /// <summary>
        /// Confirm Group Order 
        /// </summary>
        /// <remarks>
        /// Returns estimated cost and commission information for a group of orders (OCO, BRK) without the orders actually being placed  **Base Confirmation**  (All confirmations will have these fields) * Route * Duration * Account * SummaryMessage * OrderConfirmId  **Equity Confirmation** (Base Confirmation fields + the following) * EstimatedPrice * EstimatedPriceDisplay * EstimatedCost * EstimatedCostDisplay * EstimatedCommission * EstimatedCommissionDisplay  **Forex Confirmation** (Base Confirmation fields + the following) * BaseCurrency * CounterCurrency * InitialMarginDisplay  **Futures Confirmation** (Base Confirmation fields + the following) * ProductCurrency * AccountCurrency * EstimatedCost * EstimatedPrice * EstimatedPriceDisplay * InitialMarginDisplay * EstimatedCommission * EstimatedCommissionDisplay 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="body"></param>
        /// <returns>GroupOrderConfirmResponseDefinition</returns>
        GroupOrderConfirmResponseDefinition PostOrderGroupsConfirm (string accessToken, GroupOrderConfirmRequestDefinition body);

        /// <summary>
        /// Confirm Group Order 
        /// </summary>
        /// <remarks>
        /// Returns estimated cost and commission information for a group of orders (OCO, BRK) without the orders actually being placed  **Base Confirmation**  (All confirmations will have these fields) * Route * Duration * Account * SummaryMessage * OrderConfirmId  **Equity Confirmation** (Base Confirmation fields + the following) * EstimatedPrice * EstimatedPriceDisplay * EstimatedCost * EstimatedCostDisplay * EstimatedCommission * EstimatedCommissionDisplay  **Forex Confirmation** (Base Confirmation fields + the following) * BaseCurrency * CounterCurrency * InitialMarginDisplay  **Futures Confirmation** (Base Confirmation fields + the following) * ProductCurrency * AccountCurrency * EstimatedCost * EstimatedPrice * EstimatedPriceDisplay * InitialMarginDisplay * EstimatedCommission * EstimatedCommissionDisplay 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="body"></param>
        /// <returns>ApiResponse of GroupOrderConfirmResponseDefinition</returns>
        ApiResponse<GroupOrderConfirmResponseDefinition> PostOrderGroupsConfirmWithHttpInfo (string accessToken, GroupOrderConfirmRequestDefinition body);
        #endregion Synchronous Operations
        #region Asynchronous Operations
        /// <summary>
        /// Cancel Order 
        /// </summary>
        /// <remarks>
        /// Cancels an open order. You cannot cancel an order that has been filled. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="orderId">An existing Order ID</param>
        /// <returns>Task of OrderResponseDefinition</returns>
        System.Threading.Tasks.Task<OrderResponseDefinition> CancelOrderAsync (string accessToken, string orderId);

        /// <summary>
        /// Cancel Order 
        /// </summary>
        /// <remarks>
        /// Cancels an open order. You cannot cancel an order that has been filled. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="orderId">An existing Order ID</param>
        /// <returns>Task of ApiResponse (OrderResponseDefinition)</returns>
        System.Threading.Tasks.Task<ApiResponse<OrderResponseDefinition>> CancelOrderAsyncWithHttpInfo (string accessToken, string orderId);
        /// <summary>
        /// Update Order 
        /// </summary>
        /// <remarks>
        /// Updates (Cancels &amp; Replaces) an open order. You cannot update an order that has been filled.  Rules:   | Original order type              | Fields to update                 | Can only change order type to |  | - -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- - | - -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- - | - -- -- -- -- -- -- -- -- -- -- -- -- -- -- |  | Limit Orders                     | Quantity, Stop Price             | Market                        |  | Stop Orders                      | Quantity, Stop Price             | Market                        |  | Stop Limit Orders                | Quantity, Stop Price, Stop Limit | Market                        |  | Stop Market Trailing Stop Orders | Quantity                         | Market                        |  | Trailing Stop Orders             | Offset type and value            | Market                        | 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="orderId">An existing Order ID</param>
        /// <param name="body"></param>
        /// <returns>Task of OrderResponseDefinition</returns>
        System.Threading.Tasks.Task<OrderResponseDefinition> CancelReplaceOrderAsync (string accessToken, string orderId, CancelReplaceDefinition body);

        /// <summary>
        /// Update Order 
        /// </summary>
        /// <remarks>
        /// Updates (Cancels &amp; Replaces) an open order. You cannot update an order that has been filled.  Rules:   | Original order type              | Fields to update                 | Can only change order type to |  | - -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- - | - -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- - | - -- -- -- -- -- -- -- -- -- -- -- -- -- -- |  | Limit Orders                     | Quantity, Stop Price             | Market                        |  | Stop Orders                      | Quantity, Stop Price             | Market                        |  | Stop Limit Orders                | Quantity, Stop Price, Stop Limit | Market                        |  | Stop Market Trailing Stop Orders | Quantity                         | Market                        |  | Trailing Stop Orders             | Offset type and value            | Market                        | 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="orderId">An existing Order ID</param>
        /// <param name="body"></param>
        /// <returns>Task of ApiResponse (OrderResponseDefinition)</returns>
        System.Threading.Tasks.Task<ApiResponse<OrderResponseDefinition>> CancelReplaceOrderAsyncWithHttpInfo (string accessToken, string orderId, CancelReplaceDefinition body);
        /// <summary>
        ///  Request Available Activation Triggers 
        /// </summary>
        /// <remarks>
        /// To place orders with activation triggers, a valid TriggerKey must be sent with the order.  This resource provides the available trigger methods with their corresponding key. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <returns>Task of InlineResponse200</returns>
        System.Threading.Tasks.Task<InlineResponse200> GetActivationTriggersAsync (string accessToken);

        /// <summary>
        ///  Request Available Activation Triggers 
        /// </summary>
        /// <remarks>
        /// To place orders with activation triggers, a valid TriggerKey must be sent with the order.  This resource provides the available trigger methods with their corresponding key. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <returns>Task of ApiResponse (InlineResponse200)</returns>
        System.Threading.Tasks.Task<ApiResponse<InlineResponse200>> GetActivationTriggersAsyncWithHttpInfo (string accessToken);
        /// <summary>
        /// Request Available Exchanges 
        /// </summary>
        /// <remarks>
        /// Returns a list of valid exchanges that a client can specify when posting an order. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <returns>Task of InlineResponse2001</returns>
        System.Threading.Tasks.Task<InlineResponse2001> GetExchangesAsync (string accessToken);

        /// <summary>
        /// Request Available Exchanges 
        /// </summary>
        /// <remarks>
        /// Returns a list of valid exchanges that a client can specify when posting an order. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <returns>Task of ApiResponse (InlineResponse2001)</returns>
        System.Threading.Tasks.Task<ApiResponse<InlineResponse2001>> GetExchangesAsyncWithHttpInfo (string accessToken);
        /// <summary>
        /// Submit Order 
        /// </summary>
        /// <remarks>
        /// Submits 1 or more orders 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="body"></param>
        /// <returns>Task of OrderResponseDefinition</returns>
        System.Threading.Tasks.Task<OrderResponseDefinition> PostOrderAsync (string accessToken, OrderRequestDefinition body);

        /// <summary>
        /// Submit Order 
        /// </summary>
        /// <remarks>
        /// Submits 1 or more orders 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="body"></param>
        /// <returns>Task of ApiResponse (OrderResponseDefinition)</returns>
        System.Threading.Tasks.Task<ApiResponse<OrderResponseDefinition>> PostOrderAsyncWithHttpInfo (string accessToken, OrderRequestDefinition body);
        /// <summary>
        /// Confirm Order 
        /// </summary>
        /// <remarks>
        /// Returns estimated cost and commission information for an order without the order actually being placed. The fields that are returned in the response depend on the order type. The following shows the different fields that will be returned.  **Base Confirmation**  (All confirmations will have these fields) * Route * Duration * Account * SummaryMessage * OrderConfirmId  **Equity Confirmation** (Base Confirmation fields + the following) * EstimatedPrice * EstimatedPriceDisplay * EstimatedCost * EstimatedCostDisplay * EstimatedCommission * EstimatedCommissionDisplay * DebitCreditEstimatedCost * DebitCreditEstimatedCostDisplay  **Forex Confirmation** (Base Confirmation fields + the following) * BaseCurrency * CounterCurrency * InitialMarginDisplay  **Futures Confirmation** (Base Confirmation fields + the following) * ProductCurrency * AccountCurrency * EstimatedCost * EstimatedPrice * EstimatedPriceDisplay * InitialMarginDisplay * EstimatedCommission * EstimatedCommissionDisplay 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="body"></param>
        /// <returns>Task of OrderConfirmResponseDefinition</returns>
        System.Threading.Tasks.Task<OrderConfirmResponseDefinition> PostOrderConfirmAsync (string accessToken, OrderConfirmRequestDefinition body);

        /// <summary>
        /// Confirm Order 
        /// </summary>
        /// <remarks>
        /// Returns estimated cost and commission information for an order without the order actually being placed. The fields that are returned in the response depend on the order type. The following shows the different fields that will be returned.  **Base Confirmation**  (All confirmations will have these fields) * Route * Duration * Account * SummaryMessage * OrderConfirmId  **Equity Confirmation** (Base Confirmation fields + the following) * EstimatedPrice * EstimatedPriceDisplay * EstimatedCost * EstimatedCostDisplay * EstimatedCommission * EstimatedCommissionDisplay * DebitCreditEstimatedCost * DebitCreditEstimatedCostDisplay  **Forex Confirmation** (Base Confirmation fields + the following) * BaseCurrency * CounterCurrency * InitialMarginDisplay  **Futures Confirmation** (Base Confirmation fields + the following) * ProductCurrency * AccountCurrency * EstimatedCost * EstimatedPrice * EstimatedPriceDisplay * InitialMarginDisplay * EstimatedCommission * EstimatedCommissionDisplay 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="body"></param>
        /// <returns>Task of ApiResponse (OrderConfirmResponseDefinition)</returns>
        System.Threading.Tasks.Task<ApiResponse<OrderConfirmResponseDefinition>> PostOrderConfirmAsyncWithHttpInfo (string accessToken, OrderConfirmRequestDefinition body);
        /// <summary>
        /// Submit Group Order 
        /// </summary>
        /// <remarks>
        /// Submits a group order such as Bracket and OCO Orders.  #### Order Cancels Order (OCO) An OCO order is a group of orders whereby if one of the orders is filled or partially-filled, then all of the other orders in the group are cancelled.  #### Bracket OCO Orders A bracket order is a special instance of an OCO (Order Cancel Order). Bracket orders are used to exit an existing position. They are designed to limit loss and lock in profit by “bracketing” an order with a simultaneous stop and limit order.  Bracket orders are limited so that the orders are all for the same symbol and are on the same side of the market (either all to sell or all to cover), and they are restricted to closing transactions.  The reason that they follow these rules is because the orders need to be able to auto decrement when a partial fill occurs with one of the orders. For example, if the customer has a sell limit order for 1000 shares and a sell stop order for 1000 shares, and the limit order is partially filled for 500 shares, then the customer would want the stop to remain open, but it should automatically decrement the order to 500 shares to match the remaining open position.  ### Errors When a submitted order fails, it can be seen in two different ways: - Immediately rejected during submit to the Orders API with a 400 HTTP status code. - Accepted by the API with a 200 HTTP status code, but you will see the order with a status of “REJ” (rejected) when you fetch the Order from the /v2/accounts/{id}/orders API  #### NOTE When a group order is submitted, the order execution system treats each sibling order as an individual order. Thus, the system does not validate that each order has the same Quantity, and currently it is not able to update a bracket order as one transaction, instead you must update each order within a bracket.  In order to prevent errors, please validate the data on the client side. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="body"></param>
        /// <returns>Task of GroupOrderResponseDefinition</returns>
        System.Threading.Tasks.Task<GroupOrderResponseDefinition> PostOrderGroupAsync (string accessToken, GroupOrderRequestDefinition body);

        /// <summary>
        /// Submit Group Order 
        /// </summary>
        /// <remarks>
        /// Submits a group order such as Bracket and OCO Orders.  #### Order Cancels Order (OCO) An OCO order is a group of orders whereby if one of the orders is filled or partially-filled, then all of the other orders in the group are cancelled.  #### Bracket OCO Orders A bracket order is a special instance of an OCO (Order Cancel Order). Bracket orders are used to exit an existing position. They are designed to limit loss and lock in profit by “bracketing” an order with a simultaneous stop and limit order.  Bracket orders are limited so that the orders are all for the same symbol and are on the same side of the market (either all to sell or all to cover), and they are restricted to closing transactions.  The reason that they follow these rules is because the orders need to be able to auto decrement when a partial fill occurs with one of the orders. For example, if the customer has a sell limit order for 1000 shares and a sell stop order for 1000 shares, and the limit order is partially filled for 500 shares, then the customer would want the stop to remain open, but it should automatically decrement the order to 500 shares to match the remaining open position.  ### Errors When a submitted order fails, it can be seen in two different ways: - Immediately rejected during submit to the Orders API with a 400 HTTP status code. - Accepted by the API with a 200 HTTP status code, but you will see the order with a status of “REJ” (rejected) when you fetch the Order from the /v2/accounts/{id}/orders API  #### NOTE When a group order is submitted, the order execution system treats each sibling order as an individual order. Thus, the system does not validate that each order has the same Quantity, and currently it is not able to update a bracket order as one transaction, instead you must update each order within a bracket.  In order to prevent errors, please validate the data on the client side. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="body"></param>
        /// <returns>Task of ApiResponse (GroupOrderResponseDefinition)</returns>
        System.Threading.Tasks.Task<ApiResponse<GroupOrderResponseDefinition>> PostOrderGroupAsyncWithHttpInfo (string accessToken, GroupOrderRequestDefinition body);
        /// <summary>
        /// Confirm Group Order 
        /// </summary>
        /// <remarks>
        /// Returns estimated cost and commission information for a group of orders (OCO, BRK) without the orders actually being placed  **Base Confirmation**  (All confirmations will have these fields) * Route * Duration * Account * SummaryMessage * OrderConfirmId  **Equity Confirmation** (Base Confirmation fields + the following) * EstimatedPrice * EstimatedPriceDisplay * EstimatedCost * EstimatedCostDisplay * EstimatedCommission * EstimatedCommissionDisplay  **Forex Confirmation** (Base Confirmation fields + the following) * BaseCurrency * CounterCurrency * InitialMarginDisplay  **Futures Confirmation** (Base Confirmation fields + the following) * ProductCurrency * AccountCurrency * EstimatedCost * EstimatedPrice * EstimatedPriceDisplay * InitialMarginDisplay * EstimatedCommission * EstimatedCommissionDisplay 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="body"></param>
        /// <returns>Task of GroupOrderConfirmResponseDefinition</returns>
        System.Threading.Tasks.Task<GroupOrderConfirmResponseDefinition> PostOrderGroupsConfirmAsync (string accessToken, GroupOrderConfirmRequestDefinition body);

        /// <summary>
        /// Confirm Group Order 
        /// </summary>
        /// <remarks>
        /// Returns estimated cost and commission information for a group of orders (OCO, BRK) without the orders actually being placed  **Base Confirmation**  (All confirmations will have these fields) * Route * Duration * Account * SummaryMessage * OrderConfirmId  **Equity Confirmation** (Base Confirmation fields + the following) * EstimatedPrice * EstimatedPriceDisplay * EstimatedCost * EstimatedCostDisplay * EstimatedCommission * EstimatedCommissionDisplay  **Forex Confirmation** (Base Confirmation fields + the following) * BaseCurrency * CounterCurrency * InitialMarginDisplay  **Futures Confirmation** (Base Confirmation fields + the following) * ProductCurrency * AccountCurrency * EstimatedCost * EstimatedPrice * EstimatedPriceDisplay * InitialMarginDisplay * EstimatedCommission * EstimatedCommissionDisplay 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="body"></param>
        /// <returns>Task of ApiResponse (GroupOrderConfirmResponseDefinition)</returns>
        System.Threading.Tasks.Task<ApiResponse<GroupOrderConfirmResponseDefinition>> PostOrderGroupsConfirmAsyncWithHttpInfo (string accessToken, GroupOrderConfirmRequestDefinition body);
        #endregion Asynchronous Operations
    }

    /// <summary>
    /// Represents a collection of functions to interact with the API endpoints
    /// </summary>
    public partial class OrderExecutionApi : IOrderExecutionApi
    {
        private TradeStationAPI.Client.ExceptionFactory _exceptionFactory = (name, response) => null;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderExecutionApi"/> class.
        /// </summary>
        /// <returns></returns>
        public OrderExecutionApi(String basePath)
        {
            this.Configuration = new TradeStationAPI.Client.Configuration { BasePath = basePath };

            ExceptionFactory = TradeStationAPI.Client.Configuration.DefaultExceptionFactory;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderExecutionApi"/> class
        /// using Configuration object
        /// </summary>
        /// <param name="configuration">An instance of Configuration</param>
        /// <returns></returns>
        public OrderExecutionApi(TradeStationAPI.Client.Configuration configuration = null)
        {
            if (configuration == null) // use the default one in Configuration
                this.Configuration = TradeStationAPI.Client.Configuration.Default;
            else
                this.Configuration = configuration;

            ExceptionFactory = TradeStationAPI.Client.Configuration.DefaultExceptionFactory;
        }

        /// <summary>
        /// Gets the base path of the API client.
        /// </summary>
        /// <value>The base path</value>
        public String GetBasePath()
        {
            return this.Configuration.ApiClient.RestClient.BaseUrl.ToString();
        }

        /// <summary>
        /// Sets the base path of the API client.
        /// </summary>
        /// <value>The base path</value>
        [Obsolete("SetBasePath is deprecated, please do 'Configuration.ApiClient = new ApiClient(\"http://new-path\")' instead.")]
        public void SetBasePath(String basePath)
        {
            // do nothing
        }

        /// <summary>
        /// Gets or sets the configuration object
        /// </summary>
        /// <value>An instance of the Configuration</value>
        public TradeStationAPI.Client.Configuration Configuration {get; set;}

        /// <summary>
        /// Provides a factory method hook for the creation of exceptions.
        /// </summary>
        public TradeStationAPI.Client.ExceptionFactory ExceptionFactory
        {
            get
            {
                if (_exceptionFactory != null && _exceptionFactory.GetInvocationList().Length > 1)
                {
                    throw new InvalidOperationException("Multicast delegate for ExceptionFactory is unsupported.");
                }
                return _exceptionFactory;
            }
            set { _exceptionFactory = value; }
        }

        /// <summary>
        /// Gets the default header.
        /// </summary>
        /// <returns>Dictionary of HTTP header</returns>
        [Obsolete("DefaultHeader is deprecated, please use Configuration.DefaultHeader instead.")]
        public IDictionary<String, String> DefaultHeader()
        {
            return new ReadOnlyDictionary<string, string>(this.Configuration.DefaultHeader);
        }

        /// <summary>
        /// Add default header.
        /// </summary>
        /// <param name="key">Header field name.</param>
        /// <param name="value">Header field value.</param>
        /// <returns></returns>
        [Obsolete("AddDefaultHeader is deprecated, please use Configuration.AddDefaultHeader instead.")]
        public void AddDefaultHeader(string key, string value)
        {
            this.Configuration.AddDefaultHeader(key, value);
        }

        /// <summary>
        /// Cancel Order  Cancels an open order. You cannot cancel an order that has been filled. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="orderId">An existing Order ID</param>
        /// <returns>OrderResponseDefinition</returns>
        public OrderResponseDefinition CancelOrder (string accessToken, string orderId)
        {
             ApiResponse<OrderResponseDefinition> localVarResponse = CancelOrderWithHttpInfo(accessToken, orderId);
             return localVarResponse.Data;
        }

        /// <summary>
        /// Cancel Order  Cancels an open order. You cannot cancel an order that has been filled. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="orderId">An existing Order ID</param>
        /// <returns>ApiResponse of OrderResponseDefinition</returns>
        public ApiResponse< OrderResponseDefinition > CancelOrderWithHttpInfo (string accessToken, string orderId)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling OrderExecutionApi->CancelOrder");
            // verify the required parameter 'orderId' is set
            if (orderId == null)
                throw new ApiException(400, "Missing required parameter 'orderId' when calling OrderExecutionApi->CancelOrder");

            var localVarPath = "/orders/{order_id}";
            var localVarPathParams = new Dictionary<String, String>();
            var localVarQueryParams = new List<KeyValuePair<String, String>>();
            var localVarHeaderParams = new Dictionary<String, String>(this.Configuration.DefaultHeader);
            var localVarFormParams = new Dictionary<String, String>();
            var localVarFileParams = new Dictionary<String, FileParameter>();
            Object localVarPostBody = null;

            // to determine the Content-Type header
            String[] localVarHttpContentTypes = new String[] {
                "application/json; charset=utf-8"
            };
            String localVarHttpContentType = this.Configuration.ApiClient.SelectHeaderContentType(localVarHttpContentTypes);

            // to determine the Accept header
            String[] localVarHttpHeaderAccepts = new String[] {
                "application/json; charset=utf-8"
            };
            String localVarHttpHeaderAccept = this.Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            if (orderId != null) localVarPathParams.Add("order_id", this.Configuration.ApiClient.ParameterToString(orderId)); // path parameter
            if (accessToken != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "access_token", accessToken)); // query parameter

            // authentication (OAuth2-Auth-Code) required
            // oauth required
            if (!String.IsNullOrEmpty(this.Configuration.AccessToken))
            {
                localVarHeaderParams["Authorization"] = "Bearer " + this.Configuration.AccessToken;
            }
            // authentication (OAuth2-Refresh-Token) required
            // oauth required
            if (!String.IsNullOrEmpty(this.Configuration.AccessToken))
            {
                localVarHeaderParams["Authorization"] = "Bearer " + this.Configuration.AccessToken;
            }

            // make the HTTP request
            IRestResponse localVarResponse = (IRestResponse) this.Configuration.ApiClient.CallApi(localVarPath,
                Method.DELETE, localVarQueryParams, localVarPostBody, localVarHeaderParams, localVarFormParams, localVarFileParams,
                localVarPathParams, localVarHttpContentType);

            int localVarStatusCode = (int) localVarResponse.StatusCode;

            if (ExceptionFactory != null)
            {
                Exception exception = ExceptionFactory("CancelOrder", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<OrderResponseDefinition>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (OrderResponseDefinition) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(OrderResponseDefinition)));
        }

        /// <summary>
        /// Cancel Order  Cancels an open order. You cannot cancel an order that has been filled. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="orderId">An existing Order ID</param>
        /// <returns>Task of OrderResponseDefinition</returns>
        public async System.Threading.Tasks.Task<OrderResponseDefinition> CancelOrderAsync (string accessToken, string orderId)
        {
             ApiResponse<OrderResponseDefinition> localVarResponse = await CancelOrderAsyncWithHttpInfo(accessToken, orderId);
             return localVarResponse.Data;

        }

        /// <summary>
        /// Cancel Order  Cancels an open order. You cannot cancel an order that has been filled. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="orderId">An existing Order ID</param>
        /// <returns>Task of ApiResponse (OrderResponseDefinition)</returns>
        public async System.Threading.Tasks.Task<ApiResponse<OrderResponseDefinition>> CancelOrderAsyncWithHttpInfo (string accessToken, string orderId)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling OrderExecutionApi->CancelOrder");
            // verify the required parameter 'orderId' is set
            if (orderId == null)
                throw new ApiException(400, "Missing required parameter 'orderId' when calling OrderExecutionApi->CancelOrder");

            var localVarPath = "/orders/{order_id}";
            var localVarPathParams = new Dictionary<String, String>();
            var localVarQueryParams = new List<KeyValuePair<String, String>>();
            var localVarHeaderParams = new Dictionary<String, String>(this.Configuration.DefaultHeader);
            var localVarFormParams = new Dictionary<String, String>();
            var localVarFileParams = new Dictionary<String, FileParameter>();
            Object localVarPostBody = null;

            // to determine the Content-Type header
            String[] localVarHttpContentTypes = new String[] {
                "application/json; charset=utf-8"
            };
            String localVarHttpContentType = this.Configuration.ApiClient.SelectHeaderContentType(localVarHttpContentTypes);

            // to determine the Accept header
            String[] localVarHttpHeaderAccepts = new String[] {
                "application/json; charset=utf-8"
            };
            String localVarHttpHeaderAccept = this.Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            if (orderId != null) localVarPathParams.Add("order_id", this.Configuration.ApiClient.ParameterToString(orderId)); // path parameter
            if (accessToken != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "access_token", accessToken)); // query parameter

            // authentication (OAuth2-Auth-Code) required
            // oauth required
            if (!String.IsNullOrEmpty(this.Configuration.AccessToken))
            {
                localVarHeaderParams["Authorization"] = "Bearer " + this.Configuration.AccessToken;
            }
            // authentication (OAuth2-Refresh-Token) required
            // oauth required
            if (!String.IsNullOrEmpty(this.Configuration.AccessToken))
            {
                localVarHeaderParams["Authorization"] = "Bearer " + this.Configuration.AccessToken;
            }

            // make the HTTP request
            IRestResponse localVarResponse = (IRestResponse) await this.Configuration.ApiClient.CallApiAsync(localVarPath,
                Method.DELETE, localVarQueryParams, localVarPostBody, localVarHeaderParams, localVarFormParams, localVarFileParams,
                localVarPathParams, localVarHttpContentType);

            int localVarStatusCode = (int) localVarResponse.StatusCode;

            if (ExceptionFactory != null)
            {
                Exception exception = ExceptionFactory("CancelOrder", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<OrderResponseDefinition>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (OrderResponseDefinition) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(OrderResponseDefinition)));
        }

        /// <summary>
        /// Update Order  Updates (Cancels &amp; Replaces) an open order. You cannot update an order that has been filled.  Rules:   | Original order type              | Fields to update                 | Can only change order type to |  | - -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- - | - -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- - | - -- -- -- -- -- -- -- -- -- -- -- -- -- -- |  | Limit Orders                     | Quantity, Stop Price             | Market                        |  | Stop Orders                      | Quantity, Stop Price             | Market                        |  | Stop Limit Orders                | Quantity, Stop Price, Stop Limit | Market                        |  | Stop Market Trailing Stop Orders | Quantity                         | Market                        |  | Trailing Stop Orders             | Offset type and value            | Market                        | 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="orderId">An existing Order ID</param>
        /// <param name="body"></param>
        /// <returns>OrderResponseDefinition</returns>
        public OrderResponseDefinition CancelReplaceOrder (string accessToken, string orderId, CancelReplaceDefinition body)
        {
             ApiResponse<OrderResponseDefinition> localVarResponse = CancelReplaceOrderWithHttpInfo(accessToken, orderId, body);
             return localVarResponse.Data;
        }

        /// <summary>
        /// Update Order  Updates (Cancels &amp; Replaces) an open order. You cannot update an order that has been filled.  Rules:   | Original order type              | Fields to update                 | Can only change order type to |  | - -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- - | - -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- - | - -- -- -- -- -- -- -- -- -- -- -- -- -- -- |  | Limit Orders                     | Quantity, Stop Price             | Market                        |  | Stop Orders                      | Quantity, Stop Price             | Market                        |  | Stop Limit Orders                | Quantity, Stop Price, Stop Limit | Market                        |  | Stop Market Trailing Stop Orders | Quantity                         | Market                        |  | Trailing Stop Orders             | Offset type and value            | Market                        | 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="orderId">An existing Order ID</param>
        /// <param name="body"></param>
        /// <returns>ApiResponse of OrderResponseDefinition</returns>
        public ApiResponse< OrderResponseDefinition > CancelReplaceOrderWithHttpInfo (string accessToken, string orderId, CancelReplaceDefinition body)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling OrderExecutionApi->CancelReplaceOrder");
            // verify the required parameter 'orderId' is set
            if (orderId == null)
                throw new ApiException(400, "Missing required parameter 'orderId' when calling OrderExecutionApi->CancelReplaceOrder");
            // verify the required parameter 'body' is set
            if (body == null)
                throw new ApiException(400, "Missing required parameter 'body' when calling OrderExecutionApi->CancelReplaceOrder");

            var localVarPath = "/orders/{order_id}";
            var localVarPathParams = new Dictionary<String, String>();
            var localVarQueryParams = new List<KeyValuePair<String, String>>();
            var localVarHeaderParams = new Dictionary<String, String>(this.Configuration.DefaultHeader);
            var localVarFormParams = new Dictionary<String, String>();
            var localVarFileParams = new Dictionary<String, FileParameter>();
            Object localVarPostBody = null;

            // to determine the Content-Type header
            String[] localVarHttpContentTypes = new String[] {
                "application/json; charset=utf-8"
            };
            String localVarHttpContentType = this.Configuration.ApiClient.SelectHeaderContentType(localVarHttpContentTypes);

            // to determine the Accept header
            String[] localVarHttpHeaderAccepts = new String[] {
                "application/json; charset=utf-8"
            };
            String localVarHttpHeaderAccept = this.Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            if (orderId != null) localVarPathParams.Add("order_id", this.Configuration.ApiClient.ParameterToString(orderId)); // path parameter
            if (accessToken != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "access_token", accessToken)); // query parameter
            if (body != null && body.GetType() != typeof(byte[]))
            {
                localVarPostBody = this.Configuration.ApiClient.Serialize(body); // http body (model) parameter
            }
            else
            {
                localVarPostBody = body; // byte array
            }


            // make the HTTP request
            IRestResponse localVarResponse = (IRestResponse) this.Configuration.ApiClient.CallApi(localVarPath,
                Method.PUT, localVarQueryParams, localVarPostBody, localVarHeaderParams, localVarFormParams, localVarFileParams,
                localVarPathParams, localVarHttpContentType);

            int localVarStatusCode = (int) localVarResponse.StatusCode;

            if (ExceptionFactory != null)
            {
                Exception exception = ExceptionFactory("CancelReplaceOrder", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<OrderResponseDefinition>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (OrderResponseDefinition) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(OrderResponseDefinition)));
        }

        /// <summary>
        /// Update Order  Updates (Cancels &amp; Replaces) an open order. You cannot update an order that has been filled.  Rules:   | Original order type              | Fields to update                 | Can only change order type to |  | - -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- - | - -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- - | - -- -- -- -- -- -- -- -- -- -- -- -- -- -- |  | Limit Orders                     | Quantity, Stop Price             | Market                        |  | Stop Orders                      | Quantity, Stop Price             | Market                        |  | Stop Limit Orders                | Quantity, Stop Price, Stop Limit | Market                        |  | Stop Market Trailing Stop Orders | Quantity                         | Market                        |  | Trailing Stop Orders             | Offset type and value            | Market                        | 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="orderId">An existing Order ID</param>
        /// <param name="body"></param>
        /// <returns>Task of OrderResponseDefinition</returns>
        public async System.Threading.Tasks.Task<OrderResponseDefinition> CancelReplaceOrderAsync (string accessToken, string orderId, CancelReplaceDefinition body)
        {
             ApiResponse<OrderResponseDefinition> localVarResponse = await CancelReplaceOrderAsyncWithHttpInfo(accessToken, orderId, body);
             return localVarResponse.Data;

        }

        /// <summary>
        /// Update Order  Updates (Cancels &amp; Replaces) an open order. You cannot update an order that has been filled.  Rules:   | Original order type              | Fields to update                 | Can only change order type to |  | - -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- - | - -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- - | - -- -- -- -- -- -- -- -- -- -- -- -- -- -- |  | Limit Orders                     | Quantity, Stop Price             | Market                        |  | Stop Orders                      | Quantity, Stop Price             | Market                        |  | Stop Limit Orders                | Quantity, Stop Price, Stop Limit | Market                        |  | Stop Market Trailing Stop Orders | Quantity                         | Market                        |  | Trailing Stop Orders             | Offset type and value            | Market                        | 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="orderId">An existing Order ID</param>
        /// <param name="body"></param>
        /// <returns>Task of ApiResponse (OrderResponseDefinition)</returns>
        public async System.Threading.Tasks.Task<ApiResponse<OrderResponseDefinition>> CancelReplaceOrderAsyncWithHttpInfo (string accessToken, string orderId, CancelReplaceDefinition body)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling OrderExecutionApi->CancelReplaceOrder");
            // verify the required parameter 'orderId' is set
            if (orderId == null)
                throw new ApiException(400, "Missing required parameter 'orderId' when calling OrderExecutionApi->CancelReplaceOrder");
            // verify the required parameter 'body' is set
            if (body == null)
                throw new ApiException(400, "Missing required parameter 'body' when calling OrderExecutionApi->CancelReplaceOrder");

            var localVarPath = "/orders/{order_id}";
            var localVarPathParams = new Dictionary<String, String>();
            var localVarQueryParams = new List<KeyValuePair<String, String>>();
            var localVarHeaderParams = new Dictionary<String, String>(this.Configuration.DefaultHeader);
            var localVarFormParams = new Dictionary<String, String>();
            var localVarFileParams = new Dictionary<String, FileParameter>();
            Object localVarPostBody = null;

            // to determine the Content-Type header
            String[] localVarHttpContentTypes = new String[] {
                "application/json; charset=utf-8"
            };
            String localVarHttpContentType = this.Configuration.ApiClient.SelectHeaderContentType(localVarHttpContentTypes);

            // to determine the Accept header
            String[] localVarHttpHeaderAccepts = new String[] {
                "application/json; charset=utf-8"
            };
            String localVarHttpHeaderAccept = this.Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            if (orderId != null) localVarPathParams.Add("order_id", this.Configuration.ApiClient.ParameterToString(orderId)); // path parameter
            if (accessToken != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "access_token", accessToken)); // query parameter
            if (body != null && body.GetType() != typeof(byte[]))
            {
                localVarPostBody = this.Configuration.ApiClient.Serialize(body); // http body (model) parameter
            }
            else
            {
                localVarPostBody = body; // byte array
            }


            // make the HTTP request
            IRestResponse localVarResponse = (IRestResponse) await this.Configuration.ApiClient.CallApiAsync(localVarPath,
                Method.PUT, localVarQueryParams, localVarPostBody, localVarHeaderParams, localVarFormParams, localVarFileParams,
                localVarPathParams, localVarHttpContentType);

            int localVarStatusCode = (int) localVarResponse.StatusCode;

            if (ExceptionFactory != null)
            {
                Exception exception = ExceptionFactory("CancelReplaceOrder", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<OrderResponseDefinition>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (OrderResponseDefinition) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(OrderResponseDefinition)));
        }

        /// <summary>
        ///  Request Available Activation Triggers  To place orders with activation triggers, a valid TriggerKey must be sent with the order.  This resource provides the available trigger methods with their corresponding key. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <returns>InlineResponse200</returns>
        public InlineResponse200 GetActivationTriggers (string accessToken)
        {
             ApiResponse<InlineResponse200> localVarResponse = GetActivationTriggersWithHttpInfo(accessToken);
             return localVarResponse.Data;
        }

        /// <summary>
        ///  Request Available Activation Triggers  To place orders with activation triggers, a valid TriggerKey must be sent with the order.  This resource provides the available trigger methods with their corresponding key. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <returns>ApiResponse of InlineResponse200</returns>
        public ApiResponse< InlineResponse200 > GetActivationTriggersWithHttpInfo (string accessToken)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling OrderExecutionApi->GetActivationTriggers");

            var localVarPath = "/orderexecution/activationtriggers";
            var localVarPathParams = new Dictionary<String, String>();
            var localVarQueryParams = new List<KeyValuePair<String, String>>();
            var localVarHeaderParams = new Dictionary<String, String>(this.Configuration.DefaultHeader);
            var localVarFormParams = new Dictionary<String, String>();
            var localVarFileParams = new Dictionary<String, FileParameter>();
            Object localVarPostBody = null;

            // to determine the Content-Type header
            String[] localVarHttpContentTypes = new String[] {
                "application/json; charset=utf-8"
            };
            String localVarHttpContentType = this.Configuration.ApiClient.SelectHeaderContentType(localVarHttpContentTypes);

            // to determine the Accept header
            String[] localVarHttpHeaderAccepts = new String[] {
                "application/json; charset=utf-8"
            };
            String localVarHttpHeaderAccept = this.Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            if (accessToken != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "access_token", accessToken)); // query parameter

            // authentication (OAuth2-Auth-Code) required
            // oauth required
            if (!String.IsNullOrEmpty(this.Configuration.AccessToken))
            {
                localVarHeaderParams["Authorization"] = "Bearer " + this.Configuration.AccessToken;
            }
            // authentication (OAuth2-Refresh-Token) required
            // oauth required
            if (!String.IsNullOrEmpty(this.Configuration.AccessToken))
            {
                localVarHeaderParams["Authorization"] = "Bearer " + this.Configuration.AccessToken;
            }

            // make the HTTP request
            IRestResponse localVarResponse = (IRestResponse) this.Configuration.ApiClient.CallApi(localVarPath,
                Method.GET, localVarQueryParams, localVarPostBody, localVarHeaderParams, localVarFormParams, localVarFileParams,
                localVarPathParams, localVarHttpContentType);

            int localVarStatusCode = (int) localVarResponse.StatusCode;

            if (ExceptionFactory != null)
            {
                Exception exception = ExceptionFactory("GetActivationTriggers", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<InlineResponse200>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (InlineResponse200) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(InlineResponse200)));
        }

        /// <summary>
        ///  Request Available Activation Triggers  To place orders with activation triggers, a valid TriggerKey must be sent with the order.  This resource provides the available trigger methods with their corresponding key. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <returns>Task of InlineResponse200</returns>
        public async System.Threading.Tasks.Task<InlineResponse200> GetActivationTriggersAsync (string accessToken)
        {
             ApiResponse<InlineResponse200> localVarResponse = await GetActivationTriggersAsyncWithHttpInfo(accessToken);
             return localVarResponse.Data;

        }

        /// <summary>
        ///  Request Available Activation Triggers  To place orders with activation triggers, a valid TriggerKey must be sent with the order.  This resource provides the available trigger methods with their corresponding key. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <returns>Task of ApiResponse (InlineResponse200)</returns>
        public async System.Threading.Tasks.Task<ApiResponse<InlineResponse200>> GetActivationTriggersAsyncWithHttpInfo (string accessToken)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling OrderExecutionApi->GetActivationTriggers");

            var localVarPath = "/orderexecution/activationtriggers";
            var localVarPathParams = new Dictionary<String, String>();
            var localVarQueryParams = new List<KeyValuePair<String, String>>();
            var localVarHeaderParams = new Dictionary<String, String>(this.Configuration.DefaultHeader);
            var localVarFormParams = new Dictionary<String, String>();
            var localVarFileParams = new Dictionary<String, FileParameter>();
            Object localVarPostBody = null;

            // to determine the Content-Type header
            String[] localVarHttpContentTypes = new String[] {
                "application/json; charset=utf-8"
            };
            String localVarHttpContentType = this.Configuration.ApiClient.SelectHeaderContentType(localVarHttpContentTypes);

            // to determine the Accept header
            String[] localVarHttpHeaderAccepts = new String[] {
                "application/json; charset=utf-8"
            };
            String localVarHttpHeaderAccept = this.Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            if (accessToken != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "access_token", accessToken)); // query parameter

            // authentication (OAuth2-Auth-Code) required
            // oauth required
            if (!String.IsNullOrEmpty(this.Configuration.AccessToken))
            {
                localVarHeaderParams["Authorization"] = "Bearer " + this.Configuration.AccessToken;
            }
            // authentication (OAuth2-Refresh-Token) required
            // oauth required
            if (!String.IsNullOrEmpty(this.Configuration.AccessToken))
            {
                localVarHeaderParams["Authorization"] = "Bearer " + this.Configuration.AccessToken;
            }

            // make the HTTP request
            IRestResponse localVarResponse = (IRestResponse) await this.Configuration.ApiClient.CallApiAsync(localVarPath,
                Method.GET, localVarQueryParams, localVarPostBody, localVarHeaderParams, localVarFormParams, localVarFileParams,
                localVarPathParams, localVarHttpContentType);

            int localVarStatusCode = (int) localVarResponse.StatusCode;

            if (ExceptionFactory != null)
            {
                Exception exception = ExceptionFactory("GetActivationTriggers", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<InlineResponse200>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (InlineResponse200) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(InlineResponse200)));
        }

        /// <summary>
        /// Request Available Exchanges  Returns a list of valid exchanges that a client can specify when posting an order. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <returns>InlineResponse2001</returns>
        public InlineResponse2001 GetExchanges (string accessToken)
        {
             ApiResponse<InlineResponse2001> localVarResponse = GetExchangesWithHttpInfo(accessToken);
             return localVarResponse.Data;
        }

        /// <summary>
        /// Request Available Exchanges  Returns a list of valid exchanges that a client can specify when posting an order. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <returns>ApiResponse of InlineResponse2001</returns>
        public ApiResponse< InlineResponse2001 > GetExchangesWithHttpInfo (string accessToken)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling OrderExecutionApi->GetExchanges");

            var localVarPath = "/orderexecution/exchanges";
            var localVarPathParams = new Dictionary<String, String>();
            var localVarQueryParams = new List<KeyValuePair<String, String>>();
            var localVarHeaderParams = new Dictionary<String, String>(this.Configuration.DefaultHeader);
            var localVarFormParams = new Dictionary<String, String>();
            var localVarFileParams = new Dictionary<String, FileParameter>();
            Object localVarPostBody = null;

            // to determine the Content-Type header
            String[] localVarHttpContentTypes = new String[] {
                "application/json; charset=utf-8"
            };
            String localVarHttpContentType = this.Configuration.ApiClient.SelectHeaderContentType(localVarHttpContentTypes);

            // to determine the Accept header
            String[] localVarHttpHeaderAccepts = new String[] {
                "application/json; charset=utf-8"
            };
            String localVarHttpHeaderAccept = this.Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            if (accessToken != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "access_token", accessToken)); // query parameter


            // make the HTTP request
            IRestResponse localVarResponse = (IRestResponse) this.Configuration.ApiClient.CallApi(localVarPath,
                Method.GET, localVarQueryParams, localVarPostBody, localVarHeaderParams, localVarFormParams, localVarFileParams,
                localVarPathParams, localVarHttpContentType);

            int localVarStatusCode = (int) localVarResponse.StatusCode;

            if (ExceptionFactory != null)
            {
                Exception exception = ExceptionFactory("GetExchanges", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<InlineResponse2001>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (InlineResponse2001) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(InlineResponse2001)));
        }

        /// <summary>
        /// Request Available Exchanges  Returns a list of valid exchanges that a client can specify when posting an order. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <returns>Task of InlineResponse2001</returns>
        public async System.Threading.Tasks.Task<InlineResponse2001> GetExchangesAsync (string accessToken)
        {
             ApiResponse<InlineResponse2001> localVarResponse = await GetExchangesAsyncWithHttpInfo(accessToken);
             return localVarResponse.Data;

        }

        /// <summary>
        /// Request Available Exchanges  Returns a list of valid exchanges that a client can specify when posting an order. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <returns>Task of ApiResponse (InlineResponse2001)</returns>
        public async System.Threading.Tasks.Task<ApiResponse<InlineResponse2001>> GetExchangesAsyncWithHttpInfo (string accessToken)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling OrderExecutionApi->GetExchanges");

            var localVarPath = "/orderexecution/exchanges";
            var localVarPathParams = new Dictionary<String, String>();
            var localVarQueryParams = new List<KeyValuePair<String, String>>();
            var localVarHeaderParams = new Dictionary<String, String>(this.Configuration.DefaultHeader);
            var localVarFormParams = new Dictionary<String, String>();
            var localVarFileParams = new Dictionary<String, FileParameter>();
            Object localVarPostBody = null;

            // to determine the Content-Type header
            String[] localVarHttpContentTypes = new String[] {
                "application/json; charset=utf-8"
            };
            String localVarHttpContentType = this.Configuration.ApiClient.SelectHeaderContentType(localVarHttpContentTypes);

            // to determine the Accept header
            String[] localVarHttpHeaderAccepts = new String[] {
                "application/json; charset=utf-8"
            };
            String localVarHttpHeaderAccept = this.Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            if (accessToken != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "access_token", accessToken)); // query parameter


            // make the HTTP request
            IRestResponse localVarResponse = (IRestResponse) await this.Configuration.ApiClient.CallApiAsync(localVarPath,
                Method.GET, localVarQueryParams, localVarPostBody, localVarHeaderParams, localVarFormParams, localVarFileParams,
                localVarPathParams, localVarHttpContentType);

            int localVarStatusCode = (int) localVarResponse.StatusCode;

            if (ExceptionFactory != null)
            {
                Exception exception = ExceptionFactory("GetExchanges", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<InlineResponse2001>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (InlineResponse2001) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(InlineResponse2001)));
        }

        /// <summary>
        /// Submit Order  Submits 1 or more orders 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="body"></param>
        /// <returns>OrderResponseDefinition</returns>
        public OrderResponseDefinition PostOrder (string accessToken, OrderRequestDefinition body)
        {
             ApiResponse<OrderResponseDefinition> localVarResponse = PostOrderWithHttpInfo(accessToken, body);
             return localVarResponse.Data;
        }

        /// <summary>
        /// Submit Order  Submits 1 or more orders 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="body"></param>
        /// <returns>ApiResponse of OrderResponseDefinition</returns>
        public ApiResponse< OrderResponseDefinition > PostOrderWithHttpInfo (string accessToken, OrderRequestDefinition body)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling OrderExecutionApi->PostOrder");
            // verify the required parameter 'body' is set
            if (body == null)
                throw new ApiException(400, "Missing required parameter 'body' when calling OrderExecutionApi->PostOrder");

            var localVarPath = "/orders";
            var localVarPathParams = new Dictionary<String, String>();
            var localVarQueryParams = new List<KeyValuePair<String, String>>();
            var localVarHeaderParams = new Dictionary<String, String>(this.Configuration.DefaultHeader);
            var localVarFormParams = new Dictionary<String, String>();
            var localVarFileParams = new Dictionary<String, FileParameter>();
            Object localVarPostBody = null;

            // to determine the Content-Type header
            String[] localVarHttpContentTypes = new String[] {
                "application/json; charset=utf-8"
            };
            String localVarHttpContentType = this.Configuration.ApiClient.SelectHeaderContentType(localVarHttpContentTypes);

            // to determine the Accept header
            String[] localVarHttpHeaderAccepts = new String[] {
                "application/json; charset=utf-8"
            };
            String localVarHttpHeaderAccept = this.Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            if (accessToken != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "access_token", accessToken)); // query parameter
            if (body != null && body.GetType() != typeof(byte[]))
            {
                localVarPostBody = this.Configuration.ApiClient.Serialize(body); // http body (model) parameter
            }
            else
            {
                localVarPostBody = body; // byte array
            }

            // authentication (OAuth2-Auth-Code) required
            // oauth required
            if (!String.IsNullOrEmpty(this.Configuration.AccessToken))
            {
                localVarHeaderParams["Authorization"] = "Bearer " + this.Configuration.AccessToken;
            }
            // authentication (OAuth2-Refresh-Token) required
            // oauth required
            if (!String.IsNullOrEmpty(this.Configuration.AccessToken))
            {
                localVarHeaderParams["Authorization"] = "Bearer " + this.Configuration.AccessToken;
            }

            // make the HTTP request
            IRestResponse localVarResponse = (IRestResponse) this.Configuration.ApiClient.CallApi(localVarPath,
                Method.POST, localVarQueryParams, localVarPostBody, localVarHeaderParams, localVarFormParams, localVarFileParams,
                localVarPathParams, localVarHttpContentType);

            int localVarStatusCode = (int) localVarResponse.StatusCode;

            if (ExceptionFactory != null)
            {
                Exception exception = ExceptionFactory("PostOrder", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<OrderResponseDefinition>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (OrderResponseDefinition) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(OrderResponseDefinition)));
        }

        /// <summary>
        /// Submit Order  Submits 1 or more orders 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="body"></param>
        /// <returns>Task of OrderResponseDefinition</returns>
        public async System.Threading.Tasks.Task<OrderResponseDefinition> PostOrderAsync (string accessToken, OrderRequestDefinition body)
        {
             ApiResponse<OrderResponseDefinition> localVarResponse = await PostOrderAsyncWithHttpInfo(accessToken, body);
             return localVarResponse.Data;

        }

        /// <summary>
        /// Submit Order  Submits 1 or more orders 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="body"></param>
        /// <returns>Task of ApiResponse (OrderResponseDefinition)</returns>
        public async System.Threading.Tasks.Task<ApiResponse<OrderResponseDefinition>> PostOrderAsyncWithHttpInfo (string accessToken, OrderRequestDefinition body)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling OrderExecutionApi->PostOrder");
            // verify the required parameter 'body' is set
            if (body == null)
                throw new ApiException(400, "Missing required parameter 'body' when calling OrderExecutionApi->PostOrder");

            var localVarPath = "/orders";
            var localVarPathParams = new Dictionary<String, String>();
            var localVarQueryParams = new List<KeyValuePair<String, String>>();
            var localVarHeaderParams = new Dictionary<String, String>(this.Configuration.DefaultHeader);
            var localVarFormParams = new Dictionary<String, String>();
            var localVarFileParams = new Dictionary<String, FileParameter>();
            Object localVarPostBody = null;

            // to determine the Content-Type header
            String[] localVarHttpContentTypes = new String[] {
                "application/json; charset=utf-8"
            };
            String localVarHttpContentType = this.Configuration.ApiClient.SelectHeaderContentType(localVarHttpContentTypes);

            // to determine the Accept header
            String[] localVarHttpHeaderAccepts = new String[] {
                "application/json; charset=utf-8"
            };
            String localVarHttpHeaderAccept = this.Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            if (accessToken != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "access_token", accessToken)); // query parameter
            if (body != null && body.GetType() != typeof(byte[]))
            {
                localVarPostBody = this.Configuration.ApiClient.Serialize(body); // http body (model) parameter
            }
            else
            {
                localVarPostBody = body; // byte array
            }

            // authentication (OAuth2-Auth-Code) required
            // oauth required
            if (!String.IsNullOrEmpty(this.Configuration.AccessToken))
            {
                localVarHeaderParams["Authorization"] = "Bearer " + this.Configuration.AccessToken;
            }
            // authentication (OAuth2-Refresh-Token) required
            // oauth required
            if (!String.IsNullOrEmpty(this.Configuration.AccessToken))
            {
                localVarHeaderParams["Authorization"] = "Bearer " + this.Configuration.AccessToken;
            }

            // make the HTTP request
            IRestResponse localVarResponse = (IRestResponse) await this.Configuration.ApiClient.CallApiAsync(localVarPath,
                Method.POST, localVarQueryParams, localVarPostBody, localVarHeaderParams, localVarFormParams, localVarFileParams,
                localVarPathParams, localVarHttpContentType);

            int localVarStatusCode = (int) localVarResponse.StatusCode;

            if (ExceptionFactory != null)
            {
                Exception exception = ExceptionFactory("PostOrder", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<OrderResponseDefinition>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (OrderResponseDefinition) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(OrderResponseDefinition)));
        }

        /// <summary>
        /// Confirm Order  Returns estimated cost and commission information for an order without the order actually being placed. The fields that are returned in the response depend on the order type. The following shows the different fields that will be returned.  **Base Confirmation**  (All confirmations will have these fields) * Route * Duration * Account * SummaryMessage * OrderConfirmId  **Equity Confirmation** (Base Confirmation fields + the following) * EstimatedPrice * EstimatedPriceDisplay * EstimatedCost * EstimatedCostDisplay * EstimatedCommission * EstimatedCommissionDisplay * DebitCreditEstimatedCost * DebitCreditEstimatedCostDisplay  **Forex Confirmation** (Base Confirmation fields + the following) * BaseCurrency * CounterCurrency * InitialMarginDisplay  **Futures Confirmation** (Base Confirmation fields + the following) * ProductCurrency * AccountCurrency * EstimatedCost * EstimatedPrice * EstimatedPriceDisplay * InitialMarginDisplay * EstimatedCommission * EstimatedCommissionDisplay 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="body"></param>
        /// <returns>OrderConfirmResponseDefinition</returns>
        public OrderConfirmResponseDefinition PostOrderConfirm (string accessToken, OrderConfirmRequestDefinition body)
        {
             ApiResponse<OrderConfirmResponseDefinition> localVarResponse = PostOrderConfirmWithHttpInfo(accessToken, body);
             return localVarResponse.Data;
        }

        /// <summary>
        /// Confirm Order  Returns estimated cost and commission information for an order without the order actually being placed. The fields that are returned in the response depend on the order type. The following shows the different fields that will be returned.  **Base Confirmation**  (All confirmations will have these fields) * Route * Duration * Account * SummaryMessage * OrderConfirmId  **Equity Confirmation** (Base Confirmation fields + the following) * EstimatedPrice * EstimatedPriceDisplay * EstimatedCost * EstimatedCostDisplay * EstimatedCommission * EstimatedCommissionDisplay * DebitCreditEstimatedCost * DebitCreditEstimatedCostDisplay  **Forex Confirmation** (Base Confirmation fields + the following) * BaseCurrency * CounterCurrency * InitialMarginDisplay  **Futures Confirmation** (Base Confirmation fields + the following) * ProductCurrency * AccountCurrency * EstimatedCost * EstimatedPrice * EstimatedPriceDisplay * InitialMarginDisplay * EstimatedCommission * EstimatedCommissionDisplay 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="body"></param>
        /// <returns>ApiResponse of OrderConfirmResponseDefinition</returns>
        public ApiResponse< OrderConfirmResponseDefinition > PostOrderConfirmWithHttpInfo (string accessToken, OrderConfirmRequestDefinition body)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling OrderExecutionApi->PostOrderConfirm");
            // verify the required parameter 'body' is set
            if (body == null)
                throw new ApiException(400, "Missing required parameter 'body' when calling OrderExecutionApi->PostOrderConfirm");

            var localVarPath = "/orders/confirm";
            var localVarPathParams = new Dictionary<String, String>();
            var localVarQueryParams = new List<KeyValuePair<String, String>>();
            var localVarHeaderParams = new Dictionary<String, String>(this.Configuration.DefaultHeader);
            var localVarFormParams = new Dictionary<String, String>();
            var localVarFileParams = new Dictionary<String, FileParameter>();
            Object localVarPostBody = null;

            // to determine the Content-Type header
            String[] localVarHttpContentTypes = new String[] {
                "application/json; charset=utf-8"
            };
            String localVarHttpContentType = this.Configuration.ApiClient.SelectHeaderContentType(localVarHttpContentTypes);

            // to determine the Accept header
            String[] localVarHttpHeaderAccepts = new String[] {
                "application/json; charset=utf-8"
            };
            String localVarHttpHeaderAccept = this.Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            if (accessToken != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "access_token", accessToken)); // query parameter
            if (body != null && body.GetType() != typeof(byte[]))
            {
                localVarPostBody = this.Configuration.ApiClient.Serialize(body); // http body (model) parameter
            }
            else
            {
                localVarPostBody = body; // byte array
            }

            // authentication (OAuth2-Auth-Code) required
            // oauth required
            if (!String.IsNullOrEmpty(this.Configuration.AccessToken))
            {
                localVarHeaderParams["Authorization"] = "Bearer " + this.Configuration.AccessToken;
            }
            // authentication (OAuth2-Refresh-Token) required
            // oauth required
            if (!String.IsNullOrEmpty(this.Configuration.AccessToken))
            {
                localVarHeaderParams["Authorization"] = "Bearer " + this.Configuration.AccessToken;
            }

            // make the HTTP request
            IRestResponse localVarResponse = (IRestResponse) this.Configuration.ApiClient.CallApi(localVarPath,
                Method.POST, localVarQueryParams, localVarPostBody, localVarHeaderParams, localVarFormParams, localVarFileParams,
                localVarPathParams, localVarHttpContentType);

            int localVarStatusCode = (int) localVarResponse.StatusCode;

            if (ExceptionFactory != null)
            {
                Exception exception = ExceptionFactory("PostOrderConfirm", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<OrderConfirmResponseDefinition>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (OrderConfirmResponseDefinition) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(OrderConfirmResponseDefinition)));
        }

        /// <summary>
        /// Confirm Order  Returns estimated cost and commission information for an order without the order actually being placed. The fields that are returned in the response depend on the order type. The following shows the different fields that will be returned.  **Base Confirmation**  (All confirmations will have these fields) * Route * Duration * Account * SummaryMessage * OrderConfirmId  **Equity Confirmation** (Base Confirmation fields + the following) * EstimatedPrice * EstimatedPriceDisplay * EstimatedCost * EstimatedCostDisplay * EstimatedCommission * EstimatedCommissionDisplay * DebitCreditEstimatedCost * DebitCreditEstimatedCostDisplay  **Forex Confirmation** (Base Confirmation fields + the following) * BaseCurrency * CounterCurrency * InitialMarginDisplay  **Futures Confirmation** (Base Confirmation fields + the following) * ProductCurrency * AccountCurrency * EstimatedCost * EstimatedPrice * EstimatedPriceDisplay * InitialMarginDisplay * EstimatedCommission * EstimatedCommissionDisplay 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="body"></param>
        /// <returns>Task of OrderConfirmResponseDefinition</returns>
        public async System.Threading.Tasks.Task<OrderConfirmResponseDefinition> PostOrderConfirmAsync (string accessToken, OrderConfirmRequestDefinition body)
        {
             ApiResponse<OrderConfirmResponseDefinition> localVarResponse = await PostOrderConfirmAsyncWithHttpInfo(accessToken, body);
             return localVarResponse.Data;

        }

        /// <summary>
        /// Confirm Order  Returns estimated cost and commission information for an order without the order actually being placed. The fields that are returned in the response depend on the order type. The following shows the different fields that will be returned.  **Base Confirmation**  (All confirmations will have these fields) * Route * Duration * Account * SummaryMessage * OrderConfirmId  **Equity Confirmation** (Base Confirmation fields + the following) * EstimatedPrice * EstimatedPriceDisplay * EstimatedCost * EstimatedCostDisplay * EstimatedCommission * EstimatedCommissionDisplay * DebitCreditEstimatedCost * DebitCreditEstimatedCostDisplay  **Forex Confirmation** (Base Confirmation fields + the following) * BaseCurrency * CounterCurrency * InitialMarginDisplay  **Futures Confirmation** (Base Confirmation fields + the following) * ProductCurrency * AccountCurrency * EstimatedCost * EstimatedPrice * EstimatedPriceDisplay * InitialMarginDisplay * EstimatedCommission * EstimatedCommissionDisplay 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="body"></param>
        /// <returns>Task of ApiResponse (OrderConfirmResponseDefinition)</returns>
        public async System.Threading.Tasks.Task<ApiResponse<OrderConfirmResponseDefinition>> PostOrderConfirmAsyncWithHttpInfo (string accessToken, OrderConfirmRequestDefinition body)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling OrderExecutionApi->PostOrderConfirm");
            // verify the required parameter 'body' is set
            if (body == null)
                throw new ApiException(400, "Missing required parameter 'body' when calling OrderExecutionApi->PostOrderConfirm");

            var localVarPath = "/orders/confirm";
            var localVarPathParams = new Dictionary<String, String>();
            var localVarQueryParams = new List<KeyValuePair<String, String>>();
            var localVarHeaderParams = new Dictionary<String, String>(this.Configuration.DefaultHeader);
            var localVarFormParams = new Dictionary<String, String>();
            var localVarFileParams = new Dictionary<String, FileParameter>();
            Object localVarPostBody = null;

            // to determine the Content-Type header
            String[] localVarHttpContentTypes = new String[] {
                "application/json; charset=utf-8"
            };
            String localVarHttpContentType = this.Configuration.ApiClient.SelectHeaderContentType(localVarHttpContentTypes);

            // to determine the Accept header
            String[] localVarHttpHeaderAccepts = new String[] {
                "application/json; charset=utf-8"
            };
            String localVarHttpHeaderAccept = this.Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            if (accessToken != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "access_token", accessToken)); // query parameter
            if (body != null && body.GetType() != typeof(byte[]))
            {
                localVarPostBody = this.Configuration.ApiClient.Serialize(body); // http body (model) parameter
            }
            else
            {
                localVarPostBody = body; // byte array
            }

            // authentication (OAuth2-Auth-Code) required
            // oauth required
            if (!String.IsNullOrEmpty(this.Configuration.AccessToken))
            {
                localVarHeaderParams["Authorization"] = "Bearer " + this.Configuration.AccessToken;
            }
            // authentication (OAuth2-Refresh-Token) required
            // oauth required
            if (!String.IsNullOrEmpty(this.Configuration.AccessToken))
            {
                localVarHeaderParams["Authorization"] = "Bearer " + this.Configuration.AccessToken;
            }

            // make the HTTP request
            IRestResponse localVarResponse = (IRestResponse) await this.Configuration.ApiClient.CallApiAsync(localVarPath,
                Method.POST, localVarQueryParams, localVarPostBody, localVarHeaderParams, localVarFormParams, localVarFileParams,
                localVarPathParams, localVarHttpContentType);

            int localVarStatusCode = (int) localVarResponse.StatusCode;

            if (ExceptionFactory != null)
            {
                Exception exception = ExceptionFactory("PostOrderConfirm", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<OrderConfirmResponseDefinition>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (OrderConfirmResponseDefinition) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(OrderConfirmResponseDefinition)));
        }

        /// <summary>
        /// Submit Group Order  Submits a group order such as Bracket and OCO Orders.  #### Order Cancels Order (OCO) An OCO order is a group of orders whereby if one of the orders is filled or partially-filled, then all of the other orders in the group are cancelled.  #### Bracket OCO Orders A bracket order is a special instance of an OCO (Order Cancel Order). Bracket orders are used to exit an existing position. They are designed to limit loss and lock in profit by “bracketing” an order with a simultaneous stop and limit order.  Bracket orders are limited so that the orders are all for the same symbol and are on the same side of the market (either all to sell or all to cover), and they are restricted to closing transactions.  The reason that they follow these rules is because the orders need to be able to auto decrement when a partial fill occurs with one of the orders. For example, if the customer has a sell limit order for 1000 shares and a sell stop order for 1000 shares, and the limit order is partially filled for 500 shares, then the customer would want the stop to remain open, but it should automatically decrement the order to 500 shares to match the remaining open position.  ### Errors When a submitted order fails, it can be seen in two different ways: - Immediately rejected during submit to the Orders API with a 400 HTTP status code. - Accepted by the API with a 200 HTTP status code, but you will see the order with a status of “REJ” (rejected) when you fetch the Order from the /v2/accounts/{id}/orders API  #### NOTE When a group order is submitted, the order execution system treats each sibling order as an individual order. Thus, the system does not validate that each order has the same Quantity, and currently it is not able to update a bracket order as one transaction, instead you must update each order within a bracket.  In order to prevent errors, please validate the data on the client side. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="body"></param>
        /// <returns>GroupOrderResponseDefinition</returns>
        public GroupOrderResponseDefinition PostOrderGroup (string accessToken, GroupOrderRequestDefinition body)
        {
             ApiResponse<GroupOrderResponseDefinition> localVarResponse = PostOrderGroupWithHttpInfo(accessToken, body);
             return localVarResponse.Data;
        }

        /// <summary>
        /// Submit Group Order  Submits a group order such as Bracket and OCO Orders.  #### Order Cancels Order (OCO) An OCO order is a group of orders whereby if one of the orders is filled or partially-filled, then all of the other orders in the group are cancelled.  #### Bracket OCO Orders A bracket order is a special instance of an OCO (Order Cancel Order). Bracket orders are used to exit an existing position. They are designed to limit loss and lock in profit by “bracketing” an order with a simultaneous stop and limit order.  Bracket orders are limited so that the orders are all for the same symbol and are on the same side of the market (either all to sell or all to cover), and they are restricted to closing transactions.  The reason that they follow these rules is because the orders need to be able to auto decrement when a partial fill occurs with one of the orders. For example, if the customer has a sell limit order for 1000 shares and a sell stop order for 1000 shares, and the limit order is partially filled for 500 shares, then the customer would want the stop to remain open, but it should automatically decrement the order to 500 shares to match the remaining open position.  ### Errors When a submitted order fails, it can be seen in two different ways: - Immediately rejected during submit to the Orders API with a 400 HTTP status code. - Accepted by the API with a 200 HTTP status code, but you will see the order with a status of “REJ” (rejected) when you fetch the Order from the /v2/accounts/{id}/orders API  #### NOTE When a group order is submitted, the order execution system treats each sibling order as an individual order. Thus, the system does not validate that each order has the same Quantity, and currently it is not able to update a bracket order as one transaction, instead you must update each order within a bracket.  In order to prevent errors, please validate the data on the client side. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="body"></param>
        /// <returns>ApiResponse of GroupOrderResponseDefinition</returns>
        public ApiResponse< GroupOrderResponseDefinition > PostOrderGroupWithHttpInfo (string accessToken, GroupOrderRequestDefinition body)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling OrderExecutionApi->PostOrderGroup");
            // verify the required parameter 'body' is set
            if (body == null)
                throw new ApiException(400, "Missing required parameter 'body' when calling OrderExecutionApi->PostOrderGroup");

            var localVarPath = "/orders/groups";
            var localVarPathParams = new Dictionary<String, String>();
            var localVarQueryParams = new List<KeyValuePair<String, String>>();
            var localVarHeaderParams = new Dictionary<String, String>(this.Configuration.DefaultHeader);
            var localVarFormParams = new Dictionary<String, String>();
            var localVarFileParams = new Dictionary<String, FileParameter>();
            Object localVarPostBody = null;

            // to determine the Content-Type header
            String[] localVarHttpContentTypes = new String[] {
                "application/json; charset=utf-8"
            };
            String localVarHttpContentType = this.Configuration.ApiClient.SelectHeaderContentType(localVarHttpContentTypes);

            // to determine the Accept header
            String[] localVarHttpHeaderAccepts = new String[] {
                "application/json; charset=utf-8"
            };
            String localVarHttpHeaderAccept = this.Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            if (accessToken != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "access_token", accessToken)); // query parameter
            if (body != null && body.GetType() != typeof(byte[]))
            {
                localVarPostBody = this.Configuration.ApiClient.Serialize(body); // http body (model) parameter
            }
            else
            {
                localVarPostBody = body; // byte array
            }

            // authentication (OAuth2-Auth-Code) required
            // oauth required
            if (!String.IsNullOrEmpty(this.Configuration.AccessToken))
            {
                localVarHeaderParams["Authorization"] = "Bearer " + this.Configuration.AccessToken;
            }
            // authentication (OAuth2-Refresh-Token) required
            // oauth required
            if (!String.IsNullOrEmpty(this.Configuration.AccessToken))
            {
                localVarHeaderParams["Authorization"] = "Bearer " + this.Configuration.AccessToken;
            }

            // make the HTTP request
            IRestResponse localVarResponse = (IRestResponse) this.Configuration.ApiClient.CallApi(localVarPath,
                Method.POST, localVarQueryParams, localVarPostBody, localVarHeaderParams, localVarFormParams, localVarFileParams,
                localVarPathParams, localVarHttpContentType);

            int localVarStatusCode = (int) localVarResponse.StatusCode;

            if (ExceptionFactory != null)
            {
                Exception exception = ExceptionFactory("PostOrderGroup", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<GroupOrderResponseDefinition>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (GroupOrderResponseDefinition) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(GroupOrderResponseDefinition)));
        }

        /// <summary>
        /// Submit Group Order  Submits a group order such as Bracket and OCO Orders.  #### Order Cancels Order (OCO) An OCO order is a group of orders whereby if one of the orders is filled or partially-filled, then all of the other orders in the group are cancelled.  #### Bracket OCO Orders A bracket order is a special instance of an OCO (Order Cancel Order). Bracket orders are used to exit an existing position. They are designed to limit loss and lock in profit by “bracketing” an order with a simultaneous stop and limit order.  Bracket orders are limited so that the orders are all for the same symbol and are on the same side of the market (either all to sell or all to cover), and they are restricted to closing transactions.  The reason that they follow these rules is because the orders need to be able to auto decrement when a partial fill occurs with one of the orders. For example, if the customer has a sell limit order for 1000 shares and a sell stop order for 1000 shares, and the limit order is partially filled for 500 shares, then the customer would want the stop to remain open, but it should automatically decrement the order to 500 shares to match the remaining open position.  ### Errors When a submitted order fails, it can be seen in two different ways: - Immediately rejected during submit to the Orders API with a 400 HTTP status code. - Accepted by the API with a 200 HTTP status code, but you will see the order with a status of “REJ” (rejected) when you fetch the Order from the /v2/accounts/{id}/orders API  #### NOTE When a group order is submitted, the order execution system treats each sibling order as an individual order. Thus, the system does not validate that each order has the same Quantity, and currently it is not able to update a bracket order as one transaction, instead you must update each order within a bracket.  In order to prevent errors, please validate the data on the client side. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="body"></param>
        /// <returns>Task of GroupOrderResponseDefinition</returns>
        public async System.Threading.Tasks.Task<GroupOrderResponseDefinition> PostOrderGroupAsync (string accessToken, GroupOrderRequestDefinition body)
        {
             ApiResponse<GroupOrderResponseDefinition> localVarResponse = await PostOrderGroupAsyncWithHttpInfo(accessToken, body);
             return localVarResponse.Data;

        }

        /// <summary>
        /// Submit Group Order  Submits a group order such as Bracket and OCO Orders.  #### Order Cancels Order (OCO) An OCO order is a group of orders whereby if one of the orders is filled or partially-filled, then all of the other orders in the group are cancelled.  #### Bracket OCO Orders A bracket order is a special instance of an OCO (Order Cancel Order). Bracket orders are used to exit an existing position. They are designed to limit loss and lock in profit by “bracketing” an order with a simultaneous stop and limit order.  Bracket orders are limited so that the orders are all for the same symbol and are on the same side of the market (either all to sell or all to cover), and they are restricted to closing transactions.  The reason that they follow these rules is because the orders need to be able to auto decrement when a partial fill occurs with one of the orders. For example, if the customer has a sell limit order for 1000 shares and a sell stop order for 1000 shares, and the limit order is partially filled for 500 shares, then the customer would want the stop to remain open, but it should automatically decrement the order to 500 shares to match the remaining open position.  ### Errors When a submitted order fails, it can be seen in two different ways: - Immediately rejected during submit to the Orders API with a 400 HTTP status code. - Accepted by the API with a 200 HTTP status code, but you will see the order with a status of “REJ” (rejected) when you fetch the Order from the /v2/accounts/{id}/orders API  #### NOTE When a group order is submitted, the order execution system treats each sibling order as an individual order. Thus, the system does not validate that each order has the same Quantity, and currently it is not able to update a bracket order as one transaction, instead you must update each order within a bracket.  In order to prevent errors, please validate the data on the client side. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="body"></param>
        /// <returns>Task of ApiResponse (GroupOrderResponseDefinition)</returns>
        public async System.Threading.Tasks.Task<ApiResponse<GroupOrderResponseDefinition>> PostOrderGroupAsyncWithHttpInfo (string accessToken, GroupOrderRequestDefinition body)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling OrderExecutionApi->PostOrderGroup");
            // verify the required parameter 'body' is set
            if (body == null)
                throw new ApiException(400, "Missing required parameter 'body' when calling OrderExecutionApi->PostOrderGroup");

            var localVarPath = "/orders/groups";
            var localVarPathParams = new Dictionary<String, String>();
            var localVarQueryParams = new List<KeyValuePair<String, String>>();
            var localVarHeaderParams = new Dictionary<String, String>(this.Configuration.DefaultHeader);
            var localVarFormParams = new Dictionary<String, String>();
            var localVarFileParams = new Dictionary<String, FileParameter>();
            Object localVarPostBody = null;

            // to determine the Content-Type header
            String[] localVarHttpContentTypes = new String[] {
                "application/json; charset=utf-8"
            };
            String localVarHttpContentType = this.Configuration.ApiClient.SelectHeaderContentType(localVarHttpContentTypes);

            // to determine the Accept header
            String[] localVarHttpHeaderAccepts = new String[] {
                "application/json; charset=utf-8"
            };
            String localVarHttpHeaderAccept = this.Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            if (accessToken != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "access_token", accessToken)); // query parameter
            if (body != null && body.GetType() != typeof(byte[]))
            {
                localVarPostBody = this.Configuration.ApiClient.Serialize(body); // http body (model) parameter
            }
            else
            {
                localVarPostBody = body; // byte array
            }

            // authentication (OAuth2-Auth-Code) required
            // oauth required
            if (!String.IsNullOrEmpty(this.Configuration.AccessToken))
            {
                localVarHeaderParams["Authorization"] = "Bearer " + this.Configuration.AccessToken;
            }
            // authentication (OAuth2-Refresh-Token) required
            // oauth required
            if (!String.IsNullOrEmpty(this.Configuration.AccessToken))
            {
                localVarHeaderParams["Authorization"] = "Bearer " + this.Configuration.AccessToken;
            }

            // make the HTTP request
            IRestResponse localVarResponse = (IRestResponse) await this.Configuration.ApiClient.CallApiAsync(localVarPath,
                Method.POST, localVarQueryParams, localVarPostBody, localVarHeaderParams, localVarFormParams, localVarFileParams,
                localVarPathParams, localVarHttpContentType);

            int localVarStatusCode = (int) localVarResponse.StatusCode;

            if (ExceptionFactory != null)
            {
                Exception exception = ExceptionFactory("PostOrderGroup", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<GroupOrderResponseDefinition>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (GroupOrderResponseDefinition) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(GroupOrderResponseDefinition)));
        }

        /// <summary>
        /// Confirm Group Order  Returns estimated cost and commission information for a group of orders (OCO, BRK) without the orders actually being placed  **Base Confirmation**  (All confirmations will have these fields) * Route * Duration * Account * SummaryMessage * OrderConfirmId  **Equity Confirmation** (Base Confirmation fields + the following) * EstimatedPrice * EstimatedPriceDisplay * EstimatedCost * EstimatedCostDisplay * EstimatedCommission * EstimatedCommissionDisplay  **Forex Confirmation** (Base Confirmation fields + the following) * BaseCurrency * CounterCurrency * InitialMarginDisplay  **Futures Confirmation** (Base Confirmation fields + the following) * ProductCurrency * AccountCurrency * EstimatedCost * EstimatedPrice * EstimatedPriceDisplay * InitialMarginDisplay * EstimatedCommission * EstimatedCommissionDisplay 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="body"></param>
        /// <returns>GroupOrderConfirmResponseDefinition</returns>
        public GroupOrderConfirmResponseDefinition PostOrderGroupsConfirm (string accessToken, GroupOrderConfirmRequestDefinition body)
        {
             ApiResponse<GroupOrderConfirmResponseDefinition> localVarResponse = PostOrderGroupsConfirmWithHttpInfo(accessToken, body);
             return localVarResponse.Data;
        }

        /// <summary>
        /// Confirm Group Order  Returns estimated cost and commission information for a group of orders (OCO, BRK) without the orders actually being placed  **Base Confirmation**  (All confirmations will have these fields) * Route * Duration * Account * SummaryMessage * OrderConfirmId  **Equity Confirmation** (Base Confirmation fields + the following) * EstimatedPrice * EstimatedPriceDisplay * EstimatedCost * EstimatedCostDisplay * EstimatedCommission * EstimatedCommissionDisplay  **Forex Confirmation** (Base Confirmation fields + the following) * BaseCurrency * CounterCurrency * InitialMarginDisplay  **Futures Confirmation** (Base Confirmation fields + the following) * ProductCurrency * AccountCurrency * EstimatedCost * EstimatedPrice * EstimatedPriceDisplay * InitialMarginDisplay * EstimatedCommission * EstimatedCommissionDisplay 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="body"></param>
        /// <returns>ApiResponse of GroupOrderConfirmResponseDefinition</returns>
        public ApiResponse< GroupOrderConfirmResponseDefinition > PostOrderGroupsConfirmWithHttpInfo (string accessToken, GroupOrderConfirmRequestDefinition body)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling OrderExecutionApi->PostOrderGroupsConfirm");
            // verify the required parameter 'body' is set
            if (body == null)
                throw new ApiException(400, "Missing required parameter 'body' when calling OrderExecutionApi->PostOrderGroupsConfirm");

            var localVarPath = "/orders/groups/confirm";
            var localVarPathParams = new Dictionary<String, String>();
            var localVarQueryParams = new List<KeyValuePair<String, String>>();
            var localVarHeaderParams = new Dictionary<String, String>(this.Configuration.DefaultHeader);
            var localVarFormParams = new Dictionary<String, String>();
            var localVarFileParams = new Dictionary<String, FileParameter>();
            Object localVarPostBody = null;

            // to determine the Content-Type header
            String[] localVarHttpContentTypes = new String[] {
                "application/json; charset=utf-8"
            };
            String localVarHttpContentType = this.Configuration.ApiClient.SelectHeaderContentType(localVarHttpContentTypes);

            // to determine the Accept header
            String[] localVarHttpHeaderAccepts = new String[] {
                "application/json; charset=utf-8"
            };
            String localVarHttpHeaderAccept = this.Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            if (accessToken != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "access_token", accessToken)); // query parameter
            if (body != null && body.GetType() != typeof(byte[]))
            {
                localVarPostBody = this.Configuration.ApiClient.Serialize(body); // http body (model) parameter
            }
            else
            {
                localVarPostBody = body; // byte array
            }

            // authentication (OAuth2-Auth-Code) required
            // oauth required
            if (!String.IsNullOrEmpty(this.Configuration.AccessToken))
            {
                localVarHeaderParams["Authorization"] = "Bearer " + this.Configuration.AccessToken;
            }
            // authentication (OAuth2-Refresh-Token) required
            // oauth required
            if (!String.IsNullOrEmpty(this.Configuration.AccessToken))
            {
                localVarHeaderParams["Authorization"] = "Bearer " + this.Configuration.AccessToken;
            }

            // make the HTTP request
            IRestResponse localVarResponse = (IRestResponse) this.Configuration.ApiClient.CallApi(localVarPath,
                Method.POST, localVarQueryParams, localVarPostBody, localVarHeaderParams, localVarFormParams, localVarFileParams,
                localVarPathParams, localVarHttpContentType);

            int localVarStatusCode = (int) localVarResponse.StatusCode;

            if (ExceptionFactory != null)
            {
                Exception exception = ExceptionFactory("PostOrderGroupsConfirm", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<GroupOrderConfirmResponseDefinition>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (GroupOrderConfirmResponseDefinition) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(GroupOrderConfirmResponseDefinition)));
        }

        /// <summary>
        /// Confirm Group Order  Returns estimated cost and commission information for a group of orders (OCO, BRK) without the orders actually being placed  **Base Confirmation**  (All confirmations will have these fields) * Route * Duration * Account * SummaryMessage * OrderConfirmId  **Equity Confirmation** (Base Confirmation fields + the following) * EstimatedPrice * EstimatedPriceDisplay * EstimatedCost * EstimatedCostDisplay * EstimatedCommission * EstimatedCommissionDisplay  **Forex Confirmation** (Base Confirmation fields + the following) * BaseCurrency * CounterCurrency * InitialMarginDisplay  **Futures Confirmation** (Base Confirmation fields + the following) * ProductCurrency * AccountCurrency * EstimatedCost * EstimatedPrice * EstimatedPriceDisplay * InitialMarginDisplay * EstimatedCommission * EstimatedCommissionDisplay 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="body"></param>
        /// <returns>Task of GroupOrderConfirmResponseDefinition</returns>
        public async System.Threading.Tasks.Task<GroupOrderConfirmResponseDefinition> PostOrderGroupsConfirmAsync (string accessToken, GroupOrderConfirmRequestDefinition body)
        {
             ApiResponse<GroupOrderConfirmResponseDefinition> localVarResponse = await PostOrderGroupsConfirmAsyncWithHttpInfo(accessToken, body);
             return localVarResponse.Data;

        }

        /// <summary>
        /// Confirm Group Order  Returns estimated cost and commission information for a group of orders (OCO, BRK) without the orders actually being placed  **Base Confirmation**  (All confirmations will have these fields) * Route * Duration * Account * SummaryMessage * OrderConfirmId  **Equity Confirmation** (Base Confirmation fields + the following) * EstimatedPrice * EstimatedPriceDisplay * EstimatedCost * EstimatedCostDisplay * EstimatedCommission * EstimatedCommissionDisplay  **Forex Confirmation** (Base Confirmation fields + the following) * BaseCurrency * CounterCurrency * InitialMarginDisplay  **Futures Confirmation** (Base Confirmation fields + the following) * ProductCurrency * AccountCurrency * EstimatedCost * EstimatedPrice * EstimatedPriceDisplay * InitialMarginDisplay * EstimatedCommission * EstimatedCommissionDisplay 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="body"></param>
        /// <returns>Task of ApiResponse (GroupOrderConfirmResponseDefinition)</returns>
        public async System.Threading.Tasks.Task<ApiResponse<GroupOrderConfirmResponseDefinition>> PostOrderGroupsConfirmAsyncWithHttpInfo (string accessToken, GroupOrderConfirmRequestDefinition body)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling OrderExecutionApi->PostOrderGroupsConfirm");
            // verify the required parameter 'body' is set
            if (body == null)
                throw new ApiException(400, "Missing required parameter 'body' when calling OrderExecutionApi->PostOrderGroupsConfirm");

            var localVarPath = "/orders/groups/confirm";
            var localVarPathParams = new Dictionary<String, String>();
            var localVarQueryParams = new List<KeyValuePair<String, String>>();
            var localVarHeaderParams = new Dictionary<String, String>(this.Configuration.DefaultHeader);
            var localVarFormParams = new Dictionary<String, String>();
            var localVarFileParams = new Dictionary<String, FileParameter>();
            Object localVarPostBody = null;

            // to determine the Content-Type header
            String[] localVarHttpContentTypes = new String[] {
                "application/json; charset=utf-8"
            };
            String localVarHttpContentType = this.Configuration.ApiClient.SelectHeaderContentType(localVarHttpContentTypes);

            // to determine the Accept header
            String[] localVarHttpHeaderAccepts = new String[] {
                "application/json; charset=utf-8"
            };
            String localVarHttpHeaderAccept = this.Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            if (accessToken != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "access_token", accessToken)); // query parameter
            if (body != null && body.GetType() != typeof(byte[]))
            {
                localVarPostBody = this.Configuration.ApiClient.Serialize(body); // http body (model) parameter
            }
            else
            {
                localVarPostBody = body; // byte array
            }

            // authentication (OAuth2-Auth-Code) required
            // oauth required
            if (!String.IsNullOrEmpty(this.Configuration.AccessToken))
            {
                localVarHeaderParams["Authorization"] = "Bearer " + this.Configuration.AccessToken;
            }
            // authentication (OAuth2-Refresh-Token) required
            // oauth required
            if (!String.IsNullOrEmpty(this.Configuration.AccessToken))
            {
                localVarHeaderParams["Authorization"] = "Bearer " + this.Configuration.AccessToken;
            }

            // make the HTTP request
            IRestResponse localVarResponse = (IRestResponse) await this.Configuration.ApiClient.CallApiAsync(localVarPath,
                Method.POST, localVarQueryParams, localVarPostBody, localVarHeaderParams, localVarFormParams, localVarFileParams,
                localVarPathParams, localVarHttpContentType);

            int localVarStatusCode = (int) localVarResponse.StatusCode;

            if (ExceptionFactory != null)
            {
                Exception exception = ExceptionFactory("PostOrderGroupsConfirm", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<GroupOrderConfirmResponseDefinition>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (GroupOrderConfirmResponseDefinition) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(GroupOrderConfirmResponseDefinition)));
        }

    }
}
