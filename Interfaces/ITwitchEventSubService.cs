using System;
using System.Threading.Tasks;
using NanoTwitchLeafs.Objects;

namespace NanoTwitchLeafs.Interfaces;

public interface ITwitchEventSubService
{
    event EventHandler<TwitchEvent> OnTwitchEvent;
    Task Connect();
    Task Disconnect();
}