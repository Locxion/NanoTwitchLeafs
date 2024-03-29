﻿using log4net;
using NanoTwitchLeafs.Objects;
using System;
using System.Windows;
using NanoTwitchLeafs.Interfaces;

namespace NanoTwitchLeafs.Windows
{
    /// <summary>
    /// Interaction logic for DevicesInfoWindow.xaml
    /// </summary>
    public partial class DevicesInfoWindow : Window
    {
        private readonly ISettingsService _settingsService;
        private readonly INanoService _nanoService;
        private readonly ILog _logger = LogManager.GetLogger(typeof(DevicesInfoWindow));
        
        public DevicesInfoWindow(ISettingsService settingsService, INanoService nanoService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _nanoService = nanoService ?? throw new ArgumentNullException(nameof(nanoService));
            Constants.SetCultureInfo(_settingsService.CurrentSettings.Language);
            InitializeComponent();
            InitializeData();
        }

        private void InitializeData()
        {
            if (_settingsService.CurrentSettings.NanoSettings.NanoLeafDevices.Count == 0)
            {
                removeDevice_Button.IsEnabled = false;
                renameDevice_Button.IsEnabled = false;
                pingDevice_Button.IsEnabled = false;    
                return;
            }
            foreach (var device in _settingsService.CurrentSettings.NanoSettings.NanoLeafDevices)
            {
                devices_ListBox.Items.Add($"{device.PublicName} - {device.Address}:{device.Port} - {device.DeviceName}");
            }
            devices_ListBox.SelectedIndex = 0;
            _logger.Info($"Showing {_settingsService.CurrentSettings.NanoSettings.NanoLeafDevices.Count} Devices in List");
        }

        private void Close_Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void RemoveDevice_Button_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = devices_ListBox.SelectedIndex;
            NanoLeafDevice nanoLeafDevice = _settingsService.CurrentSettings.NanoSettings.NanoLeafDevices[selectedIndex];
            if (MessageBox.Show(string.Format(Properties.Resources.Code_Devices_MessageBox_RemoveConfirm, nanoLeafDevice.DeviceName), Properties.Resources.Code_Devices_MessageBox_RemoveConfirm_Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
            {
                return;
            }

            _settingsService.CurrentSettings.NanoSettings.NanoLeafDevices.Remove(nanoLeafDevice);

            _logger.Info($"Removed Device {nanoLeafDevice.PublicName}.");
            devices_ListBox.Items.Clear();
            InitializeData();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            // MessageBox.Show("Please dont forget to press Save and restart the Program if you removed one or more Devices!");
        }

        private void Rename_Button_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = devices_ListBox.SelectedIndex;
            string oldName = _settingsService.CurrentSettings.NanoSettings.NanoLeafDevices[selectedIndex].PublicName;
            _settingsService.CurrentSettings.NanoSettings.NanoLeafDevices[selectedIndex].PublicName = _nanoService.GetUserInputNameForNewDevice();

            _settingsService.SaveSettings();
            _logger.Info($"Renamed Device {oldName} to {_settingsService.CurrentSettings.NanoSettings.NanoLeafDevices[selectedIndex].PublicName}");
            devices_ListBox.Items.Clear();
            InitializeData();
        }

        private async void updateDevices_Button_Click(object sender, RoutedEventArgs e)
        {
            if (await _nanoService.UpdateAllNanoleafDevices())
            {
                MessageBox.Show(Properties.Resources.Code_Devices_Messagebox_UpdateDevices_Succes, Properties.Resources.General_MessageBox_Sucess_Title);
                _settingsService.SaveSettings();
            }
        }

        private async void pingDevice_Button_Click(object sender, RoutedEventArgs e)
        {
            if (devices_ListBox.SelectedItem == null)
            {
                MessageBox.Show(Properties.Resources.Code_Devices_Messagebox_SelectDevice, Properties.Resources.General_MessageBox_Error_Title, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var selectedIndex = devices_ListBox.SelectedIndex;
            await _nanoService.IdentifyController(_settingsService.CurrentSettings.NanoSettings.NanoLeafDevices[selectedIndex]);
        }
    }
}