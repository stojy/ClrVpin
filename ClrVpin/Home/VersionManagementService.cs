using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ClrVpin.Logging;
using Utils;
using Notification = ClrVpin.Shared.Notification;

namespace ClrVpin.Home;

public static class VersionManagementService
{
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
        var releases = await VersionManagement.Check(Model.SettingsManager.Settings.Guid, "stojy", "ClrVpin", msg => Logger.Info($"Version checking: {msg}"));

        if (releases.Any())
            await VersionManagementView.Show(releases, parent);
        else if (showIfNoUpdateExists) 
            await Notification.ShowSuccess("HomeDialog", "No Updates Available");

        // update last check AFTER processing to ensure the msi installer (if invoked) doesn't update the version (since it causes the process to exit before it reaches here)
        // - intention is that the new version when it starts up will perform another version check to ensure everything is up to date (which it should be!)
        Model.SettingsManager.Settings.LastCheckForNewVersion = DateTime.Now;
        Model.SettingsManager.Write();
    }
}