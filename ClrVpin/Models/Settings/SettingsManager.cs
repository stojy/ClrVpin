using System;
using System.IO;
using System.Text.Json;

namespace ClrVpin.Models.Settings
{
    public class SettingsManager
    {
        static SettingsManager()
        {
            Folder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Stoj", "ClrVpin");
            Path = System.IO.Path.Combine(Folder, "ClrVpin.settings");

            // create folder (and sub-folders) if it doesn't exist
            Directory.CreateDirectory(Folder);
        }

        public static string Folder { get; set; }
        public static string Path { get; set; }

        public static Settings Read()
        {
            Settings settings;

            // retrieve existing config (from disk) or create a fresh one
            if (File.Exists(Path))
            {
                var data = File.ReadAllText(Path);
                settings = JsonSerializer.Deserialize<Settings>(data);
            }
            else
            {
                return Reset();
            }

            return settings;
        }

        public static Settings Reset()
        {
            var settings = new Settings();

            Write(settings);

            return settings;
        }

        public static void Write(Settings settings)
        {
            var serializedSettings = JsonSerializer.Serialize(settings);
            File.WriteAllText(Path, serializedSettings);
        }
    }
}