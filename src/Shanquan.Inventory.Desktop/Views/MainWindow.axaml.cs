using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Shanquan.Inventory.Desktop.ViewModels;
using Shanquan.Inventory.Application.CoreBusiness;
using System.Text;

namespace Shanquan.Inventory.Desktop.Views;

public partial class MainWindow : Window
{
    private bool _selectingSalesCustomer;
    private bool _selectingSalesProduct;
    private WindowEdge? _resizeEdge;
    private PixelPoint _resizeStartPointer;
    private PixelPoint _resizeStartPosition;
    private double _resizeStartWidth;
    private double _resizeStartHeight;

    public MainWindow()
    {
        InitializeComponent();
        UpdateMaximizeIcon();
    }

    public MainWindow(MainViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        if (e.ClickCount == 2)
        {
            ToggleWindowState();
            return;
        }
        BeginMoveDrag(e);
    }

    private void Minimize_Click(object? sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Maximize_Click(object? sender, RoutedEventArgs e) => ToggleWindowState();
    private void Close_Click(object? sender, RoutedEventArgs e) => Close();

    private void ToggleWindowState()
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        UpdateMaximizeIcon();
    }

    private void UpdateMaximizeIcon()
    {
        var isMaximized = WindowState == WindowState.Maximized;
        MaximizeSingleIcon.IsVisible = !isMaximized;
        RestoreWindowIcon.IsVisible = isMaximized;
    }

    private void ResizeBorder_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (WindowState != WindowState.Normal) return;
        if (sender is not Control { Tag: string edgeName } resizeHandle || !Enum.TryParse<WindowEdge>(edgeName, out var edge)) return;

        _resizeEdge = edge;
        _resizeStartPointer = this.PointToScreen(e.GetPosition(this));
        _resizeStartPosition = Position;
        _resizeStartWidth = Bounds.Width;
        _resizeStartHeight = Bounds.Height;
        e.Pointer.Capture(resizeHandle);
        e.Handled = true;
    }

    private void ResizeBorder_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_resizeEdge is not { } edge || WindowState != WindowState.Normal) return;

        var pointer = this.PointToScreen(e.GetPosition(this));
        var scale = RenderScaling <= 0 ? 1d : RenderScaling;
        var deltaX = (pointer.X - _resizeStartPointer.X) / scale;
        var deltaY = (pointer.Y - _resizeStartPointer.Y) / scale;
        var resizeWest = edge is WindowEdge.West or WindowEdge.NorthWest or WindowEdge.SouthWest;
        var resizeEast = edge is WindowEdge.East or WindowEdge.NorthEast or WindowEdge.SouthEast;
        var resizeNorth = edge is WindowEdge.North or WindowEdge.NorthWest or WindowEdge.NorthEast;
        var resizeSouth = edge is WindowEdge.South or WindowEdge.SouthWest or WindowEdge.SouthEast;

        var targetWidth = resizeWest
            ? Math.Max(MinWidth, _resizeStartWidth - deltaX)
            : resizeEast
                ? Math.Max(MinWidth, _resizeStartWidth + deltaX)
                : _resizeStartWidth;
        var targetHeight = resizeNorth
            ? Math.Max(MinHeight, _resizeStartHeight - deltaY)
            : resizeSouth
                ? Math.Max(MinHeight, _resizeStartHeight + deltaY)
                : _resizeStartHeight;

        Width = targetWidth;
        Height = targetHeight;

        var positionX = resizeWest
            ? _resizeStartPosition.X + (int)Math.Round((_resizeStartWidth - targetWidth) * scale)
            : _resizeStartPosition.X;
        var positionY = resizeNorth
            ? _resizeStartPosition.Y + (int)Math.Round((_resizeStartHeight - targetHeight) * scale)
            : _resizeStartPosition.Y;
        Position = new PixelPoint(positionX, positionY);
        e.Handled = true;
    }

    private void ResizeBorder_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_resizeEdge is null) return;
        _resizeEdge = null;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void ResizeBorder_PointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        => _resizeEdge = null;

    private void SalesCustomerSearchInput_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_selectingSalesCustomer || DataContext is not MainViewModel viewModel) return;
        if (viewModel.SelectedCustomer is not null && SalesCustomerSearchInput.Text != viewModel.SelectedCustomer.DisplayName)
            viewModel.SelectedCustomer = null;
        RefreshSalesCustomerResults(viewModel, SalesCustomerSearchInput.Text, SalesCustomerSearchInput.IsFocused);
    }

    private void SalesCustomerSearchInput_GotFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel) return;
        SalesProductSearchPanel.IsVisible = false;
        var searchText = viewModel.SelectedCustomer is not null && SalesCustomerSearchInput.Text == viewModel.SelectedCustomer.DisplayName
            ? ""
            : SalesCustomerSearchInput.Text;
        RefreshSalesCustomerResults(viewModel, searchText, true);
        if (viewModel.SelectedCustomer is not null) SalesCustomerSearchInput.SelectAll();
    }

    private void ToggleSalesCustomerDropDown_Click(object? sender, RoutedEventArgs e)
    {
        if (SalesCustomerSearchPanel.IsVisible)
        {
            SalesCustomerSearchPanel.IsVisible = false;
            return;
        }
        if (DataContext is not MainViewModel viewModel) return;
        var searchText = viewModel.SelectedCustomer is not null && SalesCustomerSearchInput.Text == viewModel.SelectedCustomer.DisplayName
            ? ""
            : SalesCustomerSearchInput.Text;
        RefreshSalesCustomerResults(viewModel, searchText, true);
        SalesCustomerSearchInput.Focus();
    }

    private void SalesCustomerSearchInput_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            SalesCustomerSearchPanel.IsVisible = false;
            e.Handled = true;
            return;
        }
        if (e.Key != Key.Enter || DataContext is not MainViewModel viewModel) return;
        if (viewModel.FilteredSalesCustomers.FirstOrDefault() is { } first)
        {
            SelectSalesCustomer(viewModel, first);
            e.Handled = true;
        }
    }

    private void SelectSalesCustomer_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: CustomerListItem customer } && DataContext is MainViewModel viewModel)
            SelectSalesCustomer(viewModel, customer);
    }

    private void SelectSalesCustomer(MainViewModel viewModel, CustomerListItem customer)
    {
        _selectingSalesCustomer = true;
        viewModel.SelectedCustomer = customer;
        viewModel.SalesCustomerSearchText = customer.DisplayName;
        SalesCustomerSearchInput.Text = customer.DisplayName;
        SalesCustomerSearchPanel.IsVisible = false;
        _selectingSalesCustomer = false;
    }

    private void RefreshSalesCustomerResults(MainViewModel viewModel, string? searchText, bool open)
    {
        var count = viewModel.FilterSalesCustomers(searchText);
        SalesCustomerResultCount.Text = $"{count} 条结果";
        if (open) SalesCustomerSearchPanel.IsVisible = true;
    }

    private void SalesProductSearchInput_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_selectingSalesProduct || DataContext is not MainViewModel viewModel) return;
        RefreshSalesProductResults(viewModel, SalesProductSearchInput.Text, SalesProductSearchInput.IsFocused);
    }

    private void SalesProductSearchInput_GotFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel) return;
        SalesCustomerSearchPanel.IsVisible = false;
        RefreshSalesProductResults(viewModel, SalesProductSearchInput.Text, true);
    }

    private void ToggleSalesProductDropDown_Click(object? sender, RoutedEventArgs e)
    {
        if (SalesProductSearchPanel.IsVisible)
        {
            SalesProductSearchPanel.IsVisible = false;
            return;
        }
        if (DataContext is not MainViewModel viewModel) return;
        RefreshSalesProductResults(viewModel, SalesProductSearchInput.Text, true);
        SalesProductSearchInput.Focus();
    }

    private void SalesProductSearchInput_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            SalesProductSearchPanel.IsVisible = false;
            e.Handled = true;
            return;
        }
        if (e.Key != Key.Enter || DataContext is not MainViewModel viewModel) return;
        if (viewModel.FilteredSalesProducts.FirstOrDefault() is { } first)
        {
            SelectSalesProduct(viewModel, first);
            e.Handled = true;
        }
    }

    private void SelectSalesProduct_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ProductListItem product } && DataContext is MainViewModel viewModel)
            SelectSalesProduct(viewModel, product);
    }

    private void SelectSalesProduct(MainViewModel viewModel, ProductListItem product)
    {
        _selectingSalesProduct = true;
        SalesProductSearchPanel.IsVisible = false;
        if (viewModel.AddSalesProductFromSearch(product))
            viewModel.SalesProductSearchText = "";
        SalesProductSearchInput.Text = viewModel.SalesProductSearchText;
        _selectingSalesProduct = false;
    }

    private void RefreshSalesProductResults(MainViewModel viewModel, string? searchText, bool open)
    {
        var count = viewModel.FilterSalesProducts(searchText);
        SalesProductResultCount.Text = $"{count} 条结果";
        if (open) SalesProductSearchPanel.IsVisible = true;
    }

    private void CustomerView_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: CustomerListItem customer } || DataContext is not MainViewModel viewModel) return;
        viewModel.SelectedManagedCustomer = customer;
        viewModel.OpenEditCustomerCommand.Execute(null);
    }

    private async void ExportCustomers_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel) return;
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "导出客户档案",
            SuggestedFileName = $"客户档案-{DateTime.Now:yyyyMMdd}.csv",
            FileTypeChoices = [new FilePickerFileType("CSV 表格") { Patterns = ["*.csv"] }],
        });
        if (file is null) return;

        await using var stream = await file.OpenWriteAsync();
        stream.SetLength(0);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(true));
        await writer.WriteLineAsync("客户编号,客户名称,联系电话,地址,当前欠款,信用额度,账期,累计销售,最近交易,状态");
        foreach (var customer in viewModel.FilteredCustomers)
        {
            await writer.WriteLineAsync(string.Join(',',
                Csv(customer.Code), Csv(customer.Name), Csv(customer.Phone), Csv(customer.Address),
                Csv(customer.ReceivableText), Csv(customer.CreditLimitText), Csv(customer.PaymentTermText), Csv(customer.TotalSalesText),
                Csv(customer.LastTransactionText), Csv(customer.StatusText)));
        }
        viewModel.OperationMessage = $"已导出 {viewModel.FilteredCustomers.Count} 位客户";
    }

    private void InventorySummaryRow_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        if (sender is Border { Tag: InventorySummaryRow row } && DataContext is MainViewModel viewModel)
        {
            viewModel.ToggleInventoryRow(row);
            e.Handled = true;
        }
    }

    private async void EditInventorySku_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: InventorySkuDetailRow detail } && DataContext is MainViewModel viewModel)
            await viewModel.OpenEditInventorySkuAsync(detail.SkuId);
    }

    private void DashboardTask_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        if (sender is Border { Tag: TaskItem task } && DataContext is MainViewModel viewModel)
        {
            viewModel.SelectSectionCommand.Execute(task.TargetSection);
            e.Handled = true;
        }
    }

    private void RecentOrder_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        if (sender is Border { Tag: RecentOrder order } && DataContext is MainViewModel viewModel)
        {
            viewModel.OpenShippingOrder(order.OrderId);
            e.Handled = true;
        }
    }

    private void ShippingStatusFilter_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string status } && DataContext is MainViewModel viewModel)
            viewModel.SelectShippingStatusCommand.Execute(status);
    }

    private void CustomerStatement_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: CustomerReceivableSummary customer } && DataContext is MainViewModel viewModel)
            viewModel.ShowCustomerStatementCommand.Execute(customer);
    }

    private async void ExportReceivables_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel) return;
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "导出应收汇总",
            SuggestedFileName = $"客户应收汇总-{DateTime.Now:yyyyMMdd}.csv",
            FileTypeChoices = [new FilePickerFileType("CSV 表格") { Patterns = ["*.csv"] }],
        });
        if (file is null) return;

        await using var stream = await file.OpenWriteAsync();
        stream.SetLength(0);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(true));
        await writer.WriteLineAsync("客户名称,当前欠款,信用额度,未到期,7天内到期,逾期金额,最长逾期天数,最近交易");
        foreach (var customer in viewModel.ReceivableCustomers)
        {
            var open = viewModel.OpenReceivables.Where(x => x.CustomerId == customer.CustomerId).ToList();
            await writer.WriteLineAsync(string.Join(',',
                Csv(customer.Name), Csv(customer.AmountText), Csv(customer.CreditText),
                Csv(MoneyText.Format(open.Where(x => !x.IsOverdue).Sum(x => x.OutstandingCents))),
                Csv(MoneyText.Format(open.Where(x => x.IsDueWithinSevenDays).Sum(x => x.OutstandingCents))),
                Csv(MoneyText.Format(open.Where(x => x.IsOverdue).Sum(x => x.OutstandingCents))),
                Csv((open.Count == 0 ? 0 : open.Max(x => x.OverdueDays)).ToString()),
                Csv(customer.LastTransactionText)));
        }
        viewModel.OperationMessage = $"已导出 {viewModel.ReceivableCustomers.Count} 位客户的应收汇总";
    }

    private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
}
