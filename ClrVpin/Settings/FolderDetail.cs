using System;
using Microsoft.Xaml.Behaviors.Core;
using PropertyChanged;

namespace ClrVpin.Settings
{
    [AddINotifyPropertyChangedInterface]
    public class FolderDetail
    {
        public string Folder { get; set; }

        public string Extensions { get; set; }
        public string Description { get; }
        public ActionCommand FolderExplorerCommand { get; set; }
        public ActionCommand FolderChangedCommand { get; set; }

        public FolderDetail(string description, string folder, Action<string> setFolder, string extensions)
        {
            Folder = folder;
            Description = description;
            Extensions = string.Join(", ", extensions);
            
            FolderChangedCommand = new ActionCommand(() => setFolder(Folder));
            FolderExplorerCommand = new ActionCommand(() => FolderUtil.Get(description, Folder, setFolder));
        }
    }
}