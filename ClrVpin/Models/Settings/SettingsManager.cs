using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PropertyChanged;

namespace ClrVpin.Models.Settings
{
    [AddINotifyPropertyChangedInterface]
    public class SettingsManager
    {
        private SettingsManager()
        {
            SettingsHelper.CreateRootFolder(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Stoj", "ClrVpin"));

            _defaultSettings = SettingsHelper.Read<DefaultSettings>(null);
            Settings = SettingsHelper.Read<Settings>(_defaultSettings);

            UpdateIsValid();
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
            // reset Settings, but keep defaultSettings unchanged.. i.e. the defaultSettings are used to seed the reset Settings
            Settings = SettingsHelper.Reset<Settings>(_defaultSettings);
            WasReset = true;
        }

        public void Write()
        {
            // write default and settings
            SettingsHelper.Write(_defaultSettings);
            SettingsHelper.Write(Settings);

            UpdateIsValid();
        }

        private void UpdateIsValid()
        {
            var paths = new List<string>
            {
                Settings.PinballFolder,
                Settings.FrontendFolder,
                Settings.BackupFolder
            };
            paths.AddRange(Settings.GetAllContentTypes().Select(x => x.Folder));

            IsValid = paths.All(path => Directory.Exists(path) || File.Exists(path));
        }

        private readonly DefaultSettings _defaultSettings;
        private static SettingsManager _sessionManager;
    }
}