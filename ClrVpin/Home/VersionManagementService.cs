using System;
using System.Linq;
using System.Threading.Tasks;
using ClrVpin.Controls;
using ClrVpin.Logging;
using Utils;
using Notification = ClrVpin.Shared.Notification;

namespace ClrVpin.Home;

public static class VersionManagementService
{
    public static bool ShouldCheck()
    {
        var settings = Model.SettingsManager.Settings;
        var shouldCheckForUpdate = settings.EnableCheckForUpdatesAutomatically &&
                                   (settings.LastCheckForNewVersion == null || DateTime.Now - settings.LastCheckForNewVersion.Value > TimeSpan.FromDays(1));

        Logger.Info($"Version checking: shouldCheckForUpdate={shouldCheckForUpdate}, EnableCheckForUpdatesAutomatically={settings.EnableCheckForUpdatesAutomatically}, LastCheckForNewVersion={settings.LastCheckForNewVersion}");

        return shouldCheckForUpdate;
    }

    public static async Task CheckAndHandle(MaterialWindowEx parent = null, bool showIfNoUpdateExists = false)
    {
        const string dialogHost = "HomeDialog";

        try
        {
            var settings = Model.SettingsManager.Settings;
            var releases = await VersionManagement.Check(settings.Guid, "stojy", "ClrVpin", settings.EnableCheckForUpdatesPreRelease, msg => Logger.Info($"Version checking: {msg}"));

            if (releases.Any())
                await VersionManagementView.Show(releases, parent);
            else if (showIfNoUpdateExists)
                await Notification.ShowSuccess(dialogHost, "No Updates Available");

            // update last check AFTER processing to ensure the msi installer (if invoked) doesn't update the version (since it causes the process to exit before it reaches here)
            // - intention is that the new version when it starts up will perform another version check to ensure everything is up to date (which it should be!)
            settings.LastCheckForNewVersion = DateTime.Now;
            
            Model.SettingsManager.Write();
        }
        catch (Exception e)
        {
            await Notification.ShowError(dialogHost, "Update Check Failed", "", e.Message, true, true);
        }
    }
}