using System;
using log4net;
using NanoTwitchLeafs.Controller;
using NanoTwitchLeafs.Objects;
using System.Windows;
using NanoTwitchLeafs.Interfaces;

namespace NanoTwitchLeafs.Windows
{
    /// <summary>
    /// Interaction logic for BlacklistWindow.xaml
    /// </summary>
    public partial class BlacklistWindow : Window
    {
        private readonly IAppSettingsService _appSettingsService;
        private readonly AppSettings _appSettings;
        private readonly ILog _logger = LogManager.GetLogger(typeof(MainWindow));

        public BlacklistWindow(IAppSettingsService appSettingsService)
        {
            _appSettingsService = appSettingsService ?? throw new ArgumentNullException(nameof(appSettingsService));
            _appSettings = _appSettingsService.LoadSettings();
            Constants.SetCultureInfo(_appSettings.Language);
            InitializeComponent();

            FillListBox();
        }

        private void blacklist_Add_Button_Click(object sender, RoutedEventArgs e)
        {
            string username = blackbox_Input_Textbox.Text.ToLower();
            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show(Properties.Resources.General_MessageBox_EnterUsername, Properties.Resources.General_MessageBox_Error_Title);
                return;
            }

            if (_appSettings.Blacklist.Contains(username))
            {
                MessageBox.Show(Properties.Resources.General_MessageBox_UsernameExists, Properties.Resources.General_MessageBox_Error_Title);
                return;
            }

            _appSettings.Blacklist.Add(username);
            _logger.Info(string.Format(Properties.Resources.General_Blacklist_MessageBox_AddedUserX, username));
            blackbox_Input_Textbox.Text = "";
            _appSettingsService.SaveSettings(_appSettings);
            FillListBox();
        }

        private void blacklist_Remove_Button_Click(object sender, RoutedEventArgs e)
        {
            string username;
            if (blacklist_ListBox.SelectedItem == null)
            {
                MessageBox.Show(Properties.Resources.General_MessageBox_NoUserSelected, Properties.Resources.General_MessageBox_Error_Title);
                return;
            }

            if (!string.IsNullOrWhiteSpace(blackbox_Input_Textbox.Text))
            {
                username = blackbox_Input_Textbox.Text.ToLower();
            }
            else
            {
                username = blacklist_ListBox.SelectedItem.ToString().ToLower();
            }

            if (_appSettings.Blacklist.Contains(username))
            {
                _appSettings.Blacklist.Remove(username);
                _logger.Info(string.Format(Properties.Resources.General_Blacklist_MessageBox_RemovedUserX, username));
            }
            _appSettingsService.SaveSettings(_appSettings);
            blackbox_Input_Textbox.Text = "";

            FillListBox();
        }

        private void FillListBox()
        {
            blacklist_ListBox.Items.Clear();

            foreach (string username in _appSettings.Blacklist)
            {
                blacklist_ListBox.Items.Add(username);
            }
        }
    }
}