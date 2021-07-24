using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PropertyChanged;
using Utils;

namespace ClrVpin.Models.Settings
{
    [AddINotifyPropertyChangedInterface]
    public class Settings
    {
        public Settings()
        {
            // default settings
            // - during json.net deserialization.. ctor is invoked BEFORE deserialized version overwrites the values, i.e. they will be overwritten where a stored setting exists
            Version = MinVersion;

            PinballFolder = @"C:\vp\apps\vpx";

            BackupFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ClrVpin", "backup");
            TrainerWheels = true;

            FrontendFolder = @"C:\vp\apps\PinballX";
            AllContentTypes = new List<ContentType>
            {
                new ContentType {Enum = ContentTypeEnum.Tables, Tip = "Playfield table", Extensions = "*.vpx, *.vpt", Category = ContentTypeCategoryEnum.Pinball},
                new ContentType {Enum = ContentTypeEnum.Backglasses, Tip = "Image used for the backglass", Extensions = "*.directb2s", Category = ContentTypeCategoryEnum.Pinball},
                new ContentType {Enum = ContentTypeEnum.PointOfViews, Tip = "3D camera configuration", Extensions = "*.pov", Category = ContentTypeCategoryEnum.Pinball},
                new ContentType {Enum = ContentTypeEnum.Database, Tip = "Pinball X or Pinball Y database file", Extensions = "*.xml", Category = ContentTypeCategoryEnum.Database},
                new ContentType {Enum = ContentTypeEnum.TableAudio, Tip = "Audio used when displaying a table", Extensions = "*.mp3, *.wav", Category = ContentTypeCategoryEnum.Media},
                new ContentType {Enum = ContentTypeEnum.LaunchAudio, Tip = "Audio used when launching a table", Extensions = "*.mp3, *.wav", Category = ContentTypeCategoryEnum.Media},
                new ContentType {Enum = ContentTypeEnum.TableVideos, Tip = "Video used when displaying a table", Extensions = "*.f4v, *.mp4, *.mkv", Category = ContentTypeCategoryEnum.Media},
                new ContentType {Enum = ContentTypeEnum.BackglassVideos, Tip = "Video used when displaying a table's backglass", Extensions = "*.f4v, *.mp4, *.mkv", Category = ContentTypeCategoryEnum.Media},
                new ContentType {Enum = ContentTypeEnum.WheelImages, Tip = "Image used when displaying a table", Extensions = "*.png, *.apng, *.jpg", Category = ContentTypeCategoryEnum.Media}
            };
            AllContentTypes.ForEach(x => x.Description = x.Enum.GetDescription());

            Rebuilder = new RebuilderSettings();
            Scanner = new ScannerSettings();
        }

        public int Version { get; set; }

        public string PinballFolder { get; set; } // todo; remove?
        
        public string FrontendFolder { get; set; }
        public List<ContentType> AllContentTypes { get; set; }

        public string BackupFolder { get; set; }
        public bool TrainerWheels { get; set; }

        public ScannerSettings Scanner { get; set; }
        public RebuilderSettings Rebuilder { get; set; }

        public static int MinVersion = 1;

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
    }
}