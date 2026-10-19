using Avalonia;
using Avalonia.Media;
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
        using var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(ConfigureServices)
            .Build();

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
    {
        var builder = AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();

        if (OperatingSystem.IsMacOS())
        {
            // Use Chinese font metrics on macOS to avoid clipping fallback glyphs.
            // Keep the Windows 7 font selection and layout unchanged.
            builder.With(new FontManagerOptions { DefaultFamilyName = "PingFang SC" });
        }

        return builder;
    }

    private static void ConfigureServices(HostBuilderContext _, IServiceCollection services)
    {
        services.AddInventoryInfrastructure();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();
    }
}
