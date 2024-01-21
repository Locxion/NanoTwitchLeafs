using log4net;
using NanoTwitchLeafs.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using TwitchLib.Api;
using TwitchLib.Api.Core.Exceptions;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events.FollowerService;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;

namespace NanoTwitchLeafs.Controller
{

    public delegate void OnBitsReceived(string username, int amount);
    public delegate void OnChannelPointsRedeemed(string username, string promt, Guid guid);

    public class TwitchPubSubController : IDisposable
    {
        private TwitchPubSub _client;
        private readonly ILog _logger = LogManager.GetLogger(typeof(TwitchPubSubController));
        private AppSettings _appSettings;

        private TwitchAPI _api;

        public event OnBitsReceived OnBitsReceived;

        public event OnChannelPointsRedeemed OnChannelPointsRedeemed;

        private string _userId = "";

        public bool IsConnected = false;

        public async void Connect(AppSettings appSettings)
        {
            _client = new TwitchPubSub();

            _appSettings = appSettings;

            _client.OnPubSubServiceConnected += OnPubSubServiceConnected;
            _client.OnListenResponse += OnListenResponse;
            _client.OnBitsReceivedV2 += _client_OnBitsReceivedV2;
            _client.OnChannelPointsRewardRedeemed += _client_OnChannelPointsRewardRedeemed;

            _api = new TwitchAPI();
            _api.Settings.ClientId = HelperClass.GetTwitchApiCredentials(_appSettings).ClientId;

            _userId = await HelperClass.GetUserId(_api, _appSettings, _appSettings.ChannelName);
            if (_userId == null)
            {
                MessageBox.Show(Properties.Resources.Code_PubSub_MessageBox_ConnectError,
                    Properties.Resources.General_MessageBox_Error_Title, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _client.Connect();
        }

        private void _client_OnChannelPointsRewardRedeemed(object sender, OnChannelPointsRewardRedeemedArgs e)
        {
            _logger.Debug($"Recieved ChannelPoints Reward from {e.RewardRedeemed.Redemption.User.DisplayName}. Amount - {e.RewardRedeemed.Redemption.Reward.Cost}. ID {e.RewardRedeemed.Redemption.Reward.Id}");
            OnChannelPointsRedeemed?.Invoke(e.RewardRedeemed.Redemption.User.DisplayName, e.RewardRedeemed.Redemption.Reward.Prompt, Guid.Parse(e.RewardRedeemed.Redemption.Reward.Id));
        }
        
        private void _client_OnBitsReceivedV2(object sender, OnBitsReceivedV2Args e)
        {
            _logger.Debug($"Recieved Bits from {e.UserName}. Amount - {e.BitsUsed}.");
            OnBitsReceived?.Invoke(e.UserName, e.BitsUsed);
        }
        
        private void OnPubSubServiceConnected(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_userId))
            {
                _logger.Error("ChannelID is invalid!");
                return;
            }

            _logger.Info($"Trying to Connect to PubSub-Stream on {_appSettings.ChannelName}");

            _client.ListenToFollows(_userId);
            _client.ListenToBitsEventsV2(_userId);
            _client.ListenToChannelPoints(_userId);

            _logger.Debug($"Sending Auth Topics ...");

            if (_appSettings.ChannelName.ToLower() != _appSettings.BotName.ToLower())
            {
                // SendTopics accepts an oauth optionally, which is necessary for some topics
                _client.SendTopics("oauth:" + _appSettings.BroadcasterAuthObject.Access_Token);
            }
            else
            {
                // SendTopics accepts an oauth optionally, which is necessary for some topics
                _client.SendTopics("oauth:" + _appSettings.BotAuthObject.Access_Token);
            }
        }

        private void OnListenResponse(object sender, OnListenResponseArgs e)
        {
            if (!e.Successful)
            {
                _logger.Error($"Failed to listen on PubSub Service! Response: {e.Response.Error}");
            }
            else
            {
                _logger.Info($"Listening to {e.Topic} ... OK");
            }
            IsConnected = true;
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        public void Dispose()
        {
            if (!disposedValue)
            {
                if (_client == null)
                {
                    return;
                }
                _client.Disconnect();

                IsConnected = false;

                disposedValue = true;
            }
        }

        #endregion IDisposable Support
    }
}