using NanoTwitchLeafs.Controller;
using System;
using System.Windows;
using NanoTwitchLeafs.Interfaces;

namespace NanoTwitchLeafs.Windows
{
    /// <summary>
    /// Interaction logic for InputDialogWindow.xaml
    /// </summary>
    public partial class InputDialogWindow : Window
    {
        private readonly NanoService _nanoService;

        public InputDialogWindow(NanoService nanoService, string languageCode)
        {
            Constants.SetCultureInfo(languageCode);
            InitializeComponent();
            _nanoService = nanoService ?? throw new ArgumentNullException(nameof(nanoService));
        }

        private void InputDialog_Button_Click(object sender, RoutedEventArgs e)
        {
            _nanoService.TempName = inputDialog_TextBox.Text;

            Close();
        }
    }
}