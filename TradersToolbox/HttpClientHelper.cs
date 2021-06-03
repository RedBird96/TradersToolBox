using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TradersToolbox
{
    public static class HttpClientHelper
    {
        const string testHostPath = "http://bing.com/";

        public static HttpClient GetHttpClient(string proxyName = null, string proxyPassword = null)
        {
            var handler = new HttpClientHandler();

            if (IsUsingProxy())
            {
                if (string.IsNullOrEmpty(proxyName))
                    proxyName = Properties.Settings.Default.ProxyName;          // reading app settings
                if (string.IsNullOrEmpty(proxyPassword))
                    proxyPassword = Properties.Settings.Default.ProxyPassword;  // reading app settings

                handler.Proxy = WebRequest.GetSystemWebProxy();

                if (!string.IsNullOrEmpty(proxyName) && !string.IsNullOrEmpty(proxyPassword))
                    handler.Proxy.Credentials = new NetworkCredential(proxyName, proxyPassword);
            }

            var httpClient = new HttpClient(handler);
            httpClient.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue()
            {
                NoCache = true,
                NoStore = true
            };
            return httpClient;
        }

        public static bool IsUsingProxy()
        {
            IWebProxy sysProxy = WebRequest.GetSystemWebProxy();
            if (sysProxy == null)
                return false;
            else
                return sysProxy.GetProxy(new Uri(testHostPath)).OriginalString != testHostPath;
        }

        public static async Task<bool> TestConnection(HttpClient httpClient)
        {
            var resp = await httpClient.GetAsync(testHostPath);
            return resp.IsSuccessStatusCode;
        }
    }
}
