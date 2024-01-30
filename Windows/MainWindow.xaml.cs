using Hardcodet.Wpf.TaskbarNotification;
using log4net;
using log4net.Core;
using log4net.Repository.Hierarchy;
using NanoTwitchLeafs.Colors;
using NanoTwitchLeafs.Objects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using NanoTwitchLeafs.Enums;
using NanoTwitchLeafs.Interfaces;
using Application = System.Windows.Application;
using ComboBox = System.Windows.Controls.ComboBox;
using ListBox = System.Windows.Controls.ListBox;
using MessageBox = System.Windows.MessageBox;

namespace NanoTwitchLeafs.Windows
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public readonly List<string> _twitchChat = new();
		private static readonly ObservableCollection<string> Console = new();
		private LoadingWindow _loadingWindow;

		private readonly ILog _logger = LogManager.GetLogger(typeof(MainWindow));

		private readonly ISettingsService _settingsService;
		private readonly IAnalyticsService _analyticsService;
		private readonly IHypeRateService _hypeRateService;
		private readonly IStreamLabsService _streamLabsService;
		private readonly IStreamLabsAuthService _streamLabsAuthService;
		private readonly IUpdateService _updateService;
		private readonly INanoService _nanoService;
		private readonly ITriggerService _triggerService;
		private readonly IStreamingPlatformService _streamingPlatformService;
		private readonly TaskbarIcon _tbi = new TaskbarIcon();
		private readonly ServiceProvider _serviceProvider = DependencyConfig.ServiceProvider;
		#region Init

		public MainWindow(ISettingsService settingsService, IAnalyticsService analyticsService, IHypeRateService hypeRateService,
			IStreamLabsService streamLabsService, IStreamLabsAuthService streamLabsAuthService, IUpdateService updateService,
			INanoService nanoService, ITriggerService triggerService, IStreamingPlatformService streamingPlatformService)
		{
			_settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
			_analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
			_hypeRateService = hypeRateService ?? throw new ArgumentNullException(nameof(hypeRateService));
			_streamLabsService = streamLabsService ?? throw new ArgumentNullException(nameof(streamLabsService));
			_streamLabsAuthService = streamLabsAuthService ?? throw new ArgumentNullException(nameof(streamLabsAuthService));
			_updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));
			_nanoService = nanoService ?? throw new ArgumentNullException(nameof(nanoService));
			_triggerService = triggerService ?? throw new ArgumentNullException(nameof(triggerService));
			_streamingPlatformService = streamingPlatformService ?? throw new ArgumentNullException(nameof(streamingPlatformService));

			// Set Language before Init of Window
			Constants.SetCultureInfo(_settingsService.CurrentSettings.Language);

			// Init Window and Controls
			InitializeComponent();
			_streamingPlatformService.OnMessageReceived += StreamingPlatformServiceOnMessageReceived;

			_tbi.Icon = new System.Drawing.Icon(@"nanotwitchleafs.ico");
			_tbi.TrayMouseDoubleClick += NotifyIcon_Click;
			_tbi.TrayBalloonTipClicked += NotifyIcon_Click;
			_tbi.PopupActivation = PopupActivationMode.LeftOrDoubleClick;
			_tbi.ContextMenu = new ContextMenu();
			_tbi.MenuActivation = PopupActivationMode.RightClick;
			var itemShow = new MenuItem() { Header = Properties.Resources.Window_NotifyContextMenu_Button_Show_Text };
			itemShow.Click += ItemShow_Click;
			_tbi.ContextMenu.Items.Add(itemShow);
			var itemExit = new MenuItem() { Header = Properties.Resources.Window_Devices_Button_Close };
			itemExit.Click += ItemExit_Click;
			_tbi.ContextMenu.Items.Add(itemExit);
			
			var appender = new NanoTwitchLeafsAppender();
			appender.OnMessageLogged += Logger_OnAppenderMessage;
			((IAppenderAttachable)((Hierarchy)LogManager.GetRepository()).Root).AddAppender(appender);
#if RELEASE
            _tbi.ToolTipText = $"NanoTwitchLeafs {typeof(AppInfoWindow).Assembly.GetName().Version}";
#endif
#if BETA
            _tbi.ToolTipText = $"NanoTwitchLeafs {typeof(AppInfoWindow).Assembly.GetName().Version} - Beta";
#endif
#if DEBUG
			_tbi.ToolTipText = $"NanoTwitchLeafs {typeof(AppInfoWindow).Assembly.GetName().Version} - DEBUG";
#endif

			_logger.Info("Initialize HypeRate Events");
			_hypeRateService.OnHeartRateReceived += HypeRateServiceOnHeartRateReceived;
			_hypeRateService.OnConnect += HypeRateServiceOnOnConnect;
			_hypeRateService.OnDisconnect += HypeRateServiceOnOnDisconnect;
			
			// Initialize Data
			_logger.Info("Initialize Data");
			InitializeData();

			if (_settingsService.CurrentSettings.AutoConnect)
			{
				_logger.Info("Preparing for Auto Connect ...");
				AutoConnect();
			}
			else
			{
				_logger.Info("Auto Connect not enabled!");
			}
		}

		private void StreamingPlatformServiceOnMessageReceived(object sender, ChatMessage chatMessage)
		{
			InsetMessageIntoChatBox(chatMessage);
		}

		private void ItemExit_Click(object sender, RoutedEventArgs e)
		{
			_analyticsService.SendPing(PingType.Stop, "Shutting down");
			Close();
		}

		private void ItemShow_Click(object sender, RoutedEventArgs e)
		{
			if (WindowState == WindowState.Normal)
			{
				this.Activate();
			}
			else
			{
				this.Show();
				this.WindowState = WindowState.Normal;
			}
		}
		private void OnCallLoadingWindow(bool state)
		{
			Application.Current.Dispatcher.Invoke(() =>
			{
				if (state)
				{
					_loadingWindow = new LoadingWindow(_settingsService.CurrentSettings.Language)
					{
						Owner = Main_Window
					};
					_loadingWindow.Show();
				}
				else
				{
					if (_loadingWindow == null || !_loadingWindow.IsVisible)
						return;

					_loadingWindow.Close();
				}
			});
		}

		private void NotifyIcon_Click(object sender, EventArgs e)
		{
			this.Show();
			this.WindowState = WindowState.Normal;
		}

		#endregion

		#region Methods

		private void InitializeData()
		{
			try
			{
				// Fill Language Combobox with available Languages
				var languages = new List<string>
			{
				"Deutsch",
				"English",
                // "French",
                // "Portuguese",
                // "Slovak"
            };

				language_Combobox.ItemsSource = languages;

				switch (_settingsService.CurrentSettings.Language)
				{
					case "de-DE":
						language_Combobox.SelectedIndex = 0;
						break;

					case "en-US":
						language_Combobox.SelectedIndex = 1;
						break;

					// case "fr-FR":
					//     language_Combobox.SelectedIndex = 2;
					//     break;
					//
					// case "pt-BR":
					//     language_Combobox.SelectedIndex = 3;
					//     break;
					//
					// case "sk-SK":
					//     language_Combobox.SelectedIndex = 4;
					//     break;

					default:
						language_Combobox.SelectedIndex = 1;
						break;
				}

				twitchChat_ListBox.ItemsSource = _twitchChat;
				console_ListBox.ItemsSource = Console;

				// Bot Settings
				whisperMode_Checkbox.IsChecked = _settingsService.CurrentSettings.WhisperMode;
				commandPrefix_TextBox.Text = _settingsService.CurrentSettings.CommandPrefix;
				response_CheckBox.IsChecked = _settingsService.CurrentSettings.ChatResponse;

				// Nano Settings
				nanoCmd_Button.IsEnabled = _settingsService.CurrentSettings.NanoSettings.TriggerEnabled;
				nanoCmd_Checkbox.IsChecked = _settingsService.CurrentSettings.NanoSettings.TriggerEnabled;
				nanoCooldown_Checkbox.IsChecked = _settingsService.CurrentSettings.NanoSettings.CooldownEnabled;
				nanoCooldownIgnore_Checkbox.IsEnabled = _settingsService.CurrentSettings.NanoSettings.CooldownIgnore;
				nanoCooldown_TextBox.Text = _settingsService.CurrentSettings.NanoSettings.Cooldown.ToString();
				commandRestore_CheckBox.IsChecked = _settingsService.CurrentSettings.NanoSettings.ChangeBackOnCommand;
				keywordRestore_Checkbox.IsChecked = _settingsService.CurrentSettings.NanoSettings.ChangeBackOnKeyword;

				// App Settings
				autoIPRefresh_Checkbox.IsChecked = _settingsService.CurrentSettings.AutoIpRefresh;
				debugCmd_Checkbox.IsChecked = _settingsService.CurrentSettings.DebugEnabled;
				blacklist_CheckBox.IsChecked = _settingsService.CurrentSettings.BlacklistEnabled;
				UseOwnServiceCredentials_Checkbox.IsChecked = _settingsService.CurrentSettings.UseOwnServiceCredentials;
				TwitchClientId_Textbox.Text = _settingsService.CurrentSettings.TwitchClientId;
				TwitchClientSecret_Textbox.Password = _settingsService.CurrentSettings.TwitchClientSecret;
				analyticsChannel_Checkbox.IsChecked = _settingsService.CurrentSettings.AnalyticsChannelName;
				DebugCmd_Checkbox_Click(this, null);

				// HypeRate
				hypeRateId_Textbox.Text = _settingsService.CurrentSettings.HypeRateId;

				// Twitch Link Settings
				if (_settingsService.CurrentSettings.BroadcasterAvatarUrl != null && _settingsService.CurrentSettings.BroadcasterAuthObject != null)
				{
					TwitchLinkAvatar_Image.Source = new BitmapImage(_settingsService.CurrentSettings.BroadcasterAvatarUrl);
					TwitchLink_Label.Content = Properties.Resources.Window_Main_Tabs_TwitchLogin_AccountLabel_Text2 + _settingsService.CurrentSettings.ChannelName;
					ConnectTwitchAccount_Button.IsEnabled = false;
					DisconnectTwitchAccount_Button.IsEnabled = true;
					ConnectChat_Button.IsEnabled = true;
				}

				// Streamlabs
				if (_settingsService.CurrentSettings.StreamlabsInformation.StreamlabsAToken != null && !string.IsNullOrWhiteSpace(_settingsService.CurrentSettings.StreamlabsInformation.StreamlabsAToken))
				{
					Streamlabs_Image.Source = TwitchLinkAvatar_Image.Source;
					StreamlabsUnlink_Button.IsEnabled = true;
					StreamlabsLink_Button.IsEnabled = false;
					StreamlabsConnectButton.IsEnabled = true;
					StreamlabsInfo_TextBlock.Text = $"Connected to Account {_settingsService.CurrentSettings.ChannelName}.";
				}
				StreamlabsClientId_Textbox.Text = _settingsService.CurrentSettings.StreamlabsClientId;
				StreamlabsClientSecret_Textbox.Password = _settingsService.CurrentSettings.StreamlabsClientSecret;

				ChangeEnabledUi();
			}
			catch (Exception ex)
			{
				_logger.Error("Could not fill Controls. Settings Corrupt. Generating blank Settings instead.");
				_logger.Error(ex.Message, ex);
				_settingsService.ReturnBlankSettings();
				InitializeData();
			}
		}

		private void Logger_OnAppenderMessage(Level level, DateTime logTime, string message)
		{
			Logger_OnMessage($"[{logTime.ToShortTimeString()}][{level}] {message}");
		}

		private void Logger_OnMessage(string message)
		{
			if (console_ListBox == null)
				return;

			console_ListBox.Dispatcher.Invoke(() =>
			{
				Console.Add(message);
				console_ListBox.Items.Refresh();
				UpdateScrollBar(console_ListBox);
			});
		}

		#endregion

		#region UI Logic

		private void blacklist_CheckBox_Click(object sender, RoutedEventArgs e)
		{
			if (blacklist_CheckBox.IsChecked == true)
			{
				_settingsService.CurrentSettings.BlacklistEnabled = true;
				_logger.Info("Blacklist enabled!");
			}
			else if (blacklist_CheckBox.IsChecked == false)
			{
				_settingsService.CurrentSettings.BlacklistEnabled = false;
				_logger.Info("Blacklist disabled!");
			}
		}

		private void blacklist_Button_Click(object sender, RoutedEventArgs e)
		{
			_logger.Info("Show Blacklist Window");
			var blacklistWindow = _serviceProvider.GetRequiredService<BlacklistWindow>();
			blacklistWindow.Owner = this;
			blacklistWindow.Show();
		}

		private async void CheckForUpdate_Button_Click(object sender, RoutedEventArgs e)
		{
			await _updateService.CheckForUpdates();
		}

		private async void LoadEffects_Button_Click(object sender, RoutedEventArgs e)
		{
			if (_settingsService.CurrentSettings.NanoSettings.NanoLeafDevices.Count < 1)
			{
				_logger.Error(Properties.Resources.Code_Main_MessageBox_NoDevice);
				MessageBox.Show(Properties.Resources.Code_Main_MessageBox_NoDevice, Properties.Resources.General_MessageBox_Error_Title, MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			List<string> effects = await _nanoService.GetEffectList(_settingsService.CurrentSettings.NanoSettings.NanoLeafDevices[0]);

			Effects_ComboBox.ItemsSource = effects;
		}

		private async void SetBaseEffect_Button_Click(object sender, RoutedEventArgs e)
		{
			if (Effects_ComboBox.SelectedItem == null)
			{
				return;
			}

			if (string.IsNullOrWhiteSpace(Effects_ComboBox.SelectedItem.ToString()))
			{
				_logger.Warn(Properties.Resources.Code_Main_MessageBox_SelectEffect);
				MessageBox.Show(Properties.Resources.Code_Main_MessageBox_SelectEffect, Properties.Resources.General_MessageBox_Error_Title, MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			foreach (var device in _settingsService.CurrentSettings.NanoSettings.NanoLeafDevices)
			{
				await _nanoService.SetEffect(device, Effects_ComboBox.SelectedItem.ToString());
				_logger.Info($"Set {Effects_ComboBox.SelectedItem} to Device {device.PublicName}");
			}
		}

		private void language_Combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ComboBox comboBox = (ComboBox)sender;
			string selectedLanguage = comboBox.SelectedItem.ToString();
			string selectedLanguageCode;

			switch (selectedLanguage)
			{
				case "English":
					selectedLanguageCode = "en-US";
					break;

				case "Deutsch":
					selectedLanguageCode = "de-DE";
					break;

				// case "French":
				//     selectedLanguageCode = "fr-FR";
				//     break;
				//
				// case "Portuguese":
				//     selectedLanguageCode = "pt-BR";
				//     break;
				//
				// case "Slovak":
				//     selectedLanguageCode = "sk-SK";
				//     break;

				default:
					selectedLanguageCode = "en-US";
					break;
			}

			if (selectedLanguageCode == _settingsService.CurrentSettings.Language)
			{
				return;
			}

			_settingsService.CurrentSettings.Language = selectedLanguageCode;

			if (MessageBox.Show(Properties.Resources.Code_Main_MessageBox_Restart, Properties.Resources.Code_Main_MessageBox_Restart_Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
			{
				AppRestart();
			}
		}

		private async void SetBaseColor_Button_Click(object sender, RoutedEventArgs e)
		{
			if (ColorPicker.SelectedColor == null)
			{
				MessageBox.Show(Properties.Resources.Window_TriggerDetail_ColorPicker_Error,
					Properties.Resources.General_MessageBox_Error_Title);
				return;
			}
			foreach (var device in _settingsService.CurrentSettings.NanoSettings.NanoLeafDevices)
			{
				var pickerColor = ColorPicker.SelectedColor.Value;
				var color = new RgbColor(pickerColor.R, pickerColor.G, pickerColor.B, 255);
				await _nanoService.SetColor(device, color);
			}
		}

		private void ConnectTwitchAccount_Button_Click(object sender, RoutedEventArgs e)
		{
			var twitchLinkWindow = _serviceProvider.GetRequiredService<TwitchLinkWindow>();
			twitchLinkWindow.Owner = this;
			twitchLinkWindow.ShowDialog();
		}

		private void DisconnectTwitchAccount_Button_Click(object sender, RoutedEventArgs e)
		{
			TwitchLinkAvatar_Image.Source = new BitmapImage(new Uri("/NanoTwitchLeafs;component/Assets/nanotwitchleafs_error_logo.png", UriKind.Relative));
			_settingsService.CurrentSettings.BroadcasterAvatarUrl = null;
			_settingsService.CurrentSettings.BotAvatarUrl = null;
			_settingsService.CurrentSettings.BotName = null;
			_settingsService.CurrentSettings.BotAuthObject = null;
			_settingsService.CurrentSettings.BroadcasterAuthObject = null;
			_settingsService.CurrentSettings.BroadcasterAvatarUrl = null;
			_settingsService.CurrentSettings.BotAvatarUrl = null;
			_settingsService.CurrentSettings.ChannelName = null;
			if (_streamingPlatformService.IsConnected())
			{
				DisconnectFromChat();
			}
			_settingsService.SaveSettings();
			DisconnectTwitchAccount_Button.IsEnabled = false;
			ConnectTwitchAccount_Button.IsEnabled = true;
			ConnectChat_Button.IsEnabled = false;
			TwitchLink_Label.Content = Properties.Resources.Window_Main_Tabs_TwitchLogin_AccountLabel_Text;
		}

		private void ConnectChat_Button_Click(object sender, RoutedEventArgs e)
		{
			ConnectStreamingServices();
		}

		private void DisconnectChat_Button_Click(object sender, RoutedEventArgs e)
		{
			DisconnectFromChat();
		}
		private void Main_Window_Closed(object sender, EventArgs e)
		{
			Window_Closing(null, null);
		}

		private void StreamlabsConnectButton_Click(object sender, RoutedEventArgs e)
		{
			_logger.Info("Connect to Streamlabs Socket");
			_streamLabsService.Connect();

			StreamlabsConnectButton.IsEnabled = false;
			StreamlabsDisconnectButton.IsEnabled = true;
		}

		private void StreamlabsDisconnectButton_Click(object sender, RoutedEventArgs e)
		{
			if (_streamLabsService.IsConnected())
			{
				_logger.Info("Disconnect from Streamlabs Socket");
				_streamLabsService.Disconnect();
			}
			StreamlabsConnectButton.IsEnabled = true;
			StreamlabsDisconnectButton.IsEnabled = false;
		}

		private void HypeRateConnect_Button_Click(object sender, RoutedEventArgs e)
		{
			_settingsService.CurrentSettings.HypeRateId = hypeRateId_Textbox.Text;
			if (_settingsService.CurrentSettings.HypeRateId == "HypeRateId" || string.IsNullOrWhiteSpace(_settingsService.CurrentSettings.HypeRateId))
			{
				MessageBox.Show(Properties.Resources.Window_Main_Tabs_HypeRate_MessageBox_EnterID_Text, Properties.Resources.General_MessageBox_Error_Title,
					MessageBoxButton.OK, MessageBoxImage.Error);
				_logger.Error("Please enter your HypeRate Id");
				return;
			}
			_hypeRateService.Connect();
		}

		private void HypeRateDisconnect_Button_Click(object sender, RoutedEventArgs e)
		{
			_hypeRateService.Disconnect();
		}

		private void HypeRateServiceOnOnDisconnect(object sender, EventArgs e)
		{
			Application.Current.Dispatcher.Invoke(() =>
			{
				HypeRateConnect_Button.IsEnabled = true;
				HypeRateDisconnect_Button.IsEnabled = false;
			});
		}

		private void HypeRateServiceOnOnConnect(object sender, EventArgs e)
		{
			Application.Current.Dispatcher.Invoke(() =>
			{
				HypeRateConnect_Button.IsEnabled = false;
				HypeRateDisconnect_Button.IsEnabled = true;
			});
		}

		private void HypeRateServiceOnHeartRateReceived(object sender, int hearthRate)
		{
			Application.Current.Dispatcher.Invoke(() =>
			{
				hypeRate_Label.Content = hearthRate;
			});
		}
		private async void StreamlabsLink_Button_Click(object sender, RoutedEventArgs e)
		{
			if (await _streamLabsAuthService.LinkAccount())
			{
				Streamlabs_Image.Source = TwitchLinkAvatar_Image.Source;
				StreamlabsLink_Button.IsEnabled = false;
				StreamlabsUnlink_Button.IsEnabled = true;
				StreamlabsConnectButton.IsEnabled = true;
				StreamlabsInfo_TextBlock.Text = $"{Properties.Resources.Windows_Main_Tabs_Streamlabs_Linktext_Textblock2} {_settingsService.CurrentSettings.ChannelName}.";
			}
			else
			{
				_logger.Error("Could not connect Streamlabs Account!");
			}
		}

		private void StreamlabsUnlink_Button_Click(object sender, RoutedEventArgs e)
		{
			_settingsService.CurrentSettings.StreamlabsInformation = new StreamlabsInformation();
			StreamlabsInfo_TextBlock.Text = Properties.Resources.Windows_Main_Tabs_Streamlabs_Linktext_Textblock1;
			StreamlabsLink_Button.IsEnabled = true;
			StreamlabsUnlink_Button.IsEnabled = false;
			StreamlabsConnectButton.IsEnabled = false;
			Streamlabs_Image.Source = new BitmapImage(new Uri("/NanoTwitchLeafs;component/Assets/nanotwitchleafs_error_logo.png", UriKind.Relative));
		}

		private void HypeRateDiscord_Label_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			//https://discord.gg/dqutnTAj6j
			Process.Start("https://discord.gg/dqutnTAj6j");
		}

		private void HypeRateWebsite_Label_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			//https://hyperate.io
			Process.Start("https://hyperate.io");
		}

		private void UseOwnServiceCredentials_Checkbox_Click(object sender, RoutedEventArgs e)
		{
			if (UseOwnServiceCredentials_Checkbox.IsChecked == true)
			{
				_settingsService.CurrentSettings.UseOwnServiceCredentials = true;
				TwitchClientId_Textbox.IsEnabled = true;
				TwitchClientSecret_Textbox.IsEnabled = true;
				StreamlabsClientId_Textbox.IsEnabled = true;
				StreamlabsClientSecret_Textbox.IsEnabled = true;
			}
			else
			{
				_settingsService.CurrentSettings.UseOwnServiceCredentials = false;
				TwitchClientId_Textbox.IsEnabled = false;
				TwitchClientSecret_Textbox.IsEnabled = false;
				StreamlabsClientId_Textbox.IsEnabled = false;
				StreamlabsClientSecret_Textbox.IsEnabled = false;
			}
		}

		private void Open_Dir_Button_Click(object sender, RoutedEventArgs e)
		{
			Process.Start(Constants.PROGRAMFILESFOLDER_PATH);
		}

		private void Save_Button_Click(object sender, RoutedEventArgs e)
		{
			// ParseSettings
			ParseValuesIntoAppSettings();

			// Save all
			_settingsService.SaveSettings();

			MessageBox.Show(Properties.Resources.General_MessageBox_SettingsSaved, Properties.Resources.General_MessageBox_Sucess_Title, MessageBoxButton.OK, MessageBoxImage.Information);
		}

		private void ParseValuesIntoAppSettings()
		{
			// Bot Settings
			_settingsService.CurrentSettings.WhisperMode = (bool)whisperMode_Checkbox.IsChecked;
			_settingsService.CurrentSettings.CommandPrefix = commandPrefix_TextBox.Text;
			_settingsService.CurrentSettings.ChatResponse = (bool)response_CheckBox.IsChecked;

			// Nano Settings
			_settingsService.CurrentSettings.NanoSettings.TriggerEnabled = (bool)nanoCmd_Checkbox.IsChecked;
			_settingsService.CurrentSettings.NanoSettings.CooldownEnabled = (bool)nanoCooldown_Checkbox.IsChecked;
			_settingsService.CurrentSettings.NanoSettings.Cooldown = Convert.ToInt32(nanoCooldown_TextBox.Text);
			_settingsService.CurrentSettings.NanoSettings.ChangeBackOnCommand = (bool)commandRestore_CheckBox.IsChecked;
			_settingsService.CurrentSettings.NanoSettings.ChangeBackOnKeyword = (bool)keywordRestore_Checkbox.IsChecked;

			// App Settings
			_settingsService.CurrentSettings.AutoIpRefresh = (bool)autoIPRefresh_Checkbox.IsChecked;
			_settingsService.CurrentSettings.DebugEnabled = (bool)debugCmd_Checkbox.IsChecked;
			_settingsService.CurrentSettings.AutoConnect = (bool)autoConnect_Checkbox.IsChecked;
			_settingsService.CurrentSettings.UseOwnServiceCredentials = (bool)UseOwnServiceCredentials_Checkbox.IsChecked;
			_settingsService.CurrentSettings.TwitchClientId = TwitchClientId_Textbox.Text;
			_settingsService.CurrentSettings.TwitchClientSecret = TwitchClientSecret_Textbox.Password;
			_settingsService.CurrentSettings.AnalyticsChannelName = (bool)analyticsChannel_Checkbox.IsChecked;

			// Hype Rate
			_settingsService.CurrentSettings.HypeRateId = hypeRateId_Textbox.Text;

			// Streamlabs
			_settingsService.CurrentSettings.StreamlabsClientId = StreamlabsClientId_Textbox.Text;
			_settingsService.CurrentSettings.StreamlabsClientSecret = StreamlabsClientSecret_Textbox.Password;
		}

		private void SendMessage_Button_Click(object sender, RoutedEventArgs e)
		{
			string username = _settingsService.CurrentSettings.BotName;
			string message = sendMessage_TextBox.Text;
			_streamingPlatformService.SendMessage(message);
			var chatMessage = new ChatMessage(StreamingPlatformEnum.Sent, username, true, true, true, message, new Color());
			_triggerService.HandleMessage(chatMessage);
			sendMessage_TextBox.Text = "";
		}

		private void InsetMessageIntoChatBox(ChatMessage chatMessage)
		{
			string formattedMessage = $"[{DateTime.Now.ToLongTimeString()}][{chatMessage.Platform}] {chatMessage.Username}: {chatMessage.Message}";

			_logger.Info(formattedMessage);

			twitchChat_ListBox.Dispatcher.Invoke(() =>
			{
				_twitchChat.Add(formattedMessage);
				twitchChat_ListBox.Items.Refresh();
				UpdateScrollBar(twitchChat_ListBox);
			});
		}

		private void SendMessage_TextBox_TouchEnter(object sender, System.Windows.Input.TouchEventArgs e)
		{
			SendMessage_Button_Click(sender, e);
		}

		private void NanoCmd_Button_Click(object sender, RoutedEventArgs e)
		{
			var triggerWindow = _serviceProvider.GetRequiredService<TriggerWindow>();
			triggerWindow.Owner = this;
			

			if (!CheckForDuplicateWindow(triggerWindow))
			{
				triggerWindow.Show();
			}
		}

		private void NanoCmd_Checkbox_Click(object sender, RoutedEventArgs e)
		{
#if !DEBUG
            if (_settingsService.CurrentSettings.DebugEnabled == false && _settingsService.CurrentSettings.NanoSettings.NanoLeafDevices.Count < 1)
            {
                MessageBox.Show(Properties.Resources.Code_Main_MessageBox_PairDevice, Properties.Resources.General_MessageBox_Error_Title);
                _logger.Error("Can't Enable Commands without Pairing and Test Connection first!");
                nanoCmd_Checkbox.IsChecked = false;
                nanoCmd_Button.IsEnabled = false;
                return;
            }
#endif

			if (nanoCmd_Checkbox.IsChecked == true)
			{
				nanoCmd_Button.IsEnabled = true;
				_settingsService.CurrentSettings.NanoSettings.TriggerEnabled = true;
			}
			else if (nanoCmd_Checkbox.IsChecked == false)
			{
				nanoCmd_Button.IsEnabled = false;
				_settingsService.CurrentSettings.NanoSettings.TriggerEnabled = false;
			}
		}

		private void nanoPairing_Button_Click(object sender, RoutedEventArgs e)
		{
			PairingWindow pairingWindow = _serviceProvider.GetRequiredService<PairingWindow>();
			pairingWindow.Owner = this;
			

			if (!CheckForDuplicateWindow(pairingWindow))
			{
				pairingWindow.Show();
			}
		}

		private async void NanoTestConnection_Button_Click(object sender, RoutedEventArgs e)
		{
			await TestNanoConnection();
		}

		private void AppInfo_Button_Click(object sender, RoutedEventArgs e)
		{
			AppInfoWindow appInfoWindow = _serviceProvider.GetRequiredService<AppInfoWindow>();
			appInfoWindow.Owner = this;

			if (!CheckForDuplicateWindow(appInfoWindow))
			{
				appInfoWindow.Show();
			}
		}

		private void SendMessage_TextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == System.Windows.Input.Key.Enter)
			{
				SendMessage_Button_Click(sender, e);
			}
		}

		private void WhisperMode_Checkbox_Click(object sender, RoutedEventArgs e)
		{
			if (whisperMode_Checkbox.IsChecked == true)
			{
				_settingsService.CurrentSettings.WhisperMode = true;
				_logger.Info("Whisper Mode enabled!");
			}
			else if (whisperMode_Checkbox.IsChecked == false)
			{
				_settingsService.CurrentSettings.WhisperMode = false;
				_logger.Info("Whisper Mode disabled!");
			}
		}

		private void DebugCmd_Checkbox_Click(object sender, RoutedEventArgs e)
		{
			if (debugCmd_Checkbox.IsChecked == true)
			{
				_settingsService.CurrentSettings.DebugEnabled = true;
				((Hierarchy)LogManager.GetRepository()).Root.Level = Level.Debug;
				((Hierarchy)LogManager.GetRepository()).RaiseConfigurationChanged(EventArgs.Empty);
				_logger.Info("Debug Mode enabled!");
			}
			else if (debugCmd_Checkbox.IsChecked == false)
			{
				_settingsService.CurrentSettings.DebugEnabled = false;
				((Hierarchy)LogManager.GetRepository()).Root.Level = Level.Info;
				((Hierarchy)LogManager.GetRepository()).RaiseConfigurationChanged(EventArgs.Empty);
				_logger.Info("Debug Mode disabled!");
			}
		}

		public void UpdateScrollBar(ListBox listBox)
		{
			if (listBox != null && listBox.Items.Count > 0)
			{
				listBox.SelectedIndex = listBox.Items.Count - 1;
				listBox.UpdateLayout();
				listBox.ScrollIntoView(listBox.Items[listBox.Items.Count - 1]);
			}
		}

		private void ChatConsole_TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var item = (TabItem)chatconsole_TabControl.SelectedItem;

			if (item == null)
				return;

			if (item.Name == "chat_Tabitem")
			{
				UpdateScrollBar(twitchChat_ListBox);
			}
			else
			{
				UpdateScrollBar(console_ListBox);
			}
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
		}

		private void Application_Exit(object sender, ExitEventArgs e)
		{
		}

		private void autoIPRefresh_Checkbox_Click(object sender, RoutedEventArgs e)
		{
			if (autoIPRefresh_Checkbox.IsChecked == true)
			{
				_settingsService.CurrentSettings.AutoIpRefresh = true;
			}
			else
			{
				_settingsService.CurrentSettings.AutoIpRefresh = false;
			}
		}

		private void NanoCooldown_TextBox_TextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
		{
			_settingsService.CurrentSettings.NanoSettings.Cooldown = Convert.ToInt32(nanoCooldown_TextBox.Text);
		}

		private void NanoCooldown_TextBox_LostFocus(object sender, RoutedEventArgs e)
		{
			_settingsService.CurrentSettings.NanoSettings.Cooldown = Convert.ToInt32(nanoCooldown_TextBox.Text);
		}

		private void NanoCooldown_Checkbox_Click(object sender, RoutedEventArgs e)
		{
			_settingsService.CurrentSettings.NanoSettings.CooldownEnabled = (bool)nanoCooldown_Checkbox.IsChecked;
			_settingsService.CurrentSettings.NanoSettings.Cooldown = Convert.ToInt32(nanoCooldown_TextBox.Text);

			if (_settingsService.CurrentSettings.NanoSettings.CooldownEnabled)
			{
				SendChatResponse(string.Format(Properties.Resources.Code_Main_ChatMessage_CooldownON, _settingsService.CurrentSettings.NanoSettings.Cooldown));
				nanoCooldownIgnore_Checkbox.IsEnabled = true;
				_logger.Info("Cooldown enabled!");
				nanoCooldown_TextBox.IsEnabled = false;
			}
			else
			{
				SendChatResponse(Properties.Resources.Code_Main_ChatMessage_CooldownOFF);
				nanoCooldownIgnore_Checkbox.IsEnabled = false;
				_logger.Info("Cooldown disabled!");
				nanoCooldown_TextBox.IsEnabled = true;
			}
		}

		private void Response_CheckBox_Click(object sender, RoutedEventArgs e)
		{
			_settingsService.CurrentSettings.ChatResponse = (bool)response_CheckBox.IsChecked;
		}

		private void analyticsChannel_Checkbox_Click(object sender, RoutedEventArgs e)
		{
			_settingsService.CurrentSettings.AnalyticsChannelName = (bool)analyticsChannel_Checkbox.IsChecked;
		}

		private void KeywordRestore_Checkbox_Click(object sender, RoutedEventArgs e)
		{
			_settingsService.CurrentSettings.NanoSettings.ChangeBackOnKeyword = (bool)keywordRestore_Checkbox.IsChecked;
		}

		private void CommandRestore_CheckBox_Click(object sender, RoutedEventArgs e)
		{
			_settingsService.CurrentSettings.NanoSettings.ChangeBackOnCommand = (bool)commandRestore_CheckBox.IsChecked;
		}

		private void AutoRestoreHelp_Button_Click(object sender, RoutedEventArgs e)
		{
			MessageBox.Show(Properties.Resources.Code_Main_MessageBox_AutoRestore, Properties.Resources.General_MessageBox_Hint_Title);
		}

		private void EventReset_Button_Click(object sender, RoutedEventArgs e)
		{
			_triggerService.ResetEventQueue();
		}

		private void EventRestart_Button_Click(object sender, RoutedEventArgs e)
		{
			_triggerService.RestartEventQueue();
		}

		private void Responses_Button_Click(object sender, RoutedEventArgs e)
		{
			ResponsesWindow responsesWindow = _serviceProvider.GetRequiredService<ResponsesWindow>();

			if (!CheckForDuplicateWindow(responsesWindow))
			{
				responsesWindow.Show();
			}
		}

		private void AutoConnect_Checkbox_Click(object sender, RoutedEventArgs e)
		{
			if (autoConnect_Checkbox.IsChecked == true)
			{
				_settingsService.CurrentSettings.AutoConnect = true;
				CheckNanoDevices();
			}
			else
			{
				_settingsService.CurrentSettings.AutoConnect = false;
			}
		}

		private void ShowDevices_Button_Click(object sender, RoutedEventArgs e)
		{
			_logger.Info("Show Device Info");
			DevicesInfoWindow devicesInfoWindow = _serviceProvider.GetRequiredService<DevicesInfoWindow>();
			devicesInfoWindow.Owner = this;

			if (!CheckForDuplicateWindow(devicesInfoWindow))
			{
				devicesInfoWindow.Show();
			}
		}

		private void FaqLink_Button_Click(object sender, RoutedEventArgs e)
		{
			Process.Start("https://nanotwitchleafs.de/faq/");
		}

		private async void ResetBrightness_Button_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				int brightness = Convert.ToInt32(brightnessReset_TextBox.Text);
				if (brightness < 0 || brightness > 100)
				{
					MessageBox.Show(Properties.Resources.Code_Main_MessageBox_Value0100, Properties.Resources.General_MessageBox_Error_Title);
					return;
				}

				foreach (NanoLeafDevice device in _settingsService.CurrentSettings.NanoSettings.NanoLeafDevices)
				{
					await _nanoService.SetBrightness(device, brightness);
					_logger.Info($"Reset Brightness of Device {device.PublicName} to {brightness}% ");
				}
			}
			catch
			{
				MessageBox.Show(Properties.Resources.Code_Main_MessageBox_Value0100, Properties.Resources.General_MessageBox_Error_Title);
				_logger.Error("Value has to be between 0 and 100!");
			}
		}

		private void commandPrefix_TextBox_LostFocus(object sender, RoutedEventArgs e)
		{
			if (String.IsNullOrWhiteSpace(commandPrefix_TextBox.Text))
			{
				MessageBox.Show(Properties.Resources.Code_Main_MessageBox_EmptyPrefix, Properties.Resources.General_MessageBox_Error_Title);
				_logger.Warn("CommandPrefix Box cannot be Empty!");
				commandPrefix_TextBox.Text = "!";
			}
		}

		private void NanoCooldownIgnore_Checkbox_Click(object sender, RoutedEventArgs e)
		{
			_settingsService.CurrentSettings.NanoSettings.CooldownIgnore = nanoCooldownIgnore_Checkbox.IsEnabled;
		}

		#endregion

		#region Methods

		private void ConnectStreamingServices()
		{
			try
			{
				_streamingPlatformService.Connect();
				ConnectChat_Button.IsEnabled = false;
				DisconnectChat_Button.IsEnabled = true;
				sendMessage_TextBox.IsEnabled = true;
				sendMessage_Button.IsEnabled = true;
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				_logger.Error(ex);
			}
		}

		private void DisconnectFromChat()
		{
			try
			{
				_streamingPlatformService.Disconnect();
				ConnectChat_Button.IsEnabled = true;
				DisconnectChat_Button.IsEnabled = false;
				sendMessage_TextBox.IsEnabled = false;
				sendMessage_Button.IsEnabled = false;
			}
			catch (Exception e)
			{
				_logger.Error(e.Message);
				_logger.Error(e);
			}
		}

		private void AppRestart()
		{
			var info = new ProcessStartInfo
			{
				Arguments = "/C ping 127.0.0.1 -n 2 && \"" + System.Reflection.Assembly.GetEntryAssembly()?.Location + "\"",
				WindowStyle = ProcessWindowStyle.Hidden,
				CreateNoWindow = true,
				FileName = "cmd.exe"
			};
			Process.Start(info);
			this.Close();
		}

		private bool CheckForDuplicateWindow(Window newWindow)
		{
			var count = 0;
			foreach (Window window in App.Current.Windows)
			{
				if (window.Name == newWindow.Name)
				{
					count++;
				}
			}

			return count != 1;
		}

		private void ChangeEnabledUi()
		{
			if (_settingsService.CurrentSettings.NanoSettings.NanoLeafDevices.Count != 0)
			{
				nanoTestConnection_Button.IsEnabled = true;
			}
			else
			{
				nanoTestConnection_Button.IsEnabled = false;
			}

			if (_settingsService.CurrentSettings.AutoConnect)
			{
				autoConnect_Checkbox.IsChecked = true;
			}
			else
			{
				autoConnect_Checkbox.IsChecked = false;
			}

			if (_settingsService.CurrentSettings.NanoSettings.CooldownEnabled)
			{
				nanoCooldown_TextBox.IsEnabled = false;
			}

			if (_settingsService.CurrentSettings.UseOwnServiceCredentials)
			{
				TwitchClientId_Textbox.IsEnabled = true;
				TwitchClientSecret_Textbox.IsEnabled = true;
				StreamlabsClientId_Textbox.IsEnabled = true;
				StreamlabsClientSecret_Textbox.IsEnabled = true;
			}

			help_TextBlock.Text = string.Format(Properties.Resources.Code_Main_Label_NanoHelpText, _settingsService.CurrentSettings.CommandPrefix);
		}

		private void _twitchController_OnConsoleMessageReceived(string message)
		{
			_logger.Info(message);
		}

		private void _twitchController_OnChatMessageReceived(ChatMessage chatMessage)
		{
			string formattedMessage = $"{DateTime.Now.ToLongTimeString()}: {chatMessage.Username} - {chatMessage.Message}";

			twitchChat_ListBox.Dispatcher.Invoke(() =>
			{
				_twitchChat.Add(formattedMessage);
				twitchChat_ListBox.Items.Refresh();
				UpdateScrollBar(twitchChat_ListBox);
			});
		}

		private void SendChatResponse(string message)
		{
			if (!_settingsService.CurrentSettings.ChatResponse)
			{
				return;
			}

			_streamingPlatformService.SendMessage(message);
		}

		private async Task<bool> TestNanoConnection()
		{
			nanoInfo_TextBox.IsEnabled = true;
			nanoInfo_TextBox.Text = "";
			int deviceCount = _settingsService.CurrentSettings.NanoSettings.NanoLeafDevices.Count;
			if (deviceCount > 1)
			{
				nanoInfo_TextBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
				nanoInfo_TextBox.IsReadOnly = true;
			}
			foreach (var device in _settingsService.CurrentSettings.NanoSettings.NanoLeafDevices)
			{
				if (string.IsNullOrEmpty(device.Address) || string.IsNullOrWhiteSpace(device.Address))
				{
					_logger.Error(Properties.Resources.Code_Main_MessageBox_ConTestFail + " " + Properties.Resources.Code_Main_MessageBox_AdressNull + " " + Properties.Resources.Code_Main_MessageBox_UpdateOrRepair);
					MessageBox.Show(Properties.Resources.Code_Main_MessageBox_ConTestFail + " " + Properties.Resources.Code_Main_MessageBox_AdressNull + " " + Properties.Resources.Code_Main_MessageBox_UpdateOrRepair, Properties.Resources.General_MessageBox_Error_Title);
					return false;
				}

				try
				{
					var controllerInfo = await _nanoService.GetControllerInfo(device);

					if (controllerInfo == null)
					{
						_logger.Error(string.Format(Properties.Resources.Code_Main_MessageBox_ConTestFail + " " + Properties.Resources.Code_Main_MessageBox_ControllerInfoNull, device.DeviceName));
						MessageBox.Show(Properties.Resources.Code_Main_MessageBox_ConTestFail + " " + Properties.Resources.Code_Main_MessageBox_ControllerInfoNull, Properties.Resources.General_MessageBox_Error_Title);
						return false;
					}

					if (_settingsService.CurrentSettings.NanoSettings.TriggerEnabled)
					{
						nanoCmd_Button.IsEnabled = true;
					}
					else
					{
						nanoCmd_Button.IsEnabled = false;
					}

					nanoInfo_TextBox.Text += $"Name: {device.PublicName}" + Environment.NewLine;
					nanoInfo_TextBox.Text += $"Model: {controllerInfo.model}" + Environment.NewLine;
					nanoInfo_TextBox.Text += $"Serial Number: {controllerInfo.serialNo}" + Environment.NewLine;
					nanoInfo_TextBox.Text += $"Firmware Version: {controllerInfo.firmwareVersion}" + Environment.NewLine;
					nanoInfo_TextBox.Text += $"Is On: {controllerInfo.state.on.value}" + Environment.NewLine;
					nanoInfo_TextBox.Text += $"Number of Panels: {controllerInfo.panelLayout.layout.numPanels}"+ Environment.NewLine;
					nanoInfo_TextBox.Text += $"Current Effect: {controllerInfo.effects.select}" + Environment.NewLine;
					nanoInfo_TextBox.Text += $"--------------------------------" + Environment.NewLine;

					deviceCount--;
					if (controllerInfo.effects.select == "*Shuffle*" || controllerInfo.effects.select == "*Dynamic*")
					{
						MessageBox.Show(Properties.Resources.Code_Main_MessageBox_UndefinedEffect, Properties.Resources.Code_Main_MessageBox_UndefinedEffect_Title);
						_logger.Warn(Properties.Resources.Code_Main_MessageBox_UndefinedEffect);
					}

					device.NanoleafControllerInfo = controllerInfo;
				}
				catch (Exception ex)
				{
					MessageBox.Show(Properties.Resources.Code_Main_MessageBox_ConFail, Properties.Resources.General_MessageBox_Error_Title);

					_logger.Error(ex.Message, ex);
					return false;
				}
			}

			nanoInfo_TextBox.Text += $"Devices Total: {_settingsService.CurrentSettings.NanoSettings.NanoLeafDevices.Count}" + Environment.NewLine;

			_settingsService.SaveSettings();

			return true;
		}

		private bool CheckNanoDevices()
		{
			if (_settingsService.CurrentSettings.NanoSettings.NanoLeafDevices.Count == 0)
			{
				MessageBox.Show(Properties.Resources.Code_Main_MessageBox_NoDevice, Properties.Resources.General_MessageBox_Error_Title);
				_settingsService.CurrentSettings.AutoConnect = false;
				autoConnect_Checkbox.IsChecked = false;
				return false;
			}

			return true;
		}

		private async void AutoConnect()
		{
			if (!CheckNanoDevices())
			{
				_logger.Warn("[Auto Connection] Error in NanoLeaf Settings!");
				return;
			}

			_logger.Info("[Auto Connection] NanoLeaf Settings ... OK");
			_logger.Info("[Auto Connection] Try NanoLeaf Connection Test ...");

			if (!await TestNanoConnection())
			{
				_logger.Error("[Auto Connection] Couldn't connect to NanoLeaf Device!");
				return;
			}

			_logger.Info("[Auto Connection] NanoLeaf Connection ... OK");
			_logger.Info("[Auto Connection] Try to connect to HypeRate ...");

			if (!string.IsNullOrWhiteSpace(_settingsService.CurrentSettings.HypeRateId) && !await TestHypeRateConnection())
			{
				_logger.Error("[Auto Connection] Could not connect to HypeRateIO ... ");
				return;
			}

			if (_hypeRateService.IsConnected())
			{
				HypeRateConnect_Button.IsEnabled = false;
				HypeRateDisconnect_Button.IsEnabled = true;
				_logger.Info("[Auto Connection] Connected to HypeRateIO ... OK");
			}

			_logger.Info("[Auto Connection] Try to connect to Streamlabs ...");
			if (!string.IsNullOrWhiteSpace(_settingsService.CurrentSettings.StreamlabsInformation.StreamlabsSocketToken) && !await TestStreamlabsConnection())
			{
				_logger.Error("[Auto Connection] Could not connect to Streamlabs ... ");
				return;
			}

			if (_streamLabsService.IsConnected())
			{
				StreamlabsConnectButton.IsEnabled = false;
				StreamlabsDisconnectButton.IsEnabled = true;
				_logger.Info("[Auto Connection] Try to connect to Twitch ...");
			}

			ConnectStreamingServices();
		}

		private async Task<bool> TestStreamlabsConnection()
		{
			await _streamLabsService.Connect();

			await Task.Delay(1000 * 1);

			return _streamLabsService.IsConnected();
		}

		private async Task<bool> TestHypeRateConnection()
		{
			_hypeRateService.Connect();

			await Task.Delay(1000 * 1);

			return _hypeRateService.IsConnected();
		}

		#endregion

		private void Export_Settings_Button_OnClick(object sender, RoutedEventArgs e)
		{
			if (MessageBox.Show(Properties.Resources.General_MessageBox_ExportSettings_Text,
				    Properties.Resources.General_MessageBox_ExportSettings_Title, MessageBoxButton.YesNo,
				    MessageBoxImage.Question) == MessageBoxResult.Yes)
			{
				_settingsService.ExportSettings();
			}
		}
	}
}