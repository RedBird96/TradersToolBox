using Microsoft.VisualStudio.TestTools.UnitTesting;
using PolygonIO.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PolygonIO.Api.Tests
{
    [TestClass()]
    public class StocksEquitiesApiTests
    {
        StocksEquitiesApi StocksApi;

        public static readonly string PolygonApiKey = "K6CD972EJqZjLSQ6UNjRIYu78e8_tSLK";
        readonly string[] symbolNames = new string[] {
            "TMBR", "PED", "UUU",   //AMX           +
            "MJJ", /*"PXR",*/ "OVT",    //ARCA      ??
            "FFEB", "IDHD",/*"EDOM",*/  //BATS      ??
            /*"UPL","WTW","NCS",      //CBO
            "AAIR","WVXI","ELEV",   //CVEM
            "HOLA","BKKN","KSGT",*/   //GREY
            "ZXIET","ZIEXT","ZEXIT", //IEXG
            //"FLKS",                 //LSEN
            //"TWLVU",                //MDX
            //"BRS","TI","CBK",       //MIO
            "XM","AAL","CHI",       //NASDAQ        +
            //"SXE","LGCY","HRS",     //NDD
            //"STI-A","PERY","AYA",   //NGS
            //"XMYFX","XFEIX","ICOVEX",   //NMF
            //"NXEO","IKAN","LOOK",   //NSC
            //"ORG","EXA","TINY",     //NSD
            //"SSW-D",                //NSX
            "AA","WOR","UPS",       //NYE           +
            //"PATD","HANOE","ALMG",  //OBB
            //"SSTY","SMLR","GRYG",   //OTC
            //"HPPI","CUMD","GALM",   //OTCQB
            //"CZID","IALB","JCLY",   //OTCQX
            //"MOSKY","IHITF","BHFLL",    //OTO
            //"TIS",                  //SPIC
            //"CIG C","KMRAF","HTGX CL"   //empty
        };

        /*
ARCA – NYSE Arca
AMEX – NYSE MKT
BATS – BATS – BZX Exchange
BX – NASDAQ OMX BX
CHX – Chicago Stock Exchange
IEX – The Investors Exchange
NASDAQ – The NASDAQ Stock Market
NYSE – New York Stock Exchange
U – Unlisted (OTC Equity Securities)
         */


        [TestInitialize]
        public void TestInit()
        {
            PolygonIO.Client.Configuration config = new PolygonIO.Client.Configuration();
            config.AddApiKey("apiKey", PolygonApiKey);
            StocksApi = new StocksEquitiesApi(config);
        }

        [TestCleanup]
        public void TestClean()
        {
            StocksApi = null;
        }


        [TestMethod()]
        public void V1LastQuoteStocksStocksTickerGetTest()
        {
            List<string> invalidSymbols = new List<string>();

            foreach (var symbol in symbolNames)
            {
                var resp = StocksApi.V1LastQuoteStocksStocksTickerGet(symbol);
                if (resp.Last == null)
                    invalidSymbols.Add(symbol);
            }
            Assert.IsTrue(invalidSymbols.Count == 0, $"Last Quote for Symbols ({string.Join(", ", invalidSymbols)}): no result");
        }

        [TestMethod()]
        public void V1LastStocksStocksTickerGetTest()
        {
            foreach (var symbol in symbolNames)
            {
                var resp = StocksApi.V1LastStocksStocksTickerGet(symbol);
                Assert.IsNotNull(resp.Last, $"Last Trade for a Symbol ({symbol}): no result");
            }
        }

        [TestMethod()]
        public void V2AggsTickerStocksTickerPrevGetTest()
        {
            foreach (var symbol in symbolNames)
            {
                var resp = StocksApi.V2AggsTickerStocksTickerPrevGet(symbol);
                Assert.AreEqual("OK", resp.Status);
                Assert.IsTrue(resp.ResultsCount > 0, $"Previous Close for a Symbol ({symbol}): no result");
            }
        }

        [TestMethod()]
        public void V2SnapshotLocaleUsMarketsStocksTickersStocksTickerGetTest()
        {
            foreach (var symbol in symbolNames)
            {
                var resp = StocksApi.V2SnapshotLocaleUsMarketsStocksTickersStocksTickerGet(symbol);
                Assert.IsNotNull(resp.Ticker, $"Snapshot - Ticker ({symbol}): no result");
            }
        }

        [TestMethod()]
        public void SymbolsWithDataTest()
        {
            // Read all symbols from OATS Reportable Security Daily List
            // https://www.finra.org/filing-reporting/oats/oats-reportable-securities-list

            HttpClient httpClient = new HttpClient();
            var task = httpClient.GetStringAsync("http://oatsreportable.finra.org/OATSReportableSecurities-SOD.txt");
            task.Wait();

            Dictionary<string, string> symbols = new Dictionary<string, string>();

            bool skipFirstLine = true;
            using (System.IO.StringReader reader = new System.IO.StringReader(task.Result))
            {
                string line = reader.ReadLine();
                while (line != null)
                {
                    if (skipFirstLine)
                    {
                        skipFirstLine = false;
                        if(!line.StartsWith("A"))
                            continue;
                    }

                    var cells = line.Split('|');
                    if(cells.Length == 3)
                    {
                        symbols.Add(cells[0], cells[2]);
                    }

                    line = reader.ReadLine();
                }
            }

            var symbolsNASDAQ = symbols.Where(x => x.Value == "NASDAQ").Select(x => ConvertTickerFromFinra(x.Key)).ToList();
            var symbolsNYSE = symbols.Where(x => x.Value == "NYSE").Select(x => ConvertTickerFromFinra(x.Key)).ToList();
            var symbolsARCA = symbols.Where(x => x.Value == "ARCA").Select(x => ConvertTickerFromFinra(x.Key)).ToList();
            var symbolsAMEX = symbols.Where(x => x.Value == "AMEX").Select(x => ConvertTickerFromFinra(x.Key)).ToList();
            var symbolsBATS = symbols.Where(x => x.Value == "BATS").Select(x => ConvertTickerFromFinra(x.Key)).ToList();

            List<string> invalidSymbolsNASDAQ = new List<string>();

            foreach (var symbol in symbolsNASDAQ)
            {
                var resp = StocksApi.V1LastQuoteStocksStocksTickerGet(symbol);
                if (resp.Last == null)
                    invalidSymbolsNASDAQ.Add(symbol);
            }

            List<string> invalidSymbolsNYSE = new List<string>();

            foreach (var symbol in symbolsNYSE)
            {
                var resp = StocksApi.V1LastQuoteStocksStocksTickerGet(symbol);
                if (resp.Last == null)
                    invalidSymbolsNYSE.Add(symbol);
            }
            

            List<string> invalidSymbolsARCA = new List<string>();

            foreach (var symbol in symbolsARCA)
            {
                var resp = StocksApi.V1LastQuoteStocksStocksTickerGet(symbol);
                if (resp.Last == null)
                    invalidSymbolsARCA.Add(symbol);
            }
            

            List<string> invalidSymbolsAMEX = new List<string>();

            foreach (var symbol in symbolsAMEX)
            {
                var resp = StocksApi.V1LastQuoteStocksStocksTickerGet(symbol);
                if (resp.Last == null)
                    invalidSymbolsAMEX.Add(symbol);
            }
            

            List<string> invalidSymbolsBATS = new List<string>();

            foreach (var symbol in symbolsBATS)
            {
                var resp = StocksApi.V1LastQuoteStocksStocksTickerGet(symbol);
                if (resp.Last == null)
                    invalidSymbolsBATS.Add(symbol);
            }
            

            Assert.IsTrue(invalidSymbolsNASDAQ.Count == 0, $"Last Quote for Symbols ({string.Join(", ", invalidSymbolsNASDAQ)}): no result");
            Assert.IsTrue(invalidSymbolsNYSE.Count == 0, $"Last Quote for Symbols ({string.Join(", ", invalidSymbolsNYSE)}): no result");
            Assert.IsTrue(invalidSymbolsARCA.Count == 0, $"Last Quote for Symbols ({string.Join(", ", invalidSymbolsARCA)}): no result");
            Assert.IsTrue(invalidSymbolsAMEX.Count == 0, $"Last Quote for Symbols ({string.Join(", ", invalidSymbolsAMEX)}): no result");
            Assert.IsTrue(invalidSymbolsBATS.Count == 0, $"Last Quote for Symbols ({string.Join(", ", invalidSymbolsBATS)}): no result");
        }

        public string ConvertTickerFromFinra(string name)
        {
            if(name.Contains(' '))
            {
                name = Regex.Replace(name, @"\sPR(\w?)$", "-$1");
                name = Regex.Replace(name, @"\sWS$", ".WS");
                name = Regex.Replace(name, @"\sWS(\w)$", ".WS.$1");
                name = Regex.Replace(name, @"\s([AB])$", ".$1");
            }
            return name;
        }

        [TestMethod()]
        public void V1MetaExchangesGetTest()
        {
            var resp = StocksApi.V1MetaExchangesGet();
            Assert.IsTrue(resp.Count > 0);
        }
    }
}