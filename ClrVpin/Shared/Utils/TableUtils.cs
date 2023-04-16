using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ClrVpin.Logging;
using OpenMcdf;
using Utils.Extensions;

namespace ClrVpin.Shared.Utils;

public static class TableUtils
{
    public static string GetRomAndPup(string tableFile)
    {
        var script = GetScript(tableFile);
        var romName = GetRomName(script);

        var message = $"rom: {romName ?? "UNKNOWN",-8} tableFile={Path.GetFileName(tableFile)}";
        if (romName == null)
            Logger.Warn(message);
        else
            Logger.Info(message);

        return romName;
    }

    private static string GetScript(string vpxFile)
    {
        // the vpx files are encoded as CFBF (compound file binary format), which is a COM structured storage for housing files/folders within a single file
        // - openmcdf supports these files.. presumably because CFBF is the same as as MCDF (Microsoft Compound Document Files) ??
        // - refer https://en.wikipedia.org/wiki/Compound_File_Binary_Format
        //         https://github.com/ironfede/openmcdf

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

    private static string GetRomName(string script)
    {
        // find gameName usage
        var match = _gameNameUsageRegex.Match(script);
        if (!match.Success)
            return null;
        var gameName = match.Groups["gameName"].Value;

        // if gameName is enclosed in quotes, then the content IS the romName
        // - e.g. gameName="adam"
        if (gameName.StartsWith('"') && gameName.EndsWith("\""))
            return gameName.TrimStart('\"').TrimEnd('\"');

        // gameName is a variable name that references the romName, so find the variable assignment based on the variable name..
        // 1. known/common variable name - use the compiled RegEx to improve lookup performance
        //    - e.g. cGameName=cGameName... cGameName="adam"
        // 2. unknown/uncommon variable name - create RegEx on demand
        //    - e.g. cGameName=cGameName... cGameName="adam"
        match = gameName.ToLower().In(_knownGameNameVariables) ? _gameNameKnownVariablesRegex.Match(script) : Regex.Match(script, GetGameNameVariablesPattern(gameName));
        return !match.Success ? null : match.Groups["romName"].Value;
    }

    // find GameName variable assignment
    // - https://regex101.com/r/VDUvva/5
    private static string GetGameNameVariablesPattern(params string[] gameNames) => @$"(?i:{gameNames.StringJoin("|")})\s*?\=\s*\""(?<romName>\w*?)\""";

    // find GameName usage
    // - https://regex101.com/r/pmseXc/2
    private static readonly Regex _gameNameUsageRegex = new(@"Controller(?:.|\n)*?GameName\s*?\=\s*(?<gameName>.*?)\s", RegexOptions.Compiled);
    private static readonly string[] _knownGameNameVariables = { "cgamename", "gamename" };
    private static readonly Regex _gameNameKnownVariablesRegex = new(GetGameNameVariablesPattern(_knownGameNameVariables), RegexOptions.Compiled);
}