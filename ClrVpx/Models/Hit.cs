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
                Size = type == HitType.Missing ? 0 : new FileInfo(path).Length;
                SizeString = type == HitType.Missing ? null : ByteSize.FromBytes(new FileInfo(path).Length).ToString("#");
                Type = type;

                // performance tweak - explicitly assign a property instead of relying on ToString during subsequent binding
                Description = ToString();
            }
        }


        public string Path { get; }
        public string File { get; }
        public string SizeString { get; }
        public long Size { get; set; }
        public HitType Type { get;  }
        public string MediaType { get; }
        public string Description { get; }

        public sealed override string ToString() => $"{MediaType} - {Type.GetDescription()}: {Path}";

        public static HitType[] Types = { HitType.TableName, HitType.Fuzzy, HitType.WrongCase, HitType.DuplicateExtension, HitType.Missing };
    }

    public enum HitType
    {
        [Description("Perfect Match!!")]    // not displayed
        Valid,
        
        [Description("Table Name")]
        TableName,
        
        [Description("Fuzzy Name")]
        Fuzzy,
        
        [Description("Wrong Case")]
        WrongCase,
        
        [Description("Duplicate")]
        DuplicateExtension,

        [Description("Missing")]
        Missing
    }
}