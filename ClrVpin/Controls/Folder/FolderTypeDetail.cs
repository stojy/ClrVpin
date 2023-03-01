using System.Windows.Input;
using PropertyChanged;

namespace ClrVpin.Controls.Folder
{
    [AddINotifyPropertyChangedInterface]
    public class FolderTypeDetail
    {
        public string Folder { get; set; }
        public string Description { get; set; }
        public string Extensions { get; set; }
        public string KindredExtensions { get; set; }
        
        public string PatternValidation { get; set; }

        public ICommand FolderExplorerCommand { get; protected init; }
        public ICommand FolderChangedCommandWithParam { get; set; }
    }
}