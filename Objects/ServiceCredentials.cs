namespace NanoTwitchLeafs.Objects
{
	/// <summary>
	/// Class to Store all kinds of Credentials with a ClientId & Secret pair
	/// </summary>
	public class ServiceCredentials
	{
		public TwitchApiCredentials TwitchApiCredentials { get; set; }
		public StreamLabsApiCedentials StreamLabsApiCedentials { get; set; }
	}

	/// <summary>
	/// Class to Store Credentials with a ClientId & Secret pair
	/// </summary>
	public class TwitchApiCredentials
	{
		public string ClientId { get; set; }
		public string ClientSecret { get; set; }
	}

	/// <summary>
	/// Class to Store Credentials with a ClientId & Secret pair
	/// </summary>
	public class StreamLabsApiCedentials
	{
		public string ClientId { get; set; }
		public string ClientSecret { get; set; }
	}
}