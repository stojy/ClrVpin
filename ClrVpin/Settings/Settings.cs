using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ClrVpin.Models;
using PropertyChanged;
using Utils;
using ActionCommand = Microsoft.Xaml.Behaviors.Core.ActionCommand;

namespace ClrVpin.Settings
{
    [AddINotifyPropertyChangedInterface]
    public class Settings
    {
        public Settings()
        {
            FrontendFolderCommand = new ActionCommand(() => FolderUtil.Get("Frontend Root", Config.FrontendFolder, folder => Config.FrontendFolder = folder));
            AutoAssignFoldersCommand = new ActionCommand(AutoAssignFolders);

            TablesFolderCommand = new ActionCommand(() => FolderUtil.Get("Table and B2S", Config.TableFolder, folder => Config.TableFolder = folder));

            BackupFolderCommand = new ActionCommand(() => FolderUtil.Get("Backup Root", Config.BackupFolder, folder => Config.BackupFolder = folder));

            var configFrontendFolders = Config.GetFrontendFolders();
            FrontendFolders = configFrontendFolders!.Select(folder => new ContentTypeModel(folder, () => Config.SetFrontendFolders(FrontendFolders.Select(x => x.ContentType)))).ToList();
        }

        public ICommand TablesFolderCommand { get; }
        public ICommand FrontendFolderCommand { get; }
        public ICommand BackupFolderCommand { get; }
        public ICommand AutoAssignFoldersCommand { get; }

        public Config Config { get; } = Model.Config;

        public List<ContentTypeModel> FrontendFolders { get; init; }

        public void Show(Window parent)
        {
            var window = new Window
            {
                Owner = parent,
                Content = this,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Resources = parent.Resources,
                ContentTemplate = parent.FindResource("SettingsTemplate") as DataTemplate
            };
            window.Show();
            parent.Hide();

            window.Closed += (_, _) =>
            {
                Model.Config.Save();
                parent.Show();
            };
        }

        private void AutoAssignFolders()
        {
            // automatically assign folders based on the frontend root folder
            FrontendFolders.Where(x => !x.ContentType.IsDatabase).ForEach(x => x.ContentType.Folder = $@"{Config.FrontendFolder}\Media\Visual Pinball\{x.ContentType.Description}");
            FrontendFolders.First(x => x.ContentType.IsDatabase).ContentType.Folder = $@"{Config.FrontendFolder}\Media\Databases\Visual Pinball";

            Config.SetFrontendFolders(FrontendFolders.Select(x => x.ContentType));
        }
    }
}