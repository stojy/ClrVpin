using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ClrVpin.Controls.FolderSelection;
using ClrVpin.Models;
using MaterialDesignExtensions.Controls;
using PropertyChanged;
using ActionCommand = Microsoft.Xaml.Behaviors.Core.ActionCommand;

namespace ClrVpin.Settings
{
    [AddINotifyPropertyChangedInterface]
    public class SettingsViewModel
    {
        private MaterialWindow _window;

        public SettingsViewModel()
        {
            PinballFolderModel = new FolderTypeModel("Visual Pinball", Settings.PinballFolder, folder => Settings.PinballFolder = folder);

            PinballContentTypeModels = Model.Settings.GetPinballContentTypes()
                .Select(contentType => new ContentTypeModel(contentType))
                .ToList();

            FrontendFolderModel = new FolderTypeModel("Frontend", Settings.FrontendFolder, folder => Settings.FrontendFolder = folder);
            FrontendContentTypeModels = Model.Settings.GetFrontendContentTypes()
                .Select(contentType => new ContentTypeModel(contentType))
                .ToList();

            BackupFolderModel = new FolderTypeModel("Backup Root", Settings.BackupFolder, folder => Settings.BackupFolder = folder);

            AutoAssignFoldersCommand = new ActionCommand(AutoAssignFolders);
            ResetCommand = new ActionCommand(Reset);
        }

        public FolderTypeModel PinballFolderModel { get; set; }
        public List<ContentTypeModel> PinballContentTypeModels { get; set; }

        public FolderTypeModel FrontendFolderModel { get; set; }
        public List<ContentTypeModel> FrontendContentTypeModels { get; init; }
        
        public FolderTypeModel BackupFolderModel { get; set; }

        public ICommand AutoAssignFoldersCommand { get; }
        public ICommand ResetCommand { get; }

        public Models.Settings.Settings Settings { get; } = Model.Settings;

        public void Show(Window parent)
        {
            _window = new MaterialWindow
            {
                Owner = parent,
                Content = this,
                //SizeToContent = SizeToContent.WidthAndHeight,
                Height = 800,
                Width = 660,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Resources = parent.Resources,
                ContentTemplate = parent.FindResource("SettingsTemplate") as DataTemplate,
                ResizeMode = ResizeMode.NoResize,
                Title = "Settings"
            };
            _window.Show();
            parent.Hide();

            _window.Closed += (_, _) =>
            {
                Model.SettingsManager.Write();
                parent.Show();
            };
        }

        private void Close()
        {
            _window.Close();
        }

        private void AutoAssignFolders()
        {
            // automatically assign folders based on the frontend root folder
            FrontendContentTypeModels.ForEach(x =>
            {
                // for storage
                x.ContentType.Folder = x.ContentType.Category == ContentTypeCategoryEnum.Database
                    ? $@"{Settings.FrontendFolder}\Databases\Visual Pinball"
                    : $@"{Settings.FrontendFolder}\Media\Visual Pinball\{x.ContentType.Description}";

                // for display
                x.Folder = x.ContentType.Folder;
            });
        }

        private void Reset()
        {
            Model.SettingsManager.Reset();
            Close();
        }
    }
}