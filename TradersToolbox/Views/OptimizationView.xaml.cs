using DevExpress.Xpf.Charts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TradersToolbox.Core;
using TradersToolbox.Core.Serializable;

namespace TradersToolbox.Views
{
    /// <summary>
    /// Interaction logic for OptimizationView.xaml
    /// </summary>
    public partial class OptimizationView : UserControl
    {
        public OptimizationView()
        {
            InitializeComponent();

            svEntrySig.SetMode(false);
            svExitSig.SetMode(false);
            svStdExits.SetMode(false);
        }

        private void WebBrowser_TargetUpdated(object sender, DataTransferEventArgs e)
        {
            if (webBrowser.DataContext is string html)
            {
                webBrowser.NavigateToString(html);
            }
        }

        private void SimpleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                object X = webBrowser.InvokeScript("eval", new object[] { "selCopyX" });
                object Y = webBrowser.InvokeScript("eval", new object[] { "selCopyY" });
                if (X == null || Y == null)
                    throw new Exception("X or Y is equal to null");
                double pointSelectionX = Convert.ToDouble(X);
                double pointSelectionY = Convert.ToDouble(Y);

                webBrowser.Tag = new Point(pointSelectionX, pointSelectionY);
            }
            catch (Exception ex)
            {
                Logger.Current.Trace(ex, "Sensitivity - strategy generator: Can't read input values!");
                webBrowser.Tag = new Point(-2222222222, -2222222222);
            }
        }

        private void SignalsView_TargetUpdated(object sender, DataTransferEventArgs e)
        {
            if (sender is SignalsView sv && sv.listView.SelectedItem is object o)
            {
                svEntrySig.listView.SelectedItem = null;        // refresh selection to reload signal parameters to display updated initial point
                svEntrySig.listView.SelectedItem = o;
            }
        }

        private void WebBrowser_Navigated(object sender, NavigationEventArgs e)
        {
            ConfigurateWebBrowser();
        }

        void ConfigurateWebBrowser()
        {
            // ActiveX webBrowser requires SHDocVw.dll reference to prevent drop links)
            var serviceProvider = (IServiceProvider)webBrowser.Document;
            if (serviceProvider != null)
            {
                Guid serviceGuid = new Guid("0002DF05-0000-0000-C000-000000000046");
                Guid iid = typeof(SHDocVw.WebBrowser).GUID;
                var webBrowserPtr = (SHDocVw.WebBrowser)serviceProvider.QueryService(ref serviceGuid, ref iid);
                if (webBrowserPtr != null)
                {
                    webBrowserPtr.RegisterAsDropTarget = false;
#if !DEBUG
                    webBrowserPtr.Silent = true;        //ScriptErrorsSuppressed = Silent mode
#endif
                }
            }
        }

        /*private void chart3d_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            int i = 0;
        }

        private void Chart3d_MouseMove(object sender, MouseEventArgs e)
        {
            var chartControl = sender as Chart3DControl;
            var hitInfo = chartControl.CalcHitInfo(e.GetPosition(chartControl));
            if (hitInfo.InSeries)
            {
                var point = hitInfo.SeriesPoint;

                string s = "";
                if (point != null)
                    s += $"Point ({point.ActualXArgument}, {point.ActualYArgument})";

                if (hitInfo.AdditionalItem is SurfacePoint p)
                {
                    s += $"   Additional Point: ({p.X}, {p.Y}, {p.Z})";

                    //chart3d.ActualContentTransform.Transform(new System.Windows.Media.Media3D.Point3D(p.X,p.Y,p.Z))

                    //chart3d.ShowCrosshair()
                }
                else if (hitInfo.AdditionalItem != null)
                    s += "   " + hitInfo.AdditionalItem.ToString();

                gbOptions.Header = s; // $"{point.NumericXArgument:F2}, {point.NumericYArgument:F2}";
            }
        }*/


        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("6d5140c1-7436-11ce-8034-00aa006009fa")]
        internal interface IServiceProvider
        {
            [return: MarshalAs(UnmanagedType.IUnknown)]
            object QueryService(ref Guid guidService, ref Guid riid);
        }
    }
}
