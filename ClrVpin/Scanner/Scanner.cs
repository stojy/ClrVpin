using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using ClrVpin.Models;
using MaterialDesignExtensions.Controls;
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
            CheckContentTypesView = new ListCollectionView(CreateCheckContentTypes().ToList());

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
            _scannerWindow = new MaterialWindow
            {
                Owner = parent,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                //SizeToContent = SizeToContent.WidthAndHeight,
                Width = 400,
                Height = 435,
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
                Model.Config.Save();
                parent.Show();
            };
        }

        private static IEnumerable<FeatureType> CreateCheckContentTypes()
        {
            // show all hit types
            var featureTypes = Config.ContentTypes.Select(contentType =>
            {
                var featureType = new FeatureType
                {
                    Description = contentType.Description,
                    Tip = contentType.Tip,
                    IsSupported = true,
                    IsActive = Model.Config.SelectedCheckContentTypes.Contains(contentType.Description),
                    SelectedCommand = new ActionCommand(() => Model.Config.SelectedCheckContentTypes.Toggle(contentType.Description))
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
                    Tip = hitType.Tip,
                    IsSupported = true,
                    IsActive = Model.Config.SelectedCheckHitTypes.Contains(hitType.Enum)
                };

                featureType.SelectedCommand = new ActionCommand(() =>
                {
                    Model.Config.SelectedCheckHitTypes.Toggle(hitType.Enum);

                    // toggle the fix hit type checked & enabled
                    var fixHitType = _fixHitTypes.First(x => x.Description == featureType.Description);
                    fixHitType.IsSupported = featureType.IsActive && !fixHitType.IsNeverSupported;
                    if (!featureType.IsActive)
                        fixHitType.IsActive = false;
                    Model.Config.SelectedFixHitTypes.Toggle(hitType.Enum);
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
                Tip = hitType.Tip,
                IsNeverSupported = hitType.Enum == HitTypeEnum.Missing,
                IsSupported = Model.Config.SelectedCheckHitTypes.Contains(hitType.Enum) && hitType.Enum != HitTypeEnum.Missing,
                IsActive = Model.Config.SelectedFixHitTypes.Contains(hitType.Enum) && hitType.Enum != HitTypeEnum.Missing,
                SelectedCommand = new ActionCommand(() => Model.Config.SelectedFixHitTypes.Toggle(hitType.Enum))
            });

            return contentTypes.ToList();
        }

        private async void Start()
        {
            _scannerWindow.Hide();

            var progress = new Progress();
            progress.Show(_scannerWindow);
            
            // todo; retrieve 'missing games' from spreadsheet

            progress.Update("Loading Database", 0);
            var games = ScannerUtils.GetDatabases();
            
            progress.Update("Checking Files", 30);
            var unknownFiles = ScannerUtils.Check(games);

            progress.Update("Fixing Files", 60);
            var fixFiles = await ScannerUtils.FixAsync(games, unknownFiles, Model.Config.BackupFolder);

            progress.Update("Preparing Results", 100);
            await Task.Delay(10);
            Games = new ObservableCollection<Game>(games);
            ShowResults(fixFiles.Concat(unknownFiles).ToList(), progress.Duration);
         
            progress.Close();
        }

        private void ShowResults(ICollection<FixFileDetail> fixFiles, TimeSpan duration)
        {
            var scannerResults = new ScannerResults(Games);
            scannerResults.Show(_scannerWindow, 5, 5);

            var scannerStatistics = new ScannerStatistics(Games, duration, fixFiles);
            scannerStatistics.Show(_scannerWindow, 5, scannerResults.Window.Height + WindowMargin, scannerResults.Window.Width);

            var scannerExplorer = new ScannerExplorer(Games);
            scannerExplorer.Show(_scannerWindow, scannerStatistics.Window.Width + WindowMargin, scannerResults.Window.Height + WindowMargin, scannerStatistics.Window.Height);

            _loggingWindow = new Logging.Logging();
            _loggingWindow.Show(_scannerWindow, scannerResults.Window.Width + WindowMargin, 5, scannerResults.Window.Height);

            scannerResults.Window.Closed += (_, _) =>
            {
                scannerStatistics.Close();
                scannerExplorer.Close();
                _loggingWindow.Close();
                _scannerWindow.Show();
            };
        }

        private readonly IEnumerable<FeatureType> _fixHitTypes;
        private Window _scannerWindow;
        private Logging.Logging _loggingWindow;
        private const int WindowMargin = 12;
    }
}