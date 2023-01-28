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
using ClrVpin.Extensions;
using ClrVpin.Logging;
using ClrVpin.Models.Feeder;
using ClrVpin.Models.Settings;
using ClrVpin.Models.Shared;
using ClrVpin.Models.Shared.Game;
using ClrVpin.Shared;
using ClrVpin.Shared.FeatureType;
using PropertyChanged;
using Utils;
using Utils.Extensions;

namespace ClrVpin.Feeder;

[AddINotifyPropertyChangedInterface]
public class FeederViewModel : IShowViewModel
{
    public FeederViewModel()
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

    public Window Show(Window parent)
    {
        _window = new MaterialWindowEx
        {
            Owner = parent,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            SizeToContent = SizeToContent.WidthAndHeight,
            Content = this,
            Resources = parent.Resources,
            ContentTemplate = parent.FindResource("FeederTemplate") as DataTemplate,
            ResizeMode = ResizeMode.NoResize,
            Title = "Feeder"
        };

        _window.Show();
        _window.Closed += (_, _) => Model.SettingsManager.Write();

        return _window;
    }

    private void UpdateIsValid() => IsValid = true;

    private void CreateMatchCriteriaTypes()
    {
        // show all match criteria types
        // - only fuzzy is supported, but using a list for consistency with cleaner and merger
        var featureTypes = StaticSettings.MatchTypes.Where(x => x.Enum.In(HitTypeEnum.Fuzzy)).Select(matchType =>
        {
            var featureType = new FeatureType((int)matchType.Enum)
            {
                Description = matchType.Description,
                Tip = matchType.Tip,
                IsSupported = true,
                IsActive = Settings.Feeder.SelectedMatchCriteriaOptions.Contains(matchType.Enum),
                SelectedCommand = new ActionCommand(() => Settings.Feeder.SelectedMatchCriteriaOptions.Toggle(matchType.Enum)),
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
            // clear the settings to disable matching
            Settings.Feeder.SelectedMatchCriteriaOptions.Clear();

            // disable the UI so it can't be selected
            MatchFuzzy.IsActive = false;
            MatchFuzzy.IsSupported = false;
            MatchFuzzy.Tip += Model.OptionsDisabledMessage;
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
                IsActive = Settings.Feeder.SelectedFeedFixOptions.Contains(feedFix.Enum),
                SelectedCommand = new ActionCommand(() => FixFeedOptionSelected(feedFix.Enum))
            };
            return featureType;
        }).ToList();

        _feedFixDuplicateTableOption = feedFixOptions.First(x => x.Id == (int)FixFeedOptionEnum.DuplicateTable);

        return feedFixOptions.Concat(new[] { FeatureType.CreateSelectAll(feedFixOptions) });
    }

    private void FixFeedOptionSelected(FixFeedOptionEnum fixFeedOption)
    {
        Settings.Feeder.SelectedFeedFixOptions.Toggle(fixFeedOption);

        // disable 'duplicate table' option if the prerequisite fix options aren't enabled
        if (!Settings.Feeder.SelectedFeedFixOptions.ContainsAll(
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
            Settings.Feeder.SelectedFeedFixOptions.Remove(FixFeedOptionEnum.DuplicateTable);
        }
        else
        {
            _feedFixDuplicateTableOption.IsSupported = true;
        }
    }

    private async void Start()
    {
        Logger.Info($"Feeder started, settings={JsonSerializer.Serialize(Settings)}");

        _window.Hide();
        Logger.Clear();

        var progress = new ProgressViewModel();
        progress.Show(_window);

        var localGames = new List<LocalGame>();
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
                _window.TryShow();
                return;
            }
        }

        progress.Update("Fetching online database");
        var onlineGames = await FeederUtils.ReadGamesFromOnlineDatabase();

        progress.Update("Fixing online database");
        var feedFixStatistics = FeederFix.FixOnlineDatabase(onlineGames);
        Logger.Info($"Loading online database complete, duration={progress.Duration}", true);

        progress.Update("Matching online to local database");
        await FeederUtils.MatchOnlineToLocalAsync(localGames, onlineGames, UpdateProgress);
        Logger.Info($"Matching local and online databases complete, duration={progress.Duration}", true);

        progress.Update("Matching local to online database");
        var gameItems = await FeederUtils.MergeOnlineAndLocalGamesAsync(localGames, onlineGames, UpdateProgress);
        Logger.Info($"Matching local and online databases complete, duration={progress.Duration}", true);

        progress.Update("Preparing Results");

        progress.Close();

        await ShowResults(progress.Duration, gameItems, localGames, feedFixStatistics);
        Logger.Info($"Feeder rendered, duration={progress.Duration}", true);

        void UpdateProgress(string detail, float? ratioComplete) => progress.Update(null, ratioComplete, detail);
    }

    private async Task ShowResults(TimeSpan duration, IList<GameItem> gameItems, IList<LocalGame> localGames, Dictionary<string, int> fixStatistics)
    {
        var screenPosition = _window.GetCurrentScreenPosition();

        var results = new FeederResultsViewModel(gameItems, localGames);
        var showTask = results.Show(_window, screenPosition.X + WindowMargin, WindowMargin);

        var statistics = new FeederStatisticsViewModel(gameItems, duration, fixStatistics);
        statistics.Show(_window, screenPosition.X + WindowMargin, results.Window.Top + results.Window.Height + WindowMargin);

        var logging = new LoggingViewModel();
        logging.Show(_window, statistics.Window.Left + statistics.Window.Width + WindowMargin, results.Window.Top + results.Window.Height + WindowMargin,
            Model.ScreenWorkArea.Width - statistics.Window.Width - WindowMargin);

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

                _window.Close();
            };
        }

        await showTask;
    }

    //private readonly IEnumerable<string> _destinationContentTypes;
    private MaterialWindowEx _window;
    private FeatureType _feedFixDuplicateTableOption;
    private const int WindowMargin = 0;
}