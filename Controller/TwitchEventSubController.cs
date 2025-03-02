using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.Extensions.DependencyInjection;
using NanoTwitchLeafs.Objects;
using TwitchLib.Api;
using TwitchLib.Api.Core.Enums;
using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Websockets.Core.EventArgs;
using TwitchLib.EventSub.Websockets.Core.EventArgs.Channel;

namespace NanoTwitchLeafs.Controller;
public class TwitchEventSubController : IDisposable
{
    private readonly ILog _logger = LogManager.GetLogger(typeof(TwitchEventSubController));
    private readonly EventSubWebsocketClient _eventSubWebsocketClient;
    private TwitchAPI _api;
    private readonly AppSettings _appSettings;
    public event OnFollow OnFollow;

    public event OnBitsReceived OnBitsReceived;

    public event OnChannelPointsRedeemed OnChannelPointsRedeemed;
    public TwitchEventSubController(ServiceProvider serviceProvider, AppSettings appSettings)
    {
        _appSettings = appSettings;
        _eventSubWebsocketClient = serviceProvider.GetRequiredService<EventSubWebsocketClient>();
        _eventSubWebsocketClient.WebsocketConnected += OnWebsocketConnected;
        _eventSubWebsocketClient.WebsocketDisconnected += OnWebsocketDisconnected;
        _eventSubWebsocketClient.WebsocketReconnected += OnWebsocketReconnected;
        _eventSubWebsocketClient.ErrorOccurred += OnErrorOccurred;

        _eventSubWebsocketClient.ChannelFollow += OnChannelFollow;
    }
    public async Task StartAsync()
        {
            await _eventSubWebsocketClient.ConnectAsync();
        }

        public async Task StopAsync()
        {
            await _eventSubWebsocketClient.DisconnectAsync();
        }

        private async Task  OnWebsocketConnected(object? sender, WebsocketConnectedArgs e)
        { 
            /*
            Subscribe to topics via the TwitchApi.Helix.EventSub object, this example shows how to subscribe to the channel follow event used in the example above.

            var conditions = new Dictionary<string, string>()
            {
                { "broadcaster_user_id", someUserId }
            };
            var subscriptionResponse = await TwitchApi.Helix.EventSub.CreateEventSubSubscriptionAsync("channel.follow", "2", conditions,
            EventSubTransportMethod.Websocket, _eventSubWebsocketClient.SessionId);

            You can find more examples on the subscription types and their requirements here https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/
            Prerequisite: Twitchlib.Api nuget package installed (included in the Twitchlib package automatically)
            */
            _logger.Info($"Websocket {_eventSubWebsocketClient.SessionId} connected!");

            if (!e.IsRequestedReconnect)
            {
                _api = new TwitchAPI();
			
                _api.Settings.ClientId = HelperClass.GetTwitchApiCredentials(_appSettings).ClientId;
                _api.Settings.AccessToken = _appSettings.BotAuthObject.Access_Token;
                var broadcasterId = await HelperClass.GetUserId(_api, _appSettings, _appSettings.ChannelName);
                var conditions = new Dictionary<string, string>()
                {
                    { "broadcaster_user_id", broadcasterId  }
                };

                var followSubscriptionResponse = await _api.Helix.EventSub.CreateEventSubSubscriptionAsync("channel.follow", "2", conditions,
                    EventSubTransportMethod.Websocket, _eventSubWebsocketClient.SessionId);
                _logger.Debug(followSubscriptionResponse.ToString());
            }
        }

        private async Task OnWebsocketDisconnected(object? sender, EventArgs e)
        {
            _logger.Error($"Websocket {_eventSubWebsocketClient.SessionId} disconnected!");
            _logger.Error($"Websocket disconnected cause: {e}");

            // Don't do this in production. You should implement a better reconnect strategy with exponential backoff
            while (!await _eventSubWebsocketClient.ReconnectAsync())
            {
                _logger.Error("Websocket reconnect failed!");
                await Task.Delay(1000);
            }
        }

        private async Task OnWebsocketReconnected(object? sender, EventArgs e)
        {
            _logger.Warn($"Websocket {_eventSubWebsocketClient.SessionId} reconnected");
        }      
      
        private async Task OnErrorOccurred(object? sender, ErrorOccuredArgs e)
        {
            _logger.Error($"Websocket {_eventSubWebsocketClient.SessionId} - Error occurred!");
        }

        private async Task OnChannelFollow(object? sender, ChannelFollowArgs e)
        {
            var eventData = e.Notification.Payload.Event;
            OnFollow?.Invoke(eventData.UserName);
            _logger.Info($"{eventData.UserName} followed {eventData.BroadcasterUserName} at {eventData.FollowedAt}");
        }
        public void Dispose()
        {
            _eventSubWebsocketClient.WebsocketConnected -= OnWebsocketConnected;
            _eventSubWebsocketClient.WebsocketDisconnected -= OnWebsocketDisconnected;
            _eventSubWebsocketClient.WebsocketReconnected -= OnWebsocketReconnected;
            _eventSubWebsocketClient.ErrorOccurred -= OnErrorOccurred;

            _eventSubWebsocketClient.ChannelFollow -= OnChannelFollow;
            // TODO release managed resources here
        }
}