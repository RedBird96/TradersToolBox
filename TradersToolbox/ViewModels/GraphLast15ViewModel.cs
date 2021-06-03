using System;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm;
using System.ComponentModel;
using DevExpress.Mvvm.POCO;

namespace TradersToolbox.ViewModels
{
    [POCOViewModel]
    public class GraphLast15ViewModel : IDocumentContent, IGraphViewModelBase
    {
        public object Graph { get; set; }

        public virtual double WindowWidth { get; set; }
        public virtual double WindowHeight { get; set; }

        protected void OnWindowWidthChanged()
        {
            Properties.Settings.Default.GraphLast15WindowWidth = WindowWidth;
        }

        protected void OnWindowHeightChanged()
        {
            Properties.Settings.Default.GraphLast15WindowHeight = WindowHeight;
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

        public static GraphLast15ViewModel Create()
        {
            return ViewModelSource.Create(() => new GraphLast15ViewModel());
        }

        protected GraphLast15ViewModel()
        {
            WindowWidth = Properties.Settings.Default.GraphLast15WindowWidth;
            WindowHeight = Properties.Settings.Default.GraphLast15WindowHeight;
        }
    }
}