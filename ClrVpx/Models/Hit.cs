using System.ComponentModel;
using System.IO;
using ByteSizeLib;
using Utils;

namespace ClrVpx.Models
{
    public class Hit
    {
        public Hit(string mediaType, string path, HitType type)
        {
            {
                MediaType = mediaType;
                Path = path;
                File = System.IO.Path.GetFileName(path);
                Size = type == HitType.Missing ? null : ByteSize.FromBytes(new FileInfo(path).Length).ToString("#");
                Type = type;

                // performance tweak - explicitly assign a property instead of relying on ToString during subsequent binding
                Description = ToString();
            }
        }

        public string Path { get; }
        public string File { get; }
        public string Size { get; }
        public HitType Type { get;  }
        public string MediaType { get; }
        public string Description { get; }

        public sealed override string ToString() => $"{MediaType} - {Type.GetDescription()} : {File}";
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
        DuplicateExtension,

        [Description("Missing file")]
        Missing
    }
}