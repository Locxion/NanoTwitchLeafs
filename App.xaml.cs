using log4net;
using NanoTwitchLeafs.Windows;
using System;
using System.IO;
using System.Windows;
using log4net.Config;

namespace NanoTwitchLeafs
{
    /// <summary>
    /// Main application entry point. Initializes logging and displays the main window.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Application startup handler. Creates required directories, initializes logging,
        /// and displays the main window.
        /// </summary>
        /// <param name="sender">Event sender (unused).</param>
        /// <param name="e">Startup event arguments.</param>
        private async void App_Startup(object sender, StartupEventArgs e)
        {
            // Create Nanoleafs directory.
            Directory.CreateDirectory(Constants.PROGRAMFILESFOLDER_PATH);

            // Initialize Logger
            GlobalContext.Properties["LogFile"] = Constants.LOG_PATH;
            string s = new Uri(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase), "log4net.config")).LocalPath;
            XmlConfigurator.Configure(new FileInfo(s));
            
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