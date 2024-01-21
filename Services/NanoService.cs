using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using log4net;
using NanoTwitchLeafs.Colors;
using NanoTwitchLeafs.Interfaces;
using NanoTwitchLeafs.Objects;
using NanoTwitchLeafs.Windows;
using Newtonsoft.Json;
using Zeroconf;

namespace NanoTwitchLeafs.Services;

public class NanoService : INanoService
{
    private readonly ISettingsService _settingsService;
    private readonly ILog _logger = LogManager.GetLogger(typeof(NanoService));

    public string TempToken;
    public string TempName;
    
    public NanoService(ISettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
    }
    public async Task<IReadOnlyList<IZeroconfHost>> GetDevicesInNetwork()
    {
        return await ZeroconfResolver.ResolveAsync("_nanoleafapi._tcp.local.");
    }
    
    public async Task<bool> PairDevice(NanoLeafDevice nanoLeafDevice)
    {
        _logger.Debug($"Try to pair Device on {nanoLeafDevice.Address}");
        using var client = new HttpClient();
        var responseMessage = await client.PostAsync(CreateApiAddress(nanoLeafDevice, "new"), new StringContent(""));
        if (responseMessage.StatusCode != HttpStatusCode.OK)
        {
            MessageBox.Show(Properties.Resources.Code_Nano_MessageBox_PairingError, Properties.Resources.General_MessageBox_Error_Title);
            _logger.Error($"Paring Device - Response: {responseMessage.StatusCode}");
            return false;
        }

        var responseString = await responseMessage.Content.ReadAsStringAsync();
        dynamic jsonObject = JsonConvert.DeserializeObject(responseString);
        if (jsonObject != null)
        {
            var authToken = (string)jsonObject.auth_token;

            if (string.IsNullOrEmpty(authToken))
            {
                MessageBox.Show(Properties.Resources.Code_Nano_MessageBox_PairingError, Properties.Resources.General_MessageBox_Error_Title);
                _logger.Error($"Paring Device - Response: {responseString}");
                return false;
            }
            nanoLeafDevice.PublicName = GetUserInputNameForNewDevice();
            nanoLeafDevice.Token = authToken;

            _logger.Debug($"Paring Device '{nanoLeafDevice.PublicName}' - Token received: {authToken}");
        }
        else
        {
            return false;
        }

        return true;
    }

    public async Task<NanoLeafDevice> UpdateNanoLeafDevice(NanoLeafDevice nanoLeafDevice)
    {
        var devices = await GetDevicesInNetwork();
        var matchFound = false;
        foreach (var device in devices)
        {
            if (device.DisplayName != nanoLeafDevice.DeviceName ||
                device.IPAddress == nanoLeafDevice.Address + nanoLeafDevice.Port) continue;
            var newAddress = device.IPAddress.Split(':');
            nanoLeafDevice.Address = newAddress[0];
            matchFound = true;
        }

        if (!matchFound)
        {
            MessageBox.Show(string.Format(Properties.Resources.Code_Nano_MessageBox_DeviceNotFound, nanoLeafDevice.PublicName), Properties.Resources.General_MessageBox_Error_Title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        return nanoLeafDevice;
    }

    public string GetUserInputNameForNewDevice()
    {
        bool unique = false;
        string deviceName = null;

        while (!unique)
        {
            InputDialogWindow inputDialogWindow = new InputDialogWindow(this, _settingsService.CurrentSettings.Language)
            {
                Owner = Application.Current.MainWindow
            };
            inputDialogWindow.ShowDialog();
            unique = _settingsService.CurrentSettings.NanoSettings.NanoLeafDevices.All(x => x.PublicName != inputDialogWindow.inputDialog_TextBox.Text) && !string.IsNullOrWhiteSpace(inputDialogWindow.inputDialog_TextBox.Text) && !inputDialogWindow.inputDialog_TextBox.Text.Equals("New Nanoleaf Device", StringComparison.InvariantCultureIgnoreCase);

            if (!unique)
            {
                MessageBox.Show(Properties.Resources.Code_Nano_MessageBox_UniqueName, Properties.Resources.General_MessageBox_Error_Title);
            }
            else
            {
                deviceName = inputDialogWindow.inputDialog_TextBox.Text;
            }
        }
        return deviceName;
    }

    public async Task<NanoleafControllerInfo> GetControllerInfo(NanoLeafDevice nanoLeafDevice)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(5);
        client.DefaultRequestHeaders.ExpectContinue = false;
        _logger.Debug($"Getting Controller Infos ...");
        var responseMessage = await client.GetAsync(CreateApiAddress(nanoLeafDevice, nanoLeafDevice.Token));
        if (responseMessage.StatusCode != HttpStatusCode.OK)
        {
            MessageBox.Show(string.Format(Properties.Resources.Code_Main_MessageBox_ControllerInfoNull, nanoLeafDevice.DeviceName), Properties.Resources.General_MessageBox_Error_Title);
            _logger.Error($"Controller Info - Response: {responseMessage.StatusCode}");
            return null;
        }

        string responseString = await responseMessage.Content.ReadAsStringAsync();

        if (string.IsNullOrEmpty(responseString))
        {
            MessageBox.Show(string.Format(Properties.Resources.Code_Main_MessageBox_ControllerInfoNull, nanoLeafDevice.DeviceName), Properties.Resources.General_MessageBox_Error_Title);
            _logger.Error($"Error on getting Controller Info from Device - Response: {responseString}");
            return null;
        }

        var nanoleafControllerInfo = JsonConvert.DeserializeObject<NanoleafControllerInfo>(responseString);

        switch (nanoleafControllerInfo.model)
        {
            case "NL22": // Aurora
                nanoleafControllerInfo.model = "Aurora/LightPanels";
                break;

            case "NL42": // Shapes
                nanoleafControllerInfo.model = "Shapes";
                break;

            case "NL29": // Canvas
                nanoleafControllerInfo.model = "Canvas";
                break;

            case "NL52": // Elements
                nanoleafControllerInfo.model = "Elements";
                break;

            case "NL59": // Lines
                nanoleafControllerInfo.model = "Lines";
                break;

            case "NL62": // MAGRGB
                nanoleafControllerInfo.model = "MAGRGB";
                break;

            default: // Default?
                nanoleafControllerInfo.model = "Unknown";
                break;
        }
        return nanoleafControllerInfo;
    }

    public async Task<string> GetFirmwareVersionFromDevice(string nanoleafDeviceName)
    {
        foreach (var device in _settingsService.CurrentSettings.NanoSettings.NanoLeafDevices.Where(device => device.PublicName == nanoleafDeviceName))
        {
            var controllerInfo = await GetControllerInfo(device);
            return controllerInfo.firmwareVersion;
        }

        return "Device Name not Found!";
    }

    public async Task<string> GetFirmwareVersionFromDevice(IPAddress nanoleafDeviceAddress)
    {
        foreach (var device in _settingsService.CurrentSettings.NanoSettings.NanoLeafDevices.Where(device => device.Address == nanoleafDeviceAddress.ToString()))
        {
            var controllerInfo = await GetControllerInfo(device);
            return controllerInfo.firmwareVersion;
        }

        return "Device Address not Found!";
    }

    public async Task<List<string>> GetEffectList(NanoLeafDevice nanoLeafDevice)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(5);
        client.DefaultRequestHeaders.ExpectContinue = false;
        _logger.Debug($"Getting Effects List from Controller ...");

        try
        {
            var responseMessage = await client.GetAsync(CreateApiAddress(nanoLeafDevice, $"{nanoLeafDevice.Token}/effects/effectsList"));
            if (responseMessage.StatusCode != HttpStatusCode.OK)
            {
                MessageBox.Show(Properties.Resources.Code_Trigger_MessageBox_EffectList + $" '{nanoLeafDevice.PublicName}'", Properties.Resources.General_MessageBox_Error_Title);
                _logger.Error($"Effect List - Response: {responseMessage.StatusCode}");
                return null;
            }
            string responseString = await responseMessage.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(responseString))
            {
                MessageBox.Show(Properties.Resources.Code_Trigger_MessageBox_EffectList + $" '{nanoLeafDevice.PublicName}'", Properties.Resources.General_MessageBox_Error_Title);
                _logger.Error($"Error on getting Controller Effects from Device - Response: {responseString}");
                return null;
            }

            return JsonConvert.DeserializeObject<List<string>>(responseString);
        }
        catch (Exception e)
        {
            _logger.Error($"Could not get Effect List from Controller on {nanoLeafDevice.PublicName} - {nanoLeafDevice.Address}");
            _logger.Error(e.Message, e);
            return new List<string>();
        }
    }

    public async Task IdentifyController(NanoLeafDevice nanoLeafDevice)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(5);
        client.DefaultRequestHeaders.ExpectContinue = false;
        _logger.Debug($"Identify Controller with Address {nanoLeafDevice.Address}");
        var responseMessage = await client.PutAsync(CreateApiAddress(nanoLeafDevice, $"{nanoLeafDevice.Token}/identify"), null);
        if (responseMessage.StatusCode != HttpStatusCode.OK)
        {
            MessageBox.Show(string.Format(Properties.Resources.Code_Nano_MessageBox_DeviceNotFound, nanoLeafDevice.PublicName), Properties.Resources.General_MessageBox_Error_Title);
            _logger.Error($"Identify Controller - Response: {responseMessage.StatusCode}");
        }
    }

    public async Task<string> GetEffect(NanoLeafDevice nanoLeafDevice)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(5);
        client.DefaultRequestHeaders.ExpectContinue = false;
        _logger.Debug($"Getting Current Effect from Controller ...");
        var responseMessage = await client.GetAsync(CreateApiAddress(nanoLeafDevice, $"{nanoLeafDevice.Token}/effects/select"));
        if (responseMessage.StatusCode != HttpStatusCode.OK)
        {
            MessageBox.Show(string.Format(Properties.Resources.Code_Nano_MessageBox_CurrentEffect, nanoLeafDevice.PublicName), Properties.Resources.General_MessageBox_Error_Title);
            _logger.Error($"Current Effect - Response: {responseMessage.StatusCode}");
            return "";
        }

        string responseString = await responseMessage.Content.ReadAsStringAsync();
        if (string.IsNullOrEmpty(responseString))
        {
            MessageBox.Show(string.Format(Properties.Resources.Code_Nano_MessageBox_CurrentEffect, nanoLeafDevice.PublicName), Properties.Resources.General_MessageBox_Error_Title);
            _logger.Error($"Error on getting Current Effect from Device - Response: {responseString}");
            return null;
        }
        return JsonConvert.DeserializeObject<string>(responseString);
    }

    public async Task SetEffect(NanoLeafDevice nanoLeafDevice, string effect)
    {
        _logger.Debug($"Sending Effect to Controller: ControllerName = {nanoLeafDevice.PublicName} - {effect}");

        using var client = new HttpClient();
        client.BaseAddress = new Uri($"http://{nanoLeafDevice.Address}:{nanoLeafDevice.Port}/api/v1/");
        client.Timeout = TimeSpan.FromSeconds(5);
        client.DefaultRequestHeaders.ExpectContinue = false;

        var bodyContent = "{\"select\": \"" + effect + "\"}";
        var body = new StringContent(bodyContent, Encoding.UTF8, "application/json");

        await client.PutAsync($"{nanoLeafDevice.Token}/effects", body);
    }

    public async Task<int> GetHue(NanoLeafDevice nanoLeafDevice)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(5);
        client.DefaultRequestHeaders.ExpectContinue = false;
        _logger.Debug($"Getting Current Hue from Controller ...");
        var responseMessage = await client.GetAsync(CreateApiAddress(nanoLeafDevice, $"{nanoLeafDevice.Token}/state/hue"));
        if (responseMessage.StatusCode != HttpStatusCode.OK)
        {
            MessageBox.Show(string.Format(Properties.Resources.Code_Nano_MessageBox_CurrentHue, nanoLeafDevice.PublicName), Properties.Resources.General_MessageBox_Error_Title);
            _logger.Error($"Current Hue - Response: {responseMessage.StatusCode}");
            return 0;
        }

        string responseString = await responseMessage.Content.ReadAsStringAsync();
        if (string.IsNullOrEmpty(responseString))
        {
            MessageBox.Show(string.Format(Properties.Resources.Code_Nano_MessageBox_CurrentHue, nanoLeafDevice.PublicName), Properties.Resources.General_MessageBox_Error_Title);
            _logger.Error($"Error on getting Current Controller Hue from Device - Response: {responseString}");
            return 0;
        }

        dynamic obj = JsonConvert.DeserializeObject(responseString);

        return obj.value;
    }

    public async Task SetHue(NanoLeafDevice nanoLeafDevice, int hue)
    {
        _logger.Debug($"Sending Hue to Controller: ControllerName = {nanoLeafDevice.PublicName} - {hue}");

        using var client = new HttpClient();
        client.BaseAddress = new Uri($"http://{nanoLeafDevice.Address}:{nanoLeafDevice.Port}/api/v1/");
        client.Timeout = TimeSpan.FromSeconds(5);
        client.DefaultRequestHeaders.ExpectContinue = false;

        var bodyContent = "{\"hue\" : {\"value\":" + hue + "}}";
        var body = new StringContent(bodyContent, Encoding.UTF8, "application/json");

        await client.PutAsync($"{nanoLeafDevice.Token}/state", body);
    }

    public async Task<int> GetSaturation(NanoLeafDevice nanoLeafDevice)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(5);
        client.DefaultRequestHeaders.ExpectContinue = false;
        _logger.Debug($"Getting Current Saturation from Controller ...");
        var responseMessage = await client.GetAsync(CreateApiAddress(nanoLeafDevice, $"{nanoLeafDevice.Token}/state/sat"));
        if (responseMessage.StatusCode != HttpStatusCode.OK)
        {
            MessageBox.Show(string.Format(Properties.Resources.Code_Nano_MessageBox_CurrentSaturation, nanoLeafDevice.PublicName), Properties.Resources.General_MessageBox_Error_Title);
            _logger.Error($"Current Saturation - Response: {responseMessage.StatusCode}");
            return 0;
        }

        string responseString = await responseMessage.Content.ReadAsStringAsync();
        if (string.IsNullOrEmpty(responseString))
        {
            MessageBox.Show(string.Format(Properties.Resources.Code_Nano_MessageBox_CurrentSaturation, nanoLeafDevice.PublicName), Properties.Resources.General_MessageBox_Error_Title);
            _logger.Error($"Error on getting Current Controller Saturation from Device - Response: {responseString}");
            return 0;
        }

        dynamic obj = JsonConvert.DeserializeObject(responseString);

        return obj.value;
    }

    public async Task SetSaturation(NanoLeafDevice nanoLeafDevice, int saturation)
    {
        _logger.Debug($"Sending Saturation to Controller: ControllerName = {nanoLeafDevice.PublicName} - {saturation}");

        using var client = new HttpClient();
        client.BaseAddress = new Uri($"http://{nanoLeafDevice.Address}:{nanoLeafDevice.Port}/api/v1/");
        client.Timeout = TimeSpan.FromSeconds(5);
        client.DefaultRequestHeaders.ExpectContinue = false;

        var bodyContent = "{\"sat\" : {\"value\":" + saturation + "}}";
        var body = new StringContent(bodyContent, Encoding.UTF8, "application/json");

        await client.PutAsync($"{nanoLeafDevice.Token}/state", body);
    }

    public async Task<int> GetColorTemperature(NanoLeafDevice nanoLeafDevice)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(5);
        client.DefaultRequestHeaders.ExpectContinue = false;
        _logger.Debug($"Getting Current ColorTemperature from Controller ...");
        var responseMessage = await client.GetAsync(CreateApiAddress(nanoLeafDevice, $"{nanoLeafDevice.Token}/state/ct"));
        if (responseMessage.StatusCode != HttpStatusCode.OK)
        {
            MessageBox.Show(string.Format(Properties.Resources.Code_Nano_MessageBox_CurrentColorTemp, nanoLeafDevice.PublicName), Properties.Resources.General_MessageBox_Error_Title);
            _logger.Error($"Current ColorTemperature - Response: {responseMessage.StatusCode}");
            return 0;
        }

        string responseString = await responseMessage.Content.ReadAsStringAsync();
        if (string.IsNullOrEmpty(responseString))
        {
            MessageBox.Show(string.Format(Properties.Resources.Code_Nano_MessageBox_CurrentColorTemp, nanoLeafDevice.PublicName), Properties.Resources.General_MessageBox_Error_Title);
            _logger.Error($"Error on getting Current Controller ColorTemperature from Device - Response: {responseString}");
            return 0;
        }

        dynamic obj = JsonConvert.DeserializeObject(responseString);

        return obj.value;
    }

    public async Task SetColorTemperature(NanoLeafDevice nanoLeafDevice, int colorTemperature)
    {
        _logger.Debug($"Sending Color Temperature to Controller: ControllerName = {nanoLeafDevice.PublicName} - {colorTemperature}");

        using var client = new HttpClient();
        client.BaseAddress = new Uri($"http://{nanoLeafDevice.Address}:{nanoLeafDevice.Port}/api/v1/");
        client.Timeout = TimeSpan.FromSeconds(5);
        client.DefaultRequestHeaders.ExpectContinue = false;

        var bodyContent = "{\"ct\" : {\"value\":" + colorTemperature + "}}";
        var body = new StringContent(bodyContent, Encoding.UTF8, "application/json");

        await client.PutAsync($"{nanoLeafDevice.Token}/state", body);
    }

    public async Task<int> GetBrightness(NanoLeafDevice nanoLeafDevice)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(5);
        client.DefaultRequestHeaders.ExpectContinue = false;
        _logger.Debug($"Getting Current Brightness from Controller ...");
        var responseMessage = await client.GetAsync(CreateApiAddress(nanoLeafDevice, $"{nanoLeafDevice.Token}/state/brightness"));
        if (responseMessage.StatusCode != HttpStatusCode.OK)
        {
            MessageBox.Show(string.Format(Properties.Resources.Code_Nano_MessageBox_CurrentBrightness, nanoLeafDevice.PublicName), Properties.Resources.General_MessageBox_Error_Title);
            _logger.Error($"Current Brightness - Response: {responseMessage.StatusCode}");
            return 0;
        }

        string responseString = await responseMessage.Content.ReadAsStringAsync();
        if (string.IsNullOrEmpty(responseString))
        {
            MessageBox.Show(string.Format(Properties.Resources.Code_Nano_MessageBox_CurrentBrightness, nanoLeafDevice.PublicName), Properties.Resources.General_MessageBox_Error_Title);
            _logger.Error($"Error on getting Current Controller Brightness from Device - Response: {responseString}");
            return 0;
        }

        dynamic obj = JsonConvert.DeserializeObject(responseString);

        return obj.value;
    }

    public async Task SetBrightness(NanoLeafDevice nanoLeafDevice, int brightness)
    {
        _logger.Debug($"Sending Brightness to Controller: ControllerName = {nanoLeafDevice.PublicName} - {brightness}%");

        using var client = new HttpClient();
        client.BaseAddress = new Uri($"http://{nanoLeafDevice.Address}:{nanoLeafDevice.Port}/api/v1/");
        client.Timeout = TimeSpan.FromSeconds(5);
        client.DefaultRequestHeaders.ExpectContinue = false;

        var b = new { brightness = new { value = brightness } };

        var body = new StringContent(JsonConvert.SerializeObject(b), Encoding.UTF8, "application/json");

        await client.PutAsync($"{nanoLeafDevice.Token}/state", body);
    }

    public async Task<NanoLeafState> GetState(NanoLeafDevice nanoLeafDevice)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(5);
        client.DefaultRequestHeaders.ExpectContinue = false;
        _logger.Debug($"Getting Current State from Controller ...");
        var responseMessage = await client.GetAsync(CreateApiAddress(nanoLeafDevice, $"{nanoLeafDevice.Token}/state"));
        if (responseMessage.StatusCode != HttpStatusCode.OK)
        {
            MessageBox.Show(string.Format(Properties.Resources.Code_Nano_MessageBox_CurrentState, nanoLeafDevice.PublicName), "Error!");
            _logger.Error($"Current State - Response: {responseMessage.StatusCode}");
            return null;
        }

        string responseString = await responseMessage.Content.ReadAsStringAsync();
        if (string.IsNullOrEmpty(responseString))
        {
            MessageBox.Show(string.Format(Properties.Resources.Code_Nano_MessageBox_CurrentState, nanoLeafDevice.PublicName), Properties.Resources.General_MessageBox_Error_Title);
            _logger.Error($"Error on getting Current State from Device - Response: {responseString}");
            return null;
        }

        NanoLeafState nanoLeafState = new NanoLeafState { State = JsonConvert.DeserializeObject<State>(responseString), Effect = await GetEffect(nanoLeafDevice) };

        return nanoLeafState;
    }

    public async Task SetState(NanoLeafDevice nanoLeafDevice, NanoLeafState nanoLeafState)
    {
        _logger.Debug($"Sending State to Controller: ControllerName = {nanoLeafDevice.PublicName} - {nanoLeafState}");

        //Note Sending Order is Important! Hue has to be Last!?
        await SetBrightness(nanoLeafDevice, nanoLeafState.State.brightness.value);
        await SetColorTemperature(nanoLeafDevice, nanoLeafState.State.ct.value);
        await SetSaturation(nanoLeafDevice, nanoLeafState.State.sat.value);
        await SetHue(nanoLeafDevice, nanoLeafState.State.hue.value);
    }

    public async Task<bool> GetOnState(NanoLeafDevice nanoLeafDevice)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(5);
        client.DefaultRequestHeaders.ExpectContinue = false;
        _logger.Debug($"Getting Current State from Controller ...");
        var responseMessage = await client.GetAsync(CreateApiAddress(nanoLeafDevice, $"{nanoLeafDevice.Token}/state/on"));
        if (responseMessage.StatusCode != HttpStatusCode.OK)
        {
            MessageBox.Show(string.Format(Properties.Resources.Code_Nano_MessageBox_CurrentState, nanoLeafDevice.PublicName), "Error!");
            _logger.Error($"Current State - Response: {responseMessage.StatusCode}");
            return false;
        }

        string responseString = await responseMessage.Content.ReadAsStringAsync();
        if (string.IsNullOrEmpty(responseString))
        {
            MessageBox.Show(string.Format(Properties.Resources.Code_Nano_MessageBox_CurrentState, nanoLeafDevice.PublicName), Properties.Resources.General_MessageBox_Error_Title);
            _logger.Error($"Error on getting Current State from Device - Response: {responseString}");
            return false;
        }

        dynamic obj = JsonConvert.DeserializeObject(responseString);

        return obj.value;
    }

    public async Task SetOnState(NanoLeafDevice nanoLeafDevice, bool state)
    {
        _logger.Debug($"Sending State to Controller: ControllerName = {nanoLeafDevice.PublicName} - {state}");

        using var client = new HttpClient();
        client.BaseAddress = new Uri($"http://{nanoLeafDevice.Address}:{nanoLeafDevice.Port}/api/v1/");
        client.Timeout = TimeSpan.FromSeconds(5);
        client.DefaultRequestHeaders.ExpectContinue = false;

        var s = new { on = new { value = state } };
        var body = new StringContent(JsonConvert.SerializeObject(s), Encoding.UTF8, "application/json");

        await client.PutAsync($"{nanoLeafDevice.Token}/state", body);
    }

    private string CreateApiAddress(NanoLeafDevice nanoLeafDevice, string url)
    {
        string address = $"http://{nanoLeafDevice.Address}:{nanoLeafDevice.Port}/api/v1/{url}";
        _logger.Debug($"Created API endpoint address \"{address}\"");
        return address;
    }

    public async Task<bool> UpdateAllNanoleafDevices()
    {
        List<NanoLeafDevice> updatedNanoLeafDevices = new List<NanoLeafDevice>();
        int count = 0;
        foreach (var device in _settingsService.CurrentSettings.NanoSettings.NanoLeafDevices)
        {
            var updatedDevice = await UpdateNanoLeafDevice(device);
            updatedNanoLeafDevices.Add(updatedDevice);
            _logger.Info($"Updated Address from Device {device.PublicName} from {device.Address} to {updatedDevice.Address}");
            count++;
        }
        if (updatedNanoLeafDevices.Count == 0)
        {
            _logger.Warn("No Nanoleaf Devices updated! Please check Connection!");
            return false;
        }
        else
        {
            _settingsService.CurrentSettings.NanoSettings.NanoLeafDevices.Clear();
            _settingsService.CurrentSettings.NanoSettings.NanoLeafDevices = updatedNanoLeafDevices;
            _logger.Info($"Updated {count} Devices");
            return true;
        }
    }

    public async Task SetColor(NanoLeafDevice nanoLeafDevice, RgbColor rgbColor)
    {
        //Note Sending Order is Important! Hue has to be Last!?
        var color = ColorConverting.RgbToHsb(rgbColor);
        await SetBrightness(nanoLeafDevice, color.Brightness);
        await SetSaturation(nanoLeafDevice, color.Saturation);
        await SetHue(nanoLeafDevice, color.Hue);
    }

    public async Task SetPanelColor(NanoLeafDevice nanoLeafDevice, int panelId, int r, int g, int b, int a)
    {
        var template = $"1 {panelId} 1 {r} {g} {b} 0 1";

        var json = $@"
        {{
            ""write"": {{
                ""command"": ""display"",
                ""animType"": ""static"",
                ""animData"": ""{template}"",
                ""loop"": false,
                ""palette"": []
            }}
        }}";

        using var client = new HttpClient();
        client.BaseAddress = new Uri($"http://{nanoLeafDevice.Address}:{nanoLeafDevice.Port}/api/v1/");
        client.Timeout = TimeSpan.FromSeconds(5);
        client.DefaultRequestHeaders.ExpectContinue = false;

        var body = new StringContent(json, Encoding.UTF8, "application/json");

        await client.PutAsync($"{nanoLeafDevice.Token}/effects", body);
    }
}