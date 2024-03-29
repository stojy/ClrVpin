﻿using System.Collections.Generic;
using System.Linq;
using ClrVpin.Models.Cleaner;
using ClrVpin.Models.Feeder;
using ClrVpin.Models.Merger;
using ClrVpin.Models.Shared;
using ClrVpin.Models.Shared.Enums;
using Utils.Extensions;

namespace ClrVpin.Models.Settings
{
    public static class StaticSettings
    {
        static StaticSettings()
        {
            // common 
            TableManufacturedOptions.ForEach(x => x.Description = x.Enum.GetDescription());
            TechnologyTypeOptions.ForEach(x => x.Description = x.Enum.GetDescription());

            // cleaner
            AllHitTypes.ForEach(x => x.Description = x.Enum.GetDescription());
            FixablePrioritizedHitTypeEnums = AllHitTypes.Where(x => x.Fixable).Select(x => x.Enum).ToArray();

            MultipleMatchOptions.ForEach(x => x.Description = x.Enum.GetDescription());

            // merge
            MergeOptions.ForEach(x => x.Description = x.Enum.GetDescription());
            IgnoreCriteria.ForEach(x => x.Description = x.Enum.GetDescription());
            MatchTypes = AllHitTypes.Where(x => x.Enum.In(HitTypeEnum.CorrectName, HitTypeEnum.TableName, HitTypeEnum.WrongCase, HitTypeEnum.DuplicateExtension, HitTypeEnum.Fuzzy, HitTypeEnum.Unknown,
                HitTypeEnum.Unsupported)).ToArray();
            DeleteIgnoredFilesOption.Description = DeleteIgnoredFilesOption.Enum.GetDescription();

            // feeder
            TableMatchOptions.ForEach(x => x.Description = x.Enum.GetDescription());
            UrlStatusOptions.ForEach(x => x.Description = x.Enum.GetDescription());
            OnlineFileTypeOptions.ForEach(x => x.Description = x.Enum.GetDescription());
            FixFeedOptions.ForEach(x => x.Description = x.Enum.GetDescription());
            PresetDateOptions.ForEach(x => x.Description = x.Enum.GetDescription());
            MiscFeatureOptions.ForEach(x => x.Description = x.Enum.GetDescription());
            SimulatorOptions.ForEach(x => x.Description = x.Enum.GetDescription());

            // explorer
            MissingFileOptions.ForEach(x => x.Description = x.Enum.GetDescription());
            TableStaleOptions.ForEach(x => x.Description = x.Enum.GetDescription());
            TableRomOptions.ForEach(x => x.Description = x.Enum.GetDescription());
            TablePupOptions.ForEach(x => x.Description = x.Enum.GetDescription());
        }

        // hit types in priority order as determined by matching algorithm - refer MatchFilesToLocal
        public static HitTypeEnum[] FixablePrioritizedHitTypeEnums { get; }

        public static readonly IEnumerable<ContentTypeEnum> ImportantContentTypes = new[]
        {
            ContentTypeEnum.Tables, ContentTypeEnum.Backglasses, ContentTypeEnum.WheelImages, ContentTypeEnum.TableVideos, ContentTypeEnum.BackglassVideos
        };

        // cleaner matching hit types - to be used elsewhere (cleaner) to create check and fix collections
        public static readonly HitType[] AllHitTypes =
        {
            new(HitTypeEnum.CorrectName, true, "Files that match perfectly!"),
            new(HitTypeEnum.WrongCase, true, "Files that match the correct name, but have the wrong case"),
            new(HitTypeEnum.TableName, true, "Files that match against the table name instead of the table description - ONLY APPLICABLE FOR MEDIA CONTENT, since tables ALWAYS match the table name"),
            new(HitTypeEnum.Fuzzy, true, "Various 'fuzzy logic' algorithms to determine a match (refer help page for more info)", true, "https://github.com/stojy/ClrVpin/wiki/Fuzzy-Logic"),
            new(HitTypeEnum.DuplicateExtension, true, "Files that match the correct name AND have a configured file extension, but multiple extension matches exist (e.g. mkv and mp4"),
            new(HitTypeEnum.Missing, false, "Files that are missing.  Missing files can be downloaded via the 'Feeder' feature from the home page."),
            new(HitTypeEnum.Unknown, false, "Files that do match the configured file extension type, but don't match any of the tables in the database"),
            new(HitTypeEnum.Unsupported, false,
                "Files that don't match the configured file extension types - ONLY APPLICABLE FOR MEDIA CONTENT, since unsupported file types/extensions are EXPECTED to exist in the tables folder (e.g. txt, exe, ogg, etc)")
        };

        // merger matching criteria types - to be used elsewhere (merger)
        public static readonly HitType[] MatchTypes;

        // all possible file merge options - to be used elsewhere (merger)
        public static readonly IgnoreCriteria[] IgnoreCriteria =
        {
            new() {Enum = IgnoreCriteriaEnum.IgnoreIfContainsWords, Tip = "If the file is matched: ignore the source file if it contains any of the configured words"},
            new() {Enum = IgnoreCriteriaEnum.IgnoreIfSmaller, Tip = "If a destination file with the same name already exists: ignore the source file if it's smaller based on the specified percentage"},
            new() {Enum = IgnoreCriteriaEnum.IgnoreIfNotNewer, Tip = "If a destination file with the same name already exists: ignore the source file if it's not newer (using last modified timestamp)"},
            new() {Enum = IgnoreCriteriaEnum.IgnoreIfFileIsInvalid, Tip = "Ignore the source file if the file is considered as invalid (e.g. file size less than 1kB)"}
        };

        public static readonly EnumOption<DeleteIgnoredFilesEnum> DeleteIgnoredFilesOption = 
            new() {Enum = DeleteIgnoredFilesEnum.DeleteIgnoredFiles, Tip = "When enabled, merger will delete the ignored files (if trainer wheels is not enabled)."};

        // all possible file merge options - to be used elsewhere (merger)
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

        // all possible table style options - to be used elsewhere (feeder)
        public static readonly EnumOption<YesNoNullableBooleanOptionEnum>[] TableManufacturedOptions =
        {
            new() {Enum = YesNoNullableBooleanOptionEnum.True, Tip = "Manufactured table"},
            new() {Enum = YesNoNullableBooleanOptionEnum.False, Tip = "Original table, i.e. not manufactured"}
        };
        
        // all possible table technology type options - to be used elsewhere (feeder and explorer)
        public static readonly EnumOption<TechnologyTypeOptionEnum>[] TechnologyTypeOptions =
        {
            new() {Enum = TechnologyTypeOptionEnum.SS, Tip = "Solid State tables, created in the 1970s"},
            new() {Enum = TechnologyTypeOptionEnum.EM, Tip = "Electro Mechanical tables, created in the 1950s"},
            new() {Enum = TechnologyTypeOptionEnum.PM, Tip = "Pure mechanical tables, created in the 1930s"},
            new() {Enum = TechnologyTypeOptionEnum.Unknown, Tip = "Unknown table technology type"},
        };

        // all possible simulator applications - to be used elsewhere (feeder)
        public static readonly EnumOption<SimulatorOptionEnum>[] SimulatorOptions =
        {
            new() {Enum = SimulatorOptionEnum.VirtualPinballX, Tip = "Table file that supports Virtual Pinball X (including VP9)"},
            new() {Enum = SimulatorOptionEnum.FuturePinball, Tip = "Table file that supports Future Pinball"},
            new() {Enum = SimulatorOptionEnum.PinballFx, Tip = "Table file that supports FX Pinball (includes FX1, FX2, and FX3)"},
            new() {Enum = SimulatorOptionEnum.Unknown, Tip = "Table file that supports an unknown simulator"},
        };

        // all possible missing file options - to be used elsewhere (explorer)
        public static readonly EnumOption<ContentTypeEnum>[] MissingFileOptions =
            ImportantContentTypes.Select(contentType => new EnumOption<ContentTypeEnum> { Enum = contentType }).ToArray();

        // all possible table stale options - to be used elsewhere (explorer)
        public static readonly EnumOption<ContentTypeEnum>[] TableStaleOptions =
        {
            new() {Enum = ContentTypeEnum.TableVideos, Tip = "Table video files that are older than the table files (.vpx)"},
            new() {Enum = ContentTypeEnum.BackglassVideos, Tip = "Backglass video files that are older than the backglass files (.directb2s)"},
        };

        // all possible table ROM options - to be used elsewhere (explorer)
        public static readonly EnumOption<YesNoNullableBooleanOptionEnum>[] TableRomOptions =
        {
            new() {Enum = YesNoNullableBooleanOptionEnum.True, Tip = "Table script supports a ROM"},
            new() {Enum = YesNoNullableBooleanOptionEnum.False, Tip = "Table script does NOT support a ROM"},
        };

        // all possible table PuP options - to be used elsewhere (explorer)
        public static readonly EnumOption<YesNoNullableBooleanOptionEnum>[] TablePupOptions =
        {
            new() {Enum = YesNoNullableBooleanOptionEnum.True, Tip = "Table script supports PuP"},
            new() {Enum = YesNoNullableBooleanOptionEnum.False, Tip = "Table script does NOT support PuP"},
        };

        // all possible table match options
        public static readonly EnumOption<TableMatchOptionEnum>[] TableMatchOptions =
        {
            new() {Enum = TableMatchOptionEnum.LocalAndOnline, Tip = "Matched: tables exist in both your local database and the online feed"},
            new() {Enum = TableMatchOptionEnum.OnlineOnly, Tip = "Missing: tables only exist in the online feed, i.e. tables missing from your collection. Renaming your local table(s) to match the online feed may fix this."},
            new() {Enum = TableMatchOptionEnum.LocalOnly, Tip = "Unmatched: tables only exist in your local database, i.e. tables unmatched or missing from the online feed. Renaming your local table(s) to match the online feed may fix this."},
        };

        public static readonly EnumOption<UrlStatusEnum>[] UrlStatusOptions =
        {
            new() {Enum = UrlStatusEnum.Valid, Tip = "Tables with file contents that have valid URL(s)"},
            new() {Enum = UrlStatusEnum.Broken, Tip = "Tables with file contents that have broken URL(s)"},
            new() {Enum = UrlStatusEnum.Missing, Tip = "Tables with file contents that have missing URL(s)"}
        };
        
        public static readonly EnumOption<OnlineFileTypeEnum>[] OnlineFileTypeOptions =
        {
            new() {Enum = OnlineFileTypeEnum.Tables, Tip = "Tables with new content of type: Table"},
            new() {Enum = OnlineFileTypeEnum.Backglasses, Tip = "Tables with new content of type: Backglass"},
            new() {Enum = OnlineFileTypeEnum.DMDs, Tip = "Tables with new content of type: DMD"},
            new() {Enum = OnlineFileTypeEnum.Wheels, Tip = "Tables with new content of type: Wheel"},
            new() {Enum = OnlineFileTypeEnum.ROMs, Tip = "Tables with new content of type: ROM(s)"},
            new() {Enum = OnlineFileTypeEnum.MediaPacks, Tip = "Tables with new content of type: Media Pack"},
            new() {Enum = OnlineFileTypeEnum.Sounds, Tip = "Tables with new content of type: Sounds"},
            new() {Enum = OnlineFileTypeEnum.Toppers, Tip = "Tables with new content of type: Topper"},
            new() {Enum = OnlineFileTypeEnum.PuPPacks, Tip = "Tables with new content of type: PuP Pack"},
            new() {Enum = OnlineFileTypeEnum.POVs, Tip = "Tables with new content of type: Point of View"},
            new() {Enum = OnlineFileTypeEnum.AlternateSounds, Tip = "Tables with new content of type: Alternate Sound"},
            new() {Enum = OnlineFileTypeEnum.Rules, Tip = "Tables with new content of type: Rules"},
        };

        public static readonly EnumOption<MiscFeatureOptionEnum>[] MiscFeatureOptions =
        {
            new() {Enum = MiscFeatureOptionEnum.Standard, Tip = "Table or backglass files with a 'standard' feature set"},
            new() {Enum = MiscFeatureOptionEnum.FullDmd, Tip = "Backglass files that are designed for full DMD"},
            new() {Enum = MiscFeatureOptionEnum.VirtualRealityOnly, Tip = "Table files that only support virtual reality"},
            new() {Enum = MiscFeatureOptionEnum.FullSingleScreenOnly, Tip = "Table files that only support full single screen, e.g. backglass, dmd, and table rendered on a single monitor"},
            new() {Enum = MiscFeatureOptionEnum.MusicOrSoundMod, Tip = "Table files that are music and/or sound modifications"},
            new() {Enum = MiscFeatureOptionEnum.BlackAndWhiteMod, Tip = "Table files that are black & white modifications"},
            new() {Enum = MiscFeatureOptionEnum.Patch, Tip = "Table files that are patches that require the original table file to be manually merged with the patch"},
        };

        // all possible file merge options - to be used elsewhere (feeder)
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
            new() {Enum = FixFeedOptionEnum.WrongType, Tip = "Fix table type, i.e. PM, EM, or SS"},
            new() {Enum = FixFeedOptionEnum.DuplicateTable, Tip = "Merge duplicate table entries based on the IPDB URL (n/a for original tables)"},
            new() {Enum = FixFeedOptionEnum.WrongSimulator, Tip = "Fix simulator type, e.g. table designated as FP instead of VPX"}
        };

        public static readonly EnumOption<PresetDateOptionEnum>[] PresetDateOptions =
        {
            new() {Enum = PresetDateOptionEnum.Today},
            new() {Enum = PresetDateOptionEnum.Yesterday},
            new() {Enum = PresetDateOptionEnum.LastThreeDays},
            new() {Enum = PresetDateOptionEnum.LastFiveDays},
            new() {Enum = PresetDateOptionEnum.LastWeek},
            new() {Enum = PresetDateOptionEnum.LastTwoWeeks},
            new() {Enum = PresetDateOptionEnum.LastMonth},
            new() {Enum = PresetDateOptionEnum.LastThreeMonths},
            new() {Enum = PresetDateOptionEnum.LastSixMonths},
            new() {Enum = PresetDateOptionEnum.LastYear}
        };
    }
}