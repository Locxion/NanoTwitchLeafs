using log4net;
using NanoTwitchLeafs.Windows;
using System;
using System.IO;
using System.Windows;
using log4net.Config;
using Microsoft.Extensions.DependencyInjection;
using NanoTwitchLeafs.Interfaces;
using NanoTwitchLeafs.Services;

namespace NanoTwitchLeafs
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private async void App_Startup(object sender, StartupEventArgs e)
        {
            // Create Nanoleafs directory.
            Directory.CreateDirectory(Constants.PROGRAMFILESFOLDER_PATH);

            // Initialize Logger
            GlobalContext.Properties["LogFile"] = Constants.LOG_PATH;
            string s = new Uri(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase) ?? string.Empty, "log4net.config")).LocalPath;
            XmlConfigurator.Configure(new FileInfo(s));
            
            try
            {
                // var window = new MainWindow();
                // window.Show();
                //
                // await window.InitializeAsync();
                
                Bootstrapper.Run();
            }
            catch (Exception exception)
            {
                var logger = LogManager.GetLogger(typeof(App));
                logger.Error($"Error while initializing {nameof(MainWindow)}: {exception.Message}", exception);
                logger.Error(exception.Message, exception);
            }
        }
    }
    public static class Bootstrapper
    {
        public static async void Run()
        {
            var serviceProvider = DependencyConfig.ConfigureServices();

            var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
            await mainWindow.InitializeAsync();
        }
    }
        
    public static class DependencyConfig
    {
        public static ServiceProvider ConfigureServices()
        {
            var serviceProvider = new ServiceCollection()
                //Windows
                .AddTransient<MainWindow>()
                .AddTransient<ResponsesWindow>()
                .AddTransient<AppInfoWindow>()
                .AddTransient<TwitchLinkWindow>()
                .AddTransient<BlacklistWindow>()
                .AddTransient<PairingWindow>()
                .AddTransient<TriggerWindow>()
                .AddTransient<DevicesInfoWindow>()
                //Services
                .AddSingleton<IAppSettingsService, AppSettingsService>()
                // Hier füge deine Abhängigkeiten hinzu
                // .AddTransient<Interface, Implementierung>()
                .BuildServiceProvider();
            
            return serviceProvider;
        }
    }
}