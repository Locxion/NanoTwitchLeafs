using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using log4net;
using NanoTwitchLeafs.Interfaces;
using NanoTwitchLeafs.Objects;
using Newtonsoft.Json;
using TwitchLib.Api;
using TwitchLib.Api.Core.Exceptions;

namespace NanoTwitchLeafs.Services;

public class TwitchAuthService : ITwitchAuthService
{
    private readonly ISettingsService _settingsService;
    private readonly ILog _logger = LogManager.GetLogger(typeof(TwitchAuthService));
    private const string TwitchApiAddress = "https://id.twitch.tv/oauth2";
    private const string AuthorizationEndpoint = "/authorize";
    private const string TwitchScopesBot = "scope=chat:edit chat:read whispers:read whispers:edit user:manage:whispers";
    private const string TwitchScopesChannelOwner = "scope=chat:edit chat:read whispers:read whispers:edit user:manage:whispers bits:read channel:read:subscriptions channel:read:hype_train channel:read:redemptions channel:manage:redemptions moderator:read:followers user:read:email";

    private const string RedirectUri = "http://127.0.0.1:1234";
#if DEBUG
    private const string AnalyticsServerUrl = "https://localhost:7244/api";
#elif RELEASE
    private const string AnalyticsServerUrl = "https://analytics.nanotwitchleafs.de/api";
#endif
    private TwitchAPI _twitchApi = new ();
    private OAuthObject _applicationOAuth;

    public TwitchAuthService(ISettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _twitchApi.Settings.ClientId = HelperClass.GetTwitchApiCredentials(_settingsService.CurrentSettings).ClientId;
    }


    public async Task<OAuthObject> GetUserAuthToken(TwitchApiCredentials apiCredentials, bool isBroadcaster)
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

        return await PerformCodeExchange(code);
    }

    private async Task<OAuthObject> GetApplicationOAuth(bool expired = false)
    {
        var request = new HttpRequestMessage(HttpMethod.Get,$"{AnalyticsServerUrl}/twitchauth/apptoken/{expired}");

        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(AnalyticsServerUrl);
        httpClient.Timeout = TimeSpan.FromSeconds(10);
        var response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            _logger.Error("Server rejected Auth Request.");
            _logger.Error($"StatusCode: {response.StatusCode}");
            return null;
        }
        return await response.Content.ReadFromJsonAsync<OAuthObject>();    
    }

    private async Task<OAuthObject> PerformCodeExchange(string code)
    {
        var request = new HttpRequestMessage(HttpMethod.Get,$"{AnalyticsServerUrl}/twitchauth/usertoken/{code}");

        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(AnalyticsServerUrl);
        httpClient.Timeout = TimeSpan.FromSeconds(10);
        var response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            _logger.Error("Server rejected Auth Request.");
            _logger.Error($"StatusCode: {response.StatusCode}");
            return null;
        }
        return await response.Content.ReadFromJsonAsync<OAuthObject>();
    }
    
    /// <summary>
    /// Gets UserId for Username from Twitch API with provided Api, Settings, and Username
    /// </summary>
    /// <param name="userName"></param>
    /// <returns>UserId as String</returns>
    public async Task<string> GetUserId(string userName)
    {
        _applicationOAuth = await GetApplicationOAuth();
        try
        {
            var user = await _twitchApi.Helix.Users.GetUsersAsync(null, [userName.ToLower()],
                _applicationOAuth.Access_Token);
            return user.Users[0].Id;
        }
        catch (BadScopeException e)
        {
            _logger.Error("Could not get UserId. Bad Scopes for Access Token", e);
            return null;
        }
        catch (TokenExpiredException e)
        {
            _logger.Error("Could not get UserId from Api - Access Token Expired");
            _applicationOAuth = await GetApplicationOAuth(true);
            return await GetUserId(userName);
        }
        catch (Exception e)
        {
            _logger.Error("Could not get UserId from Api", e);
            return null;
        }
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
}