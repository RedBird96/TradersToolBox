using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradersToolbox.Data
{
    public class TradesListItem
    {
        public int Index { get; set; }
        public int Mode { get; set; }
        public string ModeStr
        {
            get
            {
                switch (Mode)
                {
                    case 1: return Index + "  PT"; 
                    case 2: return Index + "  SL"; 
                    case 3: return Index + "  HH"; 
                    case 4: return Index + "  LL"; 
                    case 5: return Index + "  IC"; 
                    case 6: return Index + "  EX"; 
                    case 7: return Index + "  TL"; 
                    case 8: return Index + "  RB"; 
                    case 9: return Index + "  END";
                    default: return Index.ToString();
                }
            }
        }
        public DateTime EntryDT { get; set; }
        public DateTime ExitDT { get; set; }
        public float EntryValue { get; set; }
        public float ExitValue { get; set; }
        public float PosSize { get; set; }
        public float Result { get; set; }
    }
}
