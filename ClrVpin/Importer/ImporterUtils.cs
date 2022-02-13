using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using ClrVpin.Converters;
using ClrVpin.Importer.Vps;
using ClrVpin.Logging;
using Utils.Extensions;

namespace ClrVpin.Importer
{
    internal static class ImporterUtils
    {
        static ImporterUtils()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(60),
                MaxResponseContentBufferSize = 10 * 1024 * 1024
            };

            _jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new UnixToNullableDateTimeConverter { IsFormatInSeconds = false } }
            };
        }

        public static async Task<Game[]> GetOnlineDatabase()
        {
            // create dictionary items upfront to ensure the preferred display ordering (for statistics)
            _feedFixStatistics.Clear();
            _feedFixStatistics.Add(GameNameWhitespace, 0);
            _feedFixStatistics.Add(GameManufacturerWhitespace, 0);
            _feedFixStatistics.Add(GameMissingImage, 0);
            _feedFixStatistics.Add(GameCreatedTime, 0);
            _feedFixStatistics.Add(GameUpdatedTimeTooLow, 0);
            _feedFixStatistics.Add(GameUpdatedTimeTooHigh, 0);
            _feedFixStatistics.Add(FileUpdateTimeOrdering, 0);
            _feedFixStatistics.Add(FileUpdatedTime, 0);
            _feedFixStatistics.Add(InvalidUrl, 0);

            return await _httpClient.GetFromJsonAsync<Game[]>(VisualPinballSpreadsheetDatabaseUrl, _jsonSerializerOptions);
        }

        public static Dictionary<string, int> Update(Game[] games)
        {
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

        private static void Fix(Game game)
        {
            // fix image url - assign to the first available image url.. B2S then table
            if (game.ImgUrl == null)
            {
                var imageUrl = game.B2SFiles.FirstOrDefault(x => x.ImgUrl != null)?.ImgUrl ?? game.TableFiles.FirstOrDefault(x => x.ImgUrl != null)?.ImgUrl;
                if (imageUrl != null)
                {
                    LogFixed(game, GameMissingImage, $"url='{imageUrl}'");
                    game.ImgUrl = imageUrl;
                }
            }

            // fix game name - remove whitespace
            if (game.Name != game.Name.Trim())
            {
                LogFixed(game, GameNameWhitespace);
                game.Name = game.Name.Trim();
            }

            // fix manufacturer - remove whitespace
            if (game.Manufacturer != game.Manufacturer.Trim())
            {
                LogFixed(game, GameManufacturerWhitespace, $"manufacturer='{game.Manufacturer}'");
                game.Manufacturer = game.Manufacturer.Trim();
            }

            // fix updated timestamp - must not be lower than the created timestamp
            game.AllFiles.ForEach(kv =>
            {
                kv.Value.Where(f => f.UpdatedAt < f.CreatedAt).ForEach(f =>
                {
                    LogFixedTimestamp(game, FileUpdatedTime, "updatedAt", f.UpdatedAt, "   createdAt", f.CreatedAt);
                    f.UpdatedAt = f.CreatedAt;
                });
            });

            // fix game created timestamp - must not be less than any file timestamps
            var maxCreatedAt = game.AllFilesList.Max(x => x.CreatedAt);
            if (game.LastCreatedAt < maxCreatedAt)
            {
                LogFixedTimestamp(game, GameCreatedTime, "createdAt", game.LastCreatedAt, nameof(maxCreatedAt), maxCreatedAt);
                game.LastCreatedAt = maxCreatedAt;
            }

            // fix game updated timestamp - must not be equal to the max file timestamp
            var maxUpdatedAt = game.AllFilesList.Max(x => x.UpdatedAt);
            if (game.UpdatedAt < maxUpdatedAt)
            {
                LogFixedTimestamp(game, GameUpdatedTimeTooLow, "updatedAt", game.UpdatedAt, nameof(maxUpdatedAt), maxUpdatedAt);
                game.UpdatedAt = maxUpdatedAt;
            }
            else if (game.UpdatedAt > maxUpdatedAt)
            {
                LogFixedTimestamp(game, GameUpdatedTimeTooHigh, "updatedAt", game.UpdatedAt, nameof(maxUpdatedAt), maxUpdatedAt, true);
                game.UpdatedAt = maxUpdatedAt;
            }

            // fix file ordering - ensure a game's most recent files are shown first
            game.AllFiles.ForEach(kv =>
            {
                var orderByDescending = kv.Value.OrderByDescending(x => x.UpdatedAt).ToArray();
                if (!kv.Value.SequenceEqual(orderByDescending))
                {
                    LogFixed(game, FileUpdateTimeOrdering, $"type={kv.Key}");
                    kv.Value.Clear();
                    kv.Value.AddRange(orderByDescending);
                }
            });

            // fix urls - mark any invalid urls, e.g. Abra Ca Dabra ROM url is a string warning "copyright notices"
            game.AllFiles.ForEach(kv =>
            {
                kv.Value.ForEach(f =>
                    f.Urls.ForEach(urlDetail =>
                    {
                        if (!urlDetail.Broken && !(Uri.TryCreate(urlDetail.Url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)))
                        {
                            LogFixed(game, InvalidUrl, $"type={kv.Key} url={urlDetail.Url}");
                            urlDetail.Broken = true;
                        }
                    })
                );
            });
        }

        private static void LogFixedTimestamp(Game game, string type, string gameTimeName, DateTime? gameTime, string maxFileTimeName, DateTime? maxFileTime, bool greaterThan = false)
        {
            LogFixed(game, type, $"game.{gameTimeName} '{gameTime:dd/MM/yy HH:mm:ss}' {(greaterThan ? ">" : "<")} {maxFileTimeName} '{maxFileTime:dd/MM/yy HH:mm:ss}'");
        }

        private static void LogFixed(Game game, string type, string details = null)
        {
            AddFixStatistic(type);

            var name = $"'{game.Name[..Math.Min(game.Name.Length, 23)].Trim()}'";
            Logger.Warn($"Fixed {type,-26} index={game.Index:0000} name={name,-25} {details}");
        }

        private static void AddFixStatistic(string key)
        {
            _feedFixStatistics.TryAdd(key, 0);
            _feedFixStatistics[key]++;
        }

        // refer https://github.com/Fraesh/vps-db, https://virtual-pinball-spreadsheet.web.app/
        private const string VisualPinballSpreadsheetDatabaseUrl = "https://raw.githubusercontent.com/Fraesh/vps-db/master/vpsdb.json";

        private const string GameNameWhitespace = "Game Name Whitespace";
        private const string GameMissingImage = "Game Missing Image Url";
        private const string GameManufacturerWhitespace = "Game Manufacturer Whitespace";
        private const string GameCreatedTime = "Game Created Time";
        private const string GameUpdatedTimeTooLow = "Game Updated Time Too Low";
        private const string GameUpdatedTimeTooHigh = "Game Updated Time Too High";
        private const string FileUpdateTimeOrdering = "File Update Time Ordering";
        private const string FileUpdatedTime = "File Updated Time";
        private const string InvalidUrl = "Invalid Url";

        private static readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions _jsonSerializerOptions;

        private static readonly Dictionary<string, int> _feedFixStatistics = new Dictionary<string, int>();
    }
}