using Avalonia;
using Avalonia.Controls;
using System;

namespace MystIVAssetExplorer;

public static class TreeViewBehaviors
{
    public static readonly AttachedProperty<bool> AutoExpandSingleChildProperty =
        AvaloniaProperty.RegisterAttached<TreeViewItem, bool>(
            "AutoExpandSingleChild", typeof(TreeViewBehaviors));

    public static void SetAutoExpandSingleChild(AvaloniaObject element, bool value) =>
        element.SetValue(AutoExpandSingleChildProperty, value);

    public static bool GetAutoExpandSingleChild(AvaloniaObject element) =>
        element.GetValue(AutoExpandSingleChildProperty);

    static TreeViewBehaviors()
    {
        AutoExpandSingleChildProperty.Changed.Subscribe(args =>
        {
            if (args.Sender is TreeViewItem item && args.NewValue.Value)
            {
                item.GetPropertyChangedObservable(TreeViewItem.IsExpandedProperty).Subscribe(ev =>
                {
                    if ((bool)ev.NewValue! && GetAutoExpandSingleChild(item) && item.Presenter is not null)
                    {
                        item.Presenter.ApplyTemplate();

                        if (item.Presenter.Panel!.Children is [TreeViewItem { IsExpanded: false } child])
                            child.IsExpanded = true;
                    }
                });
            }
        });
    }
}
