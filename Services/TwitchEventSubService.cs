using System;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.Extensions.Hosting;
using NanoTwitchLeafs.Enums;
using NanoTwitchLeafs.Interfaces;
using NanoTwitchLeafs.Objects;
using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Websockets.Core.EventArgs;
using TwitchLib.EventSub.Websockets.Core.EventArgs.Channel;

namespace NanoTwitchLeafs.Services;
public class TwitchEventSubService : BackgroundService, ITwitchEventSubService
{
    public event EventHandler<TwitchEvent> OnTwitchEvent;
    private readonly ILog _logger = LogManager.GetLogger(typeof(TwitchEventSubService));
    private readonly EventSubWebsocketClient _eventSubWebsocketClient;
    private bool _IsConnected;

    public TwitchEventSubService(EventSubWebsocketClient eventSubWebsocketClient)
    {
        _eventSubWebsocketClient = eventSubWebsocketClient ?? throw new ArgumentNullException(nameof(eventSubWebsocketClient));
        _eventSubWebsocketClient.WebsocketConnected += EventSubWebsocketClientOnWebsocketConnected;
        _eventSubWebsocketClient.WebsocketDisconnected += EventSubWebsocketClientOnWebsocketDisconnected;
        _eventSubWebsocketClient.WebsocketReconnected += EventSubWebsocketClientOnWebsocketReconnected;
        _eventSubWebsocketClient.ErrorOccurred += EventSubWebsocketClientOnErrorOccurred;
        _eventSubWebsocketClient.ChannelFollow += EventSubWebsocketClientOnChannelFollow;
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
        _logger.Warn(args.ToString());
        _IsConnected = false;
        // Don't do this in production. You should implement a better reconnect strategy with exponential backoff
        while (!await _eventSubWebsocketClient.ReconnectAsync())
        {
            _logger.Error("Websocket reconnect failed!");
            await Task.Delay(1000);
        }
    }

    private Task EventSubWebsocketClientOnWebsocketConnected(object sender, WebsocketConnectedArgs args)
    {
        _logger.Info($"Websocket {_eventSubWebsocketClient.SessionId} connected!");
        _IsConnected = true;
        if (!args.IsRequestedReconnect)
        {
           // sub here
        }
        return Task.CompletedTask;
    }

    public bool IsConnected()
    {
        return _IsConnected;
    }

    public async Task Connect()
    {
        await ExecuteAsync(new CancellationToken());
    }
    public async Task Disconnect()
    {
        await StopAsync(new CancellationToken());
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _eventSubWebsocketClient.ConnectAsync();
    }
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _eventSubWebsocketClient.DisconnectAsync();
    }
}
