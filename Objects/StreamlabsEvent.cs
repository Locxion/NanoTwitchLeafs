using NanoTwitchLeafs.Enums;

namespace NanoTwitchLeafs.Objects;

public class StreamlabsEvent
{
    public string Username { get; }
    public Event Event { get; }
    public bool IsAnonymous { get; }
    public int Amount { get; }
    public string Message { get; }

    public StreamlabsEvent(string username, Event @event, bool isAnonymous = false , int amount = 1, string message = "")
    {
        Username = username;
        Event = @event;
        IsAnonymous = isAnonymous;
        Amount = amount;
        Message = message;
    }

    public override string ToString()
    {
        return $"StreamlabsEvent [User: {Username}, Event:{Event}, Anonymous: {IsAnonymous}, Amount: {Amount}, Message: {Message}]";
    } 
}