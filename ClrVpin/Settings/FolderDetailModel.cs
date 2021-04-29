using System;
using ClrVpin.Models;
using Microsoft.Xaml.Behaviors.Core;
using PropertyChanged;

namespace ClrVpin.Settings
{
    [AddINotifyPropertyChangedInterface]
    public class ContentTypeModel : ContentType
    {
        public ContentTypeModel(ContentType contentType, Action updateFolderDetail)
        {
            Folder = contentType.Folder;
            Type = contentType.Type;
            Extensions = string.Join(", ", contentType.Extensions);
            IsDatabase = contentType.Type == Config.Database;

            ChangedCommand = new ActionCommand(updateFolderDetail);
            
            FolderExplorerCommand = new ActionCommand(() => FolderUtil.Get(Type, Folder, folder =>
            {
                Folder = folder;
                updateFolderDetail();
            }));
        }

        public ActionCommand FolderExplorerCommand { get; set; }
        public ActionCommand ChangedCommand { get; set; }
    }
}