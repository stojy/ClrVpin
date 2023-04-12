using System.Text;
using OpenMcdf;

namespace ClrVpin.Shared.Utils;

public static class TableUtils
{
    public static string GetRomOrPup(string tableFile)
    {
        // the vpx files are encoded as CFBF (compound file binary format), which is a COM structured storage for housing files/folders within a single file
        // - openmcdf supports these files.. presumably because CFBF is the same as as MCDF (Microsoft Compound Document Files) ??
        // - refer https://en.wikipedia.org/wiki/Compound_File_Binary_Format
        //         https://github.com/ironfede/openmcdf

        var script = GetScript(tableFile);

        var rom = ParseScript(script);

        return rom;
    }

    private static string GetScript(string tableFile)
    {
        string script = null;
        
        using var cf = new CompoundFile(tableFile);
        if (cf.RootStorage.TryGetStorage("GameStg", out var gameStorage))
        {
            var stream = gameStorage.GetStream("GameData");
            var data = stream.GetData();
            script = Encoding.UTF8.GetString(data);
            //var scriptLines = script.Split(new[] { "\r\n", "\n\r", "\r", "\n" }, StringSplitOptions.None);
        }

        return script;
    }

    private static string ParseScript(string script)
    {
        return null;
    }
}