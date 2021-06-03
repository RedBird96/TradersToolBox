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
    /// Status value for Barcharts and Tickbars. Integer value represeting values through bit mappings
    /// </summary>
    [DataContract]
    public partial class StatusDefinition :  IEquatable<StatusDefinition>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StatusDefinition" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected StatusDefinition() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="StatusDefinition" /> class.
        /// </summary>
        /// <param name="bit0">&#x60;NEW&#x60;: Set on the first time the bar is sent (required).</param>
        /// <param name="bit1">&#x60;REAL_TIME_DATA&#x60;: Set when there is data in the bar and the data is being built in \&quot;real time\&quot;\&quot; from a trade (required).</param>
        /// <param name="bit2">&#x60;HISTORICAL_DATA&#x60;: Set when there is data in the bar and the data is historical data, or is built from historical data (required).</param>
        /// <param name="bit3">&#x60;STANDARD_CLOSE&#x60;: Set when the bar is closed \&quot;normally\&quot; (e.g. a 2 tick tickchart bar was closed because of the second tick, a 10-min barchart was closed due to time, etc.) (required).</param>
        /// <param name="bit4">&#x60;END_OF_SESSION_CLOSE&#x60;: Set when the bar was closed \&quot;prematurely\&quot; due to the end of the trading session and the particular bar type is not meant to span trading sessions (required).</param>
        /// <param name="bit5">&#x60;UPDATE_CORPACTION&#x60;: Set when there was an update due to corporate action (required).</param>
        /// <param name="bit6">&#x60;UPDATE_CORRECTION&#x60;: (required).</param>
        /// <param name="bit7">&#x60;ANALYSIS_BAR&#x60;: Set when the bar should not be considered except for analysis purposes (required).</param>
        /// <param name="bit8">&#x60;EXTENDED_BAR&#x60; (required).</param>
        /// <param name="bit19">&#x60;PREV_DAY_CORRECTION&#x60; (required).</param>
        /// <param name="bit23">&#x60;AFTER_MARKET_CORRECTION&#x60; (required).</param>
        /// <param name="bit24">&#x60;PHANTOM_BAR&#x60;: Set when the bar is synthetic - thus created only to fill gaps (required).</param>
        /// <param name="bit25">&#x60;EMPTY_BAR&#x60; (required).</param>
        /// <param name="bit26">&#x60;BACKFILL_DATA&#x60; (required).</param>
        /// <param name="bit27">&#x60;ARCHIVE_DATA&#x60; (required).</param>
        /// <param name="bit28">&#x60;GHOST_BAR&#x60;: Set when the bar is empty but specifically for the end session (required).</param>
        /// <param name="bit29">&#x60;END_OF_HISTORY_STREAM&#x60;: Set on a bar to convey to consumer that all historical bars have been sent.  Historical bars are not guaranteed to be returned in order (required).</param>
        public StatusDefinition(int? bit0 = default(int?), int? bit1 = default(int?), int? bit2 = default(int?), int? bit3 = default(int?), int? bit4 = default(int?), int? bit5 = default(int?), int? bit6 = default(int?), int? bit7 = default(int?), int? bit8 = default(int?), int? bit19 = default(int?), int? bit23 = default(int?), int? bit24 = default(int?), int? bit25 = default(int?), int? bit26 = default(int?), int? bit27 = default(int?), int? bit28 = default(int?), int? bit29 = default(int?))
        {
            // to ensure "bit0" is required (not null)
            if (bit0 == null)
            {
                throw new InvalidDataException("bit0 is a required property for StatusDefinition and cannot be null");
            }
            else
            {
                this.Bit0 = bit0;
            }
            // to ensure "bit1" is required (not null)
            if (bit1 == null)
            {
                throw new InvalidDataException("bit1 is a required property for StatusDefinition and cannot be null");
            }
            else
            {
                this.Bit1 = bit1;
            }
            // to ensure "bit2" is required (not null)
            if (bit2 == null)
            {
                throw new InvalidDataException("bit2 is a required property for StatusDefinition and cannot be null");
            }
            else
            {
                this.Bit2 = bit2;
            }
            // to ensure "bit3" is required (not null)
            if (bit3 == null)
            {
                throw new InvalidDataException("bit3 is a required property for StatusDefinition and cannot be null");
            }
            else
            {
                this.Bit3 = bit3;
            }
            // to ensure "bit4" is required (not null)
            if (bit4 == null)
            {
                throw new InvalidDataException("bit4 is a required property for StatusDefinition and cannot be null");
            }
            else
            {
                this.Bit4 = bit4;
            }
            // to ensure "bit5" is required (not null)
            if (bit5 == null)
            {
                throw new InvalidDataException("bit5 is a required property for StatusDefinition and cannot be null");
            }
            else
            {
                this.Bit5 = bit5;
            }
            // to ensure "bit6" is required (not null)
            if (bit6 == null)
            {
                throw new InvalidDataException("bit6 is a required property for StatusDefinition and cannot be null");
            }
            else
            {
                this.Bit6 = bit6;
            }
            // to ensure "bit7" is required (not null)
            if (bit7 == null)
            {
                throw new InvalidDataException("bit7 is a required property for StatusDefinition and cannot be null");
            }
            else
            {
                this.Bit7 = bit7;
            }
            // to ensure "bit8" is required (not null)
            if (bit8 == null)
            {
                throw new InvalidDataException("bit8 is a required property for StatusDefinition and cannot be null");
            }
            else
            {
                this.Bit8 = bit8;
            }
            // to ensure "bit19" is required (not null)
            if (bit19 == null)
            {
                throw new InvalidDataException("bit19 is a required property for StatusDefinition and cannot be null");
            }
            else
            {
                this.Bit19 = bit19;
            }
            // to ensure "bit23" is required (not null)
            if (bit23 == null)
            {
                throw new InvalidDataException("bit23 is a required property for StatusDefinition and cannot be null");
            }
            else
            {
                this.Bit23 = bit23;
            }
            // to ensure "bit24" is required (not null)
            if (bit24 == null)
            {
                throw new InvalidDataException("bit24 is a required property for StatusDefinition and cannot be null");
            }
            else
            {
                this.Bit24 = bit24;
            }
            // to ensure "bit25" is required (not null)
            if (bit25 == null)
            {
                throw new InvalidDataException("bit25 is a required property for StatusDefinition and cannot be null");
            }
            else
            {
                this.Bit25 = bit25;
            }
            // to ensure "bit26" is required (not null)
            if (bit26 == null)
            {
                throw new InvalidDataException("bit26 is a required property for StatusDefinition and cannot be null");
            }
            else
            {
                this.Bit26 = bit26;
            }
            // to ensure "bit27" is required (not null)
            if (bit27 == null)
            {
                throw new InvalidDataException("bit27 is a required property for StatusDefinition and cannot be null");
            }
            else
            {
                this.Bit27 = bit27;
            }
            // to ensure "bit28" is required (not null)
            if (bit28 == null)
            {
                throw new InvalidDataException("bit28 is a required property for StatusDefinition and cannot be null");
            }
            else
            {
                this.Bit28 = bit28;
            }
            // to ensure "bit29" is required (not null)
            if (bit29 == null)
            {
                throw new InvalidDataException("bit29 is a required property for StatusDefinition and cannot be null");
            }
            else
            {
                this.Bit29 = bit29;
            }
        }
        
        /// <summary>
        /// &#x60;NEW&#x60;: Set on the first time the bar is sent
        /// </summary>
        /// <value>&#x60;NEW&#x60;: Set on the first time the bar is sent</value>
        [DataMember(Name="bit0", EmitDefaultValue=false)]
        public int? Bit0 { get; set; }

        /// <summary>
        /// &#x60;REAL_TIME_DATA&#x60;: Set when there is data in the bar and the data is being built in \&quot;real time\&quot;\&quot; from a trade
        /// </summary>
        /// <value>&#x60;REAL_TIME_DATA&#x60;: Set when there is data in the bar and the data is being built in \&quot;real time\&quot;\&quot; from a trade</value>
        [DataMember(Name="bit1", EmitDefaultValue=false)]
        public int? Bit1 { get; set; }

        /// <summary>
        /// &#x60;HISTORICAL_DATA&#x60;: Set when there is data in the bar and the data is historical data, or is built from historical data
        /// </summary>
        /// <value>&#x60;HISTORICAL_DATA&#x60;: Set when there is data in the bar and the data is historical data, or is built from historical data</value>
        [DataMember(Name="bit2", EmitDefaultValue=false)]
        public int? Bit2 { get; set; }

        /// <summary>
        /// &#x60;STANDARD_CLOSE&#x60;: Set when the bar is closed \&quot;normally\&quot; (e.g. a 2 tick tickchart bar was closed because of the second tick, a 10-min barchart was closed due to time, etc.)
        /// </summary>
        /// <value>&#x60;STANDARD_CLOSE&#x60;: Set when the bar is closed \&quot;normally\&quot; (e.g. a 2 tick tickchart bar was closed because of the second tick, a 10-min barchart was closed due to time, etc.)</value>
        [DataMember(Name="bit3", EmitDefaultValue=false)]
        public int? Bit3 { get; set; }

        /// <summary>
        /// &#x60;END_OF_SESSION_CLOSE&#x60;: Set when the bar was closed \&quot;prematurely\&quot; due to the end of the trading session and the particular bar type is not meant to span trading sessions
        /// </summary>
        /// <value>&#x60;END_OF_SESSION_CLOSE&#x60;: Set when the bar was closed \&quot;prematurely\&quot; due to the end of the trading session and the particular bar type is not meant to span trading sessions</value>
        [DataMember(Name="bit4", EmitDefaultValue=false)]
        public int? Bit4 { get; set; }

        /// <summary>
        /// &#x60;UPDATE_CORPACTION&#x60;: Set when there was an update due to corporate action
        /// </summary>
        /// <value>&#x60;UPDATE_CORPACTION&#x60;: Set when there was an update due to corporate action</value>
        [DataMember(Name="bit5", EmitDefaultValue=false)]
        public int? Bit5 { get; set; }

        /// <summary>
        /// &#x60;UPDATE_CORRECTION&#x60;:
        /// </summary>
        /// <value>&#x60;UPDATE_CORRECTION&#x60;:</value>
        [DataMember(Name="bit6", EmitDefaultValue=false)]
        public int? Bit6 { get; set; }

        /// <summary>
        /// &#x60;ANALYSIS_BAR&#x60;: Set when the bar should not be considered except for analysis purposes
        /// </summary>
        /// <value>&#x60;ANALYSIS_BAR&#x60;: Set when the bar should not be considered except for analysis purposes</value>
        [DataMember(Name="bit7", EmitDefaultValue=false)]
        public int? Bit7 { get; set; }

        /// <summary>
        /// &#x60;EXTENDED_BAR&#x60;
        /// </summary>
        /// <value>&#x60;EXTENDED_BAR&#x60;</value>
        [DataMember(Name="bit8", EmitDefaultValue=false)]
        public int? Bit8 { get; set; }

        /// <summary>
        /// &#x60;PREV_DAY_CORRECTION&#x60;
        /// </summary>
        /// <value>&#x60;PREV_DAY_CORRECTION&#x60;</value>
        [DataMember(Name="bit19", EmitDefaultValue=false)]
        public int? Bit19 { get; set; }

        /// <summary>
        /// &#x60;AFTER_MARKET_CORRECTION&#x60;
        /// </summary>
        /// <value>&#x60;AFTER_MARKET_CORRECTION&#x60;</value>
        [DataMember(Name="bit23", EmitDefaultValue=false)]
        public int? Bit23 { get; set; }

        /// <summary>
        /// &#x60;PHANTOM_BAR&#x60;: Set when the bar is synthetic - thus created only to fill gaps
        /// </summary>
        /// <value>&#x60;PHANTOM_BAR&#x60;: Set when the bar is synthetic - thus created only to fill gaps</value>
        [DataMember(Name="bit24", EmitDefaultValue=false)]
        public int? Bit24 { get; set; }

        /// <summary>
        /// &#x60;EMPTY_BAR&#x60;
        /// </summary>
        /// <value>&#x60;EMPTY_BAR&#x60;</value>
        [DataMember(Name="bit25", EmitDefaultValue=false)]
        public int? Bit25 { get; set; }

        /// <summary>
        /// &#x60;BACKFILL_DATA&#x60;
        /// </summary>
        /// <value>&#x60;BACKFILL_DATA&#x60;</value>
        [DataMember(Name="bit26", EmitDefaultValue=false)]
        public int? Bit26 { get; set; }

        /// <summary>
        /// &#x60;ARCHIVE_DATA&#x60;
        /// </summary>
        /// <value>&#x60;ARCHIVE_DATA&#x60;</value>
        [DataMember(Name="bit27", EmitDefaultValue=false)]
        public int? Bit27 { get; set; }

        /// <summary>
        /// &#x60;GHOST_BAR&#x60;: Set when the bar is empty but specifically for the end session
        /// </summary>
        /// <value>&#x60;GHOST_BAR&#x60;: Set when the bar is empty but specifically for the end session</value>
        [DataMember(Name="bit28", EmitDefaultValue=false)]
        public int? Bit28 { get; set; }

        /// <summary>
        /// &#x60;END_OF_HISTORY_STREAM&#x60;: Set on a bar to convey to consumer that all historical bars have been sent.  Historical bars are not guaranteed to be returned in order
        /// </summary>
        /// <value>&#x60;END_OF_HISTORY_STREAM&#x60;: Set on a bar to convey to consumer that all historical bars have been sent.  Historical bars are not guaranteed to be returned in order</value>
        [DataMember(Name="bit29", EmitDefaultValue=false)]
        public int? Bit29 { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class StatusDefinition {\n");
            sb.Append("  Bit0: ").Append(Bit0).Append("\n");
            sb.Append("  Bit1: ").Append(Bit1).Append("\n");
            sb.Append("  Bit2: ").Append(Bit2).Append("\n");
            sb.Append("  Bit3: ").Append(Bit3).Append("\n");
            sb.Append("  Bit4: ").Append(Bit4).Append("\n");
            sb.Append("  Bit5: ").Append(Bit5).Append("\n");
            sb.Append("  Bit6: ").Append(Bit6).Append("\n");
            sb.Append("  Bit7: ").Append(Bit7).Append("\n");
            sb.Append("  Bit8: ").Append(Bit8).Append("\n");
            sb.Append("  Bit19: ").Append(Bit19).Append("\n");
            sb.Append("  Bit23: ").Append(Bit23).Append("\n");
            sb.Append("  Bit24: ").Append(Bit24).Append("\n");
            sb.Append("  Bit25: ").Append(Bit25).Append("\n");
            sb.Append("  Bit26: ").Append(Bit26).Append("\n");
            sb.Append("  Bit27: ").Append(Bit27).Append("\n");
            sb.Append("  Bit28: ").Append(Bit28).Append("\n");
            sb.Append("  Bit29: ").Append(Bit29).Append("\n");
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
            return this.Equals(input as StatusDefinition);
        }

        /// <summary>
        /// Returns true if StatusDefinition instances are equal
        /// </summary>
        /// <param name="input">Instance of StatusDefinition to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(StatusDefinition input)
        {
            if (input == null)
                return false;

            return 
                (
                    this.Bit0 == input.Bit0 ||
                    (this.Bit0 != null &&
                    this.Bit0.Equals(input.Bit0))
                ) && 
                (
                    this.Bit1 == input.Bit1 ||
                    (this.Bit1 != null &&
                    this.Bit1.Equals(input.Bit1))
                ) && 
                (
                    this.Bit2 == input.Bit2 ||
                    (this.Bit2 != null &&
                    this.Bit2.Equals(input.Bit2))
                ) && 
                (
                    this.Bit3 == input.Bit3 ||
                    (this.Bit3 != null &&
                    this.Bit3.Equals(input.Bit3))
                ) && 
                (
                    this.Bit4 == input.Bit4 ||
                    (this.Bit4 != null &&
                    this.Bit4.Equals(input.Bit4))
                ) && 
                (
                    this.Bit5 == input.Bit5 ||
                    (this.Bit5 != null &&
                    this.Bit5.Equals(input.Bit5))
                ) && 
                (
                    this.Bit6 == input.Bit6 ||
                    (this.Bit6 != null &&
                    this.Bit6.Equals(input.Bit6))
                ) && 
                (
                    this.Bit7 == input.Bit7 ||
                    (this.Bit7 != null &&
                    this.Bit7.Equals(input.Bit7))
                ) && 
                (
                    this.Bit8 == input.Bit8 ||
                    (this.Bit8 != null &&
                    this.Bit8.Equals(input.Bit8))
                ) && 
                (
                    this.Bit19 == input.Bit19 ||
                    (this.Bit19 != null &&
                    this.Bit19.Equals(input.Bit19))
                ) && 
                (
                    this.Bit23 == input.Bit23 ||
                    (this.Bit23 != null &&
                    this.Bit23.Equals(input.Bit23))
                ) && 
                (
                    this.Bit24 == input.Bit24 ||
                    (this.Bit24 != null &&
                    this.Bit24.Equals(input.Bit24))
                ) && 
                (
                    this.Bit25 == input.Bit25 ||
                    (this.Bit25 != null &&
                    this.Bit25.Equals(input.Bit25))
                ) && 
                (
                    this.Bit26 == input.Bit26 ||
                    (this.Bit26 != null &&
                    this.Bit26.Equals(input.Bit26))
                ) && 
                (
                    this.Bit27 == input.Bit27 ||
                    (this.Bit27 != null &&
                    this.Bit27.Equals(input.Bit27))
                ) && 
                (
                    this.Bit28 == input.Bit28 ||
                    (this.Bit28 != null &&
                    this.Bit28.Equals(input.Bit28))
                ) && 
                (
                    this.Bit29 == input.Bit29 ||
                    (this.Bit29 != null &&
                    this.Bit29.Equals(input.Bit29))
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
                if (this.Bit0 != null)
                    hashCode = hashCode * 59 + this.Bit0.GetHashCode();
                if (this.Bit1 != null)
                    hashCode = hashCode * 59 + this.Bit1.GetHashCode();
                if (this.Bit2 != null)
                    hashCode = hashCode * 59 + this.Bit2.GetHashCode();
                if (this.Bit3 != null)
                    hashCode = hashCode * 59 + this.Bit3.GetHashCode();
                if (this.Bit4 != null)
                    hashCode = hashCode * 59 + this.Bit4.GetHashCode();
                if (this.Bit5 != null)
                    hashCode = hashCode * 59 + this.Bit5.GetHashCode();
                if (this.Bit6 != null)
                    hashCode = hashCode * 59 + this.Bit6.GetHashCode();
                if (this.Bit7 != null)
                    hashCode = hashCode * 59 + this.Bit7.GetHashCode();
                if (this.Bit8 != null)
                    hashCode = hashCode * 59 + this.Bit8.GetHashCode();
                if (this.Bit19 != null)
                    hashCode = hashCode * 59 + this.Bit19.GetHashCode();
                if (this.Bit23 != null)
                    hashCode = hashCode * 59 + this.Bit23.GetHashCode();
                if (this.Bit24 != null)
                    hashCode = hashCode * 59 + this.Bit24.GetHashCode();
                if (this.Bit25 != null)
                    hashCode = hashCode * 59 + this.Bit25.GetHashCode();
                if (this.Bit26 != null)
                    hashCode = hashCode * 59 + this.Bit26.GetHashCode();
                if (this.Bit27 != null)
                    hashCode = hashCode * 59 + this.Bit27.GetHashCode();
                if (this.Bit28 != null)
                    hashCode = hashCode * 59 + this.Bit28.GetHashCode();
                if (this.Bit29 != null)
                    hashCode = hashCode * 59 + this.Bit29.GetHashCode();
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
            // Bit0 (int?) maximum
            if(this.Bit0 > (int?)1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Bit0, must be a value less than or equal to 1.", new [] { "Bit0" });
            }

            // Bit0 (int?) minimum
            if(this.Bit0 < (int?)0)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Bit0, must be a value greater than or equal to 0.", new [] { "Bit0" });
            }

            // Bit1 (int?) maximum
            if(this.Bit1 > (int?)1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Bit1, must be a value less than or equal to 1.", new [] { "Bit1" });
            }

            // Bit1 (int?) minimum
            if(this.Bit1 < (int?)0)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Bit1, must be a value greater than or equal to 0.", new [] { "Bit1" });
            }

            // Bit2 (int?) maximum
            if(this.Bit2 > (int?)1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Bit2, must be a value less than or equal to 1.", new [] { "Bit2" });
            }

            // Bit2 (int?) minimum
            if(this.Bit2 < (int?)0)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Bit2, must be a value greater than or equal to 0.", new [] { "Bit2" });
            }

            // Bit3 (int?) maximum
            if(this.Bit3 > (int?)1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Bit3, must be a value less than or equal to 1.", new [] { "Bit3" });
            }

            // Bit3 (int?) minimum
            if(this.Bit3 < (int?)0)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Bit3, must be a value greater than or equal to 0.", new [] { "Bit3" });
            }

            // Bit4 (int?) maximum
            if(this.Bit4 > (int?)1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Bit4, must be a value less than or equal to 1.", new [] { "Bit4" });
            }

            // Bit4 (int?) minimum
            if(this.Bit4 < (int?)0)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Bit4, must be a value greater than or equal to 0.", new [] { "Bit4" });
            }

            // Bit5 (int?) maximum
            if(this.Bit5 > (int?)1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Bit5, must be a value less than or equal to 1.", new [] { "Bit5" });
            }

            // Bit5 (int?) minimum
            if(this.Bit5 < (int?)0)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Bit5, must be a value greater than or equal to 0.", new [] { "Bit5" });
            }

            // Bit6 (int?) maximum
            if(this.Bit6 > (int?)1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Bit6, must be a value less than or equal to 1.", new [] { "Bit6" });
            }

            // Bit6 (int?) minimum
            if(this.Bit6 < (int?)0)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Bit6, must be a value greater than or equal to 0.", new [] { "Bit6" });
            }

            // Bit7 (int?) maximum
            if(this.Bit7 > (int?)1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Bit7, must be a value less than or equal to 1.", new [] { "Bit7" });
            }

            // Bit7 (int?) minimum
            if(this.Bit7 < (int?)0)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Bit7, must be a value greater than or equal to 0.", new [] { "Bit7" });
            }

            // Bit8 (int?) maximum
            if(this.Bit8 > (int?)1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Bit8, must be a value less than or equal to 1.", new [] { "Bit8" });
            }

            // Bit8 (int?) minimum
            if(this.Bit8 < (int?)0)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Bit8, must be a value greater than or equal to 0.", new [] { "Bit8" });
            }

            // Bit19 (int?) maximum
            if(this.Bit19 > (int?)1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Bit19, must be a value less than or equal to 1.", new [] { "Bit19" });
            }

            // Bit19 (int?) minimum
            if(this.Bit19 < (int?)0)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Bit19, must be a value greater than or equal to 0.", new [] { "Bit19" });
            }

            // Bit23 (int?) maximum
            if(this.Bit23 > (int?)1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Bit23, must be a value less than or equal to 1.", new [] { "Bit23" });
            }

            // Bit23 (int?) minimum
            if(this.Bit23 < (int?)0)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Bit23, must be a value greater than or equal to 0.", new [] { "Bit23" });
            }

            // Bit24 (int?) maximum
            if(this.Bit24 > (int?)1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Bit24, must be a value less than or equal to 1.", new [] { "Bit24" });
            }

            // Bit24 (int?) minimum
            if(this.Bit24 < (int?)0)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Bit24, must be a value greater than or equal to 0.", new [] { "Bit24" });
            }

            // Bit25 (int?) maximum
            if(this.Bit25 > (int?)1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Bit25, must be a value less than or equal to 1.", new [] { "Bit25" });
            }

            // Bit25 (int?) minimum
            if(this.Bit25 < (int?)0)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Bit25, must be a value greater than or equal to 0.", new [] { "Bit25" });
            }

            // Bit26 (int?) maximum
            if(this.Bit26 > (int?)1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Bit26, must be a value less than or equal to 1.", new [] { "Bit26" });
            }

            // Bit26 (int?) minimum
            if(this.Bit26 < (int?)0)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Bit26, must be a value greater than or equal to 0.", new [] { "Bit26" });
            }

            // Bit27 (int?) maximum
            if(this.Bit27 > (int?)1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Bit27, must be a value less than or equal to 1.", new [] { "Bit27" });
            }

            // Bit27 (int?) minimum
            if(this.Bit27 < (int?)0)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Bit27, must be a value greater than or equal to 0.", new [] { "Bit27" });
            }

            // Bit28 (int?) maximum
            if(this.Bit28 > (int?)1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Bit28, must be a value less than or equal to 1.", new [] { "Bit28" });
            }

            // Bit28 (int?) minimum
            if(this.Bit28 < (int?)0)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Bit28, must be a value greater than or equal to 0.", new [] { "Bit28" });
            }

            // Bit29 (int?) maximum
            if(this.Bit29 > (int?)1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Bit29, must be a value less than or equal to 1.", new [] { "Bit29" });
            }

            // Bit29 (int?) minimum
            if(this.Bit29 < (int?)0)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Bit29, must be a value greater than or equal to 0.", new [] { "Bit29" });
            }

            yield break;
        }
    }

}
