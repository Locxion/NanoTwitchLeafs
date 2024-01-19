using System;
using NanoTwitchLeafs.Controller;
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
        private readonly IAppSettingsService _appSettingsService;
        private readonly AppSettings _appSettings;

        public ResponsesWindow(IAppSettingsService appSettingsService)
        {
            _appSettingsService = appSettingsService ?? throw new ArgumentNullException(nameof(appSettingsService));
            _appSettings = _appSettingsService.LoadSettings();
            Constants.SetCultureInfo(_appSettings.Language);
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
            _appSettingsService.SaveSettings(_appSettings);
            Close();
        }

        private void ResponseHelp_Button_Click(object sender, RoutedEventArgs e)
        {
            ResponsesHelpWindow responsesHelpWindow = new ResponsesHelpWindow(_appSettings.Language)
            {
                Owner = this
            };
            responsesHelpWindow.Show();
        }

        #endregion

        #region Methods

        private void SaveStrings()
        {
            _appSettings.Responses.StartupResponse = connectMessage_TextBox.Text;
            _appSettings.Responses.CommandResponse = commandResponse_TextBox.Text;
            _appSettings.Responses.CommandDurationResponse = commandDurationResponse_TextBox.Text;
            _appSettings.Responses.KeywordResponse = keywordResponse_TextBox.Text;
            _appSettings.Responses.StartupMessageActive = (bool)StartupMessage_CheckBox.IsChecked;
        }

        private void LoadStrings()
        {
            connectMessage_TextBox.Text = _appSettings.Responses.StartupResponse;
            commandResponse_TextBox.Text = _appSettings.Responses.CommandResponse;
            commandDurationResponse_TextBox.Text = _appSettings.Responses.CommandDurationResponse;
            keywordResponse_TextBox.Text = _appSettings.Responses.KeywordResponse;
            StartupMessage_CheckBox.IsChecked = _appSettings.Responses.StartupMessageActive;
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