namespace Shanquan.Inventory.Application.Abstractions;

public interface IAppDatabaseInitializer
{
    Task<DatabaseStartupResult> InitializeAsync(CancellationToken cancellationToken = default);
}

public sealed record DatabaseStartupResult(string DatabasePath, string SqliteVersion, bool Created);

