using System;
using System.Collections.Generic;
using System.Linq;
using ClrVpin.Logging;
using ClrVpin.Models.Importer.Vps;
using ClrVpin.Models.Shared.Database;
using Utils.Extensions;

namespace ClrVpin.Importer
{
    internal static class GameUpdater
    {
        public static (Dictionary<string, int> propertyStatistics, int updatedGameCount, int matchedGameCount) UpdateAllProperties(IEnumerable<OnlineGame> onlineGames, bool overwriteProperties)
        {
            var matchedOnlineGames = onlineGames.Where(x => x.Hit?.GameDetail != null).ToList();
            var propertyStatistics = CreatePropertyStatistics();
            var updatedGameCount = 0;

            matchedOnlineGames.ForEach(onlineGame =>
            {
                var beforeUpdatedPropertyCount = GetPropertiesUpdatedCount(propertyStatistics);

                CheckAndFix(onlineGame, onlineGame.Hit.GameDetail.Game, overwriteProperties, propertyStatistics);

                updatedGameCount += beforeUpdatedPropertyCount == GetPropertiesUpdatedCount(propertyStatistics) ? 0 : 1;
            });

            return (propertyStatistics, updatedGameCount, matchedOnlineGames.Count);
        }

        public static void CheckAndFix(OnlineGame onlineGame, Game game, bool overwrite, IDictionary<string, int> updatedPropertyCounts)
        {
            CheckAndFixProperty(overwrite, updatedPropertyCounts, game.Name, nameof(game.IpdbId), () => game.IpdbId, () => onlineGame.IpdbId, value => game.IpdbId = value);
            CheckAndFixProperty(overwrite, updatedPropertyCounts, game.Name, nameof(game.Author), () => game.Author, () => onlineGame.TableFiles.FirstOrDefault()?.Authors?.StringJoin(), value => game.Author = value);
            CheckAndFixProperty(overwrite, updatedPropertyCounts, game.Name, nameof(game.Comment), () => game.Comment, () => onlineGame.TableFiles.FirstOrDefault()?.Comment, value => game.Comment = value);
            CheckAndFixProperty(overwrite, updatedPropertyCounts, game.Name, nameof(game.Manufacturer), () => game.Manufacturer, () => onlineGame.Manufacturer, value => game.Manufacturer = value);
            CheckAndFixProperty(overwrite, updatedPropertyCounts, game.Name, nameof(game.Players), () => game.Players, () => onlineGame.Players?.ToString(), value => game.Players = value);
            CheckAndFixProperty(overwrite, updatedPropertyCounts, game.Name, nameof(game.Rom), () => game.Rom, () => onlineGame.RomFiles.FirstOrDefault()?.Name, value => game.Rom = value);
            CheckAndFixProperty(overwrite, updatedPropertyCounts, game.Name, nameof(game.Theme), () => game.Theme, () => onlineGame.Themes.StringJoin(), value => game.Theme = value);
            CheckAndFixProperty(overwrite, updatedPropertyCounts, game.Name, nameof(game.Type), () => game.Type, () => onlineGame.Type, value => game.Type = value);
            CheckAndFixProperty(overwrite, updatedPropertyCounts, game.Name, nameof(game.Year), () => game.Year, () => onlineGame.YearString, value => game.Year = value);
        }

        public static Dictionary<string, int> CreatePropertyStatistics() => new Dictionary<string, int>
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

        public static int GetPropertiesUpdatedCount(IDictionary<string, int> statistics) => statistics.Sum(x => x.Value);

        private static void CheckAndFixProperty(bool overwrite, IDictionary<string, int> updatedPropertyCounts, string game, 
            string property, Func<string> gameValue, Func<string> onlineGameValue, Action<string> assignAction)
        {
            if ((gameValue().IsEmpty() || overwrite) && !onlineGameValue().IsEmpty() && gameValue() != onlineGameValue())
            {
                assignAction(onlineGameValue());
                updatedPropertyCounts[property]++;

                Logger.Info($"Fixing missing info: table='{game}', {property}='{gameValue()}'");
            }
        }
    }
}
