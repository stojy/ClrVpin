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
        Valid,
        TableName,
        Fuzzy,
        WrongCase,
        Duplicate, // extension
    }
}