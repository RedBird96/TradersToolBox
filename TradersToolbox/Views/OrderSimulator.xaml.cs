using DevExpress.Mvvm;
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
using TradersToolbox.ViewModels;
using TradeStationAPI.Model;

namespace TradersToolbox.Views
{
    /// <summary>
    /// Interaction logic for OrderSimulator.xaml
    /// </summary>
    public partial class OrderSimulator : ThemedWindow
    {
        public OrderFormViewModel OrderFormVM = OrderFormViewModel.Create();

        public OrderSimulator()
        {
            InitializeComponent();
            this.btnSell.Click += btnSell_Click;
            this.btnBuy.Click += btnBuy_Click;
            this.btnBuy.IsChecked = true;
            this.DataContext = OrderFormVM;

            this.btnMarPriceAmount.Content = "USD";
            this.btnLimitPriceAmount.Content = "USD";
            this.btnStopPriceAmount.Content = "USD";

            this.labelLotTotal.Content = "0";
        }

        private void btnBuy_Click(object sender, RoutedEventArgs e)
        {
            this.btnBuy.IsChecked = true;
            this.btnSell.IsChecked = false;
            this.PlaceOrder.Content = TradersToolbox.Properties.Resources.PLACE;
        }

        private void btnSell_Click(object sender, RoutedEventArgs e)
        {
            this.btnSell.IsChecked = true;
            this.btnBuy.IsChecked = false;
            this.PlaceOrder.Content = TradersToolbox.Properties.Resources.PLACE;
        }
        private void NumericOnly(System.Object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            var text = (DevExpress.Xpf.Editors.TextEdit)sender;
            if (text.Text.Length != 0)
            {
                double.TryParse(text.Text, out _);
                if (e.Text == "." && text.Text.Contains("."))
                {
                    e.Handled = true;
                    return;
                }
                if (text.Text.Length > 10)
                {
                    e.Handled = true;
                    return;
                }
            }

            e.Handled = IsTextNumeric(e.Text);
        }
        private static bool IsTextNumeric(string str)
        {
            System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex("[^0-9.]+");
            return reg.IsMatch(str);
        }
        private void Amount_TextChanged(object sender, RoutedEventArgs e)
        {
            var text = (DevExpress.Xpf.Editors.TextEdit)sender;
            if (text.Text.Length == 0)
            {
                this.labelTotal.Content = "$0.00";
                this.labelLotTotal.Content = "0";
            }
            else
            {
                if (OrderFormVM.marketOrderTab == 1)
                {
                    if (this.btnMarPriceAmount.Content.ToString() == "USD")
                    {
                        this.labelTotal.Content = "$" + text.Text;
                        this.labelLotTotal.Content = ConvertUSDtoLOT(int.Parse(text.Text));
                    }
                    else
                    {
                        this.labelLotTotal.Content = text.Text;
                        this.labelTotal.Content = "$" + ConvertLottoUSD(int.Parse(text.Text));
                    }
                }
                else if (OrderFormVM.limitOrderTab == 1)
                {
                    if (this.btnLimitPriceAmount.Content.ToString() == "USD")
                    {
                        this.labelTotal.Content = "$" + text.Text;
                        this.labelLotTotal.Content = ConvertUSDtoLOT(int.Parse(text.Text));
                    }
                    else
                    {
                        this.labelLotTotal.Content = text.Text;
                        this.labelTotal.Content = "$" + ConvertLottoUSD(int.Parse(text.Text));
                    }
                }
                else if (OrderFormVM.stopOrderTab == 1)
                {
                    if (this.btnStopPriceAmount.Content.ToString() == "USD")
                    {
                        this.labelTotal.Content = "$" + text.Text;
                        this.labelLotTotal.Content = ConvertUSDtoLOT(int.Parse(text.Text));
                    }
                    else
                    {
                        this.labelLotTotal.Content = text.Text;
                        this.labelTotal.Content = "$" + ConvertLottoUSD(int.Parse(text.Text));
                    }
                }
            }
            if (this.labelLotTotal.Content != null)
            {
                OrderFormVM.labelAmount = int.Parse(labelLotTotal.Content.ToString());
            }
        }

        private void ClickOrderButton(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void ThemedWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private void SimpleButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            if (btn.Content.ToString() == "USD")
            {
                btn.Content = "LOT";
                if (this.editMarAmountName.Text == "" || this.editMarAmountName.Text == null)
                    this.editMarAmountName.Text = "0";
                this.editMarAmountName.Text = ConvertUSDtoLOT(int.Parse(this.editMarAmountName.Text)).ToString();
            }
            else
            {
                btn.Content = "USD";
                if (this.editMarAmountName.Text == "" || this.editMarAmountName.Text == null)
                    this.editMarAmountName.Text = "0";
                this.editMarAmountName.Text = ConvertLottoUSD(int.Parse(this.editMarAmountName.Text)).ToString();
            }
        }

        private void btnLimitPriceAmount_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            if (btn.Content.ToString() == "USD")
            {
                btn.Content = "LOT";
                if (this.editLimitAmountName.Text == "" || this.editLimitAmountName.Text == null)
                    this.editLimitAmountName.Text = "0";
                this.editLimitAmountName.Text = ConvertUSDtoLOT(int.Parse(this.editLimitAmountName.Text)).ToString();
            }
            else
            {
                btn.Content = "USD";
                if (this.editLimitAmountName.Text == "" || this.editLimitAmountName.Text == null)
                    this.editLimitAmountName.Text = "0";
                this.editLimitAmountName.Text = ConvertLottoUSD(int.Parse(this.editLimitAmountName.Text)).ToString();
            }
        }

        private void btnStopPriceAmount_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            if (btn.Content.ToString() == "USD")
            {
                btn.Content = "LOT";
                if (this.editStopAmountName.Text == "" || this.editStopAmountName.Text == null)
                    this.editStopAmountName.Text = "0";
                this.editStopAmountName.Text = ConvertUSDtoLOT(int.Parse(this.editStopAmountName.Text)).ToString();
            }
            else
            {
                btn.Content = "USD";
                if (editStopAmountName.Text == "" || editStopAmountName.Text == null)
                    editStopAmountName.Text = "0";
                editStopAmountName.Text = ConvertLottoUSD(int.Parse(editStopAmountName.Text)).ToString();
            }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (OrderFormVM.marketOrderTab == 1)
            {
                if (editMarAmountName.Text == null || editMarAmountName.Text == "")
                    editMarAmountName.Text = "0";

                if (btnMarPriceAmount.Content.ToString() == "USD")
                {
                    labelTotal.Content = "$" + editMarAmountName.Text;
                    string c = this.editMarAmountName.Text;
                    int ab = int.Parse(c);
                    labelLotTotal.Content = ConvertUSDtoLOT(ab);
                }
                else
                {
                    labelLotTotal.Content = editMarAmountName.Text;
                    labelTotal.Content = "$" + ConvertLottoUSD(int.Parse(editMarAmountName.Text));
                }
            }
            else if (OrderFormVM.limitOrderTab == 1)
            {
                if (editLimitAmountName.Text == null || editLimitAmountName.Text == "")
                    editLimitAmountName.Text = "0";

                if (btnLimitPriceAmount.Content.ToString() == "USD")
                {
                    labelTotal.Content = "$" + editLimitAmountName.Text;
                    labelLotTotal.Content = ConvertUSDtoLOT(int.Parse(editLimitAmountName.Text));
                }
                else
                {
                    labelLotTotal.Content = editLimitAmountName.Text;
                    labelTotal.Content = "$" + ConvertLottoUSD(int.Parse(editLimitAmountName.Text));
                }
            }
            else if (OrderFormVM.stopOrderTab == 1)
            {
                if (editStopAmountName.Text == null || this.editStopAmountName.Text == "")
                    editStopAmountName.Text = "0";

                if (btnStopPriceAmount.Content.ToString() == "USD")
                {
                    labelTotal.Content = "$" + this.editStopAmountName.Text;
                    labelLotTotal.Content = ConvertUSDtoLOT(int.Parse(editStopAmountName.Text));
                }
                else
                {
                    labelLotTotal.Content = this.editStopAmountName.Text;
                    labelTotal.Content = "$" + ConvertLottoUSD(int.Parse(editStopAmountName.Text));
                }
            }
            if (this.labelLotTotal.Content != null)
                OrderFormVM.labelAmount = int.Parse(labelLotTotal.Content.ToString());
        }

        public int ConvertUSDtoLOT(int usdAmount)
        {
            int result = usdAmount;
            if (OrderFormVM.Data == null || OrderFormVM.Data.Count == 0 || cbName.Text == null || cbName.Text == "" || usdAmount == 0)
                return result;

            try
            {
                if (OrderFormVM.Data.ContainsKey(cbName.Text))
                {
                    var IData = OrderFormVM.Data[cbName.Text];
                    decimal averagePr = (IData.Ask.Value + IData.Bid.Value) / 2;
                    result = (int)Math.Round(usdAmount / averagePr);
                }
            }
            catch { }
            return result;
        }

        public double ConvertLottoUSD(int lotAmount)
        {
            double result = lotAmount;
            if (OrderFormVM.Data == null || OrderFormVM.Data.Count == 0 || cbName.Text == null || cbName.Text == "")
                return result;

            try
            {
                if (OrderFormVM.Data.ContainsKey(cbName.Text))
                {
                    var IData = OrderFormVM.Data[cbName.Text];
                    decimal averagePr = (IData.Ask.Value + IData.Bid.Value) / 2;
                    result = (double)Math.Round(lotAmount * averagePr, 2);
                }
            }
            catch { }
            return result;
        }
    }
}
