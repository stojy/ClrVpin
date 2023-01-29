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

        CheckPinballContentTypesView = FeatureOptions.CreateFeatureOptionsSelectionsView(Settings.GetPinballContentTypes(), Settings.Cleaner.SelectedCheckContentTypes, UpdateIsValid);
        CheckMediaContentTypesView = FeatureOptions.CreateFeatureOptionsSelectionsView(Settings.GetMediaContentTypes(), Settings.Cleaner.SelectedCheckContentTypes, UpdateIsValid);

        CheckHitTypesView = new ListCollectionView(CreateCheckHitTypes().ToList());
    //    CheckHitTypesView = new ListCollectionView(CreateCheckHitTypes().ToList());

        _fixHitTypes = CreateFixHitTypes();
        FixHitTypesView = new ListCollectionView(_fixHitTypes.ToList());

        //MultipleMatchOptionsView = new ListCollectionView(CreateMultipleMatchOptionTypes().ToList());
        MultipleMatchOptionsView = FeatureOptions.CreateFeatureOptionsSelectionView(
            StaticSettings.MultipleMatchOptions, MultipleMatchOptionEnum.PreferMostRecentAndExceedSizeThreshold, () => Settings.Cleaner.SelectedMultipleMatchOption, UpdateExceedThresholdChecked);

        UpdateExceedThresholdChecked();
        UpdateIsValid();
    }

    public bool IsValid { get; set; }

    public ListCollectionView CheckMediaContentTypesView { get; }
    public ListCollectionView CheckPinballContentTypesView { get; }
    public ListCollectionView CheckHitTypesView { get; }
    public ListCollectionView FixHitTypesView { get; }
    public ListCollectionView MultipleMatchOptionsView { get; }
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

    private IEnumerable<FeatureType> CreateCheckHitTypes()
    {
        // show all hit types
        var featureTypes = StaticSettings.AllHitTypes.Select(hitType =>
        {
            var featureType = new FeatureType((int)hitType.Enum)
            {
                Description = hitType.Description,
                Tip = hitType.Tip,
                IsSupported = true,
                IsActive = Settings.Cleaner.SelectedCheckHitTypes.Contains(hitType.Enum),
                IsHighlighted = hitType.IsHighlighted,
                IsHelpSupported = hitType.HelpUrl != null,
                HelpAction = new ActionCommand(() => Process.Start(new ProcessStartInfo(hitType.HelpUrl) { UseShellExecute = true }))
            };

            featureType.SelectedCommand = new ActionCommand(() =>
            {
                Settings.Cleaner.SelectedCheckHitTypes.Toggle(hitType.Enum);

                // toggle the fix hit type checked & enabled
                var fixHitType = _fixHitTypes.First(x => x.Description == featureType.Description);
                fixHitType.IsSupported = featureType.IsActive && !fixHitType.IsNeverSupported;
                if (featureType.IsActive == false)
                {
                    fixHitType.IsActive = false;
                    Settings.Cleaner.SelectedFixHitTypes.Remove(hitType.Enum);
                }
            });

            return featureType;
        }).ToList();

        return featureTypes.Concat(new[] { FeatureOptions.CreateSelectAll(featureTypes) });
    }

    private IEnumerable<FeatureType> CreateFixHitTypes()
    {
        // show all hit types, but allow them to be enabled and selected indirectly via the check hit type
        var featureTypes = StaticSettings.AllHitTypes.Select(hitType => new FeatureType((int)hitType.Enum)
        {
            Description = hitType.Description,
            Tip = hitType.Tip,
            IsNeverSupported = hitType.Enum == HitTypeEnum.Missing,
            IsSupported = Settings.Cleaner.SelectedCheckHitTypes.Contains(hitType.Enum) && hitType.Enum != HitTypeEnum.Missing,
            IsActive = Settings.Cleaner.SelectedFixHitTypes.Contains(hitType.Enum) && hitType.Enum != HitTypeEnum.Missing,
            SelectedCommand = new ActionCommand(() => Settings.Cleaner.SelectedFixHitTypes.Toggle(hitType.Enum)),
            IsHighlighted = hitType.IsHighlighted,
            IsHelpSupported = hitType.HelpUrl != null,
            HelpAction = new ActionCommand(() => Process.Start(new ProcessStartInfo(hitType.HelpUrl) { UseShellExecute = true }))
        }).ToList();

        return featureTypes.Concat(new[] { FeatureOptions.CreateSelectAll(featureTypes) });
    }

    private IEnumerable<FeatureType> CreateMultipleMatchOptionTypes()
    {
        // show all hit types, but allow them to be enabled and selected indirectly via the check hit type
        var contentTypes = StaticSettings.MultipleMatchOptions.Select(hitType => new FeatureType((int)hitType.Enum)
        {
            Description = hitType.Description,
            Tip = hitType.Tip,
            IsSupported = true,
            IsActive = Settings.Cleaner.SelectedMultipleMatchOption == hitType.Enum,
            SelectedCommand = new ActionCommand(() =>
            {
                Settings.Cleaner.SelectedMultipleMatchOption = hitType.Enum;
                UpdateExceedThresholdChecked();
            })
        });

        return contentTypes.ToList();
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
        var displayTask = results.Show(_window, statistics.Window.Left + statistics.Window.Width + WindowMargin, statistics.Window.Top, width);

        var explorer = new CleanerExplorerViewModel(_games);
        explorer.Show(_window, results.Window.Left, results.Window.Top + results.Window.Height + WindowMargin, width);

        var logging = new LoggingViewModel();
        logging.Show(_window, explorer.Window.Left, explorer.Window.Top + explorer.Window.Height + WindowMargin, width);

        statistics.Window.Closed += CloseWindows();
        results.Window.Closed += CloseWindows();
        explorer.Window.Closed += CloseWindows();
        logging.Window.Closed += CloseWindows();

        EventHandler CloseWindows()
        {
            return (_, _) =>
            {
                statistics.Window.Close();
                results.Close();
                explorer.Close();
                logging.Close();

                _window.Close();
            };
        }

        await displayTask;
    }

    private readonly IEnumerable<FeatureType> _fixHitTypes;

    private ObservableCollection<LocalGame> _games;
    private MaterialWindowEx _window;
    private const int WindowMargin = 0;
}