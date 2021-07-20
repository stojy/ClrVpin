using System.Windows;
using ClrVpin.Models;
using Utils;

namespace ClrVpin
{
    public class Model
    {
        public Model(Window mainWindow)
        {
            Config = new Config();

            ScannerCommand = new ActionCommand(() => new Scanner.ScannerViewModel().Show(mainWindow));
            RebuilderCommand = new ActionCommand(() => new Rebuilder.RebuilderViewModel().Show(mainWindow));
            SettingsCommand = new ActionCommand(() => new Settings.SettingsViewModel().Show(mainWindow));
            AboutCommand = new ActionCommand(() => new About.AboutViewModel().Show(mainWindow));
        }

        public ActionCommand ScannerCommand { get; set; }
        public ActionCommand RebuilderCommand { get; set; }
        public ActionCommand SettingsCommand { get; set; }
        public ActionCommand AboutCommand { get; set; }

        public static Config Config { get; set; }
        public static Rect ScreenWorkArea { get; set; }
    }
}