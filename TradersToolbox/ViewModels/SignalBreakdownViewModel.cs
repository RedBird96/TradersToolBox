using System;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm;
using System.ComponentModel;
using System.Collections.ObjectModel;
using TradersToolbox.Data;

namespace TradersToolbox.ViewModels
{
    [POCOViewModel]
    public class SignalBreakdownViewModel : IDocumentContent
    {
        public ObservableCollection<SignalBreakdownItem> Items { get; set; }

        public double WindowWidth { get; set; } = 600;
        public double WindowHeight { get; set; } = 400;

        #region IDocumentContent
        public object Title { get; set; } = "Signal Breakdown";
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