using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ClrVpin.Models;

namespace ClrVpin.Shared
{
    public static class Fuzzy
    {
        public static (string name, string nameNoWhiteSpace, string manufacturer, int? year) GetFileDetails(string fileName)
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
            name = Clean(name, false);
            var nameNoWhiteSpace = Clean(name, true);

            return (name, nameNoWhiteSpace, manufacturer, year);
        }

        // fuzzy match against all games
        public static Game Match(this IList<Game> games, (string name, string nameNoWhiteSpace, string manufacturer, int? year) fuzzyFileDetails)
        {
            // Check EVERY game so that the most appropriate game is selected
            //   e.g. the 2nd DB entry (i.e. the sequel) should be matched..
            //        - fuzzy file="Cowboy Eight Ball 2"
            //        - DB entries (in order)="Cowboy Eight Ball (LTD do Brasil Divers�es Eletr�nicas Ltda 1981)", "Cowboy Eight Ball 2 (LTD do Brasil Divers�es Eletr�nicas Ltda 1981)"
            // Match table name (non-media) OR description (media)
            var tableMatches = games.Select(game => new { game, match = Match(game.TableFile, fuzzyFileDetails) }).Where(x => x.match.success);
            var descriptionMatches = games.Select(game => new { game, match = Match(game.Description, fuzzyFileDetails) }).Where(x => x.match.success);

            var preferredMatch = tableMatches.Concat(descriptionMatches)
                .OrderByDescending(x => x.match.score)
                .ThenByDescending(x => x.game.TableFile.Length) // tie breaker
                .FirstOrDefault();

            return preferredMatch?.game;
        }

        public static (bool success, int score) Match(string gameDetail, (string name, string nameNoWhiteSpace, string manufacturer, int? year) fileFuzzyDetails)
        {
            var gameDetailFuzzyDetails = GetFileDetails(gameDetail);

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

        private static int GetNameMatchScore(string firstName, string firstNameNoWhiteSpace, string secondName, string secondNameNoWhiteSpace)
        {
            var score = IsExactMatch(firstName, secondName) || IsExactMatch(firstNameNoWhiteSpace, secondNameNoWhiteSpace) ? 150 : 0;
            
            if (score == 0)
                score = IsStartsMatch(14, firstName, secondName) || IsStartsMatch(14, secondNameNoWhiteSpace, firstNameNoWhiteSpace)  ? 100 : 0;
            if (score == 0)
                score = IsStartsMatch(10, firstName, secondName) || IsStartsMatch(10, secondNameNoWhiteSpace, firstNameNoWhiteSpace) ? 60 : 0;
            if (score == 0)
                score = IsStartsMatch(8, firstName, secondName) || IsStartsMatch(8, secondNameNoWhiteSpace, firstNameNoWhiteSpace) ? 50 : 0;

            if (score == 0)
                score = IsContainsMatch(17, firstName, secondName) || IsContainsMatch(17, secondNameNoWhiteSpace, firstNameNoWhiteSpace) ? 100 : 0;
            if (score == 0)
                score = IsContainsMatch(13, firstName, secondName) || IsContainsMatch(13, secondNameNoWhiteSpace, firstNameNoWhiteSpace) ? 60 : 0;
            
            return score;
        }

        private static string ToNull(this string name) => name == "" ? null : name.ToLower().Trim();

        private static bool IsExactMatch(string firstFuzzyName, string secondFuzzyName) => firstFuzzyName == secondFuzzyName;

        private static bool IsContainsMatch(int numCharsToMatch, string firstFuzzyName, string secondFuzzyName) =>
            firstFuzzyName.Length >= numCharsToMatch && secondFuzzyName.Length >= numCharsToMatch && (firstFuzzyName.Contains(secondFuzzyName) || secondFuzzyName.Contains(firstFuzzyName));

        private static bool IsStartsMatch(int numCharsToMatch, string firstFuzzyName, string secondFuzzyName) =>
            firstFuzzyName.Length >= numCharsToMatch && secondFuzzyName.Length >= numCharsToMatch && (firstFuzzyName.StartsWith(secondFuzzyName) || secondFuzzyName.StartsWith(firstFuzzyName));

        private static string Clean(string name, bool removeAllWhiteSpace)
        {
            var fuzzyClean = name;

            // trim starting characters
            if (fuzzyClean?.StartsWith("a ") == true)
                fuzzyClean = fuzzyClean.TrimStart('a');

            // clean the string to make it a little cleaner for subsequent matching
            // - order is important!
            fuzzyClean = fuzzyClean?.ToLower()
                    .Replace(" a ", "")
                    .Replace("and", "")
                    .Replace("the", "")
                    .Replace("premium", "")
                    .Replace("vpx", "")
                    .Replace("&apos;", "")
                    .Replace("jp's", "")
                    .Replace("jps", "")
                    .Replace("ï¿½", "")
                    .Replace("'", "")
                    .Replace("`", "")
                    .Replace("’", "")
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

            if (removeAllWhiteSpace)
                fuzzyClean = fuzzyClean?.Replace(" ", "");

            return fuzzyClean;
        }

        // regex
        // - faster: name via looking for the first opening parenthesis.. https://regex101.com/r/tRqeOH/1
        // - slower: name is greedy search using the last opening parenthesis.. https://regex101.com/r/xiXsML/1.. @"(?<name>.*)\((?<manufacturer>\D*)(?<year>\d*)\).*"
        private static readonly Regex _fuzzyFileNameRegex = new Regex(@"(?<name>[^(]*)\((?<manufacturer>\D*)(?<year>\d*)\D*\)", RegexOptions.Compiled);
    }
}