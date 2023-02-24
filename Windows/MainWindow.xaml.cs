using Hardcodet.Wpf.TaskbarNotification;
using log4net;
using log4net.Config;
using log4net.Core;
using log4net.Repository.Hierarchy;
using NanoTwitchLeafs.Colors;
using NanoTwitchLeafs.Controller;
using NanoTwitchLeafs.Objects;
using NanoTwitchLeafs.Repositories;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
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
		private readonly List<string> _twitchChat = new List<string>();
		private static readonly ObservableCollection<string> Console = new ObservableCollection<string>();
		private LoadingWindow _loadingWindow;

		private readonly ILog _logger = LogManager.GetLogger(typeof(MainWindow));

		private readonly AppSettingsController _appSettingsController;
		private readonly AppSettings _appSettings;
		private readonly TwitchController _twitchController;
		private readonly NanoController _nanoController;
		private readonly CommandRepository _commandRepository;
		private readonly TwitchPubSubController _twitchPubSubController;
		private readonly StreamlabsController _streamlabsController;
		private readonly TriggerLogicController _triggerLogicController;
		private readonly HypeRateIOController _hypeRatecontroller;
		private readonly UpdateController _updatecontroller;
		private readonly TaskbarIcon _tbi = new TaskbarIcon();

		#region Init

		public MainWindow()
		{
			_appSettingsController = new AppSettingsController();
			_appSettings = _appSettingsController.LoadSettings();

			Constants.SetCultureInfo(_appSettings.Language);
			SetLogLevel(Level.Info);

			InitializeComponent();

#if !DEBUG
            InstanceCheck();
#endif

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

			// Create Nanoleafs directory.
			Directory.CreateDirectory(Constants.PROGRAMFILESFOLDER_PATH);

			// Initialize Logger
			var logFileName = Constants.LOG_PATH;
			GlobalContext.Properties["LogFile"] = logFileName;
			string s = new Uri(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase), "log4net.config")).LocalPath;
			XmlConfigurator.Configure(new FileInfo(s));

			var appender = new NanoTwitchLeafsAppender();
			appender.OnMessageLogged += Logger_OnAppenderMessage;
			((IAppenderAttachable)((Hierarchy)LogManager.GetRepository()).Root).AddAppender(appender);

			// Initialize Controller
#if RELEASE
            _logger.Info($"Start Program - Version: {typeof(AppInfoWindow).Assembly.GetName().Version}");
            _tbi.ToolTipText = $"NanoTwitchLeafs {typeof(AppInfoWindow).Assembly.GetName().Version}";
#endif
#if BETA
            _logger.Info($"Start Program - Version: {typeof(AppInfoWindow).Assembly.GetName().Version} - BETA");
            _tbi.ToolTipText = $"NanoTwitchLeafs {typeof(AppInfoWindow).Assembly.GetName().Version} - Beta";
#endif
#if DEBUG
			_logger.Info($"Start Program - Version: {typeof(AppInfoWindow).Assembly.GetName().Version} - DEBUG");
			_tbi.ToolTipText = $"NanoTwitchLeafs {typeof(AppInfoWindow).Assembly.GetName().Version} - DEBUG";
#endif
			AppDomain.CurrentDomain.UnhandledException += (o, args) =>
			{
				if (args.IsTerminating)
				{
					_appSettings.AutoConnect = false;
					_appSettingsController.SaveSettings(_appSettings);
					_logger.Info("Auto Connect disabled cause of Terminating Exception");
					MessageBox.Show(Properties.Resources.General_MessageBox_GeneralErrorCrash_Text, Properties.Resources.General_MessageBox_Error_Title, MessageBoxButton.OK, MessageBoxImage.Error);
				}
				else
				{
					MessageBox.Show(Properties.Resources.General_MessageBox_GeneralError_Text, Properties.Resources.General_MessageBox_Error_Title, MessageBoxButton.OK, MessageBoxImage.Error);
				}
				_logger.Error($"Exception terminated Program: {args.IsTerminating}");
				_logger.Error(args.ExceptionObject);
			};

			if (_appSettings.DebugEnabled)
				SetLogLevel(Level.Debug);

			_logger.Info("Initialize Update Controller");
			_updatecontroller = new UpdateController();

			_logger.Info("Initialize Twitch Controller");
			_twitchController = new TwitchController(_appSettingsController);
			_twitchController.OnChatMessageReceived += _twitchController_OnChatMessageReceived;
			_twitchController.CallLoadingWindow += OnCallLoadingWindow;

			_logger.Info("Initialize TwitchPubSub Controller");
			_twitchPubSubController = new TwitchPubSubController();
			_twitchController._twitchPubSubController = _twitchPubSubController;

			_logger.Info("Initialize Nano Controller");
			_nanoController = new NanoController(_appSettings);

			_logger.Info("Initialize Trigger Command Repository");

			_commandRepository = new CommandRepository(new DatabaseController<TriggerSetting>(Constants.DATABASE_PATH));

			_logger.Info("Initialize HypeRate Controller");
			_hypeRatecontroller = new HypeRateIOController(_appSettings);
			_hypeRatecontroller.OnHeartRateRecieved += _hypeRatecontroller_OnHeartRateRecieved;
			_hypeRatecontroller.OnHypeRateConnected += _hypeRatecontroller_OnHypeRateConnected;
			_hypeRatecontroller.OnHypeRateDisconnected += _hypeRatecontroller_OnHypeRateDisconnected;

			_logger.Info("Initialize Streamlabs Controller");
			_streamlabsController = new StreamlabsController(_appSettings);
			_logger.Info("Initialize StreamlabsEvetns Controller");

			try
			{
				//This has to be Last!
				_logger.Info("Initialize Trigger Logic Controller");
				_triggerLogicController = new TriggerLogicController(_appSettings, _twitchController, _commandRepository, _nanoController, _twitchPubSubController, _streamlabsController, _hypeRatecontroller);
			}
			catch (Exception ex)
			{
				MessageBox.Show(Properties.Resources.General_MessageBox_WMPinstalled_Error_Text, Properties.Resources.General_MessageBox_Error_Title, MessageBoxButton.OK, MessageBoxImage.Error);
				_logger.Error("Could not initialize Trigger Logic Controller. Check Windows Media Player!");
				_logger.Error(ex.Message, ex);
			}

			// Initialize Data
			_logger.Info("Initialize Data");
			InitializeData();

#if !DEBUG
            CheckForUpdate();
#endif
#if BETA
            MessageBox.Show(Properties.Resources.Code_Main_MessageBox_Beta, Properties.Resources.Code_Main_MessageBox_BetaTitle);
#endif
			// attach property change event to configuration object and its subsequent elements if any derived NotifyObject
			_appSettings.AttachPropertyChanged(_appSettings_PropertyChanged);

			if (_appSettings.AutoIpRefresh)
			{
				_logger.Info($"Refreshing Ip-Address of {_appSettings.NanoSettings.NanoLeafDevices.Count} Devices.");
#pragma warning disable 4014
				_nanoController.UpdateAllNanoleafDevices();
#pragma warning restore 4014
			}

			if (_appSettings.AutoConnect)
			{
				_logger.Info("Preparing for Auto Connect ...");
				AutoConnect();
			}
			else
			{
				_logger.Info("Auto Connect not enabled!");
			}
		}

		private void ItemExit_Click(object sender, RoutedEventArgs e)
		{
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

		public async Task InitializeAsync()
		{
			// Init for early Stuff
			// Was used for License Controller
			// Now empty
		}

		private void OnCallLoadingWindow(bool state)
		{
			Application.Current.Dispatcher.Invoke(() =>
			{
				if (state)
				{
					_loadingWindow = new LoadingWindow(_appSettings.Language)
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

		private void InstanceCheck()
		{
			var process = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly()?.Location));
			if (process.Count() > 1)
			{
				MessageBox.Show(Properties.Resources.Code_Main_MessageBox_InstanceCheck, Properties.Resources.General_MessageBox_Error_Title);
				Process.GetCurrentProcess().Kill();
			}
		}

		private void NotifyIcon_Click(object sender, EventArgs e)
		{
			this.Show();
			this.WindowState = WindowState.Normal;
		}

		protected override void OnStateChanged(EventArgs e)
		{
			// TODO Consider if this should be enabled or not
			// Show Application in Taskbar if you Minimize it

			// if (WindowState == WindowState.Minimized)
			//     this.Hide();

			base.OnStateChanged(e);
		}

		private void _appSettings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			_appSettingsController.SaveSettings(_appSettings);
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

				switch (_appSettings.Language)
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
				whisperMode_Checkbox.IsChecked = _appSettings.WhisperMode;
				commandPrefix_TextBox.Text = _appSettings.CommandPrefix;
				response_CheckBox.IsChecked = _appSettings.ChatResponse;

				// Nano Settings
				nanoCmd_Button.IsEnabled = _appSettings.NanoSettings.TriggerEnabled;
				nanoCmd_Checkbox.IsChecked = _appSettings.NanoSettings.TriggerEnabled;
				nanoCooldown_Checkbox.IsChecked = _appSettings.NanoSettings.CooldownEnabled;
				nanoCooldownIgnore_Checkbox.IsEnabled = _appSettings.NanoSettings.CooldownIgnore;
				nanoCooldown_TextBox.Text = _appSettings.NanoSettings.Cooldown.ToString();
				commandRestore_CheckBox.IsChecked = _appSettings.NanoSettings.ChangeBackOnCommand;
				keywordRestore_Checkbox.IsChecked = _appSettings.NanoSettings.ChangeBackOnKeyword;

				// App Settings
				autoIPRefresh_Checkbox.IsChecked = _appSettings.AutoIpRefresh;
				debugCmd_Checkbox.IsChecked = _appSettings.DebugEnabled;
				blacklist_CheckBox.IsChecked = _appSettings.BlacklistEnabled;
				UseOwnServiceCredentials_Checkbox.IsChecked = _appSettings.UseOwnServiceCredentials;
				TwitchClientId_Textbox.Text = _appSettings.TwitchClientId;
				TwitchClientSecret_Textbox.Password = _appSettings.TwitchClientSecret;
				DebugCmd_Checkbox_Click(this, null);

				// HypeRate
				hypeRateId_Textbox.Text = _appSettings.HypeRateId;

				// Twitch Link Settings
				if (_appSettings.BroadcasterAvatarUrl != null && _appSettings.BroadcasterAuthObject != null)
				{
					TwitchLinkAvatar_Image.Source = new BitmapImage(_appSettings.BroadcasterAvatarUrl);
					TwitchLink_Label.Content = Properties.Resources.Window_Main_Tabs_TwitchLogin_AccountLabel_Text2 + _appSettings.ChannelName;
					ConnectTwitchAccount_Button.IsEnabled = false;
					DisconnectTwitchAccount_Button.IsEnabled = true;
					ConnectChat_Button.IsEnabled = true;
				}

				// Streamlabs
				if (_appSettings.StreamlabsInformation.StreamlabsAToken != null && !string.IsNullOrWhiteSpace(_appSettings.StreamlabsInformation.StreamlabsAToken))
				{
					Streamlabs_Image.Source = TwitchLinkAvatar_Image.Source;
					StreamlabsUnlink_Button.IsEnabled = true;
					StreamlabsLink_Button.IsEnabled = false;
					StreamlabsConnectButton.IsEnabled = true;
					StreamlabsInfo_TextBlock.Text = $"Connected to Account {_appSettings.ChannelName}.";
				}
				StreamlabsClientId_Textbox.Text = _appSettings.StreamlabsClientId;
				StreamlabsClientSecret_Textbox.Password = _appSettings.StreamlabsClientSecret;

				ChangeEnabledUi();
			}
			catch (Exception ex)
			{
				_logger.Error("Could not fill Controls. Settings Corrupt. Generating blank Settings instead.");
				_logger.Error(ex.Message, ex);
				_appSettingsController.SaveSettings(new AppSettings());
				_appSettingsController.LoadSettings();
				InitializeData();
			}
		}

		private void CheckForUpdate()
		{
			_updatecontroller.CheckForUpdates();
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
				_appSettings.BlacklistEnabled = true;
				_logger.Info("Blacklist enabled!");
			}
			else if (blacklist_CheckBox.IsChecked == false)
			{
				_appSettings.BlacklistEnabled = false;
				_logger.Info("Blacklist disabled!");
			}
		}

		private void blacklist_Button_Click(object sender, RoutedEventArgs e)
		{
			_logger.Info("Show Blacklist Window");
			BlacklistWindow blacklistWindow = new BlacklistWindow(_appSettingsController, _appSettings)
			{
				Owner = this
			};
			blacklistWindow.Show();
		}

		private void CheckForUpdate_Button_Click(object sender, RoutedEventArgs e)
		{
			CheckForUpdate();
		}

		private async void LoadEffects_Button_Click(object sender, RoutedEventArgs e)
		{
			if (_appSettings.NanoSettings.NanoLeafDevices.Count < 1)
			{
				_logger.Error(Properties.Resources.Code_Main_MessageBox_NoDevice);
				MessageBox.Show(Properties.Resources.Code_Main_MessageBox_NoDevice, Properties.Resources.General_MessageBox_Error_Title, MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			List<string> effects = await _nanoController.GetEffectList(_appSettings.NanoSettings.NanoLeafDevices[0]);

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

			foreach (var device in _appSettings.NanoSettings.NanoLeafDevices)
			{
				await _nanoController.SetEffect(device, Effects_ComboBox.SelectedItem.ToString());
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

			if (selectedLanguageCode == _appSettings.Language)
			{
				return;
			}

			_appSettings.Language = selectedLanguageCode;

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
			foreach (var device in _appSettings.NanoSettings.NanoLeafDevices)
			{
				var pickerColor = ColorPicker.SelectedColor.Value;
				var color = new RgbColor(pickerColor.R, pickerColor.G, pickerColor.B, 255);
				await _nanoController.SetColor(device, color);
			}
		}

		private void ConnectTwitchAccount_Button_Click(object sender, RoutedEventArgs e)
		{
			var twitchLinkWindow = new TwitchLinkWindow(_appSettings, _twitchController, _appSettingsController) { Owner = this };
			twitchLinkWindow.ShowDialog();
		}

		private void DisconnectTwitchAccount_Button_Click(object sender, RoutedEventArgs e)
		{
			TwitchLinkAvatar_Image.Source = new BitmapImage(new Uri("/NanoTwitchLeafs;component/Assets/nanotwitchleafs_error_logo.png", UriKind.Relative));
			_appSettings.BroadcasterAvatarUrl = null;
			_appSettings.BotAvatarUrl = null;
			_appSettings.BotAuthObject = null;
			_appSettings.BroadcasterAuthObject = null;
			_appSettings.BroadcasterAvatarUrl = null;
			_appSettings.BotAvatarUrl = null;
			_appSettings.ChannelName = null;

			_appSettingsController.SaveSettings(_appSettings);
			DisconnectTwitchAccount_Button.IsEnabled = false;
			ConnectTwitchAccount_Button.IsEnabled = true;
			ConnectChat_Button.IsEnabled = false;
			TwitchLink_Label.Content = Properties.Resources.Window_Main_Tabs_TwitchLogin_AccountLabel_Text;
		}

		private void ConnectChat_Button_Click(object sender, RoutedEventArgs e)
		{
			ConnectToChat();
		}

		private void Main_Window_Closed(object sender, EventArgs e)
		{
			Window_Closing(null, null);
		}

		private void StreamlabsConnectButton_Click(object sender, RoutedEventArgs e)
		{
			_logger.Info("Connect to Streamlabs Socket");
			_streamlabsController.ConnectSocket();

			StreamlabsConnectButton.IsEnabled = false;
			StreamlabsDisconnectButton.IsEnabled = true;
		}

		private void StreamlabsDisconnectButton_Click(object sender, RoutedEventArgs e)
		{
			if (_streamlabsController._IsSocketConnected)
			{
				_logger.Info("Disconnect from Streamlabs Socket");
				_streamlabsController.DisconnectSocket();
			}
			StreamlabsConnectButton.IsEnabled = true;
			StreamlabsDisconnectButton.IsEnabled = false;
		}

		private void HypeRateConnect_Button_Click(object sender, RoutedEventArgs e)
		{
			_appSettings.HypeRateId = hypeRateId_Textbox.Text;
			if (_appSettings.HypeRateId == "HypeRateId" || string.IsNullOrWhiteSpace(_appSettings.HypeRateId))
			{
				MessageBox.Show(Properties.Resources.Window_Main_Tabs_HypeRate_MessageBox_EnterID_Text, Properties.Resources.General_MessageBox_Error_Title,
					MessageBoxButton.OK, MessageBoxImage.Error);
				_logger.Error("Please enter your HypeRate Id");
				return;
			}
			_hypeRatecontroller.StartListener();
		}

		private void HypeRateDisconnect_Button_Click(object sender, RoutedEventArgs e)
		{
			_hypeRatecontroller.Disconnect();
		}

		private void _hypeRatecontroller_OnHypeRateDisconnected()
		{
			Application.Current.Dispatcher.Invoke(() =>
			{
				HypeRateConnect_Button.IsEnabled = true;
				HypeRateDisconnect_Button.IsEnabled = false;
			});
		}

		private void _hypeRatecontroller_OnHypeRateConnected()
		{
			Application.Current.Dispatcher.Invoke(() =>
			{
				HypeRateConnect_Button.IsEnabled = false;
				HypeRateDisconnect_Button.IsEnabled = true;
			});
		}

		private void _hypeRatecontroller_OnHeartRateRecieved(int heartRate)
		{
			Application.Current.Dispatcher.Invoke(() =>
			{
				hypeRate_Label.Content = heartRate;
			});
		}

		private async void StreamlabsLink_Button_Click(object sender, RoutedEventArgs e)
		{
			if (await _streamlabsController.LinkAccount())
			{
				Streamlabs_Image.Source = TwitchLinkAvatar_Image.Source;
				StreamlabsLink_Button.IsEnabled = false;
				StreamlabsUnlink_Button.IsEnabled = true;
				StreamlabsConnectButton.IsEnabled = true;
				StreamlabsInfo_TextBlock.Text = $"{Properties.Resources.Windows_Main_Tabs_Streamlabs_Linktext_Textblock2} {_appSettings.ChannelName}.";
			}
			else
			{
				_logger.Error("Could not connect Streamlabs Account!");
			}
		}

		private void StreamlabsUnlink_Button_Click(object sender, RoutedEventArgs e)
		{
			_appSettings.StreamlabsInformation = new StreamlabsInformation();
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
				_appSettings.UseOwnServiceCredentials = true;
				TwitchClientId_Textbox.IsEnabled = true;
				TwitchClientSecret_Textbox.IsEnabled = true;
				StreamlabsClientId_Textbox.IsEnabled = true;
				StreamlabsClientSecret_Textbox.IsEnabled = true;
			}
			else
			{
				_appSettings.UseOwnServiceCredentials = false;
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
			_appSettingsController.SaveSettings(_appSettings);

			MessageBox.Show(Properties.Resources.General_MessageBox_SettingsSaved, Properties.Resources.General_MessageBox_Sucess_Title, MessageBoxButton.OK, MessageBoxImage.Information);
		}

		private void ParseValuesIntoAppSettings()
		{
			// Bot Settings
			_appSettings.WhisperMode = (bool)whisperMode_Checkbox.IsChecked;
			_appSettings.CommandPrefix = commandPrefix_TextBox.Text;
			_appSettings.ChatResponse = (bool)response_CheckBox.IsChecked;

			// Nano Settings
			_appSettings.NanoSettings.TriggerEnabled = (bool)nanoCmd_Checkbox.IsChecked;
			_appSettings.NanoSettings.CooldownEnabled = (bool)nanoCooldown_Checkbox.IsChecked;
			_appSettings.NanoSettings.Cooldown = Convert.ToInt32(nanoCooldown_TextBox.Text);
			_appSettings.NanoSettings.ChangeBackOnCommand = (bool)commandRestore_CheckBox.IsChecked;
			_appSettings.NanoSettings.ChangeBackOnKeyword = (bool)keywordRestore_Checkbox.IsChecked;

			// App Settings
			_appSettings.AutoIpRefresh = (bool)autoIPRefresh_Checkbox.IsChecked;
			_appSettings.DebugEnabled = (bool)debugCmd_Checkbox.IsChecked;
			_appSettings.AutoConnect = (bool)autoConnect_Checkbox.IsChecked;
			_appSettings.UseOwnServiceCredentials = (bool)UseOwnServiceCredentials_Checkbox.IsChecked;
			_appSettings.TwitchClientId = TwitchClientId_Textbox.Text;
			_appSettings.TwitchClientSecret = TwitchClientSecret_Textbox.Password;

			// Hype Rate
			_appSettings.HypeRateId = hypeRateId_Textbox.Text;

			// Streamlabs
			_appSettings.StreamlabsClientId = StreamlabsClientId_Textbox.Text;
			_appSettings.StreamlabsClientSecret = StreamlabsClientSecret_Textbox.Password;
		}

		// TODO Check if Disconnect Button is needed
		private void BotDisconnect_Button_Click(object sender, RoutedEventArgs e)
		{
			_logger.Info($"Trying to Disconnect from Twitch...");
			try
			{
				sendMessage_TextBox.IsEnabled = false;
				sendMessage_Button.IsEnabled = false;
				if (_twitchController._client != null)
				{
					_twitchController.Disconnect();
					_twitchPubSubController.Dispose();
				}
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message, ex);
			}
		}

		private void SendMessage_Button_Click(object sender, RoutedEventArgs e)
		{
			string username = _appSettings.BotName;
			string message = sendMessage_TextBox.Text;
			_twitchController.SendMessageToChat(message);
			_triggerLogicController.HandleMessage(new ChatMessage(username, true, true, true, message, new Color()));
			sendMessage_TextBox.Text = "";

			string formattedMessage = $"{DateTime.Now.ToLongTimeString()}: {username} - {message}";

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
			TriggerWindow triggerWindow = new TriggerWindow(_commandRepository, _nanoController, _appSettings, _streamlabsController, _hypeRatecontroller, _triggerLogicController, _twitchPubSubController)
			{
				Owner = this
			};

			if (!CheckForDuplicateWindow(triggerWindow))
			{
				triggerWindow.Show();
			}
		}

		private void NanoCmd_Checkbox_Click(object sender, RoutedEventArgs e)
		{
#if !DEBUG
            if (_appSettings.DebugEnabled == false && _appSettings.NanoSettings.NanoLeafDevices.Count < 1)
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
				_appSettings.NanoSettings.TriggerEnabled = true;
			}
			else if (nanoCmd_Checkbox.IsChecked == false)
			{
				nanoCmd_Button.IsEnabled = false;
				_appSettings.NanoSettings.TriggerEnabled = false;
			}
		}

		private void nanoPairing_Button_Click(object sender, RoutedEventArgs e)
		{
			PairingWindow pairingWindow = new PairingWindow(_appSettings, _nanoController)
			{
				Owner = this
			};

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
			AppInfoWindow appInfoWindow = new AppInfoWindow(_appSettings, _appSettingsController)
			{
				Owner = this
			};

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
				_appSettings.WhisperMode = true;
				_logger.Info("Whisper Mode enabled!");
			}
			else if (whisperMode_Checkbox.IsChecked == false)
			{
				_appSettings.WhisperMode = false;
				_logger.Info("Whisper Mode disabled!");
			}
		}

		private void DebugCmd_Checkbox_Click(object sender, RoutedEventArgs e)
		{
			if (debugCmd_Checkbox.IsChecked == true)
			{
				_appSettings.DebugEnabled = true;
				SetLogLevel(Level.Debug);
				_logger.Info("Debug Mode enabled!");
			}
			else if (debugCmd_Checkbox.IsChecked == false)
			{
				_appSettings.DebugEnabled = false;
				SetLogLevel(Level.Info);
				_logger.Info("Debug Mode disabled!");
			}
		}

		private void UpdateScrollBar(ListBox listBox)
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
				_appSettings.AutoIpRefresh = true;
			}
			else
			{
				_appSettings.AutoIpRefresh = false;
			}
		}

		private void NanoCooldown_TextBox_TextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
		{
			_appSettings.NanoSettings.Cooldown = Convert.ToInt32(nanoCooldown_TextBox.Text);
		}

		private void NanoCooldown_TextBox_LostFocus(object sender, RoutedEventArgs e)
		{
			_appSettings.NanoSettings.Cooldown = Convert.ToInt32(nanoCooldown_TextBox.Text);
		}

		private void NanoCooldown_Checkbox_Click(object sender, RoutedEventArgs e)
		{
			_appSettings.NanoSettings.CooldownEnabled = (bool)nanoCooldown_Checkbox.IsChecked;
			_appSettings.NanoSettings.Cooldown = Convert.ToInt32(nanoCooldown_TextBox.Text);

			if (_appSettings.NanoSettings.CooldownEnabled)
			{
				SendChatResponse(string.Format(Properties.Resources.Code_Main_ChatMessage_CooldownON, _appSettings.NanoSettings.Cooldown));
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
			_appSettings.ChatResponse = (bool)response_CheckBox.IsChecked;
		}

		private void KeywordRestore_Checkbox_Click(object sender, RoutedEventArgs e)
		{
			_appSettings.NanoSettings.ChangeBackOnKeyword = (bool)keywordRestore_Checkbox.IsChecked;
		}

		private void CommandRestore_CheckBox_Click(object sender, RoutedEventArgs e)
		{
			_appSettings.NanoSettings.ChangeBackOnCommand = (bool)commandRestore_CheckBox.IsChecked;
		}

		private void AutoRestoreHelp_Button_Click(object sender, RoutedEventArgs e)
		{
			MessageBox.Show(Properties.Resources.Code_Main_MessageBox_AutoRestore, Properties.Resources.General_MessageBox_Hint_Title);
		}

		private void EventReset_Button_Click(object sender, RoutedEventArgs e)
		{
			_triggerLogicController.ResetEventQueue();
		}

		private void EventRestart_Button_Click(object sender, RoutedEventArgs e)
		{
			_triggerLogicController.RestartEventQueue();
		}

		private void Responses_Button_Click(object sender, RoutedEventArgs e)
		{
			Responses responsesWindow = new Responses(_appSettings, _appSettingsController)
			{
				Owner = this
			};

			if (!CheckForDuplicateWindow(responsesWindow))
			{
				responsesWindow.Show();
			}
		}

		private void AutoConnect_Checkbox_Click(object sender, RoutedEventArgs e)
		{
			if (autoConnect_Checkbox.IsChecked == true)
			{
				_appSettings.AutoConnect = true;
				CheckNanoDevices();
			}
			else
			{
				_appSettings.AutoConnect = false;
			}
		}

		private void ShowDevices_Button_Click(object sender, RoutedEventArgs e)
		{
			_logger.Info("Show Device Info");
			DevicesInfoWindow devicesInfoWindow = new DevicesInfoWindow(_nanoController, _appSettings, _appSettingsController)
			{
				Owner = this
			};

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

				foreach (NanoLeafDevice device in _appSettings.NanoSettings.NanoLeafDevices)
				{
					await _nanoController.SetBrightness(device, brightness);
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
			_appSettings.NanoSettings.CooldownIgnore = nanoCooldownIgnore_Checkbox.IsEnabled;
		}

		#endregion

		#region Methods

		private void ConnectToChat()
		{
			try
			{
				_twitchController.Connect(_appSettings);
				ConnectChat_Button.IsEnabled = false;
				sendMessage_TextBox.IsEnabled = true;
				sendMessage_Button.IsEnabled = true;
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				_logger.Error(ex);
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
			if (_appSettings.NanoSettings.NanoLeafDevices.Count != 0)
			{
				nanoTestConnection_Button.IsEnabled = true;
			}
			else
			{
				nanoTestConnection_Button.IsEnabled = false;
			}

			if (_appSettings.AutoConnect)
			{
				autoConnect_Checkbox.IsChecked = true;
			}
			else
			{
				autoConnect_Checkbox.IsChecked = false;
			}

			if (_appSettings.NanoSettings.CooldownEnabled)
			{
				nanoCooldown_TextBox.IsEnabled = false;
			}

			if (_appSettings.UseOwnServiceCredentials)
			{
				TwitchClientId_Textbox.IsEnabled = true;
				TwitchClientSecret_Textbox.IsEnabled = true;
				StreamlabsClientId_Textbox.IsEnabled = true;
				StreamlabsClientSecret_Textbox.IsEnabled = true;
			}

			help_TextBlock.Text = string.Format(Properties.Resources.Code_Main_Label_NanoHelpText, _appSettings.CommandPrefix);
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
			if (!_appSettings.ChatResponse)
			{
				return;
			}

			_twitchController.SendMessageToChat(message);
		}

		private void SetLogLevel(Level logLevel)
		{
			((Hierarchy)LogManager.GetRepository()).Root.Level = logLevel;
			((Hierarchy)LogManager.GetRepository()).RaiseConfigurationChanged(EventArgs.Empty);
		}

		private async Task<bool> TestNanoConnection()
		{
			nanoInfo_TextBox.IsEnabled = true;
			nanoInfo_TextBox.Text = "";
			int deviceCount = _appSettings.NanoSettings.NanoLeafDevices.Count;
			if (deviceCount > 1)
			{
				nanoInfo_TextBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
				nanoInfo_TextBox.IsReadOnly = true;
			}
			foreach (var device in _appSettings.NanoSettings.NanoLeafDevices)
			{
				if (string.IsNullOrEmpty(device.Address) || string.IsNullOrWhiteSpace(device.Address))
				{
					_logger.Error(Properties.Resources.Code_Main_MessageBox_ConTestFail + " " + Properties.Resources.Code_Main_MessageBox_AdressNull + " " + Properties.Resources.Code_Main_MessageBox_UpdateOrRepair);
					MessageBox.Show(Properties.Resources.Code_Main_MessageBox_ConTestFail + " " + Properties.Resources.Code_Main_MessageBox_AdressNull + " " + Properties.Resources.Code_Main_MessageBox_UpdateOrRepair, Properties.Resources.General_MessageBox_Error_Title);
					return false;
				}

				try
				{
					var controllerInfo = await _nanoController.GetControllerInfo(device);

					if (controllerInfo == null)
					{
						_logger.Error(string.Format(Properties.Resources.Code_Main_MessageBox_ConTestFail + " " + Properties.Resources.Code_Main_MessageBox_ControllerInfoNull, device.DeviceName));
						MessageBox.Show(Properties.Resources.Code_Main_MessageBox_ConTestFail + " " + Properties.Resources.Code_Main_MessageBox_ControllerInfoNull, Properties.Resources.General_MessageBox_Error_Title);
						return false;
					}

					if (_appSettings.NanoSettings.TriggerEnabled)
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

			nanoInfo_TextBox.Text += $"Devices Total: {_appSettings.NanoSettings.NanoLeafDevices.Count}" + Environment.NewLine;

			_appSettingsController.SaveSettings(_appSettings);

			return true;
		}

		private bool CheckNanoDevices()
		{
			if (_appSettings.NanoSettings.NanoLeafDevices.Count == 0)
			{
				MessageBox.Show(Properties.Resources.Code_Main_MessageBox_NoDevice, Properties.Resources.General_MessageBox_Error_Title);
				_appSettings.AutoConnect = false;
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
			_logger.Info("[Auto Connection] Try NanoLeaf Pairing ...");

			if (!await TestNanoConnection())
			{
				_logger.Error("[Auto Connection] Couldn't connect to NanoLeaf Device!");
				return;
			}

			_logger.Info("[Auto Connection] NanoLeaf Paring ... OK");
			_logger.Info("[Auto Connection] Try to connect to HypeRate ...");

			if (!string.IsNullOrWhiteSpace(_appSettings.HypeRateId) && !await TestHypeRateConnection())
			{
				_logger.Error("[Auto Connection] Could not connect to HypeRateIO ... ");
				return;
			}
			HypeRateConnect_Button.IsEnabled = false;
			HypeRateDisconnect_Button.IsEnabled = true;

			_logger.Info("[Auto Connection] Connected to HypeRateIO ... OK");
			_logger.Info("[Auto Connection] Try to connect to Streamlabs ...");

			if (!string.IsNullOrWhiteSpace(_appSettings.StreamlabsInformation.StreamlabsSocketToken) && !await TestStreamlabsConnection())
			{
				_logger.Error("[Auto Connection] Could not connect to Streamlabs ... ");
				return;
			}

			StreamlabsConnectButton.IsEnabled = false;
			StreamlabsDisconnectButton.IsEnabled = true;

			_logger.Info("[Auto Connection] Try to connect to Twitch ...");

			ConnectToChat();
		}

		private async Task<bool> TestStreamlabsConnection()
		{
			_streamlabsController.ConnectSocket();

			await Task.Delay(1000 * 1);

			return _streamlabsController._IsSocketConnected;
		}

		private async Task<bool> TestHypeRateConnection()
		{
			_hypeRatecontroller.StartListener();

			await Task.Delay(1000 * 1);

			return _hypeRatecontroller._isConnected;
		}

		#endregion
	}
}