using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shanquan.Inventory.Desktop.ViewModels;
using Shanquan.Inventory.Desktop.Views;
using Shanquan.Inventory.Desktop.Services;
using Shanquan.Inventory.Infrastructure;

namespace Shanquan.Inventory.Desktop;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        ConfigureServices(builder);

        using var host = builder.Build();

        host.Start();
        App.Services = host.Services;

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            host.StopAsync().GetAwaiter().GetResult();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();

    private static void ConfigureServices(HostApplicationBuilder builder)
    {
        builder.Services.AddInventoryInfrastructure();
        builder.Services.AddSingleton<IDialogService, DialogService>();
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainWindow>();
    }
}
