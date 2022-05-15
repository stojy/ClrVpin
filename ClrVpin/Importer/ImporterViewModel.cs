using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using ClrVpin.Controls;
using ClrVpin.Logging;
using ClrVpin.Models.Importer.Vps;
using ClrVpin.Models.Settings;
using ClrVpin.Models.Shared;
using ClrVpin.Models.Shared.Database;
using ClrVpin.Shared;
using PropertyChanged;
using Utils;
using Utils.Extensions;

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
            
            UpdateIsValid();
        }

        public bool IsValid { get; set; }

        //public ObservableCollection<Game> Games { get; set; }
        public ICommand StartCommand { get; }
        public Models.Settings.Settings Settings { get; } = Model.Settings;

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

        private async void Start()
        {
            Logger.Info($"Importer started, settings={JsonSerializer.Serialize(Settings)}");

            _window.Hide();
            Logger.Clear();

            var progress = new ProgressViewModel();
            progress.Show(_window);

            var games = new List<Game>();
            if (MatchFuzzy.IsActive)
            {
                progress.Update("Loading database");
                games = TableUtils.GetGamesFromDatabases(Settings.GetAllContentTypes());
                Logger.Info($"Loading database complete, duration={progress.Duration}", true);
            }

            progress.Update("Fetching online database");
            var onlineGames = await ImporterUtils.GetOnlineDatabase();

            progress.Update("Fixing online database");
            var feedFixStatistics = ImporterUtils.FixOnlineDatabase(onlineGames);
            Logger.Info($"Loading online database complete, duration={progress.Duration}", true);

            progress.Update("Matching online to local database");
            var matchStatistics = await ImporterUtils.MatchOnlineToLocalAsync(games, onlineGames, UpdateProgress);
            Logger.Info($"Matching local and online databases complete, duration={progress.Duration}", true);

            progress.Update("Matching local to online database");
            await ImporterUtils.MatchLocalToOnlineAsync(games, onlineGames, matchStatistics, UpdateProgress);
            Logger.Info($"Matching local and online databases complete, duration={progress.Duration}", true);

            progress.Update("Preparing Results");
            ShowResults(progress.Duration, games, onlineGames, feedFixStatistics, matchStatistics);
            Logger.Info($"Importer rendered, duration={progress.Duration}", true);
            
            progress.Close();

            void UpdateProgress(string detail, float? ratioComplete) => progress.Update(null, ratioComplete, detail);
        }

        private void ShowResults(TimeSpan duration, List<Game> games, List<OnlineGame> onlineGames, Dictionary<string, int> fixStatistics, ImporterMatchStatistics matchStatistics)
        {
            var results = new ImporterResultsViewModel(onlineGames);
            results.Show(_window, WindowMargin, WindowMargin);

            var statistics = new ImporterStatisticsViewModel(games, onlineGames, duration, fixStatistics, matchStatistics);
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
                    _window.Show();
                };
            }
        }

        //private readonly IEnumerable<string> _destinationContentTypes;
        private Window _window;
        private const int WindowMargin = 0;
    }
}