using DevExpress.Xpf.Charts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradersToolbox.Brokers;
using TradersToolbox.DataSources;
using TradersToolbox.ViewModels.ChartViewModels;

namespace TradersToolbox.ViewModels.DialogsViewModels
{
    public class FormatDataViewModel
    {
        public string SymbolName { get; set; }

        public List<TimeZoneInfo> TimeZones => TimeZoneInfo.GetSystemTimeZones().ToList();

        public TimeZoneInfo CurentTimeZone { get; set; }
        public ChartIntervalItem SelectedInterval { get; set; }
        public StockHistorySettings CurrentHistorySettings { get; set; }
        public MainChartData CurrentChartData { get; set; }


        public string MeasureUnit
        {
            get { return Enum.GetName(typeof(DateTimeMeasureUnit), SelectedInterval.MeasureUnit); }
            set
            {
                DateTimeMeasureUnit unit = (DateTimeMeasureUnit)Enum.Parse(typeof(DateTimeMeasureUnit), value);
                SelectedInterval.MeasureUnit = unit;
            }
        }
        public List<string> MeasureUnits => Enum.GetNames(typeof(DateTimeMeasureUnit)).ToList();
    }
}
