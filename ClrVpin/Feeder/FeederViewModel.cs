﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ClrVpin.Controls;
using ClrVpin.Extensions;
using ClrVpin.Home;
using ClrVpin.Logging;
using ClrVpin.Models.Feeder;
using ClrVpin.Models.Settings;
using ClrVpin.Models.Shared.Enums;
using ClrVpin.Models.Shared.Game;
using ClrVpin.Shared;
using ClrVpin.Shared.FeatureType;
using ClrVpin.Shared.Utils;
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

        // create separate property for each match criteria so they can be referenced individually from the view
        var matchCriteriaView = FeatureOptions.CreateFeatureOptionsMultiSelectionView(StaticSettings.MatchTypes, () => Settings.Feeder.SelectedMatchCriteriaOptions);
        MatchFuzzy = matchCriteriaView.First(x => x.Id == (int)HitTypeEnum.Fuzzy);

        // explicitly disable fuzzy logic if ALL of the settings are not fully configured, e.g. frontend database folder not setup
        if (!Model.SettingsManager.IsValid)
        {
            FeatureOptions.ClearFeatureType(MatchFuzzy);
            FeatureOptions.DisableFeatureType(MatchFuzzy, Model.OptionsDisabledMessage);
        }

        FeedFixOptionsView = FeatureOptions.CreateFeatureOptionsMultiSelectionView(StaticSettings.FixFeedOptions, () => Settings.Feeder.SelectedFeedFixOptions, _ => FixFeedOptionSelected());

        _feedFixDuplicateTableOption = FeedFixOptionsView.First(x => x.Id == (int)FixFeedOptionEnum.DuplicateTable);

        UpdateIsValid();
    }

    public bool IsValid { get; set; }

    public ICommand StartCommand { get; }
    public Models.Settings.Settings Settings { get; } = Model.Settings;
    
    public ListCollectionView<FeatureType> FeedFixOptionsView { get; }

    public FeatureType MatchFuzzy { get; }

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

    private void FixFeedOptionSelected()
    {
        // disable 'duplicate table' option if the prerequisite fix options aren't enabled
        if (!Settings.Feeder.SelectedFeedFixOptions.ContainsAll(
                FixFeedOptionEnum.Whitespace,
                FixFeedOptionEnum.ManufacturedIncludesAuthor,
                FixFeedOptionEnum.OriginalTableIncludesIpdbUrl,
                FixFeedOptionEnum.InvalidUrlIpdb,
                FixFeedOptionEnum.UpgradeUrlHttps,
                FixFeedOptionEnum.WrongManufacturerYear,
                FixFeedOptionEnum.WrongName,
                FixFeedOptionEnum.WrongUrlIpdb,
                FixFeedOptionEnum.WrongType))
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
        ProgressViewModel progress = null;

        try
        {
            Logger.Info($"Feeder started, settings={JsonSerializer.Serialize(Settings)}");

            progress = new ProgressViewModel("Feeding");

            _window.Hide();
            Logger.Clear();

            progress.Show(_window);

            var localGames = new List<LocalGame>();
            if (MatchFuzzy.IsActive)
            {
                try
                {
                    progress.Update("Loading database");
                    localGames = await DatabaseUtils.ReadGamesFromDatabases(Settings.GetFixableContentTypes());
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

            progress.Update("Matching online to local database(s)");
            await FeederUtils.MatchOnlineToLocalAsync(localGames, onlineGames, UpdateProgress);
            Logger.Info($"Matching local and online databases complete, duration={progress.Duration}", true);

            progress.Update("Matching local to online database");
            var gameItems = await FeederUtils.MergeOnlineAndLocalGamesAsync(localGames, onlineGames, UpdateProgress);
            Logger.Info($"Matching local and online databases complete, duration={progress.Duration}", true);

            progress.Update("Preparing Results");

            progress.Close();

            await ShowResults(progress.Duration, gameItems, localGames, feedFixStatistics);
            Logger.Info($"Feeder rendered, duration={progress.Duration}", true);

            void UpdateProgress(string detail, int current, int total) => progress.Update(null, null, detail, current, total);
        }
        catch (HttpRequestException e)
        {
            progress?.Close();
            Logger.Warn(e, "Feeder");

            await Notification.ShowWarning(HomeWindow.HomeDialogHost, "Feeder Was Unsuccessful", "Unable to access the internet.  Please check your internet connection and then try again.",
                $"{Logger.GetLogs(1)}", showCloseButton:true);
            _window.Close();
        }
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
    private readonly FeatureType _feedFixDuplicateTableOption;
    private const int WindowMargin = 0;
}