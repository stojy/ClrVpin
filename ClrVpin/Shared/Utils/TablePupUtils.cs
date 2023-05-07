using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Utils.Extensions;

namespace ClrVpin.Shared.Utils;

public class TablePupUtils
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
            return GetPup(tableFile.Path);
        }).ToList();
    }

    private static (string file, bool? isSuccess, string name) GetPup(string path, bool skipLogging = false)
    {
        return TableUtils.GetName(path, "PUP", _ => false, GetPupName, skipLogging);
    }

    private static string GetPupName(string script)
    {
        // todo; implement for PUP

        // find gameName usage
        var match = _gameNameUsageRegex.Match(script);
        if (!match.Success)
            return null;
        var gameName = match.Groups["gameName"].Value;

        // if gameName is enclosed in quotes, then the content IS the romName
        // - e.g. gameName="adam"
        if (gameName.StartsWith('"') && gameName.EndsWith("\""))
            return gameName.TrimStart('\"').TrimEnd('\"');

        // gameName is a variable name that references the romName
        // - find the variable assignment based on the variable name..
        //   1. known/common variable name - use the compiled RegEx to improve lookup performance
        //      - e.g. cGameName=cGameName... cGameName="adam"
        //   2. unknown/uncommon variable name - create RegEx on demand
        //      - e.g. cGameName=cGameName... cGameName="adam"
        // - loop through the matches looking for the first match that is NOT commented out.. done via code because RegEx was too slow/complicated :(
        match = gameName.ToLower().In(_knownGameNameVariables) ? _gameNameKnownVariablesRegex.Match(script) : Regex.Match(script, GetGameNameVariablesPattern(gameName), RegexOptions.Multiline);
        var (isCommented, romName) = TableRomUtils.GetUncommentedRomName(match);
        while (isCommented)
        {
            match = match.NextMatch();
            (isCommented, romName) = TableRomUtils.GetUncommentedRomName(match);
        }
        return romName;
    }

    // find GameName usage
    // - https://regex101.com/r/pmseXc/5
    // - applying a 0 to 1000 char maximum limit between Controller and GameName..
    //   - RegEx performance optimisation
    //   - workaround where the word 'Controller' and 'GameName' both appear in a non-vPinMame context, e.g. Mephisto (Cirsa 1987).vpx
    //private static readonly Regex _gameNameUsageRegex = new(@"Controller(?:.|\n){0,100}?\.\s*GameName\s*?\=\s*(?<gameName>.*?)\s", RegexOptions.Compiled | RegexOptions.Multiline);
    //private static readonly Regex _gameNameUsageRegex = new(@"With Controller(.|\n)*?\.\s*GameName\s*?\=\s*(?<gameName>.*?)\s", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex _gameNameUsageRegex = new(@"Controller(.|\n){0,1000}\.\s*GameName\s*?\=\s*(?<gameName>.*?)[\s\:]", RegexOptions.Compiled | RegexOptions.Multiline);
    
    // find GameName variable assignment
    // - https://regex101.com/r/VDUvva/6
    private static string GetGameNameVariablesPattern(params string[] gameNames) => @$"^(?<preamble>.*?)(?i:{gameNames.StringJoin("|")})\s*?\=\s*\""(?<romName>\w*?)\""";
    private static readonly string[] _knownGameNameVariables = { "cgamename", "gamename" };
    private static readonly Regex _gameNameKnownVariablesRegex = new(GetGameNameVariablesPattern(_knownGameNameVariables), RegexOptions.Compiled | RegexOptions.Multiline);
}