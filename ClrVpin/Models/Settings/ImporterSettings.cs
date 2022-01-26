using System;
using PropertyChanged;

namespace ClrVpin.Models.Settings;

[AddINotifyPropertyChangedInterface]
public class ImporterSettings
{
    public bool IncludeOriginalTables { get; set; }
    public DateTime? UpdatedDateBegin { get; set; }
    public DateTime? UpdatedDateEnd { get; set; }
}