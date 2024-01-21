using System;
using System.Runtime.InteropServices;

namespace NanoTwitchLeafs.Interfaces;

public interface IHypeRateService
{
    event EventHandler<int> OnHeartRateReceived;
    void Connect();
    void Disconnect();
}