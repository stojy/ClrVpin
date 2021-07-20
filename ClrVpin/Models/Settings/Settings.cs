using PropertyChanged;

namespace ClrVpin.Models.Settings
{
    [AddINotifyPropertyChangedInterface]
    public class Settings
    {
        public Settings()
        {
            // default settings - will be overwritten AFTER ctor by the deserialized settings if they exist
            TableFolder = @"C:\vp\tables\vpx";
            FrontendFolder = @"C:\vp\apps\PinballX";
        }

        public string TableFolder { get; set; }
        public string FrontendFolder { get; set; }
    }
}