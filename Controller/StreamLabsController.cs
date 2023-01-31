using H.Socket.IO;
using log4net;
using NanoTwitchLeafs.Objects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NanoTwitchLeafs.Controller
{
    public delegate void OnDonationRecieved(double amount, string username);

    public class StreamlabsController
    {
        private const string RedirectUri = "http://127.0.0.1:1234/";

        private const string StreamlabsAPI = "https://streamlabs.com/api/v1.0";
        private const string AuthorizationEndpoint = "/authorize";
        private const string TokenEndpoint = "/token";
        private const string SocketTokenEndpoint = "/socket/token";

        private readonly ILog _logger = LogManager.GetLogger(typeof(StreamlabsController));
        private readonly AppSettings _appSettings;
        private string _websocketUrl = "https://sockets.streamlabs.com/?token=";
        public bool _IsSocketConnected = false;
        private SocketIoClient _socketClient;

        public event OnDonationRecieved OnDonationRecieved;

        public StreamlabsController(AppSettings appSettings)
        {
            _appSettings = appSettings;

            _socketClient = new SocketIoClient();
            _socketClient.Connected += _socketClient_Connected;
            _socketClient.Disconnected += _socketClient_Disconnected;
            _socketClient.On("event", _socketClient_EventReceived);
        }

        private void _socketClient_EventReceived(string data)
        {
            _logger.Debug(data);
            var formattedJson = FormatJson(data);
            dynamic eventObj = JsonConvert.DeserializeObject(formattedJson);

            if (eventObj?.type.ToString() == "donation")
            {
                var amount = Convert.ToDouble(eventObj.message[0].amount.ToString());
                var username = eventObj.message[0].from.ToString();
                OnDonationRecieved?.Invoke(amount, username);
            }
        }

        private void _socketClient_Disconnected(object sender, H.WebSockets.Args.WebSocketCloseEventArgs e)
        {
            _logger.Info("Disconnected from Streamlabs Socket");
            _IsSocketConnected = false;
        }

        private void _socketClient_Connected(object sender, H.Socket.IO.EventsArgs.SocketIoEventEventArgs e)
        {
            _logger.Info("Connected to Streamlabs Socket");
            _IsSocketConnected = true;
        }

        public async Task<string> GetProfileInformation()
        {
            using (var client = new HttpClient())
            {
                string response = await client.GetStringAsync($"{StreamlabsAPI}/user?access_token={_appSettings.StreamlabsInformation.StreamlabsAToken}");
                return FormatJson(response);
            }
        }

        public async Task<string> GetAccessToken()
        {
            // Source from: https://github.com/googlesamples/oauth-apps-for-windows/blob/master/OAuthDesktopApp/OAuthDesktopApp/MainWindow.xaml.cs
            // Creates an HttpListener to listen for requests on that redirect URI.

            var httpListener = new HttpListener();
            httpListener.Prefixes.Add(RedirectUri);

            httpListener.Start();

            // Creates the OAuth 2.0 authorization request.
            string state = RandomDataBase64Url(32);
            string authorizationRequest = $"{StreamlabsAPI}{AuthorizationEndpoint}?response_type=code&scope=socket.token&donations.read&redirect_uri={Uri.EscapeDataString(RedirectUri)}&client_id={_appSettings.StreamlabsClientId}&state={state}";

            // Opens request in the browser.
            Process.Start(authorizationRequest);

            // Waits for the OAuth authorization response.
            var context = await httpListener.GetContextAsync();

            // Brings this app back to the foreground.
            // Activate();

            // Sends an HTTP response to the browser.
            const string responseString = "<html><head><meta http-equiv='refresh' content='10;url=http://www.nanotwitchleafs.com/'></head><body>Please return to the app.</body></html>";
            var buffer = Encoding.UTF8.GetBytes(responseString);

            var response = context.Response;
            response.ContentLength64 = buffer.Length;
            var responseOutput = response.OutputStream;
            await responseOutput.WriteAsync(buffer, 0, buffer.Length).ContinueWith(task =>
            {
                responseOutput.Close();
                httpListener.Stop();
            });

            // Checks for errors.
            if (context.Request.QueryString.Get("error") != null)
            {
                _logger.Error($"OAuth authorization error: {context.Request.QueryString.Get("error")}.");
                return null;
            }

            if (context.Request.QueryString.Get("code") == null || context.Request.QueryString.Get("state") == null)
            {
                _logger.Error("Malformed authorization response. " + context.Request.QueryString);
                return null;
            }

            // extracts the code
            string code = context.Request.QueryString.Get("code");
            string incomingState = context.Request.QueryString.Get("state");

            // Compares the receieved state to the expected value, to ensure that
            // this app made the request which resulted in authorization.
            if (incomingState != state)
            {
                _logger.Error($"Received request with invalid state ({incomingState})");
                return null;
            }

            _logger.Debug("Authorization code: " + code);

            return await PerformCodeExchange(code);
        }

        private async Task<string> PerformCodeExchange(string code)
        {
            _logger.Info("Exchanging code for tokens...");

            // builds the  request
            string tokenRequestBody = $"code={code}&redirect_uri={Uri.EscapeDataString(RedirectUri)}&client_id={_appSettings.StreamlabsClientId}&client_secret={_appSettings.StreamlabsClientSecret}&grant_type=authorization_code";

            // sends the request
            var tokenRequest = (HttpWebRequest)WebRequest.Create($"{StreamlabsAPI}{TokenEndpoint}");
            tokenRequest.Method = "POST";
            tokenRequest.ContentType = "application/x-www-form-urlencoded";
            tokenRequest.Accept = "Accept=text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            tokenRequest.ProtocolVersion = HttpVersion.Version10;

            var byteVersion = Encoding.ASCII.GetBytes(tokenRequestBody);
            tokenRequest.ContentLength = byteVersion.Length;
            var stream = tokenRequest.GetRequestStream();
            await stream.WriteAsync(byteVersion, 0, byteVersion.Length);
            stream.Close();

            try
            {
                // gets the response
                var tokenResponse = await tokenRequest.GetResponseAsync();
                using (var reader = new StreamReader(tokenResponse.GetResponseStream()))
                {
                    // reads response body
                    string responseText = await reader.ReadToEndAsync();
                    _logger.Debug("Response: " + responseText);

                    // converts to dictionary
                    var tokenEndpointDecoded = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseText);

                    return tokenEndpointDecoded["access_token"];
                }
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    if (ex.Response is HttpWebResponse response)
                    {
                        _logger.Debug("HTTP: " + response.StatusCode);
                        using (var reader = new StreamReader(response.GetResponseStream()))
                        {
                            // reads response body
                            string responseText = await reader.ReadToEndAsync();
                            _logger.Debug("Response: " + responseText);
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Returns URI-safe data with a given input length.
        /// </summary>
        /// <param name="length">Input length (nb. output will be longer)</param>
        /// <returns></returns>
        public static string RandomDataBase64Url(uint length)
        {
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            byte[] bytes = new byte[length];
            rng.GetBytes(bytes);
            return Base64UrlEncodeNoPadding(bytes);
        }

        /// <summary>
        /// Base64url no-padding encodes the given input buffer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static string Base64UrlEncodeNoPadding(byte[] buffer)
        {
            string base64 = Convert.ToBase64String(buffer);

            // Converts base64 to base64url.
            base64 = base64.Replace("+", "-");
            base64 = base64.Replace("/", "_");
            // Strips padding.
            base64 = base64.Replace("=", "");

            return base64;
        }

        private static string FormatJson(string json)
        {
            dynamic parsedJson = JsonConvert.DeserializeObject(json);
            return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
        }

        public async Task<bool> LinkAccount()
        {
            _appSettings.StreamlabsInformation.StreamlabsUser = _appSettings.ChannelName;
            try
            {
                _appSettings.StreamlabsInformation.StreamlabsAToken = await GetAccessToken();
                _appSettings.StreamlabsInformation.StreamlabsSocketToken = await GetSocketToken();
                if (string.IsNullOrWhiteSpace(_appSettings.StreamlabsInformation.StreamlabsSocketToken))
                {
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                _logger.Error(e.Message);
                _logger.Error(e);
                return false;
            }
        }

        private async Task<string> GetSocketToken()
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.ExpectContinue = false;
                var response = await client.GetAsync(StreamlabsAPI + SocketTokenEndpoint + $"?access_token={_appSettings.StreamlabsInformation.StreamlabsAToken}");
                if (!response.IsSuccessStatusCode)
                {
                    return "";
                }

                string responseString = await response.Content.ReadAsStringAsync();
                dynamic responseObject = JsonConvert.DeserializeObject(responseString);
                return responseObject.socket_token;
            }
        }

        public async void ConnectSocket()
        {
            if (string.IsNullOrWhiteSpace(_appSettings.StreamlabsInformation.StreamlabsAToken) ||
                string.IsNullOrWhiteSpace(_appSettings.StreamlabsInformation.StreamlabsSocketToken) ||
                string.IsNullOrWhiteSpace(_appSettings.StreamlabsInformation.StreamlabsUser))
            {
                _logger.Warn("No Streamlabs Credentials ... skip Connection");
                return;
            }

            try
            {
                await ConnectSocket(_appSettings.StreamlabsInformation.StreamlabsSocketToken);
            }
            catch (Exception e)
            {
                _logger.Error("Try to connect to Streamlabs Socket failed", e);
                throw;
            }
        }

        private async Task ConnectSocket(string token)
        {
            _logger.Debug("Try to connect to Streamlabs Socket");
            await _socketClient.ConnectAsync(new Uri(_websocketUrl + token));
        }

        public async void DisconnectSocket()
        {
            CancellationToken token = new CancellationToken();
            _logger.Debug("Disconnect from Streamlabs Socket");
            await _socketClient.DisconnectAsync(token);
        }
    }
}