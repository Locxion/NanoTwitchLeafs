using System.Drawing;

namespace NanoTwitchLeafs.Objects
{
    public class QueueObject
    {
        public Trigger Trigger { get; set; }
        public string Username { get; set; }
        public bool IsKeyword { get; set; }
        public Color Color { get; set; }

        public QueueObject(Trigger trigger, string username, bool isKeyword = false, Color color = new Color())
        {
            if (string.IsNullOrEmpty(username))
            {
                throw new System.ArgumentException($"'{nameof(username)}' cannot be null or empty", nameof(username));
            }

            Trigger = trigger ?? throw new System.ArgumentNullException(nameof(trigger));
            Username = username;
            IsKeyword = isKeyword;
            Color = color;
        }
    }
}