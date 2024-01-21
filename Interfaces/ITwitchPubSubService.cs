using System;
using NanoTwitchLeafs.Objects;

namespace NanoTwitchLeafs.Interfaces;

public interface ITwitchPubSubService
{ 
    event EventHandler<TwitchEvent> OnTwitchEventReceived;
    bool IsConnected();
    void Connect();
    void Disconnect();
}