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
using System.Windows.Navigation;
using System.Windows.Shapes;
using DevExpress.Xpf.Core;

namespace TradersToolbox.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ThemedWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ThemedWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //Size it to fit the current screen
            SizeToFit();

            //Move the window at least partially into view
            MoveIntoView();

            if (Properties.Settings.Default.MainWindowState > 0)
                WindowState = WindowState.Maximized;
        }

        public void SizeToFit()
        {
            if (Height > SystemParameters.VirtualScreenHeight)
                Height = SystemParameters.VirtualScreenHeight;

            if (Width > SystemParameters.VirtualScreenWidth)
                Width = SystemParameters.VirtualScreenWidth;
        }

        public void MoveIntoView()
        {
            if (Top + Height / 2 > SystemParameters.VirtualScreenHeight)
                Top = SystemParameters.VirtualScreenHeight - Height;

            if (Left + Width / 2 > SystemParameters.VirtualScreenWidth)
                Left = SystemParameters.VirtualScreenWidth - Width;

            if (Top < 0)
                Top = 0;

            if (Left < 0)
                Left = 0;
        }

        private void ThemedWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.MainWindowState = WindowState == WindowState.Maximized ? 1 : 0;
        }
    }
}
