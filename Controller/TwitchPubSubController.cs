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
    public delegate void OnFollow(string username);

    public delegate void OnBitsReceived(string username, int amount);

    public delegate void OnChannelPointsRedeemed(string username, string promt, Guid guid);

    public class TwitchPubSubController : IDisposable
    {
        private TwitchPubSub _client;
        private readonly ILog _logger = LogManager.GetLogger(typeof(TwitchPubSubController));
        private AppSettings _appSettings;

        private TwitchAPI _api;

        public event OnFollow OnFollow;

        public event OnBitsReceived OnBitsReceived;

        public event OnChannelPointsRedeemed OnChannelPointsRedeemed;

        private string ChannelID = "";

        public bool isConnected = false;

        public async void Connect(AppSettings appSettings)
        {
            _client = new TwitchPubSub();

            _appSettings = appSettings;

            _client.OnPubSubServiceConnected += OnPubSubServiceConnected;
            _client.OnListenResponse += OnListenResponse;
            _client.OnFollow += _client_OnFollow;
            _client.OnBitsReceivedV2 += _client_OnBitsReceivedV2;
            _client.OnChannelPointsRewardRedeemed += _client_OnChannelPointsRewardRedeemed;

            _api = new TwitchAPI();
            _api.Settings.ClientId = _appSettings.TwitchClientId;

            ChannelID = await GetChannelId(_appSettings.ChannelName);
            if (ChannelID == null)
                return;

            _client.Connect();

            var followerService = new FollowerService(_api, 30);
            followerService.OnNewFollowersDetected += FollowerService_OnNewFollowersDetected;

            followerService.SetChannelsById(new List<string> { ChannelID });
        }

        private void _client_OnChannelPointsRewardRedeemed(object sender, OnChannelPointsRewardRedeemedArgs e)
        {
            _logger.Debug($"Recieved ChannelPoints Reward from {e.RewardRedeemed.Redemption.User.DisplayName}. Amount - {e.RewardRedeemed.Redemption.Reward.Cost}. ID {e.RewardRedeemed.Redemption.Reward.Id}");
            OnChannelPointsRedeemed?.Invoke(e.RewardRedeemed.Redemption.User.DisplayName, e.RewardRedeemed.Redemption.Reward.Prompt, Guid.Parse(e.RewardRedeemed.Redemption.Reward.Id));
        }

        private void FollowerService_OnNewFollowersDetected(object sender, OnNewFollowersDetectedArgs e)
        {
            if (e.NewFollowers.Count > 10)
            {
                return;
            }
            OnFollow?.Invoke(e.NewFollowers.First().FromUserName);
        }

        private async Task<string> GetChannelId(string channelName)
        {
            try
            {
                var user = await _api.Helix.Users.GetUsersAsync(null, new List<string> { channelName.ToLower() },
                    _appSettings.BroadcasterAuthObject.Access_Token);
                return user.Users[0].Id;
            }
            catch (BadScopeException e)
            {
                MessageBox.Show(Properties.Resources.Code_PubSub_MessageBox_ConnectError,
                    Properties.Resources.General_MessageBox_Error_Title, MessageBoxButton.OK, MessageBoxImage.Error);
                _logger.Error("Could not connect to PubSub due Wrong Access Credentials", e);
                return null;
            }
            catch (Exception e)
            {
                _logger.Error("Could not connect to PubSub", e);
                return null;
            }
        }

        private void _client_OnBitsReceivedV2(object sender, OnBitsReceivedV2Args e)
        {
            _logger.Debug($"Recieved Bits from {e.UserName}. Amount - {e.BitsUsed}.");
            OnBitsReceived?.Invoke(e.UserName, e.BitsUsed);
        }

        private void _client_OnFollow(object sender, OnFollowArgs e)
        {
            _logger.Debug($"Recieved Follow from {e.Username}.");
            OnFollow?.Invoke(e.Username);
        }

        private void OnPubSubServiceConnected(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ChannelID))
            {
                _logger.Error("ChannelID is invalid!");
                return;
            }

            _logger.Info($"Trying to Connect to PubSub-Stream on {_appSettings.ChannelName}");

            _client.ListenToFollows(ChannelID);
            _client.ListenToBitsEventsV2(ChannelID);
            _client.ListenToChannelPoints(ChannelID);

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
            isConnected = true;
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

                isConnected = false;

                disposedValue = true;
            }
        }

        #endregion IDisposable Support
    }
}