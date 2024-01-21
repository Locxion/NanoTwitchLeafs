using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using log4net;
using NanoTwitchLeafs.Enums;
using NanoTwitchLeafs.Interfaces;
using NanoTwitchLeafs.Objects;
using NanoTwitchLeafs.Windows;

namespace NanoTwitchLeafs.Services;

class AnalyticsService : IAnalyticsService
{
    private readonly ISettingsService _settingsService;
    private readonly ILog _logger = LogManager.GetLogger(typeof(AnalyticsService));
    private readonly Version _appVersion;
    private readonly string _appName;
    private string _appBranch;
    private int _failedPingCount = 0;
    
#if DEBUG
    private const string AnalyticsServerUrl = "https://localhost:7244/api";
#elif RELEASE
    private const string AnalyticsServerUrl = "https://analytics.nanotwitchleafs.de/api";
#endif
    
    public AnalyticsService(ISettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _appVersion = typeof(App).Assembly.GetName().Version;
        _appName = typeof(App).Assembly.GetName().FullName;
#if BETA
            _appBranch = "Beta";
#endif
#if RELEASE
           _appBranch = "Release";
#endif
#if DEBUG
        _appBranch = "Debug";
#endif
    }

    public AnalyticsService(string appBranch)
    {
        _appBranch = appBranch;
    }

    public void StartKeepAlive()
    {
        KeepAlive();
    }

    private async void KeepAlive()
    {
        while (true)
        {
            await Task.Delay(5 * 60 *  1000);
            //await Task.Delay(15 * 1000);

            var ping = BuildMessage(PingType.Ping, "Ping!");

            await SendPing(ping);
        }
    }

    public async void SendPing(PingType pingType, string message = "")
    {
        await SendPing(BuildMessage(pingType, message));
    }

    private AnalyticsPing BuildMessage(PingType pingType, string message)
    {
        var analyticsMessage = new AnalyticsPing()
        {
            InstanceId = _settingsService.CurrentSettings.InstanceID,
            PingType = pingType,
            Channel = "Anonymous",
            Timestamp = DateTime.UtcNow,
            Message = message,
            AppInformation = new AppInformation()
            {
                AppVersion = typeof(AppInfoWindow).Assembly.GetName().Version,
                Debug = _settingsService.CurrentSettings.DebugEnabled,
                DevicesCount = _settingsService.CurrentSettings.NanoSettings.NanoLeafDevices.Count
            }
        };
        if (_settingsService.CurrentSettings.AnalyticsChannelName && !string.IsNullOrWhiteSpace(_settingsService.CurrentSettings.ChannelName))
        {
            analyticsMessage.Channel = _settingsService.CurrentSettings.ChannelName.ToLower();
        }

        return analyticsMessage;
    }

    private async Task SendPing(AnalyticsPing ping)
    {
        if (_failedPingCount > 9)
        {
            _logger.Warn("Skipping Analytics Ping ... to many failed Pings for this Session");
            return;
        }
        try
        {
            if (ping == null)
                return;

            using var httpClient = new HttpClient
            {
                BaseAddress = new Uri(AnalyticsServerUrl),
                Timeout = TimeSpan.FromSeconds(5)
            };

            /*
            var content = new StringContent(JsonConvert.SerializeObject(ping), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("/ping", content);
            */
            
            var request = new HttpRequestMessage(HttpMethod.Post, $"{AnalyticsServerUrl}/ping");
            request.Content = JsonContent.Create(ping);
            var response = await httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.Error("Server rejected Analytics Message.");
                _logger.Error($"StatusCode: {response.StatusCode}");
                return;
            }

            _failedPingCount = 0;
            _logger.Debug("Analytics Message successfully send to Analytics Server.");
        }
        catch (Exception e)
        {
            _failedPingCount++;
            _logger.Warn($"Ping failed. This was the {_failedPingCount}. time.");
            _logger.Error("Could not send Analytics Ping to Server! Server may be Offline?");
            _logger.Error(e.Message);
            _logger.Error(e);
        }
    }
}