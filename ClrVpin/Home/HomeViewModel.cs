using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ClrVpin.About;
using ClrVpin.Cleaner;
using ClrVpin.Controls;
using ClrVpin.Explorer;
using ClrVpin.Extensions;
using ClrVpin.Feeder;
using ClrVpin.Merger;
using ClrVpin.Models.Settings;
using ClrVpin.Settings;
using ClrVpin.Shared;
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

        FeederCommand = new ActionCommand(Show<FeederViewModel>);
        MergerCommand = new ActionCommand(Show<MergerViewModel>);
        CleanerCommand = new ActionCommand(Show<CleanerViewModel>);
        ExplorerCommand = new ActionCommand(Show<ExplorerViewModel>);
        SettingsCommand = new ActionCommand(Show<SettingsViewModel>);
        AboutCommand = new ActionCommand(Show<AboutViewModel>);
        CloseCommand = new ActionCommand(_mainWindow.Close);

        UpdateProperties();

        _mainWindow.SizeChanged += SizeChanged;
    }

    private void UpdateProperties()
    {
        CleanerToolTip = "Clean your existing collection" + (Model.SettingsManager.IsValid ? "" : Model.OptionsDisabledMessage);
        MergerToolTip = "Merge downloaded files into your existing collection" + (Model.SettingsManager.IsValid ? "" : Model.OptionsDisabledMessage);
        ExplorerToolTip = "Explore your existing collection" + (Model.SettingsManager.IsValid ? "" : Model.OptionsDisabledMessage);
    }

    public static SettingsManager SettingsManager { get; private set; }

    public string CleanerToolTip { get; private set; }
    public string MergerToolTip { get; private set; }
    public string ExplorerToolTip { get; private set; }

    public ICommand FeederCommand { get; }
    public ICommand MergerCommand { get; }
    public ICommand CleanerCommand { get; }
    public ICommand ExplorerCommand { get; }
    public ICommand SettingsCommand { get; }
    public ICommand AboutCommand { get; }
    public ICommand CloseCommand { get; }

    public bool? IsChildWindowActive { get; set; }

    private void SizeChanged(object sender, SizeChangedEventArgs e)
    {
        _mainWindow.CentreInCurrentScreen(e.NewSize);
    }

    private void Show<T>() where T : IShowViewModel, new()
    {
        var originalPositionLeft = _mainWindow.Left;
        var originalPositionTop = _mainWindow.Top;

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

        // delay the child window active notification to avoid unnecessary screen flickering whilst the child window becomes 'truly active'
        childWindow.ContentRendered += (_, _) => Task.Delay(200).ContinueWith(_ => Application.Current.Dispatcher.BeginInvoke(() =>
        {
            // only assign child window active if it hasn't already been assigned, e.g. ensure child window hasn't already closed if it had issues starting correctly
            IsChildWindowActive ??= true;
        }));

        childWindow.Closed += (_, _) => HandleChildWindowClosed(originalPositionLeft, originalPositionTop);
    }

    private void HandleChildWindowClosed(double originalPositionLeft, double originalPositionTop)
    {
        IsChildWindowActive = false;
        UpdateProperties();

        // restore the original main window location
        _mainWindow.Left = originalPositionLeft;
        _mainWindow.Top = originalPositionTop;

        // hide then show to ensure the window is brought to the foreground
        // - it's a workaround required to reliable handle scenario where non-ClrVpin windows were active, e.g. browser
        _mainWindow.Hide();
        _mainWindow.TryShow();
    }

    private readonly MaterialWindowEx _mainWindow;
}