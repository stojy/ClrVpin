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

            // use the default settings to seed the new settings instance, e.g. for useful folder setup
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

        public static (T, string) Read<T>(DefaultSettings defaultSettings) where T : ISettings, new()
        {
            T settings;
            string resetReason = null;

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
                    {
                        //Notification.ShowWarning(HomeWindow.HomeDialogHost, "Your settings are incompatible and will be reset", "Apologies for the inconvenience").RunSynchronously();
                        settings = Reset<T>(defaultSettings);
                        resetReason = "Your settings file was out of date.";
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Failed to deserialize settings.. resetting settings");
                    settings = Reset<T>(defaultSettings);
                    resetReason = "Your settings file could not be read.";
                }
            }
            else
            {
                settings = Reset<T>(defaultSettings);
            }

            return (settings, resetReason);
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