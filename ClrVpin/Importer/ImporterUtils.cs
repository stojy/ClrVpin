using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using ClrVpin.Converters;
using ClrVpin.Logging;
using ClrVpin.Models.Importer.Vps;
using ClrVpin.Models.Shared.Game;
using ClrVpin.Shared.Fuzzy;
using Utils.Extensions;

namespace ClrVpin.Importer;

public static class ImporterUtils
{
    static ImporterUtils()
    {
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new UnixToNullableDateTimeConverter { IsFormatInSeconds = false } }
        };
    }

    public static async Task<ImporterMatchStatistics> MatchOnlineToLocalAsync(List<GameDetail> games, List<OnlineGame> onlineGames, Action<string, float?> updateProgress)
    {
        return await Task.Run(() => MatchOnlineToLocal(games, onlineGames, updateProgress));
    }

    public static async Task MatchLocalToOnlineAsync(List<GameDetail> games, List<OnlineGame> onlineGames, ImporterMatchStatistics matchStatistics, Action<string, float?> updateProgress)
    {
        await Task.Run(() => MatchLocalToOnline(games, onlineGames, matchStatistics, updateProgress));
    }

    public static async Task<List<OnlineGame>> GetOnlineDatabase()
    {
        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(60),
            MaxResponseContentBufferSize = 10 * 1024 * 1024 // 10MB
        };

        var onlineGames = (await httpClient.GetFromJsonAsync<OnlineGame[]>(VisualPinballSpreadsheetDatabaseUrl, _jsonSerializerOptions))!.ToList();

        Logger.Info($"Online database tables:  count={onlineGames.Count} (manufactured={onlineGames.Count(onlineGame => !onlineGame.IsOriginal)}, original={onlineGames.Count(onlineGame => onlineGame.IsOriginal)})");

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

    private static ImporterMatchStatistics MatchOnlineToLocal(IList<GameDetail> games, ICollection<OnlineGame> onlineGames, Action<string, float?> updateProgress)
    {
        var matchStatistics = new ImporterMatchStatistics();

        onlineGames.ForEach((onlineGame, i) =>
        {
            updateProgress(onlineGame.Name, (i + 1f) / onlineGames.Count);

            // unlike rebuilder matching, only fuzzy is used

            // unlike scanner/rebuilder we already have the manufacturer and year breakdowns, so we can skip the parsing step and assign them directly instead
            // - use GetNameDetails for consistency and assign some properties, but then override with the known values (from the feed) directly
            var fullName = $"{onlineGame.Name} ({onlineGame.Manufacturer} {onlineGame.Year})";
            var fuzzyNameDetails = Fuzzy.GetNameDetails(fullName, false);
            fuzzyNameDetails.ActualName = fullName; // overriden in order to maintain the correct capitalization
            fuzzyNameDetails.Manufacturer = onlineGame.Manufacturer;
            fuzzyNameDetails.Year = onlineGame.Year;

            var (matchedGame, score, isMatch) = games.Match(fuzzyNameDetails, false);
            if (isMatch)
            {
                var existingMatchOnlineGame = onlineGames.FirstOrDefault(online => online.Hit?.GameDetail == matchedGame);
                if (existingMatchOnlineGame != null)
                {
                    var replaceExistingMatch = existingMatchOnlineGame.Hit.Score < score;

                    var isOriginal = existingMatchOnlineGame.IsOriginal || matchedGame.Derived.IsOriginal || fuzzyNameDetails.IsOriginal;
                    var existingFullName = $"{existingMatchOnlineGame.Name} ({existingMatchOnlineGame.Manufacturer} {existingMatchOnlineGame.Year})";

                    var fuzzyLog = $"duplicate fuzzy match: replaceExisting={replaceExistingMatch}, isOriginal={isOriginal}\n" +
                                   $"- db record:                      {Fuzzy.LogGameDetail(matchedGame.Game.Name, matchedGame.Game.Description, matchedGame.Game.Manufacturer, matchedGame.Game.Year)}\n" +
                                   $"- existing feed match: score={$"{existingMatchOnlineGame.Hit.Score},",-4} {Fuzzy.LogGameDetail(existingFullName, null, existingMatchOnlineGame.Manufacturer, existingMatchOnlineGame.YearString)}\n" +
                                   $"- new feed match:      score={$"{score},",-4} {Fuzzy.LogGameDetail(fuzzyNameDetails.ActualName, null, fuzzyNameDetails.Manufacturer, fuzzyNameDetails.Year?.ToString())}";

                    if (!(isOriginal && Model.Settings.SkipLoggingForOriginalTables))
                        Logger.Info(fuzzyLog, true);

                    // if the new match has a greater score..
                    // - Yes = remove the previous hit for the SAME game since it must be wrong
                    //        e.g. onlineGame=Apache initially matches against localGame=Apache! because localDB does not have a 'Apache' game
                    //        .. but subsequently matches higher to onlineGame=Apache! as expected given this is the better (aka correct) match
                    // - No = ignore the match completely since a better match was already found
                    //        e.g. onlineGame=Apache and onlineGame=Apache! should NOT both match to the SAME localGame
                    if (replaceExistingMatch)
                    {
                        // remove match and adjust statistics
                        RemoveMatch(existingMatchOnlineGame);
                        DecrementMatchedStatistics(matchStatistics, existingMatchOnlineGame);
                        IncrementUnmatchedStatistics(matchStatistics, existingMatchOnlineGame);

                        // add new match
                        AddMatch(onlineGame, matchedGame, score);
                    }
                    else
                    {
                        // ignore match - adjust statistics as if there was no match detected.. i.e. a lesser match to the SAME local DB game == effectively not a match at all
                        IncrementUnmatchedStatistics(matchStatistics, onlineGame);
                    }
                }
                else
                {
                    AddMatch(onlineGame, matchedGame, score);
                    IncrementMatchedStatistics(matchStatistics, onlineGame);
                }
            }
            else
            {
                IncrementUnmatchedStatistics(matchStatistics, onlineGame);
            }
        });

        return matchStatistics;
    }

    private static void IncrementMatchedStatistics(ImporterMatchStatistics matchStatistics, OnlineGame onlineGame)
    {
        matchStatistics.Increment(ImporterMatchStatistics.MatchedTotal);
        matchStatistics.Increment(onlineGame.IsOriginal ? ImporterMatchStatistics.MatchedOriginal : ImporterMatchStatistics.MatchedManufactured);
    }

    private static void DecrementMatchedStatistics(ImporterMatchStatistics matchStatistics, OnlineGame onlineGame)
    {
        matchStatistics.Decrement(ImporterMatchStatistics.MatchedTotal);
        matchStatistics.Decrement(onlineGame.IsOriginal ? ImporterMatchStatistics.MatchedOriginal : ImporterMatchStatistics.MatchedManufactured);
    }

    private static void IncrementUnmatchedStatistics(ImporterMatchStatistics matchStatistics, OnlineGame onlineGame)
    {
        matchStatistics.Increment(ImporterMatchStatistics.UnmatchedOnlineTotal);
        matchStatistics.Increment(onlineGame.IsOriginal ? ImporterMatchStatistics.UnmatchedOnlineOriginal : ImporterMatchStatistics.UnmatchedOnlineManufactured);
    }

    private static void RemoveMatch(OnlineGame onlineGame)
    {
        onlineGame.Hit = null;
    }

    private static void AddMatch(OnlineGame onlineGame, GameDetail matchedGame, int? score)
    {
        onlineGame.Hit = new GameHit
        {
            GameDetail = matchedGame,
            Score = score
        };
    }

    private static void MatchLocalToOnline(IEnumerable<GameDetail> gameDetails, IEnumerable<OnlineGame> onlineGames, ImporterMatchStatistics matchStatistics, Action<string, float?> updateProgress)
    {
        var unmatchedGameDetails = gameDetails.Except(onlineGames.Where(onlineGame => onlineGame.Hit != null).Select(onlineGame => onlineGame.Hit.GameDetail)).ToList();

        // deliberately NOT performing a 'reverse' fuzzy lookup to avoid scenario where x1 online game could have multiple local files
        // - e.g. online only has 1 AC/DC entry (which is a known issue).. whereas there are multiple local files each representing the unique IPDBs (which is correct)
        unmatchedGameDetails.ForEach(localGameDetail =>
        {
            updateProgress(localGameDetail.Game.Name, null);

            Logger.Info($"Unmatched local table: '{localGameDetail.Game.Name}'");

            matchStatistics.Increment(ImporterMatchStatistics.UnmatchedLocalTotal);
            matchStatistics.Increment(localGameDetail.Derived.IsOriginal ? ImporterMatchStatistics.UnmatchedLocalOriginal : ImporterMatchStatistics.UnmatchedLocalManufactured);
        });
    }

    // refer https://github.com/Fraesh/vps-db, https://virtual-pinball-spreadsheet.web.app/
    private const string VisualPinballSpreadsheetDatabaseUrl = "https://raw.githubusercontent.com/Fraesh/vps-db/master/vpsdb.json";

    private static readonly JsonSerializerOptions _jsonSerializerOptions;
}