using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ClrVpin.Converters
{
    [ValueConversion(typeof(object), typeof(Visibility))]
    public class NegativeToVisibilityConverter : IValueConverter
    {
        public Visibility True { get; set; } = Visibility.Collapsed;
        public Visibility False { get; set; } = Visibility.Visible;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int?)value < 0 ? True : False;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}