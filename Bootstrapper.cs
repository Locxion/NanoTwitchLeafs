using Microsoft.Extensions.DependencyInjection;
using NanoTwitchLeafs.Windows;

namespace NanoTwitchLeafs;

public static class Bootstrapper
{
    public static async void Run()
    {
        var serviceProvider = DependencyConfig.ConfigureServices();
            
        var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
        await mainWindow.InitializeAsync();
    }
}