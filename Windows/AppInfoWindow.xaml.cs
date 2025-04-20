using NanoTwitchLeafs.Controller;
using NanoTwitchLeafs.Objects;
using System;
using System.Diagnostics;
using System.Windows;

namespace NanoTwitchLeafs.Windows
{
    /// <summary>
    /// Interaction logic for AppInfoWindow.xaml
    /// </summary>
    public partial class AppInfoWindow : Window
    {
        private readonly AppSettings _appSettings;

        public AppInfoWindow(AppSettings appSettings, AppSettingsController appSettingsController)
        {
            if (appSettingsController == null) throw new ArgumentNullException(nameof(appSettingsController));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            Constants.SetCultureInfo(_appSettings.Language);
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