using System.Collections.Generic;
using System.Linq;
using ClrVpin.Models.Rebuilder;
using Utils;

namespace ClrVpin.Models.Settings
{
    public static class StaticSettings
    {
        static StaticSettings()
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
        }

        // hit types in priority order as determined by matching algorithm - refer AssociateContentFilesWithGames
        public static HitTypeEnum[] FixablePrioritizedHitTypeEnums { get; }

        public static HitTypeEnum[] IrreparablePrioritizedHitTypeEnums { get; }

        public static IEnumerable<HitTypeEnum> AllHitTypeEnums { get; }

        //private static Settings Settings => Model.Settings;

        // scanner matching hit types - to be used elsewhere (scanner) to create check and fix collections
        public static HitType[] AllHitTypes =
        {
            new HitType(HitTypeEnum.Valid, true, "Files that match perfectly!"),
            new HitType(HitTypeEnum.DuplicateExtension, true, "Files that match the correct name AND have a configured file extension, but multiple extension matches exist (e.g. mkv and mp4"),
            new HitType(HitTypeEnum.WrongCase, true, "Files that match the correct name, but have the wrong case"),
            new HitType(HitTypeEnum.TableName, true, "Files that match against the table name instead of the table description - ONLY APPLICABLE FOR MEDIA CONTENT, since tables ALWAYS match the table name"),
            new HitType(HitTypeEnum.Fuzzy, true, "Files that match the 'Fuzzy logic' algorithms"),
            new HitType(HitTypeEnum.Missing, false, "Files that should match but are missing"),
            new HitType(HitTypeEnum.Unknown, false, "Files that do match the configured file extension type, but don't match any of the tables in the database"),
            new HitType(HitTypeEnum.Unsupported, false, "Files that don't match the configured file extension types - ONLY APPLICABLE FOR MEDIA CONTENT, since unsupported files are expected to exist in the tables folder (e.g. txt, exe, vbs, etc)")
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
            new MergeOption {Enum = MergeOptionEnum.PreserveDateModified, Tip = "Date modified timestamp of merged file (in the destination folder) will match the source file, else the current time will be used"},
            new MergeOption {Enum = MergeOptionEnum.RemoveSource, Tip = "Matched source files will be removed (copied to the backup folder)"}
        };
    }
}