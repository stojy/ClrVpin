using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ClrVpin.Controls;
using ClrVpin.Extensions;
using ClrVpin.Logging;
using ClrVpin.Models.Cleaner;
using ClrVpin.Models.Settings;
using ClrVpin.Models.Shared;
using ClrVpin.Models.Shared.Game;
using ClrVpin.Shared;
using ClrVpin.Shared.FeatureType;
using PropertyChanged;
using Utils;
using Utils.Extensions;

namespace ClrVpin.Cleaner;

[AddINotifyPropertyChangedInterface]
public class CleanerViewModel : IShowViewModel
{
    public CleanerViewModel()
    {
        StartCommand = new ActionCommand(Start);

        CheckPinballContentTypesView = FeatureOptions.CreateFeatureOptionsSelectionsView(
            Settings.GetPinballContentTypes(), Settings.Cleaner.SelectedCheckContentTypes, 
            _ => UpdateIsValid(), (enumOptions, enumOption) => enumOptions.Cast<ContentType>().First(x => x == enumOption).IsFolderValid);
        CheckMediaContentTypesView = FeatureOptions.CreateFeatureOptionsSelectionsView(
            Settings.GetMediaContentTypes(), Settings.Cleaner.SelectedCheckContentTypes, 
            _ => UpdateIsValid(), (enumOptions, enumOption) => enumOptions.Cast<ContentType>().First(x => x == enumOption).IsFolderValid);

        CheckHitTypesView = FeatureOptions.CreateFeatureOptionsSelectionsView(StaticSettings.AllHitTypes, Settings.Cleaner.SelectedCheckHitTypes, ToggleFixHitTypeState);
        FixHitTypesView = FeatureOptions.CreateFeatureOptionsSelectionsView(StaticSettings.AllHitTypes, Settings.Cleaner.SelectedFixHitTypes);

        // special handling for the fix hit types as they're functionality is coupled wit the criteria hit types, e.g. fix is disabled when check options are not selected
        FixHitTypesView.ForEach(fixHitFeatureType =>
        {
            var hitTypeEnum = (HitTypeEnum)fixHitFeatureType.Id;
            fixHitFeatureType.IsNeverSupported = hitTypeEnum == HitTypeEnum.Missing;
            fixHitFeatureType.IsSupported = Settings.Cleaner.SelectedCheckHitTypes.Contains(hitTypeEnum) && hitTypeEnum != HitTypeEnum.Missing || fixHitFeatureType.Id == FeatureOptions.SelectAllId;
            fixHitFeatureType.IsActive = Settings.Cleaner.SelectedFixHitTypes.Contains(hitTypeEnum) && hitTypeEnum != HitTypeEnum.Missing;
        });

        MultipleMatchOptionsView = FeatureOptions.CreateFeatureOptionsSelectionView(
            StaticSettings.MultipleMatchOptions, MultipleMatchOptionEnum.PreferMostRecentAndExceedSizeThreshold, () => Settings.Cleaner.SelectedMultipleMatchOption, UpdateExceedThresholdChecked);

        UpdateExceedThresholdChecked();
        UpdateIsValid();
    }

    public bool IsValid { get; set; }

    public ListCollectionView<FeatureType> CheckMediaContentTypesView { get; }
    public ListCollectionView<FeatureType> CheckPinballContentTypesView { get; }
    public ListCollectionView<FeatureType> CheckHitTypesView { get; }
    public ListCollectionView<FeatureType> FixHitTypesView { get; }
    public ListCollectionView<FeatureType> MultipleMatchOptionsView { get; }
    public ICommand StartCommand { get; }
    public Models.Settings.Settings Settings { get; } = Model.Settings;

    public bool ExceedSizeThresholdSelected { get; set; }

    public Window Show(Window parent)
    {
        _window = new MaterialWindowEx
        {
            Owner = parent,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            SizeToContent = SizeToContent.WidthAndHeight,
            Content = this,
            Resources = parent.Resources,
            ContentTemplate = parent.FindResource("CleanerTemplate") as DataTemplate,
            ResizeMode = ResizeMode.NoResize,
            Title = "Cleaner"
        };

        _window.Show();
        _window.Closed += (_, _) => Model.SettingsManager.Write();

        return _window;
    }

    private void UpdateIsValid() => IsValid = Settings.Cleaner.SelectedCheckContentTypes.Any();

    private void ToggleFixHitTypeState(FeatureType featureType)
    {
        // toggle the fix hit type checked & enabled
        var fixHitType = FixHitTypesView.First(x => x.Description == featureType.Description);

        fixHitType.IsSupported = featureType.IsActive && !fixHitType.IsNeverSupported;
        if (featureType.IsActive == false)
        {
            fixHitType.IsActive = false;
            Settings.Cleaner.SelectedFixHitTypes.Remove((HitTypeEnum)featureType.Id);
        }
    }

    private void UpdateExceedThresholdChecked()
    {
        ExceedSizeThresholdSelected = Settings.Cleaner.SelectedMultipleMatchOption == MultipleMatchOptionEnum.PreferMostRecentAndExceedSizeThreshold;
    }

    private async void Start()
    {
        Logger.Info($"\nCleaner started, settings={JsonSerializer.Serialize(Settings)}");

        _window.Hide();
        Logger.Clear();

        var progress = new ProgressViewModel();
        progress.Show(_window);

        List<LocalGame> games;
        try
        {
            progress.Update("Loading Database");
            games = await TableUtils.ReadGamesFromDatabases(Settings.GetSelectedCheckContentTypes());
            Logger.Info($"Loading database complete, duration={progress.Duration}", true);
        }
        catch (Exception)
        {
            progress.Close();
            _window.TryShow();
            return;
        }

        progress.Update("Matching Files");
        var unmatchedFiles = await TableUtils.MatchContentToLocalAsync(games, UpdateProgress, Settings.GetSelectedCheckContentTypes(), Settings.Cleaner.SelectedCheckHitTypes.Contains(HitTypeEnum.Unsupported));

        progress.Update("Fixing Files");
        var fixedFiles = await CleanerUtils.FixAsync(games, Settings.BackupFolder, UpdateProgress);

        progress.Update("Removing Unmatched Files");
        await CleanerUtils.RemoveUnmatchedAsync(unmatchedFiles, UpdateProgress);

        // delete empty backup folders - i.e. if there are no files (empty sub-directories are allowed)
        FileUtils.DeleteActiveBackupFolderIfEmpty();

        progress.Update("Preparing Results");
        await Task.Delay(1);
        _games = new ObservableCollection<LocalGame>(games);

        progress.Close();

        await ShowResults(fixedFiles, unmatchedFiles, progress.Duration);

        void UpdateProgress(string detail, float ratioComplete) => progress.Update(null, ratioComplete, detail);
    }

    private async Task ShowResults(ICollection<FileDetail> fixedFiles, ICollection<FileDetail> unmatchedFiles, TimeSpan duration)
    {
        var screenPosition = _window.GetCurrentScreenPosition();

        var statistics = new CleanerStatisticsViewModel(_games, duration, fixedFiles, unmatchedFiles);
        statistics.Show(_window, screenPosition.X + WindowMargin, WindowMargin);

        var width = Model.ScreenWorkArea.Width - statistics.Window.Width - WindowMargin;
        var results = new CleanerResultsViewModel(_games);
        var resultsShowTask = results.Show(_window, statistics.Window.Left + statistics.Window.Width + WindowMargin, statistics.Window.Top, width);

        //explorer.Show(_window, results.Window.Left, results.Window.Top + results.Window.Height + WindowMargin, width);

        var logging = new LoggingViewModel();
        logging.Show(_window, results.Window.Left, results.Window.Top + results.Window.Height + WindowMargin, width);

        statistics.Window.Closed += CloseWindows();
        results.Window.Closed += CloseWindows();
        logging.Window.Closed += CloseWindows();

        EventHandler CloseWindows()
        {
            return (_, _) =>
            {
                statistics.Window.Close();
                results.Close();
                logging.Close();

                _window.Close();
            };
        }

        await resultsShowTask;
    }

    private ObservableCollection<LocalGame> _games;
    private MaterialWindowEx _window;
    private const int WindowMargin = 0;
}