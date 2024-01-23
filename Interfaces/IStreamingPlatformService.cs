using System;
using System.Threading.Tasks;
using TwitchLib.EventSub.Core.Models.Chat;
using ChatMessage = NanoTwitchLeafs.Objects.ChatMessage;

namespace NanoTwitchLeafs.Interfaces;

public interface IStreamingPlatformService
{
    event EventHandler<ChatMessage> OnMessageReceived;
    Task Connect();
    Task Disconnect();
    bool IsConnected();
    Task SendMessage(string message);
}