using ClrVpin.Models;

namespace ClrVpin.Scanner
{
    public class FixFileDetail : FileDetail
    {
        public FixFileDetail(HitTypeEnum hitType, bool deleted, bool renamed, string path, long size) : base(path, size)
        {
            HitType = hitType;
            Deleted = deleted;
            Renamed = renamed;
        }

        public HitTypeEnum HitType { get; }
        public bool Deleted { get; set; }
        public bool Renamed { get; }
        public bool Ignored => !Deleted && !Renamed;
    }
}