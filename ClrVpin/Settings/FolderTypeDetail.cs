using Microsoft.Xaml.Behaviors.Core;
using PropertyChanged;

namespace ClrVpin.Settings
{
    [AddINotifyPropertyChangedInterface]
    public class FolderTypeDetail
    {
        public string Folder { get; set; }
        public string Description { get; set; }
        public string Pattern { get; set; }
        public string Extensions { get; set; }

        public ActionCommand FolderExplorerCommand { get; set; }
        public ActionCommand TextChangedCommand { get; set; }
    }
}