using System.Collections.Generic;
using System.Linq;
using ClrVpin.Logging;
using ClrVpin.Models.Importer.Vps;
using ClrVpin.Models.Shared.Database;
using ClrVpin.Shared;
using MaterialDesignThemes.Wpf;

namespace ClrVpin.Importer
{
    public static class DatabaseItemManagement
    {
        public static async void ViewDatabaseItem(List<GameDetail> gameDetails, OnlineGame onlineGame, IOnlineGameCollections onlineGameCollections)
        {
            var item = new DatabaseItem(onlineGame.Hit.GameDetail, onlineGameCollections, true);

            var result = await DialogHost.Show(item, "ImporterResultsDialog") as DatabaseItemAction?;
            if (result == DatabaseItemAction.Update)
            {
                // replace the out of date game details
                var existingGameDetail = gameDetails.First(g => g == onlineGame.Hit.GameDetail);
                existingGameDetail.Game = item.GameDetail.Game;
                existingGameDetail.Derived = item.GameDetail.Derived;
                
                TableUtils.WriteGamesToDatabase(gameDetails);
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
