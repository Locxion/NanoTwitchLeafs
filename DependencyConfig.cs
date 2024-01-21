using Microsoft.Extensions.DependencyInjection;
using NanoTwitchLeafs.Interfaces;
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
            .AddSingleton<ISettingsService, SettingsService>()
            .AddSingleton<IAppSettingsService, AppSettingsService>()
            .AddHostedService<TwitchEventSubService>()
            // Hier füge deine Abhängigkeiten hinzu
            // .AddTransient<Interface, Implementierung>()
            .BuildServiceProvider();
            
        return serviceProvider;
    }
}