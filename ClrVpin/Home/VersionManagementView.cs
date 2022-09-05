﻿using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ClrVpin.Logging;
using MaterialDesignThemes.Wpf;
using Octokit;
using PropertyChanged;
using Utils;
using Notification = ClrVpin.Shared.Notification;

namespace ClrVpin.Home
{
    [AddINotifyPropertyChangedInterface]
    public class VersionManagementView
    {
        public string Title { get; private init; }
        public string ExistingVersion { get; init; }
        public string NewVersion { get; init; }
        public string ReleaseNotes { get; init; }
        public ICommand ViewNewVersionCommand { get; private init; }

        public static bool ShouldCheck()
        {
            var settings = Model.SettingsManager.Settings;
            var shouldCheckForUpdate = settings.EnableCheckForNewVersion &&
                                       (settings.LastCheckForNewVersion == null || DateTime.Now - settings.LastCheckForNewVersion.Value > TimeSpan.FromDays(1));

            Logger.Info($"Version checking: shouldCheckForUpdate={shouldCheckForUpdate}, EnableCheckForNewVersion={settings.EnableCheckForNewVersion}, LastCheckForNewVersion={settings.LastCheckForNewVersion}");

            return shouldCheckForUpdate;
        }

        public static async Task CheckAndHandle(Window parent = null, bool showIfNoUpdateExists = false)
        {
            var release = await VersionManagement.Check(Model.SettingsManager.Settings.Guid, "stojy", "ClrVpin", msg => Logger.Info($"Version checking: {msg}"));
            if (release != null)
            {
                // hide parent window to avoid alt-tab complications where the dialog can be rendered twice.. which causes issues
                parent?.Hide();

                // cleanse the MD to make it compatible with the MD renderer..
                // - strip '#' from git issue references that are part of bullet points
                // - convert "-" bullet points to "*" bullet points
                var regex = new Regex(@"(- #|\* #)", RegexOptions.Compiled);
                var releaseNotes = regex.Replace(release.Body, "* ");

                var result = await DialogHost.Show(new VersionManagementView
                {
                    Title = "A new version is available",
                    ExistingVersion = $"{VersionManagement.GetProductVersion()} ({VersionManagement.GetBuildTime()})",
                    NewVersion = $"{release.TagName} ({release.CreatedAt.LocalDateTime})",
                    ReleaseNotes = releaseNotes,
                    ViewNewVersionCommand = new ActionCommand(() => ViewNewVersion(release))
                }, "HomeDialog") as VersionManagementAction?;

                await VersionManagement.Process(release, result);

                parent?.Show();
            }
            else if (showIfNoUpdateExists)
            {
                await Notification.ShowSuccess("HomeDialog", "No Updates Available");
            }

            // update last check AFTER processing to ensure the msi installer (if invoked) doesn't update the version (since it causes the process to exit before it reaches here)
            // - intention is that the new version when it starts up will perform another version check to ensure everything is up to date (which it should be!)
            Model.SettingsManager.Settings.LastCheckForNewVersion = DateTime.Now;
            Model.SettingsManager.Write();
        }

        private static async void ViewNewVersion(Release release)
        {
            await VersionManagement.Process(release, VersionManagementAction.View);
        }
    }
}