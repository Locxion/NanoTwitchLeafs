using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using log4net;
using NanoTwitchLeafs.Enums;
using NanoTwitchLeafs.Objects;
using NanoTwitchLeafs.Windows;
using Newtonsoft.Json;
using TwitchLib.Api;

namespace NanoTwitchLeafs.Controller
{
    public class AnalyticsController
    {
        private readonly ILog _logger = LogManager.GetLogger(typeof(AppSettingsController));
        private readonly AppSettings _appSettings;
        private readonly Version _appVersion;
        private readonly string _appName;
        private string _appBranch;
        private int failedPingCount = 0;
        
#if DEBUG
        private static readonly string _analyticsServerUrl = "https://localhost:7244/api";
#elif RELEASE
        private static readonly string _analyticsServerUrl = "https://analytics.nanotwitchleafs.de/api";
#elif BETA
        private static readonly string _analyticsServerUrl = "https://analytics.nanotwitchleafs.de/api";
#endif
        public AnalyticsController(AppSettings appSettings)
        {
            _appVersion = typeof(MainWindow).Assembly.GetName().Version;
            _appName = typeof(MainWindow).Assembly.GetName().FullName;
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));

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

        public async void KeepAlive()
        {
            while (true)
            {
                await Task.Delay(5 * 60 *  1000);
                //await Task.Delay(15 * 1000);

                var ping =  await BuildMessage(PingType.Ping, "Ping!");

                await SendPing(ping);
            }
        }

        public async void SendPing(PingType pingType, string message = "")
        {
            await SendPing( await BuildMessage(pingType, message));
        }

        private async Task<AnalyticsPing> BuildMessage(PingType pingType, string message)
        {
            var api = new TwitchAPI();
            api.Settings.ClientId = HelperClass.GetTwitchApiCredentials(_appSettings).ClientId;
            api.Settings.AccessToken = _appSettings.BotAuthObject.Access_Token;
            var analyticsMessage = new AnalyticsPing()
            {
                InstanceId = _appSettings.InstanceID,
                PingType = pingType,
                Channel = "Anonymous",
                Timestamp = DateTime.UtcNow,
                Message = message,
                AppInformation = new AppInformation()
                {
                    AppVersion = typeof(AppInfoWindow).Assembly.GetName().Version,
                    Debug = _appSettings.DebugEnabled,
                    DevicesCount = _appSettings.NanoSettings.NanoLeafDevices.Count
                },
                SubscriberCount = await HelperClass.GetChannelSubscriberCount(api, _appSettings)
            };
            if (_appSettings.AnalyticsChannelName && !string.IsNullOrWhiteSpace(_appSettings.ChannelName))
            {
                analyticsMessage.Channel = _appSettings.ChannelName.ToLower();
            }

            return analyticsMessage;
        }

        private async Task SendPing(AnalyticsPing ping)
        {
            if (failedPingCount > 9)
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
                    BaseAddress = new Uri(_analyticsServerUrl),
                    Timeout = TimeSpan.FromSeconds(5)
                };

                /*
                var content = new StringContent(JsonConvert.SerializeObject(ping), Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync("/ping", content);
                */
                
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_analyticsServerUrl}/ping");
                request.Content = JsonContent.Create(ping);
                var response = await httpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.Error("Server rejected Analytics Message.");
                    _logger.Error($"StatusCode: {response.StatusCode}");
                    return;
                }

                failedPingCount = 0;
                _logger.Debug("Analytics Message successfully send to Analytics Server.");
            }
            catch (Exception e)
            {
                failedPingCount++;
                _logger.Warn($"Ping failed. This was the {failedPingCount}. time.");
                _logger.Error("Could not send Analytics Ping to Server! Server may be Offline?");
                _logger.Error(e.Message);
                _logger.Error(e);
            }
        }
        
    }
}