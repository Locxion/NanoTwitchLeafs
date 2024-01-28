using log4net;
using System;
using System.IO;
using System.Windows;
using log4net.Config;

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
                NanoTwitchLeafs.Main.Run();
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