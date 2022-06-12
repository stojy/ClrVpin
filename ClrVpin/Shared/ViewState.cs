using System.Windows.Input;

namespace ClrVpin.Shared
{
    public class ViewState
    {
        public bool IsExpanded { get; set; }
        public bool IsSelected { get; set; }

        public ICommand NavigateToIpdbCommand { get; set; }
    }
}
