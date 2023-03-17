using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ClrVpin.Logging;
using ClrVpin.Models.Shared;
using ClrVpin.Models.Shared.Database;
using ClrVpin.Models.Shared.Game;
using ClrVpin.Shared.Fuzzy;
using Utils;
using Utils.Extensions;
using Utils.Xml;

namespace ClrVpin.Shared
{
    public static class TableUtils
    {
        public static async Task<List<LocalGame>> ReadGamesFromDatabases(IEnumerable<ContentType> contentTypes)
        {
            try
            {
                return GetGamesFromDatabases(contentTypes);
            }
            catch (Exception e)
            {
                await Notification.ShowWarning("HomeDialog",
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

        public static IList<string> GetContentFileNames(ContentType contentType, string folder)
        {
            // for each supported extension file type (e.g. vpx, vpt), retrieve all the files with a matching extension in the specified folder
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

        public static async Task<List<FileDetail>> MatchContentToLocalAsync(List<LocalGame> games, Action<string, float> updateProgress, ContentType[] contentTypes, bool includeUnsupportedFiles)
        {
            var unmatchedFiles = await Task.Run(() => MatchContentToLocal(games, updateProgress, contentTypes, includeUnsupportedFiles));
            return unmatchedFiles;
        }

        public static IEnumerable<FileDetail> MatchFilesToLocal(IList<LocalGame> localGames, IEnumerable<string> contentFiles, ContentType contentType,
            Func<LocalGame, ContentHits> getContentHits, Action<string, int> updateProgress)
        {
            var unmatchedSupportedFiles = new List<FileDetail>();

            // for each file, associate it with a game or if one can't be found, then mark it as unmatched
            // - ASSOCIATION IS DONE IRRESPECTIVE OF THE USER'S SELECTED PREFERENCE, I.E. THE USE SELECTIONS ARE CHECKED ELSEWHERE
            contentFiles.ForEach((contentFile, i) =>
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(contentFile);
                updateProgress(fileNameWithoutExtension, i + 1);

                LocalGame matchedLocalGame;

                // check for hit..
                // - a file only match one DB entry.. but a game DB entry can have multiple file hits
                // - if DB entry has more than 1 file hit, then the best match is used and the other are marked as duplicates.. e.g. wrong case, fuzzy matched, etc.
                // - ignores the check criteria.. the check criteria is only used in the results (e.g. statistics)

                // exact match
                if ((matchedLocalGame = localGames.FirstOrDefault(game => Content.GetName(game, contentType.Category) == fileNameWithoutExtension)) != null)
                {
                    // if a match already exists, then assume this match is a duplicate name with wrong extension
                    // - file extension order is important as it determines the priority of the preferred extension
                    var contentHits = getContentHits(matchedLocalGame);
                    contentHits.Add(contentHits.Hits.Any(hit => hit.Type == HitTypeEnum.CorrectName) ? HitTypeEnum.DuplicateExtension : HitTypeEnum.CorrectName, contentFile);
                }
                // wrong case match
                else if ((matchedLocalGame = localGames.FirstOrDefault(localGame =>
                             string.Equals(Content.GetName(localGame, contentType.Category), fileNameWithoutExtension, StringComparison.CurrentCultureIgnoreCase))) != null)
                {
                    getContentHits(matchedLocalGame).Add(HitTypeEnum.WrongCase, contentFile);
                }
                // media matches table name
                else if (contentType.Category == ContentTypeCategoryEnum.Media && (matchedLocalGame = localGames.FirstOrDefault(localGame => localGame.Game.Name == fileNameWithoutExtension)) != null)
                {
                    getContentHits(matchedLocalGame).Add(HitTypeEnum.TableName, contentFile);
                }
                // fuzzy matching
                else
                {
                    var fuzzyFileNameDetails = Fuzzy.Fuzzy.GetTableDetails(contentFile, true);
                    (matchedLocalGame, var score, var isMatch) = localGames.MatchToLocalDatabase(fuzzyFileNameDetails);
                    if (isMatch)
                    {
                        getContentHits(matchedLocalGame).Add(HitTypeEnum.Fuzzy, contentFile, score);
                    }
                    else
                    {
                        // unmatched
                        // - e.g. possible for..
                        //   a. table --> new table files added AND the database not updated yet
                        //   b. table support and media --> as per pinball OR extra/redundant files exist where there is no table (yet!)
                        unmatchedSupportedFiles.Add(new FileDetail(contentType.Enum, HitTypeEnum.Unknown, FixFileTypeEnum.Skipped, contentFile, new FileInfo(contentFile).Length));
                    }
                }
            });

            return unmatchedSupportedFiles;
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

                    localGame.Game.DatabaseFile = file;
                    localGame.ViewState.NavigateToIpdbCommand = new ActionCommand(() => NavigateToIpdb(localGame.Derived.IpdbUrl));
                    localGame.Content.Init(contentTypes);
                });

                localGames.AddRange(databaseLocalGames);
                LogDatabaseStatistics(databaseLocalGames, file);
            });

            LogDatabaseStatistics(localGames);

            return localGames;
        }

        private static List<FileDetail> MatchContentToLocal(List<LocalGame> games, Action<string, float> updateProgress, IEnumerable<ContentType> checkContentTypes, bool includeUnsupportedFiles)
        {
            var unmatchedFiles = new List<FileDetail>();

            // retrieve all supported files within the folder
            // - for each content type, match files (from the configured content folder location) with the correct file extension(s) to a table
            // - file matching is performed irrespective of the configured matching type (e.g. invalid case, fuzzy, etc) --> refer MatchFilesToLocal
            var contentTypeSupportedFiles = checkContentTypes.Select(contentType => new
            {
                contentType,
                supportedFiles = GetContentFileNames(contentType, contentType.Folder).ToList()
            }).ToList();

            var totalFilesCount = contentTypeSupportedFiles.Sum(details => details.supportedFiles.Count);
            var fileCount = 0;
            contentTypeSupportedFiles.ForEach(details =>
            {
                var supportedFiles = details.supportedFiles;
                var contentType = details.contentType;

                // for the specified content type, match all retrieved files to local database game entries
                // - any files that can't be matched are designated as 'unknownFiles'.. which form part of 'unmatchedFiles'
                var unmatchedSupportedFiles = MatchFilesToLocal(games, supportedFiles, contentType, game => game.Content.ContentHitsCollection.First(contentHits => contentHits.Enum == contentType.Enum),
                    (fileName, _) => updateProgress($"{contentType.Description}: {fileName}", ++fileCount / (float)totalFilesCount));

                // unmatched files = unmatchedSupportedFiles (supported file type, but failed to match) + unsupportedFiles (unsupported file type)
                unmatchedFiles.AddRange(unmatchedSupportedFiles);

                // identify any unsupported files, i.e. files in the directory that don't have a matching extension
                if (includeUnsupportedFiles)
                {
                    var unsupportedFiles = GetNonContentFileDetails(contentType, contentType.Folder);

                    // only applicable for media file types, since the 'table files' typically include misc support files (e.g. vbs, pdf, txt, etc)
                    if (contentType.Category == ContentTypeCategoryEnum.Media)
                        unmatchedFiles.AddRange(unsupportedFiles);
                }
            });

            // update each table status as missing if their were no matches
            AddMissingStatus(games);

            // unmatchedFiles = unknownFiles + unsupportedFiles
            return unmatchedFiles;
        }

        private static void AddMissingStatus(List<LocalGame> games)
        {
            games.ForEach(game =>
            {
                // add missing content
                game.Content.ContentHitsCollection.ForEach(contentHitCollection =>
                {
                    if (!contentHitCollection.Hits.Any(hit => hit.Type == HitTypeEnum.CorrectName || hit.Type == HitTypeEnum.WrongCase))
                        contentHitCollection.Add(HitTypeEnum.Missing, Content.GetName(game, contentHitCollection.ContentType.Category));
                });
            });
        }


        private static void LogDatabaseStatistics(IReadOnlyCollection<LocalGame> localGames, string file = null)
        {
            Logger.Info(
                $"Local database {(file == null ? "total" : "file")}: count={localGames.Count} (manufactured={localGames.Count(onlineGame => !onlineGame.Derived.IsOriginal)}, original={localGames.Count(onlineGame => onlineGame.Derived.IsOriginal)})" +
                $"{(file == null ? "" : ", file: " + file)}");
        }

        private static void NavigateToIpdb(string url) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}