namespace MystIVAssetExplorer.XamlHelpers;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.Windows.Input;

public static class DataGridRowHelpers
{
    public static readonly AttachedProperty<ICommand?> DoubleTappedProperty =
        AvaloniaProperty.RegisterAttached<DataGridRow, ICommand?>(
            "DoubleTapped", typeof(DataGridRowHelpers));

    public static void SetDoubleTapped(DataGridRow row, ICommand? value) => row.SetValue(DoubleTappedProperty, value);

    public static ICommand? GetDoubleTapped(DataGridRow row) => row.GetValue(DoubleTappedProperty);

    static DataGridRowHelpers()
    {
        DoubleTappedProperty.Changed.AddClassHandler<DataGridRow>((row, e) =>
        {
            if (e.OldValue is null)
            {
                if (e.NewValue is not null)
                    row.DoubleTapped += OnRowDoubleTapped;
            }
            else
            {
                if (e.NewValue is null)
                    row.DoubleTapped -= OnRowDoubleTapped;
            }
        });
    }

    private static void OnRowDoubleTapped(object? sender, TappedEventArgs e)
    {
        var row = (DataGridRow)sender!;

        var command = GetDoubleTapped(row);
        if (command is not null && command.CanExecute(row))
            command.Execute(row);
    }
}
