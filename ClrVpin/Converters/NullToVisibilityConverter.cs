using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ClrVpin.Converters
{
    [ValueConversion(typeof(object), typeof(Visibility))]
    public class NullToVisibilityConverter : IValueConverter
    {
        public Visibility True { get; set; } = Visibility.Visible;
        public Visibility False { get; set; } = Visibility.Collapsed;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? True : False;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}