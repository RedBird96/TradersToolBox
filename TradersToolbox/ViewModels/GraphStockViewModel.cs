using System;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm;
using System.ComponentModel;
using DevExpress.Mvvm.POCO;

namespace TradersToolbox.ViewModels
{
    [POCOViewModel]
    public class GraphStockViewModel : IDocumentContent, IGraphViewModelBase
    {
        public object Graph { get; set; }

        public virtual double WindowWidth { get; set; }
        public virtual double WindowHeight { get; set; }

        protected void OnWindowWidthChanged()
        {
            Properties.Settings.Default.GraphStockWindowWidth = WindowWidth;
        }

        protected void OnWindowHeightChanged()
        {
            Properties.Settings.Default.GraphStockWindowHeight = WindowHeight;
        }

        #region IDocumentContent
        public object Title { get; set; } = "Chart";
        public IDocumentOwner DocumentOwner { get; set; }

        public void OnClose(CancelEventArgs e)
        {
        }

        public void OnDestroy()
        {
        }
        #endregion

        public static GraphStockViewModel Create()
        {
            return ViewModelSource.Create(() => new GraphStockViewModel());
        }

        protected GraphStockViewModel()
        {
            WindowWidth = Properties.Settings.Default.GraphStockWindowWidth;
            WindowHeight = Properties.Settings.Default.GraphStockWindowHeight;
        }
    }
}