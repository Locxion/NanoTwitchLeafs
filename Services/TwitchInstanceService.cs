using System;
using System.Drawing;
using System.Threading.Tasks;
using log4net;
using NanoTwitchLeafs.Enums;
using NanoTwitchLeafs.Interfaces;
using NanoTwitchLeafs.Objects;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using ChatMessage = NanoTwitchLeafs.Objects.ChatMessage;

namespace NanoTwitchLeafs.Services;

public class TwitchInstanceService : ITwitchInstanceService
{
    private readonly ISettingsService _settingsService;
    private readonly ITwitchAuthService _authService;
    public event EventHandler<ChatMessage> OnChatMessageReceived;
    public event EventHandler<TwitchEvent> OnTwitchEventReceived;
    private readonly ILog _logger = LogManager.GetLogger(typeof(TwitchInstanceService));

    public bool LoginFailed = false;
    
    private readonly TwitchClient _client = new ();
    private bool _isBroadcaster = false;
    private string _userName;
    private string _channel;

    public TwitchInstanceService(ISettingsService settingsService, ITwitchAuthService authService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _authService = authService;
    }

    /// <summary>
    /// Checks if the TwitchClient is connected to the Server.
    /// </summary>
    /// <returns>True if connection is successfull.</returns>
    public bool IsConnected()
    {
        return _client.IsConnected;
    }

    /// <summary>
    /// Connects to Twitch Servers and then Joints provided Channel
    /// </summary>
    /// <param name="username">Twitch User</param>
    /// <param name="channel">Channel to Join</param>
    /// <param name="auth">OAuth from Twitch User</param>
    public async void Connect(string username, string channel, string auth)
    {
        _logger.Info($"Connecting with TwitchClient {username} to Channel {channel}...");
        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(channel) ||
            string.IsNullOrWhiteSpace(auth))
        {
            _logger.Error("Error on Login Credentials!");
            return;
        }

        _userName = username;
        _channel = channel;
        if (string.Equals(_channel, _userName, StringComparison.CurrentCultureIgnoreCase))
            _isBroadcaster = true;
        
        var credentials = new ConnectionCredentials(username, auth);
        try
        {
            _client.Initialize(credentials, channel);
            _client.OnConnected += ClientOnConnected;
            _client.OnIncorrectLogin += ClientOnIncorrectLogin;
            _client.OnJoinedChannel += ClientOnJoinedChannel;
            _client.OnMessageReceived += ClientOnMessageReceived;
            _client.OnSendReceiveData += ClientOnSendReceiveData;
            _client.OnRaidNotification += ClientOnRaidNotification;

            await _client.ConnectAsync();
        }
        catch (Exception e)
        {
            _logger.Error("Twitch Chat connection Failed!");
            _logger.Error(e);
        }
        finally
        {
            await Disconnect();
        }
    }

    /// <summary>
    /// Sends Message to connected TwitchChannel
    /// </summary>
    /// <param name="message">Max 500 Char</param>
    public async Task SendMessage(string message)
    {
        if (!_client.IsConnected)
            return;
        await _client.SendMessageAsync(_settingsService.CurrentSettings.ChannelName, message);
        _logger.Info($"-> {message}");
    }
    /// <summary>
    /// Disconnects the Twitch Client und handles the Unsubscribing from Events
    /// </summary>
    public async Task Disconnect()
    {
        _logger.Info($"Disconnecting Twitch Client {_userName}");
        _client.OnConnected -= ClientOnConnected;
        _client.OnIncorrectLogin -= ClientOnIncorrectLogin;
        _client.OnJoinedChannel -= ClientOnJoinedChannel;
        _client.OnMessageReceived -= ClientOnMessageReceived;
        _client.OnSendReceiveData -= ClientOnSendReceiveData;
        await _client.DisconnectAsync();
    }
    private Task ClientOnRaidNotification(object sender, OnRaidNotificationArgs e)
    {
        _logger.Debug($"Received Raid form {e.RaidNotification.DisplayName}");
        var twitchEvent = new TwitchEvent(e.RaidNotification.DisplayName, Event.Raid, false,
            Convert.ToInt32(e.RaidNotification.MsgParamViewerCount));
        _logger.Debug($"Received {twitchEvent}");

        OnTwitchEventReceived?.Invoke(this, twitchEvent);
        return Task.CompletedTask;
    }

    private Task ClientOnSendReceiveData(object sender, OnSendReceiveDataArgs e)
    {
        _logger.Debug(e.Data);
        return Task.CompletedTask;
    }

    private Task ClientOnMessageReceived(object sender, OnMessageReceivedArgs e)
    {
        var message = new ChatMessage(e.ChatMessage.Username, e.ChatMessage.IsSubscriber, e.ChatMessage.IsModerator, e.ChatMessage.IsVip, e.ChatMessage.Message,
            ColorTranslator.FromHtml(e.ChatMessage.HexColor));
        _logger.Info($"{message.Username} - {message.Message}");
        OnChatMessageReceived?.Invoke(this, message);
        return Task.CompletedTask;
    }

    private async Task ClientOnJoinedChannel(object sender, OnJoinedChannelArgs e)
    {
        _logger.Info($"Joined Channel {_channel} successful!");
        if (_settingsService.CurrentSettings.Responses.StartupMessageActive)
        {
            string message = _settingsService.CurrentSettings.Responses.StartupResponse;
            if (message != "")
            {
                await SendMessage(message);
            }
            _logger.Info($"-> {message}");
        }
    }

    private async Task ClientOnIncorrectLogin(object sender, OnIncorrectLoginArgs e)
    {
        var newAuth = await _authService.GetAuthToken(HelperClass.GetTwitchApiCredentials(_settingsService.CurrentSettings),
            _isBroadcaster);
        if (_isBroadcaster)
        {
            _settingsService.CurrentSettings.BroadcasterAuthObject = newAuth;
        }
        else
        {
            _settingsService.CurrentSettings.BotAuthObject = newAuth;
        }
        _settingsService.SaveSettings();
        LoginFailed = true;
    }

    private async Task ClientOnConnected(object sender, OnConnectedEventArgs e)
    {
        _logger.Info($"Connection with {_userName} successful! Trying to join Channel: {_channel}...");
        await _client.JoinChannelAsync(_channel);
    }
}