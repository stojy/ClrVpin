using ClrVpin.Models;

namespace ClrVpin.Scanner
{
    public class FixFileDetail : FileDetail
    {
        public FixFileDetail(HitType hitType, bool deleted, bool renamed, string path, long size) : base(path, size)
        {
            HitType = hitType;
            Deleted = deleted;
            Renamed = renamed;
        }

        public HitType HitType { get; }
        public bool Deleted { get; }
        public bool Renamed { get; }
    }
}