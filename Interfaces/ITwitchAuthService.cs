using System.Threading.Tasks;
using NanoTwitchLeafs.Objects;

namespace NanoTwitchLeafs.Interfaces;

public interface ITwitchAuthService
{
    Task<OAuthObject> GetAuthToken(bool isBroadcaster);
    //Task<OAuthObject> GetUserAuthToken(TwitchApiCredentials apiCredentials, bool isBroadcaster);
    Task<OAuthObject> RefreshToken(OAuthObject oAuthObject);
    Task<string> GetUserId(string userName);
    Task<string> GetAvatarUrl(string username, string token);
}