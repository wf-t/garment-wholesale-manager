using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Runtime.Versioning;

namespace Shanquan.Inventory.Desktop.Services.Printing;

public sealed record SalesPaper(string Name, int RawKind, int Width, int Height)
{
    public override string ToString() => $"{Name} ({Width * 0.254:0.#} × {Height * 0.254:0.#} mm)";
}

[SupportedOSPlatform("windows")]
public sealed class WindowsSalesPrinter : IDisposable
{
    private readonly PrintDocument _document;
    private readonly IReadOnlyList<SalesPrintPage> _pages;
    private readonly Size _pageSize;
    public int PageCount => _pages.Count;
    public string Summary { get; }

    public static string[] GetPrinters() => PrinterSettings.InstalledPrinters.Cast<string>().ToArray();
    public static string DefaultPrinter => new PrinterSettings().PrinterName;
    public static SalesPaper[] GetPapers(string printer)
    {
        var settings = new PrinterSettings { PrinterName = printer };
        if (!settings.IsValid) throw new InvalidOperationException("打印机不可用，请检查打印机连接和驱动。");
        return settings.PaperSizes.Cast<PaperSize>()
            .Select(p => new SalesPaper(p.PaperName, p.RawKind, p.Width, p.Height)).ToArray();
    }

    public WindowsSalesPrinter(SalesOrderPreviewData data, string printer, SalesPaper paper, bool landscape, short copies)
    {
        _document = new PrintDocument();
        try
        {
            var settings = _document.PrinterSettings;
            settings.PrinterName = printer;
            if (!settings.IsValid) throw new InvalidOperationException("打印机不可用，请检查打印机连接和驱动。");
            if (copies < 1 || copies > Math.Max(1, settings.MaximumCopies))
                throw new InvalidOperationException($"该打印机支持的份数为 1–{Math.Max(1, settings.MaximumCopies)}。");
            settings.Copies = copies;
            settings.Collate = true;
            if (settings.CanDuplex) settings.Duplex = Duplex.Simplex;
            var selectedPaper = settings.PaperSizes.Cast<PaperSize>().FirstOrDefault(p =>
                p.RawKind == paper.RawKind && p.Width == paper.Width && p.Height == paper.Height)
                ?? throw new InvalidOperationException("打印机已不支持所选纸张，请重新选择。");
            _document.DefaultPageSettings = new PageSettings(settings)
            {
                PaperSize = selectedPaper,
                Landscape = landscape,
                Margins = new Margins(28, 28, 28, 28),
            };
            _document.DocumentName = $"销售单 {data.TicketNumber}";
            _document.OriginAtMargins = false;
            _document.PrintController = new StandardPrintController();
            var page = _document.DefaultPageSettings;
            _pageSize = page.Bounds.Size;
            // Respect asymmetric hardware margins as well as the requested 7 mm margin.
            var area = RectangleF.Intersect(page.PrintableArea,
                new RectangleF(28, 28, _pageSize.Width - 56, _pageSize.Height - 56));
            using var graphics = settings.CreateMeasurementGraphics(page);
            ConfigureGraphics(graphics);
            _pages = SalesPrintLayout.Create(data, area.Left, area.Top, area.Width, area.Height,
                (text, width, size, bold) =>
                {
                    using var font = Font(size, bold);
                    using var format = Format(false, false);
                    return graphics.MeasureString(string.IsNullOrEmpty(text) ? " " : text, font,
                        new SizeF(width, 100000), format).Height;
                });
            Summary = $"{printer} · {paper.Name} · {(landscape ? "横向" : "纵向")} · {copies} 份 · {_pages.Count} 页/份";
        }
        catch
        {
            _document.Dispose();
            throw;
        }
    }

    private static Font Font(float points, bool bold) => new("Microsoft YaHei", points * 100 / 72,
        bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel);

    private static StringFormat Format(bool center, bool right) => new(StringFormat.GenericDefault)
    {
        Alignment = center ? StringAlignment.Center : right ? StringAlignment.Far : StringAlignment.Near,
        LineAlignment = StringAlignment.Center,
        Trimming = StringTrimming.None,
        FormatFlags = StringFormatFlags.LineLimit,
    };

    private static void ConfigureGraphics(Graphics graphics)
    {
        graphics.PageUnit = GraphicsUnit.Pixel;
        graphics.ScaleTransform(graphics.DpiX / 100, graphics.DpiY / 100);
    }

    private void Draw(Graphics graphics, int page)
    {
        using var pen = new Pen(Color.Black, .65f);
        foreach (var item in _pages[page].Items)
        {
            using var font = Font(item.FontSize, item.Bold);
            using var format = Format(item.Center, item.Right);
            var rectangle = new RectangleF(item.X, item.Y, item.Width, item.Height);
            if (item.Border)
            {
                graphics.DrawRectangle(pen, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
                rectangle.Inflate(-3, -3);
            }
            graphics.DrawString(item.Text, font, Brushes.Black, rectangle, format);
        }
    }

    public byte[] RenderPreview(int page)
    {
        if (page < 0 || page >= _pages.Count) throw new ArgumentOutOfRangeException(nameof(page));
        using var bitmap = new Bitmap((int)Math.Ceiling(_pageSize.Width * 1.5), (int)Math.Ceiling(_pageSize.Height * 1.5));
        bitmap.SetResolution(150, 150);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.White);
            ConfigureGraphics(graphics);
            Draw(graphics, page);
        }
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }

    public void Print()
    {
        var page = 0;
        void PrintPage(object? sender, PrintPageEventArgs e)
        {
            var graphics = e.Graphics ?? throw new InvalidOperationException("无法取得打印机绘图环境。");
            var state = graphics.Save();
            try
            {
                ConfigureGraphics(graphics);
                // The printer origin is its printable corner; layouts use the physical paper origin.
                graphics.TranslateTransform(-e.PageSettings.HardMarginX, -e.PageSettings.HardMarginY);
                Draw(graphics, page++);
                e.HasMorePages = page < _pages.Count;
            }
            finally { graphics.Restore(state); }
        }
        _document.PrintPage += PrintPage;
        try { _document.Print(); }
        finally { _document.PrintPage -= PrintPage; }
    }

    public void Dispose() => _document.Dispose();
}
