using System.Text.Json;
using ClrVpin.Models.Shared.Database;
using Microsoft.Xaml.Behaviors.Core;
using PropertyChanged;
using Utils.Extensions;

namespace ClrVpin.Importer
{
    [AddINotifyPropertyChangedInterface]
    public class DatabaseItem
    {
        public DatabaseItem(Game game, bool isExisting)
        {
            // clone game details so that..
            // - changes can be discarded if required, i.e. not saved
            // - allow object comparison for serialization (ignoring a few VM properties)

            var initialSerializedGame = JsonSerializer.Serialize(game.Clone());

            Game = game.Clone();
            IsExisting = isExisting;
            IsChanged = false;

            Game.ChangedCommand = new ActionCommand(() => IsChanged = !Game.IsEqual(initialSerializedGame));
        }

        public Game Game { get; }
        public bool IsExisting { get; set; }
        public bool IsChanged { get; set; }
    }
}
