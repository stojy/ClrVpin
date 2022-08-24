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
        public static (Dictionary<string, int> propertyStatistics, int updatedGameCount, int matchedGameCount) UpdateProperties(IEnumerable<OnlineGame> onlineGames, bool overwriteProperties)
        {
            var matchedOnlineGames = onlineGames.Where(x => x.Hit?.GameDetail != null).ToList();
            var propertyStatistics = CreatePropertyStatistics();
            var updatedGameCount = 0;

            matchedOnlineGames.ForEach(matchedOnlineGame =>
            {
                var updated = CheckAndFix(matchedOnlineGame, matchedOnlineGame.Hit.GameDetail.Game, overwriteProperties, propertyStatistics);

                updatedGameCount += updated ? 1 : 0;
            });

            return (propertyStatistics, updatedGameCount, matchedOnlineGames.Count);
        }

        // ReSharper disable once UnusedMethodReturnValue.Global
        public static (Dictionary<string, int> propertyStatistics, int updatedGameCount, int matchedGameCount) UpdateProperties(OnlineGame onlineGame, Game game, bool overwriteProperties)
        {
            var propertyStatistics = CreatePropertyStatistics();

            var updated = CheckAndFix(onlineGame, game, overwriteProperties, propertyStatistics);

            return (propertyStatistics, updated ? 1 : 0, 1);
        }
        
        public static bool CheckProperties(OnlineGame onlineGame, Game game, bool overwriteProperties)
        {
            var propertyStatistics = CreatePropertyStatistics();
            
            return CheckAndFix(onlineGame, game, overwriteProperties, propertyStatistics, true);
        }

        public static int GetPropertiesUpdatedCount(IDictionary<string, int> statistics) => statistics.Sum(x => x.Value);

        private static bool CheckAndFix(OnlineGame onlineGame, Game game, bool overwrite, IDictionary<string, int> propertyStatistics, bool skipUpdate = false)
        {
            var beforeUpdatedPropertyCount = GetPropertiesUpdatedCount(propertyStatistics);

            CheckAndFixProperty(overwrite, propertyStatistics, game.Name, nameof(game.Name), () => game.Name, () => onlineGame?.Description, value => game.Name = value, skipUpdate);
            CheckAndFixProperty(overwrite, propertyStatistics, game.Name, nameof(game.IpdbId), () => game.IpdbId, () => onlineGame?.IpdbId, value => game.IpdbId = value, skipUpdate);
            CheckAndFixProperty(overwrite, propertyStatistics, game.Name, nameof(game.Description), () => game.Description, () => onlineGame?.Description, value => game.Description = value, skipUpdate);
            CheckAndFixProperty(overwrite, propertyStatistics, game.Name, nameof(game.Author), () => game.Author, () => onlineGame?.TableFiles.FirstOrDefault()?.Authors?.StringJoin(), value => game.Author = value, skipUpdate);
            CheckAndFixProperty(overwrite, propertyStatistics, game.Name, nameof(game.Comment), () => game.Comment, () => onlineGame?.TableFiles.FirstOrDefault()?.Comment, value => game.Comment = value, skipUpdate);
            CheckAndFixProperty(overwrite, propertyStatistics, game.Name, nameof(game.Manufacturer), () => game.Manufacturer, () => onlineGame?.Manufacturer, value => game.Manufacturer = value, skipUpdate);
            CheckAndFixProperty(overwrite, propertyStatistics, game.Name, nameof(game.Players), () => game.Players, () => onlineGame?.Players?.ToString(), value => game.Players = value, skipUpdate);
            CheckAndFixProperty(overwrite, propertyStatistics, game.Name, nameof(game.Rom), () => game.Rom, () => onlineGame?.RomFiles.FirstOrDefault()?.Name, value => game.Rom = value, skipUpdate);
            CheckAndFixProperty(overwrite, propertyStatistics, game.Name, nameof(game.Theme), () => game.Theme, () => onlineGame?.Themes.StringJoin(), value => game.Theme = value, skipUpdate);
            CheckAndFixProperty(overwrite, propertyStatistics, game.Name, nameof(game.Type), () => game.Type, () => onlineGame?.Type, value => game.Type = value, skipUpdate);
            CheckAndFixProperty(overwrite, propertyStatistics, game.Name, nameof(game.Year), () => game.Year, () => onlineGame?.YearString, value => game.Year = value, skipUpdate);

            return beforeUpdatedPropertyCount != GetPropertiesUpdatedCount(propertyStatistics);
        }

        private static Dictionary<string, int> CreatePropertyStatistics() => new Dictionary<string, int>
        {
            { nameof(Game.Name), 0 },
            { nameof(Game.IpdbId), 0 },
            { nameof(Game.Description), 0 },
            { nameof(Game.Author), 0 },
            { nameof(Game.Comment), 0 },
            { nameof(Game.Manufacturer), 0 },
            { nameof(Game.Players), 0 },
            { nameof(Game.Rom), 0 },
            { nameof(Game.Theme), 0 },
            { nameof(Game.Type), 0 },
            { nameof(Game.Year), 0 }
        };

        private static void CheckAndFixProperty(bool overwrite, IDictionary<string, int> updatedPropertyCounts, string game,
            string property, Func<string> gameValue, Func<string> onlineGameValue, Action<string> updateAction, bool skipUpdate)
        {
            if (gameValue() != onlineGameValue() &&     // values must be different
                (gameValue().IsEmpty() || overwrite) && // local DB entry can be null OR overwrite option requested
                !onlineGameValue().IsEmpty())           // don't overwrite local DB with a null online value

            {
                if (!skipUpdate)
                {
                    Logger.Info($"Fixing missing info: table='{game}', {property}='{gameValue()}'");
                    updateAction(onlineGameValue());
                }

                updatedPropertyCounts[property]++;

            }
        }
    }
}