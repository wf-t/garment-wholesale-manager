using Shanquan.Inventory.Desktop.Services;
using Shanquan.Inventory.Desktop.Services.Printing;

namespace Shanquan.Inventory.Desktop.Tests;

public class SalesPrintLayoutTests
{
    // Deterministic text metrics exercise pagination independently of Windows GDI/printer drivers.
    private static float Measure(string text, float width, float size, bool bold) =>
        Math.Max(1, MathF.Ceiling(text.Length * size * .7f / width)) * size * 1.6f;

    private static SalesOrderPreviewData Data(int sizes, int rows) => new("SO20260915", "2026-09-15",
        "服装批发测试客户", "13800000000", "¥123456.00",
        Enumerable.Range(25, sizes).Select(i => i.ToString()).ToArray(),
        Enumerable.Range(1, rows).Select(i => new SalesOrderPreviewLine(i, $"STYLE-{i}", "浅卡其",
            Enumerable.Range(25, sizes).Reverse().Select(s => new SalesOrderPreviewSizeCell(s.ToString(), s.ToString())).ToArray(),
            "2", "¥88.00", "¥176.00")).ToArray());

    [Theory]
    [InlineData(0, 0, 771, 527)]
    [InlineData(3, 1, 771, 527)]
    [InlineData(16, 70, 771, 527)]
    [InlineData(16, 70, 527, 771)]
    public void RowsStayWithinPageAndArePrintedExactlyOnce(int sizes, int rows, float width, float height)
    {
        var pages = SalesPrintLayout.Create(Data(sizes, rows), 32, 35, width, height, Measure);
        Assert.NotEmpty(pages);
        var items = pages.SelectMany(p => p.Items).ToArray();
        for (var i = 1; i <= rows; i++) Assert.Single(items.Where(t => t.Text == $"STYLE-{i}"));
        Assert.Single(items.Where(t => t.Text == "总计：¥123456.00"));
        foreach (var page in pages)
        {
            Assert.Contains(page.Items, t => t.Text.StartsWith("第 ") && t.Text.EndsWith(" 页"));
            if (page.Items.Any(t => t.Border && !t.Bold)) Assert.Contains(page.Items, t => t.Border && t.Bold && t.Text == "金额");
            foreach (var item in page.Items)
            {
                Assert.True(item.X >= 32 && item.Y >= 35);
                Assert.True(item.X + item.Width <= 32 + width + .01f);
                Assert.True(item.Y + item.Height <= 35 + height + .01f);
            }
        }
    }

    [Fact]
    public void SizeQuantitiesFollowHeadersRatherThanInputOrder()
    {
        var page = SalesPrintLayout.Create(Data(3, 1), 28, 28, 771, 527, Measure).Single();
        var cells = page.Items.Where(t => t.Border && !t.Bold).ToArray();
        Assert.Equal(new[] { "25", "26", "27" }, cells.Skip(3).Take(3).Select(c => c.Text));
    }

    [Fact]
    public void UnprintableRowFailsInsteadOfTruncatingOrLooping()
    {
        var data = Data(3, 1);
        data = data with { Lines = new[] { data.Lines[0] with { StyleNumber = new string('W', 20000) } } };
        Assert.Throws<InvalidOperationException>(() => SalesPrintLayout.Create(data, 28, 28, 771, 527, Measure));
    }

    [Fact]
    public void ExcessiveColumnsFailInsteadOfClipping()
    {
        Assert.Throws<InvalidOperationException>(() => SalesPrintLayout.Create(Data(100, 1), 28, 28, 771, 527, Measure));
    }

    [Fact]
    public void ChangingOrientationReflowsColumns()
    {
        var data = Data(16, 5);
        var landscape = SalesPrintLayout.Create(data, 28, 28, 771, 527, Measure);
        var portrait = SalesPrintLayout.Create(data, 28, 28, 527, 771, Measure);
        Assert.True(landscape[0].Items.First(t => t.Border).Width > portrait[0].Items.First(t => t.Border).Width);
    }
}
