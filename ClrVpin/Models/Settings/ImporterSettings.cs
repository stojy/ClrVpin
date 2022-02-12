using System;
using PropertyChanged;

namespace ClrVpin.Models.Settings;

[AddINotifyPropertyChangedInterface]
public class ImporterSettings
{
    public bool IncludeOriginalTables { get; set; }
    public DateTime? UpdatedAtDateBegin { get; set; }
    public DateTime? UpdatedAtDateEnd { get; set; }
}