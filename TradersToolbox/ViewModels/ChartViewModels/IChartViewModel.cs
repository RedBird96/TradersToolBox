using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradersToolbox.Data;

namespace TradersToolbox.ViewModels.ChartViewModels
{
    public interface IChartViewModel
    {
        ChartViewModelType Type { get; }
        void InitAfterLoad(StockChartGroupViewModel Parent, ObservableCollection<TradingData> ChartDataSource);
    }

    public enum ChartViewModelType
    {
        StockChart,
        Subgraph
    }

}
