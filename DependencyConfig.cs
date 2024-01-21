using Microsoft.Extensions.DependencyInjection;
using NanoTwitchLeafs.Interfaces;
using NanoTwitchLeafs.Objects;
using NanoTwitchLeafs.Services;
using NanoTwitchLeafs.Windows;

namespace NanoTwitchLeafs;

public static class DependencyConfig
{
    public static ServiceProvider ConfigureServices()
    {
        var serviceProvider = new ServiceCollection()
            //Windows
            .AddTransient<MainWindow>()
            .AddTransient<ResponsesWindow>()
            .AddTransient<AppInfoWindow>()
            .AddTransient<TwitchLinkWindow>()
            .AddTransient<BlacklistWindow>()
            .AddTransient<PairingWindow>()
            .AddTransient<TriggerWindow>()
            .AddTransient<DevicesInfoWindow>()
            //Services
            .AddSingleton<IAnalyticsService, AnalyticsService>()
            .AddTransient<IUpdateService, UpdateService>()
            .AddSingleton<IStreamLabsService, StreamLabsService>()
            .AddSingleton<IStreamLabsAuthService, StreamLabsAuthService>()
            .AddSingleton<IHypeRateService, HypeRateService>()
            .AddSingleton<ISettingsService, SettingsService>()
            .AddSingleton<IAppSettingsService, AppSettingsService>()
            .AddSingleton<INanoService, NanoService>()
            .AddSingleton<ITriggerService, TriggerService>()
            .AddSingleton<IDatabaseService<TriggerSetting>, DatabaseService<TriggerSetting>>()
            .AddSingleton<ITriggerRepositoryService, TriggerRepositoryService>()
            .AddTransient<ITwitchInstanceService, TwitchInstanceService>()
            .AddTransient<ITwitchEventSubService, TwitchEventSubService>()
            .AddTransient<ITwitchAuthService, TwitchAuthService>()
            .AddTransient<ITwitchPubSubService, TwitchPubSubService>()
            .AddHostedService<TwitchEventSubService>()
            // Hier füge deine Abhängigkeiten hinzu
            // .AddTransient<Interface, Implementierung>()
            .BuildServiceProvider();
            
        return serviceProvider;
    }
}