using NanoTwitchLeafs.Enums;

namespace NanoTwitchLeafs.Interfaces;

public interface IAnalyticsService
{
    void StartKeepAlive();
    void SendPing(PingType type, string message);

}