using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ClrVpin.Models.Shared;

namespace ClrVpin.Converters
{
    public class HitTypeEnumToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (HitTypeEnum) value! == HitTypeEnum.Fuzzy ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}