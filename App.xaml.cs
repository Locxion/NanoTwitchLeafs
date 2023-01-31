using log4net;
using NanoTwitchLeafs.Windows;
using System;
using System.Windows;

namespace NanoTwitchLeafs
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private async void App_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                var window = new MainWindow();
                window.Show();

                await window.InitializeAsync();
            }
            catch (Exception exception)
            {
                var logger = LogManager.GetLogger(typeof(App));
                logger.Error($"Error while initializing {nameof(MainWindow)}: {exception.Message}", exception);
                logger.Error(exception.Message, exception);
            }
        }
    }
}