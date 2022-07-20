﻿using System;
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
            string[] specialChars = { "&apos;", "ï¿½", "'", "`", "’", ",", ";", "!", @"\?", @"[^\x00-\x7F]", "&" };
            var pattern = string.Join('|', specialChars);
            _trimCharRegex = new Regex($@"({pattern})", RegexOptions.Compiled);

            string[] trailingPeriod = { @"\.{1}$" };
            pattern = string.Join('|', trailingPeriod);
            _trimLastPeriodRegex = new Regex($@"({pattern})", RegexOptions.Compiled);

            // words
            _titleCaseWordExceptions = new[] { "MoD", "SG1bsoN" };
            Authors = new[] { "jps", "jp's", "sg1bson", "vpw", "starlion", "pinball58", "vp99" };
            string[] language = { "a", "and", "n'", "'n", "the", "premium", "en" };
            string[] vpx = { "vpx", "mod", "vp10", "4k", "b2s", "4player", "2021", "2022", "2023", "2024" };
            pattern = string.Join('|', Authors.Concat(language).Concat(vpx));

            // captures first word match
            // - handles start and end of string
            // - used with Regex.Replace will capture multiple matches at once.. same word or other other words
            // - lookahead match without capture: https://stackoverflow.com/a/3926546/227110
            // - https://regex101.com/r/DoztL5/1
            _trimWordRegex = new Regex($@"(?<=^|[^a-z^A-Z])({pattern})(?=$|[^a-zA-Z])", RegexOptions.Compiled);

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
            // - slower: name is greedy search using the LAST opening parenthesis.. https://regex101.com/r/xiXsML/1..   (?<name>.*)[(](?<manufacturer>\D*)((?<year>\d{4})|\d*)\D*[)].*
            _fileNameInfoRegex = new Regex(@"(?<name>[^(]*).*\((?<manufacturer>\D*)((?<year>\d{4})|\d*)\D*\).*", RegexOptions.Compiled);
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
            if (sourceName == null)
                sourceName = "";

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
                name = sourceName.ToNull();

            // fuzzy clean the name field
            var cleanName = Clean(name, false);
            var cleanNameNoWhiteSpace = Clean(name, true);

            // fuzzy clean the manufacturer field
            var cleanManufacturer = Clean(manufacturer, false);
            var cleanManufacturerNoWhiteSpace = Clean(manufacturer, true);

            return new FuzzyNameDetails(sourceName.ToLower(), cleanName.ToLowerAndTrim(), cleanNameNoWhiteSpace.ToLowerAndTrim(),
                cleanManufacturer.ToLowerAndTrim(), cleanManufacturerNoWhiteSpace.ToLowerAndTrim(), year);
        }

        // fuzzy match against all games
        public static (GameDetail game, int? score, bool isMatch) Match(this IList<GameDetail> games, FuzzyNameDetails fuzzyNameDetails, bool isFile = true)
        {
            // check EVERY DB game entry against the fuzzy name details (which can be a file file for scanner/rebuilder OR online game entry for importer(file to look for the best match)
            // - Match will create a fuzzy version (aka cleaned) of each game DB entry so it can be compared against the fuzzy file details (already cleaned)
            // - Match table name (non-media) OR description (media)
            var tableFileMatches = games.Select(game => new MatchDetail { GameDetail = game, MatchResult = Match(game.Fuzzy.TableDetails, fuzzyNameDetails) });
            var descriptionMatches = games.Select(game => new MatchDetail { GameDetail = game, MatchResult = Match(game.Fuzzy.DescriptionDetails, fuzzyNameDetails) });

            var orderedMatches = tableFileMatches.Concat(descriptionMatches)
                .OrderByDescending(x => x.MatchResult.success)
                .ThenByDescending(x => x.MatchResult.score)
                .ThenByDescending(x => x.GameDetail.Game.Name.Length) // tie breaker
                .ToList();

            var preferredMatch = orderedMatches.FirstOrDefault();

            // second chance - if there's still no match, check if the fuzzy file has a UNIQUE match within in the game DB (using a simple 'to lowercase' check)
            var isSecondChanceMatch = false;
            MatchDetail secondChanceMatch;
            if (preferredMatch?.MatchResult.score < MinMatchScore && (secondChanceMatch = GetUniqueMatch(orderedMatches, fuzzyNameDetails.Name)) != null)
                isSecondChanceMatch = UpdateMatchWithSecondChance(out preferredMatch, secondChanceMatch, 85);

            if (preferredMatch?.MatchResult.score < MinMatchScore && (secondChanceMatch = GetUniqueMatch(orderedMatches, fuzzyNameDetails.Name, 11)) != null)
                isSecondChanceMatch = UpdateMatchWithSecondChance(out preferredMatch, secondChanceMatch, 50);

            // third chance - if there's still no match, check if the non-fuzzy file has a UNIQUE match within in the game DB (using a simple 'to lowercase' check)
            if (preferredMatch?.MatchResult.score < MinMatchScore && (secondChanceMatch = GetUniqueMatch(orderedMatches, fuzzyNameDetails.ActualName)) != null)
                isSecondChanceMatch = UpdateMatchWithSecondChance(out preferredMatch, secondChanceMatch, 85);

            if (preferredMatch?.MatchResult.score < MinMatchScore && (secondChanceMatch = GetUniqueMatch(orderedMatches, fuzzyNameDetails.ActualName, 11)) != null)
                isSecondChanceMatch = UpdateMatchWithSecondChance(out preferredMatch, secondChanceMatch, 50);

            var isMatch = preferredMatch?.MatchResult.score >= MinMatchScore;
            var isOriginal = preferredMatch?.GameDetail.Derived.IsOriginal == true || fuzzyNameDetails.IsOriginal;

            var fuzzyLog = $"fuzzy table match: success={isMatch}, score={$"{preferredMatch?.MatchResult.score},",-4} isSecondChanceMatch={isSecondChanceMatch}, isOriginal={isOriginal}\n" +
                               $"- source {(isFile ? "file" : "feed")}:      {LogGameDetail(fuzzyNameDetails.ActualName, null, fuzzyNameDetails.Manufacturer, fuzzyNameDetails.Year?.ToString())}\n" +
                               $"- matched db table: {LogGameDetail(preferredMatch?.GameDetail.Game.Name, preferredMatch?.GameDetail.Game.Description, preferredMatch?.GameDetail.Game.Manufacturer, preferredMatch?.GameDetail.Game.Year)}";

            if (!(isOriginal && Model.Settings.SkipLoggingForOriginalTables))
            {
                if (isMatch)
                    Logger.Debug(fuzzyLog, true); // log as debug diagnostic as matches are are typically of 'lesser' interest
                else
                    Logger.Warn(fuzzyLog, true);
            }

            return (preferredMatch?.GameDetail, preferredMatch?.MatchResult.score, isMatch);
        }

        private static bool UpdateMatchWithSecondChance(out MatchDetail preferredMatch, MatchDetail secondChanceMatch, int scoreAdjustment)
        {
            secondChanceMatch.MatchResult.score += scoreAdjustment;
            preferredMatch = secondChanceMatch;
            
            return true;
        }

        public static string LogGameDetail(string name, string description, string manufacturer, string year)
        {
            return $"name={$"'{name}',",-55} description={$"'{description}',",-55} manufacturer={$"'{manufacturer}',",-20} year={$"{year}",-5}";
        }

        public static (bool success, int score) Match(FuzzyNameDetails gameDetailFuzzyDetails, FuzzyNameDetails fileFuzzyDetails)
        {
            var nameMatchScore = GetNameMatchScore(gameDetailFuzzyDetails.Name, gameDetailFuzzyDetails.NameNoWhiteSpace, fileFuzzyDetails.Name, fileFuzzyDetails.NameNoWhiteSpace);
            
            // manufacturer matching is the EXACT same as name matching, but the result is scaled back to 10% to reflect it's lesser importance
            // - the additional scoring though is important to distinguish between games that match exactly but only 1 has the correct manufacturer 
            var manufacturerScore = GetNameMatchScore(gameDetailFuzzyDetails.Manufacturer, gameDetailFuzzyDetails.ManufacturerNoWhiteSpace, fileFuzzyDetails.Manufacturer, fileFuzzyDetails.ManufacturerNoWhiteSpace) / 10;
            
            var yearMatchScore = GetYearMatchScore(gameDetailFuzzyDetails.Year, fileFuzzyDetails.Year);
            var lengthScore = GetLengthMatchScore(gameDetailFuzzyDetails);

            var score = nameMatchScore + manufacturerScore + yearMatchScore + lengthScore;

            // total 'identity check/match' score must be >= _minMatchScore
            return (score >= MinMatchScore, score);
        }

        public static int GetLengthMatchScore(FuzzyNameDetails fuzzyGameDetails)
        {
            // score the match length of the underlying game database entry (i.e. not the file!!)
            // - use the sanitized name to avoid white space, manufacturer, year, etc
            // - 1 for every character beyond 8 characters.. to a maximum of 15pts
            var lengthScore = (fuzzyGameDetails.NameNoWhiteSpace?.Length ?? 0) - 8;

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

        private static MatchDetail GetUniqueMatch(IEnumerable<MatchDetail> orderedMatches, string fuzzyFileName, int? startMatchLength = null)
        {
            if (fuzzyFileName == null)
                return null;

            // only strip file name if a length provided AND it is less than the string length
            if (fuzzyFileName.Length > startMatchLength)
                fuzzyFileName = fuzzyFileName.Remove(startMatchLength.Value);

            // check if we have a match that contains the fuzzy file name in BOTH the table and description
            var matchesContainingFileName = orderedMatches.Where(match => match.GameDetail.Derived.NameLowerCase.Contains(fuzzyFileName) ||
                                                                          match.GameDetail.Derived.DescriptionLowerCase.Contains(fuzzyFileName)).ToList();

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
            // null strings score zero
            if (gameName == null || fileName == null || gameNameNoWhiteSpace == null || fileNameNoWhiteSpace == null)
                return 0;

            // matching order is important.. highest priority matches must be first!
            var score = IsExactMatch(gameName, fileName) ? 150 + ScoringNoWhiteSpaceBonus : 0;
            if (score == 0)
                score = IsExactMatch(gameNameNoWhiteSpace, fileNameNoWhiteSpace) ? 150 : 0;

            // levenshtein distance
            if (score == 0)
                score = IsLevenshteinMatch(14, 2, gameName, fileName) ? 120 + ScoringNoWhiteSpaceBonus : 0;
            if (score == 0)
                score = IsLevenshteinMatch(14, 2, fileNameNoWhiteSpace, gameNameNoWhiteSpace) ? 120 : 0;

            if (score == 0)
                score = IsStartsMatch(14, gameName, fileName) ? 100 + ScoringNoWhiteSpaceBonus : 0;
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

        private static bool IsLevenshteinMatch(int minStringLength, int maxDistance, string first, string second)
        {
            if (minStringLength > first.Length || minStringLength > second.Length)
                return false;

            return LevenshteinDistance.Calculate(first, second) <= maxDistance;
        }

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
            public GameDetail GameDetail;
            public (bool success, int score) MatchResult;
        }

        private static decimal MinMatchScore => Model.Settings.MatchFuzzyMinimumPercentage;
        private static decimal MinMatchWarningScore => MinMatchScore * 1.2m;
        public const int ScoringNoWhiteSpaceBonus = 5;

        private static readonly Regex _fileNameInfoRegex;
        private static readonly Regex _trimCharRegex;
        private static readonly Regex _trimLastPeriodRegex;
        private static readonly Regex _trimWordRegex;
        private static readonly Regex _addSpacingFirstPassRegex;
        private static readonly Regex _addSpacingSecondPassRegex;
        private static readonly Regex _versionRegex;
        private static readonly Regex _preambleRegex;
        private static readonly Regex _multipleWhitespaceRegex;
        private static readonly string[] _titleCaseWordExceptions;
        public static readonly string[] Authors;
    }
}