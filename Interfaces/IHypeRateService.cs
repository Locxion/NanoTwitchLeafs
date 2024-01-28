using System;
using System.Runtime.InteropServices;

namespace NanoTwitchLeafs.Interfaces;

public interface IHypeRateService
{
    event EventHandler<int> OnHeartRateReceived;
    event EventHandler OnConnect;
    event EventHandler OnDisconnect;
    void Connect();
    void Disconnect();
    bool IsConnected();
}