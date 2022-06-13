using System;
using System.Collections.Generic;
using System.Linq;
using ClrVpin.Logging;
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
        public static async void ViewDatabaseItem(List<GameDetail> gameDetails, OnlineGame onlineGame, IOnlineGameCollections onlineGameCollections)
        {
            var item = new DatabaseItem(onlineGame.Hit.GameDetail, onlineGameCollections, true);

            var result = await DialogHost.Show(item, "ImporterResultsDialog") as DatabaseItemAction?;
            if (result == DatabaseItemAction.Update)
            {
                // replace the out of date game details
                var existingGameDetail = gameDetails.First(g => g == onlineGame.Hit.GameDetail);
                existingGameDetail.Game = item.GameDetail.Game;
                existingGameDetail.Derived = item.GameDetail.Derived;

                // only update the database file tht the game belongs to
                var gameDetailsInDatabaseFile = gameDetails.Where(gameDetail => gameDetail.Game.DatabaseFile == item.GameDetail.Game.DatabaseFile);
                TableUtils.WriteGamesToDatabase(gameDetailsInDatabaseFile, item.GameDetail.Game.DatabaseFile);

                Logger.Info($"Updated existing table: {item.GameDetail.Game.Name}");
            }
        }

        public static async void AddDatabaseItem(List<GameDetail> gameDetails, OnlineGame onlineGame, IOnlineGameCollections onlineGameCollections)
        {
            var firstTable = onlineGame.TableFiles.FirstOrDefault();
            var gameDetail = new GameDetail
            {
                Game = new Game
                {
                    DatabaseFile = "ClrVpin.xml",
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

            var item = new DatabaseItem(gameDetail, onlineGameCollections, false);

            var result = await DialogHost.Show(item, "ImporterResultsDialog") as DatabaseItemAction?;
            if (result == DatabaseItemAction.Insert)
            {
                gameDetail.Game = item.GameDetail.Game;
                gameDetail.Derived = item.GameDetail.Derived;
                
                // assume the game is now matched to remove the 'add' option
                // - in reality though the game may in theory still be unmatched if the user has changed the name/description beyond the reach of the fuzzy checking
                onlineGame.Hit = new GameHit { GameDetail = gameDetail };
                onlineGame.IsMatched = true;

                // insert the game
                gameDetails.Add(gameDetail);

                // only update the database file tht the game belongs to
                var gameDetailsInDatabaseFile = gameDetails.Where(gd => gd.Game.DatabaseFile == item.GameDetail.Game.DatabaseFile);
                TableUtils.WriteGamesToDatabase(gameDetailsInDatabaseFile, item.GameDetail.Game.DatabaseFile);

                Logger.Info($"Added new table: {item.GameDetail.Game.Name}");
            }
        }
    }
}