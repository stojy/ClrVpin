using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ClrVpin.Controls;
using ClrVpin.Models.Settings;
using ClrVpin.Models.Shared;
using ClrVpin.Models.Shared.Game;
using ClrVpin.Shared;
using PropertyChanged;
using Utils.Extensions;

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

    public ExplorerSettings Settings { get; private set; }
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

    public void UpdateCollections()
    {
        // the collections consist of all the possible permutations from BOTH the online source and the local source
        // - this is to ensure the maximum possible options are presented AND that the active item (from the local DB in the case of the update dialog) is actually in the list,
        //   otherwise it will be assigned to null via the ListCollectionView when the SelectedItem is assigned (either explicitly or via binding)

        // filters views (drop down combo boxes) - uses the online AND unmatched local DB 
        var tableNames = Games.Select(x => x.Game.Name).SelectUnique();
        //    TablesFilterView = new ListCollectionView<string>(tableNames)
        //    {
        //        // filter the table names list to reflect what's displayed in the games list, i.e. taking into account ALL of the existing filter criteria
        //        Filter = table => Filter(() => GameItemsView.Any(x => x.Name == table))
        //    };

        //    Manufacturers = GameItems.Select(x => x.Manufacturers).SelectManyUnique();
        //    ManufacturersFilterView = new ListCollectionView<string>(Manufacturers)
        //    {
        //        // filter the table names list to reflect what's displayed in the games list, i.e. taking into account ALL of the existing filter criteria
        //        Filter = manufacturer => Filter(() => GameItemsView.Any(x => x.Manufacturer == manufacturer))
        //    };

        //    Years = GameItems.Select(x => x.Years).SelectManyUnique();
        //    YearsBeginFilterView = new ListCollectionView<string>(Years)
        //    {
        //        // filter the table names list to reflect what's displayed in the games list, i.e. taking into account ALL of the existing filter criteria
        //        Filter = yearString => Filter(() => GameItemsView.Any(x => x.Year == yearString))
        //    };
        //    YearsEndFilterView = new ListCollectionView<string>(Years)
        //    {
        //        // filter the table names list to reflect what's displayed in the games list, i.e. taking into account ALL of the existing filter criteria
        //        Filter = yearString => Filter(() => GameItemsView.Any(x => x.Year == yearString))
        //    };

        //    // table HW type, i.e. SS, EM, PM
        //    Types = GameItems.Select(x => x.Types).SelectManyUnique();
        //    TypesFilterView = new ListCollectionView<string>(Types)
        //    {
        //        Filter = type => Filter(() => GameItemsView.Any(x => x.Type == type))
        //    };

        //// table formats - vpx, fp, etc
        //// - only available via online
        //    Formats = GameItems.SelectMany(x => x.OnlineGame?.TableFormats ?? new List<string>()).Distinct().Where(x => x != null).OrderBy(x => x).ToList();
        //    FormatsFilterView = new ListCollectionView<string>(Formats)
        //    {
        //        Filter = format => Filter(() => GameItemsView.Any(x => x.OnlineGame?.TableFormats.Contains(format) == true))
        //    };

        //    Themes = GameItems.Select(x => x.Themes).SelectManyUnique();
        //    Players = GameItems.Select(x => x.Players).SelectManyUnique();
        //Roms = GameItems.Select(x => x.Roms).SelectManyUnique();
        //    Authors = GameItems.Select(x => x.Authors).SelectManyUnique();
    }

    private void Initialise()
    {
        Settings = Model.Settings.Explorer;

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

    private bool Filter(Func<bool> dynamicFilteringFunc) =>
        // only evaluate the func if dynamic filtering is enabled
        !Settings.IsDynamicFiltering || dynamicFilteringFunc();


    private async Task ShowSummary()
    {
        var validHits = Games.SelectMany(x => x.Content.ContentHitsCollection).SelectMany(x => x.Hits).Where(x => x.Type == HitTypeEnum.CorrectName).ToList();
        var eligibleFiles = Games.Count * Model.Settings.AllContentTypes.Count;
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