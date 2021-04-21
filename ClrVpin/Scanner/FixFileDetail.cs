namespace ClrVpin.Scanner
{
    public class FixFileDetail : FileDetail
    {
        public FixFileDetail(bool deleted, string path, long size) : base(path, size)
        {
            Deleted = deleted;
        }

        public bool Deleted { get; }
    }
}