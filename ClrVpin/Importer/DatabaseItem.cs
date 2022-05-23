using ClrVpin.Models.Shared.Database;

namespace ClrVpin.Importer
{
    public class DatabaseItem
    {
        public DatabaseItem(Game game, bool isExisting)
        {
            Game = game;
            IsExisting = isExisting;
        }

        public Game Game { get; set; }
        public bool IsExisting { get; set; }
    }
}
