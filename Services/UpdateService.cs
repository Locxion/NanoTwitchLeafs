using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using AutoUpdaterDotNET;
using log4net;
using NanoTwitchLeafs.Controller;
using NanoTwitchLeafs.Interfaces;
using NanoTwitchLeafs.Objects;
using NanoTwitchLeafs.Windows;
using Newtonsoft.Json;

namespace NanoTwitchLeafs.Services;

public class UpdateService : IUpdateService
{
    private readonly ILog _logger = LogManager.GetLogger(typeof(UpdateController));

    private readonly string _githubUrl = $"https://api.github.com/repos/{Constants.GITHUB_OWNER}/{Constants.GITHUB_REPO}/releases/latest";
    public UpdateService()
    {
        AutoUpdater.Synchronous = true;
        AutoUpdater.RemindLaterTimeSpan = RemindLaterFormat.Days;
        AutoUpdater.ShowRemindLaterButton = true;
        AutoUpdater.ShowSkipButton = false;
        AutoUpdater.RemindLaterAt = 1; //Days
        AutoUpdater.UpdateFormSize = new System.Drawing.Size(1000, 600);
        AutoUpdater.HttpUserAgent = "NanoTwitchLeafs AutoUpdater";
        AutoUpdater.DownloadPath = Constants.TEMP_PATH;
        AutoUpdater.RunUpdateAsAdmin = true;
        AutoUpdater.ReportErrors = true;
        AutoUpdater.InstallationPath = Assembly.GetExecutingAssembly().Location;
    }
    public async Task CheckForUpdates()
    {
        _logger.Info("Pull Release Info from Github ...");
        var githubInfo = await GetGithubReleaseVersion();
        if (githubInfo == null)
        {
            _logger.Error("Could not get GitHub Release Info.");
            return;
        }
        _logger.Info("Checking for Updates ...");
        Version newVersion = Version.Parse(githubInfo.tag_name);
        var currentVersion = typeof(AppInfoWindow).Assembly.GetName().Version;

        if (currentVersion < newVersion)
        {
            var args = new UpdateInfoEventArgs
            {
                CurrentVersion = newVersion.ToString(),
                ChangelogURL = $"https://raw.githubusercontent.com/{Constants.GITHUB_OWNER}/{Constants.GITHUB_REPO}/{githubInfo.tag_name}/changelog.txt",
                IsUpdateAvailable = true,
                InstalledVersion = currentVersion,
            };
            _logger.Info($"Update found on Github! New Version: v{args.CurrentVersion}.");

            var downloadAsset = githubInfo.assets.FirstOrDefault(x => x.name.Equals($"NanoTwitchLeafs-{args.CurrentVersion}.zip"));
            if (downloadAsset == null)
            {
                _logger.Error("Could not get GitHub Application Zip Asset.");
                return;
            }

            args.DownloadURL = downloadAsset.browser_download_url;
            AutoUpdater.ShowUpdateForm(args);
        }
        else
        {
            _logger.Info("No update available.");
        }
    }
    private async Task<GithubReleaseInfo> GetGithubReleaseVersion()
    {
        try
        {
            _logger.Debug("Getting Github Release Info");
            string contentJson;
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("request");
                var response = await httpClient.GetAsync(_githubUrl);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    _logger.Error(response.ReasonPhrase);
                    _logger.Error("Could not get Release Info from Github");
                    return null;
                }
                contentJson = await response.Content.ReadAsStringAsync();
            }

            return JsonConvert.DeserializeObject<GithubReleaseInfo>(contentJson);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            _logger.Error(ex);
            return null;
        }
    }
}