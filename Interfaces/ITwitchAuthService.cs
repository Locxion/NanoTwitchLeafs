using System.Threading.Tasks;
using NanoTwitchLeafs.Objects;

namespace NanoTwitchLeafs.Interfaces;

public interface ITwitchAuthService
{
    Task<OAuthObject> GetAuthToken(TwitchApiCredentials apiCredentials, bool isBroadcaster);
    Task<string> GetAvatarUrl(string username, string token);
}