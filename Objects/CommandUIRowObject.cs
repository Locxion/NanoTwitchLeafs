using System.Windows.Controls;

namespace NanoTwitchLeafs.Objects
{
    public class CommandUIRowObject
    {
        public int Index { get; set; }
        public ComboBox TriggerComboBox { get; set; }
        public TextBox CommandTextBox { get; set; }
        public ComboBox EffectComboBox { get; set; }
        public TextBox SoundFileTextBox { get; set; }
        public Button SoundFileButton { get; set; }
        public TextBox VolumeTextBox { get; set; }
        public TextBox PointsTextBox { get; set; }
        public TextBox DurationTextBox { get; set; }
        public TextBox BrightnessTextBox { get; set; }
        public CheckBox VipOnlyCheckBox { get; set; }
        public CheckBox SubOnlyCheckBox { get; set; }
        public CheckBox ModOnlyCheckBox { get; set; }
        public Button DeletRowButton { get; set; }

        public CommandUIRowObject(int index, TextBox commandTextBox, ComboBox typeComboBox, ComboBox effectComboBox, TextBox soundFileTextBox, Button soundFileButton, TextBox volumeTextBox, TextBox pointsTextbox,
                                  TextBox durationTextbox, TextBox brightnessTextBox, CheckBox vipOnlyCheckBox, CheckBox subOnlyCheckBox, CheckBox modOnlyCheckBox, Button deleteButton)
        {
            Index = index;
            CommandTextBox = commandTextBox;
            TriggerComboBox = typeComboBox;
            EffectComboBox = effectComboBox;
            SoundFileTextBox = soundFileTextBox;
            SoundFileButton = soundFileButton;
            VolumeTextBox = volumeTextBox;
            PointsTextBox = pointsTextbox;
            DurationTextBox = durationTextbox;
            BrightnessTextBox = brightnessTextBox;
            VipOnlyCheckBox = vipOnlyCheckBox;
            SubOnlyCheckBox = subOnlyCheckBox;
            ModOnlyCheckBox = modOnlyCheckBox;
            DeletRowButton = deleteButton;
        }
    }
}