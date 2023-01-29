using ClrVpin.Models.Feeder;
using PropertyChanged;

namespace ClrVpin.Models.Shared;

[AddINotifyPropertyChangedInterface]
public class HitType : EnumOption<HitTypeEnum>
{
    public HitType(HitTypeEnum hitTypeEnum, bool fixable, string tip, bool isHighlighted = false, string helpUrl = null)
    {
        Enum = hitTypeEnum;
        Fixable = fixable;
        Tip = tip;
        IsHighlighted = isHighlighted;
        HelpUrl = helpUrl;
    }

    public bool Fixable { get; }
}