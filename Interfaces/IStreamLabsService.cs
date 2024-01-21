using System;
using System.Threading.Tasks;
using NanoTwitchLeafs.Objects;

namespace NanoTwitchLeafs.Interfaces;

public interface IStreamLabsService
{
    event EventHandler<StreamlabsEvent> OnDonationReceived;

    Task Connect();
    bool IsConnected();
    Task Disconnect();
}