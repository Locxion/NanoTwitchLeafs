namespace NanoTwitchLeafs.Objects
{
    public class NanoLeafDevice : NotifyObject
    {
        public NanoLeafDevice() : base()
        {
            Port = 16021;
        }

        /// <summary>
        /// Flexible PublicName of the Nanoleaf Device set by User
        /// </summary>
        public string PublicName
        {
            get { return Get(() => PublicName); }
            set { Set(() => PublicName, value); }
        }

        /// <summary>
        /// Fixed DeviceName of the Nanoleaf Device
        /// </summary>
        public string DeviceName
        {
            get { return Get(() => DeviceName); }
            set { Set(() => DeviceName, value); }
        }

        public string Token
        {
            get { return Get(() => Token); }
            set { Set(() => Token, value); }
        }

        public string Address
        {
            get { return Get(() => Address); }
            set { Set(() => Address, value); }
        }

        public int Port
        {
            get { return Get(() => Port); }
            set { Set(() => Port, value); }
        }

        public NanoleafControllerInfo NanoleafControllerInfo
        {
            get { return Get(() => NanoleafControllerInfo); }
            set { Set(() => NanoleafControllerInfo, value); }
        }
    }
}