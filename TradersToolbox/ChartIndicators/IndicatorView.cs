using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradersToolbox.ChartIndicators
{
    public class IndicatorView : INotifyPropertyChanged
    {
        public IndicatorView()
        {
            ID = Guid.NewGuid();
        }

        public IndicatorViewType ViewType { get; set; }
        public IndicatorType Type { get; set; }

        public StockPatternSearch.PatternStyle PatternType { get; set; }

        public string Name { get; set; }

        public Guid ID { get; set; }
        public string Title
        {
            get
            {
                return Name + " " + SubtTitle;
            }
        }



        public string _SubtTitle = string.Empty;
        public string SubtTitle
        {
            get
            {
                return _SubtTitle;
            }
            set
            {
                _SubtTitle = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Title"));
            }
        }

        public string Tag { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public enum IndicatorViewType
    {
        Subgraph = 1,
        OverlayOrSubgraph = 2,
        Overlay,
        LineAnnotation
    }

    public enum IndicatorInputType
    {
        None,
        High,
        Low,
        Open,
        Close
    }

    public enum IndicatorGraphType
    {
        None,
        Column,
        Mountain,
        Line,
        StackedMountain,
        MultiLine3,
    }

    public enum IndicatorType
    {
        ATR,
        Autocor,
        BollingerBand,
        ConsecutiveHigher,
        ConsecutiveLower,
        DayOfMonth,
        DayOfWeek,
        Doji,
        EMA,
        Hammer,
        Highest,
        Hurst,
        IBS,
        Inverted,
        KaufmanER,
        Lowest,
        MaxRange,
        MinRange,
        Month,
        PercentChange,
        PerformanceMTD,
        PerformanceQTD,
        PerformanceYTD,
        PivotPP,
        PivotR1,
        PivotR2,
        PivotS1,
        PivotS2,
        Quarter,
        Range,
        RSI,
        SMA,
        TDLM,
        TDOM,
        ValueChart,
        Volume,
        Vix,
        WeekNumber,
        WinsLast,

        KeltnerChannel,
        ParabolicSAR,
        CCI,
        ADX,
        DMIpositive,
        DMInegative,
        CompositeRSI,
        CompositeATR,
        CompositeHurst,
        CompositeStochastic,
        CompositeSuperSmooth,
        CompositeSMA,
        CompositeEMA,
        MACD,
        MACDhist,
        RateOfChange,
        Momentum,
        Stochastics,

        Median,

        BollingerBands,
        KeltnerChannels,

        Pattern
    }
}
