using System.Collections.Generic;

namespace NanoTwitchLeafs.Objects
{
    public class NanoSettings : NotifyObject
    {
        public NanoSettings() : base()
        {
            NanoLeafDevices = new List<NanoLeafDevice>();
        }

        public List<NanoLeafDevice> NanoLeafDevices
        {
            get { return Get(() => NanoLeafDevices); }
            set { Set(() => NanoLeafDevices, value); }
        }
    }
}