using System;
using System.Threading.Tasks;
using log4net;
using NanoTwitchLeafs.Interfaces;
using ChatMessage = NanoTwitchLeafs.Objects.ChatMessage;

namespace NanoTwitchLeafs.Services;

class StreamingPlatformService : IStreamingPlatformService
{
    public event EventHandler<ChatMessage> OnMessageReceived;
    private readonly ISettingsService _settingsService;
    private readonly ITwitchInstanceService _twitchInstanceServiceBroadcaster;
    private readonly ITwitchInstanceService _twitchInstanceServiceChatBot;
    private readonly ITwitchPubSubService _twitchPubSubService;
    private readonly ITwitchEventSubService _twitchEventSubService;
    private readonly ILog _logger = LogManager.GetLogger(typeof(StreamingPlatformService));
    private bool _isConnected;

    public StreamingPlatformService(ISettingsService settingsService,ITwitchInstanceService twitchInstanceServiceBroadcaster, ITwitchInstanceService twitchInstanceServiceChatBot, ITwitchPubSubService twitchPubSubService, ITwitchEventSubService twitchEventSubService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _twitchInstanceServiceBroadcaster = twitchInstanceServiceBroadcaster ?? throw new ArgumentNullException(nameof(twitchInstanceServiceBroadcaster));
        _twitchInstanceServiceChatBot = twitchInstanceServiceChatBot ?? throw new ArgumentNullException(nameof(twitchInstanceServiceChatBot));
        _twitchPubSubService = twitchPubSubService ?? throw new ArgumentNullException(nameof(twitchPubSubService));
        _twitchEventSubService = twitchEventSubService ?? throw new ArgumentNullException(nameof(twitchEventSubService));
    }

    public async Task Connect()
    {
        await ConnectTwitchServices();
        _isConnected = true;
    }

    private async Task ConnectTwitchServices()
    {
        await _twitchInstanceServiceChatBot.Connect(_settingsService.CurrentSettings.BotName, _settingsService.CurrentSettings.ChannelName, _settingsService.CurrentSettings.BotAuthObject);
        _twitchInstanceServiceChatBot.OnChatMessageReceived += TwitchInstanceServiceChatBotOnChatMessageReceived;
        
        if (_settingsService.CurrentSettings.ChannelName != _settingsService.CurrentSettings.BotName)
        {
            _logger.Warn("Double Account setup detected. Connecting with Broadcaster Account ...");
            await _twitchInstanceServiceBroadcaster.Connect(_settingsService.CurrentSettings.ChannelName, _settingsService.CurrentSettings.ChannelName, _settingsService.CurrentSettings.BroadcasterAuthObject, true);
        }

        while (!_twitchInstanceServiceChatBot.IsConnected())
        {
            _logger.Debug("Waiting for TwitchClient connection ...");
            await Task.Delay(1500);
        }
        
        await _twitchPubSubService.Connect();
        await _twitchEventSubService.Connect();
    }

    private void TwitchInstanceServiceChatBotOnChatMessageReceived(object sender, ChatMessage message)
    {
        RelayMessage(message);
    }

    private void RelayMessage(ChatMessage message)
    {
        OnMessageReceived?.Invoke(this, message);
    }

    public async Task Disconnect()
    {
        DisconnectTwitchServices();
        _isConnected = false;
    }

    private void DisconnectTwitchServices()
    {
        if (_twitchInstanceServiceChatBot.IsConnected())
            _twitchInstanceServiceChatBot.Disconnect();
        if (_twitchInstanceServiceBroadcaster.IsConnected())
            _twitchInstanceServiceBroadcaster.Disconnect();
        if(_twitchPubSubService.IsConnected())
            _twitchPubSubService.Disconnect();
        if(_twitchEventSubService.IsConnected())
            _twitchEventSubService.Disconnect();
    }

    public bool IsConnected()
    {
        return _isConnected;
    }

    public async Task SendMessage(string message)
    {
        //Twitch
        await _twitchInstanceServiceChatBot.SendMessage(message);
    }
}