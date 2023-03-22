namespace NanoTwitchLeafs.Objects
{
	/// <summary>
	/// Class to Store all kinds of Credentials
	/// </summary>
	public class ServiceCredentials
	{
		public TwitchApiCredentials TwitchApiCredentials { get; set; }
		public StreamLabsApiCedentials StreamLabsApiCedentials { get; set; }
		public HyperateApi HyperateApi { get; set; }
	}

	/// <summary>
	/// Class to Store Credentials with a ClientId & Secret pair
	/// </summary>
	public class TwitchApiCredentials
	{
		public string ClientId { get; set; }
		public string ClientSecret { get; set; }

		public TwitchApiCredentials(string clientId, string clientSecret)
		{
			ClientId = clientId;
			ClientSecret = clientSecret;
		}
	}

	/// <summary>
	/// Class to Store Credentials with a ClientId & Secret pair
	/// </summary>
	public class StreamLabsApiCedentials
	{
		public string ClientId { get; set; }
		public string ClientSecret { get; set; }

		public StreamLabsApiCedentials(string clientId, string clientSecret)
		{
			ClientId = clientId;
			ClientSecret = clientSecret;
		}
	}
	
	/// <summary>
	/// Class to Store Api Key as a String
	/// </summary>
	public class HyperateApi
	{
		public string ApiKey { get; set; }

		public HyperateApi(string apiKey)
		{
			ApiKey = apiKey;
		}
	}
}