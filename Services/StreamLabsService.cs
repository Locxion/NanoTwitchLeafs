using System;
using System.Threading;
using System.Threading.Tasks;
using H.Socket.IO;
using log4net;
using NanoTwitchLeafs.Enums;
using NanoTwitchLeafs.Interfaces;
using NanoTwitchLeafs.Objects;
using Newtonsoft.Json;

namespace NanoTwitchLeafs.Services;

class StreamLabsService : IStreamLabsService
{
    private readonly ISettingsService _settingsService;
    public event EventHandler<StreamlabsEvent> OnDonationReceived;
    private const string RedirectUri = "http://127.0.0.1:1234/";
    private const string StreamlabsApi = "https://streamlabs.com/api/v1.0";
    private const string AuthorizationEndpoint = "/authorize";
    private const string TokenEndpoint = "/token";
    private const string SocketTokenEndpoint = "/socket/token";

    private readonly ILog _logger = LogManager.GetLogger(typeof(StreamLabsService));
    private readonly string _websocketUrl = "https://sockets.streamlabs.com/?token=";
    private bool _isConnected;
    private SocketIoClient _socketClient = new();
    
    public StreamLabsService(ISettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
    }

    private void OnDisconnected(object sender, SocketIoClient.DisconnectedEventArgs e)
    {
        _logger.Info("Disconnected from Streamlabs Socket");
        _isConnected = false;
    }

    private void OnConnected(object sender, SocketIoClient.ConnectedEventArgs e)
    {
        _logger.Info("Connected to Streamlabs Socket");
        _isConnected = true;
    }

    private void OnEventReceived(string data)
    {
        _logger.Debug(data);
        var formattedJson = HelperClass.FormatJson(data);
        dynamic eventObj = JsonConvert.DeserializeObject(formattedJson);

        if (eventObj?.type.ToString() != "donation") return;
        var amount = Convert.ToDouble(eventObj.message[0].amount.ToString());
        var username = eventObj.message[0].from.ToString();
        var newEvent = new StreamlabsEvent(username, Event.Donation, false, amount);
        OnDonationReceived?.Invoke(this, newEvent);
    }
    /// <summary>
    /// Connects to Streamlabs Websocket with Streamlabs Token in AppSettings
    /// </summary>
    public async Task Connect()
    {
        if (string.IsNullOrWhiteSpace(_settingsService.CurrentSettings.StreamlabsInformation.StreamlabsAToken) ||
            string.IsNullOrWhiteSpace(_settingsService.CurrentSettings.StreamlabsInformation.StreamlabsSocketToken) ||
            string.IsNullOrWhiteSpace(_settingsService.CurrentSettings.StreamlabsInformation.StreamlabsUser))
        {
            _logger.Warn("No Streamlabs Credentials ... skip Connection");
            return;
        }
        await ConnectSocket(_settingsService.CurrentSettings.StreamlabsInformation.StreamlabsSocketToken);

    }

    public bool IsConnected()
    {
        return _isConnected;
    }

    private async Task ConnectSocket(string token)
    {
        _logger.Debug("Try to connect to Streamlabs Socket");
        try
        {
            _socketClient.Connected += OnConnected;
            _socketClient.Disconnected += OnDisconnected;
            _socketClient.On("event", OnEventReceived);
            await _socketClient.ConnectAsync(new Uri(_websocketUrl + token));
        }
        catch (Exception e)
        {
            _logger.Error("Streamlabs Connection failed!");
            _logger.Error(e);
        }
        finally
        {
            await Disconnect();
        }
    }
    /// <summary>
    /// Disconnects from Websocket
    /// </summary>
    public async Task Disconnect()
    {
        CancellationToken token = new CancellationToken();
        _logger.Debug("Disconnect from Streamlabs Socket");
        await _socketClient.DisconnectAsync(token);
    }
}