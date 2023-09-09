using System.ComponentModel;

namespace ClrVpin.Models.Feeder;

public enum MiscFeatureOptionEnum
{
    [Description("Standard")] Standard,
    [Description("Full DMD")] FullDmd,
    [Description("VR Only")] VirtualRealityOnly,
    [Description("FSS Only")] FullSingleScreenOnly,
    [Description("Music/Sound Mod")] MusicOrSoundMod,
    [Description("Black & White Mod")] BlackAndWhiteMod,
    [Description("Patch")] Patch
}