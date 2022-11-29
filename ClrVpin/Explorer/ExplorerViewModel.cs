using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using ClrVpin.Extensions;
using ClrVpin.Logging;
using ClrVpin.Models.Shared.Game;
using ClrVpin.Shared;
using PropertyChanged;

namespace ClrVpin.Explorer;

[AddINotifyPropertyChangedInterface]
public class ExplorerViewModel : IShowViewModel
{
    public Window Show(Window parent)
    {
        // create and show window to satisfy IShowViewModel.Show(), but make sure it's not visible since there are no user configurable settings
        _window = new Window
        {
            Owner = parent,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            SizeToContent = SizeToContent.WidthAndHeight,
            Content = this,
            Resources = parent.Resources,
            ContentTemplate = parent.FindResource("ExplorerTemplate") as DataTemplate,
            WindowStyle = WindowStyle.None,
            Width = 0,
            Height = 0,
            ShowInTaskbar = false,
            ShowActivated = false
        };

        _window.Show();

        // immediately start
        Start();

        _window.Closed += (_, _) => Model.SettingsManager.Write();

        return _window;
    }

    private async void Start()
    {
        Logger.Info($"\nExplorer started, settings={JsonSerializer.Serialize(_settings)}");
        _window.Hide();
        Logger.Clear();

        var progress = new ProgressViewModel();
        progress.Show(_window);

        List<LocalGame> games;
        try
        {
            progress.Update("Loading Database");
            games = await TableUtils.ReadGamesFromDatabases(_settings.GetAllContentTypes());
            Logger.Info($"Loading database complete, duration={progress.Duration}", true);
        }
        catch (Exception)
        {
            progress.Close();
            return;
        }

        progress.Update("Matching Files");
        var unmatchedFiles = await TableUtils.MatchContentToLocalAsync(games, UpdateProgress, _settings.GetAllContentTypes(), true);

        progress.Update("Preparing Results");
        await Task.Delay(1);

        progress.Close();

        await ShowResults(games, unmatchedFiles, progress.Duration);

        void UpdateProgress(string detail, float ratioComplete) => progress.Update(null, ratioComplete, detail);
    }

    private async Task ShowResults(List<LocalGame> localGames, ICollection<FileDetail> unmatchedFiles, TimeSpan duration)
    {
        var localGamesCollection = new ObservableCollection<LocalGame>(localGames);

        var screenPosition = _window.GetCurrentScreenPosition();

        var width = Model.ScreenWorkArea.Width - WindowMargin;
        var results = new ExplorerResultsViewModel(localGamesCollection);
        var displayTask = results.Show(_window, screenPosition.X, screenPosition.Y + WindowMargin, width);

        var statistics = new ExplorerStatisticsViewModel(localGamesCollection, duration, new List<FileDetail>(), unmatchedFiles);
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
                results.Window.Close();
                logging.Close();

                _window.Close();
            };
        }

        await displayTask;
    }

    private readonly Models.Settings.Settings _settings = Model.Settings;

    private Window _window;
    private const int WindowMargin = 0;
}