using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using ClrVpin.Models.Shared;
using PropertyChanged;
using Utils.Extensions;

namespace ClrVpin.Models.Settings;

[AddINotifyPropertyChangedInterface]
[Serializable]
public class Settings : ISettings
{
    public Settings()
    {
        ApplyDefaults();
    }

    private void ApplyDefaults()
    {
        // apply ALL default values
        // - the following defaults settings are assigned BEFORE json.net deserialization potentially overwrites the values
        //   i.e. they will be overwritten where a stored setting file exists, otherwise these will become the defaults
        // - some of these settings are also assigned via the XxxxSettings member field initialization
        
        // common
        Version = MinVersion;
        BackupFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ClrVpin", "backup");
        EnableDiagnosticLogging = false;
        SkipLoggingForOriginalTables = true;
        EnableCheckForUpdatesAutomatically = true;
        TrainerWheels = true;

        AllContentTypes = new List<ContentType>
        { 
            new() { Enum = ContentTypeEnum.Tables, Tip = "Playfield table", Extensions = "*.vpx, *.vpt", KindredExtensions = "*.vbs, *.txt, *.pdf", Category = ContentTypeCategoryEnum.Pinball, IsFolderRequired = true},
            new() { Enum = ContentTypeEnum.Backglasses, Tip = "Video used for the backglass", Extensions = "*.directb2s", Category = ContentTypeCategoryEnum.Pinball },
            new() { Enum = ContentTypeEnum.PointOfViews, Tip = "3D camera configuration", Extensions = "*.pov", Category = ContentTypeCategoryEnum.Pinball },
            new() { Enum = ContentTypeEnum.Database, Tip = "Pinball X or Pinball Y database file", Extensions = "*.xml", Category = ContentTypeCategoryEnum.Database, IsFolderRequired = true},
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



        // cleaner defaults
        Cleaner = new CleanerSettings();
        // very important NOT to include the database type, since doing so would cause the database file(s) to be deleted
        // - deleted because would be designated as unmatched file since no table will match 'Visual Pinball'
        Cleaner.SelectedCheckContentTypes.AddRange(GetFixableContentTypes().Select(x => x.Description).ToList());
        Cleaner.SelectedCheckHitTypes.AddRange(StaticSettings.AllHitTypes.Select(x => x.Enum).ToList());
        Cleaner.SelectedFixHitTypes.AddRange(StaticSettings.AllHitTypes.Select(x => x.Enum).ToList());

        // merger defaults
        Merger = new MergerSettings();
        Merger.SelectedMatchTypes.AddRange(StaticSettings.MatchTypes.Select(x => x.Enum).ToList());
        Merger.SelectedIgnoreCriteria.AddRange(StaticSettings.IgnoreCriteria.Select(x => x.Enum).ToList());
        Merger.SelectedMergeOptions.AddRange(StaticSettings.MergeOptions.Select(x => x.Enum).ToList());
        Merger.DeleteIgnoredFiles = true;

        // feeder defaults
        Feeder = new FeederSettings();
        Feeder.SelectedMatchCriteriaOptions.Add(HitTypeEnum.Fuzzy);
        Feeder.SelectedFeedFixOptions.AddRange(StaticSettings.FixFeedOptions.Select(x => x.Enum).ToList());

        // explorer defaults
        Explorer = new ExplorerSettings();
    }

    // the 'guid' is a special default setting that are persistent across app 'reset settings' action
    // - assigned to the underlying DefaultSettings class so that they are stored independently when the config is reset
    //   e.g. when settings reset via the UI, these default settings will remain in the separate DefaultSettings.json file to be used for reseeding the Settings file
    // - accessed via Settings as a convenience
    // - need to check for null as this is assigned AFTER ctor via Init() method
    public string Guid => _defaultSettings?.Guid;

    // ReSharper disable once MemberCanBePrivate.Global - property is serialized
    public List<ContentType> AllContentTypes { get; set; }

    public string PinballFolder { get; set; }
    public string FrontendFolder { get; set; }
    public string BackupFolder { get; set; }

    public bool EnableDiagnosticLogging { get; set; }
    public bool SkipLoggingForOriginalTables { get; set; }
    public bool EnableCheckForUpdatesAutomatically { get; set; }
    public bool EnableCheckForUpdatesPreRelease { get; set; }
    public DateTime? LastCheckForNewVersion { get; set; }
    public bool TrainerWheels { get; set; }

    public decimal MatchFuzzyMinimumPercentage { get; set; } = 100;

    public CleanerSettings Cleaner { get; set; }
    public MergerSettings Merger { get; set; }
    public FeederSettings Feeder { get; set; }
    public ExplorerSettings Explorer { get; set; }

    public int Version { get; set; }

    [JsonIgnore]
    public int MinVersion { get; set; } = 6;

    public void Init(DefaultSettings defaultSettings)
    {
        _defaultSettings = defaultSettings;
    }

    // helper methods for accessing the content types
    public ContentType[] GetAllContentTypes() => AllContentTypes.ToArray();
    public ContentType[] GetAllValidContentTypes() => AllContentTypes.Where(x => x.IsFolderValid).ToArray();
    public ContentType[] GetFixableContentTypes() => AllContentTypes.Where(x => x.Category != ContentTypeCategoryEnum.Database).ToArray();
    public ContentType[] GetPinballContentTypes() => AllContentTypes.Where(x => x.Category == ContentTypeCategoryEnum.Pinball).ToArray();
    public ContentType[] GetFrontendContentTypes() => AllContentTypes.Where(x => x.Category != ContentTypeCategoryEnum.Pinball).ToArray();
    public ContentType[] GetMediaContentTypes() => AllContentTypes.Where(x => x.Category == ContentTypeCategoryEnum.Media).ToArray();
    public ContentType GetDatabaseContentType() => AllContentTypes.First(x => x.Category == ContentTypeCategoryEnum.Database);

    public ContentType GetSelectedDestinationContentType() => AllContentTypes.First(x => x.Description == Merger.DestinationContentType);
    public ContentType GetContentType(ContentTypeEnum contentTypeEnum) => AllContentTypes.First(x => x.Enum == contentTypeEnum);

    private DefaultSettings _defaultSettings;
}