using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;
using Octokit;
using PropertyChanged;
using Utils;

namespace ClrVpin.Home;

[AddINotifyPropertyChangedInterface]
public class VersionManagementView
{
    private VersionManagementView() { }

    public string ExistingVersion { get; set; }
    public string LatestVersion { get; set; }

    public Release SelectedRelease { get; private set; }

    public ICommand ViewLatestReleaseCommand { get; private set; }
    public ICommand SelectOlderReleaseCommand { get; private set; }
    public ICommand SelectNewerReleaseCommand { get; private set; }

    public bool IsNewerReleaseEnabled { get; set; }
    public bool IsOlderReleaseEnabled { get; set; }

    public static async Task Show(List<Release> releases, Window parent = null)
    {
        var instance = new VersionManagementView();
        await instance.ShowInternal(releases, parent);
    }

    private async Task ShowInternal(List<Release> releases, Window parent = null)
    {
        _releases = releases;

        // cleanse the MD to make it compatible with the MD renderer..
        // - strip '#' from git issue references that are part of bullet points
        // - convert "-" bullet points to "*" bullet points
        var releaseNotesRegex = new Regex(@"(- #|\* #)", RegexOptions.Compiled);
        releases.ForEach(release => releaseNotesRegex.Replace(release.Body, "* "));

        var latestRelease = releases.First();

        ExistingVersion = $"{VersionManagement.GetProductVersion()} ({VersionManagement.GetBuildTime()})";
        LatestVersion = $"{latestRelease.TagName} ({latestRelease.CreatedAt.LocalDateTime})";

        // display release notes
        ViewLatestReleaseCommand = new ActionCommand(() => ViewLatestVersion(latestRelease));
        SelectOlderReleaseCommand = new ActionCommand(() => SelectRelease(true));
        SelectNewerReleaseCommand = new ActionCommand(() => SelectRelease(false));

        SelectRelease();

        // hide parent window to avoid alt-tab complications where the dialog can be rendered twice.. which causes issues
        parent?.Hide();

        // render release notes
        var result = await DialogHost.Show(this, "HomeDialog") as VersionManagementAction?;

        // install or view release
        await VersionManagement.Process(latestRelease, result);

        parent?.Show();

        // update last check AFTER processing to ensure the msi installer (if invoked) doesn't update the version (since it causes the process to exit before it reaches here)
        // - intention is that the new version when it starts up will perform another version check to ensure everything is up to date (which it should be!)
        Model.SettingsManager.Settings.LastCheckForNewVersion = DateTime.Now;
        Model.SettingsManager.Write();
    }

    private static async void ViewLatestVersion(Release release) => await VersionManagement.Process(release, VersionManagementAction.View);

    private void SelectRelease(bool? isOlderSelected = null)
    {
        if (isOlderSelected == true)
            _selectedReleaseIndex++;
        else if (isOlderSelected == false)
            _selectedReleaseIndex--;

        SelectedRelease = _releases.ElementAt(_selectedReleaseIndex);
        IsOlderReleaseEnabled = _selectedReleaseIndex < _releases.Count - 1;
        IsNewerReleaseEnabled = _selectedReleaseIndex > 0;
    }

    private int _selectedReleaseIndex;
    private List<Release> _releases;
}