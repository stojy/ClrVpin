using System;
using System.Text.Json.Serialization;
using PropertyChanged;

namespace ClrVpin.Models.Settings;

[AddINotifyPropertyChangedInterface]
[Serializable]
public class DefaultSettings : ISettings
{
    public DefaultSettings()
    {
        // special default settings override
        // - unlike the defaults defined in Settings.cs.. these values are maintained AFTER a reset
        // - refer SettingsManager.cs --> these settings are not deleted!
        Version = MinVersion;
            
        Id = Guid.NewGuid().ToString();
    }
    
    public string Id { get; set; }

    public int Version { get; set; }
        
    [JsonIgnore]
    public int MinVersion { get; set; } = 2;

    public void Init(DefaultSettings defaultSettings) => throw new NotImplementedException("Init not required for DefaultSettings");
}