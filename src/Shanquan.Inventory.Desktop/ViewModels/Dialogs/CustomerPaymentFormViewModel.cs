using CommunityToolkit.Mvvm.ComponentModel;
using Shanquan.Inventory.Application.CoreBusiness;
using Shanquan.Inventory.Desktop.Services;

namespace Shanquan.Inventory.Desktop.ViewModels.Dialogs;

public sealed partial class CustomerPaymentFormViewModel : ViewModelBase
{
    public CustomerPaymentFormViewModel(IReadOnlyList<CustomerListItem> customers, CustomerListItem? customer)
    {
        Customers = customers.Where(x => x.ReceivableCents > 0).OrderByDescending(x => x.ReceivableCents).ToList();
        SelectedCustomer = Customers.FirstOrDefault(x => x.Id == customer?.Id) ?? Customers.FirstOrDefault();
        AmountText = ((SelectedCustomer?.ReceivableCents ?? 0) / 100m).ToString("0.00");
    }

    public IReadOnlyList<CustomerListItem> Customers { get; }
    public IReadOnlyList<string> PaymentMethods { get; } = ["现金", "微信", "支付宝", "银行卡", "其他"];
    [ObservableProperty] private CustomerListItem? _selectedCustomer;
    [ObservableProperty] private string _amountText = "";
    [ObservableProperty] private DateTime? _receivedDate = DateTime.Today;
    [ObservableProperty] private string _selectedPaymentMethod = "微信";
    [ObservableProperty] private string _note = "";
    [ObservableProperty] private string _errorMessage = "";
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public string CustomerBalanceHint => SelectedCustomer is null ? "请选择欠款客户" : $"当前欠款 {SelectedCustomer.ReceivableText}";

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));
    partial void OnSelectedCustomerChanged(CustomerListItem? value)
    {
        OnPropertyChanged(nameof(CustomerBalanceHint));
        if (value is not null) AmountText = (value.ReceivableCents / 100m).ToString("0.00");
    }

    public CustomerPaymentFormData? BuildResult()
    {
        if (SelectedCustomer is null) return Fail("请选择欠款客户。");
        if (!decimal.TryParse(AmountText, out var amount) || amount <= 0) return Fail("请输入正确的收款金额。");
        var cents = checked((long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero));
        if (cents > SelectedCustomer.ReceivableCents) return Fail("收款金额不能大于客户当前欠款。");
        if (ReceivedDate is null) return Fail("请选择收款日期。");
        ErrorMessage = "";
        return new CustomerPaymentFormData(
            SelectedCustomer.Id,
            AmountText.Trim(),
            new DateTimeOffset(DateTime.SpecifyKind(ReceivedDate.Value.Date, DateTimeKind.Local)),
            SelectedPaymentMethod,
            Note.Trim());
    }

    private CustomerPaymentFormData? Fail(string message)
    {
        ErrorMessage = message;
        return null;
    }
}
