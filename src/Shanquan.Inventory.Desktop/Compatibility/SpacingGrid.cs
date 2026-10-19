using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;

namespace Shanquan.Inventory.Desktop.Compatibility;

/// <summary>
/// Backports Avalonia 12 Grid spacing to the Avalonia 11 runtime used by the
/// Windows 7 build. Spacing is applied inside each non-leading cell so the
/// existing page layout keeps the same visual separation after the downgrade.
/// </summary>
public sealed class SpacingGrid : Grid
{
    public static readonly StyledProperty<double> ColumnSpacingProperty =
        AvaloniaProperty.Register<SpacingGrid, double>(nameof(ColumnSpacing));

    public static readonly StyledProperty<double> RowSpacingProperty =
        AvaloniaProperty.Register<SpacingGrid, double>(nameof(RowSpacing));

    private readonly Dictionary<Control, Thickness> _baseMargins = new();

    public double ColumnSpacing
    {
        get => GetValue(ColumnSpacingProperty);
        set => SetValue(ColumnSpacingProperty, value);
    }

    public double RowSpacing
    {
        get => GetValue(RowSpacingProperty);
        set => SetValue(RowSpacingProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        ApplySpacing();
        return base.MeasureOverride(availableSize);
    }

    private void ApplySpacing()
    {
        foreach (var child in Children)
        {
            if (!_baseMargins.TryGetValue(child, out var baseMargin))
            {
                baseMargin = child.Margin;
                _baseMargins.Add(child, baseMargin);
            }

            var left = GetColumn(child) > 0 ? ColumnSpacing : 0;
            var top = GetRow(child) > 0 ? RowSpacing : 0;
            child.Margin = new Thickness(
                baseMargin.Left + left,
                baseMargin.Top + top,
                baseMargin.Right,
                baseMargin.Bottom);
        }
    }
}
