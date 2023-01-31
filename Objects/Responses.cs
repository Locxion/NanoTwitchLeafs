namespace NanoTwitchLeafs.Objects
{
    public class Responses : NotifyObject
    {
        public Responses() : base()
        {
            StartupResponse = Properties.Resources.Code_Responses_ConnectMessage;
            CommandResponse = Properties.Resources.Code_Responses_CommandResponse;
            CommandDurationResponse = Properties.Resources.Code_Responses_CommandDurationResponse;
            KeywordResponse = Properties.Resources.Code_Responses_KeywordResponse;
            StartupMessageActive = true;
        }

        public bool StartupMessageActive
        {
            get { return Get(() => StartupMessageActive); }
            set { Set(() => StartupMessageActive, value); }
        }

        public string StartupResponse
        {
            get { return Get(() => StartupResponse); }
            set { Set(() => StartupResponse, value); }
        }

        public string CommandResponse
        {
            get { return Get(() => CommandResponse); }
            set { Set(() => CommandResponse, value); }
        }

        public string CommandDurationResponse
        {
            get { return Get(() => CommandDurationResponse); }
            set { Set(() => CommandDurationResponse, value); }
        }

        public string KeywordResponse
        {
            get { return Get(() => KeywordResponse); }
            set { Set(() => KeywordResponse, value); }
        }
    }
}