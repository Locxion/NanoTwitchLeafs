using NanoTwitchLeafs.Controller;
using System;
using System.Windows;

namespace NanoTwitchLeafs.Windows
{
    /// <summary>
    /// Interaction logic for InputDialogWindow.xaml
    /// </summary>
    public partial class InputDialogWindow : Window
    {
        private readonly NanoController _nanoController;

        public InputDialogWindow(NanoController nanoController, string languageCode)
        {
            Constants.SetCultureInfo(languageCode);
            InitializeComponent();
            _nanoController = nanoController ?? throw new ArgumentNullException(nameof(nanoController));
        }

        private void InputDialog_Button_Click(object sender, RoutedEventArgs e)
        {
            _nanoController.TempName = inputDialog_TextBox.Text;

            Close();
        }
    }
}