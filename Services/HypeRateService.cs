using System;
using System.Security.Authentication;
using log4net;
using NanoTwitchLeafs.Interfaces;
using Newtonsoft.Json;
using SuperSocket.ClientEngine;
using WebSocket4Net;

namespace NanoTwitchLeafs.Services;

class HypeRateService : IHypeRateService
{
    private readonly ISettingsService _settingsService;
    public event EventHandler<int> OnHeartRateReceived;
    private readonly ILog _logger = LogManager.GetLogger(typeof(HypeRateService));
    private readonly string _websocketUrl = $"wss://app.hyperate.io/socket/websocket?token={Constants.ServiceCredentials.HyperateApi.ApiKey}";
    private WebSocket _webSocket;
    private bool _isConnected;

    public HypeRateService(ISettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _webSocket = new WebSocket(_websocketUrl);
    }

    private void _webSocket_MessageReceived(object sender, MessageReceivedEventArgs e)
    {
        _logger.Debug($"Received Message from HypeRate Server: {e.Message}");
        var message = JsonConvert.DeserializeObject<dynamic>(e.Message);

        if (message.@event == "hr_update")
        {
            _logger.Debug($"Received HeartRate {message.payload.hr}");
            OnHeartRateReceived?.Invoke(this,Convert.ToInt32(message.payload.hr));
        }
    }

    private void _webSocket_Closed(object sender, EventArgs e)
    {
        _logger.Info("Connection to HypeRate was closed.");
        _isConnected = false;
    }

    private void _webSocket_Error(object sender, ErrorEventArgs e)
    {
        _logger.Info("Connection to HypeRate was closed.");
        _logger.Error(e);
        _isConnected = false;    }

    private void _webSocket_Opened(object sender, EventArgs e)
    {
        _isConnected = true;
        _logger.Debug("Connect to HypeRate Server");
        var message = $"{{\r\n  \"topic\": \"hr:{_settingsService.CurrentSettings.HypeRateId}\",\r\n  \"event\": \"phx_join\",\r\n  \"payload\": {{}},\r\n  \"ref\": 0\r\n}}";
        _logger.Debug("Send 'Join Channel' Message");
        _webSocket.Send(message);
    }

    /// <summary>
    /// Connects to HypeRate Websocket
    /// </summary>
    public void Connect()
    {
        if (string.IsNullOrWhiteSpace(_settingsService.CurrentSettings.HypeRateId))
        {
            _logger.Warn("No HypeRateIO ID ... skip Connection");
            return;
        }
        try
        {
            _webSocket.Opened += _webSocket_Opened;
            _webSocket.Error += _webSocket_Error;
            _webSocket.Closed += _webSocket_Closed;
            _webSocket.MessageReceived += _webSocket_MessageReceived;
            _webSocket.AutoSendPingInterval = 25;
            _webSocket.EnableAutoSendPing = true;
            _webSocket.Security.EnabledSslProtocols = SslProtocols.Tls12;
            _webSocket.Open();
        }
        catch (Exception e)
        {
            _logger.Error("HypeRate Connection failed!");
            _logger.Error(e);
        }
        finally
        {
            Disconnect();
        }
    }

    /// <summary>
    /// Disconnects from HypeRate Websocket
    /// </summary>
    public void Disconnect()
    {
        _webSocket.Opened -= _webSocket_Opened;
        _webSocket.Error -= _webSocket_Error;
        _webSocket.Closed -= _webSocket_Closed;
        _webSocket.MessageReceived -= _webSocket_MessageReceived;
        _isConnected = false;
    }
}