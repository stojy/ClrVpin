using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ClrVpin.Controls;
using ClrVpin.Controls.Folder;
using ClrVpin.Models.Shared;
using ClrVpin.Shared;
using Microsoft.Xaml.Behaviors.Core;
using PropertyChanged;

namespace ClrVpin.Settings
{
    [AddINotifyPropertyChangedInterface]
    public class SettingsViewModel : IShowViewModel
    {
        public SettingsViewModel()
        {
            PinballFolderModel = new GenericFolderTypeModel("Visual Pinball Executable", Settings.PinballFolder, false, folder => Settings.PinballFolder = folder);
            PinballContentTypeModels = Model.Settings.GetPinballContentTypes().Select(contentType => new ContentFolderTypeModel(contentType)).ToList();

            FrontendFolderModel = new GenericFolderTypeModel("PinballY/X Frontend Executable", Settings.FrontendFolder, false, folder => Settings.FrontendFolder = folder);
            FrontendContentTypeModels = Model.Settings.GetFrontendContentTypes().Select(contentType => new ContentFolderTypeModel(contentType)).ToList();

            BackupFolderModel = new GenericFolderTypeModel("Backup Root", Settings.BackupFolder, true, folder => Settings.BackupFolder = folder);

            AutoAssignPinballFoldersCommand = new ActionCommand(AutoAssignPinballFolders);
            AutoAssignFrontendFoldersCommand = new ActionCommand(AutoAssignFrontendFolders);
            ResetCommand = new ActionCommand(Reset);
            SaveCommand = new ActionCommand(Close);

            var pinballXFolder = SettingsUtils.GetPinballXFolder();
            var pinballYFolder = SettingsUtils.GetPinballYFolder();
        }

        public GenericFolderTypeModel PinballFolderModel { get; }
        public List<ContentFolderTypeModel> PinballContentTypeModels { get; }

        public GenericFolderTypeModel FrontendFolderModel { get; }
        public List<ContentFolderTypeModel> FrontendContentTypeModels { get; }

        public GenericFolderTypeModel BackupFolderModel { get; }

        public ICommand AutoAssignPinballFoldersCommand { get; }
        public ICommand AutoAssignFrontendFoldersCommand { get; }

        public ICommand ResetCommand { get; }
        public ICommand SaveCommand { get; }

        public Models.Settings.Settings Settings { get; } = Model.Settings;

        public Window Show(Window parent)
        {
            _window = new MaterialWindowEx
            {
                Owner = parent,
                Content = this,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                SizeToContent = SizeToContent.WidthAndHeight,
                Resources = parent.Resources,
                ContentTemplate = parent.FindResource("SettingsTemplate") as DataTemplate,
                ResizeMode = ResizeMode.NoResize,
                Title = "Settings",

                // limit height to activate the scroll bar, e.g. for use on lower resolution screens (aka full hd 1080px height)
                MaxHeight = Model.ScreenWorkArea.Height
            };

            _window.Show();
            _window.Closed += (_, _) => Model.SettingsManager.Write();

            return _window;
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
                x.ContentType.Folder = $@"{Settings.PinballFolder}";

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