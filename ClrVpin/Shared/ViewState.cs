using System.Windows.Input;
using PropertyChanged;

namespace ClrVpin.Shared;

[AddINotifyPropertyChangedInterface]
public class ViewState
{
    public bool IsExpanded { get; set; }
    public bool IsSelected { get; set; }

    public ICommand NavigateToIpdbCommand { get; set; }
}