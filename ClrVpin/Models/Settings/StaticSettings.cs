using System.Collections.Generic;
using System.Linq;
using ClrVpin.Models.Rebuilder;
using ClrVpin.Models.Scanner;
using Utils;

namespace ClrVpin.Models.Settings
{
    public static class StaticSettings
    {
        static StaticSettings()
        {
            // scanner
            AllHitTypes.ForEach(x => x.Description = x.Enum.GetDescription());
            AllHitTypeEnums = AllHitTypes.Select(x => x.Enum);
            FixablePrioritizedHitTypeEnums = AllHitTypes.Where(x => x.Fixable).Select(x => x.Enum).ToArray();
            IrreparablePrioritizedHitTypeEnums = AllHitTypes.Where(x => !x.Fixable).Select(x => x.Enum).ToArray();

            HitTypes = AllHitTypes.Where(x => x.Enum != HitTypeEnum.CorrectName).ToArray();
            MultipleMatchOptions.ForEach(x => x.Description = x.Enum.GetDescription());

            // rebuilder
            MergeOptions.ForEach(x => x.Description = x.Enum.GetDescription());
            IgnoreOptions.ForEach(x => x.Description = x.Enum.GetDescription());
            MatchTypes = AllHitTypes.Where(x => x.Enum.In(HitTypeEnum.CorrectName, HitTypeEnum.TableName, HitTypeEnum.WrongCase, HitTypeEnum.DuplicateExtension, HitTypeEnum.Fuzzy, HitTypeEnum.Unknown,
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
            new HitType(HitTypeEnum.CorrectName, true, "Files that match perfectly!"),
            new HitType(HitTypeEnum.WrongCase, true, "Files that match the correct name, but have the wrong case"),
            new HitType(HitTypeEnum.TableName, true, "Files that match against the table name instead of the table description - ONLY APPLICABLE FOR MEDIA CONTENT, since tables ALWAYS match the table name"),
            new HitType(HitTypeEnum.Fuzzy, true, "Files that match the 'Fuzzy logic' algorithms"),
            new HitType(HitTypeEnum.DuplicateExtension, true, "Files that match the correct name AND have a configured file extension, but multiple extension matches exist (e.g. mkv and mp4"),
            new HitType(HitTypeEnum.Missing, false, "Files that are missing, i.e. they need to be downloaded from your favorite pinball site(s)"),
            new HitType(HitTypeEnum.Unknown, false, "Files that do match the configured file extension type, but don't match any of the tables in the database"),
            new HitType(HitTypeEnum.Unsupported, false, "Files that don't match the configured file extension types - ONLY APPLICABLE FOR MEDIA CONTENT, since unsupported files are EXPECTED to exist in the tables folder (e.g. txt, exe, ogg, etc)")
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

        // all possible multiple match fix options
        public static MultipleMatchOption[] MultipleMatchOptions =
        {
            new MultipleMatchOption {Enum = MultipleMatchOptionEnum.CorrectName, Tip = "File with the correct matching name is used, if it doesn't exist then the following names are used (in descending order): WrongCase, TableName, and Fuzzy."},
            new MultipleMatchOption {Enum = MultipleMatchOptionEnum.MostRecent, Tip = "File with the most recent modified timestamp is used"},
            new MultipleMatchOption {Enum = MultipleMatchOptionEnum.LargestSize, Tip = "File with the largest size is used"}
        };
    }
}