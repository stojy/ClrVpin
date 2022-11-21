using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using ClrVpin.Controls;
using ClrVpin.Extensions;
using ClrVpin.Logging;
using ClrVpin.Models.Shared.Game;
using ClrVpin.Shared;
using PropertyChanged;

namespace ClrVpin.Explorer;

[AddINotifyPropertyChangedInterface]
public class ExplorerViewModel : IShowViewModel
{
    public Models.Settings.Settings Settings { get; } = Model.Settings;

    public Window Show(Window parent)
    {
        _window = new MaterialWindowEx
        {
            Owner = parent,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            SizeToContent = SizeToContent.WidthAndHeight,
            Content = this,
            Resources = parent.Resources,
            ContentTemplate = parent.FindResource("ExplorerTemplate") as DataTemplate,
            ResizeMode = ResizeMode.NoResize,
            Title = "Explorer"
        };

        _window.Show();

        Start();

        _window.Closed += (_, _) => Model.SettingsManager.Write();

        return _window;
    }

    private async void Start()
    {
        Logger.Info($"\nExplorer started, settings={JsonSerializer.Serialize(Settings)}");

        _window.Hide();
        Logger.Clear();

        var progress = new ProgressViewModel();
        progress.Show(_window);

        List<LocalGame> games;
        try
        {
            progress.Update("Loading Database");
            games = await TableUtils.ReadGamesFromDatabases(Settings.GetSelectedCheckContentTypes());
            Logger.Info($"Loading database complete, duration={progress.Duration}", true);
        }
        catch (Exception)
        {
            progress.Close();
            _window.TryShow();
            return;
        }

        progress.Update("Checking Files");
        var unmatchedFiles = await ExplorerUtils.CheckAsync(games, UpdateProgress);

        progress.Update("Fixing Files");
        var fixedFiles = await ExplorerUtils.FixAsync(games, Settings.BackupFolder, UpdateProgress);

        progress.Update("Removing Unmatched Files");
        await ExplorerUtils.RemoveUnmatchedAsync(unmatchedFiles, UpdateProgress);

        // delete empty backup folders - i.e. if there are no files (empty sub-directories are allowed)
        FileUtils.DeleteActiveBackupFolderIfEmpty();

        progress.Update("Preparing Results");
        await Task.Delay(1);
        _games = new ObservableCollection<LocalGame>(games);

        progress.Close();

        await ShowResults(fixedFiles, unmatchedFiles, progress.Duration);

        void UpdateProgress(string detail, float ratioComplete) => progress.Update(null, ratioComplete, detail);
    }

    private async Task ShowResults(ICollection<FileDetail> fixedFiles, ICollection<FileDetail> unmatchedFiles, TimeSpan duration)
    {
        var screenPosition = _window.GetCurrentScreenPosition();

        var width = Model.ScreenWorkArea.Width - WindowMargin;
        var results = new ExplorerResultsViewModel(_games);
        var displayTask = results.Show(_window, screenPosition.X, screenPosition.Y + WindowMargin, width);

        var statistics = new ExplorerStatisticsViewModel(_games, duration, fixedFiles, unmatchedFiles);
        statistics.Show(_window, screenPosition.X + WindowMargin, results.Window.Top + results.Window.Height + WindowMargin);

        var logging = new LoggingViewModel();
        logging.Show(_window, statistics.Window.Left + statistics.Window.Width + WindowMargin, results.Window.Top + results.Window.Height + WindowMargin, 
            Model.ScreenWorkArea.Width - statistics.Window.Width - WindowMargin - WindowMargin);

        statistics.Window.Closed += CloseWindows();
        results.Window.Closed += CloseWindows();
        logging.Window.Closed += CloseWindows();

        EventHandler CloseWindows()
        {
            return (_, _) =>
            {
                statistics.Window.Close();
                results.Close();
                logging.Close();

                _window.Close();
            };
        }

        await displayTask;
    }

    private ObservableCollection<LocalGame> _games;
    private MaterialWindowEx _window;
    private const int WindowMargin = 0;
}