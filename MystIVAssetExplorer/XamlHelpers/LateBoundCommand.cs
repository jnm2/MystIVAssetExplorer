using Avalonia;
using Avalonia.Data;
using System;
using System.Windows.Input;

namespace MystIVAssetExplorer.XamlHelpers;

public sealed class LateBoundCommand : AvaloniaObject, ICommand
{
    private static readonly StyledProperty<ICommand?> CommandProperty = AvaloniaProperty.Register<AvaloniaObject, ICommand?>("Command");

    public IBinding? Binding { get; set; }

    event EventHandler? ICommand.CanExecuteChanged { add { } remove { } }

    private ICommand? GetCommand()
    {
        if (Binding is null) return null;

        using (Bind(CommandProperty, Binding))
            return GetValue(CommandProperty);
    }

    public bool CanExecute(object? parameter)
    {
        return GetCommand()?.CanExecute(parameter) ?? false;
    }

    public void Execute(object? parameter)
    {
        GetCommand()?.Execute(parameter);
    }
}
