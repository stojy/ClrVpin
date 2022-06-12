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
using Utils;
using Utils.Extensions;
using Utils.Xml;

namespace ClrVpin.Shared
{
    public static class TableUtils
    {
        public static List<Game> ReadGamesFromDatabases(IList<ContentType> contentTypes)
        {
            var databaseContentType = Model.Settings.GetDatabaseContentType();

            // scan through all the databases in the folder
            var files = Directory.EnumerateFiles(databaseContentType.Folder, databaseContentType.Extensions);

            var games = new List<Game>();

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
                var number = 1;
                menu.Games.ForEach(game =>
                {
                    GameDerived.Update(game, number++);
                    game.NavigateToIpdbCommand = new ActionCommand(() => NavigateToIpdb(game.Derived.IpdbUrl));
                    game.Content.Init(contentTypes);

                    // assign fuzzy name details here to avoid it being calculated multiple times when comparing against EACH of the file matches
                    game.FuzzyTableDetails = Fuzzy.GetNameDetails(game.Name, false);
                    game.FuzzyDescriptionDetails = Fuzzy.GetNameDetails(game.Description, false);
                });

                // proof of concept - serialize to disk again to verify similarity/compatibility
                WriteGamesToDatabase(menu.Games, file + ".bak");

                games.AddRange(menu.Games);
            });

            Logger.Info($"Local database table count: {games.Count} (manufactured={games.Count(onlineGame => !onlineGame.Derived.IsOriginal)}, original={games.Count(onlineGame => onlineGame.Derived.IsOriginal)})");
            return games;
        }

        public static void WriteGamesToDatabase(List<Game> games, string file = null)
        {
            if (file == null)
            {
                var databaseContentType = Model.Settings.GetDatabaseContentType();
                file = Path.Combine(databaseContentType.Folder, "Visual Pinball - ClrVpin.xml.bak") ;
            }

            var menu = new Menu { Games = games };
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

        public static IEnumerable<FileDetail> AddContentFilesToGames(IList<Game> games, IEnumerable<string> contentFiles, ContentType contentType,
            Func<Game, ContentHits> getContentHits, Action<string, int> updateProgress)
        {
            var unknownSupportedFiles = new List<FileDetail>();

            // for each file, associate it with a game or if one can't be found, then mark it as unknown
            // - ASSOCIATION IS DONE IRRESPECTIVE OF THE USER'S SELECTED PREFERENCE, I.E. THE USE SELECTIONS ARE CHECKED ELSEWHERE
            contentFiles.ForEach((contentFile, i) =>
            {
                updateProgress(Path.GetFileName(contentFile), i + 1);

                Game matchedGame;
                var fuzzyFileNameDetails = Fuzzy.GetNameDetails(contentFile, true);

                // check for hit..
                // - only 1 hit per file.. but a game can have multiple hits.. with a maximum of 1 valid hit
                // - ignores the check criteria.. the check criteria is only used in the results (e.g. statistics)
                if ((matchedGame = games.FirstOrDefault(game => game.GetContentName(contentType.Category) == Path.GetFileNameWithoutExtension(contentFile))) != null)
                {
                    // if a match already exists, then assume this match is a duplicate name with wrong extension
                    // - file extension order is important as it determines the priority of the preferred extension
                    var contentHits = getContentHits(matchedGame);
                    contentHits.Add(contentHits.Hits.Any(hit => hit.Type == HitTypeEnum.CorrectName) ? HitTypeEnum.DuplicateExtension : HitTypeEnum.CorrectName, contentFile);
                }
                else if ((matchedGame = games.FirstOrDefault(game =>
                             string.Equals(game.GetContentName(contentType.Category), Path.GetFileNameWithoutExtension(contentFile), StringComparison.CurrentCultureIgnoreCase))) != null)
                {
                    getContentHits(matchedGame).Add(HitTypeEnum.WrongCase, contentFile);
                }
                else if (contentType.Category == ContentTypeCategoryEnum.Media && (matchedGame = games.FirstOrDefault(game => game.Name == Path.GetFileNameWithoutExtension(contentFile))) != null)
                {
                    getContentHits(matchedGame).Add(HitTypeEnum.TableName, contentFile);
                }
                // fuzzy matching
                else
                {
                    (matchedGame, var score) = games.Match(fuzzyFileNameDetails);
                    if (matchedGame != null)
                    {
                        getContentHits(matchedGame).Add(HitTypeEnum.Fuzzy, contentFile, score);
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