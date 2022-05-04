namespace ClrVpin.Models.Shared
{
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

        public HitTypeEnum Enum { get; set; }
        public string Tip { get; set; }
        public bool Fixable { get; set; }
        public string Description { get; set; }
        public string HelpUrl { get; set; }
        public bool IsHighlighted { get; set; }
    }
}