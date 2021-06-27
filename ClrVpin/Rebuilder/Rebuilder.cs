using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using ClrVpin.Controls.FolderSelection;
using ClrVpin.Models;
using ClrVpin.Scanner;
using ClrVpin.Shared;
using MaterialDesignExtensions.Controls;
using PropertyChanged;
using Utils;

namespace ClrVpin.Rebuilder
{
    [AddINotifyPropertyChangedInterface]
    public class Rebuilder
    {
        public Rebuilder()
        {
            StartCommand = new ActionCommand(Start);

            MatchCriteriaTypesView = new ListCollectionView(CreateMatchCriteriaTypes().ToList());
            MergeCriteriaTypesView = new ListCollectionView(CreateMergeOptions().ToList());

            SourceFolderModel = new FolderTypeModel("Source", Model.Config.SourceFolder, folder =>
            {
                Model.Config.SourceFolder = folder;
                TryUpdateDestinationFolder(folder);
            });

            _destinationContentTypes = Model.Config.GetFrontendFolders().Where(x => !x.IsDatabase).Select(x => x.Description);
            DestinationContentTypes = new ObservableCollection<string>(_destinationContentTypes);

            Config = Model.Config;

            UpdateIsValid();
        }

        public bool IsValid { get; set; }

        public ListCollectionView MatchCriteriaTypesView { get; set; }
        public ListCollectionView MergeCriteriaTypesView { get; set; }

        public FolderTypeModel SourceFolderModel { get; set; }
        public ObservableCollection<string> DestinationContentTypes { get; set; }

        public ObservableCollection<Game> Games { get; set; }
        public ICommand StartCommand { get; set; }
        public Config Config { get; set; }

        public void Show(Window parent)
        {
            _rebuilderWindow = new MaterialWindow
            {
                Owner = parent,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                //SizeToContent = SizeToContent.WidthAndHeight,
                Width = 650,
                Height = 350,
                Content = this,
                Resources = parent.Resources,
                ContentTemplate = parent.FindResource("RebuilderTemplate") as DataTemplate,
                ResizeMode = ResizeMode.NoResize,
                Title = "Rebuilder"
            };

            _rebuilderWindow.Show();
            parent.Hide();

            _rebuilderWindow.Closed += (_, _) =>
            {
                Model.Config.Save();
                parent.Show();
            };
        }

        private void UpdateIsValid() => IsValid = !string.IsNullOrEmpty(Config.DestinationContentType);

        private void TryUpdateDestinationFolder(string folder)
        {
            // attempt to assign destination folder automatically based on the specified folder
            var contentType = _destinationContentTypes.FirstOrDefault(folder.EndsWith);
            if (contentType != null)
                Config.DestinationContentType = contentType;
            UpdateIsValid();
        }

        private static IEnumerable<FeatureType> CreateMatchCriteriaTypes()
        {
            // show all match criteria types
            var matchTypes = Config.MatchTypes.Select(matchType =>
            {
                var featureType = new FeatureType
                {
                    Description = matchType.Description,
                    Tip = matchType.Tip,
                    IsSupported = true,
                    IsActive = Model.Config.SelectedMatchTypes.Contains(matchType.Enum),
                    SelectedCommand = new ActionCommand(() => Model.Config.SelectedMatchTypes.Toggle(matchType.Enum))
                };

                return featureType;
            });

            return matchTypes.ToList();
        }

        private IEnumerable<FeatureType> CreateMergeOptions()
        {
            // show all merge options
            var featureTypes = Config.MergeOptions.Select(mergeOption =>
            {
                var featureType = new FeatureType
                {
                    Description = mergeOption.Description,
                    Tip = mergeOption.Tip,
                    IsSupported = true,
                    IsActive = Model.Config.SelectedMergeOptions.Contains(mergeOption.Enum),
                    SelectedCommand = new ActionCommand(() => Model.Config.SelectedMergeOptions.Toggle(mergeOption.Enum))
                };

                return featureType;
            });

            return featureTypes.ToList();
        }

        private async void Start()
        {
            _rebuilderWindow.Hide();
            Logging.Logger.Clear();

            var progress = new Progress();
            progress.Show(_rebuilderWindow);

            progress.Update("Loading Database", 0);
            var games = TableUtils.GetGamesFromDatabases();

            progress.Update("Checking Files", 30);
            var otherFiles = RebuilderUtils.Check(games);

            progress.Update("Merging Files", 60);
            var mergedFiles = await RebuilderUtils.MergeAsync(games, otherFiles, Model.Config.BackupFolder);

            progress.Update("Preparing Results", 100);
            await Task.Delay(10);
            Games = new ObservableCollection<Game>(games);
            //ShowResults(fixFiles.Concat(unknownFiles).ToList(), progress.Duration);
            ShowResults(otherFiles.ToList(), progress.Duration);

            progress.Close();
        }

        private void ShowResults(ICollection<FixFileDetail> fixFiles, TimeSpan duration)
        {
            var rebuilderResults = new RebuilderResults(Games);
            rebuilderResults.Show(_rebuilderWindow, 5, 5);

            var rebuilderStatistics = new RebuilderStatistics(Games, duration, fixFiles);
            rebuilderStatistics.Show(_rebuilderWindow, 5, rebuilderResults.Window.Height + WindowMargin, rebuilderResults.Window.Width);

            _loggingWindow = new Logging.Logging();
            _loggingWindow.Show(_rebuilderWindow, rebuilderResults.Window.Width + WindowMargin, 5, rebuilderResults.Window.Height);

            rebuilderResults.Window.Closed += (_, _) =>
            {
                rebuilderStatistics.Close();
                _loggingWindow.Close();
                _rebuilderWindow.Show();
            };
        }

        private readonly IEnumerable<string> _destinationContentTypes;

        private Window _rebuilderWindow;
        private Logging.Logging _loggingWindow;
        private const int WindowMargin = 12;
    }
}