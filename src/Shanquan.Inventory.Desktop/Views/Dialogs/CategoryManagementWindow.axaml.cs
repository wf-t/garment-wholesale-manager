using Avalonia.Controls;
using Avalonia.Interactivity;
using Shanquan.Inventory.Application.CoreBusiness;
using Shanquan.Inventory.Desktop.ViewModels.Dialogs;

namespace Shanquan.Inventory.Desktop.Views.Dialogs;

public partial class CategoryManagementWindow : Window
{
    public CategoryManagementWindow() => InitializeComponent();

    public CategoryManagementWindow(ICoreBusinessService businessService, IReadOnlyList<string> categories)
    {
        InitializeComponent();
        DataContext = new CategoryManagementViewModel(businessService, categories);
    }

    private void EditCategory_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: CategoryManagementItem item } && DataContext is CategoryManagementViewModel viewModel)
            viewModel.BeginEdit(item);
    }

    private async void SaveCategory_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: CategoryManagementItem item } && DataContext is CategoryManagementViewModel viewModel)
            await viewModel.SaveEditAsync(item);
    }

    private void CancelEdit_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: CategoryManagementItem item } && DataContext is CategoryManagementViewModel viewModel)
            viewModel.CancelEdit(item);
    }

    private async void DeleteCategory_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: CategoryManagementItem item } || DataContext is not CategoryManagementViewModel viewModel) return;

        var usage = await viewModel.GetUsageAsync(item);
        if (usage.HasSalesData)
        {
            viewModel.StatusMessage = $"品类“{item.Name}”下的商品已有销售单，不能删除。";
            return;
        }

        var message = usage.ProductCount == 0
            ? $"确定删除品类“{item.Name}”吗？"
            : $"品类“{item.Name}”下有 {usage.ProductCount} 个商品。继续操作将删除这些商品及其颜色尺码规格和库存记录，且不可恢复。";
        var confirmed = await new ConfirmWindow(
            usage.ProductCount == 0 ? "删除品类？" : "删除品类及关联商品？",
            message,
            usage.ProductCount == 0 ? "删除品类" : "删品类和商品").ShowDialog<bool>(this);
        if (confirmed) await viewModel.DeleteAsync(item);
    }

    private void Close_Click(object? sender, RoutedEventArgs e) => Close();
}
