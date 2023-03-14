using System.IO;
using Microsoft.Win32;

namespace ClrVpin.Settings;

public static class SettingsUtils
{
    public static string GetPinballXFolder()
    {
        // find PinballX install path by examining the uninstall registry key
        // - need to search the subkeys since PBX uses an installation GUID
        //   e.g. Computer\HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\{53F4530D-803D-482A-86DD-F82D8EC7D628}_is1
        // - locate key that contains DisplayIcon="pinballX.exe", then use the 'InstallLocation' key
        // - inspired by VPY:
        //   https://github.com/mjrgh/PinballY/blob/88c132e7775f33d353cc5fb3f0118091df2be7dd/Utilities/PBXUtil.cpp#L16
        //   https://github.com/mjrgh/PinballY/blob/88c132e7775f33d353cc5fb3f0118091df2be7dd/PinballY/GameList.cpp#L2576

        var path = SearchKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall", "DisplayIcon", "PinballX.exe", "InstallLocation") ?? 
                   SearchKey(@"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall", "DisplayIcon", "PinballX.exe", "InstallLocation");

        return path?.TrimEnd('\\');
    }

    // search for key within a specified key contained 2 levels down
    private static string SearchKey(string rootKey, string keyToMatch, string valueToMatch, string keyToReturnValue)
    {
        using var key = Registry.LocalMachine.OpenSubKey(rootKey);
        if (key != null)
        {
            var subKeyNames = key.GetSubKeyNames() ;
            foreach (var subKeyName in subKeyNames)
            {
                using var subKey = key.OpenSubKey(subKeyName);
                var value = subKey?.GetValue(keyToMatch) as string;
                if (value?.EndsWith(valueToMatch) == true)
                    return subKey.GetValue(keyToReturnValue) as string;
            }
        }

        return null;
    }
}