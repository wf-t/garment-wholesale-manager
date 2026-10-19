using Avalonia.Controls;
using Avalonia.Interactivity;
using Shanquan.Inventory.Application.CoreBusiness;
using Shanquan.Inventory.Desktop.Services;
using Shanquan.Inventory.Desktop.ViewModels.Dialogs;

namespace Shanquan.Inventory.Desktop.Views.Dialogs;

public partial class CustomerFormWindow : Window
{
    public CustomerFormWindow() : this(null) { }

    public CustomerFormWindow(CustomerListItem? customer)
    {
        InitializeComponent();
        DataContext = new CustomerFormViewModel(customer);
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CustomerFormViewModel viewModel && viewModel.BuildResult() is { } result)
            Close(result);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);
}
