using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
using NLog.LayoutRenderers;
using TradersToolbox.ViewModels;

namespace TradersToolbox
{
    /// <summary>
    /// Interaction logic for ProxySettingsWindow.xaml
    /// </summary>
    public partial class ProxySettingsWindow : ThemedWindow
    {
        public bool IsOk;

        public ProxySettingsWindow()
        {
            InitializeComponent();
        }

        private void ThemedWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (HttpClientHelper.IsUsingProxy())
            {
                IWebProxy proxy = WebRequest.GetSystemWebProxy();
                Uri uri = proxy.GetProxy(new Uri(MainWindowViewModel.hostPath));
                tbAddress.Text = uri.Host;
                tbPort.Text = uri.Port.ToString();
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            IsOk = true;
            Close();
        }
    }
}
