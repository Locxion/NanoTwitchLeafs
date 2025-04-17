using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.Extensions.DependencyInjection;
using NanoTwitchLeafs.Objects;
using TwitchLib.Api;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Core.Exceptions;
using TwitchLib.EventSub.Core.SubscriptionTypes.Channel;
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
    public bool isConnected;
    public event OnFollow OnFollow;
    public event OnSubscription OnSubscription;
    public event OnRaid OnRaid;
    public event OnGiftSubscription OnGiftSubscription;
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
        _eventSubWebsocketClient.ChannelSubscribe += OnChannelSubscribe;
        _eventSubWebsocketClient.ChannelSubscriptionMessage += OnChannelSubscriptionMessage;
        _eventSubWebsocketClient.ChannelSubscriptionGift += OnChannelSubscriptionGift;
        _eventSubWebsocketClient.ChannelRaid += OnChannelRaid;
        _eventSubWebsocketClient.ChannelCheer += OnChannelCheer;
        _eventSubWebsocketClient.ChannelPointsCustomRewardRedemptionAdd += OnChannelPointsCustomRewardRedemptionAdd;
        _eventSubWebsocketClient.ChannelPointsCustomRewardRedemptionUpdate += OnChannelPointsCustomRewardRedemptionUpdate;
    }

    private async Task OnChannelPointsCustomRewardRedemptionUpdate(object sender, ChannelPointsCustomRewardRedemptionArgs args)
    {
        var eventData = args.Notification.Payload.Event;
        HandleChannelPointsEvent(eventData);
    }

    private async Task OnChannelPointsCustomRewardRedemptionAdd(object sender, ChannelPointsCustomRewardRedemptionArgs args)
    {
        var eventData = args.Notification.Payload.Event;
        HandleChannelPointsEvent(eventData);
    }

    private void HandleChannelPointsEvent(ChannelPointsCustomRewardRedemption eventData)
    {
        //if (eventData.Status == "unfulfilled")
        //    return;
        
        OnChannelPointsRedeemed?.Invoke(eventData.UserName, eventData.UserInput, eventData.Reward.Id);
    }

    public async Task StartAsync()
    {
        await _eventSubWebsocketClient.ConnectAsync();
    }

    public async Task StopAsync()
    {
        await _eventSubWebsocketClient.DisconnectAsync();
        isConnected = false;
    }

    private async Task  OnWebsocketConnected(object? sender, WebsocketConnectedArgs e)
    { 
        isConnected = true;
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

        if (e.IsRequestedReconnect)
        {
            return;
        }
        _api = new TwitchAPI();
		
        _api.Settings.ClientId = HelperClass.GetTwitchApiCredentials(_appSettings).ClientId;
        _api.Settings.AccessToken = _appSettings.BroadcasterAuthObject.Access_Token;
        var broadcasterId = await HelperClass.GetUserId(_api, _appSettings, _appSettings.ChannelName);
        var conditions = new Dictionary<string, string>()
        {
            { "broadcaster_user_id", broadcasterId },
            { "moderator_user_id", broadcasterId }
        };
        try
        {
            await _api.Helix.EventSub.CreateEventSubSubscriptionAsync("channel.follow", "2", conditions,
                EventSubTransportMethod.Websocket, _eventSubWebsocketClient.SessionId);
            _logger.Debug("Subscribed to event Follows");
            await _api.Helix.EventSub.CreateEventSubSubscriptionAsync("channel.subscribe", "1", conditions,
                EventSubTransportMethod.Websocket, _eventSubWebsocketClient.SessionId);
            _logger.Debug("Subscribed to event Subscription");
            await _api.Helix.EventSub.CreateEventSubSubscriptionAsync("channel.cheer", "1", conditions,
                EventSubTransportMethod.Websocket, _eventSubWebsocketClient.SessionId);
            _logger.Debug("Subscribed to event Cheer");
            await _api.Helix.EventSub.CreateEventSubSubscriptionAsync("channel.channel_points_custom_reward_redemption.add", "1", conditions,
                EventSubTransportMethod.Websocket, _eventSubWebsocketClient.SessionId);
            await _api.Helix.EventSub.CreateEventSubSubscriptionAsync("channel.channel_points_custom_reward_redemption.update", "1", conditions,
                EventSubTransportMethod.Websocket, _eventSubWebsocketClient.SessionId);
            _logger.Debug("Subscribed to event ChannelPointRedemptions");
        }
        catch (HttpResponseException ex)
        {
            Console.WriteLine(await ex.HttpResponse.Content.ReadAsStringAsync());
        }

    }

    private async Task OnWebsocketDisconnected(object? sender, EventArgs e)
    {
        isConnected = false;
        _logger.Error($"Websocket {_eventSubWebsocketClient.SessionId} disconnected!");

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
        isConnected = true;
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
    private async Task OnChannelSubscribe(object sender, ChannelSubscribeArgs args)
    {
        var eventData = args.Notification.Payload.Event;
        OnSubscription?.Invoke(eventData.UserName, false);
    }
    
    private async Task OnChannelSubscriptionMessage(object sender, ChannelSubscriptionMessageArgs args)
    {
        var eventData = args.Notification.Payload.Event;
        OnSubscription?.Invoke(eventData.UserName, true);
    }        
    private async Task OnChannelSubscriptionGift(object sender, ChannelSubscriptionGiftArgs args)
    {
        var eventData = args.Notification.Payload.Event;
        OnGiftSubscription?.Invoke(eventData.UserName, eventData.Total, eventData.IsAnonymous);
    }
    private async Task OnChannelCheer(object sender, ChannelCheerArgs args)
    {
        var eventData = args.Notification.Payload.Event;
        if (eventData.IsAnonymous)
            eventData.UserName = "Anonymous";
        OnBitsReceived?.Invoke(eventData.UserName, eventData.Bits);
    }

    private async Task OnChannelRaid(object sender, ChannelRaidArgs args)
    {
        var eventData = args.Notification.Payload.Event;
        OnRaid?.Invoke(eventData.FromBroadcasterUserName, eventData.Viewers);
    }
    public void Dispose()
    {
        _eventSubWebsocketClient.WebsocketConnected -= OnWebsocketConnected;
        _eventSubWebsocketClient.WebsocketDisconnected -= OnWebsocketDisconnected;
        _eventSubWebsocketClient.WebsocketReconnected -= OnWebsocketReconnected;
        _eventSubWebsocketClient.ErrorOccurred -= OnErrorOccurred;

        _eventSubWebsocketClient.ChannelFollow -= OnChannelFollow;
        _eventSubWebsocketClient.ChannelSubscribe -= OnChannelSubscribe;
        _eventSubWebsocketClient.ChannelSubscriptionGift -= OnChannelSubscriptionGift;
        _eventSubWebsocketClient.ChannelRaid -= OnChannelRaid;
        _eventSubWebsocketClient.ChannelCheer -= OnChannelCheer;
        // TODO release managed resources here
    }
}