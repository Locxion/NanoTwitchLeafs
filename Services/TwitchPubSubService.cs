using System;
using System.Threading.Tasks;
using log4net;
using Microsoft.Extensions.Logging;
using NanoTwitchLeafs.Enums;
using NanoTwitchLeafs.Interfaces;
using NanoTwitchLeafs.Objects;
using TwitchLib.Api;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;

namespace NanoTwitchLeafs.Services;

class TwitchPubSubService : ITwitchPubSubService
{
    public event EventHandler<TwitchEvent> OnTwitchEventReceived;

    private readonly ILog _logger = LogManager.GetLogger(typeof(TwitchPubSubService));
    private readonly ISettingsService _settingsService;
    private readonly TwitchPubSub _client;
    private TwitchAPI _api;
    private string _userId = "";
    private bool _connected;

    public TwitchPubSubService(ISettingsService settingsService, ILogger<TwitchPubSub> logger)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _client = new TwitchPubSub(logger);
    }

    public bool IsConnected()
    {
        return _connected;
    }

    /// <summary>
    /// Connects to the Twitch PubSub Service and Events
    /// </summary>
    public async Task Connect()
    {
        _api = new TwitchAPI
        {
            Settings =
            {
                ClientId = HelperClass.GetTwitchApiCredentials(_settingsService.CurrentSettings).ClientId
            }
        };
        _logger.Debug($"Getting UserID for Channel: {_settingsService.CurrentSettings.ChannelName}");
        _userId = await HelperClass.GetUserId(_api, _settingsService.CurrentSettings.BroadcasterAuthObject.Access_Token, _settingsService.CurrentSettings.ChannelName);
        if (_userId == null)
        {
            _logger.Error($"Could not get User Id for User/Channel {_settingsService.CurrentSettings.ChannelName} from TwitchApi!");
            // TODO Move MSG Box into Main Window
            //MessageBox.Show(Properties.Resources.Code_PubSub_MessageBox_ConnectError, Properties.Resources.General_MessageBox_Error_Title, MessageBoxButton.OK, MessageBoxImage.Error);
            await Disconnect();
            return;
        }

        try
        {
            _client.OnPubSubServiceError += OnPubSubServiceError;
            _client.OnPubSubServiceConnected += OnPubSubServiceConnected;
            _client.OnListenResponse += OnListenResponse;
            _client.OnBitsReceivedV2 += OnBitsReceivedV2;
            _client.OnChannelPointsRewardRedeemed += OnChannelPointsRewardRedeemed;
            _logger.Debug($"Connecting to Twitch PubSub Service with UserId: {_userId}");
            await _client.ConnectAsync();
        }
        catch (Exception e)
        {
            _logger.Error("Twitch PubSub connection failed!");
            _logger.Error(e);
            await Disconnect();
        }
    }

    private async void OnPubSubServiceError(object sender, OnPubSubServiceErrorArgs e)
    {
        _logger.Error("Twitch PubSub connection failed!");
        _logger.Error(e.Exception);
        await Disconnect();
    }

    private void OnChannelPointsRewardRedeemed(object sender, OnChannelPointsRewardRedeemedArgs e)
    {
        var newTwitchEvent = new TwitchEvent(e.RewardRedeemed.Redemption.User.DisplayName, Event.ChannelPointsEvent,
            false, 1, e.RewardRedeemed.Redemption.UserInput);
        _logger.Debug($"Received {newTwitchEvent}");
        
        OnTwitchEventReceived?.Invoke(this, newTwitchEvent);
    }

    private void OnBitsReceivedV2(object sender, OnBitsReceivedV2Args e)
    {
        var newTwitchEvent = new TwitchEvent(e.UserName, Event.BitsCheered,
            e.IsAnonymous, e.BitsUsed, e.ChatMessage);
        _logger.Debug($"Received {newTwitchEvent}");

        OnTwitchEventReceived?.Invoke(this, newTwitchEvent);
    }

    private void OnListenResponse(object sender, OnListenResponseArgs e)
    {
        if (!e.Successful)
        {
            _logger.Error($"Failed to listen on PubSub Stream! Response: {e.Response.Error}");
        }
        else
        {
            _logger.Info($"Listening to {e.Topic} ... OK");
        }
    }

    private async void OnPubSubServiceConnected(object sender, EventArgs e)
    {
        _connected = true;
        _logger.Info("Connected to Twitch PubSub Service!");
        _logger.Info($"Trying to Connect to PubSub-Streams on Channel {_settingsService.CurrentSettings.ChannelName} Id: {_userId}");

        _client.ListenToBitsEventsV2(_userId);
        _client.ListenToChannelPoints(_userId);
        await _client.SendTopicsAsync(_settingsService.CurrentSettings.BroadcasterAuthObject.Access_Token);
    }

    /// <summary>
    /// Disconnects from Twitch PubSub Service and Events
    /// </summary>
    public async Task Disconnect()
    {
        _client.OnPubSubServiceError -= OnPubSubServiceError;
        _client.OnPubSubServiceConnected += OnPubSubServiceConnected;
        _client.OnListenResponse += OnListenResponse;
        _client.OnBitsReceivedV2 += OnBitsReceivedV2;
        _client.OnChannelPointsRewardRedeemed += OnChannelPointsRewardRedeemed;
        await _client.SendTopicsAsync(_settingsService.CurrentSettings.BroadcasterAuthObject.Access_Token, true);
        await _client.DisconnectAsync();
        _connected = false;
    }
}