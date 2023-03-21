using System.ComponentModel;

namespace ClrVpin.Models.Feeder;

public enum IgnoreFeatureOptionEnum
{
    [Description("VR Only")] VirtualRealityOnly,
    [Description("Full DMD")] FullDmd
}