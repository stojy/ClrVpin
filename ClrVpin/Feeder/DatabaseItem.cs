using System;
using System.Text.Json;
using System.Windows.Input;
using ClrVpin.Controls;
using ClrVpin.Models.Feeder;
using ClrVpin.Models.Feeder.Vps;
using ClrVpin.Models.Shared.Game;
using Microsoft.Xaml.Behaviors.Core;
using PropertyChanged;
using Utils.Extensions;

namespace ClrVpin.Feeder
{
    [AddINotifyPropertyChangedInterface]
    public class DatabaseItem
    {
        public DatabaseItem(OnlineGame onlineGame, LocalGame originalLocalGame, IGameCollections gameCollections, bool isExisting, TableMatchOptionEnum tableMatchType)
        {
            // clone game details so that..
            // - changes can be discarded if required, i.e. not saved
            // - allow object comparison for serialization (ignoring a few VM properties)
            // - clone LocalGame (instead of just LocalGame.Game) so the LocalGame.Derived object is available to the view (e.g. for 'is original')
            var initialSerializedGame = JsonSerializer.Serialize(originalLocalGame.Clone());

            LocalGame = originalLocalGame.Clone();
            LocalGame.Init();
            
            // LCV.SelectedItem is assigned in the VM here (versus binding in the view) to avoid (what appears to be) some race conditions with the ComboBox bindings.. SelectedItem and Text binding
            // - the race condition causes the SelectedItem (e.g. LocalGame.Game.Manufacturer) to be 'randomly' assigned as an empty string from the async callback in Materialized's DialogHost.Show
            // - not immediately obvious in the UI though as the ComboBox's display is bound to the Text binding.. which often appears correct, despite the underlying property being assigned to empty string :(
            // - extra care is also required to ensure the collections do contain the desired default item, else this will cause the selected item to be assigned as null
            ManufacturersView = new ListCollectionView<string>(gameCollections.Manufacturers, LocalGame.Game.Manufacturer);
            YearsView = new ListCollectionView<string>(gameCollections.Years, LocalGame.Game.Year);
            TypesView = new ListCollectionView<string>(gameCollections.Types, LocalGame.Game.Type);
            RomsView = new ListCollectionView<string>(gameCollections.Roms, LocalGame.Game.Rom);
            PlayersView = new ListCollectionView<string>(gameCollections.Players, LocalGame.Game.Players);
            ThemesView = new ListCollectionView<string>(gameCollections.Themes, LocalGame.Game.Theme);
            AuthorsView = new ListCollectionView<string>(gameCollections.Authors, LocalGame.Game.Author);

            Title = tableMatchType switch
            {
                TableMatchOptionEnum.LocalAndOnline => "Update Matched Table¹",
                TableMatchOptionEnum.LocalOnly => "Update Unmatched Table¹",
                TableMatchOptionEnum.OnlineOnly => "Add Missing Table¹",
                _ => null
            };

            IsExisting = isExisting;
            IsItemChanged = false;
            CheckGameAgainstFeed(onlineGame);

            MaxDateTime = DateTime.Today.AddDays(1);

            if (DateTime.TryParse(LocalGame.Game.DateAddedString, out var dateTime))
            {
                DateAdded = dateTime;
                DateAddedDateOnly = dateTime.Date;
            }

            if (DateTime.TryParse(LocalGame.Game.DateModifiedString, out dateTime))
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
                LocalGame.Game.DateAddedString = DateAdded?.ToString("yyyy-MM-dd HH:mm:ss");

                // update date/time preserving the time portion, which is unfortunately cleared by the DateTime picker
                if (DateModifiedDateOnly != null)
                    DateModified = DateModifiedDateOnly + (DateModified?.TimeOfDay ?? TimeSpan.Zero);
                else
                    DateModified = new DateTime(1900, 1, 1);
                LocalGame.Game.DateModifiedString = DateModified?.ToString("yyyy-MM-dd HH:mm:ss");

                // update rounding
                // - required because the underlying RatingsBar unfortunately doesn't bind the value to the 'ValueIncrements' used in the UI, e.g. bound value 1.456700001
                // - if the rounding value is changed, the TextBox will rebind and cause another ChangedCommand to fire
                if (decimal.TryParse(LocalGame.Game.Rating, out var rating))
                    LocalGame.Game.Rating = (Math.Round(rating * 2) / 2).ToString();

                // explicitly update dynamic game details to account for any updated properties, e.g. table name, ipdb, etc
                LocalGame.Init();

                // check if anything has changed.. used to enable the 'update' button
                IsItemChanged = !LocalGame.IsEqual(initialSerializedGame) && !LocalGame.Game.Name.IsEmpty();

                CheckGameAgainstFeed(onlineGame);
            });

            AddMissingInfoCommand = new ActionCommand(() => { GameUpdater.UpdateProperties(onlineGame, LocalGame.Game, false); });

            OverwriteAllInfoCommand = new ActionCommand(() => { GameUpdater.UpdateProperties(onlineGame, LocalGame.Game, true); });
        }

        public LocalGame LocalGame { get; }

        public bool IsExisting { get; set; }
        public bool IsItemChanged { get; private set; }
        public bool IsItemInfoMissing { get; private set; }
        public bool IsItemInfoDifferent { get; private set; }

        public ICommand LoadedCommand { get; set; }
        public ICommand UnloadedCommand { get; set; }
        public ICommand ChangedCommand { get; set; }
        public ActionCommand AddMissingInfoCommand { get; }
        public ActionCommand OverwriteAllInfoCommand { get; }

        public ListCollectionView<string> ManufacturersView { get; }
        public ListCollectionView<string> YearsView { get; }
        public ListCollectionView<string> TypesView { get; }
        public ListCollectionView<string> PlayersView { get; }
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
        public string Title { get; }

        private void CheckGameAgainstFeed(OnlineGame onlineGame)
        {
            // check if anything is different to the feed.. used to enable the update missing and update all buttons
            // - if game is unmatched then onlineGame will be null.. which is explicitly ignored thus correctly returning false for both flags
            IsItemInfoMissing = GameUpdater.CheckProperties(onlineGame, LocalGame.Game, false);
            IsItemInfoDifferent = IsItemInfoMissing || GameUpdater.CheckProperties(onlineGame, LocalGame.Game, true);
        }

        private bool _loaded;
    }
}