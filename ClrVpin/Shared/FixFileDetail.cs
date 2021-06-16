using ClrVpin.Models;

namespace ClrVpin.Shared
{
    public class FixFileDetail : FileDetail
    {
        public FixFileDetail(ContentTypeEnum contentTypeEnum, HitTypeEnum hitType, bool deleted, bool renamed, string path, long size) : base(path, size)
        {
            ContentType = contentTypeEnum;
            HitType = hitType;
            Deleted = deleted;
            Renamed = renamed;
        }

        public ContentTypeEnum ContentType { get; }
        public HitTypeEnum HitType { get; }
        public bool Deleted { get; set; }
        public bool Renamed { get; }
        public bool Ignored => !Deleted && !Renamed;
    }
}