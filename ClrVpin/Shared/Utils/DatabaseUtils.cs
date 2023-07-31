using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ClrVpin.Home;
using ClrVpin.Logging;
using ClrVpin.Models.Shared;
using ClrVpin.Models.Shared.Database;
using ClrVpin.Models.Shared.Enums;
using ClrVpin.Models.Shared.Game;
using Utils;
using Utils.Extensions;
using Utils.Xml;

namespace ClrVpin.Shared.Utils
{
    public static class DatabaseUtils
    {
        public static async Task<List<LocalGame>> ReadGamesFromDatabases(IEnumerable<ContentType> contentTypes)
        {
            try
            {
                return GetGamesFromDatabases(contentTypes);
            }
            catch (Exception e)
            {
                await Notification.ShowWarning(HomeWindow.HomeDialogHost,
                    "Unable to read PinballY/PinballX database file",
                    "Please check the database xml file is well formatted, e.g. via https://codebeautify.org/xmlvalidator.\n\n" +
                    "Alternatively, log an issue via github and upload the xml file for review.",
                    $"{e.Message}\n\n" +
                    $"{e.StackTrace}\n\n" +
                    $"{e.InnerException?.Message}\n\n" +
                    $"{e.InnerException?.StackTrace}",
                    showCloseButton: true);
                throw;
            }
        }

        public static void WriteGamesToDatabase(IEnumerable<Game> games)
        {
            var gamesByDatabase = games.GroupBy(game => game.DatabaseFile);
            gamesByDatabase.ForEach(gamesGrouping => WriteGamesToDatabase(gamesGrouping, gamesGrouping.Key, "n/a", false));
        }

        public static void WriteGamesToDatabase(IEnumerable<Game> games, string file, string game, bool isNewEntry)
        {
            if (file != null && !Path.IsPathRooted(file))
            {
                var databaseContentType = Model.Settings.GetDatabaseContentType();
                file = Path.Combine(databaseContentType.Folder, file);
            }

            Logger.Info($"{(isNewEntry ? "Adding new" : "Updating existing")} table: '{game}', database: {file}");

            var menu = new Menu { Games = games.ToList() };

            // a new backup folder is designated for every backup so that we can keep a record of every file change
            FileUtils.SetActiveBackupFolder(Model.Settings.BackupFolder);
            FileUtils.Backup(file, "merged", ContentTypeEnum.Database.GetDescription(), null, true);

            menu.SerializeToXDocument().Cleanse().SerializeToFile(file);
        }

        private static List<LocalGame> GetGamesFromDatabases(IEnumerable<ContentType> contentTypes)
        {
            var databaseContentType = Model.Settings.GetDatabaseContentType();

            // scan through all the databases in the folder
            var files = Directory.EnumerateFiles(databaseContentType.Folder, databaseContentType.Extensions);

            var localGames = new List<LocalGame>();
            var gameNumber = 1;

            files.ForEach(file =>
            {
                // explicitly open file as a stream so that the encoding can be specified to allow the non-standard characters to be read
                // - PinballY (and presumably PinballX) write the DB file as extended ASCII 'code page 1252', i.e. not utf-8 or utf-16
                // - despite 1252 being the default code page for windows, using .net5 it appears to 'code page 437'
                // - e.g. 153 (0x99)
                //   - code page 437 = Ö
                //   - code page 1252 = ™
                //   - utf-8 = �
                // - further reading.. https://en.wikipedia.org/wiki/Extended_ASCII, https://codepoints.net/U+2122?lang=en
                using var reader = new StreamReader(file, Encoding.GetEncoding("Windows-1252"));

                // PinballX unfortunately defines some of it's XML using HTML entity names which are not valid within XML
                // - technically it would be allowed if the PBX file defined an ENTITY mapping, but unfortunately it does not
                // - refer https://www.w3schools.com/html/html_entities.asp
                //   e.g. PBX uses &pos; instead of &#39; or just plain ' (apostrophe)
                // - the invalid XML causes XDocument to fail, so we strip out the common scenarios so that the file can be read (and ultimately written if changes are made)
                var xmlString = reader.ReadToEnd();
                xmlString = xmlString
                    .Replace(@"&pos;", "'")
                    .Replace(@"&copy;", "©")
                    .Replace(@"&reg;", "®")
                    .Replace(@"&amp;", "&#38;")
                    .Replace(@"&quot;", "&#34;");

                XDocument doc;
                try
                {
                    doc = XDocument.Parse(xmlString);
                    if (doc.Root == null)
                        throw new Exception("Root element missing");
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed to load database: '{file}'", e);
                }

                Menu menu;
                try
                {
                    menu = doc.Root.Deserialize<Menu>();
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed to deserialize database: '{file}'", e);
                }

                var databaseLocalGames = menu.Games.Select(g => new LocalGame { Game = g }).ToList();

                databaseLocalGames.ForEach(localGame =>
                {
                    localGame.Init(gameNumber++);

                    // support both IpdbId (PinballY) and IpdbNr (PinballX)
                    // - if both are present and different, then default to the value from IpdbId
                    if (!string.IsNullOrEmpty(localGame.Game.IpdbId))
                        localGame.Game.IpdbNr = localGame.Game.IpdbId;
                    else
                        localGame.Game.IpdbId = localGame.Game.IpdbNr;

                    localGame.Game.DatabaseFile = file;
                    localGame.ViewState.NavigateToIpdbCommand = new ActionCommand(() => ContentUtils.NavigateToIpdb(localGame.Derived.IpdbUrl));
                    localGame.Content.Init(contentTypes);
                });

                localGames.AddRange(databaseLocalGames);
                LogDatabaseStatistics(databaseLocalGames, file);
            });

            LogDatabaseStatistics(localGames);

            return localGames;
        }


        private static void LogDatabaseStatistics(IReadOnlyCollection<LocalGame> localGames, string file = null)
        {
            Logger.Info(
                $"Local database {(file == null ? "total" : "file")}: count={localGames.Count} (manufactured={localGames.Count(onlineGame => !onlineGame.Derived.IsOriginal)}, original={localGames.Count(onlineGame => onlineGame.Derived.IsOriginal)})" +
                $"{(file == null ? "" : ", file: " + file)}");
        }
    }
}