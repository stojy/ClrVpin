using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ClrVpin.Controls;
using ClrVpin.Controls.FolderSelection;
using ClrVpin.Models;
using MaterialDesignExtensions.Controls;
using Microsoft.Xaml.Behaviors.Core;
using PropertyChanged;

namespace ClrVpin.Settings
{
    [AddINotifyPropertyChangedInterface]
    public class SettingsViewModel
    {
        public SettingsViewModel()
        {
            PinballFolderModel = new FolderTypeModel("Visual Pinball Executable", Settings.PinballFolder, folder => Settings.PinballFolder = folder);

            PinballContentTypeModels = Model.Settings.GetPinballContentTypes().Select(contentType => new ContentTypeModel(contentType, () => PinballContentUpdated(contentType))).ToList();

            FrontendFolderModel = new FolderTypeModel("Frontend Executable", Settings.FrontendFolder, folder => Settings.FrontendFolder = folder);
            FrontendContentTypeModels = Model.Settings.GetFrontendContentTypes().Select(contentType => new ContentTypeModel(contentType)).ToList();

            BackupFolderModel = new FolderTypeModel("Backup Root", Settings.BackupFolder, folder => Settings.BackupFolder = folder);

            AutoAssignPinballFoldersCommand = new ActionCommand(AutoAssignPinballFolders);
            AutoAssignFrontendFoldersCommand = new ActionCommand(AutoAssignFrontendFolders);
            ResetCommand = new ActionCommand(Reset);
        }

        public FolderTypeModel PinballFolderModel { get; set; }
        public List<ContentTypeModel> PinballContentTypeModels { get; set; }

        public FolderTypeModel FrontendFolderModel { get; set; }
        public List<ContentTypeModel> FrontendContentTypeModels { get; init; }

        public FolderTypeModel BackupFolderModel { get; set; }

        public ICommand AutoAssignPinballFoldersCommand { get; }
        public ICommand AutoAssignFrontendFoldersCommand { get; }
        public ICommand ResetCommand { get; }

        public Models.Settings.Settings Settings { get; } = Model.Settings;

        public void Show(Window parent)
        {
            _window = new MaterialWindowEx
            {
                Owner = parent,
                Content = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SizeToContent = SizeToContent.WidthAndHeight,
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

        private void PinballContentUpdated(ContentType contentType)
        {
            // special handling for table as we want to update the DefaultSetting.PinballTablesFolder setting, so it can be remembered after a reset!
            if (contentType.Enum == ContentTypeEnum.Tables)
                Settings.PinballTablesFolder = contentType.Folder;
        }

        private void Close()
        {
            _window.Close();
        }

        private void AutoAssignPinballFolders()
        {
            // automatically assign folders based on the pinball root folder
            PinballContentTypeModels.ForEach(x =>
            {
                // for storage
                x.ContentType.Folder = $@"{Settings.PinballTablesFolder}";

                // for display
                x.Folder = x.ContentType.Folder;
            });
        }

        private void AutoAssignFrontendFolders()
        {
            // automatically assign folders based on the frontend root folder
            FrontendContentTypeModels.ForEach(x =>
            {
                // for storage
                switch (x.ContentType.Category)
                {
                    case ContentTypeCategoryEnum.Database:
                        x.ContentType.Folder = $@"{Settings.FrontendFolder}\Databases\Visual Pinball";
                        break;
                    case ContentTypeCategoryEnum.Media:
                        switch (x.ContentType.Enum)
                        {
                            case ContentTypeEnum.InstructionCards:
                            case ContentTypeEnum.FlyerImagesBack:
                            case ContentTypeEnum.FlyerImagesFront:
                            case ContentTypeEnum.FlyerImagesInside1:
                            case ContentTypeEnum.FlyerImagesInside2:
                            case ContentTypeEnum.FlyerImagesInside3:
                            case ContentTypeEnum.FlyerImagesInside4:
                            case ContentTypeEnum.FlyerImagesInside5:
                            case ContentTypeEnum.FlyerImagesInside6:
                                x.ContentType.Folder = $@"{Settings.FrontendFolder}\Media\{x.ContentType.Description}";
                                break;
                            default:
                                x.ContentType.Folder = $@"{Settings.FrontendFolder}\Media\Visual Pinball\{x.ContentType.Description}";
                                break;
                        }
                        break;
                }

                // for display
                x.Folder = x.ContentType.Folder;
            });
        }

        private void Reset()
        {
            Model.SettingsManager.Reset();
            Close();
        }

        private Window _window;
    }
}