using System.IO;
using System.Linq;
using System.Text;
using OpenMcdf;
using Utils.Extensions;

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

    private static string GetScript(string vpxFile)
    {
        // if there is an external vbs file present, then this takes precedence over the script within the vpx file
        var vbsFile = Path.Combine(Path.GetDirectoryName(vpxFile) ?? "", $"{Path.GetFileNameWithoutExtension(vpxFile)}.vbs");

        string script = null;

        if (File.Exists(vbsFile))
        {
            // extract script from vbs
            script = File.ReadAllText(vbsFile);
        }
        else if (File.Exists(vpxFile))
        {
            // extract script from vpx
            using var cf = new CompoundFile(vpxFile);
            if (cf.RootStorage.TryGetStorage("GameStg", out var gameStorage))
            {
                var stream = gameStorage.GetStream("GameData");
                var data = stream.GetData();

                var i = data.IndexOf(Encoding.ASCII.GetBytes("CODE"));

                script = Encoding.ASCII.GetString(data.Skip(i + 8).ToArray()); // 8 length = sizeof(CODE) + 4 length(?) bytes
            }
        }

        return script;
    }

    private static string ParseScript(string script)
    {
        return null;
    }
}