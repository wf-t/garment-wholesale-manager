using Shanquan.Inventory.Application.Abstractions;

namespace Shanquan.Inventory.Infrastructure.Persistence;

public sealed class AppPaths : IAppPaths
{
    public AppPaths()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ShanquanInventory");

        DataDirectory = Path.Combine(root, "data");
        BackupDirectory = Path.Combine(root, "backups");
        LogDirectory = Path.Combine(root, "logs");
        DatabasePath = Path.Combine(DataDirectory, "main.db");
    }

    public string DataDirectory { get; }
    public string DatabasePath { get; }
    public string BackupDirectory { get; }
    public string LogDirectory { get; }

    public void EnsureDirectories()
    {
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(BackupDirectory);
        Directory.CreateDirectory(LogDirectory);
    }
}

