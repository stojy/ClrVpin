using PropertyChanged;

namespace ClrVpin.Models.Shared;

[AddINotifyPropertyChangedInterface]
public class FolderType
{
    public string Folder { get; set; }
    public string Description { get; set; }
}