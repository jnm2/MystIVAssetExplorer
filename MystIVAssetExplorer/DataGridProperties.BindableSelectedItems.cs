namespace MystIVAssetExplorer;

using Avalonia;
using Avalonia.Controls;
using System;
using System.Collections;
using System.Collections.Specialized;

public static class DataGridProperties
{
    public static readonly AttachedProperty<IList?> BindableSelectedItemsProperty =
        AvaloniaProperty.RegisterAttached<DataGrid, IList?>(
            "BindableSelectedItems", typeof(DataGridProperties));

    public static void SetBindableSelectedItems(DataGrid grid, IList? value) =>
        grid.SetValue(BindableSelectedItemsProperty, value);

    public static IList? GetBindableSelectedItems(DataGrid grid) =>
        grid.GetValue(BindableSelectedItemsProperty);

    static DataGridProperties()
    {
        BindableSelectedItemsProperty.Changed.AddClassHandler<DataGrid>((grid, e) =>
        {
            (e.OldValue as INotifyCollectionChanged)?.CollectionChanged -= OnBindingCollectionChanged;
            (e.NewValue as INotifyCollectionChanged)?.CollectionChanged += OnBindingCollectionChanged;
            OnBindingCollectionChanged(e.NewValue, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

            grid.SelectionChanged -= OnGridSelectionChanged;
            grid.SelectionChanged += OnGridSelectionChanged;

            void OnGridSelectionChanged(object? sender, SelectionChangedEventArgs e)
            {
                if (grid.GetValue(BindableSelectedItemsProperty) is not { IsReadOnly: false, IsFixedSize: false } collection)
                    throw new NotImplementedException();

                foreach (var item in e.RemovedItems)
                    collection.Remove(item);

                foreach (var item in e.AddedItems)
                    if (!collection.Contains(item))
                        collection.Add(item);
            }

            void OnBindingCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
            {
                if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                    var collection = (IList)sender!;

                    for (var i = grid.SelectedItems.Count - 1; i >= 0; i--)
                    {
                        if (!collection.Contains(grid.SelectedItems[i]))
                            grid.SelectedItems.RemoveAt(i);
                    }

                    foreach (var item in collection)
                        if (!grid.SelectedItems.Contains(item))
                            grid.SelectedItems.Add(item);
                    return;
                }

                if (e.Action is NotifyCollectionChangedAction.Remove or NotifyCollectionChangedAction.Replace)
                {
                    foreach (var item in e.OldItems!)
                        grid.SelectedItems.Remove(item);
                }

                if (e.Action is NotifyCollectionChangedAction.Add or NotifyCollectionChangedAction.Replace)
                {
                    foreach (var item in e.NewItems!)
                        if (!grid.SelectedItems.Contains(item))
                            grid.SelectedItems.Add(item);
                }
            }
        });
    }
}
