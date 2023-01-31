using System.Windows;

namespace NanoTwitchLeafs.Windows
{
    /// <summary>
    /// Interaction logic for ResponsesHelpWindow.xaml
    /// </summary>
    public partial class ResponsesHelpWindow : Window
    {
        public ResponsesHelpWindow(string languageCode)
        {
            Constants.SetCultureInfo(languageCode);
            InitializeComponent();
        }
    }
}