using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

namespace Orchard;

/// <summary>A decorative loading table with consumer-owned row and column counts.</summary>
public sealed class BleeckerSkeletonTable : Control
{
    private StackPanel? rowsHost;
    public BleeckerSkeletonTable() => DefaultStyleKey = typeof(BleeckerSkeletonTable);

    public static readonly DependencyProperty RowsProperty = DependencyProperty.Register(nameof(Rows), typeof(int), typeof(BleeckerSkeletonTable),
        new PropertyMetadata(5, (owner, _) => ((BleeckerSkeletonTable)owner).Rebuild()));
    public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register(nameof(Columns), typeof(int), typeof(BleeckerSkeletonTable),
        new PropertyMetadata(4, (owner, _) => ((BleeckerSkeletonTable)owner).Rebuild()));
    public int Rows { get => (int)GetValue(RowsProperty); set => SetValue(RowsProperty, value); }
    public int Columns { get => (int)GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }

    protected override void OnApplyTemplate()
    {
        rowsHost?.Children.Clear();
        rowsHost = null;
        base.OnApplyTemplate();
        if (Template is null) return;
        rowsHost = GetTemplateChild("PART_Rows") as StackPanel ?? throw new InvalidOperationException("SkeletonTable template requires PART_Rows.");
        Rebuild();
    }
    private void Rebuild()
    {
        if (rowsHost is null) return;
        rowsHost.Children.Clear();
        // Array.from({ length }) treats negative counts as zero, without changing the input.
        var columns = Math.Max(0, Columns);
        for (var row = 0; row <= Math.Max(0, Rows); row++)
        {
            var line = new Grid { ColumnSpacing = 8, Height = columns == 0 ? 0 : row == 0 ? 32 : 40, UseLayoutRounding = false };
            for (var column = 0; column < columns; column++)
            {
                line.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                var shape = new BleeckerSkeleton { Height = row == 0 ? 32 : 40 };
                Grid.SetColumn(shape, column);
                line.Children.Add(shape);
            }
            rowsHost.Children.Add(line);
        }
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new BleeckerSkeletonAutomationPeer(this);
}
