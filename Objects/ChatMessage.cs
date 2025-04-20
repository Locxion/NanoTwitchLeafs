using System.Drawing;

namespace NanoTwitchLeafs.Objects
{
    public class ChatMessage
    {
        public string Username { get; set; }
        public bool IsSubscriber { get; set; }
        public bool IsModerator { get; set; }
        public bool IsVip { get; set; }
        public string Message { get; set; }
        public Color Color { get; }

        public ChatMessage(string username, bool isSubscriber, bool isModerator, bool isVip, string message, Color color)
        {
            Username = username;
            IsSubscriber = isSubscriber;
            IsModerator = isModerator;
            IsVip = isVip;
            Message = message;
            Color = color;
        }
    }
}