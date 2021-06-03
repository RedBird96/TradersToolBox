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
    /// OrderRequestDefinition
    /// </summary>
    [DataContract]
    public partial class OrderRequestDefinition :  IEquatable<OrderRequestDefinition>, IValidatableObject
    {
        /// <summary>
        /// Defines AssetType
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum AssetTypeEnum
        {
            
            /// <summary>
            /// Enum EQ for value: EQ
            /// </summary>
            [EnumMember(Value = "EQ")]
            EQ = 1,
            
            /// <summary>
            /// Enum FU for value: FU
            /// </summary>
            [EnumMember(Value = "FU")]
            FU = 2,
            
            /// <summary>
            /// Enum OP for value: OP
            /// </summary>
            [EnumMember(Value = "OP")]
            OP = 3
        }

        /// <summary>
        /// Gets or Sets AssetType
        /// </summary>
        [DataMember(Name="AssetType", EmitDefaultValue=false)]
        public AssetTypeEnum AssetType { get; set; }
        /// <summary>
        /// Allowed durations vary by Asset Type * DAY - Day, valid until the end of the regular trading session. * DYP - Day Plus; valid until the end of the extended trading session * GTC - Good till canceled * GCP - Good till canceled plus * GTD - Good through date * GDP - Good through date plus * OPG - At the opening; only valid for listed stocks at the opening session Price * CLO - On Close; orders that target the closing session of an exchange. * IOC - Immediate or Cancel; filled immediately or canceled, partial fills are accepted * FOK - Fill or Kill; orders are filled entirely or canceled, partial fills are not accepted * 1 or 1 MIN - 1 minute; expires after the 1 minute * 3 or 3 MIN - 3 minutes; expires after the 3 minutes * 5 or 5 MIN - 5 minutes; expires after the 5 minutes 
        /// </summary>
        /// <value>Allowed durations vary by Asset Type * DAY - Day, valid until the end of the regular trading session. * DYP - Day Plus; valid until the end of the extended trading session * GTC - Good till canceled * GCP - Good till canceled plus * GTD - Good through date * GDP - Good through date plus * OPG - At the opening; only valid for listed stocks at the opening session Price * CLO - On Close; orders that target the closing session of an exchange. * IOC - Immediate or Cancel; filled immediately or canceled, partial fills are accepted * FOK - Fill or Kill; orders are filled entirely or canceled, partial fills are not accepted * 1 or 1 MIN - 1 minute; expires after the 1 minute * 3 or 3 MIN - 3 minutes; expires after the 3 minutes * 5 or 5 MIN - 5 minutes; expires after the 5 minutes </value>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum DurationEnum
        {
            
            /// <summary>
            /// Enum DAY for value: DAY
            /// </summary>
            [EnumMember(Value = "DAY")]
            DAY = 1,
            
            /// <summary>
            /// Enum DYP for value: DYP
            /// </summary>
            [EnumMember(Value = "DYP")]
            DYP = 2,
            
            /// <summary>
            /// Enum GTC for value: GTC
            /// </summary>
            [EnumMember(Value = "GTC")]
            GTC = 3,
            
            /// <summary>
            /// Enum GCP for value: GCP
            /// </summary>
            [EnumMember(Value = "GCP")]
            GCP = 4,
            
            /// <summary>
            /// Enum GTD for value: GTD
            /// </summary>
            [EnumMember(Value = "GTD")]
            GTD = 5,
            
            /// <summary>
            /// Enum GDP for value: GDP
            /// </summary>
            [EnumMember(Value = "GDP")]
            GDP = 6,
            
            /// <summary>
            /// Enum OPG for value: OPG
            /// </summary>
            [EnumMember(Value = "OPG")]
            OPG = 7,
            
            /// <summary>
            /// Enum CLO for value: CLO
            /// </summary>
            [EnumMember(Value = "CLO")]
            CLO = 8,
            
            /// <summary>
            /// Enum IOC for value: IOC
            /// </summary>
            [EnumMember(Value = "IOC")]
            IOC = 9,
            
            /// <summary>
            /// Enum FOK for value: FOK
            /// </summary>
            [EnumMember(Value = "FOK")]
            FOK = 10,
            
            /// <summary>
            /// Enum _1 for value: 1
            /// </summary>
            [EnumMember(Value = "1")]
            _1 = 11,
            
            /// <summary>
            /// Enum _1MIN for value: 1 MIN
            /// </summary>
            [EnumMember(Value = "1 MIN")]
            _1MIN = 12,
            
            /// <summary>
            /// Enum _3 for value: 3
            /// </summary>
            [EnumMember(Value = "3")]
            _3 = 13,
            
            /// <summary>
            /// Enum _3MIN for value: 3 MIN
            /// </summary>
            [EnumMember(Value = "3 MIN")]
            _3MIN = 14,
            
            /// <summary>
            /// Enum _5 for value: 5
            /// </summary>
            [EnumMember(Value = "5")]
            _5 = 15,
            
            /// <summary>
            /// Enum _5MIN for value: 5 MIN
            /// </summary>
            [EnumMember(Value = "5 MIN")]
            _5MIN = 16
        }

        /// <summary>
        /// Allowed durations vary by Asset Type * DAY - Day, valid until the end of the regular trading session. * DYP - Day Plus; valid until the end of the extended trading session * GTC - Good till canceled * GCP - Good till canceled plus * GTD - Good through date * GDP - Good through date plus * OPG - At the opening; only valid for listed stocks at the opening session Price * CLO - On Close; orders that target the closing session of an exchange. * IOC - Immediate or Cancel; filled immediately or canceled, partial fills are accepted * FOK - Fill or Kill; orders are filled entirely or canceled, partial fills are not accepted * 1 or 1 MIN - 1 minute; expires after the 1 minute * 3 or 3 MIN - 3 minutes; expires after the 3 minutes * 5 or 5 MIN - 5 minutes; expires after the 5 minutes 
        /// </summary>
        /// <value>Allowed durations vary by Asset Type * DAY - Day, valid until the end of the regular trading session. * DYP - Day Plus; valid until the end of the extended trading session * GTC - Good till canceled * GCP - Good till canceled plus * GTD - Good through date * GDP - Good through date plus * OPG - At the opening; only valid for listed stocks at the opening session Price * CLO - On Close; orders that target the closing session of an exchange. * IOC - Immediate or Cancel; filled immediately or canceled, partial fills are accepted * FOK - Fill or Kill; orders are filled entirely or canceled, partial fills are not accepted * 1 or 1 MIN - 1 minute; expires after the 1 minute * 3 or 3 MIN - 3 minutes; expires after the 3 minutes * 5 or 5 MIN - 5 minutes; expires after the 5 minutes </value>
        [DataMember(Name="Duration", EmitDefaultValue=false)]
        public DurationEnum Duration { get; set; }
        /// <summary>
        /// Defines OrderType
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum OrderTypeEnum
        {
            
            /// <summary>
            /// Enum Limit for value: Limit
            /// </summary>
            [EnumMember(Value = "Limit")]
            Limit = 1,
            
            /// <summary>
            /// Enum Market for value: Market
            /// </summary>
            [EnumMember(Value = "Market")]
            Market = 2,
            
            /// <summary>
            /// Enum StopLimit for value: StopLimit
            /// </summary>
            [EnumMember(Value = "StopLimit")]
            StopLimit = 3,
            
            /// <summary>
            /// Enum StopMarket for value: StopMarket
            /// </summary>
            [EnumMember(Value = "StopMarket")]
            StopMarket = 4
        }

        /// <summary>
        /// Gets or Sets OrderType
        /// </summary>
        [DataMember(Name="OrderType", EmitDefaultValue=false)]
        public OrderTypeEnum OrderType { get; set; }
        /// <summary>
        /// Must be UPPERCASE * AMEX - &#x60;AMEX&#x60; - &#x60;EQ&#x60; * ARCA - &#x60;ARCX&#x60; - &#x60;EQ&#x60; * BATS - &#x60;BATS&#x60; - &#x60;EQ&#x60; * BEAR - &#x60;SuperDOT&#x60; - &#x60;EQ&#x60; * BOX - &#x60;BOX&#x60; - &#x60;OP&#x60; * BYX - &#x60;BYX&#x60; - &#x60;EQ&#x60; * CFE - &#x60;CFE&#x60; - &#x60;FU&#x60; * CME - &#x60;CBOT&#x60; - &#x60;FU&#x60; * CME - &#x60;CME&#x60; - &#x60;FU&#x60; * CME - &#x60;COMEX&#x60; - &#x60;FU&#x60; * CME - &#x60;NYMEX&#x60; - &#x60;FU&#x60; * CNX - &#x60;Current Spot FX&#x60; - &#x60;FX&#x60; * CSFB - &#x60;CSFB&#x60; - &#x60;EQ&#x60; * CSRS - &#x60;CSRS&#x60; - &#x60;EQ&#x60; * CSRS - &#x60;TWAP-ALGO&#x60; - &#x60;EQ&#x60; * CSRS - &#x60;VWAP-ALGO&#x60; - &#x60;EQ&#x60; * CSRX - &#x60;POV-ALGO&#x60; - &#x60;EQ&#x60; * CTDL - &#x60;CDRG&#x60; - &#x60;EQ&#x60; * EDGA - &#x60;EDGA&#x60; - &#x60;EQ&#x60; * EDGX - &#x60;EDGX&#x60; - &#x60;EQ&#x60; * EURX - &#x60;EUREX&#x60; - &#x60;FU&#x60; * IBFX - &#x60;Interbank Fx&#x60; - &#x60;FX&#x60; * ICE - &#x60;ICEUS&#x60; - &#x60;FU&#x60; * ICEB - &#x60;ICEBS&#x60; - &#x60;FU&#x60; * ICEE - &#x60;ICEEU&#x60; - &#x60;FU&#x60; * IEXG - &#x60;IEX&#x60; - &#x60;EQ&#x60; * KCG - &#x60;Knight Link&#x60; - &#x60;EQ&#x60; * NQBX - &#x60;NQBX&#x60; - &#x60;EQ&#x60; * NSDQ - &#x60;NASDAQ Type 1 Retail Orders&#x60; - &#x60;EQ&#x60; * NSDQ - &#x60;NSDQ&#x60; - &#x60;EQ&#x60; * NYSE - &#x60;NYSE Type 1 Retail Orders&#x60; - &#x60;EQ&#x60; * NYSE - &#x60;NYSE&#x60; - &#x60;EQ&#x60; * SIG - &#x60;BATS&#x60; - &#x60;OP&#x60; * SIG - &#x60;C2&#x60; - &#x60;OP&#x60; * SIG - &#x60;EDGO&#x60; - &#x60;OP&#x60; * SIG - &#x60;GMNI&#x60; - &#x60;OP&#x60; * SIG - &#x60;ISE Mercury&#x60; - &#x60;OP&#x60; * SIG - &#x60;MIAX&#x60; - &#x60;OP&#x60; * SIG - &#x60;MPRL&#x60; - &#x60;OP&#x60; * SIG - &#x60;Nasdaq BX&#x60; - &#x60;OP&#x60; * SOHO - &#x60;SOHO&#x60; - &#x60;EQ&#x60; * WEX - &#x60;Sweep-ALGO&#x60; - &#x60;OP&#x60; * WEX - &#x60;SweepPI-ALGO&#x60; - &#x60;OP&#x60; * WEX2 - &#x60;Sweep-ALGO&#x60; - &#x60;EQ&#x60; * WEX2 - &#x60;SweepPI-ALGO&#x60; - &#x60;EQ&#x60; 
        /// </summary>
        /// <value>Must be UPPERCASE * AMEX - &#x60;AMEX&#x60; - &#x60;EQ&#x60; * ARCA - &#x60;ARCX&#x60; - &#x60;EQ&#x60; * BATS - &#x60;BATS&#x60; - &#x60;EQ&#x60; * BEAR - &#x60;SuperDOT&#x60; - &#x60;EQ&#x60; * BOX - &#x60;BOX&#x60; - &#x60;OP&#x60; * BYX - &#x60;BYX&#x60; - &#x60;EQ&#x60; * CFE - &#x60;CFE&#x60; - &#x60;FU&#x60; * CME - &#x60;CBOT&#x60; - &#x60;FU&#x60; * CME - &#x60;CME&#x60; - &#x60;FU&#x60; * CME - &#x60;COMEX&#x60; - &#x60;FU&#x60; * CME - &#x60;NYMEX&#x60; - &#x60;FU&#x60; * CNX - &#x60;Current Spot FX&#x60; - &#x60;FX&#x60; * CSFB - &#x60;CSFB&#x60; - &#x60;EQ&#x60; * CSRS - &#x60;CSRS&#x60; - &#x60;EQ&#x60; * CSRS - &#x60;TWAP-ALGO&#x60; - &#x60;EQ&#x60; * CSRS - &#x60;VWAP-ALGO&#x60; - &#x60;EQ&#x60; * CSRX - &#x60;POV-ALGO&#x60; - &#x60;EQ&#x60; * CTDL - &#x60;CDRG&#x60; - &#x60;EQ&#x60; * EDGA - &#x60;EDGA&#x60; - &#x60;EQ&#x60; * EDGX - &#x60;EDGX&#x60; - &#x60;EQ&#x60; * EURX - &#x60;EUREX&#x60; - &#x60;FU&#x60; * IBFX - &#x60;Interbank Fx&#x60; - &#x60;FX&#x60; * ICE - &#x60;ICEUS&#x60; - &#x60;FU&#x60; * ICEB - &#x60;ICEBS&#x60; - &#x60;FU&#x60; * ICEE - &#x60;ICEEU&#x60; - &#x60;FU&#x60; * IEXG - &#x60;IEX&#x60; - &#x60;EQ&#x60; * KCG - &#x60;Knight Link&#x60; - &#x60;EQ&#x60; * NQBX - &#x60;NQBX&#x60; - &#x60;EQ&#x60; * NSDQ - &#x60;NASDAQ Type 1 Retail Orders&#x60; - &#x60;EQ&#x60; * NSDQ - &#x60;NSDQ&#x60; - &#x60;EQ&#x60; * NYSE - &#x60;NYSE Type 1 Retail Orders&#x60; - &#x60;EQ&#x60; * NYSE - &#x60;NYSE&#x60; - &#x60;EQ&#x60; * SIG - &#x60;BATS&#x60; - &#x60;OP&#x60; * SIG - &#x60;C2&#x60; - &#x60;OP&#x60; * SIG - &#x60;EDGO&#x60; - &#x60;OP&#x60; * SIG - &#x60;GMNI&#x60; - &#x60;OP&#x60; * SIG - &#x60;ISE Mercury&#x60; - &#x60;OP&#x60; * SIG - &#x60;MIAX&#x60; - &#x60;OP&#x60; * SIG - &#x60;MPRL&#x60; - &#x60;OP&#x60; * SIG - &#x60;Nasdaq BX&#x60; - &#x60;OP&#x60; * SOHO - &#x60;SOHO&#x60; - &#x60;EQ&#x60; * WEX - &#x60;Sweep-ALGO&#x60; - &#x60;OP&#x60; * WEX - &#x60;SweepPI-ALGO&#x60; - &#x60;OP&#x60; * WEX2 - &#x60;Sweep-ALGO&#x60; - &#x60;EQ&#x60; * WEX2 - &#x60;SweepPI-ALGO&#x60; - &#x60;EQ&#x60; </value>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum RouteEnum
        {
            
            /// <summary>
            /// Enum Intelligent for value: Intelligent
            /// </summary>
            [EnumMember(Value = "Intelligent")]
            Intelligent = 1,
            
            /// <summary>
            /// Enum AMEX for value: AMEX
            /// </summary>
            [EnumMember(Value = "AMEX")]
            AMEX = 2,
            
            /// <summary>
            /// Enum ARCA for value: ARCA
            /// </summary>
            [EnumMember(Value = "ARCA")]
            ARCA = 3,
            
            /// <summary>
            /// Enum AUTO for value: AUTO
            /// </summary>
            [EnumMember(Value = "AUTO")]
            AUTO = 4,
            
            /// <summary>
            /// Enum BATS for value: BATS
            /// </summary>
            [EnumMember(Value = "BATS")]
            BATS = 5,
            
            /// <summary>
            /// Enum BEAR for value: BEAR
            /// </summary>
            [EnumMember(Value = "BEAR")]
            BEAR = 6,
            
            /// <summary>
            /// Enum BOX for value: BOX
            /// </summary>
            [EnumMember(Value = "BOX")]
            BOX = 7,
            
            /// <summary>
            /// Enum BYX for value: BYX
            /// </summary>
            [EnumMember(Value = "BYX")]
            BYX = 8,
            
            /// <summary>
            /// Enum CFE for value: CFE
            /// </summary>
            [EnumMember(Value = "CFE")]
            CFE = 9,
            
            /// <summary>
            /// Enum CME for value: CME
            /// </summary>
            [EnumMember(Value = "CME")]
            CME = 10,
            
            /// <summary>
            /// Enum CNX for value: CNX
            /// </summary>
            [EnumMember(Value = "CNX")]
            CNX = 11,
            
            /// <summary>
            /// Enum CSFB for value: CSFB
            /// </summary>
            [EnumMember(Value = "CSFB")]
            CSFB = 12,
            
            /// <summary>
            /// Enum CSRS for value: CSRS
            /// </summary>
            [EnumMember(Value = "CSRS")]
            CSRS = 13,
            
            /// <summary>
            /// Enum CSRX for value: CSRX
            /// </summary>
            [EnumMember(Value = "CSRX")]
            CSRX = 14,
            
            /// <summary>
            /// Enum CTDL for value: CTDL
            /// </summary>
            [EnumMember(Value = "CTDL")]
            CTDL = 15,
            
            /// <summary>
            /// Enum EDGA for value: EDGA
            /// </summary>
            [EnumMember(Value = "EDGA")]
            EDGA = 16,
            
            /// <summary>
            /// Enum EDGX for value: EDGX
            /// </summary>
            [EnumMember(Value = "EDGX")]
            EDGX = 17,
            
            /// <summary>
            /// Enum EURX for value: EURX
            /// </summary>
            [EnumMember(Value = "EURX")]
            EURX = 18,
            
            /// <summary>
            /// Enum IBFX for value: IBFX
            /// </summary>
            [EnumMember(Value = "IBFX")]
            IBFX = 19,
            
            /// <summary>
            /// Enum ICE for value: ICE
            /// </summary>
            [EnumMember(Value = "ICE")]
            ICE = 20,
            
            /// <summary>
            /// Enum ICEB for value: ICEB
            /// </summary>
            [EnumMember(Value = "ICEB")]
            ICEB = 21,
            
            /// <summary>
            /// Enum ICEE for value: ICEE
            /// </summary>
            [EnumMember(Value = "ICEE")]
            ICEE = 22,
            
            /// <summary>
            /// Enum IEXG for value: IEXG
            /// </summary>
            [EnumMember(Value = "IEXG")]
            IEXG = 23,
            
            /// <summary>
            /// Enum KCG for value: KCG
            /// </summary>
            [EnumMember(Value = "KCG")]
            KCG = 24,
            
            /// <summary>
            /// Enum NQBX for value: NQBX
            /// </summary>
            [EnumMember(Value = "NQBX")]
            NQBX = 25,
            
            /// <summary>
            /// Enum NSDQ for value: NSDQ
            /// </summary>
            [EnumMember(Value = "NSDQ")]
            NSDQ = 26,
            
            /// <summary>
            /// Enum NYSE for value: NYSE
            /// </summary>
            [EnumMember(Value = "NYSE")]
            NYSE = 27,
            
            /// <summary>
            /// Enum SIG for value: SIG
            /// </summary>
            [EnumMember(Value = "SIG")]
            SIG = 28,
            
            /// <summary>
            /// Enum SOHO for value: SOHO
            /// </summary>
            [EnumMember(Value = "SOHO")]
            SOHO = 29,
            
            /// <summary>
            /// Enum WEX2 for value: WEX2
            /// </summary>
            [EnumMember(Value = "WEX2")]
            WEX2 = 30,
            
            /// <summary>
            /// Enum WEX for value: WEX
            /// </summary>
            [EnumMember(Value = "WEX")]
            WEX = 31
        }

        /// <summary>
        /// Must be UPPERCASE * AMEX - &#x60;AMEX&#x60; - &#x60;EQ&#x60; * ARCA - &#x60;ARCX&#x60; - &#x60;EQ&#x60; * BATS - &#x60;BATS&#x60; - &#x60;EQ&#x60; * BEAR - &#x60;SuperDOT&#x60; - &#x60;EQ&#x60; * BOX - &#x60;BOX&#x60; - &#x60;OP&#x60; * BYX - &#x60;BYX&#x60; - &#x60;EQ&#x60; * CFE - &#x60;CFE&#x60; - &#x60;FU&#x60; * CME - &#x60;CBOT&#x60; - &#x60;FU&#x60; * CME - &#x60;CME&#x60; - &#x60;FU&#x60; * CME - &#x60;COMEX&#x60; - &#x60;FU&#x60; * CME - &#x60;NYMEX&#x60; - &#x60;FU&#x60; * CNX - &#x60;Current Spot FX&#x60; - &#x60;FX&#x60; * CSFB - &#x60;CSFB&#x60; - &#x60;EQ&#x60; * CSRS - &#x60;CSRS&#x60; - &#x60;EQ&#x60; * CSRS - &#x60;TWAP-ALGO&#x60; - &#x60;EQ&#x60; * CSRS - &#x60;VWAP-ALGO&#x60; - &#x60;EQ&#x60; * CSRX - &#x60;POV-ALGO&#x60; - &#x60;EQ&#x60; * CTDL - &#x60;CDRG&#x60; - &#x60;EQ&#x60; * EDGA - &#x60;EDGA&#x60; - &#x60;EQ&#x60; * EDGX - &#x60;EDGX&#x60; - &#x60;EQ&#x60; * EURX - &#x60;EUREX&#x60; - &#x60;FU&#x60; * IBFX - &#x60;Interbank Fx&#x60; - &#x60;FX&#x60; * ICE - &#x60;ICEUS&#x60; - &#x60;FU&#x60; * ICEB - &#x60;ICEBS&#x60; - &#x60;FU&#x60; * ICEE - &#x60;ICEEU&#x60; - &#x60;FU&#x60; * IEXG - &#x60;IEX&#x60; - &#x60;EQ&#x60; * KCG - &#x60;Knight Link&#x60; - &#x60;EQ&#x60; * NQBX - &#x60;NQBX&#x60; - &#x60;EQ&#x60; * NSDQ - &#x60;NASDAQ Type 1 Retail Orders&#x60; - &#x60;EQ&#x60; * NSDQ - &#x60;NSDQ&#x60; - &#x60;EQ&#x60; * NYSE - &#x60;NYSE Type 1 Retail Orders&#x60; - &#x60;EQ&#x60; * NYSE - &#x60;NYSE&#x60; - &#x60;EQ&#x60; * SIG - &#x60;BATS&#x60; - &#x60;OP&#x60; * SIG - &#x60;C2&#x60; - &#x60;OP&#x60; * SIG - &#x60;EDGO&#x60; - &#x60;OP&#x60; * SIG - &#x60;GMNI&#x60; - &#x60;OP&#x60; * SIG - &#x60;ISE Mercury&#x60; - &#x60;OP&#x60; * SIG - &#x60;MIAX&#x60; - &#x60;OP&#x60; * SIG - &#x60;MPRL&#x60; - &#x60;OP&#x60; * SIG - &#x60;Nasdaq BX&#x60; - &#x60;OP&#x60; * SOHO - &#x60;SOHO&#x60; - &#x60;EQ&#x60; * WEX - &#x60;Sweep-ALGO&#x60; - &#x60;OP&#x60; * WEX - &#x60;SweepPI-ALGO&#x60; - &#x60;OP&#x60; * WEX2 - &#x60;Sweep-ALGO&#x60; - &#x60;EQ&#x60; * WEX2 - &#x60;SweepPI-ALGO&#x60; - &#x60;EQ&#x60; 
        /// </summary>
        /// <value>Must be UPPERCASE * AMEX - &#x60;AMEX&#x60; - &#x60;EQ&#x60; * ARCA - &#x60;ARCX&#x60; - &#x60;EQ&#x60; * BATS - &#x60;BATS&#x60; - &#x60;EQ&#x60; * BEAR - &#x60;SuperDOT&#x60; - &#x60;EQ&#x60; * BOX - &#x60;BOX&#x60; - &#x60;OP&#x60; * BYX - &#x60;BYX&#x60; - &#x60;EQ&#x60; * CFE - &#x60;CFE&#x60; - &#x60;FU&#x60; * CME - &#x60;CBOT&#x60; - &#x60;FU&#x60; * CME - &#x60;CME&#x60; - &#x60;FU&#x60; * CME - &#x60;COMEX&#x60; - &#x60;FU&#x60; * CME - &#x60;NYMEX&#x60; - &#x60;FU&#x60; * CNX - &#x60;Current Spot FX&#x60; - &#x60;FX&#x60; * CSFB - &#x60;CSFB&#x60; - &#x60;EQ&#x60; * CSRS - &#x60;CSRS&#x60; - &#x60;EQ&#x60; * CSRS - &#x60;TWAP-ALGO&#x60; - &#x60;EQ&#x60; * CSRS - &#x60;VWAP-ALGO&#x60; - &#x60;EQ&#x60; * CSRX - &#x60;POV-ALGO&#x60; - &#x60;EQ&#x60; * CTDL - &#x60;CDRG&#x60; - &#x60;EQ&#x60; * EDGA - &#x60;EDGA&#x60; - &#x60;EQ&#x60; * EDGX - &#x60;EDGX&#x60; - &#x60;EQ&#x60; * EURX - &#x60;EUREX&#x60; - &#x60;FU&#x60; * IBFX - &#x60;Interbank Fx&#x60; - &#x60;FX&#x60; * ICE - &#x60;ICEUS&#x60; - &#x60;FU&#x60; * ICEB - &#x60;ICEBS&#x60; - &#x60;FU&#x60; * ICEE - &#x60;ICEEU&#x60; - &#x60;FU&#x60; * IEXG - &#x60;IEX&#x60; - &#x60;EQ&#x60; * KCG - &#x60;Knight Link&#x60; - &#x60;EQ&#x60; * NQBX - &#x60;NQBX&#x60; - &#x60;EQ&#x60; * NSDQ - &#x60;NASDAQ Type 1 Retail Orders&#x60; - &#x60;EQ&#x60; * NSDQ - &#x60;NSDQ&#x60; - &#x60;EQ&#x60; * NYSE - &#x60;NYSE Type 1 Retail Orders&#x60; - &#x60;EQ&#x60; * NYSE - &#x60;NYSE&#x60; - &#x60;EQ&#x60; * SIG - &#x60;BATS&#x60; - &#x60;OP&#x60; * SIG - &#x60;C2&#x60; - &#x60;OP&#x60; * SIG - &#x60;EDGO&#x60; - &#x60;OP&#x60; * SIG - &#x60;GMNI&#x60; - &#x60;OP&#x60; * SIG - &#x60;ISE Mercury&#x60; - &#x60;OP&#x60; * SIG - &#x60;MIAX&#x60; - &#x60;OP&#x60; * SIG - &#x60;MPRL&#x60; - &#x60;OP&#x60; * SIG - &#x60;Nasdaq BX&#x60; - &#x60;OP&#x60; * SOHO - &#x60;SOHO&#x60; - &#x60;EQ&#x60; * WEX - &#x60;Sweep-ALGO&#x60; - &#x60;OP&#x60; * WEX - &#x60;SweepPI-ALGO&#x60; - &#x60;OP&#x60; * WEX2 - &#x60;Sweep-ALGO&#x60; - &#x60;EQ&#x60; * WEX2 - &#x60;SweepPI-ALGO&#x60; - &#x60;EQ&#x60; </value>
        [DataMember(Name="Route", EmitDefaultValue=false)]
        public RouteEnum? Route { get; set; }
        /// <summary>
        /// Conveys the intent of the trade * BUY - &#x60;equities&#x60; and &#x60;futures&#x60; * SELL - &#x60;equities&#x60; and &#x60;futures&#x60; * BUYTOCOVER - &#x60;equities&#x60; * SELLSHORT - &#x60;equities&#x60; * BUYTOOPEN - &#x60;options&#x60; * BUYTOCLOSE - &#x60;options&#x60; * SELLTOOPEN - &#x60;options&#x60; * SELLTOCLOSE - &#x60;options&#x60; 
        /// </summary>
        /// <value>Conveys the intent of the trade * BUY - &#x60;equities&#x60; and &#x60;futures&#x60; * SELL - &#x60;equities&#x60; and &#x60;futures&#x60; * BUYTOCOVER - &#x60;equities&#x60; * SELLSHORT - &#x60;equities&#x60; * BUYTOOPEN - &#x60;options&#x60; * BUYTOCLOSE - &#x60;options&#x60; * SELLTOOPEN - &#x60;options&#x60; * SELLTOCLOSE - &#x60;options&#x60; </value>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum TradeActionEnum
        {
            
            /// <summary>
            /// Enum BUY for value: BUY
            /// </summary>
            [EnumMember(Value = "BUY")]
            BUY = 1,
            
            /// <summary>
            /// Enum SELL for value: SELL
            /// </summary>
            [EnumMember(Value = "SELL")]
            SELL = 2,
            
            /// <summary>
            /// Enum BUYTOCOVER for value: BUYTOCOVER
            /// </summary>
            [EnumMember(Value = "BUYTOCOVER")]
            BUYTOCOVER = 3,
            
            /// <summary>
            /// Enum SELLSHORT for value: SELLSHORT
            /// </summary>
            [EnumMember(Value = "SELLSHORT")]
            SELLSHORT = 4,
            
            /// <summary>
            /// Enum BUYTOOPEN for value: BUYTOOPEN
            /// </summary>
            [EnumMember(Value = "BUYTOOPEN")]
            BUYTOOPEN = 5,
            
            /// <summary>
            /// Enum BUYTOCLOSE for value: BUYTOCLOSE
            /// </summary>
            [EnumMember(Value = "BUYTOCLOSE")]
            BUYTOCLOSE = 6,
            
            /// <summary>
            /// Enum SELLTOOPEN for value: SELLTOOPEN
            /// </summary>
            [EnumMember(Value = "SELLTOOPEN")]
            SELLTOOPEN = 7,
            
            /// <summary>
            /// Enum SELLTOCLOSE for value: SELLTOCLOSE
            /// </summary>
            [EnumMember(Value = "SELLTOCLOSE")]
            SELLTOCLOSE = 8
        }

        /// <summary>
        /// Conveys the intent of the trade * BUY - &#x60;equities&#x60; and &#x60;futures&#x60; * SELL - &#x60;equities&#x60; and &#x60;futures&#x60; * BUYTOCOVER - &#x60;equities&#x60; * SELLSHORT - &#x60;equities&#x60; * BUYTOOPEN - &#x60;options&#x60; * BUYTOCLOSE - &#x60;options&#x60; * SELLTOOPEN - &#x60;options&#x60; * SELLTOCLOSE - &#x60;options&#x60; 
        /// </summary>
        /// <value>Conveys the intent of the trade * BUY - &#x60;equities&#x60; and &#x60;futures&#x60; * SELL - &#x60;equities&#x60; and &#x60;futures&#x60; * BUYTOCOVER - &#x60;equities&#x60; * SELLSHORT - &#x60;equities&#x60; * BUYTOOPEN - &#x60;options&#x60; * BUYTOCLOSE - &#x60;options&#x60; * SELLTOOPEN - &#x60;options&#x60; * SELLTOCLOSE - &#x60;options&#x60; </value>
        [DataMember(Name="TradeAction", EmitDefaultValue=false)]
        public TradeActionEnum TradeAction { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="OrderRequestDefinition" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected OrderRequestDefinition() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="OrderRequestDefinition" /> class.
        /// </summary>
        /// <param name="accountKey">Must be a valid Account Key for that user and Asset Type  (required).</param>
        /// <param name="advancedOptions">advancedOptions.</param>
        /// <param name="assetType">assetType (required).</param>
        /// <param name="duration">Allowed durations vary by Asset Type * DAY - Day, valid until the end of the regular trading session. * DYP - Day Plus; valid until the end of the extended trading session * GTC - Good till canceled * GCP - Good till canceled plus * GTD - Good through date * GDP - Good through date plus * OPG - At the opening; only valid for listed stocks at the opening session Price * CLO - On Close; orders that target the closing session of an exchange. * IOC - Immediate or Cancel; filled immediately or canceled, partial fills are accepted * FOK - Fill or Kill; orders are filled entirely or canceled, partial fills are not accepted * 1 or 1 MIN - 1 minute; expires after the 1 minute * 3 or 3 MIN - 3 minutes; expires after the 3 minutes * 5 or 5 MIN - 5 minutes; expires after the 5 minutes  (required).</param>
        /// <param name="gTDDate">Date that Order is valid through. Input Format: MM/DD/YYYY Required for orders with Duration &#x3D; GTD. .</param>
        /// <param name="limitPrice">limitPrice.</param>
        /// <param name="stopPrice">stopPrice.</param>
        /// <param name="orderConfirmId">A unique identifier regarding an order used to prevent duplicates.  Must be unique per API key, per order, per user. .</param>
        /// <param name="orderType">orderType (required).</param>
        /// <param name="quantity">quantity (required).</param>
        /// <param name="route">Must be UPPERCASE * AMEX - &#x60;AMEX&#x60; - &#x60;EQ&#x60; * ARCA - &#x60;ARCX&#x60; - &#x60;EQ&#x60; * BATS - &#x60;BATS&#x60; - &#x60;EQ&#x60; * BEAR - &#x60;SuperDOT&#x60; - &#x60;EQ&#x60; * BOX - &#x60;BOX&#x60; - &#x60;OP&#x60; * BYX - &#x60;BYX&#x60; - &#x60;EQ&#x60; * CFE - &#x60;CFE&#x60; - &#x60;FU&#x60; * CME - &#x60;CBOT&#x60; - &#x60;FU&#x60; * CME - &#x60;CME&#x60; - &#x60;FU&#x60; * CME - &#x60;COMEX&#x60; - &#x60;FU&#x60; * CME - &#x60;NYMEX&#x60; - &#x60;FU&#x60; * CNX - &#x60;Current Spot FX&#x60; - &#x60;FX&#x60; * CSFB - &#x60;CSFB&#x60; - &#x60;EQ&#x60; * CSRS - &#x60;CSRS&#x60; - &#x60;EQ&#x60; * CSRS - &#x60;TWAP-ALGO&#x60; - &#x60;EQ&#x60; * CSRS - &#x60;VWAP-ALGO&#x60; - &#x60;EQ&#x60; * CSRX - &#x60;POV-ALGO&#x60; - &#x60;EQ&#x60; * CTDL - &#x60;CDRG&#x60; - &#x60;EQ&#x60; * EDGA - &#x60;EDGA&#x60; - &#x60;EQ&#x60; * EDGX - &#x60;EDGX&#x60; - &#x60;EQ&#x60; * EURX - &#x60;EUREX&#x60; - &#x60;FU&#x60; * IBFX - &#x60;Interbank Fx&#x60; - &#x60;FX&#x60; * ICE - &#x60;ICEUS&#x60; - &#x60;FU&#x60; * ICEB - &#x60;ICEBS&#x60; - &#x60;FU&#x60; * ICEE - &#x60;ICEEU&#x60; - &#x60;FU&#x60; * IEXG - &#x60;IEX&#x60; - &#x60;EQ&#x60; * KCG - &#x60;Knight Link&#x60; - &#x60;EQ&#x60; * NQBX - &#x60;NQBX&#x60; - &#x60;EQ&#x60; * NSDQ - &#x60;NASDAQ Type 1 Retail Orders&#x60; - &#x60;EQ&#x60; * NSDQ - &#x60;NSDQ&#x60; - &#x60;EQ&#x60; * NYSE - &#x60;NYSE Type 1 Retail Orders&#x60; - &#x60;EQ&#x60; * NYSE - &#x60;NYSE&#x60; - &#x60;EQ&#x60; * SIG - &#x60;BATS&#x60; - &#x60;OP&#x60; * SIG - &#x60;C2&#x60; - &#x60;OP&#x60; * SIG - &#x60;EDGO&#x60; - &#x60;OP&#x60; * SIG - &#x60;GMNI&#x60; - &#x60;OP&#x60; * SIG - &#x60;ISE Mercury&#x60; - &#x60;OP&#x60; * SIG - &#x60;MIAX&#x60; - &#x60;OP&#x60; * SIG - &#x60;MPRL&#x60; - &#x60;OP&#x60; * SIG - &#x60;Nasdaq BX&#x60; - &#x60;OP&#x60; * SOHO - &#x60;SOHO&#x60; - &#x60;EQ&#x60; * WEX - &#x60;Sweep-ALGO&#x60; - &#x60;OP&#x60; * WEX - &#x60;SweepPI-ALGO&#x60; - &#x60;OP&#x60; * WEX2 - &#x60;Sweep-ALGO&#x60; - &#x60;EQ&#x60; * WEX2 - &#x60;SweepPI-ALGO&#x60; - &#x60;EQ&#x60; .</param>
        /// <param name="symbol">Must be UPPERCASE (required).</param>
        /// <param name="tradeAction">Conveys the intent of the trade * BUY - &#x60;equities&#x60; and &#x60;futures&#x60; * SELL - &#x60;equities&#x60; and &#x60;futures&#x60; * BUYTOCOVER - &#x60;equities&#x60; * SELLSHORT - &#x60;equities&#x60; * BUYTOOPEN - &#x60;options&#x60; * BUYTOCLOSE - &#x60;options&#x60; * SELLTOOPEN - &#x60;options&#x60; * SELLTOCLOSE - &#x60;options&#x60;  (required).</param>
        /// <param name="oSOs">oSOs.</param>
        /// <param name="legs">legs.</param>
        public OrderRequestDefinition(string accountKey = default(string), AdvancedOptionsDefinition advancedOptions = default(AdvancedOptionsDefinition), AssetTypeEnum assetType = default(AssetTypeEnum), DurationEnum duration = default(DurationEnum), string gTDDate = default(string), string limitPrice = default(string), string stopPrice = default(string), string orderConfirmId = default(string), OrderTypeEnum orderType = default(OrderTypeEnum), string quantity = default(string), RouteEnum? route = default(RouteEnum?), string symbol = default(string), TradeActionEnum tradeAction = default(TradeActionEnum), List<OrderRequestDefinitionOSOs> oSOs = default(List<OrderRequestDefinitionOSOs>), List<OrderConfirmRequestDefinitionLegs> legs = default(List<OrderConfirmRequestDefinitionLegs>))
        {
            // to ensure "accountKey" is required (not null)
            if (accountKey == null)
            {
                throw new InvalidDataException("accountKey is a required property for OrderRequestDefinition and cannot be null");
            }
            else
            {
                this.AccountKey = accountKey;
            }
            // to ensure "assetType" is required (not null)
            if (assetType == null)
            {
                throw new InvalidDataException("assetType is a required property for OrderRequestDefinition and cannot be null");
            }
            else
            {
                this.AssetType = assetType;
            }
            // to ensure "duration" is required (not null)
            if (duration == null)
            {
                throw new InvalidDataException("duration is a required property for OrderRequestDefinition and cannot be null");
            }
            else
            {
                this.Duration = duration;
            }
            // to ensure "orderType" is required (not null)
            if (orderType == null)
            {
                throw new InvalidDataException("orderType is a required property for OrderRequestDefinition and cannot be null");
            }
            else
            {
                this.OrderType = orderType;
            }
            // to ensure "quantity" is required (not null)
            if (quantity == null)
            {
                throw new InvalidDataException("quantity is a required property for OrderRequestDefinition and cannot be null");
            }
            else
            {
                this.Quantity = quantity;
            }
            // to ensure "symbol" is required (not null)
            if (symbol == null)
            {
                throw new InvalidDataException("symbol is a required property for OrderRequestDefinition and cannot be null");
            }
            else
            {
                this.Symbol = symbol;
            }
            // to ensure "tradeAction" is required (not null)
            if (tradeAction == null)
            {
                throw new InvalidDataException("tradeAction is a required property for OrderRequestDefinition and cannot be null");
            }
            else
            {
                this.TradeAction = tradeAction;
            }
            this.AdvancedOptions = advancedOptions;
            this.GTDDate = gTDDate;
            this.LimitPrice = limitPrice;
            this.StopPrice = stopPrice;
            this.OrderConfirmId = orderConfirmId;
            this.Route = route;
            this.OSOs = oSOs;
            this.Legs = legs;
        }
        
        /// <summary>
        /// Must be a valid Account Key for that user and Asset Type 
        /// </summary>
        /// <value>Must be a valid Account Key for that user and Asset Type </value>
        [DataMember(Name="AccountKey", EmitDefaultValue=false)]
        public string AccountKey { get; set; }

        /// <summary>
        /// Gets or Sets AdvancedOptions
        /// </summary>
        [DataMember(Name="AdvancedOptions", EmitDefaultValue=false)]
        public AdvancedOptionsDefinition AdvancedOptions { get; set; }



        /// <summary>
        /// Date that Order is valid through. Input Format: MM/DD/YYYY Required for orders with Duration &#x3D; GTD. 
        /// </summary>
        /// <value>Date that Order is valid through. Input Format: MM/DD/YYYY Required for orders with Duration &#x3D; GTD. </value>
        [DataMember(Name="GTDDate", EmitDefaultValue=false)]
        public string GTDDate { get; set; }

        /// <summary>
        /// Gets or Sets LimitPrice
        /// </summary>
        [DataMember(Name="LimitPrice", EmitDefaultValue=false)]
        public string LimitPrice { get; set; }

        /// <summary>
        /// Gets or Sets StopPrice
        /// </summary>
        [DataMember(Name="StopPrice", EmitDefaultValue=false)]
        public string StopPrice { get; set; }

        /// <summary>
        /// A unique identifier regarding an order used to prevent duplicates.  Must be unique per API key, per order, per user. 
        /// </summary>
        /// <value>A unique identifier regarding an order used to prevent duplicates.  Must be unique per API key, per order, per user. </value>
        [DataMember(Name="OrderConfirmId", EmitDefaultValue=false)]
        public string OrderConfirmId { get; set; }


        /// <summary>
        /// Gets or Sets Quantity
        /// </summary>
        [DataMember(Name="Quantity", EmitDefaultValue=false)]
        public string Quantity { get; set; }


        /// <summary>
        /// Must be UPPERCASE
        /// </summary>
        /// <value>Must be UPPERCASE</value>
        [DataMember(Name="Symbol", EmitDefaultValue=false)]
        public string Symbol { get; set; }


        /// <summary>
        /// Gets or Sets OSOs
        /// </summary>
        [DataMember(Name="OSOs", EmitDefaultValue=false)]
        public List<OrderRequestDefinitionOSOs> OSOs { get; set; }

        /// <summary>
        /// Gets or Sets Legs
        /// </summary>
        [DataMember(Name="Legs", EmitDefaultValue=false)]
        public List<OrderConfirmRequestDefinitionLegs> Legs { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class OrderRequestDefinition {\n");
            sb.Append("  AccountKey: ").Append(AccountKey).Append("\n");
            sb.Append("  AdvancedOptions: ").Append(AdvancedOptions).Append("\n");
            sb.Append("  AssetType: ").Append(AssetType).Append("\n");
            sb.Append("  Duration: ").Append(Duration).Append("\n");
            sb.Append("  GTDDate: ").Append(GTDDate).Append("\n");
            sb.Append("  LimitPrice: ").Append(LimitPrice).Append("\n");
            sb.Append("  StopPrice: ").Append(StopPrice).Append("\n");
            sb.Append("  OrderConfirmId: ").Append(OrderConfirmId).Append("\n");
            sb.Append("  OrderType: ").Append(OrderType).Append("\n");
            sb.Append("  Quantity: ").Append(Quantity).Append("\n");
            sb.Append("  Route: ").Append(Route).Append("\n");
            sb.Append("  Symbol: ").Append(Symbol).Append("\n");
            sb.Append("  TradeAction: ").Append(TradeAction).Append("\n");
            sb.Append("  OSOs: ").Append(OSOs).Append("\n");
            sb.Append("  Legs: ").Append(Legs).Append("\n");
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
            return this.Equals(input as OrderRequestDefinition);
        }

        /// <summary>
        /// Returns true if OrderRequestDefinition instances are equal
        /// </summary>
        /// <param name="input">Instance of OrderRequestDefinition to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(OrderRequestDefinition input)
        {
            if (input == null)
                return false;

            return 
                (
                    this.AccountKey == input.AccountKey ||
                    (this.AccountKey != null &&
                    this.AccountKey.Equals(input.AccountKey))
                ) && 
                (
                    this.AdvancedOptions == input.AdvancedOptions ||
                    (this.AdvancedOptions != null &&
                    this.AdvancedOptions.Equals(input.AdvancedOptions))
                ) && 
                (
                    this.AssetType == input.AssetType ||
                    (this.AssetType != null &&
                    this.AssetType.Equals(input.AssetType))
                ) && 
                (
                    this.Duration == input.Duration ||
                    (this.Duration != null &&
                    this.Duration.Equals(input.Duration))
                ) && 
                (
                    this.GTDDate == input.GTDDate ||
                    (this.GTDDate != null &&
                    this.GTDDate.Equals(input.GTDDate))
                ) && 
                (
                    this.LimitPrice == input.LimitPrice ||
                    (this.LimitPrice != null &&
                    this.LimitPrice.Equals(input.LimitPrice))
                ) && 
                (
                    this.StopPrice == input.StopPrice ||
                    (this.StopPrice != null &&
                    this.StopPrice.Equals(input.StopPrice))
                ) && 
                (
                    this.OrderConfirmId == input.OrderConfirmId ||
                    (this.OrderConfirmId != null &&
                    this.OrderConfirmId.Equals(input.OrderConfirmId))
                ) && 
                (
                    this.OrderType == input.OrderType ||
                    (this.OrderType != null &&
                    this.OrderType.Equals(input.OrderType))
                ) && 
                (
                    this.Quantity == input.Quantity ||
                    (this.Quantity != null &&
                    this.Quantity.Equals(input.Quantity))
                ) && 
                (
                    this.Route == input.Route ||
                    (this.Route != null &&
                    this.Route.Equals(input.Route))
                ) && 
                (
                    this.Symbol == input.Symbol ||
                    (this.Symbol != null &&
                    this.Symbol.Equals(input.Symbol))
                ) && 
                (
                    this.TradeAction == input.TradeAction ||
                    (this.TradeAction != null &&
                    this.TradeAction.Equals(input.TradeAction))
                ) && 
                (
                    this.OSOs == input.OSOs ||
                    this.OSOs != null &&
                    this.OSOs.SequenceEqual(input.OSOs)
                ) && 
                (
                    this.Legs == input.Legs ||
                    this.Legs != null &&
                    this.Legs.SequenceEqual(input.Legs)
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
                if (this.AccountKey != null)
                    hashCode = hashCode * 59 + this.AccountKey.GetHashCode();
                if (this.AdvancedOptions != null)
                    hashCode = hashCode * 59 + this.AdvancedOptions.GetHashCode();
                if (this.AssetType != null)
                    hashCode = hashCode * 59 + this.AssetType.GetHashCode();
                if (this.Duration != null)
                    hashCode = hashCode * 59 + this.Duration.GetHashCode();
                if (this.GTDDate != null)
                    hashCode = hashCode * 59 + this.GTDDate.GetHashCode();
                if (this.LimitPrice != null)
                    hashCode = hashCode * 59 + this.LimitPrice.GetHashCode();
                if (this.StopPrice != null)
                    hashCode = hashCode * 59 + this.StopPrice.GetHashCode();
                if (this.OrderConfirmId != null)
                    hashCode = hashCode * 59 + this.OrderConfirmId.GetHashCode();
                if (this.OrderType != null)
                    hashCode = hashCode * 59 + this.OrderType.GetHashCode();
                if (this.Quantity != null)
                    hashCode = hashCode * 59 + this.Quantity.GetHashCode();
                if (this.Route != null)
                    hashCode = hashCode * 59 + this.Route.GetHashCode();
                if (this.Symbol != null)
                    hashCode = hashCode * 59 + this.Symbol.GetHashCode();
                if (this.TradeAction != null)
                    hashCode = hashCode * 59 + this.TradeAction.GetHashCode();
                if (this.OSOs != null)
                    hashCode = hashCode * 59 + this.OSOs.GetHashCode();
                if (this.Legs != null)
                    hashCode = hashCode * 59 + this.Legs.GetHashCode();
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
            // AccountKey (string) minLength
            if(this.AccountKey != null && this.AccountKey.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for AccountKey, length must be greater than 1.", new [] { "AccountKey" });
            }

            // AssetType (string) minLength
            //if(this.AssetType != null && this.AssetType.Length < 1)
            //{
            //    yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for AssetType, length must be greater than 1.", new [] { "AssetType" });
            //}

            // Duration (string) minLength
            //if(this.Duration != null && this.Duration.Length < 1)
            //{
            //    yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Duration, length must be greater than 1.", new [] { "Duration" });
            //}

            // GTDDate (string) maxLength
            if(this.GTDDate != null && this.GTDDate.Length > 10)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for GTDDate, length must be less than 10.", new [] { "GTDDate" });
            }

            // GTDDate (string) minLength
            if(this.GTDDate != null && this.GTDDate.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for GTDDate, length must be greater than 1.", new [] { "GTDDate" });
            }

            // LimitPrice (string) minLength
            if(this.LimitPrice != null && this.LimitPrice.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for LimitPrice, length must be greater than 1.", new [] { "LimitPrice" });
            }

            // StopPrice (string) minLength
            if(this.StopPrice != null && this.StopPrice.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for StopPrice, length must be greater than 1.", new [] { "StopPrice" });
            }

            // OrderConfirmId (string) maxLength
            if(this.OrderConfirmId != null && this.OrderConfirmId.Length > 25)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for OrderConfirmId, length must be less than 25.", new [] { "OrderConfirmId" });
            }

            // OrderConfirmId (string) minLength
            if(this.OrderConfirmId != null && this.OrderConfirmId.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for OrderConfirmId, length must be greater than 1.", new [] { "OrderConfirmId" });
            }

            // OrderType (string) minLength
            //if(this.OrderType != null && this.OrderType.Length < 1)
            //{
            //    yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for OrderType, length must be greater than 1.", new [] { "OrderType" });
            //}

            // Quantity (string) minLength
            if(this.Quantity != null && this.Quantity.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Quantity, length must be greater than 1.", new [] { "Quantity" });
            }

            // Route (string) minLength
            //if(this.Route != null && this.Route.Length < 1)
            //{
            //    yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Route, length must be greater than 1.", new [] { "Route" });
            //}

            // Symbol (string) minLength
            if(this.Symbol != null && this.Symbol.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Symbol, length must be greater than 1.", new [] { "Symbol" });
            }

            // TradeAction (string) minLength
            //if(this.TradeAction != null && this.TradeAction.Length < 1)
            //{
            //    yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for TradeAction, length must be greater than 1.", new [] { "TradeAction" });
            //}

            yield break;
        }
    }

}
