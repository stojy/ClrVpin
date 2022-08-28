using System.Windows.Controls;
using System.Windows.Input;
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

        public ICommand FolderExplorerCommand { get; set; }
        public ICommand FolderChangedCommandWithParam { get; set; }
    }
}