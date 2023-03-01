using System;
using System.Windows.Controls;
using PropertyChanged;
using Utils;

namespace ClrVpin.Controls.Folder
{
    [AddINotifyPropertyChangedInterface]
    public class GenericFolderTypeModel : FolderTypeDetail
    {
        public GenericFolderTypeModel(string description, string folder, Action<string> setFolder) 
        {
            Folder = folder;
            Description = description;

            FolderChangedCommandWithParam = new ActionCommand<TextChangedEventArgs>(e =>
            {
                // workaround for validation error not updating binding
                // - refer ContentFolderTypeModel comments
                if (e.Source is TextBox textBox)
                    Folder = textBox.Text;

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