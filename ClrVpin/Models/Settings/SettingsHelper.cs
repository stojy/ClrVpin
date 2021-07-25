using System;
using System.IO;
using System.Text.Json;
using ClrVpin.Logging;

namespace ClrVpin.Models.Settings
{
    public static class SettingsHelper
    {
        public static T Reset<T>(DefaultSettings defaultSettings) where T : ISettings, new()
        {
            var settings = new T();
            if (defaultSettings != null)
                settings.Init(defaultSettings);

            Write(settings);

            return settings;
        }

        public static void Write<T>(T settings)
        {
            var serializedSettings = JsonSerializer.Serialize(settings);
            File.WriteAllText(GetPath<T>(), serializedSettings);
        }

        public static T Read<T>(DefaultSettings defaultSettings) where T : ISettings, new()
        {
            T settings;

            // retrieve existing config (from disk) or create a fresh one
            var path = GetPath<T>();
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
                        settings = Reset<T>(defaultSettings);
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Failed to deserialize settings.. resetting settings");
                    settings = Reset<T>(defaultSettings);
                }
            }
            else
            {
                settings = Reset<T>(defaultSettings);
            }

            return settings;
        }

        public static void CreateRootFolder(string rootFolder)
        {
            _rootFolder = rootFolder;

            // create folder (and sub-folders) if it doesn't exist
            Directory.CreateDirectory(_rootFolder);
        }

        private static string GetPath<T>() => Path.Combine(_rootFolder, $"{typeof(T).Name}.json");

        private static string _rootFolder;
    }
}