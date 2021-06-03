using DevExpress.XtraPrinting.Native;
using SciChart.Charting.Visuals.RenderableSeries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradersToolbox.SeriesInfos
{
    public class CustomLineSeriesInfo : SciChart.Charting.Model.ChartData.SeriesInfo
    {
        public string FormattedCloseValue { get; set; }
        public string DateValue { get; set; }
        public string TimeValue { get; set; }
        public List<Pair<string, string>> AdditionalData { get; set; }

        public CustomLineSeriesInfo(IRenderableSeries rSeries) : base(rSeries)
        {
        }
    }
}
