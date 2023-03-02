using System;
using ClrVpin.Models.Feeder;
using PropertyChanged;

namespace ClrVpin.Models.Shared;

[AddINotifyPropertyChangedInterface]
public class FolderType<T> : EnumOption<T> where T : Enum
{
    public string Folder { get; set; }
    
    // required folders are reserved for those that are essential for operation of both VPX (i.e. tables) and frontend (i.e. database)
    // - refer https://github.com/stojy/ClrVpin/issues/53
    public bool IsFolderRequired { get; set; }
    
    public bool IsFolderValid { get; set; }
}