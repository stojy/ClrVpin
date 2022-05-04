using System.Collections.Generic;
using System.Linq;
using PropertyChanged;
using Utils.Extensions;

namespace ClrVpin.Models.Shared
{
    [AddINotifyPropertyChangedInterface]
    public class ContentType : FolderType
    {
        public ContentTypeEnum Enum { get; set; }
        public ContentTypeCategoryEnum Category { get; set; }

        public string Extensions { get; set; }
        public string KindredExtensions { get; set; }
        public string Tip { get; set; }

        public IEnumerable<string> ExtensionsList => Extensions?.Split(",").Select(x => x.Trim()) ?? new List<string>();
        public IEnumerable<string> KindredExtensionsList => KindredExtensions?.Split(",").Select(x => x.Trim()) ?? new List<string>();

        public override string ToString() => $"{Enum.GetDescription()}";
    }
}