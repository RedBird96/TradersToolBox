using DevExpress.XtraPrinting.Native;
using SciChart.Charting.Visuals.RenderableSeries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradersToolbox.SeriesInfos
{
    public class CustomOhlcSeriesInfo : SciChart.Charting.Model.ChartData.SeriesInfo
    {
        public string FormattedOpenValue { get; set; }
        public string FormattedHighValue { get; set; }
        public string FormattedLowValue { get; set; }
        public string FormattedCloseValue { get; set; }
        public string DateValue { get; set; }
        public string TimeValue { get; set; }
        public string Volume { get; set; }

        public List<Pair<string, string>> AdditionalData { get; set; }

        public CustomOhlcSeriesInfo(IRenderableSeries rSeries) : base(rSeries)
        {
        }
    }

}
