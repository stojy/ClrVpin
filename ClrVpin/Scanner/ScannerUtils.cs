using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ClrVpin.Models;
using NLog;
using Utils;

namespace ClrVpin.Scanner
{
    public static class ScannerUtils
    {
        public static List<FixFileDetail> Check(List<Game> games)
        {
            var unknownFiles = new List<FixFileDetail>();

            // for the configured content types only.. check the installed content files against those specified in the database
            var checkContentTypes = Content.SupportedTypes.Where(type => Config.CheckContentTypes.Contains(type.Type));
            checkContentTypes.ForEach(contentSetup =>
            {
                var mediaFiles = GetMedia(contentSetup);
                var unknownMedia = AddMedia(games, mediaFiles, contentSetup.GetContentHits);

                // todo; add non-media content, e.g. tables and b2s

                unknownFiles.AddRange(unknownMedia);
            });

            CheckMissing(games);

            return unknownFiles;
        }

        public static List<Game> GetDatabase()
        {
            var file = $@"{Config.VpxFrontendFolder}\Databases\Visual Pinball\Visual Pinball.xml";
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

            return menu.Games;
        }

        public static List<FixFileDetail> Fix(List<Game> games)
        {
            var deletedHits = new List<FixFileDetail>();

            games.ForEach(game =>
            {
                game.Content.ContentHitsCollection.ForEach(contentHitCollection =>
                {
                    if (contentHitCollection.Hits.Any(hit => hit.Type == HitType.Valid))
                        // all hit files can be deleted :)
                        contentHitCollection.Hits.Where(hit => hit.Type != HitType.Valid).ForEach(hit =>
                        {
                            switch (hit.Type)
                            {
                                case HitType.DuplicateExtension:
                                case HitType.Fuzzy:
                                case HitType.TableName:
                                case HitType.WrongCase:
                                    deletedHits.Add(Delete(hit));
                                    break;
                            }
                        });
                });
            });

            return deletedHits;
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
                    unknownMediaFiles.Add(new FixFileDetail(HitType.Unknown, false, mediaFile, new FileInfo(mediaFile).Length));
                }
            });

            return unknownMediaFiles;
        }

        private static IEnumerable<string> GetMedia(ContentType contentType)
        {
            var files = contentType.Extensions.Select(ext => Directory.GetFiles(contentType.QualifiedFolder, ext));

            return files.SelectMany(x => x).ToList();
        }

        private static FixFileDetail Delete(Hit hit)
        {
            var deleted = false;
            if (Config.FixHitTypes.Contains(hit.Type))
            {
                deleted = true;
                Logger.Info($"deleting: type={hit.Type}, content={hit.ContentType}, path={hit.Path}");
            }

            return new FixFileDetail(hit.Type, deleted, hit.Path, hit.Size);
        }

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    }
}