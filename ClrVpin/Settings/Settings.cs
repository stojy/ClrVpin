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

            FrontendFolders = new[]
            {
                // todo; store folder extensions -- stored as comma delimited.. config to present as list?  use json thingy
                new FolderDetail(TableAudio, () => Config.FrontendTableAudioFolder, folder => Config.FrontendTableAudioFolder = folder, "*.mp3, *.wav"),
                new FolderDetail(LaunchAudio, () => Config.FrontendLaunchAudioFolder, folder => Config.FrontendLaunchAudioFolder = folder, "*.mp3, *.wav"),
                new FolderDetail(TableVideos, () => Config.FrontendTableVideosFolder, folder => Config.FrontendTableVideosFolder = folder, "*.f4v, *.mp4"),
                new FolderDetail(BackglassVideos, () => Config.FrontendBackglassVideosFolder, folder => Config.FrontendBackglassVideosFolder = folder, "*.f4v, *.mp4"),
                new FolderDetail(WheelImages, () => Config.FrontendWheelImagesFolder, folder => Config.FrontendWheelImagesFolder = folder, "*.png, *.jpg")

                //new ContentType {Type = "Tables", Extensions = new[] {"*.png"}, GetXxxHits = g => g.WheelImageHits},
                //new ContentType {Type = "Backglass", Extensions = new[] {"*.png"}, GetXxxHits = g => g.WheelImageHits},
                //new ContentType {Type = "Point of View", Extensions = new[] {"*.png"}, GetXxxHits = g => g.WheelImageHits},
            };
        }

        public ICommand TablesFolderCommand { get; }
        public ICommand FrontendFolderCommand { get; }
        public ICommand AutoAssignFoldersCommand { get; }

        public Config Config { get; } = Model.Config;

        public FolderDetail[] FrontendFolders { get; init; }

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

        public const string TableAudio = "Table Audio";
        public const string LaunchAudio = "Launch Audio";
        public const string TableVideos = "Table Videos";
        public const string BackglassVideos = "Backglass Videos";
        public const string WheelImages = "Wheel Images";
    }
}