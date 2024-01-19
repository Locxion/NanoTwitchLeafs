using NanoTwitchLeafs.Controller;
using System;
using System.Diagnostics;
using System.Windows;
using NanoTwitchLeafs.Interfaces;

namespace NanoTwitchLeafs.Windows
{
    /// <summary>
    /// Interaction logic for AppInfoWindow.xaml
    /// </summary>
    public partial class AppInfoWindow : Window
    {
        public AppInfoWindow(IAppSettingsService appSettingsService)
        {
            var appSettingsService1 = appSettingsService ?? throw new ArgumentNullException(nameof(appSettingsService));
            var appSettings = appSettingsService1.LoadSettings();
            Constants.SetCultureInfo(appSettings.Language);
            InitializeComponent();
            version_Label.Content = typeof(AppInfoWindow).Assembly.GetName().Version;
        }

        private void Feedback_Button_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/Locxion/NanoTwitchLeafs/issues");
        }

        private void Discord_Label_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Process.Start("https://discord.gg/w92xZKd");
        }

        private void Github_Label_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Process.Start("https://github.com/Locxion/NanoTwitchLeafs");
        }
    }
}