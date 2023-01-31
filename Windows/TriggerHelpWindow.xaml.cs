using System.Windows;

namespace NanoTwitchLeafs.Windows
{
    /// <summary>
    /// Interaction logic for TriggerHelpWindow.xaml
    /// </summary>
    public partial class TriggerHelpWindow : Window
    {
        public TriggerHelpWindow(string languageCode)
        {
            Constants.SetCultureInfo(languageCode);
            InitializeComponent();
        }
    }
}