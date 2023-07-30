using System.ComponentModel;

namespace ClrVpin.Models.Feeder;

public enum TableNewFileOptionEnum
{
    [Description("Tables")] Tables,
    [Description("Backglasses")] Backglasses,
    [Description("DMDs")] DMDs,
    [Description("Wheels")] Wheels,
    [Description("ROMs")] ROMs,
    [Description("Media Packs")] MediaPacks,
    [Description("Sounds")] Sounds,
    [Description("Toppers")] Toppers,
    [Description("PuP Packs")] PuPPacks,
    [Description("POVs")] POVs,
    [Description("Alt. Sounds")] AlternateSounds,
    [Description("Rules")] Rules,
}