using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shanquan.Inventory.Application.CoreBusiness;
using Shanquan.Inventory.Desktop.Services;

namespace Shanquan.Inventory.Desktop.ViewModels.Dialogs;

public sealed partial class InventoryFormViewModel : ViewModelBase
{
    private readonly ICoreBusinessService? _businessService;
    private readonly InventoryFormMode _mode;
    private readonly IReadOnlyList<SkuListItem> _skus;
    private readonly IReadOnlyList<StockListItem> _stock;

    public InventoryFormViewModel(
        ICoreBusinessService? businessService,
        InventoryFormMode mode,
        IReadOnlyList<ProductListItem> products,
        IReadOnlyList<WarehouseListItem> warehouses,
        IReadOnlyList<string> categories,
        IReadOnlyList<ColorListItem> colors,
        IReadOnlyList<SkuListItem> skus,
        IReadOnlyList<StockListItem> stock,
        ProductListItem? product,
        SkuListItem? sku)
    {
        _businessService = businessService;
        _mode = mode;
        _skus = skus;
        _stock = stock;
        Products = new ObservableCollection<ProductListItem>(products);
        FilteredProducts = new ObservableCollection<ProductListItem>(products);
        FilteredSkus = new ObservableCollection<SkuListItem>(skus);
        Categories = new ObservableCollection<string>(categories.Distinct(StringComparer.OrdinalIgnoreCase));
        Seasons = ["春季", "夏季", "秋季", "冬季"];
        ColorOptions = new ObservableCollection<ColorChoice>(colors.Select(x => new ColorChoice(x.Id, x.DisplayName, x.Value, x.Hex)));
        SizeOptions = ["均码", "XS", "S", "M", "L", "XL", "2XL", "3XL", "25", "26", "27", "28", "29", "30", "31", "32", "33", "34", "35"];

        foreach (var warehouse in warehouses)
            WarehouseEntries.Add(new WarehouseDraft(warehouse.Id, warehouse.Code, warehouse.Name));
        SelectedWarehouseEntry = WarehouseEntries.FirstOrDefault();

        SelectedProduct = product ?? (sku is null ? null : Products.FirstOrDefault(x => x.Id == sku.ProductId));
        ProductSearchText = SelectedProduct?.DisplayName ?? "";
        SelectedSku = mode is InventoryFormMode.Receive or InventoryFormMode.Adjust
            ? null
            : sku ?? FilteredSkus.FirstOrDefault();
        ProductId = product?.Id;
        StyleNumber = product?.StyleNumber ?? "";
        ProductName = product?.Name ?? "";
        SelectedCategory = product?.Category ?? Categories.FirstOrDefault() ?? "";
        SelectedSeason = Seasons.Contains(product?.Season ?? "") ? product!.Season : Seasons.First();
        QuantityText = mode == InventoryFormMode.Adjust ? "0" : "1";
        Note = mode == InventoryFormMode.Adjust ? "库存盘点" : "采购到货";
        BasePriceText = ((product?.BasePriceCents ?? 0) / 100m).ToString("0.00");
        CostPriceText = ((product?.CostPriceCents ?? 0) / 100m).ToString("0.00");

        var variant = new ProductVariantEntry(ColorOptions, SizeOptions);
        if (sku is not null)
        {
            variant.ColorCode = sku.ColorCode;
            variant.SizeCode = sku.SizeCode;
            variant.SelectedColor = ColorOptions.FirstOrDefault(x => x.Value == sku.ColorCode);
            Barcode = sku.Barcode;
        }
        VariantEntries.Add(variant);

        (Title, Description, SaveText) = mode switch
        {
            InventoryFormMode.Product => ("新增商品", "一次录入商品主档、仓库及多个颜色尺码规格。", "保存商品及规格"),
            InventoryFormMode.Sku => ("新增规格", "搜索已有款号，逐行补录颜色、尺码与初始数量。", "保存新增规格"),
            InventoryFormMode.EditSku => ("编辑尺码规格", "修改当前商品规格的颜色、尺码或条码。", "保存规格修改"),
            InventoryFormMode.Receive => ("采购入库", "选择仓库和款号，按本款颜色尺码录入本次入库件数。", "确认入库"),
            _ => ("库存盘点", "选择仓库和款号，按本款颜色尺码录入盘点后的实际库存。", "确认调整"),
        };

        RebuildStockRows();
    }

    public string Title { get; }
    public string Description { get; }
    public string SaveText { get; }
    public double DialogHeight => IsProduct ? 740 : IsSku ? 700 : IsEditSku ? 560 : 760;
    public bool IsProduct => _mode == InventoryFormMode.Product;
    public bool IsSku => _mode == InventoryFormMode.Sku;
    public bool IsEditSku => _mode == InventoryFormMode.EditSku;
    public bool IsVariantForm => IsSku || IsEditSku;
    public bool IsStock => _mode is InventoryFormMode.Receive or InventoryFormMode.Adjust;
    public bool IsAdjust => _mode == InventoryFormMode.Adjust;
    public bool ShowsWarehouseEditor => IsProduct;
    public bool ShowsWarehouseSelector => IsProduct || IsSku || IsStock;
    public bool ShowsVariantQuantity => IsProduct || IsSku;
    public string QuantityLabel => IsAdjust ? "盘点后库存" : "入库数量";
    public string StockInputLabel => IsAdjust ? "盘后库存" : "本次入库";
    public string StockNoteLabel => IsAdjust ? "盘点原因  *" : "备注（可选）";
    public string StockEmptyText => StockEmptyMessage;
    public string StockHintText => IsAdjust
        ? "只提交与当前实际库存有差额的规格；盘后库存不能低于已锁定件数。"
        : "只提交本次入库数量大于 0 的规格，未填写的颜色尺码不会入库。";
    public ObservableCollection<ProductListItem> Products { get; }
    public ObservableCollection<ProductListItem> FilteredProducts { get; }
    public ObservableCollection<SkuListItem> FilteredSkus { get; }
    public ObservableCollection<string> Categories { get; }
    public ObservableCollection<WarehouseDraft> WarehouseEntries { get; } = [];
    public ObservableCollection<ProductVariantEntry> VariantEntries { get; } = [];
    public ObservableCollection<StockSkuLine> StockRows { get; } = [];
    public bool HasStockRows => StockRows.Count > 0;
    public bool HasNoStockRows => IsStock && StockRows.Count == 0;
    public IReadOnlyList<string> Seasons { get; }
    public ObservableCollection<ColorChoice> ColorOptions { get; }
    public IReadOnlyList<string> SizeOptions { get; }
    public Guid? ProductId { get; }
    public string VariantCountText => $"已添加 {VariantEntries.Count} 条规格，可继续添加";
    public bool HasNoFilteredProducts => FilteredProducts.Count == 0;

    [ObservableProperty] private ProductListItem? _selectedProduct;
    [ObservableProperty] private WarehouseDraft? _selectedWarehouseEntry;
    [ObservableProperty] private SkuListItem? _selectedSku;
    [ObservableProperty] private string _productSearchText = "";
    [ObservableProperty] private string _skuSearchText = "";
    [ObservableProperty] private string _styleNumber;
    [ObservableProperty] private string _productName;
    [ObservableProperty] private string _selectedCategory;
    [ObservableProperty] private string _selectedSeason;
    [ObservableProperty] private string _basePriceText;
    [ObservableProperty] private string _costPriceText;
    [ObservableProperty] private string _barcode = "";
    [ObservableProperty] private string _quantityText;
    [ObservableProperty] private string _note;
    [ObservableProperty] private string _errorMessage = "";
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));
    partial void OnSelectedProductChanged(ProductListItem? value)
    {
        if (IsStock) RebuildStockRows();
    }
    partial void OnSelectedWarehouseEntryChanged(WarehouseDraft? value)
    {
        if (IsStock) RebuildStockRows();
    }

    public int FilterProducts(string? searchText)
    {
        var terms = (searchText ?? "")
            .Replace('·', ' ')
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var matches = Products
            .Where(product => terms.Length == 0 || terms.All(term =>
                product.StyleNumber.Contains(term, StringComparison.OrdinalIgnoreCase)
                || product.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || product.Category.Contains(term, StringComparison.OrdinalIgnoreCase)
                || product.Season.Contains(term, StringComparison.OrdinalIgnoreCase)))
            .Take(100)
            .ToList();

        FilteredProducts.Clear();
        foreach (var product in matches) FilteredProducts.Add(product);
        OnPropertyChanged(nameof(HasNoFilteredProducts));
        return matches.Count;
    }

    [RelayCommand]
    private void AddVariant()
    {
        VariantEntries.Add(new ProductVariantEntry(ColorOptions, SizeOptions));
        OnPropertyChanged(nameof(VariantCountText));
    }

    public void RemoveVariant(ProductVariantEntry entry)
    {
        if (VariantEntries.Count > 1) VariantEntries.Remove(entry);
        else
        {
            entry.ColorCode = "";
            entry.SizeCode = "";
            entry.Quantity = 0;
        }
        OnPropertyChanged(nameof(VariantCountText));
    }

    public void IncrementVariant(ProductVariantEntry entry) => entry.Quantity++;
    public void DecrementVariant(ProductVariantEntry entry)
    {
        if (entry.Quantity > 0) entry.Quantity--;
    }

    public async Task RefreshWarehousesAsync()
    {
        if (_businessService is null) return;
        try
        {
            var selectedId = SelectedWarehouseEntry?.Id;
            var workspace = await _businessService.GetWorkspaceAsync();
            WarehouseEntries.Clear();
            foreach (var warehouse in workspace.Warehouses)
                WarehouseEntries.Add(new WarehouseDraft(warehouse.Id, warehouse.Code, warehouse.Name));
            SelectedWarehouseEntry = WarehouseEntries.FirstOrDefault(x => x.Id == selectedId) ?? WarehouseEntries.FirstOrDefault();
            RebuildStockRows();
            ErrorMessage = "";
        }
        catch (Exception exception) { ErrorMessage = exception.Message; }
    }

    public async Task RefreshCategoriesAsync()
    {
        if (_businessService is null) return;
        try
        {
            var selected = SelectedCategory;
            var workspace = await _businessService.GetWorkspaceAsync();
            Categories.Clear();
            foreach (var category in workspace.Categories) Categories.Add(category);
            SelectedCategory = Categories.Contains(selected, StringComparer.OrdinalIgnoreCase)
                ? Categories.First(x => string.Equals(x, selected, StringComparison.OrdinalIgnoreCase))
                : Categories.FirstOrDefault() ?? "";
            ErrorMessage = "";
        }
        catch (Exception exception) { ErrorMessage = exception.Message; }
    }

    public async Task RefreshColorsAsync()
    {
        if (_businessService is null) return;
        try
        {
            var workspace = await _businessService.GetWorkspaceAsync();
            ColorOptions.Clear();
            foreach (var color in workspace.Colors)
                ColorOptions.Add(new ColorChoice(color.Id, color.DisplayName, color.Value, color.Hex));
        }
        catch (Exception exception) { ErrorMessage = exception.Message; }
    }

    public InventoryFormData? BuildResult()
    {
        var variants = VariantEntries.Select(x => new InventoryVariantFormData(x.ColorCode.Trim(), x.SizeCode.Trim(), x.Quantity)).ToList();
        if (IsProduct)
        {
            if (string.IsNullOrWhiteSpace(StyleNumber)) return Fail("款号不能为空。");
            if (string.IsNullOrWhiteSpace(ProductName)) return Fail("商品名称不能为空。");
            if (string.IsNullOrWhiteSpace(SelectedCategory)) return Fail("请选择商品品类。");
            if (string.IsNullOrWhiteSpace(SelectedSeason)) return Fail("请选择商品季节。");
            if (!decimal.TryParse(BasePriceText, out var price) || price <= 0) return Fail("统一售价必须大于零。");
            if (!decimal.TryParse(CostPriceText, out var costPrice) || costPrice <= 0) return Fail("成本价必须大于零。");
        }
        if (IsSku && SelectedProduct is null) return Fail("请选择商品款号。");
        if ((IsProduct || IsSku) && SelectedWarehouseEntry is null) return Fail("请选择入库仓库。");
        if ((IsProduct || IsSku) && WarehouseEntries.Any(x => string.IsNullOrWhiteSpace(x.Code) || string.IsNullOrWhiteSpace(x.Name)))
            return Fail("请完整填写仓库编号和仓库名称。");
        if (IsProduct || IsSku || IsEditSku)
        {
            if (variants.Any(x => string.IsNullOrWhiteSpace(x.ColorCode) || string.IsNullOrWhiteSpace(x.SizeCode)))
                return Fail("每条规格都必须选择或输入颜色和尺码。");
            if (variants.GroupBy(x => $"{x.ColorCode}\u001f{x.SizeCode}", StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1))
                return Fail("存在重复的颜色和尺码组合。");
        }
        var stockLines = new List<InventoryStockLineFormData>();
        if (IsStock)
        {
            if (SelectedWarehouseEntry?.Id is null) return Fail("请选择已有仓库。");
            if (SelectedProduct is null) return Fail("请选择商品款号。");
            if (StockRows.Count == 0) return Fail(StockEmptyMessage);
            foreach (var row in StockRows)
            {
                if (!long.TryParse(row.QuantityText, out var quantity) || quantity < 0)
                    return Fail($"{row.ColorCode}/{row.SizeCode} 的数量必须是非负整数。");
                if (IsAdjust && quantity < row.Reserved)
                    return Fail($"{row.ColorCode}/{row.SizeCode} 盘后库存不能低于已锁定 {row.Reserved} 件。");
                row.Quantity = quantity;
                if (IsAdjust)
                {
                    if (quantity != row.OnHand)
                        stockLines.Add(new InventoryStockLineFormData(row.SkuId, quantity));
                }
                else if (quantity > 0)
                    stockLines.Add(new InventoryStockLineFormData(row.SkuId, quantity));
            }
            if (stockLines.Count == 0)
                return Fail(IsAdjust ? "盘点数量与当前库存相同，无需调整。" : "请至少录入一个规格的入库数量。");
            if (IsAdjust && string.IsNullOrWhiteSpace(Note)) return Fail("盘点必须填写原因。");
        }

        ErrorMessage = "";
        return new InventoryFormData(
            _mode,
            IsProduct ? ProductId : SelectedProduct?.Id,
            SelectedWarehouseEntry?.Id,
            IsEditSku ? SelectedSku?.Id : stockLines.FirstOrDefault()?.SkuId,
            StyleNumber.Trim(), ProductName.Trim(), SelectedCategory.Trim(), SelectedSeason.Trim(), BasePriceText.Trim(), CostPriceText.Trim(),
            variants, Barcode.Trim(), QuantityText.Trim(), Note.Trim(), SelectedWarehouseEntry?.Code.Trim() ?? "",
            WarehouseEntries.Select(x => new WarehouseFormData(x.Id, x.Code.Trim(), x.Name.Trim())).ToList(),
            Categories.ToList(),
            stockLines);
    }

    public void IncrementStockRow(StockSkuLine row) => row.Quantity++;
    public void DecrementStockRow(StockSkuLine row)
    {
        if (row.Quantity > 0) row.Quantity--;
    }

    private string StockEmptyMessage
    {
        get
        {
            if (SelectedWarehouseEntry?.Id is null) return "请选择仓库。";
            if (SelectedProduct is null) return "请搜索并选择商品款号。";
            if (!_skus.Any(x => x.ProductId == SelectedProduct.Id))
                return "该款还没有颜色尺码规格，请先新增规格。";
            return IsAdjust
                ? "该仓还没有此商品库存，请先办理采购入库。"
                : "该款还没有可入库的颜色尺码规格。";
        }
    }

    private void RebuildStockRows()
    {
        StockRows.Clear();
        if (!IsStock || SelectedProduct is null || SelectedWarehouseEntry?.Id is null)
        {
            NotifyStockRowsChanged();
            return;
        }

        var warehouseId = SelectedWarehouseEntry.Id.Value;
        var productSkus = _skus
            .Where(x => x.ProductId == SelectedProduct.Id)
            .OrderBy(x => x.ColorCode)
            .ThenBy(x => int.TryParse(x.SizeCode, out var number) ? number : int.MaxValue)
            .ThenBy(x => x.SizeCode)
            .ToList();
        foreach (var sku in productSkus)
        {
            var balance = _stock.FirstOrDefault(x => x.SkuId == sku.Id && x.WarehouseId == warehouseId);
            if (IsAdjust && balance is null) continue;
            var onHand = balance?.OnHand ?? 0;
            var reserved = balance?.Reserved ?? 0;
            StockRows.Add(new StockSkuLine(sku.Id, sku.ColorCode, sku.SizeCode, onHand, reserved, IsAdjust ? onHand : 0, IsAdjust));
        }
        NotifyStockRowsChanged();
    }

    private void NotifyStockRowsChanged()
    {
        OnPropertyChanged(nameof(HasStockRows));
        OnPropertyChanged(nameof(HasNoStockRows));
        OnPropertyChanged(nameof(StockEmptyText));
    }

    private InventoryFormData? Fail(string message)
    {
        ErrorMessage = message;
        return null;
    }

}

public sealed partial class WarehouseDraft : ObservableObject
{
    public WarehouseDraft(Guid? id, string code, string name)
    {
        Id = id;
        Code = code;
        Name = name;
    }
    [ObservableProperty] private Guid? _id;
    [ObservableProperty] private string _code;
    [ObservableProperty] private string _name;
    public string DisplayName => $"{Code} · {Name}";
    partial void OnCodeChanged(string value) => OnPropertyChanged(nameof(DisplayName));
    partial void OnNameChanged(string value) => OnPropertyChanged(nameof(DisplayName));
}

public sealed partial class ProductVariantEntry : ObservableObject
{
    public ProductVariantEntry(IReadOnlyList<ColorChoice> colorOptions, IReadOnlyList<string> sizeOptions)
    {
        ColorOptions = colorOptions;
        SizeOptions = sizeOptions;
    }
    public IReadOnlyList<ColorChoice> ColorOptions { get; }
    public IReadOnlyList<string> SizeOptions { get; }
    [ObservableProperty] private ColorChoice? _selectedColor;
    [ObservableProperty] private string _colorCode = "";
    [ObservableProperty] private string _sizeCode = "";
    private long _quantity;
    private string _quantityText = "0";

    public long Quantity
    {
        get => _quantity;
        set
        {
            value = Math.Max(0, value);
            if (!SetProperty(ref _quantity, value)) return;
            _quantityText = value.ToString();
            OnPropertyChanged(nameof(QuantityText));
        }
    }

    public string QuantityText
    {
        get => _quantityText;
        set
        {
            if (!SetProperty(ref _quantityText, value)) return;
            var quantity = long.TryParse(value, out var parsed) && parsed >= 0 ? parsed : 0;
            if (_quantity == quantity) return;
            _quantity = quantity;
            OnPropertyChanged(nameof(Quantity));
        }
    }
    partial void OnSelectedColorChanged(ColorChoice? value)
    {
        if (value is not null) ColorCode = value.Value;
    }
}

public sealed partial class StockSkuLine : ObservableObject
{
    public StockSkuLine(Guid skuId, string colorCode, string sizeCode, long onHand, long reserved, long quantity, bool isAdjust)
    {
        SkuId = skuId;
        ColorCode = colorCode;
        SizeCode = sizeCode;
        OnHand = onHand;
        Reserved = reserved;
        _isAdjust = isAdjust;
        Quantity = quantity;
    }

    private readonly bool _isAdjust;

    public Guid SkuId { get; }
    public string ColorCode { get; }
    public string SizeCode { get; }
    public long OnHand { get; }
    public long Reserved { get; }
    public long Available => OnHand - Reserved;

    private long _quantity;
    private string _quantityText = "0";

    public long Quantity
    {
        get => _quantity;
        set
        {
            value = Math.Max(0, value);
            if (!SetProperty(ref _quantity, value)) return;
            _quantityText = value.ToString();
            OnPropertyChanged(nameof(QuantityText));
            OnPropertyChanged(nameof(ChangeText));
        }
    }

    public string QuantityText
    {
        get => _quantityText;
        set
        {
            if (!SetProperty(ref _quantityText, value)) return;
            var quantity = long.TryParse(value, out var parsed) && parsed >= 0 ? parsed : 0;
            if (_quantity == quantity) return;
            _quantity = quantity;
            OnPropertyChanged(nameof(Quantity));
            OnPropertyChanged(nameof(ChangeText));
        }
    }

    public string ChangeText
    {
        get
        {
            if (!_isAdjust)
                return Quantity == 0 ? "—" : $"+{Quantity}";
            var change = Quantity - OnHand;
            if (change == 0) return "—";
            return change > 0 ? $"+{change}" : change.ToString();
        }
    }
}

public sealed record ColorChoice(Guid Id, string DisplayName, string Value, string Swatch)
{
    public override string ToString() => DisplayName;
}
