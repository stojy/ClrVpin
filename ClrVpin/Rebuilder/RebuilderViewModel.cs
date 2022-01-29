using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using ClrVpin.Controls;
using ClrVpin.Controls.FolderSelection;
using ClrVpin.Logging;
using ClrVpin.Models;
using ClrVpin.Models.Rebuilder;
using ClrVpin.Models.Settings;
using ClrVpin.Shared;
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

            IgnoreWordsString = string.Join(", ", Settings.Rebuilder.IgnoreIWords);
            IgnoreWordsChangedCommand = new ActionCommand(IgnoreWordsChanged);

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

        public string IgnoreWordsString { get; set; }
        public ICommand IgnoreWordsChangedCommand { get; set; }

        public FeatureType IgnoreIfNotNewerFeature { get; set; }
        public FeatureType IgnoreIfSmallerFeature { get; set; }
        public FeatureType IgnoreIfContainsWordsFeature { get; set; }
        public FeatureType DeleteIgnoredFilesOptionFeature { get; set; }
        public FeatureType IgnoreSelectClearAllFeature { get; set; }

        public void Show(Window parent)
        {
            _rebuilderWindow = new MaterialWindowEx
            {
                Owner = parent,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SizeToContent = SizeToContent.WidthAndHeight,
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

        private void IgnoreWordsChanged()
        {
            Settings.Rebuilder.IgnoreIWords = IgnoreWordsString == null ? new List<string>() : IgnoreWordsString.Split(",").Select(x => x.Trim().ToLower()).ToList();
        }

        private void UpdateIsValid() => IsValid = !string.IsNullOrEmpty(Settings.Rebuilder.DestinationContentType);

        private void TryUpdateDestinationFolder(string folder)
        {
            // attempt to assign destination folder automatically based on the specified folder
            var contentType = _destinationContentTypes.FirstOrDefault(c => folder.ToLower().EndsWith(c.ToLower()));
            Settings.Rebuilder.DestinationContentType = contentType;

            UpdateIsValid();
        }

        private IEnumerable<FeatureType> CreateMatchCriteriaTypes()
        {
            // show all match criteria types
            // - except for unknown and unsupported which are used 'under the hood' for subsequent reporting
            var featureTypes = StaticSettings.MatchTypes.Where(x => !x.Enum.In(HitTypeEnum.CorrectName, HitTypeEnum.Unknown, HitTypeEnum.Unsupported)).Select(matchType =>
            {
                var featureType = new FeatureType((int) matchType.Enum)
                {
                    Description = matchType.Description,
                    Tip = matchType.Tip,
                    IsSupported = true,
                    IsActive = Settings.Rebuilder.SelectedMatchTypes.Contains(matchType.Enum),
                    SelectedCommand = new ActionCommand(() => Settings.Rebuilder.SelectedMatchTypes.Toggle(matchType.Enum)),
                    IsHighlighted = matchType.IsHighlighted,
                    IsHelpSupported = matchType.HelpUrl != null,
                    HelpAction = new ActionCommand(() => Process.Start(new ProcessStartInfo(matchType.HelpUrl) {UseShellExecute = true}))
                };

                return featureType;
            }).ToList();

            return featureTypes.Concat(new[] { FeatureType.CreateSelectAll(featureTypes) });
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
            }).ToList();

            IgnoreIfContainsWordsFeature = featureTypes.First(x => x.Id == (int) IgnoreOptionEnum.IgnoreIfContainsWords);
            IgnoreIfSmallerFeature = featureTypes.First(x => x.Id == (int) IgnoreOptionEnum.IgnoreIfSmaller);
            IgnoreIfNotNewerFeature = featureTypes.First(x => x.Id == (int) IgnoreOptionEnum.IgnoreIfNotNewer);
            
            // delete ignored isn't technically an ignored option.. but added here to keep it consistent visually
            DeleteIgnoredFilesOptionFeature = CreateDeleteIgnoredFilesOption();
            featureTypes.Add(DeleteIgnoredFilesOptionFeature);

            IgnoreSelectClearAllFeature = FeatureType.CreateSelectAll(featureTypes);

            return featureTypes;
        }

        public FeatureType CreateDeleteIgnoredFilesOption()
        {
            var feature = new FeatureType(-1)
            {
                Description = StaticSettings.DeleteIgnoredFilesOption.Description,
                Tip = StaticSettings.DeleteIgnoredFilesOption.Tip,
                IsSupported = true,
                IsActive = Settings.Rebuilder.DeleteIgnoredFiles,
                SelectedCommand = new ActionCommand(() => Settings.Rebuilder.DeleteIgnoredFiles = !Settings.Rebuilder.DeleteIgnoredFiles)
            };

            return feature;
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
            }).ToList();

            return featureTypes.Concat(new[] { FeatureType.CreateSelectAll(featureTypes) });
        }

        private async void Start()
        {
            Logger.Info($"\nRebuilder started, settings={JsonSerializer.Serialize(Settings)}");

            _rebuilderWindow.Hide();
            Logger.Clear();

            var progress = new ProgressViewModel();
            progress.Show(_rebuilderWindow);

            progress.Update("Loading Database");
            var games = TableUtils.GetGamesFromDatabases(new List<ContentType> {Settings.GetSelectedDestinationContentType()});

            progress.Update("Checking Files");
            var unmatchedFiles = await RebuilderUtils.CheckAsync(games, UpdateProgress);

            progress.Update("Merging Files");
            var gameFiles = await RebuilderUtils.MergeAsync(games, Settings.BackupFolder, UpdateProgress);

            progress.Update("Removing Unmatched Ignored Files");
            await RebuilderUtils.RemoveUnmatchedIgnoredAsync(unmatchedFiles, UpdateProgress);

            progress.Update("Preparing Results");
            await Task.Delay(1);
            Games = new ObservableCollection<Game>(games);

            ShowResults(gameFiles, unmatchedFiles, progress.Duration);

            progress.Close();

            void UpdateProgress(string detail, int percentage) => progress.Update(null, percentage, detail);
        }

        private void ShowResults(ICollection<FileDetail> gameFiles, ICollection<FileDetail> unmatchedFiles, TimeSpan duration)
        {
            var rebuilderStatistics = new RebuilderStatisticsViewModel(Games, duration, gameFiles, unmatchedFiles);
            rebuilderStatistics.Show(_rebuilderWindow, WindowMargin, WindowMargin);

            var rebuilderResults = new RebuilderResultsViewModel(Games);
            rebuilderResults.Show(_rebuilderWindow, rebuilderStatistics.Window.Left + rebuilderStatistics.Window.Width + WindowMargin, WindowMargin);

            var logging = new LoggingViewModel();
            logging.Show(_rebuilderWindow, rebuilderResults.Window.Left, rebuilderResults.Window.Top + rebuilderResults.Window.Height + WindowMargin);

            logging.Window.Closed += CloseWindows();
            rebuilderResults.Window.Closed += CloseWindows();
            rebuilderStatistics.Window.Closed += CloseWindows();

            EventHandler CloseWindows()
            {
                return (_, _) =>
                {
                    rebuilderResults.Close();
                    rebuilderStatistics.Window.Close();
                    logging.Close();
                    _rebuilderWindow.Show();
                };
            }
        }


        private readonly IEnumerable<string> _destinationContentTypes;
        private Window _rebuilderWindow;

        private const int WindowMargin = 0;
    }
}