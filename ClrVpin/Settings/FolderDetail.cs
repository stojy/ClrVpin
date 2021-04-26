using System;
using Microsoft.Xaml.Behaviors.Core;
using PropertyChanged;

namespace ClrVpin.Settings
{
    [AddINotifyPropertyChangedInterface]
    public class FolderDetail
    {
        private readonly Func<string> _getFolder;
        private readonly Action<string> _setFolder;

        public string Folder
        {
            get => _getFolder();
            set => _setFolder(value);
        }

        public string Extensions { get; set; }
        public string Description { get; }
        public ActionCommand SelectCommand { get; set; }

        public FolderDetail(string description, Func<string> getFolder, Action<string> setFolder, string extensions)
        {
            _setFolder = setFolder;
            _getFolder = getFolder;

            Description = description;
            Extensions = string.Join(", ", extensions);
            SelectCommand = new ActionCommand(() => FolderUtil.Get(description, Folder, folder => Folder = folder));
        }
    }
}