using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PropertyChanged;

namespace ClrVpin.Models.Settings
{
    [AddINotifyPropertyChangedInterface]
    public class SettingsManager
    {
        private SettingsManager()
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Stoj", "ClrVpin");
            _path = Path.Combine(folder, "ClrVpin.settings");

            // create folder (and sub-folders) if it doesn't exist
            Directory.CreateDirectory(folder);

            Read();
        }

        public bool WasReset { get; private set; }
        public bool IsValid { get; private set; }

        public Settings Settings { get; set; }

        public static SettingsManager Create()
        {
            return _sessionManager ??= new SettingsManager();
        }

        public void Reset()
        {
            Settings = new Settings();
            Write();

            WasReset = true;
        }

        public void Write()
        {
            var serializedSettings = JsonSerializer.Serialize(Settings);
            File.WriteAllText(_path, serializedSettings);

            UpdateIsValid();
        }

        private void Read()
        {
            // retrieve existing config (from disk) or create a fresh one
            if (File.Exists(_path))
            {
                var data = File.ReadAllText(_path);
                Settings = JsonSerializer.Deserialize<Settings>(data);
            }
            else
            {
                Reset();
            }

            UpdateIsValid();
        }

        private void UpdateIsValid()
        {
            var paths = new List<string>
            {
                Settings.TableFolder,
                Settings.FrontendFolder,
                Settings.BackupFolder
            };
            paths.AddRange(Settings.FrontendFolders.Select(x => x.Folder));

            IsValid = paths.All(path => Directory.Exists(path) || File.Exists(path));
        }

        private readonly string _path;
        private static SettingsManager _sessionManager;
    }
}