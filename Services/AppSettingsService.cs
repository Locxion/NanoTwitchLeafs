using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using log4net;
using NanoTwitchLeafs.Interfaces;
using NanoTwitchLeafs.Objects;
using Newtonsoft.Json;

namespace NanoTwitchLeafs.Services;

public class AppSettingsService : IAppSettingsService
{
    private const DataProtectionScope DataProtectionScope = System.Security.Cryptography.DataProtectionScope.CurrentUser;

    private readonly ILog _logger = LogManager.GetLogger(typeof(AppSettingsService));

    public AppSettingsService()
    {
    }
    
    /// <summary>
    /// Loads Settings from Settings Path
    /// </summary>
    /// <returns>AppSettings</returns>
    public AppSettings LoadSettings()
    {
#if DEBUG
        if (File.Exists(Constants.DEBUG_SETTINGS_PATH))
        {
            return LoadSettings(Constants.DEBUG_SETTINGS_PATH);
        }

        return new AppSettings();

#else
            if (!File.Exists(Constants.SETTINGS_PATH))
            {
                _logger.Warn("No Settings File found ... Load Blank Settings.");
                return new AppSettings();
            }
            else
            {
                _logger.Info("Settings File found ...");
                return LoadSettings(Constants.SETTINGS_PATH);
            }
#endif
    }

    /// <summary>
    /// Saves Settings
    /// </summary>
    /// <param name="appSettings"></param>
    public void SaveSettings(AppSettings appSettings)
    {
        try
        {
#if DEBUG
            File.WriteAllText(Constants.DEBUG_SETTINGS_PATH, JsonConvert.SerializeObject(appSettings, Formatting.Indented));
            _logger.Debug($"Write settings to {Constants.DEBUG_SETTINGS_PATH}");
#else
                string json = JsonConvert.SerializeObject(appSettings);
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                byte[] encryptedData = ProtectedData.Protect(bytes, null, DataProtectionScope);
                string base64 = Convert.ToBase64String(encryptedData);
                _logger.Debug("Save Settings to File " + Constants.SETTINGS_PATH);
                File.WriteAllText(Constants.SETTINGS_PATH, base64);
#endif
        }
        catch (Exception ex)
        {
            _logger.Error("Could not write to Settings File!");
            _logger.Error(ex.Message);
        }
    }

    /// <summary>
    /// Loads Settings
    /// </summary>
    /// <param name="path"></param>
    /// <returns>AppSettings</returns>
    private AppSettings LoadSettings(string path)
    {
        try
        {
#if DEBUG
            _logger.Debug($"Load settings from {Constants.DEBUG_SETTINGS_PATH}");
            return JsonConvert.DeserializeObject<AppSettings>(File.ReadAllText(path));
#else
                string base64 = File.ReadAllText(path);
                byte[] encryptedData = Convert.FromBase64String(base64);
                byte[] decryptedData = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope);
                string json = Encoding.UTF8.GetString(decryptedData);
                _logger.Debug("Load Settings from File " + path);
                return JsonConvert.DeserializeObject<AppSettings>(json);
#endif
        }
        catch (Exception ex)
        {
            _logger.Error("Could not Load Settings from File!");
            _logger.Error(ex.Message);
            _logger.Error("Loading blank Settings instead.");
            return new AppSettings();
        }
    }
    
}
