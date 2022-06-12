using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using ClrVpin.Logging;
using ClrVpin.Models.Shared;
using ClrVpin.Models.Shared.Database;
using ClrVpin.Shared.Fuzzy;
using Utils;
using Utils.Extensions;
using Utils.Xml;

namespace ClrVpin.Shared
{
    public static class TableUtils
    {
        public static List<GameDetail> ReadGamesFromDatabases(IList<ContentType> contentTypes)
        {
            var databaseContentType = Model.Settings.GetDatabaseContentType();

            // scan through all the databases in the folder
            var files = Directory.EnumerateFiles(databaseContentType.Folder, databaseContentType.Extensions);

            var gameDetails = new List<GameDetail>();

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

                var doc = XDocument.Load(reader);
                if (doc.Root == null)
                    throw new Exception($"Failed to load database: '{file}'");

                var menu = doc.Root.Deserialize<Menu>();
                var databaseGameDetails = menu.Games.Select(g => new GameDetail { Game = g }).ToList();

                var number = 1;
                databaseGameDetails.ForEach(gameDetail =>
                {
                    GameDerived.Init(gameDetail, number++);
                    gameDetail.ViewState.NavigateToIpdbCommand = new ActionCommand(() => NavigateToIpdb(gameDetail.Derived.IpdbUrl));
                    gameDetail.Content.Init(contentTypes);

                    // assign fuzzy name details up front to avoid it being re-calculated multiple times later on, e.g. when comparing against EACH of the file matches
                    gameDetail.Fuzzy.TableDetails = Fuzzy.Fuzzy.GetNameDetails(gameDetail.Game.Name, false);
                    gameDetail.Fuzzy.DescriptionDetails = Fuzzy.Fuzzy.GetNameDetails(gameDetail.Game.Description, false);
                });

                // proof of concept - serialize to disk again to verify similarity/compatibility
                WriteGamesToDatabase(databaseGameDetails, file + ".bak");

                gameDetails.AddRange(databaseGameDetails);
            });

            Logger.Info($"Local database table count: {gameDetails.Count} (manufactured={gameDetails.Count(onlineGame => !onlineGame.Derived.IsOriginal)}, original={gameDetails.Count(onlineGame => onlineGame.Derived.IsOriginal)})");
            return gameDetails;
        }

        public static void WriteGamesToDatabase(IEnumerable<GameDetail> gameDetails, string file = null)
        {
            var games = gameDetails.Select(gameDetail => gameDetail.Game);

            if (file == null)
            {
                var databaseContentType = Model.Settings.GetDatabaseContentType();
                file = Path.Combine(databaseContentType.Folder, "Visual Pinball - ClrVpin.xml.bak") ;
            }

            var menu = new Menu { Games = games.ToList() };
            menu.SerializeToXDocument().Cleanse().SerializeToFile(file);
        }

        public static IList<string> GetContentFileNames(ContentType contentType, string folder)
        {
            var supportedFiles = contentType.ExtensionsList.Select(ext => Directory.EnumerateFiles(folder, ext));

            return supportedFiles.SelectMany(x => x).ToList();
        }

        public static IEnumerable<FileDetail> GetNonContentFileDetails(ContentType contentType, string folder)
        {
            // return all files that don't match the supported file extensions
            var supportedExtensions = contentType.ExtensionsList.Select(x => x.TrimStart('*').ToLower()).ToList();
            var kindredExtensions = contentType.KindredExtensionsList.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.TrimStart('*').ToLower());
            supportedExtensions.AddRange(kindredExtensions);

            var allFiles = Directory.EnumerateFiles(folder).Select(x => x.ToLower());

            var unsupportedFiles = allFiles.Where(file => !supportedExtensions.Any(file.EndsWith));

            var unsupportedFixFiles = unsupportedFiles.Select(file => new FileDetail(contentType.Enum, HitTypeEnum.Unsupported, FixFileTypeEnum.Skipped, file, new FileInfo(file).Length));

            return unsupportedFixFiles.ToList();
        }

        public static IEnumerable<FileDetail> AddContentFilesToGames(IList<GameDetail> gameDetails, IEnumerable<string> contentFiles, ContentType contentType,
            Func<GameDetail, ContentHits> getContentHits, Action<string, int> updateProgress)
        {
            var unknownSupportedFiles = new List<FileDetail>();

            // for each file, associate it with a game or if one can't be found, then mark it as unknown
            // - ASSOCIATION IS DONE IRRESPECTIVE OF THE USER'S SELECTED PREFERENCE, I.E. THE USE SELECTIONS ARE CHECKED ELSEWHERE
            contentFiles.ForEach((contentFile, i) =>
            {
                updateProgress(Path.GetFileName(contentFile), i + 1);

                GameDetail matchedGameDetail;
                var fuzzyFileNameDetails = Fuzzy.Fuzzy.GetNameDetails(contentFile, true);

                // check for hit..
                // - only 1 hit per file.. but a game can have multiple hits.. with a maximum of 1 valid hit
                // - ignores the check criteria.. the check criteria is only used in the results (e.g. statistics)
                if ((matchedGameDetail = gameDetails.FirstOrDefault(game => Content.GetName(game, contentType.Category) == Path.GetFileNameWithoutExtension(contentFile))) != null)
                {
                    // if a match already exists, then assume this match is a duplicate name with wrong extension
                    // - file extension order is important as it determines the priority of the preferred extension
                    var contentHits = getContentHits(matchedGameDetail);
                    contentHits.Add(contentHits.Hits.Any(hit => hit.Type == HitTypeEnum.CorrectName) ? HitTypeEnum.DuplicateExtension : HitTypeEnum.CorrectName, contentFile);
                }
                else if ((matchedGameDetail = gameDetails.FirstOrDefault(game =>
                             string.Equals(Content.GetName(game, contentType.Category), Path.GetFileNameWithoutExtension(contentFile), StringComparison.CurrentCultureIgnoreCase))) != null)
                {
                    getContentHits(matchedGameDetail).Add(HitTypeEnum.WrongCase, contentFile);
                }
                else if (contentType.Category == ContentTypeCategoryEnum.Media && (matchedGameDetail = gameDetails.FirstOrDefault(gameDetail => gameDetail.Game.Name == Path.GetFileNameWithoutExtension(contentFile))) != null)
                {
                    getContentHits(matchedGameDetail).Add(HitTypeEnum.TableName, contentFile);
                }
                // fuzzy matching
                else
                {
                    (matchedGameDetail, var score) = gameDetails.Match(fuzzyFileNameDetails);
                    if (matchedGameDetail != null)
                    {
                        getContentHits(matchedGameDetail).Add(HitTypeEnum.Fuzzy, contentFile, score);
                    }
                    else
                    {
                        // possible for..
                        // - table --> new table files added AND the database not updated yet
                        // - table support and media --> as per pinball OR extra/redundant files exist where there is no table (yet!)
                        unknownSupportedFiles.Add(new FileDetail(contentType.Enum, HitTypeEnum.Unknown, FixFileTypeEnum.Skipped, contentFile, new FileInfo(contentFile).Length));
                    }
                }
            });

            return unknownSupportedFiles;
        }

        private static void NavigateToIpdb(string url) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}