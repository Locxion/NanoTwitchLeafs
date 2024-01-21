using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using NanoTwitchLeafs.Colors;
using NanoTwitchLeafs.Objects;
using Zeroconf;
// ReSharper disable PossibleNullReferenceException
// ReSharper disable CommentTypo
// ReSharper disable StringLiteralTypo

namespace NanoTwitchLeafs.Interfaces;

public interface INanoService
{
    Task<IReadOnlyList<IZeroconfHost>> GetDevicesInNetwork();
    Task<bool> PairDevice(NanoLeafDevice nanoLeafDevice);
    Task<NanoLeafDevice> UpdateNanoLeafDevice(NanoLeafDevice nanoLeafDevice);
    string GetUserInputNameForNewDevice();
    Task<NanoleafControllerInfo> GetControllerInfo(NanoLeafDevice nanoLeafDevice);
    Task<string> GetFirmwareVersionFromDevice(string nanoleafDeviceName);
    Task<string> GetFirmwareVersionFromDevice(IPAddress nanoleafDeviceAddress);
    Task<List<string>> GetEffectList(NanoLeafDevice nanoLeafDevice);
    Task IdentifyController(NanoLeafDevice nanoLeafDevice);
    Task<string> GetEffect(NanoLeafDevice nanoLeafDevice);
    Task SetEffect(NanoLeafDevice nanoLeafDevice, string effect);
    Task<int> GetHue(NanoLeafDevice nanoLeafDevice);
    Task SetHue(NanoLeafDevice nanoLeafDevice, int hue);
    Task<int> GetSaturation(NanoLeafDevice nanoLeafDevice);
    Task SetSaturation(NanoLeafDevice nanoLeafDevice, int saturation);
    Task<int> GetColorTemperature(NanoLeafDevice nanoLeafDevice);
    Task SetColorTemperature(NanoLeafDevice nanoLeafDevice, int colorTemperature);
    Task<int> GetBrightness(NanoLeafDevice nanoLeafDevice);
    Task SetBrightness(NanoLeafDevice nanoLeafDevice, int brightness);
    Task<NanoLeafState> GetState(NanoLeafDevice nanoLeafDevice);
    Task SetState(NanoLeafDevice nanoLeafDevice, NanoLeafState nanoLeafState);
    Task<bool> GetOnState(NanoLeafDevice nanoLeafDevice);
    Task SetOnState(NanoLeafDevice nanoLeafDevice, bool state);
    Task<bool> UpdateAllNanoleafDevices();
    Task SetColor(NanoLeafDevice nanoLeafDevice, RgbColor rgbColor);
    Task SetPanelColor(NanoLeafDevice nanoLeafDevice, int panelId, int r, int g, int b, int a);
}