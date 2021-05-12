using System.ComponentModel;

namespace ClrVpin.Models
{
    public enum ContentTypeEnum
    {
        // not displayed
        [Description("Database")] Database,
        [Description("Table Audio")] TableAudio,
        [Description("Launch Audio")] LaunchAudio,
        [Description("Table Videos")] TableVideos,
        [Description("Backglass Videos")] BackglassVideos,
        [Description("Wheel Images")] WheelImages
    }
}