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

            Read();
        }

        public static string Folder { get; set; }
        public static string Path { get; set; }

        public static Settings Settings { get; set; }

        public static void Reset()
        {
            Settings = new Settings();
            Write();
        }

        public static void Write()
        {
            var serializedSettings = JsonSerializer.Serialize(Settings);
            File.WriteAllText(Path, serializedSettings);
        }

        private static void Read()
        {
            // retrieve existing config (from disk) or create a fresh one
            if (File.Exists(Path))
            {
                var data = File.ReadAllText(Path);
                Settings = JsonSerializer.Deserialize<Settings>(data);
            }
            else
            {
                Reset();
            }
        }
    }
}