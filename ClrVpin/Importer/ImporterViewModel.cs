using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using ClrVpin.Controls;
using ClrVpin.Logging;
using ClrVpin.Models;
using ClrVpin.Models.Settings;
using ClrVpin.Models.Shared;
using ClrVpin.Shared;
using PropertyChanged;
using Utils;
using Utils.Extensions;
using Game = ClrVpin.Models.Importer.Vps.Game;

namespace ClrVpin.Importer
{
    [AddINotifyPropertyChangedInterface]
    public class ImporterViewModel
    {
        public ImporterViewModel()
        {
            StartCommand = new ActionCommand(Start);

            //DestinationContentTypeSelectedCommand = new ActionCommand(UpdateIsValid);

            CreateMatchCriteriaTypes();
            
            CreateIgnoreCriteria();

            IgnoreWordsString = string.Join(", ", Settings.Importer.IgnoreIWords);
            IgnoreWordsChangedCommand = new ActionCommand(IgnoreWordsChanged);

            UpdateIsValid();
        }

        public bool IsValid { get; set; }

        //public ObservableCollection<Game> Games { get; set; }
        public ICommand StartCommand { get; }
        public Models.Settings.Settings Settings { get; } = Model.Settings;

        public string IgnoreWordsString { get; set; }
        public ICommand IgnoreWordsChangedCommand { get; set; }

        //public FeatureType IgnoreIfNotNewerFeature { get; set; }
        //public FeatureType IgnoreIfSmallerFeature { get; set; }
        public FeatureType IgnoreIfContainsWordsFeature { get; set; }
        //public FeatureType DeleteIgnoredFilesOptionFeature { get; set; }
        //public FeatureType IgnoreSelectClearAllFeature { get; set; }

        public void Show(Window parent)
        {
            _window = new MaterialWindowEx
            {
                Owner = parent,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SizeToContent = SizeToContent.WidthAndHeight,
                Content = this,
                Resources = parent.Resources,
                ContentTemplate = parent.FindResource("ImporterTemplate") as DataTemplate,
                ResizeMode = ResizeMode.NoResize,
                Title = "Importer"
            };

            _window.Show();
            parent.Hide();

            _window.Closed += (_, _) =>
            {
                Model.SettingsManager.Write();
                parent.Show();
            };
        }

        private void IgnoreWordsChanged()
        {
            Settings.Importer.IgnoreIWords = IgnoreWordsString == null ? new List<string>() : IgnoreWordsString.Split(",").Select(x => x.Trim().ToLower()).ToList();
        }

        private void UpdateIsValid() => IsValid = true;

        private void CreateMatchCriteriaTypes()
        {
            // show all match criteria types
            // - only fuzzy is supported, but using a list for consistency with scanner and rebuilder
            var featureTypes = StaticSettings.MatchTypes.Where(x => x.Enum.In(HitTypeEnum.Fuzzy)).Select(matchType =>
            {
                var featureType = new FeatureType((int)matchType.Enum)
                {
                    Description = matchType.Description,
                    Tip = matchType.Tip,
                    IsSupported = true,
                    IsActive = Settings.Importer.SelectedMatchTypes.Contains(matchType.Enum),
                    SelectedCommand = new ActionCommand(() => Settings.Importer.SelectedMatchTypes.Toggle(matchType.Enum)),
                    IsHighlighted = matchType.IsHighlighted,
                    IsHelpSupported = matchType.HelpUrl != null,
                    HelpAction = new ActionCommand(() => Process.Start(new ProcessStartInfo(matchType.HelpUrl) { UseShellExecute = true }))
                };

                return featureType;
            }).ToList();

            // create separate property for each so they can be referenced individually in the UI
            MatchFuzzy = featureTypes.First(x => x.Id == (int)HitTypeEnum.Fuzzy);
        }

        public FeatureType MatchFuzzy { get; private set; }

        private void CreateIgnoreCriteria()
        {
            // show all ignore criteria
            // - only ignore words is supported, but using a list for consistency with scanner and rebuilder
            var featureTypes = StaticSettings.IgnoreCriteria.Where(x => x.Enum.In(IgnoreCriteriaEnum.IgnoreIfContainsWords)).Select(criteria =>
            {
                var featureType = new FeatureType((int)criteria.Enum)
                {
                    Description = criteria.Description,
                    Tip = criteria.Tip,
                    IsSupported = true,
                    IsActive = Settings.Importer.SelectedIgnoreCriteria.Contains(criteria.Enum),
                    SelectedCommand = new ActionCommand(() => Settings.Importer.SelectedIgnoreCriteria.Toggle(criteria.Enum))
                };

                return featureType;
            }).ToList();

            IgnoreIfContainsWordsFeature = featureTypes.First(x => x.Id == (int)IgnoreCriteriaEnum.IgnoreIfContainsWords);
        }

        private async void Start()
        {
            Logger.Info($"Importer started, settings={JsonSerializer.Serialize(Settings)}");

            _window.Hide();
            Logger.Clear();

            var progress = new ProgressViewModel();
            progress.Show(_window);

            progress.Update("Fetching online DB");
            var games = await ImporterUtils.GetOnlineDatabase();

            progress.Update("Updating online DB");
            var feedFixStatistics = ImporterUtils.Update(games);

            Logger.Info($"Loading online DB complete, duration={progress.Duration}", true);

            //var unmatchedFiles = await RebuilderUtils.CheckAsync(games, UpdateProgress);

            //progress.Update("Merging Files");
            //var gameFiles = await RebuilderUtils.MergeAsync(games, Settings.BackupFolder, UpdateProgress);

            //progress.Update("Removing Unmatched Ignored Files");
            //await RebuilderUtils.RemoveUnmatchedIgnoredAsync(unmatchedFiles, UpdateProgress);

            //progress.Update("Preparing Results");
            //await Task.Delay(1);
            //Games = new ObservableCollection<Game>(games);

            
            ShowResults(progress.Duration, games, feedFixStatistics);
            Logger.Info($"Importer rendered, duration={progress.Duration}", true);
            
            progress.Close();

            //void UpdateProgress(string detail, int percentage) => progress.Update(null, percentage, detail);
        }

        private void ShowResults(TimeSpan duration, List<Game> games, Dictionary<string, int> feedFixStatistics)
        {
            var results = new ImporterResultsViewModel(games);
            results.Show(_window, WindowMargin, WindowMargin);

            var statistics = new ImporterStatisticsViewModel(duration, feedFixStatistics);
            statistics.Show(_window, WindowMargin, results.Window.Top + results.Window.Height + WindowMargin);

            var logging = new LoggingViewModel();
            logging.Show(_window, statistics.Window.Left + statistics.Window.Width + WindowMargin, results.Window.Top + results.Window.Height + WindowMargin);

            logging.Window.Closed += CloseWindows();
            results.Window.Closed += CloseWindows();
            statistics.Window.Closed += CloseWindows();

            EventHandler CloseWindows()
            {
                return (_, _) =>
                {
                    results.Close();
                    statistics.Close();
                    logging.Close();
                    //_window.Show();
                    _window.Close();
                };
            }
        }

        //private readonly IEnumerable<string> _destinationContentTypes;
        private Window _window;
        private const int WindowMargin = 0;
    }
}