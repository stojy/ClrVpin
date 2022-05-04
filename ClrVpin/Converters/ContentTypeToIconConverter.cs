using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ClrVpin.Models;
using ClrVpin.Models.Shared;
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
                ContentTypeEnum.Tables => PackIconKind.GamepadVariant,
                ContentTypeEnum.Backglasses => PackIconKind.ImageArea,
                ContentTypeEnum.PointOfViews => PackIconKind.EyeOutline,
                ContentTypeEnum.Database => PackIconKind.Database,
                ContentTypeEnum.TableAudio => PackIconKind.Music,
                ContentTypeEnum.LaunchAudio => PackIconKind.MusicNote,
                ContentTypeEnum.BackglassVideos => PackIconKind.Video,
                ContentTypeEnum.TableVideos => PackIconKind.Video3dVariant,
                ContentTypeEnum.WheelImages => PackIconKind.Image,
                ContentTypeEnum.TopperVideos => PackIconKind.VideoVintage,
                ContentTypeEnum.InstructionCards => PackIconKind.FileDocumentOutline,
                ContentTypeEnum.FlyerImagesBack => PackIconKind.SignRealEstate,
                ContentTypeEnum.FlyerImagesFront => PackIconKind.SignRealEstate,
                ContentTypeEnum.FlyerImagesInside1 => PackIconKind.SignRealEstate,
                ContentTypeEnum.FlyerImagesInside2 => PackIconKind.SignRealEstate,
                ContentTypeEnum.FlyerImagesInside3 => PackIconKind.SignRealEstate,
                ContentTypeEnum.FlyerImagesInside4 => PackIconKind.SignRealEstate,
                ContentTypeEnum.FlyerImagesInside5 => PackIconKind.SignRealEstate,
                ContentTypeEnum.FlyerImagesInside6 => PackIconKind.SignRealEstate,
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
