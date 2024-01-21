using NanoTwitchLeafs.Controller;
using NanoTwitchLeafs.Objects;

namespace NanoTwitchLeafs.Interfaces;

public interface IAppSettingsService
{
    AppSettings LoadSettings();
    void SaveSettings(AppSettings appSettings);
}