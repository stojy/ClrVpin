using System;
using System.Text.Json;
using System.Windows.Input;
using ClrVpin.Controls;
using ClrVpin.Models.Shared.Database;
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
            IsItemChanged = false;

            ManufacturersView = new ListCollectionView<string>(onlineGameCollections.Manufacturers);
            YearsView = new ListCollectionView<string>(onlineGameCollections.Years);
            TypesView = new ListCollectionView<string>(onlineGameCollections.Types);
            RomsView = new ListCollectionView<string>(onlineGameCollections.Roms);
            PlayersView = new ListCollectionView<int?>(onlineGameCollections.Players);
            ThemesView = new ListCollectionView<string>(onlineGameCollections.Themes);
            AuthorsView = new ListCollectionView<string>(onlineGameCollections.Authors);
            
            MaxDateTime = DateTime.Today.AddDays(1);
            
            if (DateTime.TryParse(Game.DateAddedString, out var dateTime))
            {
                DateAdded = dateTime;
                DateAddedDateOnly = dateTime.Date;
            }

            if (DateTime.TryParse(Game.DateModifiedString, out dateTime))
            {
                DateModified = dateTime;
                DateModifiedDateOnly = dateTime.Date;
            }

            LoadedCommand = new ActionCommand(() => _loaded = true);
            UnloadedCommand = new ActionCommand(() => _loaded = false);
            ChangedCommand = new ActionCommand(() =>
            {
                // skip unnecessary changes that occur before control is loaded OR after the control has been unloaded
                // - e.g. populating items source for combobox causes the 'SelectedItem' to change which results in a 'false positive' Changed event firing
                if (!_loaded)
                    return;

                // update date/time preserving the time portion, which is unfortunately cleared by the DateTime picker
                if (DateAddedDateOnly != null)
                    DateAdded = DateAddedDateOnly + (DateAdded?.TimeOfDay ?? TimeSpan.Zero);
                else
                    DateAdded = new DateTime(1900, 1, 1);
                Game.DateAddedString = DateAdded?.ToString("yyyy-MM-dd HH:mm:ss");

                // update date/time preserving the time portion, which is unfortunately cleared by the DateTime picker
                if (
                DateModifiedDateOnly != null)
                    DateModified = DateModifiedDateOnly + (DateModified?.TimeOfDay ?? TimeSpan.Zero);
                else
                    DateModified = new DateTime(1900, 1, 1);
                Game.DateModifiedString = DateModified?.ToString("yyyy-MM-dd HH:mm:ss");

                // explicitly recalculate dynamic VM properties
                GameDerived.Update(Game);

                // indicate whether anything has changed
                IsItemChanged = !Game.IsEqual(initialSerializedGame);
            });
        }

        public Game Game { get; }
        public bool IsExisting { get; set; }
        public bool IsItemChanged { get; set; }

        public ICommand ChangedCommand { get; set; }

        public ICommand LoadedCommand { get; set; }

        public ICommand UnloadedCommand { get; set; }

        public ListCollectionView<string> ManufacturersView { get; }

        public ListCollectionView<string> YearsView { get; }

        public ListCollectionView<string> TypesView { get; }

        public ListCollectionView<int?> PlayersView { get; }

        public ListCollectionView<string> RomsView { get; }

        public ListCollectionView<string> ThemesView { get; }

        public ListCollectionView<string> AuthorsView { get; }
    
        public DateTime MaxDateTime { get; set; }

        public DateTime? DateModified { get; set; }

        // date only portion to accommodate the DatePicker which resets the time portion when a date is selected
        public DateTime? DateModifiedDateOnly { get; set; }

        public DateTime? DateAdded { get; set; }

        // date only portion to accommodate the DatePicker which resets the time portion when a date is selected
        public DateTime? DateAddedDateOnly { get; set; }
    }
}
