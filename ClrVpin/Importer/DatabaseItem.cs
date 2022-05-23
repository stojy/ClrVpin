using ClrVpin.Logging;
using ClrVpin.Models.Importer.Vps;
using MaterialDesignThemes.Wpf;
using Utils.Extensions;

namespace ClrVpin.Importer
{
    public static class DatabaseItem
    {
        public static async void ShowDatabaseItem(OnlineGame onlineGame)
        {
            // copy game details so that changes can be discarded if required, i.e. not saved
            var game = onlineGame.Hit.Game.Clone();

            var result = await DialogHost.Show(game, "ImporterResultsDialog") as DatabaseItemAction?;

            Logger.Info($"Database Item: action={result}");
        }

        public static void AddDatabaseItem(OnlineGame onlineGame)
        {
            // todo; create new entry
            //DialogHost.Show(onlineGame.Hit.Game, "ImporterResultsDialog");
        }

    }
}
