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
        public static string ActiveBackupFolder { get; private set; }

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

        public static IEnumerable<FileDetail> GetUnsupportedMediaFileDetails(ContentType contentType, string folder)
        {
            // return all files that don't match the supported file extensions
            var supportedExtensions = contentType.ExtensionsList.Select(x => x.TrimStart('*').ToLower());

            var allFiles = Directory.EnumerateFiles(folder).Select(x => x.ToLower());

            var unsupportedFiles = allFiles.Where(file => !supportedExtensions.Any(file.EndsWith));

            var unsupportedFixFiles = unsupportedFiles.Select(file => new FileDetail(contentType.Enum, HitTypeEnum.Unsupported, null, file, new FileInfo(file).Length));

            return unsupportedFixFiles.ToList();
        }

        public static string SetActiveBackupFolder(string rootBackupFolder)
        {
            _rootBackupFolder = rootBackupFolder;
            return ActiveBackupFolder = $"{rootBackupFolder}\\{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
        }

        public static IEnumerable<FileDetail> AssociateMediaFilesWithGames(IReadOnlyCollection<Game> games, IEnumerable<string> mediaFiles, ContentTypeEnum contentTypeEnum,
            Func<Game, ContentHits> getContentHits)
        {
            var unknownMediaFiles = new List<FileDetail>();

            // for each file, associate it with a game or if one can't be found, then mark it as unknown
            // - ASSOCIATION IS DONE IRRESPECTIVE OF THE USER'S SELECTED PREFERENCE, I.E. THE USE SELECTIONS ARE CHECKED ELSEWHERE
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
                    unknownMediaFiles.Add(new FileDetail(contentTypeEnum, HitTypeEnum.Unknown, null, mediaFile, new FileInfo(mediaFile).Length));
                }
            }

            return unknownMediaFiles;
        }

        public static bool TryGet(IEnumerable<Hit> hits, out Hit hit, params HitTypeEnum[] hitTypes)
        {
            // return the first entry found
            hit = hits.FirstOrDefault(h => hitTypes.Contains(h.Type));
            return hit != null;
        }

        public static IEnumerable<FileDetail> DeleteAllExcept(IEnumerable<Hit> hits, Hit hit, ICollection<HitTypeEnum> supportedHitTypes)
        {
            var deleted = new List<FileDetail>();

            // delete all 'real' files except the specified hit
            hits.Except(hit).Where(x => x.Size.HasValue).ForEach(h => deleted.Add(Delete(h, supportedHitTypes)));

            return deleted;
        }

        public static void Delete(string file, HitTypeEnum hitType, string contentType)
        {
            var prefix = Model.Config.TrainerWheels ? "Skipped (trainer wheels are on) " : "";
            Logger.Warn($"{prefix}Deleting file.. type: {hitType.GetDescription()}, content: {contentType ?? "n/a"}, file: {file}");

            if (!Model.Config.TrainerWheels)
            {
                Backup(file, "deleted");
                File.Delete(file);
            }
        }

        public static FileDetail Rename(Hit hit, Game game, ICollection<HitTypeEnum> supportedHitTypes)
        {
            var renamed = false;

            if (supportedHitTypes.Contains(hit.Type))
            {
                renamed = true;

                var extension = Path.GetExtension(hit.Path);
                var path = Path.GetDirectoryName(hit.Path);
                var newFile = Path.Combine(path!, $"{game.Description}{extension}");

                var prefix = Model.Config.TrainerWheels ? "Skipped (trainer wheels are on) " : "";
                Logger.Info($"{prefix}Renaming file.. type: {hit.Type.GetDescription()}, content: {hit.ContentType}, original: {hit.Path}, new: {newFile}");

                if (!Model.Config.TrainerWheels)
                {
                    Backup(hit.Path, "renamed");
                    File.Move(hit.Path!, newFile, true);
                }
            }

            return new FileDetail(hit.ContentTypeEnum, hit.Type, renamed ? FixFileTypeEnum.Renamed : null, hit.Path, hit.Size ?? 0);
        }

        public static void Backup(string file, string subFolder = "")
        {
            // backup file (aka copy) to the specified sub folder
            var backupFile = CreateBackupFileName(file, subFolder);
            File.Copy(file, backupFile, true);
        }

        public static void DeleteActiveBackupFolderIfEmpty()
        {
            // delete empty backup folders - i.e. if there are no files (empty sub-directories are allowed)
            if (Directory.Exists(ActiveBackupFolder))
            {
                var files = Directory.EnumerateFiles(ActiveBackupFolder, "*", SearchOption.AllDirectories);
                if (!files.Any())
                {
                    Logger.Info($"Deleting empty backup folder: '{ActiveBackupFolder}'");
                    Directory.Delete(ActiveBackupFolder, true);
                }
            }

            // if directory doesn't exist (e.g. deleted as per above OR never existed), then assign the active folder back to the root folder, i.e. a valid folder that exists
            if (!Directory.Exists(ActiveBackupFolder)) 
                ActiveBackupFolder = _rootBackupFolder;
        }

        private static void NavigateToIpdb(string url) => Process.Start(new ProcessStartInfo(url) {UseShellExecute = true});

        private static FileDetail Delete(Hit hit, ICollection<HitTypeEnum> supportedHitTypes)
        {
            var deleted = false;

            // only delete file if configured to do so
            if (supportedHitTypes.Contains(hit.Type))
            {
                deleted = true;
                Delete(hit.Path, hit.Type, hit.ContentType);
            }

            return new FileDetail(hit.ContentTypeEnum, hit.Type, deleted ? FixFileTypeEnum.Deleted : null, hit.Path, hit.Size ?? 0);
        }

        private static string CreateBackupFileName(string file, string subFolder = "")
        {
            var contentFolder = Path.GetDirectoryName(file)!.Split("\\").Last();
            var folder = Path.Combine(ActiveBackupFolder, subFolder, contentFolder);
            var destFileName = Path.Combine(folder, Path.GetFileName(file));

            // store backup file in the same folder structure as the source file
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            return destFileName;
        }

        private static string _rootBackupFolder;
    }
}