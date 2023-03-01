using System;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace ClrVpin.Controls.Folder
{
    public class FolderUtil
    {
        public static void Get(string description, string folder, Action<string> updateAction)
        {
            var openFileDialog = new CommonOpenFileDialog
            {
                InitialDirectory = folder,
                DefaultDirectory = "c:\\",
                EnsurePathExists = true,
                Title = $"Select folder: {description}",
                IsFolderPicker = true
            };
            if (openFileDialog.ShowDialog() == CommonFileDialogResult.Ok)
                updateAction(openFileDialog.FileName);
        }
    }
}