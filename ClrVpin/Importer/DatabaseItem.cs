using System.Text.Json;
using ClrVpin.Controls;
using ClrVpin.Models.Shared.Database;
using ClrVpin.Shared;
using Microsoft.Xaml.Behaviors.Core;
using PropertyChanged;
using Utils.Extensions;

namespace ClrVpin.Importer
{
    [AddINotifyPropertyChangedInterface]
    public class DatabaseItem
    {
        public DatabaseItem(Game game, IOnlineGameCollections onlineGameCollections, bool isExisting)
        {
            // clone game details so that..
            // - changes can be discarded if required, i.e. not saved
            // - allow object comparison for serialization (ignoring a few VM properties)

            var initialSerializedGame = JsonSerializer.Serialize(game.Clone());

            Game = game.Clone();
            IsExisting = isExisting;
            IsChanged = false;

            Game.ManufacturersView = new ListCollectionView<string>(onlineGameCollections.Manufacturers);
            Game.YearsView = new ListCollectionView<string>(onlineGameCollections.Years);
            Game.TypesView = new ListCollectionView<string>(onlineGameCollections.Types);
            Game.RomsView = new ListCollectionView<string>(onlineGameCollections.Roms);
            Game.PlayersView = new ListCollectionView<int?>(onlineGameCollections.Players);
            Game.ThemesView = new ListCollectionView<string>(onlineGameCollections.Themes);
            Game.AuthorsView = new ListCollectionView<string>(onlineGameCollections.Authors);
            Game.RatingsView = new ListCollectionView<double?>(onlineGameCollections.Ratings);
            
            Game.ChangedCommand = new ActionCommand(() =>
            {
                // explicitly recalculate dynamic VM properties
                TableUtils.UpdateGameProperties(Game);

                // indicate whether anything has changed
                IsChanged = !Game.IsEqual(initialSerializedGame);
            });
        }

        public Game Game { get; }
        public bool IsExisting { get; set; }
        public bool IsChanged { get; set; }
    }
}
