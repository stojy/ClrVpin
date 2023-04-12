using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using ClrVpin.Converters;
using ClrVpin.Logging;
using ClrVpin.Models.Feeder;
using ClrVpin.Models.Feeder.Vps;
using ClrVpin.Models.Shared.Game;
using ClrVpin.Shared.Fuzzy;
using Utils.Extensions;

namespace ClrVpin.Feeder;

public static class FeederUtils
{
    static FeederUtils()
    {
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new UnixToNullableDateTimeConverter { IsFormatInSeconds = false } }
        };
    }

    public static async Task MatchOnlineToLocalAsync(List<LocalGame> localGames, List<OnlineGame> onlineGames, Action<string, float?> updateProgress)
    {
        await Task.Run(() => MatchOnlineToLocal(localGames, onlineGames, updateProgress));
    }

    public static async Task<IList<GameItem>> MergeOnlineAndLocalGamesAsync(List<LocalGame> localGames, List<OnlineGame> onlineGames, Action<string, float?> updateProgress)
    {
        return await Task.Run(() => MergeLocalAndOnlineGames(localGames, onlineGames, updateProgress));
    }

    public static async Task<List<OnlineGame>> ReadGamesFromOnlineDatabase()
    {
        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(60),
            MaxResponseContentBufferSize = 10 * 1024 * 1024 // 10MB
        };

        var onlineGames = (await httpClient.GetFromJsonAsync<OnlineGame[]>(VisualPinballSpreadsheetDatabaseUrl, _jsonSerializerOptions))!.ToList();

        Logger.Info($"Online database pre-fix:  count={onlineGames.Count} (manufactured={onlineGames.Count(onlineGame => !GameDerived.CheckIsOriginal(onlineGame.Manufacturer, onlineGame.Name))}, " +
                    $"original={onlineGames.Count(onlineGame => GameDerived.CheckIsOriginal(onlineGame.Manufacturer, onlineGame.Name))})");

        return onlineGames;
    }

    public static OnlineGame GetUniqueGame(List<OnlineGame> onlineGames)
    {
        // create cleansed list of names ordered in ascending size
        var cleansed = onlineGames.Select(x =>
        {
            var cleanName = x.Name.Trim();

            return new
            {
                name = x.Name,
                cleanName
            };
        }).OrderBy(x => x.name.Length);

        // unique game is the first in the list item in the cleansed list 
        var uniqueGame = onlineGames.First(onlineGame => onlineGame.Name == cleansed.First().name);

        return uniqueGame;
    }

    private static void MatchOnlineToLocal(IList<LocalGame> localGames, ICollection<OnlineGame> onlineGames, Action<string, float?> updateProgress)
    {
        onlineGames.ForEach((onlineGame, i) =>
        {
            updateProgress(onlineGame.Name, (i + 1f) / onlineGames.Count);

            // todo; perform an exact match check to potentially avoid the need for the more expensive fuzzy match check
            // - similar to DatabaseUtils.MatchFilesToLocal() uses since VPX/PBY mandates that the entries must be the same!
            // - a LOT faster!!


            // unlike merger matching, only fuzzy is used

            // unlike cleaner/merger..
            // - we already have the manufacturer and year breakdowns, so we can skip the parsing step and assign them directly instead
            // - use GetTableDetails for consistency and assign some properties, but then override with the known values (from the feed) directly
            var fullName = $"{onlineGame.Name} ({onlineGame.Manufacturer} {onlineGame.Year})";
            var fuzzyNameDetails = Fuzzy.GetTableDetails(fullName, false);
            fuzzyNameDetails.Manufacturer = onlineGame.Manufacturer;
            fuzzyNameDetails.Year = onlineGame.Year;

            var (localMatchedGame, score, isMatch) = localGames.MatchToLocalDatabase(fuzzyNameDetails, false);
            if (isMatch)
            {
                // would it be more efficient to match to the online games instead of the reverse.. matching to local database
                // - ensure a specified local game will only have at most 1 online match (the best match( match exists for every local game).. i.e. no need for the 'is higher scoring' handling below
                // - since only 1 online game can be chosen, we could progressively reduce the 'pool of online games' as candidates by filtering out those that are already matched
                // - BUT, it would require some reworking of Fuzzy.MatchToLocalDatabase() to accommodate

                // check to see if the local game has already been matched to an online game
                var existingMatchOnlineGame = onlineGames.FirstOrDefault(online => online.Hit?.LocalGame == localMatchedGame);
                if (existingMatchOnlineGame != null)
                {
                    var replaceExistingMatch = existingMatchOnlineGame.Hit.Score < score;

                    var isOriginal = existingMatchOnlineGame.IsOriginal || localMatchedGame.Derived.IsOriginal || fuzzyNameDetails.IsOriginal;
                    var existingFullName = $"{existingMatchOnlineGame.Name} ({existingMatchOnlineGame.Manufacturer} {existingMatchOnlineGame.Year})";

                    var fuzzyLog = $"duplicate fuzzy match: replaceExisting={replaceExistingMatch}, isOriginal={isOriginal}\n" +
                                   $"- db record:                      {Fuzzy.LogGameInfo(localMatchedGame.Game.Name, localMatchedGame.Game.Description, localMatchedGame.Game.Manufacturer, localMatchedGame.Game.Year)}\n" +
                                   $"- existing feed match: score={$"{existingMatchOnlineGame.Hit.Score},",-4} {Fuzzy.LogGameInfo(existingFullName, null, existingMatchOnlineGame.Manufacturer, existingMatchOnlineGame.YearString)}\n" +
                                   $"- new feed match:      score={$"{score},",-4} {Fuzzy.LogGameInfo(fuzzyNameDetails.ActualName, null, fuzzyNameDetails.Manufacturer, fuzzyNameDetails.Year?.ToString())}";

                    if (!(isOriginal && Model.Settings.SkipLoggingForOriginalTables))
                        Logger.Info(fuzzyLog, true);

                    // if the new match has a greater score..
                    // - Yes = remove the previous hit for the SAME game since it must be wrong
                    //        e.g. onlineGame=Apache initially matches against localLocalGame=Apache! because localDB does not have a 'Apache' game
                    //        .. but subsequently matches higher to onlineGame=Apache! as expected given this is the better (aka correct) match
                    // - No = ignore the match completely since a better match was already found
                    //        e.g. onlineGame=Apache and onlineGame=Apache! should NOT both match to the SAME localLocalGame
                    if (replaceExistingMatch)
                    {
                        // remove match and adjust statistics
                        RemoveMatch(existingMatchOnlineGame);

                        // add new match
                        AddMatch(onlineGame, localMatchedGame, score);
                    }
                    // else.. ignore match - adjust statistics as if there was no match detected.. i.e. a lesser match to the SAME local DB game == effectively not a match at all
                }
                else
                {
                    AddMatch(onlineGame, localMatchedGame, score);
                }
            }
        });
    }

    private static void RemoveMatch(OnlineGame onlineGame)
    {
        onlineGame.Hit = null;
    }

    private static void AddMatch(OnlineGame onlineGame, LocalGame localMatchedLocalGame, int? score)
    {
        // link both entities together so they can be referenced from all perspectives.. matched, unmatched, and missing (refer TableMatchOptionEnum)
        onlineGame.Hit = new LocalGameHit
        {
            LocalGame = localMatchedLocalGame,
            Score = score
        };

        localMatchedLocalGame.OnlineGame = onlineGame;
    }

    private static List<GameItem> MergeLocalAndOnlineGames(IEnumerable<LocalGame> localGames, ICollection<OnlineGame> onlineGames, Action<string, float?> updateProgress)
    {
        // the earlier 'online to local' matching has already determined the matches.. so no need to redo it again
        // - deliberately NOT performing a 'reverse' fuzzy lookup to avoid scenario where x1 online game could have multiple local files
        // - e.g. online only has 1 AC/DC entry (which is a known issue).. whereas there are multiple local files each representing the unique IPDBs (which is correct)
        var localOnlyGames = localGames.Except(onlineGames.Where(onlineGame => onlineGame.Hit != null).Select(onlineGame => onlineGame.Hit.LocalGame)).ToList();

        // merge online and local games into a single collection of GameItem
        var onlineGameItems = onlineGames.Select(onlineGame => new GameItem(onlineGame));
        var localOnlyGameItems = localOnlyGames.Select(localOnlyGameDetail => new GameItem(localOnlyGameDetail));

        var allGameItems = onlineGameItems.Concat(localOnlyGameItems).OrderBy(item => item.Name).ToList();
        allGameItems.ForEach((gameItem, index) => gameItem.Index = index + 1);

        // logging - missing games
        var missingGames = onlineGames
            .Where(onlineGame => !onlineGame.IsOriginal && onlineGame.TableFormats.Contains("VPX") && onlineGame.TableAvailability == TableAvailabilityOptionEnum.Available && onlineGame.Hit == null)
            .OrderBy(onlineGame => onlineGame.Name)
            .ToList();
        Logger.Info($"Fuzzy matching: missing table count={missingGames.Count} (only exists in the online feed.. restricted to tables that are manufactured, VPX, and available for download)");
        missingGames.ForEach(missingGame => Logger.Debug($"- missing table: '{missingGame.Description}'"));

        // logging - unmatched games
        Logger.Info($"Fuzzy matching: unmatched table count={localOnlyGames.Count} (only exists in the local database.. unrestricted to include manufactured and original tables)");
        localOnlyGames.OrderBy(localGame => localGame.Game.Name).ForEach(onlyLocalGame =>
        {
            updateProgress(onlyLocalGame.Game.Name, null);
            Logger.Debug($"- unmatched table: '{onlyLocalGame.Game.Name}'");
        });
        
        return allGameItems;
    }

    // refer https://github.com/Fraesh/vps-db, https://virtual-pinball-spreadsheet.web.app/
    private const string VisualPinballSpreadsheetDatabaseUrl = "https://raw.githubusercontent.com/Fraesh/vps-db/master/vpsdb.json";

    private static readonly JsonSerializerOptions _jsonSerializerOptions;
}