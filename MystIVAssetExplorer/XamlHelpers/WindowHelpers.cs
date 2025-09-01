using Avalonia;
using Avalonia.Controls;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace MystIVAssetExplorer.XamlHelpers;

public static partial class WindowHelpers
{
    public static readonly AttachedProperty<bool> IsToolWindowProperty =
        AvaloniaProperty.RegisterAttached<Window, bool>("IsToolWindow", typeof(WindowHelpers));

    public static bool GetIsToolWindow(Window element) => element.GetValue(IsToolWindowProperty);

    public static void SetIsToolWindow(Window element, bool value) => element.SetValue(IsToolWindowProperty, value);

    static WindowHelpers()
    {
        IsToolWindowProperty.Changed.Subscribe(args =>
        {
            if (!OperatingSystem.IsWindows()) return;

            var window = (Window)args.Sender;

            window.Opened += (_, _) =>
            {
                var handle = window.TryGetPlatformHandle()!.Handle;

                var exStyle = GetWindowLongW(handle, GWL_EXSTYLE);
                if (exStyle == 0) throw new Win32Exception();

                exStyle |= WS_EX_TOOLWINDOW;

                if (SetWindowLongW(handle, GWL_EXSTYLE, exStyle) == 0)
                    throw new Win32Exception();
            };
        });
    }

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial int GetWindowLongW(nint hWnd, int nIndex);

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial int SetWindowLongW(nint hWnd, int nIndex, int dwNewLong);
}
