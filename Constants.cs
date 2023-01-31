using System;
using System.Globalization;
using System.IO;
using System.Threading;

namespace NanoTwitchLeafs
{
    public static class Constants
    {
        public static readonly string TEMP_PATH = Path.GetTempPath();
        public static string DESKTOP_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "NanoTwitchLeafs-Beta"); // Desktop folder
        private static readonly string APPDATA_PATH = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData); // AppData folder
        public static readonly string PROGRAMFILESFOLDER_PATH = Path.Combine(APPDATA_PATH, "NanoTwitchLeafs");     // Path for program config folder
        private static readonly string LOGFOLDER_PATH = Path.Combine(PROGRAMFILESFOLDER_PATH, "logs"); // Path for old Logfiles

        private static readonly string SETTINGS_FILE = "settings.txt";
        public static string SETTINGS_PATH = Path.Combine(PROGRAMFILESFOLDER_PATH, SETTINGS_FILE);
        public static readonly string DEBUG_SETTINGS_PATH = SETTINGS_FILE;

        public static string DATABASE_FILE = "nanotwitchleafs.sqlite";
        public static readonly string DATABASE_PATH = Path.Combine(PROGRAMFILESFOLDER_PATH, DATABASE_FILE);

        public static readonly string LOG_PATH = Path.Combine(LOGFOLDER_PATH, "nanotwichleafs.log");

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