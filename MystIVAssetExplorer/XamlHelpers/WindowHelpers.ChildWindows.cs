using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace MystIVAssetExplorer.XamlHelpers;

public static partial class WindowHelpers
{
    public static readonly AttachedProperty<AvaloniaList<ChildWindow>> ChildWindowsProperty =
        AvaloniaProperty.RegisterAttached<Visual, AvaloniaList<ChildWindow>>(
            "ChildWindows",
            typeof(WindowHelpers));

    private static readonly Dictionary<ChildWindow, Window> OpenWindows = new();

    public static AvaloniaList<ChildWindow> GetChildWindows(Visual element)
    {
        var value = element.GetValue(ChildWindowsProperty);
        if (value is null)
        {
            value = new();
            element.SetValue(ChildWindowsProperty, value);
        }
        return value;
    }

    private static void OnChildWindowsPropertyChanged(AvaloniaPropertyChangedEventArgs<AvaloniaList<ChildWindow>> args)
    {
        args.NewValue.Value.CollectionChanged += (_, e) =>
        {
            if (e.Action != NotifyCollectionChangedAction.Add) throw new NotImplementedException();

            foreach (ChildWindow item in e.NewItems!)
            {
                item.PropertyChanged += (_, e) =>
                {
                    if (e.Property == ChildWindow.ContentProperty) UpdateContent((Visual)args.Sender, item);
                };
                UpdateContent((Visual)args.Sender, item);
            }
        };
    }

    private static void UpdateContent(Visual owner, ChildWindow childWindow)
    {
        if (childWindow.Content is null)
        {
            if (OpenWindows.Remove(childWindow, out var window))
                window.Close();
        }
        else
        {
            if (!OpenWindows.TryGetValue(childWindow, out var window))
            {
                window = (Window)Activator.CreateInstance(childWindow.ViewType)!;
                window.Closed += (_, _) => childWindow.Content = null;
                window.Show((Window)owner.GetVisualRoot()!);
                OpenWindows.Add(childWindow, window);
            }
            window.DataContext = childWindow.Content;
        }
    }
}
