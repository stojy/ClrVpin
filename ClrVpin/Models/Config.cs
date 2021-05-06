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
            CheckContentTypes = new ObservableStringCollection<string>(Properties.Settings.Default.CheckContentTypes).Observable;
            CheckHitTypes = new ObservableCollectionJson<HitTypeEnum>(Properties.Settings.Default.CheckHitTypes, value => Properties.Settings.Default.CheckHitTypes = value)
                .Observable;
            FixHitTypes = new ObservableCollectionJson<HitTypeEnum>(Properties.Settings.Default.FixHitTypes, value => Properties.Settings.Default.FixHitTypes = value).Observable;

            // reset the settings if the user's stored settings version differs to the default version
            if (Properties.Settings.Default.SettingsVersion != Properties.Settings.Default.GetDefault<int>(nameof(Properties.Settings.Default.SettingsVersion)))
                Default();

            ContentTypes = GetFrontendFolders().Where(x => !x.IsDatabase).ToArray();
            HitTypes.ForEach(x => x.Description = x.Enum.GetDescription());
        }

        // all possible content types (except database) - to be used elsewhere to create check collections
        public static ContentType[] ContentTypes { get; set; }

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

        public string TableFolder
        {
            get => Properties.Settings.Default.TableFolder;
            set => Properties.Settings.Default.TableFolder = value;
        }

        public List<ContentType> GetFrontendFolders() => JsonSerializer.Deserialize<List<ContentType>>(FrontendFoldersJson);
        public void SetFrontendFolders(IEnumerable<ContentType> frontendFolders) => FrontendFoldersJson = JsonSerializer.Serialize(frontendFolders);

        private void Default()
        {
            var defaultFrontendFolders = new List<ContentType>
            {
                new ContentType {Description = "Database", Tip = "Pinball X or Pinball Y database file", Extensions = "*.xml", IsDatabase = true},
                new ContentType {Description = "Table Audio", Tip = "Audio used when displaying a table", Extensions = "*.mp3, *.wav"},
                new ContentType {Description = "Launch Audio", Tip = "Audio used when launching a table", Extensions = "*.mp3, *.wav"},
                new ContentType {Description = "Table Videos", Tip = "Video used when displaying a table", Extensions = "*.f4v, *.mp4"},
                new ContentType {Description = "Backglass Videos", Tip = "Video used when displaying a table's backglass", Extensions = "*.f4v, *.mp4"},
                new ContentType {Description = "Wheel Images", Tip = "Image used when displaying a table", Extensions = "*.png, *.jpg"}

                // todo; table folders
                //new ContentType_Obsolete {Enum = "Tables", Extensions = new[] {"*.png"}, GetXxxHits = g => g.WheelImageHits},
                //new ContentType_Obsolete {Enum = "Backglass", Extensions = new[] {"*.png"}, GetXxxHits = g => g.WheelImageHits},
                //new ContentType_Obsolete {Enum = "Point of View", Extensions = new[] {"*.png"}, GetXxxHits = g => g.WheelImageHits},
            };
            FrontendFoldersJson = JsonSerializer.Serialize(defaultFrontendFolders);

            BackupFolder = Path.Combine(Directory.GetCurrentDirectory(), "backup");
        }

        public readonly ObservableCollection<string> CheckContentTypes;
        public readonly ObservableCollection<HitTypeEnum> CheckHitTypes;
        public readonly ObservableCollection<HitTypeEnum> FixHitTypes;

        // todo; change from enum to class and include tool tip
        // all possible hit types - to be used elsewhere to create check and fix collections
        public static HitType[] HitTypes =
        {
            new HitType {Enum = HitTypeEnum.Missing, Tip = "Files that should match but are missing"},
            new HitType {Enum = HitTypeEnum.TableName, Tip = "Files matches against table instead of the description"},
            new HitType {Enum = HitTypeEnum.DuplicateExtension, Tip = "Files matches with multiple extensions (e.g. mp3 and wav)"},
            new HitType {Enum = HitTypeEnum.WrongCase, Tip = "Files matches that have the wrong case"},
            new HitType {Enum = HitTypeEnum.Fuzzy, Tip = "Files matches based on some 'fuzzy logic'"},
            new HitType {Enum = HitTypeEnum.Unknown, Tip = "Unknown files that don't match anything"}
        };
    }
}