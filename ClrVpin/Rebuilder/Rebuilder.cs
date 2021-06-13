using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using ClrVpin.Controls.FolderSelection;
using ClrVpin.Models;
using ClrVpin.Scanner;
using MaterialDesignExtensions.Controls;
using PropertyChanged;
using Utils;

namespace ClrVpin.Rebuilder
{
    [AddINotifyPropertyChangedInterface]
    public class Rebuilder
    {
        public Rebuilder()
        {
            StartCommand = new ActionCommand(Start);
            
            MatchCriteriaTypesView = new ListCollectionView(CreateMatchCriteriaTypes().ToList());
            OverwriteCriteriaTypesView = new ListCollectionView(CreateOverwriteOptions().ToList());

            SourceFolderModel = new FolderTypeModel("Source", Model.Config.SourceFolder, folder =>
            {
                Model.Config.SourceFolder = folder;
                TryUpdateDestinationFolder(folder);
            });

            _destinationContentTypes = Model.Config.GetFrontendFolders().Where(x=> !x.IsDatabase).Select(x => x.Description);
            DestinationContentTypes = new ObservableCollection<string>(_destinationContentTypes);

            Config = Model.Config;
        }

        private void TryUpdateDestinationFolder(string folder)
        {
            // attempt to assign destination folder automatically based on the specified folder
            var contentType = _destinationContentTypes.FirstOrDefault(folder.EndsWith);
            if (contentType != null)
                Config.DestinationContentType = contentType;
        }

        public ListCollectionView MatchCriteriaTypesView { get; set; }
        public ListCollectionView OverwriteCriteriaTypesView { get; set; }
        
        public FolderTypeModel SourceFolderModel { get; set; }
        public ObservableCollection<string> DestinationContentTypes { get; set; }
        public string DestinationContentType { get; set; }

        public ObservableCollection<Game> Games { get; set; }
        public ICommand StartCommand { get; set; }
        public Config Config { get; set; }

        public void Show(Window parent)
        {
            _rebuilderWindow = new MaterialWindow
            {
                Owner = parent,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                //SizeToContent = SizeToContent.WidthAndHeight,
                Width = 650,
                Height = 400,
                Content = this,
                Resources = parent.Resources,
                ContentTemplate = parent.FindResource("RebuilderTemplate") as DataTemplate,
                ResizeMode = ResizeMode.NoResize,
                Title = "Rebuilder"
            };

            _rebuilderWindow.Show();
            parent.Hide();

            _rebuilderWindow.Closed += (_, _) =>
            {
                Model.Config.Save();
                parent.Show();
            };
        }

        private static IEnumerable<FeatureType> CreateMatchCriteriaTypes()
        {
            // show all match criteria types
            var matchTypes = Config.MatchTypes.Select(matchType =>
            {
                var featureType = new FeatureType
                {
                    Description = matchType.Description,
                    Tip = matchType.Tip,
                    IsSupported = true,
                    IsActive = Model.Config.CheckContentTypes.Contains(matchType.Description), // todo
                    SelectedCommand = new ActionCommand(() => Model.Config.MatchHitTypes.Toggle(matchType.Enum))
                };

                return featureType;
            });

            return matchTypes.ToList();
        }

        private IEnumerable<FeatureType> CreateOverwriteOptions()
        {
            // show all overwrite options
            var featureTypes = Config.OverwriteOptions.Select(overwriteOption =>
            {
                var featureType = new FeatureType
                {
                    Description = overwriteOption.Description,
                    Tip = overwriteOption.Tip,
                    IsSupported = true,
                    IsActive = Model.Config.SelectedOverwriteOptions.Contains(overwriteOption.Enum),
                    SelectedCommand = new ActionCommand(() => Model.Config.SelectedOverwriteOptions.Toggle(overwriteOption.Enum))
                };

                return featureType;
            });

            return featureTypes.ToList();
        }

        private async void Start()
        {
            _rebuilderWindow.Hide();

            var progress = new Progress();
            progress.Show(_rebuilderWindow);

            // todo; retrieve 'missing games' from spreadsheet

            progress.Update("Loading Database", 0);
            var games = ScannerUtils.GetDatabases();

            progress.Update("Checking Files", 30);
            //var unknownFiles = ScannerUtils.Check(games);

            progress.Update("Fixing Files", 60);
            //var fixFiles = await ScannerUtils.FixAsync(games, unknownFiles, Model.Config.BackupFolder);

            progress.Update("Preparing Results", 100);
            await Task.Delay(10);
            Games = new ObservableCollection<Game>(games);
            //ShowResults(fixFiles.Concat(unknownFiles).ToList(), progress.Duration);

            progress.Close();
        }

        private void ShowResults(ICollection<FixFileDetail> fixFiles, TimeSpan duration)
        {
            var scannerResults = new ScannerResults(Games);
            scannerResults.Show(_rebuilderWindow, 5, 5);

            var scannerStatistics = new ScannerStatistics(Games, duration, fixFiles);
            scannerStatistics.Show(_rebuilderWindow, 5, scannerResults.Window.Height + WindowMargin, scannerResults.Window.Width);

            var scannerExplorer = new ScannerExplorer(Games);
            scannerExplorer.Show(_rebuilderWindow, scannerStatistics.Window.Width + WindowMargin, scannerResults.Window.Height + WindowMargin, scannerStatistics.Window.Height);

            _loggingWindow = new Logging.Logging();
            _loggingWindow.Show(_rebuilderWindow, scannerResults.Window.Width + WindowMargin, 5, scannerResults.Window.Height);

            scannerResults.Window.Closed += (_, _) =>
            {
                scannerStatistics.Close();
                scannerExplorer.Close();
                _loggingWindow.Close();
                _rebuilderWindow.Show();
            };
        }

        private Window _rebuilderWindow;
        private Logging.Logging _loggingWindow;
        private IEnumerable<string> _destinationContentTypes;
        private const int WindowMargin = 12;
    }
}