namespace ClrVpin.Models.Settings
{
    public interface ISettings
    {
        int Version { get; set; }
        int MinVersion { get; set; }
        void Init(DefaultSettings defaultSettings);
    }
}