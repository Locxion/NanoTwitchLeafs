using NanoTwitchLeafs.Objects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using log4net;
using TwitchLib.Api;
using TwitchLib.Api.Core.Exceptions;

namespace NanoTwitchLeafs
{
	/// <summary>
	/// Helper Methods
	/// </summary>
	public static class HelperClass
	{
		private static readonly ILog _logger = LogManager.GetLogger(typeof(HelperClass));

		/// <summary>
		/// Returns URI-safe data with a given input length.
		/// </summary>
		/// <param name="length">Input length (nb. output will be longer)</param>
		/// <returns></returns>
		public static string RandomDataBase64Url(uint length)
		{
			RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
			byte[] bytes = new byte[length];
			rng.GetBytes(bytes);
			return Base64UrlEncodeNoPadding(bytes);
		}

		//    /// <summary>
		//    /// Base64url no-padding encodes the given input buffer.
		//    /// </summary>
		//    /// <param name="buffer"></param>
		//    /// <returns></returns>
		public static string Base64UrlEncodeNoPadding(byte[] buffer)
		{
			string base64 = Convert.ToBase64String(buffer);

			//Converts base64 to base64url.
			base64 = base64.Replace("+", "-");
			base64 = base64.Replace("/", "_");
			//Strips padding.
			base64 = base64.Replace("=", "");

			return base64;
		}

		/// <summary>
		/// Formating string into JsonString
		/// </summary>
		/// <param name="json"></param>
		/// <returns></returns>
		public static string FormatJson(string json)
		{
			dynamic parsedJson = JsonConvert.DeserializeObject(json);
			return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
		}

		/// <summary>
		/// Returns TwitchApiCredentials depending on AppSetting UseOwnServiceCredentials
		/// </summary>
		/// <param name="appSettings"></param>
		/// <returns></returns>
		public static TwitchApiCredentials GetTwitchApiCredentials(AppSettings appSettings)
		{
			if (appSettings.UseOwnServiceCredentials)
				return new TwitchApiCredentials(appSettings.TwitchClientId, appSettings.TwitchClientSecret);

			return Constants.ServiceCredentials.TwitchApiCredentials;
		}

		/// <summary>
		/// Returns StreamLabsApiCedentials depending on AppSetting UseOwnServiceCredentials
		/// </summary>
		/// <param name="appSettings"></param>
		/// <returns></returns>
		public static StreamLabsApiCedentials GetStreamLabsApiCredentials(AppSettings appSettings)
		{
			if (appSettings.UseOwnServiceCredentials)
				return new StreamLabsApiCedentials(appSettings.TwitchClientId, appSettings.TwitchClientSecret);

			return Constants.ServiceCredentials.StreamLabsApiCedentials;
		}

		/// <summary>
		/// Splits a String into Parts with a Max Char length
		/// </summary>
		/// <param name="input"></param>
		/// <param name="maxLength"></param>
		/// <param name="seperator"></param>
		/// <returns></returns>
		public static List<string> SplitString(string input, int maxLength, char seperator = ' ')
		{
			List<string> result = new List<string>();
			int startIndex = 0;

			while (input.Length > maxLength)
			{
				int spaceIndex = input.LastIndexOf(seperator, maxLength);

				if (spaceIndex == -1)
				{
					spaceIndex = maxLength;
				}

				string part = input.Substring(startIndex, spaceIndex - startIndex).Trim();
				result.Add(part);

				input = input.Substring(spaceIndex).Trim();
			}

			if (!string.IsNullOrWhiteSpace(input))
			{
				result.Add(input);
			}

			return result;
		}
		/// <summary>
		/// Gets UserId for Username from Twitch API with provided Api, Settings, and Username
		/// </summary>
		/// <param name="api"></param>
		/// <param name="appSettings"></param>
		/// <param name="userName"></param>
		/// <returns></returns>
		public static async Task<string> GetUserId(TwitchAPI api, AppSettings appSettings, string userName)
		{
			try
			{
				var user = await api.Helix.Users.GetUsersAsync(null, new List<string> { userName.ToLower() },
					appSettings.BroadcasterAuthObject.Access_Token);
				return user.Users[0].Id;
			}
			catch (BadScopeException e)
			{

				_logger.Error("Could not get UserId. Bad Scopes for Access Token", e);
				return null;
			}
			catch (Exception e)
			{
				_logger.Error("Could not get UserId from Api", e);
				return null;
			}
		}
	}
}