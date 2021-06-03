using System;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm;
using System.ComponentModel;
using DevExpress.Mvvm.POCO;

namespace TradersToolbox.ViewModels
{
    [POCOViewModel]
    public class GraphRandOOSViewModel : IDocumentContent, IGraphViewModelBase
    {
        public object Graph { get; set; }

        public virtual double WindowWidth { get; set; }
        public virtual double WindowHeight { get; set; }

        protected void OnWindowWidthChanged()
        {
            Properties.Settings.Default.RandomOOSWindowWidth = WindowWidth;
        }

        protected void OnWindowHeightChanged()
        {
            Properties.Settings.Default.RandomOOSWindowHeight = WindowHeight;
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

        public static GraphRandOOSViewModel Create()
        {
            return ViewModelSource.Create(() => new GraphRandOOSViewModel());
        }


        protected GraphRandOOSViewModel()
        {
            WindowWidth = Properties.Settings.Default.RandomOOSWindowWidth;
            WindowHeight = Properties.Settings.Default.RandomOOSWindowHeight;
        }
    }
}