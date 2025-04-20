using log4net;
using NanoTwitchLeafs.Colors;
using NanoTwitchLeafs.Controller;
using NanoTwitchLeafs.Enums;
using NanoTwitchLeafs.Objects;
using NanoTwitchLeafs.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NanoTwitchLeafs.Windows
{
    public partial class TriggerWindow : Window
    {
        private readonly ILog _logger = LogManager.GetLogger(typeof(TriggerWindow));
        private readonly CommandRepository _commandRepository;
        private readonly NanoController _nanoController;
        private readonly AppSettings _appSettings;
        private readonly StreamlabsController _streamlabsController;
        private readonly HypeRateIOController _hypeRateIoController;
        private readonly TriggerLogicController _triggerLogicController;
        public readonly TwitchEventSubController _twitchEventSubController;

        public TriggerWindow(CommandRepository commandRepository, NanoController nanoController, AppSettings appSettings, StreamlabsController streamlabsController, HypeRateIOController hypeRateIoController, TriggerLogicController triggerLogicController, TwitchEventSubController twitchEventSubController = null)
        {
            _commandRepository = commandRepository ?? throw new ArgumentNullException(nameof(commandRepository));
            _nanoController = nanoController ?? throw new ArgumentNullException(nameof(nanoController));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _streamlabsController = streamlabsController;
            _hypeRateIoController = hypeRateIoController;
            _triggerLogicController = triggerLogicController ?? throw new ArgumentNullException(nameof(triggerLogicController));
            _twitchEventSubController = twitchEventSubController;
            Constants.SetCultureInfo(_appSettings.Language);
            InitializeComponent();

            LoadTrigger();
        }

        private void LoadTrigger()
        {
            List<TriggerSetting> triggerSettings = _commandRepository.GetList().ToList();
            triggerSettings.OrderBy(x => x.ID);
            List<TriggerListObject> TriggerListItems = new List<TriggerListObject>();
            foreach (TriggerSetting triggerSetting in triggerSettings)
            {
                int OnOffSliderValue = 0;
                var OnOffSliderBackground = Brushes.White;
                if (triggerSetting.IsActive.HasValue && triggerSetting.IsActive.Value)
                {
                    OnOffSliderValue = 0;
                    OnOffSliderBackground = Brushes.LimeGreen;
                }
                else
                {
                    OnOffSliderValue = 1;
                    OnOffSliderBackground = Brushes.White;
                }

                string soundEffect = "X";
                if (!string.IsNullOrWhiteSpace(triggerSetting.SoundFilePath))
                {
                    string[] soundEffectArray = triggerSetting.SoundFilePath.Split('\\');

                    soundEffect = soundEffectArray[soundEffectArray.Length - 1];
                }

                string vipsubmod = "Vip[N] Sub[N] Mod[N]";
                if (triggerSetting.VipOnly)
                {
                    vipsubmod = vipsubmod.Replace("Vip[N]", "Vip[Y]");
                }
                if (triggerSetting.SubscriberOnly)
                {
                    vipsubmod = vipsubmod.Replace("Sub[N]", "Sub[Y]");
                }
                if (triggerSetting.ModeratorOnly)
                {
                    vipsubmod = vipsubmod.Replace("Mod[N]", "Mod[Y]");
                }

                Button editButton = new Button
                {
                    Width = 40,
                    Height = 22,
                    Margin = new Thickness(12, 3, 0, 3),
                    Name = "TriggerEdit_Button_" + triggerSetting.ID.ToString(),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Content = Properties.Resources.Window_Trigger_Button_Edit
                };

                editButton.Click += EditButton_Click;

                Button delete_Button = new Button
                {
                    Width = 40,
                    Height = 22,
                    Margin = new Thickness(12, 3, 0, 3),
                    Name = "TriggerDelete_Button_" + triggerSetting.ID.ToString(),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Content = Properties.Resources.Window_Trigger_Button_Delete
                };

                delete_Button.Click += DeleteButton_Click;

                TriggerListObject triggerListObject = new TriggerListObject
                {
                    OnOffSliderValue = OnOffSliderValue,
                    OnOffSliderBackground = OnOffSliderBackground,
                    ID = triggerSetting.ID.ToString(),
                    Trigger = triggerSetting.Trigger,
                    Command = triggerSetting.CMD,
                    Sound = soundEffect,
                    Duration = triggerSetting.Duration.ToString(),
                    Brightness = triggerSetting.Brightness.ToString(),
                    Amount = triggerSetting.Amount.ToString(),
                    Cooldown = triggerSetting.Cooldown.ToString(),
                    VipSubMod = vipsubmod,
                };

                if (triggerListObject.Trigger != TriggerTypeEnum.Command.ToString() && triggerListObject.Trigger != TriggerTypeEnum.Keyword.ToString())
                {
                    triggerListObject.Command = "/";
                }

                var color = ColorConverting.RgbToMediacolor(new RgbColor(triggerSetting.R, triggerSetting.G, triggerSetting.B, 255));
                if (!triggerSetting.IsColor)
                {
                    triggerListObject.Effect = triggerSetting.Effect;
                }
                else
                {
                    triggerListObject.Background = new SolidColorBrush(color);
                }

                TriggerListItems.Add(triggerListObject);
                _logger.Debug($"Loading Trigger with id {triggerSetting.ID}.");
            }
            TriggerListItems = TriggerListItems.OrderBy(x => x.Trigger).ToList();
            Trigger_Listview.ItemsSource = TriggerListItems;
            _logger.Info($"Loaded {triggerSettings.Count} Triggers from Database.");
        }

        private void OnOffSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            bool IsActive;
            Slider slider = (Slider)sender;
            if (e.NewValue == 0)
            {
                slider.Background = Brushes.LimeGreen;
                IsActive = true;
            }
            else
            {
                slider.Background = Brushes.White;
                IsActive = false;
            }

            var dataContext = slider.DataContext as TriggerListObject;
            var triggerId = int.Parse(dataContext.ID);

            List<TriggerSetting> triggerSettings = _commandRepository.GetList();
            TriggerSetting triggerSetting = triggerSettings.Where(l => l.ID == triggerId).FirstOrDefault();
            triggerSetting.IsActive = IsActive;
            _commandRepository.Update(triggerSetting);
            _logger.Info($"Trigger with the ID {triggerSetting.ID} is now updated to IsActive: {IsActive}.");
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var dataContext = button.DataContext as TriggerListObject;

            var triggerId = int.Parse(dataContext.ID);
            List<TriggerSetting> triggerSettings = _commandRepository.GetList();
            TriggerSetting triggerSetting = triggerSettings.Where(l => l.ID == triggerId).FirstOrDefault();
            _commandRepository.Delete(triggerSetting);
            LoadTrigger();
        }


        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var dataContext = button.DataContext as TriggerListObject;

            var triggerId = int.Parse(dataContext.ID);
            List<TriggerSetting> triggerSettings = _commandRepository.GetList();
            TriggerSetting triggerSetting = triggerSettings.Where(l => l.ID == triggerId).FirstOrDefault();

            var obj = new QueueObject(triggerSetting, "Test");
            _triggerLogicController.AddToQueue(obj);
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var dataContext = button.DataContext as TriggerListObject;

            var triggerId = int.Parse(dataContext.ID);
            List<TriggerSetting> triggerSettings = _commandRepository.GetList();
            TriggerSetting triggerSetting = triggerSettings.Where(l => l.ID == triggerId).FirstOrDefault();

            OpenTriggerDetails(triggerSetting);
        }

        private void NewCmd_Button_Click(object sender, RoutedEventArgs e)
        {
            OpenTriggerDetails();
        }

        private async void OpenTriggerDetails(TriggerSetting triggerSetting = null)
        {
            var effectList = await _nanoController.GetEffectList(_appSettings.NanoSettings.NanoLeafDevices[0]);

            if (effectList == null)
            {
                this.Close();
                _logger.Error("Connection failed! Couldn't get Effect List!");
                System.Windows.MessageBox.Show(Properties.Resources.Code_Trigger_MessageBox_EffectList, Properties.Resources.General_MessageBox_Error_Title);
                return;
            }

            Window triggerDetailWindow = new TriggerDetailWindow(_appSettings, _commandRepository, effectList, _streamlabsController, _hypeRateIoController, triggerSetting, _twitchEventSubController)
            {
                Owner = this
            };
            triggerDetailWindow.Closed += TriggerDetailWindow_Closed;
            triggerDetailWindow.Show();
        }

        private void TriggerDetailWindow_Closed(object sender, EventArgs e)
        {
            LoadTrigger();
        }

    }
}