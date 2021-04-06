using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Utils;

namespace ClrVpx.Models
{
    public class HitTypeDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is HitType hitType ? hitType.GetDescription() : DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}