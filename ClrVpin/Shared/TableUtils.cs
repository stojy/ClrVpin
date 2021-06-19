using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ClrVpin.Logging;
using ClrVpin.Models;
using Utils;

namespace ClrVpin.Shared
{
    public static class TableUtils
    {
        public static List<Game> GetGamesFromDatabases()
        {
            var databaseDetail = Model.Config.GetFrontendFolders().First(x => x.IsDatabase);

            // scan through all the databases in the folder
            var files = Directory.EnumerateFiles(databaseDetail.Folder, databaseDetail.Extensions);

            var games = new List<Game>();

            files.ForEach(file =>
            {
                var doc = XDocument.Load(file);
                if (doc.Root == null)
                    throw new Exception($"Failed to load database: '{file}'");

                var menu = doc.Root.Deserialize<Menu>();
                var number = 1;
                menu.Games.ForEach(g =>
                {
                    g.Number = number++;
                    g.Ipdb = g.IpdbId ?? g.IpdbNr;
                    g.IpdbUrl = string.IsNullOrEmpty(g.Ipdb) ? "" : $"https://www.ipdb.org/machine.cgi?id={g.Ipdb}";
                    g.NavigateToIpdbCommand = new ActionCommand(() => NavigateToIpdb(g.IpdbUrl));
                });

                games.AddRange(menu.Games);
            });

            return games;
        }

        public static IEnumerable<string> GetMediaFileNames(ContentType contentType, string folder)
        {
            var files = contentType.ExtensionsList.Select(ext => Directory.EnumerateFiles(folder, ext));

            return files.SelectMany(x => x).ToList();
        }

        public static IEnumerable<FixFileDetail> GetUnsupportedMediaFileDetails(ContentType contentType, string folder)
        {
            // return all files that don't match the supported file extensions
            var supportedExtensions = contentType.ExtensionsList.Select(x => x.TrimStart('*').ToLower());

            var allFiles = Directory.EnumerateFiles(folder).Select(x => x.ToLower());

            var unsupportedFiles = allFiles.Where(file => !supportedExtensions.Any(file.EndsWith));

            var unsupportedFixFiles = unsupportedFiles.Select(file => new FixFileDetail(contentType.Enum, HitTypeEnum.Unsupported, false, false, file, new FileInfo(file).Length));

            return unsupportedFixFiles.ToList();
        }

        public static string GetActiveBackupFolder(string backupFolder)
        {
            return _activeBackupFolder = $"{backupFolder}\\{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
        }

        public static IEnumerable<FixFileDetail> AssociateMediaFilesToGames(IReadOnlyCollection<Game> games, IEnumerable<string> mediaFiles, ContentTypeEnum contentTypeEnum,
            Func<Game, ContentHits> getContentHits)
        {
            var unknownMediaFiles = new List<FixFileDetail>();

            // for each file, associate it with a game or if one can't be found, then mark it as unknown
            foreach (var mediaFile in mediaFiles)
            {
                Game matchedGame;

                // check for hit..
                // - only 1 hit per file.. but a game can have multiple hits.. with a maximum of 1 valid hit
                // - ignores the check criteria.. the check criteria is only used in the results (e.g. statistics)
                // - todo; fuzzy match.. e.g. partial matches, etc.
                if ((matchedGame = games.FirstOrDefault(game => game.Description == Path.GetFileNameWithoutExtension(mediaFile))) != null)
                {
                    // if a match already exists, then assume this match is a duplicate name with wrong extension
                    // - file extension order is important as it determines the priority of the preferred extension
                    var contentHits = getContentHits(matchedGame);
                    contentHits.Add(contentHits.Hits.Any(hit => hit.Type == HitTypeEnum.Valid) ? HitTypeEnum.DuplicateExtension : HitTypeEnum.Valid, mediaFile);
                }
                else if ((matchedGame =
                    games.FirstOrDefault(game => string.Equals(game.Description, Path.GetFileNameWithoutExtension(mediaFile), StringComparison.CurrentCultureIgnoreCase))) != null)
                {
                    getContentHits(matchedGame).Add(HitTypeEnum.WrongCase, mediaFile);
                }
                else if ((matchedGame = games.FirstOrDefault(game => game.TableFile == Path.GetFileNameWithoutExtension(mediaFile))) != null)
                {
                    getContentHits(matchedGame).Add(HitTypeEnum.TableName, mediaFile);
                }
                else if ((matchedGame = games.FirstOrDefault(game =>
                        game.TableFile.StartsWith(Path.GetFileNameWithoutExtension(mediaFile)) || Path.GetFileNameWithoutExtension(mediaFile).StartsWith(game.TableFile) ||
                        game.Description.StartsWith(Path.GetFileNameWithoutExtension(mediaFile)) || Path.GetFileNameWithoutExtension(mediaFile).StartsWith(game.Description))
                    ) != null)
                {
                    // todo; add more 'fuzzy' checks
                    getContentHits(matchedGame).Add(HitTypeEnum.Fuzzy, mediaFile);
                }
                else
                {
                    unknownMediaFiles.Add(new FixFileDetail(contentTypeEnum, HitTypeEnum.Unknown, false, false, mediaFile, new FileInfo(mediaFile).Length));
                }
            }

            return unknownMediaFiles;
        }

        private static void NavigateToIpdb(string url) => Process.Start(new ProcessStartInfo(url) {UseShellExecute = true});

        private static bool TryGet(IEnumerable<Hit> hits, out Hit hit, params HitTypeEnum[] hitTypes)
        {
            // return the first entry found
            hit = hits.FirstOrDefault(h => hitTypes.Contains(h.Type));
            return hit != null;
        }

        private static IEnumerable<FixFileDetail> DeleteAllExcept(IEnumerable<Hit> hits, Hit hit)
        {
            var deleted = new List<FixFileDetail>();

            // delete all 'real' files except the specified hit
            hits.Except(hit).Where(x => x.Size.HasValue).ForEach(h => deleted.Add(Delete(h)));

            return deleted;
        }

        private static FixFileDetail Delete(Hit hit)
        {
            var deleted = false;

            // only delete file if configured to do so
            if (Model.Config.SelectedFixHitTypes.Contains(hit.Type))
            {
                deleted = true;
                Delete(hit.Path, hit.Type, hit.ContentType);
            }

            return new FixFileDetail(hit.ContentTypeEnum, hit.Type, deleted, false, hit.Path, hit.Size ?? 0);
        }

        private static void Delete(string file, HitTypeEnum hitType, string contentType)
        {
            var backupFileName = CreateBackupFileName(file);

            var prefix = Model.Config.TrainerWheels ? "Skipped (trainer wheels are on) " : "";
            Logger.Warn($"{prefix}Deleting file.. type: {hitType.GetDescription()}, content: {contentType ?? "n/a"}, file: {file}, backup: {backupFileName}");

            if (!Model.Config.TrainerWheels)
                File.Move(file, backupFileName, true);
        }

        private static FixFileDetail Rename(Hit hit, Game game)
        {
            var renamed = false;

            if (Model.Config.SelectedFixHitTypes.Contains(hit.Type))
            {
                renamed = true;

                var extension = Path.GetExtension(hit.Path);
                var path = Path.GetDirectoryName(hit.Path);
                var newFile = Path.Combine(path!, $"{game.Description}{extension}");

                var backupFileName = CreateBackupFileName(hit.Path);
                var prefix = Model.Config.TrainerWheels ? "Skipped (trainer wheels are on) " : "";
                Logger.Info($"{prefix}Renaming file.. type: {hit.Type.GetDescription()}, content: {hit.ContentType}, original: {hit.Path}, new: {newFile}, backup: {backupFileName}");

                if (!Model.Config.TrainerWheels)
                {
                    File.Copy(hit.Path!, backupFileName, true);
                    File.Move(hit.Path!, newFile, true);
                }
            }

            return new FixFileDetail(hit.ContentTypeEnum, hit.Type, false, renamed, hit.Path, hit.Size ?? 0);
        }

        private static string CreateBackupFileName(string file)
        {
            var baseFolder = Path.GetDirectoryName(file)!.Split("\\").Last();
            var folder = Path.Combine(_activeBackupFolder, baseFolder);
            var destFileName = Path.Combine(folder, Path.GetFileName(file));

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            return destFileName;
        }

        private static void CheckMissing(List<Game> games)
        {
            games.ForEach(game =>
            {
                // add missing content
                game.Content.ContentHitsCollection.ForEach(contentHitCollection =>
                {
                    if (!contentHitCollection.Hits.Any(hit => hit.Type == HitTypeEnum.Valid || hit.Type == HitTypeEnum.WrongCase))
                        contentHitCollection.Add(HitTypeEnum.Missing, game.Description);
                });
            });
        }

        private static string _activeBackupFolder;
    }
}