using System;
using Microsoft.Xaml.Behaviors.Core;
using PropertyChanged;

namespace ClrVpin.Settings
{
    [AddINotifyPropertyChangedInterface]
    public class FolderTypeModel : FolderTypeDetail
    {
        public FolderTypeModel(string description, string folder, Action<string> setFolder) 
        {
            Folder = folder;
            Description = description;

            TextChangedCommand = new ActionCommand(() =>
            {
                // for storage
                setFolder(Folder);
            });
            
            FolderExplorerCommand = new ActionCommand(() => FolderUtil.Get(Description, Folder, updateFolder =>
            {
                // for display & storage.. storage is triggered via the TextChangedCommand
                Folder = updateFolder;
            }));
        }
    }
}