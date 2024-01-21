using System.Threading.Tasks;
using NanoTwitchLeafs.Objects;

namespace NanoTwitchLeafs.Interfaces;

public interface IStreamLabsAuthService
{
	Task<bool> LinkAccount();
	Task<string> GetAccessToken(StreamLabsApiCedentials apiCredentials);
	Task<string> GetProfileInformation();
}