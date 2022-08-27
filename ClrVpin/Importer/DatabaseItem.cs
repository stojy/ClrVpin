using System;
using System.Text.Json;
using System.Windows.Input;
using ClrVpin.Controls;
using ClrVpin.Models.Importer;
using ClrVpin.Models.Importer.Vps;
using ClrVpin.Models.Shared.Game;
using Microsoft.Xaml.Behaviors.Core;
using PropertyChanged;
using Utils.Extensions;

namespace ClrVpin.Importer
{
    [AddINotifyPropertyChangedInterface]
    public class DatabaseItem
    {
        public DatabaseItem(OnlineGame onlineGame, GameDetail originalGameDetail, IOnlineGameCollections onlineGameCollections, bool isExisting, TableMatchOptionEnum tableMatchType)
        {
            // clone game details so that..
            // - changes can be discarded if required, i.e. not saved
            // - allow object comparison for serialization (ignoring a few VM properties)
            // - clone GameDetail (instead of just GameDetail.Game) so the GameDetail.Derived object is available to the view (e.g. for 'is original')
            var initialSerializedGame = JsonSerializer.Serialize(originalGameDetail.Clone());

            GameDetail = originalGameDetail.Clone();
            GameDetail.Init();

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

            ManufacturersView = new ListCollectionView<string>(onlineGameCollections.Manufacturers);
            YearsView = new ListCollectionView<string>(onlineGameCollections.Years);
            TypesView = new ListCollectionView<string>(onlineGameCollections.Types);
            RomsView = new ListCollectionView<string>(onlineGameCollections.Roms);
            PlayersView = new ListCollectionView<int?>(onlineGameCollections.Players);
            ThemesView = new ListCollectionView<string>(onlineGameCollections.Themes);
            AuthorsView = new ListCollectionView<string>(onlineGameCollections.Authors);

            MaxDateTime = DateTime.Today.AddDays(1);

            if (DateTime.TryParse(GameDetail.Game.DateAddedString, out var dateTime))
            {
                DateAdded = dateTime;
                DateAddedDateOnly = dateTime.Date;
            }

            if (DateTime.TryParse(GameDetail.Game.DateModifiedString, out dateTime))
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
                GameDetail.Game.DateAddedString = DateAdded?.ToString("yyyy-MM-dd HH:mm:ss");

                // update date/time preserving the time portion, which is unfortunately cleared by the DateTime picker
                if (DateModifiedDateOnly != null)
                    DateModified = DateModifiedDateOnly + (DateModified?.TimeOfDay ?? TimeSpan.Zero);
                else
                    DateModified = new DateTime(1900, 1, 1);
                GameDetail.Game.DateModifiedString = DateModified?.ToString("yyyy-MM-dd HH:mm:ss");

                // update rounding
                // - required because the underlying RatingsBar unfortunately doesn't bind the value to the 'ValueIncrements' used in the UI, e.g. bound value 1.456700001
                // - if the rounding value is changed, the textbox will rebind and cause another ChangedCommand to fire
                if (decimal.TryParse(GameDetail.Game.Rating, out var rating))
                    GameDetail.Game.Rating = (Math.Round(rating * 2) / 2).ToString();

                // explicitly update dynamic game details to account for any updated properties, e.g. table name, ipdb, etc
                GameDetail.Init();

                // check if anything has changed.. used to enable the 'update' button
                IsItemChanged = !GameDetail.IsEqual(initialSerializedGame) && !GameDetail.Game.Name.IsEmpty();

                CheckGameAgainstFeed(onlineGame);
            });

            AddMissingInfoCommand = new ActionCommand(() =>
            {
                GameUpdater.UpdateProperties(onlineGame, GameDetail.Game, false);
            });

            OverwriteAllInfoCommand = new ActionCommand(() =>
            {
                GameUpdater.UpdateProperties(onlineGame, GameDetail.Game, true);
            });
        }

        private void CheckGameAgainstFeed(OnlineGame onlineGame)
        {
            // check if anything is different to the feed.. used to enable the update missing and update all buttons
            // - if game is unmatched then onlineGame will be null.. which is explicitly ignored thus correctly returning false for both flags
            IsItemInfoMissing = GameUpdater.CheckProperties(onlineGame, GameDetail.Game, false);
            IsItemInfoDifferent = IsItemInfoMissing || GameUpdater.CheckProperties(onlineGame, GameDetail.Game, true);
        }

        public GameDetail GameDetail { get; }

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
        private bool _loaded;
        public string Title { get; set; }
    }
}