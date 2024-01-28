using System;
using log4net;
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
        private readonly ISettingsService _settingsService;
        private readonly ILog _logger = LogManager.GetLogger(typeof(MainWindow));

        public BlacklistWindow(ISettingsService settingsService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            Constants.SetCultureInfo(_settingsService.CurrentSettings.Language);
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

            if (_settingsService.CurrentSettings.Blacklist.Contains(username))
            {
                MessageBox.Show(Properties.Resources.General_MessageBox_UsernameExists, Properties.Resources.General_MessageBox_Error_Title);
                return;
            }

            _settingsService.CurrentSettings.Blacklist.Add(username);
            _logger.Info(string.Format(Properties.Resources.General_Blacklist_MessageBox_AddedUserX, username));
            blackbox_Input_Textbox.Text = "";
            _settingsService.SaveSettings();
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

            if (_settingsService.CurrentSettings.Blacklist.Contains(username))
            {
                _settingsService.CurrentSettings.Blacklist.Remove(username);
                _logger.Info(string.Format(Properties.Resources.General_Blacklist_MessageBox_RemovedUserX, username));
            }
            _settingsService.SaveSettings();
            blackbox_Input_Textbox.Text = "";

            FillListBox();
        }

        private void FillListBox()
        {
            blacklist_ListBox.Items.Clear();

            foreach (string username in _settingsService.CurrentSettings.Blacklist)
            {
                blacklist_ListBox.Items.Add(username);
            }
        }
    }
}