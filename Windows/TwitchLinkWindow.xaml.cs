using log4net;
using NanoTwitchLeafs.Controller;
using NanoTwitchLeafs.Objects;
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Events;
// ReSharper disable InconsistentNaming

namespace NanoTwitchLeafs.Windows
{
	/// <summary>
	/// Interaction logic for TwitchLinkWindow.xaml
	/// </summary>
	public partial class TwitchLinkWindow : Window
	{
		private readonly ILog _logger = LogManager.GetLogger(typeof(TriggerLogicController));
		private readonly AppSettings _appSettings;
		private readonly TwitchController _twitchController;
		private readonly AppSettingsController _appSettingsController;
		private bool _doubleAccount = false;
		private bool _isBroadcaster = true;
		private string _broadCasterAccountName;
		private string _botAccountName;
		private Uri _broadCasterAvatarUrl;
		private Uri _botAccountAvatarUrl;
		private OAuthObject _broadcasterAuthObject;
		private OAuthObject _botAuthObject;

		public TwitchLinkWindow(AppSettings appSettings, TwitchController twitchController, AppSettingsController appSettingsController)
		{
			_appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
			_twitchController = twitchController ?? throw new ArgumentNullException(nameof(twitchController));
			_appSettingsController = appSettingsController ?? throw new ArgumentNullException(nameof(appSettingsController));
			Constants.SetCultureInfo(_appSettings.Language);
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
				_broadcasterAuthObject = await _twitchController.GetAuthToken(HelperClass.GetTwitchApiCredentials(_appSettings), true);

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
					_broadCasterAvatarUrl = new Uri(await _twitchController.GetAvatarUrl(accountName, _broadcasterAuthObject.Access_Token));
					BroadcasterAvatar_Image.Source = new BitmapImage(_broadCasterAvatarUrl);
				}
				catch (Exception e)
				{
					_logger.Error("Could not convert Profile Picture", e);
					BroadcasterAvatar_Image.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/nanotwitchleafs_error_logo.png"));
					_broadCasterAvatarUrl = new Uri("pack://application:,,,/Assets/nanotwitchleafs_error_logo.png");
				}

				BroadcasterLink_Label.Visibility = Visibility.Visible;
				BCNext_Button.IsEnabled = true;
			}
			else
			{
				_botAccountName = accountName;
				_botAuthObject = await _twitchController.GetAuthToken(HelperClass.GetTwitchApiCredentials(_appSettings), false);

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
					_botAccountAvatarUrl = new Uri(await _twitchController.GetAvatarUrl(accountName, _botAuthObject.Access_Token));
					BotAccountAvatar_Image.Source = new BitmapImage(_botAccountAvatarUrl);
				}
				catch (Exception e)
				{
					_logger.Error("Could not convert Profile Picture", e);
					BotAccountAvatar_Image.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/nanotwitchleafs_error_logo.png"));
					_botAccountAvatarUrl = new Uri("pack://application:,,,/Assets/nanotwitchleafs_error_logo.png");
				}

				BotLink_Label.Visibility = Visibility.Visible;
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
			var client = new TwitchClient();
			var credentials = new ConnectionCredentials(username, "oauth:" + auth, disableUsernameCheck: true);
			client.Initialize(credentials);
			// Setup TaskCompetitionSource
			var connectTcs = new TaskCompletionSource<bool>();
			var joinTcs = new TaskCompletionSource<bool>();
			var sendMsgTcs = new TaskCompletionSource<bool>();	
			var disconnectTcs = new TaskCompletionSource<bool>();

			// Setup Events
			void OnBotClientConnected(object sender, OnConnectedArgs args)
			{
				connectTcs.SetResult(true);
			}

			client.OnConnected += OnBotClientConnected;

			void OnBotClientIncorrectLogin(object sender, OnIncorrectLoginArgs args) => connectTcs.SetResult(false);

			client.OnIncorrectLogin += OnBotClientIncorrectLogin;

			void OnBotClientJoinedChannel(object sender, OnJoinedChannelArgs args)
			{
				if (client.TwitchUsername != args.Channel)
				{
					joinTcs.SetResult(false);
					return;
				}
				joinTcs.SetResult(true);
			}

			client.OnJoinedChannel += OnBotClientJoinedChannel;

			void OnBotClientDisconnected(object sender, OnDisconnectedEventArgs args) => disconnectTcs.SetResult(true);

			client.OnDisconnected += OnBotClientDisconnected;

			void OnBotClientFailureToReceiveJoinConfirmation(object sender, OnFailureToReceiveJoinConfirmationArgs args) => joinTcs.SetResult(false);

			client.OnFailureToReceiveJoinConfirmation += OnBotClientFailureToReceiveJoinConfirmation;
			client.OnMessageReceived += BotClientOnOnMessageReceived;
			
			void BotClientOnOnMessageReceived(object sender, OnMessageReceivedArgs e)
			{
				if (e.ChatMessage.Message == "Testing NanoTwitchLeafs Chat Connection")
				{
					sendMsgTcs.SetResult(true);
				}
			}
			
			client.OnLog+= OnBotClientLog;
			void OnBotClientLog(object sender, OnLogArgs e)
			{
				var message = "[TwitchConsole] " + e.Data;
				SendMessageToListBox(message);
			}

			try
			{
				// Connect to Twitch
				var isConnected = client.Connect();
				if (!isConnected)
				{
					_logger.Error("Could not connect to Twitch Server!");
					return false;
				}

				// Wait for Connect or IncorrectLogin Event - Timeout Delay 2 Seconds
				var connectResultTask = await Task.WhenAny(connectTcs.Task, Task.Delay(TimeSpan.FromSeconds(2)));
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

				client.JoinChannel(client.TwitchUsername);

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
				client.SendMessage(_broadCasterAccountName, "Testing NanoTwitchLeafs Chat Connection");

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
				client.Disconnect();

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
				client.OnConnected -= OnBotClientConnected;
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
			_appSettings.BotName = _broadCasterAccountName;
			_appSettings.BotAvatarUrl = _broadCasterAvatarUrl;
			_appSettings.BotAuthObject = _broadcasterAuthObject;
			_appSettings.BroadcasterAvatarUrl = _broadCasterAvatarUrl;
			_appSettings.BroadcasterAuthObject = _broadcasterAuthObject;
			if (_doubleAccount)
			{
				_appSettings.BotName = _botAccountName;
				_appSettings.BotAvatarUrl = _botAccountAvatarUrl;
				_appSettings.BotAuthObject = _botAuthObject;
			}

			_appSettings.ChannelName = _broadCasterAccountName;

			_appSettingsController.SaveSettings(_appSettings);
		}

		#endregion

		private void TwitchLink_Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(_appSettings.BroadcasterAvatarUrl.ToString()) || _broadcasterAuthObject == null)
			{
				return;
			}

			((MainWindow)App.Current.MainWindow).TwitchLinkAvatar_Image.Source = new BitmapImage(_appSettings.BroadcasterAvatarUrl);
			((MainWindow)App.Current.MainWindow).TwitchLink_Label.Content = $"Connected to Twitch Channel {_appSettings.ChannelName}";
			((MainWindow)App.Current.MainWindow).ConnectTwitchAccount_Button.IsEnabled = false;
			((MainWindow)App.Current.MainWindow).DisconnectTwitchAccount_Button.IsEnabled = true;
			((MainWindow)App.Current.MainWindow).ConnectChat_Button.IsEnabled = true;
		}
	}
}