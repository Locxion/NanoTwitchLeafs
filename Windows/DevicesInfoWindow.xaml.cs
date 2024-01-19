using log4net;
using NanoTwitchLeafs.Controller;
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
        private readonly IAppSettingsService _appSettingsService;
        public  NanoController _nanoController;
        private readonly AppSettings _appSettings;
        private readonly ILog _logger = LogManager.GetLogger(typeof(DevicesInfoWindow));
        
        public DevicesInfoWindow(IAppSettingsService appSettingsService)
        {
            _appSettingsService = appSettingsService ?? throw new ArgumentNullException(nameof(appSettingsService));
            _appSettings = _appSettingsService.LoadSettings();
            Constants.SetCultureInfo(_appSettings.Language);
            InitializeComponent();
            InitializeData();
        }

        private void InitializeData()
        {
            if (_appSettings.NanoSettings.NanoLeafDevices.Count == 0)
            {
                removeDevice_Button.IsEnabled = false;
                renameDevice_Button.IsEnabled = false;
                pingDevice_Button.IsEnabled = false;    
                return;
            }
            foreach (var device in _appSettings.NanoSettings.NanoLeafDevices)
            {
                devices_ListBox.Items.Add($"{device.PublicName} - {device.Address}:{device.Port} - {device.DeviceName}");
            }
            devices_ListBox.SelectedIndex = 0;
            _logger.Info($"Showing {_appSettings.NanoSettings.NanoLeafDevices.Count} Devices in List");
        }

        private void Close_Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void RemoveDevice_Button_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = devices_ListBox.SelectedIndex;
            NanoLeafDevice nanoLeafDevice = _appSettings.NanoSettings.NanoLeafDevices[selectedIndex];
            if (MessageBox.Show(string.Format(Properties.Resources.Code_Devices_MessageBox_RemoveConfirm, nanoLeafDevice.DeviceName), Properties.Resources.Code_Devices_MessageBox_RemoveConfirm_Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
            {
                return;
            }

            _appSettings.NanoSettings.NanoLeafDevices.Remove(nanoLeafDevice);

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
            string oldName = _appSettings.NanoSettings.NanoLeafDevices[selectedIndex].PublicName;
            _appSettings.NanoSettings.NanoLeafDevices[selectedIndex].PublicName = _nanoController.GetUserInputNameForNewDevice();

            _appSettingsService.SaveSettings(_appSettings);
            _logger.Info($"Renamed Device {oldName} to {_appSettings.NanoSettings.NanoLeafDevices[selectedIndex].PublicName}");
            devices_ListBox.Items.Clear();
            InitializeData();
        }

        private async void updateDevices_Button_Click(object sender, RoutedEventArgs e)
        {
            if (await _nanoController.UpdateAllNanoleafDevices())
            {
                MessageBox.Show(Properties.Resources.Code_Devices_Messagebox_UpdateDevices_Succes, Properties.Resources.General_MessageBox_Sucess_Title);
                _appSettingsService.SaveSettings(_appSettings);
            }
        }

        private async void pingDevice_Button_Click(object sender, RoutedEventArgs e)
        {
            if (devices_ListBox.SelectedItem == null)
            {
                MessageBox.Show(Properties.Resources.Code_Devices_Messagebox_SelectDevice, Properties.Resources.General_MessageBox_Error_Title, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            int selectedIndex = devices_ListBox.SelectedIndex;
            await _nanoController.IdentifyController(_appSettings.NanoSettings.NanoLeafDevices[selectedIndex]);
        }
    }
}