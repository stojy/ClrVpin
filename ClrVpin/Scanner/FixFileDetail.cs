using ClrVpin.Models;

namespace ClrVpin.Scanner
{
    public class FixFileDetail : FileDetail
    {
        public FixFileDetail(HitType hitType, bool deleted, string path, long size) : base(path, size)
        {
            HitType = hitType;
            Deleted = deleted;
        }

        public HitType HitType { get; }
        public bool Deleted { get; }
    }
}