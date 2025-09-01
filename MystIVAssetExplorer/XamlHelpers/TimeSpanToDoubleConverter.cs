using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using System;
using System.Globalization;

namespace MystIVAssetExplorer.XamlHelpers;

public sealed class TimeSpanToDoubleConverter : MarkupExtension, IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return ((TimeSpan)value!).TotalSeconds;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return TimeSpan.FromSeconds((double)value!);
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return this;
    }
}
