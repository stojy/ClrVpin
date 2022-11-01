using System.Windows;
using ClrVpin.Models.Settings;
using PropertyChanged;

namespace ClrVpin;

[AddINotifyPropertyChangedInterface]
public static class Model
{
    public static SettingsManager SettingsManager { get; set; }
    public static Models.Settings.Settings Settings { get; set; }

    public static Rect ScreenWorkArea { get; set; }

    public const string OptionsDisabledMessage = "... DISABLED BECAUSE THE FOLDER SETTINGS (USED BY PBY/PBX) ARE INCOMPLETE";
}