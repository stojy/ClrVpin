using PropertyChanged;

namespace ClrVpx.Settings
{
    [AddINotifyPropertyChangedInterface]
    public class Settings
    {
        public static string VpxFrontendFolder { get; set; } = @"C:\vp\apps\PinballX";

        public static string VpxTablesFolder { get; set; } = @"C:\vp\tables\vpx";
    }
}