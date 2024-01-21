using System;
using log4net;
using NanoTwitchLeafs.Interfaces;
using NanoTwitchLeafs.Objects;

namespace NanoTwitchLeafs.Services
{
    public class SettingsService: ISettingsService
    {
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

        private AppSettings ResetSettings()
        {
            var blankSettings = new AppSettings();
            CurrentSettings = blankSettings;
            SaveSettings();
            return CurrentSettings;
        }
    }
}