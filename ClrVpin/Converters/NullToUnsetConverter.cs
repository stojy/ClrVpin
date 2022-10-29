using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ClrVpin.Converters;

public class NullToUnsetConverter :IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // return UnsetValue for those controls that don't handle binding to null values, e.g. Image.Source
        // - https://stackoverflow.com/a/5628347/227110
        return value ?? DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
