using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ClrVpin.Controls;
using ClrVpin.Logging;
using ClrVpin.Models.Importer;
using ClrVpin.Models.Importer.Vps;
using ClrVpin.Models.Settings;
using ClrVpin.Models.Shared;
using ClrVpin.Models.Shared.Database;
using ClrVpin.Models.Shared.Game;
using ClrVpin.Shared;
using MaterialDesignThemes.Wpf;
using PropertyChanged;
using Utils;
using Utils.Extensions;

namespace ClrVpin.Importer
{
    public interface IOnlineGameCollections
    {
        List<string> Manufacturers { get; }
        List<string> Types { get; }
        List<string> Years { get; }
        List<int?> Players { get; }
        List<string> Roms { get; }
        List<string> Themes { get; }
        List<string> Authors { get; }
    }

    [AddINotifyPropertyChangedInterface]
    public class ImporterResultsViewModel : IOnlineGameCollections
    {
        public ImporterResultsViewModel(List<GameDetail> games, List<OnlineGame> onlineGames, ImporterMatchStatistics matchStatistics)
        {
            _games = games;
            _matchStatistics = matchStatistics.ToDictionary();
            IsMatchingEnabled = Model.Settings.Importer.SelectedMatchTypes.Any();

            // assign VM properties
            onlineGames.ForEach(onlineGame =>
            {
                // image - for showing dialog with larger view of image
                onlineGame.ImageUrlSelection = new UrlSelection
                {
                    Url = onlineGame.ImgUrl,
                    SelectedCommand = new ActionCommand(() => ShowImage(onlineGame.ImgUrl))
                };

                // local database show/add commands
                onlineGame.UpdateDatabaseEntryCommand = new ActionCommand(() => DatabaseItemManagement.UpdateDatabaseItem(games, onlineGame, this));
                onlineGame.CreateDatabaseEntryCommand = new ActionCommand(() => DatabaseItemManagement.CreateDatabaseItem(games, onlineGame, this));

                // show large image popup
                onlineGame.ImageFiles.ForEach(imageFile =>
                {
                    imageFile.ImageUrlSelection = new UrlSelection
                    {
                        Url = imageFile.ImgUrl,
                        SelectedCommand = new ActionCommand(() => ShowImage(imageFile.ImgUrl))
                    };
                });

                onlineGame.IsMatchingEnabled = IsMatchingEnabled;
                onlineGame.UpdateDatabaseEntryTooltip += onlineGame.IsMatchingEnabled ? "" : MatchingDisabledMessage;
                onlineGame.CreateDatabaseEntryTooltip += onlineGame.IsMatchingEnabled ? "" : MatchingDisabledMessage;

                onlineGame.YearString = onlineGame.Year.ToString();

                // extract IpdbId
                var match = _regexExtractIpdbId.Match(onlineGame.IpdbUrl ?? string.Empty);
                if (match.Success)
                    onlineGame.IpdbId = match.Groups["ipdbId"].Value;

                onlineGame.IsMatched = onlineGame.Hit != null;

                // navigate to url
                onlineGame.AllFiles.Select(x => x.Value).SelectMany(x => x).ForEach(file => { file.Urls.ForEach(url => url.SelectedCommand = new ActionCommand(() => NavigateToUrl(url.Url))); });
            });

            // main games view (data grid)
            OnlineGames = new ObservableCollection<OnlineGame>(onlineGames);
            OnlineGamesView = new ListCollectionView<OnlineGame>(OnlineGames)
            {
                // filter the table names list to reflect the various view filtering criteria
                Filter = game =>
                    (TableFilter == null || game.Name.Contains(TableFilter, StringComparison.OrdinalIgnoreCase)) &&
                    (ManufacturerFilter == null || game.Manufacturer.Contains(ManufacturerFilter, StringComparison.OrdinalIgnoreCase)) &&
                    (Settings.SelectedTableStyleOption == TableStyleOptionEnum.Both ||
                     (Settings.SelectedTableStyleOption == TableStyleOptionEnum.Manufactured && !game.IsOriginal) ||
                     (Settings.SelectedTableStyleOption == TableStyleOptionEnum.Original && game.IsOriginal)) &&
                    (Settings.SelectedTableMatchOption == TableMatchOptionEnum.Both ||
                     (Settings.SelectedTableMatchOption == TableMatchOptionEnum.Matched && game.Hit != null) ||
                     (Settings.SelectedTableMatchOption == TableMatchOptionEnum.Unmatched && game.Hit == null)) &&
                    (YearBeginFilter == null || string.Compare(game.YearString, YearBeginFilter, StringComparison.OrdinalIgnoreCase) >= 0) &&
                    (YearEndFilter == null || string.Compare(game.YearString, YearEndFilter, StringComparison.OrdinalIgnoreCase) <= 0) &&
                    (TypeFilter == null || game.Type?.Equals(TypeFilter, StringComparison.OrdinalIgnoreCase) == true) &&
                    (Settings.UpdatedAtDateBegin == null || game.UpdatedAt == null || game.UpdatedAt.Value >= Settings.UpdatedAtDateBegin) &&
                    (Settings.UpdatedAtDateEnd == null || game.UpdatedAt == null || game.UpdatedAt.Value < Settings.UpdatedAtDateEnd.Value.AddDays(1))
            };
            OnlineGamesView.MoveCurrentToFirst();

            // filters views (drop down combo boxes)
            TablesFilterView = new ListCollectionView<string>(onlineGames.Select(x => x.Name).Distinct().OrderBy(x => x).ToList())
            {
                // filter the table names list to reflect what's displayed in the games list, i.e. taking into account ALL of the existing filter criteria
                Filter = table => OnlineGamesView.Any(x => x.Name == table)
            };

            Manufacturers = onlineGames.Select(x => x.Manufacturer).Distinct().OrderBy(x => x).ToList();
            ManufacturersFilterView = new ListCollectionView<string>(Manufacturers)
            {
                // filter the table names list to reflect what's displayed in the games list, i.e. taking into account ALL of the existing filter criteria
                Filter = manufacturer => OnlineGamesView.Any(x => x.Manufacturer == manufacturer)
            };

            Years = onlineGames.Select(x => x.YearString).Distinct().Where(x => x != null).OrderBy(x => x).ToList();
            YearsBeginFilterView = new ListCollectionView<string>(Years)
            {
                // filter the table names list to reflect what's displayed in the games list, i.e. taking into account ALL of the existing filter criteria
                Filter = yearString => OnlineGamesView.Any(x => x.YearString == yearString)
            };

            YearsEndFilterView = new ListCollectionView<string>(Years)
            {
                // filter the table names list to reflect what's displayed in the games list, i.e. taking into account ALL of the existing filter criteria
                Filter = yearString => OnlineGamesView.Any(x => x.YearString == yearString)
            };

            Types = onlineGames.Select(x => x.Type).Distinct().Where(x => x != null).OrderBy(x => x).ToList();
            TypesFilterView = new ListCollectionView<string>(Types);

            Players = onlineGames.Select(x => x.Players).Distinct().Where(x => x != null).OrderBy(x => x).ToList();
            Roms = onlineGames.Select(x => x.RomFiles?.FirstOrDefault()?.Name).Distinct().Where(x => !string.IsNullOrEmpty(x)).OrderBy(x => x).ToList();
            Themes = onlineGames.Select(x => string.Join(", ", x.Themes)).Distinct().Where(x => !string.IsNullOrEmpty(x)).OrderBy(x => x).ToList();
            var tablesWithAuthors = onlineGames.Select(x => x.TableFiles.Select(table => string.Join(", ", table.Authors.OrderBy(author => author)))).SelectMany(x => x);
            Authors = tablesWithAuthors.Distinct().Where(x => !string.IsNullOrEmpty(x)).OrderBy(x => x).ToList();

            // generic handler for all the filter changes.. since all of the combo box values will need to be re-evaluated in sync anyway
            FilterChanged = new ActionCommand(() =>
            {
                // update main list
                OnlineGamesView.Refresh();

                // update filters based on what is shown in the main list
                TablesFilterView.Refresh();
                ManufacturersFilterView.Refresh();
                YearsBeginFilterView.Refresh();
                YearsEndFilterView.Refresh();
                TypesFilterView.Refresh();
            });

            UpdatedFilterChanged = new ActionCommand(() =>
            {
                UpdateIsNew();
                FilterChanged.Execute(null);
            });

            NavigateToIpdbCommand = new ActionCommand<string>(url => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }));

            UpdateIsNew();

            BackupFolder = Model.Settings.BackupFolder;
            NavigateToBackupFolderCommand = new ActionCommand(NavigateToBackupFolder);

            TableStyleOptionsView = new ListCollectionView<FeatureType>(CreateTableStyleOptions().ToList());
            TableMatchOptionsView = new ListCollectionView<FeatureType>(CreateTableMatchOptions(IsMatchingEnabled).ToList());

            AutoAssignDatabasePropertiesTip += "Update any missing information in your local database from online sources" + (IsMatchingEnabled ? "" : MatchingDisabledMessage);
            AutoAssignDatabasePropertiesCommand = new ActionCommand(AutoAssignDatabaseProperties);
        }

        public string AutoAssignDatabasePropertiesTip { get; }

        public ListCollectionView<FeatureType> TableStyleOptionsView { get; }
        public ListCollectionView<FeatureType> TableMatchOptionsView { get; }

        public string BackupFolder { get; }
        public ICommand NavigateToBackupFolderCommand { get; }

        public ImporterSettings Settings { get; } = Model.Settings.Importer;

        // todo; move filters into a separate class?
        public ListCollectionView<string> TablesFilterView { get; }
        public ListCollectionView<string> ManufacturersFilterView { get; }
        public ListCollectionView<string> YearsBeginFilterView { get; }
        public ListCollectionView<string> YearsEndFilterView { get; }
        public ListCollectionView<string> TypesFilterView { get; }

        public string TableFilter { get; set; }
        public string ManufacturerFilter { get; set; }
        public string YearBeginFilter { get; set; }
        public string YearEndFilter { get; set; }
        public string TypeFilter { get; set; }

        public ObservableCollection<OnlineGame> OnlineGames { get; }
        public ListCollectionView<OnlineGame> OnlineGamesView { get; }

        public Window Window { get; private set; }

        public OnlineGame SelectedOnlineGame { get; set; }

        public ICommand FilterChanged { get; set; }
        public ICommand UpdatedFilterChanged { get; set; }

        public ICommand NavigateToIpdbCommand { get; }
        public ICommand AutoAssignDatabasePropertiesCommand { get; }
        public bool IsMatchingEnabled { get; }


        // IOnlineGameCollections
        public List<string> Manufacturers { get; }
        public List<string> Types { get; }
        public List<string> Years { get; }
        public List<int?> Players { get; }
        public List<string> Roms { get; }
        public List<string> Themes { get; }
        public List<string> Authors { get; }

        public async Task Show(Window parentWindow, double left, double top)
        {
            Window = new MaterialWindowEx
            {
                Owner = parentWindow,
                Title = "Results",
                Left = left,
                Top = top,
                Width = Model.ScreenWorkArea.Width - left - WindowMargin,
                Height = (Model.ScreenWorkArea.Height - 10) * 0.73,
                Content = this,
                Resources = parentWindow.Resources,
                ContentTemplate = parentWindow.FindResource("ImporterResultsTemplate") as DataTemplate
            };

            Window.Show();

            await ShowResultSummary();
        }

        private async Task ShowResultSummary()
        {
            // simplified summary of the ImporterStatisticsViewModel info
            var onlineManufacturedCount = OnlineGames.Count(game => !game.IsOriginal);
            var onlineOriginalCount = OnlineGames.Count(game => game.IsOriginal);

            var detail = "Your collection.." +
                         CreatePercentageStatistic("Manufactured", _matchStatistics[ImporterMatchStatistics.MatchedManufactured], onlineManufacturedCount) +
                         CreatePercentageStatistic("Original", _matchStatistics[ImporterMatchStatistics.MatchedOriginal], onlineOriginalCount) +
                         $"\n- Unknown Tables:  {_matchStatistics[ImporterMatchStatistics.UnmatchedLocalTotal]}";

            var isSuccess = onlineManufacturedCount == _matchStatistics[ImporterMatchStatistics.MatchedManufactured];
            await (isSuccess ? Notification.ShowSuccess(DialogHostName, detail) : Notification.ShowWarning(DialogHostName, detail));
        }

        private static string CreatePercentageStatistic(string title, int count, int totalCount)
        {
            return $"\n- {title} Tables:  {count} of {totalCount} ({100f * count / totalCount:F2}%)";
        }

        public void Close()
        {
            Model.SettingsManager.Write();
            Window.Close();
        }

        private async void AutoAssignDatabaseProperties()
        {
            var matchedOnlineGames = OnlineGames.Where(x => x.Hit?.GameDetail != null).ToList();
            var updatedPropertyCounts = new Dictionary<string, int>
            {
                { nameof(Game.IpdbId), 0 },
                { nameof(Game.Author), 0 },
                { nameof(Game.Comment), 0 },
                { nameof(Game.Manufacturer), 0 },
                { nameof(Game.Players), 0 },
                { nameof(Game.Rom), 0 },
                { nameof(Game.Theme), 0 },
                { nameof(Game.Type), 0 },
                { nameof(Game.Year), 0 }
            };
            var updatedGameCount = 0;

            matchedOnlineGames.ForEach(onlineGame =>
            {
                var game = onlineGame.Hit.GameDetail.Game;
                var beforeCount = GetUpdateCount(updatedPropertyCounts);

                CheckAndFixMissingProperty(updatedPropertyCounts, game.Name, nameof(game.IpdbId), () => game.IpdbId, () => onlineGame.IpdbId, value => game.IpdbId = value);
                CheckAndFixMissingProperty(updatedPropertyCounts, game.Name, nameof(game.Author), () => game.Author, () => onlineGame.TableFiles.FirstOrDefault()?.Authors?.StringJoin(", "), value => game.Author = value);
                CheckAndFixMissingProperty(updatedPropertyCounts, game.Name, nameof(game.Comment), () => game.Comment, () => onlineGame.TableFiles.FirstOrDefault()?.Comment, value => game.Comment = value);
                CheckAndFixMissingProperty(updatedPropertyCounts, game.Name, nameof(game.Manufacturer), () => game.Manufacturer, () => onlineGame.Manufacturer, value => game.Manufacturer = value);
                CheckAndFixMissingProperty(updatedPropertyCounts, game.Name, nameof(game.Players), () => game.Players, () => onlineGame.Players?.ToString(), value => game.Players = value);
                CheckAndFixMissingProperty(updatedPropertyCounts, game.Name, nameof(game.Rom), () => game.Rom, () => onlineGame.RomFiles.FirstOrDefault()?.Name, value => game.Rom = value);
                CheckAndFixMissingProperty(updatedPropertyCounts, game.Name, nameof(game.Theme), () => game.Theme, () => onlineGame.Themes.StringJoin(", "), value => game.Theme = value);
                CheckAndFixMissingProperty(updatedPropertyCounts, game.Name, nameof(game.Type), () => game.Type, () => onlineGame.Type, value => game.Type = value);
                CheckAndFixMissingProperty(updatedPropertyCounts, game.Name, nameof(game.Year), () => game.Year, () => onlineGame.YearString, value => game.Year = value);

                updatedGameCount += beforeCount == GetUpdateCount(updatedPropertyCounts) ? 0 : 1;
            });

            // write ALL games back to the database(s) - i.e. irrespective of whether matched or not
            TableUtils.WriteGamesToDatabase(_games.Select(x => x.Game));

            var properties = updatedPropertyCounts.Select(property => $"- {property.Key}: {property.Value}").StringJoin("\n");
            var details = $"Tables analyzed: {matchedOnlineGames.Count}\n\n" +
                          $"Tables fixed: {updatedGameCount}\n\n" +
                          "Missing info fixed:\n" +
                          $"{properties}";
            await Notification.ShowSuccess(DialogHostName, details);

            Logger.Info($"Fixed missing info: table count: {updatedGameCount}, info count: {GetUpdateCount(updatedPropertyCounts)}");
        }

        private static int GetUpdateCount(IDictionary<string, int> updatedPropertyCounts) => updatedPropertyCounts.Sum(x => x.Value);

        private static void CheckAndFixMissingProperty(IDictionary<string, int> updatedPropertyCounts, string game, string property, Func<string> gameValue, Func<string> onlineGameValue, Action<string> assignAction)
        {
            if (gameValue().IsEmpty() && !onlineGameValue().IsEmpty())
            {
                assignAction(onlineGameValue());
                updatedPropertyCounts[property]++;

                Logger.Info($"Fixing missing info: table='{game}', {property}='{gameValue()}'");
            }
        }

        private IEnumerable<FeatureType> CreateTableStyleOptions()
        {
            // all table style options
            var featureTypes = StaticSettings.TableStyleOptions.Select(tableStyleOption =>
            {
                var featureType = new FeatureType((int)tableStyleOption.Enum)
                {
                    Tag = "TableStyleOption",
                    Description = tableStyleOption.Description,
                    Tip = tableStyleOption.Tip,
                    IsSupported = true,
                    IsActive = tableStyleOption.Enum == Model.Settings.Importer.SelectedTableStyleOption,
                    SelectedCommand = new ActionCommand(() =>
                    {
                        Model.Settings.Importer.SelectedTableStyleOption = tableStyleOption.Enum;
                        FilterChanged.Execute(null);
                    })
                };
                return featureType;
            }).ToList();

            return featureTypes;
        }

        private IEnumerable<FeatureType> CreateTableMatchOptions(bool isMatchingEnabled)
        {
            // because matching is disabled, all tables will be unmatched
            if (!isMatchingEnabled)
                Model.Settings.Importer.SelectedTableMatchOption = TableMatchOptionEnum.Unmatched;

            // all table match options
            var featureTypes = StaticSettings.TableMatchOptions.Select(tableMatchOption =>
            {
                var featureType = new FeatureType((int)tableMatchOption.Enum)
                {
                    Tag = "TableMatchOption",
                    Description = tableMatchOption.Description,
                    Tip = tableMatchOption.Tip,
                    IsSupported = tableMatchOption.Enum == TableMatchOptionEnum.Unmatched || isMatchingEnabled,
                    IsActive = tableMatchOption.Enum == Model.Settings.Importer.SelectedTableMatchOption,
                    SelectedCommand = new ActionCommand(() =>
                    {
                        Model.Settings.Importer.SelectedTableMatchOption = tableMatchOption.Enum;
                        FilterChanged.Execute(null);
                    })
                };
                if (!isMatchingEnabled && tableMatchOption.Enum != TableMatchOptionEnum.Unmatched)
                    featureType.Tip += featureType.Tip + MatchingDisabledMessage;

                return featureType;
            }).ToList();

            return featureTypes;
        }

        private void NavigateToBackupFolder() => Process.Start("explorer.exe", BackupFolder);

        private void UpdateIsNew()
        {
            // flag models if they satisfy the update time range
            OnlineGames.ForEach(game => game.AllFiles.ForEach(kv =>
            {
                var (_, files) = kv;
                files.ForEach(file =>
                {
                    // flag file - if the update time range is satisfied
                    file.IsNew = file.UpdatedAt >= (Settings.UpdatedAtDateBegin ?? DateTime.MinValue) && file.UpdatedAt <= (Settings.UpdatedAtDateEnd?.AddDays(1) ?? DateTime.Now);

                    // flag each url within the file - required to allow for simpler view binding
                    file.Urls.ForEach(url => url.IsNew = file.IsNew);
                });

                // flag file collection (e.g. backglasses)
                files.IsNew = files.Any(file => file.IsNew);
            }));
        }

        private static void NavigateToUrl(string url) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

        private static void ShowImage(string tableImgUrl)
        {
            var imageUrlSelection = new UrlSelection
            {
                Url = tableImgUrl,
                SelectedCommand = new ActionCommand(() => DialogHost.Close("ImporterResultsDialog"))
            };

            DialogHost.Show(imageUrlSelection, "ImporterResultsDialog");
        }

        private readonly Regex _regexExtractIpdbId = new Regex(@"http.?:\/\/www\.ipdb\.org\/machine\.cgi\?id=(?<ipdbId>\d*)$", RegexOptions.Compiled);
        private readonly List<GameDetail> _games;
        private readonly Dictionary<string, int> _matchStatistics;
        private const string DialogHostName = "ImporterResultsDialog";

        private const int WindowMargin = 0;
        private const string MatchingDisabledMessage = "... DISABLED BECAUSE MATCHING WASN'T USED";
    }
}