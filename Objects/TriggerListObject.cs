using System.Windows.Media;

namespace NanoTwitchLeafs.Objects
{
    public class TriggerListObject
    {
        public int OnOffSliderValue { get; set; }
        public Brush OnOffSliderBackground { get; set; }
        public string ID { get; set; }
        public string Trigger { get; set; }
        public string Command { get; set; }
        public string Effect { get; set; }
        public Brush Background { get; set; }
        public string Sound { get; set; }
        public string Duration { get; set; }
        public string Brightness { get; set; }
        public string Amount { get; set; }
        public string Cooldown { get; set; }
        public string VipSubMod { get; set; }
    }
}