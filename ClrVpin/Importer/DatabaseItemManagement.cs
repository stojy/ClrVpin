using System.Collections.Generic;
using ClrVpin.Logging;
using ClrVpin.Models.Importer.Vps;
using ClrVpin.Models.Shared.Database;
using ClrVpin.Shared;
using MaterialDesignThemes.Wpf;

namespace ClrVpin.Importer
{
    public static class DatabaseItemManagement
    {
        public static async void ViewDatabaseItem(List<Game> games, OnlineGame onlineGame, IOnlineGameCollections onlineGameCollections)
        {
            var item = new DatabaseItem(onlineGame.Hit.Game, onlineGameCollections, true);

            var result = await DialogHost.Show(item, "ImporterResultsDialog") as DatabaseItemAction?;
            if (result == DatabaseItemAction.Update)
            {
                // replace the older game with the updated game
                var gameIndex = games.FindIndex(g => g == onlineGame.Hit.Game);
                if (gameIndex >= 0)
                    games[gameIndex] = item.Game;

                // replace online game reference to the updated game
                onlineGame.Hit.Game = item.Game;

                TableUtils.WriteGamesToDatabase(games);
            }

            Logger.Info($"Database Item: action={result}");
        }

        public static void AddDatabaseItem(OnlineGame onlineGame)
        {
            // todo; create new entry
            //DialogHost.Show(onlineGame.Hit.Game, "ImporterResultsDialog");
        }
    }
}
