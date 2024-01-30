using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using log4net;
using NanoTwitchLeafs.Interfaces;
using NanoTwitchLeafs.Objects;
using Newtonsoft.Json;

namespace NanoTwitchLeafs.Services;

class StreamLabsAuthService : IStreamLabsAuthService
{
    private readonly ILog _logger = LogManager.GetLogger(typeof(StreamLabsAuthService));

    private readonly ISettingsService _settingsService;
    private const string RedirectUri = "http://127.0.0.1:1234/";

    private const string StreamlabsApi = "https://streamlabs.com/api/v2.0";
    private const string AuthorizationEndpoint = "/authorize";
    private const string TokenEndpoint = "/token";
    private const string SocketTokenEndpoint = "/socket/token";

    public StreamLabsAuthService(ISettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
    }
    /// <summary>
    /// Pulls the ProfileInformation from the Streamlabs Api
    /// </summary>
    /// <returns>Profile Information in Json Format</returns>
    public async Task<string> GetProfileInformation()
    {
        using var client = new HttpClient();
        string response = await client.GetStringAsync($"{StreamlabsApi}/user?access_token={_settingsService.CurrentSettings.StreamlabsInformation.StreamlabsAToken}");
        return HelperClass.FormatJson(response);
    }

    /// <summary>
    /// Get Access Token from Streamlabs Api
    /// </summary>
    /// <returns></returns>
    public async Task<string> GetAccessToken(StreamLabsApiCedentials apiCredentials)
    {
        // Source from: https://github.com/googlesamples/oauth-apps-for-windows/blob/master/OAuthDesktopApp/OAuthDesktopApp/MainWindow.xaml.cs
        // Creates an HttpListener to listen for requests on that redirect URI.

        var httpListener = new HttpListener();
        httpListener.Prefixes.Add(RedirectUri);

        try
        {
            httpListener.Start();
        }
        catch (HttpListenerException e)
        {
            _logger.Error("Could not open Port on Local Machine - Blocked by other Service!");
            _logger.Error(e.Message, e);
            return null;
        }

        // Creates the OAuth 2.0 authorization request.
        string state = HelperClass.RandomDataBase64Url(32);
        string authorizationRequest = $"{StreamlabsApi}{AuthorizationEndpoint}?response_type=code&scope=socket.token&donations.read&redirect_uri={Uri.EscapeDataString(RedirectUri)}&client_id={apiCredentials.ClientId}&state={state}";

        // Opens request in the browser.
        Process.Start(authorizationRequest);

        // Waits for the OAuth authorization response.
        var context = await httpListener.GetContextAsync();

        // Brings this app back to the foreground.
        // Activate();

        // Sends an HTTP response to the browser.
        const string responseString = "<html><head><meta http-equiv='refresh' content='10;url=https://www.nanotwitchleafs.com/'></head><body>Please return to the app.</body></html>";
        var buffer = Encoding.UTF8.GetBytes(responseString);

        var response = context.Response;
        response.ContentLength64 = buffer.Length;
        var responseOutput = response.OutputStream;
        await responseOutput.WriteAsync(buffer, 0, buffer.Length).ContinueWith(_ =>
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

        // Compares the received state to the expected value, to ensure that
        // this app made the request which resulted in authorization.
        if (incomingState != state)
        {
            _logger.Error($"Received request with invalid state ({incomingState})");
            return null;
        }

        _logger.Debug("Authorization code: " + code);

        return await PerformCodeExchange(code, apiCredentials);
    }

    private async Task<string> PerformCodeExchange(string code, StreamLabsApiCedentials apiCredentials)
    {
        _logger.Info("Exchanging code for tokens...");

        // builds the  request
        var tokenRequestBody = $"code={code}&redirect_uri={Uri.EscapeDataString(RedirectUri)}&client_id={apiCredentials.ClientId}&client_secret={apiCredentials.ClientSecret}&grant_type=authorization_code";

        // sends the request
        var tokenRequest = (HttpWebRequest)WebRequest.Create($"{StreamlabsApi}{TokenEndpoint}");
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
            using var reader = new StreamReader(tokenResponse.GetResponseStream() ?? throw new InvalidOperationException());
            // reads response body
            string responseText = await reader.ReadToEndAsync();
            _logger.Debug("Response: " + responseText);

            // converts to dictionary
            var tokenEndpointDecoded = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseText);

            return tokenEndpointDecoded["access_token"];
        }
        catch (WebException ex)
        {
            if (ex.Status == WebExceptionStatus.ProtocolError)
            {
                if (ex.Response is HttpWebResponse response)
                {
                    _logger.Debug("HTTP: " + response.StatusCode);
                    using var reader = new StreamReader(response.GetResponseStream() ?? throw new InvalidOperationException());
                    // reads response body
                    string responseText = await reader.ReadToEndAsync();
                    _logger.Debug("Response: " + responseText);
                }
            }
        }

        return null;
    }

    public async Task<bool> LinkAccount()
    {
        _settingsService.CurrentSettings.StreamlabsInformation.StreamlabsUser = _settingsService.CurrentSettings.ChannelName;
        try
        {
            _settingsService.CurrentSettings.StreamlabsInformation.StreamlabsAToken = await GetAccessToken(HelperClass.GetStreamLabsApiCredentials(_settingsService.CurrentSettings));
            _settingsService.CurrentSettings.StreamlabsInformation.StreamlabsSocketToken = await GetSocketToken();
            if (string.IsNullOrWhiteSpace(_settingsService.CurrentSettings.StreamlabsInformation.StreamlabsSocketToken))
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
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(10);
        client.DefaultRequestHeaders.ExpectContinue = false;
        var response = await client.GetAsync(StreamlabsApi + SocketTokenEndpoint + $"?access_token={_settingsService.CurrentSettings.StreamlabsInformation.StreamlabsAToken}");
        if (!response.IsSuccessStatusCode)
        {
            return ""; 
        }

        var responseString = await response.Content.ReadAsStringAsync();
        dynamic responseObject = JsonConvert.DeserializeObject(responseString);
        return responseObject?.socket_token;
    }
}