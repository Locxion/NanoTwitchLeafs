using log4net;
using NanoTwitchLeafs.Colors;
using NanoTwitchLeafs.Enums;
using NanoTwitchLeafs.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using NanoTwitchLeafs.Interfaces;
using Trigger = NanoTwitchLeafs.Objects.Trigger;

namespace NanoTwitchLeafs.Windows
{
    public partial class TriggerWindow : Window
    {
        private readonly ILog _logger = LogManager.GetLogger(typeof(TriggerWindow));
        private readonly ITriggerRepositoryService _triggerRepositoryService;
        private readonly ISettingsService _settingsService;
        private readonly ITriggerService _triggerService;
        private readonly INanoService _nanoService;

        public TriggerWindow(ITriggerRepositoryService triggerRepositoryService, ISettingsService settingsService, ITriggerService triggerService, INanoService nanoService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _triggerService = triggerService ?? throw new ArgumentNullException(nameof(triggerService));
            _nanoService = nanoService ?? throw new ArgumentNullException(nameof(nanoService));
            _triggerRepositoryService = triggerRepositoryService ?? throw new ArgumentNullException(nameof(triggerRepositoryService));

            Constants.SetCultureInfo(_settingsService.CurrentSettings.Language);
            InitializeComponent();

            LoadTrigger();
        }

        private void LoadTrigger()
        {
            var triggers = _triggerRepositoryService.GetList().ToList();
            triggers.OrderBy(x => x.Id);
            var triggerListItems = new List<TriggerListObject>();
            foreach (var trigger in triggers)
            {
                var onFffSliderValue = 0;
                SolidColorBrush onOffSliderBackground;
                if (trigger.IsActive.HasValue && trigger.IsActive.Value)
                {
                    onFffSliderValue = 0;
                    onOffSliderBackground = Brushes.LimeGreen;
                }
                else
                {
                    onFffSliderValue = 1;
                    onOffSliderBackground = Brushes.White;
                }

                var soundEffect = "X";
                if (!string.IsNullOrWhiteSpace(trigger.SoundFilePath))
                {
                    string[] soundEffectArray = trigger.SoundFilePath.Split('\\');

                    soundEffect = soundEffectArray[soundEffectArray.Length - 1];
                }

                var vipsubmod = "Vip[N] Sub[N] Mod[N]";
                if (trigger.VipOnly)
                {
                    vipsubmod = vipsubmod.Replace("Vip[N]", "Vip[Y]");
                }
                if (trigger.SubscriberOnly)
                {
                    vipsubmod = vipsubmod.Replace("Sub[N]", "Sub[Y]");
                }
                if (trigger.ModeratorOnly)
                {
                    vipsubmod = vipsubmod.Replace("Mod[N]", "Mod[Y]");
                }

                var editButton = new Button
                {
                    Width = 40,
                    Height = 22,
                    Margin = new Thickness(12, 3, 0, 3),
                    Name = "TriggerEdit_Button_" + trigger.Id.ToString(),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Content = Properties.Resources.Window_Trigger_Button_Edit
                };

                editButton.Click += EditButton_Click;

                var deleteButton = new Button
                {
                    Width = 40,
                    Height = 22,
                    Margin = new Thickness(12, 3, 0, 3),
                    Name = "TriggerDelete_Button_" + trigger.Id.ToString(),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Content = Properties.Resources.Window_Trigger_Button_Delete
                };

                deleteButton.Click += DeleteButton_Click;

                var triggerListObject = new TriggerListObject
                {
                    OnOffSliderValue = onFffSliderValue,
                    OnOffSliderBackground = onOffSliderBackground,
                    ID = trigger.Id.ToString(),
                    Trigger = trigger.Type,
                    Command = trigger.ChatCommand,
                    Sound = soundEffect,
                    Duration = trigger.Duration.ToString(),
                    Brightness = trigger.Brightness.ToString(),
                    Amount = trigger.Amount.ToString(),
                    Cooldown = trigger.Cooldown.ToString(),
                    VipSubMod = vipsubmod,
                };

                if (triggerListObject.Trigger != TriggerTypeEnum.Command.ToString() && triggerListObject.Trigger != TriggerTypeEnum.Keyword.ToString())
                {
                    triggerListObject.Command = "/";
                }

                var color = ColorConverting.RgbToMediacolor(new RgbColor(trigger.R, trigger.G, trigger.B, 255));
                if (!trigger.IsColor)
                {
                    triggerListObject.Effect = trigger.Effect;
                }
                else
                {
                    triggerListObject.Background = new SolidColorBrush(color);
                }

                triggerListItems.Add(triggerListObject);
                _logger.Debug($"Loading Trigger with id {trigger.Id}.");
            }
            triggerListItems = triggerListItems.OrderBy(x => x.Trigger).ToList();
            Trigger_Listview.ItemsSource = triggerListItems;
            _logger.Info($"Loaded {triggers.Count} Triggers from Database.");
        }

        private void OnOffSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            bool IsActive;
            var slider = (Slider)sender;
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

            var triggers = _triggerRepositoryService.GetList();
            var trigger = triggers.FirstOrDefault(l => l.Id == triggerId);
            trigger.IsActive = IsActive;
            _triggerRepositoryService.Update(trigger);
            _logger.Info($"Trigger with the ID {trigger.Id} is now updated to IsActive: {IsActive}.");
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var dataContext = button.DataContext as TriggerListObject;

            var triggerId = int.Parse(dataContext.ID);
            var triggers = _triggerRepositoryService.GetList();
            var trigger = triggers.FirstOrDefault(l => l.Id == triggerId);
            _triggerRepositoryService.Delete(trigger);
            LoadTrigger();
        }


        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var dataContext = button.DataContext as TriggerListObject;

            var triggerId = int.Parse(dataContext.ID);
            var triggers = _triggerRepositoryService.GetList();
            var trigger = triggers.FirstOrDefault(l => l.Id == triggerId);

            var obj = new QueueObject(trigger, "Test");
            _triggerService.AddToQueue(obj);
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var dataContext = button.DataContext as TriggerListObject;

            var triggerId = int.Parse(dataContext.ID);
            var triggers = _triggerRepositoryService.GetList();
            var trigger = triggers.FirstOrDefault(l => l.Id == triggerId);

            OpenTriggerDetails(trigger);
        }

        private void NewCmd_Button_Click(object sender, RoutedEventArgs e)
        {
            OpenTriggerDetails();
        }

        private async void OpenTriggerDetails(Trigger trigger = null)
        {
            var effectList = await _nanoService.GetEffectList(_settingsService.CurrentSettings.NanoSettings.NanoLeafDevices[0]);

            if (effectList == null)
            {
                Close();
                _logger.Error("Connection failed! Couldn't get Effect List!");
                MessageBox.Show(Properties.Resources.Code_Trigger_MessageBox_EffectList, Properties.Resources.General_MessageBox_Error_Title);
                return;
            }       
            var serviceProvider = DependencyConfig.ServiceProvider;

            var triggerDetailWindow = serviceProvider.GetRequiredService<TriggerDetailWindow>();
            triggerDetailWindow.Trigger = trigger;
            triggerDetailWindow.Owner = this;
            triggerDetailWindow.Closed += TriggerDetailWindow_Closed;
            triggerDetailWindow.Show();
        }

        private void TriggerDetailWindow_Closed(object sender, EventArgs e)
        {
            LoadTrigger();
        }

    }
}