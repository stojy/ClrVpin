using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ClrVpin.Models;
using Microsoft.Xaml.Behaviors.Core;
using PropertyChanged;

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

            var configFrontendFolders = Config.GetFrontendFolders();
            FrontendFolders = configFrontendFolders!.Select(folder => new FolderDetailModel(folder, () => Config.SetFrontendFolders(FrontendFolders))).ToList();

            // todo; table folders
            //new ContentType {Type = "Tables", Extensions = new[] {"*.png"}, GetXxxHits = g => g.WheelImageHits},
            //new ContentType {Type = "Backglass", Extensions = new[] {"*.png"}, GetXxxHits = g => g.WheelImageHits},
            //new ContentType {Type = "Point of View", Extensions = new[] {"*.png"}, GetXxxHits = g => g.WheelImageHits},
        }

        public ICommand TablesFolderCommand { get; }
        public ICommand FrontendFolderCommand { get; }
        public ICommand AutoAssignFoldersCommand { get; }

        public Config Config { get; } = Model.Config;

        public List<FolderDetailModel> FrontendFolders { get; init; }

        public void Show(Window parent)
        {
            var window = new Window
            {
                Owner = parent,
                Content = this,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ContentTemplate = parent.FindResource("SettingsTemplate") as DataTemplate
            };
            window.Show();
            parent.Hide();

            window.Closed += (_, _) =>
            {
                Properties.Settings.Default.Save();
                parent.Show();
            };
        }

        private void AutoAssignFolders()
        {
        }
    }
}