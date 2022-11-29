using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ClrVpin.Controls;
using ClrVpin.Models.Shared;
using ClrVpin.Models.Shared.Game;
using ClrVpin.Shared;
using PropertyChanged;

namespace ClrVpin.Explorer;

[AddINotifyPropertyChangedInterface]
public class ExplorerResultsViewModel
{
    public ExplorerResultsViewModel(ObservableCollection<LocalGame> games)
    {
        Games = games;
        GamesView = new ListCollectionView<LocalGame>(games);
        Initialise();
    }

    private Models.Settings.Settings Settings { get; set; }
    public ListCollectionView<LocalGame> GamesView { get; }

    public Window Window { get; private set; }

    public ObservableCollection<LocalGame> Games { get; }

    public async Task Show(Window parentWindow, double left, double top, double width)
    {
        Window = new MaterialWindowEx
        {
            Owner = parentWindow,
            Title = "Results",
            Left = left,
            Top = top,
            Width = width,
            Height = (Model.ScreenWorkArea.Height - WindowMargin - WindowMargin) * 0.8,
            Content = this,
            Resources = parentWindow.Resources,
            ContentTemplate = parentWindow.FindResource("ExplorerResultsTemplate") as DataTemplate
        };
        Window.Show();

        await ShowSummary();
    }

    private void Initialise()
    {
        Settings = Model.Settings;

        //_allContentFeatureTypes = CreateAllContentFeatureTypes();
        //AllContentFeatureTypesView = new ListCollectionView<FeatureType>(_allContentFeatureTypes.ToList());

        //_allHitFeatureTypes = CreateAllHitFeatureTypes();
        //AllHitFeatureTypesView = new ListCollectionView<FeatureType>(_allHitFeatureTypes.ToList());

        //SearchTextCommand = new ActionCommand(SearchTextChanged);
        //ExpandGamesCommand = new ActionCommand<bool>(ExpandItems);

        //BackupFolder = FileUtils.ActiveBackupFolder;
        //NavigateToBackupFolderCommand = new ActionCommand(NavigateToBackupFolder);

        //UpdateStatus(Games);
        //InitView();
    }

    //protected override IList<FeatureType> CreateAllContentFeatureTypes()
    //{
    //    // show all content types, but assign enabled and active based on the cleaner configuration
    //    var featureTypes = Settings.GetFixableContentTypes().Select(contentType => new FeatureType((int)contentType.Enum)
    //    {
    //        Description = contentType.Description,
    //        Tip = contentType.Tip,

    //        // t_odo; use id
    //        IsSupported = Settings.Cleaner.SelectedCheckContentTypes.Contains(contentType.Description),
    //        IsActive = Settings.Cleaner.SelectedCheckContentTypes.Contains(contentType.Description),
    //        SelectedCommand = new ActionCommand(UpdateHitsView)
    //    }).ToList();

    //    return featureTypes.Concat(new[] { FeatureType.CreateSelectAll(featureTypes) }).ToList();
    //}

    //protected override IList<FeatureType> CreateAllHitFeatureTypes()
    //{
    //    // show all hit types, but assign enabled and active based on the cleaner configuration
    //    // - for completeness the valid hits are also visible, but disabled by default since no fixes were required
    //    var featureTypes = StaticSettings.AllHitTypes.Select(hitType => new FeatureType((int)hitType.Enum)
    //    {
    //        Description = hitType.Description,
    //        IsSupported = Settings.Cleaner.SelectedCheckHitTypes.Contains(hitType.Enum) || hitType.Enum == HitTypeEnum.CorrectName,
    //        IsActive = Settings.Cleaner.SelectedCheckHitTypes.Contains(hitType.Enum),
    //        SelectedCommand = new ActionCommand(UpdateHitsView)
    //    }).ToList();

    //    return featureTypes.Concat(new[] { FeatureType.CreateSelectAll(featureTypes) }).ToList();
    //}

    private async Task ShowSummary()
    {
        var validHits = Games.SelectMany(x => x.Content.ContentHitsCollection).SelectMany(x => x.Hits).Where(x => x.Type == HitTypeEnum.CorrectName).ToList();
        var eligibleFiles = Games.Count * Settings.Cleaner.SelectedCheckContentTypes.Count;
        var missingFilesCount = eligibleFiles - validHits.Count;

        var detail = CreatePercentageStatistic("Missing Files", missingFilesCount, eligibleFiles);
        var isSuccess = missingFilesCount == 0;

        await (isSuccess ? Notification.ShowSuccess(DialogHostName, "All Files Are Good") : Notification.ShowWarning(DialogHostName, "Missing or Incorrect Files", null, detail));
    }

    private static string CreatePercentageStatistic(string title, int count, int totalCount)
    {
        var percentage = totalCount == 0 ? 0 : 100f * count / totalCount;
        return $"{title}:  {count} of {totalCount} ({percentage:F2}%)";
    }

    private const int WindowMargin = 0;

    private const string DialogHostName = "ResultsDialog";
}