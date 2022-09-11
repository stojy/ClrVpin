using PropertyChanged;

namespace ClrVpin.Models.Shared;

[AddINotifyPropertyChangedInterface]
public class HitType
{
    public HitType(HitTypeEnum hitTypeEnum, bool fixable, string tip, bool isHighlighted = false, string helpUrl = null)
    {
        Enum = hitTypeEnum;
        Fixable = fixable;
        Tip = tip;
        IsHighlighted = isHighlighted;
        HelpUrl = helpUrl;
    }

    public HitTypeEnum Enum { get; }
    public string Tip { get; }
    public bool Fixable { get; }
    public string Description { get; set; }
    public string HelpUrl { get; }
    public bool IsHighlighted { get; }
}