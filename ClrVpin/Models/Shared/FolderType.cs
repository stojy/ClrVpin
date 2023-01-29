using System;
using ClrVpin.Models.Feeder;
using PropertyChanged;

namespace ClrVpin.Models.Shared;

[AddINotifyPropertyChangedInterface]
public class FolderType<T> : EnumOption<T> where T : Enum
{
    public string Folder { get; set; }
}