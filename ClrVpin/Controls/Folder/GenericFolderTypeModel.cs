using System;
using System.Windows.Controls;
using PropertyChanged;
using Utils;

namespace ClrVpin.Controls.Folder;

[AddINotifyPropertyChangedInterface]
public class GenericFolderTypeModel : FolderTypeDetail
{
    public GenericFolderTypeModel(string description, string folder, bool isRequired, Action<string> setFolder)
    {
        _setFolder = setFolder;
        Folder = folder;
        Description = $"{description} {(isRequired ? "¹" : "²")}";
        IsRequired = isRequired;

        FolderChangedCommandWithParam = new ActionCommand<TextChangedEventArgs>(e =>
        {
            // workaround for validation error not updating binding
            // - refer ContentFolderTypeModel comments
            if (e.Source is TextBox textBox)
                Folder = textBox.Text;

            // for storage
            _setFolder(Folder);
        });

        FolderExplorerCommand = new ActionCommand(() => FolderUtil.Get(Description, Folder, updateFolder =>
        {
            // for display & storage.. storage is triggered via the TextChangedCommand
            Folder = updateFolder;
        }));
    }

    public void SetFolder(string folder)
    {
        // for storage
        _setFolder(Folder);

        // for display
        Folder = folder;
    }

    private readonly Action<string> _setFolder;
}