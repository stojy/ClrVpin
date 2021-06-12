using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using PropertyChanged;
using Utils;

namespace ClrVpin.Models
{
    [AddINotifyPropertyChangedInterface]
    public class Config
    {
        // abstract the underlying settings designer class because..
        // - users are agnostic to the underlying Properties.Settings.Default get/set implementation
        // - simpler xaml binding to avoid need for either..
        //   - StaticResource reference; prone to errors if Default isn't referenced in Folder (i.e. else new class used)
        //     e.g. <properties:Settings x:Key="Settings"/>
        //          Text="{Binding Source={StaticResource Settings}, Folder=Default.FrontendFolder}"
        //   - Static reference; too long
        //     e.g. Text="{Binding Source={x:Static p:Settings.Default}, Folder=FrontendFolder}"
        //   - vs a simple regular data binding
        //     e.g. Text="{Binding FrontendFolder}"

        public Config()
        {
            // scanner
            CheckContentTypes = new ObservableStringCollection<string>(Properties.Settings.Default.CheckContentTypes).Observable;
            CheckHitTypes = new ObservableCollectionJson<HitTypeEnum>(Properties.Settings.Default.CheckHitTypes, value => Properties.Settings.Default.CheckHitTypes = value).Observable;
            FixHitTypes = new ObservableCollectionJson<HitTypeEnum>(Properties.Settings.Default.FixHitTypes, value => Properties.Settings.Default.FixHitTypes = value).Observable;
            HitTypes.ForEach(x => x.Description = x.Enum.GetDescription());

            // rebuilder
            MatchHitTypes = new ObservableCollectionJson<HitTypeEnum>(Properties.Settings.Default.MatchHitTypes, value => Properties.Settings.Default.MatchHitTypes = value).Observable;
            SelectedOverwriteOptions = new ObservableCollectionJson<OverwriteOptionEnum>(Properties.Settings.Default.OverwriteOptions, value => Properties.Settings.Default.OverwriteOptions = value).Observable;
            MatchTypes.ForEach(x => x.Description = x.Enum.GetDescription());
            OverwriteOptions.ForEach(x => x.Description = x.Enum.GetDescription());

            // reset the settings if the user's stored settings version differs to the default version
            if (Properties.Settings.Default.ActualVersion < Properties.Settings.Default.RequiredVersion)
                Reset();

            ContentTypes = GetFrontendFolders().Where(x => !x.IsDatabase).ToArray();

            UpdateIsValid();
        }

        // all possible content types (except database) - to be used elsewhere to create check collections
        public static ContentType[] ContentTypes { get; set; }

        public string TableFolder
        {
            get => Properties.Settings.Default.TableFolder;
            set => Properties.Settings.Default.TableFolder = value;
        }

        private string FrontendFoldersJson
        {
            get => Properties.Settings.Default.FrontendFoldersJson;
            set => Properties.Settings.Default.FrontendFoldersJson = value;
        }

        public string FrontendFolder
        {
            get => Properties.Settings.Default.FrontendFolder;
            set => Properties.Settings.Default.FrontendFolder = value;
        }

        public string BackupFolder
        {
            get => Properties.Settings.Default.BackupFolder;
            set => Properties.Settings.Default.BackupFolder = value;
        }
        
        public bool TrainerWheels
        {
            get => Properties.Settings.Default.TrainerWheels;
            set => Properties.Settings.Default.TrainerWheels = value;
        }

        // rebuilder
        public string SourceFolder
        {
            get => Properties.Settings.Default.SourceFolder;
            set => Properties.Settings.Default.SourceFolder = value;
        }

        public bool WasReset { get; private set; }
        public bool IsValid { get; private set; } 

        public List<ContentType> GetFrontendFolders() => JsonSerializer.Deserialize<List<ContentType>>(FrontendFoldersJson);
        public void SetFrontendFolders(IEnumerable<ContentType> frontendFolders) => FrontendFoldersJson = JsonSerializer.Serialize(frontendFolders);

        public void Save()
        {
            Properties.Settings.Default.Save();
            WasReset = false;
            UpdateIsValid();
        }

        private void UpdateIsValid()
        {
            var paths = new List<string>
            {
                TableFolder,
                FrontendFolder,
                BackupFolder
            };
            paths.AddRange(GetFrontendFolders().Select(x => x.Folder));

            IsValid = paths.All(path => Directory.Exists(path) || File.Exists(path));
        }

        public void Reset()
        {
            var defaultFrontendFolders = new List<ContentType>
            {
                new ContentType {Enum = ContentTypeEnum.Database, Tip = "Pinball X or Pinball Y database file", Extensions = "*.xml", IsDatabase = true},
                new ContentType {Enum = ContentTypeEnum.TableAudio, Tip = "Audio used when displaying a table", Extensions = "*.mp3, *.wav"},
                new ContentType {Enum = ContentTypeEnum.LaunchAudio, Tip = "Audio used when launching a table", Extensions = "*.mp3, *.wav"},
                new ContentType {Enum = ContentTypeEnum.TableVideos, Tip = "Video used when displaying a table", Extensions = "*.f4v, *.mp4, *.mkv"},
                new ContentType {Enum = ContentTypeEnum.BackglassVideos, Tip = "Video used when displaying a table's backglass", Extensions = "*.f4v, *.mp4, *.mkv"},
                new ContentType {Enum = ContentTypeEnum.WheelImages, Tip = "Image used when displaying a table", Extensions = "*.png, *.apng, *.jpg"}

                // todo; table folders
                //new ContentType_Obsolete {Enum = "Tables", Extensions = new[] {"*.png"}, GetXxxHits = g => g.WheelImageHits},
                //new ContentType_Obsolete {Enum = "Backglass", Extensions = new[] {"*.png"}, GetXxxHits = g => g.WheelImageHits},
                //new ContentType_Obsolete {Enum = "Point of View", Extensions = new[] {"*.png"}, GetXxxHits = g => g.WheelImageHits},
            };
            defaultFrontendFolders.ForEach(x => x.Description = x.Enum.GetDescription());

            // entire front end folder object is stored to disk, e.g. including the enum description
            FrontendFoldersJson = JsonSerializer.Serialize(defaultFrontendFolders);

            BackupFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ClrVpin", "backup");

            TrainerWheels = true;

            SourceFolder = SpecialFolder.Downloads;

            // update actual version to indicate the config is now compatible and doesn't need to be reset again
            Properties.Settings.Default.ActualVersion = Properties.Settings.Default.RequiredVersion;

            Save();

            WasReset = true;
        }

        // scanner
        public ObservableCollection<string> CheckContentTypes;
        public ObservableCollection<HitTypeEnum> CheckHitTypes;
        public ObservableCollection<HitTypeEnum> FixHitTypes;

        // all possible hit types - to be used elsewhere (scanner) to create check and fix collections
        public static HitType[] HitTypes =
        {
            new HitType {Enum = HitTypeEnum.Missing, Tip = "Files that should match but are missing"},
            new HitType {Enum = HitTypeEnum.TableName, Tip = "Allow matching against table instead of the description"},
            new HitType {Enum = HitTypeEnum.DuplicateExtension, Tip = "Files that match with multiple extensions (e.g. mp3 and wav)"},
            new HitType {Enum = HitTypeEnum.WrongCase, Tip = "Case insensitive file matching"},
            new HitType {Enum = HitTypeEnum.Fuzzy, Tip = "'Fuzzy logic' file matching"},
            new HitType {Enum = HitTypeEnum.Unknown, Tip = "Unknown files that don't match any tables"},
            new HitType {Enum = HitTypeEnum.Unsupported, Tip = "Unsupported files that don't match the configured file extension types"}
        };

        // rebuilder
        public ObservableCollection<HitTypeEnum> MatchHitTypes;
        public ObservableCollection<OverwriteOptionEnum> SelectedOverwriteOptions;

        // all possible match criteria types - to be used elsewhere (rebuilder)
        public static HitType[] MatchTypes =
        {
            new HitType {Enum = HitTypeEnum.TableName, Tip = "Allow matching against table instead of the description"},
            new HitType {Enum = HitTypeEnum.WrongCase, Tip = "Case insensitive file matching"},
            new HitType {Enum = HitTypeEnum.Fuzzy, Tip = "'Fuzzy logic' file matching"},
        };

        // all possible file overwrite options - to be used elsewhere (rebuilder)
        public static OverwriteOption[] OverwriteOptions =
        {
            new OverwriteOption {Enum = OverwriteOptionEnum.IgnoreSmaller, Tip = "Ignore any source files if their size is less than 90% of the destination file"},
            new OverwriteOption {Enum = OverwriteOptionEnum.IgnoreOlder, Tip = "Ignore any source files if their modified timestamp is older than the destination file"},
            new OverwriteOption {Enum = OverwriteOptionEnum.PreserveTimestamp, Tip = "The timestamp of the source file will be used for created or overwritten destination file"},
            new OverwriteOption {Enum = OverwriteOptionEnum.RemoveSource, Tip = "Matched source files will be removed (copied to the backup folder)"},
        };
    }
}