using System;
using log4net;
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
    private readonly TwitchPubSub _client = new();
    private TwitchAPI _api;
    private string _userId = "";
    private bool _connected;

    public TwitchPubSubService(ISettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
    }

    public bool IsConnected()
    {
        return _connected;
    }

    /// <summary>
    /// Connects to the Twitch PubSub Service and Events
    /// </summary>
    public async void Connect()
    {
        _api = new TwitchAPI
        {
            Settings =
            {
                ClientId = HelperClass.GetTwitchApiCredentials(_settingsService.CurrentSettings).ClientId
            }
        };

        _userId = await HelperClass.GetUserId(_api, _settingsService.CurrentSettings, _settingsService.CurrentSettings.ChannelName);
        if (_userId == null)
        {
            _logger.Error($"Could not get User Id for User/Channel {_settingsService.CurrentSettings.ChannelName} from TwitchApi!");
            // TODO Move MSG Box into Main Window
            //MessageBox.Show(Properties.Resources.Code_PubSub_MessageBox_ConnectError, Properties.Resources.General_MessageBox_Error_Title, MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            _client.OnPubSubServiceError += OnPubSubServiceError;
            _client.OnPubSubServiceConnected += OnPubSubServiceConnected;
            _client.OnListenResponse += OnListenResponse;
            _client.OnBitsReceivedV2 += OnBitsReceivedV2;
            _client.OnChannelPointsRewardRedeemed += OnChannelPointsRewardRedeemed;
        
            _client.Connect();
        }
        catch (Exception e)
        {
            _logger.Error("Twitch PubSub connection failed!");
            _logger.Error(e);
        }
        finally
        {
            Disconnect();
        }
    }

    private void OnPubSubServiceError(object sender, OnPubSubServiceErrorArgs e)
    {
        _logger.Error("Twitch PubSub connection failed!");
        _logger.Error(e.Exception);
        Disconnect();
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

    private void OnPubSubServiceConnected(object sender, EventArgs e)
    {
        _connected = true;
        _logger.Info("Connected to Twitch PubSub Service!");
        _logger.Info($"Trying to Connect to PubSub-Stream on {_settingsService.CurrentSettings.ChannelName}");

        _client.ListenToBitsEventsV2(_userId);
        _client.ListenToChannelPoints(_userId);
    }

    /// <summary>
    /// Disconnects from Twitch PubSub Service and Events
    /// </summary>
    public void Disconnect()
    {
        _client.OnPubSubServiceError -= OnPubSubServiceError;
        _client.OnPubSubServiceConnected += OnPubSubServiceConnected;
        _client.OnListenResponse += OnListenResponse;
        _client.OnBitsReceivedV2 += OnBitsReceivedV2;
        _client.OnChannelPointsRewardRedeemed += OnChannelPointsRewardRedeemed;
        _client.Disconnect();
        _connected = false;
    }
}