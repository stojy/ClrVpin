using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ClrVpin.Converters
{
    public class BoolToStyleConverter : IValueConverter
    {
        public Style True { get; set; }
        public Style False { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is true ? True : False;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}