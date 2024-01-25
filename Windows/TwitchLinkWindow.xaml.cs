using log4net;
using NanoTwitchLeafs.Objects;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;
using NanoTwitchLeafs.Interfaces;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Events;
using OnConnectedEventArgs = TwitchLib.Client.Events.OnConnectedEventArgs;

// ReSharper disable InconsistentNaming

namespace NanoTwitchLeafs.Windows
{
	/// <summary>
	/// Interaction logic for TwitchLinkWindow.xaml
	/// </summary>
	public partial class TwitchLinkWindow : Window
	{
		private readonly ISettingsService _settingsService;
		private readonly ITwitchAuthService _authService;
		private readonly ILog _logger = LogManager.GetLogger(typeof(TwitchLinkWindow));
		private bool _doubleAccount = false;
		private string _broadCasterAccountName;
		private string _botAccountName;
		private Uri _broadCasterAvatarUrl;
		private Uri _botAccountAvatarUrl;
		private OAuthObject _broadcasterAuthObject;
		private OAuthObject _botAuthObject;

		public TwitchLinkWindow(ISettingsService settingsService , ITwitchAuthService authService)
		{
			_settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
			_authService = authService ?? throw new ArgumentNullException(nameof(authService));
			Constants.SetCultureInfo(_settingsService.CurrentSettings.Language);
			InitializeComponent();
		}

		#region UI Eventhandler

		private void TextBlock_MouseEnter(object sender, MouseEventArgs e)
		{
			var textBlock = (TextBlock)sender;
			textBlock.Background = Brushes.DarkGray;
		}

		private void TextBlock_MouseLeave(object sender, MouseEventArgs e)
		{
			var textBlock = (TextBlock)sender;
			textBlock.Background = new SolidColorBrush(Color.FromRgb(0xE5, 0xE5, 0xE5));
		}

		private void Double_TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
		{
			_doubleAccount = true;
			ConnectBroadcaster_TabItem.IsSelected = true;
		}

		private void Single_TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
		{
			_doubleAccount = false;
			ConnectBroadcaster_TabItem.IsSelected = true;
		}

		private void BCBack_Button_Click(object sender, RoutedEventArgs e)
		{
			Setup_TabItem.IsSelected = true;
		}

		private void BCNext_Button_Click(object sender, RoutedEventArgs e)
		{
			if (_doubleAccount)
			{
				ConnectBot_TabItem.IsSelected = true;
			}
			else
			{
				Test_TabItem.IsSelected = true;
			}
		}

		private void BotNext_Button_Click(object sender, RoutedEventArgs e)
		{
			Test_TabItem.IsSelected = true;
		}

		private void TestNext_Button_Click(object sender, RoutedEventArgs e)
		{
			Done_TabItem.IsSelected = true;
		}

		private void BotBack_Button_Click(object sender, RoutedEventArgs e)
		{
			ConnectBroadcaster_TabItem.IsSelected = true;
		}

		private void TestBack_Button_Click(object sender, RoutedEventArgs e)
		{
			if (_doubleAccount)
			{
				ConnectBot_TabItem.IsSelected = true;
			}
			else
			{
				ConnectBroadcaster_TabItem.IsSelected = true;
			}
		}

		private void Test_TabItem_GotFocus(object sender, RoutedEventArgs e)
		{
			if (_doubleAccount)
			{
				BotLink_Label.Visibility = Visibility.Visible;
			}
			else
			{
				BotLink_Label.Visibility = Visibility.Hidden;
			}
		}

		private void LinkBroadcaster_Button_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(BroadcasterAccount_Textbox.Text) || BroadcasterAccount_Textbox.Text == "Broadcaster Username")
			{
				MessageBox.Show(Properties.Resources.General_MessageBox_EnterUsername,
					Properties.Resources.General_MessageBox_Error_Title);
				return;
			}

			LinkAccount(BroadcasterAccount_Textbox.Text, true);
		}

		private void LinkBot_Button_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(BotAccount_Textbox.Text) || BotAccount_Textbox.Text == "Bot Username")
			{
				MessageBox.Show(Properties.Resources.General_MessageBox_EnterUsername,
					Properties.Resources.General_MessageBox_Error_Title);
				return;
			}
			LinkAccount(BotAccount_Textbox.Text, false);
		}

		private void Test_Button_Click(object sender, RoutedEventArgs e)
		{
			TestTwitchConnections();
		}

		private void SaveSettings_Button_Click(object sender, RoutedEventArgs e)
		{
			SaveSettings();
			this.Close();
		}

		#endregion

		#region Methods

		private async void LinkAccount(string accountName, bool isBroadcaster)
		{
			if (isBroadcaster)
			{
				_broadCasterAccountName = accountName;
				_broadcasterAuthObject = await _authService.GetUserAuthToken(HelperClass.GetTwitchApiCredentials(_settingsService.CurrentSettings), true);

				if (_broadcasterAuthObject == null)
				{
					_logger.Error("Error on getting AuthObject for Broadcaster Account");
					MessageBox.Show("Could not get AuthObject for Broadcaster Account!", Properties.Resources.General_MessageBox_Error_Title);
					BroadcasterLink_Label.Visibility = Visibility.Hidden;
					BCNext_Button.IsEnabled = false;
					return;
				}

				try
				{
					_broadCasterAvatarUrl = new Uri(await _authService.GetAvatarUrl(accountName, _broadcasterAuthObject.Access_Token));
					BroadcasterAvatar_Image.Source = new BitmapImage(_broadCasterAvatarUrl);
				}
				catch (Exception e)
				{
					_logger.Error("Could not convert Profile Picture", e);
					BroadcasterAvatar_Image.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/nanotwitchleafs_error_logo.png"));
					_broadCasterAvatarUrl = new Uri("pack://application:,,,/Assets/nanotwitchleafs_error_logo.png");
				}

				BroadcasterLink_Label.Visibility = Visibility.Visible;
				// Bring Window Back up
				Activate();
				BCNext_Button.IsEnabled = true;
			}
			else
			{
				_botAccountName = accountName;
				_botAuthObject = await _authService.GetUserAuthToken(HelperClass.GetTwitchApiCredentials(_settingsService.CurrentSettings), false);

				if (_botAuthObject == null)
				{
					_logger.Error("Error on getting AuthObject for Bot Account");
					MessageBox.Show("Could not get AuthObject for Bot Account!", Properties.Resources.General_MessageBox_Error_Title);
					BotLink_Label.Visibility = Visibility.Hidden;
					BotNext_Button.IsEnabled = false;
					return;
				}

				try
				{
					_botAccountAvatarUrl = new Uri(await _authService.GetAvatarUrl(accountName, _botAuthObject.Access_Token));
					BotAccountAvatar_Image.Source = new BitmapImage(_botAccountAvatarUrl);
				}
				catch (Exception e)
				{
					_logger.Error("Could not convert Profile Picture", e);
					BotAccountAvatar_Image.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/nanotwitchleafs_error_logo.png"));
					_botAccountAvatarUrl = new Uri("pack://application:,,,/Assets/nanotwitchleafs_error_logo.png");
				}

				BotLink_Label.Visibility = Visibility.Visible;
				// Bring Window Back up
				Activate();
				BotNext_Button.IsEnabled = true;
			}
		}

		private async void TestTwitchConnections()
		{
			if (_doubleAccount)
			{
				var resultBot = await TestConnection(_botAccountName, _botAuthObject.Access_Token, false);
				var resultBroadcaster =await TestConnection(_broadCasterAccountName, _broadcasterAuthObject.Access_Token, true);
				if (resultBot && resultBroadcaster)
				{
					SendMessageToListBox(Properties.Resources.Window_TwitchLink_Tab_Test_ConTestDone_Text);
					Application.Current.Dispatcher.Invoke(() =>
					{
						TestNext_Button.IsEnabled = true;
					});
				}
			}
			else
			{
				var result = await TestConnection(_broadCasterAccountName, _broadcasterAuthObject.Access_Token, true);
				if (result)
				{
					SendMessageToListBox(Properties.Resources.Window_TwitchLink_Tab_Test_ConTestDone_Text);
					Application.Current.Dispatcher.Invoke(() =>
					{
						TestNext_Button.IsEnabled = true;
					});
				}
			}
		}

		private async Task<bool> TestConnection(string username, string auth, bool isBroadcaster)
		{ 
			// Setup Twitch Client for first "Bot" connection
			// var loggerFactory = LoggerFactory.Create(builder =>
			// {
			// 	builder.;
			// });
			var factory = LoggerFactory.Create(x =>
			{
				x.SetMinimumLevel(LogLevel.Trace);
				x.AddConsole();
			});
			var client = new TwitchClient(null, ClientProtocol.WebSocket, null, factory);
			var credentials = new ConnectionCredentials(username, "oauth:" + auth, disableUsernameCheck: true);
			client.Initialize(credentials);
			// Setup TaskCompetitionSource
			var connectTcs = new TaskCompletionSource<bool>();
			var joinTcs = new TaskCompletionSource<bool>();
			var sendMsgTcs = new TaskCompletionSource<bool>();	
			var disconnectTcs = new TaskCompletionSource<bool>();

			// Setup Events
			client.OnConnected += OnBotClientOnConnected ;
			Task OnBotClientOnConnected(object sender, OnConnectedEventArgs args)
			{
				connectTcs.SetResult(true);
				return Task.CompletedTask;
			}
			
			client.OnIncorrectLogin += OnBotClientIncorrectLogin;
			Task OnBotClientIncorrectLogin(object sender, OnIncorrectLoginArgs e)
			{
				connectTcs.SetResult(false);
				return Task.CompletedTask; 
			}

			client.OnJoinedChannel += OnBotClientJoinedChannel;
			Task OnBotClientJoinedChannel(object sender, OnJoinedChannelArgs args)
			{
				if (client.TwitchUsername != args.Channel)
				{
					joinTcs.SetResult(false);
					return Task.CompletedTask; 
				}
				joinTcs.SetResult(true);
				return Task.CompletedTask; 
			}

			client.OnDisconnected += OnBotClientDisconnected;
			Task OnBotClientDisconnected(object sender, OnDisconnectedEventArgs args)
			{
				disconnectTcs.SetResult(true);
				return Task.CompletedTask;
			}

			client.OnFailureToReceiveJoinConfirmation += OnBotClientFailureToReceiveJoinConfirmation;
			Task OnBotClientFailureToReceiveJoinConfirmation(object sender, OnFailureToReceiveJoinConfirmationArgs args)
			{
				joinTcs.SetResult(false);
				return Task.CompletedTask;
			}

			client.OnMessageReceived += BotClientOnOnMessageReceived;
			Task BotClientOnOnMessageReceived(object sender, OnMessageReceivedArgs e)
			{
				if (e.ChatMessage.Message == "Testing NanoTwitchLeafs Chat Connection")
				{
					sendMsgTcs.SetResult(true);
				}

				return Task.CompletedTask;
			}
			
			client.OnConnectionError += ClientOnOnConnectionError;
			Task ClientOnOnConnectionError(object sender, OnConnectionErrorArgs e)
			{
				var message = "[TwitchConsole] " + e.Error;
				SendMessageToListBox(message);
				return Task.CompletedTask;
			}
			
			client.OnSendReceiveData += ClientOnOnSendReceiveData;
			Task ClientOnOnSendReceiveData(object sender, OnSendReceiveDataArgs e)
			{
				var message = "[TwitchConsole] " + e.Data;
				SendMessageToListBox(message);
				return Task.CompletedTask;
			}

			// Event no longer Exists in 4.0
			// client.log+= OnBotClientLog;
			// void OnBotClientLog(object sender, OnLogArgs e)
			// {
			// 	var message = "[TwitchConsole] " + e.Data;
			// 	SendMessageToListBox(message);
			// }

			try
			{
				// Connect to Twitch
				var isConnected = await client.ConnectAsync();
				if (!isConnected)
				{
					_logger.Error("Could not connect to Twitch Server!");
					return false;
				}

				// Wait for Connect or IncorrectLogin Event - Timeout Delay 5 Seconds
				var connectResultTask = await Task.WhenAny(connectTcs.Task, Task.Delay(TimeSpan.FromSeconds(5)));
				if (connectResultTask != connectTcs.Task)
				{
					_logger.Error("Could not connect to Twitch Server! Timeout reached!");
					return false;
				}
				
				var connectResult = await connectTcs.Task;
				if (!connectResult)
				{
					SendMessageToListBox($"Got 'Login Incorrect' Message for Account {credentials.TwitchUsername}! Please go back and check your Credentials and link again!");
					_logger.Error(($"Incorrect Login for Account {credentials.TwitchUsername}."));
					return false;
				}

				SetProgress(1, isBroadcaster);
				SendMessageToListBox("Connected to Twitch IRC Network!");
				SendMessageToListBox("Try to join Channel: " + client.TwitchUsername);

				// Try to Join Channel

				await client.JoinChannelAsync(client.TwitchUsername);

				var joinResultTask = await Task.WhenAny(joinTcs.Task, Task.Delay(TimeSpan.FromSeconds(2)));
				if (joinResultTask != joinTcs.Task)
				{
					_logger.Error("Could not join Twitch Channel! Timeout reached!");
					return false;
				}

				var joinResult = await joinTcs.Task;
				if (!joinResult)
				{
					SendMessageToListBox($"Could not join Twitch Channel: {_broadCasterAccountName}! Please go back and check your Credentials and link again!");
					_logger.Error(($"Could not join Twitch Channel: {_broadCasterAccountName}."));
					return false;
				}

				SetProgress(2, isBroadcaster);
				SendMessageToListBox($"Joined Channel: {_broadCasterAccountName}");
				SendMessageToListBox("Send Test Message to Chat: 'Testing NanoTwitchLeafs Chat Connection'");

				// Send Test Message to Twitch Channel
				_ = client.SendMessageAsync(_broadCasterAccountName, "Testing NanoTwitchLeafs Chat Connection");

				// Disabled til we can really confirm that the Message was sent
				/*var sendMsgResultTask = await Task.WhenAny(sendMsgTcs.Task, Task.Delay(TimeSpan.FromSeconds(2)));
				if (sendMsgResultTask != sendMsgTcs.Task)
				{
					_logger.Error("Could not send Test Message to Channel! Timeout reached!");
					return;
				}

				var sendMsgResult = await sendMsgTcs.Task;
				if (!sendMsgResult)
				{
					_logger.Error("Could not send Test Message to Channel!");
					return;
				}*/
				SetProgress(3, isBroadcaster);

				// Disconnect from Twitch Server
				await client.DisconnectAsync();

				var disconnectResultTask = await Task.WhenAny(disconnectTcs.Task, Task.Delay(TimeSpan.FromSeconds(2)));
				if (disconnectResultTask != disconnectTcs.Task)
				{
					SendMessageToListBox("Didn't get Disconnection Event after requesting Disconnect from Server! Timeout reached!");
					_logger.Error("Didn't get Disconnection Event after requesting Disconnect from Server! Timeout reached!");
					return false;
				}

				var disconnectResult = await disconnectTcs.Task;
				if (disconnectResult)
				{
					SetProgress(4, isBroadcaster);
					SetProgress(5, isBroadcaster);
					return true;
				}
				return false;
			}
			catch (Exception ex)
			{
				return false;
			}
			finally
			{
				client.OnConnected -= OnBotClientOnConnected;
				client.OnIncorrectLogin -= OnBotClientIncorrectLogin;
				client.OnJoinedChannel -= OnBotClientJoinedChannel;
				client.OnDisconnected -= OnBotClientDisconnected;
				client.OnFailureToReceiveJoinConfirmation -= OnBotClientFailureToReceiveJoinConfirmation;
				client.OnMessageReceived -= BotClientOnOnMessageReceived;
			}
		}
		
		private void SendMessageToListBox(string message)
		{
			var dateTime = DateTime.Now;
			message = $"{dateTime.TimeOfDay} - {message}";

			Application.Current.Dispatcher.Invoke(() =>
			{
				TestConnection_Listbox.Items.Add(message);
				if (VisualTreeHelper.GetChildrenCount(TestConnection_Listbox) > 0)
				{
					Border border = (Border)VisualTreeHelper.GetChild(TestConnection_Listbox, 0);
					ScrollViewer scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);
					scrollViewer.ScrollToBottom();
				}
			});
		}

		private void SetProgress(int progress, bool isBroadcaster)
		{
			Application.Current.Dispatcher.Invoke(() =>
			{
				switch (progress)
				{
					case 1:
						if (isBroadcaster)
						{
							BroadcasterProgress1_Checkbox.IsChecked = true;
						}
						else
						{
							BotProgress1_Checkbox.IsChecked = true;
						}
						break;

					case 2:
						if (isBroadcaster)
						{
							BroadcasterProgress2_Checkbox.IsChecked = true;
						}
						else
						{
							BotProgress2_Checkbox.IsChecked = true;
						}
						break;

					case 3:
						if (isBroadcaster)
						{
							BroadcasterProgress3_Checkbox.IsChecked = true;
						}
						else
						{
							BotProgress3_Checkbox.IsChecked = true;
						}
						break;

					case 4:
						if (isBroadcaster)
						{
							BroadcasterProgress4_Checkbox.IsChecked = true;
						}
						else
						{
							BotProgress4_Checkbox.IsChecked = true;
						}
						break;

					case 5:
						if (isBroadcaster)
						{
							BroadcasterProgress5_Checkbox.IsChecked = true;
						}
						else
						{
							BotProgress5_Checkbox.IsChecked = true;
						}
						break;
				}
			});
		}

		private void SaveSettings()
		{
			_settingsService.CurrentSettings.BotName = _broadCasterAccountName;
			_settingsService.CurrentSettings.BotAvatarUrl = _broadCasterAvatarUrl;
			_settingsService.CurrentSettings.BotAuthObject = _broadcasterAuthObject;
			_settingsService.CurrentSettings.BroadcasterAvatarUrl = _broadCasterAvatarUrl;
			_settingsService.CurrentSettings.BroadcasterAuthObject = _broadcasterAuthObject;
			if (_doubleAccount)
			{
				_settingsService.CurrentSettings.BotName = _botAccountName;
				_settingsService.CurrentSettings.BotAvatarUrl = _botAccountAvatarUrl;
				_settingsService.CurrentSettings.BotAuthObject = _botAuthObject;
			}

			_settingsService.CurrentSettings.ChannelName = _broadCasterAccountName;

			_settingsService.SaveSettings();
		}

		#endregion

		private void TwitchLink_Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (_settingsService.CurrentSettings.BroadcasterAvatarUrl == null || _broadcasterAuthObject == null)
			{
				return;
			}

			((MainWindow)App.Current.MainWindow).TwitchLinkAvatar_Image.Source = new BitmapImage(_settingsService.CurrentSettings.BroadcasterAvatarUrl);
			((MainWindow)App.Current.MainWindow).TwitchLink_Label.Content = $"Connected to Twitch Channel {_settingsService.CurrentSettings.ChannelName}";
			((MainWindow)App.Current.MainWindow).ConnectTwitchAccount_Button.IsEnabled = false;
			((MainWindow)App.Current.MainWindow).DisconnectTwitchAccount_Button.IsEnabled = true;
			((MainWindow)App.Current.MainWindow).ConnectChat_Button.IsEnabled = true;
		}
	}
}