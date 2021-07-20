﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using ClrVpin.Controls.FolderSelection;
using ClrVpin.Logging;
using ClrVpin.Models;
using ClrVpin.Models.Settings;
using ClrVpin.Shared;
using MaterialDesignExtensions.Controls;
using PropertyChanged;
using Utils;

namespace ClrVpin.Rebuilder
{
    [AddINotifyPropertyChangedInterface]
    public class RebuilderViewModel
    {
        public RebuilderViewModel()
        {
            StartCommand = new ActionCommand(Start);

            MatchCriteriaTypesView = new ListCollectionView(CreateMatchCriteriaTypes().ToList());
            IgnoreOptionsTypesView = new ListCollectionView(CreateIgnoreOptions().ToList());
            MergeOptionsTypesView = new ListCollectionView(CreateMergeOptions().ToList());

            SourceFolderModel = new FolderTypeModel("Source", Settings.Rebuilder.SourceFolder, folder =>
            {
                Settings.Rebuilder.SourceFolder = folder;
                TryUpdateDestinationFolder(folder);
            });

            _destinationContentTypes = SettingsManager.Settings.FrontendFolders.Where(x => !x.IsDatabase).Select(x => x.Description);
            DestinationContentTypes = new ObservableCollection<string>(_destinationContentTypes);

            UpdateIsValid();
        }


        public bool IsValid { get; set; }

        public ListCollectionView MatchCriteriaTypesView { get; set; }
        public ListCollectionView IgnoreOptionsTypesView { get; set; }
        public ListCollectionView MergeOptionsTypesView { get; set; }

        public FolderTypeModel SourceFolderModel { get; set; }
        public ObservableCollection<string> DestinationContentTypes { get; set; }

        public ObservableCollection<Game> Games { get; set; }
        public ICommand StartCommand { get; set; }
        public Config Config { get; } = Model.Config;
        public Models.Settings.Settings Settings { get; } = SettingsManager.Settings;

        public void Show(Window parent)
        {
            _rebuilderWindow = new MaterialWindow
            {
                Owner = parent,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                //SizeToContent = SizeToContent.WidthAndHeight,
                Width = 660,
                Height = 425,
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

        private void UpdateIsValid() => IsValid = !string.IsNullOrEmpty(Settings.Rebuilder.DestinationContentType);

        private void TryUpdateDestinationFolder(string folder)
        {
            // attempt to assign destination folder automatically based on the specified folder
            var contentType = _destinationContentTypes.FirstOrDefault(folder.EndsWith);
            Settings.Rebuilder.DestinationContentType = contentType;
            
            UpdateIsValid();
        }

        private IEnumerable<FeatureType> CreateMatchCriteriaTypes()
        {
            // show all match criteria types
            // - except for unknown and unsupported which are used 'under the hood' for subsequent reporting
            var matchTypes = Config.MatchTypes.Where(x => !x.Enum.In(HitTypeEnum.Valid, HitTypeEnum.Unknown, HitTypeEnum.Unsupported)).Select(matchType =>
            {
                var featureType = new FeatureType((int) matchType.Enum)
                {
                    Description = matchType.Description,
                    Tip = matchType.Tip,
                    IsSupported = true,
                    IsActive = Settings.Rebuilder.SelectedMatchTypes.Contains(matchType.Enum),
                    SelectedCommand = new ActionCommand(() => Settings.Rebuilder.SelectedMatchTypes.Toggle(matchType.Enum))
                };

                return featureType;
            });

            return matchTypes.ToList();
        }

        private IEnumerable<FeatureType> CreateIgnoreOptions()
        {
            // show all merge options
            var featureTypes = Config.IgnoreOptions.Select(ignoreOption =>
            {
                var featureType = new FeatureType((int) ignoreOption.Enum)
                {
                    Description = ignoreOption.Description,
                    Tip = ignoreOption.Tip,
                    IsSupported = true,
                    IsActive = Settings.Rebuilder.SelectedIgnoreOptions.Contains(ignoreOption.Enum),
                    SelectedCommand = new ActionCommand(() => Settings.Rebuilder.SelectedIgnoreOptions.Toggle(ignoreOption.Enum))
                };

                return featureType;
            });

            return featureTypes.ToList();
        }
        
        private IEnumerable<FeatureType> CreateMergeOptions()
        {
            // show all merge options
            var featureTypes = Config.MergeOptions.Select(mergeOption =>
            {
                var featureType = new FeatureType((int) mergeOption.Enum)
                {
                    Description = mergeOption.Description,
                    Tip = mergeOption.Tip,
                    IsSupported = true,
                    IsActive = Settings.Rebuilder.SelectedMergeOptions.Contains(mergeOption.Enum),
                    SelectedCommand = new ActionCommand(() => Settings.Rebuilder.SelectedMergeOptions.Toggle(mergeOption.Enum))
                };

                return featureType;
            });

            return featureTypes.ToList();
        }

        private async void Start()
        {
            _rebuilderWindow.Hide();
            Logger.Clear();

            var progress = new ProgressViewModel();
            progress.Show(_rebuilderWindow);

            progress.Update("Loading Database", 0);
            var games = TableUtils.GetGamesFromDatabases();

            progress.Update("Checking Files", 30);
            var unknownFiles = RebuilderUtils.Check(games);

            progress.Update("Merging Files", 60);
            var gameFiles = await RebuilderUtils.MergeAsync(games, Settings.BackupFolder);

            // unlike scanner, unknownFiles (unsupported and unknown) are deliberately NOT removed

            progress.Update("Preparing Results", 100);
            await Task.Delay(10);
            Games = new ObservableCollection<Game>(games);

            ShowResults(gameFiles, unknownFiles, progress.Duration);

            progress.Close();
        }

        private void ShowResults(ICollection<FileDetail> gameFiles, ICollection<FileDetail> unknownFiles, TimeSpan duration)
        {
            var rebuilderStatistics = new RebuilderStatisticsViewModel(Games, duration, gameFiles, unknownFiles);
            rebuilderStatistics.Show(_rebuilderWindow, WindowMargin, WindowMargin);

            var rebuilderResults = new RebuilderResultsViewModel(Games);
            rebuilderResults.Show(_rebuilderWindow, rebuilderStatistics.Window.Left + rebuilderStatistics.Window.Width + WindowMargin, WindowMargin);

            _loggingWindow = new Logging.Logging();
            _loggingWindow.Show(_rebuilderWindow, rebuilderResults.Window.Left, rebuilderResults.Window.Top + rebuilderResults.Window.Height + WindowMargin);

            rebuilderStatistics.Window.Closed += (_, _) =>
            {
                rebuilderResults.Close();
                _loggingWindow.Close();
                _rebuilderWindow.Show();
            };
        }

        private readonly IEnumerable<string> _destinationContentTypes;
        private Window _rebuilderWindow;
        private Logging.Logging _loggingWindow;

        private const int WindowMargin = 5;
    }
}