using System;
using System.Globalization;
using System.Windows.Data;

namespace ClrVpin.Converters
{
    [ValueConversion(typeof(object), typeof(object))]
    public class GreaterThanZeroConverter : IValueConverter
    {
        public object TrueValue { get; set; } = true;
        public object FalseValue { get; set; } = true;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int?)value > 0 ? TrueValue : FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}