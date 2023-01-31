using System.Drawing;

namespace NanoTwitchLeafs.Objects
{
    public class QueueObject
    {
        public TriggerSetting TriggerSetting { get; set; }
        public string Username { get; set; }
        public bool IsKeyword { get; set; }
        public Color Color { get; set; }

        public QueueObject(TriggerSetting triggerSetting, string username, bool isKeyword = false, Color color = new Color())
        {
            if (string.IsNullOrEmpty(username))
            {
                throw new System.ArgumentException($"'{nameof(username)}' cannot be null or empty", nameof(username));
            }

            TriggerSetting = triggerSetting ?? throw new System.ArgumentNullException(nameof(triggerSetting));
            Username = username;
            IsKeyword = isKeyword;
            Color = color;
        }
    }
}