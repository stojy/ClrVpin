using System.ComponentModel;

namespace ClrVpin.Models.Feeder;

public enum IgnoreFeatureOptionEnum
{
    [Description("VR Only")] VirtualRealityOnly,
    [Description("Music/Sound Mod")] MusicOrSoundMod,
    [Description("Full DMD")] FullDmd
}