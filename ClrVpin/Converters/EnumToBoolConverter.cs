using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ClrVpin.Converters;

[ValueConversion(typeof(Enum), typeof(bool))]
public class EnumToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is not Enum enumParameter|| value is not Enum enumValue)
            return DependencyProperty.UnsetValue;

        return enumParameter.Equals(enumValue);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}