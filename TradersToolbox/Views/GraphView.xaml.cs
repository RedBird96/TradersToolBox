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
using TTWinForms;
using TradersToolbox.ViewModels;
using DevExpress.Xpf.Core;

namespace TradersToolbox.Views
{
    /// <summary>
    /// Interaction logic for GraphView.xaml
    /// </summary>
    public partial class GraphView : System.Windows.Controls.UserControl
    {
        public GraphView()
        {
            InitializeComponent();

            ThemeManager.ApplicationThemeChanged += ThemeManager_ApplicationThemeChanged;
        }

        ~GraphView()
        {
            ThemeManager.ApplicationThemeChanged -= ThemeManager_ApplicationThemeChanged;
        }

        private void ThemeManager_ApplicationThemeChanged(DependencyObject sender, ThemeChangedRoutedEventArgs e)
        {
            var host = mainGrid.Children[0] as System.Windows.Forms.Integration.WindowsFormsHost;
            (host.Child as IThemedGraph).UpdateTheme(e.ThemeName);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is IGraphViewModelBase vm)
            {
                // Create the interop host control.
                System.Windows.Forms.Integration.WindowsFormsHost host =
                    new System.Windows.Forms.Integration.WindowsFormsHost()
                    {
                        Child = vm.Graph as System.Windows.Forms.Control
                    };

                // Add the interop host control to the Grid
                // control's collection of child controls.
                mainGrid.Children.Add(host);

                // apply initial theme
                ThemeManager_ApplicationThemeChanged(null, new ThemeChangedRoutedEventArgs(ThemeManager.ActualApplicationThemeName));
            }
        }
    }
}
