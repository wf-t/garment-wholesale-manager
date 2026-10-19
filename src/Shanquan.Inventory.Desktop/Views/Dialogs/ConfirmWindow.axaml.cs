using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Shanquan.Inventory.Desktop.Views.Dialogs;

public partial class ConfirmWindow : Window
{
    public ConfirmWindow() => InitializeComponent();
    public ConfirmWindow(
        string title,
        string message,
        string confirmText,
        string? inputLabel = null,
        string? inputPlaceholder = null)
    {
        InitializeComponent();
        TitleText.Text = title;
        MessageText.Text = message;
        ConfirmButton.Content = confirmText;
        if (!string.IsNullOrWhiteSpace(inputLabel))
        {
            Height = 380;
            InputPanel.IsVisible = true;
            InputLabelText.Text = inputLabel;
            OptionalInputTextBox.Watermark = inputPlaceholder ?? "请输入";
            Opened += (_, _) => OptionalInputTextBox.Focus();
        }
    }

    public string InputText => OptionalInputTextBox.Text?.Trim() ?? "";

    private void Confirm_Click(object? sender, RoutedEventArgs e) => Close(true);
    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
