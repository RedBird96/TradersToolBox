using System;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm;
using System.ComponentModel;
using System.Data;

namespace TradersToolbox.ViewModels
{
    [POCOViewModel]
    public class CorrelationMatrixViewModel : IDocumentContent
    {
        public DataTable DataTable { get; set; }
        public double WindowWidth { get; set; }
        public double WindowHeight { get; set; }

        #region IDocumentContent
        public object Title { get; set; } = "Correlation Matrix";
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