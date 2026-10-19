using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shanquan.Inventory.Application.CoreBusiness;

namespace Shanquan.Inventory.Desktop.ViewModels.Dialogs;

public sealed partial class WarehouseManagementViewModel : ViewModelBase
{
    private readonly ICoreBusinessService _businessService;

    public WarehouseManagementViewModel(ICoreBusinessService businessService, IReadOnlyList<WarehouseListItem> warehouses)
    {
        _businessService = businessService;
        ReplaceWarehouses(warehouses);
    }

    public ObservableCollection<WarehouseManagementItem> Warehouses { get; } = [];
    public ObservableCollection<WarehouseManagementItem> FilteredWarehouses { get; } = [];
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string _newCode = "";
    [ObservableProperty] private string _newName = "";
    [ObservableProperty] private string _statusMessage = "可在此新增、查找、编辑或删除仓库。";

    public bool HasNoResults => FilteredWarehouses.Count == 0;
    public string ResultSummary => $"共 {FilteredWarehouses.Count} 个仓库";
    partial void OnSearchTextChanged(string value) => ApplyFilter();

    [RelayCommand]
    private async Task AddWarehouseAsync()
    {
        try
        {
            var id = await _businessService.SaveWarehouseAsync(null, NewCode, NewName);
            var addedName = NewName.Trim();
            NewCode = "";
            NewName = "";
            SearchText = "";
            await RefreshAsync();
            StatusMessage = $"已新增仓库“{addedName}”。";
        }
        catch (Exception exception) { StatusMessage = exception.Message; }
    }

    public void BeginEdit(WarehouseManagementItem item)
    {
        foreach (var warehouse in Warehouses.Where(x => x != item)) warehouse.IsEditing = false;
        item.EditCode = item.Code;
        item.EditName = item.Name;
        item.IsEditing = true;
        StatusMessage = $"正在编辑仓库“{item.Name}”。";
    }

    public void CancelEdit(WarehouseManagementItem item)
    {
        item.EditCode = item.Code;
        item.EditName = item.Name;
        item.IsEditing = false;
        StatusMessage = "已取消编辑。";
    }

    public async Task SaveEditAsync(WarehouseManagementItem item)
    {
        try
        {
            await _businessService.SaveWarehouseAsync(item.Id, item.EditCode, item.EditName);
            await RefreshAsync();
            StatusMessage = "仓库信息已更新。";
        }
        catch (Exception exception) { StatusMessage = exception.Message; }
    }

    public async Task DeleteAsync(WarehouseManagementItem item)
    {
        try
        {
            await _businessService.DeleteWarehouseAsync(item.Id);
            await RefreshAsync();
            StatusMessage = $"已删除仓库“{item.Name}”。";
        }
        catch (Exception exception) { StatusMessage = exception.Message; }
    }

    private async Task RefreshAsync()
    {
        var workspace = await _businessService.GetWorkspaceAsync();
        ReplaceWarehouses(workspace.Warehouses);
    }

    private void ReplaceWarehouses(IEnumerable<WarehouseListItem> warehouses)
    {
        Warehouses.Clear();
        foreach (var warehouse in warehouses.OrderBy(x => x.Code))
            Warehouses.Add(new WarehouseManagementItem(warehouse.Id, warehouse.Code, warehouse.Name));
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var keyword = SearchText.Trim();
        var matches = Warehouses.Where(x => keyword.Length == 0
            || x.Code.Contains(keyword, StringComparison.OrdinalIgnoreCase)
            || x.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        FilteredWarehouses.Clear();
        foreach (var warehouse in matches) FilteredWarehouses.Add(warehouse);
        OnPropertyChanged(nameof(HasNoResults));
        OnPropertyChanged(nameof(ResultSummary));
    }
}

public sealed partial class WarehouseManagementItem : ObservableObject
{
    public WarehouseManagementItem(Guid id, string code, string name)
    {
        Id = id;
        Code = code;
        Name = name;
        EditCode = code;
        EditName = name;
    }

    public Guid Id { get; }
    public string Code { get; }
    public string Name { get; }
    [ObservableProperty] private string _editCode;
    [ObservableProperty] private string _editName;
    [ObservableProperty] private bool _isEditing;
    public bool IsNotEditing => !IsEditing;
    partial void OnIsEditingChanged(bool value) => OnPropertyChanged(nameof(IsNotEditing));
}
