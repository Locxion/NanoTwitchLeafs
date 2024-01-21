using System.Threading.Tasks;

namespace NanoTwitchLeafs.Interfaces;

public interface IUpdateService
{
    Task CheckForUpdates();
}