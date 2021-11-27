using System;
using System.IO;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace ClrVpin.Importer
{
    internal class Importer
    {
        public void Get()
        {
            GoogleCredential credential;
            string spreadSheetId = "18edFWq2--Yw8iRX_ou3cGeQ3pAJ6zM0kWHYFWZW1fWg"; //https://docs.google.com/spreadsheets/d/1k8_maP610F5BZFrlJq-9il0CGRi7R3nJnYf8T7qMbe8/
            using (var stream = new FileStream(@"c:\code\hopeful-theorem-258912-740bd1225dad.json", FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream).CreateScoped(SheetsService.Scope.SpreadsheetsReadonly);

                var sheetsService = new SheetsService(new BaseClientService.Initializer()
                {
                    //HttpClientInitializer = credential,
                    ApiKey = "AIzaSyDtjpBqrYqhWhgpgmrGjBbXWio94AnNGCA",
                    ApplicationName = "You application name",
                });
                sheetsService.HttpClient.DefaultRequestHeaders.Referrer = new Uri("https://stoj.net");

                var range = "A:Z";

                var request = sheetsService.Spreadsheets.Values.Get(spreadSheetId, range);

                var response = request.Execute();
            }
        }
        
        public void Get2()
        {
            GoogleCredential credential;
            string spreadSheetId = "18edFWq2--Yw8iRX_ou3cGeQ3pAJ6zM0kWHYFWZW1fWg"; //https://docs.google.com/spreadsheets/d/1k8_maP610F5BZFrlJq-9il0CGRi7R3nJnYf8T7qMbe8/
            using (var stream = new FileStream(@"c:\code\hopeful-theorem-258912-740bd1225dad.json", FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream).CreateScoped(SheetsService.Scope.SpreadsheetsReadonly);

                var sheetsService = new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "You application name",
                });

                var range = "A:";

                var request = sheetsService.Spreadsheets.Values.Get(spreadSheetId, range);

                ValueRange response = request.Execute();
            }

        }
    }
}
