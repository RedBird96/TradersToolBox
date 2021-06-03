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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.IO;
using System.Web;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using RestSharp;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;

namespace TradeStationAPI.Client
{
    /// <summary>
    /// API client is mainly responsible for making the HTTP call to the API backend.
    /// </summary>
    public partial class ApiClient
    {
        private JsonSerializerSettings serializerSettings = new JsonSerializerSettings
        {
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
        };

        /// <summary>
        /// Allows for extending request processing for <see cref="ApiClient"/> generated code.
        /// </summary>
        /// <param name="request">The RestSharp request object</param>
        partial void InterceptRequest(IRestRequest request);

        /// <summary>
        /// Allows for extending response processing for <see cref="ApiClient"/> generated code.
        /// </summary>
        /// <param name="request">The RestSharp request object</param>
        /// <param name="response">The RestSharp response object</param>
        partial void InterceptResponse(IRestRequest request, IRestResponse response);

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiClient" /> class
        /// with default configuration.
        /// </summary>
        public ApiClient()
        {
            Configuration = TradeStationAPI.Client.Configuration.Default;
            RestClient = new RestClient("https://api.tradestation.com/v2");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiClient" /> class
        /// with default base path (https://api.tradestation.com/v2).
        /// </summary>
        /// <param name="config">An instance of Configuration.</param>
        public ApiClient(Configuration config)
        {
            Configuration = config ?? TradeStationAPI.Client.Configuration.Default;

            RestClient = new RestClient(Configuration.BasePath);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiClient" /> class
        /// with default configuration.
        /// </summary>
        /// <param name="basePath">The base path.</param>
        public ApiClient(String basePath = "https://api.tradestation.com/v2")
        {
           if (String.IsNullOrEmpty(basePath))
                throw new ArgumentException("basePath cannot be empty");

            RestClient = new RestClient(basePath);
            Configuration = Client.Configuration.Default;
        }

        /// <summary>
        /// Gets or sets the default API client for making HTTP calls.
        /// </summary>
        /// <value>The default API client.</value>
        [Obsolete("ApiClient.Default is deprecated, please use 'Configuration.Default.ApiClient' instead.")]
        public static ApiClient Default;

        /// <summary>
        /// Gets or sets an instance of the IReadableConfiguration.
        /// </summary>
        /// <value>An instance of the IReadableConfiguration.</value>
        /// <remarks>
        /// <see cref="IReadableConfiguration"/> helps us to avoid modifying possibly global
        /// configuration values from within a given client. It does not guarantee thread-safety
        /// of the <see cref="Configuration"/> instance in any way.
        /// </remarks>
        public IReadableConfiguration Configuration { get; set; }

        /// <summary>
        /// Gets or sets the RestClient.
        /// </summary>
        /// <value>An instance of the RestClient</value>
        public RestClient RestClient { get; set; }

        public void ConfigureWebRequest(Action<HttpWebRequest> configurator) => RestClient.ConfigureWebRequest(configurator);

        // Creates and sets up a RestRequest prior to a call.
        private RestRequest PrepareRequest(
            String path, RestSharp.Method method, List<KeyValuePair<String, String>> queryParams, Object postBody,
            Dictionary<String, String> headerParams, Dictionary<String, String> formParams,
            Dictionary<String, FileParameter> fileParams, Dictionary<String, String> pathParams,
            String contentType)
        {
            var request = new RestRequest(path, method);

            // add path parameter, if any
            foreach(var param in pathParams)
                request.AddParameter(param.Key, param.Value, ParameterType.UrlSegment);

            // add header parameter, if any
            foreach(var param in headerParams)
                request.AddHeader(param.Key, param.Value);

            // add query parameter, if any
            foreach(var param in queryParams)
                request.AddQueryParameter(param.Key, param.Value);

            // add static query parameter - API version
            request.AddQueryParameter("APIVersion", "20160101");

            // add form parameter, if any
            foreach (var param in formParams)
                request.AddParameter(param.Key, param.Value);

            // add file parameter, if any
            foreach(var param in fileParams)
            {
                request.AddFile(param.Value.Name, param.Value.Writer, param.Value.FileName, param.Value.ContentLength, param.Value.ContentType);
            }

            if (postBody != null) // http body (model or byte[]) parameter
            {
                request.AddParameter(contentType, postBody, ParameterType.RequestBody);
            }

            return request;
        }

        /// <summary>
        /// Makes the HTTP request (Sync).
        /// </summary>
        /// <param name="path">URL path.</param>
        /// <param name="method">HTTP method.</param>
        /// <param name="queryParams">Query parameters.</param>
        /// <param name="postBody">HTTP body (POST request).</param>
        /// <param name="headerParams">Header parameters.</param>
        /// <param name="formParams">Form parameters.</param>
        /// <param name="fileParams">File parameters.</param>
        /// <param name="pathParams">Path parameters.</param>
        /// <param name="contentType">Content Type of the request</param>
        /// <returns>Object</returns>
        public Object CallApi(
            String path, RestSharp.Method method, List<KeyValuePair<String, String>> queryParams, Object postBody,
            Dictionary<String, String> headerParams, Dictionary<String, String> formParams,
            Dictionary<String, FileParameter> fileParams, Dictionary<String, String> pathParams,
            String contentType)
        {
            var request = PrepareRequest(
                path, method, queryParams, postBody, headerParams, formParams, fileParams,
                pathParams, contentType);

            // set timeout
            RestClient.Timeout = Configuration.Timeout;
            // set user agent
            RestClient.UserAgent = Configuration.UserAgent;

            InterceptRequest(request);
            var response = RestClient.Execute(request);
            InterceptResponse(request, response);

            return (Object) response;
        }
        /// <summary>
        /// Makes the asynchronous HTTP request.
        /// </summary>
        /// <param name="path">URL path.</param>
        /// <param name="method">HTTP method.</param>
        /// <param name="queryParams">Query parameters.</param>
        /// <param name="postBody">HTTP body (POST request).</param>
        /// <param name="headerParams">Header parameters.</param>
        /// <param name="formParams">Form parameters.</param>
        /// <param name="fileParams">File parameters.</param>
        /// <param name="pathParams">Path parameters.</param>
        /// <param name="contentType">Content type.</param>
        /// <returns>The Task instance.</returns>
        public async Task<Object> CallApiAsync(
            String path, RestSharp.Method method, List<KeyValuePair<String, String>> queryParams, Object postBody,
            Dictionary<String, String> headerParams, Dictionary<String, String> formParams,
            Dictionary<String, FileParameter> fileParams, Dictionary<String, String> pathParams,
            String contentType)
        {
            var request = PrepareRequest(
                path, method, queryParams, postBody, headerParams, formParams, fileParams,
                pathParams, contentType);

            InterceptRequest(request);
            var response = await RestClient.ExecuteAsync(request);
            InterceptResponse(request, response);
            return (Object)response;
        }

        /// <summary>
        /// Makes the asynchronous HTTP stream request.
        /// </summary>
        /// <param name="path">URL path.</param>
        /// <param name="method">HTTP method.</param>
        /// <param name="queryParams">Query parameters.</param>
        /// <param name="postBody">HTTP body (POST request).</param>
        /// <param name="headerParams">Header parameters.</param>
        /// <param name="formParams">Form parameters.</param>
        /// <param name="fileParams">File parameters.</param>
        /// <param name="pathParams">Path parameters.</param>
        /// <param name="contentType">Content type.</param>
        /// <returns>The Task instance.</returns>
        public async Task<Object> CallApiStreamAsync(
            String path, RestSharp.Method method, List<KeyValuePair<String, String>> queryParams, Object postBody,
            Dictionary<String, String> headerParams, Dictionary<String, String> formParams,
            Dictionary<String, FileParameter> fileParams, Dictionary<String, String> pathParams,
            String contentType, Func<string, Exception, Task<bool>> OnChunkReceived, CancellationToken cancellationToken)
        {
            var request = PrepareRequest(
                path, method, queryParams, postBody, headerParams, formParams, fileParams,
                pathParams, contentType);

            // Todo: update TradeStationAPI to use HttpClient as a base service, not HttpWebRequest.
            // The possible solution is to use NSwag client generator with HttpClient injection.
            // Do not create HttpClient on each function call.

            var client = new HttpClient();
            foreach (var header in headerParams)
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            client.DefaultRequestHeaders.Add("UserAgent", "Swagger-Codegen/1.0.0/csharp");
            client.Timeout = Timeout.InfiniteTimeSpan;
            client.DefaultRequestHeaders.Connection.Clear();
            client.DefaultRequestHeaders.ConnectionClose = false;
            client.DefaultRequestHeaders.Connection.Add("Keep-Alive");
            var response = await client.GetAsync(RestClient.BuildUri(request), HttpCompletionOption.ResponseHeadersRead);

            var result = new RestResponse() { StatusCode = response.StatusCode };

            if (response.StatusCode == HttpStatusCode.OK)
            {
                _ = Task.Run(async () =>
                {
                    using (var responseStream = await response.Content.ReadAsStreamAsync())
                    {

                        byte[] buffer = new byte[128 * 1024];

                        while (!cancellationToken.IsCancellationRequested)
                        {
                            if (responseStream.CanRead)                     // there is data to read
                            {
                                int readCount = 0;
                                try
                                {
                                    readCount = responseStream.Read(buffer, 0, buffer.Length);
                                }
                                catch (Exception ex)
                                {
                                    await OnChunkReceived(null, ex);
                                    return;
                                }
                                if (readCount > 0)
                                {
                                    string res = RestClient.Encoding.GetString(buffer, 0, readCount);
                                    bool isContinue = await OnChunkReceived(res, null);
                                    if (!isContinue)
                                        return;
                                }
                                else
                                    return; // no data to read. Exit
                            }
                            else
                            {
                                try
                                {
                                    await Task.Delay(200, cancellationToken);   // wait next portion of data
                                }
                                catch (TaskCanceledException) { }
                            }
                        }
                    }
                }, cancellationToken);
            }
            else
            { 
            }
            /*
            request.ReadWriteTimeout = 123000000;   // more than 24 hours

            request.AdvancedResponseWriter = async (responseStream, resp) =>
            {
                if (resp.StatusCode != HttpStatusCode.OK) return;   // don't read stream if response is not successful

                byte[] buffer = new byte[128 * 1024];

                while (!cancellationToken.IsCancellationRequested)
                {
                    if (responseStream.CanRead)                     // there is data to read
                    {
                        int readCount = 0;
                        try
                        {
                            readCount = responseStream.Read(buffer, 0, buffer.Length);
                        }
                        catch (Exception ex)
                        {
                            OnChunkReceived(null, ex);
                            return;
                        }
                        if (readCount > 0)
                        {
                            string res = RestClient.Encoding.GetString(buffer, 0, readCount);
                            OnChunkReceived(res, null);
                        }
                        else
                            return; // no data to read. Exit
                    }
                    else
                    {
                        try
                        {
                            await Task.Delay(200, cancellationToken);   // wait next portion of data
                        }
                        catch (TaskCanceledException) { }
                    }
                }
            };

            InterceptRequest(request);
            var response = await RestClient.ExecuteAsync(request);
            InterceptResponse(request, response);
            */
            return result;
        }

        /// <summary>
        /// Escape string (url-encoded).
        /// </summary>
        /// <param name="str">String to be escaped.</param>
        /// <returns>Escaped string.</returns>
        public string EscapeString(string str)
        {
            return UrlEncode(str);
        }

        /// <summary>
        /// Create FileParameter based on Stream.
        /// </summary>
        /// <param name="name">Parameter name.</param>
        /// <param name="stream">Input stream.</param>
        /// <returns>FileParameter.</returns>
        public FileParameter ParameterToFile(string name, Stream stream)
        {
            if (stream is FileStream)
                return FileParameter.Create(name, ReadAsBytes(stream), Path.GetFileName(((FileStream)stream).Name));
            else
                return FileParameter.Create(name, ReadAsBytes(stream), "no_file_name_provided");
        }

        /// <summary>
        /// If parameter is DateTime, output in a formatted string (default ISO 8601), customizable with Configuration.DateTime.
        /// If parameter is a list, join the list with ",".
        /// Otherwise just return the string.
        /// </summary>
        /// <param name="obj">The parameter (header, path, query, form).</param>
        /// <returns>Formatted string.</returns>
        public string ParameterToString(object obj)
        {
            if (obj is DateTime)
                // Return a formatted date string - Can be customized with Configuration.DateTimeFormat
                // Defaults to an ISO 8601, using the known as a Round-trip date/time pattern ("o")
                // https://msdn.microsoft.com/en-us/library/az4se3k1(v=vs.110).aspx#Anchor_8
                // For example: 2009-06-15T13:45:30.0000000
                return ((DateTime)obj).ToString (Configuration.DateTimeFormat);
            else if (obj is DateTimeOffset)
                // Return a formatted date string - Can be customized with Configuration.DateTimeFormat
                // Defaults to an ISO 8601, using the known as a Round-trip date/time pattern ("o")
                // https://msdn.microsoft.com/en-us/library/az4se3k1(v=vs.110).aspx#Anchor_8
                // For example: 2009-06-15T13:45:30.0000000
                return ((DateTimeOffset)obj).ToString (Configuration.DateTimeFormat);
            else if (obj is IList)
            {
                var flattenedString = new StringBuilder();
                foreach (var param in (IList)obj)
                {
                    if (flattenedString.Length > 0)
                        flattenedString.Append(",");
                    flattenedString.Append(param);
                }
                return flattenedString.ToString();
            }
            else
                return Convert.ToString (obj);
        }

        // todo: Deserialize Async

        /// <summary>
        /// Deserialize the JSON string into a proper object.
        /// </summary>
        /// <param name="response">The HTTP response.</param>
        /// <param name="type">Object type.</param>
        /// <returns>Object representation of the JSON string.</returns>
        public object Deserialize(IRestResponse response, Type type)
        {
            IList<Parameter> headers = response.Headers;
            if (type == typeof(byte[])) // return byte array
            {
                return response.RawBytes;
            }

            // TODO: ? if (type.IsAssignableFrom(typeof(Stream)))
            if (type == typeof(Stream))
            {
                if (headers != null)
                {
                    var filePath = String.IsNullOrEmpty(Configuration.TempFolderPath)
                        ? Path.GetTempPath()
                        : Configuration.TempFolderPath;
                    var regex = new Regex(@"Content-Disposition=.*filename=['""]?([^'""\s]+)['""]?$");
                    foreach (var header in headers)
                    {
                        var match = regex.Match(header.ToString());
                        if (match.Success)
                        {
                            string fileName = filePath + SanitizeFilename(match.Groups[1].Value.Replace("\"", "").Replace("'", ""));
                            File.WriteAllBytes(fileName, response.RawBytes);
                            return new FileStream(fileName, FileMode.Open);
                        }
                    }
                }
                var stream = new MemoryStream(response.RawBytes);
                return stream;
            }

            if (type.Name.StartsWith("System.Nullable`1[[System.DateTime")) // return a datetime object
            {
                return DateTime.Parse(response.Content,  null, System.Globalization.DateTimeStyles.RoundtripKind);
            }

            if (type == typeof(String) || type.Name.StartsWith("System.Nullable")) // return primitive type
            {
                return ConvertType(response.Content, type);
            }

            // at this point, it must be a model (json)
            try
            {
                return JsonConvert.DeserializeObject(response.Content, type, serializerSettings);
            }
            catch (Exception e)
            {
                throw new ApiException(500, e.Message);
            }
        }

        /// <summary>
        /// Serialize an input (model) into JSON string
        /// </summary>
        /// <param name="obj">Object.</param>
        /// <returns>JSON string.</returns>
        public String Serialize(object obj)
        {
            try
            {
                return obj != null ? JsonConvert.SerializeObject(obj) : null;
            }
            catch (Exception e)
            {
                throw new ApiException(500, e.Message);
            }
        }

        /// <summary>
        ///Check if the given MIME is a JSON MIME.
        ///JSON MIME examples:
        ///    application/json
        ///    application/json; charset=UTF8
        ///    APPLICATION/JSON
        ///    application/vnd.company+json
        /// </summary>
        /// <param name="mime">MIME</param>
        /// <returns>Returns True if MIME type is json.</returns>
        public bool IsJsonMime(String mime)
        {
            var jsonRegex = new Regex("(?i)^(application/json|[^;/ \t]+/[^;/ \t]+[+]json)[ \t]*(;.*)?$");
            return mime != null && (jsonRegex.IsMatch(mime) || mime.Equals("application/json-patch+json"));
        }

        /// <summary>
        /// Select the Content-Type header's value from the given content-type array:
        /// if JSON type exists in the given array, use it;
        /// otherwise use the first one defined in 'consumes'
        /// </summary>
        /// <param name="contentTypes">The Content-Type array to select from.</param>
        /// <returns>The Content-Type header to use.</returns>
        public String SelectHeaderContentType(String[] contentTypes)
        {
            if (contentTypes.Length == 0)
                return "application/json";

            foreach (var contentType in contentTypes)
            {
                if (IsJsonMime(contentType.ToLower()))
                    return contentType;
            }

            return contentTypes[0]; // use the first content type specified in 'consumes'
        }

        /// <summary>
        /// Select the Accept header's value from the given accepts array:
        /// if JSON exists in the given array, use it;
        /// otherwise use all of them (joining into a string)
        /// </summary>
        /// <param name="accepts">The accepts array to select from.</param>
        /// <returns>The Accept header to use.</returns>
        public String SelectHeaderAccept(String[] accepts)
        {
            if (accepts.Length == 0)
                return null;

            if (accepts.Contains("application/json", StringComparer.OrdinalIgnoreCase))
                return "application/json";

            return String.Join(",", accepts);
        }

        /// <summary>
        /// Encode string in base64 format.
        /// </summary>
        /// <param name="text">String to be encoded.</param>
        /// <returns>Encoded string.</returns>
        public static string Base64Encode(string text)
        {
            return System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(text));
        }

        /// <summary>
        /// Dynamically cast the object into target type.
        /// </summary>
        /// <param name="fromObject">Object to be casted</param>
        /// <param name="toObject">Target type</param>
        /// <returns>Casted object</returns>
        public static dynamic ConvertType(dynamic fromObject, Type toObject)
        {
            return Convert.ChangeType(fromObject, toObject);
        }

        /// <summary>
        /// Convert stream to byte array
        /// </summary>
        /// <param name="inputStream">Input stream to be converted</param>
        /// <returns>Byte array</returns>
        public static byte[] ReadAsBytes(Stream inputStream)
        {
            byte[] buf = new byte[16*1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int count;
                while ((count = inputStream.Read(buf, 0, buf.Length)) > 0)
                {
                    ms.Write(buf, 0, count);
                }
                return ms.ToArray();
            }
        }

        /// <summary>
        /// URL encode a string
        /// Credit/Ref: https://github.com/restsharp/RestSharp/blob/master/RestSharp/Extensions/StringExtensions.cs#L50
        /// </summary>
        /// <param name="input">String to be URL encoded</param>
        /// <returns>Byte array</returns>
        public static string UrlEncode(string input)
        {
            const int maxLength = 32766;

            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            if (input.Length <= maxLength)
            {
                return Uri.EscapeDataString(input);
            }

            StringBuilder sb = new StringBuilder(input.Length * 2);
            int index = 0;

            while (index < input.Length)
            {
                int length = Math.Min(input.Length - index, maxLength);
                string subString = input.Substring(index, length);

                sb.Append(Uri.EscapeDataString(subString));
                index += subString.Length;
            }

            return sb.ToString();
        }

        /// <summary>
        /// Sanitize filename by removing the path
        /// </summary>
        /// <param name="filename">Filename</param>
        /// <returns>Filename</returns>
        public static string SanitizeFilename(string filename)
        {
            Match match = Regex.Match(filename, @".*[/\\](.*)$");

            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            else
            {
                return filename;
            }
        }

        /// <summary>
        /// Convert params to key/value pairs. 
        /// Use collectionFormat to properly format lists and collections.
        /// </summary>
        /// <param name="name">Key name.</param>
        /// <param name="value">Value object.</param>
        /// <returns>A list of KeyValuePairs</returns>
        public IEnumerable<KeyValuePair<string, string>> ParameterToKeyValuePairs(string collectionFormat, string name, object value)
        {
            var parameters = new List<KeyValuePair<string, string>>();

            if (IsCollection(value) && collectionFormat == "multi")
            {
                var valueCollection = value as IEnumerable;
                parameters.AddRange(from object item in valueCollection select new KeyValuePair<string, string>(name, ParameterToString(item)));
            }
            else
            {
                parameters.Add(new KeyValuePair<string, string>(name, ParameterToString(value)));
            }

            return parameters;
        }

        /// <summary>
        /// Check if generic object is a collection.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>True if object is a collection type</returns>
        private static bool IsCollection(object value)
        {
            return value is IList || value is ICollection;
        }
    }
}
