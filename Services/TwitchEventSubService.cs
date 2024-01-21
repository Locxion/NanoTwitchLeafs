using System;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.Extensions.Hosting;
using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Websockets.Core.EventArgs;
using TwitchLib.EventSub.Websockets.Core.EventArgs.Channel;

namespace NanoTwitchLeafs.Services;


public delegate void OnFollow(string username);

public class TwitchEventSubService : BackgroundService
{
    public event OnFollow OnFollow;
    private readonly ILog _logger = LogManager.GetLogger(typeof(TwitchEventSubService));
    private readonly EventSubWebsocketClient _eventSubWebsocketClient;

    public TwitchEventSubService(EventSubWebsocketClient eventSubWebsocketClient)
    {
        _eventSubWebsocketClient = eventSubWebsocketClient ?? throw new ArgumentNullException(nameof(eventSubWebsocketClient));
        _eventSubWebsocketClient.WebsocketConnected += OnWebsocketConnected;
        _eventSubWebsocketClient.WebsocketDisconnected += OnWebsocketDisconnected;
        _eventSubWebsocketClient.WebsocketReconnected += OnWebsocketReconnected;
        _eventSubWebsocketClient.ErrorOccurred += OnErrorOccurred;
        _eventSubWebsocketClient.ChannelFollow += OnChannelFollow;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _eventSubWebsocketClient.ConnectAsync();
    }
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _eventSubWebsocketClient.DisconnectAsync();
    }

    private void OnWebsocketConnected(object? sender, WebsocketConnectedArgs e)
    {
        _logger.Info($"Websocket {_eventSubWebsocketClient.SessionId} connected!");

        if (!e.IsRequestedReconnect)
        {
            // subscribe to topics
        }
    }

    private async void OnWebsocketDisconnected(object? sender, EventArgs e)
    {
        _logger.Error($"Websocket {_eventSubWebsocketClient.SessionId} disconnected!");

        // Don't do this in production. You should implement a better reconnect strategy with exponential backoff
        while (!await _eventSubWebsocketClient.ReconnectAsync())
        {
            _logger.Error("Websocket reconnect failed!");
            await Task.Delay(1000);
        }
    }

    private void OnWebsocketReconnected(object? sender, EventArgs e)
    {
        _logger.Warn($"Websocket {_eventSubWebsocketClient.SessionId} reconnected");
    }      
  
    private void OnErrorOccurred(object? sender, ErrorOccuredArgs e)
    {
        _logger.Error($"Websocket {_eventSubWebsocketClient.SessionId} - Error occurred!");
    }

    private void OnChannelFollow(object? sender, ChannelFollowArgs e)
    {
        var eventData = e.Notification.Payload.Event;
        OnFollow?.Invoke(eventData.UserName);
        _logger.Info($"{eventData.UserName} followed {eventData.BroadcasterUserName} at {eventData.FollowedAt}");
    }
}
