namespace ClrVpin.Models
{
    public class HitType
    {
        public HitType(HitTypeEnum hitTypeEnum, bool fixable, string tip)
        {
            Enum = hitTypeEnum;
            Fixable = fixable;
            Tip = tip;
        }

        public HitTypeEnum Enum { get; set; }
        public string Tip { get; set; }
        public bool Fixable { get; set; }
        public string Description { get; set; }
    }
}