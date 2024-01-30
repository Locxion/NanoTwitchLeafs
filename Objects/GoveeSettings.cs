using System.Collections.Generic;

namespace NanoTwitchLeafs.Objects;

public class GoveeSettings : NotifyObject
{
    public GoveeSettings() : base()
    {
        GoveeDevices = new List<GoveeDevice>();
    }
        
    public List<GoveeDevice> GoveeDevices
    {
        get { return Get(() => GoveeDevices); }
        set { Set(() => GoveeDevices, value); }
    }
}