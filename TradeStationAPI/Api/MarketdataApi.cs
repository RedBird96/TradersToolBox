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
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp;
using TradeStationAPI.Client;
using TradeStationAPI.Model;

namespace TradeStationAPI.Api
{
    public delegate void BarchartReceivedHandler(string symbol, BarchartDefinition bar, object obj, Exception ex);

    /// <summary>
    /// Represents a collection of functions to interact with the API endpoints
    /// </summary>
    public interface IMarketdataApi : IApiAccessor
    {
        #region Synchronous Operations
        /// <summary>
        /// Get Quote 
        /// </summary>
        /// <remarks>
        /// Gets the latest Level 1 Quote for the given Symbol 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbols">1 or more Symbol Names (comma-separated)</param>
        /// <returns>QuoteDefinition</returns>
        QuoteDefinition GetQuotes (string accessToken, string symbols);

        /// <summary>
        /// Get Quote 
        /// </summary>
        /// <remarks>
        /// Gets the latest Level 1 Quote for the given Symbol 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbols">1 or more Symbol Names (comma-separated)</param>
        /// <returns>ApiResponse of QuoteDefinition</returns>
        ApiResponse<QuoteDefinition> GetQuotesWithHttpInfo (string accessToken, string symbols);
        /// <summary>
        /// Get Symbol Info 
        /// </summary>
        /// <remarks>
        /// Finds the given symbol and returns a collection of fields describing the symbol, its origin exchange, and other pertinant information. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">Symbol to lookup</param>
        /// <returns>SymbolDefinition</returns>
        SymbolDefinition GetSymbol (string accessToken, string symbol);

        /// <summary>
        /// Get Symbol Info 
        /// </summary>
        /// <remarks>
        /// Finds the given symbol and returns a collection of fields describing the symbol, its origin exchange, and other pertinant information. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">Symbol to lookup</param>
        /// <returns>ApiResponse of SymbolDefinition</returns>
        ApiResponse<SymbolDefinition> GetSymbolWithHttpInfo (string accessToken, string symbol);
        /// <summary>
        /// Get Symbol List 
        /// </summary>
        /// <remarks>
        /// Gets a specific Symbol List 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbolListId">A valid Symbol List ID</param>
        /// <returns>SymbolListDefinition</returns>
        SymbolListDefinition GetSymbolListByID (string accessToken, string symbolListId);

        /// <summary>
        /// Get Symbol List 
        /// </summary>
        /// <remarks>
        /// Gets a specific Symbol List 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbolListId">A valid Symbol List ID</param>
        /// <returns>ApiResponse of SymbolListDefinition</returns>
        ApiResponse<SymbolListDefinition> GetSymbolListByIDWithHttpInfo (string accessToken, string symbolListId);
        /// <summary>
        /// Get Symbols in a Symbol List 
        /// </summary>
        /// <remarks>
        /// Gets the Symbols for a specific Symbol List 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbolListId">A valid Symbol List ID</param>
        /// <returns>SymbolListSymbolsDefinition</returns>
        SymbolListSymbolsDefinition GetSymbolListSymbolsByID (string accessToken, string symbolListId);

        /// <summary>
        /// Get Symbols in a Symbol List 
        /// </summary>
        /// <remarks>
        /// Gets the Symbols for a specific Symbol List 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbolListId">A valid Symbol List ID</param>
        /// <returns>ApiResponse of SymbolListSymbolsDefinition</returns>
        ApiResponse<SymbolListSymbolsDefinition> GetSymbolListSymbolsByIDWithHttpInfo (string accessToken, string symbolListId);
        /// <summary>
        /// Get all Symbol Lists 
        /// </summary>
        /// <remarks>
        /// Gets a list of all Symbol Lists 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <returns>SymbolListsDefinition</returns>
        SymbolListsDefinition GetSymbolLists (string accessToken);

        /// <summary>
        /// Get all Symbol Lists 
        /// </summary>
        /// <remarks>
        /// Gets a list of all Symbol Lists 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <returns>ApiResponse of SymbolListsDefinition</returns>
        ApiResponse<SymbolListsDefinition> GetSymbolListsWithHttpInfo (string accessToken);
        /// <summary>
        /// Search for Symbols 
        /// </summary>
        /// <remarks>
        /// Searches symbols based upon input criteria including Name, Category and Country. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="criteria">Criteria are represented as Key/value pairs (&#x60;&amp;&#x60; separated):  &#x60;N&#x60;: Name of Symbol. (Optional)  &#x60;C&#x60;: Asset categories. (Optional) Possible values:   - &#x60;Future&#x60; or &#x60;FU&#x60;   - &#x60;FutureOption&#x60; or &#x60;FO&#x60;   - &#x60;Stock&#x60; or &#x60;S&#x60; (Default)   - &#x60;StockOption&#x60; or &#x60;SO&#x60; (If root is specified, default category)   - &#x60;Index&#x60; or &#x60;IDX&#x60;   - &#x60;CurrencyOption&#x60; or &#x60;CO&#x60;   - &#x60;MutualFund&#x60; or &#x60;MF&#x60;   - &#x60;MoneyMarketFund&#x60; or &#x60;MMF&#x60;   - &#x60;IndexOption&#x60; or &#x60;IO&#x60;   - &#x60;Bond&#x60; or &#x60;B&#x60;   - &#x60;Forex&#x60; or &#x60;FX&#x60;  &#x60;Cnt&#x60;: Country where the symbol is traded in. (Optional) Possible values:   - &#x60;ALL&#x60; if not presented (Default)   - &#x60;US&#x60;   - &#x60;DE&#x60;   - &#x60;CA&#x60;  #### For Equities Lookups:  &#x60;N&#x60;: partial/full symbol name, will return all symbols that contain the provided name value  &#x60;Desc&#x60;: Name of the company  &#x60;Flg&#x60;: indicates whether symbols no longer trading should be included in the results returned. (Optional) This criteria is not returned in the symbol data. Possible values:   - &#x60;true&#x60;   - &#x60;false&#x60; (Default)  &#x60;Cnt&#x60;: Country where the symbol is traded in. (Optional) Possible values:   - &#x60;ALL&#x60; if not presented (Default)   - &#x60;US&#x60;   - &#x60;DE&#x60;   - &#x60;CA&#x60;  #### For Options Lookups: (Category&#x3D;StockOption, IndexOption, FutureOption or CurrencyOption)  &#x60;R&#x60;: Symbol root. Required field, the symbol the option is a derivative of, this search will not return options based on a partial root.  &#x60;Stk&#x60;: Number of strikes prices above and below the underlying price   - Default value 3  &#x60;Spl&#x60;: Strike price low  &#x60;Sph&#x60;: Strike price high  &#x60;Exd&#x60;: Number of expiration dates.   - Default value 3  &#x60;Edl&#x60;: Expiration date low, ex: 01-05-2011  &#x60;Edh&#x60;: Expiration date high, ex: 01-20-2011  &#x60;OT&#x60;: Option type. Possible values:   - &#x60;Both&#x60; (Default)   - &#x60;Call&#x60;   - &#x60;Put&#x60;  &#x60;FT&#x60;: Future type for FutureOptions. Possible values:   - &#x60;Electronic&#x60; (Default)   - &#x60;Pit&#x60;  &#x60;ST&#x60;: Symbol type: Possible values:   - &#x60;Both&#x60;   - &#x60;Composite&#x60; (Default)   - &#x60;Regional&#x60;  #### For Futures Lookups: (Category &#x3D; Future)  &#x60;Desc&#x60;: Description of symbol traded  &#x60;R&#x60;: Symbol root future trades  &#x60;FT&#x60;: Futures type. Possible values:   - &#x60;None&#x60;   - &#x60;PIT&#x60;   - &#x60;Electronic&#x60; (Default)   - &#x60;Combined&#x60;  &#x60;Cur&#x60;: Currency. Possible values:   - &#x60;All&#x60;   - &#x60;USD&#x60; (Default)   - &#x60;AUD&#x60;   - &#x60;CAD&#x60;   - &#x60;CHF&#x60;   - &#x60;DKK&#x60;   - &#x60;EUR&#x60;   - &#x60;DBP&#x60;   - &#x60;HKD&#x60;   - &#x60;JPY&#x60;   - &#x60;NOK&#x60;   - &#x60;NZD&#x60;   - &#x60;SEK&#x60;   - &#x60;SGD&#x60;  &#x60;Exp&#x60;: whether to include expired contracts   - &#x60;false&#x60; (Default)   - &#x60;true&#x60;  &#x60;Cnt&#x60;: Country where the symbol is traded in. (Optional) Possible values:   - &#x60;ALL&#x60; if not presented (Default)   - &#x60;US&#x60;   - &#x60;DE&#x60;   - &#x60;CA&#x60;  #### For Forex Lookups:  &#x60;N&#x60;: partial/full symbol name. Use all or null for a list of all forex symbols  &#x60;Desc&#x60;: Description  Note:   - The exchange returned for all forex searches will be &#x60;FX&#x60;   - The country returned for all forex searches will be &#x60;FOREX&#x60; </param>
        /// <returns>SymbolSearchDefinition</returns>
        SymbolSearchDefinition SearchSymbols (string accessToken, string criteria);

        /// <summary>
        /// Search for Symbols 
        /// </summary>
        /// <remarks>
        /// Searches symbols based upon input criteria including Name, Category and Country. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="criteria">Criteria are represented as Key/value pairs (&#x60;&amp;&#x60; separated):  &#x60;N&#x60;: Name of Symbol. (Optional)  &#x60;C&#x60;: Asset categories. (Optional) Possible values:   - &#x60;Future&#x60; or &#x60;FU&#x60;   - &#x60;FutureOption&#x60; or &#x60;FO&#x60;   - &#x60;Stock&#x60; or &#x60;S&#x60; (Default)   - &#x60;StockOption&#x60; or &#x60;SO&#x60; (If root is specified, default category)   - &#x60;Index&#x60; or &#x60;IDX&#x60;   - &#x60;CurrencyOption&#x60; or &#x60;CO&#x60;   - &#x60;MutualFund&#x60; or &#x60;MF&#x60;   - &#x60;MoneyMarketFund&#x60; or &#x60;MMF&#x60;   - &#x60;IndexOption&#x60; or &#x60;IO&#x60;   - &#x60;Bond&#x60; or &#x60;B&#x60;   - &#x60;Forex&#x60; or &#x60;FX&#x60;  &#x60;Cnt&#x60;: Country where the symbol is traded in. (Optional) Possible values:   - &#x60;ALL&#x60; if not presented (Default)   - &#x60;US&#x60;   - &#x60;DE&#x60;   - &#x60;CA&#x60;  #### For Equities Lookups:  &#x60;N&#x60;: partial/full symbol name, will return all symbols that contain the provided name value  &#x60;Desc&#x60;: Name of the company  &#x60;Flg&#x60;: indicates whether symbols no longer trading should be included in the results returned. (Optional) This criteria is not returned in the symbol data. Possible values:   - &#x60;true&#x60;   - &#x60;false&#x60; (Default)  &#x60;Cnt&#x60;: Country where the symbol is traded in. (Optional) Possible values:   - &#x60;ALL&#x60; if not presented (Default)   - &#x60;US&#x60;   - &#x60;DE&#x60;   - &#x60;CA&#x60;  #### For Options Lookups: (Category&#x3D;StockOption, IndexOption, FutureOption or CurrencyOption)  &#x60;R&#x60;: Symbol root. Required field, the symbol the option is a derivative of, this search will not return options based on a partial root.  &#x60;Stk&#x60;: Number of strikes prices above and below the underlying price   - Default value 3  &#x60;Spl&#x60;: Strike price low  &#x60;Sph&#x60;: Strike price high  &#x60;Exd&#x60;: Number of expiration dates.   - Default value 3  &#x60;Edl&#x60;: Expiration date low, ex: 01-05-2011  &#x60;Edh&#x60;: Expiration date high, ex: 01-20-2011  &#x60;OT&#x60;: Option type. Possible values:   - &#x60;Both&#x60; (Default)   - &#x60;Call&#x60;   - &#x60;Put&#x60;  &#x60;FT&#x60;: Future type for FutureOptions. Possible values:   - &#x60;Electronic&#x60; (Default)   - &#x60;Pit&#x60;  &#x60;ST&#x60;: Symbol type: Possible values:   - &#x60;Both&#x60;   - &#x60;Composite&#x60; (Default)   - &#x60;Regional&#x60;  #### For Futures Lookups: (Category &#x3D; Future)  &#x60;Desc&#x60;: Description of symbol traded  &#x60;R&#x60;: Symbol root future trades  &#x60;FT&#x60;: Futures type. Possible values:   - &#x60;None&#x60;   - &#x60;PIT&#x60;   - &#x60;Electronic&#x60; (Default)   - &#x60;Combined&#x60;  &#x60;Cur&#x60;: Currency. Possible values:   - &#x60;All&#x60;   - &#x60;USD&#x60; (Default)   - &#x60;AUD&#x60;   - &#x60;CAD&#x60;   - &#x60;CHF&#x60;   - &#x60;DKK&#x60;   - &#x60;EUR&#x60;   - &#x60;DBP&#x60;   - &#x60;HKD&#x60;   - &#x60;JPY&#x60;   - &#x60;NOK&#x60;   - &#x60;NZD&#x60;   - &#x60;SEK&#x60;   - &#x60;SGD&#x60;  &#x60;Exp&#x60;: whether to include expired contracts   - &#x60;false&#x60; (Default)   - &#x60;true&#x60;  &#x60;Cnt&#x60;: Country where the symbol is traded in. (Optional) Possible values:   - &#x60;ALL&#x60; if not presented (Default)   - &#x60;US&#x60;   - &#x60;DE&#x60;   - &#x60;CA&#x60;  #### For Forex Lookups:  &#x60;N&#x60;: partial/full symbol name. Use all or null for a list of all forex symbols  &#x60;Desc&#x60;: Description  Note:   - The exchange returned for all forex searches will be &#x60;FX&#x60;   - The country returned for all forex searches will be &#x60;FOREX&#x60; </param>
        /// <returns>ApiResponse of SymbolSearchDefinition</returns>
        ApiResponse<SymbolSearchDefinition> SearchSymbolsWithHttpInfo (string accessToken, string criteria);
        /// <summary>
        /// Stream BarChart - Bars Back 
        /// </summary>
        /// <remarks>
        /// Streams barchart data starting from a number of bars back from last date, each bar filling interval of unit. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">A Symbol Name</param>
        /// <param name="interval">Interval that each bar will consist of. **For Daily, Weekly, and Monthly units this value must be 1.**</param>
        /// <param name="unit">Unit of time for each bar interval.</param>
        /// <param name="barsBack">The number of bars to stream, going back from time 00:00:00 of the day specified in lastDate</param>
        /// <param name="lastDate">The date to use as the end point when getting bars back. Date is of form MM-DD-YYYY, and is for time 00:00:00 of that day.</param>
        /// <param name="sessionTemplate">United States (US) stock market session templates, that extend bars returned to include those outside of the regular trading session. Ignored for non-US equity symbols. (optional)</param>
        /// <returns>BarchartDefinition</returns>
        BarchartDefinition StreamBarchartsBarsBack (string accessToken, string symbol, int? interval, string unit, int? barsBack, string lastDate, string sessionTemplate = null);

        /// <summary>
        /// Stream BarChart - Bars Back 
        /// </summary>
        /// <remarks>
        /// Streams barchart data starting from a number of bars back from last date, each bar filling interval of unit. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">A Symbol Name</param>
        /// <param name="interval">Interval that each bar will consist of. **For Daily, Weekly, and Monthly units this value must be 1.**</param>
        /// <param name="unit">Unit of time for each bar interval.</param>
        /// <param name="barsBack">The number of bars to stream, going back from time 00:00:00 of the day specified in lastDate</param>
        /// <param name="lastDate">The date to use as the end point when getting bars back. Date is of form MM-DD-YYYY, and is for time 00:00:00 of that day.</param>
        /// <param name="sessionTemplate">United States (US) stock market session templates, that extend bars returned to include those outside of the regular trading session. Ignored for non-US equity symbols. (optional)</param>
        /// <returns>ApiResponse of BarchartDefinition</returns>
        ApiResponse<BarchartDefinition> StreamBarchartsBarsBackWithHttpInfo (string accessToken, string symbol, int? interval, string unit, int? barsBack, string lastDate, string sessionTemplate = null);
        /// <summary>
        /// Stream BarChart - Days Back 
        /// </summary>
        /// <remarks>
        /// Streams barchart data starting from a number of days back from last date, each bar filling interval of unit. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">A Symbol Name</param>
        /// <param name="interval">Interval that each bar will consist of. **For Daily, Weekly, and Monthly units this value must be 1.**</param>
        /// <param name="unit">Unit of time for each bar interval.</param>
        /// <param name="daysBack">The number of bars to stream, going back from time 00:00:00 of the day specified in lastDate. Cannot exceed greater than 57600 if unit is Minute.</param>
        /// <param name="sessionTemplate">United States (US) stock market session templates, that extend bars returned to include those outside of the regular trading session. Ignored for non-US equity symbols. (optional)</param>
        /// <param name="lastDate">The date to use as the end point when getting days back. Date is of form MM-DD-YYYY, and optionally can specify a starting time with format MM-DD-YYYYt08:00:00 and even further UTC offset with format MM-DD-YYYYt12:00:00-0600 (optional)</param>
        /// <returns>BarchartDefinition</returns>
        BarchartDefinition StreamBarchartsDaysBack (string accessToken, string symbol, int? interval, string unit, int? daysBack, string sessionTemplate = null, string lastDate = null);

        /// <summary>
        /// Stream BarChart - Days Back 
        /// </summary>
        /// <remarks>
        /// Streams barchart data starting from a number of days back from last date, each bar filling interval of unit. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">A Symbol Name</param>
        /// <param name="interval">Interval that each bar will consist of. **For Daily, Weekly, and Monthly units this value must be 1.**</param>
        /// <param name="unit">Unit of time for each bar interval.</param>
        /// <param name="daysBack">The number of bars to stream, going back from time 00:00:00 of the day specified in lastDate. Cannot exceed greater than 57600 if unit is Minute.</param>
        /// <param name="sessionTemplate">United States (US) stock market session templates, that extend bars returned to include those outside of the regular trading session. Ignored for non-US equity symbols. (optional)</param>
        /// <param name="lastDate">The date to use as the end point when getting days back. Date is of form MM-DD-YYYY, and optionally can specify a starting time with format MM-DD-YYYYt08:00:00 and even further UTC offset with format MM-DD-YYYYt12:00:00-0600 (optional)</param>
        /// <returns>ApiResponse of BarchartDefinition</returns>
        ApiResponse<BarchartDefinition> StreamBarchartsDaysBackWithHttpInfo (string accessToken, string symbol, int? interval, string unit, int? daysBack, string sessionTemplate = null, string lastDate = null);
        /// <summary>
        /// Stream BarChart - Starting on Date 
        /// </summary>
        /// <remarks>
        /// Streams barchart data starting from startDate, each bar filling quantity of unit. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">A Symbol Name</param>
        /// <param name="interval">Interval that each bar will consist of. **For Daily, Weekly, and Monthly units this value must be 1.**</param>
        /// <param name="unit">Unit of time for each bar interval.</param>
        /// <param name="startDate">The starting date to begin streaming bars from. Date is of form MM-DD-YYYY, and optionally can specify a starting time with format MM-DD-YYYYt08:00:00 and even further UTC offset with format MM-DD-YYYYt12:00:00-0600</param>
        /// <param name="sessionTemplate">United States (US) stock market session templates, that extend bars returned to include those outside of the regular trading session. Ignored for non-US equity symbols. (optional)</param>
        /// <returns>BarchartDefinition</returns>
        BarchartDefinition StreamBarchartsFromStartDate (string accessToken, string symbol, int? interval, string unit, string startDate, string sessionTemplate = null);

        /// <summary>
        /// Stream BarChart - Starting on Date 
        /// </summary>
        /// <remarks>
        /// Streams barchart data starting from startDate, each bar filling quantity of unit. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">A Symbol Name</param>
        /// <param name="interval">Interval that each bar will consist of. **For Daily, Weekly, and Monthly units this value must be 1.**</param>
        /// <param name="unit">Unit of time for each bar interval.</param>
        /// <param name="startDate">The starting date to begin streaming bars from. Date is of form MM-DD-YYYY, and optionally can specify a starting time with format MM-DD-YYYYt08:00:00 and even further UTC offset with format MM-DD-YYYYt12:00:00-0600</param>
        /// <param name="sessionTemplate">United States (US) stock market session templates, that extend bars returned to include those outside of the regular trading session. Ignored for non-US equity symbols. (optional)</param>
        /// <returns>ApiResponse of BarchartDefinition</returns>
        ApiResponse<BarchartDefinition> StreamBarchartsFromStartDateWithHttpInfo (string accessToken, string symbol, int? interval, string unit, string startDate, string sessionTemplate = null);
        /// <summary>
        /// Stream BarChart - Date Range 
        /// </summary>
        /// <remarks>
        /// Streams barchart data starting from startDate to end date, each bar filling interval of unit. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">A Symbol Name</param>
        /// <param name="interval">Interval that each bar will consist of. **For Daily, Weekly, and Monthly units this value must be 1.**</param>
        /// <param name="unit">Unit of time for each bar interval.</param>
        /// <param name="startDate">The starting date to begin streaming bars from. Date is of form MM-DD-YYYY, and optionally can specify a starting time with format MM-DD-YYYYt08:00:00 and even further UTC offset with format MM-DD-YYYYt12:00:00-0600</param>
        /// <param name="endDate">The ending date for bars streamed. Date is of form MM-DD-YYYY, and optionally can specify a starting time with format MM-DD-YYYYt08:00:00 and even further UTC offset with format MM-DD-YYYYt12:00:00-0600</param>
        /// <param name="sessionTemplate">United States (US) stock market session templates, that extend bars returned to include those outside of the regular trading session. Ignored for non-US equity symbols. (optional)</param>
        /// <returns>BarchartDefinition</returns>
        BarchartDefinition StreamBarchartsFromStartDateToEndDate (string accessToken, string symbol, int? interval, string unit, string startDate, string endDate, string sessionTemplate = null);

        /// <summary>
        /// Stream BarChart - Date Range 
        /// </summary>
        /// <remarks>
        /// Streams barchart data starting from startDate to end date, each bar filling interval of unit. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">A Symbol Name</param>
        /// <param name="interval">Interval that each bar will consist of. **For Daily, Weekly, and Monthly units this value must be 1.**</param>
        /// <param name="unit">Unit of time for each bar interval.</param>
        /// <param name="startDate">The starting date to begin streaming bars from. Date is of form MM-DD-YYYY, and optionally can specify a starting time with format MM-DD-YYYYt08:00:00 and even further UTC offset with format MM-DD-YYYYt12:00:00-0600</param>
        /// <param name="endDate">The ending date for bars streamed. Date is of form MM-DD-YYYY, and optionally can specify a starting time with format MM-DD-YYYYt08:00:00 and even further UTC offset with format MM-DD-YYYYt12:00:00-0600</param>
        /// <param name="sessionTemplate">United States (US) stock market session templates, that extend bars returned to include those outside of the regular trading session. Ignored for non-US equity symbols. (optional)</param>
        /// <returns>ApiResponse of BarchartDefinition</returns>
        ApiResponse<BarchartDefinition> StreamBarchartsFromStartDateToEndDateWithHttpInfo (string accessToken, string symbol, int? interval, string unit, string startDate, string endDate, string sessionTemplate = null);
        /// <summary>
        /// Stream Quote Changes 
        /// </summary>
        /// <remarks>
        /// Streams the latest Quote information for the given Symbols. The first chunk in the stream is a full quote snapshot - subsequent chunks only contain fields of the quote object that have changed since the last chunk.  An invalid symbol name will result in a response of this form - {\&quot;Symbol\&quot;:\&quot;BADEXAMPLESYMBOL\&quot;,\&quot;Error\&quot;:\&quot;FAILED, EX_INVALID_SYMBOL\&quot;}  If the user is not entitled for the symbol requested, response will be of this form - {\&quot;Symbol\&quot;:\&quot;EXAMPLESYMBOL\&quot;,\&quot;Error\&quot;:\&quot;FAILED, EX_NOT_ENTITLED\&quot;} 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbols">1 or more Symbol Names (comma-separated)</param>
        /// <param name="transferEncoding">a header with the value of &#x60;Chunked&#x60; must be passed to streaming resources</param>
        /// <returns>QuoteDefinition</returns>
        QuoteDefinition StreamQuotesChanges (string accessToken, string symbols, string transferEncoding);

        /// <summary>
        /// Stream Quote Changes 
        /// </summary>
        /// <remarks>
        /// Streams the latest Quote information for the given Symbols. The first chunk in the stream is a full quote snapshot - subsequent chunks only contain fields of the quote object that have changed since the last chunk.  An invalid symbol name will result in a response of this form - {\&quot;Symbol\&quot;:\&quot;BADEXAMPLESYMBOL\&quot;,\&quot;Error\&quot;:\&quot;FAILED, EX_INVALID_SYMBOL\&quot;}  If the user is not entitled for the symbol requested, response will be of this form - {\&quot;Symbol\&quot;:\&quot;EXAMPLESYMBOL\&quot;,\&quot;Error\&quot;:\&quot;FAILED, EX_NOT_ENTITLED\&quot;} 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbols">1 or more Symbol Names (comma-separated)</param>
        /// <param name="transferEncoding">a header with the value of &#x60;Chunked&#x60; must be passed to streaming resources</param>
        /// <returns>ApiResponse of QuoteDefinition</returns>
        ApiResponse<QuoteDefinition> StreamQuotesChangesWithHttpInfo (string accessToken, string symbols, string transferEncoding);
        /// <summary>
        /// Stream Quote Snapshots 
        /// </summary>
        /// <remarks>
        /// Streams the latest Quote for the given Symbols. Each chunk is a full quote object.  An invalid symbol name will result in a response of this form - {\&quot;Symbol\&quot;:\&quot;BADSYMBOLEXAMPLE\&quot;,\&quot;Error\&quot;:\&quot;FAILED, EX_INVALID_SYMBOL\&quot;}  If the user is not entitled for the symbol requested, response will be of this form - {\&quot;Symbol\&quot;:\&quot;EXAMPLESYMBOL\&quot;,\&quot;Error\&quot;:\&quot;FAILED, EX_NOT_ENTITLED\&quot;} 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbols">1 or more Symbol Names (comma-separated)</param>
        /// <returns>QuoteDefinition</returns>
        QuoteDefinition StreamQuotesSnapshots (string accessToken, string symbols);

        /// <summary>
        /// Stream Quote Snapshots 
        /// </summary>
        /// <remarks>
        /// Streams the latest Quote for the given Symbols. Each chunk is a full quote object.  An invalid symbol name will result in a response of this form - {\&quot;Symbol\&quot;:\&quot;BADSYMBOLEXAMPLE\&quot;,\&quot;Error\&quot;:\&quot;FAILED, EX_INVALID_SYMBOL\&quot;}  If the user is not entitled for the symbol requested, response will be of this form - {\&quot;Symbol\&quot;:\&quot;EXAMPLESYMBOL\&quot;,\&quot;Error\&quot;:\&quot;FAILED, EX_NOT_ENTITLED\&quot;} 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbols">1 or more Symbol Names (comma-separated)</param>
        /// <returns>ApiResponse of QuoteDefinition</returns>
        ApiResponse<QuoteDefinition> StreamQuotesSnapshotsWithHttpInfo (string accessToken, string symbols);
        /// <summary>
        /// Stream Tick Bars 
        /// </summary>
        /// <remarks>
        /// Streams tick bars data from a number of bars back, each bar returned separated by interval number of ticks. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">A Symbol Name</param>
        /// <param name="interval">Interval for each bar returned (in ticks)</param>
        /// <param name="barsBack">The number of bars to stream, going back from current time</param>
        /// <returns>TickbarDefinition</returns>
        TickbarDefinition StreamTickBars (string accessToken, string symbol, int? interval, int? barsBack);

        /// <summary>
        /// Stream Tick Bars 
        /// </summary>
        /// <remarks>
        /// Streams tick bars data from a number of bars back, each bar returned separated by interval number of ticks. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">A Symbol Name</param>
        /// <param name="interval">Interval for each bar returned (in ticks)</param>
        /// <param name="barsBack">The number of bars to stream, going back from current time</param>
        /// <returns>ApiResponse of TickbarDefinition</returns>
        ApiResponse<TickbarDefinition> StreamTickBarsWithHttpInfo (string accessToken, string symbol, int? interval, int? barsBack);
        /// <summary>
        /// Suggest Symbols 
        /// </summary>
        /// <remarks>
        /// Suggests symbols semantically based upon partial input of symbol name, company name, or description 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="top">The top number of results to return</param>
        /// <param name="filter">An OData filter to apply to the results</param>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="text">Symbol text for suggestion</param>
        /// <returns>SymbolSuggestDefinition</returns>
        SymbolSuggestDefinition Suggestsymbols (int? top, string filter, string accessToken, string text);

        /// <summary>
        /// Suggest Symbols 
        /// </summary>
        /// <remarks>
        /// Suggests symbols semantically based upon partial input of symbol name, company name, or description 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="top">The top number of results to return</param>
        /// <param name="filter">An OData filter to apply to the results</param>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="text">Symbol text for suggestion</param>
        /// <returns>ApiResponse of SymbolSuggestDefinition</returns>
        ApiResponse<SymbolSuggestDefinition> SuggestsymbolsWithHttpInfo (int? top, string filter, string accessToken, string text);
        #endregion Synchronous Operations
        #region Asynchronous Operations
        /// <summary>
        /// Get Quote 
        /// </summary>
        /// <remarks>
        /// Gets the latest Level 1 Quote for the given Symbol 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbols">1 or more Symbol Names (comma-separated)</param>
        /// <returns>Task of QuoteDefinition</returns>
        Task<QuoteDefinition> GetQuotesAsync (string accessToken, string symbols);

        /// <summary>
        /// Get Quote 
        /// </summary>
        /// <remarks>
        /// Gets the latest Level 1 Quote for the given Symbol 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbols">1 or more Symbol Names (comma-separated)</param>
        /// <returns>Task of ApiResponse (QuoteDefinition)</returns>
        Task<ApiResponse<QuoteDefinition>> GetQuotesAsyncWithHttpInfo (string accessToken, string symbols);
        /// <summary>
        /// Get Symbol Info 
        /// </summary>
        /// <remarks>
        /// Finds the given symbol and returns a collection of fields describing the symbol, its origin exchange, and other pertinant information. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">Symbol to lookup</param>
        /// <returns>Task of SymbolDefinition</returns>
        Task<SymbolDefinition> GetSymbolAsync (string accessToken, string symbol);

        /// <summary>
        /// Get Symbol Info 
        /// </summary>
        /// <remarks>
        /// Finds the given symbol and returns a collection of fields describing the symbol, its origin exchange, and other pertinant information. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">Symbol to lookup</param>
        /// <returns>Task of ApiResponse (SymbolDefinition)</returns>
        Task<ApiResponse<SymbolDefinition>> GetSymbolAsyncWithHttpInfo (string accessToken, string symbol);
        /// <summary>
        /// Get Symbol List 
        /// </summary>
        /// <remarks>
        /// Gets a specific Symbol List 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbolListId">A valid Symbol List ID</param>
        /// <returns>Task of SymbolListDefinition</returns>
        Task<SymbolListDefinition> GetSymbolListByIDAsync (string accessToken, string symbolListId);

        /// <summary>
        /// Get Symbol List 
        /// </summary>
        /// <remarks>
        /// Gets a specific Symbol List 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbolListId">A valid Symbol List ID</param>
        /// <returns>Task of ApiResponse (SymbolListDefinition)</returns>
        Task<ApiResponse<SymbolListDefinition>> GetSymbolListByIDAsyncWithHttpInfo (string accessToken, string symbolListId);
        /// <summary>
        /// Get Symbols in a Symbol List 
        /// </summary>
        /// <remarks>
        /// Gets the Symbols for a specific Symbol List 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbolListId">A valid Symbol List ID</param>
        /// <returns>Task of SymbolListSymbolsDefinition</returns>
        Task<SymbolListSymbolsDefinition> GetSymbolListSymbolsByIDAsync (string accessToken, string symbolListId);

        /// <summary>
        /// Get Symbols in a Symbol List 
        /// </summary>
        /// <remarks>
        /// Gets the Symbols for a specific Symbol List 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbolListId">A valid Symbol List ID</param>
        /// <returns>Task of ApiResponse (SymbolListSymbolsDefinition)</returns>
        Task<ApiResponse<SymbolListSymbolsDefinition>> GetSymbolListSymbolsByIDAsyncWithHttpInfo (string accessToken, string symbolListId);
        /// <summary>
        /// Get all Symbol Lists 
        /// </summary>
        /// <remarks>
        /// Gets a list of all Symbol Lists 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <returns>Task of SymbolListsDefinition</returns>
        Task<SymbolListsDefinition> GetSymbolListsAsync (string accessToken);

        /// <summary>
        /// Get all Symbol Lists 
        /// </summary>
        /// <remarks>
        /// Gets a list of all Symbol Lists 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <returns>Task of ApiResponse (SymbolListsDefinition)</returns>
        Task<ApiResponse<SymbolListsDefinition>> GetSymbolListsAsyncWithHttpInfo (string accessToken);
        /// <summary>
        /// Search for Symbols 
        /// </summary>
        /// <remarks>
        /// Searches symbols based upon input criteria including Name, Category and Country. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="criteria">Criteria are represented as Key/value pairs (&#x60;&amp;&#x60; separated):  &#x60;N&#x60;: Name of Symbol. (Optional)  &#x60;C&#x60;: Asset categories. (Optional) Possible values:   - &#x60;Future&#x60; or &#x60;FU&#x60;   - &#x60;FutureOption&#x60; or &#x60;FO&#x60;   - &#x60;Stock&#x60; or &#x60;S&#x60; (Default)   - &#x60;StockOption&#x60; or &#x60;SO&#x60; (If root is specified, default category)   - &#x60;Index&#x60; or &#x60;IDX&#x60;   - &#x60;CurrencyOption&#x60; or &#x60;CO&#x60;   - &#x60;MutualFund&#x60; or &#x60;MF&#x60;   - &#x60;MoneyMarketFund&#x60; or &#x60;MMF&#x60;   - &#x60;IndexOption&#x60; or &#x60;IO&#x60;   - &#x60;Bond&#x60; or &#x60;B&#x60;   - &#x60;Forex&#x60; or &#x60;FX&#x60;  &#x60;Cnt&#x60;: Country where the symbol is traded in. (Optional) Possible values:   - &#x60;ALL&#x60; if not presented (Default)   - &#x60;US&#x60;   - &#x60;DE&#x60;   - &#x60;CA&#x60;  #### For Equities Lookups:  &#x60;N&#x60;: partial/full symbol name, will return all symbols that contain the provided name value  &#x60;Desc&#x60;: Name of the company  &#x60;Flg&#x60;: indicates whether symbols no longer trading should be included in the results returned. (Optional) This criteria is not returned in the symbol data. Possible values:   - &#x60;true&#x60;   - &#x60;false&#x60; (Default)  &#x60;Cnt&#x60;: Country where the symbol is traded in. (Optional) Possible values:   - &#x60;ALL&#x60; if not presented (Default)   - &#x60;US&#x60;   - &#x60;DE&#x60;   - &#x60;CA&#x60;  #### For Options Lookups: (Category&#x3D;StockOption, IndexOption, FutureOption or CurrencyOption)  &#x60;R&#x60;: Symbol root. Required field, the symbol the option is a derivative of, this search will not return options based on a partial root.  &#x60;Stk&#x60;: Number of strikes prices above and below the underlying price   - Default value 3  &#x60;Spl&#x60;: Strike price low  &#x60;Sph&#x60;: Strike price high  &#x60;Exd&#x60;: Number of expiration dates.   - Default value 3  &#x60;Edl&#x60;: Expiration date low, ex: 01-05-2011  &#x60;Edh&#x60;: Expiration date high, ex: 01-20-2011  &#x60;OT&#x60;: Option type. Possible values:   - &#x60;Both&#x60; (Default)   - &#x60;Call&#x60;   - &#x60;Put&#x60;  &#x60;FT&#x60;: Future type for FutureOptions. Possible values:   - &#x60;Electronic&#x60; (Default)   - &#x60;Pit&#x60;  &#x60;ST&#x60;: Symbol type: Possible values:   - &#x60;Both&#x60;   - &#x60;Composite&#x60; (Default)   - &#x60;Regional&#x60;  #### For Futures Lookups: (Category &#x3D; Future)  &#x60;Desc&#x60;: Description of symbol traded  &#x60;R&#x60;: Symbol root future trades  &#x60;FT&#x60;: Futures type. Possible values:   - &#x60;None&#x60;   - &#x60;PIT&#x60;   - &#x60;Electronic&#x60; (Default)   - &#x60;Combined&#x60;  &#x60;Cur&#x60;: Currency. Possible values:   - &#x60;All&#x60;   - &#x60;USD&#x60; (Default)   - &#x60;AUD&#x60;   - &#x60;CAD&#x60;   - &#x60;CHF&#x60;   - &#x60;DKK&#x60;   - &#x60;EUR&#x60;   - &#x60;DBP&#x60;   - &#x60;HKD&#x60;   - &#x60;JPY&#x60;   - &#x60;NOK&#x60;   - &#x60;NZD&#x60;   - &#x60;SEK&#x60;   - &#x60;SGD&#x60;  &#x60;Exp&#x60;: whether to include expired contracts   - &#x60;false&#x60; (Default)   - &#x60;true&#x60;  &#x60;Cnt&#x60;: Country where the symbol is traded in. (Optional) Possible values:   - &#x60;ALL&#x60; if not presented (Default)   - &#x60;US&#x60;   - &#x60;DE&#x60;   - &#x60;CA&#x60;  #### For Forex Lookups:  &#x60;N&#x60;: partial/full symbol name. Use all or null for a list of all forex symbols  &#x60;Desc&#x60;: Description  Note:   - The exchange returned for all forex searches will be &#x60;FX&#x60;   - The country returned for all forex searches will be &#x60;FOREX&#x60; </param>
        /// <returns>Task of SymbolSearchDefinition</returns>
        Task<SymbolSearchDefinition> SearchSymbolsAsync (string accessToken, string criteria);

        /// <summary>
        /// Search for Symbols 
        /// </summary>
        /// <remarks>
        /// Searches symbols based upon input criteria including Name, Category and Country. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="criteria">Criteria are represented as Key/value pairs (&#x60;&amp;&#x60; separated):  &#x60;N&#x60;: Name of Symbol. (Optional)  &#x60;C&#x60;: Asset categories. (Optional) Possible values:   - &#x60;Future&#x60; or &#x60;FU&#x60;   - &#x60;FutureOption&#x60; or &#x60;FO&#x60;   - &#x60;Stock&#x60; or &#x60;S&#x60; (Default)   - &#x60;StockOption&#x60; or &#x60;SO&#x60; (If root is specified, default category)   - &#x60;Index&#x60; or &#x60;IDX&#x60;   - &#x60;CurrencyOption&#x60; or &#x60;CO&#x60;   - &#x60;MutualFund&#x60; or &#x60;MF&#x60;   - &#x60;MoneyMarketFund&#x60; or &#x60;MMF&#x60;   - &#x60;IndexOption&#x60; or &#x60;IO&#x60;   - &#x60;Bond&#x60; or &#x60;B&#x60;   - &#x60;Forex&#x60; or &#x60;FX&#x60;  &#x60;Cnt&#x60;: Country where the symbol is traded in. (Optional) Possible values:   - &#x60;ALL&#x60; if not presented (Default)   - &#x60;US&#x60;   - &#x60;DE&#x60;   - &#x60;CA&#x60;  #### For Equities Lookups:  &#x60;N&#x60;: partial/full symbol name, will return all symbols that contain the provided name value  &#x60;Desc&#x60;: Name of the company  &#x60;Flg&#x60;: indicates whether symbols no longer trading should be included in the results returned. (Optional) This criteria is not returned in the symbol data. Possible values:   - &#x60;true&#x60;   - &#x60;false&#x60; (Default)  &#x60;Cnt&#x60;: Country where the symbol is traded in. (Optional) Possible values:   - &#x60;ALL&#x60; if not presented (Default)   - &#x60;US&#x60;   - &#x60;DE&#x60;   - &#x60;CA&#x60;  #### For Options Lookups: (Category&#x3D;StockOption, IndexOption, FutureOption or CurrencyOption)  &#x60;R&#x60;: Symbol root. Required field, the symbol the option is a derivative of, this search will not return options based on a partial root.  &#x60;Stk&#x60;: Number of strikes prices above and below the underlying price   - Default value 3  &#x60;Spl&#x60;: Strike price low  &#x60;Sph&#x60;: Strike price high  &#x60;Exd&#x60;: Number of expiration dates.   - Default value 3  &#x60;Edl&#x60;: Expiration date low, ex: 01-05-2011  &#x60;Edh&#x60;: Expiration date high, ex: 01-20-2011  &#x60;OT&#x60;: Option type. Possible values:   - &#x60;Both&#x60; (Default)   - &#x60;Call&#x60;   - &#x60;Put&#x60;  &#x60;FT&#x60;: Future type for FutureOptions. Possible values:   - &#x60;Electronic&#x60; (Default)   - &#x60;Pit&#x60;  &#x60;ST&#x60;: Symbol type: Possible values:   - &#x60;Both&#x60;   - &#x60;Composite&#x60; (Default)   - &#x60;Regional&#x60;  #### For Futures Lookups: (Category &#x3D; Future)  &#x60;Desc&#x60;: Description of symbol traded  &#x60;R&#x60;: Symbol root future trades  &#x60;FT&#x60;: Futures type. Possible values:   - &#x60;None&#x60;   - &#x60;PIT&#x60;   - &#x60;Electronic&#x60; (Default)   - &#x60;Combined&#x60;  &#x60;Cur&#x60;: Currency. Possible values:   - &#x60;All&#x60;   - &#x60;USD&#x60; (Default)   - &#x60;AUD&#x60;   - &#x60;CAD&#x60;   - &#x60;CHF&#x60;   - &#x60;DKK&#x60;   - &#x60;EUR&#x60;   - &#x60;DBP&#x60;   - &#x60;HKD&#x60;   - &#x60;JPY&#x60;   - &#x60;NOK&#x60;   - &#x60;NZD&#x60;   - &#x60;SEK&#x60;   - &#x60;SGD&#x60;  &#x60;Exp&#x60;: whether to include expired contracts   - &#x60;false&#x60; (Default)   - &#x60;true&#x60;  &#x60;Cnt&#x60;: Country where the symbol is traded in. (Optional) Possible values:   - &#x60;ALL&#x60; if not presented (Default)   - &#x60;US&#x60;   - &#x60;DE&#x60;   - &#x60;CA&#x60;  #### For Forex Lookups:  &#x60;N&#x60;: partial/full symbol name. Use all or null for a list of all forex symbols  &#x60;Desc&#x60;: Description  Note:   - The exchange returned for all forex searches will be &#x60;FX&#x60;   - The country returned for all forex searches will be &#x60;FOREX&#x60; </param>
        /// <returns>Task of ApiResponse (SymbolSearchDefinition)</returns>
        Task<ApiResponse<SymbolSearchDefinition>> SearchSymbolsAsyncWithHttpInfo (string accessToken, string criteria);
        /// <summary>
        /// Stream BarChart - Bars Back 
        /// </summary>
        /// <remarks>
        /// Streams barchart data starting from a number of bars back from last date, each bar filling interval of unit. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">A Symbol Name</param>
        /// <param name="interval">Interval that each bar will consist of. **For Daily, Weekly, and Monthly units this value must be 1.**</param>
        /// <param name="unit">Unit of time for each bar interval.</param>
        /// <param name="barsBack">The number of bars to stream, going back from time 00:00:00 of the day specified in lastDate</param>
        /// <param name="lastDate">The date to use as the end point when getting bars back. Date is of form MM-DD-YYYY, and is for time 00:00:00 of that day.</param>
        /// <param name="sessionTemplate">United States (US) stock market session templates, that extend bars returned to include those outside of the regular trading session. Ignored for non-US equity symbols. (optional)</param>
        /// <returns>Task of BarchartDefinition</returns>
        Task<HttpStatusCode> StreamBarchartsBarsBackAsync (string accessToken, string symbol, int? interval, string unit, int? barsBack, string lastDate,
            BarchartReceivedHandler OnDataReceived, object targetList, CancellationToken cancellationToken, string sessionTemplate = null);

        /// <summary>
        /// Stream BarChart - Bars Back 
        /// </summary>
        /// <remarks>
        /// Streams barchart data starting from a number of bars back from last date, each bar filling interval of unit. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">A Symbol Name</param>
        /// <param name="interval">Interval that each bar will consist of. **For Daily, Weekly, and Monthly units this value must be 1.**</param>
        /// <param name="unit">Unit of time for each bar interval.</param>
        /// <param name="barsBack">The number of bars to stream, going back from time 00:00:00 of the day specified in lastDate</param>
        /// <param name="lastDate">The date to use as the end point when getting bars back. Date is of form MM-DD-YYYY, and is for time 00:00:00 of that day.</param>
        /// <param name="sessionTemplate">United States (US) stock market session templates, that extend bars returned to include those outside of the regular trading session. Ignored for non-US equity symbols. (optional)</param>
        /// <returns>Task of ApiResponse (BarchartDefinition)</returns>
        Task<ApiResponse<BarchartDefinition>> StreamBarchartsBarsBackAsyncWithHttpInfo (string accessToken, string symbol, int? interval, string unit, int? barsBack, string lastDate,
            BarchartReceivedHandler OnDataReceived, object targetList, CancellationToken cancellationToken, string sessionTemplate = null);
        /// <summary>
        /// Stream BarChart - Days Back 
        /// </summary>
        /// <remarks>
        /// Streams barchart data starting from a number of days back from last date, each bar filling interval of unit. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">A Symbol Name</param>
        /// <param name="interval">Interval that each bar will consist of. **For Daily, Weekly, and Monthly units this value must be 1.**</param>
        /// <param name="unit">Unit of time for each bar interval.</param>
        /// <param name="daysBack">The number of bars to stream, going back from time 00:00:00 of the day specified in lastDate. Cannot exceed greater than 57600 if unit is Minute.</param>
        /// <param name="sessionTemplate">United States (US) stock market session templates, that extend bars returned to include those outside of the regular trading session. Ignored for non-US equity symbols. (optional)</param>
        /// <param name="lastDate">The date to use as the end point when getting days back. Date is of form MM-DD-YYYY, and optionally can specify a starting time with format MM-DD-YYYYt08:00:00 and even further UTC offset with format MM-DD-YYYYt12:00:00-0600 (optional)</param>
        /// <returns>Task of BarchartDefinition</returns>
        Task<BarchartDefinition> StreamBarchartsDaysBackAsync (string accessToken, string symbol, int? interval, string unit, int? daysBack, string sessionTemplate = null, string lastDate = null);

        /// <summary>
        /// Stream BarChart - Days Back 
        /// </summary>
        /// <remarks>
        /// Streams barchart data starting from a number of days back from last date, each bar filling interval of unit. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">A Symbol Name</param>
        /// <param name="interval">Interval that each bar will consist of. **For Daily, Weekly, and Monthly units this value must be 1.**</param>
        /// <param name="unit">Unit of time for each bar interval.</param>
        /// <param name="daysBack">The number of bars to stream, going back from time 00:00:00 of the day specified in lastDate. Cannot exceed greater than 57600 if unit is Minute.</param>
        /// <param name="sessionTemplate">United States (US) stock market session templates, that extend bars returned to include those outside of the regular trading session. Ignored for non-US equity symbols. (optional)</param>
        /// <param name="lastDate">The date to use as the end point when getting days back. Date is of form MM-DD-YYYY, and optionally can specify a starting time with format MM-DD-YYYYt08:00:00 and even further UTC offset with format MM-DD-YYYYt12:00:00-0600 (optional)</param>
        /// <returns>Task of ApiResponse (BarchartDefinition)</returns>
        Task<ApiResponse<BarchartDefinition>> StreamBarchartsDaysBackAsyncWithHttpInfo (string accessToken, string symbol, int? interval, string unit, int? daysBack, string sessionTemplate = null, string lastDate = null);
        /// <summary>
        /// Stream BarChart - Starting on Date 
        /// </summary>
        /// <remarks>
        /// Streams barchart data starting from startDate, each bar filling quantity of unit. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">A Symbol Name</param>
        /// <param name="interval">Interval that each bar will consist of. **For Daily, Weekly, and Monthly units this value must be 1.**</param>
        /// <param name="unit">Unit of time for each bar interval.</param>
        /// <param name="startDate">The starting date to begin streaming bars from. Date is of form MM-DD-YYYY, and optionally can specify a starting time with format MM-DD-YYYYt08:00:00 and even further UTC offset with format MM-DD-YYYYt12:00:00-0600</param>
        /// <param name="sessionTemplate">United States (US) stock market session templates, that extend bars returned to include those outside of the regular trading session. Ignored for non-US equity symbols. (optional)</param>
        /// <returns>Task of BarchartDefinition</returns>
        Task<HttpStatusCode> StreamBarchartsFromStartDateAsync (string accessToken, string symbol, int? interval, string unit, string startDate,
            BarchartReceivedHandler OnDataReceived, object targetList, CancellationToken cancellationToken, string sessionTemplate = null);

        /// <summary>
        /// Stream BarChart - Starting on Date 
        /// </summary>
        /// <remarks>
        /// Streams barchart data starting from startDate, each bar filling quantity of unit. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">A Symbol Name</param>
        /// <param name="interval">Interval that each bar will consist of. **For Daily, Weekly, and Monthly units this value must be 1.**</param>
        /// <param name="unit">Unit of time for each bar interval.</param>
        /// <param name="startDate">The starting date to begin streaming bars from. Date is of form MM-DD-YYYY, and optionally can specify a starting time with format MM-DD-YYYYt08:00:00 and even further UTC offset with format MM-DD-YYYYt12:00:00-0600</param>
        /// <param name="sessionTemplate">United States (US) stock market session templates, that extend bars returned to include those outside of the regular trading session. Ignored for non-US equity symbols. (optional)</param>
        /// <returns>Task of ApiResponse (BarchartDefinition)</returns>
        Task<ApiResponse<BarchartDefinition>> StreamBarchartsFromStartDateAsyncWithHttpInfo (string accessToken, string symbol, int? interval, string unit, string startDate,
            BarchartReceivedHandler OnDataReceived, object targetList, CancellationToken cancellationToken, string sessionTemplate = null);
        /// <summary>
        /// Stream BarChart - Date Range 
        /// </summary>
        /// <remarks>
        /// Streams barchart data starting from startDate to end date, each bar filling interval of unit. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">A Symbol Name</param>
        /// <param name="interval">Interval that each bar will consist of. **For Daily, Weekly, and Monthly units this value must be 1.**</param>
        /// <param name="unit">Unit of time for each bar interval.</param>
        /// <param name="startDate">The starting date to begin streaming bars from. Date is of form MM-DD-YYYY, and optionally can specify a starting time with format MM-DD-YYYYt08:00:00 and even further UTC offset with format MM-DD-YYYYt12:00:00-0600</param>
        /// <param name="endDate">The ending date for bars streamed. Date is of form MM-DD-YYYY, and optionally can specify a starting time with format MM-DD-YYYYt08:00:00 and even further UTC offset with format MM-DD-YYYYt12:00:00-0600</param>
        /// <param name="sessionTemplate">United States (US) stock market session templates, that extend bars returned to include those outside of the regular trading session. Ignored for non-US equity symbols. (optional)</param>
        /// <returns>Task of BarchartDefinition</returns>
        Task<HttpStatusCode> StreamBarchartsFromStartDateToEndDateAsync (string accessToken, string symbol, int? interval, string unit, string startDate, string endDate,
            BarchartReceivedHandler OnDataReceived, object targetList, CancellationToken cancellationToken, string sessionTemplate = null);

        /// <summary>
        /// Stream BarChart - Date Range 
        /// </summary>
        /// <remarks>
        /// Streams barchart data starting from startDate to end date, each bar filling interval of unit. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">A Symbol Name</param>
        /// <param name="interval">Interval that each bar will consist of. **For Daily, Weekly, and Monthly units this value must be 1.**</param>
        /// <param name="unit">Unit of time for each bar interval.</param>
        /// <param name="startDate">The starting date to begin streaming bars from. Date is of form MM-DD-YYYY, and optionally can specify a starting time with format MM-DD-YYYYt08:00:00 and even further UTC offset with format MM-DD-YYYYt12:00:00-0600</param>
        /// <param name="endDate">The ending date for bars streamed. Date is of form MM-DD-YYYY, and optionally can specify a starting time with format MM-DD-YYYYt08:00:00 and even further UTC offset with format MM-DD-YYYYt12:00:00-0600</param>
        /// <param name="sessionTemplate">United States (US) stock market session templates, that extend bars returned to include those outside of the regular trading session. Ignored for non-US equity symbols. (optional)</param>
        /// <returns>Task of ApiResponse (BarchartDefinition)</returns>
        Task<ApiResponse<BarchartDefinition>> StreamBarchartsFromStartDateToEndDateAsyncWithHttpInfo (string accessToken, string symbol, int? interval, string unit, string startDate, string endDate,
            BarchartReceivedHandler OnDataReceived, object targetList, CancellationToken cancellationToken, string sessionTemplate = null);
        /// <summary>
        /// Stream Quote Changes 
        /// </summary>
        /// <remarks>
        /// Streams the latest Quote information for the given Symbols. The first chunk in the stream is a full quote snapshot - subsequent chunks only contain fields of the quote object that have changed since the last chunk.  An invalid symbol name will result in a response of this form - {\&quot;Symbol\&quot;:\&quot;BADEXAMPLESYMBOL\&quot;,\&quot;Error\&quot;:\&quot;FAILED, EX_INVALID_SYMBOL\&quot;}  If the user is not entitled for the symbol requested, response will be of this form - {\&quot;Symbol\&quot;:\&quot;EXAMPLESYMBOL\&quot;,\&quot;Error\&quot;:\&quot;FAILED, EX_NOT_ENTITLED\&quot;} 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbols">1 or more Symbol Names (comma-separated)</param>
        /// <returns>Task of QuoteDefinition</returns>
        Task<HttpStatusCode> StreamQuotesChangesAsync (string accessToken, string symbols, Action<QuoteDefinitionInner, Exception> OnQuoteReceived, CancellationToken cancellationToken);

        /// <summary>
        /// Stream Quote Changes 
        /// </summary>
        /// <remarks>
        /// Streams the latest Quote information for the given Symbols. The first chunk in the stream is a full quote snapshot - subsequent chunks only contain fields of the quote object that have changed since the last chunk.  An invalid symbol name will result in a response of this form - {\&quot;Symbol\&quot;:\&quot;BADEXAMPLESYMBOL\&quot;,\&quot;Error\&quot;:\&quot;FAILED, EX_INVALID_SYMBOL\&quot;}  If the user is not entitled for the symbol requested, response will be of this form - {\&quot;Symbol\&quot;:\&quot;EXAMPLESYMBOL\&quot;,\&quot;Error\&quot;:\&quot;FAILED, EX_NOT_ENTITLED\&quot;} 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbols">1 or more Symbol Names (comma-separated)</param>
        /// <returns>Task of ApiResponse (QuoteDefinition)</returns>
        Task<ApiResponse<QuoteDefinition>> StreamQuotesChangesAsyncWithHttpInfo (string accessToken, string symbols, Action<QuoteDefinitionInner, Exception> OnQuoteReceived, CancellationToken cancellationToken);
        /// <summary>
        /// Stream Quote Snapshots 
        /// </summary>
        /// <remarks>
        /// Streams the latest Quote for the given Symbols. Each chunk is a full quote object.  An invalid symbol name will result in a response of this form - {\&quot;Symbol\&quot;:\&quot;BADSYMBOLEXAMPLE\&quot;,\&quot;Error\&quot;:\&quot;FAILED, EX_INVALID_SYMBOL\&quot;}  If the user is not entitled for the symbol requested, response will be of this form - {\&quot;Symbol\&quot;:\&quot;EXAMPLESYMBOL\&quot;,\&quot;Error\&quot;:\&quot;FAILED, EX_NOT_ENTITLED\&quot;} 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbols">1 or more Symbol Names (comma-separated)</param>
        /// <returns>Task of QuoteDefinition</returns>
        Task<QuoteDefinition> StreamQuotesSnapshotsAsync (string accessToken, string symbols);

        /// <summary>
        /// Stream Quote Snapshots 
        /// </summary>
        /// <remarks>
        /// Streams the latest Quote for the given Symbols. Each chunk is a full quote object.  An invalid symbol name will result in a response of this form - {\&quot;Symbol\&quot;:\&quot;BADSYMBOLEXAMPLE\&quot;,\&quot;Error\&quot;:\&quot;FAILED, EX_INVALID_SYMBOL\&quot;}  If the user is not entitled for the symbol requested, response will be of this form - {\&quot;Symbol\&quot;:\&quot;EXAMPLESYMBOL\&quot;,\&quot;Error\&quot;:\&quot;FAILED, EX_NOT_ENTITLED\&quot;} 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbols">1 or more Symbol Names (comma-separated)</param>
        /// <returns>Task of ApiResponse (QuoteDefinition)</returns>
        Task<ApiResponse<QuoteDefinition>> StreamQuotesSnapshotsAsyncWithHttpInfo (string accessToken, string symbols);
        /// <summary>
        /// Stream Tick Bars 
        /// </summary>
        /// <remarks>
        /// Streams tick bars data from a number of bars back, each bar returned separated by interval number of ticks. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">A Symbol Name</param>
        /// <param name="interval">Interval for each bar returned (in ticks)</param>
        /// <param name="barsBack">The number of bars to stream, going back from current time</param>
        /// <returns>Task of TickbarDefinition</returns>
        Task<TickbarDefinition> StreamTickBarsAsync (string accessToken, string symbol, int? interval, int? barsBack);

        /// <summary>
        /// Stream Tick Bars 
        /// </summary>
        /// <remarks>
        /// Streams tick bars data from a number of bars back, each bar returned separated by interval number of ticks. 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">A Symbol Name</param>
        /// <param name="interval">Interval for each bar returned (in ticks)</param>
        /// <param name="barsBack">The number of bars to stream, going back from current time</param>
        /// <returns>Task of ApiResponse (TickbarDefinition)</returns>
        Task<ApiResponse<TickbarDefinition>> StreamTickBarsAsyncWithHttpInfo (string accessToken, string symbol, int? interval, int? barsBack);
        /// <summary>
        /// Suggest Symbols 
        /// </summary>
        /// <remarks>
        /// Suggests symbols semantically based upon partial input of symbol name, company name, or description 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="top">The top number of results to return</param>
        /// <param name="filter">An OData filter to apply to the results</param>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="text">Symbol text for suggestion</param>
        /// <returns>Task of SymbolSuggestDefinition</returns>
        Task<SymbolSuggestDefinition> SuggestsymbolsAsync (int? top, string filter, string accessToken, string text);

        /// <summary>
        /// Suggest Symbols 
        /// </summary>
        /// <remarks>
        /// Suggests symbols semantically based upon partial input of symbol name, company name, or description 
        /// </remarks>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="top">The top number of results to return</param>
        /// <param name="filter">An OData filter to apply to the results</param>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="text">Symbol text for suggestion</param>
        /// <returns>Task of ApiResponse (SymbolSuggestDefinition)</returns>
        Task<ApiResponse<SymbolSuggestDefinition>> SuggestsymbolsAsyncWithHttpInfo (int? top, string filter, string accessToken, string text);
        #endregion Asynchronous Operations
    }

    /// <summary>
    /// Represents a collection of functions to interact with the API endpoints
    /// </summary>
    public partial class MarketdataApi : IMarketdataApi
    {
        private TradeStationAPI.Client.ExceptionFactory _exceptionFactory = (name, response) => null;

        /// <summary>
        /// Initializes a new instance of the <see cref="MarketdataApi"/> class.
        /// </summary>
        /// <returns></returns>
        public MarketdataApi(String basePath)
        {
            this.Configuration = new TradeStationAPI.Client.Configuration { BasePath = basePath };

            ExceptionFactory = TradeStationAPI.Client.Configuration.DefaultExceptionFactory;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MarketdataApi"/> class
        /// using Configuration object
        /// </summary>
        /// <param name="configuration">An instance of Configuration</param>
        /// <returns></returns>
        public MarketdataApi(TradeStationAPI.Client.Configuration configuration = null)
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
        /// Get Quote  Gets the latest Level 1 Quote for the given Symbol 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbols">1 or more Symbol Names (comma-separated)</param>
        /// <returns>QuoteDefinition</returns>
        public QuoteDefinition GetQuotes (string accessToken, string symbols)
        {
             ApiResponse<QuoteDefinition> localVarResponse = GetQuotesWithHttpInfo(accessToken, symbols);
             return localVarResponse.Data;
        }

        /// <summary>
        /// Get Quote  Gets the latest Level 1 Quote for the given Symbol 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbols">1 or more Symbol Names (comma-separated)</param>
        /// <returns>ApiResponse of QuoteDefinition</returns>
        public ApiResponse< QuoteDefinition > GetQuotesWithHttpInfo (string accessToken, string symbols)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling MarketdataApi->GetQuotes");
            // verify the required parameter 'symbols' is set
            if (symbols == null)
                throw new ApiException(400, "Missing required parameter 'symbols' when calling MarketdataApi->GetQuotes");

            var localVarPath = "/data/quote/{symbols}";
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

            if (symbols != null) localVarPathParams.Add("symbols", this.Configuration.ApiClient.ParameterToString(symbols)); // path parameter
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
                Exception exception = ExceptionFactory("GetQuotes", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<QuoteDefinition>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (QuoteDefinition) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(QuoteDefinition)));
        }

        /// <summary>
        /// Get Quote  Gets the latest Level 1 Quote for the given Symbol 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbols">1 or more Symbol Names (comma-separated)</param>
        /// <returns>Task of QuoteDefinition</returns>
        public async Task<QuoteDefinition> GetQuotesAsync (string accessToken, string symbols)
        {
             ApiResponse<QuoteDefinition> localVarResponse = await GetQuotesAsyncWithHttpInfo(accessToken, symbols);
             return localVarResponse.Data;

        }

        /// <summary>
        /// Get Quote  Gets the latest Level 1 Quote for the given Symbol 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbols">1 or more Symbol Names (comma-separated)</param>
        /// <returns>Task of ApiResponse (QuoteDefinition)</returns>
        public async Task<ApiResponse<QuoteDefinition>> GetQuotesAsyncWithHttpInfo (string accessToken, string symbols)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling MarketdataApi->GetQuotes");
            // verify the required parameter 'symbols' is set
            if (symbols == null)
                throw new ApiException(400, "Missing required parameter 'symbols' when calling MarketdataApi->GetQuotes");

            var localVarPath = "/data/quote/{symbols}";
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

            if (symbols != null) localVarPathParams.Add("symbols", this.Configuration.ApiClient.ParameterToString(symbols)); // path parameter
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
                Exception exception = ExceptionFactory("GetQuotes", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<QuoteDefinition>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (QuoteDefinition) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(QuoteDefinition)));
        }

        /// <summary>
        /// Get Symbol Info  Finds the given symbol and returns a collection of fields describing the symbol, its origin exchange, and other pertinant information. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">Symbol to lookup</param>
        /// <returns>SymbolDefinition</returns>
        public SymbolDefinition GetSymbol (string accessToken, string symbol)
        {
             ApiResponse<SymbolDefinition> localVarResponse = GetSymbolWithHttpInfo(accessToken, symbol);
             return localVarResponse.Data;
        }

        /// <summary>
        /// Get Symbol Info  Finds the given symbol and returns a collection of fields describing the symbol, its origin exchange, and other pertinant information. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">Symbol to lookup</param>
        /// <returns>ApiResponse of SymbolDefinition</returns>
        public ApiResponse< SymbolDefinition > GetSymbolWithHttpInfo (string accessToken, string symbol)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling MarketdataApi->GetSymbol");
            // verify the required parameter 'symbol' is set
            if (symbol == null)
                throw new ApiException(400, "Missing required parameter 'symbol' when calling MarketdataApi->GetSymbol");

            var localVarPath = "/data/symbol/{symbol}";
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

            if (symbol != null) localVarPathParams.Add("symbol", this.Configuration.ApiClient.ParameterToString(symbol)); // path parameter
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
                Exception exception = ExceptionFactory("GetSymbol", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<SymbolDefinition>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (SymbolDefinition) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(SymbolDefinition)));
        }

        /// <summary>
        /// Get Symbol Info  Finds the given symbol and returns a collection of fields describing the symbol, its origin exchange, and other pertinant information. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">Symbol to lookup</param>
        /// <returns>Task of SymbolDefinition</returns>
        public async Task<SymbolDefinition> GetSymbolAsync (string accessToken, string symbol)
        {
             ApiResponse<SymbolDefinition> localVarResponse = await GetSymbolAsyncWithHttpInfo(accessToken, symbol);
             return localVarResponse.Data;

        }

        /// <summary>
        /// Get Symbol Info  Finds the given symbol and returns a collection of fields describing the symbol, its origin exchange, and other pertinant information. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">Symbol to lookup</param>
        /// <returns>Task of ApiResponse (SymbolDefinition)</returns>
        public async Task<ApiResponse<SymbolDefinition>> GetSymbolAsyncWithHttpInfo (string accessToken, string symbol)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling MarketdataApi->GetSymbol");
            // verify the required parameter 'symbol' is set
            if (symbol == null)
                throw new ApiException(400, "Missing required parameter 'symbol' when calling MarketdataApi->GetSymbol");

            var localVarPath = "/data/symbol/{symbol}";
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

            if (symbol != null) localVarPathParams.Add("symbol", this.Configuration.ApiClient.ParameterToString(symbol)); // path parameter
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
                Exception exception = ExceptionFactory("GetSymbol", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<SymbolDefinition>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (SymbolDefinition) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(SymbolDefinition)));
        }

        /// <summary>
        /// Get Symbol List  Gets a specific Symbol List 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbolListId">A valid Symbol List ID</param>
        /// <returns>SymbolListDefinition</returns>
        public SymbolListDefinition GetSymbolListByID (string accessToken, string symbolListId)
        {
             ApiResponse<SymbolListDefinition> localVarResponse = GetSymbolListByIDWithHttpInfo(accessToken, symbolListId);
             return localVarResponse.Data;
        }

        /// <summary>
        /// Get Symbol List  Gets a specific Symbol List 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbolListId">A valid Symbol List ID</param>
        /// <returns>ApiResponse of SymbolListDefinition</returns>
        public ApiResponse< SymbolListDefinition > GetSymbolListByIDWithHttpInfo (string accessToken, string symbolListId)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling MarketdataApi->GetSymbolListByID");
            // verify the required parameter 'symbolListId' is set
            if (symbolListId == null)
                throw new ApiException(400, "Missing required parameter 'symbolListId' when calling MarketdataApi->GetSymbolListByID");

            var localVarPath = "/data/symbollists/{symbol_list_id}";
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

            if (symbolListId != null) localVarPathParams.Add("symbol_list_id", this.Configuration.ApiClient.ParameterToString(symbolListId)); // path parameter
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
                Exception exception = ExceptionFactory("GetSymbolListByID", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<SymbolListDefinition>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (SymbolListDefinition) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(SymbolListDefinition)));
        }

        /// <summary>
        /// Get Symbol List  Gets a specific Symbol List 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbolListId">A valid Symbol List ID</param>
        /// <returns>Task of SymbolListDefinition</returns>
        public async Task<SymbolListDefinition> GetSymbolListByIDAsync (string accessToken, string symbolListId)
        {
             ApiResponse<SymbolListDefinition> localVarResponse = await GetSymbolListByIDAsyncWithHttpInfo(accessToken, symbolListId);
             return localVarResponse.Data;

        }

        /// <summary>
        /// Get Symbol List  Gets a specific Symbol List 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbolListId">A valid Symbol List ID</param>
        /// <returns>Task of ApiResponse (SymbolListDefinition)</returns>
        public async Task<ApiResponse<SymbolListDefinition>> GetSymbolListByIDAsyncWithHttpInfo (string accessToken, string symbolListId)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling MarketdataApi->GetSymbolListByID");
            // verify the required parameter 'symbolListId' is set
            if (symbolListId == null)
                throw new ApiException(400, "Missing required parameter 'symbolListId' when calling MarketdataApi->GetSymbolListByID");

            var localVarPath = "/data/symbollists/{symbol_list_id}";
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

            if (symbolListId != null) localVarPathParams.Add("symbol_list_id", this.Configuration.ApiClient.ParameterToString(symbolListId)); // path parameter
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
                Exception exception = ExceptionFactory("GetSymbolListByID", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<SymbolListDefinition>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (SymbolListDefinition) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(SymbolListDefinition)));
        }

        /// <summary>
        /// Get Symbols in a Symbol List  Gets the Symbols for a specific Symbol List 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbolListId">A valid Symbol List ID</param>
        /// <returns>SymbolListSymbolsDefinition</returns>
        public SymbolListSymbolsDefinition GetSymbolListSymbolsByID (string accessToken, string symbolListId)
        {
             ApiResponse<SymbolListSymbolsDefinition> localVarResponse = GetSymbolListSymbolsByIDWithHttpInfo(accessToken, symbolListId);
             return localVarResponse.Data;
        }

        /// <summary>
        /// Get Symbols in a Symbol List  Gets the Symbols for a specific Symbol List 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbolListId">A valid Symbol List ID</param>
        /// <returns>ApiResponse of SymbolListSymbolsDefinition</returns>
        public ApiResponse< SymbolListSymbolsDefinition > GetSymbolListSymbolsByIDWithHttpInfo (string accessToken, string symbolListId)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling MarketdataApi->GetSymbolListSymbolsByID");
            // verify the required parameter 'symbolListId' is set
            if (symbolListId == null)
                throw new ApiException(400, "Missing required parameter 'symbolListId' when calling MarketdataApi->GetSymbolListSymbolsByID");

            var localVarPath = "/data/symbollists/{symbol_list_id}/symbols";
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

            if (symbolListId != null) localVarPathParams.Add("symbol_list_id", this.Configuration.ApiClient.ParameterToString(symbolListId)); // path parameter
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
                Exception exception = ExceptionFactory("GetSymbolListSymbolsByID", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<SymbolListSymbolsDefinition>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (SymbolListSymbolsDefinition) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(SymbolListSymbolsDefinition)));
        }

        /// <summary>
        /// Get Symbols in a Symbol List  Gets the Symbols for a specific Symbol List 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbolListId">A valid Symbol List ID</param>
        /// <returns>Task of SymbolListSymbolsDefinition</returns>
        public async Task<SymbolListSymbolsDefinition> GetSymbolListSymbolsByIDAsync (string accessToken, string symbolListId)
        {
             ApiResponse<SymbolListSymbolsDefinition> localVarResponse = await GetSymbolListSymbolsByIDAsyncWithHttpInfo(accessToken, symbolListId);
             return localVarResponse.Data;

        }

        /// <summary>
        /// Get Symbols in a Symbol List  Gets the Symbols for a specific Symbol List 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbolListId">A valid Symbol List ID</param>
        /// <returns>Task of ApiResponse (SymbolListSymbolsDefinition)</returns>
        public async Task<ApiResponse<SymbolListSymbolsDefinition>> GetSymbolListSymbolsByIDAsyncWithHttpInfo (string accessToken, string symbolListId)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling MarketdataApi->GetSymbolListSymbolsByID");
            // verify the required parameter 'symbolListId' is set
            if (symbolListId == null)
                throw new ApiException(400, "Missing required parameter 'symbolListId' when calling MarketdataApi->GetSymbolListSymbolsByID");

            var localVarPath = "/data/symbollists/{symbol_list_id}/symbols";
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

            if (symbolListId != null) localVarPathParams.Add("symbol_list_id", this.Configuration.ApiClient.ParameterToString(symbolListId)); // path parameter
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
                Exception exception = ExceptionFactory("GetSymbolListSymbolsByID", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<SymbolListSymbolsDefinition>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (SymbolListSymbolsDefinition) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(SymbolListSymbolsDefinition)));
        }

        /// <summary>
        /// Get all Symbol Lists  Gets a list of all Symbol Lists 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <returns>SymbolListsDefinition</returns>
        public SymbolListsDefinition GetSymbolLists (string accessToken)
        {
             ApiResponse<SymbolListsDefinition> localVarResponse = GetSymbolListsWithHttpInfo(accessToken);
             return localVarResponse.Data;
        }

        /// <summary>
        /// Get all Symbol Lists  Gets a list of all Symbol Lists 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <returns>ApiResponse of SymbolListsDefinition</returns>
        public ApiResponse< SymbolListsDefinition > GetSymbolListsWithHttpInfo (string accessToken)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling MarketdataApi->GetSymbolLists");

            var localVarPath = "/data/symbollists";
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
                Exception exception = ExceptionFactory("GetSymbolLists", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<SymbolListsDefinition>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (SymbolListsDefinition) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(SymbolListsDefinition)));
        }

        /// <summary>
        /// Get all Symbol Lists  Gets a list of all Symbol Lists 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <returns>Task of SymbolListsDefinition</returns>
        public async Task<SymbolListsDefinition> GetSymbolListsAsync (string accessToken)
        {
             ApiResponse<SymbolListsDefinition> localVarResponse = await GetSymbolListsAsyncWithHttpInfo(accessToken);
             return localVarResponse.Data;

        }

        /// <summary>
        /// Get all Symbol Lists  Gets a list of all Symbol Lists 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <returns>Task of ApiResponse (SymbolListsDefinition)</returns>
        public async Task<ApiResponse<SymbolListsDefinition>> GetSymbolListsAsyncWithHttpInfo (string accessToken)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling MarketdataApi->GetSymbolLists");

            var localVarPath = "/data/symbollists";
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
                Exception exception = ExceptionFactory("GetSymbolLists", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<SymbolListsDefinition>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (SymbolListsDefinition) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(SymbolListsDefinition)));
        }

        /// <summary>
        /// Search for Symbols  Searches symbols based upon input criteria including Name, Category and Country. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="criteria">Criteria are represented as Key/value pairs (&#x60;&amp;&#x60; separated):  &#x60;N&#x60;: Name of Symbol. (Optional)  &#x60;C&#x60;: Asset categories. (Optional) Possible values:   - &#x60;Future&#x60; or &#x60;FU&#x60;   - &#x60;FutureOption&#x60; or &#x60;FO&#x60;   - &#x60;Stock&#x60; or &#x60;S&#x60; (Default)   - &#x60;StockOption&#x60; or &#x60;SO&#x60; (If root is specified, default category)   - &#x60;Index&#x60; or &#x60;IDX&#x60;   - &#x60;CurrencyOption&#x60; or &#x60;CO&#x60;   - &#x60;MutualFund&#x60; or &#x60;MF&#x60;   - &#x60;MoneyMarketFund&#x60; or &#x60;MMF&#x60;   - &#x60;IndexOption&#x60; or &#x60;IO&#x60;   - &#x60;Bond&#x60; or &#x60;B&#x60;   - &#x60;Forex&#x60; or &#x60;FX&#x60;  &#x60;Cnt&#x60;: Country where the symbol is traded in. (Optional) Possible values:   - &#x60;ALL&#x60; if not presented (Default)   - &#x60;US&#x60;   - &#x60;DE&#x60;   - &#x60;CA&#x60;  #### For Equities Lookups:  &#x60;N&#x60;: partial/full symbol name, will return all symbols that contain the provided name value  &#x60;Desc&#x60;: Name of the company  &#x60;Flg&#x60;: indicates whether symbols no longer trading should be included in the results returned. (Optional) This criteria is not returned in the symbol data. Possible values:   - &#x60;true&#x60;   - &#x60;false&#x60; (Default)  &#x60;Cnt&#x60;: Country where the symbol is traded in. (Optional) Possible values:   - &#x60;ALL&#x60; if not presented (Default)   - &#x60;US&#x60;   - &#x60;DE&#x60;   - &#x60;CA&#x60;  #### For Options Lookups: (Category&#x3D;StockOption, IndexOption, FutureOption or CurrencyOption)  &#x60;R&#x60;: Symbol root. Required field, the symbol the option is a derivative of, this search will not return options based on a partial root.  &#x60;Stk&#x60;: Number of strikes prices above and below the underlying price   - Default value 3  &#x60;Spl&#x60;: Strike price low  &#x60;Sph&#x60;: Strike price high  &#x60;Exd&#x60;: Number of expiration dates.   - Default value 3  &#x60;Edl&#x60;: Expiration date low, ex: 01-05-2011  &#x60;Edh&#x60;: Expiration date high, ex: 01-20-2011  &#x60;OT&#x60;: Option type. Possible values:   - &#x60;Both&#x60; (Default)   - &#x60;Call&#x60;   - &#x60;Put&#x60;  &#x60;FT&#x60;: Future type for FutureOptions. Possible values:   - &#x60;Electronic&#x60; (Default)   - &#x60;Pit&#x60;  &#x60;ST&#x60;: Symbol type: Possible values:   - &#x60;Both&#x60;   - &#x60;Composite&#x60; (Default)   - &#x60;Regional&#x60;  #### For Futures Lookups: (Category &#x3D; Future)  &#x60;Desc&#x60;: Description of symbol traded  &#x60;R&#x60;: Symbol root future trades  &#x60;FT&#x60;: Futures type. Possible values:   - &#x60;None&#x60;   - &#x60;PIT&#x60;   - &#x60;Electronic&#x60; (Default)   - &#x60;Combined&#x60;  &#x60;Cur&#x60;: Currency. Possible values:   - &#x60;All&#x60;   - &#x60;USD&#x60; (Default)   - &#x60;AUD&#x60;   - &#x60;CAD&#x60;   - &#x60;CHF&#x60;   - &#x60;DKK&#x60;   - &#x60;EUR&#x60;   - &#x60;DBP&#x60;   - &#x60;HKD&#x60;   - &#x60;JPY&#x60;   - &#x60;NOK&#x60;   - &#x60;NZD&#x60;   - &#x60;SEK&#x60;   - &#x60;SGD&#x60;  &#x60;Exp&#x60;: whether to include expired contracts   - &#x60;false&#x60; (Default)   - &#x60;true&#x60;  &#x60;Cnt&#x60;: Country where the symbol is traded in. (Optional) Possible values:   - &#x60;ALL&#x60; if not presented (Default)   - &#x60;US&#x60;   - &#x60;DE&#x60;   - &#x60;CA&#x60;  #### For Forex Lookups:  &#x60;N&#x60;: partial/full symbol name. Use all or null for a list of all forex symbols  &#x60;Desc&#x60;: Description  Note:   - The exchange returned for all forex searches will be &#x60;FX&#x60;   - The country returned for all forex searches will be &#x60;FOREX&#x60; </param>
        /// <returns>SymbolSearchDefinition</returns>
        public SymbolSearchDefinition SearchSymbols (string accessToken, string criteria)
        {
             ApiResponse<SymbolSearchDefinition> localVarResponse = SearchSymbolsWithHttpInfo(accessToken, criteria);
             return localVarResponse.Data;
        }

        /// <summary>
        /// Search for Symbols  Searches symbols based upon input criteria including Name, Category and Country. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="criteria">Criteria are represented as Key/value pairs (&#x60;&amp;&#x60; separated):  &#x60;N&#x60;: Name of Symbol. (Optional)  &#x60;C&#x60;: Asset categories. (Optional) Possible values:   - &#x60;Future&#x60; or &#x60;FU&#x60;   - &#x60;FutureOption&#x60; or &#x60;FO&#x60;   - &#x60;Stock&#x60; or &#x60;S&#x60; (Default)   - &#x60;StockOption&#x60; or &#x60;SO&#x60; (If root is specified, default category)   - &#x60;Index&#x60; or &#x60;IDX&#x60;   - &#x60;CurrencyOption&#x60; or &#x60;CO&#x60;   - &#x60;MutualFund&#x60; or &#x60;MF&#x60;   - &#x60;MoneyMarketFund&#x60; or &#x60;MMF&#x60;   - &#x60;IndexOption&#x60; or &#x60;IO&#x60;   - &#x60;Bond&#x60; or &#x60;B&#x60;   - &#x60;Forex&#x60; or &#x60;FX&#x60;  &#x60;Cnt&#x60;: Country where the symbol is traded in. (Optional) Possible values:   - &#x60;ALL&#x60; if not presented (Default)   - &#x60;US&#x60;   - &#x60;DE&#x60;   - &#x60;CA&#x60;  #### For Equities Lookups:  &#x60;N&#x60;: partial/full symbol name, will return all symbols that contain the provided name value  &#x60;Desc&#x60;: Name of the company  &#x60;Flg&#x60;: indicates whether symbols no longer trading should be included in the results returned. (Optional) This criteria is not returned in the symbol data. Possible values:   - &#x60;true&#x60;   - &#x60;false&#x60; (Default)  &#x60;Cnt&#x60;: Country where the symbol is traded in. (Optional) Possible values:   - &#x60;ALL&#x60; if not presented (Default)   - &#x60;US&#x60;   - &#x60;DE&#x60;   - &#x60;CA&#x60;  #### For Options Lookups: (Category&#x3D;StockOption, IndexOption, FutureOption or CurrencyOption)  &#x60;R&#x60;: Symbol root. Required field, the symbol the option is a derivative of, this search will not return options based on a partial root.  &#x60;Stk&#x60;: Number of strikes prices above and below the underlying price   - Default value 3  &#x60;Spl&#x60;: Strike price low  &#x60;Sph&#x60;: Strike price high  &#x60;Exd&#x60;: Number of expiration dates.   - Default value 3  &#x60;Edl&#x60;: Expiration date low, ex: 01-05-2011  &#x60;Edh&#x60;: Expiration date high, ex: 01-20-2011  &#x60;OT&#x60;: Option type. Possible values:   - &#x60;Both&#x60; (Default)   - &#x60;Call&#x60;   - &#x60;Put&#x60;  &#x60;FT&#x60;: Future type for FutureOptions. Possible values:   - &#x60;Electronic&#x60; (Default)   - &#x60;Pit&#x60;  &#x60;ST&#x60;: Symbol type: Possible values:   - &#x60;Both&#x60;   - &#x60;Composite&#x60; (Default)   - &#x60;Regional&#x60;  #### For Futures Lookups: (Category &#x3D; Future)  &#x60;Desc&#x60;: Description of symbol traded  &#x60;R&#x60;: Symbol root future trades  &#x60;FT&#x60;: Futures type. Possible values:   - &#x60;None&#x60;   - &#x60;PIT&#x60;   - &#x60;Electronic&#x60; (Default)   - &#x60;Combined&#x60;  &#x60;Cur&#x60;: Currency. Possible values:   - &#x60;All&#x60;   - &#x60;USD&#x60; (Default)   - &#x60;AUD&#x60;   - &#x60;CAD&#x60;   - &#x60;CHF&#x60;   - &#x60;DKK&#x60;   - &#x60;EUR&#x60;   - &#x60;DBP&#x60;   - &#x60;HKD&#x60;   - &#x60;JPY&#x60;   - &#x60;NOK&#x60;   - &#x60;NZD&#x60;   - &#x60;SEK&#x60;   - &#x60;SGD&#x60;  &#x60;Exp&#x60;: whether to include expired contracts   - &#x60;false&#x60; (Default)   - &#x60;true&#x60;  &#x60;Cnt&#x60;: Country where the symbol is traded in. (Optional) Possible values:   - &#x60;ALL&#x60; if not presented (Default)   - &#x60;US&#x60;   - &#x60;DE&#x60;   - &#x60;CA&#x60;  #### For Forex Lookups:  &#x60;N&#x60;: partial/full symbol name. Use all or null for a list of all forex symbols  &#x60;Desc&#x60;: Description  Note:   - The exchange returned for all forex searches will be &#x60;FX&#x60;   - The country returned for all forex searches will be &#x60;FOREX&#x60; </param>
        /// <returns>ApiResponse of SymbolSearchDefinition</returns>
        public ApiResponse< SymbolSearchDefinition > SearchSymbolsWithHttpInfo (string accessToken, string criteria)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling MarketdataApi->SearchSymbols");
            // verify the required parameter 'criteria' is set
            if (criteria == null)
                throw new ApiException(400, "Missing required parameter 'criteria' when calling MarketdataApi->SearchSymbols");

            var localVarPath = "/data/symbols/search/{criteria}";
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

            if (criteria != null) localVarPathParams.Add("criteria", this.Configuration.ApiClient.ParameterToString(criteria)); // path parameter
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
                Exception exception = ExceptionFactory("SearchSymbols", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<SymbolSearchDefinition>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (SymbolSearchDefinition) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(SymbolSearchDefinition)));
        }

        /// <summary>
        /// Search for Symbols  Searches symbols based upon input criteria including Name, Category and Country. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="criteria">Criteria are represented as Key/value pairs (&#x60;&amp;&#x60; separated):  &#x60;N&#x60;: Name of Symbol. (Optional)  &#x60;C&#x60;: Asset categories. (Optional) Possible values:   - &#x60;Future&#x60; or &#x60;FU&#x60;   - &#x60;FutureOption&#x60; or &#x60;FO&#x60;   - &#x60;Stock&#x60; or &#x60;S&#x60; (Default)   - &#x60;StockOption&#x60; or &#x60;SO&#x60; (If root is specified, default category)   - &#x60;Index&#x60; or &#x60;IDX&#x60;   - &#x60;CurrencyOption&#x60; or &#x60;CO&#x60;   - &#x60;MutualFund&#x60; or &#x60;MF&#x60;   - &#x60;MoneyMarketFund&#x60; or &#x60;MMF&#x60;   - &#x60;IndexOption&#x60; or &#x60;IO&#x60;   - &#x60;Bond&#x60; or &#x60;B&#x60;   - &#x60;Forex&#x60; or &#x60;FX&#x60;  &#x60;Cnt&#x60;: Country where the symbol is traded in. (Optional) Possible values:   - &#x60;ALL&#x60; if not presented (Default)   - &#x60;US&#x60;   - &#x60;DE&#x60;   - &#x60;CA&#x60;  #### For Equities Lookups:  &#x60;N&#x60;: partial/full symbol name, will return all symbols that contain the provided name value  &#x60;Desc&#x60;: Name of the company  &#x60;Flg&#x60;: indicates whether symbols no longer trading should be included in the results returned. (Optional) This criteria is not returned in the symbol data. Possible values:   - &#x60;true&#x60;   - &#x60;false&#x60; (Default)  &#x60;Cnt&#x60;: Country where the symbol is traded in. (Optional) Possible values:   - &#x60;ALL&#x60; if not presented (Default)   - &#x60;US&#x60;   - &#x60;DE&#x60;   - &#x60;CA&#x60;  #### For Options Lookups: (Category&#x3D;StockOption, IndexOption, FutureOption or CurrencyOption)  &#x60;R&#x60;: Symbol root. Required field, the symbol the option is a derivative of, this search will not return options based on a partial root.  &#x60;Stk&#x60;: Number of strikes prices above and below the underlying price   - Default value 3  &#x60;Spl&#x60;: Strike price low  &#x60;Sph&#x60;: Strike price high  &#x60;Exd&#x60;: Number of expiration dates.   - Default value 3  &#x60;Edl&#x60;: Expiration date low, ex: 01-05-2011  &#x60;Edh&#x60;: Expiration date high, ex: 01-20-2011  &#x60;OT&#x60;: Option type. Possible values:   - &#x60;Both&#x60; (Default)   - &#x60;Call&#x60;   - &#x60;Put&#x60;  &#x60;FT&#x60;: Future type for FutureOptions. Possible values:   - &#x60;Electronic&#x60; (Default)   - &#x60;Pit&#x60;  &#x60;ST&#x60;: Symbol type: Possible values:   - &#x60;Both&#x60;   - &#x60;Composite&#x60; (Default)   - &#x60;Regional&#x60;  #### For Futures Lookups: (Category &#x3D; Future)  &#x60;Desc&#x60;: Description of symbol traded  &#x60;R&#x60;: Symbol root future trades  &#x60;FT&#x60;: Futures type. Possible values:   - &#x60;None&#x60;   - &#x60;PIT&#x60;   - &#x60;Electronic&#x60; (Default)   - &#x60;Combined&#x60;  &#x60;Cur&#x60;: Currency. Possible values:   - &#x60;All&#x60;   - &#x60;USD&#x60; (Default)   - &#x60;AUD&#x60;   - &#x60;CAD&#x60;   - &#x60;CHF&#x60;   - &#x60;DKK&#x60;   - &#x60;EUR&#x60;   - &#x60;DBP&#x60;   - &#x60;HKD&#x60;   - &#x60;JPY&#x60;   - &#x60;NOK&#x60;   - &#x60;NZD&#x60;   - &#x60;SEK&#x60;   - &#x60;SGD&#x60;  &#x60;Exp&#x60;: whether to include expired contracts   - &#x60;false&#x60; (Default)   - &#x60;true&#x60;  &#x60;Cnt&#x60;: Country where the symbol is traded in. (Optional) Possible values:   - &#x60;ALL&#x60; if not presented (Default)   - &#x60;US&#x60;   - &#x60;DE&#x60;   - &#x60;CA&#x60;  #### For Forex Lookups:  &#x60;N&#x60;: partial/full symbol name. Use all or null for a list of all forex symbols  &#x60;Desc&#x60;: Description  Note:   - The exchange returned for all forex searches will be &#x60;FX&#x60;   - The country returned for all forex searches will be &#x60;FOREX&#x60; </param>
        /// <returns>Task of SymbolSearchDefinition</returns>
        public async Task<SymbolSearchDefinition> SearchSymbolsAsync (string accessToken, string criteria)
        {
             ApiResponse<SymbolSearchDefinition> localVarResponse = await SearchSymbolsAsyncWithHttpInfo(accessToken, criteria);
             return localVarResponse.Data;

        }

        /// <summary>
        /// Search for Symbols  Searches symbols based upon input criteria including Name, Category and Country. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="criteria">Criteria are represented as Key/value pairs (&#x60;&amp;&#x60; separated):  &#x60;N&#x60;: Name of Symbol. (Optional)  &#x60;C&#x60;: Asset categories. (Optional) Possible values:   - &#x60;Future&#x60; or &#x60;FU&#x60;   - &#x60;FutureOption&#x60; or &#x60;FO&#x60;   - &#x60;Stock&#x60; or &#x60;S&#x60; (Default)   - &#x60;StockOption&#x60; or &#x60;SO&#x60; (If root is specified, default category)   - &#x60;Index&#x60; or &#x60;IDX&#x60;   - &#x60;CurrencyOption&#x60; or &#x60;CO&#x60;   - &#x60;MutualFund&#x60; or &#x60;MF&#x60;   - &#x60;MoneyMarketFund&#x60; or &#x60;MMF&#x60;   - &#x60;IndexOption&#x60; or &#x60;IO&#x60;   - &#x60;Bond&#x60; or &#x60;B&#x60;   - &#x60;Forex&#x60; or &#x60;FX&#x60;  &#x60;Cnt&#x60;: Country where the symbol is traded in. (Optional) Possible values:   - &#x60;ALL&#x60; if not presented (Default)   - &#x60;US&#x60;   - &#x60;DE&#x60;   - &#x60;CA&#x60;  #### For Equities Lookups:  &#x60;N&#x60;: partial/full symbol name, will return all symbols that contain the provided name value  &#x60;Desc&#x60;: Name of the company  &#x60;Flg&#x60;: indicates whether symbols no longer trading should be included in the results returned. (Optional) This criteria is not returned in the symbol data. Possible values:   - &#x60;true&#x60;   - &#x60;false&#x60; (Default)  &#x60;Cnt&#x60;: Country where the symbol is traded in. (Optional) Possible values:   - &#x60;ALL&#x60; if not presented (Default)   - &#x60;US&#x60;   - &#x60;DE&#x60;   - &#x60;CA&#x60;  #### For Options Lookups: (Category&#x3D;StockOption, IndexOption, FutureOption or CurrencyOption)  &#x60;R&#x60;: Symbol root. Required field, the symbol the option is a derivative of, this search will not return options based on a partial root.  &#x60;Stk&#x60;: Number of strikes prices above and below the underlying price   - Default value 3  &#x60;Spl&#x60;: Strike price low  &#x60;Sph&#x60;: Strike price high  &#x60;Exd&#x60;: Number of expiration dates.   - Default value 3  &#x60;Edl&#x60;: Expiration date low, ex: 01-05-2011  &#x60;Edh&#x60;: Expiration date high, ex: 01-20-2011  &#x60;OT&#x60;: Option type. Possible values:   - &#x60;Both&#x60; (Default)   - &#x60;Call&#x60;   - &#x60;Put&#x60;  &#x60;FT&#x60;: Future type for FutureOptions. Possible values:   - &#x60;Electronic&#x60; (Default)   - &#x60;Pit&#x60;  &#x60;ST&#x60;: Symbol type: Possible values:   - &#x60;Both&#x60;   - &#x60;Composite&#x60; (Default)   - &#x60;Regional&#x60;  #### For Futures Lookups: (Category &#x3D; Future)  &#x60;Desc&#x60;: Description of symbol traded  &#x60;R&#x60;: Symbol root future trades  &#x60;FT&#x60;: Futures type. Possible values:   - &#x60;None&#x60;   - &#x60;PIT&#x60;   - &#x60;Electronic&#x60; (Default)   - &#x60;Combined&#x60;  &#x60;Cur&#x60;: Currency. Possible values:   - &#x60;All&#x60;   - &#x60;USD&#x60; (Default)   - &#x60;AUD&#x60;   - &#x60;CAD&#x60;   - &#x60;CHF&#x60;   - &#x60;DKK&#x60;   - &#x60;EUR&#x60;   - &#x60;DBP&#x60;   - &#x60;HKD&#x60;   - &#x60;JPY&#x60;   - &#x60;NOK&#x60;   - &#x60;NZD&#x60;   - &#x60;SEK&#x60;   - &#x60;SGD&#x60;  &#x60;Exp&#x60;: whether to include expired contracts   - &#x60;false&#x60; (Default)   - &#x60;true&#x60;  &#x60;Cnt&#x60;: Country where the symbol is traded in. (Optional) Possible values:   - &#x60;ALL&#x60; if not presented (Default)   - &#x60;US&#x60;   - &#x60;DE&#x60;   - &#x60;CA&#x60;  #### For Forex Lookups:  &#x60;N&#x60;: partial/full symbol name. Use all or null for a list of all forex symbols  &#x60;Desc&#x60;: Description  Note:   - The exchange returned for all forex searches will be &#x60;FX&#x60;   - The country returned for all forex searches will be &#x60;FOREX&#x60; </param>
        /// <returns>Task of ApiResponse (SymbolSearchDefinition)</returns>
        public async Task<ApiResponse<SymbolSearchDefinition>> SearchSymbolsAsyncWithHttpInfo (string accessToken, string criteria)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling MarketdataApi->SearchSymbols");
            // verify the required parameter 'criteria' is set
            if (criteria == null)
                throw new ApiException(400, "Missing required parameter 'criteria' when calling MarketdataApi->SearchSymbols");

            var localVarPath = "/data/symbols/search/{criteria}";
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

            if (criteria != null) localVarPathParams.Add("criteria", this.Configuration.ApiClient.ParameterToString(criteria)); // path parameter
            if (accessToken != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "access_token", accessToken)); // query parameter

            // make the HTTP request
            IRestResponse localVarResponse = (IRestResponse) await this.Configuration.ApiClient.CallApiAsync(localVarPath,
                Method.GET, localVarQueryParams, localVarPostBody, localVarHeaderParams, localVarFormParams, localVarFileParams,
                localVarPathParams, localVarHttpContentType);

            int localVarStatusCode = (int) localVarResponse.StatusCode;

            if (ExceptionFactory != null)
            {
                Exception exception = ExceptionFactory("SearchSymbols", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<SymbolSearchDefinition>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (SymbolSearchDefinition) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(SymbolSearchDefinition)));
        }

        /// <summary>
        /// Stream BarChart - Bars Back  Streams barchart data starting from a number of bars back from last date, each bar filling interval of unit. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">A Symbol Name</param>
        /// <param name="interval">Interval that each bar will consist of. **For Daily, Weekly, and Monthly units this value must be 1.**</param>
        /// <param name="unit">Unit of time for each bar interval.</param>
        /// <param name="barsBack">The number of bars to stream, going back from time 00:00:00 of the day specified in lastDate</param>
        /// <param name="lastDate">The date to use as the end point when getting bars back. Date is of form MM-DD-YYYY, and is for time 00:00:00 of that day.</param>
        /// <param name="sessionTemplate">United States (US) stock market session templates, that extend bars returned to include those outside of the regular trading session. Ignored for non-US equity symbols. (optional)</param>
        /// <returns>BarchartDefinition</returns>
        public BarchartDefinition StreamBarchartsBarsBack (string accessToken, string symbol, int? interval, string unit, int? barsBack, string lastDate, string sessionTemplate = null)
        {
             ApiResponse<BarchartDefinition> localVarResponse = StreamBarchartsBarsBackWithHttpInfo(accessToken, symbol, interval, unit, barsBack, lastDate, sessionTemplate);
             return localVarResponse.Data;
        }

        /// <summary>
        /// Stream BarChart - Bars Back  Streams barchart data starting from a number of bars back from last date, each bar filling interval of unit. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">A Symbol Name</param>
        /// <param name="interval">Interval that each bar will consist of. **For Daily, Weekly, and Monthly units this value must be 1.**</param>
        /// <param name="unit">Unit of time for each bar interval.</param>
        /// <param name="barsBack">The number of bars to stream, going back from time 00:00:00 of the day specified in lastDate</param>
        /// <param name="lastDate">The date to use as the end point when getting bars back. Date is of form MM-DD-YYYY, and is for time 00:00:00 of that day.</param>
        /// <param name="sessionTemplate">United States (US) stock market session templates, that extend bars returned to include those outside of the regular trading session. Ignored for non-US equity symbols. (optional)</param>
        /// <returns>ApiResponse of BarchartDefinition</returns>
        public ApiResponse< BarchartDefinition > StreamBarchartsBarsBackWithHttpInfo (string accessToken, string symbol, int? interval, string unit, int? barsBack, string lastDate, string sessionTemplate = null)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling MarketdataApi->StreamBarchartsBarsBack");
            // verify the required parameter 'symbol' is set
            if (symbol == null)
                throw new ApiException(400, "Missing required parameter 'symbol' when calling MarketdataApi->StreamBarchartsBarsBack");
            // verify the required parameter 'interval' is set
            if (interval == null)
                throw new ApiException(400, "Missing required parameter 'interval' when calling MarketdataApi->StreamBarchartsBarsBack");
            // verify the required parameter 'unit' is set
            if (unit == null)
                throw new ApiException(400, "Missing required parameter 'unit' when calling MarketdataApi->StreamBarchartsBarsBack");
            // verify the required parameter 'barsBack' is set
            if (barsBack == null)
                throw new ApiException(400, "Missing required parameter 'barsBack' when calling MarketdataApi->StreamBarchartsBarsBack");
            // verify the required parameter 'lastDate' is set
            if (lastDate == null)
                throw new ApiException(400, "Missing required parameter 'lastDate' when calling MarketdataApi->StreamBarchartsBarsBack");

            var localVarPath = "/stream/barchart/{symbol}/{interval}/{unit}/{barsBack}/{lastDate}/...";
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
                "text/plain",
                "application/vnd.tradestation.streams+json"
            };
            String localVarHttpHeaderAccept = this.Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            if (symbol != null) localVarPathParams.Add("symbol", this.Configuration.ApiClient.ParameterToString(symbol)); // path parameter
            if (interval != null) localVarPathParams.Add("interval", this.Configuration.ApiClient.ParameterToString(interval)); // path parameter
            if (unit != null) localVarPathParams.Add("unit", this.Configuration.ApiClient.ParameterToString(unit)); // path parameter
            if (barsBack != null) localVarPathParams.Add("barsBack", this.Configuration.ApiClient.ParameterToString(barsBack)); // path parameter
            if (lastDate != null) localVarPathParams.Add("lastDate", this.Configuration.ApiClient.ParameterToString(lastDate)); // path parameter
            if (accessToken != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "access_token", accessToken)); // query parameter
            if (sessionTemplate != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "SessionTemplate", sessionTemplate)); // query parameter

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
                Exception exception = ExceptionFactory("StreamBarchartsBarsBack", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<BarchartDefinition>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (BarchartDefinition) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(BarchartDefinition)));
        }

        /// <summary>
        /// Stream BarChart - Bars Back  Streams barchart data starting from a number of bars back from last date, each bar filling interval of unit. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">A Symbol Name</param>
        /// <param name="interval">Interval that each bar will consist of. **For Daily, Weekly, and Monthly units this value must be 1.**</param>
        /// <param name="unit">Unit of time for each bar interval.</param>
        /// <param name="barsBack">The number of bars to stream, going back from time 00:00:00 of the day specified in lastDate</param>
        /// <param name="lastDate">The date to use as the end point when getting bars back. Date is of form MM-DD-YYYY, and is for time 00:00:00 of that day.</param>
        /// <param name="sessionTemplate">United States (US) stock market session templates, that extend bars returned to include those outside of the regular trading session. Ignored for non-US equity symbols. (optional)</param>
        /// <returns>Task of BarchartDefinition</returns>
        public async Task<HttpStatusCode> StreamBarchartsBarsBackAsync (string accessToken, string symbol, int? interval, string unit, int? barsBack, string lastDate,
            BarchartReceivedHandler OnDataReceived, object targetList, CancellationToken cancellationToken, string sessionTemplate = null)
        {
             ApiResponse<BarchartDefinition> localVarResponse = await StreamBarchartsBarsBackAsyncWithHttpInfo(accessToken, symbol, interval, unit, barsBack, lastDate,
                 OnDataReceived, targetList, cancellationToken, sessionTemplate);
             return (HttpStatusCode)localVarResponse.StatusCode;
        }
        

        /// <summary>
        /// Stream BarChart - Bars Back  Streams barchart data starting from a number of bars back from last date, each bar filling interval of unit. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">A Symbol Name</param>
        /// <param name="interval">Interval that each bar will consist of. **For Daily, Weekly, and Monthly units this value must be 1.**</param>
        /// <param name="unit">Unit of time for each bar interval.</param>
        /// <param name="barsBack">The number of bars to stream, going back from time 00:00:00 of the day specified in lastDate</param>
        /// <param name="lastDate">The date to use as the end point when getting bars back. Date is of form MM-DD-YYYY, and is for time 00:00:00 of that day.</param>
        /// <param name="sessionTemplate">United States (US) stock market session templates, that extend bars returned to include those outside of the regular trading session. Ignored for non-US equity symbols. (optional)</param>
        /// <returns>Task of ApiResponse (BarchartDefinition)</returns>
        public async Task<ApiResponse<BarchartDefinition>> StreamBarchartsBarsBackAsyncWithHttpInfo (string accessToken, string symbol, int? interval, string unit,
            int? barsBack, string lastDate, BarchartReceivedHandler OnDataReceived, object targetList, CancellationToken cancellationToken, string sessionTemplate = null)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling MarketdataApi->StreamBarchartsBarsBack");
            // verify the required parameter 'symbol' is set
            if (symbol == null)
                throw new ApiException(400, "Missing required parameter 'symbol' when calling MarketdataApi->StreamBarchartsBarsBack");
            // verify the required parameter 'interval' is set
            if (interval == null)
                throw new ApiException(400, "Missing required parameter 'interval' when calling MarketdataApi->StreamBarchartsBarsBack");
            // verify the required parameter 'unit' is set
            if (unit == null)
                throw new ApiException(400, "Missing required parameter 'unit' when calling MarketdataApi->StreamBarchartsBarsBack");
            // verify the required parameter 'barsBack' is set
            if (barsBack == null)
                throw new ApiException(400, "Missing required parameter 'barsBack' when calling MarketdataApi->StreamBarchartsBarsBack");
            // verify the required parameter 'lastDate' is set
            if (lastDate == null)
                throw new ApiException(400, "Missing required parameter 'lastDate' when calling MarketdataApi->StreamBarchartsBarsBack");

            var localVarPath = "/stream/barchart/{symbol}/{interval}/{unit}/{barsBack}/{lastDate}/...";
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
                "text/plain",
                "application/vnd.tradestation.streams+json"
            };
            String localVarHttpHeaderAccept = this.Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            if (symbol != null) localVarPathParams.Add("symbol", this.Configuration.ApiClient.ParameterToString(symbol)); // path parameter
            if (interval != null) localVarPathParams.Add("interval", this.Configuration.ApiClient.ParameterToString(interval)); // path parameter
            if (unit != null) localVarPathParams.Add("unit", this.Configuration.ApiClient.ParameterToString(unit)); // path parameter
            if (barsBack != null) localVarPathParams.Add("barsBack", this.Configuration.ApiClient.ParameterToString(barsBack)); // path parameter
            if (lastDate != null) localVarPathParams.Add("lastDate", this.Configuration.ApiClient.ParameterToString(lastDate)); // path parameter
            if (accessToken != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "access_token", accessToken)); // query parameter
            if (sessionTemplate != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "SessionTemplate", sessionTemplate)); // query parameter

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

            string messageBuffer = string.Empty;
            object locker = new object();

            // proccess chunks callback (true - continue, false - close stream)
            async Task<bool> OnChunkReceived(string str, Exception ex)
            {
                if (ex != null)
                {
                    OnDataReceived(symbol, null, targetList, ex);
                    return false;
                }
                if (string.IsNullOrEmpty(str)) return true;

                lock (locker)
                {
                    messageBuffer += str;
                }

                while (true)
                {
                    string json;
                    lock (locker)
                    {
                        // check stream stop
                        if (messageBuffer.StartsWith("END"))
                        {
                            OnDataReceived(symbol, null, targetList, new Exception("END of stream message received"));
                            return false;
                        }
                        else if (messageBuffer.StartsWith("ERROR"))
                        {
                            OnDataReceived(symbol, null, targetList, new Exception($"ERROR message received ({messageBuffer})"));
                            return false;
                        }
                        int index = messageBuffer.IndexOf('\n');
                        if (index == 0)
                        {
                            messageBuffer = messageBuffer.Substring(1);
                            continue;
                        }
                        else if (index < 0)
                            return true;
                        else
                        {
                            json = messageBuffer.Substring(0, index);
                            messageBuffer = messageBuffer.Substring(index + 1);
                        }
                    }

                    try
                    {
                        await Task.Run(() =>
                        {
                            if (cancellationToken.IsCancellationRequested)
                                return null;

                            return (BarchartDefinition)JsonConvert.DeserializeObject(json, typeof(BarchartDefinition),
                                new JsonSerializerSettings { ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor });
                        }).ContinueWith((task) =>
                        {
                            if(task.Result != null)
                                OnDataReceived(symbol, task.Result, targetList, null);
                        });
                    }
                    catch (Exception e)
                    {
                        OnDataReceived(symbol, null, targetList, e);
                        return false;
                    }
                }
            }

            // make the HTTP request
            IRestResponse localVarResponse = (IRestResponse) await this.Configuration.ApiClient.CallApiStreamAsync(localVarPath,
                Method.GET, localVarQueryParams, localVarPostBody, localVarHeaderParams, localVarFormParams, localVarFileParams,
                localVarPathParams, localVarHttpContentType, OnChunkReceived, cancellationToken);

            int localVarStatusCode = (int) localVarResponse.StatusCode;

            if (ExceptionFactory != null)
            {
                Exception exception = ExceptionFactory("StreamBarchartsBarsBack", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<BarchartDefinition>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (BarchartDefinition) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(BarchartDefinition)));
        }

        /// <summary>
        /// Stream BarChart - Days Back  Streams barchart data starting from a number of days back from last date, each bar filling interval of unit. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">A Symbol Name</param>
        /// <param name="interval">Interval that each bar will consist of. **For Daily, Weekly, and Monthly units this value must be 1.**</param>
        /// <param name="unit">Unit of time for each bar interval.</param>
        /// <param name="daysBack">The number of bars to stream, going back from time 00:00:00 of the day specified in lastDate. Cannot exceed greater than 57600 if unit is Minute.</param>
        /// <param name="sessionTemplate">United States (US) stock market session templates, that extend bars returned to include those outside of the regular trading session. Ignored for non-US equity symbols. (optional)</param>
        /// <param name="lastDate">The date to use as the end point when getting days back. Date is of form MM-DD-YYYY, and optionally can specify a starting time with format MM-DD-YYYYt08:00:00 and even further UTC offset with format MM-DD-YYYYt12:00:00-0600 (optional)</param>
        /// <returns>BarchartDefinition</returns>
        public BarchartDefinition StreamBarchartsDaysBack (string accessToken, string symbol, int? interval, string unit, int? daysBack, string sessionTemplate = null, string lastDate = null)
        {
             ApiResponse<BarchartDefinition> localVarResponse = StreamBarchartsDaysBackWithHttpInfo(accessToken, symbol, interval, unit, daysBack, sessionTemplate, lastDate);
             return localVarResponse.Data;
        }

        /// <summary>
        /// Stream BarChart - Days Back  Streams barchart data starting from a number of days back from last date, each bar filling interval of unit. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">A Symbol Name</param>
        /// <param name="interval">Interval that each bar will consist of. **For Daily, Weekly, and Monthly units this value must be 1.**</param>
        /// <param name="unit">Unit of time for each bar interval.</param>
        /// <param name="daysBack">The number of bars to stream, going back from time 00:00:00 of the day specified in lastDate. Cannot exceed greater than 57600 if unit is Minute.</param>
        /// <param name="sessionTemplate">United States (US) stock market session templates, that extend bars returned to include those outside of the regular trading session. Ignored for non-US equity symbols. (optional)</param>
        /// <param name="lastDate">The date to use as the end point when getting days back. Date is of form MM-DD-YYYY, and optionally can specify a starting time with format MM-DD-YYYYt08:00:00 and even further UTC offset with format MM-DD-YYYYt12:00:00-0600 (optional)</param>
        /// <returns>ApiResponse of BarchartDefinition</returns>
        public ApiResponse< BarchartDefinition > StreamBarchartsDaysBackWithHttpInfo (string accessToken, string symbol, int? interval, string unit, int? daysBack, string sessionTemplate = null, string lastDate = null)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling MarketdataApi->StreamBarchartsDaysBack");
            // verify the required parameter 'symbol' is set
            if (symbol == null)
                throw new ApiException(400, "Missing required parameter 'symbol' when calling MarketdataApi->StreamBarchartsDaysBack");
            // verify the required parameter 'interval' is set
            if (interval == null)
                throw new ApiException(400, "Missing required parameter 'interval' when calling MarketdataApi->StreamBarchartsDaysBack");
            // verify the required parameter 'unit' is set
            if (unit == null)
                throw new ApiException(400, "Missing required parameter 'unit' when calling MarketdataApi->StreamBarchartsDaysBack");
            // verify the required parameter 'daysBack' is set
            if (daysBack == null)
                throw new ApiException(400, "Missing required parameter 'daysBack' when calling MarketdataApi->StreamBarchartsDaysBack");

            var localVarPath = "/stream/barchart/{symbol}/{interval}/{unit}";
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
                "text/plain",
                "application/vnd.tradestation.streams+json"
            };
            String localVarHttpHeaderAccept = this.Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            if (symbol != null) localVarPathParams.Add("symbol", this.Configuration.ApiClient.ParameterToString(symbol)); // path parameter
            if (interval != null) localVarPathParams.Add("interval", this.Configuration.ApiClient.ParameterToString(interval)); // path parameter
            if (unit != null) localVarPathParams.Add("unit", this.Configuration.ApiClient.ParameterToString(unit)); // path parameter
            if (accessToken != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "access_token", accessToken)); // query parameter
            if (sessionTemplate != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "SessionTemplate", sessionTemplate)); // query parameter
            if (daysBack != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "daysBack", daysBack)); // query parameter
            if (lastDate != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "lastDate", lastDate)); // query parameter

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
                Exception exception = ExceptionFactory("StreamBarchartsDaysBack", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<BarchartDefinition>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (BarchartDefinition) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(BarchartDefinition)));
        }

        /// <summary>
        /// Stream BarChart - Days Back  Streams barchart data starting from a number of days back from last date, each bar filling interval of unit. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">A Symbol Name</param>
        /// <param name="interval">Interval that each bar will consist of. **For Daily, Weekly, and Monthly units this value must be 1.**</param>
        /// <param name="unit">Unit of time for each bar interval.</param>
        /// <param name="daysBack">The number of bars to stream, going back from time 00:00:00 of the day specified in lastDate. Cannot exceed greater than 57600 if unit is Minute.</param>
        /// <param name="sessionTemplate">United States (US) stock market session templates, that extend bars returned to include those outside of the regular trading session. Ignored for non-US equity symbols. (optional)</param>
        /// <param name="lastDate">The date to use as the end point when getting days back. Date is of form MM-DD-YYYY, and optionally can specify a starting time with format MM-DD-YYYYt08:00:00 and even further UTC offset with format MM-DD-YYYYt12:00:00-0600 (optional)</param>
        /// <returns>Task of BarchartDefinition</returns>
        public async Task<BarchartDefinition> StreamBarchartsDaysBackAsync (string accessToken, string symbol, int? interval, string unit, int? daysBack, string sessionTemplate = null, string lastDate = null)
        {
             ApiResponse<BarchartDefinition> localVarResponse = await StreamBarchartsDaysBackAsyncWithHttpInfo(accessToken, symbol, interval, unit, daysBack, sessionTemplate, lastDate);
             return localVarResponse.Data;

        }

        /// <summary>
        /// Stream BarChart - Days Back  Streams barchart data starting from a number of days back from last date, each bar filling interval of unit. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">A Symbol Name</param>
        /// <param name="interval">Interval that each bar will consist of. **For Daily, Weekly, and Monthly units this value must be 1.**</param>
        /// <param name="unit">Unit of time for each bar interval.</param>
        /// <param name="daysBack">The number of bars to stream, going back from time 00:00:00 of the day specified in lastDate. Cannot exceed greater than 57600 if unit is Minute.</param>
        /// <param name="sessionTemplate">United States (US) stock market session templates, that extend bars returned to include those outside of the regular trading session. Ignored for non-US equity symbols. (optional)</param>
        /// <param name="lastDate">The date to use as the end point when getting days back. Date is of form MM-DD-YYYY, and optionally can specify a starting time with format MM-DD-YYYYt08:00:00 and even further UTC offset with format MM-DD-YYYYt12:00:00-0600 (optional)</param>
        /// <returns>Task of ApiResponse (BarchartDefinition)</returns>
        public async Task<ApiResponse<BarchartDefinition>> StreamBarchartsDaysBackAsyncWithHttpInfo (string accessToken, string symbol, int? interval, string unit, int? daysBack, string sessionTemplate = null, string lastDate = null)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling MarketdataApi->StreamBarchartsDaysBack");
            // verify the required parameter 'symbol' is set
            if (symbol == null)
                throw new ApiException(400, "Missing required parameter 'symbol' when calling MarketdataApi->StreamBarchartsDaysBack");
            // verify the required parameter 'interval' is set
            if (interval == null)
                throw new ApiException(400, "Missing required parameter 'interval' when calling MarketdataApi->StreamBarchartsDaysBack");
            // verify the required parameter 'unit' is set
            if (unit == null)
                throw new ApiException(400, "Missing required parameter 'unit' when calling MarketdataApi->StreamBarchartsDaysBack");
            // verify the required parameter 'daysBack' is set
            if (daysBack == null)
                throw new ApiException(400, "Missing required parameter 'daysBack' when calling MarketdataApi->StreamBarchartsDaysBack");

            var localVarPath = "/stream/barchart/{symbol}/{interval}/{unit}";
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
                "text/plain",
                "application/vnd.tradestation.streams+json"
            };
            String localVarHttpHeaderAccept = this.Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            if (symbol != null) localVarPathParams.Add("symbol", this.Configuration.ApiClient.ParameterToString(symbol)); // path parameter
            if (interval != null) localVarPathParams.Add("interval", this.Configuration.ApiClient.ParameterToString(interval)); // path parameter
            if (unit != null) localVarPathParams.Add("unit", this.Configuration.ApiClient.ParameterToString(unit)); // path parameter
            if (accessToken != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "access_token", accessToken)); // query parameter
            if (sessionTemplate != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "SessionTemplate", sessionTemplate)); // query parameter
            if (daysBack != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "daysBack", daysBack)); // query parameter
            if (lastDate != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "lastDate", lastDate)); // query parameter

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
                Exception exception = ExceptionFactory("StreamBarchartsDaysBack", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<BarchartDefinition>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (BarchartDefinition) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(BarchartDefinition)));
        }

        /// <summary>
        /// Stream BarChart - Starting on Date  Streams barchart data starting from startDate, each bar filling quantity of unit. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">A Symbol Name</param>
        /// <param name="interval">Interval that each bar will consist of. **For Daily, Weekly, and Monthly units this value must be 1.**</param>
        /// <param name="unit">Unit of time for each bar interval.</param>
        /// <param name="startDate">The starting date to begin streaming bars from. Date is of form MM-DD-YYYY, and optionally can specify a starting time with format MM-DD-YYYYt08:00:00 and even further UTC offset with format MM-DD-YYYYt12:00:00-0600</param>
        /// <param name="sessionTemplate">United States (US) stock market session templates, that extend bars returned to include those outside of the regular trading session. Ignored for non-US equity symbols. (optional)</param>
        /// <returns>BarchartDefinition</returns>
        public BarchartDefinition StreamBarchartsFromStartDate (string accessToken, string symbol, int? interval, string unit, string startDate, string sessionTemplate = null)
        {
             ApiResponse<BarchartDefinition> localVarResponse = StreamBarchartsFromStartDateWithHttpInfo(accessToken, symbol, interval, unit, startDate, sessionTemplate);
             return localVarResponse.Data;
        }

        /// <summary>
        /// Stream BarChart - Starting on Date  Streams barchart data starting from startDate, each bar filling quantity of unit. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">A Symbol Name</param>
        /// <param name="interval">Interval that each bar will consist of. **For Daily, Weekly, and Monthly units this value must be 1.**</param>
        /// <param name="unit">Unit of time for each bar interval.</param>
        /// <param name="startDate">The starting date to begin streaming bars from. Date is of form MM-DD-YYYY, and optionally can specify a starting time with format MM-DD-YYYYt08:00:00 and even further UTC offset with format MM-DD-YYYYt12:00:00-0600</param>
        /// <param name="sessionTemplate">United States (US) stock market session templates, that extend bars returned to include those outside of the regular trading session. Ignored for non-US equity symbols. (optional)</param>
        /// <returns>ApiResponse of BarchartDefinition</returns>
        public ApiResponse< BarchartDefinition > StreamBarchartsFromStartDateWithHttpInfo (string accessToken, string symbol, int? interval, string unit, string startDate, string sessionTemplate = null)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling MarketdataApi->StreamBarchartsFromStartDate");
            // verify the required parameter 'symbol' is set
            if (symbol == null)
                throw new ApiException(400, "Missing required parameter 'symbol' when calling MarketdataApi->StreamBarchartsFromStartDate");
            // verify the required parameter 'interval' is set
            if (interval == null)
                throw new ApiException(400, "Missing required parameter 'interval' when calling MarketdataApi->StreamBarchartsFromStartDate");
            // verify the required parameter 'unit' is set
            if (unit == null)
                throw new ApiException(400, "Missing required parameter 'unit' when calling MarketdataApi->StreamBarchartsFromStartDate");
            // verify the required parameter 'startDate' is set
            if (startDate == null)
                throw new ApiException(400, "Missing required parameter 'startDate' when calling MarketdataApi->StreamBarchartsFromStartDate");

            var localVarPath = "/stream/barchart/{symbol}/{interval}/{unit}/{startDate}";
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
                "text/plain",
                "application/vnd.tradestation.streams+json"
            };
            String localVarHttpHeaderAccept = this.Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            if (symbol != null) localVarPathParams.Add("symbol", this.Configuration.ApiClient.ParameterToString(symbol)); // path parameter
            if (interval != null) localVarPathParams.Add("interval", this.Configuration.ApiClient.ParameterToString(interval)); // path parameter
            if (unit != null) localVarPathParams.Add("unit", this.Configuration.ApiClient.ParameterToString(unit)); // path parameter
            if (startDate != null) localVarPathParams.Add("startDate", this.Configuration.ApiClient.ParameterToString(startDate)); // path parameter
            if (accessToken != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "access_token", accessToken)); // query parameter
            if (sessionTemplate != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "SessionTemplate", sessionTemplate)); // query parameter

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
                Exception exception = ExceptionFactory("StreamBarchartsFromStartDate", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<BarchartDefinition>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (BarchartDefinition) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(BarchartDefinition)));
        }

        /// <summary>
        /// Stream BarChart - Starting on Date  Streams barchart data starting from startDate, each bar filling quantity of unit. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">A Symbol Name</param>
        /// <param name="interval">Interval that each bar will consist of. **For Daily, Weekly, and Monthly units this value must be 1.**</param>
        /// <param name="unit">Unit of time for each bar interval.</param>
        /// <param name="startDate">The starting date to begin streaming bars from. Date is of form MM-DD-YYYY, and optionally can specify a starting time with format MM-DD-YYYYt08:00:00 and even further UTC offset with format MM-DD-YYYYt12:00:00-0600</param>
        /// <param name="sessionTemplate">United States (US) stock market session templates, that extend bars returned to include those outside of the regular trading session. Ignored for non-US equity symbols. (optional)</param>
        /// <returns>Task of BarchartDefinition</returns>
        public async Task<HttpStatusCode> StreamBarchartsFromStartDateAsync (string accessToken, string symbol, int? interval, string unit, string startDate,
            BarchartReceivedHandler OnDataReceived, object targetList, CancellationToken cancellationToken, string sessionTemplate = null)
        {
             ApiResponse<BarchartDefinition> localVarResponse = await StreamBarchartsFromStartDateAsyncWithHttpInfo(accessToken, symbol, interval, unit, startDate,
                 OnDataReceived, targetList, cancellationToken, sessionTemplate);
             return (HttpStatusCode)localVarResponse.StatusCode;
        }

        /// <summary>
        /// Stream BarChart - Starting on Date  Streams barchart data starting from startDate, each bar filling quantity of unit. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">A Symbol Name</param>
        /// <param name="interval">Interval that each bar will consist of. **For Daily, Weekly, and Monthly units this value must be 1.**</param>
        /// <param name="unit">Unit of time for each bar interval.</param>
        /// <param name="startDate">The starting date to begin streaming bars from. Date is of form MM-DD-YYYY, and optionally can specify a starting time with format MM-DD-YYYYt08:00:00 and even further UTC offset with format MM-DD-YYYYt12:00:00-0600</param>
        /// <param name="sessionTemplate">United States (US) stock market session templates, that extend bars returned to include those outside of the regular trading session. Ignored for non-US equity symbols. (optional)</param>
        /// <returns>Task of ApiResponse (BarchartDefinition)</returns>
        public async Task<ApiResponse<BarchartDefinition>> StreamBarchartsFromStartDateAsyncWithHttpInfo (string accessToken,
            string symbol, int? interval, string unit, string startDate, BarchartReceivedHandler OnDataReceived, object targetList,
            CancellationToken cancellationToken, string sessionTemplate = null)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling MarketdataApi->StreamBarchartsFromStartDate");
            // verify the required parameter 'symbol' is set
            if (symbol == null)
                throw new ApiException(400, "Missing required parameter 'symbol' when calling MarketdataApi->StreamBarchartsFromStartDate");
            // verify the required parameter 'interval' is set
            if (interval == null)
                throw new ApiException(400, "Missing required parameter 'interval' when calling MarketdataApi->StreamBarchartsFromStartDate");
            // verify the required parameter 'unit' is set
            if (unit == null)
                throw new ApiException(400, "Missing required parameter 'unit' when calling MarketdataApi->StreamBarchartsFromStartDate");
            // verify the required parameter 'startDate' is set
            if (startDate == null)
                throw new ApiException(400, "Missing required parameter 'startDate' when calling MarketdataApi->StreamBarchartsFromStartDate");

            var localVarPath = "/stream/barchart/{symbol}/{interval}/{unit}/{startDate}";
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
                "text/plain",
                "application/vnd.tradestation.streams+json"
            };
            String localVarHttpHeaderAccept = this.Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            if (symbol != null) localVarPathParams.Add("symbol", this.Configuration.ApiClient.ParameterToString(symbol)); // path parameter
            if (interval != null) localVarPathParams.Add("interval", this.Configuration.ApiClient.ParameterToString(interval)); // path parameter
            if (unit != null) localVarPathParams.Add("unit", this.Configuration.ApiClient.ParameterToString(unit)); // path parameter
            if (startDate != null) localVarPathParams.Add("startDate", this.Configuration.ApiClient.ParameterToString(startDate)); // path parameter
            if (accessToken != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "access_token", accessToken)); // query parameter
            if (sessionTemplate != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "SessionTemplate", sessionTemplate)); // query parameter
            
            string messageBuffer = string.Empty;
            object locker = new object();

            // proccess chunks callback (true - continue, false - close stream)
            async Task<bool> OnChunkReceived(string str, Exception ex)
            {
                if (ex != null)
                {
                    OnDataReceived(symbol, null, targetList, ex);
                    return false;
                }
                if (string.IsNullOrEmpty(str)) return true;

                lock (locker)
                {
                    messageBuffer += str;
                }

                while (true)
                {
                    string json;
                    lock (locker)
                    {
                        // check stream stop
                        if (messageBuffer.StartsWith("END"))
                        {
                            OnDataReceived(symbol, null, targetList, new Exception("END of stream message received"));
                            return false;
                        }
                        else if (messageBuffer.StartsWith("ERROR"))
                        {
                            OnDataReceived(symbol, null, targetList, new Exception($"ERROR message received ({messageBuffer})"));
                            return false;
                        }
                        int index = messageBuffer.IndexOf('\n');
                        if (index == 0)
                        {
                            messageBuffer = messageBuffer.Substring(1);
                            continue;
                        }
                        else if (index < 0)
                            return true;
                        else
                        {
                            json = messageBuffer.Substring(0, index);
                            messageBuffer = messageBuffer.Substring(index + 1);
                        }
                    }

                    try
                    {
                        await Task.Run(() =>
                        {
                            if (cancellationToken.IsCancellationRequested)
                                return null;

                            return (BarchartDefinition)JsonConvert.DeserializeObject(json, typeof(BarchartDefinition),
                                new JsonSerializerSettings { ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor });
                        }).ContinueWith((task) =>
                        {
                            if (task.Result != null)
                                OnDataReceived(symbol, task.Result, targetList, null);
                        });
                    }
                    catch (Exception e)
                    {
                        OnDataReceived(symbol, null, targetList, e);
                        return false;
                    }
                }
            }

            // make the HTTP request
            IRestResponse localVarResponse = (IRestResponse) await this.Configuration.ApiClient.CallApiStreamAsync(localVarPath,
                Method.GET, localVarQueryParams, localVarPostBody, localVarHeaderParams, localVarFormParams, localVarFileParams,
                localVarPathParams, localVarHttpContentType, OnChunkReceived, cancellationToken);

            int localVarStatusCode = (int) localVarResponse.StatusCode;

            /*
            if (ExceptionFactory != null)
            {
                Exception exception = ExceptionFactory("StreamBarchartsFromStartDate", localVarResponse);
                if (exception != null) throw exception;
            }
            */
            return new ApiResponse<BarchartDefinition>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (BarchartDefinition) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(BarchartDefinition)));
        }

        /// <summary>
        /// Stream BarChart - Date Range  Streams barchart data starting from startDate to end date, each bar filling interval of unit. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">A Symbol Name</param>
        /// <param name="interval">Interval that each bar will consist of. **For Daily, Weekly, and Monthly units this value must be 1.**</param>
        /// <param name="unit">Unit of time for each bar interval.</param>
        /// <param name="startDate">The starting date to begin streaming bars from. Date is of form MM-DD-YYYY, and optionally can specify a starting time with format MM-DD-YYYYt08:00:00 and even further UTC offset with format MM-DD-YYYYt12:00:00-0600</param>
        /// <param name="endDate">The ending date for bars streamed. Date is of form MM-DD-YYYY, and optionally can specify a starting time with format MM-DD-YYYYt08:00:00 and even further UTC offset with format MM-DD-YYYYt12:00:00-0600</param>
        /// <param name="sessionTemplate">United States (US) stock market session templates, that extend bars returned to include those outside of the regular trading session. Ignored for non-US equity symbols. (optional)</param>
        /// <returns>BarchartDefinition</returns>
        public BarchartDefinition StreamBarchartsFromStartDateToEndDate (string accessToken, string symbol, int? interval, string unit, string startDate, string endDate, string sessionTemplate = null)
        {
             ApiResponse<BarchartDefinition> localVarResponse = StreamBarchartsFromStartDateToEndDateWithHttpInfo(accessToken, symbol, interval, unit, startDate, endDate, sessionTemplate);
             return localVarResponse.Data;
        }

        /// <summary>
        /// Stream BarChart - Date Range  Streams barchart data starting from startDate to end date, each bar filling interval of unit. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">A Symbol Name</param>
        /// <param name="interval">Interval that each bar will consist of. **For Daily, Weekly, and Monthly units this value must be 1.**</param>
        /// <param name="unit">Unit of time for each bar interval.</param>
        /// <param name="startDate">The starting date to begin streaming bars from. Date is of form MM-DD-YYYY, and optionally can specify a starting time with format MM-DD-YYYYt08:00:00 and even further UTC offset with format MM-DD-YYYYt12:00:00-0600</param>
        /// <param name="endDate">The ending date for bars streamed. Date is of form MM-DD-YYYY, and optionally can specify a starting time with format MM-DD-YYYYt08:00:00 and even further UTC offset with format MM-DD-YYYYt12:00:00-0600</param>
        /// <param name="sessionTemplate">United States (US) stock market session templates, that extend bars returned to include those outside of the regular trading session. Ignored for non-US equity symbols. (optional)</param>
        /// <returns>ApiResponse of BarchartDefinition</returns>
        public ApiResponse< BarchartDefinition > StreamBarchartsFromStartDateToEndDateWithHttpInfo (string accessToken, string symbol, int? interval, string unit, string startDate, string endDate, string sessionTemplate = null)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling MarketdataApi->StreamBarchartsFromStartDateToEndDate");
            // verify the required parameter 'symbol' is set
            if (symbol == null)
                throw new ApiException(400, "Missing required parameter 'symbol' when calling MarketdataApi->StreamBarchartsFromStartDateToEndDate");
            // verify the required parameter 'interval' is set
            if (interval == null)
                throw new ApiException(400, "Missing required parameter 'interval' when calling MarketdataApi->StreamBarchartsFromStartDateToEndDate");
            // verify the required parameter 'unit' is set
            if (unit == null)
                throw new ApiException(400, "Missing required parameter 'unit' when calling MarketdataApi->StreamBarchartsFromStartDateToEndDate");
            // verify the required parameter 'startDate' is set
            if (startDate == null)
                throw new ApiException(400, "Missing required parameter 'startDate' when calling MarketdataApi->StreamBarchartsFromStartDateToEndDate");
            // verify the required parameter 'endDate' is set
            if (endDate == null)
                throw new ApiException(400, "Missing required parameter 'endDate' when calling MarketdataApi->StreamBarchartsFromStartDateToEndDate");

            var localVarPath = "/stream/barchart/{symbol}/{interval}/{unit}/{startDate}/{endDate}";
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
                "text/plain",
                "application/vnd.tradestation.streams+json"
            };
            String localVarHttpHeaderAccept = this.Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            if (symbol != null) localVarPathParams.Add("symbol", this.Configuration.ApiClient.ParameterToString(symbol)); // path parameter
            if (interval != null) localVarPathParams.Add("interval", this.Configuration.ApiClient.ParameterToString(interval)); // path parameter
            if (unit != null) localVarPathParams.Add("unit", this.Configuration.ApiClient.ParameterToString(unit)); // path parameter
            if (startDate != null) localVarPathParams.Add("startDate", this.Configuration.ApiClient.ParameterToString(startDate)); // path parameter
            if (endDate != null) localVarPathParams.Add("endDate", this.Configuration.ApiClient.ParameterToString(endDate)); // path parameter
            if (accessToken != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "access_token", accessToken)); // query parameter
            if (sessionTemplate != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "SessionTemplate", sessionTemplate)); // query parameter

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
                Exception exception = ExceptionFactory("StreamBarchartsFromStartDateToEndDate", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<BarchartDefinition>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (BarchartDefinition) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(BarchartDefinition)));
        }

        /// <summary>
        /// Stream BarChart - Date Range  Streams barchart data starting from startDate to end date, each bar filling interval of unit. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">A Symbol Name</param>
        /// <param name="interval">Interval that each bar will consist of. **For Daily, Weekly, and Monthly units this value must be 1.**</param>
        /// <param name="unit">Unit of time for each bar interval.</param>
        /// <param name="startDate">The starting date to begin streaming bars from. Date is of form MM-DD-YYYY, and optionally can specify a starting time with format MM-DD-YYYYt08:00:00 and even further UTC offset with format MM-DD-YYYYt12:00:00-0600</param>
        /// <param name="endDate">The ending date for bars streamed. Date is of form MM-DD-YYYY, and optionally can specify a starting time with format MM-DD-YYYYt08:00:00 and even further UTC offset with format MM-DD-YYYYt12:00:00-0600</param>
        /// <param name="sessionTemplate">United States (US) stock market session templates, that extend bars returned to include those outside of the regular trading session. Ignored for non-US equity symbols. (optional)</param>
        /// <returns>Task of BarchartDefinition</returns>
        public async Task<HttpStatusCode> StreamBarchartsFromStartDateToEndDateAsync (string accessToken, string symbol, int? interval, string unit, string startDate, string endDate,
            BarchartReceivedHandler OnDataReceived, object targetList, CancellationToken cancellationToken, string sessionTemplate = null)
        {
             ApiResponse<BarchartDefinition> localVarResponse = await StreamBarchartsFromStartDateToEndDateAsyncWithHttpInfo(accessToken, symbol, interval, unit,
                 startDate, endDate, OnDataReceived, targetList, cancellationToken, sessionTemplate);
             return (HttpStatusCode)localVarResponse.StatusCode;

        }

        /// <summary>
        /// Stream BarChart - Date Range  Streams barchart data starting from startDate to end date, each bar filling interval of unit. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">A Symbol Name</param>
        /// <param name="interval">Interval that each bar will consist of. **For Daily, Weekly, and Monthly units this value must be 1.**</param>
        /// <param name="unit">Unit of time for each bar interval.</param>
        /// <param name="startDate">The starting date to begin streaming bars from. Date is of form MM-DD-YYYY, and optionally can specify a starting time with format MM-DD-YYYYt08:00:00 and even further UTC offset with format MM-DD-YYYYt12:00:00-0600</param>
        /// <param name="endDate">The ending date for bars streamed. Date is of form MM-DD-YYYY, and optionally can specify a starting time with format MM-DD-YYYYt08:00:00 and even further UTC offset with format MM-DD-YYYYt12:00:00-0600</param>
        /// <param name="sessionTemplate">United States (US) stock market session templates, that extend bars returned to include those outside of the regular trading session. Ignored for non-US equity symbols. (optional)</param>
        /// <returns>Task of ApiResponse (BarchartDefinition)</returns>
        public async Task<ApiResponse<BarchartDefinition>> StreamBarchartsFromStartDateToEndDateAsyncWithHttpInfo (string accessToken, string symbol, int? interval, string unit, string startDate, string endDate,
            BarchartReceivedHandler OnDataReceived, object targetList, CancellationToken cancellationToken, string sessionTemplate = null)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling MarketdataApi->StreamBarchartsFromStartDateToEndDate");
            // verify the required parameter 'symbol' is set
            if (symbol == null)
                throw new ApiException(400, "Missing required parameter 'symbol' when calling MarketdataApi->StreamBarchartsFromStartDateToEndDate");
            // verify the required parameter 'interval' is set
            if (interval == null)
                throw new ApiException(400, "Missing required parameter 'interval' when calling MarketdataApi->StreamBarchartsFromStartDateToEndDate");
            // verify the required parameter 'unit' is set
            if (unit == null)
                throw new ApiException(400, "Missing required parameter 'unit' when calling MarketdataApi->StreamBarchartsFromStartDateToEndDate");
            // verify the required parameter 'startDate' is set
            if (startDate == null)
                throw new ApiException(400, "Missing required parameter 'startDate' when calling MarketdataApi->StreamBarchartsFromStartDateToEndDate");
            // verify the required parameter 'endDate' is set
            if (endDate == null)
                throw new ApiException(400, "Missing required parameter 'endDate' when calling MarketdataApi->StreamBarchartsFromStartDateToEndDate");

            var localVarPath = "/stream/barchart/{symbol}/{interval}/{unit}/{startDate}/{endDate}";
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
                "text/plain",
                "application/vnd.tradestation.streams+json"
            };
            String localVarHttpHeaderAccept = this.Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            if (symbol != null) localVarPathParams.Add("symbol", this.Configuration.ApiClient.ParameterToString(symbol)); // path parameter
            if (interval != null) localVarPathParams.Add("interval", this.Configuration.ApiClient.ParameterToString(interval)); // path parameter
            if (unit != null) localVarPathParams.Add("unit", this.Configuration.ApiClient.ParameterToString(unit)); // path parameter
            if (startDate != null) localVarPathParams.Add("startDate", this.Configuration.ApiClient.ParameterToString(startDate)); // path parameter
            if (endDate != null) localVarPathParams.Add("endDate", this.Configuration.ApiClient.ParameterToString(endDate)); // path parameter
            if (accessToken != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "access_token", accessToken)); // query parameter
            if (sessionTemplate != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "SessionTemplate", sessionTemplate)); // query parameter

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



            string messageBuffer = string.Empty;
            object locker = new object();

            // proccess chunks callback (true - continue, false - close stream)
            async Task<bool> OnChunkReceived(string str, Exception ex)
            {
                if (ex != null)
                {
                    OnDataReceived(symbol, null, targetList, ex);
                    return false;
                }
                if (string.IsNullOrEmpty(str)) return true;

                lock (locker)
                {
                    messageBuffer += str;
                }

                while (true)
                {
                    string json;
                    lock (locker)
                    {
                        // check stream stop
                        if (messageBuffer.StartsWith("END"))
                        {
                            OnDataReceived(symbol, null, targetList, new Exception("END of stream message received"));
                            return false;
                        }
                        else if (messageBuffer.StartsWith("ERROR"))
                        {
                            OnDataReceived(symbol, null, targetList, new Exception($"ERROR message received ({messageBuffer})"));
                            return false;
                        }
                        int index = messageBuffer.IndexOf('\n');
                        if (index == 0)
                        {
                            messageBuffer = messageBuffer.Substring(1);
                            continue;
                        }
                        else if (index < 0)
                            return true;
                        else
                        {
                            json = messageBuffer.Substring(0, index);
                            messageBuffer = messageBuffer.Substring(index + 1);
                        }
                    }

                    try
                    {
                        await Task.Run(() =>
                        {
                            if (cancellationToken.IsCancellationRequested)
                                return null;

                            return (BarchartDefinition)JsonConvert.DeserializeObject(json, typeof(BarchartDefinition),
                                new JsonSerializerSettings { ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor });
                        }).ContinueWith((task) =>
                        {
                            if (task.Result != null)
                                OnDataReceived(symbol, task.Result, targetList, null);
                        });
                    }
                    catch (Exception e)
                    {
                        OnDataReceived(symbol, null, targetList, e);
                        return false;
                    }
                }
            }


            // make the HTTP request
            IRestResponse localVarResponse = (IRestResponse) await this.Configuration.ApiClient.CallApiStreamAsync(localVarPath,
                Method.GET, localVarQueryParams, localVarPostBody, localVarHeaderParams, localVarFormParams, localVarFileParams,
                localVarPathParams, localVarHttpContentType, OnChunkReceived, cancellationToken);

            int localVarStatusCode = (int) localVarResponse.StatusCode;

            if (ExceptionFactory != null)
            {
                Exception exception = ExceptionFactory("StreamBarchartsFromStartDateToEndDate", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<BarchartDefinition>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (BarchartDefinition) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(BarchartDefinition)));
        }

        /// <summary>
        /// Stream Quote Changes  Streams the latest Quote information for the given Symbols. The first chunk in the stream is a full quote snapshot - subsequent chunks only contain fields of the quote object that have changed since the last chunk.  An invalid symbol name will result in a response of this form - {\&quot;Symbol\&quot;:\&quot;BADEXAMPLESYMBOL\&quot;,\&quot;Error\&quot;:\&quot;FAILED, EX_INVALID_SYMBOL\&quot;}  If the user is not entitled for the symbol requested, response will be of this form - {\&quot;Symbol\&quot;:\&quot;EXAMPLESYMBOL\&quot;,\&quot;Error\&quot;:\&quot;FAILED, EX_NOT_ENTITLED\&quot;} 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbols">1 or more Symbol Names (comma-separated)</param>
        /// <param name="transferEncoding">a header with the value of &#x60;Chunked&#x60; must be passed to streaming resources</param>
        /// <returns>QuoteDefinition</returns>
        public QuoteDefinition StreamQuotesChanges (string accessToken, string symbols, string transferEncoding)
        {
             ApiResponse<QuoteDefinition> localVarResponse = StreamQuotesChangesWithHttpInfo(accessToken, symbols, transferEncoding);
             return localVarResponse.Data;
        }

        /// <summary>
        /// Stream Quote Changes  Streams the latest Quote information for the given Symbols. The first chunk in the stream is a full quote snapshot - subsequent chunks only contain fields of the quote object that have changed since the last chunk.  An invalid symbol name will result in a response of this form - {\&quot;Symbol\&quot;:\&quot;BADEXAMPLESYMBOL\&quot;,\&quot;Error\&quot;:\&quot;FAILED, EX_INVALID_SYMBOL\&quot;}  If the user is not entitled for the symbol requested, response will be of this form - {\&quot;Symbol\&quot;:\&quot;EXAMPLESYMBOL\&quot;,\&quot;Error\&quot;:\&quot;FAILED, EX_NOT_ENTITLED\&quot;} 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbols">1 or more Symbol Names (comma-separated)</param>
        /// <param name="transferEncoding">a header with the value of &#x60;Chunked&#x60; must be passed to streaming resources</param>
        /// <returns>ApiResponse of QuoteDefinition</returns>
        public ApiResponse< QuoteDefinition > StreamQuotesChangesWithHttpInfo (string accessToken, string symbols, string transferEncoding)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling MarketdataApi->StreamQuotesChanges");
            // verify the required parameter 'symbols' is set
            if (symbols == null)
                throw new ApiException(400, "Missing required parameter 'symbols' when calling MarketdataApi->StreamQuotesChanges");
            // verify the required parameter 'transferEncoding' is set
            if (transferEncoding == null)
                throw new ApiException(400, "Missing required parameter 'transferEncoding' when calling MarketdataApi->StreamQuotesChanges");

            var localVarPath = "/stream/quote/changes/{symbols}";
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
                "text/plain",
                "application/vnd.tradestation.streams+json"
            };
            String localVarHttpHeaderAccept = this.Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            if (symbols != null) localVarPathParams.Add("symbols", this.Configuration.ApiClient.ParameterToString(symbols)); // path parameter
            if (accessToken != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "access_token", accessToken)); // query parameter
            if (transferEncoding != null) localVarHeaderParams.Add("Transfer-Encoding", this.Configuration.ApiClient.ParameterToString(transferEncoding)); // header parameter

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
                Exception exception = ExceptionFactory("StreamQuotesChanges", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<QuoteDefinition>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (QuoteDefinition) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(QuoteDefinition)));
        }

        /// <summary>
        /// Stream Quote Changes  Streams the latest Quote information for the given Symbols. The first chunk in the stream is a full quote snapshot - subsequent chunks only contain fields of the quote object that have changed since the last chunk.  An invalid symbol name will result in a response of this form - {\&quot;Symbol\&quot;:\&quot;BADEXAMPLESYMBOL\&quot;,\&quot;Error\&quot;:\&quot;FAILED, EX_INVALID_SYMBOL\&quot;}  If the user is not entitled for the symbol requested, response will be of this form - {\&quot;Symbol\&quot;:\&quot;EXAMPLESYMBOL\&quot;,\&quot;Error\&quot;:\&quot;FAILED, EX_NOT_ENTITLED\&quot;} 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbols">1 or more Symbol Names (comma-separated)</param>
        /// <returns>Task of QuoteDefinition</returns>
        public async Task<HttpStatusCode> StreamQuotesChangesAsync (string accessToken, string symbols, Action<QuoteDefinitionInner, Exception> OnQuoteReceived, CancellationToken cancellationToken)
        {
             ApiResponse<QuoteDefinition> localVarResponse = await StreamQuotesChangesAsyncWithHttpInfo(accessToken, symbols, OnQuoteReceived, cancellationToken);
             return (HttpStatusCode)localVarResponse.StatusCode;
        }

        /// <summary>
        /// Stream Quote Changes  Streams the latest Quote information for the given Symbols. The first chunk in the stream is a full quote snapshot - subsequent chunks only contain fields of the quote object that have changed since the last chunk.  An invalid symbol name will result in a response of this form - {\&quot;Symbol\&quot;:\&quot;BADEXAMPLESYMBOL\&quot;,\&quot;Error\&quot;:\&quot;FAILED, EX_INVALID_SYMBOL\&quot;}  If the user is not entitled for the symbol requested, response will be of this form - {\&quot;Symbol\&quot;:\&quot;EXAMPLESYMBOL\&quot;,\&quot;Error\&quot;:\&quot;FAILED, EX_NOT_ENTITLED\&quot;} 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbols">1 or more Symbol Names (comma-separated)</param>
        /// <returns>Task of ApiResponse (QuoteDefinition)</returns>
        public async Task<ApiResponse<QuoteDefinition>> StreamQuotesChangesAsyncWithHttpInfo (string accessToken, string symbols,
            Action<QuoteDefinitionInner, Exception> OnQuoteReceived, CancellationToken cancellationToken)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling MarketdataApi->StreamQuotesChanges");
            // verify the required parameter 'symbols' is set
            if (symbols == null)
                throw new ApiException(400, "Missing required parameter 'symbols' when calling MarketdataApi->StreamQuotesChanges");
            // verify the required parameter 'OnChunkReceived' is set
            if (OnQuoteReceived == null)
                throw new ApiException(400, "Missing required parameter 'OnChunkReceived' when calling MarketdataApi->StreamQuotesChanges");

            var localVarPath = "/stream/quote/changes/{symbols}";
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
                "text/plain",
                "application/vnd.tradestation.streams+json"
            };
            String localVarHttpHeaderAccept = this.Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            if (symbols != null) localVarPathParams.Add("symbols", this.Configuration.ApiClient.ParameterToString(symbols)); // path parameter
            if (accessToken != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "access_token", accessToken)); // query parameter

            string messageBuffer = string.Empty;

            // proccess chunks callback (true - continue, false - close stream)
            async Task<bool> OnChunkReceived(string str, Exception ex)
            {
                if (ex != null)
                {
                    OnQuoteReceived(null, ex);
                    return false;
                }
                if (string.IsNullOrEmpty(str)) return true;

                messageBuffer += str;

                // check stream stop
                if (messageBuffer.StartsWith("END"))
                {
                    OnQuoteReceived(null, new Exception("END of stream message received"));
                    return false;
                }
                else if (messageBuffer.StartsWith("ERROR"))
                {
                    OnQuoteReceived(null, new Exception($"ERROR message received ({messageBuffer})"));
                    return false;
                }

                int index = messageBuffer.IndexOf('\n');
                while (index >= 0)
                {
                    if (index > 1) // not empty
                    {
                        string json = messageBuffer.Substring(0, index);
                        QuoteDefinitionInner quote = null;
                        try
                        {
                            quote = (QuoteDefinitionInner) JsonConvert.DeserializeObject(json, typeof(QuoteDefinitionInner),
                                new JsonSerializerSettings { ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor });
                        }
                        catch(Exception e)
                        {
                            OnQuoteReceived(null, e);
                            messageBuffer = string.Empty;
                            return false;
                        }
                        OnQuoteReceived(quote, null);
                    }
                    messageBuffer = messageBuffer.Substring(index + 1);
                    index = messageBuffer.IndexOf('\n');
                }
                return await Task.FromResult(true);
            }

            // make the HTTP request
            IRestResponse localVarResponse = (IRestResponse) await this.Configuration.ApiClient.CallApiStreamAsync(localVarPath,
                Method.GET, localVarQueryParams, localVarPostBody, localVarHeaderParams, localVarFormParams, localVarFileParams,
                localVarPathParams, localVarHttpContentType, OnChunkReceived, cancellationToken);

            int localVarStatusCode = (int) localVarResponse.StatusCode;

/*
            if (ExceptionFactory != null)
            {
                Exception exception = ExceptionFactory("StreamQuotesChanges", localVarResponse);
                if (exception != null) throw exception;
            }
*/
            return new ApiResponse<QuoteDefinition>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (QuoteDefinition) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(QuoteDefinition)));
        }

        /// <summary>
        /// Stream Quote Snapshots  Streams the latest Quote for the given Symbols. Each chunk is a full quote object.  An invalid symbol name will result in a response of this form - {\&quot;Symbol\&quot;:\&quot;BADSYMBOLEXAMPLE\&quot;,\&quot;Error\&quot;:\&quot;FAILED, EX_INVALID_SYMBOL\&quot;}  If the user is not entitled for the symbol requested, response will be of this form - {\&quot;Symbol\&quot;:\&quot;EXAMPLESYMBOL\&quot;,\&quot;Error\&quot;:\&quot;FAILED, EX_NOT_ENTITLED\&quot;} 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbols">1 or more Symbol Names (comma-separated)</param>
        /// <returns>QuoteDefinition</returns>
        public QuoteDefinition StreamQuotesSnapshots (string accessToken, string symbols)
        {
             ApiResponse<QuoteDefinition> localVarResponse = StreamQuotesSnapshotsWithHttpInfo(accessToken, symbols);
             return localVarResponse.Data;
        }

        /// <summary>
        /// Stream Quote Snapshots  Streams the latest Quote for the given Symbols. Each chunk is a full quote object.  An invalid symbol name will result in a response of this form - {\&quot;Symbol\&quot;:\&quot;BADSYMBOLEXAMPLE\&quot;,\&quot;Error\&quot;:\&quot;FAILED, EX_INVALID_SYMBOL\&quot;}  If the user is not entitled for the symbol requested, response will be of this form - {\&quot;Symbol\&quot;:\&quot;EXAMPLESYMBOL\&quot;,\&quot;Error\&quot;:\&quot;FAILED, EX_NOT_ENTITLED\&quot;} 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbols">1 or more Symbol Names (comma-separated)</param>
        /// <returns>ApiResponse of QuoteDefinition</returns>
        public ApiResponse< QuoteDefinition > StreamQuotesSnapshotsWithHttpInfo (string accessToken, string symbols)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling MarketdataApi->StreamQuotesSnapshots");
            // verify the required parameter 'symbols' is set
            if (symbols == null)
                throw new ApiException(400, "Missing required parameter 'symbols' when calling MarketdataApi->StreamQuotesSnapshots");

            var localVarPath = "/stream/quote/snapshots/{symbols}";
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
                "text/plain",
                "application/vnd.tradestation.streams+json"
            };
            String localVarHttpHeaderAccept = this.Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            if (symbols != null) localVarPathParams.Add("symbols", this.Configuration.ApiClient.ParameterToString(symbols)); // path parameter
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
                Exception exception = ExceptionFactory("StreamQuotesSnapshots", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<QuoteDefinition>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (QuoteDefinition) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(QuoteDefinition)));
        }

        /// <summary>
        /// Stream Quote Snapshots  Streams the latest Quote for the given Symbols. Each chunk is a full quote object.  An invalid symbol name will result in a response of this form - {\&quot;Symbol\&quot;:\&quot;BADSYMBOLEXAMPLE\&quot;,\&quot;Error\&quot;:\&quot;FAILED, EX_INVALID_SYMBOL\&quot;}  If the user is not entitled for the symbol requested, response will be of this form - {\&quot;Symbol\&quot;:\&quot;EXAMPLESYMBOL\&quot;,\&quot;Error\&quot;:\&quot;FAILED, EX_NOT_ENTITLED\&quot;} 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbols">1 or more Symbol Names (comma-separated)</param>
        /// <returns>Task of QuoteDefinition</returns>
        public async Task<QuoteDefinition> StreamQuotesSnapshotsAsync (string accessToken, string symbols)
        {
             ApiResponse<QuoteDefinition> localVarResponse = await StreamQuotesSnapshotsAsyncWithHttpInfo(accessToken, symbols);
             return localVarResponse.Data;

        }

        /// <summary>
        /// Stream Quote Snapshots  Streams the latest Quote for the given Symbols. Each chunk is a full quote object.  An invalid symbol name will result in a response of this form - {\&quot;Symbol\&quot;:\&quot;BADSYMBOLEXAMPLE\&quot;,\&quot;Error\&quot;:\&quot;FAILED, EX_INVALID_SYMBOL\&quot;}  If the user is not entitled for the symbol requested, response will be of this form - {\&quot;Symbol\&quot;:\&quot;EXAMPLESYMBOL\&quot;,\&quot;Error\&quot;:\&quot;FAILED, EX_NOT_ENTITLED\&quot;} 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbols">1 or more Symbol Names (comma-separated)</param>
        /// <returns>Task of ApiResponse (QuoteDefinition)</returns>
        public async Task<ApiResponse<QuoteDefinition>> StreamQuotesSnapshotsAsyncWithHttpInfo (string accessToken, string symbols)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling MarketdataApi->StreamQuotesSnapshots");
            // verify the required parameter 'symbols' is set
            if (symbols == null)
                throw new ApiException(400, "Missing required parameter 'symbols' when calling MarketdataApi->StreamQuotesSnapshots");

            var localVarPath = "/stream/quote/snapshots/{symbols}";
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
                "text/plain",
                "application/vnd.tradestation.streams+json"
            };
            String localVarHttpHeaderAccept = this.Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            if (symbols != null) localVarPathParams.Add("symbols", this.Configuration.ApiClient.ParameterToString(symbols)); // path parameter
            if (accessToken != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "access_token", accessToken)); // query parameter

            // make the HTTP request
            IRestResponse localVarResponse = (IRestResponse) await this.Configuration.ApiClient.CallApiAsync(localVarPath,
                Method.GET, localVarQueryParams, localVarPostBody, localVarHeaderParams, localVarFormParams, localVarFileParams,
                localVarPathParams, localVarHttpContentType);

            int localVarStatusCode = (int) localVarResponse.StatusCode;

            if (ExceptionFactory != null)
            {
                Exception exception = ExceptionFactory("StreamQuotesSnapshots", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<QuoteDefinition>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (QuoteDefinition) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(QuoteDefinition)));
        }

        /// <summary>
        /// Stream Tick Bars  Streams tick bars data from a number of bars back, each bar returned separated by interval number of ticks. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">A Symbol Name</param>
        /// <param name="interval">Interval for each bar returned (in ticks)</param>
        /// <param name="barsBack">The number of bars to stream, going back from current time</param>
        /// <returns>TickbarDefinition</returns>
        public TickbarDefinition StreamTickBars (string accessToken, string symbol, int? interval, int? barsBack)
        {
             ApiResponse<TickbarDefinition> localVarResponse = StreamTickBarsWithHttpInfo(accessToken, symbol, interval, barsBack);
             return localVarResponse.Data;
        }

        /// <summary>
        /// Stream Tick Bars  Streams tick bars data from a number of bars back, each bar returned separated by interval number of ticks. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">A Symbol Name</param>
        /// <param name="interval">Interval for each bar returned (in ticks)</param>
        /// <param name="barsBack">The number of bars to stream, going back from current time</param>
        /// <returns>ApiResponse of TickbarDefinition</returns>
        public ApiResponse< TickbarDefinition > StreamTickBarsWithHttpInfo (string accessToken, string symbol, int? interval, int? barsBack)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling MarketdataApi->StreamTickBars");
            // verify the required parameter 'symbol' is set
            if (symbol == null)
                throw new ApiException(400, "Missing required parameter 'symbol' when calling MarketdataApi->StreamTickBars");
            // verify the required parameter 'interval' is set
            if (interval == null)
                throw new ApiException(400, "Missing required parameter 'interval' when calling MarketdataApi->StreamTickBars");
            // verify the required parameter 'barsBack' is set
            if (barsBack == null)
                throw new ApiException(400, "Missing required parameter 'barsBack' when calling MarketdataApi->StreamTickBars");

            var localVarPath = "/stream/tickbars/{symbol}/{interval}/{barsBack}";
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
                "text/plain",
                "application/vnd.tradestation.streams+json"
            };
            String localVarHttpHeaderAccept = this.Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            if (symbol != null) localVarPathParams.Add("symbol", this.Configuration.ApiClient.ParameterToString(symbol)); // path parameter
            if (interval != null) localVarPathParams.Add("interval", this.Configuration.ApiClient.ParameterToString(interval)); // path parameter
            if (barsBack != null) localVarPathParams.Add("barsBack", this.Configuration.ApiClient.ParameterToString(barsBack)); // path parameter
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
                Exception exception = ExceptionFactory("StreamTickBars", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<TickbarDefinition>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (TickbarDefinition) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(TickbarDefinition)));
        }

        /// <summary>
        /// Stream Tick Bars  Streams tick bars data from a number of bars back, each bar returned separated by interval number of ticks. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">A Symbol Name</param>
        /// <param name="interval">Interval for each bar returned (in ticks)</param>
        /// <param name="barsBack">The number of bars to stream, going back from current time</param>
        /// <returns>Task of TickbarDefinition</returns>
        public async Task<TickbarDefinition> StreamTickBarsAsync (string accessToken, string symbol, int? interval, int? barsBack)
        {
             ApiResponse<TickbarDefinition> localVarResponse = await StreamTickBarsAsyncWithHttpInfo(accessToken, symbol, interval, barsBack);
             return localVarResponse.Data;

        }

        /// <summary>
        /// Stream Tick Bars  Streams tick bars data from a number of bars back, each bar returned separated by interval number of ticks. 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="symbol">A Symbol Name</param>
        /// <param name="interval">Interval for each bar returned (in ticks)</param>
        /// <param name="barsBack">The number of bars to stream, going back from current time</param>
        /// <returns>Task of ApiResponse (TickbarDefinition)</returns>
        public async Task<ApiResponse<TickbarDefinition>> StreamTickBarsAsyncWithHttpInfo (string accessToken, string symbol, int? interval, int? barsBack)
        {
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling MarketdataApi->StreamTickBars");
            // verify the required parameter 'symbol' is set
            if (symbol == null)
                throw new ApiException(400, "Missing required parameter 'symbol' when calling MarketdataApi->StreamTickBars");
            // verify the required parameter 'interval' is set
            if (interval == null)
                throw new ApiException(400, "Missing required parameter 'interval' when calling MarketdataApi->StreamTickBars");
            // verify the required parameter 'barsBack' is set
            if (barsBack == null)
                throw new ApiException(400, "Missing required parameter 'barsBack' when calling MarketdataApi->StreamTickBars");

            var localVarPath = "/stream/tickbars/{symbol}/{interval}/{barsBack}";
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
                "text/plain",
                "application/vnd.tradestation.streams+json"
            };
            String localVarHttpHeaderAccept = this.Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            if (symbol != null) localVarPathParams.Add("symbol", this.Configuration.ApiClient.ParameterToString(symbol)); // path parameter
            if (interval != null) localVarPathParams.Add("interval", this.Configuration.ApiClient.ParameterToString(interval)); // path parameter
            if (barsBack != null) localVarPathParams.Add("barsBack", this.Configuration.ApiClient.ParameterToString(barsBack)); // path parameter
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
                Exception exception = ExceptionFactory("StreamTickBars", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<TickbarDefinition>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (TickbarDefinition) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(TickbarDefinition)));
        }

        /// <summary>
        /// Suggest Symbols  Suggests symbols semantically based upon partial input of symbol name, company name, or description 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="top">The top number of results to return</param>
        /// <param name="filter">An OData filter to apply to the results</param>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="text">Symbol text for suggestion</param>
        /// <returns>SymbolSuggestDefinition</returns>
        public SymbolSuggestDefinition Suggestsymbols (int? top, string filter, string accessToken, string text)
        {
             ApiResponse<SymbolSuggestDefinition> localVarResponse = SuggestsymbolsWithHttpInfo(top, filter, accessToken, text);
             return localVarResponse.Data;
        }

        /// <summary>
        /// Suggest Symbols  Suggests symbols semantically based upon partial input of symbol name, company name, or description 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="top">The top number of results to return</param>
        /// <param name="filter">An OData filter to apply to the results</param>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="text">Symbol text for suggestion</param>
        /// <returns>ApiResponse of SymbolSuggestDefinition</returns>
        public ApiResponse< SymbolSuggestDefinition > SuggestsymbolsWithHttpInfo (int? top, string filter, string accessToken, string text)
        {
            // verify the required parameter 'top' is set
            if (top == null)
                throw new ApiException(400, "Missing required parameter 'top' when calling MarketdataApi->Suggestsymbols");
            // verify the required parameter 'filter' is set
            if (filter == null)
                throw new ApiException(400, "Missing required parameter 'filter' when calling MarketdataApi->Suggestsymbols");
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling MarketdataApi->Suggestsymbols");
            // verify the required parameter 'text' is set
            if (text == null)
                throw new ApiException(400, "Missing required parameter 'text' when calling MarketdataApi->Suggestsymbols");

            var localVarPath = "/data/symbols/suggest/{text}";
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

            if (text != null) localVarPathParams.Add("text", this.Configuration.ApiClient.ParameterToString(text)); // path parameter
            if (top != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "$top", top)); // query parameter
            if (filter != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "$filter", filter)); // query parameter
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
                Exception exception = ExceptionFactory("Suggestsymbols", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<SymbolSuggestDefinition>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (SymbolSuggestDefinition) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(SymbolSuggestDefinition)));
        }

        /// <summary>
        /// Suggest Symbols  Suggests symbols semantically based upon partial input of symbol name, company name, or description 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="top">The top number of results to return</param>
        /// <param name="filter">An OData filter to apply to the results</param>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="text">Symbol text for suggestion</param>
        /// <returns>Task of SymbolSuggestDefinition</returns>
        public async Task<SymbolSuggestDefinition> SuggestsymbolsAsync (int? top, string filter, string accessToken, string text)
        {
             ApiResponse<SymbolSuggestDefinition> localVarResponse = await SuggestsymbolsAsyncWithHttpInfo(top, filter, accessToken, text);
             return localVarResponse.Data;

        }

        /// <summary>
        /// Suggest Symbols  Suggests symbols semantically based upon partial input of symbol name, company name, or description 
        /// </summary>
        /// <exception cref="TradeStationAPI.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="top">The top number of results to return</param>
        /// <param name="filter">An OData filter to apply to the results</param>
        /// <param name="accessToken">A valid OAuth2 token used to authorize access to the resource</param>
        /// <param name="text">Symbol text for suggestion</param>
        /// <returns>Task of ApiResponse (SymbolSuggestDefinition)</returns>
        public async Task<ApiResponse<SymbolSuggestDefinition>> SuggestsymbolsAsyncWithHttpInfo (int? top, string filter, string accessToken, string text)
        {
            // verify the required parameter 'top' is set
            if (top == null)
                throw new ApiException(400, "Missing required parameter 'top' when calling MarketdataApi->Suggestsymbols");
            // verify the required parameter 'filter' is set
            if (filter == null)
                throw new ApiException(400, "Missing required parameter 'filter' when calling MarketdataApi->Suggestsymbols");
            // verify the required parameter 'accessToken' is set
            if (accessToken == null)
                throw new ApiException(400, "Missing required parameter 'accessToken' when calling MarketdataApi->Suggestsymbols");
            // verify the required parameter 'text' is set
            if (text == null)
                throw new ApiException(400, "Missing required parameter 'text' when calling MarketdataApi->Suggestsymbols");

            var localVarPath = "/data/symbols/suggest/{text}";
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

            if (text != null) localVarPathParams.Add("text", this.Configuration.ApiClient.ParameterToString(text)); // path parameter
            if (top != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "$top", top)); // query parameter
            if (filter != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "$filter", filter)); // query parameter
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
                Exception exception = ExceptionFactory("Suggestsymbols", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<SymbolSuggestDefinition>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (SymbolSuggestDefinition) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(SymbolSuggestDefinition)));
        }

    }
}
