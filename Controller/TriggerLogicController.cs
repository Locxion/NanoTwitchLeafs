using log4net;
using NanoTwitchLeafs.Colors;
using NanoTwitchLeafs.Enums;
using NanoTwitchLeafs.Objects;
using NanoTwitchLeafs.Repositories;
using NanoTwitchLeafs.Windows;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using WMPLib;

namespace NanoTwitchLeafs.Controller
{
	public class TriggerLogicController 
	{
		private readonly ILog _logger = LogManager.GetLogger(typeof(TriggerLogicController));
		private readonly AppSettings _appSettings;
		private readonly TwitchController _twitchController;
		private readonly CommandRepository _commandRepository;
		private readonly NanoController _nanoController;
		private readonly TwitchPubSubController _twitchPubSubController;

		private readonly Dictionary<int, DateTime> _triggerCoolDowns = new Dictionary<int, DateTime>();
		private DateTime _lastGlobalCooldown;
		private DateTime _lastNanoHelp;

		private CancellationTokenSource _queueToken;
		private CancellationTokenSource _cooldownToken;
		private readonly WindowsMediaPlayer _wmPlayer = new WindowsMediaPlayer();

		private BufferBlock<QueueObject> _queue = new BufferBlock<QueueObject>();

		private const int ChatMessageDelay = 1750;
		private int _lastHeartRate;
		private TriggerSetting _lastTrigger;

		public TriggerLogicController(AppSettings settings, TwitchController twitchController, CommandRepository commandRepository,
									  NanoController nanoController, TwitchPubSubController twitchPubSubController, StreamlabsController streamLabsController, HypeRateIOController hypeRateIoController)
		{
			_appSettings = settings;
			_twitchController = twitchController;
			_commandRepository = commandRepository;
			_nanoController = nanoController;
			_twitchPubSubController = twitchPubSubController;

			_lastGlobalCooldown = DateTime.Now;
			_lastNanoHelp = DateTime.Now;

			_twitchController.OnChatMessageReceived += HandleMessage;
			_twitchController.OnTwitchEventReceived += HandleTwitchEventTrigger;
			_twitchPubSubController.OnBitsReceived += HandleTwitchPubSubEventBits;
			_twitchPubSubController.OnFollow += HandleTwitchPubSubEventFollow;
			_twitchPubSubController.OnChannelPointsRedeemed += HandleChannelPointsTrigger;
			_twitchController.OnHostEvent += HandleHostEventTrigger;
			hypeRateIoController.OnHeartRateRecieved += HypeRateIoControllerOnHeartRateReceived;
			streamLabsController.OnDonationRecieved += StreamLabsControllerOnDonationReceived;
			RunQueueHandler();
			RunCooldownHandler();
		}

		private void StreamLabsControllerOnDonationReceived(double amount, string username)
		{
			_logger.Debug($"Received Donation from Stream Labs Server. Username: {username} - Amount: {amount}");
			HandleDonations(amount, username);
		}

		private void HandleDonations(double amount, string username)
		{
			QueueObject queueObject = null;
			var donationTrigger = _commandRepository.GetList()
				.Where(x => x.Trigger == "Donation")
				.OrderBy(x => x.Amount)
				.ToList();

			foreach (var trigger in donationTrigger)
			{
				if (!trigger.IsActive.HasValue || !trigger.IsActive.Value)
				{
					continue;
				}
				// Check if the amount of bits are higher than this trigger requires.
				if (amount < trigger.Amount)
					break;

				queueObject = new QueueObject(trigger, username);
			}

			if (queueObject != null)
			{
				AddToQueue(queueObject);
			}
		}

		private void HypeRateIoControllerOnHeartRateReceived(int heartRate)
		{
			if (_lastHeartRate == heartRate)
			{
				return;
			}

			_lastHeartRate = heartRate;
			HandleHeartRate(heartRate);
		}

		private void HandleHeartRate(int heartRate)
		{
			QueueObject queueObject = null;
			var heartTrigger = _commandRepository.GetList()
				.Where(x => x.Trigger == "HypeRate")
				.OrderBy(x => x.Amount)
				.ToList();

			TriggerSetting newTrigger = new TriggerSetting();

			foreach (var trigger in heartTrigger)
			{
				if (!trigger.IsActive.HasValue || !trigger.IsActive.Value)
				{
					continue;
				}
				// Check if the amount of bits are higher than this trigger requires.
				if (heartRate < trigger.Amount)
				{
					newTrigger = trigger;
					break;
				}

				queueObject = new QueueObject(trigger, "HeartRateIO");
			}

			if (queueObject != null)
			{
				if (_lastTrigger == null || _lastTrigger.ID == newTrigger.ID)
				{
					_lastTrigger = newTrigger;
					return;
				}

				queueObject.TriggerSetting.Duration = 0;
				AddToQueue(queueObject);
				_lastTrigger = newTrigger;
			}
		}

		private void RunCooldownHandler()
		{
			Task.Run(async () =>
			{
				_logger.Debug("Start Cooldown Handler ...");
				_cooldownToken = new CancellationTokenSource();
				var token = _cooldownToken.Token;
				while (!token.IsCancellationRequested)
				{
					await Task.Delay(1000, token);

					if (_triggerCoolDowns.Count == 0)
					{
						continue;
					}

					Dictionary<int, DateTime> expiredCoolDowns = new Dictionary<int, DateTime>();

					foreach (var triggerCoolDown in _triggerCoolDowns)
					{
						if (triggerCoolDown.Value > DateTime.Now)
						{
							continue;
						}

						expiredCoolDowns.Add(triggerCoolDown.Key, triggerCoolDown.Value);
					}

					foreach (var expiredCooldown in expiredCoolDowns)
					{
						_logger.Debug($"Removed Cooldown with Trigger ID {expiredCooldown.Key}");
						_triggerCoolDowns.Remove(expiredCooldown.Key);
					}
				}
			});
		}

		private void RunQueueHandler()
		{
			Task.Run(async () =>
			{
				_queueToken = new CancellationTokenSource();
				var token = _queueToken.Token;

				_logger.Info("Queue Started ...");
				while (!token.IsCancellationRequested)
				{
					try
					{
						QueueObject queueObject;

						try
						{
							queueObject = await _queue.ReceiveAsync(token);
						}
						catch (OperationCanceledException)
						{
							queueObject = null;
						}

						if (queueObject == null)
							continue;

						RefreshRemainingQueueElements();

						if (!string.IsNullOrWhiteSpace(queueObject.TriggerSetting.SoundFilePath))
						{
							_logger.Debug("Found Sound File Path ...");
							PlaySound(queueObject.TriggerSetting.SoundFilePath, queueObject.TriggerSetting.Volume.GetValueOrDefault(50));
						}

						if (queueObject.TriggerSetting.Trigger == TriggerTypeEnum.UsernameColor.ToString())
						{
							await SendColorToSinglePanel(queueObject.Color, queueObject.TriggerSetting.Brightness);
						}
						else
						{
							await SendEffectToController(queueObject);
						}
					}
					catch (Exception ex)
					{
						_logger.Error(ex.Message, ex);
					}
				}
			});
		}

		private void PlaySound(string path, int volume)
		{
			_wmPlayer.controls.stop();
			_wmPlayer.settings.volume = volume;

			try
			{
				_logger.Debug($"Read Sound File: {path}");
				if (!File.Exists(path))
				{
					_logger.Error($"Specified Sound File not found! Please Check: {path}!");
					return;
				}
				_wmPlayer.URL = path;
				_logger.Debug($"Play Sound File: {path}");
				_wmPlayer.controls.play();
				_wmPlayer.PlayStateChange += PlayStateChange;
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message, ex);
			}
		}

		private void PlayStateChange(int newState)
		{
			if (newState == 1)
			{
				_logger.Debug("Finished Sound File");
			}
		}

		/// <summary>
		/// Adds new QueueObject to Queue
		/// </summary>
		/// <param name="obj"></param>
		public void AddToQueue(QueueObject obj)
		{
			if (obj == null)
				return;

			_queue.Post(obj);
			_logger.Debug($"Added {obj.TriggerSetting.Trigger}-Trigger to queue");
			RefreshRemainingQueueElements();
		}

		private void RefreshRemainingQueueElements()
		{
			Application.Current.Dispatcher.Invoke(() =>
			{
				(((MainWindow)Application.Current.MainWindow)!).nanoQueueCount_TextBox.Text = _queue.Count.ToString();
			});
		}

		private void HandleTwitchPubSubEventFollow(string username)
		{
			if (CheckBlacklist(username))
				return;
			HandleTwitchEventTrigger(username, "Follower", false, 0);
		}

		private void HandleTwitchPubSubEventBits(string username, int amount)
		{
			if (CheckBlacklist(username))
				return;
			QueueObject queueObject = null;
			var bitsTrigger = _commandRepository.GetList()
				.Where(x => x.Trigger == TriggerTypeEnum.Bits.ToString())
				.OrderBy(x => x.Amount)
				.ToList();

			foreach (var trigger in bitsTrigger)
			{
				if (!trigger.IsActive.HasValue || !trigger.IsActive.Value)
				{
					continue;
				}
				// Check if the amount of bits are higher than this trigger requires.
				if (amount < trigger.Amount)
					break;

				queueObject = new QueueObject(trigger, username);
			}

			if (queueObject != null)
			{
				AddToQueue(queueObject);
			}
		}

		private void HandleChannelPointsTrigger(string username, string message, Guid guid)
		{
			if (CheckBlacklist(username))
				return;

			foreach (var trigger in _commandRepository.GetList())
			{
				if (!trigger.IsActive.HasValue || !trigger.IsActive.Value)
				{
					continue;
				}
				if (trigger.Trigger == TriggerTypeEnum.ChannelPoints.ToString() && trigger.ChannelPointsGuid == guid.ToString())
				{
					QueueObject queueObject = new QueueObject(trigger, $"{username}-ChannelPoints");
					AddToQueue(queueObject);
				}
			}
		}

		private void HandleTwitchEventTrigger(string username, string twitchEvent, bool isAnonymous, int amount)
		{
			if (CheckBlacklist(username))
				return;

			if (twitchEvent == TriggerTypeEnum.GiftBomb.ToString() || twitchEvent == TriggerTypeEnum.AnonGiftBomb.ToString())
			{
				HandleBombEventTrigger(username, twitchEvent, isAnonymous, amount);
				return;
			}

			if (isAnonymous)
			{
				twitchEvent = TriggerTypeEnum.AnonGiftSubscription.ToString();
			}

			foreach (TriggerSetting trigger in _commandRepository.GetList())
			{
				if (!trigger.IsActive.HasValue || !trigger.IsActive.Value)
				{
					continue;
				}
				if (trigger.Trigger != twitchEvent)
					continue;

				QueueObject queueObject = new QueueObject(trigger, $"TwitchEvent-{twitchEvent}");
				AddToQueue(queueObject);
				return;
			}
		}

		private void HandleBombEventTrigger(string username, string twitchEvent, bool isAnonymous, int amount)
		{
			if (isAnonymous)
			{
				twitchEvent = TriggerTypeEnum.AnonGiftBomb.ToString();
			}

			QueueObject queueObject = null;
			var bombTrigger = _commandRepository.GetList()
				.Where(x => x.Trigger == twitchEvent)
				.OrderBy(x => x.Amount)
				.ToList();

			foreach (var trigger in bombTrigger)
			{
				if (!trigger.IsActive.HasValue || !trigger.IsActive.Value)
				{
					continue;
				}
				// Check if the amount of subs are higher than this trigger requires.
				if (amount < trigger.Amount)
					break;

				queueObject = new QueueObject(trigger, username);
			}

			if (queueObject != null)
			{
				AddToQueue(queueObject);
			}
		}

		private void HandleHostEventTrigger(int amount, string username, bool isRaid)
		{
			if (CheckBlacklist(username))
				return;

			var eventType = TriggerTypeEnum.Host.ToString();
			if (isRaid)
			{
				eventType = TriggerTypeEnum.Raid.ToString();
			}

			QueueObject queueObject = null;
			var hostTrigger = _commandRepository.GetList()
				.Where(x => x.Trigger == eventType)
				.OrderBy(x => x.Amount)
				.ToList();

			foreach (var trigger in hostTrigger)
			{
				if (!trigger.IsActive.HasValue || !trigger.IsActive.Value)
				{
					continue;
				}
				// Check if the amount of viewers are higher than this trigger requires.
				if (amount < trigger.Amount)
					break;

				queueObject = new QueueObject(trigger, username);
			}

			if (queueObject != null)
			{
				AddToQueue(queueObject);
			}
		}

		public async void HandleMessage(ChatMessage chatMessage)
		{
			chatMessage.Message = chatMessage.Message.ToLower();
			chatMessage.Username = chatMessage.Username.ToLower();
			//Set Broadcaster to Sub Mod and Vip
			if (chatMessage.Username == _appSettings.ChannelName.ToLower() || Constants.DEVELOPER.Contains(chatMessage.Username))
			{
				chatMessage.IsModerator = true;
				chatMessage.IsSubscriber = true;
				chatMessage.IsVip = true;
			}

			if (chatMessage.Message.StartsWith("!nanodebug") && Constants.DEVELOPER.Contains(chatMessage.Username))
			{
				await HandleDebugCommands(chatMessage);
				return;
			}

			var isCommand = false;

			if (CheckBlacklist(chatMessage.Username))
				return;
			//If no Command found look for Keyword
			if (chatMessage.Message.StartsWith(_appSettings.CommandPrefix))
			{
				isCommand = HandleCommandTriggerAsync(chatMessage);
			}

			if (!isCommand)
			{
				HandleKeywordTrigger(chatMessage);
			}

			HandleUsernameColorTrigger(chatMessage);
		}

		private async Task HandleDebugCommands(ChatMessage chatMessage)
		{
			string message = chatMessage.Message.Substring(11);
			
			switch (message)
                {
                    case "info":
                        _twitchController.SendWhisper(chatMessage.Username, $"There are {_appSettings.NanoSettings.NanoLeafDevices.Count} Devices Paired.");
                        await Task.Delay(ChatMessageDelay);
                        foreach (NanoLeafDevice device in _appSettings.NanoSettings.NanoLeafDevices)
                        {
                            var state = await _nanoController.GetState(device);
                            var fwVersion = await _nanoController.GetFirmwareVersionFromDevice(IPAddress.Parse(device.Address));
                            _twitchController.SendWhisper(chatMessage.Username, $"Device: {device.PublicName} - FW: v{fwVersion} - Effect: {state.Effect}");
                            await Task.Delay(ChatMessageDelay);
                        }
                        _twitchController.SendWhisper(chatMessage.Username, $"There are {_commandRepository.GetCount()} Triggers configured.");
                        await Task.Delay(ChatMessageDelay);
                        _twitchController.SendWhisper(chatMessage.Username, $"General Settings: Autostart: {_appSettings.AutoConnect}, Responses: {_appSettings.ChatResponse}, WhisperMode: {_appSettings.WhisperMode}.");
                        await Task.Delay(ChatMessageDelay);
                        _twitchController.SendWhisper(chatMessage.Username, $"Cooldown: {_appSettings.NanoSettings.CooldownEnabled}-{_appSettings.NanoSettings.Cooldown}, ChangeBack on Commands: {_appSettings.NanoSettings.ChangeBackOnCommand} - Keywords: {_appSettings.NanoSettings.ChangeBackOnKeyword}.");
                        await Task.Delay(ChatMessageDelay);
                        _twitchController.SendWhisper(chatMessage.Username, $"Events in Queue: {_queue.Count}, Command Prefix: '{_appSettings.CommandPrefix}'.");
                        break;

                    case "get effects":
                        var effects = await _nanoController.GetEffectList(_appSettings.NanoSettings.NanoLeafDevices[0]);
                        _twitchController.SendWhisper(chatMessage.Username, string.Join(", ", effects));
                        break;

                    case "get trigger":
                        List<TriggerSetting> triggers = _commandRepository.GetList();
                        _twitchController.SendWhisper(chatMessage.Username, $"There are {triggers.Count} Triggers configured:");
                        await Task.Delay(ChatMessageDelay);
                        foreach (var trigger in triggers)
                        {
                            _twitchController.SendWhisper(chatMessage.Username, $"ID {trigger.ID},Active: {trigger.IsActive}, {trigger.Trigger}, {trigger.CMD}, {trigger.Effect}, Dur: {trigger.Duration}, Amount: {trigger.Amount}, Bright: {trigger.Brightness}, CD: {trigger.Cooldown}, Vip: {trigger.VipOnly}, Sub: {trigger.SubscriberOnly}, Mod: {trigger.ModeratorOnly}.");
                            await Task.Delay(ChatMessageDelay);
                        }
                        _twitchController.SendWhisper(chatMessage.Username, $"List complete.");
                        break;

                    case "reset queue":
                        _twitchController.SendWhisper(chatMessage.Username, $"Removed {_queue.Count} Events from Queue.");
                        ResetEventQueue();
                        break;
                }
			
			if (message.StartsWith("set effect"))
			{
				var effectString = message.Substring(11);

				var array = effectString.Split(' ');
				var effect = "";
				var arrayLength = array.Count();
				if (arrayLength > 1 && int.TryParse(array[arrayLength - 1], out int duration))
				{
					for (int i = 0; i < arrayLength - 1; i++)
					{
						effect += array[i] + " ";
					}
					effect = effect.Remove(effect.Length - 1, 1);
				}
				else
				{
					effect = array[0];
					duration = 5;
				}

				var triggerSetting = new TriggerSetting { Brightness = 100, Duration = duration, Effect = effect, IsColor = false };
				var queueObject = new QueueObject(triggerSetting, $"{chatMessage.Username}[Developer]");
				AddToQueue(queueObject);
			}
			if (message.StartsWith("set rgb"))
			{
				var rgbString = message.Substring(8);

				var rgbArray = rgbString.Split(' ');
				const string effect = "*Solid*";
				var arrayLength = rgbArray.Count();
				if (arrayLength > 3 && int.TryParse(rgbArray[arrayLength - 1], out var duration1))
				{
					// empty
				}
				else
				{
					duration1 = 5;
				}

				var triggerSetting = new TriggerSetting { Brightness = 100, Duration = duration1, Effect = effect, IsColor = true, R = Convert.ToByte(rgbArray[0]), G = Convert.ToByte(rgbArray[1]), B = Convert.ToByte(rgbArray[2]) };
				var queueObject = new QueueObject(triggerSetting, $"{chatMessage.Username} [Developer]");
				AddToQueue(queueObject);
			}
		}

		private void HandleUsernameColorTrigger(ChatMessage chatMessage)
		{
			if (!_commandRepository.TriggerOfTypeExists(TriggerTypeEnum.UsernameColor))
			{
				return;
			}

			var trigger = _commandRepository.GetTriggerOfType(TriggerTypeEnum.UsernameColor);
			if (trigger == null || trigger.IsActive == false)
			{
				return;
			}

			if (chatMessage.Color.IsEmpty)
			{
				return;
			}

			var queueObject = new QueueObject(trigger, chatMessage.Username, false, chatMessage.Color);
			AddToQueue(queueObject);
			_logger.Debug($"Added Color #{chatMessage.Color.Name} from {chatMessage.Username} to Queue.");
		}

		private async Task SendColorToSinglePanel(Color color, int brightness)
		{
			var random = new Random();

			foreach (var device in _appSettings.NanoSettings.NanoLeafDevices)
			{
				if (device.NanoleafControllerInfo is null)
				{
					_logger.Warn($"Nanoleaf Info for Device {device.Address} is null!");
					continue;
				}
				if (device.NanoleafControllerInfo.panelLayout is null)
				{
					_logger.Warn($"Panel Layout for Device {device.Address} is null!");
					continue;
				}
				if (device.NanoleafControllerInfo.panelLayout.layout is null)
				{
					_logger.Warn($"Layout for Device {device.Address} is null!");
					continue;
				}				
				if (device.NanoleafControllerInfo.panelLayout.layout.numPanels <= 0)
				{
					_logger.Warn($"Number of Panels for Device {device.Address} is 0!");
					continue;
				}
                
				
				var randomIndex = random.Next(0, device.NanoleafControllerInfo.panelLayout.layout.numPanels);

				var panelId = device.NanoleafControllerInfo.panelLayout.layout.positionData[randomIndex]
					.panelId;
				_logger.Debug($"Set Panel with ID: {panelId} to Color #{color.Name} on Device {device.PublicName}");

				await _nanoController.SetPanelColor(device, panelId, color.R, color.G, color.B, color.A);
				await _nanoController.SetBrightness(device, brightness);
			}
		}

		private bool CheckBlacklist(string username)
		{
			if (_appSettings.BlacklistEnabled && _appSettings.Blacklist.Contains(username.ToLower()))
			{
				_logger.Warn($"Event/Message from '{username}' was ignored cause of a Blacklist Entry!");
				return true;
			}
			return false;
		}

		private bool HandleCommandTriggerAsync(ChatMessage chatMessage)
		{
			QueueObject queueObject;

			// Help Command
			if (chatMessage.Message == $"{_appSettings.CommandPrefix}nano help")
			{
				HandleHelpMessage(chatMessage.Username);
				return true;
			}

			// Remove Command Prefix from Message
			chatMessage.Message = chatMessage.Message.Substring(1);

			// Check if Message is empty
			if (string.IsNullOrWhiteSpace(chatMessage.Message))
			{
				return false;
			}

			// Get Object matching to the Command
			TriggerSetting trigger = _commandRepository.GetByCommandName(chatMessage.Message);

			if (trigger == null)
			{
				_logger.Debug("Selected command was 'null'!");
				return false;
			}

			// Response, when trigger is disabled
			if (!_appSettings.NanoSettings.TriggerEnabled)
			{
				if (_appSettings.WhisperMode)
				{
					_twitchController.SendWhisper(chatMessage.Username, Properties.Resources.Code_TriggerLogic_ChatMessage_TriggerNotActive);
				}
				else
				{
					SendMessageToChat($"@{chatMessage.Username}! " + Properties.Resources.Code_TriggerLogic_ChatMessage_TriggerNotActive);
				}

				return true;
			}

			if (!trigger.IsActive.HasValue || !trigger.IsActive.Value)
			{
				return false;
			}

			//Check for Moderator // Broadcaster Only Command
			if (trigger.ModeratorOnly && trigger.Trigger == TriggerTypeEnum.Command.ToString())
			{
				if (!chatMessage.IsModerator && chatMessage.Username != _appSettings.ChannelName)
				{
					if (_appSettings.WhisperMode)
					{
						_twitchController.SendWhisper(chatMessage.Username, Properties.Resources.Code_TriggerLogic_ChatMessage_ModOnly);
					}
					else
					{
						SendMessageToChat($"@{chatMessage.Username}! " + Properties.Resources.Code_TriggerLogic_ChatMessage_ModOnly);
					}
					return true;
				}

				if (_appSettings.NanoSettings.ChangeBackOnCommand)
				{
					queueObject = new QueueObject(trigger, chatMessage.Username);
					AddToQueue(queueObject);
					return true;
				}

				queueObject = new QueueObject(trigger, chatMessage.Username);
				AddToQueue(queueObject);
				return true;
			}

			//Check for Moderator Ignore Cooldown
			if (chatMessage.IsModerator && _appSettings.NanoSettings.CooldownIgnore)
			{
				if (_appSettings.NanoSettings.ChangeBackOnCommand)
				{
					queueObject = new QueueObject(trigger, chatMessage.Username);
					AddToQueue(queueObject);
					return true;
				}

				queueObject = new QueueObject(trigger, chatMessage.Username);
				AddToQueue(queueObject);
				return true;
			}

			// Check if Global Cooldown is enabled and over
			double seconds = (DateTime.Now - _lastGlobalCooldown).TotalSeconds;
			if (_appSettings.NanoSettings.CooldownEnabled && seconds < _appSettings.NanoSettings.Cooldown)
			{
				if (_appSettings.WhisperMode)
				{
					_twitchController.SendWhisper(chatMessage.Username, Properties.Resources.Code_TriggerLogic_ChatMessage_Cooldown);
				}
				else
				{
					SendMessageToChat($"@{chatMessage.Username}! " + Properties.Resources.Code_TriggerLogic_ChatMessage_Cooldown);
				}
				return true;
			}

			// Check for Trigger Cooldown
			if (_triggerCoolDowns.ContainsKey(trigger.ID))
			{
				if (_appSettings.WhisperMode)
				{
					_twitchController.SendWhisper(chatMessage.Username, Properties.Resources.Code_TriggerLogic_ChatMessage_Cooldown);
				}
				else
				{
					SendMessageToChat($"@{chatMessage.Username}! " + Properties.Resources.Code_TriggerLogic_ChatMessage_Cooldown);
				}
				return true;
			}

			//Check for Vip Only Trigger
			if (trigger.VipOnly && !chatMessage.IsVip)
			{
				if (_appSettings.WhisperMode)
				{
					_twitchController.SendWhisper(chatMessage.Username, Properties.Resources.Code_TriggerLogic_ChatMessage_VipOnly);
				}
				else
				{
					SendMessageToChat($"@{chatMessage.Username}! " + Properties.Resources.Code_TriggerLogic_ChatMessage_VipOnly);
				}
				return true;
			}

			//Check for Subscriber Only Trigger
			if (trigger.SubscriberOnly && !chatMessage.IsSubscriber)
			{
				if (_appSettings.WhisperMode)
				{
					_twitchController.SendWhisper(chatMessage.Username, Properties.Resources.Code_TriggerLogic_ChatMessage_SubOnly);
				}
				else
				{
					SendMessageToChat($"@{chatMessage.Username}! " + Properties.Resources.Code_TriggerLogic_ChatMessage_SubOnly);
				}
				return true;
			}

			DateTime triggerDatTime = DateTime.Now;

			// Check if Auto Restore is true
			if (_appSettings.NanoSettings.ChangeBackOnCommand)
			{
				queueObject = new QueueObject(trigger, chatMessage.Username);
				AddToQueue(queueObject);
				_lastGlobalCooldown = DateTime.Now;
				_triggerCoolDowns.Add(trigger.ID, triggerDatTime.AddSeconds(trigger.Cooldown));
				return true;
			}

			queueObject = new QueueObject(trigger, chatMessage.Username);
			AddToQueue(queueObject);
			_lastGlobalCooldown = DateTime.Now;
			_triggerCoolDowns.Add(trigger.ID, triggerDatTime.AddSeconds(trigger.Cooldown));
			return true;
		}

		private void HandleHelpMessage(string username)
		{
			if ((DateTime.Now - _lastNanoHelp).TotalSeconds < 5)
			{
				return;
			}

			_lastNanoHelp = DateTime.Now;

			if (!_appSettings.NanoSettings.TriggerEnabled)
			{
				if (_appSettings.WhisperMode)
				{
					_twitchController.SendWhisper(username, Properties.Resources.Code_TriggerLogic_ChatMessage_TriggerNotActive);
				}
				else
				{
					SendMessageToChat($"@{username}! " + Properties.Resources.Code_TriggerLogic_ChatMessage_TriggerNotActive);
				}

				return;
			}

			List<TriggerSetting> activeCommands = new List<TriggerSetting>();
			List<TriggerSetting> moderatorCommands = new List<TriggerSetting>();
			List<TriggerSetting> activeKeywords = new List<TriggerSetting>();

			foreach (TriggerSetting triggerSetting in _commandRepository.GetList())
			{
				if (!triggerSetting.ModeratorOnly && triggerSetting.Trigger == TriggerTypeEnum.Command.ToString())
				{
					activeCommands.Add(triggerSetting);
				}

				if (triggerSetting.ModeratorOnly && triggerSetting.Trigger == TriggerTypeEnum.Command.ToString())
				{
					moderatorCommands.Add(triggerSetting);
				}

				if (triggerSetting.Trigger == TriggerTypeEnum.Keyword.ToString())
				{
					activeKeywords.Add(triggerSetting);
				}
			}

			var currentCommandsAnswerStringBuilder = new StringBuilder($"{username}! " + $"{Properties.Resources.Code_TriggerLogic_ChatMessage_ActiveCommands}");
			var currentModCommandsAnswerStringBuilder = new StringBuilder("Active ModeratorCommands:");
			var currentKeywordsAnswerStringBuilder = new StringBuilder("Active Keywords:");
			foreach (var command in activeCommands)
			{
				currentCommandsAnswerStringBuilder.Append($" - [{_appSettings.CommandPrefix}{command.CMD}]");
			}
			foreach (var command in moderatorCommands)
			{
				currentModCommandsAnswerStringBuilder.Append($" - [{_appSettings.CommandPrefix}{command.CMD}]");
			}
			foreach (var command in activeKeywords)
			{
				currentKeywordsAnswerStringBuilder.Append($" - [{command.CMD}]");
			}

			if (activeCommands.Count == 0)
			{
				SendMessageToChat(Properties.Resources.Code_TriggerLogic_ChatMessage_NoActiveCommands);
				return;
			}

			var message = currentCommandsAnswerStringBuilder.ToString();
			var splitMessages = HelperClass.SplitString(message, Constants.TwitchMessageMaxLength, '[');

			foreach (var splitMessage in splitMessages)
			{
				if (_appSettings.WhisperMode)
				{
					_twitchController.SendWhisper(username, splitMessage);
				}
				else
				{
					SendMessageToChat(splitMessage);
				}
			}
		}

		private void HandleKeywordTrigger(ChatMessage chatMessage)
		{
			QueueObject queueObject;
			TriggerSetting trigger = GetTriggerSettingFromMessage(chatMessage.Message, chatMessage.IsSubscriber, chatMessage.IsModerator);

			if (trigger == null)
			{
				return;
			}

			if (!trigger.IsActive.HasValue || !trigger.IsActive.Value)
			{
				return;
			}

			if ((chatMessage.IsModerator && _appSettings.NanoSettings.CooldownIgnore) || chatMessage.Username == _appSettings.ChannelName)
			{
				if (_appSettings.NanoSettings.ChangeBackOnKeyword)
				{
					queueObject = new QueueObject(trigger, chatMessage.Username, true);
					AddToQueue(queueObject);
					return;
				}
				else
				{
					queueObject = new QueueObject(trigger, chatMessage.Username, true);
					AddToQueue(queueObject);
					return;
				}
			}

			DateTime triggerDateTime = DateTime.Now;
			// Check if Cooldown is enabled and over
			if (_appSettings.NanoSettings.CooldownEnabled && ((DateTime.Now - _lastGlobalCooldown).TotalSeconds < _appSettings.NanoSettings.Cooldown))
			{
				return;
			}

			if (_triggerCoolDowns.ContainsKey(trigger.ID))
			{
				return;
			}

			if (_appSettings.NanoSettings.ChangeBackOnKeyword)
			{
				queueObject = new QueueObject(trigger, chatMessage.Username, true);
				AddToQueue(queueObject);
				_lastGlobalCooldown = DateTime.Now;
				_triggerCoolDowns.Add(trigger.ID, triggerDateTime.AddSeconds(trigger.Cooldown));
				return;
			}

			queueObject = new QueueObject(trigger, chatMessage.Username, true);
			AddToQueue(queueObject);
			_lastGlobalCooldown = DateTime.Now;
			_triggerCoolDowns.Add(trigger.ID, triggerDateTime.AddSeconds(trigger.Cooldown));
		}

		private TriggerSetting GetTriggerSettingFromMessage(string message, bool isSubscriber, bool isModerator)
		{
			foreach (TriggerSetting triggerSetting in _commandRepository.GetList())
			{
				if (triggerSetting.SubscriberOnly && isSubscriber == false)
					continue;

				if (triggerSetting.ModeratorOnly && isModerator == false)
					continue;

				if (triggerSetting.Trigger != TriggerTypeEnum.Keyword.ToString())
				{
					continue;
				}

				string[] keywordArray = triggerSetting.CMD.Split(',');
				foreach (var keyword in keywordArray)
				{
					if (message.IndexOf(keyword, 0, StringComparison.CurrentCultureIgnoreCase) != -1)
					{
						return triggerSetting;
					}
				}
			}

			return null;
		}

		private async Task SendEffectToController(QueueObject queueObject)
		{
			_logger.Debug($"Set Effect '{queueObject.TriggerSetting.Effect}' for {queueObject.TriggerSetting.Duration} Seconds on {_appSettings.NanoSettings.NanoLeafDevices.Count} Devices");
			Dictionary<NanoLeafDevice, NanoLeafState> currentStates = new Dictionary<NanoLeafDevice, NanoLeafState>();
			foreach (var device in _appSettings.NanoSettings.NanoLeafDevices)
			{
				var currentState = await _nanoController.GetState(device);

				_logger.Debug($"Save Current state '{currentState}' from Device {device.PublicName}");
				currentStates.Add(device, currentState);
			}

			foreach (var device in _appSettings.NanoSettings.NanoLeafDevices)
			{
				_logger.Debug($"Set Effect '{queueObject.TriggerSetting.Effect}' on {device.PublicName} for {queueObject.TriggerSetting.Duration} Seconds");
				if (!queueObject.TriggerSetting.IsColor)
				{
					await _nanoController.SetEffect(device, queueObject.TriggerSetting.Effect);
				}
				else
				{
					var color = new RgbColor(queueObject.TriggerSetting.R, queueObject.TriggerSetting.G, queueObject.TriggerSetting.B, 255);
					await _nanoController.SetColor(device, color);
				}
				await _nanoController.SetBrightness(device, queueObject.TriggerSetting.Brightness);
			}

			if (!queueObject.Username.Contains("TwitchEvent"))
			{
				string response;
				if (!queueObject.TriggerSetting.IsColor)
				{
					response = _appSettings.Responses.CommandDurationResponse.Replace("{username}", queueObject.Username).Replace("{effect}", queueObject.TriggerSetting.Effect).Replace("{duration}", queueObject.TriggerSetting.Duration.ToString());
				}
				else
				{
					response = _appSettings.Responses.CommandDurationResponse.Replace("{username}", queueObject.Username).Replace("{effect}", new RgbColor(queueObject.TriggerSetting.R, queueObject.TriggerSetting.G, queueObject.TriggerSetting.B, 255).ToString()).Replace("{duration}", queueObject.TriggerSetting.Duration.ToString());
				}
				if (!string.IsNullOrWhiteSpace(response))
					SendChatResponse(response);
			}

			if (queueObject.TriggerSetting.Duration != 0)
			{
				await Task.Delay(Convert.ToInt32(queueObject.TriggerSetting.Duration) * 1000);

				_logger.Debug($"Change back to last state on {_appSettings.NanoSettings.NanoLeafDevices.Count} Devices");
				foreach (var device in _appSettings.NanoSettings.NanoLeafDevices)
				{
					var oldState = currentStates[device];
					_logger.Debug($"Change back to state '{oldState.Effect}' on {device.PublicName}");
					if (oldState.Effect != "*Solid*")
					{
						await _nanoController.SetEffect(device, oldState.Effect);
						await _nanoController.SetBrightness(device, oldState.State.brightness.value);
						if (!oldState.State.on.value)
						{
							await _nanoController.SetOnState(device, false);
						}
					}
					else
					{
						await _nanoController.SetState(device, oldState);
					}

					_wmPlayer.controls.stop();
				}
			}
		}

		private void SendChatResponse(string message)
		{
			if (!_appSettings.ChatResponse || message == "")
			{
				return;
			}

			SendMessageToChat(message);
		}

		/// <summary>
		/// Deletes all remaining Objects in Queue
		/// </summary>
		public void ResetEventQueue()
		{
			var count = _queue.Count;
			_logger.Info($"Removed {count} Events from Queue.");

			_queue = new BufferBlock<QueueObject>();
			RefreshRemainingQueueElements();
		}

		private void SendMessageToChat(string message)
		{
			var formattedMessage = $"{DateTime.Now.ToLongTimeString()}: >>>>> - {message}";
			/* TODO: Fix message into ListBox
            ((MainWindow)App.Current.MainWindow).twitchChat_ListBox.Dispatcher.Invoke(() =>
            {
                ((MainWindow)App.Current.MainWindow)._twitchChat.Add(formattedMessage);
                ((MainWindow)App.Current.MainWindow).twitchChat_ListBox.Items.Refresh();
                ((MainWindow)App.Current.MainWindow).UpdateScrollBar(((MainWindow)App.Current.MainWindow).twitchChat_ListBox);
            });
            */
			_twitchController.SendMessageToChat(message);
		}

		/// <summary>
		/// Restarts Queue
		/// </summary>
		public void RestartEventQueue()
		{
			_logger.Info("Restarting Queue...");
			_queueToken.Cancel();
			RunQueueHandler();
		}
	}
}