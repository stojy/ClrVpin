﻿using System.ComponentModel;

namespace ClrVpin.Models.Feeder;

public enum MiscFeatureOptionEnum
{
    [Description("VR Only")] VirtualRealityOnly,
    [Description("Music/Sound Mod")] MusicOrSoundMod,
    [Description("Black & White Mod")] BlackAndWhiteMod,
    [Description("Full DMD")] FullDmd
}