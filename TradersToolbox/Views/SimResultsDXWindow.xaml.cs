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
using DevExpress.Xpf.Core;


namespace TradersToolbox.Views
{
    /// <summary>
    /// Interaction logic for SimResultsDXWindow.xaml
    /// </summary>
    public partial class SimResultsDXWindow : ThemedWindow
    {
        public SimResultsDXWindow()
        {
            InitializeComponent();
        }

        private void ThemedWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(DXMessageBox.Show(this, "Do you really want to close the results window?", "Results window closing...", MessageBoxButton.YesNo,
                MessageBoxImage.None, MessageBoxResult.No, MessageBoxOptions.None, FloatingMode.Adorner, true) == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
        }
    }
}
