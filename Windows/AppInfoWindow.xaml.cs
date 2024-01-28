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
        private readonly ISettingsService _settingsService;

        public AppInfoWindow(ISettingsService settingsService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            Constants.SetCultureInfo(settingsService.CurrentSettings.Language);
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