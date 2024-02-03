using System.Collections.Generic;
using GoveeCSharpConnector.Objects;

namespace NanoTwitchLeafs.Objects;

public class GoveeSettings : NotifyObject
{
    public GoveeSettings() : base()
    {
        GoveeDevices = new List<GoveeDevice>();
        GoveeApiKey = "";
    }

    public string GoveeApiKey
    {
        get { return Get(() => GoveeApiKey); }
        set { Set(() => GoveeApiKey, value); }
    }
    public List<GoveeDevice> GoveeDevices
    {
        get { return Get(() => GoveeDevices); }
        set { Set(() => GoveeDevices, value); }
    }
}