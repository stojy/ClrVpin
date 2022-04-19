using System.Windows.Controls;
using PropertyChanged;
using Utils;

namespace ClrVpin.Controls.FolderSelection
{
    [AddINotifyPropertyChangedInterface]
    public class FolderTypeDetail
    {
        public string Folder { get; set; }
        public string Description { get; set; }
        public string Extensions { get; set; }
        public string KindredExtensions { get; set; }
        
        public string PatternValidation { get; set; }

        public ActionCommand FolderExplorerCommand { get; set; }
        public ActionCommand<TextChangedEventArgs> FolderChangedCommandWithParam { get; set; }
    }
}