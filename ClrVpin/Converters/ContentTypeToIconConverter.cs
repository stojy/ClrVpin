using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ClrVpin.Models;
using MaterialDesignThemes.Wpf;

namespace ClrVpin.Converters
{
    [ValueConversion(typeof(ContentTypeEnum), typeof(string))]
    public class ContentTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is ContentTypeEnum))
                return DependencyProperty.UnsetValue;

            var icon = (ContentTypeEnum) value switch
            {
                ContentTypeEnum.Database => PackIconKind.Database,
                ContentTypeEnum.TableAudio => PackIconKind.Music,
                ContentTypeEnum.LaunchAudio => PackIconKind.MusicNote,
                ContentTypeEnum.BackglassVideos => PackIconKind.Video,
                ContentTypeEnum.TableVideos => PackIconKind.Video3dVariant,
                ContentTypeEnum.WheelImages => PackIconKind.Image,
                _ => PackIconKind.Help
            };

            return icon.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
