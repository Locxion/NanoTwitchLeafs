using log4net;
using NanoTwitchLeafs.Objects;
using Newtonsoft.Json;
using System;
using System.Security.Authentication;
using System.Windows;
using WebSocket4Net;

namespace NanoTwitchLeafs.Controller
{
    public delegate void OnHeartRateRecieved(int heartRate);

    public delegate void OnHypeRateDisconnected();

    public delegate void OnHypeRateConnected();

    public class HypeRateIOController
    {
        private readonly AppSettings _appSettings;

        public event OnHeartRateRecieved OnHeartRateRecieved;

        public event OnHypeRateConnected OnHypeRateConnected;

        public event OnHypeRateDisconnected OnHypeRateDisconnected;

        private readonly ILog _logger = LogManager.GetLogger(typeof(NanoController));

        private readonly string _websocketUrl = "wss://app.hyperate.io/socket/websocket?token=hOXGm5VyUHypYDLfMVSCpVLkUwrfLqRwXbPyBhVVyOrI3XBV65IMxVRspXAhVNU4";
        private WebSocket _webSocket;
        public bool _isConnected = false;

        public HypeRateIOController(AppSettings appSettings)
        {
            _appSettings = appSettings;
            _webSocket = new WebSocket(_websocketUrl);
            _webSocket.Opened += _webSocket_Opened;
            _webSocket.Error += _webSocket_Error;
            _webSocket.Closed += _webSocket_Closed;
            _webSocket.MessageReceived += _webSocket_MessageReceived;
            _webSocket.AutoSendPingInterval = 25;
            _webSocket.EnableAutoSendPing = true;
            _webSocket.Security.EnabledSslProtocols = SslProtocols.Tls12;
        }

        private void _webSocket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            _logger.Debug($"Recieved Message from HypeRate Server: {e.Message}");
            var message = JsonConvert.DeserializeObject<dynamic>(e.Message);

            if (message.@event == "hr_update")
            {
                _logger.Debug($"Recieved HeartRate {message.payload.hr}");
                OnHeartRateRecieved?.Invoke(Convert.ToInt32(message.payload.hr));
            }
        }

        private void _webSocket_Closed(object sender, EventArgs e)
        {
            _logger.Debug("Closed Connection to HypeRate Server");
            _isConnected = false;
            OnHypeRateDisconnected?.Invoke();
        }

        private void _webSocket_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            _logger.Error(e.Exception);
            _logger.Error(e.Exception.Message);
            MessageBox.Show(Properties.Resources.General_MessageBox_HypeRate_Error_Message, Properties.Resources.General_MessageBox_Error_Title,
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void _webSocket_Opened(object sender, EventArgs e)
        {
            _isConnected = true;
            OnHypeRateConnected?.Invoke();
            _logger.Debug("Connect to HypeRate Server");
            var message = $"{{\r\n  \"topic\": \"hr:{_appSettings.HypeRateId}\",\r\n  \"event\": \"phx_join\",\r\n  \"payload\": {{}},\r\n  \"ref\": 0\r\n}}";
            _logger.Debug("Send 'Join Channel' Message");
            _webSocket.Send(message);
        }

        public void StartListener()
        {
            if (string.IsNullOrWhiteSpace(_appSettings.HypeRateId))
            {
                _logger.Warn("No HypeRateIO ID ... skip Connection");
                return;
            }
            _webSocket.Open();
        }

        public void Disconnect()
        {
            _webSocket.Close();
        }
    }
}