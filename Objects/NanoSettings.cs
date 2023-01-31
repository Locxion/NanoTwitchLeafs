using System.Collections.Generic;

namespace NanoTwitchLeafs.Objects
{
    public class NanoSettings : NotifyObject
    {
        public NanoSettings() : base()
        {
            NanoLeafDevices = new List<NanoLeafDevice>();
            ChangeBackOnCommand = true;
            ChangeBackOnKeyword = true;
        }

        public List<NanoLeafDevice> NanoLeafDevices
        {
            get { return Get(() => NanoLeafDevices); }
            set { Set(() => NanoLeafDevices, value); }
        }

        public bool TriggerEnabled
        {
            get { return Get(() => TriggerEnabled); }
            set { Set(() => TriggerEnabled, value); }
        }

        public bool CooldownEnabled
        {
            get { return Get(() => CooldownEnabled); }
            set { Set(() => CooldownEnabled, value); }
        }

        public bool CooldownIgnore
        {
            get { return Get(() => CooldownIgnore); }
            set { Set(() => CooldownIgnore, value); }
        }

        public int Cooldown
        {
            get { return Get(() => Cooldown); }
            set { Set(() => Cooldown, value); }
        }

        public bool ChangeBackOnCommand
        {
            get { return Get(() => ChangeBackOnCommand); }
            set { Set(() => ChangeBackOnCommand, value); }
        }

        public bool ChangeBackOnKeyword
        {
            get { return Get(() => ChangeBackOnKeyword); }
            set { Set(() => ChangeBackOnKeyword, value); }
        }
    }
}