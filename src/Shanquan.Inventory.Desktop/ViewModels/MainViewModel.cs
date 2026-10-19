using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shanquan.Inventory.Application.Abstractions;
using Shanquan.Inventory.Application.CoreBusiness;
using Shanquan.Inventory.Desktop.Services;

namespace Shanquan.Inventory.Desktop.ViewModels;

public sealed partial class MainViewModel : ViewModelBase
{
    private readonly IAppDatabaseInitializer _databaseInitializer;
    private readonly ICoreBusinessService _businessService;
    private readonly IDialogService _dialogService;
    private bool _isInitialized;
    private bool _isRefreshing;

    public MainViewModel(
        IAppDatabaseInitializer databaseInitializer,
        ICoreBusinessService businessService,
        IDialogService dialogService)
    {
        _databaseInitializer = databaseInitializer;
        _businessService = businessService;
        _dialogService = dialogService;
        _ = InitializeAsync();
    }

    public ObservableCollection<MetricCard> Metrics { get; } = [];
    public ObservableCollection<int> SalesStatisticsYears { get; } = [];
    public ObservableCollection<RecentOrder> RecentOrders { get; } = [];
    public ObservableCollection<TopSellingProduct> TopSellingProducts { get; } = [];
    public ObservableCollection<TaskItem> Tasks { get; } = [];
    public ObservableCollection<CustomerListItem> Customers { get; } = [];
    public ObservableCollection<CustomerListItem> ActiveCustomers { get; } = [];
    public ObservableCollection<CustomerListItem> FilteredSalesCustomers { get; } = [];
    public ObservableCollection<WarehouseListItem> Warehouses { get; } = [];
    public ObservableCollection<ProductListItem> Products { get; } = [];
    public ObservableCollection<SkuListItem> Skus { get; } = [];
    public ObservableCollection<ProductListItem> AvailableSalesProducts { get; } = [];
    public ObservableCollection<ProductListItem> FilteredSalesProducts { get; } = [];
    public ObservableCollection<SalesOrderListItem> SalesOrders { get; } = [];
    public ObservableCollection<SalesOrderListItem> FilteredShippingOrders { get; } = [];
    public ObservableCollection<ShippingOrderLineItem> ShippingOrderLines { get; } = [];
    public ObservableCollection<ShippingOrderLineItem> SelectedShippingOrderLines { get; } = [];
    public ObservableCollection<ShippingWorkflowStep> ShippingWorkflowSteps { get; } = [];
    public ObservableCollection<ShippingStatusFilterItem> ShippingStatusFilterOptions { get; } = [];
    public ObservableCollection<StockListItem> StockItems { get; } = [];
    public ObservableCollection<StockListItem> FilteredStockItems { get; } = [];
    public ObservableCollection<InventorySummaryRow> InventorySummaryRows { get; } = [];
    public ObservableCollection<InventorySummaryRow> FilteredInventorySummaryRows { get; } = [];
    public ObservableCollection<string> InventoryWarehouseFilters { get; } = [];
    public ObservableCollection<string> InventoryCategoryFilters { get; } = [];
    public ObservableCollection<string> InventorySeasonFilters { get; } = [];
    public ObservableCollection<string> Categories { get; } = [];
    public ObservableCollection<ColorListItem> Colors { get; } = [];
    public IReadOnlyList<string> InventoryStatusFilters { get; } = ["全部状态", "充足", "偏低", "预警", "缺货"];
    public ObservableCollection<CustomerListItem> FilteredCustomers { get; } = [];
    public ObservableCollection<StockMovementListItem> StockMovements { get; } = [];
    public ObservableCollection<ReceivableListItem> Receivables { get; } = [];
    public ObservableCollection<OpenReceivableListItem> OpenReceivables { get; } = [];
    public ObservableCollection<CustomerReceivableSummary> ReceivableCustomers { get; } = [];
    public ObservableCollection<ReceivableAgingRow> ReceivableAgingRows { get; } = [];
    public ObservableCollection<SalesEntryRow> SalesEntryRows { get; } = [];
    public IReadOnlyList<string> CustomerStatusFilters { get; } = ["全部客户", "正常客户", "有欠款", "已停用"];
    public IReadOnlyList<string> PaymentStatuses { get; } = ["未收款", "已收款"];
    public IReadOnlyList<string> ShippingStatusFilters { get; } = ["全部", "待配货", "配货中", "待发货", "已发货", "已签收"];
    public IReadOnlyList<int> SalesStatisticsMonths { get; } = Enumerable.Range(1, 12).ToArray();

    private IReadOnlyList<SalesPeriodSummary> _salesPeriodSummaries = [];

    [ObservableProperty]
    private string _selectedSection = "经营首页";

    [ObservableProperty]
    private string _databaseStatus = "正在检查本地数据…";

    [ObservableProperty]
    private string _operationMessage = "系统准备中";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private CustomerListItem? _selectedCustomer;

    [ObservableProperty]
    private WarehouseListItem? _selectedWarehouse;

    [ObservableProperty]
    private ProductListItem? _selectedSalesProduct;

    [ObservableProperty]
    private SalesOrderListItem? _selectedShippingOrder;

    [ObservableProperty] private string _selectedShippingStatusFilter = "全部";

    [ObservableProperty] private CustomerListItem? _selectedManagedCustomer;
    [ObservableProperty] private string _customerCodeText = "";
    [ObservableProperty] private string _customerNameText = "";
    [ObservableProperty] private string _customerPhoneText = "";
    [ObservableProperty] private string _customerAddressText = "";
    [ObservableProperty] private string _customerCreditLimitText = "0.00";

    [ObservableProperty] private ProductListItem? _selectedManagedProduct;
    [ObservableProperty] private string _productStyleNumberText = "";
    [ObservableProperty] private string _productNameText = "";
    [ObservableProperty] private string _productCategoryText = "";
    [ObservableProperty] private string _productSeasonText = "";
    [ObservableProperty] private string _productBasePriceText = "0.00";
    [ObservableProperty] private string _productCostPriceText = "0.00";
    [ObservableProperty] private string _skuColorText = "";
    [ObservableProperty] private string _skuSizeText = "";
    [ObservableProperty] private string _skuBarcodeText = "";

    [ObservableProperty] private WarehouseListItem? _selectedStockWarehouse;
    [ObservableProperty] private SkuListItem? _selectedStockSku;
    [ObservableProperty] private string _stockQuantityText = "1";
    [ObservableProperty] private string _stockNoteText = "采购到货";
    [ObservableProperty] private string _salesCustomerSearchText = "";
    [ObservableProperty] private string _salesProductSearchText = "";
    [ObservableProperty] private string _selectedPaymentStatus = "未收款";
    [ObservableProperty] private DateTime? _selectedSalesOrderDate = DateTime.Today;
    [ObservableProperty] private string _freightText = "0.00";
    [ObservableProperty] private string _discountText = "0.00";
    [ObservableProperty] private string _salesDraftStatusText = "未保存";
    [ObservableProperty] private string _customerSearchText = "";
    [ObservableProperty] private string _selectedCustomerStatusFilter = "全部客户";
    [ObservableProperty] private string _inventorySearchText = "";
    [ObservableProperty] private string _selectedInventoryWarehouseFilter = "全部仓库";
    [ObservableProperty] private string _selectedInventoryStatusFilter = "全部状态";
    [ObservableProperty] private string _selectedInventoryCategoryFilter = "全部品类";
    [ObservableProperty] private string _selectedInventorySeasonFilter = "全部季节";
    [ObservableProperty] private InventorySummaryRow? _selectedInventorySummaryRow;
    [ObservableProperty] private string _selectedSalesStatisticsPeriod = "按月";
    [ObservableProperty] private int _selectedSalesStatisticsYear = DateTime.Today.Year;
    [ObservableProperty] private int _selectedSalesStatisticsMonth = DateTime.Today.Month;

    public bool IsDashboard => SelectedSection == "经营首页";
    public bool IsSales => SelectedSection == "销售开单";
    public bool IsCustomers => SelectedSection == "客户管理";
    public bool IsStock => SelectedSection == "库存管理";
    public bool IsShipping => SelectedSection == "配货发货";
    public bool IsReceivables => SelectedSection == "应收账款";
    public bool IsOtherSection => !(IsDashboard || IsSales || IsCustomers || IsStock || IsShipping || IsReceivables);
    public long CartQuantity => SalesEntryRows.Sum(x => x.TotalQuantity);
    public long CartAmountCents => SalesEntryRows.Sum(x => x.AmountCents);
    public string CartSummary => $"共 {CartQuantity} 件 · {MoneyText.Format(CartAmountCents)}";
    public string CartQuantityText => $"{CartQuantity} 件";
    public string CartAmountText => MoneyText.Format(CartAmountCents);
    public long FreightCents => TryParseMoney(FreightText);
    public long DiscountCents => TryParseMoney(DiscountText);
    public long PayableAmountCents => Math.Max(0, checked(CartAmountCents + FreightCents - DiscountCents));
    public string FreightAmountText => MoneyText.Format(FreightCents);
    public string DiscountAmountText => MoneyText.Format(DiscountCents);
    public string PayableAmountText => MoneyText.Format(PayableAmountCents);
    public string CurrentReceivedText => SelectedPaymentStatus == "已收款" ? PayableAmountText : MoneyText.Format(0);
    public string CurrentDebtText => SelectedPaymentStatus == "已收款" ? MoneyText.Format(0) : PayableAmountText;
    public string SalesCreditHint
    {
        get
        {
            if (SelectedCustomer is null) return "选择客户后将自动校验信用额度";
            if (SelectedPaymentStatus == "已收款") return "本单已收款，不增加客户欠款";
            if (SelectedCustomer.CreditLimitCents <= 0) return "该客户未设置信用额度上限";
            var remaining = SelectedCustomer.CreditLimitCents - SelectedCustomer.ReceivableCents - SelectedCustomerPendingDebtCents - PayableAmountCents;
            return remaining >= 0
                ? $"本单审核后，客户信用额度预计剩余 {MoneyText.Format(remaining)}"
                : $"本单将超出客户信用额度 {MoneyText.Format(-remaining)}，无法审核";
        }
    }
    public string SalesCreditHintColor => SelectedCustomer is not null &&
                                           SelectedPaymentStatus == "未收款" &&
                                           SelectedCustomer.CreditLimitCents > 0 &&
                                           SelectedCustomer.CreditLimitCents - SelectedCustomer.ReceivableCents - SelectedCustomerPendingDebtCents - PayableAmountCents < 0
        ? "#9D3037"
        : "#817773";
    public bool HasNoSalesEntryRows => SalesEntryRows.Count == 0;
    public bool HasNoFilteredSalesCustomers => FilteredSalesCustomers.Count == 0;
    public bool HasNoFilteredSalesProducts => FilteredSalesProducts.Count == 0;
    public string DashboardDateText => $"{DateTime.Today:yyyy年M月d日} · {CultureInfo.GetCultureInfo("zh-CN").DateTimeFormat.GetDayName(DateTime.Today.DayOfWeek)}";
    public string DashboardGreetingText => DateTime.Now.Hour switch
    {
        < 6 => "夜深了，注意休息",
        < 12 => "早上好，今天从哪笔生意开始？",
        < 18 => "下午好，继续把生意打理得井井有条",
        _ => "晚上好，看看今天还有哪些事情待处理",
    };
    public string DashboardTaskCount => Tasks.Count.ToString();
    public bool IsMonthlySalesStatistics => SelectedSalesStatisticsPeriod == "按月";
    public bool IsYearlySalesStatistics => SelectedSalesStatisticsPeriod == "按年";
    public string SalesStatisticsPeriodText => IsMonthlySalesStatistics
        ? $"{SelectedSalesStatisticsYear}年{SelectedSalesStatisticsMonth}月"
        : $"{SelectedSalesStatisticsYear}年全年";
    public string SalesStatisticsAmountText
    {
        get
        {
            var summaries = SelectedSalesStatisticsSummaries;
            return MoneyText.Format(summaries.Sum(x => x.AmountCents));
        }
    }
    public string SalesStatisticsDetailText
    {
        get
        {
            var summaries = SelectedSalesStatisticsSummaries;
            return $"共 {summaries.Sum(x => x.Quantity):N0} 件 · {summaries.Sum(x => x.OrderCount):N0} 笔销售单";
        }
    }
    private IEnumerable<SalesPeriodSummary> SelectedSalesStatisticsSummaries => _salesPeriodSummaries.Where(x =>
        x.Year == SelectedSalesStatisticsYear &&
        (!IsMonthlySalesStatistics || x.Month == SelectedSalesStatisticsMonth));
    public string SalesOrderNumberText => $"草稿 {(SelectedSalesOrderDate ?? DateTime.Today):yyyyMMdd}";
    public string SalesOrderDraftText => "正式单号将在审核时生成 · 草稿可保存在本机";
    public string SalesOrderDateText => (SelectedSalesOrderDate ?? DateTime.Today).ToString("yyyy/MM/dd");
    public long SelectedCustomerPendingDebtCents => SelectedCustomer is null
        ? 0
        : SalesOrders.Where(x => x.CustomerId == SelectedCustomer.Id && x.IsActiveFulfillment && x.PaymentStatus == "未收款")
            .Sum(x => x.TotalAmountCents);
    public string SelectedCustomerSalesHint => SelectedCustomer is null
        ? "请选择开单客户"
        : SelectedCustomer.CreditLimitCents <= 0
            ? $"当前欠款 {SelectedCustomer.ReceivableText} · 未设置额度上限"
            : $"当前欠款 {SelectedCustomer.ReceivableText} · 含待发订单可用 {MoneyText.Format(Math.Max(0, SelectedCustomer.CreditLimitCents - SelectedCustomer.ReceivableCents - SelectedCustomerPendingDebtCents))}";
    public string CustomerTotalText => Customers.Count.ToString("N0");
    public string CustomerNewThisMonthText => $"本月新增 {Customers.Count(x => x.CreatedAt.LocalDateTime.Year == DateTime.Today.Year && x.CreatedAt.LocalDateTime.Month == DateTime.Today.Month)} 位";
    public string CustomerMonthlyDealText => Customers.Count(x => x.MonthlySalesCents > 0).ToString("N0");
    public string CustomerMonthlyDealRateText => $"成交率 {(Customers.Count == 0 ? 0 : Customers.Count(x => x.MonthlySalesCents > 0) * 100m / Customers.Count):0.0}%";
    public string CustomerReceivableCountText => Customers.Count(x => x.ReceivableCents > 0).ToString("N0");
    public string CustomerReceivableTotalText => $"欠款总额 {MoneyText.Format(Customers.Sum(x => x.ReceivableCents))}";
    public int PendingShipmentCount => SalesOrders.Count(x => x.IsActiveFulfillment);
    public bool HasPendingShipments => PendingShipmentCount > 0;
    public string StockOnHandTotalText => StockItems.Sum(x => x.OnHand).ToString("N0");
    public string StockReservedTotalText => StockItems.Sum(x => x.Reserved).ToString("N0");
    public string StockAvailableTotalText => StockItems.Sum(x => x.Available).ToString("N0");
    public string StockValueText
    {
        get
        {
            var priceByStyle = Products.GroupBy(x => x.StyleNumber).ToDictionary(x => x.Key, x => x.First().CostPriceCents);
            var cents = StockItems.Sum(x => x.OnHand * priceByStyle.GetValueOrDefault(x.StyleNumber));
            return MoneyText.Format(cents);
        }
    }
    public string StockWarningCountText => InventorySummaryRows.Count(x => x.Status is "预警" or "缺货").ToString("N0");
    public string StockOutCountText => $"缺货 {InventorySummaryRows.Count(x => x.Status == "缺货")} 个款色";
    public string StockReservedOrderText => $"涉及 {PendingShipmentCount} 笔订单";
    public int WaitingPickingCount => SalesOrders.Count(x => x.CanStartPicking);
    public int PickingCount => SalesOrders.Count(x => x.Status == "配货中");
    public int ReadyToShipCount => SalesOrders.Count(x => x.Status == "待发货");
    public string WaitingPickingQuantityText => SalesOrders.Where(x => x.CanStartPicking).Sum(x => x.TotalQuantity).ToString("N0");
    public string PickingQuantityText => SalesOrders.Where(x => x.Status == "配货中").Sum(x => x.TotalQuantity).ToString("N0");
    public string ReadyToShipQuantityText => SalesOrders.Where(x => x.Status == "待发货").Sum(x => x.TotalQuantity).ToString("N0");
    public string PendingShipmentQuantityText => SalesOrders.Where(x => x.IsActiveFulfillment).Sum(x => x.TotalQuantity).ToString("N0");
    public string PendingShipmentAmountText => MoneyText.Format(SalesOrders.Where(x => x.IsActiveFulfillment).Sum(x => x.TotalAmountCents));
    public string ReceivableTotalText => MoneyText.Format(OpenReceivables.Sum(x => x.OutstandingCents));
    public string ReceivableCustomerCountText => OpenReceivables.Select(x => x.CustomerId).Distinct().Count().ToString("N0");
    public string ReceivableCustomerCountLabel => $"{ReceivableCustomerCountText} 位欠款客户";
    public bool HasReceivables => OpenReceivables.Count > 0;
    public string ReceivableEntryCountText => Receivables.Count.ToString("N0");
    public string ReceivableDueSoonText => MoneyText.Format(OpenReceivables.Where(x => x.IsDueWithinSevenDays).Sum(x => x.OutstandingCents));
    public string ReceivableDueSoonCustomerText => $"涉及 {OpenReceivables.Where(x => x.IsDueWithinSevenDays).Select(x => x.CustomerId).Distinct().Count()} 位客户";
    public string ReceivableOverdueText => MoneyText.Format(OpenReceivables.Where(x => x.IsOverdue).Sum(x => x.OutstandingCents));
    public string ReceivableOverdueDetailText => OpenReceivables.Any(x => x.IsOverdue)
        ? $"最长逾期 {OpenReceivables.Max(x => x.OverdueDays)} 天"
        : "暂无逾期";
    public string ReceivableRecoveredText => MoneyText.Format(-Receivables.Where(x => x.EntryType == "收款" && x.CreatedAt.LocalDateTime.Year == DateTime.Today.Year && x.CreatedAt.LocalDateTime.Month == DateTime.Today.Month).Sum(x => x.AmountCents));
    public string TodayShippedCountText => SalesOrders.Count(x => x.ShippedAt?.LocalDateTime.Date == DateTime.Today).ToString("N0");
    public string TodayShippedQuantityText => $"共 {SalesOrders.Where(x => x.ShippedAt?.LocalDateTime.Date == DateTime.Today).Sum(x => x.TotalQuantity):N0} 件";
    public bool HasSelectedShippingOrder => SelectedShippingOrder is not null;
    public string ShippingOrderDetailSummary => SelectedShippingOrderLines.Count == 0
        ? "请选择销售单查看明细"
        : $"{SelectedShippingOrderLines.Count} 个颜色尺码 · 共 {SelectedShippingOrderLines.Sum(x => x.Quantity):N0} 件";
    public string ShippingPrimaryActionText => SelectedShippingOrder?.DisplayStatus switch
    {
        "待配货" => "开始配货",
        "配货中" => "配货完成",
        "待发货" => "确认整单发货",
        "已发货" => "确认客户签收",
        "已签收" => "订单已完成",
        _ => "请选择销售单",
    };
    public bool CanExecuteShippingPrimaryAction => SelectedShippingOrder?.DisplayStatus switch
    {
        "待配货" => true,
        "配货中" => true,
        "待发货" => true,
        "已发货" => true,
        _ => false,
    };

    partial void OnSelectedCustomerChanged(CustomerListItem? value)
    {
        OnPropertyChanged(nameof(SelectedCustomerSalesHint));
        OnPropertyChanged(nameof(SelectedCustomerPendingDebtCents));
        OnPropertyChanged(nameof(SalesCreditHint));
        OnPropertyChanged(nameof(SalesCreditHintColor));
        MarkSalesDraftDirty();
    }

    partial void OnSelectedWarehouseChanged(WarehouseListItem? value)
    {
        if (_isRefreshing) return;
        if (SalesEntryRows.Count > 0)
        {
            SalesEntryRows.Clear();
            OperationMessage = "发货仓库已变更，请重新选择商品以加载该仓库存。";
            NotifyCartChanged();
        }
        RefreshAvailableSalesProducts();
    }

    partial void OnSelectedPaymentStatusChanged(string value)
    {
        OnPropertyChanged(nameof(CurrentReceivedText));
        OnPropertyChanged(nameof(CurrentDebtText));
        OnPropertyChanged(nameof(SalesCreditHint));
        OnPropertyChanged(nameof(SalesCreditHintColor));
        MarkSalesDraftDirty();
    }

    partial void OnSelectedSalesOrderDateChanged(DateTime? value)
    {
        OnPropertyChanged(nameof(SalesOrderDateText));
        OnPropertyChanged(nameof(SalesOrderNumberText));
        MarkSalesDraftDirty();
    }

    partial void OnSelectedShippingOrderChanged(SalesOrderListItem? value) => RefreshSelectedShippingOrder();
    partial void OnSelectedShippingStatusFilterChanged(string value) => FilterShippingOrders();

    partial void OnFreightTextChanged(string value) => NotifyCartChanged();
    partial void OnDiscountTextChanged(string value) => NotifyCartChanged();

    partial void OnSelectedManagedCustomerChanged(CustomerListItem? value)
    {
        if (_isRefreshing || value is null) return;
        LoadCustomerEditor(value);
    }

    partial void OnSelectedManagedProductChanged(ProductListItem? value)
    {
        if (_isRefreshing || value is null) return;
        LoadProductEditor(value);
    }

    partial void OnCustomerSearchTextChanged(string value) => FilterCustomerList(value);
    partial void OnSelectedCustomerStatusFilterChanged(string value) => FilterCustomerList(CustomerSearchText);
    partial void OnInventorySearchTextChanged(string value)
    {
        FilterStockList(value);
        FilterInventorySummaryRows();
    }
    partial void OnSelectedInventoryWarehouseFilterChanged(string value) => FilterInventorySummaryRows();
    partial void OnSelectedInventoryStatusFilterChanged(string value) => FilterInventorySummaryRows();
    partial void OnSelectedInventoryCategoryFilterChanged(string value) => FilterInventorySummaryRows();
    partial void OnSelectedInventorySeasonFilterChanged(string value) => FilterInventorySummaryRows();
    partial void OnSelectedInventorySummaryRowChanged(InventorySummaryRow? value)
    {
        if (value is null) return;
        SelectedManagedProduct = Products.FirstOrDefault(x => x.StyleNumber == value.StyleNumber) ?? SelectedManagedProduct;
    }

    partial void OnSelectedSalesStatisticsPeriodChanged(string value)
    {
        OnPropertyChanged(nameof(IsMonthlySalesStatistics));
        OnPropertyChanged(nameof(IsYearlySalesStatistics));
        NotifySalesStatisticsChanged();
    }

    partial void OnSelectedSalesStatisticsYearChanged(int value) => NotifySalesStatisticsChanged();
    partial void OnSelectedSalesStatisticsMonthChanged(int value) => NotifySalesStatisticsChanged();

    [RelayCommand]
    private void SelectSection(string section)
    {
        SelectedSection = section;
        NotifySectionChanged();
    }

    [RelayCommand]
    private void SelectSalesStatisticsPeriod(string period) => SelectedSalesStatisticsPeriod = period;

    [RelayCommand]
    private void CreateSalesOrder()
    {
        SelectedSection = "销售开单";
        NotifySectionChanged();
    }

    public void OpenShippingOrder(Guid orderId)
    {
        SelectedSection = "配货发货";
        SelectedShippingStatusFilter = "全部";
        FilterShippingOrders();
        SelectedShippingOrder = FilteredShippingOrders.FirstOrDefault(x => x.Id == orderId);
        NotifySectionChanged();
    }

    private void AddSalesProductRows(ProductListItem product, bool includeOutOfStockColors = false)
    {
        if (SelectedWarehouse is null)
        {
            OperationMessage = "请先选择发货仓库。";
            SelectedSalesProduct = null;
            return;
        }
        if (SalesEntryRows.Any(x => x.ProductId == product.Id))
        {
            OperationMessage = $"款号 {product.StyleNumber} 已在商品明细中。";
            SelectedSalesProduct = null;
            return;
        }

        var warehouseStock = StockItems
            .Where(x => x.WarehouseId == SelectedWarehouse.Id)
            .ToDictionary(x => x.SkuId);
        var productSkus = Skus
            .Where(x => x.ProductId == product.Id)
            .Select(sku => warehouseStock.TryGetValue(sku.Id, out var stock)
                ? sku with { OnHand = stock.OnHand, Reserved = stock.Reserved }
                : sku with { OnHand = 0, Reserved = 0 })
            .ToList();
        var colors = productSkus
            .Where(x => includeOutOfStockColors || x.Available > 0)
            .Select(x => x.ColorCode)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        if (productSkus.Count == 0 || colors.Count == 0)
        {
            OperationMessage = $"款号 {product.StyleNumber} 暂无可销售的颜色尺码。";
            SelectedSalesProduct = null;
            return;
        }

        foreach (var color in colors)
        {
            SalesEntryRows.Add(new SalesEntryRow(
                product.Id,
                product.StyleNumber,
                product.Name,
                colors,
                productSkus,
                color,
                NotifyCartChanged,
                RemoveSalesEntryRow));
        }

        SelectedSalesProduct = null;
        OperationMessage = $"已带入 {product.StyleNumber} 的 {colors.Count} 个颜色和全部尺码。";
        NotifyCartChanged();
    }

    public bool AddSalesProductFromSearch(ProductListItem product)
    {
        var rowCount = SalesEntryRows.Count;
        AddSalesProductRows(product);
        return SalesEntryRows.Count > rowCount;
    }

    public int FilterSalesCustomers(string? searchText)
    {
        var terms = (searchText ?? "")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var matches = ActiveCustomers
            .Where(customer => terms.Length == 0 || terms.All(term =>
                customer.Code.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                customer.Name.Contains(term, StringComparison.OrdinalIgnoreCase)))
            .Take(100)
            .ToList();
        Replace(FilteredSalesCustomers, matches);
        OnPropertyChanged(nameof(HasNoFilteredSalesCustomers));
        return matches.Count;
    }

    public int FilterSalesProducts(string? searchText)
    {
        var terms = (searchText ?? "")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var matches = AvailableSalesProducts
            .Where(product => terms.Length == 0 || terms.All(term =>
                product.StyleNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                product.Name.Contains(term, StringComparison.OrdinalIgnoreCase)))
            .Take(100)
            .ToList();
        Replace(FilteredSalesProducts, matches);
        OnPropertyChanged(nameof(HasNoFilteredSalesProducts));
        return matches.Count;
    }

    private void RemoveSalesEntryRow(SalesEntryRow row)
    {
        SalesEntryRows.Remove(row);
        OperationMessage = $"已移除 {row.StyleNumber} · {row.SelectedColor}";
        NotifyCartChanged();
    }

    private void RefreshAvailableSalesProducts()
    {
        if (SelectedWarehouse is null)
        {
            Replace(AvailableSalesProducts, []);
            Replace(FilteredSalesProducts, []);
            OnPropertyChanged(nameof(HasNoFilteredSalesProducts));
            return;
        }
        var availableSkuIds = StockItems
            .Where(x => x.WarehouseId == SelectedWarehouse.Id && x.Available > 0)
            .Select(x => x.SkuId)
            .ToHashSet();
        var productIds = Skus.Where(x => availableSkuIds.Contains(x.Id)).Select(x => x.ProductId).ToHashSet();
        Replace(AvailableSalesProducts, Products.Where(x => productIds.Contains(x.Id)));
        FilterSalesProducts(SalesProductSearchText);
    }

    [RelayCommand]
    private void NewCustomer()
    {
        SelectedManagedCustomer = null;
        CustomerCodeText = $"C{Customers.Count + 1:000}";
        CustomerNameText = "";
        CustomerPhoneText = "";
        CustomerAddressText = "";
        CustomerCreditLimitText = "0.00";
        OperationMessage = "正在新增客户档案";
    }

    [RelayCommand]
    private async Task SaveCustomerAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            var customerId = await _businessService.SaveCustomerAsync(new SaveCustomerRequest(
                SelectedManagedCustomer?.Id,
                CustomerCodeText,
                CustomerNameText,
                CustomerPhoneText,
                CustomerAddressText,
                ParseMoney(CustomerCreditLimitText, "信用额度"),
                SelectedManagedCustomer?.PaymentTermDays ?? 30));
            await RefreshAsync(SelectedCustomer?.Id);
            SelectedManagedCustomer = Customers.FirstOrDefault(x => x.Id == customerId);
            OperationMessage = "客户档案已保存";
        }
        catch (Exception exception) { OperationMessage = exception.Message; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ToggleCustomerActiveAsync()
    {
        if (IsBusy || SelectedManagedCustomer is null) return;
        try
        {
            IsBusy = true;
            var active = !SelectedManagedCustomer.IsActive;
            await _businessService.SetCustomerActiveAsync(SelectedManagedCustomer.Id, active);
            await RefreshAsync(SelectedCustomer?.Id);
            OperationMessage = active ? "客户已重新启用" : "客户已停用，不再允许开单";
        }
        catch (Exception exception) { OperationMessage = exception.Message; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void NewProduct()
    {
        SelectedManagedProduct = null;
        ProductStyleNumberText = "";
        ProductNameText = "";
        ProductCategoryText = "";
        ProductSeasonText = "";
        ProductBasePriceText = "0.00";
        ProductCostPriceText = "0.00";
        OperationMessage = "正在新增商品款号";
    }

    [RelayCommand]
    private async Task SaveProductAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            var productId = await _businessService.SaveProductAsync(new SaveProductRequest(
                SelectedManagedProduct?.Id,
                ProductStyleNumberText,
                ProductNameText,
                ProductCategoryText,
                ProductSeasonText,
                ParseMoney(ProductBasePriceText, "商品统一价"),
                ParseMoney(ProductCostPriceText, "商品成本价")));
            await RefreshAsync(SelectedCustomer?.Id);
            SelectedManagedProduct = Products.FirstOrDefault(x => x.Id == productId);
            OperationMessage = "商品档案已保存";
        }
        catch (Exception exception) { OperationMessage = exception.Message; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task AddSkuAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            if (SelectedManagedProduct is null) throw new InvalidOperationException("请先选择或保存商品款号。");
            await _businessService.AddSkuAsync(new AddSkuRequest(
                SelectedManagedProduct.Id,
                SkuColorText,
                SkuSizeText,
                SkuBarcodeText));
            SkuColorText = "";
            SkuSizeText = "";
            SkuBarcodeText = "";
            await RefreshAsync(SelectedCustomer?.Id);
            OperationMessage = "颜色尺码规格已新增，可办理入库";
        }
        catch (Exception exception) { OperationMessage = exception.Message; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ReceiveStockAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            if (SelectedStockWarehouse is null) throw new InvalidOperationException("请选择入库仓库。");
            if (SelectedStockSku is null) throw new InvalidOperationException("请选择入库商品规格。");
            if (!long.TryParse(StockQuantityText, out var quantity) || quantity <= 0)
                throw new InvalidOperationException("入库数量必须是大于零的整数。");
            var number = await _businessService.ReceiveStockAsync(new ReceiveStockRequest(
                SelectedStockWarehouse.Id,
                SelectedStockSku.Id,
                quantity,
                StockNoteText));
            await RefreshAsync(SelectedCustomer?.Id);
            OperationMessage = $"入库单 {number} 已完成，库存增加 {quantity} 件";
        }
        catch (Exception exception) { OperationMessage = exception.Message; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task AdjustStockAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            if (SelectedStockWarehouse is null) throw new InvalidOperationException("请选择盘点仓库。");
            if (SelectedStockSku is null) throw new InvalidOperationException("请选择盘点商品规格。");
            if (!long.TryParse(StockQuantityText, out var target) || target < 0)
                throw new InvalidOperationException("盘点库存必须是非负整数。");
            var number = await _businessService.AdjustStockAsync(new AdjustStockRequest(
                SelectedStockWarehouse.Id,
                SelectedStockSku.Id,
                target,
                StockNoteText));
            await RefreshAsync(SelectedCustomer?.Id);
            OperationMessage = $"盘点单 {number} 已完成";
        }
        catch (Exception exception) { OperationMessage = exception.Message; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task OpenNewCustomerAsync() => await OpenCustomerFormAsync(null);

    [RelayCommand]
    private async Task OpenEditCustomerAsync()
    {
        if (SelectedManagedCustomer is null)
        {
            OperationMessage = "请先在客户列表中选择客户。";
            return;
        }
        await OpenCustomerFormAsync(SelectedManagedCustomer);
    }

    [RelayCommand]
    private async Task OpenNewProductAsync()
        => await OpenInventoryFormAsync(InventoryFormMode.Product, null);

    [RelayCommand]
    private async Task OpenEditProductAsync()
    {
        if (SelectedManagedProduct is null)
        {
            OperationMessage = "请先选择要编辑的商品。";
            return;
        }
        await OpenInventoryFormAsync(InventoryFormMode.Product, SelectedManagedProduct);
    }

    [RelayCommand]
    private async Task OpenNewSkuAsync()
        => await OpenInventoryFormAsync(InventoryFormMode.Sku, SelectedManagedProduct);

    public void ToggleInventoryRow(InventorySummaryRow row)
    {
        row.IsExpanded = !row.IsExpanded;
    }

    public async Task OpenEditInventorySkuAsync(Guid skuId)
    {
        var sku = Skus.FirstOrDefault(x => x.Id == skuId);
        if (sku is null)
        {
            OperationMessage = "商品规格不存在或已停用。";
            return;
        }
        var product = Products.FirstOrDefault(x => x.Id == sku.ProductId);
        await OpenInventoryFormAsync(InventoryFormMode.EditSku, product, sku);
    }

    [RelayCommand]
    private async Task OpenReceiveStockAsync()
        => await OpenInventoryFormAsync(InventoryFormMode.Receive, null);

    [RelayCommand]
    private async Task OpenAdjustStockAsync()
        => await OpenInventoryFormAsync(InventoryFormMode.Adjust, null);

    [RelayCommand]
    private async Task SaveSalesDraftAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            var lines = BuildSalesOrderLines();
            if (SelectedCustomer is null) throw new InvalidOperationException("请选择客户。");
            if (SelectedWarehouse is null) throw new InvalidOperationException("请选择发货仓库。");
            await _businessService.SaveSalesDraftAsync(new SaveSalesDraftRequest(
                SelectedCustomer.Id,
                SelectedWarehouse.Id,
                lines,
                ToLocalDateTimeOffset(SelectedSalesOrderDate ?? DateTime.Today),
                ParseSalesMoney(FreightText, "运费"),
                ParseSalesMoney(DiscountText, "优惠金额"),
                SelectedPaymentStatus));
            SalesDraftStatusText = $"已保存 {DateTime.Now:HH:mm}";
            OperationMessage = "销售草稿已保存到本机。";
        }
        catch (Exception exception) { OperationMessage = exception.Message; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ClearSalesOrderAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            await _businessService.ClearSalesDraftAsync();
            ResetSalesForm();
            OperationMessage = "已清空当前销售单和本机草稿。";
        }
        catch (Exception exception) { OperationMessage = exception.Message; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task PreviewSalesOrderAsync()
    {
        try
        {
            var lines = BuildSalesOrderLines();
            if (SelectedCustomer is null) throw new InvalidOperationException("请选择客户。");
            if (SelectedWarehouse is null) throw new InvalidOperationException("请选择发货仓库。");
            var lineLookup = lines.ToDictionary(x => x.SkuId);
            var previewRows = SalesEntryRows
                .Select(row => new
                {
                    Row = row,
                    Quantities = row.SizeEntries
                        .Where(cell => lineLookup.ContainsKey(cell.SkuId))
                        .GroupBy(cell => cell.SizeCode)
                        .ToDictionary(x => x.Key, x => x.Sum(cell => cell.Quantity)),
                })
                .Where(x => x.Quantities.Count > 0)
                .ToList();
            var sizeHeaders = Enumerable.Range(26, 12).Select(x => x.ToString()).ToList();
            var previewLines = previewRows
                .Select((x, index) =>
                {
                    var totalQuantity = x.Quantities.Values.Sum();
                    return new SalesOrderPreviewLine(
                        index + 1,
                        x.Row.StyleNumber,
                        x.Row.SelectedColor,
                        sizeHeaders.Select(size => new SalesOrderPreviewSizeCell(
                            size,
                            x.Quantities.TryGetValue(size, out var quantity) ? quantity.ToString("N0") : "")).ToList(),
                        totalQuantity.ToString("N0"),
                        MoneyText.Format(x.Row.UnitPriceCents),
                        MoneyText.Format(checked(totalQuantity * x.Row.UnitPriceCents)));
                })
                .ToList();
            var documentDate = SelectedSalesOrderDate ?? DateTime.Today;
            await _dialogService.ShowSalesOrderPreviewAsync(new SalesOrderPreviewData(
                SalesOrderNumberText,
                $"{documentDate:yyyy-MM-dd} {DateTime.Now:HH:mm}",
                SelectedCustomer.Name,
                SelectedCustomer.Phone,
                PayableAmountText,
                sizeHeaders,
                previewLines));
        }
        catch (Exception exception) { OperationMessage = exception.Message; }
    }

    [RelayCommand]
    private async Task SubmitSalesOrderAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            if (SelectedCustomer is null) throw new InvalidOperationException("请选择客户。");
            if (SelectedWarehouse is null) throw new InvalidOperationException("请选择仓库。");
            var orderLines = BuildSalesOrderLines();
            var freightCents = ParseSalesMoney(FreightText, "运费");
            var discountCents = ParseSalesMoney(DiscountText, "优惠金额");
            if (discountCents > checked(CartAmountCents + freightCents))
                throw new InvalidOperationException("优惠金额不能大于商品金额与运费之和。");

            var number = await _businessService.CreateAndReserveSalesOrderAsync(new CreateSalesOrderRequest(
                SelectedCustomer.Id,
                SelectedWarehouse.Id,
                orderLines,
                ToLocalDateTimeOffset(SelectedSalesOrderDate ?? DateTime.Today),
                freightCents,
                discountCents,
                SelectedPaymentStatus));

            await _businessService.ClearSalesDraftAsync();
            ResetSalesForm();
            await RefreshAsync(SelectedCustomer.Id);
            OperationMessage = $"销售单 {number} 已审核并锁定库存";
        }
        catch (Exception exception)
        {
            OperationMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private List<CreateSalesOrderLine> BuildSalesOrderLines()
    {
        var duplicate = SalesEntryRows
            .GroupBy(x => new { x.ProductId, Color = x.SelectedColor.Trim().ToUpperInvariant() })
            .FirstOrDefault(x => x.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"款号 {duplicate.First().StyleNumber} 的颜色 {duplicate.First().SelectedColor} 重复，请删除重复行。");

        var lines = SalesEntryRows.SelectMany(x => x.BuildOrderLines()).ToList();
        if (lines.Count == 0) throw new InvalidOperationException("请先选择商品并录入尺码数量。");
        var freightCents = ParseSalesMoney(FreightText, "运费");
        var discountCents = ParseSalesMoney(DiscountText, "优惠金额");
        if (discountCents > checked(CartAmountCents + freightCents))
            throw new InvalidOperationException("优惠金额不能大于商品金额与运费之和。");
        return lines;
    }

    private void ResetSalesForm()
    {
        SalesEntryRows.Clear();
        SelectedSalesProduct = null;
        SalesProductSearchText = "";
        SelectedSalesOrderDate = DateTime.Today;
        FreightText = "0.00";
        DiscountText = "0.00";
        SelectedPaymentStatus = "未收款";
        SalesDraftStatusText = "未保存";
        NotifyCartChanged(markDraftDirty: false);
    }

    [RelayCommand]
    private void SelectShippingStatus(string status) => SelectedShippingStatusFilter = status;

    [RelayCommand]
    private async Task ExecuteShippingPrimaryActionAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            var order = SelectedShippingOrder ?? throw new InvalidOperationException("请选择销售单。");
            var selectedOrderId = order.Id;
            switch (order.DisplayStatus)
            {
                case "待配货":
                    await _businessService.StartPickingAsync(order.Id);
                    OperationMessage = $"销售单 {order.Number} 已开始配货";
                    break;
                case "配货中":
                    await _businessService.CompletePickingAsync(order.Id);
                    OperationMessage = $"销售单 {order.Number} 已完成配货，等待发货";
                    break;
                case "待发货":
                    var expressTrackingNumber = await _dialogService.ShowShippingConfirmAsync(
                        "确认整单发货",
                        $"销售单 {order.Number} 发货后将扣减库存，并按收款状态生成客户应收。",
                        "确认发货");
                    if (expressTrackingNumber is null) return;
                    var shipmentNumber = await _businessService.ShipSalesOrderAsync(order.Id, expressTrackingNumber);
                    OperationMessage = expressTrackingNumber.Length == 0
                        ? $"发货单 {shipmentNumber} 已完成，库存与账款状态已同步"
                        : $"发货单 {shipmentNumber} 已完成，快递单号 {expressTrackingNumber}";
                    break;
                case "已发货":
                    if (!await _dialogService.ShowConfirmAsync("确认客户签收", $"确认客户已收到销售单 {order.Number} 的全部商品？", "确认签收")) return;
                    await _businessService.ConfirmSalesOrderReceiptAsync(order.Id);
                    OperationMessage = $"销售单 {order.Number} 已确认客户签收";
                    break;
                default:
                    throw new InvalidOperationException("当前销售单已经完成，无需继续操作。");
            }
            await RefreshAsync(SelectedCustomer?.Id);
            SelectedShippingOrder = SalesOrders.FirstOrDefault(x => x.Id == selectedOrderId);
        }
        catch (Exception exception)
        {
            OperationMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RecordCustomerPaymentAsync(CustomerReceivableSummary? customerSummary = null)
    {
        if (IsBusy) return;
        try
        {
            var customer = customerSummary is null ? null : Customers.FirstOrDefault(x => x.Id == customerSummary.CustomerId);
            var result = await _dialogService.ShowCustomerPaymentFormAsync(Customers, customer);
            if (result is null) return;
            IsBusy = true;
            var number = await _businessService.RecordCustomerPaymentAsync(new RecordCustomerPaymentRequest(
                result.CustomerId,
                ParseMoney(result.AmountText, "收款金额"),
                result.ReceivedAt,
                result.PaymentMethod,
                result.Note));
            await RefreshAsync(SelectedCustomer?.Id);
            OperationMessage = $"收款单 {number} 已登记并完成欠款核销";
        }
        catch (Exception exception) { OperationMessage = exception.Message; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ShowCustomerStatementAsync(CustomerReceivableSummary customer)
    {
        var lines = Receivables.Where(x => x.CustomerId == customer.CustomerId)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new CustomerStatementLine(
                x.CreatedAt.LocalDateTime.ToString("yyyy-MM-dd"),
                x.DirectionText,
                x.ReferenceNumber,
                x.AmountCents > 0 ? MoneyText.Format(x.AmountCents) : "—",
                x.AmountCents < 0 ? MoneyText.Format(-x.AmountCents) : "—",
                string.IsNullOrWhiteSpace(x.Note) ? x.PaymentMethod : x.Note))
            .ToList();
        await _dialogService.ShowCustomerStatementAsync(new CustomerStatementData(
            customer.Name,
            $"截至 {DateTime.Today:yyyy年M月d日}",
            customer.AmountText,
            lines));
    }

    [RelayCommand]
    private async Task RefreshDataAsync()
    {
        await RefreshAsync(SelectedCustomer?.Id);
        OperationMessage = "业务数据已刷新";
    }

    private async Task InitializeAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _databaseInitializer.InitializeAsync();
            DatabaseStatus = $"本地数据正常 · SQLite {result.SqliteVersion}";
            await RefreshAsync();
            await RestoreSalesDraftAsync();
            _isInitialized = true;
            OperationMessage = "核心业务链已就绪";
        }
        catch (Exception exception)
        {
            DatabaseStatus = "本地数据库异常";
            OperationMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RestoreSalesDraftAsync()
    {
        var draft = await _businessService.GetSalesDraftAsync();
        if (draft is null) return;
        var customer = ActiveCustomers.FirstOrDefault(x => x.Id == draft.CustomerId);
        var warehouse = Warehouses.FirstOrDefault(x => x.Id == draft.WarehouseId);
        if (customer is null || warehouse is null)
        {
            SalesDraftStatusText = "草稿数据已失效";
            return;
        }

        _isRefreshing = true;
        try
        {
            SelectedCustomer = customer;
            SalesCustomerSearchText = customer.DisplayName;
            SelectedWarehouse = warehouse;
            SelectedSalesOrderDate = draft.DocumentDate.LocalDateTime.Date;
            FreightText = (draft.FreightCents / 100m).ToString("0.00", CultureInfo.CurrentCulture);
            DiscountText = (draft.DiscountCents / 100m).ToString("0.00", CultureInfo.CurrentCulture);
            SelectedPaymentStatus = PaymentStatuses.Contains(draft.PaymentStatus) ? draft.PaymentStatus : "未收款";
            SalesEntryRows.Clear();
            RefreshAvailableSalesProducts();

            var skuById = Skus.ToDictionary(x => x.Id);
            var productIds = draft.Lines
                .Where(x => skuById.ContainsKey(x.SkuId))
                .Select(x => skuById[x.SkuId].ProductId)
                .Distinct()
                .ToList();
            foreach (var productId in productIds)
            {
                var product = Products.FirstOrDefault(x => x.Id == productId);
                if (product is not null) AddSalesProductRows(product, includeOutOfStockColors: true);
            }
            foreach (var line in draft.Lines)
            {
                if (!skuById.TryGetValue(line.SkuId, out var sku)) continue;
                var row = SalesEntryRows.FirstOrDefault(x =>
                    x.ProductId == sku.ProductId &&
                    string.Equals(x.SelectedColor, sku.ColorCode, StringComparison.OrdinalIgnoreCase));
                row?.RestoreLine(line.SkuId, line.Quantity, line.UnitPriceCents);
            }
            SalesDraftStatusText = $"已恢复 {draft.UpdatedAt.LocalDateTime:MM-dd HH:mm}";
            OperationMessage = "已恢复本机保存的销售草稿。";
            NotifyCartChanged();
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private async Task OpenCustomerFormAsync(CustomerListItem? customer)
    {
        try
        {
            var result = await _dialogService.ShowCustomerFormAsync(customer);
            if (result is null) return;
            IsBusy = true;
            var customerId = await _businessService.SaveCustomerAsync(new SaveCustomerRequest(
                result.Id,
                result.Code,
                result.Name,
                result.Phone,
                result.Address,
                ParseMoney(result.CreditLimitText, "信用额度"),
                int.Parse(result.PaymentTermDaysText, CultureInfo.InvariantCulture)));
            await RefreshAsync(SelectedCustomer?.Id);
            SelectedManagedCustomer = Customers.FirstOrDefault(x => x.Id == customerId);
            OperationMessage = customer is null ? "客户已新增" : "客户档案已更新";
        }
        catch (Exception exception) { OperationMessage = exception.Message; }
        finally { IsBusy = false; }
    }

    private async Task OpenInventoryFormAsync(InventoryFormMode mode, ProductListItem? product, SkuListItem? sku = null)
    {
        try
        {
            var result = await _dialogService.ShowInventoryFormAsync(mode, Products, Warehouses, Categories, Colors, Skus, StockItems, product, sku);
            if (result is null)
            {
                // 弹窗内的品类、仓库和颜色维护会直接修改数据库，
                // 即使用户取消了当前商品表单，主列表也必须重新加载。
                IsBusy = true;
                await RefreshAsync(SelectedCustomer?.Id);
                return;
            }
            IsBusy = true;
            switch (result.Mode)
            {
                case InventoryFormMode.Product:
                {
                    var categoryList = result.Categories.Append(result.Category).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    await _businessService.SaveCategoriesAsync(categoryList);
                    var savedWarehouses = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
                    foreach (var warehouse in result.Warehouses)
                        savedWarehouses[warehouse.Code] = await _businessService.SaveWarehouseAsync(warehouse.Code, warehouse.Name);
                    var warehouseId = savedWarehouses[result.SelectedWarehouseCode];
                    var productId = await _businessService.SaveProductAsync(new SaveProductRequest(
                        result.ProductId,
                        result.StyleNumber,
                        result.ProductName,
                        result.Category,
                        result.Season,
                        ParseMoney(result.BasePriceText, "统一售价"),
                        ParseMoney(result.CostPriceText, "成本价")));
                    var createdCount = 0;
                    foreach (var variant in result.Variants)
                    {
                        var skuId = await _businessService.AddSkuAsync(new AddSkuRequest(productId, variant.ColorCode, variant.SizeCode, "", warehouseId));
                        if (variant.Quantity > 0)
                            await _businessService.ReceiveStockAsync(new ReceiveStockRequest(warehouseId, skuId, variant.Quantity, "新品初始入库"));
                        createdCount++;
                    }
                    OperationMessage = $"商品已新增，并生成 {createdCount} 个颜色尺码规格";
                    await RefreshAsync(SelectedCustomer?.Id);
                    SelectedManagedProduct = Products.FirstOrDefault(x => x.Id == productId);
                    break;
                }
                case InventoryFormMode.Sku:
                {
                    var warehouseId = result.WarehouseId ?? throw new InvalidOperationException("请选择入库仓库。");
                    var existing = Skus.Where(x => x.ProductId == result.ProductId).Select(x => $"{x.ColorCode}\u001f{x.SizeCode}").ToHashSet(StringComparer.OrdinalIgnoreCase);
                    var addedCount = 0;
                    foreach (var variant in result.Variants)
                    {
                        if (existing.Contains($"{variant.ColorCode}\u001f{variant.SizeCode}")) continue;
                        var skuId = await _businessService.AddSkuAsync(new AddSkuRequest(result.ProductId!.Value, variant.ColorCode, variant.SizeCode, "", warehouseId));
                        if (variant.Quantity > 0)
                            await _businessService.ReceiveStockAsync(new ReceiveStockRequest(warehouseId, skuId, variant.Quantity, "规格补录初始入库"));
                        addedCount++;
                    }
                    if (addedCount == 0) throw new InvalidOperationException("录入的颜色尺码组合已经全部存在。");
                    OperationMessage = $"已新增 {addedCount} 个颜色尺码规格";
                    await RefreshAsync(SelectedCustomer?.Id);
                    break;
                }
                case InventoryFormMode.EditSku:
                {
                    var variant = result.Variants[0];
                    await _businessService.SaveSkuAsync(new SaveSkuRequest(
                        result.SkuId!.Value, variant.ColorCode, variant.SizeCode, result.Barcode));
                    OperationMessage = "尺码规格已更新";
                    await RefreshAsync(SelectedCustomer?.Id);
                    break;
                }
                case InventoryFormMode.Receive:
                {
                    var numbers = new List<string>();
                    foreach (var line in result.StockLines)
                    {
                        numbers.Add(await _businessService.ReceiveStockAsync(new ReceiveStockRequest(
                            result.WarehouseId!.Value, line.SkuId, line.Quantity, result.Note)));
                    }
                    OperationMessage = numbers.Count == 1
                        ? $"入库单 {numbers[0]} 已完成"
                        : $"已完成 {numbers.Count} 条采购入库，单号 {numbers[0]} 起";
                    await RefreshAsync(SelectedCustomer?.Id);
                    break;
                }
                case InventoryFormMode.Adjust:
                {
                    var numbers = new List<string>();
                    foreach (var line in result.StockLines)
                    {
                        numbers.Add(await _businessService.AdjustStockAsync(new AdjustStockRequest(
                            result.WarehouseId!.Value, line.SkuId, line.Quantity, result.Note)));
                    }
                    OperationMessage = numbers.Count == 1
                        ? $"盘点单 {numbers[0]} 已完成"
                        : $"已完成 {numbers.Count} 条库存盘点，单号 {numbers[0]} 起";
                    await RefreshAsync(SelectedCustomer?.Id);
                    break;
                }
            }
        }
        catch (Exception exception) { OperationMessage = exception.Message; }
        finally { IsBusy = false; }
    }

    private async Task RefreshAsync(Guid? customerId = null)
    {
        var selectedCustomerId = customerId ?? SelectedCustomer?.Id;
        var selectedWarehouseId = SelectedWarehouse?.Id;
        var selectedManagedCustomerId = SelectedManagedCustomer?.Id;
        var selectedManagedProductId = SelectedManagedProduct?.Id;
        var selectedStockWarehouseId = SelectedStockWarehouse?.Id;
        var selectedStockSkuId = SelectedStockSku?.Id;
        var selectedShippingOrderId = SelectedShippingOrder?.Id;
        var workspace = await _businessService.GetWorkspaceAsync(selectedCustomerId);

        _isRefreshing = true;
        try
        {
            Replace(Customers, workspace.Customers);
            Replace(ActiveCustomers, workspace.Customers.Where(x => x.IsActive));
            FilterSalesCustomers(SalesCustomerSearchText);
            Replace(Warehouses, workspace.Warehouses);
            Replace(Categories, workspace.Categories);
            Replace(Colors, workspace.Colors);
            Replace(Products, workspace.Products);
            Replace(Skus, workspace.Skus);
            Replace(SalesOrders, workspace.SalesOrders);
            Replace(ShippingOrderLines, workspace.ShippingOrderLines);
            Replace(StockItems, workspace.Stock);
            Replace(StockMovements, workspace.StockMovements);
            Replace(Receivables, workspace.Receivables);
            Replace(OpenReceivables, workspace.OpenReceivables);
            RefreshSalesStatistics(workspace.SalesPeriodSummaries);
            Replace(ReceivableCustomers, workspace.Customers
                .Where(x => x.ReceivableCents > 0)
                .OrderByDescending(x => x.ReceivableCents)
                .Select(x => new CustomerReceivableSummary(
                    x.Id,
                    x.Name,
                    x.ReceivableCents,
                    x.CreditLimitCents,
                    x.LastTransactionText)));
            var warehouseFilter = SelectedInventoryWarehouseFilter;
            Replace(InventoryWarehouseFilters, new[] { "全部仓库" }.Concat(workspace.Stock.Select(x => x.WarehouseName).Distinct().OrderBy(x => x)));
            SelectedInventoryWarehouseFilter = InventoryWarehouseFilters.Contains(warehouseFilter) ? warehouseFilter : "全部仓库";
            var categoryFilter = SelectedInventoryCategoryFilter;
            Replace(InventoryCategoryFilters, new[] { "全部品类" }.Concat(workspace.Products.Select(x => x.Category).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x)));
            SelectedInventoryCategoryFilter = InventoryCategoryFilters.Contains(categoryFilter) ? categoryFilter : "全部品类";
            var seasonFilter = SelectedInventorySeasonFilter;
            Replace(InventorySeasonFilters, new[] { "全部季节" }.Concat(workspace.Products.Select(x => x.Season).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x)));
            SelectedInventorySeasonFilter = InventorySeasonFilters.Contains(seasonFilter) ? seasonFilter : "全部季节";
            RebuildInventorySummaryRows();
            FilterCustomerList(CustomerSearchText);
            FilterStockList(InventorySearchText);
            FilterInventorySummaryRows();

            SelectedCustomer = ActiveCustomers.FirstOrDefault(x => x.Id == selectedCustomerId);
            SalesCustomerSearchText = SelectedCustomer?.DisplayName ?? "";
            FilterSalesCustomers(SalesCustomerSearchText);
            SelectedWarehouse = Warehouses.FirstOrDefault(x => x.Id == selectedWarehouseId) ?? Warehouses.FirstOrDefault();
            RefreshAvailableSalesProducts();
            SelectedSalesProduct = null;
            SalesProductSearchText = "";
            FilterSalesProducts(SalesProductSearchText);
            FilterShippingOrders();
            SelectedShippingOrder = FilteredShippingOrders.FirstOrDefault(x => x.Id == selectedShippingOrderId)
                ?? FilteredShippingOrders.FirstOrDefault();
            SelectedManagedCustomer = Customers.FirstOrDefault(x => x.Id == selectedManagedCustomerId);
            SelectedManagedProduct = Products.FirstOrDefault(x => x.Id == selectedManagedProductId) ?? Products.FirstOrDefault();
            SelectedStockWarehouse = Warehouses.FirstOrDefault(x => x.Id == selectedStockWarehouseId) ?? Warehouses.FirstOrDefault();
            SelectedStockSku = Skus.FirstOrDefault(x => x.Id == selectedStockSkuId) ?? Skus.FirstOrDefault();
            RefreshDashboard(workspace);
            RefreshReceivableAgingRows();
            NotifyCustomerSummaryChanged();
            NotifyPageSummariesChanged();
            if (SelectedManagedCustomer is not null) LoadCustomerEditor(SelectedManagedCustomer);
            if (SelectedManagedProduct is not null) LoadProductEditor(SelectedManagedProduct);
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private void RefreshDashboard(BusinessWorkspace workspace)
    {
        var today = DateTime.Today;
        var todayOrders = workspace.SalesOrders.Where(x => x.DocumentDate.LocalDateTime.Date == today).ToList();
        var todaySupplementalPayments = workspace.Receivables
            .Where(x => x.EntryType == "收款" && x.CreatedAt.LocalDateTime.Date == today)
            .Sum(x => -x.AmountCents);
        var todayCashSales = todayOrders.Where(x => x.PaymentStatus == "已收款").Sum(x => x.TotalAmountCents);
        Replace(Metrics,
        [
            new("今日销售", MoneyText.Format(todayOrders.Sum(x => x.TotalAmountCents)), $"{todayOrders.Sum(x => x.TotalQuantity)} 件 · {todayOrders.Count} 笔", "#9D3037", "#918783"),
            new("今日收款", MoneyText.Format(todayCashSales + todaySupplementalPayments), $"现结 {todayOrders.Count(x => x.PaymentStatus == "已收款")} 笔 · 补收 {workspace.Receivables.Count(x => x.EntryType == "收款" && x.CreatedAt.LocalDateTime.Date == today)} 笔", "#D5CBC5", "#918783"),
            new("客户应收", MoneyText.Format(workspace.OpenReceivables.Sum(x => x.OutstandingCents)), $"{workspace.OpenReceivables.Select(x => x.CustomerId).Distinct().Count()} 位欠款客户", "#D69231", "#C07B24"),
            new("库存总量", workspace.Stock.Sum(x => x.OnHand).ToString("N0"), $"{workspace.Products.Count} 款 · {workspace.Warehouses.Count} 个仓库", "#D5CBC5", "#918783"),
        ]);
        Replace(RecentOrders, workspace.SalesOrders.Take(5).Select((x, index) => new RecentOrder(
            x.Id, index + 1, x.Number, x.CustomerName, x.QuantityText, x.AmountText, x.DisplayStatus, x.BusinessTimeText)));
        var recentOrderIds = workspace.SalesOrders
            .Where(x => x.DocumentDate.LocalDateTime.Date >= today.AddDays(-6))
            .Select(x => x.Id)
            .ToHashSet();
        Replace(TopSellingProducts, workspace.ShippingOrderLines
            .Where(x => recentOrderIds.Contains(x.SalesOrderId))
            .GroupBy(x => new { x.StyleNumber, x.ProductName })
            .Select(x => new TopSellingProduct(x.Key.StyleNumber, x.Key.ProductName, x.Sum(y => y.Quantity)))
            .OrderByDescending(x => x.Quantity)
            .ThenBy(x => x.StyleNumber)
            .Take(3)
            .Select((x, index) => x with { Rank = index + 1 }));
        var tasks = new List<TaskItem>();
        var activeOrders = workspace.SalesOrders.Where(x => x.IsActiveFulfillment).ToList();
        if (activeOrders.Count > 0)
            tasks.Add(new TaskItem("配", $"{activeOrders.Count} 笔订单待处理", $"共 {activeOrders.Sum(x => x.TotalQuantity):N0} 件", "配货发货"));
        var overdue = workspace.OpenReceivables.Where(x => x.IsOverdue).ToList();
        if (overdue.Count > 0)
            tasks.Add(new TaskItem("款", $"{overdue.Select(x => x.CustomerId).Distinct().Count()} 位客户欠款逾期", MoneyText.Format(overdue.Sum(x => x.OutstandingCents)), "应收账款"));
        var dueSoon = workspace.OpenReceivables.Where(x => x.IsDueWithinSevenDays).ToList();
        if (dueSoon.Count > 0)
            tasks.Add(new TaskItem("款", $"{dueSoon.Select(x => x.CustomerId).Distinct().Count()} 位客户 7 天内到期", MoneyText.Format(dueSoon.Sum(x => x.OutstandingCents)), "应收账款"));
        Replace(Tasks, tasks);
        OnPropertyChanged(nameof(DashboardTaskCount));
    }

    private void RefreshSalesStatistics(IReadOnlyList<SalesPeriodSummary> summaries)
    {
        _salesPeriodSummaries = summaries;
        var currentYear = DateTime.Today.Year;
        var selectedYear = SelectedSalesStatisticsYear;
        Replace(SalesStatisticsYears, summaries.Select(x => x.Year).Append(currentYear).Distinct().OrderByDescending(x => x));
        SelectedSalesStatisticsYear = SalesStatisticsYears.Contains(selectedYear) ? selectedYear : currentYear;
        NotifySalesStatisticsChanged();
    }

    private void NotifySalesStatisticsChanged()
    {
        OnPropertyChanged(nameof(SalesStatisticsPeriodText));
        OnPropertyChanged(nameof(SalesStatisticsAmountText));
        OnPropertyChanged(nameof(SalesStatisticsDetailText));
    }

    private void FilterShippingOrders()
    {
        Replace(ShippingStatusFilterOptions, ShippingStatusFilters.Select(x => new ShippingStatusFilterItem(x, x == SelectedShippingStatusFilter)));
        var matches = SelectedShippingStatusFilter == "全部"
            ? SalesOrders
            : SalesOrders.Where(x => x.DisplayStatus == SelectedShippingStatusFilter);
        Replace(FilteredShippingOrders, matches.OrderByDescending(x => x.BusinessTime));
        if (SelectedShippingOrder is not null && !FilteredShippingOrders.Any(x => x.Id == SelectedShippingOrder.Id))
            SelectedShippingOrder = FilteredShippingOrders.FirstOrDefault();
    }

    private void RefreshSelectedShippingOrder()
    {
        Replace(SelectedShippingOrderLines, SelectedShippingOrder is null
            ? []
            : ShippingOrderLines.Where(x => x.SalesOrderId == SelectedShippingOrder.Id));
        var status = SelectedShippingOrder?.DisplayStatus ?? "";
        var completedThrough = status switch
        {
            "待配货" => 2,
            "配货中" => 2,
            "待发货" => 4,
            "已发货" => 5,
            "已签收" => 6,
            _ => 0,
        };
        var currentStep = status switch
        {
            "待配货" or "配货中" => 3,
            "待发货" => 5,
            "已发货" => 6,
            _ => 0,
        };
        string[] labels = ["订单审核", "库存锁定", "拣货", "打包确认", "发货出库", "客户签收"];
        Replace(ShippingWorkflowSteps, labels.Select((label, index) => new ShippingWorkflowStep(
            index + 1,
            label,
            index + 1 <= completedThrough ? "completed" : index + 1 == currentStep ? "current" : "pending")));
        OnPropertyChanged(nameof(HasSelectedShippingOrder));
        OnPropertyChanged(nameof(ShippingOrderDetailSummary));
        OnPropertyChanged(nameof(ShippingPrimaryActionText));
        OnPropertyChanged(nameof(CanExecuteShippingPrimaryAction));
    }

    private void RefreshReceivableAgingRows()
    {
        var total = OpenReceivables.Sum(x => x.OutstandingCents);
        var rows = new[]
        {
            new { Label = "未到期", Amount = OpenReceivables.Where(x => !x.IsOverdue).Sum(x => x.OutstandingCents), Accent = false },
            new { Label = "逾期 1–7 天", Amount = OpenReceivables.Where(x => x.OverdueDays is >= 1 and <= 7).Sum(x => x.OutstandingCents), Accent = false },
            new { Label = "逾期 8–30 天", Amount = OpenReceivables.Where(x => x.OverdueDays is >= 8 and <= 30).Sum(x => x.OutstandingCents), Accent = true },
            new { Label = "逾期 30 天以上", Amount = OpenReceivables.Where(x => x.OverdueDays > 30).Sum(x => x.OutstandingCents), Accent = true },
        };
        Replace(ReceivableAgingRows, rows.Select(x => new ReceivableAgingRow(
            x.Label,
            x.Amount,
            total == 0 ? 0 : Math.Round(x.Amount * 100m / total, 1),
            x.Accent)));
    }

    private void FilterCustomerList(string keyword)
    {
        var normalized = keyword.Trim();
        var searched = string.IsNullOrWhiteSpace(normalized)
            ? Customers
            : Customers.Where(x =>
                x.Code.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                x.Name.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                x.Phone.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                x.Address.Contains(normalized, StringComparison.OrdinalIgnoreCase));
        var matches = SelectedCustomerStatusFilter switch
        {
            "正常客户" => searched.Where(x => x.IsActive),
            "有欠款" => searched.Where(x => x.ReceivableCents > 0),
            "已停用" => searched.Where(x => !x.IsActive),
            _ => searched,
        };
        Replace(FilteredCustomers, matches);
        if (SelectedManagedCustomer is not null && !FilteredCustomers.Any(x => x.Id == SelectedManagedCustomer.Id))
            SelectedManagedCustomer = null;
    }

    private void FilterStockList(string keyword)
    {
        var normalized = keyword.Trim();
        var matches = string.IsNullOrWhiteSpace(normalized)
            ? StockItems
            : StockItems.Where(x =>
                x.StyleNumber.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                x.ProductName.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                x.Variant.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                x.WarehouseName.Contains(normalized, StringComparison.OrdinalIgnoreCase));
        Replace(FilteredStockItems, matches);
    }

    private void RebuildInventorySummaryRows()
    {
        Replace(InventorySummaryRows, BuildInventorySummaryRows(StockItems));
    }

    private void FilterInventorySummaryRows()
    {
        var currentKey = SelectedInventorySummaryRow?.Key;
        var expandedKeys = FilteredInventorySummaryRows.Where(x => x.IsExpanded).Select(x => x.Key).ToHashSet();
        var source = SelectedInventoryWarehouseFilter == "全部仓库"
            ? StockItems
            : StockItems.Where(x => x.WarehouseName == SelectedInventoryWarehouseFilter);
        var rows = BuildInventorySummaryRows(source);
        var keyword = InventorySearchText.Trim();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            rows = rows.Where(x =>
                x.StyleNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                x.ProductName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                x.Category.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                x.Season.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                x.ColorCode.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                x.Details.Any(detail => detail.SizeCode.Contains(keyword, StringComparison.OrdinalIgnoreCase))).ToList();
        }
        if (SelectedInventoryStatusFilter != "全部状态")
            rows = rows.Where(x => x.Status == SelectedInventoryStatusFilter).ToList();
        if (SelectedInventoryCategoryFilter != "全部品类")
            rows = rows.Where(x => x.Category == SelectedInventoryCategoryFilter).ToList();
        if (SelectedInventorySeasonFilter != "全部季节")
            rows = rows.Where(x => x.Season == SelectedInventorySeasonFilter).ToList();

        foreach (var row in rows.Where(x => expandedKeys.Contains(x.Key))) row.IsExpanded = true;

        Replace(FilteredInventorySummaryRows, rows);
        SelectedInventorySummaryRow = FilteredInventorySummaryRows.FirstOrDefault(x => x.Key == currentKey);
    }

    private List<InventorySummaryRow> BuildInventorySummaryRows(IEnumerable<StockListItem> source)
    {
        return source
            .GroupBy(x => new { x.StyleNumber, x.ProductName, x.ColorCode })
            .Select(group =>
            {
                var onHand = group.Sum(x => x.OnHand);
                var reserved = group.Sum(x => x.Reserved);
                var available = onHand - reserved;
                var status = available switch
                {
                    <= 0 => "缺货",
                    <= 5 => "预警",
                    <= 10 => "偏低",
                    _ => "充足",
                };
                var sizes = group.Select(x => x.SizeCode).Distinct()
                    .OrderBy(x => int.TryParse(x, out var number) ? number : int.MaxValue)
                    .ThenBy(x => x)
                    .ToList();
                return new InventorySummaryRow(
                    $"{group.Key.StyleNumber}|{group.Key.ColorCode}",
                    group.Key.StyleNumber,
                    group.Key.ProductName,
                    group.First().Category,
                    group.First().Season,
                    group.Key.ColorCode,
                    string.Join(" / ", sizes),
                    onHand,
                    reserved,
                    available,
                    status,
                    group.GroupBy(x => x.SkuId).Select(skuGroup =>
                    {
                        var first = skuGroup.First();
                        var sku = Skus.FirstOrDefault(x => x.Id == first.SkuId);
                        return new InventorySkuDetailRow(
                            first.SkuId,
                            first.SizeCode,
                            string.Join("、", skuGroup.Select(x => x.WarehouseName).Distinct()),
                            sku?.Barcode ?? "",
                            skuGroup.Sum(x => x.OnHand),
                            skuGroup.Sum(x => x.Reserved));
                    }).OrderBy(x => int.TryParse(x.SizeCode, out var n) ? n : int.MaxValue).ThenBy(x => x.SizeCode).ToList());
            })
            .OrderBy(x => x.StyleNumber)
            .ThenBy(x => x.ColorCode)
            .ToList();
    }

    private void LoadCustomerEditor(CustomerListItem customer)
    {
        CustomerCodeText = customer.Code;
        CustomerNameText = customer.Name;
        CustomerPhoneText = customer.Phone;
        CustomerAddressText = customer.Address;
        CustomerCreditLimitText = (customer.CreditLimitCents / 100m).ToString("0.00");
    }

    private void LoadProductEditor(ProductListItem product)
    {
        ProductStyleNumberText = product.StyleNumber;
        ProductNameText = product.Name;
        ProductCategoryText = product.Category;
        ProductSeasonText = product.Season;
        ProductBasePriceText = (product.BasePriceCents / 100m).ToString("0.00");
        ProductCostPriceText = (product.CostPriceCents / 100m).ToString("0.00");
    }

    private static long ParseMoney(string text, string fieldName)
    {
        if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var value) || value < 0)
            throw new InvalidOperationException($"{fieldName}格式不正确。");
        return checked((long)Math.Round(value * 100m, MidpointRounding.AwayFromZero));
    }

    private static long ParseSalesMoney(string text, string fieldName)
    {
        if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var value) || value < 0)
            throw new InvalidOperationException($"{fieldName}必须是非负金额。");
        return checked((long)Math.Round(value * 100m, MidpointRounding.AwayFromZero));
    }

    private static long TryParseMoney(string text)
    {
        if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var value) || value < 0)
            return 0;
        try { return checked((long)Math.Round(value * 100m, MidpointRounding.AwayFromZero)); }
        catch (OverflowException) { return 0; }
    }

    private static DateTimeOffset ToLocalDateTimeOffset(DateTime value)
        => new(DateTime.SpecifyKind(value.Date, DateTimeKind.Local));

    private void NotifySectionChanged()
    {
        OnPropertyChanged(nameof(IsDashboard));
        OnPropertyChanged(nameof(IsSales));
        OnPropertyChanged(nameof(IsCustomers));
        OnPropertyChanged(nameof(IsStock));
        OnPropertyChanged(nameof(IsShipping));
        OnPropertyChanged(nameof(IsReceivables));
        OnPropertyChanged(nameof(IsOtherSection));
    }

    private void MarkSalesDraftDirty()
    {
        if (_isInitialized && !_isRefreshing)
            SalesDraftStatusText = "有未保存更改";
    }

    private void NotifyCartChanged() => NotifyCartChanged(markDraftDirty: true);

    private void NotifyCartChanged(bool markDraftDirty)
    {
        OnPropertyChanged(nameof(CartQuantity));
        OnPropertyChanged(nameof(CartAmountCents));
        OnPropertyChanged(nameof(CartSummary));
        OnPropertyChanged(nameof(CartQuantityText));
        OnPropertyChanged(nameof(CartAmountText));
        OnPropertyChanged(nameof(FreightCents));
        OnPropertyChanged(nameof(DiscountCents));
        OnPropertyChanged(nameof(PayableAmountCents));
        OnPropertyChanged(nameof(FreightAmountText));
        OnPropertyChanged(nameof(DiscountAmountText));
        OnPropertyChanged(nameof(PayableAmountText));
        OnPropertyChanged(nameof(CurrentReceivedText));
        OnPropertyChanged(nameof(CurrentDebtText));
        OnPropertyChanged(nameof(SalesCreditHint));
        OnPropertyChanged(nameof(SalesCreditHintColor));
        OnPropertyChanged(nameof(HasNoSalesEntryRows));
        if (markDraftDirty) MarkSalesDraftDirty();
    }

    private void NotifyCustomerSummaryChanged()
    {
        OnPropertyChanged(nameof(CustomerTotalText));
        OnPropertyChanged(nameof(CustomerNewThisMonthText));
        OnPropertyChanged(nameof(CustomerMonthlyDealText));
        OnPropertyChanged(nameof(CustomerMonthlyDealRateText));
        OnPropertyChanged(nameof(CustomerReceivableCountText));
        OnPropertyChanged(nameof(CustomerReceivableTotalText));
        OnPropertyChanged(nameof(PendingShipmentCount));
        OnPropertyChanged(nameof(HasPendingShipments));
    }

    private void NotifyPageSummariesChanged()
    {
        OnPropertyChanged(nameof(StockOnHandTotalText));
        OnPropertyChanged(nameof(StockReservedTotalText));
        OnPropertyChanged(nameof(StockAvailableTotalText));
        OnPropertyChanged(nameof(StockValueText));
        OnPropertyChanged(nameof(StockWarningCountText));
        OnPropertyChanged(nameof(StockOutCountText));
        OnPropertyChanged(nameof(StockReservedOrderText));
        OnPropertyChanged(nameof(PendingShipmentQuantityText));
        OnPropertyChanged(nameof(PendingShipmentAmountText));
        OnPropertyChanged(nameof(WaitingPickingCount));
        OnPropertyChanged(nameof(PickingCount));
        OnPropertyChanged(nameof(ReadyToShipCount));
        OnPropertyChanged(nameof(WaitingPickingQuantityText));
        OnPropertyChanged(nameof(PickingQuantityText));
        OnPropertyChanged(nameof(ReadyToShipQuantityText));
        OnPropertyChanged(nameof(ReceivableTotalText));
        OnPropertyChanged(nameof(ReceivableCustomerCountText));
        OnPropertyChanged(nameof(ReceivableCustomerCountLabel));
        OnPropertyChanged(nameof(HasReceivables));
        OnPropertyChanged(nameof(ReceivableEntryCountText));
        OnPropertyChanged(nameof(ReceivableDueSoonText));
        OnPropertyChanged(nameof(ReceivableDueSoonCustomerText));
        OnPropertyChanged(nameof(ReceivableOverdueText));
        OnPropertyChanged(nameof(ReceivableOverdueDetailText));
        OnPropertyChanged(nameof(ReceivableRecoveredText));
        OnPropertyChanged(nameof(TodayShippedCountText));
        OnPropertyChanged(nameof(TodayShippedQuantityText));
        OnPropertyChanged(nameof(SalesOrderNumberText));
        OnPropertyChanged(nameof(SalesOrderDraftText));
        OnPropertyChanged(nameof(SelectedCustomerPendingDebtCents));
        OnPropertyChanged(nameof(SelectedCustomerSalesHint));
        OnPropertyChanged(nameof(SalesCreditHint));
        OnPropertyChanged(nameof(SalesCreditHintColor));
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source) target.Add(item);
    }
}

public sealed partial class SalesEntryRow : ObservableObject
{
    private readonly IReadOnlyList<SkuListItem> _productSkus;
    private readonly Action _changed;
    private bool _isRebuilding;

    public SalesEntryRow(
        Guid productId,
        string styleNumber,
        string productName,
        IReadOnlyList<string> colorOptions,
        IReadOnlyList<SkuListItem> productSkus,
        string selectedColor,
        Action changed,
        Action<SalesEntryRow> remove)
    {
        ProductId = productId;
        StyleNumber = styleNumber;
        ProductName = productName;
        ColorOptions = colorOptions;
        _productSkus = productSkus;
        _changed = changed;
        RemoveCommand = new RelayCommand(() => remove(this));
        _selectedColor = selectedColor;
        RebuildSizes();
    }

    public Guid ProductId { get; }
    public string StyleNumber { get; }
    public string ProductName { get; }
    public IReadOnlyList<string> ColorOptions { get; }
    public ObservableCollection<SalesSizeEntryCell> SizeEntries { get; } = [];
    public IRelayCommand RemoveCommand { get; }

    [ObservableProperty] private string _selectedColor;
    [ObservableProperty] private string _unitPriceText = "0.00";

    public long UnitPriceCents
    {
        get
        {
            if (!decimal.TryParse(UnitPriceText, NumberStyles.Number, CultureInfo.CurrentCulture, out var value) || value < 0)
                return 0;
            try { return checked((long)Math.Round(value * 100m, MidpointRounding.AwayFromZero)); }
            catch (OverflowException) { return 0; }
        }
    }
    public long TotalQuantity => SizeEntries.Sum(x => x.Quantity);
    public long AmountCents => checked(TotalQuantity * UnitPriceCents);
    public string TotalQuantityText => $"{TotalQuantity} 件";
    public string AmountText => MoneyText.Format(AmountCents);

    partial void OnSelectedColorChanged(string value)
    {
        if (_isRebuilding) return;
        RebuildSizes();
    }

    partial void OnUnitPriceTextChanged(string value) => NotifyTotalsChanged();

    public IEnumerable<CreateSalesOrderLine> BuildOrderLines()
    {
        var populatedCells = SizeEntries.Where(x => !string.IsNullOrWhiteSpace(x.QuantityText)).ToList();
        foreach (var cell in populatedCells) cell.Validate();
        var salesCells = populatedCells.Where(x => x.Quantity > 0).ToList();
        if (salesCells.Count == 0) return [];
        if (UnitPriceCents <= 0) throw new InvalidOperationException($"款号 {StyleNumber} 的销售单价必须大于零。");
        return salesCells.Select(x => new CreateSalesOrderLine(x.SkuId, x.Quantity, UnitPriceCents)).ToList();
    }

    public void RestoreLine(Guid skuId, long quantity, long? unitPriceCents)
    {
        var cell = SizeEntries.FirstOrDefault(x => x.SkuId == skuId);
        if (cell is null) return;
        cell.QuantityText = quantity > 0 ? quantity.ToString(CultureInfo.InvariantCulture) : "";
        if (unitPriceCents is > 0)
            UnitPriceText = (unitPriceCents.Value / 100m).ToString("0.00", CultureInfo.CurrentCulture);
    }

    private void RebuildSizes()
    {
        _isRebuilding = true;
        try
        {
            SizeEntries.Clear();
            var colorSkus = _productSkus
                .Where(x => string.Equals(x.ColorCode, SelectedColor, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => int.TryParse(x.SizeCode, out var number) ? number : int.MaxValue)
                .ThenBy(x => x.SizeCode)
                .ToList();
            foreach (var sku in colorSkus)
                SizeEntries.Add(new SalesSizeEntryCell(sku.Id, sku.SizeCode, sku.Available, NotifyTotalsChanged));

            var priceCents = colorSkus.FirstOrDefault()?.EffectivePriceCents ?? 0;
            UnitPriceText = (priceCents / 100m).ToString("0.00", CultureInfo.CurrentCulture);
        }
        finally
        {
            _isRebuilding = false;
        }
        NotifyTotalsChanged();
    }

    private void NotifyTotalsChanged()
    {
        OnPropertyChanged(nameof(UnitPriceCents));
        OnPropertyChanged(nameof(TotalQuantity));
        OnPropertyChanged(nameof(TotalQuantityText));
        OnPropertyChanged(nameof(AmountCents));
        OnPropertyChanged(nameof(AmountText));
        _changed();
    }
}

public sealed partial class SalesSizeEntryCell : ObservableObject
{
    private readonly Action _changed;

    public SalesSizeEntryCell(Guid skuId, string sizeCode, long available, Action changed)
    {
        SkuId = skuId;
        SizeCode = sizeCode;
        Available = available;
        _changed = changed;
    }

    public Guid SkuId { get; }
    public string SizeCode { get; }
    public long Available { get; }
    public string AvailableText => $"可用 {Available:N0}";
    [ObservableProperty] private string _quantityText = "";
    public long Quantity => long.TryParse(QuantityText, out var value) && value > 0 ? value : 0;

    partial void OnQuantityTextChanged(string value)
    {
        OnPropertyChanged(nameof(Quantity));
        _changed();
    }

    public void Validate()
    {
        if (!long.TryParse(QuantityText, out var value) || value < 0)
            throw new InvalidOperationException($"尺码 {SizeCode} 的数量必须是非负整数。");
        if (value > Available)
            throw new InvalidOperationException($"尺码 {SizeCode} 可用库存只有 {Available} 件。");
    }
}

public sealed partial class InventorySummaryRow : ObservableObject
{
    public InventorySummaryRow(
        string key,
        string styleNumber,
        string productName,
        string category,
        string season,
        string colorCode,
        string sizesText,
        long onHand,
        long reserved,
        long available,
        string status,
        IReadOnlyList<InventorySkuDetailRow> details)
    {
        Key = key;
        StyleNumber = styleNumber;
        ProductName = productName;
        Category = category;
        Season = season;
        ColorCode = colorCode;
        SizesText = sizesText;
        OnHand = onHand;
        Reserved = reserved;
        Available = available;
        Status = status;
        Details = details;
    }

    public string Key { get; }
    public string StyleNumber { get; }
    public string ProductName { get; }
    public string Category { get; }
    public string Season { get; }
    public string ColorCode { get; }
    public string SizesText { get; }
    public long OnHand { get; }
    public long Reserved { get; }
    public long Available { get; }
    public string Status { get; }
    public IReadOnlyList<InventorySkuDetailRow> Details { get; }
    [ObservableProperty] private bool _isExpanded;
    public string ExpandGlyph => IsExpanded ? "⌄" : "›";
    partial void OnIsExpandedChanged(bool value) => OnPropertyChanged(nameof(ExpandGlyph));

    public string ColorDotBrush => ColorCode switch
    {
        var value when value.Contains("黑", StringComparison.OrdinalIgnoreCase) => "#292321",
        var value when value.Contains("红", StringComparison.OrdinalIgnoreCase) => "#9D3037",
        var value when value.Contains("蓝", StringComparison.OrdinalIgnoreCase) => "#93B3C5",
        var value when value.Contains("卡其", StringComparison.OrdinalIgnoreCase) => "#B7A277",
        var value when value.Contains("米", StringComparison.OrdinalIgnoreCase) => "#E6DCC4",
        _ => "#A89B93",
    };

    public string StatusBackground => Status switch
    {
        "充足" => "#E7F3ED",
        "偏低" => "#FFF1DA",
        "预警" => "#FAE8E9",
        _ => "#ECE9E7",
    };

    public string StatusForeground => Status switch
    {
        "充足" => "#3D8C69",
        "偏低" => "#B77720",
        "预警" => "#B44449",
        _ => "#817773",
    };
}

public sealed record InventorySkuDetailRow(
    Guid SkuId,
    string SizeCode,
    string WarehouseText,
    string Barcode,
    long OnHand,
    long Reserved)
{
    public long Available => OnHand - Reserved;
    public string BarcodeText => string.IsNullOrWhiteSpace(Barcode) ? "未设置条码" : Barcode;
}

public sealed record MetricCard(string Label, string Value, string Detail, string AccentBrush, string DetailBrush);

public sealed record RecentOrder(Guid OrderId, int Rank, string Number, string Customer, string Quantity, string Amount, string Status, string TimeText)
{
    public string Initial => string.IsNullOrWhiteSpace(Customer) ? "单" : Customer[..1];
    public int PerformancePercent => Math.Max(22, 100 - ((Rank - 1) * 18));
    public string DisplayStatus => Status switch
    {
        "已锁库" => "待配货",
        "已发货" => "已发货",
        _ => Status,
    };
    public string StatusBackground => Status is "已发货" or "已签收" ? "#E7F3ED" : Rank % 2 == 0 ? "#E8F0F8" : "#FAE8E9";
    public string StatusForeground => Status is "已发货" or "已签收" ? "#318A65" : Rank % 2 == 0 ? "#477EA4" : "#9D3037";
}

public sealed record TopSellingProduct(string StyleNumber, string ProductName, long Quantity)
{
    public int Rank { get; init; }
    public string QuantityText => $"{Quantity:N0} 件";
    public int PerformancePercent => Math.Max(25, 100 - ((Rank - 1) * 22));
}

public sealed record TaskItem(string Glyph, string Title, string Detail, string TargetSection)
{
    public string BackgroundBrush => Glyph switch
    {
        "发" => "#FFF1DA",
        "款" => "#E8F0F8",
        _ => "#FAE8E9",
    };
    public string ForegroundBrush => Glyph switch
    {
        "发" => "#B77720",
        "款" => "#477EA4",
        _ => "#9D3037",
    };
}

public sealed record CustomerReceivableSummary(Guid CustomerId, string Name, long AmountCents, long CreditLimitCents, string LastTransactionText)
{
    public string Initial => string.IsNullOrWhiteSpace(Name) ? "客" : Name[..1];
    public string AmountText => MoneyText.Format(AmountCents);
    public string CreditText => $"额度 {MoneyText.Format(CreditLimitCents)}";
}

public sealed record ShippingWorkflowStep(int Number, string Label, string State)
{
    public string MarkerText => State == "completed" ? "✓" : Number.ToString();
    public string Background => State switch { "completed" => "#318A65", "current" => "#9D3037", _ => "#ECE8E5" };
    public string Foreground => State == "pending" ? "#9E938E" : "White";
    public string LabelForeground => State == "current" ? "#9D3037" : "#817773";
}

public sealed record ShippingStatusFilterItem(string Name, bool IsSelected);

public sealed record ReceivableAgingRow(string Label, long AmountCents, decimal Percent, bool IsAccent)
{
    public string AmountText => MoneyText.Format(AmountCents);
    public string PercentText => $"{Percent:0.#}%";
    public string ProgressBrush => IsAccent ? "#C9852D" : "#9D3037";
}
