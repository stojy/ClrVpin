using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClrVpin.Models.Feeder;
using ClrVpin.Models.Feeder.Vps;
using ClrVpin.Models.Shared.Database;
using ClrVpin.Models.Shared.Game;
using ClrVpin.Shared;
using MaterialDesignThemes.Wpf;
using Utils.Extensions;

namespace ClrVpin.Feeder;

public static class DatabaseItemManagement
{
    private static string DefaultDatabaseFile => Path.Combine(Model.Settings.GetDatabaseContentType().Folder, "ClrVpin.xml");

    public static async void UpdateDatabaseItem(string dialogHostName, IList<LocalGame> localGames, GameItem gameItem, IGameCollections gameCollections, Action removeGameItem)
    {
        var item = new DatabaseItem(gameItem.OnlineGame, gameItem.LocalGame, gameCollections, true, gameItem.TableMatchType);

        var result = await DialogHost.Show(item, dialogHostName) as DatabaseItemAction?;
        if (result == DatabaseItemAction.Update)
        {
            // replace the existing game details with the updated details
            var existingLocalGame = localGames.First(g => g == gameItem.LocalGame);
            existingLocalGame.Game = item.LocalGame.Game;
            existingLocalGame.Derived = item.LocalGame.Derived;

            Update(localGames, gameCollections, item, false);
        }
        else if (result == DatabaseItemAction.Delete)
        {
            // remove the local game from the local DB
            localGames.Remove(gameItem.LocalGame);
            Update(localGames, gameCollections, item, false);

            // remove the local game from the game item list
            if (gameItem.OnlineGame != null)
            {
                // matched item - keep GameItem in the list, but remove the hit (matching reference)
                gameItem.OnlineGame.Hit = null;
                gameItem.Update(null);
            }
            else
            {
                // unmatched item - remove GameItem from the list, i.e. since there's no longer any reference (online or local) to the game
                removeGameItem();
            }
        }
    }

    public static async void CreateDatabaseItem(string dialogHostName, IList<LocalGame> localGames, GameItem gameItem, IGameCollections gameCollections)
    {
        var onlineGame = gameItem.OnlineGame;

        var firstTable = onlineGame.TableFiles.FirstOrDefault();
        var localGame = new LocalGame
        {
            Game = new Game
            {
                DatabaseFile = DefaultDatabaseFile,
                Name = onlineGame.Description,
                Description = onlineGame.Description,
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

        var item = new DatabaseItem(onlineGame, localGame, gameCollections, false, gameItem.TableMatchType);

        var result = await DialogHost.Show(item, dialogHostName) as DatabaseItemAction?;
        if (result == DatabaseItemAction.Insert)
        {
            localGame.Game = item.LocalGame.Game;
            localGame.Derived = item.LocalGame.Derived;

            // assume the game is now matched to remove the 'add' option
            // - in reality though the game may in theory still be unmatched if the user has changed the name/description beyond the reach of the fuzzy checking
            onlineGame.Hit = new LocalGameHit { LocalGame = localGame };
            gameItem.Update(localGame);

            // add the new game to the local DB
            localGames.Add(localGame);

            Update(localGames, gameCollections, item, true);
        }
    }

    private static void Update(IEnumerable<LocalGame> localGames, IGameCollections gameCollections, DatabaseItem databaseItem, bool isNewEntry)
    {
        // update all games that reside in the same database file as the updated game
        var localGamesInDatabaseFile = localGames.Where(localGame => localGame.Game.DatabaseFile == databaseItem.LocalGame.Game.DatabaseFile);
        TableUtils.WriteGamesToDatabase(localGamesInDatabaseFile.Select(x => x.Game), databaseItem.LocalGame.Game.DatabaseFile, databaseItem.LocalGame.Game.Name, isNewEntry);
            
        gameCollections.UpdateCollections();
    }
}