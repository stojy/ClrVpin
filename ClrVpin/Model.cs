using System.Windows;
using System.Windows.Input;
using ClrVpin.Models.Settings;
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

            ScannerCommand = new ActionCommand(() => new Scanner.ScannerViewModel().Show(mainWindow));
            RebuilderCommand = new ActionCommand(() => new Rebuilder.RebuilderViewModel().Show(mainWindow));
            ImporterCommand = new ActionCommand(() => new Importer.ImporterViewModel().Show(mainWindow));
            
            SettingsCommand = new ActionCommand(() => new Settings.SettingsViewModel().Show(mainWindow));
            AboutCommand = new ActionCommand(() => new About.AboutViewModel().Show(mainWindow));
        }

        public ICommand ScannerCommand { get; set; }
        public ICommand RebuilderCommand { get; set; }
        public ICommand ImporterCommand { get; set; }
        public ICommand SettingsCommand { get; set; }
        public ICommand AboutCommand { get; set; }

        public static SettingsManager SettingsManager { get; set; }
        public static Models.Settings.Settings Settings { get; set; }

        public static Rect ScreenWorkArea { get; set; }
    }
}