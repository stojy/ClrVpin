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
            Description = contentType.Description;
            Extensions = string.Join(", ", contentType.Extensions);
            IsDatabase = contentType.Description == "Database";

            ChangedCommand = new ActionCommand(updateFolderDetail);
            
            FolderExplorerCommand = new ActionCommand(() => FolderUtil.Get(Description, Folder, folder =>
            {
                Folder = folder;
                updateFolderDetail();
            }));
        }

        public ActionCommand FolderExplorerCommand { get; set; }
        public ActionCommand ChangedCommand { get; set; }
    }
}