using System.Diagnostics;
using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Shanquan.Inventory.Desktop.Services;

namespace Shanquan.Inventory.Desktop.Views.Dialogs;

public partial class SalesOrderPreviewWindow : Window
{
    public SalesOrderPreviewWindow() : this(new SalesOrderPreviewData("", "", "", "", "", [], [])) { }

    public SalesOrderPreviewWindow(SalesOrderPreviewData data)
    {
        InitializeComponent();
        DataContext = data;
        PrintHint.Text = OperatingSystem.IsWindows()
            ? "打印设置中可选择纸张、方向和份数"
            : "A5 横向 · 浏览器打印时请关闭页眉和页脚";
    }

    private void Close_Click(object? sender, RoutedEventArgs e) => Close();

    private async void Print_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not SalesOrderPreviewData data) return;
        try
        {
            if (OperatingSystem.IsWindows())
            {
                await new WindowsSalesPrintWindow(data).ShowDialog(this);
                return;
            }

            var printDirectory = Path.Combine(Path.GetTempPath(), "ShanquanInventory", "print");
            Directory.CreateDirectory(printDirectory);
            var printFile = Path.Combine(printDirectory, $"sales-order-{DateTime.Now:yyyyMMddHHmmssfff}.html");
            await File.WriteAllTextAsync(printFile, data.ToPrintableHtml(), new UTF8Encoding(false));
            Process.Start(new ProcessStartInfo
            {
                FileName = printFile,
                UseShellExecute = true,
            });
        }
        catch (Exception exception)
        {
            await new ConfirmWindow("无法调起打印", $"打印页启动失败：{exception.Message}", "关闭")
                .ShowDialog<bool>(this);
        }
    }
}
