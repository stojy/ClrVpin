using ClrVpin.Logging;
using Microsoft.Win32;

namespace ClrVpin.Settings;

public static class SettingsUtils
{
    public static string GetVpxFolder()
    {
        // find VPX install path by it's COM/type-library registration
        // - e.g. Computer\HKEY_CURRENT_USER\SOFTWARE\Classes\TypeLib\{384DF69D-3592-4041-848D-9A2D5CD081A0}\1.0
        var (path, key) = SearchKey(Registry.CurrentUser, @"SOFTWARE\Classes\TypeLib", "1.0", DefaultFieldName, "Visual Pinball", "HELPDIR", DefaultFieldName);
        
        return Process("Visual Pinball X", path, key);
    }

    public static string GetTablesFolder()
    {
        // find VPX most recently used table folder
        // - i.e. Computer\HKEY_CURRENT_USER\SOFTWARE\Visual Pinball\VP10\RecentDir
        var rootKey = @"SOFTWARE\Visual Pinball\VP10\RecentDir";
        using var key = Registry.CurrentUser.OpenSubKey(rootKey);
        var valueKey = "LoadDir";
        var path = key?.GetValue(valueKey) as string;

        // if we can't find the MRU table, then revert to the VPX installation path
        if (path != null)
            return Process("Tables", path, @$"{Registry.CurrentUser.Name}\{rootKey}\{valueKey}");
        
        Logger.Warn("Using VPX installation path to calculate the table path");
        var vpxPath = GetVpxFolder();
        if (vpxPath != null)
            path = @$"{vpxPath}\tables";

        return Process("Tables", path, @$"{Registry.CurrentUser.Name}\{rootKey}\{valueKey}");
    }

    public static string GetPinballYFolder()
    {
        // find PinballY install path by going directly to the Pinscape registry setting
        // - i.e. Computer\HKEY_CURRENT_USER\SOFTWARE\Pinscape Labs\PinballY
        const string rootKey = @"SOFTWARE\Pinscape Labs\PinballY";
        const string valueKey = "InstallPath";
        
        using var key = Registry.CurrentUser.OpenSubKey(rootKey);
        var path = key?.GetValue(valueKey) as string;

        return Process("PinballY", path, @$"{Registry.CurrentUser.Name}\{rootKey}\{valueKey}");
    }

    public static string GetPinballXFolder()
    {
        // find PinballX install path by examining the uninstall registry key
        // - need to search the subkeys since PBX uses an installation GUID for it's parent key
        //   e.g. Computer\HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\{53F4530D-803D-482A-86DD-F82D8EC7D628}_is1
        // - locate key that contains DisplayIcon="pinballX.exe", then use the 'InstallLocation' key
        // - inspired by VPY:
        //   https://github.com/mjrgh/PinballY/blob/88c132e7775f33d353cc5fb3f0118091df2be7dd/Utilities/PBXUtil.cpp#L16
        //   https://github.com/mjrgh/PinballY/blob/88c132e7775f33d353cc5fb3f0118091df2be7dd/PinballY/GameList.cpp#L2576

        // search for '\PinballX.exe' with preceding slash to avoid mismatch with VPinball.exe
        var (path, key) = SearchKey(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall", null, "DisplayIcon", @"\PinballX.exe", null, "InstallLocation");
        if (path == null)
            (path, key) = SearchKey(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", null, "DisplayIcon", @"\PinballX.exe", null, "InstallLocation");

        return Process("PinballX", path, key);
    }

    private static string Process(string product, string path, string key)
    {
        var trimmedPath = path?.TrimEnd('\\');

        Logger.Info($"Detected installation: product='{product}', path={trimmedPath ?? "n/a"}, key={key ?? "n/a"}");

        return trimmedPath;
    }

    private static (string path, string key) SearchKey(RegistryKey rootKey, string parentKeyName, string subParentKeySuffix, string fieldNameToMatch, string fieldValueToMatch, string lowerKeyName, string lowerFieldName)
    {
        using var parentKey = rootKey.OpenSubKey(parentKeyName);
        if (parentKey != null)
        {
            var subParentKeyNames = parentKey.GetSubKeyNames();

            // iterate through every subkey looking for the first matching key/value
            foreach (var subParentKeyName in subParentKeyNames)
            {
                var subParentKeyNameWithSuffix = subParentKeySuffix == null ? subParentKeyName : @$"{subParentKeyName}\{subParentKeySuffix}";
                using var subParentKey = parentKey.OpenSubKey(subParentKeyNameWithSuffix);

                var value = subParentKey?.GetValue(fieldNameToMatch) as string;
                if (value?.Contains(fieldValueToMatch) == true)
                {
                    if (lowerKeyName != null)
                    {
                        using var lowerKey = subParentKey.OpenSubKey(lowerKeyName);
                        return (lowerKey?.GetValue(lowerFieldName) as string, @$"{subParentKey.Name}\{lowerKeyName}\{lowerFieldName}");
                    }

                    return (subParentKey.GetValue(lowerFieldName) as string, $@"{subParentKey.Name}\{lowerFieldName}");
                }
            }
        }

        return (null, null);
    }

    private const string DefaultFieldName = "";
}