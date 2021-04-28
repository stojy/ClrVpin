using System;
using ClrVpin.Models;
using Microsoft.Xaml.Behaviors.Core;
using PropertyChanged;

namespace ClrVpin.Settings
{
    [AddINotifyPropertyChangedInterface]
    public class FolderDetailModel : FolderDetail
    {
        public FolderDetailModel(FolderDetail folderDetail, Action updateFolderDetail)
        {
            Folder = folderDetail.Folder;
            Description = folderDetail.Description;
            Extensions = string.Join(", ", folderDetail.Extensions);

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