using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ClrVpin.Models;
using Utils.Extensions;

namespace ClrVpin.Shared
{
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public static class Fuzzy
    {
        static Fuzzy()
        {
            // compile and store regex to improve performance

            // chars
            // - special consideration for non-ascii characters (i.e. 8 bit chars) as handling of these between IPDB, XML DB, and file names is often inconsistent
            string[] specialChars = { "&apos;", "ï¿½", "'", "`", "’", ",", ";", "!", @"\?", @"\.$", @"[^\x00-\x7F]" };
            var pattern = string.Join('|', specialChars);
            _trimCharRegex = new Regex($@"({pattern})", RegexOptions.Compiled);

            // words
            _titleCaseWordExceptions = new[] { "MoD", "SG1bsoN" };
            string[] authors = { "jps", "jp's", "sg1bson", "vpw", "starlion", "pinball58" };
            string[] language = { "a", "and", "the", "premium" };
            string[] vpx = { "vpx", "mod", "vp10", "4k" };
            pattern = string.Join('|', authors.Concat(language).Concat(vpx));

            // captures first word match
            // - handles start and end of string
            // - used with Regex.Replace will capture multiple matches at once.. same word or other other words
            // - lookahead match without capture: https://stackoverflow.com/a/3926546/227110
            // - https://regex101.com/r/DoztL5/1
            _trimWordRegex = new Regex($@"(?<=^|[^a-z^A-Z])({pattern})(?=$|[^a-zA-Z])", RegexOptions.Compiled | RegexOptions.IgnoreCase);

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
            // - number must be at end of string
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
            // - faster: name via looking for the first opening parenthesis.. https://regex101.com/r/tRqeOH/1
            // - slower: name is greedy search using the last opening parenthesis.. https://regex101.com/r/xiXsML/1.. @"(?<name>.*)\((?<manufacturer>\D*)(?<year>\d*)\).*"
            _fileNameInfoRegex = new Regex(@"(?<name>[^(]*)\((?<manufacturer>\D*)((?<year>\d{4})|\d*)\D*\)", RegexOptions.Compiled);
        }

        public static string Clean(string name, bool removeAllWhiteSpace)
        {
            if (name == null)
                return null;

            // clean the string to make it a little cleaner for subsequent matching
            // - order is important!

            // insert word break for any camel casing, e.g. "SpotACard" becomes "Sport A Card"
            var cleanName = name.FromTitleCase(_titleCaseWordExceptions);

            // easier comparison when everything is in the same case
            cleanName = cleanName.ToLowerAndTrim();

            // trim (whole) words
            cleanName = _trimWordRegex.Replace(cleanName, "");

            // trim chars - must trim extension period for version to work correctly!
            cleanName = _trimCharRegex.Replace(cleanName, "");

            // add whitespace - first pass
            cleanName = _addSpacingFirstPassRegex.Replace(cleanName, " ");

            // trim version
            cleanName = _versionRegex.Replace(cleanName, "");

            // trim preamble
            cleanName = _preambleRegex.Replace(cleanName, "");

            // add whitespace - second pass
            cleanName = _addSpacingSecondPassRegex.Replace(cleanName, " ");

            // substitutions
            cleanName = cleanName
                .Replace("&", " and ")
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

        public static (string name, string nameNoWhiteSpace, string manufacturer, int? year) GetNameDetails(string sourceName, bool isFileName)
        {
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
                sourceName = Path.GetFileNameWithoutExtension(sourceName ?? "");

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
                name = sourceName.ToNull();

            // fuzzy clean the name field
            var cleanName = Clean(name, false);
            var cleanNameNoWhiteSpace = Clean(name, true);

            return (cleanName.ToLowerAndTrim(), cleanNameNoWhiteSpace.ToLowerAndTrim(), manufacturer.ToLowerAndTrim(), year);
        }

        // fuzzy match against all games
        public static (Game game, int? score) Match(this IList<Game> games, (string name, string nameNoWhiteSpace, string manufacturer, int? year) fuzzyFileDetails)
        {
            // check EVERY game against the file to look for the best match
            // - e.g. the 2nd DB entry (i.e. the sequel) should be matched..
            //        - fuzzy file="Cowboy Eight Ball 2"
            //        - DB entries (in order)="Cowboy Eight Ball (LTD do Brasil Divers�es Eletr�nicas Ltda 1981)", "Cowboy Eight Ball 2 (LTD do Brasil Divers�es Eletr�nicas Ltda 1981)"
            // - Match table name (non-media) OR description (media)
            var tableFileMatches = games.Select(game => new MatchDetail { Game = game, MatchResult = Match(game.TableFile, fuzzyFileDetails) });
            var descriptionMatches = games.Select(game => new MatchDetail { Game = game, MatchResult = Match(game.Description, fuzzyFileDetails) });

            var orderedMatches = tableFileMatches.Concat(descriptionMatches)
                .OrderByDescending(x => x.MatchResult.success)
                .ThenByDescending(x => x.MatchResult.score)
                .ThenByDescending(x => x.Game.TableFile.Length) // tie breaker
                .ToList();

            var preferredMatch = orderedMatches.FirstOrDefault();
            var score = preferredMatch?.MatchResult.score;

            // if there's still no match, check if file has a UNIQUE match within in the game DB
            if (score < MinMatchScore && (preferredMatch = GetUniqueMatch(orderedMatches, fuzzyFileDetails.name)) != null)
            {
                score = preferredMatch.MatchResult.score;
                score += 85;
            }

            if (score < MinMatchScore && (preferredMatch = GetUniqueMatch(orderedMatches, fuzzyFileDetails.name, 11)) != null)
            {
                score = preferredMatch.MatchResult.score;
                score += 50;
            }

            return (score >= MinMatchScore ? preferredMatch?.Game : null, score);
        }

        public static (bool success, int score) Match(string gameDetail, (string name, string nameNoWhiteSpace, string manufacturer, int? year) fileFuzzyDetails)
        {
            var gameDetailFuzzyDetails = GetNameDetails(gameDetail, false);

            var nameMatchScore = GetNameMatchScore(gameDetailFuzzyDetails.name, gameDetailFuzzyDetails.nameNoWhiteSpace, fileFuzzyDetails.name, fileFuzzyDetails.nameNoWhiteSpace);
            var yearMatchScore = GetYearMatchScore(gameDetailFuzzyDetails.year, fileFuzzyDetails.year);
            var lengthScore = GetLengthMatchScore(gameDetailFuzzyDetails);

            var score = yearMatchScore + nameMatchScore + lengthScore;

            // total 'identity check/match' score must be 100 or more
            return (score >= 100, score);
        }

        public static int GetLengthMatchScore((string name, string nameNoWhiteSpace, string manufacturer, int? year) fuzzyGameDetails)
        {
            // score the match length of the underlying game database entry (i.e. not the file!!)
            // - use the sanitized name to avoid white space, manufacturer, year, etc
            // - 1 for every character beyond 8 characters.. to a maximum of 15pts
            var lengthScore = fuzzyGameDetails.nameNoWhiteSpace.Length - 8;

            return Math.Max(0, Math.Min(15, lengthScore));
        }

        public static (string, bool) GetScoreDetail(int? score)
        {
            var warning = score < 120;

            var message = score == null ? "n/a" : $"{score / 100f:P0}";
            if (warning)
                message = $"low {message}";

            return (message, warning);
        }

        private static MatchDetail GetUniqueMatch(List<MatchDetail> orderedMatches, string fuzzyFileName, int? startMatchLength = null)
        {
            // only strip file name if a length provided AND it is les than the string lenth
            if (fuzzyFileName.Length > startMatchLength)
                fuzzyFileName = fuzzyFileName.Remove(startMatchLength.Value);

            // check if we have a match that contains the fuzzy file name in BOTH the table and description
            var matchesContainingFileName = orderedMatches.Where(match => match.Game.TableFile.ToLower().Contains(fuzzyFileName) ||
                                                                          match.Game.Description.ToLower().Contains(fuzzyFileName)).ToList();

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

        private static int GetNameMatchScore(string gameName, string gameNameNoWhiteSpace, string fileName, string fileNameNoWhiteSpace)
        {
            // matching order is important.. highest priority matches must be first!
            var score = IsExactMatch(gameName, fileName) ? 150 + ScoringNoWhiteSpaceBonus : 0;
            if (score == 0)
                score = IsExactMatch(gameNameNoWhiteSpace, fileNameNoWhiteSpace) ? 150 : 0;

            if (score == 0)
                score = IsStartsMatch(14, gameName, fileName) ? 100 + ScoringNoWhiteSpaceBonus: 0;
            if (score == 0)
                score = IsStartsMatch(14, fileNameNoWhiteSpace, gameNameNoWhiteSpace) ? 100 : 0;
            
            if (score == 0)
                score = IsStartsMatch(10, gameName, fileName) ? 60 + ScoringNoWhiteSpaceBonus : 0;
            if (score == 0)
                score = IsStartsMatch(10, fileNameNoWhiteSpace, gameNameNoWhiteSpace) ? 60 : 0;

            if (score == 0)
                score = IsStartsMatch(8, gameName, fileName) ? 50 + ScoringNoWhiteSpaceBonus : 0;
            if (score == 0)
                score = IsStartsMatch(8, fileNameNoWhiteSpace, gameNameNoWhiteSpace) ? 50 : 0;

            if (score == 0)
                score = IsContainsMatch(17, gameName, fileName) ? 100 + ScoringNoWhiteSpaceBonus : 0;
            if (score == 0)
                score = IsContainsMatch(17, fileNameNoWhiteSpace, gameNameNoWhiteSpace) ? 100 : 0;
            
            if (score == 0)
                score = IsContainsMatch(13, gameName, fileName) ? 60 + ScoringNoWhiteSpaceBonus : 0;
            if (score == 0)
                score = IsContainsMatch(13, fileNameNoWhiteSpace, gameNameNoWhiteSpace) ? 60 : 0;

            if (score == 0)
                score = IsStartsAndEndsMatch(7, 8, gameName, fileName) ? 60 + ScoringNoWhiteSpaceBonus : 0;
            if (score == 0)
                score = IsStartsAndEndsMatch(7, 8, fileNameNoWhiteSpace, gameNameNoWhiteSpace) ? 60 : 0;

            return score;
        }

        private static string ToNull(this string name) => string.IsNullOrWhiteSpace(name) ? null : name;
        private static string ToLowerAndTrim(this string name) => string.IsNullOrWhiteSpace(name) ? null : name.ToLower().Trim();

        private static bool IsExactMatch(string first, string second) => first == second;

        private static bool IsStartsMatch(int minStringLength, string first, string second)
        {
            if (minStringLength > first.Length || minStringLength > second.Length)
                return false;

            return first.StartsWith(second.Remove(minStringLength)) || second.StartsWith(first.Remove(minStringLength));
        }

        private static bool IsContainsMatch(int minStringLength, string first, string second)
        {
            if (minStringLength > first.Length || minStringLength > second.Length)
                return false;

            return first.Contains(second.Remove(minStringLength)) || second.Contains(first.Remove(minStringLength));
        }

        private static bool IsStartsAndEndsMatch(int startMatchLength, int endMatchLength, string first, string second)
        {
            if (first.Length < Math.Max(startMatchLength, endMatchLength) || second.Length < Math.Max(startMatchLength, endMatchLength))
                return false;

            return first.StartsWith(second.Remove(startMatchLength)) && first.EndsWith(second.Substring(second.Length - endMatchLength));
        }

        // non-anonymous type so it can be passed as a method parameter
        // - refer https://stackoverflow.com/questions/6624811/how-to-pass-anonymous-types-as-parameters
        private class MatchDetail
        {
            public Game Game;
            public (bool success, int score) MatchResult;
        }

        private const int MinMatchScore = 100;
        public const int ScoringNoWhiteSpaceBonus = 5;

        private static readonly Regex _fileNameInfoRegex;
        private static readonly Regex _trimCharRegex;
        private static readonly Regex _trimWordRegex;
        private static readonly Regex _addSpacingFirstPassRegex;
        private static readonly Regex _addSpacingSecondPassRegex;
        private static readonly Regex _versionRegex;
        private static readonly Regex _preambleRegex;
        private static readonly Regex _multipleWhitespaceRegex;
        private static readonly string[] _titleCaseWordExceptions;
    }
}