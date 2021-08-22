using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
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
            DestinationContentTypeSelectedCommand = new ActionCommand(UpdateIsValid);

            MatchCriteriaTypesView = new ListCollectionView(CreateMatchCriteriaTypes().ToList());
            IgnoreOptionsTypesView = new ListCollectionView(CreateIgnoreOptions().ToList());
            MergeOptionsTypesView = new ListCollectionView(CreateMergeOptions().ToList());

            SourceFolderModel = new FolderTypeModel("Source", Settings.Rebuilder.SourceFolder, folder =>
            {
                Settings.Rebuilder.SourceFolder = folder;
                TryUpdateDestinationFolder(folder);
            });

            _destinationContentTypes = Model.Settings.GetFixableContentTypes().Select(x => x.Description);
            DestinationContentTypes = new ObservableCollection<string>(_destinationContentTypes);

            UpdateIsValid();
        }


        public bool IsValid { get; set; }

        public ListCollectionView MatchCriteriaTypesView { get; set; }
        public ListCollectionView IgnoreOptionsTypesView { get; set; }
        public ListCollectionView MergeOptionsTypesView { get; set; }

        public FolderTypeModel SourceFolderModel { get; set; }
        public ObservableCollection<string> DestinationContentTypes { get; set; }

        public ICommand DestinationContentTypeSelectedCommand { get; set; }
        public ObservableCollection<Game> Games { get; set; }
        public ICommand StartCommand { get; set; }
        public Models.Settings.Settings Settings { get; } = Model.Settings;


        public void Show(Window parent)
        {
            _rebuilderWindow = new MaterialWindow
            {
                Owner = parent,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                //SizeToContent = SizeToContent.WidthAndHeight,
                Width = 680,
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
                Model.SettingsManager.Write();
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
            var matchTypes = StaticSettings.MatchTypes.Where(x => !x.Enum.In(HitTypeEnum.CorrectName, HitTypeEnum.Unknown, HitTypeEnum.Unsupported)).Select(matchType =>
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
            var featureTypes = StaticSettings.IgnoreOptions.Select(ignoreOption =>
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
            var featureTypes = StaticSettings.MergeOptions.Select(mergeOption =>
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
            Logger.Info($"\nRebuilder started, settings={JsonSerializer.Serialize(Settings)}");

            _rebuilderWindow.Hide();
            Logger.Clear();

            var progress = new ProgressViewModel();
            progress.Show(_rebuilderWindow);
            
            progress.Update("Loading Database", 0);
            var games = TableUtils.GetGamesFromDatabases(new List<ContentType> {Settings.GetSelectedDestinationContentType()});

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