using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using NanoTwitchLeafs.Colors;

namespace NanoTwitchLeafs.Interfaces;

public interface IGoveeService
{
    Task<List<GoveeDevice>> GetDevices();
    Task<GoveeState> GetState(string deviceId, string deviceModel);
    Task ToggleDevice(string deviceId, string deviceModel, bool state);
    Task SetBrightness(string deviceId, string deviceMode, int brightness);
    Task SetColor(string deviceId, string deviceModel, RgbColor rgbColor);
}

public class GoveeService : IGoveeService
{
    private const string GoveeApiHost = "https://developer-api.govee.com/v1";
    private readonly ISettingsService _settingsService;
    private HttpClient _httpClient = new();

    public GoveeService(ISettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _httpClient.DefaultRequestHeaders.Add("Govee-API-Key", _settingsService.CurrentSettings.GoveeSettings.GoveeApiKey );
    }
    
    public async Task<List<GoveeDevice>> GetDevices()
    {
        var response = await _httpClient.GetFromJsonAsync<GoveeResponse>($"{GoveeApiHost}/devices");

        return response.Data.Devices;
    }

    public async Task<GoveeState> GetState(string deviceId, string deviceModel)
    {
        throw new System.NotImplementedException();
    }

    public async Task ToggleDevice(string deviceId, string deviceModel, bool state)
    {
        throw new System.NotImplementedException();
    }

    public async Task SetBrightness(string deviceId, string deviceMode, int brightness)
    {
        throw new System.NotImplementedException();
    }

    public async Task SetColor(string deviceId, string deviceModel, RgbColor rgbColor)
    {
        throw new System.NotImplementedException();
    }
}