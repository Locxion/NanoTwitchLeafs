using System;
using NanoTwitchLeafs.Objects;
using System.Windows;
using NanoTwitchLeafs.Interfaces;

namespace NanoTwitchLeafs.Windows
{
    /// <summary>
    /// Interaction logic for Responses.xaml
    /// </summary>
    public partial class ResponsesWindow : Window
    {
        private readonly ISettingsService _settingsService;

        public ResponsesWindow(ISettingsService settingsService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

            Constants.SetCultureInfo(settingsService.CurrentSettings.Language);
            InitializeComponent();

            LoadStrings();
        }

        #region UI Methods

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ResetToDefault();
        }

        private void SaveAndClose_Button_Click(object sender, RoutedEventArgs e)
        {
            SaveStrings();
            _settingsService.SaveSettings();
            Close();
        }

        private void ResponseHelp_Button_Click(object sender, RoutedEventArgs e)
        {
            ResponsesHelpWindow responsesHelpWindow = new ResponsesHelpWindow(_settingsService.CurrentSettings.Language)
            {
                Owner = this
            };
            responsesHelpWindow.Show();
        }

        #endregion

        #region Methods

        private void SaveStrings()
        {
            _settingsService.CurrentSettings.Responses.StartupResponse = connectMessage_TextBox.Text;
            _settingsService.CurrentSettings.Responses.CommandResponse = commandResponse_TextBox.Text;
            _settingsService.CurrentSettings.Responses.CommandDurationResponse = commandDurationResponse_TextBox.Text;
            _settingsService.CurrentSettings.Responses.KeywordResponse = keywordResponse_TextBox.Text;
            _settingsService.CurrentSettings.Responses.StartupMessageActive = (bool)StartupMessage_CheckBox.IsChecked;
        }

        private void LoadStrings()
        {
            connectMessage_TextBox.Text = _settingsService.CurrentSettings.Responses.StartupResponse;
            commandResponse_TextBox.Text = _settingsService.CurrentSettings.Responses.CommandResponse;
            commandDurationResponse_TextBox.Text = _settingsService.CurrentSettings.Responses.CommandDurationResponse;
            keywordResponse_TextBox.Text = _settingsService.CurrentSettings.Responses.KeywordResponse;
            StartupMessage_CheckBox.IsChecked = _settingsService.CurrentSettings.Responses.StartupMessageActive;
        }

        private void ResetToDefault()
        {
            connectMessage_TextBox.Text = Properties.Resources.Code_Responses_ConnectMessage;
            commandResponse_TextBox.Text = Properties.Resources.Code_Responses_CommandResponse;
            commandDurationResponse_TextBox.Text = Properties.Resources.Code_Responses_CommandDurationResponse;
            keywordResponse_TextBox.Text = Properties.Resources.Code_Responses_KeywordResponse;
        }

        #endregion
    }
}