using System;
using System.Threading.Tasks;

namespace NanoTwitchLeafs.Interfaces;

public interface IDeviceService
{
    Task DiscoverDevice();
    Task PairDevice();
    Task SendEffectToDevice();
    Task SendColorToDevice();
    Task SendColorToPart();
}

public class DeviceService : IDeviceService
{
    private readonly ISettingsService _settingsService;

    public DeviceService(ISettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
    }

    public async Task DiscoverDevice()
    {
        throw new NotImplementedException();
    }

    public async Task PairDevice()
    {
        throw new System.NotImplementedException();
    }

    public async Task SendEffectToDevice()
    {
        throw new System.NotImplementedException();
    }

    public async Task SendColorToDevice()
    {
        throw new System.NotImplementedException();
    }

    public async Task SendColorToPart()
    {
        throw new System.NotImplementedException();
    }
    
}