using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ClrVpin.Logging;
using ClrVpin.Models;
using Utils;

namespace ClrVpin.Scanner
{
    public static class ScannerUtils
    {
        public static List<FixFileDetail> Check(List<Game> games)
        {
            var unknownFiles = new List<FixFileDetail>();

            // for the configured content types only.. check the installed content files against those specified in the database
            var checkContentTypes = Model.Config.GetFrontendFolders()
                .Where(x => !x.IsDatabase)
                .Where(type => Model.Config.CheckContentTypes.Contains(type.Type));

            checkContentTypes.ForEach(contentType =>
            {
                var mediaFiles = GetMedia(contentType);
                var unknownMedia = AddMedia(games, mediaFiles, game => game.Content.ContentHitsCollection.First(contentHits => contentHits.Type == contentType.Type));

                // todo; add non-media content, e.g. tables and b2s

                unknownFiles.AddRange(unknownMedia);
            });

            CheckMissing(games);

            return unknownFiles;
        }

        public static List<Game> GetDatabases()
        {
            var databaseDetail = Model.Config.GetFrontendFolders().First(x => x.IsDatabase);

            // scan through all the databases in the folder
            var files = Directory.GetFiles(databaseDetail.Folder, databaseDetail.Extensions);

            var games = new List<Game>();

            files.ForEach(file =>
            {
                var doc = XDocument.Load(file);
                if (doc.Root == null)
                    throw new Exception("Failed to load database");

                var menu = doc.Root.Deserialize<Menu>();
                var number = 1;
                menu.Games.ForEach(g =>
                {
                    g.Number = number++;
                    g.Ipdb = g.IpdbId ?? g.IpdbNr;
                });

                games.AddRange(menu.Games);
            });

            return games;
        }

        public static List<FixFileDetail> Fix(List<Game> games, List<FixFileDetail> unknownFileDetails, string backupFolder)
        {
            _activeBackupFolder = $"{backupFolder}\\{DateTime.Now:yyyy-MM-dd_hh-mm-ss}";
            
            var fixedFileDetails = new List<FixFileDetail>();

            // fix files associated with games
            games.ForEach(game =>
            {
                game.Content.ContentHitsCollection.ForEach(contentHitCollection =>
                {
                    if (TryGet(contentHitCollection.Hits, out var hit, HitType.Valid))
                    {
                        // valid hit exists.. so delete everything else
                        fixedFileDetails.AddRange(DeleteAllExcept(contentHitCollection.Hits, hit));
                    }
                    else if (TryGet(contentHitCollection.Hits, out hit, HitType.WrongCase, HitType.TableName, HitType.Fuzzy))
                    {
                        // for all 3 hit types.. rename file and delete other entries
                        fixedFileDetails.Add(Rename(hit, game));
                        fixedFileDetails.AddRange(DeleteAllExcept(contentHitCollection.Hits, hit));
                    }

                    // other hit types are n/a
                    // - duplicate extension - already taken care as a valid hit will exist
                    // - unknown - not associated with a game.. handled elsewhere
                    // - missing - can't be fixed.. requires file to be downloaded
                });
            });

            // delete files NOT associated with games, i.e. unknown files
            unknownFileDetails.ForEach(x =>
            {
                if (Model.Config.FixHitTypes.Contains(HitType.Unknown))
                {
                    x.Deleted = true;
                    Delete(x.Path, x.HitType, null);
                }
            });

            return fixedFileDetails;
        }

        private static bool TryGet(IEnumerable<Hit> hits, out Hit hit, params HitType[] hitTypes)
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
            if (Model.Config.FixHitTypes.Contains(hit.Type))
            {
                deleted = true;
                Delete(hit.Path, hit.Type, hit.ContentType);
            }

            return new FixFileDetail(hit.Type, deleted, false, hit.Path, hit.Size ?? 0);
        }

        private static void Delete(string file, HitType hitType, string contentType)
        {
            var backupFileName = CreateBackupFileName(file);

            Logger.Warn($"Deleting file.. type: {hitType.GetDescription()}, content: {contentType ?? "n/a"}, file: {file}, backup: {backupFileName}");

            File.Move(file, backupFileName, true);
        }

        private static FixFileDetail Rename(Hit hit, Game game)
        {
            var renamed = false;

            if (Model.Config.FixHitTypes.Contains(hit.Type))
            {
                renamed = true;

                var extension = Path.GetExtension(hit.Path);
                var path = Path.GetDirectoryName(hit.Path);
                var newFile = Path.Combine(path!, $"{game.Description}{extension}");

                var backupFileName = CreateBackupFileName(hit.Path);
                Logger.Info($"Renaming file.. type: {hit.Type.GetDescription()}, content: {hit.ContentType}, original: {hit.Path}, new: {newFile}, backup: {backupFileName}");

                File.Copy(hit.Path!, backupFileName, true);
                File.Move(hit.Path!, newFile, true);
            }

            return new FixFileDetail(hit.Type, false, renamed, hit.Path, hit.Size ?? 0);
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
                    if (!contentHitCollection.Hits.Any(hit => hit.Type == HitType.Valid || hit.Type == HitType.WrongCase))
                        contentHitCollection.Add(HitType.Missing, game.Description);
                });
            });
        }

        private static IEnumerable<FixFileDetail> AddMedia(IReadOnlyCollection<Game> games, IEnumerable<string> mediaFiles, Func<Game, ContentHits> getContentHits)
        {
            var unknownMediaFiles = new List<FixFileDetail>();

            mediaFiles.ForEach(mediaFile =>
            {
                Game matchedGame;

                // check for hit..
                // - skip hit types that aren't configured
                // - only 1 hit per file.. but a game can have multiple hits.. with a maximum of 1 valid hit
                // - todo; fuzzy match.. e.g. partial matches, etc.
                if ((matchedGame = games.FirstOrDefault(game => game.Description == Path.GetFileNameWithoutExtension(mediaFile))) != null)
                {
                    // if a match already exists, then assume this match is a duplicate name with wrong extension
                    // - file extension order is important as it determines the priority of the preferred extension
                    var contentHits = getContentHits(matchedGame);
                    contentHits.Add(contentHits.Hits.Any(hit => hit.Type == HitType.Valid) ? HitType.DuplicateExtension : HitType.Valid, mediaFile);
                }
                else if ((matchedGame = games.FirstOrDefault(game =>
                    string.Equals(game.Description, Path.GetFileNameWithoutExtension(mediaFile), StringComparison.CurrentCultureIgnoreCase))) != null)
                {
                    getContentHits(matchedGame).Add(HitType.WrongCase, mediaFile);
                }
                else if ((matchedGame = games.FirstOrDefault(game => game.TableFile == Path.GetFileNameWithoutExtension(mediaFile))) != null)
                {
                    getContentHits(matchedGame).Add(HitType.TableName, mediaFile);
                }
                else
                {
                    unknownMediaFiles.Add(new FixFileDetail(HitType.Unknown, false, false, mediaFile, new FileInfo(mediaFile).Length));
                }
            });

            return unknownMediaFiles;
        }

        private static IEnumerable<string> GetMedia(ContentType contentType)
        {
            var files = contentType.ExtensionsList.Select(ext => Directory.GetFiles(contentType.Folder, ext));

            return files.SelectMany(x => x).ToList();
        }

        private static string _activeBackupFolder;
    }
}