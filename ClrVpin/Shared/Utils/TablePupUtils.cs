using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ClrVpin.Logging;
using Utils.Extensions;

namespace ClrVpin.Shared.Utils;

public static class TablePupUtils
{
    public static async Task<List<(string file, bool? isSuccess, string name)>> GetPupsAsync(IEnumerable<TableFileDetail> tableFileDetails, Action<string, float> updateAction)
    {
        // run on a separate thread to avoid blocking the caller thread (e.g. UI) since this is a potentially slow operation
        return await Task.Run(() => GetPups(tableFileDetails.ToList(), updateAction));
    }

    private static List<(string file, bool? isSuccess, string name)> GetPups(ICollection<TableFileDetail> tableFileDetails, Action<string, float> updateAction)
    {
        var totalFiles = tableFileDetails.Count;

        return tableFileDetails.Select((tableFile, i) =>
        {
            updateAction(Path.GetFileName(tableFile.Path), (i + 1) / (float)totalFiles);
            return GetPup(tableFile.Path, true);
        }).ToList();
    }

    private static (string file, bool? isSuccess, string name) GetPup(string path, bool skipLogging = false)
    {
        return TableUtils.GetName(path, "PuP", _ => false, script => GetPupName(script, path), skipLogging);
    }

    private static (string name, bool? isSuccess) GetPupName(string script, string path)
    {
        // find pup variable name assignment
        var match = _pupClassNameRegex.Match(script);
        if (!match.Success)
            return (null, null);
        var pupClassName = match.Groups["pupVariableName"].Value;

        // find pup game variable name
        match = pupClassName.In(_knownPupClassNames) ? _pupGameVariableNameRegex.Match(script) : Regex.Match(script, GetPupGameVariableNamesPattern(pupClassName), RegexOptions.Multiline);
        if (!match.Success)
        {
            Logger.Warn($"PuP: Failed to find game name table: '{Path.GetFileName(path)}'");
            return (null, false);
        }
        var pupGameVariableName = match.Groups["pupGameVariableName"].Value;

        // if pupGameVariableName is enclosed in quotes, then the content IS the pup folder
        // - e.g. PuPlayer.B2SInit "","gog_2020"
        if (pupGameVariableName.StartsWith('"') && pupGameVariableName.EndsWith("\""))
        {
            var name = pupGameVariableName.TrimStart('\"').TrimEnd('\"');
            return (name, true);
        }

        // pupGameVariableName is a variable name that references the pup folder
        // - find the variable assignment based on the variable name..
        //   1. known/common variable name - use the compiled RegEx to improve lookup performance
        //      - e.g. cPuPPack="adam"
        //   2. unknown/uncommon variable name - create RegEx on demand
        //      - e.g. PuPPack_folder="adam"
        // - loop through the matches looking for the first match that is NOT commented out.. done via code because RegEx was too slow/complicated :(
        match = pupGameVariableName.In(_knownPupGameVariableNames) ? _pupGameNameRegex.Match(script) : Regex.Match(script, GetPupGameNamePattern(pupGameVariableName), RegexOptions.Multiline);
        var (isCommented, pupName) = GetUncommentedPupName(match);
        while (isCommented)
        {
            match = match.NextMatch();
            (isCommented, pupName) = GetUncommentedPupName(match);
        }
        return (pupName, pupName != null);
    }

    private static (bool isCommented, string romName) GetUncommentedPupName(Match match)
    {
        if (!match.Success)
            return (false, null);
        if (match.Groups["preamble"].Value.Contains("'"))
            return (true, null);

        return (false, match.Groups["pupName"].Value);
    }

    // find pup class name
    // - https://regex101.com/r/P8EwEU/2
    // - e.g. Set PuPlayer = CreateObject("PinUpPlayer.PinDisplay")
    private static readonly Regex _pupClassNameRegex = new(@"Set\s*(?<pupClassName>\w*)\s*=\s*CreateObject\(\""PinUpPlayer.PinDisplay\""\)", RegexOptions.Compiled | RegexOptions.Multiline);

    // find pup game name variable name
    // - https://regex101.com/r/zrJElD/2
    // - e.g. PuPlayer.B2SInit "", cGameName
    private static string GetPupGameVariableNamesPattern(params string[] pupClassNames) => @$"({pupClassNames.StringJoin("|")}).B2SInit.*,\s*(?<pupGameVariableName>[\""\w]*)\s*";
    private static readonly string[] _knownPupClassNames = { "PuPlayer"};
    private static readonly Regex _pupGameVariableNameRegex = new(GetPupGameVariableNamesPattern(_knownPupClassNames), RegexOptions.Compiled | RegexOptions.Multiline);

    // find pup game name assignment
    // - https://regex101.com/r/x4aPlv/2
    // - e.g. Const cGameName = "b66_orig"
    //        cPuPPack = "smanve_101c"
    private static string GetPupGameNamePattern(params string[] pupVariableNames) => @$"^(?<preamble>.*)(?i:{pupVariableNames.StringJoin("|")})\s*?\=\s*\""(?<pupName>.*?)\""";
    private static readonly string[] _knownPupGameVariableNames = { "cPuPPack" };
    private static readonly Regex _pupGameNameRegex = new(GetPupGameNamePattern(_knownPupGameVariableNames), RegexOptions.Compiled | RegexOptions.Multiline);
}