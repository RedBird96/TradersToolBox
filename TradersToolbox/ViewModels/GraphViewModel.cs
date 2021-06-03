using System;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm;
using System.ComponentModel;
using DevExpress.Mvvm.POCO;

namespace TradersToolbox.ViewModels
{
    public interface IGraphViewModelBase
    {
        object Graph { get; }
    }

    [POCOViewModel]
    public class GraphViewModel : IDocumentContent, IGraphViewModelBase
    {
        public object Graph { get; set; }

        public virtual double WindowWidth { get; set; }
        public virtual double WindowHeight { get; set; }

        protected void OnWindowWidthChanged()
        {
            Properties.Settings.Default.GraphWindowWidth = WindowWidth;
        }

        protected void OnWindowHeightChanged()
        {
            Properties.Settings.Default.GraphWindowHeight = WindowHeight;
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

        public static GraphViewModel Create()
        {
            return ViewModelSource.Create(() => new GraphViewModel());
        }

        protected GraphViewModel()
        {
            WindowWidth = Properties.Settings.Default.GraphWindowWidth;
            WindowHeight = Properties.Settings.Default.GraphWindowHeight;
        }
    }
}