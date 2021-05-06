using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using ClrVpin.Models;
using PropertyChanged;
using Utils;

namespace ClrVpin.Scanner
{
    [AddINotifyPropertyChangedInterface]
    public class Scanner
    {
        public Scanner()
        {
            StartCommand = new ActionCommand(Start);
            //ConfigureCheckContentTypesCommand = new ActionCommand<string>(ConfigureCheckContentTypes);
            CheckContentTypesView = new ListCollectionView(CreateContentTypes().ToList());

            CheckHitTypesView = new ListCollectionView(CreateCheckHitTypes().ToList());

            _fixHitTypes = CreateFixHitTypes();
            FixHitTypesView = new ListCollectionView(_fixHitTypes.ToList());
        }

        public ListCollectionView CheckContentTypesView { get; set; }
        public ListCollectionView CheckHitTypesView { get; set; }
        public ListCollectionView FixHitTypesView { get; set; }

        public ObservableCollection<Game> Games { get; set; }
        public ICommand StartCommand { get; set; }

        public void Show(Window parent)
        {
            _scannerWindow = new Window
            {
                Owner = parent,
                Title = "Scanner",
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SizeToContent = SizeToContent.WidthAndHeight,
                Content = this,
                ContentTemplate = parent.FindResource("ScannerTemplate") as DataTemplate
            };

            _scannerWindow.Show();
            parent.Hide();

            _scannerWindow.Closed += (_, _) =>
            {
                Properties.Settings.Default.Save();
                parent.Show();
            };
        }

        private static IEnumerable<FeatureType> CreateContentTypes()
        {
            // show all hit types
            var featureTypes = Config.ContentTypes.Select(contentType =>
            {
                var featureType = new FeatureType
                {
                    Description = contentType.Type,
                    Tip = contentType.Tip,
                    IsSupported = true,
                    IsActive = Model.Config.CheckContentTypes.Contains(contentType.Type),
                    SelectedCommand = new ActionCommand(() => Model.Config.CheckContentTypes.Toggle(contentType.Type))
                };

                return featureType;
            });

            return featureTypes.ToList();
        }

        private IEnumerable<FeatureType> CreateCheckHitTypes()
        {
            // show all hit types
            var featureTypes = Config.HitTypes.Select(hitType =>
            {
                var featureType = new FeatureType
                {
                    Description = hitType.Description,
                    IsSupported = true,
                    IsActive = Model.Config.CheckHitTypes.Contains(hitType.Type)
                };

                featureType.SelectedCommand = new ActionCommand(() =>
                {
                    Model.Config.CheckHitTypes.Toggle(hitType.Type);

                    // toggle the fix hit type checked & enabled
                    var fixHitType = _fixHitTypes.First(x => x.Description == featureType.Description);
                    fixHitType.IsSupported = featureType.IsActive && !fixHitType.IsNeverSupported;
                    if (!featureType.IsActive)
                        fixHitType.IsActive = false;
                    Model.Config.FixHitTypes.Toggle(hitType.Type);
                });

                return featureType;
            });

            return featureTypes.ToList();
        }

        private static IEnumerable<FeatureType> CreateFixHitTypes()
        {
            // show all hit types, but allow them to be enabled and selected indirectly via the check hit type
            var contentTypes = Config.HitTypes.Select(hitType => new FeatureType
            {
                Description = hitType.Description,
                IsNeverSupported = hitType.Type == HitTypeEnum.Missing,
                IsSupported = Model.Config.CheckHitTypes.Contains(hitType.Type) && hitType.Type != HitTypeEnum.Missing,
                IsActive = Model.Config.FixHitTypes.Contains(hitType.Type) && hitType.Type != HitTypeEnum.Missing,
                SelectedCommand = new ActionCommand(() => Model.Config.FixHitTypes.Toggle(hitType.Type))
            });

            return contentTypes.ToList();
        }

        private void Start()
        {
            // todo; show progress bar
            _loggingWindow = new Logging.Logging();
            _loggingWindow.Show(_scannerWindow, ScannerResults.Width, 5, ScannerResults.Height);

            _scanStopWatch = Stopwatch.StartNew();

            var games = ScannerUtils.GetDatabases();

            // todo; retrieve 'missing games' from spreadsheet

            var unknownFiles = ScannerUtils.Check(games);

            var fixFiles = ScannerUtils.Fix(games, unknownFiles, Model.Config.BackupFolder);

            Games = new ObservableCollection<Game>(games);

            _scanStopWatch.Stop();

            ShowResults(fixFiles.Concat(unknownFiles).ToList());
        }

        private void ShowResults(ICollection<FixFileDetail> fixFiles)
        {
            var scannerResults = new ScannerResults(Games);
            scannerResults.Show(_scannerWindow, 5, 5);

            var scannerStatistics = new ScannerStatistics(Games, _scanStopWatch, fixFiles);
            scannerStatistics.Show(_scannerWindow, 5, scannerResults.Window.Height + 5);

            var explorerWindow = new ScannerExplorer(Games);
            explorerWindow.Show(_scannerWindow, scannerStatistics.Window.Width, scannerResults.Window.Height + 5, scannerStatistics.Window.Height);

            _scannerWindow.Hide();
            scannerResults.Window.Closed += (_, _) =>
            {
                scannerStatistics.Close();
                explorerWindow.Close();
                _loggingWindow.Close();
                _scannerWindow.Show();
            };
        }

        private readonly IEnumerable<FeatureType> _fixHitTypes;
        private Window _scannerWindow;
        private Stopwatch _scanStopWatch;
        private Logging.Logging _loggingWindow;
    }
}