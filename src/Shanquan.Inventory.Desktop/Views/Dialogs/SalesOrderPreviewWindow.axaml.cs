using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Shanquan.Inventory.Desktop.Services;

namespace Shanquan.Inventory.Desktop.Views.Dialogs;

public partial class SalesOrderPreviewWindow : Window
{
    public SalesOrderPreviewWindow() : this(new SalesOrderPreviewData("", "", "", "", "", "", "", "", "", [])) { }

    public SalesOrderPreviewWindow(SalesOrderPreviewData data)
    {
        InitializeComponent();
        DataContext = data;
    }

    private void Close_Click(object? sender, RoutedEventArgs e) => Close();

    private async void Export_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not SalesOrderPreviewData data) return;
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "导出销售单打印文本",
            SuggestedFileName = $"{data.DraftNumber}-销售单.txt",
            FileTypeChoices = [new FilePickerFileType("文本文件") { Patterns = ["*.txt"] }],
        });
        if (file is null) return;
        await using var stream = await file.OpenWriteAsync();
        stream.SetLength(0);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(true));
        await writer.WriteAsync(data.ToPrintableText());
    }
}
