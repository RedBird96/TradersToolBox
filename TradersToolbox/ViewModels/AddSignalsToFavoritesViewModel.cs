using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.POCO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TradersToolbox.Core;
using TradersToolbox.Core.Serializable;

namespace TradersToolbox.ViewModels
{
    [POCOViewModel]
    public class AddSignalsToFavoritesViewModel
    {
        public virtual List<Signal> Signals { get; set; }
        public virtual ObservableCollection<Signal> SelectedSignals { get; set; } = new ObservableCollection<Signal>();

        protected ICurrentWindowService CurrentWindowService { get { return this.GetService<ICurrentWindowService>(); } }

        public static AddSignalsToFavoritesViewModel Create()
        {
            return ViewModelSource.Create(() => new AddSignalsToFavoritesViewModel());
        }
        protected AddSignalsToFavoritesViewModel()
        {
        }

        public void Add()
        {
            if(SelectedSignals!=null)
                foreach(var s in SelectedSignals)
                    SignalsData.AddToFavorites(s.Key);

            CurrentWindowService.Close();
        }

        public void Cancel()
        {
            CurrentWindowService.Close();
        }
    }
}