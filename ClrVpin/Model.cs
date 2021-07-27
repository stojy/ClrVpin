using System.Diagnostics;
using System.Windows;
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
            SettingsCommand = new ActionCommand(() => new Settings.SettingsViewModel().Show(mainWindow));
            AboutCommand = new ActionCommand(() => new About.AboutViewModel().Show(mainWindow));
            DonateCommand = new ActionCommand(() => new Donate.DonateViewModel().Show(mainWindow));
        }

        public ActionCommand ScannerCommand { get; set; }
        public ActionCommand RebuilderCommand { get; set; }
        public ActionCommand SettingsCommand { get; set; }
        public ActionCommand AboutCommand { get; set; }
        public ActionCommand DonateCommand { get; set; }

        public static SettingsManager SettingsManager { get; set; }
        public static Models.Settings.Settings Settings { get; set; }

        public static Rect ScreenWorkArea { get; set; }
    }
}