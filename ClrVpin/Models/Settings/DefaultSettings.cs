using System;
using System.Text.Json.Serialization;
using PropertyChanged;

namespace ClrVpin.Models.Settings
{
    [AddINotifyPropertyChangedInterface]
    public class DefaultSettings : ISettings
    {
        public DefaultSettings()
        {
            // special default settings override
            // - unlike the defaults defined in Settings.cs.. these values are maintained AFTER a reset
            // - refer SettingsManager.cs --> these settings are not deleted!
            Version = MinVersion;

            PinballFolder = @"C:\vp\tables\vpx";
            FrontendFolder = @"C:\vp\apps\PinballX";
            Guid = System.Guid.NewGuid().ToString();
        }

        public string PinballFolder { get; set; }
        public string FrontendFolder { get; set; }
        
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global - setter required for json.net
        public string Guid { get; set; }

        public int Version { get; set; }
        
        [JsonIgnore]
        public int MinVersion { get; set; } = 1;

        public void Init(DefaultSettings defaultSettings) => throw new NotImplementedException("Init not required for DefaultSettings");
    }
}