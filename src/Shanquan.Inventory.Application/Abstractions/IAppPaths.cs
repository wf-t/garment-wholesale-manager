namespace Shanquan.Inventory.Application.Abstractions;

public interface IAppPaths
{
    string DataDirectory { get; }
    string DatabasePath { get; }
    string BackupDirectory { get; }
    string LogDirectory { get; }
}

