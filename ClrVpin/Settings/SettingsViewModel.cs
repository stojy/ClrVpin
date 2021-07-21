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
            //TablesFolderCommand = new ActionCommand(() => FolderUtil.Get("Table and B2S", Config.TableFolder, folder => Config.TableFolder = folder));

            TableFolderModel = new FolderTypeModel("Tables and Backglasses", Settings.TableFolder, folder => Settings.TableFolder = folder);
            FrontendFolderModel = new FolderTypeModel("Frontend Root", Settings.FrontendFolder, folder => Settings.FrontendFolder = folder);
            BackupFolderModel = new FolderTypeModel("Backup Root", Settings.BackupFolder, folder => Settings.BackupFolder = folder);

            AutoAssignFoldersCommand = new ActionCommand(AutoAssignFolders);
            ResetCommand = new ActionCommand(Reset);

            FrontendFolders = Model.Settings.FrontendFolders
                .Select(folder => new ContentTypeModel(folder, () => Config.SetFrontendFolders(FrontendFolders.Select(x => x.ContentType))))
                .ToList();
        }

        public FolderTypeModel TableFolderModel { get; set; }
        public FolderTypeModel FrontendFolderModel { get; set; }
        public FolderTypeModel BackupFolderModel { get; set; }

        public ICommand AutoAssignFoldersCommand { get; }
        public ICommand ResetCommand { get; }

        public Config Config { get; } = Model.Config;
        public Models.Settings.Settings Settings { get; } = Model.Settings;

        public List<ContentTypeModel> FrontendFolders { get; init; }

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
            FrontendFolders.ForEach(x =>
            {
                // for storage
                x.ContentType.Folder = x.ContentType.IsDatabase
                    ? $@"{Settings.FrontendFolder}\Databases\Visual Pinball"
                    : $@"{Settings.FrontendFolder}\Media\Visual Pinball\{x.ContentType.Description}";

                // for display
                x.Folder = x.ContentType.Folder;
            });
            
            Config.SetFrontendFolders(FrontendFolders.Select(x => x.ContentType));
        }

        private void Reset()
        {
            Model.SettingsManager.Reset();
            Close();
        }
    }
}