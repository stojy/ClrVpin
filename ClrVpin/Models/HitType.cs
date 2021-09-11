namespace ClrVpin.Models
{
    public class HitType
    {
        public HitType(HitTypeEnum hitTypeEnum, bool fixable, string tip, string helpUrl = null)
        {
            Enum = hitTypeEnum;
            Fixable = fixable;
            Tip = tip;
            HelpUrl = helpUrl;
        }

        public HitTypeEnum Enum { get; set; }
        public string Tip { get; set; }
        public bool Fixable { get; set; }
        public string Description { get; set; }
        public string HelpUrl { get; set; }
    }
}