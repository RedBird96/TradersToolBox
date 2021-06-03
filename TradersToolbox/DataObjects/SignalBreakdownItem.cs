using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradersToolbox.Data
{
    public class SignalBreakdownItem
    {
        public string SignalName { get; set; }
        public float Win { get; set; }
        public float Loss { get; set; }
        public float Diff => Win - Loss;
    }
}
