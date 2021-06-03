using System;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm;
using System.ComponentModel;
using DevExpress.Mvvm.POCO;

namespace TradersToolbox.ViewModels
{
    [POCOViewModel]
    public class GraphBreakdownViewModel : IDocumentContent, IGraphViewModelBase
    {
        public object Graph { get; set; }

        public virtual double WindowWidth { get; set; }
        public virtual double WindowHeight { get; set; }

        protected void OnWindowWidthChanged()
        {
            Properties.Settings.Default.BreakdownWindowWidth = WindowWidth;
        }

        protected void OnWindowHeightChanged()
        {
            Properties.Settings.Default.BreakdownWindowHeight = WindowHeight;
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

        public static GraphBreakdownViewModel Create()
        {
            return ViewModelSource.Create(() => new GraphBreakdownViewModel());
        }

        protected GraphBreakdownViewModel()
        {
            WindowWidth = Properties.Settings.Default.BreakdownWindowWidth;
            WindowHeight = Properties.Settings.Default.BreakdownWindowHeight;
        }
    }
}