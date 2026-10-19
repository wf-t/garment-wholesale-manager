namespace Shanquan.Inventory.Domain.Common;

public sealed class DomainRuleException(string message) : InvalidOperationException(message);

