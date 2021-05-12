using System;
using ClrVpin.Models;
using Microsoft.Xaml.Behaviors.Core;
using PropertyChanged;

namespace ClrVpin.Settings
{
    [AddINotifyPropertyChangedInterface]
    public class ContentTypeModel
    {
        public ContentTypeModel(ContentType contentType, Action updateFolderDetail)
        {
            ContentType = contentType;

            Extensions = string.Join(", ", contentType.Extensions);

            ChangedCommand = new ActionCommand(updateFolderDetail);
            
            FolderExplorerCommand = new ActionCommand(() => FolderUtil.Get(ContentType.Description, ContentType.Folder, folder =>
            {
                ContentType.Folder = folder;
                updateFolderDetail();
            }));
        }

        public ContentType ContentType { get; set; }
        public string Extensions { get; set; }
        public ActionCommand FolderExplorerCommand { get; set; }
        public ActionCommand ChangedCommand { get; set; }
    }
}