using CommunityToolkit.Mvvm.ComponentModel;
using Shanquan.Inventory.Application.CoreBusiness;
using Shanquan.Inventory.Desktop.Services;

namespace Shanquan.Inventory.Desktop.ViewModels.Dialogs;

public sealed partial class CustomerFormViewModel : ViewModelBase
{
    private readonly Guid? _id;

    public CustomerFormViewModel(CustomerListItem? customer)
    {
        _id = customer?.Id;
        Title = customer is null ? "新增客户" : "编辑客户";
        Code = customer?.Code ?? "";
        Name = customer?.Name ?? "";
        Phone = customer?.Phone ?? "";
        Address = customer?.Address ?? "";
        CreditLimitText = ((customer?.CreditLimitCents ?? 0) / 100m).ToString("0.00");
        PaymentTermDaysText = (customer?.PaymentTermDays ?? 30).ToString();
    }

    public string Title { get; }
    [ObservableProperty] private string _code;
    [ObservableProperty] private string _name;
    [ObservableProperty] private string _phone;
    [ObservableProperty] private string _address;
    [ObservableProperty] private string _creditLimitText;
    [ObservableProperty] private string _paymentTermDaysText;
    [ObservableProperty] private string _errorMessage = "";
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));

    public CustomerFormData? BuildResult()
    {
        if (string.IsNullOrWhiteSpace(Code)) return Fail("客户编号不能为空。");
        if (string.IsNullOrWhiteSpace(Name)) return Fail("客户名称不能为空。");
        if (!decimal.TryParse(CreditLimitText, out var credit) || credit < 0) return Fail("信用额度格式不正确。");
        if (!int.TryParse(PaymentTermDaysText, out var paymentTermDays) || paymentTermDays is < 0 or > 3650)
            return Fail("账期天数必须是 0 到 3650 之间的整数。");
        ErrorMessage = "";
        return new CustomerFormData(_id, Code.Trim(), Name.Trim(), Phone.Trim(), Address.Trim(), CreditLimitText.Trim(), paymentTermDays.ToString());
    }

    private CustomerFormData? Fail(string message)
    {
        ErrorMessage = message;
        return null;
    }
}
