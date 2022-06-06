using System;
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
        private bool _loaded;

        public DatabaseItem(Game originalGame, IOnlineGameCollections onlineGameCollections, bool isExisting)
        {
            // clone game details so that..
            // - changes can be discarded if required, i.e. not saved
            // - allow object comparison for serialization (ignoring a few VM properties)
            var initialSerializedGame = JsonSerializer.Serialize(originalGame.Clone());

            Game = originalGame.Clone();
            IsExisting = isExisting;
            IsChanged = false;

            Game.ManufacturersView = new ListCollectionView<string>(onlineGameCollections.Manufacturers);
            Game.YearsView = new ListCollectionView<string>(onlineGameCollections.Years);
            Game.TypesView = new ListCollectionView<string>(onlineGameCollections.Types);
            Game.RomsView = new ListCollectionView<string>(onlineGameCollections.Roms);
            Game.PlayersView = new ListCollectionView<int?>(onlineGameCollections.Players);
            Game.ThemesView = new ListCollectionView<string>(onlineGameCollections.Themes);
            Game.AuthorsView = new ListCollectionView<string>(onlineGameCollections.Authors);
            Game.MaxDateTime = DateTime.Today.AddDays(1);

            if (DateTime.TryParse(Game.DateModifiedString, out var dateTime))
            {
                Game.DateModified = dateTime;
                Game.DateModifiedDateOnly = dateTime.Date;
            }

            Game.LoadedCommand = new ActionCommand(() => _loaded = true);
            Game.UnloadedCommand = new ActionCommand(() => _loaded = false);
            Game.ChangedCommand = new ActionCommand(() =>
            {
                // skip unnecessary changes that occur before control is loaded OR after the control has been unloaded
                // - e.g. populating items source for combobox causes the 'SelectedItem' to change which results in a 'false positiive' Changed event firing
                if (!_loaded)
                    return;

                // update date/time preserving the time portion, which is unfortunately cleared by the DateTime picker
                if (Game.DateModifiedDateOnly != null)
                    Game.DateModified = Game.DateModifiedDateOnly + (Game.DateModified?.TimeOfDay ?? TimeSpan.Zero);
                else
                    Game.DateModified = new DateTime(1900, 1, 1);
                Game.DateModifiedString = Game.DateModified?.ToString("yyyy-MM-dd HH:mm:ss");

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
