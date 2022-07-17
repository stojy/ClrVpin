using System.Linq;
using ClrVpin.Models.Importer;
using ClrVpin.Models.Rebuilder;
using ClrVpin.Models.Scanner;
using ClrVpin.Models.Shared;
using Utils.Extensions;

namespace ClrVpin.Models.Settings
{
    public static class StaticSettings
    {
        static StaticSettings()
        {
            // scanner
            AllHitTypes.ForEach(x => x.Description = x.Enum.GetDescription());
            FixablePrioritizedHitTypeEnums = AllHitTypes.Where(x => x.Fixable).Select(x => x.Enum).ToArray();

            MultipleMatchOptions.ForEach(x => x.Description = x.Enum.GetDescription());

            // rebuilder
            MergeOptions.ForEach(x => x.Description = x.Enum.GetDescription());
            IgnoreCriteria.ForEach(x => x.Description = x.Enum.GetDescription());
            MatchTypes = AllHitTypes.Where(x => x.Enum.In(HitTypeEnum.CorrectName, HitTypeEnum.TableName, HitTypeEnum.WrongCase, HitTypeEnum.DuplicateExtension, HitTypeEnum.Fuzzy, HitTypeEnum.Unknown,
                HitTypeEnum.Unsupported)).ToArray();

            // importer
            TableStyleOptions.ForEach(x => x.Description = x.Enum.GetDescription());
            TableMatchOptions.ForEach(x => x.Description = x.Enum.GetDescription());
            FixFeedOptions.ForEach(x => x.Description = x.Enum.GetDescription());
        }

        // hit types in priority order as determined by matching algorithm - refer AddContentFilesToGames
        public static HitTypeEnum[] FixablePrioritizedHitTypeEnums { get; }

        //private static Settings Settings => Model.Settings;

        // scanner matching hit types - to be used elsewhere (scanner) to create check and fix collections
        public static readonly HitType[] AllHitTypes =
        {
            new HitType(HitTypeEnum.CorrectName, true, "Files that match perfectly!"),
            new HitType(HitTypeEnum.WrongCase, true, "Files that match the correct name, but have the wrong case"),
            new HitType(HitTypeEnum.TableName, true, "Files that match against the table name instead of the table description - ONLY APPLICABLE FOR MEDIA CONTENT, since tables ALWAYS match the table name"),
            new HitType(HitTypeEnum.Fuzzy, true, "Files that match the 'Fuzzy logic' algorithms (refer help page for more info)", true, "https://github.com/stojy/ClrVpin/wiki/Fuzzy-Logic"),
            new HitType(HitTypeEnum.DuplicateExtension, true, "Files that match the correct name AND have a configured file extension, but multiple extension matches exist (e.g. mkv and mp4"),
            new HitType(HitTypeEnum.Missing, false, "Files that are missing, i.e. they need to be downloaded from your favorite pinball site(s)"),
            new HitType(HitTypeEnum.Unknown, false, "Files that do match the configured file extension type, but don't match any of the tables in the database"),
            new HitType(HitTypeEnum.Unsupported, false,
                "Files that don't match the configured file extension types - ONLY APPLICABLE FOR MEDIA CONTENT, since unsupported files are EXPECTED to exist in the tables folder (e.g. txt, exe, ogg, etc)")
        };

        // rebuilder matching criteria types - to be used elsewhere (rebuilder)
        public static readonly HitType[] MatchTypes;

        // all possible file merge options - to be used elsewhere (rebuilder)
        public static readonly IgnoreCriteria[] IgnoreCriteria =
        {
            new IgnoreCriteria {Enum = IgnoreCriteriaEnum.IgnoreIfContainsWords, Tip = "If the file is matched: ignore the source file if it contains any of the configured words"},
            new IgnoreCriteria {Enum = IgnoreCriteriaEnum.IgnoreIfSmaller, Tip = "If a destination file with the same name already exists: ignore the source file if it's smaller based on the specified percentage"},
            new IgnoreCriteria {Enum = IgnoreCriteriaEnum.IgnoreIfNotNewer, Tip = "If a destination file with the same name already exists: ignore the source file if it's not newer (using last modified timestamp)"}
        };

        public static readonly Option DeleteIgnoredFilesOption = new Option {Tip = "When enabled, rebuilder will delete the ignored files (if trainer wheels is not enabled).", Description = "Delete Ignored Files"};

        // all possible file merge options - to be used elsewhere (rebuilder)
        public static readonly MergeOption[] MergeOptions =
        {
            new MergeOption {Enum = MergeOptionEnum.PreserveDateModified, Tip = "Date modified timestamp of merged file (in the destination folder) will match the source file, else the current time will be used"},
            new MergeOption {Enum = MergeOptionEnum.RemoveSource, Tip = "Matched source files will be removed (copied to the backup folder)"}
        };

        // all possible multiple match fix options
        public static readonly MultipleMatchOption[] MultipleMatchOptions =
        {
            new MultipleMatchOption
            {
                Enum = MultipleMatchOptionEnum.PreferCorrectName, Tip = "File with the correct matching name is used, if it doesn't exist then the following names are used (in descending order): WrongCase, TableName, and Fuzzy."
            },
            new MultipleMatchOption {Enum = MultipleMatchOptionEnum.PreferMostRecent, Tip = "File with the most recent modified timestamp is used"},
            new MultipleMatchOption {Enum = MultipleMatchOptionEnum.PreferLargestSize, Tip = "File with the largest size is used"},
            new MultipleMatchOption
            {
                Enum = MultipleMatchOptionEnum.PreferMostRecentAndExceedSizeThreshold,
                Tip = "File with the most recent modified timestamp AND exceeds the size threshold of the existing correct file (if one exists) is used, i.e. avoid using newer but smaller files"
            }
        };

        // all possible table style options - to be used elsewhere (importer)
        public static readonly TableStyleOption[] TableStyleOptions =
        {
            new TableStyleOption {Enum = TableStyleOptionEnum.Manufactured, Tip = "A physical table has been manufactured"},
            new TableStyleOption {Enum = TableStyleOptionEnum.Original, Tip = "An original table creation that has not been manufactured"},
            new TableStyleOption {Enum = TableStyleOptionEnum.Both, Tip = "Manufactured AND original tables"}
        };

        // all possible matching types 
        public static readonly TableMatchOption[] TableMatchOptions =
        {
            new TableMatchOption {Enum = TableMatchOptionEnum.Matched, Tip = "Online tables that exist in your local database"},
            new TableMatchOption {Enum = TableMatchOptionEnum.Unmatched, Tip = "Online tables that do NOT exist in your local database"},
            new TableMatchOption {Enum = TableMatchOptionEnum.Both, Tip = "All online tables irrespective of whether they exist in your local database"}
        };

        // all possible file merge options - to be used elsewhere (importer)
        public static readonly FixFeedOption[] FixFeedOptions =
        {
            new FixFeedOption {Enum = FixFeedOptionEnum.Whitespace, Tip = "Remove excess whitespace from table and manufacturer descriptions"},
            new FixFeedOption {Enum = FixFeedOptionEnum.MissingImageUrl, Tip = "Fix missing image by assigning an alternate image from the same table (if available)"},
            new FixFeedOption {Enum = FixFeedOptionEnum.ManufacturedTableIncludesAuthor, Tip = "Remove author from manufactured table (original tables are unchanged)"},
            new FixFeedOption {Enum = FixFeedOptionEnum.OriginalTableIncludesIpdbUrl, Tip = "Remove IPDB URL from original (non-manufactured) table"},
            new FixFeedOption {Enum = FixFeedOptionEnum.CreatedTime, Tip = "Fix content creation time so that it's NOT less than the last updated time"},
            new FixFeedOption {Enum = FixFeedOptionEnum.UpdatedTime, Tip = "Fix content updated time. e.g. missing, before created time, after current time"},
            new FixFeedOption {Enum = FixFeedOptionEnum.UpgradeUrlHttps, Tip = "Upgrade URL from http to https"},
            new FixFeedOption {Enum = FixFeedOptionEnum.InvalidUrlIpdb, Tip = "Mark incorrect IPDB URL as invalid (navigation to IPDB site will be disabled)"},
            new FixFeedOption {Enum = FixFeedOptionEnum.InvalidUrlContent, Tip = "Mark incorrect content URL as invalid (navigation to web content will be disabled)"},
            new FixFeedOption {Enum = FixFeedOptionEnum.WrongUrlIpdb, Tip = "Fix IPDB URL, e.g. named tables with wrong IPDB URL, original table referencing IPDB URL"},
            new FixFeedOption {Enum = FixFeedOptionEnum.WrongUrlContent, Tip = "Fix content URL, e.g. vpuniverse.com URL path"},
            new FixFeedOption {Enum = FixFeedOptionEnum.WrongManufacturerAndYear, Tip = "Fix manufacturer name"},
            new FixFeedOption {Enum = FixFeedOptionEnum.WrongName, Tip = "Fix table name"},
            new FixFeedOption {Enum = FixFeedOptionEnum.DuplicateTable, Tip = "Merge duplicate table entries based on the IPDB URL (n/a for original tables)"},
        };
    }
}