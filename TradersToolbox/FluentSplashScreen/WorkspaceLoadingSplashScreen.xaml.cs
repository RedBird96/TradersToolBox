using DevExpress.Xpf.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TradersToolbox
{
    /// <summary>
    /// Interaction logic for WorkspaceLoadingSplashScreen.xaml
    /// </summary>
    public partial class WorkspaceLoadingSplashScreen : SplashScreenWindow, ISplashScreen
    {
        public WorkspaceLoadingSplashScreen()
        {
            InitializeComponent();
        }

        public void CloseSplashScreen()
        {
            Close();
        }

        public void Progress(double value)
        {
        }

        public void SetProgressState(bool isIndeterminate)
        {
        }
    }
}
