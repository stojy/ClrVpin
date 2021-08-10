using System.ComponentModel;

namespace ClrVpin.Models
{
    public enum ContentTypeEnum
    {
        // front end - PBX/PBY
        [Description("Database")]
        Database,

        [Description("Table Audio")]
        TableAudio,

        [Description("Launch Audio")]
        LaunchAudio,

        [Description("Table Videos")]
        TableVideos,

        [Description("Backglass Videos")]
        BackglassVideos,

        [Description("Wheel Images")]
        WheelImages,

        [Description("Topper Videos")]
        TopperVideos,

        [Description("Instruction Cards")]
        InstructionCards,

        //[Description("Flyer Images")]
        //FlyerImages,

        // pinball - VPX
        [Description("Tables")]
        Tables,

        [Description("Backglasses")]
        Backglasses,

        [Description("Point Of Views")]
        PointOfViews
    }
}