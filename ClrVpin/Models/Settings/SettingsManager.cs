using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PropertyChanged;
using Utils.Extensions;
using Path = System.IO.Path;

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
            
            // update config after read to store any new properties that have been added
            Write();

            UpdateIsValid();
        }

        public bool WasReset { get; private set; }
        public bool IsValid { get; private set; }
        public Settings Settings { get; private set; }

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
            // following special folders are considered mandatory
            var specialFolders = new List<string>
            {
                //Settings.PinballFolder,
                //Settings.FrontendFolder,
                Settings.BackupFolder
            };

            // assume all folders are invalid
            Settings.GetAllContentTypes().ForEach(x => x.IsFolderValid = false);

            // verify content folder types that are either..
            // - required
            // - optional, but have been defined
            var contentTypesToVerify = Settings.GetAllContentTypes().Where(x => x.IsFolderRequired  || !string.IsNullOrEmpty(x.Folder)).ToList();
            contentTypesToVerify.ForEach(x => x.IsFolderValid = DoesFolderOrFileExist(x.Folder));

            IsValid = contentTypesToVerify.All(x => x.IsFolderValid) && specialFolders.All(DoesFolderOrFileExist);
        }

        private static bool DoesFolderOrFileExist(string path) => Directory.Exists(path) || File.Exists(path);

        private readonly DefaultSettings _defaultSettings;
        private static SettingsManager _sessionManager;
    }
}