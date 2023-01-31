namespace NanoTwitchLeafs.Objects
{
    public class StreamlabsInformation : NotifyObject
    {
        public StreamlabsInformation() : base()
        {
        }

        public string StreamlabsAToken
        {
            get { return Get(() => StreamlabsAToken); }
            set { Set(() => StreamlabsAToken, value); }
        }

        public string StreamlabsUser
        {
            get { return Get(() => StreamlabsUser); }
            set { Set(() => StreamlabsUser, value); }
        }

        public string StreamlabsSocketToken
        {
            get { return Get(() => StreamlabsSocketToken); }
            set { Set(() => StreamlabsSocketToken, value); }
        }
    }
}