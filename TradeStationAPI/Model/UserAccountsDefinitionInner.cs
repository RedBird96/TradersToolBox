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
    /// UserAccountsDefinitionInner
    /// </summary>
    [DataContract]
    public partial class UserAccountsDefinitionInner :  IEquatable<UserAccountsDefinitionInner>, IValidatableObject
    {
        /// <summary>
        /// Name of the type of the account.
        /// </summary>
        /// <value>Name of the type of the account.</value>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum TypeDescriptionEnum
        {
            
            /// <summary>
            /// Enum Cash for value: Cash
            /// </summary>
            [EnumMember(Value = "Cash")]
            Cash = 1,
            
            /// <summary>
            /// Enum Margin for value: Margin
            /// </summary>
            [EnumMember(Value = "Margin")]
            Margin = 2,
            
            /// <summary>
            /// Enum DVP for value: DVP
            /// </summary>
            [EnumMember(Value = "DVP")]
            DVP = 3,
            
            /// <summary>
            /// Enum Futures for value: Futures
            /// </summary>
            [EnumMember(Value = "Futures")]
            Futures = 4
        }

        /// <summary>
        /// Name of the type of the account.
        /// </summary>
        /// <value>Name of the type of the account.</value>
        [DataMember(Name="TypeDescription", EmitDefaultValue=false)]
        public TypeDescriptionEnum TypeDescription { get; set; }
        /// <summary>
        /// Status of a specific account. * A - Active * X - Closed * C - Closing Transaction Only * F - Margin Call - Closing Transactions Only * I - Inactive * L - Liquidating Transactions Only * R - Restricted * D - 90 Day Restriction-Closing Transaction Only 
        /// </summary>
        /// <value>Status of a specific account. * A - Active * X - Closed * C - Closing Transaction Only * F - Margin Call - Closing Transactions Only * I - Inactive * L - Liquidating Transactions Only * R - Restricted * D - 90 Day Restriction-Closing Transaction Only </value>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum StatusEnum
        {
            
            /// <summary>
            /// Enum A for value: A
            /// </summary>
            [EnumMember(Value = "A")]
            A = 1,
            
            /// <summary>
            /// Enum X for value: X
            /// </summary>
            [EnumMember(Value = "X")]
            X = 2,
            
            /// <summary>
            /// Enum C for value: C
            /// </summary>
            [EnumMember(Value = "C")]
            C = 3,
            
            /// <summary>
            /// Enum F for value: F
            /// </summary>
            [EnumMember(Value = "F")]
            F = 4,
            
            /// <summary>
            /// Enum I for value: I
            /// </summary>
            [EnumMember(Value = "I")]
            I = 5,
            
            /// <summary>
            /// Enum L for value: L
            /// </summary>
            [EnumMember(Value = "L")]
            L = 6,
            
            /// <summary>
            /// Enum R for value: R
            /// </summary>
            [EnumMember(Value = "R")]
            R = 7,
            
            /// <summary>
            /// Enum D for value: D
            /// </summary>
            [EnumMember(Value = "D")]
            D = 8
        }

        /// <summary>
        /// Status of a specific account. * A - Active * X - Closed * C - Closing Transaction Only * F - Margin Call - Closing Transactions Only * I - Inactive * L - Liquidating Transactions Only * R - Restricted * D - 90 Day Restriction-Closing Transaction Only 
        /// </summary>
        /// <value>Status of a specific account. * A - Active * X - Closed * C - Closing Transaction Only * F - Margin Call - Closing Transactions Only * I - Inactive * L - Liquidating Transactions Only * R - Restricted * D - 90 Day Restriction-Closing Transaction Only </value>
        [DataMember(Name="Status", EmitDefaultValue=false)]
        public StatusEnum? Status { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="UserAccountsDefinitionInner" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected UserAccountsDefinitionInner() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="UserAccountsDefinitionInner" /> class.
        /// </summary>
        /// <param name="alias">A user specified name that identifies a TradeStation account. (required).</param>
        /// <param name="altId">TradeStation Alternate ID..</param>
        /// <param name="displayName">Set to Alternate ID if it exists otherwise will be the TradeStation Account ID (required).</param>
        /// <param name="isStockLocateEligible">True if this account is stock locate eligible; otherwise, false. (required).</param>
        /// <param name="key">Account Identifier. (required).</param>
        /// <param name="name">Account Name. (required).</param>
        /// <param name="type">Type of the account:  * &#x60;C&#x60; Cash * &#x60;M&#x60; Margin * &#x60;F&#x60; Futures * &#x60;D&#x60; DVP  (required).</param>
        /// <param name="typeDescription">Name of the type of the account. (required).</param>
        /// <param name="status">Status of a specific account. * A - Active * X - Closed * C - Closing Transaction Only * F - Margin Call - Closing Transactions Only * I - Inactive * L - Liquidating Transactions Only * R - Restricted * D - 90 Day Restriction-Closing Transaction Only .</param>
        /// <param name="statusDescription">String value for Status attribute..</param>
        public UserAccountsDefinitionInner(string alias = default, string altId = default, string displayName = default, bool? isStockLocateEligible = default,
            decimal? key = default, string name = default, string type = default, TypeDescriptionEnum typeDescription = default, StatusEnum? status = default, string statusDescription = default)
        {
            // to ensure "alias" is required (not null)
            if (alias == null)
            {
                throw new InvalidDataException("alias is a required property for UserAccountsDefinitionInner and cannot be null");
            }
            else
            {
                this.Alias = alias;
            }
            // to ensure "displayName" is required (not null)
            if (displayName == null)
            {
                throw new InvalidDataException("displayName is a required property for UserAccountsDefinitionInner and cannot be null");
            }
            else
            {
                this.DisplayName = displayName;
            }
            // to ensure "isStockLocateEligible" is required (not null)
            if (isStockLocateEligible == null)
            {
                throw new InvalidDataException("isStockLocateEligible is a required property for UserAccountsDefinitionInner and cannot be null");
            }
            else
            {
                this.IsStockLocateEligible = isStockLocateEligible;
            }
            // to ensure "key" is required (not null)
            if (key == null)
            {
                throw new InvalidDataException("key is a required property for UserAccountsDefinitionInner and cannot be null");
            }
            else
            {
                this.Key = key;
            }
            // to ensure "name" is required (not null)
            if (name == null)
            {
                throw new InvalidDataException("name is a required property for UserAccountsDefinitionInner and cannot be null");
            }
            else
            {
                this.Name = name;
            }
            // to ensure "type" is required (not null)
            if (type == null)
            {
                throw new InvalidDataException("type is a required property for UserAccountsDefinitionInner and cannot be null");
            }
            else
            {
                this.Type = type;
            }
            // to ensure "typeDescription" is required (not null)
            if (typeDescription == null)
            {
                throw new InvalidDataException("typeDescription is a required property for UserAccountsDefinitionInner and cannot be null");
            }
            else
            {
                this.TypeDescription = typeDescription;
            }
            this.AltId = altId;
            this.Status = status;
            this.StatusDescription = statusDescription;
        }
        
        /// <summary>
        /// A user specified name that identifies a TradeStation account.
        /// </summary>
        /// <value>A user specified name that identifies a TradeStation account.</value>
        [DataMember(Name="Alias", EmitDefaultValue=false)]
        public string Alias { get; set; }

        /// <summary>
        /// TradeStation Alternate ID.
        /// </summary>
        /// <value>TradeStation Alternate ID.</value>
        [DataMember(Name="AltId", EmitDefaultValue=false)]
        public string AltId { get; set; }

        /// <summary>
        /// Set to Alternate ID if it exists otherwise will be the TradeStation Account ID
        /// </summary>
        /// <value>Set to Alternate ID if it exists otherwise will be the TradeStation Account ID</value>
        [DataMember(Name="DisplayName", EmitDefaultValue=false)]
        public string DisplayName { get; set; }

        /// <summary>
        /// True if this account is stock locate eligible; otherwise, false.
        /// </summary>
        /// <value>True if this account is stock locate eligible; otherwise, false.</value>
        [DataMember(Name="IsStockLocateEligible", EmitDefaultValue=false)]
        public bool? IsStockLocateEligible { get; set; }

        /// <summary>
        /// Account Identifier.
        /// </summary>
        /// <value>Account Identifier.</value>
        [DataMember(Name="Key", EmitDefaultValue=false)]
        public decimal? Key { get; set; }

        /// <summary>
        /// Account Name.
        /// </summary>
        /// <value>Account Name.</value>
        [DataMember(Name="Name", EmitDefaultValue=false)]
        public string Name { get; set; }

        /// <summary>
        /// Type of the account:  * &#x60;C&#x60; Cash * &#x60;M&#x60; Margin * &#x60;F&#x60; Futures * &#x60;D&#x60; DVP 
        /// </summary>
        /// <value>Type of the account:  * &#x60;C&#x60; Cash * &#x60;M&#x60; Margin * &#x60;F&#x60; Futures * &#x60;D&#x60; DVP </value>
        [DataMember(Name="Type", EmitDefaultValue=false)]
        public string Type { get; set; }



        /// <summary>
        /// String value for Status attribute.
        /// </summary>
        /// <value>String value for Status attribute.</value>
        [DataMember(Name="StatusDescription", EmitDefaultValue=false)]
        public string StatusDescription { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class UserAccountsDefinitionInner {\n");
            sb.Append("  Alias: ").Append(Alias).Append("\n");
            sb.Append("  AltId: ").Append(AltId).Append("\n");
            sb.Append("  DisplayName: ").Append(DisplayName).Append("\n");
            sb.Append("  IsStockLocateEligible: ").Append(IsStockLocateEligible).Append("\n");
            sb.Append("  Key: ").Append(Key).Append("\n");
            sb.Append("  Name: ").Append(Name).Append("\n");
            sb.Append("  Type: ").Append(Type).Append("\n");
            sb.Append("  TypeDescription: ").Append(TypeDescription).Append("\n");
            sb.Append("  Status: ").Append(Status).Append("\n");
            sb.Append("  StatusDescription: ").Append(StatusDescription).Append("\n");
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
            return this.Equals(input as UserAccountsDefinitionInner);
        }

        /// <summary>
        /// Returns true if UserAccountsDefinitionInner instances are equal
        /// </summary>
        /// <param name="input">Instance of UserAccountsDefinitionInner to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(UserAccountsDefinitionInner input)
        {
            if (input == null)
                return false;

            return 
                (
                    this.Alias == input.Alias ||
                    (this.Alias != null &&
                    this.Alias.Equals(input.Alias))
                ) && 
                (
                    this.AltId == input.AltId ||
                    (this.AltId != null &&
                    this.AltId.Equals(input.AltId))
                ) && 
                (
                    this.DisplayName == input.DisplayName ||
                    (this.DisplayName != null &&
                    this.DisplayName.Equals(input.DisplayName))
                ) && 
                (
                    this.IsStockLocateEligible == input.IsStockLocateEligible ||
                    (this.IsStockLocateEligible != null &&
                    this.IsStockLocateEligible.Equals(input.IsStockLocateEligible))
                ) && 
                (
                    this.Key == input.Key ||
                    (this.Key != null &&
                    this.Key.Equals(input.Key))
                ) && 
                (
                    this.Name == input.Name ||
                    (this.Name != null &&
                    this.Name.Equals(input.Name))
                ) && 
                (
                    this.Type == input.Type ||
                    (this.Type != null &&
                    this.Type.Equals(input.Type))
                ) && 
                (
                    this.TypeDescription == input.TypeDescription ||
                    (this.TypeDescription != null &&
                    this.TypeDescription.Equals(input.TypeDescription))
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
                if (this.Alias != null)
                    hashCode = hashCode * 59 + this.Alias.GetHashCode();
                if (this.AltId != null)
                    hashCode = hashCode * 59 + this.AltId.GetHashCode();
                if (this.DisplayName != null)
                    hashCode = hashCode * 59 + this.DisplayName.GetHashCode();
                if (this.IsStockLocateEligible != null)
                    hashCode = hashCode * 59 + this.IsStockLocateEligible.GetHashCode();
                if (this.Key != null)
                    hashCode = hashCode * 59 + this.Key.GetHashCode();
                if (this.Name != null)
                    hashCode = hashCode * 59 + this.Name.GetHashCode();
                if (this.Type != null)
                    hashCode = hashCode * 59 + this.Type.GetHashCode();
                if (this.TypeDescription != null)
                    hashCode = hashCode * 59 + this.TypeDescription.GetHashCode();
                if (this.Status != null)
                    hashCode = hashCode * 59 + this.Status.GetHashCode();
                if (this.StatusDescription != null)
                    hashCode = hashCode * 59 + this.StatusDescription.GetHashCode();
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
            // DisplayName (string) minLength
            if(this.DisplayName != null && this.DisplayName.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for DisplayName, length must be greater than 1.", new [] { "DisplayName" });
            }

            // Name (string) minLength
            if(this.Name != null && this.Name.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Name, length must be greater than 1.", new [] { "Name" });
            }

            // Type (string) minLength
            if(this.Type != null && this.Type.Length < 1)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Type, length must be greater than 1.", new [] { "Type" });
            }

            // TypeDescription (string) minLength
            //if(this.TypeDescription != null && this.TypeDescription.Length < 1)
            //{
            //    yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for TypeDescription, length must be greater than 1.", new [] { "TypeDescription" });
            //}

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

            yield break;
        }
    }

}
