using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

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

        public static (bool success, int score) Match(string first, (string name, string nameNoWhiteSpace, string manufacturer, int? year) secondFuzzy)
        {
            var firstFuzzy = GetFileDetails(first);

            var nameMatchScore = GetNameMatchScore(firstFuzzy.name, firstFuzzy.nameNoWhiteSpace, secondFuzzy.name, secondFuzzy.nameNoWhiteSpace);
            var yearMatchScore = YearMatchScore(firstFuzzy.year, secondFuzzy.year);
            var score = yearMatchScore + nameMatchScore;

            // total 'identity check/match' score must be 100 or more
            return (score >= 100, score);
        }

        private static int YearMatchScore(int? firstYear, int? secondYear)
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

        private static string Clean(string first, bool removeAllWhiteSpace)
        {
            // clean the string to make it a little cleaner for subsequent matching
            // - order is important!
            var fuzzyClean = first?.ToLower()
                    .Replace("the", "")
                    .Replace("premium", "")
                    .Replace("vpx", "")
                    .Replace("&apos;", "")
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
        // - faster: name via looking for the first opening parenthesis.. https://regex101.com/r/CxKJK1/1
        // - slower: name is greedy search using the last opening parenthesis.. https://regex101.com/r/xiXsML/1.. @"(?<name>.*)\((?<manufacturer>\D*)(?<year>\d*)\).*"
        private static readonly Regex _fuzzyFileNameRegex = new Regex(@"(?<name>[^(]*)\((?<manufacturer>\D*)(?<year>\d*)\)", RegexOptions.Compiled);
    }
}