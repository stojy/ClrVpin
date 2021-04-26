using System;
using System.Windows;
using System.Windows.Input;
using ClrVpin.Models;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.Xaml.Behaviors.Core;
using PropertyChanged;

namespace ClrVpin.Settings
{
    [AddINotifyPropertyChangedInterface]
    public class Settings
    {
        public Settings()
        {
            FrontendFolderCommand = new ActionCommand(() => GetFolder(Config.FrontendFolder, folder => Config.FrontendFolder = folder));
            TablesFolderCommand = new ActionCommand(() => GetFolder(Config.TableFolder, folder => Config.TableFolder = folder));
        }

        public ICommand TablesFolderCommand { get; set; }
        public ICommand FrontendFolderCommand { get; }
        public Config Config { get; } = Model.Config;

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

        private static void GetFolder(string folder, Action<string> updateAction)
        {
            var openFileDialog = new CommonOpenFileDialog
            {
                InitialDirectory = folder,
                DefaultDirectory = "c:\\",
                EnsurePathExists = true,
                Title = $"Select folder: {folder}",
                IsFolderPicker = true
            };
            if (openFileDialog.ShowDialog() == CommonFileDialogResult.Ok)
                updateAction(openFileDialog.FileName);
        }
    }
}