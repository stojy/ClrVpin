using ClrVpin.Models;
using Utils;

namespace ClrVpin
{
    public class Model
    {
        public Model(MainWindow mainWindow)
        {
            Config = new Config();

            ScannerCommand = new ActionCommand(() => new Scanner.Scanner(mainWindow).Show());
            RebuilderCommand = new ActionCommand(() => new Rebuilder.Rebuilder(mainWindow).Show());
            SettingsCommand = new ActionCommand(() => new Settings.Settings(mainWindow).Show());
            AboutCommand = new ActionCommand(() => new About.About(mainWindow).Show());
        }

        public ActionCommand ScannerCommand { get; set; }
        public ActionCommand RebuilderCommand { get; set; }
        public ActionCommand SettingsCommand { get; set; }
        public ActionCommand AboutCommand { get; set; }

        public static Config Config { get; set; }
    }
}