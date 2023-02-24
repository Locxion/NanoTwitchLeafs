using log4net;
using NanoTwitchLeafs.Controller;
using NanoTwitchLeafs.Objects;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TwitchLib.Client;
using TwitchLib.Client.Models;

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
		private TwitchClient _client = new TwitchClient();
		private TwitchClient _client2 = new TwitchClient();

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
			TestTwitchConnection();
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

		private void TestTwitchConnection()
		{
			var credentials = new ConnectionCredentials(_broadCasterAccountName, "oauth:" + _broadcasterAuthObject.Access_Token);
			_client.Initialize(credentials);

			_client.OnLog += Client_OnLog;
			_client.OnConnected += Client_OnConnected;
			_client.OnJoinedChannel += Client_OnJoinedChannel;
			_client.OnMessageReceived += Client_OnMessageReceived;
			_client.OnIncorrectLogin += Client_OnIncorrectLogin;

			_client.Connect();
		}

		private void Client_OnIncorrectLogin(object sender, TwitchLib.Client.Events.OnIncorrectLoginArgs e)
		{
			SendMessageToListBox($"Got 'Login Incorrect' Message for Account {e.Exception.Username}! Please go back and check your Credentials and link again!");
			_client.Disconnect();
		}

		private void Client_OnMessageReceived(object sender, TwitchLib.Client.Events.OnMessageReceivedArgs e)
		{
			SendMessageToListBox(e.ChatMessage.BotUsername + ": " + e.ChatMessage.Message);
		}

		private void Client_OnConnected(object sender, TwitchLib.Client.Events.OnConnectedArgs e)
		{
			SetProgress(1, _isBroadcaster);
			SendMessageToListBox("Connected to Twitch IRC Network!");
			SendMessageToListBox("Try to join Channel: " + _broadCasterAccountName);
			if (_isBroadcaster)
			{
				_client.JoinChannel(_broadCasterAccountName);
			}
			else
			{
				_client2.JoinChannel(_broadCasterAccountName);
			}
		}

		private async void Client_OnJoinedChannel(object sender, TwitchLib.Client.Events.OnJoinedChannelArgs e)
		{
			SetProgress(2, _isBroadcaster);
			SendMessageToListBox("Joined Channel: " + e.Channel);
			SendMessageToListBox("Send Test Message to Chat: 'Testing NanoTwitchLeafs Chat Connection' ");
			if (_isBroadcaster)
			{
				_client.SendMessage(_broadCasterAccountName, "Testing NanoTwitchLeafs Chat Connection");
			}
			else
			{
				_client2.SendMessage(_broadCasterAccountName, "Testing NanoTwitchLeafs Chat Connection");
			}
			SetProgress(3, _isBroadcaster);
			await Task.Delay(1000);
			SetProgress(4, _isBroadcaster);
			SetProgress(5, _isBroadcaster);

			bool broadCasterDone = false;
			bool botDone = false;

			Application.Current.Dispatcher.Invoke(() =>
			{
				broadCasterDone = BroadcasterProgress5_Checkbox.IsChecked != null && (bool)BroadcasterProgress5_Checkbox.IsChecked;
				botDone = BotProgress5_Checkbox.IsChecked != null && (bool)BotProgress5_Checkbox.IsChecked;
			});

			if (!_doubleAccount)
			{
				if (broadCasterDone)
				{
					SendMessageToListBox(Properties.Resources.Window_TwitchLink_Tab_Test_ConTestDone_Text);
					Application.Current.Dispatcher.Invoke(() =>
					{
						TestNext_Button.IsEnabled = true;
					});
					_client.Disconnect();
					return;
				}
			}
			else
			{
				if (broadCasterDone && botDone)
				{
					SendMessageToListBox(Properties.Resources.Window_TwitchLink_Tab_Test_ConTestDone_Text);
					Application.Current.Dispatcher.Invoke(() =>
					{
						TestNext_Button.IsEnabled = true;
					});
					_client.Disconnect();
					_client2.Disconnect();
					return;
				}
			}

			var credentials = new ConnectionCredentials(_botAccountName, "oauth:" + _botAuthObject.Access_Token);
			_client2.Initialize(credentials);
			_isBroadcaster = false;

			_client2.OnLog += Client_OnLog;
			_client2.OnConnected += Client_OnConnected;
			_client2.OnJoinedChannel += Client_OnJoinedChannel;
			_client2.OnMessageReceived += Client_OnMessageReceived;
			_client2.OnIncorrectLogin += Client_OnIncorrectLogin;
			_client2.Connect();
		}

		private void Client_OnLog(object sender, TwitchLib.Client.Events.OnLogArgs e)
		{
			var message = "[TwitchConsole] " + e.Data;
			SendMessageToListBox(message);
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