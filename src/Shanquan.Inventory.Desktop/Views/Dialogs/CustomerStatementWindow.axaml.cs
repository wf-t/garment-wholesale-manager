using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Shanquan.Inventory.Desktop.Services;
using System.Text;

namespace Shanquan.Inventory.Desktop.Views.Dialogs;

public partial class CustomerStatementWindow : Window
{
    public CustomerStatementWindow() => InitializeComponent();
    public CustomerStatementWindow(CustomerStatementData data)
    {
        InitializeComponent();
        DataContext = data;
    }
    private async void Export_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not CustomerStatementData data) return;
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "导出客户对账单",
            SuggestedFileName = $"{data.CustomerName}-对账单-{DateTime.Now:yyyyMMdd}.csv",
            FileTypeChoices = [new FilePickerFileType("CSV 表格") { Patterns = ["*.csv"] }],
        });
        if (file is null) return;
        await using var stream = await file.OpenWriteAsync();
        stream.SetLength(0);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(true));
        await writer.WriteLineAsync($"客户,{Csv(data.CustomerName)}");
        await writer.WriteLineAsync($"对账期间,{Csv(data.PeriodText)}");
        await writer.WriteLineAsync($"未结余额,{Csv(data.ReceivableText)}");
        await writer.WriteLineAsync("日期,类型,单号,应收,收款,备注");
        foreach (var line in data.Lines)
            await writer.WriteLineAsync(string.Join(',', Csv(line.DateText), Csv(line.Type), Csv(line.ReferenceNumber), Csv(line.DebitText), Csv(line.CreditText), Csv(line.Note)));
    }
    private void Close_Click(object? sender, RoutedEventArgs e) => Close();
    private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
}
