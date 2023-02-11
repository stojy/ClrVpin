using System.ComponentModel;

namespace ClrVpin.Models.Shared;

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

    [Description("Flyer Images\\Back")]
    FlyerImagesBack,
    [Description("Flyer Images\\Front")]
    FlyerImagesFront,
    [Description("Flyer Images\\Inside1")]
    FlyerImagesInside1,
    [Description("Flyer Images\\Inside2")]
    FlyerImagesInside2,
    [Description("Flyer Images\\Inside3")]
    FlyerImagesInside3,
    [Description("Flyer Images\\Inside4")]
    FlyerImagesInside4,
    [Description("Flyer Images\\Inside5")]
    FlyerImagesInside5,
    [Description("Flyer Images\\Inside6")]
    FlyerImagesInside6,

    // pinball - VPX
    [Description("Tables")]
    Tables,

    [Description("Backglasses")]
    Backglasses,

    [Description("Point Of Views")]
    PointOfViews
}