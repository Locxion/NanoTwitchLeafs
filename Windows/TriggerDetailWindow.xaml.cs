﻿using log4net;
using Microsoft.Win32;
using NanoTwitchLeafs.Controller;
using NanoTwitchLeafs.Enums;
using NanoTwitchLeafs.Objects;
using NanoTwitchLeafs.Repositories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NanoTwitchLeafs.Windows
{
	/// <summary>
	/// Interaction logic for TriggerDetailWindow.xaml
	/// </summary>
	public partial class TriggerDetailWindow : Window
	{
		private readonly CommandRepository _commandRepository;
		private readonly StreamlabsController _streamLabsController;
		private readonly HypeRateIOController _hypeRateIoController;
		private readonly AppSettings _appSettings;
		private readonly ILog _logger = LogManager.GetLogger(typeof(TriggerWindow));
		private readonly TwitchEventSubController _twitchEventSubController;

		private string _channelPointsGuid;
		private TriggerSetting TriggerSetting { get; set; }

		public TriggerDetailWindow(AppSettings appSettings, CommandRepository commandRepository, List<string> effectList, StreamlabsController streamLabsController, HypeRateIOController hypeRateIoController, TriggerSetting triggerSetting = null, TwitchEventSubController eventSubController = null)
		{
			_commandRepository = commandRepository ?? throw new ArgumentNullException(nameof(commandRepository));
			_streamLabsController = streamLabsController;
			_hypeRateIoController = hypeRateIoController ?? throw new ArgumentNullException(nameof(hypeRateIoController));
			_appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
			_twitchEventSubController = eventSubController;

			Constants.SetCultureInfo(_appSettings.Language);
			InitializeComponent();

			if (_twitchEventSubController != null && _twitchEventSubController.IsConnected)
			{
				_twitchEventSubController.OnChannelPointsRedeemed += TwitchEventSubController_OnChannelPointsRedeemed;
				_channelPointsGuid = "{00000000-0000-0000-0000-000000000000}";
			}

			foreach (var effect in effectList)
			{
				Effect_ComboBox.Items.Add(effect);
			}

			if (triggerSetting != null)
			{
				TriggerSetting = triggerSetting;
				InitData();
			}
		}

		private void TwitchEventSubController_OnChannelPointsRedeemed(string username, string promt, string guid)
		{
			if (_appSettings.ChannelName.ToLower() != username.ToLower())
			{
				return;
			}

			Dispatcher.BeginInvoke(new Action(() => channelPointsDetection_Label.Foreground = Brushes.Green));
			Dispatcher.BeginInvoke(new Action(() => channelPointsDetection_Label.Content = string.Format(Properties.Resources.Code_TriggerDetail_Label_CPGuid, guid)));
			_channelPointsGuid = guid;
		}

		public TriggerDetailWindow()
		{
			InitializeComponent();
		}

		private void InitData()
		{
			// Set On/Off Slider State
			if (TriggerSetting.IsActive == true)
			{
				OnOff_Slider.Value = 0;
				OnOff_Slider.Background = Brushes.LimeGreen;
			}
			else
			{
				OnOff_Slider.Value = 1;
				OnOff_Slider.Background = Brushes.White;
			}

			// Set Selected Item in Effect Dropdown
			foreach (string effect in Effect_ComboBox.Items)
			{
				if (effect == TriggerSetting.Effect)
				{
					Effect_ComboBox.SelectedItem = effect;
				}
			}

			// Set Radio Button for Effect or Color
			if (TriggerSetting.IsColor)
			{
				Effect_RadioButton.IsChecked = false;
				Effect_ComboBox.IsEnabled = false;
				Color_RadioButton.IsChecked = true;
				Color_RadioButton.IsEnabled = true;
				ColorPicker.IsEnabled = true;
			}

			// Set Color Picker to saved Color
			Color color = new Color { R = TriggerSetting.R, G = TriggerSetting.G, B = TriggerSetting.B, A = 255 };
			ColorPicker.SelectedColor = color;

			// Fill Command/Keyword Textbox
			CommandKeyword_Textbox.Text = TriggerSetting.CMD;

			// Fill SoundEffectPath Textbox
			SoundFilePath_Textbox.Text = TriggerSetting.SoundFilePath;
			if (!string.IsNullOrWhiteSpace(TriggerSetting.SoundFilePath))
			{
				SoundFilePath_Textbox.IsEnabled = true;
			}

			// Check for HypeRate Service Connected
			if (!_hypeRateIoController._isConnected)
			{
				HypeRate_RadioButton.IsEnabled = false;
			}

			// Check for Streamlabs Websocket Connection
			if (!_streamLabsController._IsSocketConnected)
			{
				Donation_RadioButton.IsEnabled = false;
			}

			// Fill Options Texboxes and Checkboxes
			Duration_Textbox.Text = TriggerSetting.Duration.ToString();
			Brightness_Textbox.Text = TriggerSetting.Brightness.ToString();
			Amount_Textbox.Text = TriggerSetting.Amount.ToString();
			Cooldown_Textbox.Text = TriggerSetting.Cooldown.ToString();
			Volume_Textbox.Text = TriggerSetting.Volume.ToString();
			_channelPointsGuid = TriggerSetting.ChannelPointsGuid;

			Viponly_Checkbox.IsChecked = TriggerSetting.VipOnly;
			Subonly_Checkbox.IsChecked = TriggerSetting.SubscriberOnly;
			Modonly_Checkbox.IsChecked = TriggerSetting.ModeratorOnly;

			if (TriggerSetting.ChannelPointsGuid != null && TriggerSetting.ChannelPointsGuid != "{00000000-0000-0000-0000-000000000000}")
			{
				Dispatcher.BeginInvoke(new Action(() => channelPointsDetection_Label.Foreground = Brushes.Green));
				Dispatcher.BeginInvoke(new Action(() => channelPointsDetection_Label.Content = string.Format(Properties.Resources.Code_TriggerDetail_Label_CPGuidSet, TriggerSetting.ChannelPointsGuid)));
			}

			SetControlsEnabled();

			Checkbox_Click(null, null);
		}

		private void SetControlsEnabled()
		{
			// Set Controls Enabled State && Radio Buttons
			switch (TriggerSetting.Trigger)
			{
				case "Command":
					Cmd_RadioButton.IsChecked = true;
					CommandKeyword_Textbox.IsEnabled = true;
					Amount_Textbox.IsEnabled = false;
					Viponly_Checkbox.IsEnabled = true;
					Subonly_Checkbox.IsEnabled = true;
					Modonly_Checkbox.IsEnabled = true;
					Cooldown_Textbox.IsEnabled = true;
					Channelpoints_Grid.Visibility = Visibility.Hidden;
					if (_appSettings.NanoSettings.ChangeBackOnCommand)
					{
						Duration_Textbox.IsEnabled = true;
					}
					else
					{
						Duration_Textbox.IsEnabled = false;
					}
					break;

				case "Subscription":
					NewSub_RadioButton.IsChecked = true;
					CommandKeyword_Textbox.IsEnabled = false;
					Amount_Textbox.IsEnabled = false;
					Duration_Textbox.IsEnabled = true;
					Viponly_Checkbox.IsEnabled = false;
					Subonly_Checkbox.IsEnabled = false;
					Modonly_Checkbox.IsEnabled = false;
					Cooldown_Textbox.IsEnabled = false;
					Channelpoints_Grid.Visibility = Visibility.Hidden;
					break;

				case "ReSubscription":
					Resub_RadioButton.IsChecked = true;
					CommandKeyword_Textbox.IsEnabled = false;
					Amount_Textbox.IsEnabled = false;
					Duration_Textbox.IsEnabled = true;
					Viponly_Checkbox.IsEnabled = false;
					Subonly_Checkbox.IsEnabled = false;
					Modonly_Checkbox.IsEnabled = false;
					Cooldown_Textbox.IsEnabled = false;
					Channelpoints_Grid.Visibility = Visibility.Hidden;
					break;

				case "GiftSubscription":
					Giftsub_RadioButton.IsChecked = true;
					CommandKeyword_Textbox.IsEnabled = false;
					Amount_Textbox.IsEnabled = false;
					Duration_Textbox.IsEnabled = true;
					Viponly_Checkbox.IsEnabled = false;
					Subonly_Checkbox.IsEnabled = false;
					Modonly_Checkbox.IsEnabled = false;
					Cooldown_Textbox.IsEnabled = false;
					Channelpoints_Grid.Visibility = Visibility.Hidden;
					break;

				case "GiftBomb":
					GiftBomb_RadioButton.IsChecked = true;
					CommandKeyword_Textbox.IsEnabled = false;
					Amount_Textbox.IsEnabled = true;
					Duration_Textbox.IsEnabled = true;
					Viponly_Checkbox.IsEnabled = false;
					Subonly_Checkbox.IsEnabled = false;
					Modonly_Checkbox.IsEnabled = false;
					Cooldown_Textbox.IsEnabled = false;
					Channelpoints_Grid.Visibility = Visibility.Hidden;
					break;

				case "AnonGiftSubscription":
					AnonGiftSub_RadioButton.IsChecked = true;
					CommandKeyword_Textbox.IsEnabled = false;
					Amount_Textbox.IsEnabled = false;
					Duration_Textbox.IsEnabled = true;
					Viponly_Checkbox.IsEnabled = false;
					Subonly_Checkbox.IsEnabled = false;
					Modonly_Checkbox.IsEnabled = false;
					Cooldown_Textbox.IsEnabled = false;
					Channelpoints_Grid.Visibility = Visibility.Hidden;
					break;

				case "AnonGiftBomb":
					AnonGiftBomb_RadioButton.IsChecked = true;
					CommandKeyword_Textbox.IsEnabled = false;
					Amount_Textbox.IsEnabled = true;
					Duration_Textbox.IsEnabled = true;
					Viponly_Checkbox.IsEnabled = false;
					Subonly_Checkbox.IsEnabled = false;
					Modonly_Checkbox.IsEnabled = false;
					Cooldown_Textbox.IsEnabled = false;
					Channelpoints_Grid.Visibility = Visibility.Hidden;
					break;

				case "HypeTrain":
					HypeTrain_RadioButton.IsChecked = true;
					CommandKeyword_Textbox.IsEnabled = false;
					Amount_Textbox.IsEnabled = true;
					Duration_Textbox.IsEnabled = true;
					Viponly_Checkbox.IsEnabled = false;
					Subonly_Checkbox.IsEnabled = false;
					Modonly_Checkbox.IsEnabled = false;
					Cooldown_Textbox.IsEnabled = false;
					Channelpoints_Grid.Visibility = Visibility.Hidden;
					break;

				case "Raid":
					Raid_RadioButton.IsChecked = true;
					CommandKeyword_Textbox.IsEnabled = false;
					Amount_Textbox.IsEnabled = false;
					Duration_Textbox.IsEnabled = true;
					Viponly_Checkbox.IsEnabled = false;
					Subonly_Checkbox.IsEnabled = false;
					Modonly_Checkbox.IsEnabled = false;
					Cooldown_Textbox.IsEnabled = false;
					Channelpoints_Grid.Visibility = Visibility.Hidden;
					break;

				case "Follower":
					Follower_RadioButton.IsChecked = true;
					CommandKeyword_Textbox.IsEnabled = false;
					Amount_Textbox.IsEnabled = false;
					Duration_Textbox.IsEnabled = true;
					Viponly_Checkbox.IsEnabled = false;
					Subonly_Checkbox.IsEnabled = false;
					Modonly_Checkbox.IsEnabled = false;
					Cooldown_Textbox.IsEnabled = false;
					Channelpoints_Grid.Visibility = Visibility.Hidden;
					break;

				case "Bits":
					Bits_RadioButton.IsChecked = true;
					CommandKeyword_Textbox.IsEnabled = false;
					Amount_Textbox.IsEnabled = true;
					Duration_Textbox.IsEnabled = true;
					Viponly_Checkbox.IsEnabled = false;
					Subonly_Checkbox.IsEnabled = false;
					Modonly_Checkbox.IsEnabled = false;
					Cooldown_Textbox.IsEnabled = false;
					Channelpoints_Grid.Visibility = Visibility.Hidden;
					break;

				case "Keyword":
					Keyword_RadioButton.IsChecked = true;
					CommandKeyword_Textbox.IsEnabled = true;
					Amount_Textbox.IsEnabled = false;
					Viponly_Checkbox.IsEnabled = true;
					Subonly_Checkbox.IsEnabled = true;
					Modonly_Checkbox.IsEnabled = true;
					Cooldown_Textbox.IsEnabled = true;
					Channelpoints_Grid.Visibility = Visibility.Hidden;
					if (_appSettings.NanoSettings.ChangeBackOnKeyword)
					{
						Duration_Textbox.IsEnabled = true;
					}
					else
					{
						Duration_Textbox.IsEnabled = false;
					}
					break;

				case "ChannelPoints":
					Channelpoints_RadioButton.IsChecked = true;
					CommandKeyword_Textbox.IsEnabled = false;
					Amount_Textbox.IsEnabled = false;
					Duration_Textbox.IsEnabled = true;
					Viponly_Checkbox.IsEnabled = false;
					Subonly_Checkbox.IsEnabled = false;
					Modonly_Checkbox.IsEnabled = false;
					Cooldown_Textbox.IsEnabled = false;
					Channelpoints_Grid.Visibility = Visibility.Visible;
					break;

				case "HypeRate":
					HypeRate_RadioButton.IsChecked = true;
					CommandKeyword_Textbox.IsEnabled = false;
					Amount_Textbox.IsEnabled = true;
					Duration_Textbox.IsEnabled = false;
					Viponly_Checkbox.IsEnabled = false;
					Subonly_Checkbox.IsEnabled = false;
					Modonly_Checkbox.IsEnabled = false;
					Cooldown_Textbox.IsEnabled = false;
					break;

				case "Donation":
					Donation_RadioButton.IsChecked = true;
					CommandKeyword_Textbox.IsEnabled = false;
					Amount_Textbox.IsEnabled = true;
					Duration_Textbox.IsEnabled = false;
					Viponly_Checkbox.IsEnabled = false;
					Subonly_Checkbox.IsEnabled = false;
					Modonly_Checkbox.IsEnabled = false;
					Cooldown_Textbox.IsEnabled = false;
					break;

				case "UsernameColor":
					UserColor_RadioButton.IsChecked = true;
					CommandKeyword_Textbox.IsEnabled = false;
					Amount_Textbox.IsEnabled = false;
					Duration_Textbox.IsEnabled = false;
					Viponly_Checkbox.IsEnabled = false;
					Subonly_Checkbox.IsEnabled = false;
					Modonly_Checkbox.IsEnabled = false;
					Cooldown_Textbox.IsEnabled = false;
					Effect_RadioButton.IsEnabled = false;
					Color_RadioButton.IsEnabled = false;
					ColorPicker.IsEnabled = false;
					Effect_ComboBox.IsEnabled = false;
					break;
			}

			if (string.IsNullOrWhiteSpace(SoundFilePath_Textbox.Text))
			{
				SoundFilePath_Textbox.IsEnabled = true;
				Volume_Textbox.IsEnabled = true;
			}
		}

		private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (e.NewValue == 0)
			{
				OnOff_Slider.Background = Brushes.LimeGreen;
			}
			else
			{
				OnOff_Slider.Background = Brushes.White;
			}
		}

		private void Save_Button_Click(object sender, RoutedEventArgs e)
		{
			// Do Value checks before attempt to Save
			if (!ValueChecks())
			{
				return;
			}

			// Get all Triggers
			List<TriggerSetting> triggerSettings = _commandRepository.GetList().ToList();

			// If Trigger already exists
			if (TriggerSetting != null)
			{
				// Search for existing Trigger and Remove it from List
				foreach (TriggerSetting setting in triggerSettings)
				{
					if (setting.ID == TriggerSetting.ID)
					{
						triggerSettings.Remove(setting);
						break;
					}
				}
			}

			string triggerType = "";
			Color color = new Color { R = 0, G = 0, B = 0, A = 255 };

			// Get Activated Radio Button
			if (Cmd_RadioButton.IsChecked == true)
			{
				triggerType = TriggerTypeEnum.Command.ToString();
			}
			if (Keyword_RadioButton.IsChecked == true)
			{
				triggerType = TriggerTypeEnum.Keyword.ToString();
			}
			if (Follower_RadioButton.IsChecked == true)
			{
				triggerType = TriggerTypeEnum.Follower.ToString();
			}
			if (Bits_RadioButton.IsChecked == true)
			{
				triggerType = TriggerTypeEnum.Bits.ToString();
			}
			if (HypeTrain_RadioButton.IsChecked == true)
			{
				triggerType = TriggerTypeEnum.HypeTrain.ToString();
			}
			if (Raid_RadioButton.IsChecked == true)
			{
				triggerType = TriggerTypeEnum.Raid.ToString();
			}
			if (NewSub_RadioButton.IsChecked == true)
			{
				triggerType = TriggerTypeEnum.Subscription.ToString();
			}
			if (Resub_RadioButton.IsChecked == true)
			{
				triggerType = TriggerTypeEnum.ReSubscription.ToString();
			}
			if (AnonGiftSub_RadioButton.IsChecked == true)
			{
				triggerType = TriggerTypeEnum.AnonGiftSubscription.ToString();
			}
			if (Giftsub_RadioButton.IsChecked == true)
			{
				triggerType = TriggerTypeEnum.GiftSubscription.ToString();
			}
			if (AnonGiftBomb_RadioButton.IsChecked == true)
			{
				triggerType = TriggerTypeEnum.AnonGiftBomb.ToString();
			}
			if (GiftBomb_RadioButton.IsChecked == true)
			{
				triggerType = TriggerTypeEnum.GiftBomb.ToString();
			}
			if (Channelpoints_RadioButton.IsChecked == true)
			{
				triggerType = TriggerTypeEnum.ChannelPoints.ToString();
			}
			if (HypeRate_RadioButton.IsChecked == true)
			{
				triggerType = TriggerTypeEnum.HypeRate.ToString();
			}
			if (Donation_RadioButton.IsChecked == true)
			{
				triggerType = TriggerTypeEnum.Donation.ToString();
			}

			if (UserColor_RadioButton.IsChecked == true)
			{
				triggerType = TriggerTypeEnum.UsernameColor.ToString();
			}

			// Get Status of Trigger
			bool isActive;
			if (OnOff_Slider.Value == 0)
			{
				isActive = true;
			}
			else
			{
				isActive = false;
			}

			// Check if Effect is Color or not
			bool IsColor = false;
			string effect = "";
			if (Effect_RadioButton.IsChecked == false)
			{
				IsColor = true;
			}
			else
			{
				if (UserColor_RadioButton.IsChecked == true)
				{
					effect = "UserColor";
				}
				else
				{
					effect = Effect_ComboBox.SelectedItem.ToString();
				}
			}
			// Check for Invalid CP GUID
			if (triggerType == "ChannelPoints" && _channelPointsGuid == "{00000000-0000-0000-0000-000000000000}")
			{
				MessageBox.Show(Properties.Resources.Code_TriggerDetail_MessageBox_NoRewardDetected, Properties.Resources.General_MessageBox_Error_Title, MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			// Get RGB Values from Color Picker
			if (ColorPicker.SelectedColor == null && IsColor)
			{
				MessageBox.Show(Properties.Resources.Window_TriggerDetail_ColorPicker_Error, Properties.Resources.General_MessageBox_Error_Title, MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}
			else if (ColorPicker.SelectedColor != null && IsColor)
			{
				color = (Color)ColorPicker.SelectedColor;
			}

			// Replace Comma with Point
			Amount_Textbox.Text = Amount_Textbox.Text.Replace(",", ".");

			// Create new TriggerSetting
			TriggerSetting newTriggerSetting = new TriggerSetting
			{
				CMD = CommandKeyword_Textbox.Text,
				IsActive = isActive,
				Brightness = Convert.ToInt32(Brightness_Textbox.Text),
				Cooldown = Convert.ToDouble(Cooldown_Textbox.Text),
				Duration = Convert.ToDouble(Duration_Textbox.Text),
				IsColor = IsColor,
				Effect = effect,
				R = color.R,
				G = color.G,
				B = color.B,
				Amount = Convert.ToDouble(Amount_Textbox.Text),
				Volume = Convert.ToInt32(Volume_Textbox.Text),
				Trigger = triggerType,
				SoundFilePath = SoundFilePath_Textbox.Text,
				VipOnly = (bool)Viponly_Checkbox.IsChecked,
				ModeratorOnly = (bool)Modonly_Checkbox.IsChecked,
				SubscriberOnly = (bool)Subonly_Checkbox.IsChecked
			};

			if (newTriggerSetting.ChannelPointsGuid == "{00000000-0000-0000-0000-000000000000}")
			{
				newTriggerSetting.ChannelPointsGuid = _channelPointsGuid;
			}

			// Add New Trigger Setting to the existing Triggers in List
			triggerSettings.Add(newTriggerSetting);

			// Clear Database
			_commandRepository.ClearAll();

			// Read correct Index and insert to Database
			int index = 0;
			foreach (TriggerSetting trigger in triggerSettings)
			{
				trigger.ID = index;
				_logger.Debug($"Insert Trigger Setting with ID {index} into Repository.");
				_commandRepository.Insert(trigger);
				index++;
			}

			_logger.Info($"Saved Trigger to Database. There are currently {triggerSettings.Count} Trigger.");
			MessageBox.Show(Properties.Resources.General_MessageBox_SettingsSaved, Properties.Resources.General_MessageBox_Sucess_Title, MessageBoxButton.OK, MessageBoxImage.Information);
			Close();
		}

		private bool ValueChecks()
		{
			if (UserColor_RadioButton.IsChecked == false && Effect_RadioButton.IsChecked == true && (Effect_ComboBox.SelectedValue == null || string.IsNullOrWhiteSpace(Effect_ComboBox.SelectedValue.ToString())))
			{
				_logger.Error("Please choose an Effect for your Trigger! Can not be Empty!");
				MessageBox.Show(Properties.Resources.Code_TriggerDetail_MessageBox_NoEffectSelected, Properties.Resources.General_MessageBox_Error_Title);
				return false;
			}

			if (string.IsNullOrWhiteSpace(CommandKeyword_Textbox.Text) && (Cmd_RadioButton.IsChecked == true || Keyword_RadioButton.IsChecked == true))
			{
				_logger.Warn("Please enter a Command/Keyword for your Trigger! Can not be Empty!");
				MessageBox.Show(Properties.Resources.Code_TriggerDetail_MessageBox_CmdBoxEmpty, Properties.Resources.General_MessageBox_Error_Title);
				return false;
			}

			if (Convert.ToInt32(Brightness_Textbox.Text) > 100 || Convert.ToInt32(Brightness_Textbox.Text) < 0 || string.IsNullOrWhiteSpace(Brightness_Textbox.Text))
			{
				_logger.Warn("Please enter a Brightness Value between 0 and 100! Can not be Empty!");
				MessageBox.Show(Properties.Resources.Code_TriggerDetail_MessageBox_BrightnessValue, Properties.Resources.General_MessageBox_Error_Title);
				Brightness_Textbox.Text = "50";
				return false;
			}

			if (Convert.ToInt32(Volume_Textbox.Text) > 100 || Convert.ToInt32(Volume_Textbox.Text) < 0 || string.IsNullOrWhiteSpace(Volume_Textbox.Text))
			{
				_logger.Warn("Please enter a Volume Value between 0 and 100! Can not be Empty!");
				MessageBox.Show(Properties.Resources.Code_TriggerDetail_MessageBox_VolumeValue, Properties.Resources.General_MessageBox_Error_Title);
				Volume_Textbox.Text = "50";
				return false;
			}

			if (string.IsNullOrWhiteSpace(Amount_Textbox.Text) || Convert.ToDouble(Amount_Textbox.Text) < 0)
			{
				_logger.Warn("Please enter a Amount Value even if you dont use it! Can not be Empty or Negative!");
				MessageBox.Show(Properties.Resources.Code_TriggerDetail_MessageBox_AmountValue, Properties.Resources.General_MessageBox_Error_Title);
				Amount_Textbox.Text = "0";
				return false;
			}
			if (string.IsNullOrWhiteSpace(Duration_Textbox.Text) || Convert.ToDouble(Duration_Textbox.Text) < 0)
			{
				_logger.Warn("Please enter a Duration Value even if you dont use it! Can not be Empty or Negative!");
				MessageBox.Show(Properties.Resources.Code_TriggerDetail_MessageBox_DurationValue, Properties.Resources.General_MessageBox_Error_Title);
				Duration_Textbox.Text = "0";
				return false;
			}
			if (string.IsNullOrWhiteSpace(Cooldown_Textbox.Text) || Convert.ToDouble(Cooldown_Textbox.Text) < 0)
			{
				_logger.Warn("Please enter a Cooldown Value even if you dont use it! Enter 0 to disable the Trigger Cooldown! Can not be Empty!");
				MessageBox.Show(Properties.Resources.Code_TriggerDetail_MessageBox_CooldownValue, Properties.Resources.General_MessageBox_Error_Title);
				Cooldown_Textbox.Text = "0";
				return false;
			}

			return true;
		}

		private void TriggerHelp_Button_Click(object sender, RoutedEventArgs e)
		{
			TriggerHelpWindow triggerHelpWindow = new TriggerHelpWindow(_appSettings.Language)
			{
				Owner = this
			};
			triggerHelpWindow.Show();
		}

		#region Ui Stuff

		private void SoundFilePath_Textbox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(SoundFilePath_Textbox.Text))
			{
				SoundFilePath_Textbox.IsEnabled = false;
				Volume_Textbox.IsEnabled = false;
			}
			else
			{
				SoundFilePath_Textbox.IsEnabled = true;
				Volume_Textbox.IsEnabled = true;
			}
		}

		private void SelectSoundFilePath_Button_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			if (openFileDialog.ShowDialog() == true)
			{
				SoundFilePath_Textbox.Text = openFileDialog.FileName;
			}

			if (Path.GetExtension(SoundFilePath_Textbox.Text) != ".mp3" && Path.GetExtension(SoundFilePath_Textbox.Text) != ".wav" && !string.IsNullOrWhiteSpace(SoundFilePath_Textbox.Text))
			{
				MessageBox.Show(Properties.Resources.Code_TriggerDetail_MessageBox_SoundfileFormat, Properties.Resources.General_MessageBox_Error_Title);
				SoundFilePath_Textbox.Text = "";
				SelectSoundFilePath_Button_Click(sender, e);
			}
		}

		private void brightness_TextBoxKeyUp(object sender, KeyEventArgs e)
		{
			try
			{
				// Check for Value Prefix
				if (Convert.ToInt32(Brightness_Textbox.Text) > 100 || Convert.ToInt32(Brightness_Textbox.Text) < 0 || string.IsNullOrWhiteSpace(Brightness_Textbox.Text) || Brightness_Textbox.Text == "Cmd/Keyword")
				{
					Brightness_Textbox.BorderBrush = Brushes.Red;
				}
				else
				{
					Brightness_Textbox.BorderBrush = Brushes.SlateGray;
				}
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message, ex);
			}
		}

		private void Volume_Textbox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (int.TryParse(Volume_Textbox.Text, out int result))
			{
				if (result < 0 || result > 100)
				{
					MessageBox.Show("You can only enter a Number between 0 and 100!", "Error!");
					_logger.Error("You can only enter a Number between 0 and 100!");
					Volume_Textbox.Text = "50";
				}
			}
			else
			{
				MessageBox.Show("You can only enter a Number between 0 and 100!", "Error!");
				_logger.Error("You can only enter a Number between 0 and 100!");
				Volume_Textbox.Text = "50";
			}
		}

		#endregion

		private void Checkbox_Click(object sender, RoutedEventArgs e)
		{
			var titles = new List<string>();
			if (Viponly_Checkbox.IsChecked == true)
				titles.Add("VIP");
			if (Subonly_Checkbox.IsChecked == true)
				titles.Add("Sub");
			if (Modonly_Checkbox.IsChecked == true)
				titles.Add("Mod");

			if (titles.Count == 0)
				titles.Add(Properties.Resources.Code_TriggerDetail_Label_NothingSpecial);

			string joinedString = string.Join(" & ", titles);
			Vipsubmod_Textbox.Text = string.Format(Properties.Resources.Code_TriggerDetail_Label_VipSubMod, joinedString);
		}

		private void TriggerRadioButton_Click(object sender, RoutedEventArgs e)
		{
			if (Cmd_RadioButton.IsChecked == true)
			{
				CommandKeyword_Textbox.IsEnabled = true;
				Amount_Textbox.IsEnabled = false;
				Viponly_Checkbox.IsEnabled = true;
				Subonly_Checkbox.IsEnabled = true;
				Modonly_Checkbox.IsEnabled = true;
				Cooldown_Textbox.IsEnabled = true;
				Channelpoints_Grid.Visibility = Visibility.Hidden;
				if (_appSettings.NanoSettings.ChangeBackOnCommand)
				{
					Duration_Textbox.IsEnabled = true;
				}
				else
				{
					Duration_Textbox.IsEnabled = false;
				}
			}

			if (NewSub_RadioButton.IsChecked == true)
			{
				CommandKeyword_Textbox.IsEnabled = false;
				Amount_Textbox.IsEnabled = false;
				Duration_Textbox.IsEnabled = true;
				Viponly_Checkbox.IsEnabled = false;
				Subonly_Checkbox.IsEnabled = false;
				Modonly_Checkbox.IsEnabled = false;
				Cooldown_Textbox.IsEnabled = false;
				Channelpoints_Grid.Visibility = Visibility.Hidden;
			}

			if (Resub_RadioButton.IsChecked == true)
			{
				CommandKeyword_Textbox.IsEnabled = false;
				Amount_Textbox.IsEnabled = false;
				Duration_Textbox.IsEnabled = true;
				Viponly_Checkbox.IsEnabled = false;
				Subonly_Checkbox.IsEnabled = false;
				Modonly_Checkbox.IsEnabled = false;
				Cooldown_Textbox.IsEnabled = false;
				Channelpoints_Grid.Visibility = Visibility.Hidden;
			}

			if (Giftsub_RadioButton.IsChecked == true)
			{
				CommandKeyword_Textbox.IsEnabled = false;
				Amount_Textbox.IsEnabled = false;
				Duration_Textbox.IsEnabled = true;
				Viponly_Checkbox.IsEnabled = false;
				Subonly_Checkbox.IsEnabled = false;
				Modonly_Checkbox.IsEnabled = false;
				Cooldown_Textbox.IsEnabled = false;
				Channelpoints_Grid.Visibility = Visibility.Hidden;
			}

			if (AnonGiftSub_RadioButton.IsChecked == true)
			{
				CommandKeyword_Textbox.IsEnabled = false;
				Amount_Textbox.IsEnabled = false;
				Duration_Textbox.IsEnabled = true;
				Viponly_Checkbox.IsEnabled = false;
				Subonly_Checkbox.IsEnabled = false;
				Modonly_Checkbox.IsEnabled = false;
				Cooldown_Textbox.IsEnabled = false;
				Channelpoints_Grid.Visibility = Visibility.Hidden;
			}

			if (GiftBomb_RadioButton.IsChecked == true)
			{
				CommandKeyword_Textbox.IsEnabled = false;
				Amount_Textbox.IsEnabled = true;
				Duration_Textbox.IsEnabled = true;
				Viponly_Checkbox.IsEnabled = false;
				Subonly_Checkbox.IsEnabled = false;
				Modonly_Checkbox.IsEnabled = false;
				Cooldown_Textbox.IsEnabled = false;
				Channelpoints_Grid.Visibility = Visibility.Hidden;
			}

			if (AnonGiftBomb_RadioButton.IsChecked == true)
			{
				CommandKeyword_Textbox.IsEnabled = false;
				Amount_Textbox.IsEnabled = true;
				Duration_Textbox.IsEnabled = true;
				Viponly_Checkbox.IsEnabled = false;
				Subonly_Checkbox.IsEnabled = false;
				Modonly_Checkbox.IsEnabled = false;
				Cooldown_Textbox.IsEnabled = false;
				Channelpoints_Grid.Visibility = Visibility.Hidden;
			}

			if (HypeTrain_RadioButton.IsChecked == true)
			{
				CommandKeyword_Textbox.IsEnabled = false;
				Amount_Textbox.IsEnabled = true;
				Duration_Textbox.IsEnabled = true;
				Viponly_Checkbox.IsEnabled = false;
				Subonly_Checkbox.IsEnabled = false;
				Modonly_Checkbox.IsEnabled = false;
				Cooldown_Textbox.IsEnabled = false;
				Channelpoints_Grid.Visibility = Visibility.Hidden;
			}

			if (Raid_RadioButton.IsChecked == true)
			{
				CommandKeyword_Textbox.IsEnabled = false;
				Amount_Textbox.IsEnabled = false;
				Duration_Textbox.IsEnabled = true;
				Viponly_Checkbox.IsEnabled = false;
				Subonly_Checkbox.IsEnabled = false;
				Modonly_Checkbox.IsEnabled = false;
				Cooldown_Textbox.IsEnabled = false;
				Channelpoints_Grid.Visibility = Visibility.Hidden;
			}

			if (Follower_RadioButton.IsChecked == true)
			{
				CommandKeyword_Textbox.IsEnabled = false;
				Amount_Textbox.IsEnabled = false;
				Duration_Textbox.IsEnabled = true;
				Viponly_Checkbox.IsEnabled = false;
				Subonly_Checkbox.IsEnabled = false;
				Modonly_Checkbox.IsEnabled = false;
				Cooldown_Textbox.IsEnabled = false;
				Channelpoints_Grid.Visibility = Visibility.Hidden;
			}

			if (Bits_RadioButton.IsChecked == true)
			{
				CommandKeyword_Textbox.IsEnabled = false;
				Amount_Textbox.IsEnabled = true;
				Duration_Textbox.IsEnabled = true;
				Viponly_Checkbox.IsEnabled = false;
				Subonly_Checkbox.IsEnabled = false;
				Modonly_Checkbox.IsEnabled = false;
				Cooldown_Textbox.IsEnabled = false;
				Channelpoints_Grid.Visibility = Visibility.Hidden;
			}

			if (Keyword_RadioButton.IsChecked == true)
			{
				CommandKeyword_Textbox.IsEnabled = true;
				Amount_Textbox.IsEnabled = false;
				Viponly_Checkbox.IsEnabled = true;
				Subonly_Checkbox.IsEnabled = true;
				Modonly_Checkbox.IsEnabled = true;
				Cooldown_Textbox.IsEnabled = true;
				Channelpoints_Grid.Visibility = Visibility.Hidden;
				if (_appSettings.NanoSettings.ChangeBackOnKeyword)
				{
					Duration_Textbox.IsEnabled = true;
				}
				else
				{
					Duration_Textbox.IsEnabled = false;
				}
			}

			if (Channelpoints_RadioButton.IsChecked == true)
			{
				CommandKeyword_Textbox.IsEnabled = false;
				Amount_Textbox.IsEnabled = false;
				Duration_Textbox.IsEnabled = true;
				Viponly_Checkbox.IsEnabled = false;
				Subonly_Checkbox.IsEnabled = false;
				Modonly_Checkbox.IsEnabled = false;
				Cooldown_Textbox.IsEnabled = false;
				Channelpoints_Grid.Visibility = Visibility.Visible;
				if (!_twitchEventSubController.IsConnected)
				{
					MessageBox.Show(Properties.Resources.Code_TriggerDetail_MessageBox_CPNoConnection, Properties.Resources.General_MessageBox_Hint_Title, MessageBoxButton.OK, MessageBoxImage.Information);
				}
			}

			if (HypeRate_RadioButton.IsChecked == true)
			{
				CommandKeyword_Textbox.IsEnabled = false;
				Amount_Textbox.IsEnabled = true;
				Duration_Textbox.IsEnabled = false;
				Viponly_Checkbox.IsEnabled = false;
				Subonly_Checkbox.IsEnabled = false;
				Modonly_Checkbox.IsEnabled = false;
				Cooldown_Textbox.IsEnabled = false;
			}

			if (Donation_RadioButton.IsChecked == true)
			{
				CommandKeyword_Textbox.IsEnabled = false;
				Amount_Textbox.IsEnabled = true;
				Duration_Textbox.IsEnabled = false;
				Viponly_Checkbox.IsEnabled = false;
				Subonly_Checkbox.IsEnabled = false;
				Modonly_Checkbox.IsEnabled = false;
				Cooldown_Textbox.IsEnabled = false;
			}

			if (UserColor_RadioButton.IsChecked == true)
			{
				CommandKeyword_Textbox.IsEnabled = false;
				Amount_Textbox.IsEnabled = false;
				Duration_Textbox.IsEnabled = false;
				Viponly_Checkbox.IsEnabled = false;
				Subonly_Checkbox.IsEnabled = false;
				Modonly_Checkbox.IsEnabled = false;
				Cooldown_Textbox.IsEnabled = false;
				Effect_RadioButton.IsEnabled = false;
				Color_RadioButton.IsEnabled = false;
				ColorPicker.IsEnabled = false;
				Effect_ComboBox.IsEnabled = false;
			}
		}

		private void EffectRadioButton_Click(object sender, RoutedEventArgs e)
		{
			if (Effect_RadioButton.IsChecked == true)
			{
				Effect_ComboBox.IsEnabled = true;
				ColorPicker.IsEnabled = false;
			}
			else
			{
				Effect_ComboBox.IsEnabled = false;
				ColorPicker.IsEnabled = true;
			}
		}
	}
}