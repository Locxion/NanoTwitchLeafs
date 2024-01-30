using NanoTwitchLeafs.Windows;
using System;
using System.Collections.Generic;

namespace NanoTwitchLeafs.Objects
{
	public class AppSettings : NotifyObject
	{
		public AppSettings() : base()
		{
			Responses = new Responses();
			BotAuthObject = new OAuthObject();
			BroadcasterAuthObject = new OAuthObject();
			CommandPrefix = "!";
			NanoSettings = new NanoSettings();
			GoveeSettings = new GoveeSettings();
			TriggerSettings = new TriggerSettings();
			InstanceID = Guid.NewGuid();
			AppVersion = typeof(AppInfoWindow).Assembly.GetName().Version;
			AnalyticsChannelName = true;
			BlacklistEnabled = false;
			Blacklist = new List<string>();
			AutoIpRefresh = false;
			Language = "en-US";
			StreamlabsInformation = new StreamlabsInformation();
			UseOwnServiceCredentials = false;
			TwitchClientId = "";
			TwitchClientSecret = "";
			StreamlabsClientId = "";
			StreamlabsClientSecret = "";
			GoveeApiKey = "";
		}
		public TriggerSettings TriggerSettings
		{
			get { return Get(() => TriggerSettings); }
			set { Set(() => TriggerSettings, value); }
		}
		public GoveeSettings GoveeSettings
		{
			get { return Get(() => GoveeSettings); }
			set { Set(() => GoveeSettings, value); }
		}
		public string GoveeApiKey
		{
			get { return Get(() => GoveeApiKey); }
			set { Set(() => GoveeApiKey, value); }
		}
		public bool AnalyticsChannelName
		{
			get { return Get(() => AnalyticsChannelName); }
			set { Set(() => AnalyticsChannelName, value); }
		}
		public bool UseOwnServiceCredentials
		{
			get { return Get(() => UseOwnServiceCredentials); }
			set { Set(() => UseOwnServiceCredentials, value); }
		}

		public string TwitchClientId
		{
			get { return Get(() => TwitchClientId); }
			set { Set(() => TwitchClientId, value); }
		}

		public string TwitchClientSecret
		{
			get { return Get(() => TwitchClientSecret); }
			set { Set(() => TwitchClientSecret, value); }
		}

		public string StreamlabsClientId
		{
			get { return Get(() => StreamlabsClientId); }
			set { Set(() => StreamlabsClientId, value); }
		}

		public string StreamlabsClientSecret
		{
			get { return Get(() => StreamlabsClientSecret); }
			set { Set(() => StreamlabsClientSecret, value); }
		}

		public StreamlabsInformation StreamlabsInformation
		{
			get { return Get(() => StreamlabsInformation); }
			set { Set(() => StreamlabsInformation, value); }
		}

		public DateTimeOffset LastValidation
		{
			get { return Get(() => LastValidation); }
			set { Set(() => LastValidation, value); }
		}

		public Uri BotAvatarUrl
		{
			get { return Get(() => BotAvatarUrl); }
			set { Set(() => BotAvatarUrl, value); }
		}

		public Uri BroadcasterAvatarUrl
		{
			get { return Get(() => BroadcasterAvatarUrl); }
			set { Set(() => BroadcasterAvatarUrl, value); }
		}

		public string BotName
		{
			get { return Get(() => BotName); }
			set { Set(() => BotName, value); }
		}

		public OAuthObject BotAuthObject
		{
			get { return Get(() => BotAuthObject); }
			set { Set(() => BotAuthObject, value); }
		}

		public string ChannelName
		{
			get { return Get(() => ChannelName); }
			set { Set(() => ChannelName, value); }
		}

		public OAuthObject BroadcasterAuthObject
		{
			get { return Get(() => BroadcasterAuthObject); }
			set { Set(() => BroadcasterAuthObject, value); }
		}

		public bool WhisperMode
		{
			get { return Get(() => WhisperMode); }
			set { Set(() => WhisperMode, value); }
		}

		public bool ChatResponse
		{
			get { return Get(() => ChatResponse); }
			set { Set(() => ChatResponse, value); }
		}

		public Responses Responses
		{
			get { return Get(() => Responses); }
			set { Set(() => Responses, value); }
		}

		public string CommandPrefix
		{
			get { return Get(() => CommandPrefix); }
			set { Set(() => CommandPrefix, value); }
		}

		public NanoSettings NanoSettings
		{
			get { return Get(() => NanoSettings); }
			set { Set(() => NanoSettings, value); }
		}

		public bool DebugEnabled
		{
			get { return Get(() => DebugEnabled); }
			set { Set(() => DebugEnabled, value); }
		}

		public bool AutoConnect
		{
			get { return Get(() => AutoConnect); }
			set { Set(() => AutoConnect, value); }
		}

		public Guid InstanceID
		{
			get { return Get(() => InstanceID); }
			set { Set(() => InstanceID, value); }
		}

		public Version AppVersion
		{
			get { return Get(() => AppVersion); }
			set { Set(() => AppVersion, value); }
		}

		public bool BlacklistEnabled
		{
			get { return Get(() => BlacklistEnabled); }
			set { Set(() => BlacklistEnabled, value); }
		}

		public List<string> Blacklist
		{
			get { return Get(() => Blacklist); }
			set { Set(() => Blacklist, value); }
		}

		public bool AutoIpRefresh
		{
			get { return Get(() => AutoIpRefresh); }
			set { Set(() => AutoIpRefresh, value); }
		}

		public string Language
		{
			get { return Get(() => Language); }
			set { Set(() => Language, value); }
		}

		public string HypeRateId
		{
			get { return Get(() => HypeRateId); }
			set { Set(() => HypeRateId, value); }
		}
	}
}