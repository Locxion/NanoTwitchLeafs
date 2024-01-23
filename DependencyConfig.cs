using Microsoft.Extensions.DependencyInjection;
using NanoTwitchLeafs.Interfaces;
using NanoTwitchLeafs.Objects;
using NanoTwitchLeafs.Services;
using NanoTwitchLeafs.Windows;
using TwitchLib.EventSub.Websockets.Extensions;

namespace NanoTwitchLeafs;

public static class DependencyConfig
{
    public static ServiceProvider ConfigureServices()
    {
        var serviceProvider = new ServiceCollection()
            //Windows
            .AddTransient<AppInfoWindow>()
            .AddTransient<BlacklistWindow>()
            .AddTransient<DevicesInfoWindow>()
            .AddTransient<MainWindow>()
            .AddTransient<PairingWindow>()
            .AddTransient<ResponsesWindow>()
            .AddTransient<TriggerDetailWindow>()
            .AddTransient<TriggerWindow>()
            .AddTransient<TwitchLinkWindow>()
            //Services
            .AddLogging()
            .AddSingleton<IAnalyticsService, AnalyticsService>()
            .AddSingleton<IAppSettingsService, AppSettingsService>()
            .AddSingleton<IDatabaseService<TriggerSetting>, DatabaseService<TriggerSetting>>()
            .AddSingleton<IHypeRateService, HypeRateService>()
            .AddSingleton<INanoService, NanoService>()
            .AddSingleton<ISettingsService, SettingsService>()
            .AddSingleton<IStreamingPlatformService, StreamingPlatformService>()
            .AddSingleton<IStreamLabsAuthService, StreamLabsAuthService>()
            .AddSingleton<IStreamLabsService, StreamLabsService>()
            .AddSingleton<ITriggerRepositoryService, TriggerRepositoryService>()
            .AddSingleton<ITriggerService, TriggerService>()
            .AddTransient<ITwitchAuthService, TwitchAuthService>()
            .AddTransient<ITwitchEventSubService, TwitchEventSubService>()
            .AddTransient<ITwitchInstanceService, TwitchInstanceService>()
            .AddTransient<ITwitchPubSubService, TwitchPubSubService>()
            .AddTransient<IUpdateService, UpdateService>()
            //.AddHostedService<TwitchEventSubService>()
            .AddTwitchLibEventSubWebsockets()
            // Hier füge deine Abhängigkeiten hinzu
            // .AddTransient<Interface, Implementierung>()
            .BuildServiceProvider();
        return serviceProvider;
    }
}