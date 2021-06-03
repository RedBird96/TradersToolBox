using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.POCO;
using System;
using System.Collections.ObjectModel;

namespace TradersToolbox.ViewModels
{
    [POCOViewModel]
    public class SimulatedTradingViewModel
    {
        public ObservableCollection<string> Symbols { get; set; }

        public static SimulatedTradingViewModel Create()
        {
            return ViewModelSource.Create(() => new SimulatedTradingViewModel());
        }
        protected SimulatedTradingViewModel()
        {
            
        }
    }
}