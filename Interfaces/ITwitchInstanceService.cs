using System;
using System.Threading.Tasks;
using NanoTwitchLeafs.Objects;
using ChatMessage = NanoTwitchLeafs.Objects.ChatMessage;

namespace NanoTwitchLeafs.Interfaces;

public interface ITwitchInstanceService
{
    event EventHandler<ChatMessage> OnChatMessageReceived;
    event EventHandler<TwitchEvent> OnTwitchEventReceived;

    public bool IsConnected();

    void Connect(string username, string channel, string auth);
    Task SendMessage(string message);
    Task Disconnect();

}