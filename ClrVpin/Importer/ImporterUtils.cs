using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using ClrVpin.Importer.Vps;
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
            games.ForEach(game =>
            {
                // group all files into a single collection so they can be treated generically
                // ReSharper disable once CoVariantArrayConversion
                game.AllFiles.Add(nameof(game.TableFiles), game.TableFiles);
                // ReSharper disable once CoVariantArrayConversion
                game.AllFiles.Add(nameof(game.B2SFiles), game.B2SFiles);
                game.AllFiles.Add(nameof(game.RuleFiles), game.RuleFiles);
                game.AllFiles.Add(nameof(game.AltColorFiles), game.AltColorFiles);
                game.AllFiles.Add(nameof(game.AltSoundFiles), game.AltSoundFiles);
                game.AllFiles.Add(nameof(game.MediaPackFiles), game.MediaPackFiles);
                game.AllFiles.Add(nameof(game.PovFiles), game.PovFiles);
                game.AllFiles.Add(nameof(game.PupPackFiles), game.PupPackFiles);
                game.AllFiles.Add(nameof(game.RomFiles), game.RomFiles);
                game.AllFiles.Add(nameof(game.SoundFiles), game.SoundFiles);
                game.AllFiles.Add(nameof(game.TopperFiles), game.TopperFiles);
                game.AllFiles.Add(nameof(game.WheelArtFiles), game.WheelArtFiles);

                game.ImageFiles = game.TableFiles.Concat(game.B2SFiles).ToList();

                // enforce sorted file ordering
                // - required for some tables, e.g. 300
                game.AllFiles.ForEach(kv => game.AllFiles[kv.Key] = kv.Value.OrderByDescending(x => x.UpdatedAt).ToArray());

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
            // ensure top level image url is assigned if it's not been assigned
            game.ImgUrl ??= game.B2SFiles.FirstOrDefault(x => x.ImgUrl != null)?.ImgUrl ?? game.TableFiles.FirstOrDefault(x => x.ImgUrl != null)?.ImgUrl;
        }

        // refer https://github.com/Fraesh/vps-db, https://virtual-pinball-spreadsheet.web.app/
        private const string VisualPinballSpreadsheetDatabaseUrl = "https://raw.githubusercontent.com/Fraesh/vps-db/master/vpsdb.json";

        private static readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions _jsonSerializerOptions;
    }
}