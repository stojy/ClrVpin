//using System;
//using System.Drawing;
//using System.Globalization;
//using System.Windows;
//using System.Windows.Data;
//using System.Windows.Media;
//using Brush = System.Drawing.Brush;

//namespace ClrVpin.Converters
//{
//    [ValueConversion(typeof(bool), typeof(Brush))]
//    public class BoolToBrushConverter : IValueConverter
//    {
//        public string True { get; set; } = "SecondaryHueDarkBrush";
//        public string False { get; set; } = "PrimaryHueDarkBrush";

//        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
//        {
//            if (!(value is bool))
//                return DependencyProperty.UnsetValue;

//            return (Brush)value ? new Brush(True) : False;
//        }

//        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
//    }
//}