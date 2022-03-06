using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using ClrVpin.Models;
using Utils;
using Utils.Extensions;

namespace ClrVpin.Shared
{
    public static class TableUtils
    {
        public static List<Game> GetGamesFromDatabases(IList<ContentType> contentTypes)
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
                    game.Number = number++;
                    game.Ipdb = game.IpdbId ?? game.IpdbNr;
                    game.IpdbUrl = string.IsNullOrEmpty(game.Ipdb) ? "" : $"https://www.ipdb.org/machine.cgi?id={game.Ipdb}";
                    game.NavigateToIpdbCommand = new ActionCommand(() => NavigateToIpdb(game.IpdbUrl));
                    game.Content.Init(contentTypes);

                    // assign fuzzy name details here to avoid it being calculated multiple times when comparing against EACH of the file matches
                    game.FuzzyTableDetails = Fuzzy.GetNameDetails(game.Name, false);
                    game.FuzzyDescriptionDetails = Fuzzy.GetNameDetails(game.Description, false);
                });

                WriteDatabase(file, doc);

                games.AddRange(menu.Games);
            });

            return games;
        }

        private static void WriteDatabase(string file, XDocument doc)
        {
            // proof of concept to confirm DB can be written back as an EXACT match when the view model properties are present
            using var writer = XmlWriter.Create(file + ".bak", new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "\t",
                Encoding = Encoding.GetEncoding("Windows-1252"),
                DoNotEscapeUriAttributes = true
            });
            doc.Save(writer);
        }

        public static IList<string> GetContentFileNames(ContentType contentType, string folder)
        {
            var supportedFiles = contentType.ExtensionsList.Select(ext => Directory.EnumerateFiles(folder, ext));

            return supportedFiles.SelectMany(x => x).ToList();
        }

        public static IEnumerable<FileDetail> GetUnsupportedMediaFileDetails(ContentType contentType, string folder)
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

        public static IEnumerable<FileDetail> AssociateContentFilesWithGames(IList<Game> games, IList<string> supportedFiles, ContentType contentType,
            Func<Game, ContentHits> getContentHits, Action<string, int> updateProgress)
        {
            var unknownSupportedFiles = new List<FileDetail>();

            // for each file, associate it with a game or if one can't be found, then mark it as unknown
            // - ASSOCIATION IS DONE IRRESPECTIVE OF THE USER'S SELECTED PREFERENCE, I.E. THE USE SELECTIONS ARE CHECKED ELSEWHERE
            supportedFiles.ForEach((supportedFile, i) =>
            {
                updateProgress(Path.GetFileName(supportedFile), i + 1);

                Game matchedGame;
                var fuzzyFileNameDetails = Fuzzy.GetNameDetails(supportedFile, true);

                // check for hit..
                // - only 1 hit per file.. but a game can have multiple hits.. with a maximum of 1 valid hit
                // - ignores the check criteria.. the check criteria is only used in the results (e.g. statistics)
                if ((matchedGame = games.FirstOrDefault(game => game.GetContentName(contentType.Category) == Path.GetFileNameWithoutExtension(supportedFile))) != null)
                {
                    // if a match already exists, then assume this match is a duplicate name with wrong extension
                    // - file extension order is important as it determines the priority of the preferred extension
                    var contentHits = getContentHits(matchedGame);
                    contentHits.Add(contentHits.Hits.Any(hit => hit.Type == HitTypeEnum.CorrectName) ? HitTypeEnum.DuplicateExtension : HitTypeEnum.CorrectName, supportedFile);
                }
                else if ((matchedGame = games.FirstOrDefault(game =>
                             string.Equals(game.GetContentName(contentType.Category), Path.GetFileNameWithoutExtension(supportedFile), StringComparison.CurrentCultureIgnoreCase))) != null)
                {
                    getContentHits(matchedGame).Add(HitTypeEnum.WrongCase, supportedFile);
                }
                else if (contentType.Category == ContentTypeCategoryEnum.Media && (matchedGame = games.FirstOrDefault(game => game.Name == Path.GetFileNameWithoutExtension(supportedFile))) != null)
                {
                    getContentHits(matchedGame).Add(HitTypeEnum.TableName, supportedFile);
                }
                // fuzzy matching
                else
                {
                    (matchedGame, var score) = games.Match(fuzzyFileNameDetails);
                    if (matchedGame != null)
                    {
                        getContentHits(matchedGame).Add(HitTypeEnum.Fuzzy, supportedFile, score);
                    }
                    else
                    {
                        // possible for..
                        // - table --> new table files added AND the database not updated yet
                        // - table support and media --> as per pinball OR extra/redundant files exist where there is no table (yet!)
                        unknownSupportedFiles.Add(new FileDetail(contentType.Enum, HitTypeEnum.Unknown, FixFileTypeEnum.Skipped, supportedFile, new FileInfo(supportedFile).Length));
                    }
                }
            });

            return unknownSupportedFiles;
        }

        private static void NavigateToIpdb(string url) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}