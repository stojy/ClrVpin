using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ClrVpin.About;
using ClrVpin.Controls;
using ClrVpin.Extensions;
using ClrVpin.Importer;
using ClrVpin.Models.Settings;
using ClrVpin.Rebuilder;
using ClrVpin.Scanner;
using ClrVpin.Settings;
using PropertyChanged;
using Utils;

namespace ClrVpin.Home;

[AddINotifyPropertyChangedInterface]
public class HomeViewModel
{
    public HomeViewModel(MaterialWindowEx mainWindow)
    {
        _mainWindow = mainWindow;

        // static instances of SettingsManager and Settings for convenience/simplicity
        SettingsManager = Model.SettingsManager = SettingsManager.Create();
        Model.Settings = Model.SettingsManager.Settings;

        ScannerCommand = new ActionCommand(Show<ScannerViewModel>);
        RebuilderCommand = new ActionCommand(Show<RebuilderViewModel>);
        ImporterCommand = new ActionCommand(Show<ImporterViewModel>);
        SettingsCommand = new ActionCommand(Show<SettingsViewModel>);
        AboutCommand = new ActionCommand(Show<AboutViewModel>);
        CloseCommand = new ActionCommand(_mainWindow.Close);

        ScannerToolTip = "Scan existing content and optionally fix" + (Model.SettingsManager.IsValid ? "" : Model.OptionsDisabledMessage);
        RebuilderToolTip = "Rebuild existing library by merging new content from alternate folders" + (Model.SettingsManager.IsValid ? "" : Model.OptionsDisabledMessage);

        _mainWindow.SizeChanged += SizeChanged;
    }

    public static SettingsManager SettingsManager { get; private set; }

    public string ScannerToolTip { get; }
    public string RebuilderToolTip { get; }

    public ICommand ScannerCommand { get; }
    public ICommand RebuilderCommand { get; }
    public ICommand ImporterCommand { get; }
    public ICommand SettingsCommand { get; }
    public ICommand AboutCommand { get; }
    public ICommand CloseCommand { get; }

    public bool IsChildWindowActive { get; set; }

    private void SizeChanged(object sender, SizeChangedEventArgs e)
    {
        _mainWindow.CentreInCurrentScreen(e.NewSize);
    }

    private void Show<T>() where T : IShowViewModel, new()
    {
        // the main window MUST remain visible as a workaround a 'feature' where the UI stops updating when windows performs a 'global window update'
        // - 'UI stops updating' has the input working (e.g. mouse clicks), but no visual updates are made (e.g. checkbox state not visually updated)
        // - the window will sometimes resume working (including any previous clicks) if it's dragged to the same window as the home window
        // - suspected, but not proven, to be an issue with the top level overlay..
        //   a. MaterialDesignExtensions - requires main window to NOT be hidden/collapsed or minimized to the task bar
        //   b. MaterialDesign without MDE - main window can be collapsed.. but can't be hidden.  weird!!
        //   c. root window (HomeView) - MUST be a MaterialDesignExtensions window, i.e. can't use a regular Window (as root at any rate) with MDE window :(
        // - examples.. UAC prompt, git extensions push, chrome file download, windows lock screen, windows screensaver(?), etc.

        var viewModel = new T();
        var childWindow = viewModel.Show(_mainWindow);

        var originalPositionLeft = _mainWindow.Left;
        var originalPositionTop = _mainWindow.Top;

        // delay the child window active notification to avoid unnecessary screen flickering whilst the child window becomes 'truly active'
        childWindow.ContentRendered += (_, _) => Task.Delay(200).ContinueWith(_ => Application.Current.Dispatcher.BeginInvoke(() =>
        {
            IsChildWindowActive = true;
        }));

        childWindow.Closed += (_, _) =>
        {
            IsChildWindowActive = false;

            // restore the original main window location
            _mainWindow.Left = originalPositionLeft;
            _mainWindow.Top = originalPositionTop;

            // hide then show to ensure the window is brought to the foreground
            // - it's a workaround required in case other non-ClrVpin windows were active, e.g. browser
            _mainWindow.Hide();
            _mainWindow.TryShow();
        };
    }

    private readonly MaterialWindowEx _mainWindow;
}