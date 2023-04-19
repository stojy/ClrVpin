using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ClrVpin.Logging;
using OpenMcdf;
using Utils.Extensions;

namespace ClrVpin.Shared.Utils;

public record TableFileDetail(string Type, string Path);

public static class TableUtils
{
    public static async Task<List<(bool? isSuccess, string name)>> GetRomsAsync(IEnumerable<TableFileDetail> tableFileDetails, Action<string, float> updateAction)
    {
        // run on a separate thread to avoid blocking the caller thread (e.g. UI) since this is a potentially slow operation
        return await Task.Run(() => GetRoms(tableFileDetails.ToList(), updateAction));
    }

    private static List<(bool? isSuccess, string name)> GetRoms(ICollection<TableFileDetail> tableFileDetails, Action<string, float> updateAction)
    {
        var totalFiles = tableFileDetails.Count;

        return tableFileDetails.Select((tableFile, i) =>
        {
            updateAction(Path.GetFileName(tableFile.Path), (i + 1) / (float)totalFiles);
            return GetRom(tableFile.Type, tableFile.Path);
        }).ToList();
    }

    private static (bool? isSuccess, string name) GetRom(string type, string path)
    {
        var fileName = Path.GetFileName(path);

        // skip checking ROM if the table type doesn't support a ROM
        if (type.In("PM", "EM") || _solidStateTableImplementationWithoutRom.Contains(fileName))
            return (null, null);

        var script = GetScript(path);
        var romName = GetRomName(script);

        var message = $"Detected ROM: {romName ?? "UNKNOWN",-10} tableFile={fileName}";
        if (romName == null)
            Logger.Warn(message);
        else
            Logger.Info(message);

        return (romName != null, romName);
    }

    // solid state tables that are known to be implemented without a ROM
    private static readonly HashSet<string> _solidStateTableImplementationWithoutRom = new(new []
    {
        "4X4 (Atari 1983).vpx"
    });
        
        



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