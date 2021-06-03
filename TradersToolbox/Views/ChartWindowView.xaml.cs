using DevExpress.Mvvm;
using DevExpress.Utils.CommonDialogs.Internal;
using DevExpress.Xpf.Bars;
using DevExpress.Xpf.Charts.Themes;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
using TradersToolbox.ViewModels;
using Path = System.IO.Path;

namespace TradersToolbox.Views
{
    /// <summary>
    /// Interaction logic for ChartWindowView.xaml
    /// </summary>
    public partial class ChartWindowView : UserControl
    {
        public ChartWindowView()
        {
            InitializeComponent();

            Messenger.Default.Register<ChartWindowViewModel>(this, (action) => ReceiveDoSomethingMessage(action));

        }

        private void itemClick(object sender, ItemClickEventArgs e)
        {
            string filename;
            if ((filename = getFileName()) != "")
            { 
                int width = Convert.ToInt32(this.ActualWidth);
                int height = Convert.ToInt32(this.ActualHeight);
                var renderTargetBitmap = new RenderTargetBitmap(width, height, 96, 96,PixelFormats.Pbgra32);

                renderTargetBitmap.Render(this);

                var pngImage = new PngBitmapEncoder();
                pngImage.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

                using (var fileStream = File.Create(filename))
                {
                    pngImage.Save(fileStream);
                }
            }
        }

        private string getFileName()
        {
            string strFilename = "";
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.InitialDirectory = Directory.GetCurrentDirectory();
            saveFileDialog1.Title = "Save PNG Files";
            saveFileDialog1.DefaultExt = "png";
            saveFileDialog1.Filter = "PNG files (*.png)|*.png";
            saveFileDialog1.FilterIndex = 2;
            saveFileDialog1.RestoreDirectory = true;
            if (saveFileDialog1.ShowDialog() == true)
            {
                strFilename = saveFileDialog1.FileName;
            }

            return strFilename;
        }

        private void ReceiveDoSomethingMessage(ChartWindowViewModel view)
        {
            ChartWindowViewModel vi = (ChartWindowViewModel)DataContext;
            if (vi != view)
                return;
            TypeConverter tc = new ColorConverter();
            Color red2 = (Color)tc.ConvertFromString(view.SelectedlinkDataItem.LinkColorName);
            LinkeChart.Background = new SolidColorBrush(red2);
            this.ChartOpenOrders.IsChecked = view.OpenOrderVisible;
        }
    }
}
