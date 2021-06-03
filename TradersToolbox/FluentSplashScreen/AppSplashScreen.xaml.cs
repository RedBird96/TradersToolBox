using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using DevExpress.Xpf.Core;
using TradersToolbox.Core;
using TradersToolbox.ViewModels;

namespace TradersToolbox
{
    /// <summary>
    /// Interaction logic for AppSplashScreen.xaml
    /// </summary>
    public partial class AppSplashScreen : SplashScreenWindow
    {
        readonly Timer timerCloseAbnormal;

        public AppSplashScreen()
        {
            InitializeComponent();

            timerCloseAbnormal = new Timer(5000)
            {
                AutoReset = false    // execute once
            };
            timerCloseAbnormal.Elapsed += Timer_Elapsed;
        }

#if LOGGED_IN
        private void SplashScreenWindow_ContentRendered(object sender, EventArgs e) { }
#else
        private async void SplashScreenWindow_ContentRendered(object sender, EventArgs e)
        {
            // Logging in
            PART_Status.Text = "Connecting to server...";

            await Task.Run(async () =>
            {
                HttpClient client = MainWindowViewModel.HttpClient;

                try
                {
                    string str = await Signin(client);

                    if (string.IsNullOrEmpty(str))   //success
                    {
                        Dispatcher.Invoke(() =>
                        {
                            PART_Status.Text = "Logged in. Loading...";
                            Close();
                        });
                        return;
                    }
                    else
                    {
                        Dispatcher.Invoke(() => PART_Status.Text = str);
                    }
                }
                catch (HttpRequestException ex1)
                {
                    Dispatcher.Invoke(() => {
                        PART_Status.Text = "Unable to connect to remote server!";
                    });
                    Logger.Current.Warn(ex1, "Unable to connect to remote server!");

                    if (HttpClientHelper.IsUsingProxy())
                    { 
                        Dispatcher.Invoke(() =>
                        {
                            PART_Status.Text = "Proxy authentication required";
                        });
                        if(string.IsNullOrEmpty(Properties.Settings.Default.ProxyName))
                            Properties.Settings.Default.ProxyName = CredentialCache.DefaultNetworkCredentials?.UserName;
                        if (string.IsNullOrEmpty(Properties.Settings.Default.ProxyPassword))
                            Properties.Settings.Default.ProxyPassword = CredentialCache.DefaultNetworkCredentials?.Password;

                        bool proxyOK = false;
                        await Dispatcher.InvokeAsync(() =>
                        {
                            ProxySettingsWindow ps = new ProxySettingsWindow()
                            {
                                Title = "Proxy authentication required"
                            };
                            ps.ShowDialog();
                            proxyOK = ps.IsOk;
                        });
                        if (proxyOK)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                PART_Status.Text = "Connecting to Proxy...";
                            });
                            MainWindowViewModel.HttpClient = null;  //reset
                            client = MainWindowViewModel.HttpClient;
                            // try to download again
                            try
                            {
                                string str = await Signin(client);

                                if (string.IsNullOrEmpty(str))   //success
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        PART_Status.Text = "Logged in. Loading...";
                                        Close();
                                    });
                                    return;
                                }
                                else
                                {
                                    Dispatcher.Invoke(() => PART_Status.Text = str);
                                }
                            }
                            catch (WebException)
                            {
                                Dispatcher.Invoke(() => PART_Status.Text = "Unable to connect to proxy server!");
                            }
                            catch
                            {
                                Dispatcher.Invoke(() => PART_Status.Text = "Authentication error. Access denied!");
                            }
                        }
                    }
                }
                catch
                {
                    Dispatcher.Invoke(() => {
                        PART_Status.Text = "Authentication error. Access denied!";
                    });
                }
                timerCloseAbnormal.Start();
            });
        }
#endif

        // server-based authorization
        private async Task<string> Signin(HttpClient client)
        {
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;
            Random rand = new Random(723);

            string an = string.Empty;
            string valid = string.Empty;
            StringBuilder rsb = new StringBuilder();
            string hw = Security.HardwareID;
            {
                string rs = Security.GetHash(Guid.NewGuid().ToString());
                for (int i = 0; i < rs.Length; i++)
                    rsb.Append(rs[i] == '-' ? (char)(48 + rand.Next(10)) : rs[i]);
            }
            StringBuilder creq = new StringBuilder();
            for (int i = 0; i < hw.Length; i++)
                creq.Append((hw[i] + rsb[i]).ToString());
            string remd = Security.GetHash(Security.GetHash(creq.ToString()).Replace("-", "").ToLower()).Replace("-", "");

            string ans = await client.GetStringAsync(MainWindowViewModel.hostPath + "login.php?" + rsb.ToString() + remd);
            //ans = c.DownloadString("http://192.168.0.103/TT/login.php?" + rsb.ToString() + remd);

            if (ans.Length > 100)
            {   //parse server answer
                string b = "0.23.4.ms-metrix";
                string e = "//ms-metrix,extra,null";
                int bb = ans.IndexOf(b);
                if (bb >= 0)
                {
                    int ee = ans.IndexOf(e);
                    if (ee >= 0)
                    {
                        bb += b.Length;
                        an = ans.Substring(bb, ee - bb);
                    }
                }
                b = "valid_date_beg";
                e = "valid_date_end";
                bb = ans.IndexOf(b);
                if (bb >= 0)
                {
                    int ee = ans.IndexOf(e);
                    if (ee >= 0)
                    {
                        bb += b.Length;
                        valid = ans.Substring(bb, ee - bb).Trim();
                    }
                }
            }
            else return "Authentication error 1. Access denied!";

            if (an.Length == 64)
            {
                string kk = an.Substring(32);
                StringBuilder scc = new StringBuilder();
                string actCode = Security.ActivationCode;
                for (int i = 0; i < actCode.Length; i++)
                    scc.Append((actCode[i] + rsb[i]).ToString());
                string loc = Security.GetHash(Security.GetHash(scc.ToString()).Replace("-", "").ToLower()).Replace("-", "");
                int compRes = comparer.Compare(loc, kk);
                if (compRes > 0)
                    MainWindowViewModel.serverAnswerString = Security.HardwareID;
                else if (compRes < 0)
                    MainWindowViewModel.serverAnswerString = "AuthF" + rand.Next(10);
                else
                    MainWindowViewModel.serverAnswerString = actCode;
                if (DateTime.TryParseExact(valid, "d.M.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
                    MainWindowViewModel.serverAnswerDT = dt;
                
                return string.Empty;
            }
            else return "Authentication error 2. Access denied!";
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                Close();
            });
        }
    }
}

/*// Create a custom routed event by first registering a RoutedEventID
// This event uses the bubbling routing strategy
public static readonly RoutedEvent LogInEvent = EventManager.RegisterRoutedEvent(
    "LogIn", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(AppSplashScreen));

// Provide CLR accessors for the event
public event RoutedEventHandler LogIn
{
    add { AddHandler(LogInEvent, value); }
    remove { RemoveHandler(LogInEvent, value); }
}

// This method raises the Tap event
void RaiseLogInEvent()
{
    RoutedEventArgs newEventArgs = new RoutedEventArgs(AppSplashScreen.LogInEvent);
    RaiseEvent(newEventArgs);
}*/