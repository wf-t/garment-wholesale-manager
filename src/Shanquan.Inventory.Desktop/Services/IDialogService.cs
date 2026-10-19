using System.Net;
using System.Text;
using Shanquan.Inventory.Application.CoreBusiness;

namespace Shanquan.Inventory.Desktop.Services;

public interface IDialogService
{
    Task<CustomerFormData?> ShowCustomerFormAsync(CustomerListItem? customer);
    Task<CustomerPaymentFormData?> ShowCustomerPaymentFormAsync(IReadOnlyList<CustomerListItem> customers, CustomerListItem? customer = null);
    Task ShowCustomerStatementAsync(CustomerStatementData data);
    Task<bool> ShowConfirmAsync(string title, string message, string confirmText);
    Task<string?> ShowShippingConfirmAsync(string title, string message, string confirmText);
    Task ShowSalesOrderPreviewAsync(SalesOrderPreviewData data);
    Task<InventoryFormData?> ShowInventoryFormAsync(
        InventoryFormMode mode,
        IReadOnlyList<ProductListItem> products,
        IReadOnlyList<WarehouseListItem> warehouses,
        IReadOnlyList<string> categories,
        IReadOnlyList<ColorListItem> colors,
        IReadOnlyList<SkuListItem> skus,
        IReadOnlyList<StockListItem> stock,
        ProductListItem? product = null,
        SkuListItem? sku = null);
}

public sealed record CustomerFormData(
    Guid? Id,
    string Code,
    string Name,
    string Phone,
    string Address,
    string CreditLimitText,
    string PaymentTermDaysText);

public sealed record CustomerPaymentFormData(
    Guid CustomerId,
    string AmountText,
    DateTimeOffset ReceivedAt,
    string PaymentMethod,
    string Note);

public sealed record CustomerStatementData(
    string CustomerName,
    string PeriodText,
    string ReceivableText,
    IReadOnlyList<CustomerStatementLine> Lines);

public sealed record CustomerStatementLine(
    string DateText,
    string Type,
    string ReferenceNumber,
    string DebitText,
    string CreditText,
    string Note);

public sealed record SalesOrderPreviewData(
    string TicketNumber,
    string DocumentDate,
    string Customer,
    string Phone,
    string TotalAmount,
    IReadOnlyList<string> SizeHeaders,
    IReadOnlyList<SalesOrderPreviewLine> Lines)
{
    public string Title => "盛轩服饰销售单";
    public string TicketNumberLabel => $"票号：{TicketNumber}";
    public string DocumentDateLabel => $"日期：{DocumentDate}";
    public string CustomerLabel => $"客户：{Customer}";
    public string PhoneLabel => $"电话：{(string.IsNullOrWhiteSpace(Phone) ? "—" : Phone)}";

    public string ToPrintableText()
    {
        var rows = Lines.Select(x => string.Join('\t',
            [x.Sequence.ToString(), x.StyleNumber, x.Color, .. x.Sizes.Select(size => size.Quantity), x.Quantity, x.UnitPrice, x.Amount]));
        return string.Join(Environment.NewLine,
        [
            Title,
            $"{TicketNumberLabel}\t{DocumentDateLabel}\t{CustomerLabel}\t{PhoneLabel}",
            "",
            string.Join('\t', ["序号", "货号", "颜色", .. SizeHeaders, "数量", "单价", "金额"]),
            .. rows,
            "",
            $"总计：{TotalAmount}",
        ]);
    }

    public string ToPrintableHtml()
    {
        static string Html(string value) => WebUtility.HtmlEncode(value);
        // Relative widths keep every size column inside the A5 printable area.
        var totalColumnWeight = 99d + 9d * SizeHeaders.Count;
        string ColumnWidth(int weight) => (weight * 100d / totalColumnWeight)
            .ToString("0.######", System.Globalization.CultureInfo.InvariantCulture) + "%";

        var builder = new StringBuilder();
        builder.Append("<!doctype html><html><head><meta charset=\"utf-8\"><title>")
            .Append(Html(Title))
            .Append("</title><style>")
            .Append("@page{size:A5 landscape;margin:7mm}*")
            .Append("{box-sizing:border-box}body{margin:0;color:#111;font-family:'Microsoft YaHei','SimSun','PingFang SC',sans-serif;font-size:8pt}")
            .Append("h1{margin:0 0 4mm;text-align:center;font-size:16pt}")
            .Append(".meta{margin-bottom:3mm;font-size:0}.meta span{display:inline-block;width:25%;padding-right:2mm;vertical-align:top;font-size:8pt;word-wrap:break-word;overflow-wrap:anywhere}")
            .Append("table{width:100%;border-collapse:collapse;table-layout:fixed}th,td{border:1px solid #333;padding:1.5mm .5mm;text-align:center;vertical-align:middle;word-wrap:break-word;overflow-wrap:anywhere}")
            .Append("th{background:#f3f3f3;font-weight:600}")
            .Append(".seq{width:").Append(ColumnWidth(9)).Append("}.style{width:").Append(ColumnWidth(20))
            .Append("}.color{width:").Append(ColumnWidth(15)).Append("}.size{width:").Append(ColumnWidth(9))
            .Append("}.qty{width:").Append(ColumnWidth(13)).Append("}.price{width:").Append(ColumnWidth(19))
            .Append("}.amount{width:").Append(ColumnWidth(23)).Append("}")
            .Append("thead{display:table-header-group}tr{page-break-inside:avoid;break-inside:avoid}")
            .Append("td.price,td.amount{text-align:right}.total{margin-top:3mm;text-align:right;font-size:11pt;font-weight:700;page-break-inside:avoid}")
            .Append(".print-help{margin:0 0 6mm;padding:3mm;border:1px solid #aaa;font-size:10pt;line-height:1.6}")
            .Append("@media screen{body{max-width:196mm;margin:8mm auto;padding:0 3mm}}@media print{.print-help{display:none!important}}")
            .Append("</style></head><body><div class=\"print-help\">打印设置：纸张选 A5，方向选横向，每张纸打印 1 页，缩放选适合页面（或默认）。")
            .Append("请关闭“页眉和页脚”，避免打印日期、页码和文件目录地址。若使用 IE，请在“页面设置”中将页眉、页脚全部设为空。")
            .Append("<br><button type=\"button\" onclick=\"window.print()\">打印销售单</button></div><h1>")
            .Append(Html(Title))
            .Append("</h1><div class=\"meta\"><span>").Append(Html(TicketNumberLabel))
            .Append("</span><span>").Append(Html(DocumentDateLabel))
            .Append("</span><span>").Append(Html(CustomerLabel))
            .Append("</span><span>").Append(Html(PhoneLabel))
            .Append("</span></div><table><colgroup><col class=\"seq\"><col class=\"style\"><col class=\"color\">");

        foreach (var _ in SizeHeaders) builder.Append("<col class=\"size\">");
        builder.Append("<col class=\"qty\"><col class=\"price\"><col class=\"amount\"></colgroup><thead><tr>")
            .Append("<th>序号</th><th>货号</th><th>颜色</th>");
        foreach (var size in SizeHeaders) builder.Append("<th>").Append(Html(size)).Append("</th>");
        builder.Append("<th>数量</th><th>单价</th><th>金额</th></tr></thead><tbody>");

        foreach (var line in Lines)
        {
            builder.Append("<tr><td>").Append(line.Sequence)
                .Append("</td><td>").Append(Html(line.StyleNumber))
                .Append("</td><td>").Append(Html(line.Color)).Append("</td>");
            foreach (var size in line.Sizes) builder.Append("<td>").Append(Html(size.Quantity)).Append("</td>");
            builder.Append("<td>").Append(Html(line.Quantity))
                .Append("</td><td class=\"price\">").Append(Html(line.UnitPrice))
                .Append("</td><td class=\"amount\">").Append(Html(line.Amount))
                .Append("</td></tr>");
        }

        builder.Append("</tbody></table><div class=\"total\">总计：")
            .Append(Html(TotalAmount))
            .Append("</div><script>window.onload=function(){setTimeout(function(){window.print();},300);};</script></body></html>");
        return builder.ToString();
    }
}

public sealed record SalesOrderPreviewLine(
    int Sequence,
    string StyleNumber,
    string Color,
    IReadOnlyList<SalesOrderPreviewSizeCell> Sizes,
    string Quantity,
    string UnitPrice,
    string Amount);

public sealed record SalesOrderPreviewSizeCell(string SizeCode, string Quantity);

public enum InventoryFormMode
{
    Product,
    Sku,
    EditSku,
    Receive,
    Adjust,
}

public sealed record InventoryFormData(
    InventoryFormMode Mode,
    Guid? ProductId,
    Guid? WarehouseId,
    Guid? SkuId,
    string StyleNumber,
    string ProductName,
    string Category,
    string Season,
    string BasePriceText,
    string CostPriceText,
    IReadOnlyList<InventoryVariantFormData> Variants,
    string Barcode,
    string QuantityText,
    string Note,
    string SelectedWarehouseCode,
    IReadOnlyList<WarehouseFormData> Warehouses,
    IReadOnlyList<string> Categories,
    IReadOnlyList<InventoryStockLineFormData> StockLines);

public sealed record InventoryVariantFormData(string ColorCode, string SizeCode, long Quantity);
public sealed record InventoryStockLineFormData(Guid SkuId, long Quantity);
public sealed record WarehouseFormData(Guid? Id, string Code, string Name);
