using NanoTwitchLeafs.Objects;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;

namespace NanoTwitchLeafs
{
    /// <summary>
    /// Static constants for application configuration, paths, and service credentials.
    /// </summary>
    public static class Constants
    {
        #region Github

        /// <summary>GitHub repository owner for update checks.</summary>
        public static readonly string GITHUB_OWNER = "Locxion";
        /// <summary>GitHub repository name for update checks.</summary>
        public static readonly string GITHUB_REPO = "NanoTwitchLeafs";

        #endregion

        #region Paths

        /// <summary>System temporary folder path.</summary>
        public static readonly string TEMP_PATH = Path.GetTempPath();
        private static readonly string APPDATA_PATH = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        /// <summary>Application data folder (%APPDATA%/NanoTwitchLeafs).</summary>
        public static readonly string PROGRAMFILESFOLDER_PATH = Path.Combine(APPDATA_PATH, "NanoTwitchLeafs");
        private static readonly string LOGFOLDER_PATH = Path.Combine(PROGRAMFILESFOLDER_PATH, "logs");

        private static readonly string SETTINGS_FILE = "settings.txt";
        private static readonly string DEBUG_SETTINGS_FILE = "debug_settings.txt";

        /// <summary>Path to user settings file (settings.txt).</summary>
        public static readonly string SETTINGS_PATH = Path.Combine(PROGRAMFILESFOLDER_PATH, SETTINGS_FILE);
        /// <summary>Path to debug settings file (debug_settings.txt).</summary>
        public static readonly string DEBUG_SETTINGS_PATH = Path.Combine(PROGRAMFILESFOLDER_PATH, DEBUG_SETTINGS_FILE);

        /// <summary>SQLite database filename.</summary>
        public static readonly string DATABASE_FILE = "nanotwitchleafs.sqlite";
        /// <summary>Full path to SQLite database file.</summary>
        public static readonly string DATABASE_PATH = Path.Combine(PROGRAMFILESFOLDER_PATH, DATABASE_FILE);

        /// <summary>Full path to log file (nanotwichleafs.log).</summary>
        public static readonly string LOG_PATH = Path.Combine(LOGFOLDER_PATH, "nanotwichleafs.log");
        
        /// <summary>Usernames with developer privileges (bypass certain restrictions).</summary>
        public static readonly List<string> DEVELOPER = new List<string> { "locxion", "silverdark", "revyn112" };
        #endregion

        #region ServiceCredentials

        /// <summary>Directory containing service API credentials (ClientId/Secret).</summary>
        public static readonly string SERVICE_CREDENTIALS_PATH = AppDomain.CurrentDomain.BaseDirectory + "/ServiceCredentials";
        /// <summary>Loaded service credentials (Twitch, Streamlabs, etc.).</summary>
        public static ServiceCredentials ServiceCredentials;

        #endregion

        #region Twitch

        /// <summary>Maximum allowed message length for Twitch chat (500 chars).</summary>
        public static int TwitchMessageMaxLength = 500;

        #endregion

        /// <summary>
        /// Configures application localization and UI language.
        /// </summary>
        /// <param name="languageCode">ISO culture code (e.g., "en-US", "de-DE").</param>
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