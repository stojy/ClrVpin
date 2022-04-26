using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Octokit;
using FileMode = System.IO.FileMode;

namespace Utils
{
    public static class VersionManagement
    {
        public static string GetProductVersion()
        {
            // product version is a string, i.e. capable of storing as SemVer
            return _productVersion ??= FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly()?.Location!).ProductVersion;
        }

        public static async Task<Release> CheckForUpdate(string author, string repository)
        {
            var client = new GitHubClient(new ProductHeaderValue(repository));

            var release = await client.Repository.Release.GetLatest(author, repository);

            var existingVersion = GetProductVersion();

            // no need to compare which is 'latest' as this is already taken care of by the underlying git API
            return existingVersion != release.TagName ? release : null;
        }


        public static async Task ProcessAction(Release release, VersionManagementAction? action)
        {
            switch (action)
            {
                case VersionManagementAction.Install:
                    Install(await Download(release));
                    break;
                case VersionManagementAction.View:
                    View(release);
                    break;
                default:
                    return;
            }
        }

        private static async Task<string> Download(Release release)
        {
            // download as stream
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            var msiAsset = release.Assets.First(asset => asset.BrowserDownloadUrl.EndsWith(".msi"));
            await using var stream = await httpClient.GetStreamAsync(msiAsset.BrowserDownloadUrl);
            
            // store stream as file
            var fileName = @$"{SpecialFolder.Downloads}\{Path.GetFileNameWithoutExtension(msiAsset.Name)}-{release.TagName}{Path.GetExtension(msiAsset.Name)}";
            await using var fileStream = new FileStream(fileName, FileMode.Create);
            await stream.CopyToAsync(fileStream);

            return fileName;
        }

        private static void Install(string fileName)
        {
            Process.Start(new ProcessStartInfo(fileName) { UseShellExecute = true });
            Environment.Exit(0);
        }

        private static void View(Release release)
        {
            Process.Start(new ProcessStartInfo(release.HtmlUrl) { UseShellExecute = true });
        }

        private static string _productVersion;
    }
}