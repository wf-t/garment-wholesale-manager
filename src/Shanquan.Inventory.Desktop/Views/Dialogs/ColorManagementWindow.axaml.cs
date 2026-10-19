using Avalonia.Controls;
using Avalonia.Interactivity;
using Shanquan.Inventory.Application.CoreBusiness;
using Shanquan.Inventory.Desktop.ViewModels.Dialogs;

namespace Shanquan.Inventory.Desktop.Views.Dialogs;

public partial class ColorManagementWindow : Window
{
    public ColorManagementWindow() => InitializeComponent();

    public ColorManagementWindow(ICoreBusinessService businessService, IReadOnlyList<ColorListItem> colors)
    {
        InitializeComponent();
        DataContext = new ColorManagementViewModel(businessService, colors);
    }

    private void EditColor_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ColorManagementItem item } && DataContext is ColorManagementViewModel viewModel)
            viewModel.BeginEdit(item);
    }

    private async void SaveColor_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ColorManagementItem item } && DataContext is ColorManagementViewModel viewModel)
            await viewModel.SaveEditAsync(item);
    }

    private void CancelEdit_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ColorManagementItem item } && DataContext is ColorManagementViewModel viewModel)
            viewModel.CancelEdit(item);
    }

    private async void DeleteColor_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ColorManagementItem item } || DataContext is not ColorManagementViewModel viewModel) return;
        var confirmed = await new ConfirmWindow(
            "删除颜色？",
            $"确定删除颜色“{item.Name}”吗？已被商品规格使用的颜色将受保护，无法删除。",
            "删除颜色").ShowDialog<bool>(this);
        if (confirmed) await viewModel.DeleteAsync(item);
    }

    private void Close_Click(object? sender, RoutedEventArgs e) => Close();
}
