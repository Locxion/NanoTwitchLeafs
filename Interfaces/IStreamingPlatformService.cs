using System.Threading.Tasks;

namespace NanoTwitchLeafs.Interfaces;

public interface IStreamingPlatformService
{
    Task Connect();
    Task Disconnect();
    bool IsConnected();
    Task SendMessage(string message);
}