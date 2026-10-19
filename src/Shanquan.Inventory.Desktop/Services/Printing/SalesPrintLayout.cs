namespace Shanquan.Inventory.Desktop.Services.Printing;

// Coordinates are hundredths of an inch, matching Windows printer page settings.
public sealed record PrintText(string Text, float X, float Y, float Width, float Height,
    float FontSize = 8, bool Bold = false, bool Center = false, bool Right = false, bool Border = false);
public sealed record SalesPrintPage(IReadOnlyList<PrintText> Items);

public static class SalesPrintLayout
{
    public static IReadOnlyList<SalesPrintPage> Create(SalesOrderPreviewData data,
        float left, float top, float width, float height,
        Func<string, float, float, bool, float> measure)
    {
        if (width < 200 || height < 150)
            throw new InvalidOperationException("打印机可打印区域太小，请选择 A5 或更大的纸张。");
        var weights = new[] { 9f, 20, 15 }.Concat(Enumerable.Repeat(9f, data.SizeHeaders.Count))
            .Concat(new[] { 13f, 19, 23 }).ToArray();
        var widths = weights.Select(w => width * w / weights.Sum()).ToArray();
        if (widths.Min() < 14)
            throw new InvalidOperationException("尺码列过多，当前纸张无法清晰容纳，请选择横向或更大的纸张。");
        var headers = new[] { "序号", "货号", "颜色" }.Concat(data.SizeHeaders)
            .Concat(new[] { "数量", "单价", "金额" }).ToArray();
        const float padding = 3;
        const float footer = 25;
        var pages = new List<SalesPrintPage>();
        var index = 0;
        do
        {
            var items = new List<PrintText>();
            var y = top;
            void Text(string text, float x, float w, float size, bool bold = false, bool center = false)
            {
                var h = measure(text, w, size, bold) + 4;
                items.Add(new(text, x, y, w, h, size, bold, center));
                y += h;
            }
            Text(data.Title, left, width, 16, true, true);
            y += 7;
            var metadata = new[] { data.TicketNumberLabel, data.DocumentDateLabel, data.CustomerLabel, data.PhoneLabel };
            var metaHeight = metadata.Max(t => measure(t, width / 2 - 8, 8, false)) + 4;
            for (var m = 0; m < metadata.Length; m++)
                items.Add(new(metadata[m], left + (m % 2) * width / 2, y + (m / 2) * metaHeight,
                    width / 2 - 8, metaHeight));
            y += metaHeight * 2 + 8;
            float RowHeight(string[] cells, bool bold) => cells.Select((t, i) =>
                measure(t, widths[i] - padding * 2, 8, bold) + padding * 2).Max();
            void Row(string[] cells, float h, bool bold)
            {
                var x = left;
                for (var i = 0; i < cells.Length; i++)
                {
                    items.Add(new(cells[i], x, y, widths[i], h, 8, bold,
                        Center: bold || i < cells.Length - 2, Right: !bold && i >= cells.Length - 2, Border: true));
                    x += widths[i];
                }
                y += h;
            }
            Row(headers, RowHeight(headers, true), true);
            var contentBottom = top + height - footer;
            var first = index;
            while (index < data.Lines.Count)
            {
                var line = data.Lines[index];
                var cells = new[] { line.Sequence.ToString(), line.StyleNumber, line.Color }
                    .Concat(data.SizeHeaders.Select(s => line.Sizes.FirstOrDefault(c => c.SizeCode == s)?.Quantity ?? ""))
                    .Concat(new[] { line.Quantity, line.UnitPrice, line.Amount }).ToArray();
                var h = RowHeight(cells, false);
                if (y + h > contentBottom) break;
                Row(cells, h, false);
                index++;
            }
            if (index < data.Lines.Count && first == index)
                throw new InvalidOperationException($"第 {data.Lines[index].Sequence} 行内容超出单页高度，请缩短货号、颜色或客户信息，或选择更大的纸张。");
            if (y > contentBottom)
                throw new InvalidOperationException("单据信息超出可打印区域，请缩短客户信息或选择更大的纸张。");
            if (index == data.Lines.Count)
            {
                var total = $"总计：{data.TotalAmount}";
                var h = measure(total, width, 11, true) + 8;
                if (h > contentBottom - (top + 30))
                    throw new InvalidOperationException("合计内容过长，无法打印。");
                if (y + h > contentBottom)
                {
                    items.Add(new($"第 {pages.Count + 1} 页", left, top + height - 18, width, 18, Center: true));
                    pages.Add(new(items));
                    items = new List<PrintText>();
                    y = top;
                    Text(data.Title + "（合计）", left, width, 16, true, true);
                    Text(data.TicketNumberLabel, left, width, 8);
                }
                items.Add(new(total, left, y + 4, width, h, 11, true, Right: true));
            }
            items.Add(new($"第 {pages.Count + 1} 页", left, top + height - 18, width, 18, Center: true));
            pages.Add(new(items));
        } while (index < data.Lines.Count);
        return pages;
    }
}
