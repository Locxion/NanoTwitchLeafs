using NanoTwitchLeafs.Objects;

namespace NanoTwitchLeafs.Interfaces;

public interface ISettingsService
{
    AppSettings CurrentSettings { get; set; }

    void SaveSettings();

    AppSettings ReturnBlankSettings();
}