using log4net;
using NanoTwitchLeafs.Enums;
using NanoTwitchLeafs.Objects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Models;
using ChatMessage = NanoTwitchLeafs.Objects.ChatMessage;
using MessageBox = System.Windows.MessageBox;

namespace NanoTwitchLeafs.Controller
{
	public delegate void ChatMessageReceived(ChatMessage message);

	public delegate void TwitchEventReceived(string username, string twitchEvent, bool isAnonymous = false, int amount = 1);

	public delegate void CallLoadingWindow(bool state);

	public delegate void RaidEvent(int amount, string username, bool isRaid = false);

	public class TwitchController
	{
		private const string TwitchApiAddress = "https://id.twitch.tv/oauth2";
		private const string AuthorizationEndpoint = "/authorize";
		private const string TokenEndpoint = "/token";
		private const string TwitchScopesBot = "scope=chat:edit chat:read whispers:read whispers:edit user:manage:whispers";
		private const string TwitchScopesChannelOwner = "scope=chat:edit chat:read whispers:read whispers:edit user:manage:whispers bits:read channel:read:subscriptions channel:read:hype_train channel:read:redemptions channel:manage:redemptions";

		private const string RedirectUri = "http://127.0.0.1:1234";

		private readonly Subject<OnGiftedSubscriptionArgs> _subscriptionSubject = new Subject<OnGiftedSubscriptionArgs>();

		private readonly ILog _logger = LogManager.GetLogger(typeof(TwitchController));
		public TwitchClient Client;
		private TwitchClient _broadCasterClient;
		private TwitchAPI _api;
		private AppSettings _appSettings;
		public TwitchPubSubController TwitchPubSubController;
		public TwitchEventSubController EventSubController;
		private readonly AppSettingsController _appSettingsController;

		public List<string> ChannelModerator { get; set; }
		

		private bool _firstTryToConnectBotAccount = true;
		private bool _firstTryToConnectBroadcasterAccount = true;

		public event EventHandler OnDisconnected;

		public event ChatMessageReceived OnChatMessageReceived;

		public event TwitchEventReceived OnTwitchEventReceived;

		public event RaidEvent OnRaidEvent;

		public event CallLoadingWindow OnCallLoadingWindow;

		public TwitchController(AppSettingsController appSettingsController)
		{
			_appSettingsController = appSettingsController ?? throw new ArgumentNullException(nameof(appSettingsController));
			_appSettings = _appSettingsController.LoadSettings();
			_subscriptionSubject
				.Buffer(TimeSpan.FromSeconds(1))
				.Where(x => x.Count > 0)
				.Subscribe(messages =>
				{
					var groups = messages.GroupBy(x => x.GiftedSubscription.UserId, x => x);
					foreach (var messageGroup in groups)
					{
						if (messageGroup.Count() != 1)
						{
							_logger.Debug("Got more Events. Ignore single Gift Subscription Event");
							continue;
						}

						_logger.Debug("Sending single Gift Subscription");
						var e = messageGroup.First();
						OnTwitchEventReceived?.Invoke(e.GiftedSubscription.DisplayName, TriggerTypeEnum.GiftSubscription.ToString(), e.GiftedSubscription.IsAnonymous);
					}
				});
			
		}

		/// <summary>
		/// Connects to Twitch Services with provided AppSettings
		/// </summary>
		/// <param name="appSettings"></param>
		public void Connect(AppSettings appSettings)
		{
			Client?.Disconnect();

			_appSettings = appSettings;

			// if (string.IsNullOrEmpty(_appSettings.BotName) || string.IsNullOrEmpty(_appSettings.ChannelName) || string.IsNullOrEmpty(_appSettings.BotAuthObject.Access_Token))
			// {
			//     MessageBox.Show(Properties.Resources.Code_Twitch_MessageBox_LoginIncorrect, Properties.Resources.General_MessageBox_Error_Title);
			//     ((MainWindow)App.Current.MainWindow).sendMessage_TextBox.IsEnabled = false;
			//     ((MainWindow)App.Current.MainWindow).sendMessage_Button.IsEnabled = false;
			//     _logger.Error("Please make sure Username, Channel and AuthToken are correct.");
			//     return;
			// }

			EstablishTwitchConnection();
		}

		private void EstablishTwitchConnection()
		{
			if (string.IsNullOrWhiteSpace(_appSettings.BotName) || _appSettings.BotAuthObject == null)
				return;

			ConnectionCredentials credentials = new ConnectionCredentials(_appSettings.BotName.ToLower(), "oauth:" + _appSettings.BotAuthObject.Access_Token);
			var clientOptions = new ClientOptions();
			WebSocketClient customClient = new WebSocketClient(clientOptions);
			Client = new TwitchClient(customClient);
			Client.Initialize(credentials, _appSettings.ChannelName.ToLower());

			Client.OnLog += Client_OnLog;
			Client.OnConnected += Client_OnConnected;
			Client.OnJoinedChannel += Client_OnJoinedChannel;
			Client.OnMessageReceived += Client_OnMessageReceived;
			Client.OnWhisperReceived += Client_OnWhisperReceived;
			Client.OnModeratorsReceived += Client_OnModeratorsReceived;
			Client.OnDisconnected += Client_OnDisconnected;
			Client.OnIncorrectLogin += Client_OnIncorrectLogin;

			if (_appSettings.BotName.ToLower() == _appSettings.ChannelName.ToLower())
			{
				// Effect Event Handlers
				Client.OnNewSubscriber += OnNewSubscriber;
				//_client.OnBeingHosted += OnBeingHosted;
				Client.OnRaidNotification += OnRaidNotification;
				Client.OnGiftedSubscription += OnGiftedSubscription;
				Client.OnReSubscriber += OnReSubscriber;
				Client.OnCommunitySubscription += OnCommunitySubscription;
			}

			Client.Connect();
		}

		private void Client_OnDisconnected(object sender, OnDisconnectedEventArgs e)
		{
			OnDisconnected?.Invoke(this, EventArgs.Empty);
			_logger.Info($"Disconnected from Twitch.");
		}

		private void BroadCasterClient_OnDisconnected(object sender, OnDisconnectedEventArgs e)
		{
			_logger.Info($"Disconnected from Twitch.");
		}

		private async void Client_OnIncorrectLogin(object sender, OnIncorrectLoginArgs e)
		{
			if (_firstTryToConnectBotAccount)
			{
				_logger.Warn("Got incorrect Login Message from Twitch ... (Bot Account)");
				_logger.Warn("Try to refresh Access Tokens ... This could take a Second ... or Two ...");

				OnCallLoadingWindow?.Invoke(true);

				Disconnect(true);
				var newOauth = await PerformCodeExchange(_appSettings.BotAuthObject.Refresh_Token, HelperClass.GetTwitchApiCredentials(_appSettings), true);
				_appSettings.BotAuthObject = newOauth;
				if (_appSettings.BotName == _appSettings.ChannelName)
					_appSettings.BroadcasterAuthObject = newOauth;
				_appSettingsController.SaveSettings(_appSettings);
				_firstTryToConnectBotAccount = false;

				OnCallLoadingWindow?.Invoke(false);
				EstablishTwitchConnection();
			}
			else
			{
				MessageBox.Show(Properties.Resources.Code_Twitch_MessageBox_LoginIncorrect, Properties.Resources.General_MessageBox_Error_Title);
				_logger.Error("Incorrect Login Data! Please check your Credentials!");
			}
		}

		private async void BroadCasterClient_OnIncorrectLogin(object sender, OnIncorrectLoginArgs e)
		{
			if (_firstTryToConnectBroadcasterAccount)
			{
				_logger.Warn("Got incorrect Login Message from Twitch ...(Broadcaster Account)");
				_logger.Warn("Try to refresh Access Tokens ... This could take a Second ... or Two ...");
				Disconnect();

				OnCallLoadingWindow?.Invoke(true);

				var newOauth = await PerformCodeExchange(_appSettings.BroadcasterAuthObject.Refresh_Token, HelperClass.GetTwitchApiCredentials(_appSettings), true);
				_appSettings.BroadcasterAuthObject = newOauth;
				_appSettingsController.SaveSettings(_appSettings);

				_firstTryToConnectBroadcasterAccount = false;

				ConnectionCredentials credentials = new ConnectionCredentials(_appSettings.ChannelName.ToLower(), "oauth:" + _appSettings.BroadcasterAuthObject.Access_Token);
				_broadCasterClient = new TwitchClient();

				_broadCasterClient.OnConnected += BroadCasterClient_OnConnected;
				_broadCasterClient.OnIncorrectLogin += BroadCasterClient_OnIncorrectLogin;
				//_broadCasterClient.OnLog += Client_OnLog; //Disabled to prevent spam in the Log
				_broadCasterClient.OnDisconnected += BroadCasterClient_OnDisconnected;

				// Effect Event Handlers
				_broadCasterClient.OnNewSubscriber += OnNewSubscriber;
				//_broadCasterClient.OnBeingHosted += OnBeingHosted;
				_broadCasterClient.OnRaidNotification += OnRaidNotification;
				_broadCasterClient.OnGiftedSubscription += OnGiftedSubscription;
				_broadCasterClient.OnReSubscriber += OnReSubscriber;

				_broadCasterClient.Initialize(credentials, _appSettings.ChannelName.ToLower());
				_broadCasterClient.Connect();
				OnCallLoadingWindow?.Invoke(false);
			}
			else
			{
				MessageBox.Show(Properties.Resources.Code_Twitch_MessageBox_LoginIncorrect, Properties.Resources.General_MessageBox_Error_Title);
				_logger.Error("Incorrect Broadcaster Login Data! Please check your Credentials!");
			}
		}

		private void Client_OnConnected(object sender, OnConnectedArgs e)
		{
			_firstTryToConnectBotAccount = true;
			_logger.Debug($"Connected to {e.AutoJoinChannel} with Account {e.BotUsername}.");
			OnCallLoadingWindow?.Invoke(false);
			if (_appSettings.BotName.ToLower() != _appSettings.ChannelName.ToLower())
			{
				_logger.Debug("Bot Account detected. Init Broadcaster Twitch Connection...");
				if (_broadCasterClient?.IsInitialized ?? false)
					_broadCasterClient.Disconnect();

				OnCallLoadingWindow?.Invoke(true);

				ConnectionCredentials broadcasterCredentials = new ConnectionCredentials(_appSettings.ChannelName.ToLower(), "oauth:" + _appSettings.BroadcasterAuthObject.Access_Token);
				_broadCasterClient = new TwitchClient();
				_broadCasterClient.Initialize(broadcasterCredentials, _appSettings.ChannelName.ToLower());

				_broadCasterClient.OnConnected += BroadCasterClient_OnConnected;
				_broadCasterClient.OnIncorrectLogin += BroadCasterClient_OnIncorrectLogin;
				//_broadCasterClient.OnLog += Client_OnLog; //Disabled to prevent spam in the Log
				_broadCasterClient.OnDisconnected += BroadCasterClient_OnDisconnected;

				// Effect Event Handlers
				_broadCasterClient.OnNewSubscriber += OnNewSubscriber;
				//_broadCasterClient.OnBeingHosted += OnBeingHosted;
				_broadCasterClient.OnRaidNotification += OnRaidNotification;
				_broadCasterClient.OnGiftedSubscription += OnGiftedSubscription;
				_broadCasterClient.OnReSubscriber += OnReSubscriber;
				_broadCasterClient.OnCommunitySubscription += OnCommunitySubscription;

				_broadCasterClient.Connect();
				OnCallLoadingWindow?.Invoke(false);
			}

			if (Client.IsConnected && (_appSettings.BotName.ToLower() == _appSettings.ChannelName.ToLower()))
			{
				//TwitchPubSubController.Connect(_appSettings);
			}

			EventSubController.StartAsync();
		}

		private void BroadCasterClient_OnConnected(object sender, OnConnectedArgs e)
		{
			OnCallLoadingWindow?.Invoke(false);
			_firstTryToConnectBroadcasterAccount = true;
			TwitchPubSubController.Connect(_appSettings);
			_logger.Debug($"Connected to {e.AutoJoinChannel} with BroadcasterAccount {e.BotUsername}.");
		}

		private void Client_OnModeratorsReceived(object sender, OnModeratorsReceivedArgs e)
		{
			ChannelModerator = e.Moderators;
		}

		/// <summary>
		/// Disconnects from Twitch Services
		/// </summary>
		/// <param name="both"></param>
		public void Disconnect(bool both = false)
		{
			if (Client is not null && Client.IsConnected)
			{
				Client.Disconnect();
				Client.OnLog -= Client_OnLog;
				Client.OnConnected -= Client_OnConnected;
				Client.OnJoinedChannel -= Client_OnJoinedChannel;
				Client.OnMessageReceived -= Client_OnMessageReceived;
				Client.OnWhisperReceived -= Client_OnWhisperReceived;
				Client.OnModeratorsReceived -= Client_OnModeratorsReceived;
				Client.OnDisconnected -= Client_OnDisconnected;
				Client.OnIncorrectLogin -= Client_OnIncorrectLogin;
				Client.OnNewSubscriber -= OnNewSubscriber;
				//_client.OnBeingHosted -= OnBeingHosted;
				Client.OnRaidNotification -= OnRaidNotification;
				Client.OnGiftedSubscription -= OnGiftedSubscription;
				Client.OnReSubscriber -= OnReSubscriber;
				Client.OnCommunitySubscription -= OnCommunitySubscription;
				Client = null;
			}
			
			if (both)
			{
				if (_broadCasterClient is not null && _broadCasterClient.IsConnected)
				{
					_broadCasterClient.Disconnect();
					_broadCasterClient.OnConnected -= BroadCasterClient_OnConnected;
					_broadCasterClient.OnIncorrectLogin -= BroadCasterClient_OnIncorrectLogin;
					//_broadCasterClient.OnLog -= Client_OnLog; //Disabled to prevent spam in the Log
					_broadCasterClient.OnDisconnected -= BroadCasterClient_OnDisconnected;

					// Effect Event Handlers
					_broadCasterClient.OnNewSubscriber -= OnNewSubscriber;
					//_broadCasterClient.OnBeingHosted -= OnBeingHosted;
					_broadCasterClient.OnRaidNotification -= OnRaidNotification;
					_broadCasterClient.OnGiftedSubscription -= OnGiftedSubscription;
					_broadCasterClient.OnReSubscriber -= OnReSubscriber;
					_broadCasterClient.OnCommunitySubscription -= OnCommunitySubscription;
					_broadCasterClient = null;
				}
			}

			EventSubController.StopAsync();
			OnCallLoadingWindow?.Invoke(false);
		}

		private void OnNewSubscriber(object sender, OnNewSubscriberArgs e)
		{
			_logger.Debug($"Received New Subscription from {e.Subscriber.DisplayName}.");
			OnTwitchEventReceived?.Invoke(e.Subscriber.DisplayName, TriggerTypeEnum.Subscription.ToString());
		}

		private void OnReSubscriber(object sender, OnReSubscriberArgs e)
		{
			_logger.Debug($"Received Re-Subscription from {e.ReSubscriber.DisplayName}. Month - {e.ReSubscriber.Months}.");
			OnTwitchEventReceived?.Invoke(e.ReSubscriber.DisplayName, TriggerTypeEnum.ReSubscription.ToString());
		}

		private void OnGiftedSubscription(object sender, OnGiftedSubscriptionArgs e)
		{
			_logger.Debug($"Received Gift Subscription. Anonymous - {e.GiftedSubscription.IsAnonymous}.");

			_subscriptionSubject.OnNext(e);
		}

		private void OnCommunitySubscription(object sender, OnCommunitySubscriptionArgs e)
		{
			_logger.Debug($"Received Gift Bomb. Anonymous - {e.GiftedSubscription.IsAnonymous}. Amount - {e.GiftedSubscription.MsgParamMassGiftCount}");
			OnTwitchEventReceived?.Invoke(e.GiftedSubscription.DisplayName, TriggerTypeEnum.GiftBomb.ToString(), e.GiftedSubscription.IsAnonymous, e.GiftedSubscription.MsgParamMassGiftCount);
		}

		private void OnRaidNotification(object sender, OnRaidNotificationArgs e)
		{
			_logger.Debug($"Received Raid form {e.RaidNotification.DisplayName}");
			OnRaidEvent?.Invoke(Convert.ToInt32(e.RaidNotification.MsgParamViewerCount), e.RaidNotification.DisplayName, true);
		}

		// private void OnBeingHosted(object sender, OnBeingHostedArgs e)
		// {
		//     _logger.Debug($"Received Host form {e.BeingHostedNotification.HostedByChannel}. Viewers - {e.BeingHostedNotification.Viewers}");
		//     OnHostEvent?.Invoke(e.BeingHostedNotification.Viewers, e.BeingHostedNotification.Channel);
		// }

		/// <summary>
		/// Sends Message to connected TwitchChannel
		/// </summary>
		/// <param name="message"></param>
		public void SendMessageToChat(string message)
		{
			if (!Client.IsConnected)
				return;
			Client.SendMessage(_appSettings.ChannelName, message);
			_logger.Info($"-> {message}");
		}

		/// <summary>
		/// Sends Message to User
		/// </summary>
		/// <param name="userName"></param>
		/// <param name="message"></param>
		public async void SendWhisper(string userName, string message)
		{
			if (Client is null || !Client.IsConnected)
				return;

			//Ignore Whisper when try to send to own User Account
			if (userName.ToLower() == _appSettings.ChannelName.ToLower())
			{
				_logger.Warn("Could not send Whisper to own User. Ignoring Message");
				return;
			}
			
			_api = new TwitchAPI();
			
			_api.Settings.ClientId = HelperClass.GetTwitchApiCredentials(_appSettings).ClientId;
			_api.Settings.AccessToken = _appSettings.BotAuthObject.Access_Token;
			
			var fromUserId = await HelperClass.GetUserId(_api, _appSettings, _appSettings.BotName);
			var toUserId = await HelperClass.GetUserId(_api, _appSettings, userName);

			try
			{
				await _api.Helix.Whispers.SendWhisperAsync(fromUserId, toUserId, message, true);
				_logger.Info($"-> to {userName} - {message}");
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				_logger.Error(ex);
			}
		}

		private void Client_OnLog(object sender, OnLogArgs e)
		{
			_logger.Debug(e.Data);
		}

		private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
		{
			if (_appSettings.Responses.StartupMessageActive)
			{
				string message = _appSettings.Responses.StartupResponse;
				if (message != "")
				{
					Client.SendMessage(e.Channel, message);
				}
				_logger.Info($"-> {message}");
			}
			OnCallLoadingWindow?.Invoke(false);
		}

		private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
		{
			var message = new ChatMessage(e.ChatMessage.Username, e.ChatMessage.IsSubscriber, e.ChatMessage.IsModerator, e.ChatMessage.IsVip, e.ChatMessage.Message,
				ColorTranslator.FromHtml(e.ChatMessage.ColorHex));
			_logger.Info($"{message.Username} - {message.Message}");
			OnChatMessageReceived?.Invoke(message);
		}

		private void Client_OnWhisperReceived(object sender, OnWhisperReceivedArgs e)
		{
			//_client.SendWhisper(e.WhisperMessage.Username, "I'm a Bot. I'm not programmed to answer Whisper Messages!");
		}

		#region Auth Handling

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
					ClientId = HelperClass.GetTwitchApiCredentials(_appSettings).ClientId,
					AccessToken = token
				}
			};

			var getUsersResponse = await api.Helix.Users.GetUsersAsync(null, [userName], token);
			return getUsersResponse.Users[0].ProfileImageUrl;
		}

		#endregion
	}

	public class TwitchClientWrapper : IDisposable
	{
		private TwitchClient _twitchClient;

		public event EventHandler<OnConnectedArgs> OnConnected;

		public TwitchClientWrapper()
		{
			_twitchClient = new TwitchClient();

			_twitchClient.OnConnected += OnConnected;
		}

		#region IDisposable Support

		private bool _disposed = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					if (_twitchClient.IsConnected)
						_twitchClient.Disconnect();

					_twitchClient.OnConnected -= OnConnected;
					_twitchClient = null;
				}

				_disposed = true;
			}
		}

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
		}

		#endregion
	}
}