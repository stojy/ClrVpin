using System;
using System.Threading.Tasks;
using ClrVpin.Logging;
using Google;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Utils;

namespace ClrVpin.Importer
{
    internal static class ImporterUtils
    {
        public static async Task Get()
        {
            try
            {
                var sheetsService = new SheetsService(new BaseClientService.Initializer
                {
                    ApiKey = Cipher.Decrypt(Model.Settings.EncryptedSpreadsheetKey, "+KZeasX7C+X^!!Lk>x*Y=aJ+G*yjP@Xgy9vu+'7>/A:4C7z?PQ3e$=aAXyVr\aq/"),
                    ApplicationName = "ClrVpin" // actual name doesn't need to match google app or key name
                });
                sheetsService.HttpClient.DefaultRequestHeaders.Referrer = new Uri(_referrer);

                var request = sheetsService.Spreadsheets.Values.Get(_visualPinballSpreadsheet36, "A:Z");

                var response = await request.ExecuteAsync();
            }
            catch (GoogleApiException e)
            {
                Logger.Error(e, "Failed to retrieve online database");
                throw;
            }
        }

        private static readonly string _visualPinballSpreadsheet36 = "18edFWq2--Yw8iRX_ou3cGeQ3pAJ6zM0kWHYFWZW1fWg"; // https://docs.google.com/spreadsheets/d/1k8_maP610F5BZFrlJq-9il0CGRi7R3nJnYf8T7qMbe8/
        private static readonly string _referrer = "https://stoj.net";
    }
}