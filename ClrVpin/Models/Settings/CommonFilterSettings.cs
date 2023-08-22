using System;
using System.Collections.ObjectModel;
using ClrVpin.Models.Shared.Enums;
using PropertyChanged;

namespace ClrVpin.Models.Settings;

[AddINotifyPropertyChangedInterface]
[Serializable]
public class CommonFilterSettings
{
    public CommonFilterSettings()
    {
        // default settings
        SelectedTechnologyTypeOptions = new ObservableCollection<TechnologyTypeOptionEnum>
        {
            TechnologyTypeOptionEnum.SS, TechnologyTypeOptionEnum.EM, TechnologyTypeOptionEnum.PM, TechnologyTypeOptionEnum.Unknown
        };

        SelectedManufacturedOptions = new ObservableCollection<YesNoNullableBooleanOptionEnum> { YesNoNullableBooleanOptionEnum.True };
    }
    public bool IsDynamicFiltering { get; set; }

    public string SelectedTableFilter { get; set; }
    public string SelectedManufacturerFilter { get; set; }

    public ObservableCollection<TechnologyTypeOptionEnum> SelectedTechnologyTypeOptions { get; set; }
    
    public string SelectedYearBeginFilter { get; set; }
    public string SelectedYearEndFilter { get; set; }

    public DateTime? SelectedUpdatedAtDateBegin { get; set; }
    public DateTime? SelectedUpdatedAtDateEnd { get; set; }

    public ObservableCollection<YesNoNullableBooleanOptionEnum> SelectedManufacturedOptions { get; set; }
}