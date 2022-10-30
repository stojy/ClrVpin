using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Utils.Extensions;

namespace ClrVpin.Converters;

public class NullOrEmptyToUnsetConverter :IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string stringValue)
        {
            // return UnsetValue for those controls that don't handle binding to null or empty string values, e.g. Image.Source
            // - https://stackoverflow.com/a/5628347/227110

            if (stringValue.IsEmpty())
                return DependencyProperty.UnsetValue;
            return value;
        }

        return value ?? DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
