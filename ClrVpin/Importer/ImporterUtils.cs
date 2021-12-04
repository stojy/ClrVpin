using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ClrVpin.Importer.Vps;

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
        }

        public static async Task GetOnlineDatabase()
        {
            var response = await _httpClient.GetFromJsonAsync<Game[]>(VisualPinballSpreadsheetDatabaseUrl);
        }

        private static readonly HttpClient _httpClient;

        // refer https://github.com/Fraesh/vps-db, https://virtual-pinball-spreadsheet.web.app/
        private const string VisualPinballSpreadsheetDatabaseUrl = "https://raw.githubusercontent.com/Fraesh/vps-db/master/vpsdb.json";
    }
}