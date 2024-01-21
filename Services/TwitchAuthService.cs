using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using log4net;
using NanoTwitchLeafs.Interfaces;
using NanoTwitchLeafs.Objects;
using Newtonsoft.Json;
using TwitchLib.Api;

namespace NanoTwitchLeafs.Services;

public class TwitchAuthService : ITwitchAuthService
{
    private readonly ISettingsService _settingsService;
    private readonly ILog _logger = LogManager.GetLogger(typeof(TwitchAuthService));
    private const string TwitchApiAddress = "https://id.twitch.tv/oauth2";
    private const string AuthorizationEndpoint = "/authorize";
    private const string TokenEndpoint = "/token";
    private const string TwitchScopesBot = "scope=chat:edit chat:read whispers:read whispers:edit user:manage:whispers";
    private const string TwitchScopesChannelOwner = "scope=chat:edit chat:read whispers:read whispers:edit user:manage:whispers bits:read channel:read:subscriptions channel:read:hype_train channel:read:redemptions channel:manage:redemptions";

    private const string RedirectUri = "http://127.0.0.1:1234";

    public TwitchAuthService(ISettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
    }
    public async Task<OAuthObject> GetAuthToken(TwitchApiCredentials apiCredentials, bool isBroadcaster)
    {
        //Source from: https:github.com/googlesamples/oauth-apps-for-windows/blob/master/OAuthDesktopApp/OAuthDesktopApp/MainWindow.xaml.cs
        //Creates an HttpListener to listen for requests on that redirect URI.

        var httpListener = new HttpListener();
        httpListener.Prefixes.Add("http://127.0.0.1:1234/");
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

        //Creates the OAuth 2.0 authorization request.
        var state = HelperClass.RandomDataBase64Url(32);
        var authorizationRequest = "";
        if (isBroadcaster)
        {
            authorizationRequest = $"{TwitchApiAddress}{AuthorizationEndpoint}?client_id={apiCredentials.ClientId}&redirect_uri={Uri.EscapeDataString(RedirectUri)}&response_type=code&{TwitchScopesChannelOwner}&force_verify=true&state={state}";
        }
        else
        {
            authorizationRequest = $"{TwitchApiAddress}{AuthorizationEndpoint}?client_id={apiCredentials.ClientId}&redirect_uri={Uri.EscapeDataString(RedirectUri)}&response_type=code&{TwitchScopesBot}&force_verify=true&state={state}";
        }

        //Opens request in the browser.
        Process.Start(authorizationRequest);

        //Waits for the OAuth authorization response.

        var context = await httpListener.GetContextAsync();

        //Brings this app back to the foreground.
        //Activate();

        //Sends an HTTP response to the browser.
        const string responseString = "<html><head><meta http-equiv='refresh' content='10;url=https:www.nanotwitchleafs.com/'></head><body>Please return to the app.</body></html>";
        var buffer = Encoding.UTF8.GetBytes(responseString);

        var response = context.Response;
        response.ContentLength64 = buffer.Length;
        var responseOutput = response.OutputStream;
        await responseOutput.WriteAsync(buffer, 0, buffer.Length).ContinueWith(task =>
        {
            responseOutput.Close();
            httpListener.Stop();
        });

        //Checks for errors.
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

        //extracts the code
        string code = context.Request.QueryString.Get("code");
        string incomingState = context.Request.QueryString.Get("state");

        //compares the received state to the expected value, to ensure that this app made the request which resulted in authorization.
        if (incomingState != state)
        {
            _logger.Error($"Received request with invalid state ({incomingState})");
            return null;
        }

        _logger.Debug("Authorization code: " + code);

        return await PerformCodeExchange(code, apiCredentials);
    }

    /// <summary>
    /// Pulls Avatar Url from TwitchUser
    /// </summary>
    /// <param name="userName"></param>
    /// <param name="token"></param>
    /// <returns>Url as String</returns>
    public async Task<string> GetAvatarUrl(string userName, string token)
    {
        var api = new TwitchAPI
        {
            Settings =
            {
                ClientId = HelperClass.GetTwitchApiCredentials(_settingsService.CurrentSettings).ClientId,
                AccessToken = token
            }
        };

        var getUsersResponse = await api.Helix.Users.GetUsersAsync(null, [userName], token);
        return getUsersResponse.Users[0].ProfileImageUrl;
    }


    private async Task<OAuthObject> PerformCodeExchange(string code, TwitchApiCredentials apiCredentials, bool isRefresh = false)
    {
        _logger.Info("Exchanging code for tokens...");
        string tokenRequestBody = "";

        if (!isRefresh)
        {
            //builds the  request
            tokenRequestBody = $"code={code}&redirect_uri={Uri.EscapeDataString(RedirectUri)}&client_id={apiCredentials.ClientId}&client_secret={apiCredentials.ClientSecret}&grant_type=authorization_code";
        }
        else
        {
            //builds the  request
            tokenRequestBody = $"refresh_token={code}&client_id={apiCredentials.ClientId}&client_secret={apiCredentials.ClientSecret}&grant_type=refresh_token";
        }

        //sends the request
        var tokenRequest = (HttpWebRequest)WebRequest.Create($"{TwitchApiAddress}{TokenEndpoint}");
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
            //gets the response
            var tokenResponse = await tokenRequest.GetResponseAsync();
            using (var reader = new StreamReader(tokenResponse.GetResponseStream()))
            {
                //reads response body
                string responseText = await reader.ReadToEndAsync();
                _logger.Debug("Response: " + responseText);

                responseText = responseText.Remove(136) + "}";

                //converts to dictionary
                dynamic tokenEndpointDecoded = JsonConvert.DeserializeObject(responseText);

                return new OAuthObject { Access_Token = tokenEndpointDecoded["access_token"], Refresh_Token = tokenEndpointDecoded["refresh_token"], Expires_In = tokenEndpointDecoded["expires_in"] };
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
                        //reads response body
                        string responseText = await reader.ReadToEndAsync();
                        _logger.Debug("Response: " + responseText);
                    }
                }
            }
        }

        return null;
    }
}