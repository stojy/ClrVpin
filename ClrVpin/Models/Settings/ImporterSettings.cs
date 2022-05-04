using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ClrVpin.Models.Rebuilder;
using PropertyChanged;

namespace ClrVpin.Models.Settings;

[AddINotifyPropertyChangedInterface]
public class ImporterSettings
{
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global - setter required for json.net
    public ObservableCollection<HitTypeEnum> SelectedMatchTypes { get; set; } = new ObservableCollection<HitTypeEnum>();
    public ObservableCollection<IgnoreOptionEnum> SelectedIgnoreOptions { get; set; } = new ObservableCollection<IgnoreOptionEnum>();

    public List<string> IgnoreIWords { get; set; } = new List<string>{"nude", "adult", "tba", "original", "orginal", "b&w", " bw ", "2scr", "2 screen"};

    // filtering options
    public bool IncludeOriginalTables { get; set; }
    public DateTime? UpdatedAtDateBegin { get; set; }
    public DateTime? UpdatedAtDateEnd { get; set; }
}