using NanoTwitchLeafs.Objects;

namespace NanoTwitchLeafs.Interfaces;

public interface ITriggerService
{
    void AddToQueue(QueueObject obj);
    void HandleMessage(ChatMessage chatMessage);
    void ResetEventQueue();
    void RestartEventQueue();
}