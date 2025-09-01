using System;

namespace MystIVAssetExplorer.XamlHelpers;

public static partial class WindowHelpers
{
    static WindowHelpers()
    {
        ChildWindowsProperty.Changed.Subscribe(OnChildWindowsPropertyChanged);
        IsToolWindowProperty.Changed.Subscribe(OnIsToolWindowPropertyChanged);
    }
}
