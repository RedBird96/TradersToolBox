using System;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm;
using System.ComponentModel;
using TTWinForms;
using System.Text;
using TradersToolbox.Core;
using DevExpress.Mvvm.POCO;
using System.Linq;

namespace TradersToolbox.ViewModels
{
    [POCOViewModel]
    public class GraphIntradayViewModel : IDocumentContent, IGraphViewModelBase
    {
        public object Graph { get; set; }
        public SimStrategy strategy;

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

        protected IMessageBoxService MessageBoxService { get { return this.GetService<IMessageBoxService>(/*ServiceSearchMode.PreferParents*/); } }

        #region IDocumentContent
        public object Title { get; set; } = "Chart";
        public IDocumentOwner DocumentOwner { get; set; }

        public void OnClose(CancelEventArgs e)
        {
            GraphIntraday gr = Graph as GraphIntraday;
            if (gr.selectedIndex < 0) return;

            StringBuilder str = new StringBuilder("Do you want to save selected parameters for further processing?");
            str.AppendLine(Environment.NewLine);

            try
            {
                str.AppendFormat("Strategy name: {0}{1}{1}", strategy.Name, Environment.NewLine);
                str.AppendFormat("Max PL: {0}{1}", gr.Args[gr.selectedIndex][0], Environment.NewLine);
                str.AppendFormat("Min PL: {0}{1}", gr.Args[gr.selectedIndex][1], Environment.NewLine);
                str.AppendFormat("Start: {0}{1}", gr.Args[gr.selectedIndex][2], Environment.NewLine);
                str.AppendFormat("End: {0}{1}", gr.Args[gr.selectedIndex][3], Environment.NewLine);
                str.AppendFormat("Max Trades: {0}{1}", gr.Args[gr.selectedIndex][4], Environment.NewLine);
            }
            catch
            {
                MessageBoxService.Show("Error! Unable to find strategy", "Intraday checks");
                return;
            }

            var res = MessageBoxService.Show(str.ToString(), "Intraday checks", System.Windows.MessageBoxButton.YesNoCancel);
            if (res == System.Windows.MessageBoxResult.Cancel)
                e.Cancel = true;
            else if (res == System.Windows.MessageBoxResult.Yes)
                strategy.intradayTestResults = gr.Args[gr.selectedIndex].ToList();
        }

        public void OnDestroy()
        {
        }
        #endregion

        public static GraphIntradayViewModel Create()
        {
            return ViewModelSource.Create(() => new GraphIntradayViewModel());
        }

        protected GraphIntradayViewModel()
        {
            WindowWidth = Properties.Settings.Default.GraphWindowWidth;
            WindowHeight = Properties.Settings.Default.GraphWindowHeight;
        }
    }
}