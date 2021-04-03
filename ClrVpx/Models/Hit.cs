using System.ComponentModel;

namespace ClrVpx.Models
{
    public class Hit
    {
        public string Path { get; set; }
        public string File { get; set; }
        public string Size { get; set; }
        
        public HitType Type { get; set; }
    }

    public enum HitType
    {
        [Description("Perfect match!!")]    // not displayed
        Valid,
        
        [Description("Table name matched")]
        TableName,
        
        [Description("Fuzzy name matched")]
        Fuzzy,
        
        [Description("Wrong case matched")]
        WrongCase,
        
        [Description("Duplicate file extension found")]
        DuplicateExtension
    }
}