using System;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm;
using System.ComponentModel;
using DevExpress.Mvvm.POCO;

namespace TradersToolbox.ViewModels
{
    [POCOViewModel]
    public class GraphHistorgamViewModel : IDocumentContent, IGraphViewModelBase
    {
        public object Graph { get; set; }

        public virtual double WindowWidth { get; set; }
        public virtual double WindowHeight { get; set; }

        protected void OnWindowWidthChanged()
        {
            Properties.Settings.Default.HistogramWindowWidth = WindowWidth;
        }

        protected void OnWindowHeightChanged()
        {
            Properties.Settings.Default.HistogramWindowHeight = WindowHeight;
        }

        #region IDocumentContent
        public object Title { get; set; } = "Metrics";
        public IDocumentOwner DocumentOwner { get; set; }

        public void OnClose(CancelEventArgs e)
        {
        }

        public void OnDestroy()
        {
        }
        #endregion

        public static GraphHistorgamViewModel Create()
        {
            return ViewModelSource.Create(() => new GraphHistorgamViewModel());
        }

        protected GraphHistorgamViewModel()
        {
            WindowWidth = Properties.Settings.Default.HistogramWindowWidth;
            WindowHeight = Properties.Settings.Default.HistogramWindowHeight;
        }
    }
}