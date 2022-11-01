using System.Windows;
using System.Windows.Input;
using ClrVpin.About;
using ClrVpin.Importer;
using ClrVpin.Models.Settings;
using ClrVpin.Rebuilder;
using ClrVpin.Scanner;
using ClrVpin.Settings;
using PropertyChanged;
using Utils;

namespace ClrVpin
{
    [AddINotifyPropertyChangedInterface]
    public class Model
    {
        public Model(Window mainWindow)
        {
            _mainWindow = mainWindow;
            // static instances of SettingsManager and Settings for convenience/simplicity
            SettingsManager = SettingsManager.Create();
            Settings = SettingsManager.Settings;

            ScannerCommand = new ActionCommand(Show<ScannerViewModel>);
            RebuilderCommand = new ActionCommand(Show<RebuilderViewModel>);
            ImporterCommand = new ActionCommand(Show<ImporterViewModel>);
            SettingsCommand = new ActionCommand(Show<SettingsViewModel>);
            AboutCommand = new ActionCommand(Show<AboutViewModel>);

            ScannerToolTip = "Scan existing content and optionally fix" + (SettingsManager.IsValid ? "" : OptionsDisabledMessage);
            RebuilderToolTip = "Rebuild existing library by merging new content from alternate folders" + (SettingsManager.IsValid ? "" : OptionsDisabledMessage);
        }

        public string ScannerToolTip { get; }
        public string RebuilderToolTip { get; }

        public ICommand ScannerCommand { get; }
        public ICommand RebuilderCommand { get; }
        public ICommand ImporterCommand { get; }
        public ICommand SettingsCommand { get; }
        public ICommand AboutCommand { get; }

        public static SettingsManager SettingsManager { get; private set; }
        public static Models.Settings.Settings Settings { get; set; }

        public static Rect ScreenWorkArea { get; set; }

        private void Show<T>() where T : IShowViewModel, new()
        {
            // the main window MUST remain visible as a workaround a known bug(?)/feature where the UI stops updating when windows performs a 'global window update'
            // - 'UI stops updating' has the input working (e.g. mouse clicks), but no visual updates are made (e.g. checkbox state not visually updated)
            // - suspected, but not proven, to be an issue with the top level overlay..
            //   a. MaterialDesignExtensions - requires main window to NOT be hidden/collapsed or minimized to the task bar
            //   b. MaterialDesign without MDE - main window can be collapsed.. but can't be hidden.  weird!!
            // - examples.. UAC prompt, git extensions push, chrome file download, windows lock screen, windows screensaver(?), etc.
            _mainWindow.IsEnabled = false;

            var viewModel = new T();
            var childWindow = viewModel.Show(_mainWindow);
            
            childWindow.Closed += (_, _) => _mainWindow.IsEnabled = true;
        }

        private readonly Window _mainWindow;

        public const string OptionsDisabledMessage = "... DISABLED BECAUSE THE FOLDER SETTINGS (USED BY PBY/PBX) ARE INCOMPLETE";
    }
}