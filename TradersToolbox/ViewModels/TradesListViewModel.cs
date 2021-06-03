using System;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm;
using System.ComponentModel;
using System.Collections.ObjectModel;
using TradersToolbox.Data;

namespace TradersToolbox.ViewModels
{
    [POCOViewModel]
    public class TradesListViewModel : IDocumentContent
    {
        public ObservableCollection<TradesListItem> Trades { get; set; }
        public bool IsRawColumnVisible { get; set; }

        public virtual double WindowWidth { get; set; } = 500;
        public virtual double WindowHeight { get; set; } = 500;

        #region IDocumentContent
        public object Title { get; set; } = "Trades";
        public IDocumentOwner DocumentOwner { get; set; }

        public void OnClose(CancelEventArgs e)
        {
        }

        public void OnDestroy()
        {
        }
        #endregion
    }
}