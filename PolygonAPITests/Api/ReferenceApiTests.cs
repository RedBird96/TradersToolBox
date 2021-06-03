using Microsoft.VisualStudio.TestTools.UnitTesting;
using PolygonIO.Api;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolygonIO.Api.Tests
{
    [TestClass()]
    public class ReferenceApiTests
    {
        ReferenceApi ReferenceApi;

        [TestInitialize]
        public void TestInit()
        {
            PolygonIO.Client.Configuration config = new PolygonIO.Client.Configuration();
            config.AddApiKey("apiKey", StocksEquitiesApiTests.PolygonApiKey);
            ReferenceApi = new ReferenceApi(config);
        }

        [TestCleanup]
        public void TestClean()
        {
            ReferenceApi = null;
        }

        [TestMethod()]
        public void V2ReferenceTickersGetTest()
        {
            ConcurrentBag<PolygonIO.Model.TickersTickers> symbols = new ConcurrentBag<PolygonIO.Model.TickersTickers>();

            var firstResp = ReferenceApi.V2ReferenceTickersGet(null, null, "stocks", null, null, 50);

            Assert.AreEqual("OK", firstResp.Status);

            int totalCount = firstResp.Count ?? 0;

            if (totalCount > 0)
            {
                foreach (var item in firstResp.Tickers)
                {
                    if (item.Active.HasValue && item.Active.Value)
                        symbols.Add(item);
                }

                List<Task> tasks = new List<Task>();

                int pageNumber = 2;
                for (; 50 * (pageNumber - 1) < totalCount; pageNumber++)
                {
                    var gett = ReferenceApi.V2ReferenceTickersGetAsync(null, null, "stocks", null, null, 50, pageNumber);
                    var addt = gett.ContinueWith((x) =>
                    {
                        Assert.AreEqual("OK", x.Result.Status);

                        foreach (var item in x.Result.Tickers)
                        {
                            if (item.Active.HasValue && item.Active.Value)
                                symbols.Add(item);
                        }
                    });
                    tasks.Add(addt);
                }

                Task.WaitAll(tasks.ToArray());
            }

            //Save to file
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[");
            foreach (var s in symbols)
                sb.AppendLine(s.ToJson() + ",");
            sb.AppendLine("]");
            System.IO.File.WriteAllText("c:\\Users\\Dimatsi\\Downloads\\apiTest.txt", sb.ToString());


            // Analize exchanges
            sb.Clear();
            var gr = symbols.GroupBy(x => x.PrimaryExch);
            foreach (var g in gr)
            {
                var items = symbols.Where(x => x.PrimaryExch == g.Key);

                sb.AppendLine($"{g.Key}\t{items.Count()}");
            }
            System.IO.File.WriteAllText("c:\\Users\\Dimatsi\\Downloads\\apiTestExchanges.txt", sb.ToString());
        }
    }
}