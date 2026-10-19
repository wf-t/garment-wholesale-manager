using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shanquan.Inventory.Application.Abstractions;
using Shanquan.Inventory.Application.CoreBusiness;
using Shanquan.Inventory.Infrastructure.CoreBusiness;
using Shanquan.Inventory.Infrastructure.Persistence;

namespace Shanquan.Inventory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInventoryInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<AppPaths>();
        services.AddSingleton<IAppPaths>(provider => provider.GetRequiredService<AppPaths>());
        services.AddDbContextFactory<InventoryDbContext>((provider, options) =>
        {
            var paths = provider.GetRequiredService<AppPaths>();
            options.UseSqlite($"Data Source={paths.DatabasePath};Cache=Shared;Default Timeout=5");
        });
        services.AddSingleton<IAppDatabaseInitializer, DatabaseInitializer>();
        services.AddSingleton<ICoreBusinessService, CoreBusinessService>();
        return services;
    }
}
