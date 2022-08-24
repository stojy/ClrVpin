using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClrVpin.Models.Importer;
using ClrVpin.Models.Importer.Vps;
using ClrVpin.Models.Shared.Database;
using ClrVpin.Models.Shared.Game;
using ClrVpin.Shared;
using MaterialDesignThemes.Wpf;
using Utils.Extensions;

namespace ClrVpin.Importer
{
    public static class DatabaseItemManagement
    {
        public static async void UpdateDatabaseItem(IList<GameDetail> gameDetails, GameItem gameItem, IOnlineGameCollections onlineGameCollections)
        {
            var item = new DatabaseItem(gameItem.OnlineGame, gameItem.GameDetail, onlineGameCollections, true);

            var result = await DialogHost.Show(item, "ImporterResultsDialog") as DatabaseItemAction?;
            if (result == DatabaseItemAction.Update)
            {
                // replace the existing game details with the updated details
                var existingGameDetail = gameDetails.First(g => g == gameItem.GameDetail);
                existingGameDetail.Game = item.GameDetail.Game;
                existingGameDetail.Derived = item.GameDetail.Derived;

                // update all games that reside in the same database file as the updated game
                var gameDetailsInDatabaseFile = gameDetails.Where(gameDetail => gameDetail.Game.DatabaseFile == item.GameDetail.Game.DatabaseFile);
                TableUtils.WriteGamesToDatabase(gameDetailsInDatabaseFile.Select(x => x.Game), item.GameDetail.Game.DatabaseFile, item.GameDetail.Game.Name, false);
            }
        }

        public static async void CreateDatabaseItem(IList<GameDetail> gameDetails, GameItem gameItem, IOnlineGameCollections onlineGameCollections)
        {
            var onlineGame = gameItem.OnlineGame;

            var firstTable = onlineGame.TableFiles.FirstOrDefault();
            var gameDetail = new GameDetail
            {
                Game = new Game
                {
                    DatabaseFile = DefaultDatabaseFile,
                    Name = onlineGame.Name,
                    Description = onlineGame.Name,
                    IpdbId = onlineGame.IpdbId,

                    Manufacturer = onlineGame.Manufacturer,
                    Year = onlineGame.YearString,
                    Players = onlineGame.Players?.ToString(),
                    Author = firstTable?.Authors?.StringJoin(),

                    Theme = onlineGame.Themes.StringJoin(),
                    Version = firstTable?.Version,
                    Comment = firstTable?.Comment,
                    Rom = onlineGame.RomFiles?.FirstOrDefault()?.Name,
                    Type = onlineGame.Type,

                    DateAddedString = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    DateModifiedString = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),

                    Enabled = true.ToString(),
                    HideBackglass = true.ToString(),
                    HideDmd = true.ToString(),
                    HideTopper = true.ToString()
                }
            };

            var item = new DatabaseItem(onlineGame, gameDetail, onlineGameCollections, false);

            var result = await DialogHost.Show(item, "ImporterResultsDialog") as DatabaseItemAction?;
            if (result == DatabaseItemAction.Insert)
            {
                gameDetail.Game = item.GameDetail.Game;
                gameDetail.Derived = item.GameDetail.Derived;

                // assume the game is now matched to remove the 'add' option
                // - in reality though the game may in theory still be unmatched if the user has changed the name/description beyond the reach of the fuzzy checking
                onlineGame.Hit = new GameHit { GameDetail = gameDetail };
                gameItem.GameDetail = gameDetail;

                // insert the game
                gameDetails.Add(gameDetail);

                // update all games that belong to the newly created game (defaulting to 'ClrVpin.xml')
                var gameDetailsInDatabaseFile = gameDetails.Where(gd => gd.Game.DatabaseFile == item.GameDetail.Game.DatabaseFile);
                TableUtils.WriteGamesToDatabase(gameDetailsInDatabaseFile.Select(x => x.Game), item.GameDetail.Game.DatabaseFile, item.GameDetail.Game.Name, true);
            }
        }

        private static string DefaultDatabaseFile => Path.Combine(Model.Settings.GetDatabaseContentType().Folder, "ClrVpin.xml");
    }
}