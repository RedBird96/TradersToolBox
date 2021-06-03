using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradersToolbox.DataObjects
{
    //public enum Exchange { NASDAQ, NYSE, AMEX, ARCA, BATS }

    public class TickerData
    {
        public string Ticker { get; set; }
        public string Exchange { get; set; }
        public string Description { get; set; }

        public TickerData()
        {

        }

        public TickerData(string ticker, string exchange, string description)
        {
            Ticker = ticker;
            Exchange = exchange;
            Description = description;
        }
    }
}
