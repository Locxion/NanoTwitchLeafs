using log4net;
using NanoTwitchLeafs.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using NanoTwitchLeafs.Interfaces;
using Zeroconf;

namespace NanoTwitchLeafs.Windows
{
    /// <summary>
    /// Interaction logic for PairingWindow.xaml
    /// </summary>
    public partial class PairingWindow : Window
    {
        private readonly ISettingsService _settingsService;
        private readonly INanoService _nanoService;
        private readonly ILog _logger = LogManager.GetLogger(typeof(PairingWindow));

        private IReadOnlyList<IZeroconfHost> networkDevices;

        public PairingWindow(ISettingsService settingsService, INanoService nanoService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _nanoService = nanoService ?? throw new ArgumentNullException(nameof(nanoService));
            Constants.SetCultureInfo(settingsService.CurrentSettings.Language);
            InitializeComponent();
            FillDeviceList();
        }

        private async void FillDeviceList()
        {
            autoDetectIps_ListBox.Items.Clear();

            networkDevices = await _nanoService.GetDevicesInNetwork();

            int count = 0;

            foreach (var device in networkDevices)
            {
                string content = "";
                if (_settingsService.CurrentSettings.NanoSettings.NanoLeafDevices.Any(x => x.Address == device.IPAddress))
                {
                    content = $"{device.DisplayName} -{device.IPAddress}- {Properties.Resources.Code_Pairing_DeviceList_Paired}";
                }
                else
                {
                    content = $"{device.DisplayName} -{device.IPAddress}- {Properties.Resources.Code_Pairing_DeviceList_NotPaired}";
                    count++;
                }

                ListBoxItem item = new ListBoxItem { Content = content };
                autoDetectIps_ListBox.Items.Add(item);
            }

            if (autoDetectIps_ListBox.Items.Count == 0)
            {
                autoDetectIps_ListBox.Items.Add(Properties.Resources.Code_Pairing_MessageBox_DeviceNotFound);
            }
            else if (autoDetectIps_ListBox.Items.Count > 0)
            {
                autoDetect_Button.IsEnabled = true;
                nanoIP_TextBox.IsEnabled = true;
                ipAddress_Label.IsEnabled = true;
                autoDetectIps_ListBox.IsEnabled = true;
            }
        }

        private async void AutoDetect_Button_Click(object sender, RoutedEventArgs e)
        {
            autoDetectIps_ListBox.Items.Clear();

            MessageBox.Show(Properties.Resources.Code_Pairing_MessageBox_Pairing, Properties.Resources.Code_Pairing_MessageBox_Pairing_Title);

            networkDevices = await _nanoService.GetDevicesInNetwork();

            int count = 0;

            foreach (var device in networkDevices)
            {
                string content = "";
                if (_settingsService.CurrentSettings.NanoSettings.NanoLeafDevices.Any(x => x.Address == device.IPAddress))
                {
                    content = $"{device.DisplayName} -{device.IPAddress}- {Properties.Resources.Code_Pairing_DeviceList_Paired}";
                }
                else
                {
                    content = $"{device.DisplayName} -{device.IPAddress}- {Properties.Resources.Code_Pairing_DeviceList_NotPaired}";
                    count++;
                }

                ListBoxItem item = new ListBoxItem { Content = content };
                autoDetectIps_ListBox.Items.Add(item);
            }

            networkDevices = networkDevices.Where(x => _settingsService.CurrentSettings.NanoSettings.NanoLeafDevices.All(y => y.Address != x.IPAddress)).ToList();

            if (count == 0)
            {
                MessageBox.Show(Properties.Resources.Code_Pairing_MessageBox_DeviceNotFound, Properties.Resources.General_MessageBox_Error_Title);
                _logger.Warn("Could not find Devices in Local Network!");
                nanoIP_TextBox.IsEnabled = true;
                ipAddress_Label.IsEnabled = true;
            }
            else if (count == 1)
            {
                try
                {
                    NanoLeafDevice newNanoLeafDevice = new NanoLeafDevice
                    {
                        Address = networkDevices.FirstOrDefault().IPAddress,
                        Port = networkDevices.FirstOrDefault().Services["_nanoleafapi._tcp.local."].Port,
                        PublicName = Properties.Resources.General_Label_NanoleafDevice,
                        Token = ""
                    };
                    if (await _nanoService.PairDevice(newNanoLeafDevice))
                    {
                        MessageBox.Show(Properties.Resources.Code_Pairing_MessageBox_PairingSucessfull, Properties.Resources.General_MessageBox_Sucess_Title);
                        _logger.Info("Pair successful!");
                        NanoleafControllerInfo controllerInfo = await _nanoService.GetControllerInfo(newNanoLeafDevice);
                        newNanoLeafDevice.DeviceName = controllerInfo.name;
                        _settingsService.CurrentSettings.NanoSettings.NanoLeafDevices.Add(newNanoLeafDevice);
                        ((MainWindow)App.Current.MainWindow).nanoTestConnection_Button.IsEnabled = true;
                        DisplayHelpMessageMultipleDevices();
                        Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(Properties.Resources.Code_Pairing_MessageBox_PairingError, Properties.Resources.General_MessageBox_Error_Title);
                    nanoIP_TextBox.IsEnabled = true;
                    ipAddress_Label.IsEnabled = true;
                    _logger.Error(ex.Message, ex);
                }
            }
            else if (count > 1)
            {
                MessageBox.Show(Properties.Resources.Code_Pairing_MessageBox_MultiDeviceDetected);

                nanoIP_TextBox.IsEnabled = true;
                ipAddress_Label.IsEnabled = true;
                autoDetectIps_ListBox.IsEnabled = true;
            }
        }

        private async void ManualDetect_Button_Click(object sender, RoutedEventArgs e)
        {
            if (nanoIP_TextBox.Text == "" || nanoIP_TextBox.Text == "0.0.0.0")
            {
                MessageBox.Show(Properties.Resources.Code_Pairing_MessageBox_ValidIP);
                return;
            }

            if (!IsValidIP(nanoIP_TextBox.Text))
            {
                MessageBox.Show(Properties.Resources.Code_Pairing_MessageBox_ValidIP);
                return;
            }

            if (_settingsService.CurrentSettings.NanoSettings.NanoLeafDevices.Any(x => x.Address == nanoIP_TextBox.Text))
            {
                MessageBox.Show(Properties.Resources.Code_Pairing_MessageBox_AlreadyCon, Properties.Resources.General_MessageBox_Error_Title);
                return;
            }

            MessageBox.Show(Properties.Resources.Code_Pairing_MessageBox_Pairing, Properties.Resources.Code_Pairing_MessageBox_Pairing_Title);

            try
            {
                NanoLeafDevice newNanoLeafDevice = new NanoLeafDevice
                {
                    Address = nanoIP_TextBox.Text,
                    Port = 16021,
                    PublicName = Properties.Resources.General_Label_NanoleafDevice,
                    Token = ""
                };
                if (await _nanoService.PairDevice(newNanoLeafDevice))
                {
                    MessageBox.Show(Properties.Resources.Code_Pairing_MessageBox_PairingSucessfull, Properties.Resources.General_MessageBox_Sucess_Title);
                    _logger.Info("Pair successful!");
                    NanoleafControllerInfo controllerInfo = await _nanoService.GetControllerInfo(newNanoLeafDevice);
                    newNanoLeafDevice.DeviceName = controllerInfo.name;
                    _settingsService.CurrentSettings.NanoSettings.NanoLeafDevices.Add(newNanoLeafDevice);
                    ((MainWindow)App.Current.MainWindow).nanoTestConnection_Button.IsEnabled = true;

                    DisplayHelpMessageMultipleDevices();
                    Close();
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(Properties.Resources.Code_Pairing_MessageBox_PairingError, Properties.Resources.General_MessageBox_Error_Title);

                _logger.Error(ex.Message, ex);
            }
        }

        private bool IsValidIP(string ip)
        {
            return IPAddress.TryParse(ip, out _);
        }

        private void AutoDetectIps_ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (autoDetectIps_ListBox.Items.Count == 0)
            {
                return;
            }

            string ipPort = ((ListBoxItem)autoDetectIps_ListBox.SelectedValue).Content.ToString().Split('-')[1];
            var ipPortArray = ipPort.Split(':');

            nanoIP_TextBox.Text = ipPortArray[0];
        }

        private void DisplayHelpMessageMultipleDevices()
        {
            if (_settingsService.CurrentSettings.NanoSettings.NanoLeafDevices.Count > 1)
            {
                MessageBox.Show(Properties.Resources.Code_Pairing_MessageBox_PairingEffects, Properties.Resources.General_MessageBox_Hint_Title);
                _logger.Info(Properties.Resources.Code_Pairing_MessageBox_PairingEffects);
            }
        }
    }
}