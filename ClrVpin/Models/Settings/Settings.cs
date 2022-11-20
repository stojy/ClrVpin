using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using ClrVpin.Models.Shared;
using PropertyChanged;
using Utils.Extensions;

namespace ClrVpin.Models.Settings
{
    [AddINotifyPropertyChangedInterface]
    public class Settings : ISettings
    {
        public Settings()
        {
            // following settings are assigned BEFORE json.net deserialization potentially overwrites the values
            // - i.e. they will be overwritten where a stored setting file exists, otherwise these will become the defaults
            Version = MinVersion;

            BackupFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ClrVpin", "backup");
            EnableDiagnosticLogging = false;
            SkipLoggingForOriginalTables = true;
            EnableCheckForNewVersion = true;
            TrainerWheels = true;

            AllContentTypes = new List<ContentType>
            {
                new() { Enum = ContentTypeEnum.Tables, Tip = "Playfield table", Extensions = "*.vpx, *.vpt", KindredExtensions = "*.vbs, *.txt, *.pdf", Category = ContentTypeCategoryEnum.Pinball },
                new() { Enum = ContentTypeEnum.Backglasses, Tip = "Image used for the backglass", Extensions = "*.directb2s", Category = ContentTypeCategoryEnum.Pinball },
                new() { Enum = ContentTypeEnum.PointOfViews, Tip = "3D camera configuration", Extensions = "*.pov", Category = ContentTypeCategoryEnum.Pinball },
                new() { Enum = ContentTypeEnum.Database, Tip = "Pinball X or Pinball Y database file", Extensions = "*.xml", Category = ContentTypeCategoryEnum.Database },
                new() { Enum = ContentTypeEnum.TableAudio, Tip = "Audio used when displaying a table", Extensions = "*.mp3, *.wav", Category = ContentTypeCategoryEnum.Media },
                new() { Enum = ContentTypeEnum.LaunchAudio, Tip = "Audio used when launching a table", Extensions = "*.mp3, *.wav", Category = ContentTypeCategoryEnum.Media },
                new() { Enum = ContentTypeEnum.TableVideos, Tip = "Video used when displaying a table", Extensions = "*.f4v, *.mp4, *.mkv", Category = ContentTypeCategoryEnum.Media },
                new() { Enum = ContentTypeEnum.BackglassVideos, Tip = "Video used when displaying a table's backglass", Extensions = "*.f4v, *.mp4, *.mkv", Category = ContentTypeCategoryEnum.Media },
                new() { Enum = ContentTypeEnum.WheelImages, Tip = "Image used when displaying a table", Extensions = "*.gif, *.png, *.apng, *.jpg", Category = ContentTypeCategoryEnum.Media },
                new() { Enum = ContentTypeEnum.TopperVideos, Tip = "Video used when displaying the topper", Extensions = "*.f4v, *.mp4, *.mkv", Category = ContentTypeCategoryEnum.Media },
                new() { Enum = ContentTypeEnum.InstructionCards, Tip = "Image used when displaying instruction cards", Extensions = "*.png, *.jpg, *.swf", Category = ContentTypeCategoryEnum.Media },
                new() { Enum = ContentTypeEnum.FlyerImagesBack, Tip = "Image used when displaying flyer", Extensions = "*.png, *.jpg", Category = ContentTypeCategoryEnum.Media },
                new() { Enum = ContentTypeEnum.FlyerImagesFront, Tip = "Image used when displaying flyer", Extensions = "*.png, *.jpg", Category = ContentTypeCategoryEnum.Media },
                new() { Enum = ContentTypeEnum.FlyerImagesInside1, Tip = "Image used when displaying flyer", Extensions = "*.png, *.jpg", Category = ContentTypeCategoryEnum.Media },
                new() { Enum = ContentTypeEnum.FlyerImagesInside2, Tip = "Image used when displaying flyer", Extensions = "*.png, *.jpg", Category = ContentTypeCategoryEnum.Media },
                new() { Enum = ContentTypeEnum.FlyerImagesInside3, Tip = "Image used when displaying flyer", Extensions = "*.png, *.jpg", Category = ContentTypeCategoryEnum.Media },
                new() { Enum = ContentTypeEnum.FlyerImagesInside4, Tip = "Image used when displaying flyer", Extensions = "*.png, *.jpg", Category = ContentTypeCategoryEnum.Media },
                new() { Enum = ContentTypeEnum.FlyerImagesInside5, Tip = "Image used when displaying flyer", Extensions = "*.png, *.jpg", Category = ContentTypeCategoryEnum.Media },
                new() { Enum = ContentTypeEnum.FlyerImagesInside6, Tip = "Image used when displaying flyer", Extensions = "*.png, *.jpg", Category = ContentTypeCategoryEnum.Media }
            };
            AllContentTypes.ForEach(x => x.Description = x.Enum.GetDescription());

            // initialise all settings to enabled 
            Cleaner = new CleanerSettings();

            // very important NOT to include the database type, since doing so would cause the database file(s) to be deleted
            // - deleted because would be designated as unmatched file since no table will match 'Visual Pinball'
            Cleaner.SelectedCheckContentTypes.AddRange(GetFixableContentTypes().Select(x => x.Description).ToList());
            Cleaner.SelectedCheckHitTypes.AddRange(StaticSettings.AllHitTypes.Select(x => x.Enum).ToList());
            Cleaner.SelectedFixHitTypes.AddRange(StaticSettings.AllHitTypes.Select(x => x.Enum).ToList());

            Merger = new MergerSettings();
            Merger.SelectedMatchTypes.AddRange(StaticSettings.MatchTypes.Select(x => x.Enum).ToList());
            Merger.SelectedIgnoreCriteria.AddRange(StaticSettings.IgnoreCriteria.Select(x => x.Enum).ToList());
            Merger.SelectedMergeOptions.AddRange(StaticSettings.MergeOptions.Select(x => x.Enum).ToList());
            Merger.DeleteIgnoredFiles = true;

            Importer = new ImporterSettings();
            Importer.SelectedMatchCriteriaOptions.Add(HitTypeEnum.Fuzzy);
            Importer.SelectedFeedFixOptions.AddRange(StaticSettings.FixFeedOptions.Select(x => x.Enum).ToList());
        }

        // default settings
        // - assigned to the underlying DefaultSettings class so that they are stored independently when the config is reset
        //   e.g. when settings reset via the UI, these default settings will remain in the separate DefaultSettings.json file to be used for reseeding the Settings file
        // - accessed via Settings as a convenience
        // - need to check for null as this is assigned AFTER ctor via Init() method
        public string PinballFolder
        {
            get => _defaultSettings?.PinballFolder;
            set
            {
                if (_defaultSettings != null)
                    _defaultSettings.PinballFolder = value;
            }
        }

        public string FrontendFolder
        {
            get => _defaultSettings?.FrontendFolder;
            set
            {
                if (_defaultSettings != null)
                    _defaultSettings.FrontendFolder = value;
            }
        }
        public string Guid => _defaultSettings?.Guid;

        // ReSharper disable once MemberCanBePrivate.Global - property is serialized
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global - property is serialized
        public List<ContentType> AllContentTypes { get; set; }

        public string BackupFolder { get; set; }
        public bool EnableDiagnosticLogging { get; set; }
        public bool SkipLoggingForOriginalTables { get; set; }
        public bool EnableCheckForNewVersion { get; set; }
        public DateTime? LastCheckForNewVersion { get; set; }
        public bool TrainerWheels { get; set; }

        public decimal MatchFuzzyMinimumPercentage { get; set; } = 100;

        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global - used by Json.Net during deserialization
        public CleanerSettings Cleaner { get; set; }
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global - used by Json.Net during deserialization
        public MergerSettings Merger { get; set; }
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global - used by Json.Net during deserialization
        public ImporterSettings Importer { get; set; }

        public int Version { get; set; }

        [JsonIgnore]
        public int MinVersion { get; set; } = 2;

        public void Init(DefaultSettings defaultSettings)
        {
            _defaultSettings = defaultSettings;
        }

        // helper methods for accessing the content types
        public ContentType[] GetAllContentTypes() => AllContentTypes.ToArray();
        public ContentType[] GetFixableContentTypes() => AllContentTypes.Where(x => x.Category != ContentTypeCategoryEnum.Database).ToArray();
        public ContentType[] GetPinballContentTypes() => AllContentTypes.Where(x => x.Category == ContentTypeCategoryEnum.Pinball).ToArray();
        public ContentType[] GetFrontendContentTypes() => AllContentTypes.Where(x => x.Category != ContentTypeCategoryEnum.Pinball).ToArray();
        public ContentType[] GetMediaContentTypes() => AllContentTypes.Where(x => x.Category == ContentTypeCategoryEnum.Media).ToArray();
        public ContentType GetDatabaseContentType() => AllContentTypes.First(x => x.Category == ContentTypeCategoryEnum.Database);

        // it shouldn't be possible to select the database file since it's not selectable from the UI
        // - but with an abundance caution we explicitly ignore it since if it were included the cleaner would attempt to delete the file as 'unmatched'
        // - refer ctor
        public ContentType[] GetSelectedCheckContentTypes() => GetFixableContentTypes().Where(type => Cleaner.SelectedCheckContentTypes.Contains(type.Description)).ToArray();

        public ContentType GetSelectedDestinationContentType() => AllContentTypes.First(x => x.Description == Merger.DestinationContentType);
        public ContentType GetContentType(ContentTypeEnum contentTypeEnum) => AllContentTypes.First(x => x.Enum == contentTypeEnum);

        private DefaultSettings _defaultSettings;
    }
}