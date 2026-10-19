using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shanquan.Inventory.Application.CoreBusiness;

namespace Shanquan.Inventory.Desktop.ViewModels.Dialogs;

public sealed partial class ColorManagementViewModel : ViewModelBase
{
    private readonly ICoreBusinessService _businessService;

    public ColorManagementViewModel(ICoreBusinessService businessService, IReadOnlyList<ColorListItem> colors)
    {
        _businessService = businessService;
        ReplaceColors(colors);
    }

    public ObservableCollection<ColorManagementItem> Colors { get; } = [];
    public ObservableCollection<ColorManagementItem> FilteredColors { get; } = [];
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string _newColorName = "";
    [ObservableProperty] private string _statusMessage = "直接输入业务中使用的颜色名称，例如“黑色”或“浅卡其”。";

    public bool HasNoResults => FilteredColors.Count == 0;
    public string ResultSummary => $"共 {FilteredColors.Count} 种颜色";
    partial void OnSearchTextChanged(string value) => ApplyFilter();

    [RelayCommand]
    private async Task AddColorAsync()
    {
        try
        {
            var name = NewColorName.Trim();
            await _businessService.SaveColorNameAsync(null, name);
            NewColorName = "";
            SearchText = "";
            await RefreshAsync();
            StatusMessage = $"已新增颜色“{name}”。";
        }
        catch (Exception exception) { StatusMessage = exception.Message; }
    }

    public void BeginEdit(ColorManagementItem item)
    {
        foreach (var color in Colors.Where(x => x != item)) color.IsEditing = false;
        item.EditName = item.Name;
        item.IsEditing = true;
        StatusMessage = $"正在编辑颜色“{item.Name}”。";
    }

    public void CancelEdit(ColorManagementItem item)
    {
        item.EditName = item.Name;
        item.IsEditing = false;
        StatusMessage = "已取消编辑。";
    }

    public async Task SaveEditAsync(ColorManagementItem item)
    {
        try
        {
            var name = item.EditName.Trim();
            await _businessService.SaveColorNameAsync(item.Id, name);
            await RefreshAsync();
            StatusMessage = $"颜色已更新为“{name}”。";
        }
        catch (Exception exception) { StatusMessage = exception.Message; }
    }

    public async Task DeleteAsync(ColorManagementItem item)
    {
        try
        {
            await _businessService.DeleteColorAsync(item.Id);
            await RefreshAsync();
            StatusMessage = $"已删除颜色“{item.Name}”。";
        }
        catch (Exception exception) { StatusMessage = exception.Message; }
    }

    private async Task RefreshAsync()
    {
        var workspace = await _businessService.GetWorkspaceAsync();
        ReplaceColors(workspace.Colors);
    }

    private void ReplaceColors(IEnumerable<ColorListItem> colors)
    {
        Colors.Clear();
        foreach (var color in colors.OrderBy(x => x.ChineseName))
            Colors.Add(new ColorManagementItem(color.Id, color.ChineseName));
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var keyword = SearchText.Trim();
        var matches = Colors.Where(x => keyword.Length == 0 || x.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        FilteredColors.Clear();
        foreach (var color in matches) FilteredColors.Add(color);
        OnPropertyChanged(nameof(HasNoResults));
        OnPropertyChanged(nameof(ResultSummary));
    }
}

public sealed partial class ColorManagementItem : ObservableObject
{
    public ColorManagementItem(Guid id, string name)
    {
        Id = id;
        Name = name;
        EditName = name;
    }

    public Guid Id { get; }
    public string Name { get; }
    [ObservableProperty] private string _editName;
    [ObservableProperty] private bool _isEditing;
    public bool IsNotEditing => !IsEditing;
    partial void OnIsEditingChanged(bool value) => OnPropertyChanged(nameof(IsNotEditing));
}
