using ClrVpin.Logging;
using ClrVpin.Models.Importer.Vps;
using MaterialDesignThemes.Wpf;

namespace ClrVpin.Importer
{
    public static class DatabaseItemManagement
    {
        public static async void ShowDatabaseItem(OnlineGame onlineGame, IOnlineGameCollections onlineGameCollections)
        {
            var item = new DatabaseItem(onlineGame.Hit.Game, onlineGameCollections, true);

            var result = await DialogHost.Show(item, "ImporterResultsDialog") as DatabaseItemAction?;

            Logger.Info($"Database Item: action={result}");
        }

        public static void AddDatabaseItem(OnlineGame onlineGame)
        {
            // todo; create new entry
            //DialogHost.Show(onlineGame.Hit.Game, "ImporterResultsDialog");
        }
    }
}
