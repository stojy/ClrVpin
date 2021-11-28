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
            // default settings overrides
            // - during json.net deserialization.. ctor is invoked BEFORE deserialized version overwrites the values, i.e. they will be overwritten where a stored setting exists
            // - these settings are used to override the default Settings in the event of a reset
            Version = MinVersion;

            PinballFolder = @"C:\vp\tables\vpx";
            PinballTablesFolder = @"C:\vp\tables\vpx";
            FrontendFolder = @"C:\vp\apps\PinballX";
            
            EncryptedSpreadsheeetKey = "uvcGOLbr8gUniuPdRENAvDl5lYbj2jXeVqXhjEIRm1piYwh8HKHphnsqhwmi5D+XeB17hLyzZbOiljWIqv7HOjutMzM1MqgixwMpYqr/tM898OqyAQDRcTzDZOaFShGi";
        }

        public string PinballFolder { get; set; }
        public string PinballTablesFolder { get; set; }
        public string FrontendFolder { get; set; }
        public string EncryptedSpreadsheeetKey { get; set; }

        public int Version { get; set; }
        
        [JsonIgnore]
        public int MinVersion { get; set; } = 1;

        public void Init(DefaultSettings defaultSettings) => throw new NotImplementedException("Init not required for DefaultSettings");
    }
}