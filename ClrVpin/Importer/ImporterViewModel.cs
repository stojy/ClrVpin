using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using ClrVpin.Controls;
using ClrVpin.Logging;
using ClrVpin.Models.Importer;
using ClrVpin.Models.Importer.Vps;
using ClrVpin.Models.Settings;
using ClrVpin.Models.Shared;
using ClrVpin.Models.Shared.Game;
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

            CreateMatchCriteriaTypes();

            FeedFixOptionsView = new ListCollectionView(CreateFeedFixOptions().ToList());

            UpdateIsValid();
        }

        public bool IsValid { get; set; }

        public ICommand StartCommand { get; }
        public Models.Settings.Settings Settings { get; } = Model.Settings;

        public ListCollectionView FeedFixOptionsView { get; }

        public FeatureType MatchFuzzy { get; private set; }

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
                    IsActive = Settings.Importer.SelectedMatchCriteriaOptions.Contains(matchType.Enum),
                    SelectedCommand = new ActionCommand(() => Settings.Importer.SelectedMatchCriteriaOptions.Toggle(matchType.Enum)),
                    IsHighlighted = matchType.IsHighlighted,
                    IsHelpSupported = matchType.HelpUrl != null,
                    HelpAction = new ActionCommand(() => Process.Start(new ProcessStartInfo(matchType.HelpUrl) { UseShellExecute = true }))
                };

                return featureType;
            }).ToList();

            // create separate property for each so they can be referenced individually in the UI
            MatchFuzzy = featureTypes.First(x => x.Id == (int)HitTypeEnum.Fuzzy);

            // explicitly disable fuzzy logic if ALL of the settings are not fully configured, e.g. frontend database folder not setup
            if (!Model.SettingsManager.IsValid)
            {
                MatchFuzzy.IsActive = false;
                MatchFuzzy.IsSupported = false;
                MatchFuzzy.Tip += "... DISABLED BECAUSE THE SETTINGS ARE INCOMPLETE";
            }
        }

        private IEnumerable<FeatureType> CreateFeedFixOptions()
        {
            // show all feed fix options
            var feedFixOptions = StaticSettings.FixFeedOptions.Select(feedFix =>
            {
                var featureType = new FeatureType((int)feedFix.Enum)
                {
                    Description = feedFix.Description,
                    Tip = feedFix.Tip,
                    IsSupported = true,
                    IsActive = Settings.Importer.SelectedFeedFixOptions.Contains(feedFix.Enum),
                    SelectedCommand = new ActionCommand(() => FixFeedOptionSelected(feedFix.Enum))
                };
                return featureType;
            }).ToList();

            _feedFixDuplicateTableOption = feedFixOptions.First(x => x.Id == (int)FixFeedOptionEnum.DuplicateTable);

            return feedFixOptions.Concat(new[] { FeatureType.CreateSelectAll(feedFixOptions) });
        }

        private void FixFeedOptionSelected(FixFeedOptionEnum fixFeedOption)
        {
            Settings.Importer.SelectedFeedFixOptions.Toggle(fixFeedOption);

            // disable 'duplicate table' option if the prerequisite fix options aren't enabled
            if (!Settings.Importer.SelectedFeedFixOptions.ContainsAll(
                    FixFeedOptionEnum.Whitespace,
                    FixFeedOptionEnum.ManufacturedIncludesAuthor,
                    FixFeedOptionEnum.OriginalTableIncludesIpdbUrl,
                    FixFeedOptionEnum.InvalidUrlIpdb,
                    FixFeedOptionEnum.UpgradeUrlHttps,
                    FixFeedOptionEnum.WrongManufacturerYear,
                    FixFeedOptionEnum.WrongName,
                    FixFeedOptionEnum.WrongUrlIpdb))
            {
                _feedFixDuplicateTableOption.IsActive = false;
                _feedFixDuplicateTableOption.IsSupported = false;
                Settings.Importer.SelectedFeedFixOptions.Remove(FixFeedOptionEnum.DuplicateTable);
            }
            else
            {
                _feedFixDuplicateTableOption.IsSupported = true;
            }
        }

        private async void Start()
        {
            Logger.Info($"Importer started, settings={JsonSerializer.Serialize(Settings)}");

            _window.Hide();
            Logger.Clear();

            var progress = new ProgressViewModel();
            progress.Show(_window);

            var localGames = new List<GameDetail>();
            if (MatchFuzzy.IsActive)
            {
                try
                {
                    progress.Update("Loading database");
                    localGames = await TableUtils.ReadGamesFromDatabases(Settings.GetFixableContentTypes());
                    Logger.Info($"Loading database complete, duration={progress.Duration}", true);
                }
                catch (Exception)
                {
                    progress.Close();
                    _window.Show();
                    return;
                }
            }

            progress.Update("Fetching online database");
            var onlineGames = await ImporterUtils.ReadGamesFromOnlineDatabase();

            progress.Update("Fixing online database");
            var feedFixStatistics = ImporterFix.FixOnlineDatabase(onlineGames);
            Logger.Info($"Loading online database complete, duration={progress.Duration}", true);

            progress.Update("Matching online to local database");
            var matchStatistics = await ImporterUtils.MatchOnlineToLocalAsync(localGames, onlineGames, UpdateProgress);
            Logger.Info($"Matching local and online databases complete, duration={progress.Duration}", true);

            progress.Update("Matching local to online database");
            await ImporterUtils.MatchLocalToOnlineAsync(localGames, onlineGames, matchStatistics, UpdateProgress);
            Logger.Info($"Matching local and online databases complete, duration={progress.Duration}", true);

            progress.Close();

            progress.Update("Preparing Results");
            await ShowResults(progress.Duration, localGames, onlineGames, feedFixStatistics, matchStatistics);
            Logger.Info($"Importer rendered, duration={progress.Duration}", true);

            void UpdateProgress(string detail, float? ratioComplete) => progress.Update(null, ratioComplete, detail);
        }

        private async Task ShowResults(TimeSpan duration, List<GameDetail> games, List<OnlineGame> onlineGames, Dictionary<string, int> fixStatistics, ImporterMatchStatistics matchStatistics)
        {
            var results = new ImporterResultsViewModel(games, onlineGames, matchStatistics);
            var showTask = results.Show(_window, WindowMargin, WindowMargin);

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

            await showTask;
        }

        //private readonly IEnumerable<string> _destinationContentTypes;
        private Window _window;
        private FeatureType _feedFixDuplicateTableOption;
        private const int WindowMargin = 0;
    }
}