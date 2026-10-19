using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Shanquan.Inventory.Desktop.Services;
using Shanquan.Inventory.Desktop.Services.Printing;

namespace Shanquan.Inventory.Desktop.Views.Dialogs;

[SupportedOSPlatform("windows")]
public sealed class WindowsSalesPrintWindow : Window
{
    private readonly SalesOrderPreviewData _data;
    private readonly ComboBox _printer = new() { MinWidth = 260 };
    private readonly ComboBox _paper = new() { MinWidth = 220 };
    private readonly ComboBox _direction = new() { ItemsSource = new[] { "横向", "纵向" }, SelectedIndex = 0, MinWidth = 90 };
    private readonly NumericUpDown _copies = new() { Minimum = 1, Maximum = 999, Value = 1, Width = 80, FormatString = "0" };
    private readonly Button _preview = new() { Content = "更新预览", Padding = new Thickness(16, 9) };
    private readonly Button _print = new() { Content = "确认打印", IsEnabled = false, Padding = new Thickness(20, 9) };
    private readonly Button _previous = new() { Content = "上一页", IsEnabled = false };
    private readonly Button _next = new() { Content = "下一页", IsEnabled = false };
    private readonly TextBlock _pageLabel = new() { VerticalAlignment = VerticalAlignment.Center };
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10) };
    private readonly Image _image = new() { Stretch = Stretch.Uniform };
    private readonly StackPanel _settings;
    private WindowsSalesPrinter? _job;
    private Bitmap? _bitmap;
    private int _page;
    private bool _busy;

    public WindowsSalesPrintWindow(SalesOrderPreviewData data)
    {
        _data = data;
        Title = "销售单打印 — Windows";
        Width = 1100;
        Height = 780;
        MinWidth = 980;
        MinHeight = 620;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brush.Parse("#F1EEEB");
        _settings = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        void Field(string label, Control control)
        {
            var field = new StackPanel { Spacing = 5 };
            field.Children.Add(new TextBlock { Text = label });
            field.Children.Add(control);
            _settings.Children.Add(field);
        }
        Field("打印机", _printer);
        Field("纸张（打印机支持的尺寸）", _paper);
        Field("方向", _direction);
        Field("份数", _copies);
        _settings.Children.Add(_preview);
        _preview.VerticalAlignment = VerticalAlignment.Bottom;
        var header = new StackPanel();
        header.Children.Add(_settings);
        header.Children.Add(_status);
        var footer = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 12, Margin = new Thickness(0, 12, 0, 0) };
        footer.Children.Add(_previous);
        footer.Children.Add(_pageLabel);
        footer.Children.Add(_next);
        var close = new Button { Content = "关闭", Padding = new Thickness(16, 9) };
        close.Click += (_, _) => Close();
        footer.Children.Add(close);
        footer.Children.Add(_print);
        var root = new DockPanel { Margin = new Thickness(22) };
        DockPanel.SetDock(header, Dock.Top);
        DockPanel.SetDock(footer, Dock.Bottom);
        root.Children.Add(header);
        root.Children.Add(footer);
        root.Children.Add(new Border { Background = Brushes.White, Padding = new Thickness(8), Child = _image });
        Content = root;
        _printer.SelectionChanged += (_, _) => LoadPapers();
        _paper.SelectionChanged += (_, _) => Invalidate();
        _direction.SelectionChanged += (_, _) => Invalidate();
        _copies.ValueChanged += (_, _) => Invalidate();
        _preview.Click += async (_, _) => await UpdatePreview();
        _print.Click += async (_, _) => await Print();
        _previous.Click += (_, _) => ShowPage(_page - 1);
        _next.Click += (_, _) => ShowPage(_page + 1);
        Opened += async (_, _) =>
        {
            try
            {
                var printers = WindowsSalesPrinter.GetPrinters();
                _printer.ItemsSource = printers;
                _printer.SelectedItem = printers.FirstOrDefault(p => p == WindowsSalesPrinter.DefaultPrinter) ?? printers.FirstOrDefault();
                if (printers.Length == 0) _status.Text = "未找到打印机，请先在 Windows 中安装打印机驱动。";
                else if (_paper.SelectedItem != null) await UpdatePreview();
            }
            catch (Exception ex) { _status.Text = $"读取打印机失败：{ex.Message}"; }
        };
        Closing += (_, e) => { if (_busy) e.Cancel = true; };
        Closed += (_, _) => { _bitmap?.Dispose(); _job?.Dispose(); };
    }

    private void LoadPapers()
    {
        Invalidate();
        _paper.ItemsSource = Array.Empty<SalesPaper>();
        if (_printer.SelectedItem is not string printer) return;
        try
        {
            var papers = WindowsSalesPrinter.GetPapers(printer);
            _paper.ItemsSource = papers;
            // Do not silently switch an A5 job to the printer's default A4/Letter paper.
            _paper.SelectedItem = papers.FirstOrDefault(p => p.RawKind == 11 ||
                (Math.Abs(p.Width - 583) <= 3 && Math.Abs(p.Height - 827) <= 3));
            if (_paper.SelectedItem == null)
                _status.Text = "该打印机未提供标准 A5，请检查驱动，或明确选择其他纸张后更新预览。";
        }
        catch (Exception ex) { _status.Text = $"读取纸张失败：{ex.Message}"; }
    }

    private void Invalidate()
    {
        _print.IsEnabled = false;
        _previous.IsEnabled = _next.IsEnabled = false;
        _job?.Dispose();
        _job = null;
        _image.Source = null;
        _bitmap?.Dispose();
        _bitmap = null;
        _pageLabel.Text = "";
        _status.Text = "请选择纸张和方向，点击“更新预览”。确认打印将使用预览中的同一套设置。";
    }

    private void SetBusy(bool value)
    {
        _busy = value;
        _settings.IsEnabled = !value;
        _print.IsEnabled = !value && _job != null;
        _previous.IsEnabled = !value && _page > 0 && _job != null;
        _next.IsEnabled = !value && _job != null && _page + 1 < _job.PageCount;
    }

    private async Task UpdatePreview()
    {
        Invalidate();
        if (_printer.SelectedItem is not string printer || _paper.SelectedItem is not SalesPaper paper)
        {
            _status.Text = "请先选择打印机和纸张。";
            return;
        }
        var landscape = _direction.SelectedIndex == 0;
        var value = _copies.Value;
        if (value == null || value != decimal.Truncate(value.Value) || value < 1 || value > 999)
        {
            _status.Text = "打印份数必须为 1–999 的整数。";
            return;
        }
        SetBusy(true);
        _status.Text = "正在按打印机的可打印区域生成预览…";
        try
        {
            _job = await Task.Run(() => new WindowsSalesPrinter(_data, printer, paper, landscape, (short)value.Value));
            ShowPage(0);
            if (_job != null) _status.Text = _job.Summary + "。请按打印机要求装入所选纸张。";
        }
        catch (Exception ex)
        {
            Invalidate();
            _status.Text = $"生成预览失败：{ex.Message}";
        }
        finally { SetBusy(false); }
    }

    private void ShowPage(int page)
    {
        if (_job == null || page < 0 || page >= _job.PageCount) return;
        try
        {
            using var stream = new MemoryStream(_job.RenderPreview(page));
            var bitmap = new Bitmap(stream);
            _image.Source = bitmap;
            _bitmap?.Dispose();
            _bitmap = bitmap;
            _page = page;
            _pageLabel.Text = $"第 {page + 1} / {_job.PageCount} 页";
            _previous.IsEnabled = page > 0;
            _next.IsEnabled = page + 1 < _job.PageCount;
        }
        catch (Exception ex)
        {
            Invalidate();
            _status.Text = $"显示预览失败：{ex.Message}";
        }
    }

    private async Task Print()
    {
        if (_job == null || _busy) return;
        var job = _job;
        SetBusy(true);
        _status.Text = "正在提交打印任务，请勿重复点击…";
        try
        {
            await Task.Run(job.Print);
            _status.Text = "已提交到 Windows 打印队列：" + job.Summary + "。";
        }
        catch (Exception ex)
        {
            _status.Text = $"打印未正常完成：{ex.Message}。请先检查打印队列，避免重复出单。";
        }
        finally
        {
            // Require a fresh preview before another submission to prevent accidental duplicate jobs.
            _job = null;
            job.Dispose();
            SetBusy(false);
        }
    }
}
