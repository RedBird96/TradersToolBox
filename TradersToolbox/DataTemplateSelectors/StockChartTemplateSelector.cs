using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TradersToolbox.ViewModels;
using TradersToolbox.ViewModels.ChartViewModels;

namespace TradersToolbox.DataTemplateSelectors
{
    public class StockChartTemplateSelector : DataTemplateSelector
    {
        public DataTemplate StockChartTemplate { get; set; }
        public DataTemplate SubgraphTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {

            if (item != null && item is IChartViewModel)
            {
                switch ((item as IChartViewModel).Type)
                {
                    case ChartViewModelType.StockChart:
                        return StockChartTemplate;
                    case ChartViewModelType.Subgraph:
                        return SubgraphTemplate;
                    default:
                        return null;
                }
            }
            return null;
        }
    }
}
