using NanoTwitchLeafs.Enums;

namespace NanoTwitchLeafs.Objects;

public class TwitchEvent
{
    public string Username { get; }
    public Event Event { get; }
    public bool IsAnonymous { get; }
    public int Amount { get; }
    public string Message { get; }

    public TwitchEvent(string username, Event @event, bool isAnonymous = false , int amount = 1, string message = "")
    {
        Username = username;
        Event = @event;
        IsAnonymous = isAnonymous;
        Amount = amount;
        Message = message;
    }

    public override string ToString()
    {
        return $"TwitchEvent [User: {Username}, Event:{Event}, Anonymous: {IsAnonymous}, Amount: {Amount}, Message: {Message}]";
    }
}