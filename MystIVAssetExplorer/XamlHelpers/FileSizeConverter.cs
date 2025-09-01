using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using System;
using System.Globalization;

namespace MystIVAssetExplorer.XamlHelpers;

public class FileSizeConverter : MarkupExtension, IValueConverter
{
    private readonly string[] suffixes = ["bytes", "KB", "MB", "GB", "TB"];

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return null;

        var size = System.Convert.ToDouble(value);

        var suffixIndex = 0;

        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        var formatString = size < 10 ? "0.##" : size < 100 ? "0.#" : "0";

        return size.ToString(formatString, culture) + " " + suffixes[suffixIndex];
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return this;
    }
}
