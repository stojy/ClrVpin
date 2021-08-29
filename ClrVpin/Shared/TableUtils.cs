using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ClrVpin.Models;
using Utils;

namespace ClrVpin.Shared
{
    public static class TableUtils
    {
        public static List<Game> GetGamesFromDatabases(IList<ContentType> contentTypes)
        {
            var databaseContentType = Model.Settings.GetDatabaseContentType();

            // scan through all the databases in the folder
            var files = Directory.EnumerateFiles(databaseContentType.Folder, databaseContentType.Extensions);

            var games = new List<Game>();

            files.ForEach(file =>
            {
                var doc = XDocument.Load(file);
                if (doc.Root == null)
                    throw new Exception($"Failed to load database: '{file}'");

                var menu = doc.Root.Deserialize<Menu>();
                var number = 1;
                menu.Games.ForEach(game =>
                {
                    game.Number = number++;
                    game.Ipdb = game.IpdbId ?? game.IpdbNr;
                    game.IpdbUrl = string.IsNullOrEmpty(game.Ipdb) ? "" : $"https://www.ipdb.org/machine.cgi?id={game.Ipdb}";
                    game.NavigateToIpdbCommand = new ActionCommand(() => NavigateToIpdb(game.IpdbUrl));
                    game.Content.Init(contentTypes);
                });

                games.AddRange(menu.Games);
            });

            return games;
        }

        public static IEnumerable<string> GetContentFileNames(ContentType contentType, string folder)
        {
            var files = contentType.ExtensionsList.Select(ext => Directory.EnumerateFiles(folder, ext));

            return files.SelectMany(x => x).ToList();
        }

        public static IEnumerable<FileDetail> GetUnsupportedMediaFileDetails(ContentType contentType, string folder)
        {
            // return all files that don't match the supported file extensions
            var supportedExtensions = contentType.ExtensionsList.Select(x => x.TrimStart('*').ToLower()).ToList();
            var kindredExtensions = contentType.KindredExtensionsList.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.TrimStart('*').ToLower());
            supportedExtensions.AddRange(kindredExtensions);

            var allFiles = Directory.EnumerateFiles(folder).Select(x => x.ToLower());

            var unsupportedFiles = allFiles.Where(file => !supportedExtensions.Any(file.EndsWith));

            var unsupportedFixFiles = unsupportedFiles.Select(file => new FileDetail(contentType.Enum, HitTypeEnum.Unsupported, null, file, new FileInfo(file).Length));

            return unsupportedFixFiles.ToList();
        }

        public static IEnumerable<FileDetail> AssociateContentFilesWithGames(IReadOnlyCollection<Game> games, IEnumerable<string> matchedFiles, ContentType contentType,
            Func<Game, ContentHits> getContentHits)
        {
            var unknownMediaFiles = new List<FileDetail>();

            // for each file, associate it with a game or if one can't be found, then mark it as unknown
            // - ASSOCIATION IS DONE IRRESPECTIVE OF THE USER'S SELECTED PREFERENCE, I.E. THE USE SELECTIONS ARE CHECKED ELSEWHERE
            foreach (var matchedFile in matchedFiles)
            {
                Game matchedGame;
                var fuzzyFileNameDetails = GetFuzzyFileNameDetails(matchedFile);

                // check for hit..
                // - only 1 hit per file.. but a game can have multiple hits.. with a maximum of 1 valid hit
                // - ignores the check criteria.. the check criteria is only used in the results (e.g. statistics)
                if ((matchedGame = games.FirstOrDefault(game => game.GetContentName(contentType.Category) == Path.GetFileNameWithoutExtension(matchedFile))) != null)
                {
                    // if a match already exists, then assume this match is a duplicate name with wrong extension
                    // - file extension order is important as it determines the priority of the preferred extension
                    var contentHits = getContentHits(matchedGame);
                    contentHits.Add(contentHits.Hits.Any(hit => hit.Type == HitTypeEnum.CorrectName) ? HitTypeEnum.DuplicateExtension : HitTypeEnum.CorrectName, matchedFile);
                }
                else if ((matchedGame = games.FirstOrDefault(game =>
                    string.Equals(game.GetContentName(contentType.Category), Path.GetFileNameWithoutExtension(matchedFile), StringComparison.CurrentCultureIgnoreCase))) != null)
                {
                    getContentHits(matchedGame).Add(HitTypeEnum.WrongCase, matchedFile);
                }
                else if (contentType.Category == ContentTypeCategoryEnum.Media && (matchedGame = games.FirstOrDefault(game => game.TableFile == Path.GetFileNameWithoutExtension(matchedFile))) != null)
                {
                    getContentHits(matchedGame).Add(HitTypeEnum.TableName, matchedFile);
                }
                // fuzzy match against table name (non-media) or description (media)
                else if ((matchedGame = games.FirstOrDefault(game => FuzzyMatch(game.TableFile, fuzzyFileNameDetails) || FuzzyMatch(game.Description, fuzzyFileNameDetails))) != null)
                {
                    getContentHits(matchedGame).Add(HitTypeEnum.Fuzzy, matchedFile);
                }
                else
                {
                    // possible for..
                    // - pinball --> new table files added AND the database not updated yet
                    // - media --> as per pinball OR extra/redundant files exist where there is no table (yet!)
                    unknownMediaFiles.Add(new FileDetail(contentType.Enum, HitTypeEnum.Unknown, null, matchedFile, new FileInfo(matchedFile).Length));
                }
            }

            return unknownMediaFiles;
        }

        public static bool TryGet(IEnumerable<Hit> hits, out Hit hit, params HitTypeEnum[] hitTypes)
        {
            // return the first entry found
            hit = hits.FirstOrDefault(h => hitTypes.Contains(h.Type));
            return hit != null;
        }

        public static (string name, string manufacturer, int? year) GetFuzzyFileNameDetails(string fileName)
        {
            // return the fuzzy portion of the filename..
            // - no file extensions
            // - name: up to last opening parenthesis (if it exists!)
            // - manufacturer: words up to the first year (if it exists!)
            // - year: digits up to the first closing parenthesis (if it exists!)
            string name;
            string manufacturer = null;
            int? year = null;

            fileName = Path.GetFileNameWithoutExtension(fileName ?? "");
            var result = _fuzzyFileNameRegex.Match(fileName);

            if (result.Success)
            {
                // strip any additional parenthesis content out
                name = result.Groups["name"].Value.ToNull();

                manufacturer = result.Groups["manufacturer"].Value.Split("(").Last().ToNull();

                if (int.TryParse(result.Groups["year"].Value, out var parsedYear))
                    year = parsedYear;
            }
            else
                name = fileName.ToNull();

            // fuzzy clean the name field
            name = FuzzyClean(name);

            return (name, manufacturer, year);
        }

        private static string ToNull(this string name) => name == "" ? null : name.ToLower().Trim();

        public static bool FuzzyMatch(string first, string second)
        {
            return FuzzyMatch(first, GetFuzzyFileNameDetails(second));
        }
        
        private static bool FuzzyMatch(string first, (string name, string manufacturer, int? year) secondFuzzy)
        {
            var firstFuzzy = GetFuzzyFileNameDetails(first);

            var exactMatch = firstFuzzy.name == secondFuzzy.name;

            var startsMatch = firstFuzzy.name.Length >= 15 && secondFuzzy.name.Length >= 15 && (firstFuzzy.name.StartsWith(secondFuzzy.name) || secondFuzzy.name.StartsWith(firstFuzzy.name));

            var containsMatch = firstFuzzy.name.Length >= 20 && secondFuzzy.name.Length >= 20 && (firstFuzzy.name.Contains(secondFuzzy.name) || secondFuzzy.name.Contains(firstFuzzy.name));

            // if both names include years.. then they must match
            var yearMismatch = firstFuzzy.year.HasValue && secondFuzzy.year.HasValue && Math.Abs(firstFuzzy.year.Value - secondFuzzy.year.Value) > 1;

            return !yearMismatch && (exactMatch || startsMatch || containsMatch);
        }

        private static string FuzzyClean(string first)
        {
            // clean the string to make it a little cleaner for subsequent matching
            // - order is important!
            var fuzzyClean = first?.ToLower()
                    .Replace("the", "")
                    .Replace("&apos;", "")
                    .Replace("'", "")
                    .Replace("`", "")
                    .Replace(",", "")
                    .Replace(";", "")
                    .Replace("!", "")
                    .Replace("?", "")
                    .Replace("-", " ")
                    .Replace(" - ", "")
                    .Replace("_", " ")
                    .Replace(".", " ")
                    .Replace("&", " and ")
                    .Replace(" iv", " 4")
                    .Replace(" iii", " 3")
                    .Replace(" ii", " 2")
                    .Replace("    ", " ")
                    .Replace("   ", " ")
                    .Replace("  ", " ")
                    .TrimStart()
                    .Trim()
                ;

            return fuzzyClean;
        }

        // regex
        // - faster: name via looking for the first opening parenthesis.. https://regex101.com/r/CxKJK1/1
        // - slower: name is greedy search using the last opening parenthesis.. https://regex101.com/r/xiXsML/1.. @"(?<name>.*)\((?<manufacturer>\D*)(?<year>\d*)\).*"
        private static readonly Regex _fuzzyFileNameRegex = new Regex(@"(?<name>[^(]*)\((?<manufacturer>\D*)(?<year>\d*)\)", RegexOptions.Compiled);
     
        private static void NavigateToIpdb(string url) => Process.Start(new ProcessStartInfo(url) {UseShellExecute = true});
    }
}