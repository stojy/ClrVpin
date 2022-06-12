using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using ClrVpin.Converters;
using ClrVpin.Logging;
using ClrVpin.Models.Importer.Vps;
using ClrVpin.Models.Shared.Database;
using ClrVpin.Shared;
using Utils.Extensions;

namespace ClrVpin.Importer
{
    internal static class ImporterUtils
    {
        static ImporterUtils()
        {
            _jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new UnixToNullableDateTimeConverter { IsFormatInSeconds = false } }
            };
        }

        public static async Task<ImporterMatchStatistics> MatchOnlineToLocalAsync(List<Game> games, List<OnlineGame> onlineGames, Action<string, float?> updateProgress)
        {
            return await Task.Run(() => MatchOnlineToLocal(games, onlineGames, updateProgress));
        }

        public static async Task MatchLocalToOnlineAsync(List<Game> games, List<OnlineGame> onlineGames, ImporterMatchStatistics matchStatistics, Action<string, float?> updateProgress)
        {
            await Task.Run(() => MatchLocalToOnline(games, onlineGames, matchStatistics, updateProgress));
        }

        public static async Task<List<OnlineGame>> GetOnlineDatabase()
        {
            // create dictionary items upfront to ensure the preferred display ordering (for statistics)
            _feedFixStatistics.Clear();
            _feedFixStatistics.Add(FixGameNameWhitespace, 0);
            _feedFixStatistics.Add(FixGameManufacturerWhitespace, 0);
            _feedFixStatistics.Add(FixGameMissingImage, 0);
            _feedFixStatistics.Add(FixGameCreatedTime, 0);
            _feedFixStatistics.Add(FixGameUpdatedTimeTooLow, 0);
            _feedFixStatistics.Add(FixGameUpdatedTimeTooHigh, 0);
            _feedFixStatistics.Add(FixFileUpdateTimeOrdering, 0);
            _feedFixStatistics.Add(FixFileUpdatedTime, 0);
            _feedFixStatistics.Add(FixInvalidUrl, 0);
            _feedFixStatistics.Add(FixWrongUrl, 0);

            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(60),
                MaxResponseContentBufferSize = 10 * 1024 * 1024 // 10MB
            };

            var onlineGames = (await httpClient.GetFromJsonAsync<OnlineGame[]>(VisualPinballSpreadsheetDatabaseUrl, _jsonSerializerOptions))!.ToList();

            Logger.Info($"Online database table count: {onlineGames.Count} (manufactured={onlineGames.Count(onlineGame => !onlineGame.IsOriginal)}, original={onlineGames.Count(onlineGame => onlineGame.IsOriginal)})");
            return onlineGames;
        }

        public static Dictionary<string, int> FixOnlineDatabase(List<OnlineGame> games)
        {
            // fix game ordering - alphanumerical
            var orderedDames = games.OrderBy(game => game.Name).ToArray();
            games.Clear();
            games.AddRange(orderedDames);

            // various updates and/or fixes to the feed
            games.ForEach((game, index) =>
            {
                game.Index = index + 1;

                // group files into collections so they can be treated generically
                game.AllFiles = new Dictionary<string, FileCollection>
                {
                    { nameof(game.TableFiles), new FileCollection(game.TableFiles) },
                    { nameof(game.B2SFiles), new FileCollection(game.B2SFiles) },
                    { nameof(game.RuleFiles), new FileCollection(game.RuleFiles) },
                    { nameof(game.AltColorFiles), new FileCollection(game.AltColorFiles) },
                    { nameof(game.AltSoundFiles), new FileCollection(game.AltSoundFiles) },
                    { nameof(game.MediaPackFiles), new FileCollection(game.MediaPackFiles) },
                    { nameof(game.PovFiles), new FileCollection(game.PovFiles) },
                    { nameof(game.PupPackFiles), new FileCollection(game.PupPackFiles) },
                    { nameof(game.RomFiles), new FileCollection(game.RomFiles) },
                    { nameof(game.SoundFiles), new FileCollection(game.SoundFiles) },
                    { nameof(game.TopperFiles), new FileCollection(game.TopperFiles) },
                    { nameof(game.WheelArtFiles), new FileCollection(game.WheelArtFiles) }
                };
                game.AllFilesList = game.AllFiles.Select(kv => kv.Value).SelectMany(x => x);
                game.ImageFiles = game.TableFiles.Concat(game.B2SFiles).ToList();

                Fix(game);

                // copy the dictionary files (potentially re-arranged, filtered, etc) back to the lists to ensure they are in sync
                game.TableFiles = game.AllFiles[nameof(game.TableFiles)].Cast<TableFile>().ToArray();
                game.B2SFiles = game.B2SFiles.OrderByDescending(x => x.UpdatedAt).ToArray();
                game.WheelArtFiles = game.WheelArtFiles.OrderByDescending(x => x.UpdatedAt).ToArray();
                game.RomFiles = game.RomFiles.OrderByDescending(x => x.UpdatedAt).ToArray();
                game.MediaPackFiles = game.MediaPackFiles.OrderByDescending(x => x.UpdatedAt).ToArray();
                game.AltColorFiles = game.AltColorFiles.OrderByDescending(x => x.UpdatedAt).ToArray();
                game.SoundFiles = game.SoundFiles.OrderByDescending(x => x.UpdatedAt).ToArray();
                game.TopperFiles = game.TopperFiles.OrderByDescending(x => x.UpdatedAt).ToArray();
                game.PupPackFiles = game.PupPackFiles.OrderByDescending(x => x.UpdatedAt).ToArray();
                game.PovFiles = game.PovFiles.OrderByDescending(x => x.UpdatedAt).ToArray();
                game.AltSoundFiles = game.AltSoundFiles.OrderByDescending(x => x.UpdatedAt).ToArray();
                game.RuleFiles = game.RuleFiles.OrderByDescending(x => x.UpdatedAt).ToArray();
            });

            return _feedFixStatistics;
        }

        private static ImporterMatchStatistics MatchOnlineToLocal(IList<Game> games, ICollection<OnlineGame> onlineGames, Action<string, float?> updateProgress)
        {
            var matchStatistics = new ImporterMatchStatistics();

            onlineGames.ForEach((onlineGame, i) =>
            {
                updateProgress(onlineGame.Name, (i + 1f) / onlineGames.Count);

                // unlike rebuilder matching, only fuzzy is used

                // use GetNameDetails for NameNoWhiteSpace and ActualName
                var fuzzyNameDetails = Fuzzy.GetNameDetails(onlineGame.Name, false);
                fuzzyNameDetails.Manufacturer = onlineGame.Manufacturer;
                fuzzyNameDetails.Year = onlineGame.Year;

                var (matchedGame, score) = games.Match(fuzzyNameDetails);
                if (matchedGame != null)
                {
                    matchStatistics.Add(ImporterMatchStatistics.MatchedTotal);
                    matchStatistics.Add(onlineGame.IsOriginal ? ImporterMatchStatistics.MatchedOriginal : ImporterMatchStatistics.MatchedManufactured);

                    onlineGame.Hit = new GameHit
                    {
                        Game = matchedGame,
                        Score = score
                    };
                }
                else
                {
                    matchStatistics.Add(ImporterMatchStatistics.UnmatchedOnlineTotal);
                    matchStatistics.Add(onlineGame.IsOriginal ? ImporterMatchStatistics.UnmatchedOnlineOriginal : ImporterMatchStatistics.UnmatchedOnlineManufactured);
                }
            });

            return matchStatistics;
        }
        
        private static void MatchLocalToOnline(IEnumerable<Game> games, IEnumerable<OnlineGame> onlineGames, ImporterMatchStatistics matchStatistics, Action<string, float?> updateProgress)
        {
            var unmatchedGames = games.Except(onlineGames.Where(onlineGame => onlineGame.Hit != null).Select(onlineGame => onlineGame.Hit.Game)).ToList();

            // deliberately NOT performing a 'reverse' fuzzy lookup to avoid scenario where x1 online game could have multiple local files
            // - e.g. online only has 1 AC/DC entry (which is a known issue).. whereas there are multiple local files each representing the unique IPDBs (which is correct)
            unmatchedGames.ForEach((localGame, _) =>
            {
                updateProgress(localGame.Name, null);

                Logger.Info($"Unmatched local table: '{localGame.Name}'");
                
                matchStatistics.Add(ImporterMatchStatistics.UnmatchedLocalTotal);
                matchStatistics.Add(localGame.Derived.IsOriginal ? ImporterMatchStatistics.UnmatchedLocalOriginal : ImporterMatchStatistics.UnmatchedLocalManufactured);
            });
        }

        private static void Fix(OnlineGame onlineGame)
        {
            // fix image url - assign to the first available image url.. B2S then table
            if (onlineGame.ImgUrl == null)
            {
                var imageUrl = onlineGame.B2SFiles.FirstOrDefault(x => x.ImgUrl != null)?.ImgUrl ?? onlineGame.TableFiles.FirstOrDefault(x => x.ImgUrl != null)?.ImgUrl;
                if (imageUrl != null)
                {
                    LogFixed(onlineGame, FixGameMissingImage, $"url='{imageUrl}'");
                    onlineGame.ImgUrl = imageUrl;
                }
            }

            // fix game name - remove whitespace
            if (onlineGame.Name != onlineGame.Name.Trim())
            {
                LogFixed(onlineGame, FixGameNameWhitespace);
                onlineGame.Name = onlineGame.Name.Trim();
            }

            // fix manufacturer - remove whitespace
            if (onlineGame.Manufacturer != onlineGame.Manufacturer.Trim())
            {
                LogFixed(onlineGame, FixGameManufacturerWhitespace, $"manufacturer='{onlineGame.Manufacturer}'");
                onlineGame.Manufacturer = onlineGame.Manufacturer.Trim();
            }

            // fix updated timestamp - must not be lower than the created timestamp
            onlineGame.AllFiles.ForEach(kv =>
            {
                kv.Value.Where(f => f.UpdatedAt < f.CreatedAt).ForEach(f =>
                {
                    LogFixedTimestamp(onlineGame, FixFileUpdatedTime, "updatedAt", f.UpdatedAt, "   createdAt", f.CreatedAt);
                    f.UpdatedAt = f.CreatedAt;
                });
            });

            // fix game created timestamp - must not be less than any file timestamps
            var maxCreatedAt = onlineGame.AllFilesList.Max(x => x.CreatedAt);
            if (onlineGame.LastCreatedAt < maxCreatedAt)
            {
                LogFixedTimestamp(onlineGame, FixGameCreatedTime, "createdAt", onlineGame.LastCreatedAt, nameof(maxCreatedAt), maxCreatedAt);
                onlineGame.LastCreatedAt = maxCreatedAt;
            }

            // fix game updated timestamp - must not be less than the max file timestamp
            var maxUpdatedAt = onlineGame.AllFilesList.Max(x => x.UpdatedAt);
            if (onlineGame.UpdatedAt < maxUpdatedAt)
            {
                LogFixedTimestamp(onlineGame, FixGameUpdatedTimeTooLow, "updatedAt", onlineGame.UpdatedAt, nameof(maxUpdatedAt), maxUpdatedAt);
                onlineGame.UpdatedAt = maxUpdatedAt;
            }
            else if (onlineGame.UpdatedAt > maxUpdatedAt)
            {
                LogFixedTimestamp(onlineGame, FixGameUpdatedTimeTooHigh, "updatedAt", onlineGame.UpdatedAt, nameof(maxUpdatedAt), maxUpdatedAt, true);
                onlineGame.UpdatedAt = maxUpdatedAt;
            }

            // fix file ordering - ensure a game's most recent files are shown first
            onlineGame.AllFiles.ForEach(kv =>
            {
                var orderByDescending = kv.Value.OrderByDescending(x => x.UpdatedAt).ToArray();
                if (!kv.Value.SequenceEqual(orderByDescending))
                {
                    LogFixed(onlineGame, FixFileUpdateTimeOrdering, $"type={kv.Key}");
                    kv.Value.Clear();
                    kv.Value.AddRange(orderByDescending);
                }
            });

            // fix urls
            onlineGame.AllFiles.ForEach(kv =>
            {
                kv.Value.ForEach(f =>
                    f.Urls.ForEach(urlDetail =>
                    {
                        // fix urls - mark any invalid urls, e.g. Abra Ca Dabra ROM url is a string warning "copyright notices"
                        if (!urlDetail.Broken && !(Uri.TryCreate(urlDetail.Url, UriKind.Absolute, out var generatedUrl) && (generatedUrl.Scheme == Uri.UriSchemeHttp || generatedUrl.Scheme == Uri.UriSchemeHttps)))
                        {
                            LogFixed(onlineGame, FixInvalidUrl, $"type={kv.Key} url={urlDetail.Url}");
                            urlDetail.Broken = true;
                        }

                        // fix vpuniverse urls - path
                        if (urlDetail.Url?.Contains("//vpuniverse.com/forums") == true)
                        {
                            LogFixed(onlineGame, FixWrongUrl, $"type={kv.Key} url={urlDetail.Url}");
                            urlDetail.Url = urlDetail.Url.Replace("//vpuniverse.com/forums", "//vpuniverse.com");
                        }
                    })
                );
            });

            // fix invalid IPDB Url
            // - e.g. "Not Available" frequently used for original tables
            if (!(Uri.TryCreate(onlineGame.IpdbUrl, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)))
            {
                LogFixed(onlineGame, FixInvalidIpdbUrl, $"type=IPDB url={onlineGame.IpdbUrl}");
                onlineGame.IpdbUrl = null;
            }

            // fix wrong IPDB url
            // - original tables shouldn't reference a manufactured table.. but sometimes happens as a reference to the inspiration table
            if (onlineGame.IsOriginal && onlineGame.IpdbUrl != null)
            {
                LogFixed(onlineGame, FixWrongIpdbUrl, $"type=IPDB url={onlineGame.IpdbUrl}");
                onlineGame.IpdbUrl = null;
            }
        }

        private static void LogFixedTimestamp(OnlineGame onlineGame, string type, string gameTimeName, DateTime? gameTime, string maxFileTimeName, DateTime? maxFileTime, bool greaterThan = false)
        {
            LogFixed(onlineGame, type, $"game.{gameTimeName} '{gameTime:dd/MM/yy HH:mm:ss}' {(greaterThan ? ">" : "<")} {maxFileTimeName} '{maxFileTime:dd/MM/yy HH:mm:ss}'");
        }

        private static void LogFixed(OnlineGame onlineGame, string type, string details = null)
        {
            AddFixStatistic(type);

            var name = $"'{onlineGame.Name[..Math.Min(onlineGame.Name.Length, 23)].Trim()}'";
            Logger.Warn($"Fixed {type,-26} index={onlineGame.Index:0000} name={name,-25} {details}", true);
        }

        private static void AddFixStatistic(string key)
        {
            _feedFixStatistics.TryAdd(key, 0);
            _feedFixStatistics[key]++;
        }

        // refer https://github.com/Fraesh/vps-db, https://virtual-pinball-spreadsheet.web.app/
        private const string VisualPinballSpreadsheetDatabaseUrl = "https://raw.githubusercontent.com/Fraesh/vps-db/master/vpsdb.json";

        private const string FixGameNameWhitespace = "Game Name Whitespace";
        private const string FixGameMissingImage = "Game Missing Image Url";
        private const string FixGameManufacturerWhitespace = "Game Manufacturer Whitespace";
        private const string FixGameCreatedTime = "Game Created Time";
        private const string FixGameUpdatedTimeTooLow = "Game Updated Time Too Low";
        private const string FixGameUpdatedTimeTooHigh = "Game Updated Time Too High";
        private const string FixFileUpdateTimeOrdering = "File Update Time Ordering";
        private const string FixFileUpdatedTime = "File Updated Time";
        private const string FixInvalidUrl = "Invalid Url";
        private const string FixWrongUrl = "Wrong Url";
        private const string FixInvalidIpdbUrl = "Invalid IPDB Url";
        private const string FixWrongIpdbUrl = "Wrong IPDB Url";

        private static readonly JsonSerializerOptions _jsonSerializerOptions;
        private static readonly Dictionary<string, int> _feedFixStatistics = new Dictionary<string, int>();
    }
}