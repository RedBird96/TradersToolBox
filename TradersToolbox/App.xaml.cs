//#define LOGGER_INTERNAL_TEST

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Net;
using TradersToolbox.Core;
using System.IO;
using DevExpress.Xpf.Core;
using DevExpress.Mvvm;
using TradersToolbox.ViewModels;
using TradersToolbox.Views;
using SciChart.Charting.Visuals;

namespace TradersToolbox
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly string appGuid = "3472E0E1-57B3-4874-8B3F-39834B6EA12C";
        protected Mutex mutex;

        public App()
        {
#if LOGGER_INTERNAL_TEST
            NLog.LogManager.ThrowExceptions = true;
            NLog.Common.InternalLogger.LogLevel = NLog.LogLevel.Debug;
            NLog.Common.InternalLogger.LogFile = "nlog.log";
#endif

            // Splash screen initialization
            var splashScreenViewModel = new DXSplashScreenViewModel() {
                Title = "Trader's Toolbox",
                Logo = new Uri("../../Resources/TradersToolbox.png", UriKind.Relative),
                Subtitle = "Build your personal strategy",
                Copyright = "Copyright © 2016-2021 Build Alpha." + Environment.NewLine + "All rights reserved."
            };
#if LOGGED_IN
            SplashScreenManager.Create(() => new AppSplashScreen(), splashScreenViewModel).ShowOnStartup();
#else
            SplashScreenManager.Create(() =>
            {
                var sp = new AppSplashScreen();
                sp.Closing += AppSplash_Closing;
                return sp;
            },
            splashScreenViewModel).ShowOnStartup(false);
#endif
        }

#if !LOGGED_IN
        private void AppSplash_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (string.IsNullOrEmpty(MainWindowViewModel.serverAnswerString))
            {
                Dispatcher.Invoke(() =>
                {
                    Shutdown();
                });
            }
            else
            {
                bool MainWindowNotExists = false;

                Dispatcher.Invoke(() =>
                {
                    MainWindowNotExists = (MainWindow == null);
                });

                if (MainWindowNotExists)
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        if (MainWindowViewModel.serverAnswerString.Equals(Security.ActivationCode))
                            MainWindow = new MainWindow();
                        else  // incorrect key, new user
                            MainWindow = new ActivationWindow();
                        MainWindow.Show();
                    });
                    e.Cancel = true;
                }
            }
        }
#endif

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            mutex = new Mutex(true, @"Global\" + appGuid, out bool createdNew);
            if (!createdNew)
            {
                MessageBox.Show("Instance of Trader's Toolbox is already running", "Trader's Toolbox");
                Shutdown(-1);
                return;
            }

            // global unhandled exception handlers
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;

#if DEBUG
            // Check assemblies loading
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            Logger.Current.Trace("{0} assemblies have been loaded", assemblies.Length);
            foreach (var a in assemblies)
                Logger.Current.Trace(a.FullName);
            AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;
#endif

            // Check settings
            CheckSettings();

            // for SSL access
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            // load theme
            ApplicationThemeHelper.UpdateApplicationThemeName();


#if LOGGED_IN   // run without license for developer mode
            MainWindowViewModel.serverAnswerString = Security.ActivationCode;
            StartupUri = new Uri("Views/MainWindow.xaml", UriKind.Relative);
#else
            // server-based authorization (in splash screen window)
#endif

            //SciChart License init async

            // Alex -->>
            //SciChartSurface.SetRuntimeLicenseKey("AHZKYdDma13TxANzfqyXvO2ftUsyo5RSELgPBpI0LH/udkrnvNuKrFayz2YdjPOELyVhDKNGWCN2Mf2s0ke8r1toK12ARaaXvjQpCt+OHoYBk0YsWldAos+Q+GoXINPH0KRu+yjre+OWE4yyM5XQ4TfVKRUsa0h94SoNoLBEZr4mE4HvbP18KGltgHPUFelzkRaY1Pahb0RRiT4NYq7B/eRjNAfLlmpNby1agOQwL2QUlG4gOteIbVyHqmEcnj/2CbO5HP0cL3osBk/akgwqPqKdbqMJfMmtjYxU5J9xz0uP2o9/Oteq7XZoLJWcqQfA8uf3h9BzKP0wv4QIE1J+V6smOZ3hgbyFLT5CVrF8hECnLT7QUe83evFxGADiGx4NjWNWJ4HDVOSXyw6A5XxARBj4zrxB8HGIaJCYmmQsr6TSp1hfKGdAZqURc6Qnusokm2N6kY12+dLjqUmJs77+X+2D5T0uOwNfR4YtQ7rhv2uxE567Ja99P1E1014sowA/zUGktSNkbdkvHosxsNIKIXhav1q/Logh3BS1mQAd8qLnPnq7Ao+JMxUS4v83wJB3l36KWBaPFU5XwivCL02pgvA8v7EDYlfEjbC+SDzp/X9VhNKYZ+QQjhQPYkrQ9lUB4liNaoImve7BcqMY67VimthdlsLlK4w9uTl26A==");

            // Dimatsi -->>
            //SciChartSurface.SetRuntimeLicenseKey("cgzh+nGqXY2qRIQanInCiWPiAU+KOo8OPLGNNSb2rTTga7S8gPbWVmwE4mA0wVdwJ0m8vC68+8I+mpBeQlWZPDV4Qy1De4Aw+NS/u5dswCtv/jp5JYJPIeYYMw/mLuhMLB3fTlyTVrpG+zhfapRbxfvouKNQ7r4u531Sj2Yvsk1QaZ2+WtL0yzS7MuTG2d07GyVFaxS2bqQBTLNdcZhIq49EB3o+9E4IlWf+ccmQsUtu1dbMOlMcdaFTtPYUyDvoSYMLaE9r0JCE8EhNOpm1e30FBsfsts0pPtRhTos9AkVNk/JWp/56SL32scorVuOrDa9YzL2kK5jieF62cM9T5VYZsE+suffmplKBtY2Ww++ou9s1KqOT2lAsRYhxSGnwuHojQJNXd/1cmWHrxr8d1aDxpyIsUMPgW2xlt8tO9vNuqlysxNgJaysNfFytLJPB2e+SFvxM1t3QrC/ZQY53yuQjqdRTg2ISkKggZjnaG4hhdIGmFKoNNodWlHMmFFepDe4aNZ82ws3N");

            //Nikita -->>
            SciChartSurface.SetRuntimeLicenseKey(@"qwQxphkzujDJdgugcZCMmp3htnZTguNHAJTDuNyfOQIAQ/r2HDjWI2EEUdGrANFdoB8kYWiyGYQzIcS9WIvBHQbDb08zdbekWw0ydvanuJCpCmamY2rWfhSsaHEz3PUfWM8vG/7UFUJfVLQOp6thQDhffdTzjEYY4+wjXRfOA8eeKqG29rswq7k+2i0PF7lAeGjhyy+Q9kz/ecTqzt1m10iJYQS08kiX1zjQAajJpqxIukBR0dAAMbxh3Wd80MAxRu/AO4pTDTIfgTPfDh5ci/QJ5dTmFPv9rplWu9cAI042+UBFCTdUPF4BpQGcJ0NHjwNBWj6x1QfjGwBSotdtY1fpKz2UBVnB56NZHWH5oQeBPi4vW+DjHC32SAS5O1kILNPZAekssCRW6GKXlF2lyFIhlHRWtKBFdakTjLyfZtvsCPl8OxCakjyGbl8SIoWZgkRPPJ15tbCUEreeRZwugNRlAi/vt0j0F6Oa/4TDUvDLdAEDpJnrVrJTllc3sSIrjcXgPyIBhA66CDxlJb7WmyXz7ELUWWEMLm98DM3S0b5aiETgUdXwlmpfNfonAtnHj9RjQA==");
        }


        private void CurrentDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            Logger.Current.Trace("CurrentDomain AssemblyLoad: {0}", args.LoadedAssembly.FullName);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            string desc = "CurrentDomain UnhandledException";
            Logger.Current.Error(e.ExceptionObject as Exception, desc);
            if (e.ExceptionObject is Exception ex)
                MessageBox.Show(ex.Message + Environment.NewLine + ex.InnerException?.Message, desc);
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            string desc = "Application DispatcherUnhandledException";
            Logger.Current.Error(e.Exception, desc);
            if (e.Exception != null)
                MessageBox.Show(e.Exception.Message + Environment.NewLine + e.Exception.InnerException?.Message, desc);
        }


        private void Application_Exit(object sender, ExitEventArgs e)
        {
            // Flush async target's buffers
            Logger.Shutdown();
            // Finalize mutex
            mutex.Close();
            mutex = null;
        }

        private static bool CheckSettings()
        {
            var isReset = false;
            string filename = string.Empty;

            try
            {
                var UserConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
                filename = UserConfig.FilePath;
                //"userSettings" should be replaced here with the expected label regarding your configuration file
                var userSettingsGroup = UserConfig.SectionGroups.Get("userSettings");
                if (userSettingsGroup != null   /* && userSettingsGroup.IsDeclared == true*/)
                    filename = null; // No Exception - all is good we should not delete config in Finally block
            }
            catch (ConfigurationErrorsException ex)
            {
                Logger.Current.Warn(ex, "CheckSettings: Can't read configuration file!");
                if (!string.IsNullOrEmpty(ex.Filename))
                {
                    filename = ex.Filename;
                }
                else
                {
                    if (ex.InnerException is ConfigurationErrorsException innerEx && !string.IsNullOrEmpty(innerEx.Filename))
                    {
                        filename = innerEx.Filename;
                    }
                }
            }
            catch (ArgumentException)
            {
                Logger.Current.Warn("CheckSettings: Argument exception");
            }
            catch (SystemException)
            {
                Logger.Current.Warn("CheckSettings: System exception");
            }
            finally
            {
                if (!string.IsNullOrEmpty(filename))
                {
                    if (File.Exists(filename))
                    {
                        var fileInfo = new FileInfo(filename);
                        var watcher = new FileSystemWatcher(fileInfo.Directory.FullName, fileInfo.Name);
                        File.Delete(filename);
                        isReset = true;
                        if (File.Exists(filename))
                        {
                            watcher.WaitForChanged(WatcherChangeTypes.Deleted);
                        }

                        try
                        {
                            TradersToolbox.Properties.Settings.Default.Upgrade();
                            Logger.Current.Info("CheckSettings: app settings file has been reseted");
                        }
                        catch (SystemException ex)
                        {
                            Logger.Current.Warn(ex, "CheckSettings: can't update app settings");
                        }
                    }
                }
            }
            return isReset;
        }
    }
}
