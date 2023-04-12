using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using ClrVpin.Controls;
using ClrVpin.Controls.Folder;
using ClrVpin.Extensions;
using ClrVpin.Logging;
using ClrVpin.Models.Settings;
using ClrVpin.Models.Shared;
using ClrVpin.Models.Shared.Game;
using ClrVpin.Shared;
using ClrVpin.Shared.FeatureType;
using ClrVpin.Shared.Utils;
using PropertyChanged;
using Utils;
using Utils.Extensions;

namespace ClrVpin.Merger;

[AddINotifyPropertyChangedInterface]
public class MergerViewModel : IShowViewModel
{
    public MergerViewModel() 
    {
        StartCommand = new ActionCommand(Start);
        DestinationContentTypeSelectedCommand = new ActionCommand(UpdateDestinationContentTypeSettings);

        CreateMatchCriteriaTypes();

        CreateIgnoreCriteria();

        MergeOptionsView = FeatureOptions.CreateFeatureOptionsSelectionsView(StaticSettings.MergeOptions, Settings.Merger.SelectedMergeOptions);

        SourceFolderModel = new GenericFolderTypeModel("Source", Settings.Merger.SourceFolder, true, folder =>
        {
            Settings.Merger.SourceFolder = folder;
            TryUpdateDestinationFolder(folder);
        });

        var destinationContentTypes = Model.Settings.GetFixableContentTypes().Select((contentType, i) => 
            new FeatureType(i)
            {
                Description = contentType.Description,
                IsActive = contentType.IsFolderValid,
                Tip = contentType.Tip + (contentType.IsFolderValid == false ? Model.OptionsDisabledMessage : null)
            });
        DestinationContentTypesView = new ListCollectionView<FeatureType>(destinationContentTypes);
        TryUpdateDestinationFolder(Settings.Merger.DestinationContentType);

        IgnoreWordsString = string.Join(", ", Settings.Merger.IgnoreIWords);
        IgnoreWordsChangedCommand = new ActionCommand(IgnoreWordsChanged);

        UpdateDestinationContentTypeSettings();
    }


    public bool IsValid { get; set; }

    public ListCollectionView MergeOptionsView { get; }

    public GenericFolderTypeModel SourceFolderModel { get; }
    
    public ListCollectionView<FeatureType> DestinationContentTypesView { get; }

    public ICommand DestinationContentTypeSelectedCommand { get; set; }
    public ICommand StartCommand { get; }
    public Models.Settings.Settings Settings { get; } = Model.Settings;

    public string IgnoreWordsString { get; set; }
    public ICommand IgnoreWordsChangedCommand { get; set; }

    public FeatureType IgnoreIfNotNewerFeature { get; private set; }
    public FeatureType IgnoreIfSmallerFeature { get; private set; }
    public FeatureType IgnoreIfContainsWordsFeature { get; private set; }
    public FeatureType DeleteIgnoredFilesOptionFeature { get; private set; }
    public FeatureType IgnoreSelectClearAllFeature { get; private set; }

    public FeatureType MatchDuplicate { get; private set; }

    public FeatureType MatchFuzzy { get; private set; }

    public FeatureType MatchTableName { get; private set; }

    public FeatureType MatchWrongCase { get; private set; }
    public FeatureType MatchSelectClearAllFeature { get; private set; }
    public FeatureType DestinationContentType { get; set; }

    public Window Show(Window parent)
    {
        _window = new MaterialWindowEx
        {
            Owner = parent,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            SizeToContent = SizeToContent.WidthAndHeight,
            Content = this,
            Resources = parent.Resources,
            ContentTemplate = parent.FindResource("MergerTemplate") as DataTemplate,
            ResizeMode = ResizeMode.NoResize,
            Title = "Merger"
        };

        _window.Show();
        _window.Closed += (_, _) => Model.SettingsManager.Write();

        return _window;
    }

    private void IgnoreWordsChanged()
    {
        Settings.Merger.IgnoreIWords = IgnoreWordsString == null ? new List<string>() : IgnoreWordsString.Split(",").Select(x => x.Trim().ToLower()).ToList();
    }

    private void UpdateDestinationContentTypeSettings()
    {
        Settings.Merger.DestinationContentType = DestinationContentType?.Description;
        IsValid = !string.IsNullOrEmpty(Settings.Merger.DestinationContentType);
    }

    private void TryUpdateDestinationFolder(string folder)
    {
        // attempt to assign destination folder automatically based on the specified folder
        // - if a folder for a disabled content type is specified (e.g. folder in settings hasn't been configured), then remove the selected item (if any)
        var matchedContentType = DestinationContentTypesView
            .Where(contentType => contentType.IsActive)
            .FirstOrDefault(c => folder?.ToLower().EndsWith(c.Description.ToLower()) ?? false);
        
        DestinationContentType = matchedContentType;
        DestinationContentTypesView.MoveCurrentTo(DestinationContentType);

        UpdateDestinationContentTypeSettings();
    }

    private void CreateMatchCriteriaTypes()
    {
        // show all match criteria types
        // - except for unknown and unsupported which are used 'under the hood' for subsequent reporting
        var enumOptions = StaticSettings.MatchTypes.Where(x => !x.Enum.In(HitTypeEnum.CorrectName, HitTypeEnum.Unknown, HitTypeEnum.Unsupported)).ToArray();
        var featureTypesView = FeatureOptions.CreateFeatureOptionsSelectionsView(enumOptions, Settings.Merger.SelectedMatchTypes);

        // create separate property for each so they can be referenced individually in the UI
        MatchWrongCase = featureTypesView.First(x => x.Id == (int)HitTypeEnum.WrongCase);
        MatchTableName = featureTypesView.First(x => x.Id == (int)HitTypeEnum.TableName);
        MatchDuplicate = featureTypesView.First(x => x.Id == (int)HitTypeEnum.DuplicateExtension);
        MatchFuzzy = featureTypesView.First(x => x.Id == (int)HitTypeEnum.Fuzzy);
        MatchSelectClearAllFeature = featureTypesView.First(x => x.Id == FeatureOptions.SelectAllId);
    }

    private void CreateIgnoreCriteria()
    {
        // create ignore criteria
        var ignoreFeatureTypesView = FeatureOptions.CreateFeatureOptionsSelectionsView(StaticSettings.IgnoreCriteria, Settings.Merger.SelectedIgnoreCriteria);

        // create separate property for each so they can be referenced individually in the UI
        IgnoreIfContainsWordsFeature = ignoreFeatureTypesView.First(x => x.Id == (int)IgnoreCriteriaEnum.IgnoreIfContainsWords);
        IgnoreIfSmallerFeature = ignoreFeatureTypesView.First(x => x.Id == (int)IgnoreCriteriaEnum.IgnoreIfSmaller);
        IgnoreIfNotNewerFeature = ignoreFeatureTypesView.First(x => x.Id == (int)IgnoreCriteriaEnum.IgnoreIfNotNewer);
        IgnoreSelectClearAllFeature = ignoreFeatureTypesView.First(x => x.Id == FeatureOptions.SelectAllId);

        // delete ignored isn't technically an ignored option.. but added here to keep it visually consistent
        DeleteIgnoredFilesOptionFeature = FeatureOptions.CreateFeatureType(StaticSettings.DeleteIgnoredFilesOption, Settings.Merger.DeleteIgnoredFiles);
        DeleteIgnoredFilesOptionFeature.SelectedCommand = new ActionCommand(() => Settings.Merger.DeleteIgnoredFiles = !Settings.Merger.DeleteIgnoredFiles);
        ignoreFeatureTypesView.AddNewItem(DeleteIgnoredFilesOptionFeature);
    }

    private async void Start()
    {
        Logger.Info($"\nMerger started, settings={JsonSerializer.Serialize(Settings)}");

        _window.Hide();
        Logger.Clear();

        var progress = new ProgressViewModel();
        progress.Show(_window);


        List<LocalGame> games;
        try
        {
            progress.Update("Loading Database");
            games = await DatabaseUtils.ReadGamesFromDatabases(new List<ContentType> { Settings.GetSelectedDestinationContentType() });
            Logger.Info($"Loading database complete, duration={progress.Duration}", true);
        }
        catch (Exception)
        {
            progress.Close();
            _window.TryShow();
            return;
        }

        progress.Update("Checking and Matching Files");
        var unmatchedFiles = await MergerUtils.CheckAndMatchAsync(games, UpdateProgress);

        progress.Update("Merging Files");
        var gameFiles = await MergerUtils.MergeAsync(games, Settings.BackupFolder, UpdateProgress);

        progress.Update("Removing Unmatched Ignored Files");
        await MergerUtils.RemoveUnmatchedIgnoredAsync(unmatchedFiles, UpdateProgress);

        // delete empty backup folders - i.e. if there are no files (empty sub-directories are allowed)
        FileUtils.DeleteActiveBackupFolderIfEmpty();

        progress.Update("Preparing Results");
        await Task.Delay(1);
        _games = new ObservableCollection<LocalGame>(games);

        progress.Close();

        await ShowResults(gameFiles, unmatchedFiles, progress.Duration);

        void UpdateProgress(string detail, float ratioComplete) => progress.Update(null, ratioComplete, detail);
    }

    private async Task ShowResults(ICollection<FileDetail> gameFiles, ICollection<FileDetail> unmatchedFiles, TimeSpan duration)
    {
        var screenPosition = _window.GetCurrentScreenPosition();

        var statistics = new MergerStatisticsViewModel(_games, duration, gameFiles, unmatchedFiles);
        statistics.Show(_window, screenPosition.X + WindowMargin, WindowMargin);

        var width = Model.ScreenWorkArea.Width - statistics.Window.Width - WindowMargin;
        var mergerResults = new MergerResultsViewModel(_games, gameFiles, unmatchedFiles);
        var showTask = mergerResults.Show(_window, statistics.Window.Left + statistics.Window.Width + WindowMargin, WindowMargin, width);

        var logging = new LoggingViewModel();
        logging.Show(_window, mergerResults.Window.Left, mergerResults.Window.Top + mergerResults.Window.Height + WindowMargin, width);

        logging.Window.Closed += CloseWindows();
        mergerResults.Window.Closed += CloseWindows();
        statistics.Window.Closed += CloseWindows();

        EventHandler CloseWindows()
        {
            return (_, _) =>
            {
                mergerResults.Close();
                statistics.Window.Close();
                logging.Close();

                _window.Close();
            };
        }

        await showTask;
    }

    private ObservableCollection<LocalGame> _games;
    private MaterialWindowEx _window;
    private const int WindowMargin = 0;
}