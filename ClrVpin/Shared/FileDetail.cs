namespace ClrVpin.Shared
{
    public class FileDetail
    {
        public FileDetail(string path, long size)
        {
            Path = path;
            Size = size;
        }

        public string Path { get; }
        public long Size { get; }
    }
}