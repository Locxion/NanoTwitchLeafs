﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.Extensions.Hosting;
using NanoTwitchLeafs.Enums;
using NanoTwitchLeafs.Interfaces;
using NanoTwitchLeafs.Objects;
using TwitchLib.Api;
using TwitchLib.Api.Core.Enums;
using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Websockets.Core.EventArgs;
using TwitchLib.EventSub.Websockets.Core.EventArgs.Channel;

namespace NanoTwitchLeafs.Services;
public class TwitchEventSubService : ITwitchEventSubService
{
    public event EventHandler<TwitchEvent> OnTwitchEvent;
    private readonly ILog _logger = LogManager.GetLogger(typeof(TwitchEventSubService));
    private readonly EventSubWebsocketClient _eventSubWebsocketClient;
    private readonly ISettingsService _settingsService;
    private readonly ITwitchAuthService _twitchAuthService;
    private readonly TwitchAPI _twitchApi = new();
    private bool _isConnected;
    public TwitchEventSubService(EventSubWebsocketClient eventSubWebsocketClient, ISettingsService settingsService, ITwitchAuthService twitchAuthService)
    {
        _eventSubWebsocketClient = eventSubWebsocketClient ?? throw new ArgumentNullException(nameof(eventSubWebsocketClient));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _twitchAuthService = twitchAuthService ?? throw new ArgumentNullException(nameof(twitchAuthService));
        _eventSubWebsocketClient.WebsocketConnected += EventSubWebsocketClientOnWebsocketConnected;
        _eventSubWebsocketClient.WebsocketDisconnected += EventSubWebsocketClientOnWebsocketDisconnected;
        _eventSubWebsocketClient.WebsocketReconnected += EventSubWebsocketClientOnWebsocketReconnected;
        _eventSubWebsocketClient.ErrorOccurred += EventSubWebsocketClientOnErrorOccurred;
        _eventSubWebsocketClient.ChannelFollow += EventSubWebsocketClientOnChannelFollow;
        
        _twitchApi.Settings.ClientId = HelperClass.GetTwitchApiCredentials(_settingsService.CurrentSettings).ClientId;
    }

    private Task EventSubWebsocketClientOnChannelFollow(object sender, ChannelFollowArgs args)
    {
        _logger.Debug($"{args.Notification.Payload.Event.UserName} followed {args.Notification.Payload.Event.BroadcasterUserName} at {args.Notification.Payload.Event.FollowedAt}");
        var newEvent = new TwitchEvent(args.Notification.Payload.Event.UserName, Event.NewFollower);
        OnTwitchEvent?.Invoke(this, newEvent);
        return Task.CompletedTask;
    }

    private Task EventSubWebsocketClientOnErrorOccurred(object sender, ErrorOccuredArgs args)
    {
        _logger.Error($"Websocket {_eventSubWebsocketClient.SessionId} - Error occurred!");
        _logger.Error(args.Exception);
        return Task.CompletedTask;
    }

    private Task EventSubWebsocketClientOnWebsocketReconnected(object sender, EventArgs args)
    {
        _logger.Debug($"Websocket {_eventSubWebsocketClient.SessionId} reconnected");
        return Task.CompletedTask;
    }

    private async Task EventSubWebsocketClientOnWebsocketDisconnected(object sender, EventArgs args)
    {
        _logger.Warn($"Websocket {_eventSubWebsocketClient.SessionId} disconnected!");
        _isConnected = false;
        // Don't do this in production. You should implement a better reconnect strategy with exponential backoff
        while (!await _eventSubWebsocketClient.ReconnectAsync())
        {
            _logger.Error("Websocket reconnect failed!");
            await Task.Delay(1000);
        }
    }

    private async Task EventSubWebsocketClientOnWebsocketConnected(object sender, WebsocketConnectedArgs args)
    {
        _logger.Info("Successfully connected to Twitch EventSub Websocket!");
        _logger.Debug($"Websocket {_eventSubWebsocketClient.SessionId} connected!");
        _isConnected = true;
        if (args.IsRequestedReconnect)
            return;
        _logger.Info("Try to Subscribe to EventSub Endpoints ...");

        var userId = await GetUserId(_settingsService.CurrentSettings.ChannelName);

        var condition = new Dictionary<string, string> { { "broadcaster_user_id", userId }, {"moderator_user_id", userId} };
        await _twitchApi.Helix.EventSub.CreateEventSubSubscriptionAsync("channel.follow", "2", condition, EventSubTransportMethod.Websocket,
            _eventSubWebsocketClient.SessionId, accessToken: _settingsService.CurrentSettings.BroadcasterAuthObject.Access_Token);
    }

    private async Task<string> GetUserId(string channel)
    {
        return await _twitchAuthService.GetUserId(channel);
    }

    public bool IsConnected()
    {
        return _isConnected;
    }

    public async Task Connect()
    {
        _logger.Info("Connecting to Twitch EventSub Websocket ...");
        await _eventSubWebsocketClient.ConnectAsync();
    }
    public async Task Disconnect()
    {
        await _eventSubWebsocketClient.DisconnectAsync();
    }
}
