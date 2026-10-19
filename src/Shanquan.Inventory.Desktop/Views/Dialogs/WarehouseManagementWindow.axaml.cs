using Avalonia.Controls;
using Avalonia.Interactivity;
using Shanquan.Inventory.Application.CoreBusiness;
using Shanquan.Inventory.Desktop.ViewModels.Dialogs;

namespace Shanquan.Inventory.Desktop.Views.Dialogs;

public partial class WarehouseManagementWindow : Window
{
    public WarehouseManagementWindow() => InitializeComponent();

    public WarehouseManagementWindow(ICoreBusinessService businessService, IReadOnlyList<WarehouseListItem> warehouses)
    {
        InitializeComponent();
        DataContext = new WarehouseManagementViewModel(businessService, warehouses);
    }

    private void EditWarehouse_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: WarehouseManagementItem item } && DataContext is WarehouseManagementViewModel viewModel)
            viewModel.BeginEdit(item);
    }

    private async void SaveWarehouse_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: WarehouseManagementItem item } && DataContext is WarehouseManagementViewModel viewModel)
            await viewModel.SaveEditAsync(item);
    }

    private void CancelEdit_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: WarehouseManagementItem item } && DataContext is WarehouseManagementViewModel viewModel)
            viewModel.CancelEdit(item);
    }

    private async void DeleteWarehouse_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: WarehouseManagementItem item } || DataContext is not WarehouseManagementViewModel viewModel) return;
        var confirmed = await new ConfirmWindow(
            "删除仓库？",
            $"确定删除仓库“{item.Code} · {item.Name}”吗？已有库存或业务流水的仓库将受保护，无法删除。",
            "删除仓库").ShowDialog<bool>(this);
        if (confirmed) await viewModel.DeleteAsync(item);
    }

    private void Close_Click(object? sender, RoutedEventArgs e) => Close();
}
