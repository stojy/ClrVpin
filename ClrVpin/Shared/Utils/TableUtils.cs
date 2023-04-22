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
    public static async Task<List<(string file, bool? isSuccess, string name)>> GetRomsAsync(IEnumerable<TableFileDetail> tableFileDetails, Action<string, float> updateAction)
    {
        // run on a separate thread to avoid blocking the caller thread (e.g. UI) since this is a potentially slow operation
        return await Task.Run(() => GetRoms(tableFileDetails.ToList(), updateAction));
    }

    private static List<(string file, bool? isSuccess, string name)> GetRoms(ICollection<TableFileDetail> tableFileDetails, Action<string, float> updateAction)
    {
        var totalFiles = tableFileDetails.Count;

        return tableFileDetails.Select((tableFile, i) =>
        {
            updateAction(Path.GetFileName(tableFile.Path), (i + 1) / (float)totalFiles);
            return GetRom(tableFile.Type, tableFile.Path);
        }).ToList();
    }

    private static (string file, bool? isSuccess, string name) GetRom(string type, string path)
    {
        var fileName = Path.GetFileName(path);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);

        // skip checking ROM if the table type doesn't support a ROM
        if (type.In("PM", "EM") || _solidStateTablesWithoutRomSupport.Contains(fileName))
            return (fileNameWithoutExtension, null, null);

        var script = GetScript(path);
        var romName = GetRomName(script);

        var message = $"Detected ROM: {romName ?? "FAILED",-12} tableFile={fileName}";
        if (romName == null)
            Logger.Warn(message);
        else
            Logger.Info(message);

        return (fileNameWithoutExtension, romName != null, romName);
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

        // gameName is a variable name that references the romName
        // - find the variable assignment based on the variable name..
        //   1. known/common variable name - use the compiled RegEx to improve lookup performance
        //      - e.g. cGameName=cGameName... cGameName="adam"
        //   2. unknown/uncommon variable name - create RegEx on demand
        //      - e.g. cGameName=cGameName... cGameName="adam"
        // - loop through the matches looking for the first match that is NOT commented out.. done via code because RegEx was too slow/complicated :(
        match = gameName.ToLower().In(_knownGameNameVariables) ? _gameNameKnownVariablesRegex.Match(script) : Regex.Match(script, GetGameNameVariablesPattern(gameName), RegexOptions.Multiline);
        var (isCommented, romName) = GetUncommentedRomName(match);
        while (isCommented)
        {
            match = match.NextMatch();
            (isCommented, romName) = GetUncommentedRomName(match);
        }
        return romName;
    }

    private static (bool isCommented, string romName) GetUncommentedRomName(Match match)
    {
        if (!match.Success)
            return (false, null);
        if (match.Groups["preamble"].Value.Contains("'"))
            return (true, null);
        
        return (false, match.Groups["romName"].Value);
    }

    // solid state tables that are known to be implemented without a ROM
    private static readonly HashSet<string> _solidStateTablesWithoutRomSupport = new(new[]
    {
        "4X4 (Atari 1983).vpx",
        "Alaska (Interflip 1978).vpx", // both EM ad SS tables exist, but the SS isn't implemented
        "Alive (Brunswick 1978).vpx",
        "America's Most Haunted (Spooky Pinball 2014).vpx",
        "Aspen (Brunswick 1979).vpx",
        "Batman 66 (Original 2018).vpx",
        "Black Knight Sword of Rage (Stern 2019).vpx",
        "Captain Nemo (Quetzal Pinball 2015).vpx",
        "CARtoons RC (Original 2017).vpx",
        "Cavalier (Recel 1979).vpx",
        "Circus (Brunswick 1980).vpx",
        "Circus (Gottlieb 1980).vpx",
        "dof_test_table_VPX.vpx",
        "Dragon (Gottlieb 1978).vpx", // both EM and SS tables exist, but the SS isn't implemented
        "Football (Taito do Brasil 1979).vpx",
        "Game of Thrones (Limited Edition) (Stern 2015).vpx",
        "Guardians Of The Galaxy (Stern 2017).vpx",
        "Junkyard Cats (Original 2012).vpx",
        "Kiss (Limited Edition) (Stern 2015).vpx", // newer table doesn't have ROM support.. deliberately
        "Loch Ness Monster (Game Plan 1985).vpx", // no known rom exists.. https://www.vpforums.org/index.php?app=downloads&showfile=15577
        "Mephisto (Cirsa 1987).vpx",
        "Mr. Doom (Recel 1979).vpx",
        "Pinball (Stern 1977).vpx",
        "Primus (Stern 2018).vpx",
        "Rob Zombie's Spookshow International (Spooky Pinball 2016).vpx",
        "Roman Victory (Taito do Brasil 1977).vpx",
        "Saloon (Taito do Brasil 1978).vpx",
        "Star Wars (Original 1978).vpx",
        "Swashbuckler (Recel 1979).vpx",
        "The Pabst Can Crusher (Stern 2016).vpx",
        "Total Nuclear Annihilation (Spooky Pinball 2017).vpx",
        "Whoa Nellie! Big Juicy Melons (Stern 2015).vpx",
        "Willy Wonka Pro (Original 2020).vpx",
        "Wizard of Oz (Original 2018).vpx",
        "Zeke's Peak (Taito 1984).vpx"
    });

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