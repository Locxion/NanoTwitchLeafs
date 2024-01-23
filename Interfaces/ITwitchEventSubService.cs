using System;
using System.Threading.Tasks;
using NanoTwitchLeafs.Objects;

namespace NanoTwitchLeafs.Interfaces;

public interface ITwitchEventSubService
{
    event EventHandler<TwitchEvent> OnTwitchEvent;
    bool IsConnected();
    Task Connect();
    Task Disconnect();
}