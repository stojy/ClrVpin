using System;
using System.IO;
using System.Text.Json;
using ClrVpin.Logging;

namespace ClrVpin.Models.Settings
{
    public static class SettingsHelper
    {
        public static T Reset<T>(string path, DefaultSettings defaultSettings) where T : ISettings, new()
        {
            var settings = new T();
            if (defaultSettings != null)
                settings.Init(defaultSettings);

            Write(settings, path);

            return settings;
        }

        public static void Write<T>(T settings, string path)
        {
            var serializedSettings = JsonSerializer.Serialize(settings);
            File.WriteAllText(path, serializedSettings);
        }

        public static T Read<T>(string path, DefaultSettings defaultSettings) where T : ISettings, new()
        {
            T settings;

            // retrieve existing config (from disk) or create a fresh one
            if (File.Exists(path))
            {
                var data = File.ReadAllText(path);

                try
                {
                    settings = JsonSerializer.Deserialize<T>(data);
                    if (defaultSettings != null)
                        settings!.Init(defaultSettings);

                    // reset the settings if the user's stored settings version differs to the default version
                    if (settings!.Version < settings.MinVersion)
                        settings = Reset<T>(path, defaultSettings);
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Failed to deserialize settings.. resetting settings");
                    settings = Reset<T>(path, defaultSettings);
                }
            }
            else
            {
                settings = Reset<T>(path, defaultSettings);
            }

            return settings;
        }

        public static void CreateRootFolder(string rootFolder)
        {
            _rootFolder = rootFolder;

            // create folder (and sub-folders) if it doesn't exist
            Directory.CreateDirectory(_rootFolder);
        }

        private static string _rootFolder;
    }
}