using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ClrVpin.Logging;
using ClrVpin.Models.Shared.Game;
using Utils;
using Utils.Extensions;

namespace ClrVpin.Shared.Fuzzy
{
    public static class Fuzzy
    {
        static Fuzzy()
        {
            // compile and store regex to improve performance

            // chars
            // - special consideration for non-ascii characters (i.e. 8 bit chars) as handling of these between IPDB, XML DB, and file names is often inconsistent
            string[] specialChars = { "&apos;", "ï¿½", "'", "`", "’", ",", ";", "!", @"\?", @"[^\x00-\x7F]", "&", @"\(", @"\)" };
            var pattern = string.Join('|', specialChars);
            _trimCharRegex = new Regex($@"({pattern})", RegexOptions.Compiled);

            string[] trailingPeriod = { @"\.{1}$" };
            pattern = string.Join('|', trailingPeriod);
            _trimLastPeriodRegex = new Regex($@"({pattern})", RegexOptions.Compiled);

            // words
            _titleCaseWordExceptions = new[] { "MoD", "SG1bsoN" };
            Authors = new[] { "jps", "jp's", "sg1bson", "vpw", "starlion", "pinball58", "vp99", "balutito" };
            string[] language = { "a", "and", "n'", "'n", "the", "premium", "en" };
            string[] vpx = { "vpx", "mod", "vp10", "4k", "b2s", "4player", "2021", "2022", "2023", "2024" };
            string[] types = { "em", "ss", "pm" };
            pattern = string.Join('|', Authors.Concat(language).Concat(vpx).Concat(types));

            // captures first word match
            // - handles start and end of string
            // - used with Regex.Replace will capture multiple matches at once.. same word or other other words
            // - lookahead match without capture: https://stackoverflow.com/a/3926546/227110
            // - https://regex101.com/r/DoztL5/1
            _wholeWordRegex = new Regex($@"(?<=^|[^a-z^A-Z])({pattern})(?=$|[^a-zA-Z])", RegexOptions.Compiled);

            // first pass single whitespace
            // - performed BEFORE other checks that aren't sensitive to these changes
            string[] firstPassSpacings = { "-", " - " };
            pattern = string.Join('|', firstPassSpacings);
            _addSpacingFirstPassRegex = new Regex($@"({pattern})", RegexOptions.Compiled);

            // second pass single whitespace
            // - performed AFTER other checks that are sensitive to these changes, e.g. version checking
            string[] spacings = { "_", @"\." };
            pattern = string.Join('|', spacings);
            _addSpacingSecondPassRegex = new Regex($@"({pattern})", RegexOptions.Compiled);

            // version
            // - number can be anywhere in the string
            //   - assumes other processing has completed, e.g. strip file extension, author, etc
            //   - extra whitespace ok
            // - 2 options..
            //   a. number without decimal (or underscore) - requires v/V prefix
            //   b. number with decimal (or underscore) - optional v/V prefix
            _versionRegex = new Regex(@"([vV]\d+$|[vV]?\d+\.+\d*\.*\d*\s*$|[vV]?\d+_+\d*_*\d*\s*$)", RegexOptions.Compiled);

            // preamble
            // - number.. aka file id (assumed 5 digits or more)
            _preambleRegex = new Regex(@"^(\d{5,})", RegexOptions.Compiled);

            // multiple whitespace
            _multipleWhitespaceRegex = new Regex(@"(\s{2,})", RegexOptions.Compiled);

            // file name info parsing
            // - faster: name via looking for the FIRST opening parenthesis.. https://regex101.com/r/tRqeOH/1..         (?<name>[^(]*)[(](?<manufacturer>\D*)(?<year>\d*)\D*\)
            // - slower: name is greedy search, terminating via the LAST opening parenthesis.. https://regex101.com/r/xiXsML/1..   (?<name>.*)[(](?<manufacturer>\D*)((?<year>\d{4})|\d*)\D*[)].*
            _fileNameInfoRegex = new Regex(@"(?<name>.*)[(](?<manufacturer>\D*)((?<year>\d{4})|\d*)\D*[)].*", RegexOptions.Compiled);

            // non-standard file name info parsing
            // - intended as a second chance match, i.e. only invoked if the standard (above) fails
            // - e.g. Bally Roller Derby 2.0.vpx --> manufacturer included in the table name without any parenthesis
            // - https://regex101.com/r/AYTJbL/1
            string[] manufacturers = { "bally", "williams", "stern" };
            pattern = string.Join('|', manufacturers);
            // todo; support year (in addition to manufacturer?)
            _nonStandardFileNameInfoRegex = new Regex($@".*(?<manufacturer>{pattern}).*", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            // aliases
            // - replace specific word(s)
            // - e.g. Big Injun --> Big Indian
            _aliases = new Dictionary<string, string>
            {
                new("big injun", "big indian")
            };
        }

        private static decimal MinMatchScore => Model.Settings.MatchFuzzyMinimumPercentage;
        private static decimal MinMatchWarningScore => MinMatchScore * 1.2m;

        public static string Clean(string name, bool removeAllWhiteSpace)
        {
            if (name == null)
                return null;

            // clean the string to make it a little cleaner for subsequent matching.. order is VERY important!

            // insert word break for any camel casing, e.g. "SpotACard" becomes "Sport A Card"
            var cleanName = name.FromCamelCase(_titleCaseWordExceptions);

            // easier comparison when everything is in the same case
            cleanName = cleanName.ToLowerAndTrim();

            // replace any known word aliases
            cleanName = SubstituteAliases(cleanName);

            // trim (whole) words
            cleanName = _wholeWordRegex.Replace(cleanName, "");

            // trim chars - must trim extension period for version to work correctly!
            cleanName = _trimCharRegex.Replace(cleanName, "");

            // trim last period
            // - required for version to work correctly
            // - only if there are NOT 3 (or more) trailing periods, e.g. to cater for some tables like '1-2-3...' which use the trailing periods as part of their table name
            if (!cleanName.EndsWith("..."))
                cleanName = _trimLastPeriodRegex.Replace(cleanName, "");

            // add whitespace - first pass
            cleanName = _addSpacingFirstPassRegex.Replace(cleanName, " ");

            // trim version
            if (!cleanName.EndsWith("..."))
                cleanName = _versionRegex.Replace(cleanName, "");

            // trim preamble
            cleanName = _preambleRegex.Replace(cleanName, "");

            // add whitespace - second pass
            cleanName = _addSpacingSecondPassRegex.Replace(cleanName, " ");

            // substitutions
            cleanName = cleanName
                .Replace(" iv", " 4")
                .Replace(" iii", " 3")
                .Replace(" ii", " 2");

            // remove multiple white space
            cleanName = _multipleWhitespaceRegex.Replace(cleanName, " ");

            // final white space trimming
            cleanName = cleanName.Trim();
            if (removeAllWhiteSpace)
                cleanName = cleanName.Replace(" ", "");

            return cleanName;
        }

        public static FuzzyNameDetails GetNameDetails(string sourceName, bool isFileName)
        {
            // cater for no source name, e.g. Game.Description null value.. which should no longer be possible as it's now initialized to empty string when deserialized
            sourceName ??= "";

            // return the fuzzy portion of the filename..
            // - no file extensions
            // - name: up to last opening parenthesis (if it exists!)
            // - manufacturer: words up to the first year (if it exists!)
            // - year: digits up to the first closing parenthesis (if it exists!)
            string name;
            string manufacturer = null;
            int? year = null;

            // only strip the extension if it exists, i.e. a real file and not a DB entry
            if (isFileName)
                sourceName = Path.GetFileNameWithoutExtension(sourceName);

            var result = _fileNameInfoRegex.Match(sourceName);

            if (result.Success)
            {
                // strip any additional parenthesis content out
                name = result.Groups["name"].Value.ToNull();

                manufacturer = result.Groups["manufacturer"].Value.Split("(").Last().ToNull();

                if (int.TryParse(result.Groups["year"].Value, out var parsedYear))
                    year = parsedYear;
            }
            else
            {
                // try a non-standard naming variant
                // - e.g. Bally Roller Derby 2.0.vpx --> manufacturer included in the table name without any parenthesis
                result = _nonStandardFileNameInfoRegex.Match(sourceName);
                if (result.Success)
                {
                    manufacturer = result.Groups["manufacturer"].Value.ToNull();
                    name = sourceName.Replace(manufacturer, "");
                }
                else
                {
                    name = sourceName.ToNull();
                }
            }

            // fuzzy clean the name field
            var cleanName = Clean(name, false);
            var cleanNameNoWhiteSpace = Clean(name, true);

            // fuzzy clean the manufacturer field
            var cleanManufacturer = Clean(manufacturer, false);
            var cleanManufacturerNoWhiteSpace = Clean(manufacturer, true);

            return new FuzzyNameDetails(sourceName, name?.Trim() ?? "", cleanName.ToLowerAndTrim(), cleanNameNoWhiteSpace.ToLowerAndTrim(),
                cleanManufacturer.ToLowerAndTrim(), cleanManufacturerNoWhiteSpace.ToLowerAndTrim(), year);
        }

        // fuzzy match against all games
        public static (LocalGame game, int? score, bool isMatch) MatchToLocalDatabase(this IList<LocalGame> games, FuzzyNameDetails fuzzyNameDetails, bool isFile = true)
        {
            // check EVERY DB game entry against the fuzzy name details (which can be a file file for scanner/rebuilder OR online game entry for importer(file to look for the best match)
            // - Match will create a fuzzy version (aka cleaned) of each game DB entry so it can be compared against the fuzzy file details (already cleaned)
            // - Match table name (non-media) OR description (media)
            var tableFileMatches = games.Select(game => new MatchDetail { LocalGame = game, MatchResult = Match(game.Fuzzy.TableDetails, fuzzyNameDetails) });
            var descriptionMatches = games.Select(game => new MatchDetail { LocalGame = game, MatchResult = Match(game.Fuzzy.DescriptionDetails, fuzzyNameDetails) });

            var orderedMatches = tableFileMatches.Concat(descriptionMatches)
                .OrderByDescending(x => x.MatchResult.success)
                .ThenByDescending(x => x.MatchResult.score)
                .ThenByDescending(x => x.LocalGame.Game.Name.Length) // tie breaker
                .ToList();

            var preferredMatch = orderedMatches.FirstOrDefault();

            // second chance
            // - if there's still no match, check if the fuzzy name (i.e. after processing) has a UNIQUE match within in the game DB (using a simple 'to lowercase' check)
            // - if the second chance match is the same as the preferred match, then 
            var isUniqueMatch = false;
            MatchDetail uniqueMatch;
            if (preferredMatch?.MatchResult.score < MinMatchScore && (uniqueMatch = GetUniqueMatch(orderedMatches, fuzzyNameDetails.Name, fuzzyNameDetails.NameNoWhiteSpace)) != null)
                isUniqueMatch = ChangeMatchAndChangeScore(out preferredMatch, uniqueMatch, 85);

            // third chance
            // - only applied if no unique match was found, i.e. irrespective of whether score was sufficient
            // - check if the non-fuzzy name (i.e. before processing) has a UNIQUE match within in the game DB (using a simple 'to lowercase' check)
            if (!isUniqueMatch && preferredMatch?.MatchResult.score < MinMatchScore && (uniqueMatch = GetUniqueMatch(orderedMatches, fuzzyNameDetails.ActualName, null)) != null)
                isUniqueMatch = ChangeMatchAndChangeScore(out preferredMatch, uniqueMatch, 85);

            var isMatch = preferredMatch?.MatchResult.score >= MinMatchScore;
            var isOriginal = preferredMatch?.LocalGame.Derived.IsOriginal == true || fuzzyNameDetails.IsOriginal;

            var fuzzyLog = $"fuzzy table match: success={isMatch}, score={$"{preferredMatch?.MatchResult.score},",-4} isUniqueMatch(second chance)={isUniqueMatch}, isOriginal={isOriginal}\n" +
                           $"- source {(isFile ? "file" : "feed")}:      {LogGameInfo(fuzzyNameDetails.ActualName, null, fuzzyNameDetails.Manufacturer, fuzzyNameDetails.Year?.ToString())}\n" +
                           $"- matched db table: {LogGameInfo(preferredMatch?.LocalGame.Game.Name, preferredMatch?.LocalGame.Game.Description, preferredMatch?.LocalGame.Game.Manufacturer, preferredMatch?.LocalGame.Game.Year)}";

            if (!(isOriginal && Model.Settings.SkipLoggingForOriginalTables))
            {
                if (isMatch)
                    Logger.Debug(fuzzyLog, true); // log as debug diagnostic as matches are are typically of 'lesser' interest
                else
                    Logger.Warn(fuzzyLog, true);
            }

            return (preferredMatch?.LocalGame, preferredMatch?.MatchResult.score, isMatch);
        }

        public static string LogGameInfo(string name, string description, string manufacturer, string year) => $"name={$"'{name}',",-55} description={$"'{description}',",-55} manufacturer={$"'{manufacturer}',",-20} year={$"{year}",-5}";

        public static (bool success, int score) Match(FuzzyNameDetails localGameFuzzyDetails, FuzzyNameDetails fileFuzzyDetails)
        {
            var nameMatchScore = GetNameMatchScore(localGameFuzzyDetails.Name, localGameFuzzyDetails.NameNoWhiteSpace, fileFuzzyDetails.Name, fileFuzzyDetails.NameNoWhiteSpace, true);

            // manufacturer matching is the EXACT same as name matching, but the result is scaled back to 10% to reflect it's lesser importance
            // - the additional scoring though is important to distinguish between games that match exactly but only 1 has the correct manufacturer 
            var manufacturerScore = GetNameMatchScore(localGameFuzzyDetails.Manufacturer, localGameFuzzyDetails.ManufacturerNoWhiteSpace,
                fileFuzzyDetails.Manufacturer, fileFuzzyDetails.ManufacturerNoWhiteSpace, true) / 10;

            var yearMatchScore = GetYearMatchScore(localGameFuzzyDetails.Year, fileFuzzyDetails.Year);
            var lengthScore = GetLengthMatchScore(localGameFuzzyDetails);

            var score = nameMatchScore + manufacturerScore + yearMatchScore + lengthScore;

            // total 'identity check/match' score must be >= _minMatchScore
            return (score >= MinMatchScore, score);
        }

        public static int GetLengthMatchScore(FuzzyNameDetails localGameFuzzyDetails)
        {
            // score the match length of the underlying game database entry (i.e. not the file!!)
            // - use the sanitized name to avoid white space, manufacturer, year, etc
            // - 1 for every character beyond 8 characters.. to a maximum of 15pts
            var lengthScore = (localGameFuzzyDetails.NameNoWhiteSpace?.Length ?? 0) - 8;

            return Math.Max(0, Math.Min(15, lengthScore));
        }

        public static (string, bool) GetScoreDetail(int? score)
        {
            var warning = score < MinMatchWarningScore;

            var message = score == null ? "n/a" : $"{score / 100f:P0}";
            if (warning)
                message = $"low {message}";

            return (message, warning);
        }

        private static string SubstituteAliases(string cleanName)
        {
            _aliases.ForEach(alias =>
            {
                if (cleanName.Contains(alias.Key))
                    cleanName = cleanName.Replace(alias.Key, alias.Value);
            });

            return cleanName;
        }

        private static bool ChangeMatchAndChangeScore(out MatchDetail preferredMatch, MatchDetail secondChanceMatch, int scoreAdjustment)
        {
            secondChanceMatch.MatchResult.score += scoreAdjustment;
            preferredMatch = secondChanceMatch;

            return true;
        }

        private static MatchDetail GetUniqueMatch(IEnumerable<MatchDetail> orderedMatches, string name, string nameNoWhiteSpace)
        {
            if (name == null)
                return null;

            // re-use the fuzzy name match scoring to determine if we have a match
            // - check against name OR description.. to ensure both are included (if they match)
            // - don't enable levenshtein checking as this slows down the matching considerably
            //   e.g. ~1k missing tables x ~1k database entries = ~1M calculations taking about 120s (on my i7 10th gen rig)
            // - any score match is deemed ok at this stage
            var matchesContainingFileName = orderedMatches.Where(match =>
                GetNameMatchScore(name, nameNoWhiteSpace, match.LocalGame.Game.Name, match.LocalGame.Derived.NameLowerCase, false) > 0 ||
                GetNameMatchScore(name, nameNoWhiteSpace, match.LocalGame.Game.Description, match.LocalGame.Derived.DescriptionLowerCase, false) > 0).ToList();

            // only considered a 'unique match' if it matches EXACTLY twice.. one for table and description
            return matchesContainingFileName.Count == 2 ? matchesContainingFileName.First() : null;
        }

        private static int GetYearMatchScore(int? firstYear, int? secondYear)
        {
            var yearMatchScore = 0;

            // if both names include years.. then check adjust the score based on the difference
            if (firstYear.HasValue && secondYear.HasValue)
            {
                var yearDifference = Math.Abs(firstYear.Value - secondYear.Value);

                yearMatchScore = yearDifference switch
                {
                    0 => 50,
                    1 => 40,
                    2 => -50,
                    3 => -100,
                    _ => -1000
                };
            }

            return yearMatchScore;
        }

        private static int GetNameMatchScore(string first, string firstNoWhiteSpace, string second, string secondNoWhiteSpace, bool isLevenshteinEnabled)
        {
            // matching order is important.. highest priority matches must be first!
            var score = IsExactMatch(first, second) ? 150 + ScoringNoWhiteSpaceBonus : 0;
            if (score == 0)
                score = IsExactMatch(firstNoWhiteSpace, secondNoWhiteSpace) ? 150 : 0;

            // levenshtein distance
            if (score == 0 && isLevenshteinEnabled)
                score = IsLevenshteinMatch(14, 2, first, second) ? 120 + ScoringNoWhiteSpaceBonus : 0;
            if (score == 0 && isLevenshteinEnabled)
                score = IsLevenshteinMatch(14, 2, secondNoWhiteSpace, firstNoWhiteSpace) ? 120 : 0;

            if (score == 0)
                score = IsStartsMatch(14, first, second) ? 100 + ScoringNoWhiteSpaceBonus : 0;
            if (score == 0)
                score = IsStartsMatch(14, secondNoWhiteSpace, firstNoWhiteSpace) ? 100 : 0;

            if (score == 0)
                score = IsStartsMatch(10, first, second) ? 60 + ScoringNoWhiteSpaceBonus : 0;
            if (score == 0)
                score = IsStartsMatch(10, secondNoWhiteSpace, firstNoWhiteSpace) ? 60 : 0;

            if (score == 0)
                score = IsStartsMatch(8, first, second) ? 50 + ScoringNoWhiteSpaceBonus : 0;
            if (score == 0)
                score = IsStartsMatch(8, secondNoWhiteSpace, firstNoWhiteSpace) ? 50 : 0;

            if (score == 0)
                score = IsContainsMatch(17, first, second) ? 100 + ScoringNoWhiteSpaceBonus : 0;
            if (score == 0)
                score = IsContainsMatch(17, secondNoWhiteSpace, firstNoWhiteSpace) ? 100 : 0;

            if (score == 0)
                score = IsContainsMatch(13, first, second) ? 60 + ScoringNoWhiteSpaceBonus : 0;
            if (score == 0)
                score = IsContainsMatch(13, secondNoWhiteSpace, firstNoWhiteSpace) ? 60 : 0;

            if (score == 0)
                score = IsContainsMatch(11, first, second) ? 30 + ScoringNoWhiteSpaceBonus : 0;
            if (score == 0)
                score = IsContainsMatch(11, secondNoWhiteSpace, firstNoWhiteSpace) ? 30 : 0;

            if (score == 0)
                score = IsStartsAndEndsMatch(7, 8, first, second) ? 60 + ScoringNoWhiteSpaceBonus : 0;
            if (score == 0)
                score = IsStartsAndEndsMatch(7, 8, secondNoWhiteSpace, firstNoWhiteSpace) ? 60 : 0;

            return score;
        }

        private static string ToNull(this string name) => string.IsNullOrWhiteSpace(name) ? null : name;
        private static string ToLowerAndTrim(this string name) => string.IsNullOrWhiteSpace(name) ? null : name.ToLower().Trim();

        private static bool IsExactMatch(string first, string second)
        {
            if (first == null || second == null)
                return false;

            return first == second;
        }

        private static bool IsLevenshteinMatch(int minStringLength, int maxDistance, string first, string second)
        {
            if (first == null || second == null)
                return false;

            if (minStringLength > first.Length || minStringLength > second.Length)
                return false;

            return LevenshteinDistance.Calculate(first, second) <= maxDistance;
        }

        private static bool IsStartsMatch(int minStringLength, string first, string second)
        {
            if (first == null || second == null)
                return false;

            if (minStringLength > first.Length || minStringLength > second.Length)
                return false;

            return first.StartsWith(second.Remove(minStringLength)) || second.StartsWith(first.Remove(minStringLength));
        }

        private static bool IsContainsMatch(int minStringLength, string first, string second)
        {
            if (first == null || second == null)
                return false;

            if (minStringLength > first.Length || minStringLength > second.Length)
                return false;

            return first.Contains(second.Remove(minStringLength)) || second.Contains(first.Remove(minStringLength));
        }

        private static bool IsStartsAndEndsMatch(int startMatchLength, int endMatchLength, string first, string second)
        {
            if (first == null || second == null)
                return false;

            if (first.Length < Math.Max(startMatchLength, endMatchLength) || second.Length < Math.Max(startMatchLength, endMatchLength))
                return false;

            return first.StartsWith(second.Remove(startMatchLength)) && first.EndsWith(second.Substring(second.Length - endMatchLength));
        }

        // non-anonymous type so it can be passed as a method parameter
        // - refer https://stackoverflow.com/questions/6624811/how-to-pass-anonymous-types-as-parameters
        private class MatchDetail
        {
            public LocalGame LocalGame;
            public (bool success, int score) MatchResult;
        }

        public const int ScoringNoWhiteSpaceBonus = 5;

        private static readonly Regex _fileNameInfoRegex;
        private static readonly Regex _trimCharRegex;
        private static readonly Regex _trimLastPeriodRegex;
        private static readonly Regex _wholeWordRegex;
        private static readonly Regex _addSpacingFirstPassRegex;
        private static readonly Regex _addSpacingSecondPassRegex;
        private static readonly Regex _versionRegex;
        private static readonly Regex _preambleRegex;
        private static readonly Regex _multipleWhitespaceRegex;
        private static readonly string[] _titleCaseWordExceptions;
        public static readonly string[] Authors;
        private static readonly Regex _nonStandardFileNameInfoRegex;
        private static readonly Dictionary<string, string> _aliases;
    }
}