using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using ClrVpin.Models.Rebuilder;
using ClrVpin.Models.Settings;
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
            AllHitTypes.ForEach(x => x.Description = x.Enum.GetDescription());
            AllHitTypeEnums = AllHitTypes.Select(x => x.Enum);
            FixablePrioritizedHitTypeEnums = AllHitTypes.Where(x => x.Fixable).Select(x => x.Enum).ToArray();
            IrreparablePrioritizedHitTypeEnums = AllHitTypes.Where(x => !x.Fixable).Select(x => x.Enum).ToArray();

            // reset the settings if the user's stored settings version differs to the default version
            if (Properties.Settings.Default.ActualVersion < Properties.Settings.Default.RequiredVersion)
                Reset();

            // scanner
            SelectedCheckContentTypes = new ObservableStringCollection<string>(Properties.Settings.Default.SelectedCheckContentTypes).Observable;
            SelectedCheckHitTypes = new ObservableCollectionJson<HitTypeEnum>(Properties.Settings.Default.SelectedCheckHitTypes, value => Properties.Settings.Default.SelectedCheckHitTypes = value)
                .Observable;
            SelectedFixHitTypes = new ObservableCollectionJson<HitTypeEnum>(Properties.Settings.Default.SelectedFixHitTypes, value => Properties.Settings.Default.SelectedFixHitTypes = value)
                .Observable;
            HitTypes = AllHitTypes.Where(x => x.Enum != HitTypeEnum.Valid).ToArray();

            // rebuilder
            //SelectedMatchTypes = new ObservableCollectionJson<HitTypeEnum>(_settings.Rebuilder.SelectedMatchTypes, value => Properties.Settings.Default.SelectedMatchTypes = value).Observable;
            //SelectedMergeOptions = new ObservableCollectionJson<MergeOptionEnum>(Properties.Settings.Default.MergeOptions, value => Properties.Settings.Default.MergeOptions = value).Observable;
            //SelectedIgnoreOptions = new ObservableCollectionJson<IgnoreOptionEnum>(Properties.Settings.Default.IgnoreOptions, value => Properties.Settings.Default.IgnoreOptions = value).Observable;
            MergeOptions.ForEach(x => x.Description = x.Enum.GetDescription());
            IgnoreOptions.ForEach(x => x.Description = x.Enum.GetDescription());
            MatchTypes = AllHitTypes.Where(x => x.Enum.In(HitTypeEnum.Valid, HitTypeEnum.TableName, HitTypeEnum.WrongCase, HitTypeEnum.DuplicateExtension, HitTypeEnum.Fuzzy, HitTypeEnum.Unknown,
                HitTypeEnum.Unsupported)).ToArray();

            ContentTypes = _settings.FrontendFolders.Where(x => !x.IsDatabase).ToArray();
            UpdateIsValid();
        }

        // all possible content types (except database) - to be used elsewhere to create check collections
        public static ContentType[] ContentTypes { get; set; }

        public bool WasReset { get; private set; }
        public bool IsValid { get; private set; }

        // hit types in priority order as determined by matching algorithm - refer AssociateMediaFilesWithGames
        public static HitTypeEnum[] FixablePrioritizedHitTypeEnums { get; private set; }

        public static HitTypeEnum[] IrreparablePrioritizedHitTypeEnums { get; private set; }

        public static IEnumerable<HitTypeEnum> AllHitTypeEnums { get; private set; }

        public ContentType GetDestinationContentType() => _settings.FrontendFolders.First(x => x.Description == _settings.Rebuilder.DestinationContentType);

        public void SetFrontendFolders(IEnumerable<ContentType> frontendFolders) => _settings.FrontendFolders = frontendFolders.ToList();

        public void Save()
        {
            SettingsManager.Write();

            Properties.Settings.Default.Save();
            WasReset = false;
            UpdateIsValid();
        }

        public void Reset()
        {
            SettingsManager.Reset();
            _settings = SettingsManager.Settings;

            // todo; move all the enum default values into code - i.e. out of settings.settings default


            // update actual version to indicate the config is now compatible and doesn't need to be reset again
            Properties.Settings.Default.ActualVersion = Properties.Settings.Default.RequiredVersion;

            Properties.Settings.Default.SelectedMatchTypes = JsonSerializer.Serialize(AllHitTypeEnums.Where(x => x != HitTypeEnum.Missing));

            Save();

            WasReset = true;
        }

        private void UpdateIsValid()
        {
            var paths = new List<string>
            {
                _settings.TableFolder,
                _settings.FrontendFolder,
                _settings.BackupFolder
            };
            paths.AddRange(_settings.FrontendFolders.Select(x => x.Folder));

            IsValid = paths.All(path => Directory.Exists(path) || File.Exists(path));
        }

        private Settings.Settings _settings = SettingsManager.Settings;

        // scanner
        public ObservableCollection<string> SelectedCheckContentTypes;
        public ObservableCollection<HitTypeEnum> SelectedCheckHitTypes;
        public ObservableCollection<HitTypeEnum> SelectedFixHitTypes;

        // scanner matching hit types - to be used elsewhere (scanner) to create check and fix collections
        public static HitType[] AllHitTypes =
        {
            new HitType(HitTypeEnum.Valid, true, "Files that should match but are missing"),
            new HitType(HitTypeEnum.DuplicateExtension, true, "Allow matching against multiple files with same file name but different file extensions (e.g. mkv and mp4"),
            new HitType(HitTypeEnum.WrongCase, true, "Case insensitive file matching"),
            new HitType(HitTypeEnum.TableName, true, "Allow matching against table instead of the description"),
            new HitType(HitTypeEnum.Fuzzy, true, "'Fuzzy logic' file matching"),
            new HitType(HitTypeEnum.Missing, false, "Files that should match but are missing"),
            new HitType(HitTypeEnum.Unknown, false, "Unknown files that don't match any tables"),
            new HitType(HitTypeEnum.Unsupported, false, "Unsupported files that don't match the configured file extension types")
        };

        // scanner matching hit types - to be used elsewhere (scanner) to create check and fix collections
        public static HitType[] HitTypes;

        // rebuilder matching criteria types - to be used elsewhere (rebuilder)
        public static HitType[] MatchTypes;

        // all possible file merge options - to be used elsewhere (rebuilder)
        public static IgnoreOption[] IgnoreOptions =
        {
            new IgnoreOption {Enum = IgnoreOptionEnum.IgnoreSmaller, Tip = "Ignore source files that are significantly smaller size (<50%) than the existing files"},
            new IgnoreOption {Enum = IgnoreOptionEnum.IgnoreOlder, Tip = "Ignore source files that are older (using modified timestamp) than the existing files"}
        };

        // all possible file merge options - to be used elsewhere (rebuilder)
        public static MergeOption[] MergeOptions =
        {
            new MergeOption {Enum = MergeOptionEnum.PreserveTimestamp, Tip = "The (modified) timestamp of the source file will be used for created or overwritten destination file"},
            new MergeOption {Enum = MergeOptionEnum.RemoveSource, Tip = "Matched source files will be removed (copied to the backup folder)"}
        };
    }
}