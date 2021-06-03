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
    /// Interaction logic for ActivationWindow.xaml
    /// </summary>
    public partial class ActivationWindow : ThemedWindow
    {
        public ActivationWindow()
        {
            InitializeComponent();

            RequestCodeTB.Text = Security.HardwareID;
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start("mailto:" + e.Uri.ToString());
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(emailHyperlink.NavigateUri.ToString());
        }

        private void ThemedWindow_Loaded(object sender, RoutedEventArgs e)
        {
            SplashScreenManager.CloseAll();
        }
    }
}
