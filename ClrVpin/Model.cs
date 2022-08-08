using System.Windows;
using System.Windows.Input;
using ClrVpin.About;
using ClrVpin.Importer;
using ClrVpin.Models.Settings;
using ClrVpin.Rebuilder;
using ClrVpin.Scanner;
using ClrVpin.Settings;
using Utils;

namespace ClrVpin
{
    public class Model
    {
        public Model(Window mainWindow)
        {
            // static instances of SettingsManager and Settings for convenience/simplicity
            SettingsManager = SettingsManager.Create();
            Settings = SettingsManager.Settings;

            ScannerCommand = new ActionCommand(() => new ScannerViewModel().Show(mainWindow));
            RebuilderCommand = new ActionCommand(() => new RebuilderViewModel().Show(mainWindow));
            ImporterCommand = new ActionCommand(() => new ImporterViewModel().Show(mainWindow));

            SettingsCommand = new ActionCommand(() => new SettingsViewModel().Show(mainWindow));
            AboutCommand = new ActionCommand(() => new AboutViewModel().Show(mainWindow));

            ScannerToolTip = "Scan existing content and optionally fix" + (SettingsManager.IsValid ? "" : OptionsDisabledMessage);
            RebuilderToolTip= "Rebuild existing library by merging new content from alternate folders" + (SettingsManager.IsValid ? "" : OptionsDisabledMessage);
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

        public const string OptionsDisabledMessage = "... DISABLED BECAUSE THE FOLDER SETTINGS (USED BY PBY/PBX) ARE INCOMPLETE";
    }
}