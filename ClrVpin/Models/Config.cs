using System.Collections.Generic;
using System.Linq;
using ClrVpin.Models.Rebuilder;
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

            HitTypes = AllHitTypes.Where(x => x.Enum != HitTypeEnum.Valid).ToArray();

            // rebuilder
            MergeOptions.ForEach(x => x.Description = x.Enum.GetDescription());
            IgnoreOptions.ForEach(x => x.Description = x.Enum.GetDescription());
            MatchTypes = AllHitTypes.Where(x => x.Enum.In(HitTypeEnum.Valid, HitTypeEnum.TableName, HitTypeEnum.WrongCase, HitTypeEnum.DuplicateExtension, HitTypeEnum.Fuzzy, HitTypeEnum.Unknown,
                HitTypeEnum.Unsupported)).ToArray();

            ContentTypes = Settings.FrontendFolders.Where(x => !x.IsDatabase).ToArray();
        }

        // all possible content types (except database) - to be used elsewhere to create check collections
        public static ContentType[] ContentTypes { get; set; }

        // hit types in priority order as determined by matching algorithm - refer AssociateMediaFilesWithGames
        public static HitTypeEnum[] FixablePrioritizedHitTypeEnums { get; private set; }

        public static HitTypeEnum[] IrreparablePrioritizedHitTypeEnums { get; private set; }

        public static IEnumerable<HitTypeEnum> AllHitTypeEnums { get; private set; }

        public ContentType GetDestinationContentType() => Settings.FrontendFolders.First(x => x.Description == Settings.Rebuilder.DestinationContentType);

        public void SetFrontendFolders(IEnumerable<ContentType> frontendFolders) => Settings.FrontendFolders = frontendFolders.ToList();

        private static Settings.Settings Settings => Model.Settings;

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