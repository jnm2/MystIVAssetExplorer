using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using System;
using System.Globalization;

namespace MystIVAssetExplorer;

public sealed class BoolToObjectConverter : MarkupExtension, IValueConverter
{
    public object? TrueValue { get; set; }
    public object? FalseValue { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (bool)value! ? TrueValue : FalseValue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return Equals(TrueValue, value);
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return this;
    }
}
