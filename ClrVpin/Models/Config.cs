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
            CheckHitTypes = new ObservableCollectionJson<HitTypeEnum>(Properties.Settings.Default.CheckHitTypes, value => Properties.Settings.Default.CheckHitTypes = value).Observable;
            FixHitTypes = new ObservableCollectionJson<HitTypeEnum>(Properties.Settings.Default.FixHitTypes, value => Properties.Settings.Default.FixHitTypes = value).Observable;

            // assign some default frontend folders if there are none
            if (string.IsNullOrEmpty(FrontendFoldersJson))
                Default();

            ContentTypes = GetFrontendFolders().Where(x => !x.IsDatabase).ToArray();
        }

        private void Default()
        {
            var defaultFrontendFolders = new List<ContentType>
            {
                // todo; rename Type to Description
                new ContentType {Type = "Database", Tip = "Pinball X or Pinball Y database file", Extensions = "*.xml", IsDatabase = true},
                new ContentType {Type = "Table Audio", Tip="Audio used when displaying a table", Extensions = "*.mp3, *.wav"},
                new ContentType {Type = "Launch Audio", Tip="Audio used when launching a table", Extensions = "*.mp3, *.wav"},
                new ContentType {Type = "Table Videos", Tip="Video used when displaying a table", Extensions = "*.f4v, *.mp4"},
                new ContentType {Type = "Backglass Videos", Tip="Video used when displaying a table's backglass", Extensions = "*.f4v, *.mp4"},
                new ContentType {Type = "Wheel Images", Tip="Image used when displaying a table", Extensions = "*.png, *.jpg"}
            };
            FrontendFoldersJson = JsonSerializer.Serialize(defaultFrontendFolders);

            BackupFolder = Path.Combine(Directory.GetCurrentDirectory(), "backup");
        }

        // all possible content types (except database) - to be used elsewhere to create check collections
        public static ContentType[] ContentTypes { get; set; }

        // todo; change from enum to class and include tool tip
        // all possible hit types - to be used elsewhere to create check and fix collections
        public static HitType[] HitTypes =
        {
            // todo; rename Type to Enum
            new HitType { Type = HitTypeEnum.Missing, Description = HitTypeEnum.Missing.GetDescription(), Tip = "Files that should exist because the table exists in the database"},
            new HitType { Type = HitTypeEnum.TableName, Description = HitTypeEnum.TableName.GetDescription(), Tip = "Files that are incorrectly named matching the table (e.g. .vpx file) instead of the description (as required by frontends)"},
            new HitType { Type = HitTypeEnum.DuplicateExtension, Description = HitTypeEnum.DuplicateExtension.GetDescription(), Tip = "Files that are duplicated because they have multiple supported extensions (e.g. mp3 and wav)"},
            new HitType { Type = HitTypeEnum.WrongCase, Description = HitTypeEnum.WrongCase.GetDescription(), Tip = "Files that are correctly named, but have the wrong case"},
            new HitType { Type = HitTypeEnum.Fuzzy, Description = HitTypeEnum.Fuzzy.GetDescription(), Tip = "Files that match based on various algorithms"},
            new HitType { Type = HitTypeEnum.Unknown, Description = HitTypeEnum.Unknown.GetDescription(), Tip = "Files that are not required because they do not match any database entries"}
        };

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

        public readonly ObservableCollection<string> CheckContentTypes;
        public readonly ObservableCollection<HitTypeEnum> CheckHitTypes;
        public readonly ObservableCollection<HitTypeEnum> FixHitTypes;
    }
}