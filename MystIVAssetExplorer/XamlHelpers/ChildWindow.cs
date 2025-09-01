using Avalonia;
using System;

namespace MystIVAssetExplorer.XamlHelpers;

public class ChildWindow : AvaloniaObject
{
    public static readonly StyledProperty<Type> DataTypeProperty =
        AvaloniaProperty.Register<ChildWindow, Type>(nameof(ViewType));

    public static readonly StyledProperty<object?> ContentProperty =
        AvaloniaProperty.Register<ChildWindow, object?>(nameof(Content), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public Type ViewType
    {
        get => GetValue(DataTypeProperty);
        set => SetValue(DataTypeProperty, value);
    }

    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }
}