using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Octokit;
using Semver;
using Utils.Extensions;
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

        public static DateTime GetBuildTime()
        {
            // product version is a string, i.e. capable of storing as SemVer
            return _buildTime ??= File.GetLastWriteTime(Assembly.GetEntryAssembly()?.Location!);
        }

        public static async Task<List<Release>> Check(string guid, string author, string repository, bool includePreRelease, Action<string> logAction)
        {
            // check if the current version is the latest
            var client = new GitHubClient(new ProductHeaderValue($"ClrVpin_{guid}"));
            var releases = (await client.Repository.Release.GetAll(author, repository)).ToList();   // releases ordered by descending release date/time
            var skippedReleases = new List<Release>();

            var installedVersion = GetProductVersion();

            // latest release excludes pre-release unless instructed otherwise
            var latestRelease = releases.FirstOrDefault(release => !release.Prerelease || includePreRelease);
            logAction($"existingVersion={installedVersion}, latestVersion={latestRelease?.TagName}");

            // the 'latest' release is assumed to be the last one retrieved by the git API
            // - i.e. no need for any SemVer comparison logic
            if (installedVersion != latestRelease?.TagName)
            {
                skippedReleases.Add(latestRelease);

                if (SemVersion.TryParse(installedVersion, SemVersionStyles.Any, out var installedSemVer))
                {
                    // determine what releases have been 'skipped', i.e. releases (excluding latest) that were available, but not installed
                    // - use SemVer comparison logic
                    // - bail if any version can't be cleanly SemVer parsed
                    var earlierSkippedReleases = releases.Skip(1).TakeWhile(historicalRelease =>
                    {
                        // if the version can't be SemVer parsed, e.g. 4 digit version, then don't proceed further
                        if (!SemVersion.TryParse(historicalRelease.TagName, SemVersionStyles.Any, out var historicalSemVer))
                            return false;
                        
                        // continue until the installed SemVer is greater than the historical SemVer
                        return installedSemVer.CompareSortOrderTo(historicalSemVer) == -1;
                    }).ToList();

                    // append release notes for the skipped releases, so the user knows what other features have been skipped
                    skippedReleases.AddRange(earlierSkippedReleases);

                    logAction($"skippedReleases={skippedReleases.Select(skippedRelease => skippedRelease.TagName).StringJoin()}");
                }
            }

            return skippedReleases;
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
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(5);
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
        private static DateTime? _buildTime;
    }
}