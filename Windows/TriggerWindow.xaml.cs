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
            var triggerSettings = _triggerRepositoryService.GetList().ToList();
            triggerSettings.OrderBy(x => x.ID);
            var triggerListItems = new List<TriggerListObject>();
            foreach (var triggerSetting in triggerSettings)
            {
                var onFffSliderValue = 0;
                SolidColorBrush onOffSliderBackground;
                if (triggerSetting.IsActive.HasValue && triggerSetting.IsActive.Value)
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
                if (!string.IsNullOrWhiteSpace(triggerSetting.SoundFilePath))
                {
                    string[] soundEffectArray = triggerSetting.SoundFilePath.Split('\\');

                    soundEffect = soundEffectArray[soundEffectArray.Length - 1];
                }

                var vipsubmod = "Vip[N] Sub[N] Mod[N]";
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

                var editButton = new Button
                {
                    Width = 40,
                    Height = 22,
                    Margin = new Thickness(12, 3, 0, 3),
                    Name = "TriggerEdit_Button_" + triggerSetting.ID.ToString(),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Content = Properties.Resources.Window_Trigger_Button_Edit
                };

                editButton.Click += EditButton_Click;

                var deleteButton = new Button
                {
                    Width = 40,
                    Height = 22,
                    Margin = new Thickness(12, 3, 0, 3),
                    Name = "TriggerDelete_Button_" + triggerSetting.ID.ToString(),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Content = Properties.Resources.Window_Trigger_Button_Delete
                };

                deleteButton.Click += DeleteButton_Click;

                var triggerListObject = new TriggerListObject
                {
                    OnOffSliderValue = onFffSliderValue,
                    OnOffSliderBackground = onOffSliderBackground,
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

                triggerListItems.Add(triggerListObject);
                _logger.Debug($"Loading Trigger with id {triggerSetting.ID}.");
            }
            triggerListItems = triggerListItems.OrderBy(x => x.Trigger).ToList();
            Trigger_Listview.ItemsSource = triggerListItems;
            _logger.Info($"Loaded {triggerSettings.Count} Triggers from Database.");
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

            var triggerSettings = _triggerRepositoryService.GetList();
            var triggerSetting = triggerSettings.FirstOrDefault(l => l.ID == triggerId);
            triggerSetting.IsActive = IsActive;
            _triggerRepositoryService.Update(triggerSetting);
            _logger.Info($"Trigger with the ID {triggerSetting.ID} is now updated to IsActive: {IsActive}.");
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var dataContext = button.DataContext as TriggerListObject;

            var triggerId = int.Parse(dataContext.ID);
            var triggerSettings = _triggerRepositoryService.GetList();
            var triggerSetting = triggerSettings.FirstOrDefault(l => l.ID == triggerId);
            _triggerRepositoryService.Delete(triggerSetting);
            LoadTrigger();
        }


        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var dataContext = button.DataContext as TriggerListObject;

            var triggerId = int.Parse(dataContext.ID);
            var triggerSettings = _triggerRepositoryService.GetList();
            var triggerSetting = triggerSettings.FirstOrDefault(l => l.ID == triggerId);

            var obj = new QueueObject(triggerSetting, "Test");
            _triggerService.AddToQueue(obj);
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var dataContext = button.DataContext as TriggerListObject;

            var triggerId = int.Parse(dataContext.ID);
            var triggerSettings = _triggerRepositoryService.GetList();
            var triggerSetting = triggerSettings.FirstOrDefault(l => l.ID == triggerId);

            OpenTriggerDetails(triggerSetting);
        }

        private void NewCmd_Button_Click(object sender, RoutedEventArgs e)
        {
            OpenTriggerDetails();
        }

        private async void OpenTriggerDetails(TriggerSetting triggerSetting = null)
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
            triggerDetailWindow.TriggerSetting = triggerSetting;
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