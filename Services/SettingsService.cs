using System;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using log4net;
using NanoTwitchLeafs.Interfaces;
using NanoTwitchLeafs.Objects;

namespace NanoTwitchLeafs.Services
{
    public class SettingsService: ISettingsService
    {
        private readonly ILog _logger = LogManager.GetLogger(typeof(AppSettingsService));

        private readonly IAppSettingsService _appSettingsService;
        public AppSettings CurrentSettings { get; set; }

        public SettingsService(IAppSettingsService appSettingsService)
        {
            _appSettingsService = appSettingsService ?? throw new ArgumentNullException(nameof(appSettingsService));

            CurrentSettings = _appSettingsService.LoadSettings();
        }
        
        public void SaveSettings()
        {
            _appSettingsService.SaveSettings(CurrentSettings);
        }

        public AppSettings ReturnBlankSettings()
        {
            return ResetSettings();
        }

        public void ExportSettings()
        {
            var cleanSettings = CurrentSettings;
            cleanSettings.BotAuthObject = null;
            cleanSettings.BroadcasterAuthObject = null;
            cleanSettings.StreamlabsInformation.StreamlabsAToken = null;
            cleanSettings.StreamlabsInformation.StreamlabsSocketToken = null;
            var fileDialog = new SaveFileDialog();
            fileDialog.Filter = "Textdocument (*.txt)|*.txt";
            fileDialog.DefaultExt = "txt";
            fileDialog.AddExtension = true;
            fileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(fileDialog.FileName, JsonSerializer.Serialize(cleanSettings));
                _logger.Info($"Saved Settings to:{fileDialog.FileName}");
            }
        }

        private AppSettings ResetSettings()
        {
            var blankSettings = new AppSettings();
            CurrentSettings = blankSettings;
            SaveSettings();
            return CurrentSettings;
        }
    }
}