using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Octokit;

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
                    await Install(release);
                    break;
                case VersionManagementAction.View:
                    View(release);
                    break;
                default:
                    return;
            }
        }

        private static async Task Install(Release release) { }

        private static void View(Release release)
        {
            Process.Start(new ProcessStartInfo(release.HtmlUrl) { UseShellExecute = true });
        }

        private static string _productVersion;
    }
}