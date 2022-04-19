using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using PropertyChanged;
using Utils.Extensions;

namespace ClrVpin.Models.Settings
{
    [AddINotifyPropertyChangedInterface]
    public class Settings : ISettings
    {
        public Settings()
        {
            // temporary default settings instance to prevent the json deserializer exceptions as it invoke the property getters/setters
            // - the 'real' defaultSettings will be assigned after construction via Init()
            _defaultSettings = new DefaultSettings();

            // default settings
            // - during json.net deserialization.. ctor is invoked BEFORE deserialized version overwrites the values, i.e. they will be overwritten where a stored setting exists
            Version = MinVersion;

            BackupFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ClrVpin", "backup");
            EnableDiagnosticLogging = false;
            TrainerWheels = true;

            AllContentTypes = new List<ContentType>
            {
                new ContentType {Enum = ContentTypeEnum.Tables, Tip = "Playfield table", Extensions = "*.vpx, *.vpt", KindredExtensions = "*.vbs, *.txt, *.pdf", Category = ContentTypeCategoryEnum.Pinball},
                new ContentType {Enum = ContentTypeEnum.Backglasses, Tip = "Image used for the backglass", Extensions = "*.directb2s", Category = ContentTypeCategoryEnum.Pinball},
                new ContentType {Enum = ContentTypeEnum.PointOfViews, Tip = "3D camera configuration", Extensions = "*.pov", Category = ContentTypeCategoryEnum.Pinball},
                new ContentType {Enum = ContentTypeEnum.Database, Tip = "Pinball X or Pinball Y database file", Extensions = "*.xml", Category = ContentTypeCategoryEnum.Database},
                new ContentType {Enum = ContentTypeEnum.TableAudio, Tip = "Audio used when displaying a table", Extensions = "*.mp3, *.wav", Category = ContentTypeCategoryEnum.Media},
                new ContentType {Enum = ContentTypeEnum.LaunchAudio, Tip = "Audio used when launching a table", Extensions = "*.mp3, *.wav", Category = ContentTypeCategoryEnum.Media},
                new ContentType {Enum = ContentTypeEnum.TableVideos, Tip = "Video used when displaying a table", Extensions = "*.f4v, *.mp4, *.mkv", Category = ContentTypeCategoryEnum.Media},
                new ContentType {Enum = ContentTypeEnum.BackglassVideos, Tip = "Video used when displaying a table's backglass", Extensions = "*.f4v, *.mp4, *.mkv", Category = ContentTypeCategoryEnum.Media},
                new ContentType {Enum = ContentTypeEnum.WheelImages, Tip = "Image used when displaying a table", Extensions = "*.png, *.apng, *.jpg", Category = ContentTypeCategoryEnum.Media},
                new ContentType {Enum = ContentTypeEnum.TopperVideos, Tip = "Video used when displaying the topper", Extensions = "*.f4v, *.mp4, *.mkv", Category = ContentTypeCategoryEnum.Media},
                new ContentType {Enum = ContentTypeEnum.InstructionCards, Tip = "Image used when displaying instruction cards", Extensions = "*.png, *.jpg, *.swf", Category = ContentTypeCategoryEnum.Media},
                new ContentType {Enum = ContentTypeEnum.FlyerImagesBack, Tip = "Image used when displaying flyer", Extensions = "*.png, *.jpg", Category = ContentTypeCategoryEnum.Media},
                new ContentType {Enum = ContentTypeEnum.FlyerImagesFront, Tip = "Image used when displaying flyer", Extensions = "*.png, *.jpg", Category = ContentTypeCategoryEnum.Media},
                new ContentType {Enum = ContentTypeEnum.FlyerImagesInside1, Tip = "Image used when displaying flyer", Extensions = "*.png, *.jpg", Category = ContentTypeCategoryEnum.Media},
                new ContentType {Enum = ContentTypeEnum.FlyerImagesInside2, Tip = "Image used when displaying flyer", Extensions = "*.png, *.jpg", Category = ContentTypeCategoryEnum.Media},
                new ContentType {Enum = ContentTypeEnum.FlyerImagesInside3, Tip = "Image used when displaying flyer", Extensions = "*.png, *.jpg", Category = ContentTypeCategoryEnum.Media},
                new ContentType {Enum = ContentTypeEnum.FlyerImagesInside4, Tip = "Image used when displaying flyer", Extensions = "*.png, *.jpg", Category = ContentTypeCategoryEnum.Media},
                new ContentType {Enum = ContentTypeEnum.FlyerImagesInside5, Tip = "Image used when displaying flyer", Extensions = "*.png, *.jpg", Category = ContentTypeCategoryEnum.Media},
                new ContentType {Enum = ContentTypeEnum.FlyerImagesInside6, Tip = "Image used when displaying flyer", Extensions = "*.png, *.jpg", Category = ContentTypeCategoryEnum.Media}
            };
            AllContentTypes.ForEach(x => x.Description = x.Enum.GetDescription());

            Rebuilder = new RebuilderSettings();
            Scanner = new ScannerSettings();
            Importer = new ImporterSettings();
        }

        // default settings are assigned to the underlying DefaultSettings class so that they are maintained when the config is reset, e.g. via the settings-reset ui
        public string PinballFolder
        {
            get => _defaultSettings.PinballFolder;
            set => _defaultSettings.PinballFolder = value;
        }

        public string PinballTablesFolder
        {
            get => _defaultSettings.PinballTablesFolder;
            set => _defaultSettings.PinballTablesFolder = value;
        }

        public string FrontendFolder
        {
            get => _defaultSettings.FrontendFolder;
            set => _defaultSettings.FrontendFolder = value;
        }

        public List<ContentType> AllContentTypes { get; set; }

        public string BackupFolder { get; set; }
        public bool EnableDiagnosticLogging { get; set; }
        public bool TrainerWheels { get; set; }
        
        public decimal MatchFuzzyMinimumPercentage { get; set; } = 100;

        public ScannerSettings Scanner { get; set; }
        public RebuilderSettings Rebuilder { get; set; }
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

        public ContentType[] GetSelectedCheckContentTypes() => AllContentTypes.Where(type => Scanner.SelectedCheckContentTypes.Contains(type.Description)).ToArray();
        public ContentType GetSelectedDestinationContentType() => AllContentTypes.First(x => x.Description == Rebuilder.DestinationContentType);
        public ContentType GetContentType(ContentTypeEnum contentTypeEnum) => AllContentTypes.First(x => x.Enum == contentTypeEnum);
        private DefaultSettings _defaultSettings;
    }
}