using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using log4net;
using log4net.Core;
using log4net.Repository.Hierarchy;
using Microsoft.Extensions.DependencyInjection;
using NanoTwitchLeafs.Enums;
using NanoTwitchLeafs.Interfaces;
using NanoTwitchLeafs.Objects;
using NanoTwitchLeafs.Services;
using NanoTwitchLeafs.Windows;
using Newtonsoft.Json;
using Serilog.Events;

namespace NanoTwitchLeafs;

public static class Main
{
    private static readonly ILog Logger = LogManager.GetLogger(typeof(TwitchAuthService));
    private static object _logger;

    public static async void Run()
    {
        var serviceProvider = DependencyConfig.ConfigureServices();

        var settingsService = serviceProvider.GetService<ISettingsService>();
        // attach property change event to configuration object and its subsequent elements if any derived NotifyObject
        settingsService.CurrentSettings.AttachPropertyChanged((o, args) => settingsService.SaveSettings());
        
#if RELEASE
        Logger.Info($"Start Program - Version: {typeof(AppInfoWindow).Assembly.GetName().Version}");
        InstanceCheck();
#endif
#if BETA
        MessageBox.Show(Properties.Resources.Code_Main_MessageBox_Beta, Properties.Resources.Code_Main_MessageBox_BetaTitle);
        Logger.Info($"Start Program - Version: {typeof(AppInfoWindow).Assembly.GetName().Version} - BETA");
        InstanceCheck();
#endif
#if DEBUG
        Logger.Info($"Start Program - Version: {typeof(AppInfoWindow).Assembly.GetName().Version} - DEBUG");
#endif
        // Handle Terminating Exceptions
        AppDomain.CurrentDomain.UnhandledException += (o, args) =>
        {
            if (args.IsTerminating)
            {
                settingsService.CurrentSettings.AutoConnect = false;
                settingsService.SaveSettings();
                Logger.Info("Auto Connect disabled cause of Terminating Exception");
                MessageBox.Show(Properties.Resources.General_MessageBox_GeneralErrorCrash_Text, Properties.Resources.General_MessageBox_Error_Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                MessageBox.Show(Properties.Resources.General_MessageBox_GeneralError_Text, Properties.Resources.General_MessageBox_Error_Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            Logger.Error($"Exception terminated Program: {args.IsTerminating}");
            Logger.Error(args.ExceptionObject);
        };

        // Set Log Level
        if (settingsService.CurrentSettings.DebugEnabled)
        {
            ((Hierarchy)LogManager.GetRepository()).Root.Level = Level.Debug;
            ((Hierarchy)LogManager.GetRepository()).RaiseConfigurationChanged(EventArgs.Empty);
        }
        
        // Init Main Window
        var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
        
        // Check for Updates
        var updateService = serviceProvider.GetService<IUpdateService>();
        await updateService.CheckForUpdates();

        // Start Analytics Service
        var analyticsService = serviceProvider.GetService<IAnalyticsService>();
        analyticsService.SendPing(PingType.Start, "Hello World!");
        analyticsService.StartKeepAlive();
        
        // Load Service Credentials
        LoadServiceCredentials();
    }

    private static void LoadServiceCredentials()
    {
        Logger.Info("Load Service Credentials");
        if (File.Exists(Constants.SERVICE_CREDENTIALS_PATH))
        {
            var credentialsJson = File.ReadAllText(Constants.SERVICE_CREDENTIALS_PATH);
            Constants.ServiceCredentials = JsonConvert.DeserializeObject<ServiceCredentials>(credentialsJson);
        }
        else
        {
            MessageBox.Show("Could not load provided Service Credentials!", Properties.Resources.General_MessageBox_Error_Title, MessageBoxButton.OK, MessageBoxImage.Error);
            Logger.Error("Could not load provided Service Credentials!");
            Application.Current.Shutdown();
        }
    }

    private static void InstanceCheck()
    {
        var process = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly()?.Location));
        if (process.Count() > 1)
        {
            MessageBox.Show(Properties.Resources.Code_Main_MessageBox_InstanceCheck, Properties.Resources.General_MessageBox_Error_Title);
            Process.GetCurrentProcess().Kill();
        }
    }
}