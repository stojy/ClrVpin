using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ClrVpin.Logging
{
    [ValueConversion(typeof(Level), typeof(Color))]
    public class LogLevelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is Level))
                return DependencyProperty.UnsetValue;

            var color = (Level) value switch
            {
                Level.Debug => Colors.DarkGray,
                Level.Warn => Colors.Coral,
                Level.Error => Colors.Red,
                Level.InfoHighlight => Color.FromArgb(0xff,0x00, 0xc8, 0x53), // styles.xaml: AccentLightBrush
                _ => Colors.White   // info
            };

            return new SolidColorBrush(color);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
