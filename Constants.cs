using NanoTwitchLeafs.Objects;
using System;
using System.Globalization;
using System.IO;
using System.Threading;

namespace NanoTwitchLeafs
{
	public static class Constants
	{
		#region Github

		public static readonly string GITHUB_OWNER = "Locxion";
		public static readonly string GITHUB_REPO = "NanoTwitchLeafs";

		#endregion

		#region Paths

		public static readonly string TEMP_PATH = Path.GetTempPath();
		private static readonly string APPDATA_PATH = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData); // AppData folder
		public static readonly string PROGRAMFILESFOLDER_PATH = Path.Combine(APPDATA_PATH, "NanoTwitchLeafs");     // Path for program config folder
		private static readonly string LOGFOLDER_PATH = Path.Combine(PROGRAMFILESFOLDER_PATH, "logs"); // Path for old Logfiles

		private static readonly string SETTINGS_FILE = "settings.txt";
		private static readonly string DEBUG_SETTINGS_FILE = "debug_settings.txt";

		public static readonly string SETTINGS_PATH = Path.Combine(PROGRAMFILESFOLDER_PATH, SETTINGS_FILE);
		public static readonly string DEBUG_SETTINGS_PATH = Path.Combine(PROGRAMFILESFOLDER_PATH, DEBUG_SETTINGS_FILE);

		public static readonly string DATABASE_FILE = "nanotwitchleafs.sqlite";
		public static readonly string DATABASE_PATH = Path.Combine(PROGRAMFILESFOLDER_PATH, DATABASE_FILE);

		public static readonly string LOG_PATH = Path.Combine(LOGFOLDER_PATH, "nanotwichleafs.log");

		#endregion

		#region ServiceCredentials

		public static readonly string SERVICE_CREDENTIALS_PATH = AppDomain.CurrentDomain.BaseDirectory + "/ServiceCredentials";
		public static ServiceCredentials ServiceCredentials;

		#endregion

		public static void SetCultureInfo(string languageCode)
		{
			CultureInfo cultureInfo = CultureInfo.GetCultureInfo(languageCode);
			Thread.CurrentThread.CurrentCulture = cultureInfo;
			Thread.CurrentThread.CurrentUICulture = cultureInfo;
			CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
			CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
		}
	}
}