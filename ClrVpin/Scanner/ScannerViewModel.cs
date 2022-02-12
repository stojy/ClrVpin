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
using ClrVpin.Logging;
using ClrVpin.Models;
using ClrVpin.Models.Scanner;
using ClrVpin.Models.Settings;
using ClrVpin.Shared;
using PropertyChanged;
using Utils;
using Utils.Extensions;

namespace ClrVpin.Scanner
{
    [AddINotifyPropertyChangedInterface]
    public class ScannerViewModel
    {
        public ScannerViewModel()
        {
            StartCommand = new ActionCommand(Start);
            CheckPinballContentTypesView = new ListCollectionView(CreateCheckContentTypes(Settings.GetPinballContentTypes()).ToList());
            CheckMediaContentTypesView = new ListCollectionView(CreateCheckContentTypes(Settings.GetMediaContentTypes()).ToList());

            CheckHitTypesView = new ListCollectionView(CreateCheckHitTypes().ToList());

            _fixHitTypes = CreateFixHitTypes();
            FixHitTypesView = new ListCollectionView(_fixHitTypes.ToList());

            MultipleMatchOptionsView = new ListCollectionView(CreateMultipleMatchOptionTypes().ToList());
            
            UpdateExceedThresholdChecked();
            UpdateIsValid();
        }

        public bool IsValid { get; set; }

        public ListCollectionView CheckMediaContentTypesView { get; set; }
        public ListCollectionView CheckPinballContentTypesView { get; set; }
        public ListCollectionView CheckHitTypesView { get; set; }
        public ListCollectionView FixHitTypesView { get; set; }
        public ListCollectionView MultipleMatchOptionsView { get; set; }

        public ObservableCollection<Game> Games { get; set; }
        public ICommand StartCommand { get; set; }
        public Models.Settings.Settings Settings { get; } = Model.Settings;

        public bool ExceedSizeThresholdSelected { get; set; }

        public void Show(Window parent)
        {
            _scannerWindow = new MaterialWindowEx
            {
                Owner = parent,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SizeToContent = SizeToContent.WidthAndHeight,
                Content = this,
                Resources = parent.Resources,
                ContentTemplate = parent.FindResource("ScannerTemplate") as DataTemplate,
                ResizeMode = ResizeMode.NoResize,
                Title = "Scanner"
            };

            _scannerWindow.Show();
            parent.Hide();

            _scannerWindow.Closed += (_, _) =>
            {
                Model.SettingsManager.Write();
                parent.Show();
            };
        }

        private void UpdateIsValid() => IsValid = Settings.Scanner.SelectedCheckContentTypes.Any();

        private IEnumerable<FeatureType> CreateCheckContentTypes(IEnumerable<ContentType> contentTypes)
        {
            // show all hit types
            var featureTypes = contentTypes.Select(contentType =>
            {
                var featureType = new FeatureType((int) contentType.Enum)
                {
                    Description = contentType.Description,
                    Tip = contentType.Tip,
                    IsSupported = true,
                    IsActive = Settings.Scanner.SelectedCheckContentTypes.Contains(contentType.Description),
                    SelectedCommand = new ActionCommand(() =>
                    {
                        Settings.Scanner.SelectedCheckContentTypes.Toggle(contentType.Description);
                        UpdateIsValid();
                    })
                };

                return featureType;
            }).ToList();

            return featureTypes.Concat(new [] {FeatureType.CreateSelectAll(featureTypes)});
        }

        private IEnumerable<FeatureType> CreateCheckHitTypes()
        {
            // show all hit types
            var featureTypes = StaticSettings.AllHitTypes.Select(hitType =>
            {
                var featureType = new FeatureType((int) hitType.Enum)
                {
                    Description = hitType.Description,
                    Tip = hitType.Tip,
                    IsSupported = true,
                    IsActive = Settings.Scanner.SelectedCheckHitTypes.Contains(hitType.Enum),
                    IsHighlighted = hitType.IsHighlighted,
                    IsHelpSupported = hitType.HelpUrl != null,
                    HelpAction = new ActionCommand(() => Process.Start(new ProcessStartInfo(hitType.HelpUrl) { UseShellExecute = true }))
                };

                featureType.SelectedCommand = new ActionCommand(() =>
                {
                    Settings.Scanner.SelectedCheckHitTypes.Toggle(hitType.Enum);

                    // toggle the fix hit type checked & enabled
                    var fixHitType = _fixHitTypes.First(x => x.Description == featureType.Description);
                    fixHitType.IsSupported = featureType.IsActive && !fixHitType.IsNeverSupported;
                    if (featureType.IsActive == false)
                    {
                        fixHitType.IsActive = false;
                        Settings.Scanner.SelectedFixHitTypes.Remove(hitType.Enum);
                    }
                });

                return featureType;
            }).ToList();

            return featureTypes.Concat(new[] { FeatureType.CreateSelectAll(featureTypes) });
        }

        private IEnumerable<FeatureType> CreateFixHitTypes()
        {
            // show all hit types, but allow them to be enabled and selected indirectly via the check hit type
            var featureTypes = StaticSettings.AllHitTypes.Select(hitType => new FeatureType((int) hitType.Enum)
            {
                Description = hitType.Description,
                Tip = hitType.Tip,
                IsNeverSupported = hitType.Enum == HitTypeEnum.Missing,
                IsSupported = Settings.Scanner.SelectedCheckHitTypes.Contains(hitType.Enum) && hitType.Enum != HitTypeEnum.Missing,
                IsActive = Settings.Scanner.SelectedFixHitTypes.Contains(hitType.Enum) && hitType.Enum != HitTypeEnum.Missing,
                SelectedCommand = new ActionCommand(() => Settings.Scanner.SelectedFixHitTypes.Toggle(hitType.Enum)),
                IsHighlighted = hitType.IsHighlighted,
                IsHelpSupported = hitType.HelpUrl != null,
                HelpAction = new ActionCommand(() => Process.Start(new ProcessStartInfo(hitType.HelpUrl) { UseShellExecute = true }))
            }).ToList();

            return featureTypes.Concat(new[] { FeatureType.CreateSelectAll(featureTypes) });
        }

        private IEnumerable<FeatureType> CreateMultipleMatchOptionTypes()
        {
            // show all hit types, but allow them to be enabled and selected indirectly via the check hit type
            var contentTypes = StaticSettings.MultipleMatchOptions.Select(hitType => new FeatureType((int) hitType.Enum)
            {
                Description = hitType.Description,
                Tip = hitType.Tip,
                IsSupported = true,
                IsActive = Settings.Scanner.SelectedMultipleMatchOption == hitType.Enum,
                SelectedCommand = new ActionCommand(() =>
                {
                    Settings.Scanner.SelectedMultipleMatchOption = hitType.Enum;
                    UpdateExceedThresholdChecked();
                })
            });

            return contentTypes.ToList();
        }

        private void UpdateExceedThresholdChecked()
        {
            ExceedSizeThresholdSelected = Settings.Scanner.SelectedMultipleMatchOption == MultipleMatchOptionEnum.PreferMostRecentAndExceedSizeThreshold;
        }

        private async void Start()
        {
            Logger.Info($"\nScanner started, settings={JsonSerializer.Serialize(Settings)}");

            _scannerWindow.Hide();
            Logger.Clear();

            var progress = new ProgressViewModel();
            progress.Show(_scannerWindow);

            // todo; retrieve 'missing games' from spreadsheet

            progress.Update("Loading Database");
            var games = TableUtils.GetGamesFromDatabases(Settings.GetSelectedCheckContentTypes());

            progress.Update("Checking Files");
            var unmatchedFiles = await ScannerUtils.CheckAsync(games, UpdateProgress);

            progress.Update("Fixing Files");
            var gameFiles = await ScannerUtils.FixAsync(games, Settings.BackupFolder, UpdateProgress);

            progress.Update("Removing Unmatched Files");
            await ScannerUtils.RemoveUnmatchedAsync(unmatchedFiles, UpdateProgress);

            progress.Update("Preparing Results");
            await Task.Delay(1);
            Games = new ObservableCollection<Game>(games);

            ShowResults(gameFiles, unmatchedFiles, progress.Duration);

            progress.Close();

            void UpdateProgress(string detail, int percentage) => progress.Update(null, percentage, detail);
        }

        private void ShowResults(ICollection<FileDetail> gameFiles, ICollection<FileDetail> unmatchedFiles, TimeSpan duration)
        {
            var statistics = new ScannerStatisticsViewModel(Games, duration, gameFiles, unmatchedFiles);
            statistics.Show(_scannerWindow, WindowMargin, WindowMargin);

            var results = new ScannerResultsViewModel(Games);
            results.Show(_scannerWindow, statistics.Window.Left + statistics.Window.Width + WindowMargin, statistics.Window.Top);

            var explorer = new ScannerExplorerViewModel(Games);
            explorer.Show(_scannerWindow, results.Window.Left, results.Window.Top + results.Window.Height + WindowMargin);

            var logging = new LoggingViewModel();
            logging.Show(_scannerWindow, explorer.Window.Left, explorer.Window.Top + explorer.Window.Height + WindowMargin);

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

                    _scannerWindow.Show();
                };
            }
        }

        private readonly IEnumerable<FeatureType> _fixHitTypes;
        private Window _scannerWindow;
        private const int WindowMargin = 0;
    }
}