using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Json;
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
    /// Interaction logic for TradeStationLogInWindow.xaml
    /// </summary>
    public partial class TradeStationLogInWindow : ThemedWindow
    {
        public const string TSClientId_code = "X3TIveR11fwim4WM4mtjs+CMPwwXOHeVw5u52GfbPpu+PNXkH8neezTyJGkZWqaQpMINfSSmg/bQIJhFtCxLwux4SE3QKf4oTgOjwI/JLI8JUrFGXV8ekpxyO2Q/4L/5jG/X1ZGlCR1t6UMDO0DuWA==";
        public const string TSClientSecret_code = "bm2MlZkmIQwQpKTDuec6T0j0YRj2EXQuCeYIVGC98VmqgCZHrr/5aaHCyoVsb9sMkI39FhWzsKPb2gJ909yIH3YvkjWHLOjvq8m3RpDjHhuzlXH/1voRGYUL9DEr6cgVD2jBnrdez44guanW3GQt6g==";
        public const string LocalAppName = "Trader'sToolbox";
        public const string TSAuthorizationURL1 = "https://api.tradestation.com/v2/authorize";
        public const string TSAuthorizationURL2 = "https://api.tradestation.com/v2/security/authorize";
        public const string RedirectUrl = "data:,TSAuthCode";
        readonly string TSStartPage;

        public string AccessToken;
        public string RefreshToken;
        public string UserId;

        public TradeStationLogInWindow()
        {
            InitializeComponent();

            string TSClientId = Encryption.AESThenHMAC.SimpleDecryptWithPassword(TSClientId_code, LocalAppName);

            TSStartPage = $"{TSAuthorizationURL1}/?redirect_uri={RedirectUrl}&client_id={TSClientId}&response_type=code";
        }

        private void ThemedWindow_Loaded(object sender, RoutedEventArgs e)
        {
            webBrowser.Navigate(TSStartPage);
        }

        private async void webBrowser_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            // get auth code and access token
            if (e.Uri.OriginalString.StartsWith(RedirectUrl))
            {
                webBrowser.Visibility = Visibility.Collapsed;
                try
                {
                    var t1 = Encryption.AESThenHMAC.SimpleDecryptWithPasswordAsync(TSClientId_code, LocalAppName);
                    var t2 = Encryption.AESThenHMAC.SimpleDecryptWithPasswordAsync(TSClientSecret_code, LocalAppName);

                    string AuthorizationCode = e.Uri.OriginalString.Substring(1 + e.Uri.OriginalString.IndexOf('='));
                    string TSClientId = await t1;
                    string TSClientSecret = await t2;

                    HttpClient httpClient = new HttpClient();
                    HttpContent x = new StringContent("grant_type=authorization_code&" +
                        $"client_id={TSClientId}&redirect_uri={RedirectUrl}&client_secret={TSClientSecret}&code={AuthorizationCode}&response_type=token",
                        Encoding.UTF8, "application/x-www-form-urlencoded");

                    var response = await httpClient.PostAsync(TSAuthorizationURL2, x);
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(TradeStationAPI.Model.UserCredentials));
                        if (serializer.ReadObject(await response.Content.ReadAsStreamAsync()) is TradeStationAPI.Model.UserCredentials user)
                        {
                            //LogLabel.Text += "\nSuccessful";
                            AccessToken = user.access_token;
                            RefreshToken = user.refresh_token;
                            UserId = user.userid;
                            Close();
                        }
                        else
                            throw new Exception("Can't read access token");
                    }
                    else
                    {
                        throw new Exception(await response.Content.ReadAsStringAsync());
                    }
                }
                catch (Exception ex)
                {
                   // LogLabel.Text += ex.Message;
                    webBrowser.Navigate("about:blank");
                    webBrowser.Refresh();
                    webBrowser.Navigate(TSStartPage);
                    webBrowser.Visibility = Visibility.Visible;
                    MessageBox.Show("Logging in - Unsuccessful\n\n" + ex.Message);
                }
            }
            else
            {
                
                //foreach (HtmlElement div in webBrowser.Document.GetElementsByTagName("div"))
                //{
                //    if (div.GetAttribute("className") == "header")
                //        div.Style = "{background-color: rgb(38, 137, 239);}";
                //}
            }
        }
    }
}
