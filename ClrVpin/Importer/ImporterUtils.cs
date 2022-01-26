using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using ClrVpin.Importer.Vps;
using ClrVpin.Logging;
using Utils;

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
                Converters = { new UnixToNullableDateTimeConverter {IsFormatInSeconds = false} }
            };
        }

        public static async Task<Game[]> GetOnlineDatabase()
        {
            var games = await _httpClient.GetFromJsonAsync<Game[]>(VisualPinballSpreadsheetDatabaseUrl, _jsonSerializerOptions);

            Update(games);

            return games;
        }

        private static void Update(Game[] games)
        {
            // various updates and/or fixes to the feed
            games.ForEach((game, index) =>
            {
                game.Index = index + 1;

                // group files into collections so they can be treated generically
                game.AllFiles = new Dictionary<string, File[]>
                {
                    // ReSharper disable once CoVariantArrayConversion
                    { nameof(game.TableFiles), game.TableFiles },
                    // ReSharper disable once CoVariantArrayConversion
                    { nameof(game.B2SFiles), game.B2SFiles },
                    { nameof(game.RuleFiles), game.RuleFiles },
                    { nameof(game.AltColorFiles), game.AltColorFiles },
                    { nameof(game.AltSoundFiles), game.AltSoundFiles },
                    { nameof(game.MediaPackFiles), game.MediaPackFiles },
                    { nameof(game.PovFiles), game.PovFiles },
                    { nameof(game.PupPackFiles), game.PupPackFiles },
                    { nameof(game.RomFiles), game.RomFiles },
                    { nameof(game.SoundFiles), game.SoundFiles },
                    { nameof(game.TopperFiles), game.TopperFiles },
                    { nameof(game.WheelArtFiles), game.WheelArtFiles }
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
        }

        private static void Fix(Game game)
        {
            // enforce sorted file ordering to ensure a game's most recent files are shown first
            // - arguably not a 'bug', but just a feature that the game files aren't ordered by default?
            game.AllFiles.ForEach(kv => game.AllFiles[kv.Key] = kv.Value.OrderByDescending(x => x.UpdatedAt).ToArray());

            // ensure top level image url is assigned if it's not been assigned
            game.ImgUrl ??= game.B2SFiles.FirstOrDefault(x => x.ImgUrl != null)?.ImgUrl ?? game.TableFiles.FirstOrDefault(x => x.ImgUrl != null)?.ImgUrl;

            // trim strings
            if (game.Name != game.Name.Trim())
            {
                Logger.Warn($"Fixed GAME WHITESPACE: index={game.Index:####} name='{game.Name}'");
                game.Name = game.Name.Trim();
            }
            
            if (game.Manufacturer != game.Manufacturer.Trim())
            {
                Logger.Warn($"Fixed MANUFACTURER WHITESPACE: index={game.Index:####} manufacturer='{game.Manufacturer}'");
                game.Manufacturer = game.Manufacturer.Trim();
            }

            game.AllFiles.ForEach(kv =>
            {
                // ensure updated timestamp isn't lower than the created timestamp
                kv.Value.Where(f => f.UpdatedAt < f.CreatedAt).ForEach(f =>
                {
                    LogFixedTimestamp(game, "FILE UPDATED", "updatedAt", f.UpdatedAt, "   createdAt", f.CreatedAt);
                    f.UpdatedAt = f.CreatedAt;
                });
            });

            // fix game created timestamp - must not be less than any file timestamps
            var maxCreatedAt = game.AllFilesList.Max(x => x.CreatedAt);
            if (game.LastCreatedAt < maxCreatedAt)
            {
                LogFixedTimestamp(game, "GAME CREATED", "createdAt", game.LastCreatedAt, nameof(maxCreatedAt), maxCreatedAt);
                game.LastCreatedAt = maxCreatedAt;
            }

            // fix game updated timestamp - must not be less than any file timestamps
            var maxUpdatedAt = game.AllFilesList.Max(x => x.UpdatedAt);
            if (game.UpdatedAt < maxUpdatedAt)
            {
                LogFixedTimestamp(game, "GAME UPDATED", "updatedAt", game.UpdatedAt, nameof(maxUpdatedAt), maxUpdatedAt);
                game.UpdatedAt = maxUpdatedAt;
            }
        }

        private static void LogFixedTimestamp(Game game, string type, string dateTimeName1, DateTime? dateTimeValue1, string dateTimeName2, DateTime? dateTimeValue2)
        {
            Logger.Warn($"Fixed {type} timestamp: {dateTimeName1}='{dateTimeValue1:dd/MM/yy HH:mm:ss}' < {dateTimeName2}='{dateTimeValue2:dd/MM/yy HH:mm:ss}' index={game.Index:####} game='{game.Name}'");
        }

        // refer https://github.com/Fraesh/vps-db, https://virtual-pinball-spreadsheet.web.app/
        private const string VisualPinballSpreadsheetDatabaseUrl = "https://raw.githubusercontent.com/Fraesh/vps-db/master/vpsdb.json";

        private static readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions _jsonSerializerOptions;
    }
}