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

        public static async Task<Release> Check(string author, string repository, Action<string> logAction)
        {
            var client = new GitHubClient(new ProductHeaderValue(repository));
            var release = await client.Repository.Release.GetLatest(author, repository);
            var existingVersion = GetProductVersion();

            logAction($"existingVersion={existingVersion}, newVersion={release.TagName}");

            // no need to compare which is the actual 'latest' since this is already taken care of by the underlying git API
            // - i.e. avoid any mucking about with SemVer comparison logic
            return existingVersion != release.TagName ? release : null;
        }

        public static async Task Process(Release release, VersionManagementAction? action)
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
            System.Diagnostics.Process.Start(new ProcessStartInfo(fileName) { UseShellExecute = true });
            Environment.Exit(0);
        }

        private static void View(Release release)
        {
            System.Diagnostics.Process.Start(new ProcessStartInfo(release.HtmlUrl) { UseShellExecute = true });
        }

        private static string _productVersion;
    }
}