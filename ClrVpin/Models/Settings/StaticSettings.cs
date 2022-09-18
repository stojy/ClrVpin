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
            TableAvailabilityOptions.ForEach(x => x.Description = x.Enum.GetDescription());
            TableNewContentOptions.ForEach(x => x.Description = x.Enum.GetDescription());
            FixFeedOptions.ForEach(x => x.Description = x.Enum.GetDescription());
        }

        // hit types in priority order as determined by matching algorithm - refer AddContentFilesToGames
        public static HitTypeEnum[] FixablePrioritizedHitTypeEnums { get; }

        //private static Settings Settings => Model.Settings;

        // scanner matching hit types - to be used elsewhere (scanner) to create check and fix collections
        public static readonly HitType[] AllHitTypes =
        {
            new(HitTypeEnum.CorrectName, true, "Files that match perfectly!"),
            new(HitTypeEnum.WrongCase, true, "Files that match the correct name, but have the wrong case"),
            new(HitTypeEnum.TableName, true, "Files that match against the table name instead of the table description - ONLY APPLICABLE FOR MEDIA CONTENT, since tables ALWAYS match the table name"),
            new(HitTypeEnum.Fuzzy, true, "Various 'fuzzy logic' algorithms to determine a match (refer help page for more info)", true, "https://github.com/stojy/ClrVpin/wiki/Fuzzy-Logic"),
            new(HitTypeEnum.DuplicateExtension, true, "Files that match the correct name AND have a configured file extension, but multiple extension matches exist (e.g. mkv and mp4"),
            new(HitTypeEnum.Missing, false, "Files that are missing, i.e. they need to be downloaded from your favorite pinball site(s)"),
            new(HitTypeEnum.Unknown, false, "Files that do match the configured file extension type, but don't match any of the tables in the database"),
            new(HitTypeEnum.Unsupported, false,
                "Files that don't match the configured file extension types - ONLY APPLICABLE FOR MEDIA CONTENT, since unsupported files are EXPECTED to exist in the tables folder (e.g. txt, exe, ogg, etc)")
        };

        // rebuilder matching criteria types - to be used elsewhere (rebuilder)
        public static readonly HitType[] MatchTypes;

        // all possible file merge options - to be used elsewhere (rebuilder)
        public static readonly IgnoreCriteria[] IgnoreCriteria =
        {
            new() {Enum = IgnoreCriteriaEnum.IgnoreIfContainsWords, Tip = "If the file is matched: ignore the source file if it contains any of the configured words"},
            new() {Enum = IgnoreCriteriaEnum.IgnoreIfSmaller, Tip = "If a destination file with the same name already exists: ignore the source file if it's smaller based on the specified percentage"},
            new() {Enum = IgnoreCriteriaEnum.IgnoreIfNotNewer, Tip = "If a destination file with the same name already exists: ignore the source file if it's not newer (using last modified timestamp)"}
        };

        public static readonly Option DeleteIgnoredFilesOption = new() {Tip = "When enabled, rebuilder will delete the ignored files (if trainer wheels is not enabled).", Description = "Delete Ignored Files"};

        // all possible file merge options - to be used elsewhere (rebuilder)
        public static readonly MergeOption[] MergeOptions =
        {
            new() {Enum = MergeOptionEnum.PreserveDateModified, Tip = "Date modified timestamp of merged file (in the destination folder) will match the source file, else the current time will be used"},
            new() {Enum = MergeOptionEnum.RemoveSource, Tip = "Matched source files will be removed (copied to the backup folder)"}
        };

        // all possible multiple match fix options
        public static readonly MultipleMatchOption[] MultipleMatchOptions =
        {
            new()
            {
                Enum = MultipleMatchOptionEnum.PreferCorrectName, Tip = "File with the correct matching name is used, if it doesn't exist then the following names are used (in descending order): WrongCase, TableName, and Fuzzy."
            },
            new() {Enum = MultipleMatchOptionEnum.PreferMostRecent, Tip = "File with the most recent modified timestamp is used"},
            new() {Enum = MultipleMatchOptionEnum.PreferLargestSize, Tip = "File with the largest size is used"},
            new()
            {
                Enum = MultipleMatchOptionEnum.PreferMostRecentAndExceedSizeThreshold,
                Tip = "File with the most recent modified timestamp AND exceeds the size threshold of the existing correct file (if one exists) is used, i.e. avoid using newer but smaller files"
            }
        };

        // all possible table style options - to be used elsewhere (importer)
        public static readonly EnumOption<TableStyleOptionEnum>[] TableStyleOptions =
        {
            new() {Enum = TableStyleOptionEnum.Manufactured, Tip = "A physical table has been manufactured"},
            new() {Enum = TableStyleOptionEnum.Original, Tip = "An original table creation that has not been manufactured"},
            new() {Enum = TableStyleOptionEnum.Both, Tip = "Manufactured AND original tables"}
        };

        // all possible table match options
        public static readonly EnumOption<TableMatchOptionEnum>[] TableMatchOptions =
        {
            new() {Enum = TableMatchOptionEnum.LocalAndOnline, Tip = "Tables that exist in both local database and the online feed"},
            new() {Enum = TableMatchOptionEnum.OnlineOnly, Tip = "Tables that only exist in the online feed, i.e. tables missing from your collection"},
            new() {Enum = TableMatchOptionEnum.LocalOnly, Tip = "Tables that only exist in your local database, i.e. unmatched tables that may require renaming to match the online feed"},
            new() {Enum = TableMatchOptionEnum.All, Tip = "All tables irrespective of whether they are matched, missing, or unmatched"}
        };

        public static readonly EnumOption<TableAvailabilityOptionEnum>[] TableAvailabilityOptions =
        {
            new() {Enum = TableAvailabilityOptionEnum.Available, Tip = "Tables that are available for download, i.e. valid table URL(s) exist"},
            new() {Enum = TableAvailabilityOptionEnum.Unavailable, Tip = "Tables that are unavailable for download, i.e. no valid URL(s) exist"},
            new() {Enum = TableAvailabilityOptionEnum.Both, Tip = "Available and unavailable tables"}
        };
        
        public static readonly EnumOption<TableNewContentOptionEnum>[] TableNewContentOptions =
        {
            new() {Enum = TableNewContentOptionEnum.TableBackglassDmd, Tip = "Tables with new content of type: Table, Backglass, or DMDs"},
            new() {Enum = TableNewContentOptionEnum.Other, Tip = "Tables with new content of type: Wheels, ROMs, Media Packs, Sounds, Toppers, PuP Packs, POVs, Alt. Sounds, or Rules"},
            new() {Enum = TableNewContentOptionEnum.All, Tip = "Tables with new content of any type, including any unmatched tables where new content is n/a"}
        };

        // all possible file merge options - to be used elsewhere (importer)
        public static readonly EnumOption<FixFeedOptionEnum>[] FixFeedOptions =
        {
            new() {Enum = FixFeedOptionEnum.InvalidCharacters, Tip = "Fix or remove any invalid characters (anything not supported by the windows file system)"},
            new() {Enum = FixFeedOptionEnum.Whitespace, Tip = "Remove excess whitespace from table and manufacturer descriptions"},
            new() {Enum = FixFeedOptionEnum.MissingImageUrl, Tip = "Fix missing image by assigning an alternate image from the same table (if available)"},
            new() {Enum = FixFeedOptionEnum.ManufacturedIncludesAuthor, Tip = "Remove author from manufactured table (original tables are unchanged)"},
            new() {Enum = FixFeedOptionEnum.OriginalTableIncludesIpdbUrl, Tip = "Remove IPDB URL from original (non-manufactured) table"},
            new() {Enum = FixFeedOptionEnum.CreatedTime, Tip = "Fix content creation time so that it's NOT less than the last updated time"},
            new() {Enum = FixFeedOptionEnum.UpdatedTime, Tip = "Fix content updated time. e.g. missing, before created time, after current time"},
            new() {Enum = FixFeedOptionEnum.UpgradeUrlHttps, Tip = "Upgrade URL from http to https"},
            new() {Enum = FixFeedOptionEnum.InvalidUrlIpdb, Tip = "Mark incorrect IPDB URL as invalid (navigation to IPDB site will be disabled)"},
            new() {Enum = FixFeedOptionEnum.InvalidUrlContent, Tip = "Mark incorrect content URL as invalid (navigation to web content will be disabled)"},
            new() {Enum = FixFeedOptionEnum.WrongUrlIpdb, Tip = "Fix IPDB URL, e.g. named tables with wrong IPDB URL, original table referencing IPDB URL"},
            new() {Enum = FixFeedOptionEnum.WrongUrlContent, Tip = "Fix content URL, e.g. vpuniverse.com URL path"},
            new() {Enum = FixFeedOptionEnum.WrongName, Tip = "Fix table name"},
            new() {Enum = FixFeedOptionEnum.WrongManufacturerYear, Tip = "Fix manufacturer name"},
            new() {Enum = FixFeedOptionEnum.DuplicateTable, Tip = "Merge duplicate table entries based on the IPDB URL (n/a for original tables)"},
        };
    }
}