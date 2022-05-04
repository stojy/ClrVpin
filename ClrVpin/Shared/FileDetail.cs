using ClrVpin.Models;
using ClrVpin.Models.Shared;

namespace ClrVpin.Shared
{
    public enum FixFileTypeEnum
    {
        Deleted,
        Renamed,
        Merged,
        Ignored, // ignore criteria not satisfied
        Skipped  // check/fix type not selected
    }

    public class FileDetail
    {
        public FileDetail(ContentTypeEnum contentTypeEnum, HitTypeEnum hitType, FixFileTypeEnum fixFileType, string path, long size)
        {
            Path = path;
            Size = size;

            ContentType = contentTypeEnum;
            HitType = hitType;
            Deleted = fixFileType == FixFileTypeEnum.Deleted;
            Renamed = fixFileType == FixFileTypeEnum.Renamed;
            Merged = fixFileType == FixFileTypeEnum.Merged;
            Ignored = fixFileType == FixFileTypeEnum.Ignored;
            Skipped = fixFileType == FixFileTypeEnum.Skipped;
        }

        public ContentTypeEnum ContentType { get; }
        public HitTypeEnum HitType { get; }
        
        public bool Deleted { get; set; }
        public bool Renamed { get; }
        public bool Merged { get; set; }
        public bool Ignored { get; set; }
        public bool Skipped { get; set; }

        public string Path { get; }
        public long Size { get; }
    }
}