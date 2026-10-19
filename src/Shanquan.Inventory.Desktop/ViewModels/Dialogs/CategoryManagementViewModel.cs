using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shanquan.Inventory.Application.CoreBusiness;

namespace Shanquan.Inventory.Desktop.ViewModels.Dialogs;

public sealed partial class CategoryManagementViewModel : ViewModelBase
{
    private readonly ICoreBusinessService _businessService;

    public CategoryManagementViewModel(ICoreBusinessService businessService, IReadOnlyList<string> categories)
    {
        _businessService = businessService;
        ReplaceCategories(categories);
    }

    public ObservableCollection<CategoryManagementItem> Categories { get; } = [];
    public ObservableCollection<CategoryManagementItem> FilteredCategories { get; } = [];

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string _newCategoryName = "";
    [ObservableProperty] private string _statusMessage = "可在此新增、查找、重命名或删除商品品类。";

    public bool HasNoResults => FilteredCategories.Count == 0;
    public string ResultSummary => $"共 {FilteredCategories.Count} 个品类";

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    [RelayCommand]
    private async Task AddCategoryAsync()
    {
        var name = NewCategoryName.Trim();
        if (name.Length == 0)
        {
            StatusMessage = "请输入品类名称。";
            return;
        }

        try
        {
            await _businessService.SaveCategoryAsync(name);
            NewCategoryName = "";
            SearchText = "";
            await RefreshAsync();
            StatusMessage = $"已新增品类“{name}”。";
        }
        catch (Exception exception) { StatusMessage = exception.Message; }
    }

    public void BeginEdit(CategoryManagementItem item)
    {
        foreach (var category in Categories.Where(x => x != item)) category.IsEditing = false;
        item.EditName = item.Name;
        item.IsEditing = true;
        StatusMessage = $"正在编辑品类“{item.Name}”。";
    }

    public void CancelEdit(CategoryManagementItem item)
    {
        item.EditName = item.Name;
        item.IsEditing = false;
        StatusMessage = "已取消编辑。";
    }

    public async Task SaveEditAsync(CategoryManagementItem item)
    {
        var newName = item.EditName.Trim();
        if (newName.Length == 0)
        {
            StatusMessage = "品类名称不能为空。";
            return;
        }

        var oldName = item.Name;
        try
        {
            await _businessService.RenameCategoryAsync(oldName, newName);
            await RefreshAsync();
            StatusMessage = oldName == newName
                ? "品类名称未变更。"
                : $"已将“{oldName}”重命名为“{newName}”，关联商品已同步更新。";
        }
        catch (Exception exception) { StatusMessage = exception.Message; }
    }

    public Task<CategoryUsage> GetUsageAsync(CategoryManagementItem item)
        => _businessService.GetCategoryUsageAsync(item.Name);

    public async Task DeleteAsync(CategoryManagementItem item)
    {
        try
        {
            await _businessService.DeleteCategoryAsync(item.Name);
            await RefreshAsync();
            StatusMessage = $"已删除品类“{item.Name}”。";
        }
        catch (Exception exception) { StatusMessage = exception.Message; }
    }

    private async Task RefreshAsync()
    {
        var workspace = await _businessService.GetWorkspaceAsync();
        ReplaceCategories(workspace.Categories);
    }

    private void ReplaceCategories(IEnumerable<string> categories)
    {
        Categories.Clear();
        foreach (var name in categories.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x))
            Categories.Add(new CategoryManagementItem(name));
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var keyword = SearchText.Trim();
        var matches = Categories.Where(x => keyword.Length == 0 || x.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        FilteredCategories.Clear();
        foreach (var item in matches) FilteredCategories.Add(item);
        OnPropertyChanged(nameof(HasNoResults));
        OnPropertyChanged(nameof(ResultSummary));
    }
}

public sealed partial class CategoryManagementItem : ObservableObject
{
    public CategoryManagementItem(string name)
    {
        Name = name;
        EditName = name;
    }

    public string Name { get; }
    [ObservableProperty] private string _editName;
    [ObservableProperty] private bool _isEditing;
    public bool IsNotEditing => !IsEditing;

    partial void OnIsEditingChanged(bool value) => OnPropertyChanged(nameof(IsNotEditing));
}
