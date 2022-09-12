using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ClrVpin.Converters
{
    [ValueConversion(typeof(bool), typeof(SolidColorBrush))]
    public class BoolToBrushConverter : IValueConverter
    {
        public SolidColorBrush TrueBrush { get; set; }
        public SolidColorBrush FalseBrush { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not bool boolValue)
                return DependencyProperty.UnsetValue;

            return boolValue ? TrueBrush : FalseBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}