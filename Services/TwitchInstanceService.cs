using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using log4net;
using NanoTwitchLeafs.Colors;
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
    private readonly TwitchClient _client = new ();
    private bool _isBroadcaster;
    private string _userName;
    private string _channel;
    private OAuthObject _oAuthObject;
    private bool _isConnected;
    private bool _firstTry = true;

    public TwitchInstanceService(ISettingsService settingsService, ITwitchAuthService authService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _authService = authService;
    }

    /// <summary>
    /// Checks if the TwitchClient is connected to the Server.
    /// </summary>
    /// <returns>True if connection is successful.</returns>
    public bool IsConnected()
    {
        return _isConnected;
    }

    /// <summary>
    /// Checks if the TwitchClient is a Broadcaster Account (Only for Events, no Chat ability)
    /// </summary>
    /// <returns></returns>
    public bool IsBroadcaster()
    {
        return _isBroadcaster;
    }

    /// <summary>
    /// Connects to Twitch Servers and then Joints provided Channel
    /// </summary>
    /// <param name="username">Twitch User</param>
    /// <param name="channel">Channel to Join</param>
    /// <param name="oAuthObject"></param>
    /// <param name="isBroadcaster">If the Instance is a Bot or a Broadcaster Instance</param>
    public async Task Connect(string username, string channel, OAuthObject oAuthObject, bool isBroadcaster)
    {
        _oAuthObject = oAuthObject;
        _logger.Info($"Connecting with TwitchClient {username} to Channel {channel}...");
        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(channel) ||
            string.IsNullOrWhiteSpace(oAuthObject.Access_Token))
        {
            _logger.Error("Error on Login Credentials!");
            return;
        }

        _userName = username;
        _channel = channel;
        if (string.Equals(_channel, _userName, StringComparison.CurrentCultureIgnoreCase))
            _isBroadcaster = true;
        
        var credentials = new ConnectionCredentials(username, oAuthObject.Access_Token);
        try
        {
            _client.Initialize(credentials, channel);
            _client.OnConnected += ClientOnConnected;
            _client.OnIncorrectLogin += ClientOnIncorrectLogin;
            if (!isBroadcaster)
            {
                _client.OnJoinedChannel += ClientOnJoinedChannel;
                _client.OnMessageReceived += ClientOnMessageReceived;
            }
            _client.OnSendReceiveData += ClientOnSendReceiveData;
            _client.OnRaidNotification += ClientOnRaidNotification;

            await _client.ConnectAsync();
        }
        catch (Exception e)
        {
            _logger.Error("Twitch Chat connection Failed!");
            _logger.Error(e);
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

        var chatMessage = new ChatMessage(StreamingPlatformEnum.Sent, _settingsService.CurrentSettings.BotName, true, true, true, message,ColorConverting.RgbToDrawingColor(ColorConverting.GenerateRandomRgbColor()));
        
        OnChatMessageReceived.Invoke(this,chatMessage);
        
        await _client.SendMessageAsync(_settingsService.CurrentSettings.ChannelName, message);
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
        _isConnected = false;
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
        var message = new ChatMessage(StreamingPlatformEnum.Twitch, e.ChatMessage.Username, e.ChatMessage.IsSubscriber, e.ChatMessage.IsModerator, e.ChatMessage.IsVip, e.ChatMessage.Message,
            ColorTranslator.FromHtml(e.ChatMessage.HexColor));
        _logger.Debug($"{message.Username} - {message.Message}");
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
        }
    }

    private async Task ClientOnIncorrectLogin(object sender, OnIncorrectLoginArgs e)
    {
        _isConnected = false;
        _logger.Warn($"Got IncorrectLogin from Twitch for {e.Exception.Username}");

        if (!_firstTry)
        {
            _logger.Error("Could not connect to Twitch Chat! Please re-link your Twitch Account!");
            MessageBox.Show(Properties.Resources.Code_Twitch_MessageBox_LoginIncorrect, Properties.Resources.General_MessageBox_Error_Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        
        var newAuth = await _authService.RefreshToken(_oAuthObject);

        if (_settingsService.CurrentSettings.ChannelName.ToLower() == _settingsService.CurrentSettings.BotName.ToLower())
        {
            _settingsService.CurrentSettings.BroadcasterAuthObject = newAuth;
            _settingsService.CurrentSettings.BotAuthObject = newAuth;
        }
        else
        {
            if (_isBroadcaster)
            {
                _settingsService.CurrentSettings.BroadcasterAuthObject = newAuth;
            }
            else
            {
                _settingsService.CurrentSettings.BotAuthObject = newAuth;
            } 
        }
        _oAuthObject = newAuth;
        _settingsService.SaveSettings();
        _firstTry = false;
        await Connect(_userName, _settingsService.CurrentSettings.ChannelName, _oAuthObject, _isBroadcaster);
    }

    private async Task ClientOnConnected(object sender, OnConnectedEventArgs e)
    {
        _isConnected = true;
        _logger.Info($"Connection with {_userName} successful! Trying to join Channel: {_channel}...");
        await _client.JoinChannelAsync(_channel);
    }
}